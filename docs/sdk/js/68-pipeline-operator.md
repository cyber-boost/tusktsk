# @pipeline Operator - Data Processing Pipelines

## Overview
The `@pipeline` operator in TuskLang enables the creation of complex data processing pipelines, allowing you to chain multiple operations together for efficient data transformation, filtering, and analysis.

## TuskLang Syntax

### Basic Pipeline
```tusk
# Simple data transformation pipeline
data_pipeline: @pipeline([
  @pipeline.step("read_data", "csv"),
  @pipeline.step("filter_data", "active_records"),
  @pipeline.step("transform_data", "normalize"),
  @pipeline.step("write_data", "json")
])
```

### Pipeline with Parameters
```tusk
# Pipeline with configuration
configurable_pipeline: @pipeline([
  @pipeline.step("fetch_api", { url: "https://api.example.com/data" }),
  @pipeline.step("parse_json"),
  @pipeline.step("filter", { condition: "status == 'active'" }),
  @pipeline.step("aggregate", { group_by: "category", operation: "sum" })
])
```

### Conditional Pipeline
```tusk
# Pipeline with conditional steps
conditional_pipeline: @pipeline([
  @pipeline.step("read_data"),
  @pipeline.if("data_quality_check", [
    @pipeline.step("clean_data"),
    @pipeline.step("validate_data")
  ]),
  @pipeline.step("process_data")
])
```

### Parallel Pipeline
```tusk
# Parallel processing pipeline
parallel_pipeline: @pipeline([
  @pipeline.step("fetch_data"),
  @pipeline.parallel([
    @pipeline.step("process_users"),
    @pipeline.step("process_orders"),
    @pipeline.step("process_products")
  ]),
  @pipeline.step("merge_results")
])
```

## JavaScript Integration

### Node.js Pipeline Implementation
```javascript
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
pipeline_config: @pipeline([
  @pipeline.step("read_csv", { file: "data.csv" }),
  @pipeline.step("filter", { condition: "age > 18" }),
  @pipeline.step("transform", { operation: "normalize" }),
  @pipeline.step("write_json", { file: "output.json" })
])
`);

class PipelineProcessor {
  constructor(config) {
    this.config = config.pipeline_config;
    this.steps = [];
    this.data = null;
  }

  async execute() {
    try {
      for (const step of this.config.steps) {
        this.data = await this.executeStep(step);
      }
      return this.data;
    } catch (error) {
      console.error('Pipeline execution failed:', error);
      throw error;
    }
  }

  async executeStep(step) {
    switch (step.operation) {
      case 'read_csv':
        return await this.readCSV(step.params.file);
      case 'filter':
        return this.filterData(this.data, step.params.condition);
      case 'transform':
        return this.transformData(this.data, step.params.operation);
      case 'write_json':
        return await this.writeJSON(this.data, step.params.file);
      default:
        throw new Error(`Unknown step operation: ${step.operation}`);
    }
  }

  async readCSV(filename) {
    const fs = require('fs');
    const csv = require('csv-parser');
    
    return new Promise((resolve, reject) => {
      const results = [];
      fs.createReadStream(filename)
        .pipe(csv())
        .on('data', (data) => results.push(data))
        .on('end', () => resolve(results))
        .on('error', reject);
    });
  }

  filterData(data, condition) {
    // Simple condition parser for demo
    const match = condition.match(/(\w+)\s*([><=!]+)\s*(.+)/);
    if (match) {
      const [_, field, operator, value] = match;
      return data.filter(item => {
        switch (operator) {
          case '>': return item[field] > value;
          case '<': return item[field] < value;
          case '==': return item[field] == value;
          case '!=': return item[field] != value;
          default: return true;
        }
      });
    }
    return data;
  }

  transformData(data, operation) {
    switch (operation) {
      case 'normalize':
        return data.map(item => ({
          ...item,
          normalized: true
        }));
      default:
        return data;
    }
  }

  async writeJSON(data, filename) {
    const fs = require('fs').promises;
    await fs.writeFile(filename, JSON.stringify(data, null, 2));
    return data;
  }
}

// Usage
const processor = new PipelineProcessor(config);
processor.execute().then(result => {
  console.log('Pipeline completed:', result.length, 'records processed');
});
```

### Browser Pipeline Implementation
```javascript
// Browser-based pipeline for client-side data processing
const browserConfig = tusklang.parse(`
browser_pipeline: @pipeline([
  @pipeline.step("fetch_data", { url: "/api/data" }),
  @pipeline.step("filter", { condition: "status == 'active'" }),
  @pipeline.step("sort", { field: "name" }),
  @pipeline.step("display", { target: "#results" })
])
`);

class BrowserPipelineProcessor {
  constructor(config) {
    this.config = config.browser_pipeline;
    this.data = null;
  }

  async execute() {
    try {
      for (const step of this.config.steps) {
        this.data = await this.executeStep(step);
      }
      return this.data;
    } catch (error) {
      console.error('Browser pipeline failed:', error);
      throw error;
    }
  }

  async executeStep(step) {
    switch (step.operation) {
      case 'fetch_data':
        return await this.fetchData(step.params.url);
      case 'filter':
        return this.filterData(this.data, step.params.condition);
      case 'sort':
        return this.sortData(this.data, step.params.field);
      case 'display':
        return this.displayData(this.data, step.params.target);
      default:
        throw new Error(`Unknown step: ${step.operation}`);
    }
  }

  async fetchData(url) {
    const response = await fetch(url);
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    return await response.json();
  }

  filterData(data, condition) {
    const match = condition.match(/(\w+)\s*==\s*['"]([^'"]+)['"]/);
    if (match) {
      const [_, field, value] = match;
      return data.filter(item => item[field] === value);
    }
    return data;
  }

  sortData(data, field) {
    return data.sort((a, b) => {
      if (a[field] < b[field]) return -1;
      if (a[field] > b[field]) return 1;
      return 0;
    });
  }

  displayData(data, target) {
    const element = document.querySelector(target);
    if (element) {
      element.innerHTML = data.map(item => 
        `<div>${item.name}: ${item.status}</div>`
      ).join('');
    }
    return data;
  }
}

// Usage
const browserProcessor = new BrowserPipelineProcessor(browserConfig);
browserProcessor.execute().then(result => {
  console.log('Browser pipeline completed');
});
```

## Advanced Usage Scenarios

### ETL Pipeline
```tusk
# Extract, Transform, Load pipeline
etl_pipeline: @pipeline([
  @pipeline.step("extract", { source: "database", query: "SELECT * FROM users" }),
  @pipeline.step("transform", { operations: ["clean", "normalize", "enrich"] }),
  @pipeline.step("load", { target: "data_warehouse", table: "processed_users" })
])
```

### Real-Time Data Pipeline
```tusk
# Real-time streaming pipeline
streaming_pipeline: @pipeline([
  @pipeline.step("stream_data", { source: "kafka_topic" }),
  @pipeline.step("process_window", { window_size: "5m" }),
  @pipeline.step("aggregate", { group_by: "user_id" }),
  @pipeline.step("alert", { condition: "count > 100" })
])
```

### Machine Learning Pipeline
```tusk
# ML data preprocessing pipeline
ml_pipeline: @pipeline([
  @pipeline.step("load_data"),
  @pipeline.step("preprocess", { operations: ["impute", "scale", "encode"] }),
  @pipeline.step("split", { train: 0.8, test: 0.2 }),
  @pipeline.step("train_model", { algorithm: "random_forest" }),
  @pipeline.step("evaluate", { metrics: ["accuracy", "precision", "recall"] })
])
```

## TypeScript Implementation

### Typed Pipeline System
```typescript
interface PipelineStep {
  operation: string;
  params?: Record<string, any>;
  condition?: string;
}

interface PipelineConfig {
  steps: PipelineStep[];
}

class TypedPipelineProcessor {
  private config: PipelineConfig;
  private data: any = null;

  constructor(config: PipelineConfig) {
    this.config = config;
  }

  async execute(): Promise<any> {
    for (const step of this.config.steps) {
      this.data = await this.executeStep(step);
    }
    return this.data;
  }

  private async executeStep(step: PipelineStep): Promise<any> {
    // Implementation for each step type
    switch (step.operation) {
      case 'read_csv':
        return await this.readCSV(step.params?.file);
      case 'filter':
        return this.filterData(this.data, step.params?.condition);
      case 'transform':
        return this.transformData(this.data, step.params?.operation);
      default:
        throw new Error(`Unknown step: ${step.operation}`);
    }
  }

  private async readCSV(filename: string): Promise<any[]> {
    // CSV reading implementation
    return [];
  }

  private filterData(data: any[], condition: string): any[] {
    // Filtering implementation
    return data;
  }

  private transformData(data: any[], operation: string): any[] {
    // Transformation implementation
    return data;
  }
}
```

## Real-World Examples

### Data Analytics Pipeline
```javascript
// Analytics data processing
const analyticsPipeline = new PipelineProcessor({
  steps: [
    { operation: 'read_csv', params: { file: 'analytics.csv' } },
    { operation: 'filter', params: { condition: 'date >= 2024-01-01' } },
    { operation: 'aggregate', params: { group_by: 'user_id', operation: 'sum' } },
    { operation: 'write_json', params: { file: 'analytics_summary.json' } }
  ]
});

analyticsPipeline.execute();
```

### User Data Processing
```javascript
// Process user data for insights
const userPipeline = new PipelineProcessor({
  steps: [
    { operation: 'fetch_api', params: { url: '/api/users' } },
    { operation: 'filter', params: { condition: 'status == "active"' } },
    { operation: 'enrich', params: { fields: ['profile', 'preferences'] } },
    { operation: 'analyze', params: { metrics: ['engagement', 'retention'] } }
  ]
});

userPipeline.execute();
```

## Performance Considerations
- Use streaming for large datasets
- Implement parallel processing where possible
- Cache intermediate results for expensive operations
- Monitor pipeline execution time and memory usage

## Security Notes
- Validate input data at each pipeline step
- Sanitize data before processing
- Use secure connections for data sources
- Log pipeline executions for audit purposes

## Best Practices
- Break complex pipelines into smaller, testable steps
- Use descriptive step names and parameters
- Handle errors gracefully at each step
- Monitor and optimize pipeline performance

## Related Topics
- [@stream Operator](./62-stream-operator.md) - Data streaming
- [@queue Operator](./63-queue-operator.md) - Message queue management
- [@event Operator](./66-event-operator.md) - Event-driven automation
- [Data Processing](./05-data-structures.md) - Data manipulation 