/**
 * TuskLang Advanced API Gateway and Microservices Integration
 * Provides comprehensive API management and microservices orchestration
 */

const { EventEmitter } = require('events');

class APIGateway {
  constructor(options = {}) {
    this.options = {
      port: options.port || 3000,
      cors: options.cors !== false,
      rateLimit: options.rateLimit || 100,
      timeout: options.timeout || 30000,
      ...options
    };
    
    this.routes = new Map();
    this.middleware = [];
    this.services = new Map();
    this.cache = new Map();
    this.metrics = {
      requests: 0,
      errors: 0,
      responseTime: 0
    };
    this.eventEmitter = new EventEmitter();
    this.isRunning = false;
  }

  /**
   * Register a route
   */
  registerRoute(method, path, handler, options = {}) {
    const routeKey = `${method.toUpperCase()}:${path}`;
    const route = {
      method: method.toUpperCase(),
      path,
      handler,
      options: {
        auth: options.auth || false,
        rateLimit: options.rateLimit || this.options.rateLimit,
        timeout: options.timeout || this.options.timeout,
        cache: options.cache || false,
        ...options
      }
    };

    this.routes.set(routeKey, route);
    return route;
  }

  /**
   * Register a microservice
   */
  registerService(name, service) {
    this.services.set(name, {
      name,
      service,
      health: 'healthy',
      lastCheck: Date.now(),
      metrics: {
        requests: 0,
        errors: 0,
        responseTime: 0
      }
    });
  }

  /**
   * Add middleware
   */
  addMiddleware(middleware) {
    this.middleware.push(middleware);
  }

  /**
   * Handle incoming request
   */
  async handleRequest(method, path, headers, body, params = {}) {
    const startTime = Date.now();
    const routeKey = `${method.toUpperCase()}:${path}`;
    const route = this.routes.get(routeKey);

    if (!route) {
      throw new Error(`Route not found: ${method} ${path}`);
    }

    // Apply middleware
    let request = { method, path, headers, body, params };
    for (const middleware of this.middleware) {
      request = await middleware(request);
    }

    // Check cache
    if (route.options.cache) {
      const cacheKey = this.generateCacheKey(method, path, body);
      const cached = this.cache.get(cacheKey);
      if (cached && Date.now() - cached.timestamp < 300000) { // 5 minutes
        return cached.response;
      }
    }

    // Execute handler
    try {
      const response = await this.executeHandler(route, request);
      
      // Cache response
      if (route.options.cache) {
        const cacheKey = this.generateCacheKey(method, path, body);
        this.cache.set(cacheKey, {
          response,
          timestamp: Date.now()
        });
      }

      // Update metrics
      const responseTime = Date.now() - startTime;
      this.updateMetrics(responseTime, false);

      return response;
    } catch (error) {
      this.updateMetrics(Date.now() - startTime, true);
      throw error;
    }
  }

  /**
   * Execute route handler
   */
  async executeHandler(route, request) {
    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        reject(new Error('Request timeout'));
      }, route.options.timeout);

      try {
        const result = route.handler(request);
        if (result instanceof Promise) {
          result.then(resolve).catch(reject).finally(() => clearTimeout(timeout));
        } else {
          clearTimeout(timeout);
          resolve(result);
        }
      } catch (error) {
        clearTimeout(timeout);
        reject(error);
      }
    });
  }

  /**
   * Call microservice
   */
  async callService(serviceName, method, data = {}) {
    const service = this.services.get(serviceName);
    if (!service) {
      throw new Error(`Service not found: ${serviceName}`);
    }

    const startTime = Date.now();
    try {
      const result = await service.service[method](data);
      
      // Update service metrics
      const responseTime = Date.now() - startTime;
      service.metrics.requests++;
      service.metrics.responseTime = 
        (service.metrics.responseTime * (service.metrics.requests - 1) + responseTime) / 
        service.metrics.requests;

      return result;
    } catch (error) {
      service.metrics.errors++;
      throw error;
    }
  }

  /**
   * Health check for all services
   */
  async healthCheck() {
    const health = {
      gateway: 'healthy',
      services: {},
      timestamp: new Date().toISOString()
    };

    for (const [name, service] of this.services) {
      try {
        if (service.service.healthCheck) {
          await service.service.healthCheck();
          service.health = 'healthy';
        } else {
          service.health = 'unknown';
        }
      } catch (error) {
        service.health = 'unhealthy';
      }
      
      service.lastCheck = Date.now();
      health.services[name] = service.health;
    }

    return health;
  }

  /**
   * Get gateway statistics
   */
  getStats() {
    return {
      isRunning: this.isRunning,
      routes: this.routes.size,
      services: this.services.size,
      cache: this.cache.size,
      metrics: { ...this.metrics },
      serviceMetrics: Object.fromEntries(
        Array.from(this.services.entries()).map(([name, service]) => [
          name,
          { ...service.metrics, health: service.health }
        ])
      )
    };
  }

  /**
   * Clear cache
   */
  clearCache() {
    this.cache.clear();
  }

  /**
   * Start the gateway
   */
  async start() {
    if (this.isRunning) {
      throw new Error('API Gateway is already running');
    }

    this.isRunning = true;
    this.eventEmitter.emit('started');
    console.log(`API Gateway started on port ${this.options.port}`);
  }

  /**
   * Stop the gateway
   */
  async stop() {
    this.isRunning = false;
    this.eventEmitter.emit('stopped');
    console.log('API Gateway stopped');
  }

  generateCacheKey(method, path, body) {
    return `${method}:${path}:${JSON.stringify(body)}`;
  }

  updateMetrics(responseTime, isError) {
    this.metrics.requests++;
    if (isError) {
      this.metrics.errors++;
    }
    this.metrics.responseTime = 
      (this.metrics.responseTime * (this.metrics.requests - 1) + responseTime) / 
      this.metrics.requests;
  }
}

class Microservice {
  constructor(name, options = {}) {
    this.name = name;
    this.options = {
      port: options.port || 0,
      healthCheckInterval: options.healthCheckInterval || 30000,
      ...options
    };
    
    this.methods = new Map();
    this.health = 'healthy';
    this.metrics = {
      requests: 0,
      errors: 0,
      responseTime: 0
    };
    this.eventEmitter = new EventEmitter();
  }

  /**
   * Register a method
   */
  registerMethod(name, handler, options = {}) {
    this.methods.set(name, {
      handler,
      options: {
        timeout: options.timeout || 30000,
        auth: options.auth || false,
        ...options
      }
    });
  }

  /**
   * Call a method
   */
  async callMethod(name, data = {}) {
    const method = this.methods.get(name);
    if (!method) {
      throw new Error(`Method not found: ${name}`);
    }

    const startTime = Date.now();
    try {
      const result = await method.handler(data);
      
      // Update metrics
      const responseTime = Date.now() - startTime;
      this.metrics.requests++;
      this.metrics.responseTime = 
        (this.metrics.responseTime * (this.metrics.requests - 1) + responseTime) / 
        this.metrics.requests;

      return result;
    } catch (error) {
      this.metrics.errors++;
      throw error;
    }
  }

  /**
   * Health check
   */
  async healthCheck() {
    return {
      name: this.name,
      health: this.health,
      timestamp: new Date().toISOString(),
      metrics: { ...this.metrics }
    };
  }

  /**
   * Get service statistics
   */
  getStats() {
    return {
      name: this.name,
      health: this.health,
      methods: this.methods.size,
      metrics: { ...this.metrics }
    };
  }
}

class LoadBalancer {
  constructor(options = {}) {
    this.options = {
      algorithm: options.algorithm || 'round-robin',
      healthCheckInterval: options.healthCheckInterval || 30000,
      ...options
    };
    
    this.services = new Map();
    this.currentIndex = 0;
  }

  /**
   * Add service
   */
  addService(name, service) {
    this.services.set(name, {
      name,
      service,
      weight: service.weight || 1,
      health: 'healthy',
      lastCheck: Date.now()
    });
  }

  /**
   * Get next service
   */
  getNextService() {
    const healthyServices = Array.from(this.services.values())
      .filter(s => s.health === 'healthy');

    if (healthyServices.length === 0) {
      throw new Error('No healthy services available');
    }

    switch (this.options.algorithm) {
      case 'round-robin':
        const service = healthyServices[this.currentIndex % healthyServices.length];
        this.currentIndex++;
        return service;
      
      case 'weighted':
        return this.getWeightedService(healthyServices);
      
      case 'least-connections':
        return this.getLeastConnectionsService(healthyServices);
      
      default:
        return healthyServices[0];
    }
  }

  /**
   * Get weighted service
   */
  getWeightedService(services) {
    const totalWeight = services.reduce((sum, s) => sum + s.weight, 0);
    let random = Math.random() * totalWeight;
    
    for (const service of services) {
      random -= service.weight;
      if (random <= 0) {
        return service;
      }
    }
    
    return services[0];
  }

  /**
   * Get least connections service
   */
  getLeastConnectionsService(services) {
    return services.reduce((min, service) => 
      service.metrics.requests < min.metrics.requests ? service : min
    );
  }

  /**
   * Health check all services
   */
  async healthCheck() {
    for (const [name, service] of this.services) {
      try {
        if (service.service.healthCheck) {
          await service.service.healthCheck();
          service.health = 'healthy';
        }
      } catch (error) {
        service.health = 'unhealthy';
      }
      service.lastCheck = Date.now();
    }
  }
}

module.exports = {
  APIGateway,
  Microservice,
  LoadBalancer
}; 