/**
 * TuskLang Enhanced for JavaScript - The Freedom Parser
 * =====================================================
 * "We don't bow to any king" - Support ALL syntax styles
 * 
 * Features:
 * - Multiple grouping: [], {}, <>
 * - $global vs section-local variables
 * - Cross-file communication
 * - Database queries (with adapters)
 * - All @ operators
 * - Maximum flexibility
 */

class TuskLangEnhanced {
  constructor() {
    this.globalVariables = {};
    this.sectionVariables = {};
    this.currentSection = null;
    this.parsedData = {};
    this.crossFileCache = {};
    this.databaseAdapter = null;
    this.cache = new Map();
  }

  /**
   * Set database adapter for @query operations
   */
  setDatabaseAdapter(adapter) {
    this.databaseAdapter = adapter;
  }

  /**
   * Parse TuskLang content
   */
  parse(content) {
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
   * Parse angle bracket object
   */
  parseAngleBracketObject(lines, startPos, key) {
    let position = startPos + 1;
    const obj = {};

    while (position < lines.length) {
      const line = lines[position].trim();

      // End of angle bracket object
      if (line === '<') {
        return { key, value: obj, newPosition: position + 1 };
      }

      // Skip empty lines and comments
      if (!line || line.startsWith('#')) {
        position++;
        continue;
      }

      // Remove optional semicolon
      const cleanLine = line.replace(/;$/, '');

      // Parse nested content
      if (/^([$]?[a-zA-Z_][\w-]*)\s*[:=]\s*(.+)$/.test(cleanLine)) {
        const match = cleanLine.match(/^([$]?[a-zA-Z_][\w-]*)\s*[:=]\s*(.+)$/);
        obj[match[1]] = this.parseValue(match[2].trim());
      } else if (/^([a-zA-Z_]\w*)\s*>$/.test(cleanLine)) {
        const match = cleanLine.match(/^([a-zA-Z_]\w*)\s*>$/);
        const nested = this.parseAngleBracketObject(lines, position, match[1]);
        obj[nested.key] = nested.value;
        position = nested.newPosition - 1;
      } else if (/^([a-zA-Z_]\w*)\s*\{/.test(cleanLine)) {
        const match = cleanLine.match(/^([a-zA-Z_]\w*)\s*\{/);
        const nested = this.parseCurlyBraceObject(lines, position, match[1]);
        obj[nested.key] = nested.value;
        position = nested.newPosition - 1;
      }

      position++;
    }

    return { key, value: obj, newPosition: position };
  }

  /**
   * Parse curly brace object
   */
  parseCurlyBraceObject(lines, startPos, key) {
    let position = startPos;
    const obj = {};
    
    // Check if opening brace is on same line
    const firstLine = lines[position].trim();
    const sameLine = firstLine.includes('{');
    if (sameLine) {
      position++;
    }

    while (position < lines.length) {
      const line = lines[position].trim();

      // End of object
      if (line === '}' || line.startsWith('}')) {
        return { key, value: obj, newPosition: position + 1 };
      }

      // Skip empty lines and comments
      if (!line || line.startsWith('#')) {
        position++;
        continue;
      }

      // Remove optional semicolon
      const cleanLine = line.replace(/;$/, '');

      // Parse nested content
      if (/^([$]?[a-zA-Z_][\w-]*)\s*[:=]\s*(.+)$/.test(cleanLine)) {
        const match = cleanLine.match(/^([$]?[a-zA-Z_][\w-]*)\s*[:=]\s*(.+)$/);
        obj[match[1]] = this.parseValue(match[2].trim());
      }

      position++;
    }

    return { key, value: obj, newPosition: position };
  }

  /**
   * Parse values with all enhancements
   */
  parseValue(value) {
    // Basic types
    if (value === 'true') return true;
    if (value === 'false') return false;
    if (value === 'null') return null;

    // Numbers
    if (/^-?\d+(\.\d+)?$/.test(value)) {
      return value.includes('.') ? parseFloat(value) : parseInt(value);
    }

    // $variable references (global)
    if (/^\$([a-zA-Z_]\w*)$/.test(value)) {
      const match = value.match(/^\$([a-zA-Z_]\w*)$/);
      return this.globalVariables[match[1]] || null;
    }

    // Section-local variable references
    if (/^[a-zA-Z_]\w*$/.test(value) && 
        this.currentSection && 
        this.sectionVariables[this.currentSection]?.[value] !== undefined) {
      return this.sectionVariables[this.currentSection][value];
    }

    // Cross-file references: @file.tsk.get('key')
    if (/^@([\w-]+)\.tsk\.get\(["']([^"']+)["']\)$/.test(value)) {
      const match = value.match(/^@([\w-]+)\.tsk\.get\(["']([^"']+)["']\)$/);
      return this.crossFileGet(match[1], match[2]);
    }

    // Cross-file set: @file.tsk.set('key', value)
    if (/^@([\w-]+)\.tsk\.set\(["']([^"']+)["'],\s*(.+)\)$/.test(value)) {
      const match = value.match(/^@([\w-]+)\.tsk\.set\(["']([^"']+)["'],\s*(.+)\)$/);
      return this.crossFileSet(match[1], match[2], this.parseValue(match[3]));
    }

    // @date function
    if (/^@date\(["']([^"']+)["']\)$/.test(value)) {
      const match = value.match(/^@date\(["']([^"']+)["']\)$/);
      return this.formatDate(match[1]);
    }

    // Ranges: 8888-9999
    if (/^(\d+)-(\d+)$/.test(value)) {
      const match = value.match(/^(\d+)-(\d+)$/);
      return {
        min: parseInt(match[1]),
        max: parseInt(match[2]),
        type: 'range'
      };
    }

    // @ operators
    if (/^@(\w+)\((.+)\)$/.test(value)) {
      const match = value.match(/^@(\w+)\((.+)\)$/);
      return this.executeOperator(match[1], match[2]);
    }

    // Arrays
    if (value.startsWith('[') && value.endsWith(']')) {
      try {
        return JSON.parse(value);
      } catch {
        // Simple array parsing
        const content = value.slice(1, -1);
        return content.split(',').map(item => this.parseValue(item.trim()));
      }
    }

    // Inline objects
    if (value.startsWith('{') && value.endsWith('}')) {
      try {
        return JSON.parse(value);
      } catch {
        // Simple object parsing
        const obj = {};
        const content = value.slice(1, -1);
        const pairs = content.split(',');
        pairs.forEach(pair => {
          const [k, v] = pair.split(':').map(s => s.trim());
          if (k && v) {
            obj[k.replace(/["']/g, '')] = this.parseValue(v);
          }
        });
        return obj;
      }
    }

    // String values (remove quotes)
    if ((value.startsWith('"') && value.endsWith('"')) ||
        (value.startsWith("'") && value.endsWith("'"))) {
      return value.slice(1, -1);
    }

    // String concatenation
    if (value.includes(' + ')) {
      const parts = value.split(' + ').map(part => {
        const parsed = this.parseValue(part.trim());
        return parsed !== null ? String(parsed) : '';
      });
      return parts.join('');
    }

    // Conditional/ternary
    if (value.includes(' ? ') && value.includes(' : ')) {
      const [condition, rest] = value.split(' ? ');
      const [trueVal, falseVal] = rest.split(' : ');
      const condResult = this.evaluateCondition(condition.trim());
      return condResult ? 
        this.parseValue(trueVal.trim()) : 
        this.parseValue(falseVal.trim());
    }

    // Environment variables
    if (/^@env\(["']([^"']+)["'](?:,\s*(.+))?\)$/.test(value)) {
      const match = value.match(/^@env\(["']([^"']+)["'](?:,\s*(.+))?\)$/);
      const envVar = match[1];
      const defaultVal = match[2] ? this.parseValue(match[2]) : null;
      return process.env[envVar] || defaultVal;
    }

    return value;
  }

  /**
   * Execute @ operators
   */
  async executeOperator(operator, params) {
    switch (operator) {
      case 'query':
        return this.executeQuery(params);
      
      case 'cache':
        return this.executeCacheOperator(params);
      
      case 'learn':
        return this.executeLearnOperator(params);
      
      case 'optimize':
        return this.executeOptimizeOperator(params);
      
      case 'metrics':
        return this.executeMetricsOperator(params);
      
      case 'feature':
        return this.executeFeatureOperator(params);
      
      case 'env':
        // Already handled in parseValue
        return `@${operator}(${params})`;
      
      default:
        // Unknown operator
        return `@${operator}(${params})`;
    }
  }

  /**
   * Execute database query
   */
  async executeQuery(params) {
    if (!this.databaseAdapter) {
      console.warn('No database adapter configured for @query');
      return [];
    }

    // Parse query parameters
    const match = params.match(/^["']([^"']+)["'](?:,\s*(.+))?$/);
    if (!match) return [];

    const sql = match[1];
    const args = match[2] ? this.parseQueryArgs(match[2]) : [];

    try {
      return await this.databaseAdapter.query(sql, args);
    } catch (error) {
      console.error('Query error:', error);
      return [];
    }
  }

  /**
   * Cache operator
   */
  executeCacheOperator(params) {
    const match = params.match(/^["']([^"']+)["'],\s*(.+)$/);
    if (!match) return null;

    const ttl = match[1];
    const value = this.parseValue(match[2]);
    const key = JSON.stringify({ operator: 'cache', params, value });

    // Check cache
    const cached = this.cache.get(key);
    if (cached && cached.expires > Date.now()) {
      return cached.value;
    }

    // Store in cache
    const ttlMs = this.parseTTL(ttl);
    this.cache.set(key, {
      value,
      expires: Date.now() + ttlMs
    });

    return value;
  }

  /**
   * Learn operator (placeholder)
   */
  executeLearnOperator(params) {
    const match = params.match(/^["']([^"']+)["'],\s*(.+)$/);
    if (!match) return null;

    const key = match[1];
    const defaultValue = this.parseValue(match[2]);
    
    // In a real implementation, this would use ML
    return defaultValue;
  }

  /**
   * Optimize operator (placeholder)
   */
  executeOptimizeOperator(params) {
    const match = params.match(/^["']([^"']+)["'],\s*(.+)$/);
    if (!match) return null;

    const param = match[1];
    const initialValue = this.parseValue(match[2]);
    
    // In a real implementation, this would auto-tune
    return initialValue;
  }

  /**
   * Metrics operator (placeholder)
   */
  executeMetricsOperator(params) {
    const match = params.match(/^["']([^"']+)["'](?:,\s*(.+))?$/);
    if (!match) return 0;

    const metric = match[1];
    const value = match[2] ? this.parseValue(match[2]) : null;
    
    // In a real implementation, this would track metrics
    return value || 0;
  }

  /**
   * Feature flag operator
   */
  executeFeatureOperator(params) {
    const feature = params.replace(/["']/g, '');
    // In a real implementation, check feature flags
    return false;
  }

  /**
   * Cross-file get
   */
  crossFileGet(filename, key) {
    const cacheKey = `${filename}:${key}`;
    
    // Check cache
    if (this.crossFileCache[cacheKey]) {
      return this.crossFileCache[cacheKey];
    }

    // In a real implementation, load and parse file
    console.warn(`Cross-file reference not implemented: @${filename}.tsk.get('${key}')`);
    return null;
  }

  /**
   * Cross-file set
   */
  crossFileSet(filename, key, value) {
    const cacheKey = `${filename}:${key}`;
    this.crossFileCache[cacheKey] = value;
    
    // In a real implementation, update the file
    console.warn(`Cross-file update not implemented: @${filename}.tsk.set('${key}', ${value})`);
    return value;
  }

  /**
   * Format date
   */
  formatDate(format) {
    const date = new Date();
    
    // Simple format replacements
    const replacements = {
      'Y': date.getFullYear(),
      'm': String(date.getMonth() + 1).padStart(2, '0'),
      'd': String(date.getDate()).padStart(2, '0'),
      'H': String(date.getHours()).padStart(2, '0'),
      'i': String(date.getMinutes()).padStart(2, '0'),
      's': String(date.getSeconds()).padStart(2, '0'),
      'c': date.toISOString()
    };

    let result = format;
    for (const [key, value] of Object.entries(replacements)) {
      result = result.replace(key, value);
    }
    
    return result;
  }

  /**
   * Parse TTL string
   */
  parseTTL(ttl) {
    const match = ttl.match(/^(\d+)([smhd])$/);
    if (!match) return 300000; // Default 5 minutes

    const value = parseInt(match[1]);
    const unit = match[2];

    switch (unit) {
      case 's': return value * 1000;
      case 'm': return value * 60 * 1000;
      case 'h': return value * 60 * 60 * 1000;
      case 'd': return value * 24 * 60 * 60 * 1000;
      default: return 300000;
    }
  }

  /**
   * Evaluate conditions
   */
  evaluateCondition(condition) {
    // Simple equality check
    if (condition.includes(' == ')) {
      const [left, right] = condition.split(' == ').map(s => this.parseValue(s.trim()));
      return left === right;
    }
    
    if (condition.includes(' != ')) {
      const [left, right] = condition.split(' != ').map(s => this.parseValue(s.trim()));
      return left !== right;
    }

    if (condition.includes(' > ')) {
      const [left, right] = condition.split(' > ').map(s => this.parseValue(s.trim()));
      return left > right;
    }

    if (condition.includes(' < ')) {
      const [left, right] = condition.split(' < ').map(s => this.parseValue(s.trim()));
      return left < right;
    }

    // Default: evaluate as boolean
    const value = this.parseValue(condition);
    return !!value;
  }

  /**
   * Parse query arguments
   */
  parseQueryArgs(argsStr) {
    const args = [];
    const parts = argsStr.split(',');
    
    for (const part of parts) {
      args.push(this.parseValue(part.trim()));
    }
    
    return args;
  }

  /**
   * Resolve all references
   */
  resolveReferences(data) {
    // Ensure we return the data object, not undefined
    if (!data || typeof data !== 'object') {
      return {};
    }
    
    // In a full implementation, this would resolve @ references
    // For now, just return the parsed data
    return data;
  }
}

// Export for Node.js
if (typeof module !== 'undefined' && module.exports) {
  module.exports = TuskLangEnhanced;
}

// Export for browsers
if (typeof window !== 'undefined') {
  window.TuskLangEnhanced = TuskLangEnhanced;
}