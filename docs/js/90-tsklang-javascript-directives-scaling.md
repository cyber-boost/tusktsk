# TuskLang JavaScript Documentation: #scaling Directive

## Overview

The `#scaling` directive in TuskLang defines scaling configurations and strategies, enabling declarative application scaling with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#scaling auto
  min_instances: 2
  max_instances: 10
  target_cpu: 70
  target_memory: 80
  scale_up_cooldown: 300
  scale_down_cooldown: 600
  metrics:
    - cpu_utilization
    - memory_usage
    - request_count

#scaling manual
  instances: 5
  health_check: /health
  load_balancer: true
  sticky_sessions: true
  session_timeout: 3600

#scaling scheduled
  schedules:
    - name: business_hours
      cron: "0 9 * * 1-5"
      instances: 8
    - name: weekend
      cron: "0 0 * * 0,6"
      instances: 3
    - name: night
      cron: "0 22 * * *"
      instances: 2

#scaling predictive
  algorithm: linear_regression
  training_data: 30d
  prediction_window: 1h
  confidence_threshold: 0.8
  features:
    - time_of_day
    - day_of_week
    - historical_load
    - external_events
```

## JavaScript Integration

### Auto Scaling Handler

```javascript
// auto-scaling-handler.js
const os = require('os');

class AutoScalingHandler {
  constructor(config) {
    this.config = config;
    this.minInstances = config.min_instances || 2;
    this.maxInstances = config.max_instances || 10;
    this.targetCPU = config.target_cpu || 70;
    this.targetMemory = config.target_memory || 80;
    this.scaleUpCooldown = config.scale_up_cooldown || 300;
    this.scaleDownCooldown = config.scale_down_cooldown || 600;
    this.metrics = config.metrics || ['cpu_utilization', 'memory_usage'];
    
    this.currentInstances = this.minInstances;
    this.lastScaleUp = 0;
    this.lastScaleDown = 0;
    this.metricsHistory = [];
  }

  async getCurrentMetrics() {
    const metrics = {};
    
    if (this.metrics.includes('cpu_utilization')) {
      metrics.cpu_utilization = await this.getCPUUtilization();
    }
    
    if (this.metrics.includes('memory_usage')) {
      metrics.memory_usage = await this.getMemoryUsage();
    }
    
    if (this.metrics.includes('request_count')) {
      metrics.request_count = await this.getRequestCount();
    }
    
    return metrics;
  }

  async getCPUUtilization() {
    const cpus = os.cpus();
    let totalIdle = 0;
    let totalTick = 0;
    
    cpus.forEach(cpu => {
      for (const type in cpu.times) {
        totalTick += cpu.times[type];
      }
      totalIdle += cpu.times.idle;
    });
    
    const idle = totalIdle / cpus.length;
    const total = totalTick / cpus.length;
    const utilization = 100 - (100 * idle / total);
    
    return Math.round(utilization);
  }

  async getMemoryUsage() {
    const totalMem = os.totalmem();
    const freeMem = os.freemem();
    const usedMem = totalMem - freeMem;
    const usage = (usedMem / totalMem) * 100;
    
    return Math.round(usage);
  }

  async getRequestCount() {
    // This would typically come from your application metrics
    // For demo purposes, returning a random number
    return Math.floor(Math.random() * 100);
  }

  async shouldScaleUp(metrics) {
    const now = Date.now();
    
    if (now - this.lastScaleUp < this.scaleUpCooldown * 1000) {
      return false;
    }
    
    if (this.currentInstances >= this.maxInstances) {
      return false;
    }
    
    const cpuHigh = metrics.cpu_utilization > this.targetCPU;
    const memoryHigh = metrics.memory_usage > this.targetMemory;
    
    return cpuHigh || memoryHigh;
  }

  async shouldScaleDown(metrics) {
    const now = Date.now();
    
    if (now - this.lastScaleDown < this.scaleDownCooldown * 1000) {
      return false;
    }
    
    if (this.currentInstances <= this.minInstances) {
      return false;
    }
    
    const cpuLow = metrics.cpu_utilization < (this.targetCPU * 0.5);
    const memoryLow = metrics.memory_usage < (this.targetMemory * 0.5);
    
    return cpuLow && memoryLow;
  }

  async scaleUp() {
    this.currentInstances = Math.min(this.currentInstances + 1, this.maxInstances);
    this.lastScaleUp = Date.now();
    
    console.log(`Scaling up to ${this.currentInstances} instances`);
    
    // Trigger actual scaling logic here
    await this.performScaling('up');
  }

  async scaleDown() {
    this.currentInstances = Math.max(this.currentInstances - 1, this.minInstances);
    this.lastScaleDown = Date.now();
    
    console.log(`Scaling down to ${this.currentInstances} instances`);
    
    // Trigger actual scaling logic here
    await this.performScaling('down');
  }

  async performScaling(direction) {
    // This would integrate with your deployment platform
    // (Docker, Kubernetes, AWS, etc.)
    console.log(`Performing ${direction} scaling operation`);
  }

  async checkScaling() {
    const metrics = await this.getCurrentMetrics();
    this.metricsHistory.push({
      timestamp: Date.now(),
      metrics,
      instances: this.currentInstances
    });
    
    // Keep only last 100 metrics
    if (this.metricsHistory.length > 100) {
      this.metricsHistory.shift();
    }
    
    if (await this.shouldScaleUp(metrics)) {
      await this.scaleUp();
    } else if (await this.shouldScaleDown(metrics)) {
      await this.scaleDown();
    }
    
    return {
      currentInstances: this.currentInstances,
      metrics,
      lastScaleUp: this.lastScaleUp,
      lastScaleDown: this.lastScaleDown
    };
  }

  startAutoScaling(interval = 30000) {
    this.scalingInterval = setInterval(async () => {
      await this.checkScaling();
    }, interval);
    
    console.log(`Auto scaling started with ${interval}ms interval`);
  }

  stopAutoScaling() {
    if (this.scalingInterval) {
      clearInterval(this.scalingInterval);
      this.scalingInterval = null;
      console.log('Auto scaling stopped');
    }
  }

  getScalingHistory() {
    return this.metricsHistory;
  }
}

module.exports = AutoScalingHandler;
```

### Manual Scaling Handler

```javascript
// manual-scaling-handler.js
class ManualScalingHandler {
  constructor(config) {
    this.config = config;
    this.instances = config.instances || 5;
    this.healthCheck = config.health_check || '/health';
    this.loadBalancer = config.load_balancer || false;
    this.stickySessions = config.sticky_sessions || false;
    this.sessionTimeout = config.session_timeout || 3600;
    
    this.instancesList = [];
    this.loadBalancerConfig = null;
  }

  async createInstances() {
    this.instancesList = [];
    
    for (let i = 0; i < this.instances; i++) {
      const instance = await this.createInstance(i);
      this.instancesList.push(instance);
    }
    
    if (this.loadBalancer) {
      this.loadBalancerConfig = await this.setupLoadBalancer();
    }
    
    return this.instancesList;
  }

  async createInstance(index) {
    const instance = {
      id: `instance-${index}`,
      port: 3000 + index,
      status: 'starting',
      health: 'unknown',
      createdAt: new Date()
    };
    
    // Simulate instance creation
    await this.startInstance(instance);
    
    return instance;
  }

  async startInstance(instance) {
    // This would start an actual application instance
    console.log(`Starting instance ${instance.id} on port ${instance.port}`);
    
    // Simulate health check
    setTimeout(async () => {
      instance.status = 'running';
      instance.health = 'healthy';
    }, 2000);
  }

  async setupLoadBalancer() {
    const config = {
      algorithm: this.stickySessions ? 'sticky' : 'round-robin',
      instances: this.instancesList,
      healthCheck: this.healthCheck,
      sessionTimeout: this.sessionTimeout
    };
    
    console.log('Load balancer configured:', config);
    return config;
  }

  async scaleTo(instances) {
    const currentCount = this.instancesList.length;
    
    if (instances > currentCount) {
      // Scale up
      for (let i = currentCount; i < instances; i++) {
        const instance = await this.createInstance(i);
        this.instancesList.push(instance);
      }
    } else if (instances < currentCount) {
      // Scale down
      const instancesToRemove = currentCount - instances;
      for (let i = 0; i < instancesToRemove; i++) {
        const instance = this.instancesList.pop();
        await this.stopInstance(instance);
      }
    }
    
    this.instances = instances;
    
    if (this.loadBalancer) {
      this.loadBalancerConfig = await this.setupLoadBalancer();
    }
    
    return this.instancesList;
  }

  async stopInstance(instance) {
    console.log(`Stopping instance ${instance.id}`);
    instance.status = 'stopping';
    
    // Simulate instance shutdown
    setTimeout(() => {
      instance.status = 'stopped';
    }, 1000);
  }

  async getInstanceHealth(instance) {
    try {
      const response = await fetch(`http://localhost:${instance.port}${this.healthCheck}`);
      instance.health = response.ok ? 'healthy' : 'unhealthy';
      return instance.health;
    } catch (error) {
      instance.health = 'unreachable';
      return instance.health;
    }
  }

  async checkAllInstances() {
    const healthChecks = this.instancesList.map(instance => 
      this.getInstanceHealth(instance)
    );
    
    await Promise.all(healthChecks);
    
    const healthyInstances = this.instancesList.filter(instance => 
      instance.health === 'healthy'
    );
    
    return {
      total: this.instancesList.length,
      healthy: healthyInstances.length,
      instances: this.instancesList
    };
  }

  async removeUnhealthyInstances() {
    const unhealthyInstances = this.instancesList.filter(instance => 
      instance.health !== 'healthy'
    );
    
    for (const instance of unhealthyInstances) {
      await this.stopInstance(instance);
      this.instancesList = this.instancesList.filter(i => i.id !== instance.id);
    }
    
    // Replace unhealthy instances
    const neededInstances = this.instances - this.instancesList.length;
    for (let i = 0; i < neededInstances; i++) {
      const instance = await this.createInstance(this.instancesList.length);
      this.instancesList.push(instance);
    }
    
    return this.instancesList;
  }
}

module.exports = ManualScalingHandler;
```

### Scheduled Scaling Handler

```javascript
// scheduled-scaling-handler.js
const cron = require('node-cron');

class ScheduledScalingHandler {
  constructor(config) {
    this.config = config;
    this.schedules = config.schedules || [];
    this.currentSchedule = null;
    this.scalingHandler = null;
    this.cronJobs = new Map();
  }

  setScalingHandler(handler) {
    this.scalingHandler = handler;
  }

  async startScheduledScaling() {
    for (const schedule of this.schedules) {
      const job = cron.schedule(schedule.cron, async () => {
        await this.executeSchedule(schedule);
      }, {
        scheduled: true,
        timezone: 'UTC'
      });
      
      this.cronJobs.set(schedule.name, job);
      console.log(`Scheduled scaling '${schedule.name}' with cron: ${schedule.cron}`);
    }
  }

  async executeSchedule(schedule) {
    if (!this.scalingHandler) {
      throw new Error('Scaling handler not set');
    }
    
    console.log(`Executing schedule: ${schedule.name} -> ${schedule.instances} instances`);
    
    try {
      await this.scalingHandler.scaleTo(schedule.instances);
      this.currentSchedule = schedule;
      
      console.log(`Successfully scaled to ${schedule.instances} instances`);
    } catch (error) {
      console.error(`Failed to execute schedule ${schedule.name}:`, error);
    }
  }

  async stopScheduledScaling() {
    for (const [name, job] of this.cronJobs.entries()) {
      job.stop();
      console.log(`Stopped scheduled scaling: ${name}`);
    }
    
    this.cronJobs.clear();
  }

  async getNextSchedules() {
    const now = new Date();
    const nextSchedules = [];
    
    for (const schedule of this.schedules) {
      const nextRun = this.getNextCronRun(schedule.cron);
      nextSchedules.push({
        name: schedule.name,
        instances: schedule.instances,
        nextRun,
        cron: schedule.cron
      });
    }
    
    return nextSchedules.sort((a, b) => a.nextRun - b.nextRun);
  }

  getNextCronRun(cronExpression) {
    // This is a simplified implementation
    // In production, you'd use a proper cron parser
    const now = new Date();
    const nextHour = new Date(now.getTime() + 60 * 60 * 1000);
    return nextHour;
  }

  async getCurrentSchedule() {
    return this.currentSchedule;
  }

  async addSchedule(name, cron, instances) {
    const schedule = { name, cron, instances };
    this.schedules.push(schedule);
    
    const job = cron.schedule(cron, async () => {
      await this.executeSchedule(schedule);
    });
    
    this.cronJobs.set(name, job);
    
    return schedule;
  }

  async removeSchedule(name) {
    const job = this.cronJobs.get(name);
    if (job) {
      job.stop();
      this.cronJobs.delete(name);
    }
    
    this.schedules = this.schedules.filter(s => s.name !== name);
  }
}

module.exports = ScheduledScalingHandler;
```

### Predictive Scaling Handler

```javascript
// predictive-scaling-handler.js
class PredictiveScalingHandler {
  constructor(config) {
    this.config = config;
    this.algorithm = config.algorithm || 'linear_regression';
    this.trainingData = config.training_data || '30d';
    this.predictionWindow = config.prediction_window || '1h';
    this.confidenceThreshold = config.confidence_threshold || 0.8;
    this.features = config.features || ['time_of_day', 'day_of_week', 'historical_load'];
    
    this.model = null;
    this.trainingData = [];
    this.predictions = [];
  }

  async trainModel() {
    const historicalData = await this.getHistoricalData();
    
    switch (this.algorithm) {
      case 'linear_regression':
        this.model = await this.trainLinearRegression(historicalData);
        break;
      case 'neural_network':
        this.model = await this.trainNeuralNetwork(historicalData);
        break;
      default:
        throw new Error(`Unsupported algorithm: ${this.algorithm}`);
    }
    
    console.log(`Model trained with ${historicalData.length} data points`);
  }

  async getHistoricalData() {
    // This would fetch historical load data from your metrics system
    const days = parseInt(this.trainingData);
    const data = [];
    
    for (let i = 0; i < days; i++) {
      const date = new Date(Date.now() - i * 24 * 60 * 60 * 1000);
      
      for (let hour = 0; hour < 24; hour++) {
        data.push({
          timestamp: new Date(date.getTime() + hour * 60 * 60 * 1000),
          load: this.generateHistoricalLoad(hour, date.getDay()),
          instances: this.calculateRequiredInstances(this.generateHistoricalLoad(hour, date.getDay()))
        });
      }
    }
    
    return data;
  }

  generateHistoricalLoad(hour, dayOfWeek) {
    // Simulate realistic load patterns
    const baseLoad = 50;
    const hourFactor = this.getHourFactor(hour);
    const dayFactor = this.getDayFactor(dayOfWeek);
    const noise = (Math.random() - 0.5) * 20;
    
    return Math.max(0, baseLoad * hourFactor * dayFactor + noise);
  }

  getHourFactor(hour) {
    // Peak hours: 9-17, low hours: 0-6
    if (hour >= 9 && hour <= 17) return 1.5;
    if (hour >= 0 && hour <= 6) return 0.3;
    return 0.8;
  }

  getDayFactor(dayOfWeek) {
    // Weekdays: higher load, weekends: lower load
    if (dayOfWeek >= 1 && dayOfWeek <= 5) return 1.0;
    return 0.5;
  }

  calculateRequiredInstances(load) {
    const instancesPerLoad = 0.1; // 1 instance per 10 load units
    return Math.ceil(load * instancesPerLoad);
  }

  async trainLinearRegression(data) {
    // Simplified linear regression implementation
    const features = data.map(d => this.extractFeatures(d.timestamp));
    const targets = data.map(d => d.instances);
    
    // Calculate coefficients (simplified)
    const coefficients = this.calculateCoefficients(features, targets);
    
    return {
      type: 'linear_regression',
      coefficients,
      predict: (features) => this.predictLinearRegression(features, coefficients)
    };
  }

  extractFeatures(timestamp) {
    return {
      time_of_day: timestamp.getHours(),
      day_of_week: timestamp.getDay(),
      hour_sin: Math.sin(2 * Math.PI * timestamp.getHours() / 24),
      hour_cos: Math.cos(2 * Math.PI * timestamp.getHours() / 24),
      day_sin: Math.sin(2 * Math.PI * timestamp.getDay() / 7),
      day_cos: Math.cos(2 * Math.PI * timestamp.getDay() / 7)
    };
  }

  calculateCoefficients(features, targets) {
    // Simplified coefficient calculation
    // In production, use a proper linear algebra library
    return {
      time_of_day: 0.1,
      day_of_week: -0.2,
      hour_sin: 0.3,
      hour_cos: 0.2,
      day_sin: 0.1,
      day_cos: -0.1,
      intercept: 3.0
    };
  }

  predictLinearRegression(features, coefficients) {
    let prediction = coefficients.intercept;
    
    for (const [feature, value] of Object.entries(features)) {
      if (coefficients[feature]) {
        prediction += coefficients[feature] * value;
      }
    }
    
    return Math.max(1, Math.round(prediction));
  }

  async trainNeuralNetwork(data) {
    // This would use a proper ML library like TensorFlow.js
    console.log('Neural network training not implemented');
    return null;
  }

  async predictLoad(timestamp) {
    if (!this.model) {
      await this.trainModel();
    }
    
    const features = this.extractFeatures(timestamp);
    const prediction = this.model.predict(features);
    
    const confidence = this.calculateConfidence(prediction);
    
    if (confidence < this.confidenceThreshold) {
      console.warn(`Low confidence prediction: ${confidence}`);
    }
    
    this.predictions.push({
      timestamp,
      predictedInstances: prediction,
      confidence
    });
    
    return {
      predictedInstances: prediction,
      confidence,
      features
    };
  }

  calculateConfidence(prediction) {
    // Simplified confidence calculation
    // In production, use proper statistical methods
    return 0.85 + Math.random() * 0.1;
  }

  async getPredictions() {
    return this.predictions;
  }

  async updateModel() {
    // Retrain model with new data
    await this.trainModel();
  }
}

module.exports = PredictiveScalingHandler;
```

## TypeScript Implementation

```typescript
// scaling-handler.types.ts
export interface ScalingConfig {
  min_instances?: number;
  max_instances?: number;
  target_cpu?: number;
  target_memory?: number;
  scale_up_cooldown?: number;
  scale_down_cooldown?: number;
  metrics?: string[];
  instances?: number;
  health_check?: string;
  load_balancer?: boolean;
  sticky_sessions?: boolean;
  session_timeout?: number;
  schedules?: Array<{
    name: string;
    cron: string;
    instances: number;
  }>;
  algorithm?: string;
  training_data?: string;
  prediction_window?: string;
  confidence_threshold?: number;
  features?: string[];
}

export interface ScalingResult {
  success: boolean;
  currentInstances: number;
  targetInstances?: number;
  message?: string;
  error?: string;
}

export interface ScalingHandler {
  scaleTo(instances: number): Promise<ScalingResult>;
  getStatus(): Promise<any>;
  startAutoScaling?(): Promise<void>;
  stopAutoScaling?(): Promise<void>;
}

// scaling-handler.ts
import { ScalingConfig, ScalingHandler, ScalingResult } from './scaling-handler.types';

export class TypeScriptScalingHandler implements ScalingHandler {
  protected config: ScalingConfig;
  protected currentInstances: number;

  constructor(config: ScalingConfig) {
    this.config = config;
    this.currentInstances = config.instances || 2;
  }

  async scaleTo(instances: number): Promise<ScalingResult> {
    throw new Error('Method not implemented');
  }

  async getStatus(): Promise<any> {
    throw new Error('Method not implemented');
  }
}

export class TypeScriptAutoScalingHandler extends TypeScriptScalingHandler {
  private minInstances: number;
  private maxInstances: number;
  private targetCPU: number;
  private scalingInterval: NodeJS.Timeout | null = null;

  constructor(config: ScalingConfig) {
    super(config);
    this.minInstances = config.min_instances || 2;
    this.maxInstances = config.max_instances || 10;
    this.targetCPU = config.target_cpu || 70;
  }

  async scaleTo(instances: number): Promise<ScalingResult> {
    const targetInstances = Math.max(this.minInstances, Math.min(instances, this.maxInstances));
    
    if (targetInstances === this.currentInstances) {
      return {
        success: true,
        currentInstances: this.currentInstances,
        message: 'No scaling needed'
      };
    }
    
    // Perform actual scaling
    this.currentInstances = targetInstances;
    
    return {
      success: true,
      currentInstances: this.currentInstances,
      targetInstances,
      message: `Scaled to ${targetInstances} instances`
    };
  }

  async getStatus(): Promise<any> {
    return {
      currentInstances: this.currentInstances,
      minInstances: this.minInstances,
      maxInstances: this.maxInstances,
      targetCPU: this.targetCPU
    };
  }

  async startAutoScaling(): Promise<void> {
    this.scalingInterval = setInterval(async () => {
      await this.checkScaling();
    }, 30000);
  }

  async stopAutoScaling(): Promise<void> {
    if (this.scalingInterval) {
      clearInterval(this.scalingInterval);
      this.scalingInterval = null;
    }
  }

  private async checkScaling(): Promise<void> {
    const cpuUsage = await this.getCPUUsage();
    
    if (cpuUsage > this.targetCPU && this.currentInstances < this.maxInstances) {
      await this.scaleTo(this.currentInstances + 1);
    } else if (cpuUsage < this.targetCPU * 0.5 && this.currentInstances > this.minInstances) {
      await this.scaleTo(this.currentInstances - 1);
    }
  }

  private async getCPUUsage(): Promise<number> {
    // Simplified CPU usage calculation
    return Math.random() * 100;
  }
}
```

## Advanced Usage Scenarios

### Multi-Strategy Scaling

```javascript
// multi-strategy-scaling.js
class MultiStrategyScaling {
  constructor(configs) {
    this.strategies = new Map();
    this.initializeStrategies(configs);
  }

  initializeStrategies(configs) {
    if (configs.auto) {
      const AutoScalingHandler = require('./auto-scaling-handler');
      this.strategies.set('auto', new AutoScalingHandler(configs.auto));
    }

    if (configs.manual) {
      const ManualScalingHandler = require('./manual-scaling-handler');
      this.strategies.set('manual', new ManualScalingHandler(configs.manual));
    }

    if (configs.scheduled) {
      const ScheduledScalingHandler = require('./scheduled-scaling-handler');
      this.strategies.set('scheduled', new ScheduledScalingHandler(configs.scheduled));
    }

    if (configs.predictive) {
      const PredictiveScalingHandler = require('./predictive-scaling-handler');
      this.strategies.set('predictive', new PredictiveScalingHandler(configs.predictive));
    }
  }

  async scaleWithStrategy(strategy, instances) {
    const handler = this.strategies.get(strategy);
    if (!handler) {
      throw new Error(`Strategy '${strategy}' not found`);
    }
    
    return await handler.scaleTo(instances);
  }

  async getStatusFromAll() {
    const statuses = {};
    
    for (const [strategy, handler] of this.strategies.entries()) {
      try {
        statuses[strategy] = await handler.getStatus();
      } catch (error) {
        statuses[strategy] = { error: error.message };
      }
    }
    
    return statuses;
  }

  async startAllAutoScaling() {
    for (const [strategy, handler] of this.strategies.entries()) {
      if (handler.startAutoScaling) {
        await handler.startAutoScaling();
      }
    }
  }

  async stopAllAutoScaling() {
    for (const [strategy, handler] of this.strategies.entries()) {
      if (handler.stopAutoScaling) {
        await handler.stopAutoScaling();
      }
    }
  }
}
```

### Load-Based Scaling

```javascript
// load-based-scaling.js
class LoadBasedScaling {
  constructor(scalingHandler) {
    this.scalingHandler = scalingHandler;
    this.loadThresholds = {
      low: 30,
      medium: 60,
      high: 80,
      critical: 95
    };
  }

  async checkLoadAndScale() {
    const load = await this.getCurrentLoad();
    const currentInstances = await this.scalingHandler.getStatus();
    
    let targetInstances = currentInstances.currentInstances;
    
    if (load > this.loadThresholds.critical) {
      targetInstances = Math.min(targetInstances + 3, 20);
    } else if (load > this.loadThresholds.high) {
      targetInstances = Math.min(targetInstances + 2, 15);
    } else if (load > this.loadThresholds.medium) {
      targetInstances = Math.min(targetInstances + 1, 10);
    } else if (load < this.loadThresholds.low) {
      targetInstances = Math.max(targetInstances - 1, 2);
    }
    
    if (targetInstances !== currentInstances.currentInstances) {
      await this.scalingHandler.scaleTo(targetInstances);
    }
    
    return {
      currentLoad: load,
      currentInstances: currentInstances.currentInstances,
      targetInstances,
      loadLevel: this.getLoadLevel(load)
    };
  }

  async getCurrentLoad() {
    // This would get actual load from your metrics system
    return Math.random() * 100;
  }

  getLoadLevel(load) {
    if (load > this.loadThresholds.critical) return 'critical';
    if (load > this.loadThresholds.high) return 'high';
    if (load > this.loadThresholds.medium) return 'medium';
    if (load > this.loadThresholds.low) return 'low';
    return 'very_low';
  }
}
```

## Real-World Examples

### Express.js Scaling Setup

```javascript
// express-scaling-setup.js
const express = require('express');
const MultiStrategyScaling = require('./multi-strategy-scaling');

class ExpressScalingSetup {
  constructor(app, config) {
    this.app = app;
    this.scaling = new MultiStrategyScaling(config);
  }

  setupScalingEndpoints() {
    this.app.get('/scaling/status', async (req, res) => {
      try {
        const status = await this.scaling.getStatusFromAll();
        res.json(status);
      } catch (error) {
        res.status(500).json({ error: error.message });
      }
    });

    this.app.post('/scaling/scale', async (req, res) => {
      try {
        const { strategy, instances } = req.body;
        const result = await this.scaling.scaleWithStrategy(strategy, instances);
        res.json(result);
      } catch (error) {
        res.status(500).json({ error: error.message });
      }
    });

    this.app.post('/scaling/start', async (req, res) => {
      try {
        await this.scaling.startAllAutoScaling();
        res.json({ message: 'Auto scaling started' });
      } catch (error) {
        res.status(500).json({ error: error.message });
      }
    });

    this.app.post('/scaling/stop', async (req, res) => {
      try {
        await this.scaling.stopAllAutoScaling();
        res.json({ message: 'Auto scaling stopped' });
      } catch (error) {
        res.status(500).json({ error: error.message });
      }
    });
  }
}

// Usage
const app = express();
const scalingConfig = {
  auto: {
    min_instances: 2,
    max_instances: 10,
    target_cpu: 70
  },
  scheduled: {
    schedules: [
      { name: 'business_hours', cron: '0 9 * * 1-5', instances: 8 },
      { name: 'weekend', cron: '0 0 * * 0,6', instances: 3 }
    ]
  }
};

const scalingSetup = new ExpressScalingSetup(app, scalingConfig);
scalingSetup.setupScalingEndpoints();
```

### Microservices Scaling

```javascript
// microservices-scaling.js
class MicroservicesScaling {
  constructor() {
    this.services = new Map();
    this.scalingHandlers = new Map();
  }

  addService(name, scalingHandler) {
    this.services.set(name, { name, scalingHandler });
  }

  async scaleService(name, instances) {
    const service = this.services.get(name);
    if (!service) {
      throw new Error(`Service '${name}' not found`);
    }
    
    return await service.scalingHandler.scaleTo(instances);
  }

  async scaleAllServices(instances) {
    const results = {};
    
    for (const [name, service] of this.services.entries()) {
      try {
        results[name] = await service.scalingHandler.scaleTo(instances);
      } catch (error) {
        results[name] = { success: false, error: error.message };
      }
    }
    
    return results;
  }

  async getServiceStatus(name) {
    const service = this.services.get(name);
    if (!service) {
      throw new Error(`Service '${name}' not found`);
    }
    
    return await service.scalingHandler.getStatus();
  }

  async getAllStatuses() {
    const statuses = {};
    
    for (const [name, service] of this.services.entries()) {
      statuses[name] = await service.scalingHandler.getStatus();
    }
    
    return statuses;
  }
}

// Usage
const microservices = new MicroservicesScaling();

const apiScaling = new AutoScalingHandler({ min_instances: 2, max_instances: 10 });
const workerScaling = new ManualScalingHandler({ instances: 5 });
const frontendScaling = new ScheduledScalingHandler({
  schedules: [
    { name: 'peak', cron: '0 9 * * 1-5', instances: 8 },
    { name: 'off_peak', cron: '0 18 * * 1-5', instances: 3 }
  ]
});

microservices.addService('api', apiScaling);
microservices.addService('worker', workerScaling);
microservices.addService('frontend', frontendScaling);

await microservices.scaleService('api', 5);
await microservices.getAllStatuses();
```

## Performance Considerations

### Scaling Metrics Collection

```javascript
// scaling-metrics.js
class ScalingMetrics {
  constructor() {
    this.metrics = {
      scalingEvents: [],
      performance: {
        avgScalingTime: 0,
        totalScalingEvents: 0
      }
    };
  }

  recordScalingEvent(from, to, duration, success) {
    const event = {
      timestamp: Date.now(),
      from,
      to,
      duration,
      success
    };
    
    this.metrics.scalingEvents.push(event);
    
    // Update performance metrics
    this.metrics.performance.totalScalingEvents++;
    this.metrics.performance.avgScalingTime = 
      (this.metrics.performance.avgScalingTime * (this.metrics.performance.totalScalingEvents - 1) + duration) / 
      this.metrics.performance.totalScalingEvents;
    
    // Keep only last 1000 events
    if (this.metrics.scalingEvents.length > 1000) {
      this.metrics.scalingEvents.shift();
    }
  }

  getMetrics() {
    return this.metrics;
  }

  getScalingHistory(hours = 24) {
    const cutoff = Date.now() - hours * 60 * 60 * 1000;
    return this.metrics.scalingEvents.filter(event => event.timestamp > cutoff);
  }
}
```

### Scaling Optimization

```javascript
// scaling-optimizer.js
class ScalingOptimizer {
  constructor(scalingHandler) {
    this.scalingHandler = scalingHandler;
    this.optimizationRules = [];
  }

  addOptimizationRule(rule) {
    this.optimizationRules.push(rule);
  }

  async optimizeScaling() {
    const status = await this.scalingHandler.getStatus();
    let optimizedInstances = status.currentInstances;
    
    for (const rule of this.optimizationRules) {
      const result = rule.evaluate(status);
      if (result.shouldOptimize) {
        optimizedInstances = result.recommendedInstances;
      }
    }
    
    if (optimizedInstances !== status.currentInstances) {
      await this.scalingHandler.scaleTo(optimizedInstances);
    }
    
    return {
      currentInstances: status.currentInstances,
      optimizedInstances,
      optimizations: this.optimizationRules.map(rule => rule.name)
    };
  }
}
```

## Security Notes

### Scaling Security

```javascript
// scaling-security.js
class ScalingSecurity {
  constructor() {
    this.maxInstances = 100;
    this.minInstances = 1;
    this.scalingCooldown = 60000; // 1 minute
    this.lastScaling = 0;
  }

  validateScalingRequest(instances) {
    if (instances < this.minInstances || instances > this.maxInstances) {
      throw new Error(`Invalid instance count: ${instances}`);
    }
    
    const now = Date.now();
    if (now - this.lastScaling < this.scalingCooldown) {
      throw new Error('Scaling too frequent');
    }
    
    this.lastScaling = now;
    return true;
  }

  sanitizeScalingConfig(config) {
    // Remove sensitive information from scaling config
    const sanitized = { ...config };
    delete sanitized.secrets;
    delete sanitized.apiKeys;
    return sanitized;
  }
}
```

## Best Practices

### Scaling Configuration Management

```javascript
// scaling-config-manager.js
class ScalingConfigManager {
  constructor() {
    this.configs = new Map();
  }

  setConfig(environment, config) {
    this.configs.set(environment, this.validateConfig(config));
  }

  getConfig(environment) {
    const config = this.configs.get(environment);
    if (!config) {
      throw new Error(`No configuration found for environment: ${environment}`);
    }
    return config;
  }

  validateConfig(config) {
    if (!config.strategy) {
      throw new Error('Scaling strategy is required');
    }
    
    if (config.min_instances && config.max_instances) {
      if (config.min_instances > config.max_instances) {
        throw new Error('min_instances cannot be greater than max_instances');
      }
    }
    
    return config;
  }

  getCurrentConfig() {
    const environment = process.env.NODE_ENV || 'development';
    return this.getConfig(environment);
  }
}
```

### Scaling Health Monitoring

```javascript
// scaling-health-monitor.js
class ScalingHealthMonitor {
  constructor(scalingHandler) {
    this.scalingHandler = scalingHandler;
    this.metrics = {
      scalingEvents: 0,
      failures: 0,
      avgScalingTime: 0
    };
  }

  async checkHealth() {
    try {
      const start = Date.now();
      const status = await this.scalingHandler.getStatus();
      const duration = Date.now() - start;
      
      this.metrics.scalingEvents++;
      this.metrics.avgScalingTime = 
        (this.metrics.avgScalingTime * (this.metrics.scalingEvents - 1) + duration) / 
        this.metrics.scalingEvents;
      
      return {
        status: 'healthy',
        metrics: this.metrics,
        currentStatus: status
      };
    } catch (error) {
      this.metrics.failures++;
      return {
        status: 'unhealthy',
        error: error.message,
        metrics: this.metrics
      };
    }
  }

  getMetrics() {
    return this.metrics;
  }
}
```

## Related Topics

- [@scale Operator](./60-tsklang-javascript-operator-scale.md)
- [@deploy Operator](./59-tsklang-javascript-operator-deploy.md)
- [@monitor Operator](./51-tsklang-javascript-operator-monitor.md)
- [@metrics Operator](./49-tsklang-javascript-operator-metrics.md)
- [@deployment Directive](./89-tsklang-javascript-directives-deployment.md) 