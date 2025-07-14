# TuskLang JavaScript Documentation: #logging Directive

## Overview

The `#logging` directive in TuskLang defines logging configurations and strategies, enabling declarative log management with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#logging console
  level: info
  format: json
  timestamp: true
  colors: true
  pretty_print: false

#logging file
  level: debug
  path: /var/log/app
  filename: app.log
  max_size: 10mb
  max_files: 5
  rotation: daily
  compression: true
  format: text

#logging syslog
  level: warn
  facility: local0
  host: localhost
  port: 514
  protocol: udp
  app_name: myapp

#logging elasticsearch
  level: info
  hosts: ["http://localhost:9200"]
  index: app-logs
  username: ${ES_USER}
  password: ${ES_PASSWORD}
  batch_size: 100
  flush_interval: 5000

#logging multi_target
  targets:
    - type: console
      level: info
      format: json
    - type: file
      level: debug
      path: /var/log/app
    - type: elasticsearch
      level: error
      hosts: ["http://localhost:9200"]
```

## JavaScript Integration

### Console Logger Handler

```javascript
// console-logger-handler.js
const chalk = require('chalk');

class ConsoleLoggerHandler {
  constructor(config) {
    this.config = config;
    this.level = this.getLogLevel(config.level || 'info');
    this.format = config.format || 'text';
    this.timestamp = config.timestamp !== false;
    this.colors = config.colors !== false;
    this.prettyPrint = config.pretty_print || false;
    
    this.levels = {
      error: 0,
      warn: 1,
      info: 2,
      debug: 3,
      trace: 4
    };
  }

  getLogLevel(level) {
    return this.levels[level.toLowerCase()] || this.levels.info;
  }

  shouldLog(level) {
    return this.levels[level] <= this.level;
  }

  formatTimestamp() {
    return new Date().toISOString();
  }

  formatMessage(level, message, meta = {}) {
    const timestamp = this.timestamp ? this.formatTimestamp() : '';
    
    if (this.format === 'json') {
      const logEntry = {
        timestamp,
        level: level.toUpperCase(),
        message,
        ...meta
      };
      
      return this.prettyPrint ? 
        JSON.stringify(logEntry, null, 2) : 
        JSON.stringify(logEntry);
    } else {
      let formatted = '';
      
      if (timestamp) {
        formatted += `[${timestamp}] `;
      }
      
      formatted += `[${level.toUpperCase()}] ${message}`;
      
      if (Object.keys(meta).length > 0) {
        formatted += ` ${JSON.stringify(meta)}`;
      }
      
      return formatted;
    }
  }

  colorize(level, message) {
    if (!this.colors) return message;
    
    const colors = {
      error: chalk.red,
      warn: chalk.yellow,
      info: chalk.blue,
      debug: chalk.green,
      trace: chalk.gray
    };
    
    return colors[level] ? colors[level](message) : message;
  }

  log(level, message, meta = {}) {
    if (!this.shouldLog(level)) return;
    
    const formatted = this.formatMessage(level, message, meta);
    const output = this.colorize(level, formatted);
    
    if (level === 'error') {
      console.error(output);
    } else if (level === 'warn') {
      console.warn(output);
    } else {
      console.log(output);
    }
  }

  error(message, meta = {}) {
    this.log('error', message, meta);
  }

  warn(message, meta = {}) {
    this.log('warn', message, meta);
  }

  info(message, meta = {}) {
    this.log('info', message, meta);
  }

  debug(message, meta = {}) {
    this.log('debug', message, meta);
  }

  trace(message, meta = {}) {
    this.log('trace', message, meta);
  }
}

module.exports = ConsoleLoggerHandler;
```

### File Logger Handler

```javascript
// file-logger-handler.js
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const { promisify } = require('util');

class FileLoggerHandler {
  constructor(config) {
    this.config = config;
    this.level = this.getLogLevel(config.level || 'info');
    this.logPath = config.path || '/var/log/app';
    this.filename = config.filename || 'app.log';
    this.maxSize = this.parseSize(config.max_size || '10mb');
    this.maxFiles = config.max_files || 5;
    this.rotation = config.rotation || 'daily';
    this.compression = config.compression !== false;
    this.format = config.format || 'text';
    
    this.currentFile = null;
    this.currentSize = 0;
    this.lastRotation = null;
    
    this.levels = {
      error: 0,
      warn: 1,
      info: 2,
      debug: 3,
      trace: 4
    };
  }

  async connect() {
    // Ensure log directory exists
    await fs.promises.mkdir(this.logPath, { recursive: true });
    
    // Initialize current file
    await this.initializeFile();
    
    console.log('File logger initialized successfully');
  }

  getLogLevel(level) {
    return this.levels[level.toLowerCase()] || this.levels.info;
  }

  shouldLog(level) {
    return this.levels[level] <= this.level;
  }

  parseSize(sizeStr) {
    const units = {
      b: 1,
      kb: 1024,
      mb: 1024 * 1024,
      gb: 1024 * 1024 * 1024
    };
    
    const match = sizeStr.toLowerCase().match(/^(\d+)([kmg]?b)$/);
    if (!match) return 10 * 1024 * 1024; // Default 10MB
    
    const [, size, unit] = match;
    return parseInt(size) * (units[unit] || 1);
  }

  async initializeFile() {
    const filePath = path.join(this.logPath, this.filename);
    
    try {
      const stats = await fs.promises.stat(filePath);
      this.currentSize = stats.size;
    } catch (error) {
      this.currentSize = 0;
    }
    
    this.currentFile = filePath;
  }

  formatTimestamp() {
    return new Date().toISOString();
  }

  formatMessage(level, message, meta = {}) {
    const timestamp = this.formatTimestamp();
    
    if (this.format === 'json') {
      const logEntry = {
        timestamp,
        level: level.toUpperCase(),
        message,
        ...meta
      };
      
      return JSON.stringify(logEntry) + '\n';
    } else {
      let formatted = `[${timestamp}] [${level.toUpperCase()}] ${message}`;
      
      if (Object.keys(meta).length > 0) {
        formatted += ` ${JSON.stringify(meta)}`;
      }
      
      return formatted + '\n';
    }
  }

  async shouldRotate() {
    // Check size-based rotation
    if (this.currentSize >= this.maxSize) {
      return true;
    }
    
    // Check time-based rotation
    if (this.rotation === 'daily') {
      const today = new Date().toDateString();
      if (this.lastRotation !== today) {
        return true;
      }
    }
    
    return false;
  }

  async rotate() {
    if (this.currentFile) {
      const timestamp = new Date().toISOString().split('T')[0];
      const rotatedPath = `${this.currentFile}.${timestamp}`;
      
      try {
        await fs.promises.rename(this.currentFile, rotatedPath);
        
        // Compress if enabled
        if (this.compression) {
          await this.compressFile(rotatedPath);
        }
        
        // Clean up old files
        await this.cleanupOldFiles();
        
        this.lastRotation = new Date().toDateString();
        this.currentSize = 0;
      } catch (error) {
        console.error('Error rotating log file:', error);
      }
    }
  }

  async compressFile(filePath) {
    try {
      const gzip = promisify(zlib.gzip);
      const readFile = promisify(fs.readFile);
      const writeFile = promisify(fs.writeFile);
      const unlink = promisify(fs.unlink);
      
      const content = await readFile(filePath);
      const compressed = await gzip(content);
      await writeFile(`${filePath}.gz`, compressed);
      await unlink(filePath);
    } catch (error) {
      console.error('Error compressing log file:', error);
    }
  }

  async cleanupOldFiles() {
    try {
      const files = await fs.promises.readdir(this.logPath);
      const logFiles = files
        .filter(file => file.startsWith(this.filename))
        .map(file => ({
          name: file,
          path: path.join(this.logPath, file),
          stats: null
        }));
      
      // Get file stats
      for (const file of logFiles) {
        try {
          file.stats = await fs.promises.stat(file.path);
        } catch (error) {
          // Skip files that can't be stat'd
        }
      }
      
      // Sort by modification time (oldest first)
      logFiles.sort((a, b) => {
        if (!a.stats || !b.stats) return 0;
        return a.stats.mtime.getTime() - b.stats.mtime.getTime();
      });
      
      // Remove excess files
      while (logFiles.length > this.maxFiles) {
        const file = logFiles.shift();
        try {
          await fs.promises.unlink(file.path);
        } catch (error) {
          console.error(`Error deleting old log file ${file.path}:`, error);
        }
      }
    } catch (error) {
      console.error('Error cleaning up old log files:', error);
    }
  }

  async log(level, message, meta = {}) {
    if (!this.shouldLog(level)) return;
    
    // Check if rotation is needed
    if (await this.shouldRotate()) {
      await this.rotate();
    }
    
    const formatted = this.formatMessage(level, message, meta);
    
    try {
      await fs.promises.appendFile(this.currentFile, formatted);
      this.currentSize += formatted.length;
    } catch (error) {
      console.error('Error writing to log file:', error);
    }
  }

  error(message, meta = {}) {
    return this.log('error', message, meta);
  }

  warn(message, meta = {}) {
    return this.log('warn', message, meta);
  }

  info(message, meta = {}) {
    return this.log('info', message, meta);
  }

  debug(message, meta = {}) {
    return this.log('debug', message, meta);
  }

  trace(message, meta = {}) {
    return this.log('trace', message, meta);
  }

  async disconnect() {
    // Cleanup if needed
  }
}

module.exports = FileLoggerHandler;
```

### Syslog Logger Handler

```javascript
// syslog-logger-handler.js
const dgram = require('dgram');
const net = require('net');

class SyslogLoggerHandler {
  constructor(config) {
    this.config = config;
    this.level = this.getLogLevel(config.level || 'info');
    this.facility = this.getFacility(config.facility || 'local0');
    this.host = config.host || 'localhost';
    this.port = config.port || 514;
    this.protocol = config.protocol || 'udp';
    this.appName = config.app_name || 'app';
    
    this.client = null;
    this.connected = false;
    
    this.levels = {
      error: 3, // Error
      warn: 4,  // Warning
      info: 6,  // Informational
      debug: 7, // Debug
      trace: 7  // Debug (same as debug for syslog)
    };
    
    this.facilities = {
      kern: 0,
      user: 1,
      mail: 2,
      daemon: 3,
      auth: 4,
      syslog: 5,
      lpr: 6,
      news: 7,
      uucp: 8,
      cron: 9,
      authpriv: 10,
      ftp: 11,
      local0: 16,
      local1: 17,
      local2: 18,
      local3: 19,
      local4: 20,
      local5: 21,
      local6: 22,
      local7: 23
    };
  }

  async connect() {
    if (this.protocol === 'udp') {
      this.client = dgram.createSocket('udp4');
    } else {
      this.client = new net.Socket();
      
      this.client.on('connect', () => {
        this.connected = true;
        console.log('Syslog TCP connection established');
      });
      
      this.client.on('error', (error) => {
        console.error('Syslog TCP connection error:', error);
        this.connected = false;
      });
      
      this.client.on('close', () => {
        this.connected = false;
      });
      
      await new Promise((resolve, reject) => {
        this.client.connect(this.port, this.host, resolve);
        this.client.once('error', reject);
      });
    }
    
    console.log('Syslog logger connected successfully');
  }

  getLogLevel(level) {
    return this.levels[level.toLowerCase()] || this.levels.info;
  }

  getFacility(facility) {
    return this.facilities[facility.toLowerCase()] || this.facilities.local0;
  }

  shouldLog(level) {
    return this.levels[level] <= this.level;
  }

  calculatePriority(level) {
    return this.facility * 8 + this.levels[level];
  }

  formatSyslogMessage(level, message, meta = {}) {
    const timestamp = new Date().toISOString();
    const priority = this.calculatePriority(level);
    const hostname = require('os').hostname();
    
    let syslogMessage = `<${priority}>${timestamp} ${hostname} ${this.appName}: ${message}`;
    
    if (Object.keys(meta).length > 0) {
      syslogMessage += ` ${JSON.stringify(meta)}`;
    }
    
    return syslogMessage;
  }

  async sendMessage(message) {
    if (!this.client) {
      throw new Error('Syslog client not initialized');
    }
    
    if (this.protocol === 'udp') {
      return new Promise((resolve, reject) => {
        this.client.send(message, this.port, this.host, (error) => {
          if (error) reject(error);
          else resolve();
        });
      });
    } else {
      if (!this.connected) {
        throw new Error('TCP connection not established');
      }
      
      return new Promise((resolve, reject) => {
        this.client.write(message + '\n', (error) => {
          if (error) reject(error);
          else resolve();
        });
      });
    }
  }

  async log(level, message, meta = {}) {
    if (!this.shouldLog(level)) return;
    
    try {
      const syslogMessage = this.formatSyslogMessage(level, message, meta);
      await this.sendMessage(syslogMessage);
    } catch (error) {
      console.error('Error sending syslog message:', error);
    }
  }

  error(message, meta = {}) {
    return this.log('error', message, meta);
  }

  warn(message, meta = {}) {
    return this.log('warn', message, meta);
  }

  info(message, meta = {}) {
    return this.log('info', message, meta);
  }

  debug(message, meta = {}) {
    return this.log('debug', message, meta);
  }

  trace(message, meta = {}) {
    return this.log('trace', message, meta);
  }

  async disconnect() {
    if (this.client) {
      if (this.protocol === 'udp') {
        this.client.close();
      } else {
        this.client.end();
      }
      this.client = null;
      this.connected = false;
    }
  }
}

module.exports = SyslogLoggerHandler;
```

### Elasticsearch Logger Handler

```javascript
// elasticsearch-logger-handler.js
const { Client } = require('@elastic/elasticsearch');

class ElasticsearchLoggerHandler {
  constructor(config) {
    this.config = config;
    this.level = this.getLogLevel(config.level || 'info');
    this.hosts = config.hosts || ['http://localhost:9200'];
    this.index = config.index || 'app-logs';
    this.username = config.username;
    this.password = config.password;
    this.batchSize = config.batch_size || 100;
    this.flushInterval = config.flush_interval || 5000;
    
    this.client = null;
    this.buffer = [];
    this.flushTimer = null;
    
    this.levels = {
      error: 0,
      warn: 1,
      info: 2,
      debug: 3,
      trace: 4
    };
  }

  async connect() {
    const clientConfig = {
      nodes: this.hosts,
      auth: this.username && this.password ? {
        username: this.username,
        password: this.password
      } : undefined,
      ssl: {
        rejectUnauthorized: false
      }
    };
    
    this.client = new Client(clientConfig);
    
    // Test connection
    await this.client.ping();
    
    // Start flush timer
    this.startFlushTimer();
    
    console.log('Elasticsearch logger connected successfully');
  }

  getLogLevel(level) {
    return this.levels[level.toLowerCase()] || this.levels.info;
  }

  shouldLog(level) {
    return this.levels[level] <= this.level;
  }

  startFlushTimer() {
    this.flushTimer = setInterval(() => {
      this.flush();
    }, this.flushInterval);
  }

  async log(level, message, meta = {}) {
    if (!this.shouldLog(level)) return;
    
    const logEntry = {
      timestamp: new Date().toISOString(),
      level: level.toUpperCase(),
      message,
      ...meta,
      hostname: require('os').hostname(),
      pid: process.pid
    };
    
    this.buffer.push(logEntry);
    
    // Flush if buffer is full
    if (this.buffer.length >= this.batchSize) {
      await this.flush();
    }
  }

  async flush() {
    if (this.buffer.length === 0) return;
    
    try {
      const body = this.buffer.flatMap(doc => [
        { index: { _index: this.index } },
        doc
      ]);
      
      await this.client.bulk({ body });
      
      this.buffer = [];
    } catch (error) {
      console.error('Error flushing logs to Elasticsearch:', error);
    }
  }

  error(message, meta = {}) {
    return this.log('error', message, meta);
  }

  warn(message, meta = {}) {
    return this.log('warn', message, meta);
  }

  info(message, meta = {}) {
    return this.log('info', message, meta);
  }

  debug(message, meta = {}) {
    return this.log('debug', message, meta);
  }

  trace(message, meta = {}) {
    return this.log('trace', message, meta);
  }

  async disconnect() {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
      this.flushTimer = null;
    }
    
    // Final flush
    await this.flush();
    
    if (this.client) {
      await this.client.close();
      this.client = null;
    }
  }
}

module.exports = ElasticsearchLoggerHandler;
```

### Multi-Target Logger Handler

```javascript
// multi-target-logger-handler.js
class MultiTargetLoggerHandler {
  constructor(config) {
    this.config = config;
    this.targets = [];
    this.levels = {
      error: 0,
      warn: 1,
      info: 2,
      debug: 3,
      trace: 4
    };
  }

  async connect() {
    for (const targetConfig of this.config.targets) {
      const handler = await this.createHandler(targetConfig);
      this.targets.push(handler);
    }
    
    console.log(`Multi-target logger initialized with ${this.targets.length} targets`);
  }

  async createHandler(config) {
    switch (config.type) {
      case 'console':
        const ConsoleLoggerHandler = require('./console-logger-handler');
        return new ConsoleLoggerHandler(config);
        
      case 'file':
        const FileLoggerHandler = require('./file-logger-handler');
        const fileHandler = new FileLoggerHandler(config);
        await fileHandler.connect();
        return fileHandler;
        
      case 'syslog':
        const SyslogLoggerHandler = require('./syslog-logger-handler');
        const syslogHandler = new SyslogLoggerHandler(config);
        await syslogHandler.connect();
        return syslogHandler;
        
      case 'elasticsearch':
        const ElasticsearchLoggerHandler = require('./elasticsearch-logger-handler');
        const esHandler = new ElasticsearchLoggerHandler(config);
        await esHandler.connect();
        return esHandler;
        
      default:
        throw new Error(`Unsupported logger type: ${config.type}`);
    }
  }

  async log(level, message, meta = {}) {
    const promises = this.targets.map(target => 
      target.log(level, message, meta).catch(error => {
        console.error(`Error logging to target:`, error);
      })
    );
    
    await Promise.all(promises);
  }

  error(message, meta = {}) {
    return this.log('error', message, meta);
  }

  warn(message, meta = {}) {
    return this.log('warn', message, meta);
  }

  info(message, meta = {}) {
    return this.log('info', message, meta);
  }

  debug(message, meta = {}) {
    return this.log('debug', message, meta);
  }

  trace(message, meta = {}) {
    return this.log('trace', message, meta);
  }

  async disconnect() {
    const promises = this.targets.map(target => 
      target.disconnect().catch(error => {
        console.error('Error disconnecting logger target:', error);
      })
    );
    
    await Promise.all(promises);
    this.targets = [];
  }
}

module.exports = MultiTargetLoggerHandler;
```

## TypeScript Implementation

```typescript
// logger-handler.types.ts
export interface LoggerConfig {
  level?: string;
  format?: 'json' | 'text';
  timestamp?: boolean;
  colors?: boolean;
  pretty_print?: boolean;
  path?: string;
  filename?: string;
  max_size?: string;
  max_files?: number;
  rotation?: 'daily' | 'hourly' | 'size';
  compression?: boolean;
  facility?: string;
  host?: string;
  port?: number;
  protocol?: 'udp' | 'tcp';
  app_name?: string;
  hosts?: string[];
  index?: string;
  username?: string;
  password?: string;
  batch_size?: number;
  flush_interval?: number;
  targets?: LoggerTargetConfig[];
}

export interface LoggerTargetConfig {
  type: 'console' | 'file' | 'syslog' | 'elasticsearch';
  level?: string;
  format?: string;
  [key: string]: any;
}

export interface LogEntry {
  timestamp: string;
  level: string;
  message: string;
  [key: string]: any;
}

export interface LoggerHandler {
  connect(): Promise<void>;
  log(level: string, message: string, meta?: any): Promise<void>;
  error(message: string, meta?: any): Promise<void>;
  warn(message: string, meta?: any): Promise<void>;
  info(message: string, meta?: any): Promise<void>;
  debug(message: string, meta?: any): Promise<void>;
  trace(message: string, meta?: any): Promise<void>;
  disconnect(): Promise<void>;
}

// logger-handler.ts
import { LoggerConfig, LoggerHandler, LogEntry } from './logger-handler.types';

export class TypeScriptLoggerHandler implements LoggerHandler {
  protected config: LoggerConfig;
  protected levels: Record<string, number> = {
    error: 0,
    warn: 1,
    info: 2,
    debug: 3,
    trace: 4
  };

  constructor(config: LoggerConfig) {
    this.config = config;
  }

  async connect(): Promise<void> {
    throw new Error('Method not implemented');
  }

  async log(level: string, message: string, meta: any = {}): Promise<void> {
    throw new Error('Method not implemented');
  }

  async error(message: string, meta: any = {}): Promise<void> {
    return this.log('error', message, meta);
  }

  async warn(message: string, meta: any = {}): Promise<void> {
    return this.log('warn', message, meta);
  }

  async info(message: string, meta: any = {}): Promise<void> {
    return this.log('info', message, meta);
  }

  async debug(message: string, meta: any = {}): Promise<void> {
    return this.log('debug', message, meta);
  }

  async trace(message: string, meta: any = {}): Promise<void> {
    return this.log('trace', message, meta);
  }

  async disconnect(): Promise<void> {
    throw new Error('Method not implemented');
  }

  protected getLogLevel(level: string): number {
    return this.levels[level.toLowerCase()] || this.levels.info;
  }

  protected shouldLog(level: string): boolean {
    const configLevel = this.getLogLevel(this.config.level || 'info');
    return this.levels[level] <= configLevel;
  }

  protected formatTimestamp(): string {
    return new Date().toISOString();
  }
}

export class TypeScriptConsoleLoggerHandler extends TypeScriptLoggerHandler {
  private format: string;
  private timestamp: boolean;
  private colors: boolean;
  private prettyPrint: boolean;

  constructor(config: LoggerConfig) {
    super(config);
    this.format = config.format || 'text';
    this.timestamp = config.timestamp !== false;
    this.colors = config.colors !== false;
    this.prettyPrint = config.pretty_print || false;
  }

  async connect(): Promise<void> {
    // Console logger doesn't need connection
  }

  async log(level: string, message: string, meta: any = {}): Promise<void> {
    if (!this.shouldLog(level)) return;
    
    const timestamp = this.timestamp ? this.formatTimestamp() : '';
    
    if (this.format === 'json') {
      const logEntry: LogEntry = {
        timestamp,
        level: level.toUpperCase(),
        message,
        ...meta
      };
      
      const output = this.prettyPrint ? 
        JSON.stringify(logEntry, null, 2) : 
        JSON.stringify(logEntry);
      
      console.log(output);
    } else {
      let formatted = '';
      
      if (timestamp) {
        formatted += `[${timestamp}] `;
      }
      
      formatted += `[${level.toUpperCase()}] ${message}`;
      
      if (Object.keys(meta).length > 0) {
        formatted += ` ${JSON.stringify(meta)}`;
      }
      
      console.log(formatted);
    }
  }

  async disconnect(): Promise<void> {
    // Console logger doesn't need disconnection
  }
}
```

## Advanced Usage Scenarios

### Structured Logging

```javascript
// structured-logger.js
class StructuredLogger {
  constructor(loggerHandler) {
    this.logger = loggerHandler;
    this.context = {};
  }

  setContext(key, value) {
    this.context[key] = value;
  }

  clearContext() {
    this.context = {};
  }

  log(level, message, meta = {}) {
    const enrichedMeta = { ...this.context, ...meta };
    return this.logger.log(level, message, enrichedMeta);
  }

  error(message, meta = {}) {
    return this.log('error', message, meta);
  }

  warn(message, meta = {}) {
    return this.log('warn', message, meta);
  }

  info(message, meta = {}) {
    return this.log('info', message, meta);
  }

  debug(message, meta = {}) {
    return this.log('debug', message, meta);
  }

  trace(message, meta = {}) {
    return this.log('trace', message, meta);
  }

  // Convenience methods for common scenarios
  request(req, res, next) {
    const start = Date.now();
    
    res.on('finish', () => {
      const duration = Date.now() - start;
      this.info('HTTP Request', {
        method: req.method,
        url: req.url,
        status: res.statusCode,
        duration,
        userAgent: req.get('User-Agent'),
        ip: req.ip
      });
    });
    
    next();
  }

  database(query, params, duration) {
    this.debug('Database Query', {
      query,
      params,
      duration,
      timestamp: new Date().toISOString()
    });
  }

  error(error, context = {}) {
    this.error('Application Error', {
      message: error.message,
      stack: error.stack,
      name: error.name,
      ...context
    });
  }
}
```

### Log Aggregation

```javascript
// log-aggregator.js
class LogAggregator {
  constructor() {
    this.logs = [];
    this.maxLogs = 1000;
    this.aggregationInterval = 60000; // 1 minute
    this.timer = null;
  }

  addLog(level, message, meta = {}) {
    this.logs.push({
      timestamp: new Date(),
      level,
      message,
      meta
    });

    // Keep only recent logs
    if (this.logs.length > this.maxLogs) {
      this.logs = this.logs.slice(-this.maxLogs);
    }
  }

  startAggregation() {
    this.timer = setInterval(() => {
      this.aggregate();
    }, this.aggregationInterval);
  }

  stopAggregation() {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  aggregate() {
    const now = new Date();
    const oneMinuteAgo = new Date(now.getTime() - 60000);

    // Filter logs from the last minute
    const recentLogs = this.logs.filter(log => log.timestamp > oneMinuteAgo);

    // Group by level
    const levelCounts = {};
    const messageCounts = {};

    recentLogs.forEach(log => {
      levelCounts[log.level] = (levelCounts[log.level] || 0) + 1;
      
      const messageKey = `${log.level}:${log.message}`;
      messageCounts[messageKey] = (messageCounts[messageKey] || 0) + 1;
    });

    // Generate summary
    const summary = {
      timestamp: now.toISOString(),
      totalLogs: recentLogs.length,
      levelCounts,
      messageCounts,
      topMessages: Object.entries(messageCounts)
        .sort(([,a], [,b]) => b - a)
        .slice(0, 10)
        .map(([message, count]) => ({ message, count }))
    };

    console.log('Log Summary:', JSON.stringify(summary, null, 2));
  }

  getStats() {
    const now = new Date();
    const oneHourAgo = new Date(now.getTime() - 3600000);

    const recentLogs = this.logs.filter(log => log.timestamp > oneHourAgo);
    const levelCounts = {};

    recentLogs.forEach(log => {
      levelCounts[log.level] = (levelCounts[log.level] || 0) + 1;
    });

    return {
      totalLogs: this.logs.length,
      recentLogs: recentLogs.length,
      levelCounts,
      oldestLog: this.logs[0]?.timestamp,
      newestLog: this.logs[this.logs.length - 1]?.timestamp
    };
  }
}
```

## Real-World Examples

### Express.js Logging Middleware

```javascript
// express-logging-middleware.js
class ExpressLoggingMiddleware {
  constructor(logger) {
    this.logger = logger;
  }

  requestLogger() {
    return (req, res, next) => {
      const start = Date.now();
      
      // Log request
      this.logger.info('HTTP Request Started', {
        method: req.method,
        url: req.url,
        headers: req.headers,
        body: req.body,
        ip: req.ip,
        userAgent: req.get('User-Agent')
      });

      // Override res.end to log response
      const originalEnd = res.end;
      res.end = function(chunk, encoding) {
        const duration = Date.now() - start;
        
        this.logger.info('HTTP Request Completed', {
          method: req.method,
          url: req.url,
          statusCode: res.statusCode,
          duration,
          contentLength: res.get('Content-Length'),
          userAgent: req.get('User-Agent'),
          ip: req.ip
        });

        originalEnd.call(this, chunk, encoding);
      }.bind(this);

      next();
    };
  }

  errorLogger() {
    return (error, req, res, next) => {
      this.logger.error('HTTP Request Error', {
        method: req.method,
        url: req.url,
        error: {
          message: error.message,
          stack: error.stack,
          name: error.name
        },
        headers: req.headers,
        body: req.body,
        ip: req.ip,
        userAgent: req.get('User-Agent')
      });

      next(error);
    };
  }
}
```

### Database Query Logging

```javascript
// database-query-logger.js
class DatabaseQueryLogger {
  constructor(logger) {
    this.logger = logger;
  }

  logQuery(query, params, duration, error = null) {
    const logData = {
      query,
      params,
      duration,
      timestamp: new Date().toISOString()
    };

    if (error) {
      this.logger.error('Database Query Error', {
        ...logData,
        error: {
          message: error.message,
          code: error.code,
          sqlState: error.sqlState
        }
      });
    } else {
      this.logger.debug('Database Query', logData);
    }
  }

  wrapQuery(queryFunction) {
    return async (...args) => {
      const start = Date.now();
      
      try {
        const result = await queryFunction(...args);
        const duration = Date.now() - start;
        
        this.logQuery(args[0], args[1], duration);
        return result;
      } catch (error) {
        const duration = Date.now() - start;
        this.logQuery(args[0], args[1], duration, error);
        throw error;
      }
    };
  }
}
```

## Performance Considerations

### Log Buffering

```javascript
// log-buffer.js
class LogBuffer {
  constructor(logger, options = {}) {
    this.logger = logger;
    this.buffer = [];
    this.maxBufferSize = options.maxBufferSize || 100;
    this.flushInterval = options.flushInterval || 5000;
    this.timer = null;
  }

  start() {
    this.timer = setInterval(() => {
      this.flush();
    }, this.flushInterval);
  }

  stop() {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  add(level, message, meta) {
    this.buffer.push({ level, message, meta, timestamp: Date.now() });
    
    if (this.buffer.length >= this.maxBufferSize) {
      this.flush();
    }
  }

  async flush() {
    if (this.buffer.length === 0) return;
    
    const logs = [...this.buffer];
    this.buffer = [];
    
    // Group logs by level for efficiency
    const groupedLogs = {};
    logs.forEach(log => {
      if (!groupedLogs[log.level]) {
        groupedLogs[log.level] = [];
      }
      groupedLogs[log.level].push(log);
    });
    
    // Send grouped logs
    for (const [level, levelLogs] of Object.entries(groupedLogs)) {
      for (const log of levelLogs) {
        await this.logger.log(level, log.message, log.meta);
      }
    }
  }
}
```

### Log Sampling

```javascript
// log-sampler.js
class LogSampler {
  constructor(sampleRate = 1.0) {
    this.sampleRate = sampleRate;
  }

  shouldLog() {
    return Math.random() < this.sampleRate;
  }

  wrapLogger(logger) {
    const wrapped = {};
    
    ['error', 'warn', 'info', 'debug', 'trace'].forEach(level => {
      wrapped[level] = (message, meta) => {
        if (this.shouldLog()) {
          return logger[level](message, meta);
        }
      };
    });
    
    return wrapped;
  }
}
```

## Security Notes

### Log Sanitization

```javascript
// log-sanitizer.js
class LogSanitizer {
  constructor() {
    this.sensitiveFields = ['password', 'token', 'secret', 'key', 'authorization'];
  }

  sanitize(data) {
    if (typeof data === 'object' && data !== null) {
      const sanitized = { ...data };
      
      for (const field of this.sensitiveFields) {
        if (sanitized[field]) {
          sanitized[field] = '[REDACTED]';
        }
      }
      
      return sanitized;
    }
    
    return data;
  }

  sanitizeMessage(message) {
    // Remove sensitive data from message strings
    let sanitized = message;
    
    for (const field of this.sensitiveFields) {
      const regex = new RegExp(`${field}[=:]\s*[^\s]+`, 'gi');
      sanitized = sanitized.replace(regex, `${field}=[REDACTED]`);
    }
    
    return sanitized;
  }
}
```

### Log Encryption

```javascript
// log-encryption.js
const crypto = require('crypto');

class LogEncryption {
  constructor(secretKey) {
    this.secretKey = secretKey;
    this.algorithm = 'aes-256-cbc';
  }

  encrypt(data) {
    const iv = crypto.randomBytes(16);
    const cipher = crypto.createCipher(this.algorithm, this.secretKey);
    
    let encrypted = cipher.update(JSON.stringify(data), 'utf8', 'hex');
    encrypted += cipher.final('hex');
    
    return {
      iv: iv.toString('hex'),
      data: encrypted
    };
  }

  decrypt(encryptedData) {
    const decipher = crypto.createDecipher(this.algorithm, this.secretKey);
    
    let decrypted = decipher.update(encryptedData.data, 'hex', 'utf8');
    decrypted += decipher.final('utf8');
    
    return JSON.parse(decrypted);
  }
}
```

## Best Practices

### Log Configuration Management

```javascript
// log-config-manager.js
class LogConfigManager {
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
    if (!config.type) {
      throw new Error('Logger type is required');
    }
    
    if (config.level && !['error', 'warn', 'info', 'debug', 'trace'].includes(config.level)) {
      throw new Error('Invalid log level');
    }
    
    return config;
  }

  getCurrentConfig() {
    const environment = process.env.NODE_ENV || 'development';
    return this.getConfig(environment);
  }
}
```

### Log Health Monitoring

```javascript
// log-health-monitor.js
class LogHealthMonitor {
  constructor(logger) {
    this.logger = logger;
    this.metrics = {
      logs: 0,
      errors: 0,
      warnings: 0,
      avgResponseTime: 0
    };
  }

  async checkHealth() {
    try {
      const start = Date.now();
      await this.logger.info('Health check');
      const responseTime = Date.now() - start;
      
      this.metrics.logs++;
      this.metrics.avgResponseTime = 
        (this.metrics.avgResponseTime * (this.metrics.logs - 1) + responseTime) / this.metrics.logs;
      
      return {
        status: 'healthy',
        responseTime,
        metrics: this.metrics
      };
    } catch (error) {
      this.metrics.errors++;
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

  resetMetrics() {
    this.metrics = {
      logs: 0,
      errors: 0,
      warnings: 0,
      avgResponseTime: 0
    };
  }
}
```

## Related Topics

- [@log Operator](./27-tsklang-javascript-operator-log.md)
- [@metrics Operator](./49-tsklang-javascript-operator-metrics.md)
- [@monitor Operator](./51-tsklang-javascript-operator-monitor.md)
- [@debug Operator](./52-tsklang-javascript-operator-debug.md)
- [@trace Operator](./53-tsklang-javascript-operator-trace.md) 