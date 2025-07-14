# @workflow Operator - Business Process Automation

## Overview
The `@workflow` operator in TuskLang enables the creation of complex business workflows and process automation. It supports sequential, parallel, and conditional execution paths with state management and error handling.

## TuskLang Syntax

### Basic Workflow
```tusk
# Simple approval workflow
approval_workflow: @workflow([
  @workflow.step("submit_request", "user"),
  @workflow.step("review_request", "manager"),
  @workflow.step("approve_request", "admin"),
  @workflow.step("notify_user", "system")
])
```

### Conditional Workflow
```tusk
# Workflow with conditional paths
conditional_workflow: @workflow([
  @workflow.step("submit_order", "customer"),
  @workflow.if("amount > 1000", [
    @workflow.step("manager_approval", "manager"),
    @workflow.step("process_payment", "payment_gateway")
  ], [
    @workflow.step("auto_approve", "system"),
    @workflow.step("process_payment", "payment_gateway")
  ]),
  @workflow.step("send_confirmation", "email_service")
])
```

### Parallel Workflow
```tusk
# Parallel task execution
parallel_workflow: @workflow([
  @workflow.step("create_order", "customer"),
  @workflow.parallel([
    @workflow.step("check_inventory", "inventory_system"),
    @workflow.step("validate_payment", "payment_processor"),
    @workflow.step("check_shipping", "shipping_service")
  ]),
  @workflow.step("confirm_order", "system")
])
```

### Workflow with State
```tusk
# Stateful workflow
stateful_workflow: @workflow([
  @workflow.step("initialize", { state: "pending" }),
  @workflow.step("process", { state: "processing" }),
  @workflow.step("complete", { state: "completed" })
], {
  state_store: "database",
  timeout: "24h"
})
```

## JavaScript Integration

### Node.js Workflow Engine
```javascript
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
workflow_config: @workflow([
  @workflow.step("submit_request", "user"),
  @workflow.step("review_request", "manager"),
  @workflow.if("amount > 1000", [
    @workflow.step("admin_approval", "admin")
  ]),
  @workflow.step("process_request", "system")
])
`);

class WorkflowEngine {
  constructor(config) {
    this.config = config.workflow_config;
    this.state = {};
    this.currentStep = 0;
    this.handlers = {};
  }

  registerHandler(stepName, handler) {
    this.handlers[stepName] = handler;
  }

  async execute() {
    try {
      for (const step of this.config.steps) {
        await this.executeStep(step);
        this.currentStep++;
      }
      return this.state;
    } catch (error) {
      console.error('Workflow execution failed:', error);
      await this.handleError(error);
      throw error;
    }
  }

  async executeStep(step) {
    if (step.type === 'if') {
      return await this.executeConditionalStep(step);
    } else if (step.type === 'parallel') {
      return await this.executeParallelSteps(step);
    } else {
      return await this.executeSimpleStep(step);
    }
  }

  async executeSimpleStep(step) {
    const handler = this.handlers[step.action];
    if (handler) {
      const result = await handler(this.state);
      this.state[step.action] = result;
      console.log(`Step ${step.action} completed`);
    } else {
      console.warn(`No handler found for step: ${step.action}`);
    }
  }

  async executeConditionalStep(step) {
    const condition = this.evaluateCondition(step.condition);
    const stepsToExecute = condition ? step.if_steps : step.else_steps;
    
    for (const subStep of stepsToExecute) {
      await this.executeStep(subStep);
    }
  }

  async executeParallelSteps(step) {
    const promises = step.steps.map(subStep => this.executeStep(subStep));
    await Promise.all(promises);
  }

  evaluateCondition(condition) {
    // Simple condition evaluator
    if (condition.includes('amount > 1000')) {
      return this.state.amount > 1000;
    }
    return false;
  }

  async handleError(error) {
    // Error handling logic
    console.error('Workflow error:', error);
    this.state.error = error.message;
  }
}

// Workflow handlers
const handlers = {
  submit_request: async (state) => {
    console.log('User submitting request...');
    return { status: 'submitted', timestamp: new Date() };
  },
  
  review_request: async (state) => {
    console.log('Manager reviewing request...');
    return { status: 'reviewed', approved: true };
  },
  
  admin_approval: async (state) => {
    console.log('Admin approving request...');
    return { status: 'admin_approved' };
  },
  
  process_request: async (state) => {
    console.log('Processing request...');
    return { status: 'processed', completed: true };
  }
};

// Usage
const workflow = new WorkflowEngine(config);
Object.entries(handlers).forEach(([name, handler]) => {
  workflow.registerHandler(name, handler);
});

workflow.execute().then(result => {
  console.log('Workflow completed:', result);
});
```

### Browser Workflow Implementation
```javascript
// Browser-based workflow for client-side processes
const browserConfig = tusklang.parse(`
browser_workflow: @workflow([
  @workflow.step("validate_form", "client"),
  @workflow.step("submit_data", "api"),
  @workflow.step("show_success", "ui")
])
`);

class BrowserWorkflowEngine {
  constructor(config) {
    this.config = config.browser_workflow;
    this.state = {};
  }

  async execute() {
    try {
      for (const step of this.config.steps) {
        await this.executeStep(step);
      }
      return this.state;
    } catch (error) {
      console.error('Browser workflow failed:', error);
      this.showError(error);
      throw error;
    }
  }

  async executeStep(step) {
    switch (step.action) {
      case 'validate_form':
        return await this.validateForm();
      case 'submit_data':
        return await this.submitData();
      case 'show_success':
        return this.showSuccess();
      default:
        throw new Error(`Unknown step: ${step.action}`);
    }
  }

  async validateForm() {
    const form = document.getElementById('myForm');
    const formData = new FormData(form);
    
    // Validation logic
    const errors = [];
    if (!formData.get('email')) {
      errors.push('Email is required');
    }
    if (!formData.get('name')) {
      errors.push('Name is required');
    }
    
    if (errors.length > 0) {
      throw new Error(errors.join(', '));
    }
    
    this.state.formData = Object.fromEntries(formData);
    return { validated: true };
  }

  async submitData() {
    const response = await fetch('/api/submit', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(this.state.formData)
    });
    
    if (!response.ok) {
      throw new Error('Submission failed');
    }
    
    const result = await response.json();
    this.state.submission = result;
    return result;
  }

  showSuccess() {
    const successElement = document.getElementById('success-message');
    if (successElement) {
      successElement.style.display = 'block';
      successElement.textContent = 'Form submitted successfully!';
    }
    return { displayed: true };
  }

  showError(error) {
    const errorElement = document.getElementById('error-message');
    if (errorElement) {
      errorElement.style.display = 'block';
      errorElement.textContent = error.message;
    }
  }
}

// Usage
const browserWorkflow = new BrowserWorkflowEngine(browserConfig);
browserWorkflow.execute().then(result => {
  console.log('Browser workflow completed');
});
```

## Advanced Usage Scenarios

### Order Processing Workflow
```tusk
# E-commerce order processing
order_workflow: @workflow([
  @workflow.step("create_order", "customer"),
  @workflow.parallel([
    @workflow.step("check_inventory", "inventory"),
    @workflow.step("process_payment", "payment"),
    @workflow.step("calculate_shipping", "shipping")
  ]),
  @workflow.if("payment_status == 'approved'", [
    @workflow.step("allocate_inventory", "warehouse"),
    @workflow.step("generate_shipping_label", "shipping"),
    @workflow.step("send_confirmation", "email")
  ], [
    @workflow.step("cancel_order", "system"),
    @workflow.step("send_cancellation", "email")
  ])
])
```

### User Onboarding Workflow
```tusk
# User onboarding process
onboarding_workflow: @workflow([
  @workflow.step("create_account", "user"),
  @workflow.step("verify_email", "system"),
  @workflow.step("complete_profile", "user"),
  @workflow.if("profile_complete", [
    @workflow.step("send_welcome", "email"),
    @workflow.step("activate_account", "system")
  ], [
    @workflow.step("send_reminder", "email")
  ])
])
```

### Document Approval Workflow
```tusk
# Document approval process
document_workflow: @workflow([
  @workflow.step("upload_document", "user"),
  @workflow.step("validate_format", "system"),
  @workflow.step("review_content", "reviewer"),
  @workflow.if("requires_changes", [
    @workflow.step("request_changes", "system"),
    @workflow.step("update_document", "user")
  ], [
    @workflow.step("approve_document", "approver"),
    @workflow.step("publish_document", "system")
  ])
])
```

## TypeScript Implementation

### Typed Workflow System
```typescript
interface WorkflowStep {
  type: 'step' | 'if' | 'parallel';
  action?: string;
  condition?: string;
  if_steps?: WorkflowStep[];
  else_steps?: WorkflowStep[];
  steps?: WorkflowStep[];
}

interface WorkflowConfig {
  steps: WorkflowStep[];
  options?: {
    state_store?: string;
    timeout?: string;
  };
}

class TypedWorkflowEngine {
  private config: WorkflowConfig;
  private state: Record<string, any> = {};
  private handlers: Record<string, Function> = {};

  constructor(config: WorkflowConfig) {
    this.config = config;
  }

  registerHandler(action: string, handler: Function) {
    this.handlers[action] = handler;
  }

  async execute(): Promise<Record<string, any>> {
    for (const step of this.config.steps) {
      await this.executeStep(step);
    }
    return this.state;
  }

  private async executeStep(step: WorkflowStep): Promise<void> {
    switch (step.type) {
      case 'step':
        if (step.action && this.handlers[step.action]) {
          this.state[step.action] = await this.handlers[step.action](this.state);
        }
        break;
      case 'if':
        await this.executeConditionalStep(step);
        break;
      case 'parallel':
        await this.executeParallelSteps(step);
        break;
    }
  }

  private async executeConditionalStep(step: WorkflowStep): Promise<void> {
    const condition = this.evaluateCondition(step.condition!);
    const stepsToExecute = condition ? step.if_steps! : step.else_steps!;
    
    for (const subStep of stepsToExecute) {
      await this.executeStep(subStep);
    }
  }

  private async executeParallelSteps(step: WorkflowStep): Promise<void> {
    const promises = step.steps!.map(subStep => this.executeStep(subStep));
    await Promise.all(promises);
  }

  private evaluateCondition(condition: string): boolean {
    // Condition evaluation logic
    return true;
  }
}
```

## Real-World Examples

### Customer Support Workflow
```javascript
// Customer support ticket processing
const supportWorkflow = new WorkflowEngine({
  steps: [
    { type: 'step', action: 'create_ticket' },
    { type: 'step', action: 'assign_agent' },
    { type: 'if', condition: 'priority == "high"', 
      if_steps: [{ type: 'step', action: 'escalate' }] },
    { type: 'step', action: 'resolve_ticket' },
    { type: 'step', action: 'send_feedback' }
  ]
});

supportWorkflow.execute();
```

### Project Management Workflow
```javascript
// Project task management
const projectWorkflow = new WorkflowEngine({
  steps: [
    { type: 'step', action: 'create_task' },
    { type: 'parallel', steps: [
      { type: 'step', action: 'assign_resources' },
      { type: 'step', action: 'set_deadline' }
    ]},
    { type: 'step', action: 'start_work' },
    { type: 'step', action: 'review_progress' },
    { type: 'step', action: 'complete_task' }
  ]
});

projectWorkflow.execute();
```

## Performance Considerations
- Use async/await for I/O-bound workflow steps
- Implement parallel execution where possible
- Cache workflow state for long-running processes
- Monitor workflow execution time and resource usage

## Security Notes
- Validate workflow inputs and state transitions
- Implement proper authentication for workflow steps
- Log all workflow executions for audit purposes
- Use secure storage for workflow state

## Best Practices
- Design workflows with clear entry and exit points
- Handle errors gracefully at each workflow step
- Use descriptive step names and actions
- Monitor and optimize workflow performance

## Related Topics
- [@pipeline Operator](./68-pipeline-operator.md) - Data processing pipelines
- [@event Operator](./66-event-operator.md) - Event-driven automation
- [@trigger Operator](./67-trigger-operator.md) - Reactive task execution
- [Async Programming](./26-async-programming.md) - Asynchronous patterns 