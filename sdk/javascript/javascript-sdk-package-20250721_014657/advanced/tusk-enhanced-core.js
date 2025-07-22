/**
 * TuskLang Enhanced Core
 * Integrates Error Handling, Caching, and Plugin System
 * Goals 1.1, 1.2, and 1.3 implementation
 */

const { ErrorHandler, TuskLangError, ValidationError, ParseError, OperatorError } = require('./error-handler');
const { CacheManager } = require('./cache-manager');
const { PluginManager, BuiltInPlugins } = require('./plugin-system');

class TuskLangEnhancedCore {
  constructor(options = {}) {
    // Initialize core components
    this.errorHandler = new ErrorHandler();
    this.cacheManager = new CacheManager(options.cache || {});
    this.pluginManager = new PluginManager(options.plugins || {});
    
    // Core state
    this.globalVariables = {};
    this.sectionVariables = {};
    this.currentSection = null;
    this.parsedData = {};
    this.crossFileCache = {};
    this.databaseAdapter = null;
    
    // Integration settings
    this.enableValidation = options.enableValidation !== false;
    this.enableCaching = options.enableCaching !== false;
    this.enablePlugins = options.enablePlugins !== false;
    
    // Initialize plugins if enabled
    if (this.enablePlugins) {
      this.initializeBuiltInPlugins();
    }
  }

  /**
   * Initialize built-in plugins
   */
  initializeBuiltInPlugins() {
    const loggerPlugin = BuiltInPlugins.createLoggerPlugin(this.pluginManager);
    const metricsPlugin = BuiltInPlugins.createMetricsPlugin(this.pluginManager);
    const validationPlugin = BuiltInPlugins.createValidationPlugin(this.pluginManager);
    
    this.pluginManager.registerPlugin(loggerPlugin);
    this.pluginManager.registerPlugin(metricsPlugin);
    this.pluginManager.registerPlugin(validationPlugin);
    
    // Enable all built-in plugins
    this.pluginManager.enablePlugin('logger');
    this.pluginManager.enablePlugin('metrics');
    this.pluginManager.enablePlugin('validation');
  }

  /**
   * Enhanced parse method with error handling, caching, and plugins
   */
  async parse(content, options = {}) {
    const context = {
      filename: options.filename || 'unknown',
      timestamp: new Date().toISOString(),
      options
    };

    try {
      // Execute pre-parse hooks
      if (this.enablePlugins) {
        content = await this.pluginManager.executeHooks('parse.before', content, context);
      }

      // Check cache first
      if (this.enableCaching) {
        const cacheKey = this.cacheManager.generateKey('parse', content, options);
        const cachedResult = this.cacheManager.get(cacheKey);
        if (cachedResult) {
          return cachedResult;
        }
      }

      // Validate content if enabled
      if (this.enableValidation) {
        const validationErrors = this.errorHandler.validateContent(content);
        if (validationErrors.length > 0) {
          throw new ParseError(
            `Content validation failed with ${validationErrors.length} errors`,
            0,
            0,
            content.substring(0, 100) + '...'
          );
        }
      }

      // Execute parse start hooks
      if (this.enablePlugins) {
        await this.pluginManager.executeHooks('parse.start', content, context);
      }

      // Perform actual parsing
      const result = this.performParsing(content);

      // Execute post-parse hooks
      if (this.enablePlugins) {
        await this.pluginManager.executeHooks('parse.after', result, context);
      }

      // Cache the result
      if (this.enableCaching) {
        const cacheKey = this.cacheManager.generateKey('parse', content, options);
        this.cacheManager.set(cacheKey, result, options.cacheTTL || 300000); // 5 minutes default
      }

      // Execute parse end hooks
      if (this.enablePlugins) {
        await this.pluginManager.executeHooks('parse.end', result, context);
      }

      return result;

    } catch (error) {
      // Log error
      this.errorHandler.logError(error);
      
      // Execute error hooks
      if (this.enablePlugins) {
        await this.pluginManager.executeHooks('error', error, context);
      }
      
      throw error;
    }
  }

  /**
   * Core parsing logic
   */
  performParsing(content) {
    const lines = content.split('\n');
    const result = {};
    let position = 0;

    while (position < lines.length) {
      const line = lines[position].trim();
      
      // Skip empty lines and comments
      if (!line || line.startsWith('#') || line.startsWith('//')) {
        position++;
        continue;
      }

      // Remove optional semicolon
      const cleanLine = line.replace(/;$/, '');

      // Check for section declarations []
      if (/^\[([a-zA-Z_]\w*)\]$/.test(cleanLine)) {
        const match = cleanLine.match(/^\[([a-zA-Z_]\w*)\]$/);
        this.currentSection = match[1];
        result[this.currentSection] = {};
        this.sectionVariables[this.currentSection] = {};
        position++;
        continue;
      }

      // Check for angle bracket objects >
      if (/^([a-zA-Z_]\w*)\s*>$/.test(cleanLine)) {
        const match = cleanLine.match(/^([a-zA-Z_]\w*)\s*>$/);
        const obj = this.parseAngleBracketObject(lines, position, match[1]);
        if (this.currentSection) {
          result[this.currentSection][obj.key] = obj.value;
        } else {
          result[obj.key] = obj.value;
        }
        position = obj.newPosition;
        continue;
      }

      // Check for curly brace objects {
      if (/^([a-zA-Z_]\w*)\s*\{/.test(cleanLine)) {
        const match = cleanLine.match(/^([a-zA-Z_]\w*)\s*\{/);
        const obj = this.parseCurlyBraceObject(lines, position, match[1]);
        if (this.currentSection) {
          result[this.currentSection][obj.key] = obj.value;
        } else {
          result[obj.key] = obj.value;
        }
        position = obj.newPosition;
        continue;
      }

      // Parse key-value pairs
      if (/^([$]?[a-zA-Z_][\w-]*)\s*[:=]\s*(.+)$/.test(cleanLine)) {
        const match = cleanLine.match(/^([$]?[a-zA-Z_][\w-]*)\s*[:=]\s*(.+)$/);
        const key = match[1];
        const value = this.parseValue(match[2].trim());

        // Store in result
        if (this.currentSection) {
          result[this.currentSection][key] = value;
          // Store section-local variable if not global
          if (!key.startsWith('$')) {
            this.sectionVariables[this.currentSection][key] = value;
          }
        } else {
          result[key] = value;
        }

        // Store global variables
        if (key.startsWith('$')) {
          this.globalVariables[key.substring(1)] = value;
        }

        this.parsedData[key] = value;
      }

      position++;
    }

    return this.resolveReferences(result);
  }

  /**
   * Enhanced operator execution with error handling and caching
   */
  async executeOperator(operator, params, context = {}) {
    const operatorContext = {
      ...context,
      operator,
      timestamp: new Date().toISOString()
    };

    try {
      // Execute pre-operator hooks
      if (this.enablePlugins) {
        params = await this.pluginManager.executeHooks('operator.before', params, operatorContext);
      }

      // Check cache for operator results
      if (this.enableCaching && params.cacheable !== false) {
        const cacheKey = this.cacheManager.generateKey('operator', operator, params);
        const cachedResult = this.cacheManager.get(cacheKey);
        if (cachedResult) {
          return cachedResult;
        }
      }

      // Execute operator start hooks
      if (this.enablePlugins) {
        await this.pluginManager.executeHooks('operator.execute', params, operatorContext);
      }

      // Perform actual operator execution
      const result = await this.performOperatorExecution(operator, params);

      // Cache the result
      if (this.enableCaching && params.cacheable !== false) {
        const cacheKey = this.cacheManager.generateKey('operator', operator, params);
        this.cacheManager.set(cacheKey, result, params.cacheTTL || 60000); // 1 minute default
      }

      // Execute post-operator hooks
      if (this.enablePlugins) {
        await this.pluginManager.executeHooks('operator.after', result, operatorContext);
      }

      return result;

    } catch (error) {
      // Create operator-specific error
      const operatorError = new OperatorError(
        `Operator ${operator} execution failed: ${error.message}`,
        operator,
        params
      );
      
      // Log error
      this.errorHandler.logError(operatorError);
      
      // Execute error hooks
      if (this.enablePlugins) {
        await this.pluginManager.executeHooks('error', operatorError, operatorContext);
      }
      
      throw operatorError;
    }
  }

  /**
   * Core operator execution logic
   */
  async performOperatorExecution(operator, params) {
    // This would contain the actual operator implementation
    // For now, return a placeholder
    return {
      operator,
      params,
      result: `Executed ${operator} with params: ${JSON.stringify(params)}`,
      timestamp: new Date().toISOString()
    };
  }

  /**
   * Get comprehensive system status
   */
  getSystemStatus() {
    return {
      errorHandler: {
        hasErrors: this.errorHandler.hasErrors(),
        errorCount: this.errorHandler.getErrors().length,
        errorReport: this.errorHandler.createErrorReport()
      },
      cacheManager: {
        stats: this.cacheManager.getStats(),
        size: this.cacheManager.size(),
        memoryUsage: this.cacheManager.getMemoryUsage()
      },
      pluginManager: {
        stats: this.pluginManager.getPluginStats(),
        enabledPlugins: this.pluginManager.getEnabledPlugins().map(p => p.name),
        totalHooks: this.pluginManager.hooks.size
      },
      core: {
        globalVariables: Object.keys(this.globalVariables).length,
        sections: Object.keys(this.sectionVariables).length,
        parsedData: Object.keys(this.parsedData).length,
        currentSection: this.currentSection
      },
      settings: {
        enableValidation: this.enableValidation,
        enableCaching: this.enableCaching,
        enablePlugins: this.enablePlugins
      }
    };
  }

  /**
   * Cleanup and maintenance
   */
  async cleanup() {
    // Clean expired cache entries
    if (this.enableCaching) {
      const cleanedCount = this.cacheManager.cleanup();
      console.log(`Cleaned ${cleanedCount} expired cache entries`);
    }

    // Clear error log if too large
    const errors = this.errorHandler.getErrors();
    if (errors.length > 1000) {
      this.errorHandler.clearErrors();
      console.log('Cleared error log due to size limit');
    }

    return {
      cacheCleaned: this.enableCaching ? this.cacheManager.cleanup() : 0,
      errorsCleared: errors.length > 1000 ? errors.length : 0
    };
  }

  /**
   * Export system data for debugging
   */
  exportSystemData() {
    return {
      globalVariables: this.globalVariables,
      sectionVariables: this.sectionVariables,
      parsedData: this.parsedData,
      errorLog: this.errorHandler.getErrors(),
      cacheStats: this.cacheManager.getStats(),
      pluginStats: this.pluginManager.getPluginStats(),
      timestamp: new Date().toISOString()
    };
  }

  // Helper methods (simplified versions from the original tsk-enhanced.js)
  parseAngleBracketObject(lines, startPos, key) {
    // Simplified implementation
    return { key, value: {}, newPosition: startPos + 1 };
  }

  parseCurlyBraceObject(lines, startPos, key) {
    // Simplified implementation
    return { key, value: {}, newPosition: startPos + 1 };
  }

  parseValue(value) {
    // Simplified implementation
    return value;
  }

  resolveReferences(data) {
    // Simplified implementation
    return data;
  }
}

module.exports = {
  TuskLangEnhancedCore,
  ErrorHandler,
  CacheManager,
  PluginManager,
  BuiltInPlugins
}; 