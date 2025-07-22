/**
 * TSK (TuskLang Configuration) Parser and Generator - Pure JavaScript
 * Handles the TOML-like format used by Flexchain for configuration and metadata
 * 
 * No TypeScript, just pure JavaScript goodness!
 */

class TSKParser {
  /**
   * Parse TSK content into JavaScript object
   * @param {string} content - TSK formatted string
   * @returns {Object} Parsed data
   */
  static parse(content) {
    const lines = content.split('\n');
    const result = {};
    let currentSection = null;
    let inMultilineString = false;
    let multilineKey = null;
    let multilineContent = [];

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const trimmedLine = line.trim();

      // Handle multiline strings
      if (inMultilineString) {
        if (trimmedLine === '"""') {
          if (currentSection && multilineKey) {
            result[currentSection][multilineKey] = multilineContent.join('\n');
          }
          inMultilineString = false;
          multilineKey = null;
          multilineContent = [];
          continue;
        }
        multilineContent.push(line);
        continue;
      }

      // Skip empty lines and comments
      if (!trimmedLine || trimmedLine.startsWith('#')) {
        continue;
      }

      // Section header
      const sectionMatch = trimmedLine.match(/^\[(.+)\]$/);
      if (sectionMatch) {
        currentSection = sectionMatch[1];
        result[currentSection] = {};
        continue;
      }

      // Key-value pair
      if (currentSection && trimmedLine.includes('=')) {
        const separatorIndex = trimmedLine.indexOf('=');
        const key = trimmedLine.substring(0, separatorIndex).trim();
        const valueStr = trimmedLine.substring(separatorIndex + 1).trim();

        // Check for multiline string start
        if (valueStr === '"""') {
          inMultilineString = true;
          multilineKey = key;
          continue;
        }

        const value = this.parseValue(valueStr);
        result[currentSection][key] = value;
      }
    }

    return result;
  }

  /**
   * Parse a TSK value string into appropriate JavaScript type
   * @param {string} valueStr - Value string to parse
   * @returns {*} Parsed value
   */
  static parseValue(valueStr) {
    // Null
    if (valueStr === 'null') return null;

    // Boolean
    if (valueStr === 'true') return true;
    if (valueStr === 'false') return false;

    // Number
    if (/^-?\d+$/.test(valueStr)) return parseInt(valueStr, 10);
    if (/^-?\d+\.\d+$/.test(valueStr)) return parseFloat(valueStr);

    // String
    if (valueStr.startsWith('"') && valueStr.endsWith('"')) {
      return valueStr.slice(1, -1).replace(/\\"/g, '"').replace(/\\\\/g, '\\');
    }

    // Array
    if (valueStr.startsWith('[') && valueStr.endsWith(']')) {
      const arrayContent = valueStr.slice(1, -1).trim();
      if (!arrayContent) return [];
      
      const items = this.splitArrayItems(arrayContent);
      return items.map(item => this.parseValue(item.trim()));
    }

    // Object/Map
    if (valueStr.startsWith('{') && valueStr.endsWith('}')) {
      const objContent = valueStr.slice(1, -1).trim();
      if (!objContent) return {};
      
      const pairs = this.splitObjectPairs(objContent);
      const obj = {};
      
      pairs.forEach(pair => {
        const eqIndex = pair.indexOf('=');
        if (eqIndex > -1) {
          const key = pair.substring(0, eqIndex).trim();
          const value = pair.substring(eqIndex + 1).trim();
          // Remove quotes from key if present
          const cleanKey = key.startsWith('"') && key.endsWith('"') 
            ? key.slice(1, -1) 
            : key;
          obj[cleanKey] = this.parseValue(value);
        }
      });
      
      return obj;
    }

    // @ operators (FUJSEN-style)
    // @Query operator: @Query("Users").where("active", true).find()
    if (valueStr.startsWith('@Query(')) {
      return { __operator: 'Query', expression: valueStr };
    }
    
    // @q shorthand: @q("Users").where("active", true).find()
    if (valueStr.startsWith('@q(')) {
      return { __operator: 'Query', expression: valueStr.replace('@q(', '@Query(') };
    }
    
    // @cache operator: @cache("ttl", value)
    if (valueStr.startsWith('@cache(')) {
      return { __operator: 'cache', expression: valueStr };
    }
    
    // @metrics operator: @metrics("name", value)
    if (valueStr.startsWith('@metrics(')) {
      return { __operator: 'metrics', expression: valueStr };
    }
    
    // @if operator: @if(condition, true_val, false_val)
    if (valueStr.startsWith('@if(')) {
      return { __operator: 'if', expression: valueStr };
    }
    
    // @date operator: @date("format")
    if (valueStr.startsWith('@date(')) {
      return { __operator: 'date', expression: valueStr };
    }
    
    // @optimize operator: @optimize("param", initial)
    if (valueStr.startsWith('@optimize(')) {
      return { __operator: 'optimize', expression: valueStr };
    }
    
    // @learn operator: @learn("key", default)
    if (valueStr.startsWith('@learn(')) {
      return { __operator: 'learn', expression: valueStr };
    }
    
    // @feature operator: @feature("name")
    if (valueStr.startsWith('@feature(')) {
      return { __operator: 'feature', expression: valueStr };
    }
    
    // @json operator: @json(data)
    if (valueStr.startsWith('@json(')) {
      return { __operator: 'json', expression: valueStr };
    }
    
    // @request operator: @request or @request.method
    if (valueStr === '@request' || valueStr.startsWith('@request.')) {
      return { __operator: 'request', expression: valueStr };
    }
    
    // env() function: env("VAR_NAME", "default")
    if (valueStr.startsWith('env(')) {
      return { __function: 'env', expression: valueStr };
    }
    
    // php() function: php(expression)
    if (valueStr.startsWith('php(')) {
      return { __function: 'php', expression: valueStr };
    }
    
    // file() function: file("path")
    if (valueStr.startsWith('file(')) {
      return { __function: 'file', expression: valueStr };
    }
    
    // query() function: query("Class").find()
    if (valueStr.startsWith('query(')) {
      return { __function: 'query', expression: valueStr };
    }
    
    // Return as string if no other type matches
    return valueStr;
  }

  /**
   * Split array items considering nested structures
   * @param {string} content - Array content
   * @returns {string[]} Array items
   */
  static splitArrayItems(content) {
    const items = [];
    let current = '';
    let depth = 0;
    let inString = false;
    
    for (let i = 0; i < content.length; i++) {
      const char = content[i];
      
      if (char === '"' && (i === 0 || content[i - 1] !== '\\')) {
        inString = !inString;
      }
      
      if (!inString) {
        if (char === '[' || char === '{') depth++;
        if (char === ']' || char === '}') depth--;
        
        if (char === ',' && depth === 0) {
          items.push(current.trim());
          current = '';
          continue;
        }
      }
      
      current += char;
    }
    
    if (current.trim()) {
      items.push(current.trim());
    }
    
    return items;
  }

  /**
   * Split object pairs considering nested structures
   * @param {string} content - Object content
   * @returns {string[]} Object pairs
   */
  static splitObjectPairs(content) {
    const pairs = [];
    let current = '';
    let depth = 0;
    let inString = false;
    
    for (let i = 0; i < content.length; i++) {
      const char = content[i];
      
      if (char === '"' && (i === 0 || content[i - 1] !== '\\')) {
        inString = !inString;
      }
      
      if (!inString) {
        if (char === '[' || char === '{') depth++;
        if (char === ']' || char === '}') depth--;
        
        if (char === ',' && depth === 0) {
          pairs.push(current.trim());
          current = '';
          continue;
        }
      }
      
      current += char;
    }
    
    if (current.trim()) {
      pairs.push(current.trim());
    }
    
    return pairs;
  }

  /**
   * Generate TSK content from JavaScript object
   * @param {Object} data - Data to stringify
   * @returns {string} TSK formatted string
   */
  static stringify(data) {
    let content = '# Generated by Flexchain TSK.js\n';
    content += `# ${new Date().toISOString()}\n\n`;

    for (const [section, values] of Object.entries(data)) {
      content += `[${section}]\n`;
      
      if (typeof values === 'object' && values !== null) {
        for (const [key, value] of Object.entries(values)) {
          content += `${key} = ${this.formatValue(value)}\n`;
        }
      }
      
      content += '\n';
    }

    return content.trim();
  }

  /**
   * Format a JavaScript value for TSK
   * @param {*} value - Value to format
   * @returns {string} Formatted value
   */
  static formatValue(value) {
    // Null
    if (value === null) return 'null';

    // Boolean
    if (typeof value === 'boolean') return value ? 'true' : 'false';

    // Number
    if (typeof value === 'number') return value.toString();

    // String
    if (typeof value === 'string') {
      // Multiline string
      if (value.includes('\n')) {
        return '"""\n' + value + '\n"""';
      }
      // Regular string
      return '"' + value.replace(/\\/g, '\\\\').replace(/"/g, '\\"') + '"';
    }

    // Array
    if (Array.isArray(value)) {
      const items = value.map(item => this.formatValue(item));
      return '[ ' + items.join(', ') + ' ]';
    }

    // Object
    if (typeof value === 'object') {
      const pairs = [];
      for (const [k, v] of Object.entries(value)) {
        pairs.push(`"${k}" = ${this.formatValue(v)}`);
      }
      return '{ ' + pairs.join(', ') + ' }';
    }

    return '""';
  }
}

/**
 * Shell format storage for binary data
 */
class ShellStorage {
  static MAGIC = 'FLEX';
  static VERSION = 1;
  
  /**
   * Pack data into Shell binary format
   * @param {Object} data - Data to pack
   * @returns {Buffer|Uint8Array} Binary shell data
   */
  static pack(data) {
    // In browser, use Uint8Array; in Node, use Buffer
    const isNode = typeof process !== 'undefined' && process.versions && process.versions.node;
    
    // Create header
    const encoder = new TextEncoder();
    const magic = encoder.encode(this.MAGIC);
    const idBytes = encoder.encode(data.id);
    
    // Compress data if it's not already compressed
    let compressedData = data.data;
    if (typeof data.data === 'string') {
      // Simple compression using pako or native
      if (typeof window !== 'undefined' && window.pako) {
        compressedData = window.pako.gzip(data.data);
      } else if (isNode) {
        const zlib = require('zlib');
        compressedData = zlib.gzipSync(data.data);
      } else {
        // Fallback: just encode as UTF-8
        compressedData = encoder.encode(data.data);
      }
    }
    
    // Calculate sizes
    const idLength = idBytes.length;
    const dataLength = compressedData.length;
    
    // Allocate buffer
    const totalSize = 4 + 1 + 4 + idLength + 4 + dataLength;
    const buffer = new Uint8Array(totalSize);
    let offset = 0;
    
    // Write magic bytes
    buffer.set(magic, offset);
    offset += 4;
    
    // Write version
    buffer[offset] = this.VERSION;
    offset += 1;
    
    // Write ID length and ID
    new DataView(buffer.buffer).setUint32(offset, idLength);
    offset += 4;
    buffer.set(idBytes, offset);
    offset += idLength;
    
    // Write data length and data
    new DataView(buffer.buffer).setUint32(offset, dataLength);
    offset += 4;
    buffer.set(compressedData, offset);
    
    return isNode ? Buffer.from(buffer) : buffer;
  }
  
  /**
   * Unpack Shell binary format
   * @param {Buffer|Uint8Array} shellData - Binary shell data
   * @returns {Object} Unpacked data
   */
  static unpack(shellData) {
    const decoder = new TextDecoder();
    // Convert to Uint8Array if needed
    let data;
    if (shellData instanceof Uint8Array) {
      data = shellData;
    } else if (Buffer.isBuffer(shellData)) {
      data = new Uint8Array(shellData);
    } else {
      throw new Error('Invalid shell data type');
    }
    
    const view = new DataView(data.buffer, data.byteOffset, data.byteLength);
    let offset = 0;
    
    // Check magic bytes
    const magic = decoder.decode(data.slice(offset, offset + 4));
    if (magic !== this.MAGIC) {
      throw new Error('Invalid shell format');
    }
    offset += 4;
    
    // Read version
    const version = data[offset];
    offset += 1;
    
    // Read ID
    const idLength = view.getUint32(offset);
    offset += 4;
    const id = decoder.decode(data.slice(offset, offset + idLength));
    offset += idLength;
    
    // Read data
    const dataLength = view.getUint32(offset);
    offset += 4;
    const compressedData = data.slice(offset, offset + dataLength);
    
    // Decompress data
    let decompressedData;
    const isNode = typeof process !== 'undefined' && process.versions && process.versions.node;
    
    if (typeof window !== 'undefined' && window.pako) {
      decompressedData = decoder.decode(window.pako.ungzip(compressedData));
    } else if (isNode) {
      const zlib = require('zlib');
      decompressedData = zlib.gunzipSync(Buffer.from(compressedData)).toString();
    } else {
      // Fallback: assume it's just UTF-8 encoded
      decompressedData = decoder.decode(compressedData);
    }
    
    return {
      version,
      id,
      data: decompressedData
    };
  }
}

/**
 * Helper class for working with TSK files
 */
class TSK {
  constructor(data = {}) {
    this.data = data;
    this.fujsenCache = new Map();
    this.comments = {}; // Store comments by line
    this.metadata = {}; // Store additional metadata
  }

  /**
   * Load TSK from string content
   * @param {string} content - TSK content
   * @returns {TSK} TSK instance
   */
  static fromString(content) {
    const tsk = new TSK();
    const { data, comments } = TSKParser.parseWithComments(content);
    tsk.data = data;
    tsk.comments = comments;
    return tsk;
  }
  
  /**
   * Load TSK from file (Node.js only)
   * @param {string} filepath - Path to TSK file
   * @returns {TSK} TSK instance
   */
  static fromFile(filepath) {
    if (typeof require === 'undefined') {
      throw new Error('File operations are only available in Node.js');
    }
    const fs = require('fs');
    const content = fs.readFileSync(filepath, 'utf8');
    return TSK.fromString(content);
  }
  
  /**
   * Save TSK to file (Node.js only)
   * @param {string} filepath - Path to save TSK file
   */
  toFile(filepath) {
    if (typeof require === 'undefined') {
      throw new Error('File operations are only available in Node.js');
    }
    const fs = require('fs');
    fs.writeFileSync(filepath, this.toString(), 'utf8');
  }

  /**
   * Get a section
   * @param {string} name - Section name
   * @returns {Object|undefined} Section data
   */
  getSection(name) {
    return this.data[name];
  }

  /**
   * Get a value from a section
   * @param {string} section - Section name
   * @param {string} key - Key name
   * @returns {*} Value
   */
  getValue(section, key) {
    return this.data[section]?.[key];
  }

  /**
   * Set a section
   * @param {string} name - Section name
   * @param {Object} values - Section values
   */
  setSection(name, values) {
    this.data[name] = values;
  }

  /**
   * Set a value in a section
   * @param {string} section - Section name
   * @param {string} key - Key name
   * @param {*} value - Value to set
   */
  setValue(section, key, value) {
    if (!this.data[section]) {
      this.data[section] = {};
    }
    this.data[section][key] = value;
  }

  /**
   * Convert to string
   * @returns {string} TSK formatted string
   */
  toString() {
    return TSKParser.stringify(this.data);
  }

  /**
   * Get raw data
   * @returns {Object} Raw data object
   */
  toObject() {
    return this.data;
  }

  /**
   * Execute a fujsen (serialized function) from the TSK data
   * @param {string} section - Section containing the fujsen
   * @param {string} key - Key of the fujsen field
   * @param {...*} args - Arguments to pass to the function
   * @returns {*} Result of function execution
   */
  executeFujsen(section, key, ...args) {
    const fujsenCode = this.getValue(section, key || 'fujsen');
    
    if (!fujsenCode || typeof fujsenCode !== 'string') {
      throw new Error(`No fujsen found at ${section}.${key || 'fujsen'}`);
    }

    // Check cache first
    const cacheKey = `${section}.${key || 'fujsen'}`;
    let fn = this.fujsenCache.get(cacheKey);
    
    if (!fn) {
      // Parse and compile the function
      fn = this.compileFujsen(fujsenCode);
      this.fujsenCache.set(cacheKey, fn);
    }
    
    return fn(...args);
  }
  
  /**
   * Execute fujsen with custom context
   * @param {string} section - Section containing the fujsen
   * @param {string} key - Key of the fujsen field
   * @param {Object} context - Context object (this binding)
   * @param {...*} args - Arguments to pass to the function
   * @returns {*} Result of function execution
   */
  executeFujsenWithContext(section, key, context, ...args) {
    const fujsenCode = this.getValue(section, key || 'fujsen');
    
    if (!fujsenCode || typeof fujsenCode !== 'string') {
      throw new Error(`No fujsen found at ${section}.${key || 'fujsen'}`);
    }

    const cacheKey = `${section}.${key || 'fujsen'}`;
    let fn = this.fujsenCache.get(cacheKey);
    
    if (!fn) {
      fn = this.compileFujsen(fujsenCode);
      this.fujsenCache.set(cacheKey, fn);
    }
    
    // Apply context and execute
    return fn.apply(context, args);
  }

  /**
   * Compile fujsen code into executable function
   * @param {string} code - Function code
   * @returns {Function} Compiled function
   */
  compileFujsen(code) {
    const trimmedCode = code.trim();
    
    // Check if it's a function declaration
    const funcMatch = trimmedCode.match(/^function\s+(\w+)?\s*\((.*?)\)\s*{([\s\S]*)}$/);
    
    if (funcMatch) {
      const [, name, params, body] = funcMatch;
      // Create function from parsed parts
      const paramList = params ? params.split(',').map(p => p.trim()).filter(p => p) : [];
      return new Function(...paramList, body);
    }
    
    // Check if it's an arrow function with block body
    const arrowBlockMatch = trimmedCode.match(/^\s*\((.*?)\)\s*=>\s*{([\s\S]*)}$/);
    
    if (arrowBlockMatch) {
      const [, params, body] = arrowBlockMatch;
      const paramList = params ? params.split(',').map(p => p.trim()).filter(p => p) : [];
      return new Function(...paramList, body);
    }
    
    // Check if it's an arrow function with expression body
    const arrowExprMatch = trimmedCode.match(/^\s*\((.*?)\)\s*=>\s*([\s\S]+)$/);
    
    if (arrowExprMatch) {
      const [, params, body] = arrowExprMatch;
      const paramList = params ? params.split(',').map(p => p.trim()).filter(p => p) : [];
      return new Function(...paramList, `return ${body}`);
    }
    
    // Try to evaluate as a full function expression
    try {
      return eval(`(${trimmedCode})`);
    } catch (e) {
      // Try as a function body
      try {
        return new Function(trimmedCode);
      } catch (e2) {
        throw new Error(`Failed to compile fujsen: ${e.message}`);
      }
    }
  }

  /**
   * Get all fujsen functions in a section
   * @param {string} section - Section name
   * @returns {Object} Map of fujsen functions
   */
  getFujsenMap(section) {
    const sectionData = this.getSection(section);
    if (!sectionData) return {};
    
    const fujsenMap = {};
    
    for (const [key, value] of Object.entries(sectionData)) {
      if (key === 'fujsen' || key.endsWith('_fujsen')) {
        try {
          fujsenMap[key] = this.compileFujsen(value);
        } catch (e) {
          console.warn(`Failed to compile fujsen at ${section}.${key}:`, e);
        }
      }
    }
    
    return fujsenMap;
  }

  /**
   * Add a function as fujsen to the TSK data
   * @param {string} section - Section name
   * @param {string} key - Key name (defaults to 'fujsen')
   * @param {Function} fn - Function to serialize
   */
  setFujsen(section, key, fn) {
    if (typeof fn === 'function') {
      const fnString = fn.toString();
      this.setValue(section, key || 'fujsen', fnString);
    } else {
      throw new Error('Fujsen value must be a function');
    }
  }
  
  /**
   * Store binary data with TSK metadata and .shell file
   * @param {string|Buffer|Uint8Array} data - Binary data to store
   * @param {Object} metadata - Additional metadata
   * @returns {Object} Storage info with paths
   */
  storeWithShell(data, metadata = {}) {
    const storageId = `flex_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    const type = this.detectType(data);
    const size = data.length || data.byteLength;
    
    // Create TSK metadata
    this.setSection('storage', {
      id: storageId,
      type: type,
      size: size,
      created: Date.now(),
      chunks: Math.ceil(size / 65536) // 64KB chunks
    });
    
    this.setSection('metadata', metadata);
    
    this.setSection('verification', {
      hash: this.generateHash(data),
      checksum: 'sha256'
    });
    
    // Pack shell data
    const shellData = ShellStorage.pack({
      version: 1,
      type: 'flexchain_storage',
      id: storageId,
      compression: 'gzip',
      data: data
    });
    
    return {
      storageId,
      tskData: this.toString(),
      shellData: shellData,
      type,
      size
    };
  }
  
  /**
   * Retrieve data from shell storage
   * @param {Buffer|Uint8Array} shellData - Shell binary data
   * @returns {Object} Retrieved data and metadata
   */
  retrieveFromShell(shellData) {
    const unpacked = ShellStorage.unpack(shellData);
    const storageInfo = this.getSection('storage');
    
    return {
      data: unpacked.data,
      metadata: this.getSection('metadata'),
      verification: this.getSection('verification'),
      storageInfo
    };
  }
  
  /**
   * Detect content type from data
   * @param {string|Buffer|Uint8Array} data - Data to analyze
   * @returns {string} MIME type
   */
  detectType(data) {
    // Convert to Uint8Array for consistent handling
    let bytes;
    if (typeof data === 'string') {
      bytes = new TextEncoder().encode(data);
    } else if (data instanceof Uint8Array) {
      bytes = data;
    } else if (data.buffer) {
      bytes = new Uint8Array(data.buffer);
    } else {
      return 'application/octet-stream';
    }
    
    // Check magic bytes
    if (bytes[0] === 0xFF && bytes[1] === 0xD8 && bytes[2] === 0xFF) {
      return 'image/jpeg';
    }
    if (bytes[0] === 0x89 && bytes[1] === 0x50 && bytes[2] === 0x4E && bytes[3] === 0x47) {
      return 'image/png';
    }
    if (bytes[0] === 0x25 && bytes[1] === 0x50 && bytes[2] === 0x44 && bytes[3] === 0x46) {
      return 'application/pdf';
    }
    
    // Check if it's text
    if (typeof data === 'string' || this.isTextData(bytes)) {
      return 'text/plain';
    }
    
    return 'application/octet-stream';
  }
  
  /**
   * Check if data is text
   * @param {Uint8Array} bytes - Data bytes
   * @returns {boolean} True if likely text
   */
  isTextData(bytes) {
    const sample = Math.min(bytes.length, 1000);
    for (let i = 0; i < sample; i++) {
      const byte = bytes[i];
      // Check for non-printable characters (excluding common whitespace)
      if (byte < 0x20 && byte !== 0x09 && byte !== 0x0A && byte !== 0x0D) {
        return false;
      }
      if (byte > 0x7E && byte < 0xA0) {
        return false;
      }
    }
    return true;
  }
  
  /**
   * Generate hash for data
   * @param {string|Buffer|Uint8Array} data - Data to hash
   * @returns {string} Hex hash string
   */
  generateHash(data) {
    // Simple hash implementation (in production, use crypto library)
    let hash = 0;
    const str = typeof data === 'string' ? data : data.toString();
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return 'simple:' + Math.abs(hash).toString(16);
  }
  
  /**
   * Execute @ operators and functions in TSK data
   * @param {*} value - Value that might contain operators
   * @param {Object} context - Execution context
   * @returns {*} Executed result
   */
  async executeOperators(value, context = {}) {
    // If it's an object with __operator or __function, execute it
    if (value && typeof value === 'object') {
      if (value.__operator) {
        return await this.executeOperator(value.__operator, value.expression, context);
      }
      if (value.__function) {
        return await this.executeFunction(value.__function, value.expression, context);
      }
      
      // Recursively process objects and arrays
      if (Array.isArray(value)) {
        const results = [];
        for (const item of value) {
          results.push(await this.executeOperators(item, context));
        }
        return results;
      } else {
        const result = {};
        for (const [key, val] of Object.entries(value)) {
          result[key] = await this.executeOperators(val, context);
        }
        return result;
      }
    }
    
    return value;
  }
  
  /**
   * Execute a specific @ operator
   * @param {string} operator - Operator name
   * @param {string} expression - Full expression
   * @param {Object} context - Execution context
   * @returns {*} Result
   */
  async executeOperator(operator, expression, context) {
    switch (operator) {
      case 'Query':
        return await this.executeQuery(expression, context);
      
      case 'cache':
        return await this.executeCache(expression, context);
      
      case 'metrics':
        return this.executeMetrics(expression, context);
      
      case 'if':
        return this.executeIf(expression, context);
      
      case 'date':
        return this.executeDate(expression, context);
      
      case 'optimize':
        return this.executeOptimize(expression, context);
      
      case 'learn':
        return this.executeLearn(expression, context);
      
      case 'feature':
        return this.executeFeature(expression, context);
      
      case 'json':
        return this.executeJson(expression, context);
      
      case 'request':
        return this.executeRequest(expression, context);
      
      default:
        console.warn(`Unknown operator: ${operator}`);
        return expression;
    }
  }
  
  /**
   * Execute a function (env, php, file, query)
   * @param {string} func - Function name
   * @param {string} expression - Full expression
   * @param {Object} context - Execution context
   * @returns {*} Result
   */
  async executeFunction(func, expression, context) {
    switch (func) {
      case 'env':
        return this.executeEnv(expression, context);
      
      case 'php':
        // In JS, we can't execute PHP, so return a placeholder
        return `[PHP: ${expression}]`;
      
      case 'file':
        return await this.executeFile(expression, context);
      
      case 'query':
        return await this.executeQuery(expression, context);
      
      default:
        console.warn(`Unknown function: ${func}`);
        return expression;
    }
  }
  
  /**
   * Execute @Query/@q operator
   * @param {string} expression - Query expression
   * @param {Object} context - Execution context
   * @returns {*} Query result
   */
  async executeQuery(expression, context) {
    // Parse the query expression
    const match = expression.match(/@?[Qq]uery\("([^"]+)"\)(.*)/);
    if (!match) return null;
    
    const className = match[1];
    const chainStr = match[2];
    
    // If running in browser, make API call
    if (typeof window !== 'undefined') {
      try {
        const response = await fetch('/api/tusk/query', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            className,
            chain: chainStr
          })
        });
        
        if (response.ok) {
          return await response.json();
        }
      } catch (error) {
        console.error('Query execution failed:', error);
      }
    }
    
    // Return mock data for now
    return {
      __query: className,
      __chain: chainStr,
      __result: 'Query execution not available in this environment'
    };
  }
  
  /**
   * Execute @cache operator
   * @param {string} expression - Cache expression
   * @param {Object} context - Execution context
   * @returns {*} Cached or computed value
   */
  async executeCache(expression, context) {
    const match = expression.match(/@cache\("([^"]+)"\s*,\s*(.+)\)/);
    if (!match) return null;
    
    const ttl = match[1];
    const valueExpr = match[2];
    
    // Simple in-memory cache for demo
    const cacheKey = `cache_${expression}`;
    const cached = this.metadata[cacheKey];
    
    if (cached && cached.expires > Date.now()) {
      return cached.value;
    }
    
    // Execute the value expression
    const value = await this.executeOperators(TSKParser.parseValue(valueExpr), context);
    
    // Store in cache
    const ttlMs = this.parseTTL(ttl);
    this.metadata[cacheKey] = {
      value,
      expires: Date.now() + ttlMs
    };
    
    return value;
  }
  
  /**
   * Execute @metrics operator
   * @param {string} expression - Metrics expression
   * @param {Object} context - Execution context
   * @returns {number} Metric value
   */
  executeMetrics(expression, context) {
    const match = expression.match(/@metrics\("([^"]+)"\s*,\s*(.+)\)/);
    if (!match) return 0;
    
    const name = match[1];
    const value = parseFloat(match[2]) || 1;
    
    // Store metric
    if (!this.metadata.metrics) {
      this.metadata.metrics = {};
    }
    
    if (!this.metadata.metrics[name]) {
      this.metadata.metrics[name] = 0;
    }
    
    this.metadata.metrics[name] += value;
    return this.metadata.metrics[name];
  }
  
  /**
   * Execute @if operator
   * @param {string} expression - If expression
   * @param {Object} context - Execution context
   * @returns {*} True or false branch value
   */
  executeIf(expression, context) {
    const match = expression.match(/@if\((.+?)\s*,\s*(.+?)\s*,\s*(.+)\)/);
    if (!match) return null;
    
    const condition = match[1].trim();
    const trueValue = match[2].trim();
    const falseValue = match[3].trim();
    
    // Evaluate condition
    let condResult = false;
    if (condition === 'true') {
      condResult = true;
    } else if (condition === 'false') {
      condResult = false;
    } else if (condition.startsWith('@')) {
      // Variable reference
      const varName = condition.substring(1);
      condResult = !!context[varName];
    } else {
      condResult = !!condition;
    }
    
    return TSKParser.parseValue(condResult ? trueValue : falseValue);
  }
  
  /**
   * Execute @date operator
   * @param {string} expression - Date expression
   * @param {Object} context - Execution context
   * @returns {string} Formatted date
   */
  executeDate(expression, context) {
    const match = expression.match(/@date\("?([^"\)]+)"?\)/);
    if (!match) return new Date().toISOString();
    
    const format = match[1];
    const now = new Date();
    
    if (format === 'now') {
      return now.toISOString().replace('T', ' ').substring(0, 19);
    }
    
    // Simple format replacements
    let result = format;
    result = result.replace('Y', now.getFullYear());
    result = result.replace('m', String(now.getMonth() + 1).padStart(2, '0'));
    result = result.replace('d', String(now.getDate()).padStart(2, '0'));
    result = result.replace('H', String(now.getHours()).padStart(2, '0'));
    result = result.replace('i', String(now.getMinutes()).padStart(2, '0'));
    result = result.replace('s', String(now.getSeconds()).padStart(2, '0'));
    
    return result;
  }
  
  /**
   * Execute env() function
   * @param {string} expression - Env expression
   * @param {Object} context - Execution context
   * @returns {string} Environment value
   */
  executeEnv(expression, context) {
    const match = expression.match(/env\("([^"]+)"(?:\s*,\s*"([^"]+)")?\)/);
    if (!match) return null;
    
    const varName = match[1];
    const defaultValue = match[2] || null;
    
    // Check context first, then environment
    if (context[varName] !== undefined) {
      return context[varName];
    }
    
    if (typeof process !== 'undefined' && process.env) {
      return process.env[varName] || defaultValue;
    }
    
    return defaultValue;
  }
  
  /**
   * Parse TTL string to milliseconds
   * @param {string} ttl - TTL string like "5m", "1h", "30s"
   * @returns {number} Milliseconds
   */
  parseTTL(ttl) {
    const match = ttl.match(/(\d+)([smhd])/);
    if (!match) return 60000; // Default 1 minute
    
    const value = parseInt(match[1]);
    const unit = match[2];
    
    switch (unit) {
      case 's': return value * 1000;
      case 'm': return value * 60 * 1000;
      case 'h': return value * 60 * 60 * 1000;
      case 'd': return value * 24 * 60 * 60 * 1000;
      default: return 60000;
    }
  }
  
  // Placeholder implementations for other operators
  executeOptimize(expression, context) {
    return { __optimize: expression };
  }
  
  executeLearn(expression, context) {
    return { __learn: expression };
  }
  
  executeFeature(expression, context) {
    const match = expression.match(/@feature\("([^"]+)"\)/);
    if (!match) return false;
    
    const feature = match[1];
    // Check common JS features
    const features = {
      'websocket': typeof WebSocket !== 'undefined',
      'fetch': typeof fetch !== 'undefined',
      'localstorage': typeof localStorage !== 'undefined',
      'webworker': typeof Worker !== 'undefined',
      'promise': typeof Promise !== 'undefined',
      'async': true
    };
    
    return features[feature.toLowerCase()] || false;
  }
  
  executeJson(expression, context) {
    const match = expression.match(/@json\((.+)\)/);
    if (!match) return '{}';
    
    try {
      const data = TSKParser.parseValue(match[1]);
      return JSON.stringify(data);
    } catch (e) {
      return '{}';
    }
  }
  
  executeRequest(expression, context) {
    if (typeof window === 'undefined') {
      return { method: 'GET', path: '/' };
    }
    
    const request = {
      method: 'GET',
      url: window.location.href,
      host: window.location.host,
      path: window.location.pathname,
      query: window.location.search,
      hash: window.location.hash
    };
    
    if (expression === '@request') {
      return request;
    }
    
    const match = expression.match(/@request\.(.+)/);
    if (match) {
      return request[match[1]] || null;
    }
    
    return request;
  }
  
  async executeFile(expression, context) {
    const match = expression.match(/file\("([^"]+)"\)/);
    if (!match) return null;
    
    const filePath = match[1];
    
    // In browser, make API call
    if (typeof window !== 'undefined') {
      try {
        const response = await fetch(`/api/tusk/file?path=${encodeURIComponent(filePath)}`);
        if (response.ok) {
          return await response.text();
        }
      } catch (error) {
        console.error('File read failed:', error);
      }
    }
    
    // In Node.js, read file
    if (typeof require !== 'undefined') {
      const fs = require('fs');
      try {
        return fs.readFileSync(filePath, 'utf8');
      } catch (error) {
        console.error('File read failed:', error);
      }
    }
    
    return `[File: ${filePath}]`;
  }
}

// Add parseWithComments method to TSKParser
TSKParser.parseWithComments = function(content) {
  const lines = content.split('\n');
  const result = {};
  const comments = {};
  let currentSection = null;
  let inMultilineString = false;
  let multilineKey = null;
  let multilineContent = [];
  
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const trimmedLine = line.trim();
    
    // Handle multiline strings
    if (inMultilineString) {
      if (trimmedLine === '"""') {
        if (currentSection && multilineKey) {
          result[currentSection][multilineKey] = multilineContent.join('\n');
        }
        inMultilineString = false;
        multilineKey = null;
        multilineContent = [];
        continue;
      }
      multilineContent.push(line);
      continue;
    }
    
    // Capture comments
    if (trimmedLine.startsWith('#')) {
      comments[i] = trimmedLine;
      continue;
    }
    
    // Skip empty lines
    if (!trimmedLine) {
      continue;
    }
    
    // Section header
    const sectionMatch = trimmedLine.match(/^\[(.+)\]$/);
    if (sectionMatch) {
      currentSection = sectionMatch[1];
      result[currentSection] = {};
      continue;
    }
    
    // Key-value pair
    if (currentSection && trimmedLine.includes('=')) {
      const separatorIndex = trimmedLine.indexOf('=');
      const key = trimmedLine.substring(0, separatorIndex).trim();
      const valueStr = trimmedLine.substring(separatorIndex + 1).trim();
      
      // Check for multiline string start
      if (valueStr === '"""') {
        inMultilineString = true;
        multilineKey = key;
        continue;
      }
      
      const value = TSKParser.parseValue(valueStr);
      result[currentSection][key] = value;
    }
  }
  
  return { data: result, comments };
};

// CommonJS exports for Node.js
if (typeof module !== 'undefined' && module.exports) {
  module.exports = { TSKParser, TSK, ShellStorage };
}

// Browser global
if (typeof window !== 'undefined') {
  window.TSKParser = TSKParser;
  window.TSK = TSK;
  window.ShellStorage = ShellStorage;
}