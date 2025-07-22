/**
 * TuskLang Enhanced Error Handler
 * Provides comprehensive error handling and validation for the JavaScript SDK
 */

class TuskLangError extends Error {
  constructor(message, code, details = {}) {
    super(message);
    this.name = 'TuskLangError';
    this.code = code;
    this.details = details;
    this.timestamp = new Date().toISOString();
  }
}

class ValidationError extends TuskLangError {
  constructor(message, field, value) {
    super(message, 'VALIDATION_ERROR', { field, value });
    this.name = 'ValidationError';
  }
}

class ParseError extends TuskLangError {
  constructor(message, line, column, content) {
    super(message, 'PARSE_ERROR', { line, column, content });
    this.name = 'ParseError';
  }
}

class OperatorError extends TuskLangError {
  constructor(message, operator, params) {
    super(message, 'OPERATOR_ERROR', { operator, params });
    this.name = 'OperatorError';
  }
}

class ErrorHandler {
  constructor() {
    this.errorLog = [];
    this.validationRules = new Map();
    this.setupDefaultValidationRules();
  }

  /**
   * Setup default validation rules
   */
  setupDefaultValidationRules() {
    // Variable name validation
    this.validationRules.set('variable_name', {
      pattern: /^[$]?[a-zA-Z_][\w-]*$/,
      message: 'Variable name must start with letter or underscore and contain only alphanumeric characters, underscores, and hyphens'
    });

    // Section name validation
    this.validationRules.set('section_name', {
      pattern: /^[a-zA-Z_]\w*$/,
      message: 'Section name must start with letter or underscore and contain only alphanumeric characters and underscores'
    });

    // Value validation
    this.validationRules.set('value', {
      validator: (value) => value !== undefined && value !== null,
      message: 'Value cannot be undefined or null'
    });
  }

  /**
   * Validate a value against a rule
   */
  validate(value, ruleName, context = {}) {
    const rule = this.validationRules.get(ruleName);
    if (!rule) {
      throw new ValidationError(`Unknown validation rule: ${ruleName}`);
    }

    let isValid = false;
    if (rule.pattern) {
      isValid = rule.pattern.test(value);
    } else if (rule.validator) {
      isValid = rule.validator(value, context);
    }

    if (!isValid) {
      const error = new ValidationError(rule.message, ruleName, value);
      this.logError(error);
      return false;
    }

    return true;
  }

  /**
   * Log an error
   */
  logError(error) {
    this.errorLog.push({
      error,
      stack: error.stack,
      timestamp: new Date().toISOString()
    });

    // In development mode, also console.error
    if (process.env.NODE_ENV === 'development') {
      console.error(`[TuskLang Error] ${error.name}: ${error.message}`, error.details);
    }
  }

  /**
   * Get all logged errors
   */
  getErrors() {
    return this.errorLog;
  }

  /**
   * Clear error log
   */
  clearErrors() {
    this.errorLog = [];
  }

  /**
   * Check if there are any errors
   */
  hasErrors() {
    return this.errorLog.length > 0;
  }

  /**
   * Get errors by type
   */
  getErrorsByType(errorType) {
    return this.errorLog.filter(log => log.error.name === errorType);
  }

  /**
   * Add custom validation rule
   */
  addValidationRule(name, rule) {
    this.validationRules.set(name, rule);
  }

  /**
   * Validate TuskLang content before parsing
   */
  validateContent(content) {
    const errors = [];
    const lines = content.split('\n');

    lines.forEach((line, index) => {
      const lineNum = index + 1;
      const trimmedLine = line.trim();

      // Skip empty lines and comments
      if (!trimmedLine || trimmedLine.startsWith('#') || trimmedLine.startsWith('//')) {
        return;
      }

      // Check for balanced brackets
      if (!this.checkBalancedBrackets(trimmedLine)) {
        errors.push(new ParseError(
          'Unbalanced brackets detected',
          lineNum,
          0,
          trimmedLine
        ));
      }

      // Check for valid section declarations
      if (/^\[.*\]$/.test(trimmedLine)) {
        const sectionName = trimmedLine.slice(1, -1);
        if (!this.validate(sectionName, 'section_name')) {
          errors.push(new ParseError(
            'Invalid section name',
            lineNum,
            1,
            trimmedLine
          ));
        }
      }

      // Check for valid key-value pairs
      if (/^([$]?[a-zA-Z_][\w-]*)\s*[:=]\s*(.+)$/.test(trimmedLine)) {
        const match = trimmedLine.match(/^([$]?[a-zA-Z_][\w-]*)\s*[:=]\s*(.+)$/);
        const key = match[1];
        
        if (!this.validate(key, 'variable_name')) {
          errors.push(new ParseError(
            'Invalid variable name',
            lineNum,
            0,
            trimmedLine
          ));
        }
      }
    });

    return errors;
  }

  /**
   * Check if brackets are balanced
   */
  checkBalancedBrackets(line) {
    const stack = [];
    const brackets = { '[': ']', '{': '}', '<': '>' };

    for (const char of line) {
      if (brackets[char]) {
        stack.push(char);
      } else if (Object.values(brackets).includes(char)) {
        const lastOpen = stack.pop();
        if (brackets[lastOpen] !== char) {
          return false;
        }
      }
    }

    return stack.length === 0;
  }

  /**
   * Create a formatted error report
   */
  createErrorReport() {
    if (!this.hasErrors()) {
      return { status: 'success', message: 'No errors found' };
    }

    const report = {
      status: 'error',
      totalErrors: this.errorLog.length,
      errors: this.errorLog.map(log => ({
        type: log.error.name,
        message: log.error.message,
        code: log.error.code,
        details: log.error.details,
        timestamp: log.timestamp
      })),
      summary: {
        validation: this.getErrorsByType('ValidationError').length,
        parse: this.getErrorsByType('ParseError').length,
        operator: this.getErrorsByType('OperatorError').length,
        general: this.errorLog.length - this.getErrorsByType('ValidationError').length - 
                this.getErrorsByType('ParseError').length - this.getErrorsByType('OperatorError').length
      }
    };

    return report;
  }
}

module.exports = {
  TuskLangError,
  ValidationError,
  ParseError,
  OperatorError,
  ErrorHandler
}; 