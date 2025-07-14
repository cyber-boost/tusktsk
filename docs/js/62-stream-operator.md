# @stream Operator - Data Streaming and Processing

## Overview
The `@stream` operator enables efficient data streaming and processing in TuskLang configurations, supporting both synchronous and asynchronous data flows with built-in backpressure handling.

## TuskLang Syntax

### Basic Stream Configuration
```tusk
# File processing stream
file_stream: {
  source: @stream.from_file("data.csv")
  transform: @stream.map("parse_csv")
  filter: @stream.filter("valid_records")
  destination: @stream.to_file("processed.csv")
}

# Real-time data stream
realtime_stream: {
  source: @stream.from_http("https://api.example.com/stream")
  buffer_size: 1000
  batch_size: 100
  timeout: "30s"
}
```

### Stream with Transformations
```tusk
# Data processing pipeline
data_pipeline: {
  input: @stream.from_database("SELECT * FROM events")
  
  transformations: [
    @stream.map("normalize_data"),
    @stream.filter("active_events"),
    @stream.group_by("user_id"),
    @stream.aggregate("count_events")
  ]
  
  output: @stream.to_websocket("processed_events")
}
```

### Stream with Error Handling
```tusk
# Robust stream processing
robust_stream: {
  source: @stream.from_queue("event_queue")
  
  error_handling: {
    retry_attempts: 3
    retry_delay: "5s"
    dead_letter_queue: "failed_events"
    circuit_breaker: true
  }
  
  processing: @stream.process("handle_event")
}
```

## JavaScript Integration

### Node.js Stream Processing
```javascript
const { Readable, Writable, Transform } = require('stream');
const tusklang = require('@tusklang/core');

// TuskLang configuration
const config = tusklang.parse(`
stream_config: {
  source: @stream.from_file("input.json")
  transform: @stream.map("process_data")
  filter: @stream.filter("valid_data")
  destination: @stream.to_file("output.json")
  
  options: {
    high_water_mark: 1000
    encoding: "utf8"
    object_mode: true
  }
}
`);

class StreamProcessor {
  constructor(config) {
    this.config = config.stream_config;
    this.pipeline = null;
  }
  
  async process() {
    try {
      // Create source stream
      const sourceStream = this.createSourceStream();
      
      // Create transformation pipeline
      const transformStream = this.createTransformPipeline();
      
      // Create destination stream
      const destStream = this.createDestinationStream();
      
      // Connect pipeline
      this.pipeline = sourceStream
        .pipe(transformStream)
        .pipe(destStream);
      
      // Handle pipeline events
      this.pipeline.on('error', (error) => {
        console.error('Pipeline error:', error);
        this.handleError(error);
      });
      
      this.pipeline.on('end', () => {
        console.log('Stream processing completed');
      });
      
    } catch (error) {
      console.error('Stream setup error:', error);
      throw error;
    }
  }
  
  createSourceStream() {
    const sourceType = this.config.source.type;
    
    switch (sourceType) {
      case 'file':
        return this.createFileSource();
      case 'http':
        return this.createHttpSource();
      case 'database':
        return this.createDatabaseSource();
      case 'queue':
        return this.createQueueSource();
      default:
        throw new Error(`Unknown source type: ${sourceType}`);
    }
  }
  
  createFileSource() {
    const fs = require('fs');
    const readline = require('readline');
    
    const fileStream = fs.createReadStream(this.config.source.path, {
      highWaterMark: this.config.options.high_water_mark,
      encoding: this.config.options.encoding
    });
    
    if (this.config.options.object_mode) {
      return fileStream.pipe(new Transform({
        objectMode: true,
        transform(chunk, encoding, callback) {
          try {
            const data = JSON.parse(chunk);
            callback(null, data);
          } catch (error) {
            callback(error);
          }
        }
      }));
    }
    
    return fileStream;
  }
  
  createHttpSource() {
    const https = require('https');
    
    return new Readable({
      objectMode: true,
      read() {
        // Implementation for HTTP streaming
        const req = https.get(this.config.source.url, (res) => {
          res.on('data', (chunk) => {
            this.push(chunk);
          });
          
          res.on('end', () => {
            this.push(null);
          });
        });
        
        req.on('error', (error) => {
          this.destroy(error);
        });
      }
    });
  }
  
  createDatabaseSource() {
    const { Pool } = require('pg');
    const pool = new Pool();
    
    return new Readable({
      objectMode: true,
      read() {
        const query = this.config.source.query;
        
        pool.query(query, (err, result) => {
          if (err) {
            this.destroy(err);
            return;
          }
          
          result.rows.forEach(row => {
            this.push(row);
          });
          
          this.push(null);
        });
      }
    });
  }
  
  createTransformPipeline() {
    const transforms = this.config.transformations || [];
    
    return transforms.reduce((pipeline, transform) => {
      return pipeline.pipe(this.createTransform(transform));
    }, new Transform({
      objectMode: true,
      transform(chunk, encoding, callback) {
        callback(null, chunk);
      }
    }));
  }
  
  createTransform(transformConfig) {
    return new Transform({
      objectMode: true,
      transform(chunk, encoding, callback) {
        try {
          const result = this.applyTransform(chunk, transformConfig);
          callback(null, result);
        } catch (error) {
          callback(error);
        }
      }
    });
  }
  
  applyTransform(data, transformConfig) {
    switch (transformConfig.type) {
      case 'map':
        return this.mapTransform(data, transformConfig.function);
      case 'filter':
        return this.filterTransform(data, transformConfig.condition);
      case 'group_by':
        return this.groupByTransform(data, transformConfig.field);
      case 'aggregate':
        return this.aggregateTransform(data, transformConfig.operation);
      default:
        return data;
    }
  }
  
  mapTransform(data, transformFunction) {
    // Apply mapping transformation
    return data.map(item => {
      switch (transformFunction) {
        case 'parse_csv':
          return this.parseCSV(item);
        case 'normalize_data':
          return this.normalizeData(item);
        case 'process_data':
          return this.processData(item);
        default:
          return item;
      }
    });
  }
  
  filterTransform(data, condition) {
    // Apply filtering transformation
    return data.filter(item => {
      switch (condition) {
        case 'valid_records':
          return this.isValidRecord(item);
        case 'active_events':
          return this.isActiveEvent(item);
        case 'valid_data':
          return this.isValidData(item);
        default:
          return true;
      }
    });
  }
  
  createDestinationStream() {
    const destType = this.config.destination.type;
    
    switch (destType) {
      case 'file':
        return this.createFileDestination();
      case 'websocket':
        return this.createWebSocketDestination();
      case 'database':
        return this.createDatabaseDestination();
      default:
        throw new Error(`Unknown destination type: ${destType}`);
    }
  }
  
  createFileDestination() {
    const fs = require('fs');
    
    return fs.createWriteStream(this.config.destination.path, {
      highWaterMark: this.config.options.high_water_mark,
      encoding: this.config.options.encoding
    });
  }
  
  handleError(error) {
    console.error('Stream processing error:', error);
    
    // Implement error recovery strategies
    if (this.config.error_handling) {
      this.handleErrorRecovery(error);
    }
  }
  
  handleErrorRecovery(error) {
    const errorConfig = this.config.error_handling;
    
    if (errorConfig.retry_attempts > 0) {
      this.retryProcessing();
    }
    
    if (errorConfig.dead_letter_queue) {
      this.sendToDeadLetterQueue(error);
    }
  }
}

// Usage
const processor = new StreamProcessor(config);
processor.process().catch(console.error);
```

### Browser Stream Processing
```javascript
// Browser-based stream processing
const browserConfig = tusklang.parse(`
browser_stream: {
  source: @stream.from_file_input("file-input")
  transform: @stream.map("process_chunk")
  filter: @stream.filter("valid_chunk")
  destination: @stream.to_download("processed-file.json")
  
  options: {
    chunk_size: 1024 * 1024 // 1MB chunks
    progress_callback: true
  }
}
`);

class BrowserStreamProcessor {
  constructor(config) {
    this.config = config.browser_stream;
    this.fileReader = new FileReader();
    this.chunks = [];
    this.processedChunks = [];
  }
  
  async processFile(file) {
    const chunkSize = this.config.options.chunk_size;
    const totalChunks = Math.ceil(file.size / chunkSize);
    
    for (let i = 0; i < totalChunks; i++) {
      const start = i * chunkSize;
      const end = Math.min(start + chunkSize, file.size);
      const chunk = file.slice(start, end);
      
      await this.processChunk(chunk, i, totalChunks);
    }
    
    await this.combineResults();
  }
  
  async processChunk(chunk, index, total) {
    return new Promise((resolve, reject) => {
      this.fileReader.onload = (event) => {
        try {
          const data = event.target.result;
          const processed = this.transformChunk(data);
          
          if (this.filterChunk(processed)) {
            this.processedChunks[index] = processed;
          }
          
          // Update progress
          if (this.config.options.progress_callback) {
            this.updateProgress(index + 1, total);
          }
          
          resolve();
        } catch (error) {
          reject(error);
        }
      };
      
      this.fileReader.onerror = reject;
      this.fileReader.readAsText(chunk);
    });
  }
  
  transformChunk(data) {
    try {
      const parsed = JSON.parse(data);
      return this.applyTransform(parsed, this.config.transform);
    } catch (error) {
      // Handle non-JSON data
      return this.applyTransform(data, this.config.transform);
    }
  }
  
  applyTransform(data, transformConfig) {
    switch (transformConfig.function) {
      case 'process_chunk':
        return this.processChunkData(data);
      case 'parse_csv':
        return this.parseCSVChunk(data);
      case 'normalize_data':
        return this.normalizeChunkData(data);
      default:
        return data;
    }
  }
  
  filterChunk(data) {
    const filterConfig = this.config.filter;
    
    switch (filterConfig.condition) {
      case 'valid_chunk':
        return this.isValidChunk(data);
      case 'non_empty':
        return data && Object.keys(data).length > 0;
      default:
        return true;
    }
  }
  
  async combineResults() {
    const combined = this.processedChunks
      .filter(chunk => chunk !== undefined)
      .flat();
    
    await this.saveResults(combined);
  }
  
  async saveResults(data) {
    const blob = new Blob([JSON.stringify(data, null, 2)], {
      type: 'application/json'
    });
    
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = this.config.destination.filename || 'processed-data.json';
    a.click();
    
    URL.revokeObjectURL(url);
  }
  
  updateProgress(current, total) {
    const progress = (current / total) * 100;
    
    // Update progress bar
    const progressBar = document.getElementById('progress-bar');
    if (progressBar) {
      progressBar.style.width = `${progress}%`;
      progressBar.textContent = `${Math.round(progress)}%`;
    }
    
    // Trigger custom progress event
    const event = new CustomEvent('stream-progress', {
      detail: { current, total, progress }
    });
    document.dispatchEvent(event);
  }
}

// Usage
const fileInput = document.getElementById('file-input');
const processor = new BrowserStreamProcessor(browserConfig);

fileInput.addEventListener('change', (event) => {
  const file = event.target.files[0];
  if (file) {
    processor.processFile(file).catch(console.error);
  }
});
```

## Advanced Usage Scenarios

### Real-Time Data Pipeline
```tusk
# Real-time analytics pipeline
analytics_pipeline: {
  source: @stream.from_websocket("wss://api.analytics.com/events")
  
  processing: [
    @stream.map("enrich_event_data"),
    @stream.filter("valid_events"),
    @stream.window("5m", "sliding"),
    @stream.aggregate("count_by_type"),
    @stream.throttle("1000ms")
  ]
  
  destinations: [
    @stream.to_database("analytics_events"),
    @stream.to_cache("recent_events"),
    @stream.to_alert_system("anomalies")
  ]
  
  monitoring: {
    throughput: @stream.metrics("events_per_second"),
    latency: @stream.metrics("processing_latency_ms"),
    error_rate: @stream.metrics("error_percentage")
  }
}
```

### Batch Processing with Streams
```tusk
# Large-scale batch processing
batch_processor: {
  source: @stream.from_s3("s3://bucket/large-file.csv")
  
  processing: {
    chunk_size: "10MB"
    parallel_workers: 4
    memory_limit: "2GB"
    
    transformations: [
      @stream.map("parse_csv_row"),
      @stream.filter("valid_record"),
      @stream.transform("business_logic"),
      @stream.validate("data_quality")
    ]
  }
  
  output: @stream.to_s3("s3://bucket/processed/")
  
  error_handling: {
    retry_failed_chunks: true
    max_retries: 3
    dead_letter_queue: "s3://bucket/failed/"
  }
}
```

### Event Stream Processing
```tusk
# Event-driven stream processing
event_stream: {
  source: @stream.from_kafka("user-events")
  
  stream_processing: {
    window_size: "1h"
    watermark: "5m"
    late_data_policy: "drop"
    
    operations: [
      @stream.group_by("user_id"),
      @stream.aggregate("session_duration"),
      @stream.join("user_profiles"),
      @stream.enrich("recommendations")
    ]
  }
  
  output: @stream.to_elasticsearch("user_analytics")
  
  monitoring: {
    lag: @stream.metrics("consumer_lag"),
    throughput: @stream.metrics("events_per_minute"),
    processing_time: @stream.metrics("avg_processing_ms")
  }
}
```

## TypeScript Implementation

### Typed Stream Processor
```typescript
interface StreamConfig {
  source: {
    type: 'file' | 'http' | 'database' | 'queue';
    path?: string;
    url?: string;
    query?: string;
  };
  transformations: Array<{
    type: 'map' | 'filter' | 'group_by' | 'aggregate';
    function?: string;
    condition?: string;
    field?: string;
    operation?: string;
  }>;
  destination: {
    type: 'file' | 'websocket' | 'database';
    path?: string;
    url?: string;
    table?: string;
  };
  options: {
    highWaterMark: number;
    encoding: string;
    objectMode: boolean;
  };
  errorHandling?: {
    retryAttempts: number;
    retryDelay: number;
    deadLetterQueue?: string;
    circuitBreaker: boolean;
  };
}

interface StreamData {
  id: string;
  timestamp: number;
  data: any;
  metadata?: Record<string, any>;
}

interface ProcessedData extends StreamData {
  processed: boolean;
  errors?: string[];
}

class TypedStreamProcessor {
  private config: StreamConfig;
  private pipeline: NodeJS.ReadWriteStream | null = null;
  private errorCount = 0;
  private retryCount = 0;
  
  constructor(config: StreamConfig) {
    this.config = config;
  }
  
  async process(): Promise<void> {
    try {
      const sourceStream = this.createTypedSourceStream();
      const transformStream = this.createTypedTransformPipeline();
      const destStream = this.createTypedDestinationStream();
      
      this.pipeline = sourceStream
        .pipe(transformStream)
        .pipe(destStream);
      
      this.setupPipelineHandlers();
      
    } catch (error) {
      console.error('Stream setup error:', error);
      throw error;
    }
  }
  
  private createTypedSourceStream(): NodeJS.ReadableStream {
    switch (this.config.source.type) {
      case 'file':
        return this.createFileSource();
      case 'http':
        return this.createHttpSource();
      case 'database':
        return this.createDatabaseSource();
      case 'queue':
        return this.createQueueSource();
      default:
        throw new Error(`Unknown source type: ${this.config.source.type}`);
    }
  }
  
  private createFileSource(): NodeJS.ReadableStream {
    const fs = require('fs');
    const { Transform } = require('stream');
    
    const fileStream = fs.createReadStream(this.config.source.path!, {
      highWaterMark: this.config.options.highWaterMark,
      encoding: this.config.options.encoding
    });
    
    if (this.config.options.objectMode) {
      return fileStream.pipe(new Transform({
        objectMode: true,
        transform(chunk: Buffer, encoding: string, callback: Function) {
          try {
            const data: StreamData = JSON.parse(chunk.toString());
            callback(null, data);
          } catch (error) {
            callback(error);
          }
        }
      }));
    }
    
    return fileStream;
  }
  
  private createTypedTransformPipeline(): NodeJS.Transform {
    const { Transform } = require('stream');
    
    return new Transform({
      objectMode: true,
      transform(chunk: StreamData, encoding: string, callback: Function) {
        try {
          const processed = this.applyTypedTransform(chunk);
          callback(null, processed);
        } catch (error) {
          callback(error);
        }
      }
    });
  }
  
  private applyTypedTransform(data: StreamData): ProcessedData {
    const processed: ProcessedData = {
      ...data,
      processed: true,
      errors: []
    };
    
    this.config.transformations.forEach(transform => {
      try {
        switch (transform.type) {
          case 'map':
            processed.data = this.applyMapTransform(processed.data, transform.function!);
            break;
          case 'filter':
            if (!this.applyFilterTransform(processed.data, transform.condition!)) {
              processed.processed = false;
            }
            break;
          case 'group_by':
            processed.data = this.applyGroupByTransform(processed.data, transform.field!);
            break;
          case 'aggregate':
            processed.data = this.applyAggregateTransform(processed.data, transform.operation!);
            break;
        }
      } catch (error) {
        processed.errors!.push(error.message);
        processed.processed = false;
      }
    });
    
    return processed;
  }
  
  private applyMapTransform(data: any, transformFunction: string): any {
    switch (transformFunction) {
      case 'parse_csv':
        return this.parseCSV(data);
      case 'normalize_data':
        return this.normalizeData(data);
      case 'enrich_event_data':
        return this.enrichEventData(data);
      default:
        return data;
    }
  }
  
  private applyFilterTransform(data: any, condition: string): boolean {
    switch (condition) {
      case 'valid_events':
        return this.isValidEvent(data);
      case 'valid_record':
        return this.isValidRecord(data);
      case 'non_empty':
        return data && Object.keys(data).length > 0;
      default:
        return true;
    }
  }
  
  private setupPipelineHandlers(): void {
    if (!this.pipeline) return;
    
    this.pipeline.on('error', (error: Error) => {
      console.error('Pipeline error:', error);
      this.handleTypedError(error);
    });
    
    this.pipeline.on('end', () => {
      console.log('Stream processing completed');
    });
  }
  
  private handleTypedError(error: Error): void {
    this.errorCount++;
    
    if (this.config.errorHandling) {
      if (this.retryCount < this.config.errorHandling.retryAttempts) {
        this.retryProcessing();
      } else if (this.config.errorHandling.deadLetterQueue) {
        this.sendToDeadLetterQueue(error);
      }
    }
  }
  
  private retryProcessing(): void {
    this.retryCount++;
    const delay = this.config.errorHandling!.retryDelay * Math.pow(2, this.retryCount - 1);
    
    setTimeout(() => {
      console.log(`Retrying processing (${this.retryCount}/${this.config.errorHandling!.retryAttempts})`);
      this.process().catch(console.error);
    }, delay);
  }
  
  private sendToDeadLetterQueue(error: Error): void {
    // Implementation for dead letter queue
    console.log('Sending to dead letter queue:', error.message);
  }
}
```

## Real-World Examples

### Log Processing Pipeline
```javascript
// Real-time log processing
const logConfig = tusklang.parse(`
log_processor: {
  source: @stream.from_file("/var/log/application.log")
  
  processing: [
    @stream.map("parse_log_line"),
    @stream.filter("error_logs"),
    @stream.group_by("error_type"),
    @stream.aggregate("count_errors"),
    @stream.window("1h", "tumbling")
  ]
  
  output: @stream.to_alert_system("error_alerts")
  
  monitoring: {
    error_rate: @stream.metrics("errors_per_hour"),
    processing_latency: @stream.metrics("avg_processing_ms")
  }
}
`);

class LogStreamProcessor {
  constructor(config) {
    this.config = config.log_processor;
    this.errorCounts = new Map();
    this.windowStart = Date.now();
  }
  
  parseLogLine(line) {
    const logRegex = /^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(\w+)\] (.+)$/;
    const match = line.match(logRegex);
    
    if (match) {
      return {
        timestamp: new Date(match[1]),
        level: match[2],
        message: match[3],
        errorType: this.extractErrorType(match[3])
      };
    }
    
    return null;
  }
  
  extractErrorType(message) {
    if (message.includes('SQLException')) return 'database';
    if (message.includes('TimeoutException')) return 'timeout';
    if (message.includes('OutOfMemoryError')) return 'memory';
    return 'unknown';
  }
  
  isErrorLog(logEntry) {
    return logEntry && logEntry.level === 'ERROR';
  }
  
  processErrorCounts(logEntries) {
    logEntries.forEach(entry => {
      const count = this.errorCounts.get(entry.errorType) || 0;
      this.errorCounts.set(entry.errorType, count + 1);
    });
    
    return Array.from(this.errorCounts.entries()).map(([type, count]) => ({
      errorType: type,
      count: count,
      timestamp: new Date()
    }));
  }
  
  async sendAlerts(errorCounts) {
    const highErrorTypes = errorCounts.filter(error => error.count > 10);
    
    if (highErrorTypes.length > 0) {
      await this.sendAlert({
        type: 'high_error_rate',
        errors: highErrorTypes,
        timestamp: new Date()
      });
    }
  }
}
```

### Data Migration Stream
```javascript
// Database migration using streams
const migrationConfig = tusklang.parse(`
data_migration: {
  source: @stream.from_database("SELECT * FROM old_table")
  
  processing: [
    @stream.map("transform_schema"),
    @stream.validate("data_integrity"),
    @stream.batch("1000_records"),
    @stream.retry("failed_records", 3)
  ]
  
  output: @stream.to_database("new_table")
  
  monitoring: {
    migrated_records: @stream.metrics("records_migrated"),
    failed_records: @stream.metrics("migration_failures"),
    migration_speed: @stream.metrics("records_per_second")
  }
}
`);

class DataMigrationStream {
  constructor(config) {
    this.config = config.data_migration;
    this.migratedCount = 0;
    this.failedCount = 0;
    this.startTime = Date.now();
  }
  
  transformSchema(record) {
    // Transform old schema to new schema
    return {
      id: record.old_id,
      name: record.full_name,
      email: record.email_address,
      created_at: new Date(record.created_date),
      updated_at: new Date(record.modified_date)
    };
  }
  
  validateDataIntegrity(record) {
    const errors = [];
    
    if (!record.email || !this.isValidEmail(record.email)) {
      errors.push('Invalid email');
    }
    
    if (!record.name || record.name.trim().length === 0) {
      errors.push('Empty name');
    }
    
    return errors.length === 0;
  }
  
  isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }
  
  async processBatch(batch) {
    try {
      await this.insertBatch(batch);
      this.migratedCount += batch.length;
      
      this.updateProgress();
    } catch (error) {
      this.failedCount += batch.length;
      console.error('Batch migration failed:', error);
      
      // Retry failed records
      await this.retryFailedRecords(batch);
    }
  }
  
  updateProgress() {
    const elapsed = (Date.now() - this.startTime) / 1000;
    const rate = this.migratedCount / elapsed;
    
    console.log(`Migrated: ${this.migratedCount}, Failed: ${this.failedCount}, Rate: ${rate.toFixed(2)} records/sec`);
  }
}
```

## Performance Considerations

### Memory Management
```javascript
// Memory-efficient stream processing
class MemoryEfficientStream {
  constructor(maxMemoryUsage = 100 * 1024 * 1024) { // 100MB
    this.maxMemoryUsage = maxMemoryUsage;
    this.currentMemoryUsage = 0;
    this.backpressure = false;
  }
  
  checkMemoryUsage() {
    const usage = process.memoryUsage();
    this.currentMemoryUsage = usage.heapUsed;
    
    if (this.currentMemoryUsage > this.maxMemoryUsage) {
      this.enableBackpressure();
    } else {
      this.disableBackpressure();
    }
  }
  
  enableBackpressure() {
    this.backpressure = true;
    console.log('Backpressure enabled - memory usage high');
  }
  
  disableBackpressure() {
    this.backpressure = false;
  }
  
  shouldProcess() {
    return !this.backpressure;
  }
}
```

### Parallel Processing
```javascript
// Parallel stream processing
class ParallelStreamProcessor {
  constructor(workerCount = 4) {
    this.workerCount = workerCount;
    this.workers = [];
    this.taskQueue = [];
    this.results = [];
  }
  
  async processParallel(tasks) {
    // Create workers
    for (let i = 0; i < this.workerCount; i++) {
      this.workers.push(this.createWorker(i));
    }
    
    // Distribute tasks
    const taskChunks = this.chunkArray(tasks, Math.ceil(tasks.length / this.workerCount));
    
    // Process in parallel
    const promises = taskChunks.map((chunk, index) => 
      this.workers[index].process(chunk)
    );
    
    const results = await Promise.all(promises);
    return results.flat();
  }
  
  createWorker(id) {
    return {
      id,
      async process(tasks) {
        const results = [];
        for (const task of tasks) {
          const result = await this.processTask(task);
          results.push(result);
        }
        return results;
      },
      
      async processTask(task) {
        // Simulate processing
        await new Promise(resolve => setTimeout(resolve, Math.random() * 100));
        return { ...task, processed: true, worker: id };
      }
    };
  }
  
  chunkArray(array, size) {
    const chunks = [];
    for (let i = 0; i < array.length; i += size) {
      chunks.push(array.slice(i, i + size));
    }
    return chunks;
  }
}
```

## Security Notes

### Stream Security
```javascript
// Secure stream processing
class SecureStreamProcessor {
  constructor() {
    this.allowedFileTypes = ['.json', '.csv', '.txt'];
    this.maxFileSize = 100 * 1024 * 1024; // 100MB
    this.sanitizationRules = {
      removeScripts: true,
      maxFieldLength: 1000,
      allowedFields: ['id', 'name', 'email', 'data']
    };
  }
  
  validateFile(file) {
    // Check file type
    const fileExtension = this.getFileExtension(file.name);
    if (!this.allowedFileTypes.includes(fileExtension)) {
      throw new Error('File type not allowed');
    }
    
    // Check file size
    if (file.size > this.maxFileSize) {
      throw new Error('File too large');
    }
    
    return true;
  }
  
  sanitizeData(data) {
    if (typeof data === 'string') {
      return this.sanitizeString(data);
    } else if (typeof data === 'object') {
      return this.sanitizeObject(data);
    }
    
    return data;
  }
  
  sanitizeString(str) {
    if (this.sanitizationRules.removeScripts) {
      str = str.replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '');
    }
    
    if (str.length > this.sanitizationRules.maxFieldLength) {
      str = str.substring(0, this.sanitizationRules.maxFieldLength);
    }
    
    return str;
  }
  
  sanitizeObject(obj) {
    const sanitized = {};
    
    Object.keys(obj).forEach(key => {
      if (this.sanitizationRules.allowedFields.includes(key)) {
        sanitized[key] = this.sanitizeData(obj[key]);
      }
    });
    
    return sanitized;
  }
}
```

## Best Practices

### Error Recovery
```javascript
// Robust error recovery for streams
class RobustStreamProcessor {
  constructor() {
    this.errorQueue = [];
    this.recoveryStrategies = new Map();
    this.maxRetries = 3;
  }
  
  registerRecoveryStrategy(errorType, strategy) {
    this.recoveryStrategies.set(errorType, strategy);
  }
  
  async handleError(error, data) {
    console.error('Stream processing error:', error);
    
    // Add to error queue
    this.errorQueue.push({ error, data, timestamp: Date.now() });
    
    // Try recovery
    const strategy = this.recoveryStrategies.get(error.constructor.name);
    if (strategy) {
      try {
        await strategy(error, data);
        return true;
      } catch (recoveryError) {
        console.error('Recovery failed:', recoveryError);
      }
    }
    
    // Fallback to retry
    return this.retryProcessing(data);
  }
  
  async retryProcessing(data, attempt = 1) {
    if (attempt > this.maxRetries) {
      console.error('Max retries exceeded for data:', data);
      return false;
    }
    
    try {
      await this.processData(data);
      return true;
    } catch (error) {
      console.log(`Retry attempt ${attempt} failed:`, error.message);
      await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
      return this.retryProcessing(data, attempt + 1);
    }
  }
  
  async processErrorQueue() {
    while (this.errorQueue.length > 0) {
      const { error, data } = this.errorQueue.shift();
      await this.handleError(error, data);
    }
  }
}
```

## Related Topics
- [@websocket Operator](./61-websocket-operator.md) - Real-time communication
- [@cache Operator](./46-cache-operator.md) - Caching strategies
- [@metrics Operator](./47-metrics-operator.md) - Performance monitoring
- [Async Programming](./26-async-programming.md) - Asynchronous patterns
- [Event-Driven Architecture](./27-event-driven.md) - Event handling 