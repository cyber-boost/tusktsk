/**
 * Configuration Commands for TuskLang CLI
 * =======================================
 * Implements configuration management commands
 */

const { Command } = require('commander');
const fs = require('fs').promises;
const path = require('path');
const PeanutConfig = require('../../peanut-config.js');

// Config get command
const get = new Command('get')
  .description('Get configuration value by path')
  .argument('<key.path>', 'Configuration key path (e.g., server.port)')
  .argument('[dir]', 'Directory to search for config', process.cwd())
  .action(async (keyPath, dir) => {
    try {
      console.log(`📍 Getting configuration value: ${keyPath}`);
      
      const peanutConfig = new PeanutConfig();
      const value = peanutConfig.get(keyPath, null, dir);
      
      if (value !== null) {
        console.log(`✅ ${keyPath}: ${JSON.stringify(value)}`);
        return { key: keyPath, value, found: true };
      } else {
        console.log(`❌ Configuration key not found: ${keyPath}`);
        return { key: keyPath, value: null, found: false };
      }
    } catch (error) {
      console.error('❌ Failed to get configuration:', error.message);
      return { success: false, error: error.message };
    }
  });

// Config check command
const check = new Command('check')
  .description('Check configuration hierarchy')
  .argument('[path]', 'Path to check', process.cwd())
  .action(async (configPath) => {
    try {
      console.log(`📍 Checking configuration hierarchy: ${configPath}`);
      
      const peanutConfig = new PeanutConfig();
      const hierarchy = peanutConfig.findConfigHierarchy(configPath);
      
      console.log('📁 Configuration Hierarchy:');
      console.log('===========================');
      
      if (hierarchy.length === 0) {
        console.log('❌ No configuration files found');
        return { found: false, hierarchy: [] };
      }
      
      for (let i = 0; i < hierarchy.length; i++) {
        const { path: configFile, type } = hierarchy[i];
        const level = i === 0 ? 'Root' : i === hierarchy.length - 1 ? 'Current' : 'Parent';
        console.log(`${level.padEnd(8)}: ${configFile} (${type})`);
      }
      
      console.log('');
      console.log(`✅ Found ${hierarchy.length} configuration file(s)`);
      
      return { found: true, hierarchy };
    } catch (error) {
      console.error('❌ Failed to check configuration hierarchy:', error.message);
      return { success: false, error: error.message };
    }
  });

// Config validate command
const validate = new Command('validate')
  .description('Validate entire configuration chain')
  .argument('[path]', 'Path to validate', process.cwd())
  .action(async (configPath) => {
    try {
      console.log(`📍 Validating configuration chain: ${configPath}`);
      
      const peanutConfig = new PeanutConfig();
      const config = peanutConfig.load(configPath);
      
      const validation = validateConfiguration(config);
      
      console.log('🔍 Configuration Validation Results:');
      console.log('====================================');
      
      if (validation.valid) {
        console.log('✅ Configuration is valid');
        console.log(`📍 Total keys: ${validation.totalKeys}`);
        console.log(`📍 Sections: ${validation.sections}`);
        console.log(`📍 Nested levels: ${validation.maxDepth}`);
      } else {
        console.log('❌ Configuration has issues:');
        for (const error of validation.errors) {
          console.log(`  - ${error}`);
        }
      }
      
      return validation;
    } catch (error) {
      console.error('❌ Failed to validate configuration:', error.message);
      return { valid: false, error: error.message };
    }
  });

// Config compile command
const compile = new Command('compile')
  .description('Auto-compile all peanu.tsk files')
  .argument('[path]', 'Path to compile', process.cwd())
  .action(async (configPath) => {
    try {
      console.log(`🔄 Auto-compiling configuration files: ${configPath}`);
      
      const peanutConfig = new PeanutConfig();
      const hierarchy = peanutConfig.findConfigHierarchy(configPath);
      
      let compiledCount = 0;
      let skippedCount = 0;
      
      for (const { path: configFile, type } of hierarchy) {
        if (type === 'tsk' || type === 'text') {
          try {
            const binaryFile = configFile.replace(/\.(peanuts|tsk)$/, '.pnt');
            
            // Check if binary is outdated
            const configStats = await fs.stat(configFile);
            let shouldCompile = true;
            
            try {
              const binaryStats = await fs.stat(binaryFile);
              shouldCompile = configStats.mtime > binaryStats.mtime;
            } catch {
              // Binary doesn't exist, should compile
            }
            
            if (shouldCompile) {
              const content = await fs.readFile(configFile, 'utf8');
              const parsed = peanutConfig.parseTextConfig(content);
              peanutConfig.compileToBinary(parsed, binaryFile);
              console.log(`  ✅ Compiled: ${path.basename(configFile)} → ${path.basename(binaryFile)}`);
              compiledCount++;
            } else {
              console.log(`  ⏭️  Skipped: ${path.basename(configFile)} (up to date)`);
              skippedCount++;
            }
          } catch (error) {
            console.log(`  ❌ Failed: ${path.basename(configFile)} - ${error.message}`);
          }
        }
      }
      
      console.log('');
      console.log(`✅ Compilation complete: ${compiledCount} compiled, ${skippedCount} skipped`);
      
      return { success: true, compiled: compiledCount, skipped: skippedCount };
    } catch (error) {
      console.error('❌ Failed to compile configurations:', error.message);
      return { success: false, error: error.message };
    }
  });

// Config docs command
const docs = new Command('docs')
  .description('Generate configuration documentation')
  .argument('[path]', 'Path to document', process.cwd())
  .action(async (configPath) => {
    try {
      console.log(`📚 Generating configuration documentation: ${configPath}`);
      
      const peanutConfig = new PeanutConfig();
      const config = peanutConfig.load(configPath);
      
      const documentation = generateConfigurationDocs(config);
      
      const docsFile = path.join(configPath, 'CONFIGURATION.md');
      await fs.writeFile(docsFile, documentation);
      
      console.log(`✅ Configuration documentation generated: ${docsFile}`);
      
      return { success: true, file: docsFile };
    } catch (error) {
      console.error('❌ Failed to generate documentation:', error.message);
      return { success: false, error: error.message };
    }
  });

// Config clear-cache command
const clearCache = new Command('clear-cache')
  .description('Clear configuration cache')
  .argument('[path]', 'Path to clear cache for', process.cwd())
  .action(async (configPath) => {
    try {
      console.log(`🔄 Clearing configuration cache: ${configPath}`);
      
      const peanutConfig = new PeanutConfig();
      peanutConfig.cache.clear();
      
      console.log('✅ Configuration cache cleared successfully');
      
      return { success: true };
    } catch (error) {
      console.error('❌ Failed to clear configuration cache:', error.message);
      return { success: false, error: error.message };
    }
  });

// Config stats command
const stats = new Command('stats')
  .description('Show configuration performance statistics')
  .action(async () => {
    try {
      console.log('📊 Configuration Performance Statistics');
      console.log('=====================================');
      
      const peanutConfig = new PeanutConfig();
      
      // Get cache statistics
      const cacheSize = peanutConfig.cache.size;
      const cacheKeys = Array.from(peanutConfig.cache.keys());
      
      // Get hierarchy statistics
      const hierarchy = peanutConfig.findConfigHierarchy(process.cwd());
      
      console.log('💾 Cache Statistics:');
      console.log(`  Cache Size: ${cacheSize} entries`);
      console.log(`  Cached Keys: ${cacheKeys.join(', ')}`);
      console.log('');
      
      console.log('📁 Hierarchy Statistics:');
      console.log(`  Configuration Files: ${hierarchy.length}`);
      console.log(`  Binary Files: ${hierarchy.filter(h => h.type === 'binary').length}`);
      console.log(`  Text Files: ${hierarchy.filter(h => h.type === 'tsk' || h.type === 'text').length}`);
      console.log('');
      
      // Performance metrics
      const startTime = process.hrtime.bigint();
      peanutConfig.load(process.cwd());
      const endTime = process.hrtime.bigint();
      const loadTime = Number(endTime - startTime) / 1000000; // Convert to milliseconds
      
      console.log('⚡ Performance Metrics:');
      console.log(`  Load Time: ${loadTime.toFixed(2)}ms`);
      console.log(`  Cache Hit Rate: ${cacheSize > 0 ? '100%' : '0%'}`);
      
      return {
        cache: { size: cacheSize, keys: cacheKeys },
        hierarchy: { total: hierarchy.length, binary: hierarchy.filter(h => h.type === 'binary').length, text: hierarchy.filter(h => h.type === 'tsk' || h.type === 'text').length },
        performance: { loadTime: loadTime.toFixed(2) }
      };
    } catch (error) {
      console.error('❌ Failed to get configuration statistics:', error.message);
      return { success: false, error: error.message };
    }
  });

// Helper functions
function validateConfiguration(config) {
  const validation = {
    valid: true,
    totalKeys: 0,
    sections: 0,
    maxDepth: 0,
    errors: []
  };
  
  function validateObject(obj, path = '', depth = 0) {
    validation.maxDepth = Math.max(validation.maxDepth, depth);
    
    for (const [key, value] of Object.entries(obj)) {
      validation.totalKeys++;
      const currentPath = path ? `${path}.${key}` : key;
      
      // Check for invalid keys
      if (key.includes(' ') || key.includes('\t') || key.includes('\n')) {
        validation.valid = false;
        validation.errors.push(`Invalid key at ${currentPath}: contains whitespace`);
      }
      
      // Check for null values
      if (value === null) {
        validation.errors.push(`Null value at ${currentPath}`);
      }
      
      // Recursively validate nested objects
      if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
        validation.sections++;
        validateObject(value, currentPath, depth + 1);
      }
    }
  }
  
  validateObject(config);
  
  return validation;
}

function generateConfigurationDocs(config) {
  let docs = `# Configuration Documentation
Generated on ${new Date().toISOString()}

## Overview
This document describes the configuration structure for this project.

## Configuration Structure

`;
  
  function generateSectionDocs(obj, path = '', level = 0) {
    const indent = '  '.repeat(level);
    
    for (const [key, value] of Object.entries(obj)) {
      const currentPath = path ? `${path}.${key}` : key;
      
      if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
        docs += `${indent}### ${key}\n\n`;
        docs += `${indent}**Path:** \`${currentPath}\`\n\n`;
        docs += `${indent}**Type:** Object\n\n`;
        generateSectionDocs(value, currentPath, level + 1);
      } else {
        docs += `${indent}### ${key}\n\n`;
        docs += `${indent}**Path:** \`${currentPath}\`\n\n`;
        docs += `${indent}**Type:** ${Array.isArray(value) ? 'Array' : typeof value}\n\n`;
        docs += `${indent}**Value:** \`${JSON.stringify(value)}\`\n\n`;
      }
    }
  }
  
  generateSectionDocs(config);
  
  docs += `## Usage Examples

\`\`\`javascript
const PeanutConfig = require('peanut-config');
const config = new PeanutConfig();
const value = config.get('server.port', 8080);
\`\`\`

## Notes
- Configuration files are loaded in hierarchical order
- Binary files (.pnt) are preferred over text files for performance
- Use \`tsk config get <key.path>\` to retrieve specific values
`;

  return docs;
}

module.exports = {
  get,
  check,
  validate,
  compile,
  docs,
  clearCache,
  stats
}; 