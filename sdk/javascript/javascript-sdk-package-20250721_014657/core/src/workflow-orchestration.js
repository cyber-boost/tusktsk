/**
 * TuskLang Advanced Workflow and Task Orchestration System
 * Provides comprehensive workflow management and task orchestration capabilities
 */

const { EventEmitter } = require('events');

class WorkflowEngine {
  constructor(options = {}) {
    this.options = {
      maxConcurrentTasks: options.maxConcurrentTasks || 10,
      taskTimeout: options.taskTimeout || 300000, // 5 minutes
      retryAttempts: options.retryAttempts || 3,
      retryDelay: options.retryDelay || 1000,
      ...options
    };
    
    this.workflows = new Map();
    this.tasks = new Map();
    this.executions = new Map();
    this.runningTasks = new Set();
    this.eventEmitter = new EventEmitter();
    this.isRunning = false;
  }

  /**
   * Register a workflow
   */
  registerWorkflow(name, definition) {
    const workflow = {
      name,
      definition: { ...definition },
      tasks: definition.tasks || [],
      dependencies: definition.dependencies || {},
      config: definition.config || {},
      metrics: {
        executions: 0,
        successful: 0,
        failed: 0,
        averageExecutionTime: 0
      }
    };

    this.workflows.set(name, workflow);
    return workflow;
  }

  /**
   * Register a task
   */
  registerTask(name, taskFunction, options = {}) {
    const task = {
      name,
      function: taskFunction,
      options: {
        timeout: options.timeout || this.options.taskTimeout,
        retryAttempts: options.retryAttempts || this.options.retryAttempts,
        retryDelay: options.retryDelay || this.options.retryDelay,
        ...options
      }
    };

    this.tasks.set(name, task);
    return task;
  }

  /**
   * Execute a workflow
   */
  async executeWorkflow(workflowName, input = {}, options = {}) {
    const workflow = this.workflows.get(workflowName);
    if (!workflow) {
      throw new Error(`Workflow not found: ${workflowName}`);
    }

    const executionId = this.generateExecutionId();
    const execution = {
      id: executionId,
      workflowName,
      input,
      options,
      status: 'running',
      startTime: Date.now(),
      endTime: null,
      results: {},
      errors: [],
      taskResults: new Map()
    };

    this.executions.set(executionId, execution);
    workflow.metrics.executions++;

    try {
      const result = await this.executeWorkflowTasks(workflow, execution);
      
      execution.status = 'completed';
      execution.endTime = Date.now();
      execution.results = result;
      
      workflow.metrics.successful++;
      const executionTime = execution.endTime - execution.startTime;
      workflow.metrics.averageExecutionTime = 
        (workflow.metrics.averageExecutionTime * (workflow.metrics.successful - 1) + executionTime) / 
        workflow.metrics.successful;

      this.eventEmitter.emit('workflowCompleted', { executionId, result });
      return result;
    } catch (error) {
      execution.status = 'failed';
      execution.endTime = Date.now();
      execution.errors.push(error.message);
      
      workflow.metrics.failed++;
      this.eventEmitter.emit('workflowFailed', { executionId, error });
      throw error;
    }
  }

  /**
   * Execute workflow tasks
   */
  async executeWorkflowTasks(workflow, execution) {
    const results = {};
    const completedTasks = new Set();
    const runningTasks = new Map();

    // Execute tasks based on dependencies
    while (completedTasks.size < workflow.tasks.length) {
      const availableTasks = this.getAvailableTasks(workflow, completedTasks, runningTasks);
      
      if (availableTasks.length === 0 && runningTasks.size === 0) {
        throw new Error('Circular dependency detected in workflow');
      }

      // Execute available tasks
      for (const taskName of availableTasks) {
        if (runningTasks.size >= this.options.maxConcurrentTasks) {
          break;
        }

        const taskPromise = this.executeTask(taskName, execution, results);
        runningTasks.set(taskName, taskPromise);
      }

      // Wait for at least one task to complete
      if (runningTasks.size > 0) {
        const [completedTaskName, result] = await Promise.race(
          Array.from(runningTasks.entries()).map(async ([taskName, promise]) => {
            try {
              const result = await promise;
              return [taskName, result];
            } catch (error) {
              throw { taskName, error };
            }
          })
        );

        runningTasks.delete(completedTaskName);
        completedTasks.add(completedTaskName);
        results[completedTaskName] = result;
        execution.taskResults.set(completedTaskName, result);
      }
    }

    return results;
  }

  /**
   * Get available tasks based on dependencies
   */
  getAvailableTasks(workflow, completedTasks, runningTasks) {
    return workflow.tasks.filter(taskName => {
      // Skip if already completed or running
      if (completedTasks.has(taskName) || runningTasks.has(taskName)) {
        return false;
      }

      // Check dependencies
      const dependencies = workflow.dependencies[taskName] || [];
      return dependencies.every(dep => completedTasks.has(dep));
    });
  }

  /**
   * Execute a single task
   */
  async executeTask(taskName, execution, results) {
    const task = this.tasks.get(taskName);
    if (!task) {
      throw new Error(`Task not found: ${taskName}`);
    }

    let lastError;
    
    for (let attempt = 1; attempt <= task.options.retryAttempts; attempt++) {
      try {
        const taskResult = await this.executeTaskWithTimeout(
          task.function,
          { ...execution.input, ...results },
          task.options.timeout
        );

        this.eventEmitter.emit('taskCompleted', { 
          executionId: execution.id, 
          taskName, 
          result: taskResult,
          attempt 
        });

        return taskResult;
      } catch (error) {
        lastError = error;
        
        this.eventEmitter.emit('taskFailed', { 
          executionId: execution.id, 
          taskName, 
          error: error.message,
          attempt 
        });

        if (attempt < task.options.retryAttempts) {
          await this.delay(task.options.retryDelay * attempt);
        }
      }
    }

    throw new Error(`Task ${taskName} failed after ${task.options.retryAttempts} attempts: ${lastError.message}`);
  }

  /**
   * Execute task with timeout
   */
  async executeTaskWithTimeout(taskFunction, input, timeout) {
    return new Promise((resolve, reject) => {
      const timeoutId = setTimeout(() => {
        reject(new Error('Task timeout'));
      }, timeout);

      try {
        const result = taskFunction(input);
        if (result instanceof Promise) {
          result.then(resolve).catch(reject).finally(() => clearTimeout(timeoutId));
        } else {
          clearTimeout(timeoutId);
          resolve(result);
        }
      } catch (error) {
        clearTimeout(timeoutId);
        reject(error);
      }
    });
  }

  /**
   * Get execution status
   */
  getExecutionStatus(executionId) {
    const execution = this.executions.get(executionId);
    if (!execution) {
      return null;
    }

    return {
      id: execution.id,
      workflowName: execution.workflowName,
      status: execution.status,
      startTime: execution.startTime,
      endTime: execution.endTime,
      duration: execution.endTime ? execution.endTime - execution.startTime : null,
      taskResults: Object.fromEntries(execution.taskResults),
      errors: execution.errors
    };
  }

  /**
   * Get workflow statistics
   */
  getWorkflowStats(workflowName) {
    const workflow = this.workflows.get(workflowName);
    if (!workflow) {
      return null;
    }

    return {
      name: workflow.name,
      tasks: workflow.tasks.length,
      metrics: { ...workflow.metrics }
    };
  }

  /**
   * Cancel execution
   */
  cancelExecution(executionId) {
    const execution = this.executions.get(executionId);
    if (!execution || execution.status !== 'running') {
      return false;
    }

    execution.status = 'cancelled';
    execution.endTime = Date.now();
    
    this.eventEmitter.emit('executionCancelled', { executionId });
    return true;
  }

  /**
   * Delay utility
   */
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * Generate execution ID
   */
  generateExecutionId() {
    return `exec_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}

class TaskScheduler {
  constructor(options = {}) {
    this.options = {
      maxConcurrentJobs: options.maxConcurrentJobs || 5,
      defaultInterval: options.defaultInterval || 60000, // 1 minute
      ...options
    };
    
    this.schedules = new Map();
    this.jobs = new Map();
    this.runningJobs = new Set();
    this.eventEmitter = new EventEmitter();
  }

  /**
   * Schedule a task
   */
  scheduleTask(name, taskFunction, schedule, options = {}) {
    const job = {
      name,
      function: taskFunction,
      schedule: this.parseSchedule(schedule),
      options: {
        interval: options.interval || this.options.defaultInterval,
        maxRetries: options.maxRetries || 3,
        ...options
      },
      lastRun: null,
      nextRun: null,
      metrics: {
        runs: 0,
        successful: 0,
        failed: 0
      }
    };

    this.jobs.set(name, job);
    this.updateNextRun(job);
    return job;
  }

  /**
   * Parse schedule expression
   */
  parseSchedule(schedule) {
    if (typeof schedule === 'string') {
      // Simple cron-like parsing (simplified)
      const parts = schedule.split(' ');
      return {
        type: 'cron',
        expression: schedule,
        parts
      };
    }

    if (typeof schedule === 'number') {
      return {
        type: 'interval',
        interval: schedule
      };
    }

    return schedule;
  }

  /**
   * Update next run time
   */
  updateNextRun(job) {
    const now = Date.now();
    
    if (job.schedule.type === 'interval') {
      job.nextRun = now + job.schedule.interval;
    } else if (job.schedule.type === 'cron') {
      // Simplified cron calculation
      job.nextRun = now + job.options.interval;
    }
  }

  /**
   * Start the scheduler
   */
  start() {
    this.isRunning = true;
    this.scheduleLoop();
  }

  /**
   * Stop the scheduler
   */
  stop() {
    this.isRunning = false;
  }

  /**
   * Main scheduling loop
   */
  async scheduleLoop() {
    while (this.isRunning) {
      const now = Date.now();
      const dueJobs = [];

      // Find due jobs
      for (const [name, job] of this.jobs) {
        if (job.nextRun && job.nextRun <= now && !this.runningJobs.has(name)) {
          dueJobs.push({ name, job });
        }
      }

      // Execute due jobs
      for (const { name, job } of dueJobs) {
        if (this.runningJobs.size >= this.options.maxConcurrentJobs) {
          break;
        }

        this.executeJob(name, job);
      }

      // Wait before next check
      await this.delay(1000);
    }
  }

  /**
   * Execute a scheduled job
   */
  async executeJob(name, job) {
    this.runningJobs.add(name);
    job.lastRun = Date.now();
    job.metrics.runs++;

    try {
      await job.function();
      job.metrics.successful++;
      this.eventEmitter.emit('jobCompleted', { name, job });
    } catch (error) {
      job.metrics.failed++;
      this.eventEmitter.emit('jobFailed', { name, job, error });
    } finally {
      this.runningJobs.delete(name);
      this.updateNextRun(job);
    }
  }

  /**
   * Get job statistics
   */
  getJobStats(jobName) {
    const job = this.jobs.get(jobName);
    if (!job) {
      return null;
    }

    return {
      name: job.name,
      lastRun: job.lastRun,
      nextRun: job.nextRun,
      metrics: { ...job.metrics }
    };
  }

  /**
   * Delay utility
   */
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

module.exports = {
  WorkflowEngine,
  TaskScheduler
}; 