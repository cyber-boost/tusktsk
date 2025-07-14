# @env Variables - JavaScript

## Overview

The `@env` function in TuskLang provides secure access to environment variables with fallback values and validation. This is essential for JavaScript applications that need to manage configuration across different environments (development, staging, production) while maintaining security and flexibility.

## Basic Syntax

```tsk
# Simple environment variable access
api_key: @env("API_KEY")
database_url: @env("DATABASE_URL", "sqlite:///default.db")
debug_mode: @env("DEBUG", "false")

# Environment-specific configurations
node_env: @env("NODE_ENV", "development")
port: @env("PORT", "3000")
host: @env("HOST", "localhost")
```

## JavaScript Integration

### Node.js Environment Integration

```javascript
const tusk = require('tusklang');

// Load configuration with environment variables
const config = tusk.load('environment.tsk');

// Access environment variables
console.log(config.api_key); // Value from process.env.API_KEY
console.log(config.database_url); // Value from process.env.DATABASE_URL or fallback
console.log(config.debug_mode); // "false" if DEBUG not set

// Use environment variables in application
const appConfig = {
  apiKey: config.api_key,
  databaseUrl: config.database_url,
  debug: config.debug_mode === 'true',
  port: parseInt(config.port),
  host: config.host
};

// Dynamic environment access
const customEnvVar = await tusk.env("CUSTOM_VAR", "default_value");
const requiredVar = await tusk.env("REQUIRED_VAR"); // Throws if not set
```

### Browser Environment Integration

```javascript
// Load TuskLang configuration
const config = await tusk.load('environment.tsk');

// Use environment variables in frontend
class EnvironmentManager {
  constructor() {
    this.apiKey = config.api_key;
    this.debugMode = config.debug_mode === 'true';
    this.apiUrl = config.api_url || 'https://api.example.com';
  }
  
  async getEnvironmentVariable(name, fallback = null) {
    try {
      return await tusk.env(name, fallback);
    } catch (error) {
      console.warn(`Environment variable ${name} not found, using fallback`);
      return fallback;
    }
  }
  
  isDevelopment() {
    return config.node_env === 'development';
  }
  
  isProduction() {
    return config.node_env === 'production';
  }
  
  getApiConfig() {
    return {
      url: this.apiUrl,
      key: this.apiKey,
      debug: this.debugMode
    };
  }
}
```

## Advanced Usage

### Environment-Specific Configurations

```tsk
# Environment-based configurations
development_config: @env("NODE_ENV") == "development" ? {
  "debug": true,
  "log_level": "debug",
  "cache_enabled": false
} : {}

production_config: @env("NODE_ENV") == "production" ? {
  "debug": false,
  "log_level": "error",
  "cache_enabled": true
} : {}

# Conditional environment variables
conditional_api_url: @env("NODE_ENV") == "production" ? @env("PROD_API_URL") : @env("DEV_API_URL", "http://localhost:3000")
```

### Secure Environment Variables

```tsk
# Secure environment variable access
secure_api_key: @env.secure("API_KEY")
secure_database_password: @env.secure("DB_PASSWORD")
secure_jwt_secret: @env.secure("JWT_SECRET")

# Environment variables with validation
validated_port: @env("PORT", "3000", {"type": "number", "min": 1024, "max": 65535})
validated_debug: @env("DEBUG", "false", {"type": "boolean"})
validated_url: @env("DATABASE_URL", "", {"type": "url"})
```

### Environment Variable Groups

```tsk
# Database environment variables
database_config: {
  "host": @env("DB_HOST", "localhost"),
  "port": @env("DB_PORT", "5432"),
  "name": @env("DB_NAME", "myapp"),
  "user": @env("DB_USER", "postgres"),
  "password": @env.secure("DB_PASSWORD")
}

# API environment variables
api_config: {
  "base_url": @env("API_BASE_URL", "https://api.example.com"),
  "key": @env.secure("API_KEY"),
  "timeout": @env("API_TIMEOUT", "30000"),
  "retries": @env("API_RETRIES", "3")
}
```

## JavaScript Implementation

### Custom Environment Manager

```javascript
class TuskEnvironmentManager {
  constructor() {
    this.cache = new Map();
    this.validators = new Map();
    this.securePatterns = [
      /password/i,
      /secret/i,
      /key/i,
      /token/i
    ];
  }
  
  async get(name, fallback = null, options = {}) {
    const cacheKey = `${name}_${JSON.stringify(options)}`;
    
    // Check cache first
    if (this.cache.has(cacheKey)) {
      return this.cache.get(cacheKey);
    }
    
    // Get environment variable
    let value = this.getEnvironmentVariable(name);
    
    // Use fallback if not found
    if (value === undefined || value === null) {
      if (fallback === null && options.required) {
        throw new Error(`Required environment variable ${name} is not set`);
      }
      value = fallback;
    }
    
    // Validate value if validator provided
    if (options.validator) {
      value = await this.validateValue(name, value, options.validator);
    }
    
    // Apply type conversion
    if (options.type) {
      value = this.convertType(value, options.type);
    }
    
    // Cache result
    this.cache.set(cacheKey, value);
    
    return value;
  }
  
  async getSecure(name, fallback = null) {
    const value = await this.get(name, fallback, { required: true });
    
    // Log warning for non-secure access
    if (!this.isSecureVariable(name)) {
      console.warn(`Accessing potentially sensitive environment variable: ${name}`);
    }
    
    return value;
  }
  
  getEnvironmentVariable(name) {
    // Node.js environment
    if (typeof process !== 'undefined' && process.env) {
      return process.env[name];
    }
    
    // Browser environment (limited)
    if (typeof window !== 'undefined') {
      // Note: Browser environment variables are limited
      // Usually accessed through meta tags or global variables
      return window[name] || null;
    }
    
    return null;
  }
  
  async validateValue(name, value, validator) {
    const validatorFn = this.validators.get(validator) || this.getDefaultValidator(validator);
    
    if (validatorFn) {
      const isValid = await validatorFn(value);
      if (!isValid) {
        throw new Error(`Environment variable ${name} failed validation: ${validator}`);
      }
    }
    
    return value;
  }
  
  convertType(value, type) {
    switch (type) {
      case 'number':
        const num = parseFloat(value);
        return isNaN(num) ? 0 : num;
      case 'integer':
        const int = parseInt(value, 10);
        return isNaN(int) ? 0 : int;
      case 'boolean':
        return value === 'true' || value === '1' || value === true;
      case 'array':
        return Array.isArray(value) ? value : value.split(',').map(v => v.trim());
      case 'object':
        return typeof value === 'string' ? JSON.parse(value) : value;
      default:
        return value;
    }
  }
  
  isSecureVariable(name) {
    return this.securePatterns.some(pattern => pattern.test(name));
  }
  
  getDefaultValidator(type) {
    const validators = {
      'url': (value) => {
        try {
          new URL(value);
          return true;
        } catch {
          return false;
        }
      },
      'email': (value) => {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(value);
      },
      'number': (value) => {
        return !isNaN(parseFloat(value));
      },
      'boolean': (value) => {
        return ['true', 'false', '0', '1'].includes(value.toLowerCase());
      }
    };
    
    return validators[type];
  }
  
  addValidator(name, validatorFn) {
    this.validators.set(name, validatorFn);
  }
  
  clearCache() {
    this.cache.clear();
  }
}
```

### TypeScript Support

```typescript
interface EnvironmentOptions {
  required?: boolean;
  type?: 'string' | 'number' | 'integer' | 'boolean' | 'array' | 'object';
  validator?: string | ((value: any) => boolean | Promise<boolean>);
  min?: number;
  max?: number;
}

interface DatabaseConfig {
  host: string;
  port: number;
  name: string;
  user: string;
  password: string;
}

interface APIConfig {
  baseUrl: string;
  key: string;
  timeout: number;
  retries: number;
}

class TypedEnvironmentManager {
  private cache: Map<string, any>;
  private validators: Map<string, (value: any) => boolean | Promise<boolean>>;
  
  constructor() {
    this.cache = new Map();
    this.validators = new Map();
  }
  
  async get<T>(
    name: string,
    fallback?: T,
    options: EnvironmentOptions = {}
  ): Promise<T> {
    // Implementation similar to JavaScript version
    return fallback as T;
  }
  
  async getSecure<T>(name: string, fallback?: T): Promise<T> {
    return await this.get<T>(name, fallback, { required: true });
  }
  
  async getDatabaseConfig(): Promise<DatabaseConfig> {
    return {
      host: await this.get('DB_HOST', 'localhost'),
      port: await this.get('DB_PORT', 5432, { type: 'integer' }),
      name: await this.get('DB_NAME', 'myapp'),
      user: await this.get('DB_USER', 'postgres'),
      password: await this.getSecure('DB_PASSWORD')
    };
  }
  
  async getAPIConfig(): Promise<APIConfig> {
    return {
      baseUrl: await this.get('API_BASE_URL', 'https://api.example.com'),
      key: await this.getSecure('API_KEY'),
      timeout: await this.get('API_TIMEOUT', 30000, { type: 'integer' }),
      retries: await this.get('API_RETRIES', 3, { type: 'integer' })
    };
  }
}
```

## Real-World Examples

### Application Configuration

```tsk
# Application environment configuration
app_config: {
  "name": @env("APP_NAME", "TuskLang App"),
  "version": @env("APP_VERSION", "1.0.0"),
  "environment": @env("NODE_ENV", "development"),
  "debug": @env("DEBUG", "false"),
  "port": @env("PORT", "3000"),
  "host": @env("HOST", "localhost")
}

# Feature flags from environment
feature_flags: {
  "new_ui": @env("FEATURE_NEW_UI", "false"),
  "beta_features": @env("FEATURE_BETA", "false"),
  "analytics": @env("FEATURE_ANALYTICS", "true")
}
```

### Database Configuration

```tsk
# Database environment configuration
database_env: {
  "type": @env("DB_TYPE", "postgresql"),
  "host": @env("DB_HOST", "localhost"),
  "port": @env("DB_PORT", "5432"),
  "name": @env("DB_NAME", "myapp"),
  "user": @env("DB_USER", "postgres"),
  "password": @env.secure("DB_PASSWORD"),
  "ssl": @env("DB_SSL", "false")
}

# Redis configuration
redis_env: {
  "host": @env("REDIS_HOST", "localhost"),
  "port": @env("REDIS_PORT", "6379"),
  "password": @env.secure("REDIS_PASSWORD"),
  "database": @env("REDIS_DB", "0")
}
```

### External Service Configuration

```tsk
# External API configuration
external_apis: {
  "stripe": {
    "public_key": @env("STRIPE_PUBLIC_KEY"),
    "secret_key": @env.secure("STRIPE_SECRET_KEY"),
    "webhook_secret": @env.secure("STRIPE_WEBHOOK_SECRET")
  },
  "sendgrid": {
    "api_key": @env.secure("SENDGRID_API_KEY"),
    "from_email": @env("SENDGRID_FROM_EMAIL", "noreply@example.com")
  },
  "aws": {
    "access_key": @env.secure("AWS_ACCESS_KEY_ID"),
    "secret_key": @env.secure("AWS_SECRET_ACCESS_KEY"),
    "region": @env("AWS_REGION", "us-east-1")
  }
}
```

## Performance Considerations

### Environment Variable Caching

```tsk
# Cache environment variables for performance
cached_env_vars: @cache("1h", {
  "api_key": @env("API_KEY"),
  "database_url": @env("DATABASE_URL"),
  "debug_mode": @env("DEBUG", "false")
})
```

### Lazy Loading

```javascript
// Load environment variables on demand
class LazyEnvironmentManager {
  constructor() {
    this.cache = new Map();
  }
  
  async get(name, fallback = null) {
    if (!this.cache.has(name)) {
      const value = await this.loadEnvironmentVariable(name, fallback);
      this.cache.set(name, value);
    }
    
    return this.cache.get(name);
  }
  
  async loadEnvironmentVariable(name, fallback) {
    // Implementation for loading environment variable
    return process.env[name] || fallback;
  }
}
```

## Security Notes

- **Secure Variables**: Use `@env.secure()` for sensitive data
- **Validation**: Always validate environment variables
- **Fallbacks**: Provide secure fallback values
- **Logging**: Avoid logging sensitive environment variables

```javascript
// Secure environment variable handling
class SecureEnvironmentManager extends TuskEnvironmentManager {
  constructor() {
    super();
    this.sensitiveVariables = new Set();
  }
  
  async getSecure(name, fallback = null) {
    this.sensitiveVariables.add(name);
    
    const value = await super.getSecure(name, fallback);
    
    // Mask value in logs
    this.maskSensitiveValue(name, value);
    
    return value;
  }
  
  maskSensitiveValue(name, value) {
    if (this.sensitiveVariables.has(name) && value) {
      const masked = '*'.repeat(Math.min(value.length, 8));
      console.log(`Environment variable ${name}: ${masked}`);
    }
  }
  
  validateEnvironment() {
    const requiredVars = ['DATABASE_URL', 'API_KEY', 'JWT_SECRET'];
    const missing = requiredVars.filter(varName => !process.env[varName]);
    
    if (missing.length > 0) {
      throw new Error(`Missing required environment variables: ${missing.join(', ')}`);
    }
  }
}
```

## Best Practices

1. **Validation**: Always validate environment variables
2. **Fallbacks**: Provide meaningful fallback values
3. **Security**: Use secure access for sensitive data
4. **Documentation**: Document all environment variables
5. **Type Safety**: Use appropriate type conversions
6. **Caching**: Cache environment variables for performance

## Next Steps

- Explore [@server variables](./052-at-server-variables-javascript.md) for server configuration
- Master [@global variables](./053-at-global-variables-javascript.md) for global state management
- Learn about [@debug mode](./054-at-debug-mode-javascript.md) for debugging capabilities 