/**
 * TSK (TuskLang Configuration) Parser and Generator
 * Handles the TOML-like format used by Flexchain for configuration and metadata
 */

export class TSKParser {
  /**
   * Parse TSK content into JavaScript object
   */
  static parse(content: string): Record<string, any> {
    const lines = content.split('\n');
    const result: Record<string, any> = {};
    let currentSection: string | null = null;
    let inMultilineString = false;
    let multilineKey: string | null = null;
    let multilineContent: string[] = [];

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
   */
  private static parseValue(valueStr: string): any {
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
      const obj: Record<string, any> = {};
      
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

    // Return as string if no other type matches
    return valueStr;
  }

  /**
   * Split array items considering nested structures
   */
  private static splitArrayItems(content: string): string[] {
    const items: string[] = [];
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
   */
  private static splitObjectPairs(content: string): string[] {
    const pairs: string[] = [];
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
   */
  static stringify(data: Record<string, any>): string {
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
   */
  private static formatValue(value: any): string {
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
      const pairs: string[] = [];
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
export class ShellStorage {
  static readonly MAGIC = 'FLEX';
  static readonly VERSION = 1;
  
  /**
   * Pack data into Shell binary format
   */
  static pack(data: {
    version: number;
    type: string;
    id: string;
    compression: string;
    data: string | Buffer | Uint8Array;
  }): Buffer | Uint8Array {
    const isNode = typeof process !== 'undefined' && process.versions && process.versions.node;
    
    // Create header
    const encoder = new TextEncoder();
    const magic = encoder.encode(this.MAGIC);
    const idBytes = encoder.encode(data.id);
    
    // Compress data if it's not already compressed
    let compressedData: Uint8Array;
    if (typeof data.data === 'string') {
      if (isNode) {
        const zlib = require('zlib');
        compressedData = zlib.gzipSync(data.data);
      } else {
        compressedData = encoder.encode(data.data);
      }
    } else {
      compressedData = new Uint8Array(data.data as any);
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
   */
  static unpack(shellData: Buffer | Uint8Array): {
    version: number;
    id: string;
    data: string;
  } {
    const decoder = new TextDecoder();
    const view = new DataView((shellData as any).buffer || shellData);
    let offset = 0;
    
    // Check magic bytes
    const magic = decoder.decode(new Uint8Array((shellData as any).buffer || shellData, offset, 4));
    if (magic !== this.MAGIC) {
      throw new Error('Invalid shell format');
    }
    offset += 4;
    
    // Read version
    const version = shellData[offset];
    offset += 1;
    
    // Read ID
    const idLength = view.getUint32(offset);
    offset += 4;
    const id = decoder.decode(new Uint8Array((shellData as any).buffer || shellData, offset, idLength));
    offset += idLength;
    
    // Read data
    const dataLength = view.getUint32(offset);
    offset += 4;
    const compressedData = new Uint8Array((shellData as any).buffer || shellData, offset, dataLength);
    
    // Decompress data
    let data: string;
    const isNode = typeof process !== 'undefined' && process.versions && process.versions.node;
    
    if (isNode) {
      const zlib = require('zlib');
      data = zlib.gunzipSync(Buffer.from(compressedData)).toString();
    } else {
      data = decoder.decode(compressedData);
    }
    
    return { version, id, data };
  }
}

export class TSK {
  private data: Record<string, any>;
  private fujsenCache: Map<string, Function>;
  private comments: Record<number, string>;
  private metadata: Record<string, any>;

  constructor(data: Record<string, any> = {}) {
    this.data = data;
    this.fujsenCache = new Map();
    this.comments = {};
    this.metadata = {};
  }

  /**
   * Load TSK from string content
   */
  static fromString(content: string): TSK {
    const data = TSKParser.parse(content);
    return new TSK(data);
  }

  /**
   * Get a section
   */
  getSection(name: string): Record<string, any> | undefined {
    return this.data[name];
  }

  /**
   * Get a value from a section
   */
  getValue(section: string, key: string): any {
    return this.data[section]?.[key];
  }

  /**
   * Set a section
   */
  setSection(name: string, values: Record<string, any>): void {
    this.data[name] = values;
  }

  /**
   * Set a value in a section
   */
  setValue(section: string, key: string, value: any): void {
    if (!this.data[section]) {
      this.data[section] = {};
    }
    this.data[section][key] = value;
  }

  /**
   * Convert to string
   */
  toString(): string {
    return TSKParser.stringify(this.data);
  }

  /**
   * Get raw data
   */
  toObject(): Record<string, any> {
    return this.data;
  }

  /**
   * Execute a fujsen (serialized function) from the TSK data
   */
  executeFujsen(section: string, key?: string, ...args: any[]): any {
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
   * Compile fujsen code into executable function
   */
  private compileFujsen(code: string): Function {
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
    } catch (e: any) {
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
   */
  getFujsenMap(section: string): Record<string, Function> {
    const sectionData = this.getSection(section);
    if (!sectionData) return {};
    
    const fujsenMap: Record<string, Function> = {};
    
    for (const [key, value] of Object.entries(sectionData)) {
      if (key === 'fujsen' || key.endsWith('_fujsen')) {
        try {
          fujsenMap[key] = this.compileFujsen(value as string);
        } catch (e) {
          console.warn(`Failed to compile fujsen at ${section}.${key}:`, e);
        }
      }
    }
    
    return fujsenMap;
  }

  /**
   * Add a function as fujsen to the TSK data
   */
  setFujsen(section: string, key: string | null, fn: Function): void {
    if (typeof fn === 'function') {
      const fnString = fn.toString();
      this.setValue(section, key || 'fujsen', fnString);
    } else {
      throw new Error('Fujsen value must be a function');
    }
  }
}

// Export for convenience
export default TSK;