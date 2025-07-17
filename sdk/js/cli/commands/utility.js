/**
 * Utility Commands for TuskLang CLI
 * ==================================
 * Implements utility operations
 */

const fs = require('fs').promises;
const path = require('path');
const TuskLang = require('../../index.js');

// Parse command
async function parse(file) {
  try {
    console.log(`🔄 Parsing TSK file: ${file}`);
    
    // Check if file exists
    await fs.access(file);
    
    // Read file content
    const content = await fs.readFile(file, 'utf8');
    
    // Parse with TuskLang
    const tusk = new TuskLang();
    const parsed = tusk.parse(content);
    
    console.log('✅ TSK file parsed successfully');
    console.log('📍 Parsed structure:');
    
    // Display parsed structure
    displayParsedStructure(parsed);
    
    return { success: true, parsed };
  } catch (error) {
    console.error('❌ Parsing failed:', error.message);
    return { success: false, error: error.message };
  }
}

// Validate command
async function validate(file) {
  try {
    console.log(`🔍 Validating TSK file: ${file}`);
    
    // Check if file exists
    await fs.access(file);
    
    // Read file content
    const content = await fs.readFile(file, 'utf8');
    
    // Validate with TuskLang
    const tusk = new TuskLang();
    const validation = validateTSKContent(content);
    
    console.log('✅ TSK file validation completed');
    
    if (validation.valid) {
      console.log('✅ File is valid TuskLang syntax');
      console.log(`📍 Total lines: ${validation.lines}`);
      console.log(`📍 Sections: ${validation.sections}`);
      console.log(`📍 Variables: ${validation.variables}`);
      console.log(`📍 Functions: ${validation.functions}`);
    } else {
      console.log('❌ File has syntax errors:');
      validation.errors.forEach((error, index) => {
        console.log(`  ${index + 1}. Line ${error.line}: ${error.message}`);
      });
    }
    
    return { success: true, validation };
  } catch (error) {
    console.error('❌ Validation failed:', error.message);
    return { success: false, error: error.message };
  }
}

// Convert command
async function convert(options) {
  try {
    console.log('🔄 Converting between formats...');
    
    if (!options.input || !options.output) {
      throw new Error('Both --input and --output options are required');
    }
    
    // Check if input file exists
    await fs.access(options.input);
    
    // Read input file
    const content = await fs.readFile(options.input, 'utf8');
    const inputExt = path.extname(options.input).toLowerCase();
    const outputExt = path.extname(options.output).toLowerCase();
    
    console.log(`📍 Converting from ${inputExt} to ${outputExt}`);
    
    let converted;
    
    if (inputExt === '.tsk' && outputExt === '.json') {
      // TSK to JSON
      const tusk = new TuskLang();
      const parsed = tusk.parse(content);
      converted = JSON.stringify(parsed, null, 2);
    } else if (inputExt === '.json' && outputExt === '.tsk') {
      // JSON to TSK
      const parsed = JSON.parse(content);
      converted = TuskLang.stringify(parsed);
    } else if (inputExt === '.tsk' && outputExt === '.yaml') {
      // TSK to YAML
      const tusk = new TuskLang();
      const parsed = tusk.parse(content);
      converted = convertToYAML(parsed);
    } else if (inputExt === '.yaml' && outputExt === '.tsk') {
      // YAML to TSK
      const yaml = require('js-yaml');
      const parsed = yaml.load(content);
      converted = TuskLang.stringify(parsed);
    } else {
      throw new Error(`Unsupported conversion: ${inputExt} to ${outputExt}`);
    }
    
    // Write output file
    await fs.writeFile(options.output, converted);
    
    console.log('✅ Conversion completed successfully');
    console.log(`📍 Output file: ${options.output}`);
    
    return { success: true, input: options.input, output: options.output };
  } catch (error) {
    console.error('❌ Conversion failed:', error.message);
    return { success: false, error: error.message };
  }
}

// Get command
async function get(file, keyPath) {
  try {
    console.log(`📍 Getting value from TSK file: ${file}`);
    console.log(`📍 Key path: ${keyPath}`);
    
    // Check if file exists
    await fs.access(file);
    
    // Read and parse file
    const content = await fs.readFile(file, 'utf8');
    const tusk = new TuskLang();
    const parsed = tusk.parse(content);
    
    // Get value by path
    const value = getValueByPath(parsed, keyPath);
    
    if (value !== undefined) {
      console.log(`✅ Value: ${JSON.stringify(value)}`);
      return { success: true, key: keyPath, value };
    } else {
      console.log(`❌ Key not found: ${keyPath}`);
      return { success: false, key: keyPath, found: false };
    }
  } catch (error) {
    console.error('❌ Get operation failed:', error.message);
    return { success: false, error: error.message };
  }
}

// Set command
async function set(file, keyPath, value) {
  try {
    console.log(`📍 Setting value in TSK file: ${file}`);
    console.log(`📍 Key path: ${keyPath}`);
    console.log(`📍 Value: ${value}`);
    
    // Check if file exists
    await fs.access(file);
    
    // Read and parse file
    const content = await fs.readFile(file, 'utf8');
    const tusk = new TuskLang();
    const parsed = tusk.parse(content);
    
    // Set value by path
    const updated = setValueByPath(parsed, keyPath, parseValue(value));
    
    // Convert back to TSK format
    const updatedContent = TuskLang.stringify(updated);
    
    // Write back to file
    await fs.writeFile(file, updatedContent);
    
    console.log('✅ Value set successfully');
    return { success: true, key: keyPath, value: parseValue(value) };
  } catch (error) {
    console.error('❌ Set operation failed:', error.message);
    return { success: false, error: error.message };
  }
}

// Helper functions
function displayParsedStructure(obj, indent = 0) {
  const spaces = '  '.repeat(indent);
  
  for (const [key, value] of Object.entries(obj)) {
    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      console.log(`${spaces}${key}: {`);
      displayParsedStructure(value, indent + 1);
      console.log(`${spaces}}`);
    } else {
      const displayValue = Array.isArray(value) ? `[${value.join(', ')}]` : JSON.stringify(value);
      console.log(`${spaces}${key}: ${displayValue}`);
    }
  }
}

function validateTSKContent(content) {
  const validation = {
    valid: true,
    lines: content.split('\n').length,
    sections: 0,
    variables: 0,
    functions: 0,
    errors: []
  };
  
  const lines = content.split('\n');
  
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();
    const lineNumber = i + 1;
    
    // Skip empty lines and comments
    if (!line || line.startsWith('#')) {
      continue;
    }
    
    // Check for section headers
    if (line.startsWith('[') && line.endsWith(']')) {
      validation.sections++;
      continue;
    }
    
    // Check for variable definitions
    if (line.startsWith('$')) {
      validation.variables++;
      continue;
    }
    
    // Check for function calls
    if (line.includes('@')) {
      validation.functions++;
      continue;
    }
    
    // Check for key-value pairs
    if (line.includes(':')) {
      const [key, ...valueParts] = line.split(':');
      const value = valueParts.join(':').trim();
      
      // Validate key
      if (!key.trim()) {
        validation.valid = false;
        validation.errors.push({
          line: lineNumber,
          message: 'Empty key in key-value pair'
        });
      }
      
      // Validate value
      if (!value && !line.endsWith(':')) {
        validation.valid = false;
        validation.errors.push({
          line: lineNumber,
          message: 'Empty value in key-value pair'
        });
      }
    } else if (line && !line.startsWith('[') && !line.startsWith('$') && !line.startsWith('#')) {
      // Line doesn't match any valid pattern
      validation.valid = false;
      validation.errors.push({
        line: lineNumber,
        message: 'Invalid syntax'
      });
    }
  }
  
  return validation;
}

function convertToYAML(obj, indent = 0) {
  const spaces = '  '.repeat(indent);
  let yaml = '';
  
  for (const [key, value] of Object.entries(obj)) {
    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      yaml += `${spaces}${key}:\n`;
      yaml += convertToYAML(value, indent + 1);
    } else if (Array.isArray(value)) {
      yaml += `${spaces}${key}:\n`;
      value.forEach(item => {
        yaml += `${spaces}  - ${JSON.stringify(item)}\n`;
      });
    } else {
      yaml += `${spaces}${key}: ${JSON.stringify(value)}\n`;
    }
  }
  
  return yaml;
}

function getValueByPath(obj, path) {
  const keys = path.split('.');
  let current = obj;
  
  for (const key of keys) {
    if (current && typeof current === 'object' && key in current) {
      current = current[key];
    } else {
      return undefined;
    }
  }
  
  return current;
}

function setValueByPath(obj, path, value) {
  const keys = path.split('.');
  const result = { ...obj };
  let current = result;
  
  for (let i = 0; i < keys.length - 1; i++) {
    const key = keys[i];
    if (!(key in current) || typeof current[key] !== 'object') {
      current[key] = {};
    }
    current = current[key];
  }
  
  current[keys[keys.length - 1]] = value;
  return result;
}

function parseValue(value) {
  // Try to parse as different types
  if (value === 'true' || value === 'false') {
    return value === 'true';
  }
  
  if (!isNaN(value) && value !== '') {
    return Number(value);
  }
  
  if (value.startsWith('"') && value.endsWith('"')) {
    return value.slice(1, -1);
  }
  
  if (value.startsWith("'") && value.endsWith("'")) {
    return value.slice(1, -1);
  }
  
  // Try to parse as JSON
  try {
    return JSON.parse(value);
  } catch {
    // Return as string
    return value;
  }
}

module.exports = {
  parse,
  validate,
  convert,
  get,
  set
}; 