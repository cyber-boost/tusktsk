#!/usr/bin/env node

/**
 * TuskLang Configuration Debugger
 * Advanced debugging and profiling tool for TuskLang configurations
 */

const fs = require('fs');
const path = require('path');
const { performance } = require('perf_hooks');
const chalk = require('chalk');
const { TuskLang, TuskLangEnhanced } = require('../src/index');

class TuskLangDebugger {
  constructor() {
    this.metrics = {
      parseTime: 0,
      memoryUsage: 0,
      configSize: 0,
      sections: 0,
      variables: 0,
      errors: []
    };
    this.breakpoints = new Set();
    this.watchMode = false;
    this.verbose = false;
  }

  /**
   * Debug a TuskLang configuration file
   */
  async debug(configPath, options = {}) {
    const startTime = performance.now();
    
    console.log(chalk.blue.bold('🔍 TuskLang Configuration Debugger'));
    console.log(chalk.gray(`Analyzing: ${configPath}\n`));

    try {
      // Read and analyze file
      const configContent = fs.readFileSync(configPath, 'utf8');
      this.metrics.configSize = Buffer.byteLength(configContent, 'utf8');
      
      // Parse configuration
      const tusk = new TuskLangEnhanced();
      const parseStart = performance.now();
      const config = tusk.parse(configContent);
      this.metrics.parseTime = performance.now() - parseStart;
      
      // Collect metrics
      this.collectMetrics(config, configContent);
      
      // Run analysis
      await this.analyzeConfiguration(config, configPath, options);
      
      // Generate report
      this.generateReport();
      
      // Watch mode
      if (options.watch) {
        this.watchMode = true;
        this.watchFile(configPath);
      }
      
    } catch (error) {
      this.metrics.errors.push(error.message);
      console.error(chalk.red.bold('❌ Debug Error:'), error.message);
      process.exit(1);
    }
  }

  /**
   * Collect detailed metrics from configuration
   */
  collectMetrics(config, content) {
    // Count sections
    this.metrics.sections = Object.keys(config).length;
    
    // Count variables
    this.metrics.variables = this.countVariables(config);
    
    // Memory usage
    this.metrics.memoryUsage = process.memoryUsage().heapUsed;
    
    // Performance analysis
    this.analyzePerformance(config, content);
  }

  /**
   * Count variables in configuration
   */
  countVariables(config) {
    let count = 0;
    const countInValue = (value) => {
      if (typeof value === 'string' && value.includes('$')) {
        count += (value.match(/\$/g) || []).length;
      } else if (typeof value === 'object' && value !== null) {
        Object.values(value).forEach(countInValue);
      }
    };
    
    Object.values(config).forEach(countInValue);
    return count;
  }

  /**
   * Analyze performance characteristics
   */
  analyzePerformance(config, content) {
    // Check for potential performance issues
    const issues = [];
    
    // Large configuration files
    if (this.metrics.configSize > 1024 * 1024) { // 1MB
      issues.push('Large configuration file detected (>1MB)');
    }
    
    // Deep nesting
    const maxDepth = this.getMaxDepth(config);
    if (maxDepth > 10) {
      issues.push(`Deep nesting detected (${maxDepth} levels)`);
    }
    
    // Complex variable resolution
    if (this.metrics.variables > 100) {
      issues.push('High variable count detected (>100)');
    }
    
    // Slow parsing
    if (this.metrics.parseTime > 100) { // 100ms
      issues.push('Slow parsing detected (>100ms)');
    }
    
    this.metrics.performanceIssues = issues;
  }

  /**
   * Get maximum nesting depth
   */
  getMaxDepth(obj, depth = 1) {
    let maxDepth = depth;
    
    if (typeof obj === 'object' && obj !== null) {
      Object.values(obj).forEach(value => {
        const currentDepth = this.getMaxDepth(value, depth + 1);
        maxDepth = Math.max(maxDepth, currentDepth);
      });
    }
    
    return maxDepth;
  }

  /**
   * Analyze configuration structure and content
   */
  async analyzeConfiguration(config, configPath, options) {
    console.log(chalk.yellow.bold('📊 Configuration Analysis:'));
    
    // Structure analysis
    this.analyzeStructure(config);
    
    // Security analysis
    if (options.security) {
      await this.securityAnalysis(config);
    }
    
    // Database analysis
    if (options.database) {
      await this.databaseAnalysis(config);
    }
    
    // Validation
    if (options.validate) {
      await this.validateConfiguration(config);
    }
    
    console.log('');
  }

  /**
   * Analyze configuration structure
   */
  analyzeStructure(config) {
    console.log(chalk.cyan('  📁 Structure:'));
    
    const sections = Object.keys(config);
    console.log(`    Sections: ${sections.length}`);
    console.log(`    Variables: ${this.metrics.variables}`);
    console.log(`    Max Depth: ${this.getMaxDepth(config)}`);
    
    // Section breakdown
    sections.forEach(section => {
      const sectionData = config[section];
      const keys = typeof sectionData === 'object' ? Object.keys(sectionData).length : 1;
      console.log(`    └─ ${section}: ${keys} keys`);
    });
  }

  /**
   * Security analysis
   */
  async securityAnalysis(config) {
    console.log(chalk.red('  🔒 Security Analysis:'));
    
    const issues = [];
    
    // Check for sensitive data
    const sensitivePatterns = [
      /password/i,
      /secret/i,
      /key/i,
      /token/i,
      /credential/i
    ];
    
    const checkSensitive = (obj, path = '') => {
      if (typeof obj === 'object' && obj !== null) {
        Object.entries(obj).forEach(([key, value]) => {
          const currentPath = path ? `${path}.${key}` : key;
          
          // Check key names
          sensitivePatterns.forEach(pattern => {
            if (pattern.test(key)) {
              issues.push(`Potential sensitive key: ${currentPath}`);
            }
          });
          
          // Check values
          if (typeof value === 'string' && value.length > 50) {
            issues.push(`Long value detected: ${currentPath} (${value.length} chars)`);
          }
          
          checkSensitive(value, currentPath);
        });
      }
    };
    
    checkSensitive(config);
    
    if (issues.length > 0) {
      issues.forEach(issue => console.log(`    ⚠️  ${issue}`));
    } else {
      console.log('    ✅ No security issues detected');
    }
  }

  /**
   * Database analysis
   */
  async databaseAnalysis(config) {
    console.log(chalk.green('  🗄️  Database Analysis:'));
    
    // Check for database configurations
    const dbSections = ['database', 'db', 'postgres', 'mysql', 'sqlite', 'mongodb'];
    const found = dbSections.filter(section => config[section]);
    
    if (found.length > 0) {
      console.log(`    Found database sections: ${found.join(', ')}`);
      
      found.forEach(section => {
        const dbConfig = config[section];
        console.log(`    └─ ${section}:`);
        
        if (dbConfig.host) console.log(`      Host: ${dbConfig.host}`);
        if (dbConfig.port) console.log(`      Port: ${dbConfig.port}`);
        if (dbConfig.database) console.log(`      Database: ${dbConfig.database}`);
        if (dbConfig.user) console.log(`      User: ${dbConfig.user}`);
      });
    } else {
      console.log('    No database configurations found');
    }
  }

  /**
   * Validate configuration
   */
  async validateConfiguration(config) {
    console.log(chalk.magenta('  ✅ Validation:'));
    
    try {
      const tusk = new TuskLangEnhanced();
      const validation = tusk.validate(config);
      
      if (validation.isValid) {
        console.log('    ✅ Configuration is valid');
      } else {
        console.log('    ❌ Configuration validation failed:');
        validation.errors.forEach(error => {
          console.log(`      - ${error}`);
        });
      }
    } catch (error) {
      console.log(`    ❌ Validation error: ${error.message}`);
    }
  }

  /**
   * Generate comprehensive report
   */
  generateReport() {
    console.log(chalk.blue.bold('📈 Performance Report:'));
    
    // Basic metrics
    console.log(chalk.cyan('  Metrics:'));
    console.log(`    File Size: ${(this.metrics.configSize / 1024).toFixed(2)} KB`);
    console.log(`    Parse Time: ${this.metrics.parseTime.toFixed(2)}ms`);
    console.log(`    Memory Usage: ${(this.metrics.memoryUsage / 1024 / 1024).toFixed(2)} MB`);
    console.log(`    Sections: ${this.metrics.sections}`);
    console.log(`    Variables: ${this.metrics.variables}`);
    
    // Performance issues
    if (this.metrics.performanceIssues && this.metrics.performanceIssues.length > 0) {
      console.log(chalk.yellow('  ⚠️  Performance Issues:'));
      this.metrics.performanceIssues.forEach(issue => {
        console.log(`    - ${issue}`);
      });
    }
    
    // Recommendations
    this.generateRecommendations();
  }

  /**
   * Generate optimization recommendations
   */
  generateRecommendations() {
    console.log(chalk.green('  💡 Recommendations:'));
    
    const recommendations = [];
    
    if (this.metrics.parseTime > 50) {
      recommendations.push('Consider using binary compilation for faster parsing');
    }
    
    if (this.metrics.variables > 50) {
      recommendations.push('Consider caching variable resolution for better performance');
    }
    
    if (this.metrics.configSize > 512 * 1024) { // 512KB
      recommendations.push('Consider splitting large configuration into multiple files');
    }
    
    if (this.getMaxDepth({}) > 8) {
      recommendations.push('Consider flattening deeply nested configurations');
    }
    
    if (recommendations.length > 0) {
      recommendations.forEach(rec => console.log(`    - ${rec}`));
    } else {
      console.log('    - Configuration is well-optimized');
    }
  }

  /**
   * Watch file for changes
   */
  watchFile(configPath) {
    console.log(chalk.blue.bold('\n👀 Watch Mode Active'));
    console.log(chalk.gray('Press Ctrl+C to stop watching\n'));
    
    fs.watch(configPath, (eventType, filename) => {
      if (eventType === 'change') {
        console.log(chalk.yellow(`\n🔄 File changed: ${filename}`));
        console.log(chalk.gray('Re-analyzing...\n'));
        
        // Clear console and re-run analysis
        console.clear();
        this.debug(configPath, { watch: false });
      }
    });
  }

  /**
   * Set breakpoint for debugging
   */
  setBreakpoint(condition) {
    this.breakpoints.add(condition);
  }

  /**
   * Clear all breakpoints
   */
  clearBreakpoints() {
    this.breakpoints.clear();
  }

  /**
   * Export metrics to file
   */
  exportMetrics(outputPath) {
    const report = {
      timestamp: new Date().toISOString(),
      metrics: this.metrics,
      recommendations: this.generateRecommendations()
    };
    
    fs.writeFileSync(outputPath, JSON.stringify(report, null, 2));
    console.log(chalk.green(`📊 Metrics exported to: ${outputPath}`));
  }
}

// CLI interface
if (require.main === module) {
  const args = process.argv.slice(2);
  const debugger = new TuskLangDebugger();
  
  if (args.length === 0) {
    console.log(chalk.red('Usage: node debugger.js <config-file> [options]'));
    console.log(chalk.gray('Options:'));
    console.log(chalk.gray('  --watch     Watch file for changes'));
    console.log(chalk.gray('  --security  Run security analysis'));
    console.log(chalk.gray('  --database  Run database analysis'));
    console.log(chalk.gray('  --validate  Validate configuration'));
    console.log(chalk.gray('  --export    Export metrics to file'));
    process.exit(1);
  }
  
  const configPath = args[0];
  const options = {
    watch: args.includes('--watch'),
    security: args.includes('--security'),
    database: args.includes('--database'),
    validate: args.includes('--validate'),
    export: args.includes('--export')
  };
  
  debugger.debug(configPath, options);
}

module.exports = TuskLangDebugger; 