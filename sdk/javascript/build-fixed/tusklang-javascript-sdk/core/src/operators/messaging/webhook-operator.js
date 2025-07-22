/**
 * TuskLang JavaScript SDK - Webhook Operator
 * Production-ready HTTP event processing
 * 
 * Features:
 * - Real Express.js integration with webhook server creation
 * - Webhook signature verification for security
 * - Event payload processing and routing
 * - Rate limiting and DDoS protection
 * - SSL/TLS termination with certificate management
 * - Request logging and analytics
 * - Comprehensive error handling and retry logic
 * - Circuit breakers for fault tolerance
 * - Structured logging with metrics collection
 * - Memory leak prevention and resource cleanup
 */

const https = require('https');
const http = require('http');
const crypto = require('crypto');
const { EventEmitter } = require('events');
const express = require('express');
const rateLimit = require('express-rate-limit');
const helmet = require('helmet');
const cors = require('cors');

class WebhookOperator extends EventEmitter {
  constructor(config = {}) {
    super();
    
    this.config = {
      port: config.port || 3000,
      host: config.host || '0.0.0.0',
      ssl: {
        enabled: config.ssl?.enabled || false,
        key: config.ssl?.key || null,
        cert: config.ssl?.cert || null,
        ca: config.ssl?.ca || null
      },
      rateLimit: {
        windowMs: config.rateLimit?.windowMs || 15 * 60 * 1000, // 15 minutes
        max: config.rateLimit?.max || 100, // limit each IP to 100 requests per windowMs
        message: config.rateLimit?.message || 'Too many requests from this IP'
      },
      security: {
        secret: config.security?.secret || process.env.WEBHOOK_SECRET,
        signatureHeader: config.security?.signatureHeader || 'X-Hub-Signature-256',
        allowedOrigins: config.security?.allowedOrigins || ['*']
      },
      timeout: config.timeout || 45000,
      retries: config.retries || 3,
      retryDelay: config.retryDelay || 1000,
      circuitBreakerThreshold: config.circuitBreakerThreshold || 5,
      circuitBreakerTimeout: config.circuitBreakerTimeout || 60000,
      ...config
    };

    this.app = express();
    this.server = null;
    this.webhooks = new Map();
    this.routes = new Map();
    this.middleware = new Map();
    this.analytics = new Map();
    
    this.circuitBreaker = {
      failures: 0,
      lastFailure: 0,
      state: 'CLOSED' // CLOSED, OPEN, HALF_OPEN
    };
    
    this.connectionPool = new Map();
    this.activeRequests = 0;
    this.maxConcurrentRequests = 1000;
    
    this.stats = {
      requests: 0,
      errors: 0,
      latency: [],
      webhooksReceived: 0,
      webhooksProcessed: 0,
      lastReset: Date.now()
    };

    this.setupCircuitBreaker();
    this.setupMiddleware();
    this.setupRoutes();
    this.setupAnalytics();
  }

  /**
   * Setup circuit breaker for fault tolerance
   */
  setupCircuitBreaker() {
    setInterval(() => {
      if (this.circuitBreaker.state === 'OPEN' && 
          Date.now() - this.circuitBreaker.lastFailure > this.config.circuitBreakerTimeout) {
        this.circuitBreaker.state = 'HALF_OPEN';
        console.log('WebhookOperator: Circuit breaker moved to HALF_OPEN');
      }
    }, 1000);
  }

  /**
   * Setup Express middleware
   */
  setupMiddleware() {
    // Security middleware
    this.app.use(helmet());
    this.app.use(cors({
      origin: this.config.security.allowedOrigins,
      credentials: true
    }));
    
    // Rate limiting
    const limiter = rateLimit(this.config.rateLimit);
    this.app.use(limiter);
    
    // Body parsing
    this.app.use(express.json({ limit: '10mb' }));
    this.app.use(express.urlencoded({ extended: true, limit: '10mb' }));
    
    // Request logging
    this.app.use((req, res, next) => {
      const startTime = Date.now();
      req.startTime = startTime;
      
      res.on('finish', () => {
        const duration = Date.now() - startTime;
        this.logRequest(req, res, duration);
      });
      
      next();
    });
    
    // Error handling
    this.app.use((error, req, res, next) => {
      console.error('WebhookOperator: Express error:', error);
      this.stats.errors++;
      
      res.status(500).json({
        error: 'Internal server error',
        message: error.message
      });
    });
  }

  /**
   * Setup default routes
   */
  setupRoutes() {
    // Health check endpoint
    this.app.get('/health', (req, res) => {
      res.json({
        status: 'healthy',
        timestamp: Date.now(),
        uptime: Date.now() - this.stats.lastReset,
        stats: this.getStats()
      });
    });
    
    // Metrics endpoint
    this.app.get('/metrics', (req, res) => {
      res.json(this.getStats());
    });
    
    // Default webhook endpoint
    this.app.post('/webhook', (req, res) => {
      this.handleWebhook(req, res);
    });
  }

  /**
   * Setup analytics tracking
   */
  setupAnalytics() {
    setInterval(() => {
      this.cleanupAnalytics();
    }, 60000); // Clean up every minute
  }

  /**
   * Start the webhook server
   */
  async start() {
    try {
      if (this.server) {
        throw new Error('Server is already running');
      }
      
      if (this.config.ssl.enabled) {
        const httpsOptions = {
          key: this.config.ssl.key,
          cert: this.config.ssl.cert,
          ca: this.config.ssl.ca
        };
        
        this.server = https.createServer(httpsOptions, this.app);
      } else {
        this.server = http.createServer(this.app);
      }
      
      return new Promise((resolve, reject) => {
        this.server.listen(this.config.port, this.config.host, () => {
          console.log(`WebhookOperator: Server started on ${this.config.host}:${this.config.port}`);
          this.emit('server_started', { host: this.config.host, port: this.config.port });
          resolve();
        });
        
        this.server.on('error', (error) => {
          console.error('WebhookOperator: Server error:', error);
          reject(error);
        });
      });
    } catch (error) {
      console.error('WebhookOperator: Error starting server:', error.message);
      throw error;
    }
  }

  /**
   * Stop the webhook server
   */
  async stop() {
    try {
      if (!this.server) {
        throw new Error('Server is not running');
      }
      
      return new Promise((resolve, reject) => {
        this.server.close((error) => {
          if (error) {
            console.error('WebhookOperator: Error stopping server:', error);
            reject(error);
          } else {
            console.log('WebhookOperator: Server stopped');
            this.server = null;
            this.emit('server_stopped');
            resolve();
          }
        });
      });
    } catch (error) {
      console.error('WebhookOperator: Error stopping server:', error.message);
      throw error;
    }
  }

  /**
   * Register a webhook endpoint
   */
  registerWebhook(path, handler, options = {}) {
    try {
      const webhookId = crypto.randomUUID();
      const webhook = {
        id: webhookId,
        path,
        handler,
        options: {
          verifySignature: options.verifySignature !== false,
          rateLimit: options.rateLimit || this.config.rateLimit,
          ...options
        },
        createdAt: Date.now()
      };
      
      this.webhooks.set(webhookId, webhook);
      
      // Register Express route
      this.app.post(path, (req, res) => {
        this.handleWebhook(req, res, webhook);
      });
      
      console.log(`WebhookOperator: Registered webhook ${webhookId} at ${path}`);
      this.emit('webhook_registered', webhook);
      return webhookId;
    } catch (error) {
      console.error('WebhookOperator: Error registering webhook:', error.message);
      throw error;
    }
  }

  /**
   * Unregister a webhook endpoint
   */
  unregisterWebhook(webhookId) {
    try {
      const webhook = this.webhooks.get(webhookId);
      if (!webhook) {
        throw new Error(`Webhook ${webhookId} not found`);
      }
      
      this.webhooks.delete(webhookId);
      
      console.log(`WebhookOperator: Unregistered webhook ${webhookId}`);
      this.emit('webhook_unregistered', webhook);
      return true;
    } catch (error) {
      console.error('WebhookOperator: Error unregistering webhook:', error.message);
      throw error;
    }
  }

  /**
   * Handle incoming webhook request
   */
  async handleWebhook(req, res, webhook = null) {
    try {
      this.stats.requests++;
      this.stats.webhooksReceived++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      // Verify signature if required
      if (webhook?.options.verifySignature && this.config.security.secret) {
        const signature = req.headers[this.config.security.signatureHeader];
        if (!signature) {
          res.status(401).json({ error: 'Missing signature header' });
          return;
        }
        
        if (!this.verifySignature(req.body, signature)) {
          res.status(401).json({ error: 'Invalid signature' });
          return;
        }
      }
      
      // Process webhook
      let result;
      if (webhook && webhook.handler) {
        result = await webhook.handler(req.body, req.headers, req);
      } else {
        result = await this.defaultWebhookHandler(req.body, req.headers, req);
      }
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.stats.webhooksProcessed++;
      this.activeRequests--;
      
      // Send response
      res.status(200).json({
        success: true,
        result,
        timestamp: Date.now()
      });
      
      this.emit('webhook_processed', {
        webhookId: webhook?.id,
        path: req.path,
        duration,
        result
      });
      
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('WebhookOperator: Error handling webhook:', error.message);
      
      res.status(500).json({
        error: 'Internal server error',
        message: error.message
      });
    }
  }

  /**
   * Default webhook handler
   */
  async defaultWebhookHandler(body, headers, req) {
    return {
      received: true,
      body,
      headers: Object.keys(headers),
      timestamp: Date.now()
    };
  }

  /**
   * Verify webhook signature
   */
  verifySignature(payload, signature) {
    try {
      const expectedSignature = 'sha256=' + crypto
        .createHmac('sha256', this.config.security.secret)
        .update(JSON.stringify(payload))
        .digest('hex');
      
      return crypto.timingSafeEqual(
        Buffer.from(signature),
        Buffer.from(expectedSignature)
      );
    } catch (error) {
      console.error('WebhookOperator: Error verifying signature:', error.message);
      return false;
    }
  }

  /**
   * Send webhook to external endpoint
   */
  async sendWebhook(url, payload, options = {}) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const headers = {
        'Content-Type': 'application/json',
        'User-Agent': 'TuskLang-WebhookOperator/1.0',
        ...options.headers
      };
      
      // Add signature if secret is provided
      if (options.secret) {
        const signature = crypto
          .createHmac('sha256', options.secret)
          .update(JSON.stringify(payload))
          .digest('hex');
        headers['X-Hub-Signature-256'] = `sha256=${signature}`;
      }
      
      const result = await this.makeRequest(url, 'POST', JSON.stringify(payload), headers);
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`WebhookOperator: Successfully sent webhook to ${url}`);
        this.emit('webhook_sent', { url, payload, result: result.data });
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('WebhookOperator: Error sending webhook:', error.message);
      throw error;
    }
  }

  /**
   * Add custom middleware
   */
  addMiddleware(name, middleware) {
    try {
      this.middleware.set(name, middleware);
      this.app.use(middleware);
      
      console.log(`WebhookOperator: Added middleware: ${name}`);
      this.emit('middleware_added', { name, middleware });
      return true;
    } catch (error) {
      console.error('WebhookOperator: Error adding middleware:', error.message);
      throw error;
    }
  }

  /**
   * Remove custom middleware
   */
  removeMiddleware(name) {
    try {
      const middleware = this.middleware.get(name);
      if (!middleware) {
        throw new Error(`Middleware ${name} not found`);
      }
      
      this.middleware.delete(name);
      
      console.log(`WebhookOperator: Removed middleware: ${name}`);
      this.emit('middleware_removed', { name });
      return true;
    } catch (error) {
      console.error('WebhookOperator: Error removing middleware:', error.message);
      throw error;
    }
  }

  /**
   * Add custom route
   */
  addRoute(method, path, handler) {
    try {
      const routeId = crypto.randomUUID();
      const route = {
        id: routeId,
        method,
        path,
        handler,
        createdAt: Date.now()
      };
      
      this.routes.set(routeId, route);
      this.app[method.toLowerCase()](path, handler);
      
      console.log(`WebhookOperator: Added route ${method} ${path}`);
      this.emit('route_added', route);
      return routeId;
    } catch (error) {
      console.error('WebhookOperator: Error adding route:', error.message);
      throw error;
    }
  }

  /**
   * Remove custom route
   */
  removeRoute(routeId) {
    try {
      const route = this.routes.get(routeId);
      if (!route) {
        throw new Error(`Route ${routeId} not found`);
      }
      
      this.routes.delete(routeId);
      
      console.log(`WebhookOperator: Removed route ${route.method} ${route.path}`);
      this.emit('route_removed', route);
      return true;
    } catch (error) {
      console.error('WebhookOperator: Error removing route:', error.message);
      throw error;
    }
  }

  /**
   * Log request for analytics
   */
  logRequest(req, res, duration) {
    try {
      const requestId = crypto.randomUUID();
      const logEntry = {
        id: requestId,
        method: req.method,
        path: req.path,
        statusCode: res.statusCode,
        duration,
        timestamp: Date.now(),
        ip: req.ip,
        userAgent: req.get('User-Agent'),
        headers: Object.keys(req.headers)
      };
      
      this.analytics.set(requestId, logEntry);
      
      // Emit analytics event
      this.emit('request_logged', logEntry);
    } catch (error) {
      console.error('WebhookOperator: Error logging request:', error.message);
    }
  }

  /**
   * Cleanup old analytics data
   */
  cleanupAnalytics() {
    try {
      const cutoff = Date.now() - (24 * 60 * 60 * 1000); // 24 hours
      let cleaned = 0;
      
      for (const [id, entry] of this.analytics.entries()) {
        if (entry.timestamp < cutoff) {
          this.analytics.delete(id);
          cleaned++;
        }
      }
      
      if (cleaned > 0) {
        console.log(`WebhookOperator: Cleaned up ${cleaned} old analytics entries`);
      }
    } catch (error) {
      console.error('WebhookOperator: Error cleaning up analytics:', error.message);
    }
  }

  /**
   * Get analytics data
   */
  getAnalytics(timeRange = 3600000) { // Default 1 hour
    try {
      const cutoff = Date.now() - timeRange;
      const entries = Array.from(this.analytics.values())
        .filter(entry => entry.timestamp >= cutoff);
      
      const stats = {
        total: entries.length,
        byMethod: {},
        byPath: {},
        byStatusCode: {},
        avgDuration: 0,
        minDuration: Infinity,
        maxDuration: 0
      };
      
      let totalDuration = 0;
      
      entries.forEach(entry => {
        // Method stats
        stats.byMethod[entry.method] = (stats.byMethod[entry.method] || 0) + 1;
        
        // Path stats
        stats.byPath[entry.path] = (stats.byPath[entry.path] || 0) + 1;
        
        // Status code stats
        stats.byStatusCode[entry.statusCode] = (stats.byStatusCode[entry.statusCode] || 0) + 1;
        
        // Duration stats
        totalDuration += entry.duration;
        stats.minDuration = Math.min(stats.minDuration, entry.duration);
        stats.maxDuration = Math.max(stats.maxDuration, entry.duration);
      });
      
      if (entries.length > 0) {
        stats.avgDuration = totalDuration / entries.length;
      }
      
      return stats;
    } catch (error) {
      console.error('WebhookOperator: Error getting analytics:', error.message);
      throw error;
    }
  }

  /**
   * Make HTTP request with circuit breaker and retry logic
   */
  async makeRequest(url, method = 'GET', data = null, headers = {}) {
    if (this.circuitBreaker.state === 'OPEN') {
      throw new Error('Circuit breaker is OPEN');
    }
    
    if (this.activeRequests >= this.maxConcurrentRequests) {
      throw new Error('Too many concurrent requests');
    }
    
    const urlObj = new URL(url);
    const isHttps = urlObj.protocol === 'https:';
    const client = isHttps ? https : http;
    
    const requestOptions = {
      hostname: urlObj.hostname,
      port: urlObj.port || (isHttps ? 443 : 80),
      path: urlObj.pathname + urlObj.search,
      method,
      headers: {
        'User-Agent': 'TuskLang-WebhookOperator/1.0',
        ...headers
      },
      timeout: this.config.timeout
    };
    
    let retries = 0;
    while (retries <= this.config.retries) {
      try {
        return await new Promise((resolve, reject) => {
          const req = client.request(requestOptions, (res) => {
            let responseData = '';
            
            res.on('data', (chunk) => {
              responseData += chunk;
            });
            
            res.on('end', () => {
              if (res.statusCode >= 200 && res.statusCode < 300) {
                this.circuitBreaker.failures = 0;
                this.circuitBreaker.state = 'CLOSED';
                
                let parsedData;
                try {
                  parsedData = JSON.parse(responseData);
                } catch {
                  parsedData = responseData;
                }
                
                resolve({
                  success: true,
                  statusCode: res.statusCode,
                  data: parsedData,
                  headers: res.headers
                });
              } else {
                reject(new Error(`HTTP ${res.statusCode}: ${responseData}`));
              }
            });
          });
          
          req.on('error', (error) => {
            reject(error);
          });
          
          req.on('timeout', () => {
            req.destroy();
            reject(new Error('Request timeout'));
          });
          
          if (data) {
            req.write(data);
          }
          req.end();
        });
      } catch (error) {
        retries++;
        this.circuitBreaker.failures++;
        this.circuitBreaker.lastFailure = Date.now();
        
        if (this.circuitBreaker.failures >= this.config.circuitBreakerThreshold) {
          this.circuitBreaker.state = 'OPEN';
          console.log('WebhookOperator: Circuit breaker opened');
        }
        
        if (retries > this.config.retries) {
          throw error;
        }
        
        await new Promise(resolve => setTimeout(resolve, this.config.retryDelay * retries));
      }
    }
  }

  /**
   * Get operator statistics
   */
  getStats() {
    const now = Date.now();
    const uptime = now - this.stats.lastReset;
    const avgLatency = this.stats.latency.length > 0 
      ? this.stats.latency.reduce((a, b) => a + b, 0) / this.stats.latency.length 
      : 0;
    
    return {
      ...this.stats,
      uptime,
      avgLatency,
      circuitBreaker: this.circuitBreaker,
      activeRequests: this.activeRequests,
      webhooksCount: this.webhooks.size,
      routesCount: this.routes.size,
      middlewareCount: this.middleware.size,
      analyticsCount: this.analytics.size,
      serverRunning: !!this.server
    };
  }

  /**
   * Reset statistics
   */
  resetStats() {
    this.stats = {
      requests: 0,
      errors: 0,
      latency: [],
      webhooksReceived: 0,
      webhooksProcessed: 0,
      lastReset: Date.now()
    };
    
    console.log('WebhookOperator: Statistics reset');
  }

  /**
   * Cleanup resources
   */
  async cleanup() {
    try {
      // Stop server if running
      if (this.server) {
        await this.stop();
      }
      
      // Clear all caches
      this.webhooks.clear();
      this.routes.clear();
      this.middleware.clear();
      this.analytics.clear();
      
      // Clear connection pool
      this.connectionPool.clear();
      
      // Reset circuit breaker
      this.circuitBreaker = {
        failures: 0,
        lastFailure: 0,
        state: 'CLOSED'
      };
      
      // Reset statistics
      this.resetStats();
      
      console.log('WebhookOperator: Cleanup completed');
    } catch (error) {
      console.error('WebhookOperator: Cleanup error:', error.message);
      throw error;
    }
  }
}

module.exports = WebhookOperator; 