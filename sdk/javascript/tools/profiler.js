#!/usr/bin/env node

/**
 * TuskLang Performance Profiler
 * Advanced profiling and optimization tool for TuskLang operations
 */

const fs = require('fs');
const path = require('path');
const { performance } = require('perf_hooks');
const chalk = require('chalk');
const { TuskLang, TuskLangEnhanced, PeanutConfig } = require('../src/index');

class TuskLangProfiler {
  constructor() {
    this.profiles = new Map();
    this.currentProfile = null;
    this.metrics = {
      operations: [],
      memorySnapshots: [],
      performanceMarks: [],
      bottlenecks: []
    };
    this.config = {
      enableMemoryProfiling: true,
      enableDetailedMetrics: true,
      sampleInterval: 100, // ms
      maxProfiles: 10
    };
  }

  /**
   * Start profiling a TuskLang operation
   */
  startProfile(name, options = {}) {
    const profile = {
      id: Date.now().toString(),
      name,
      startTime: performance.now(),
      endTime: null,
      duration: 0,
      memoryStart: process.memoryUsage(),
      memoryEnd: null,
      memoryDelta: null,
      operations: [],
      marks: [],
      errors: [],
      options
    };

    this.currentProfile = profile;
    this.profiles.set(profile.id, profile);

    // Start memory monitoring
    if (this.config.enableMemoryProfiling) {
      this.startMemoryMonitoring(profile.id);
    }

    console.log(chalk.blue.bold(`🚀 Started profiling: ${name}`));
    return profile.id;
  }

  /**
   * End current profile
   */
  endProfile() {
    if (!this.currentProfile) {
      throw new Error('No active profile to end');
    }

    const profile = this.currentProfile;
    profile.endTime = performance.now();
    profile.duration = profile.endTime - profile.startTime;
    profile.memoryEnd = process.memoryUsage();
    profile.memoryDelta = {
      heapUsed: profile.memoryEnd.heapUsed - profile.memoryStart.heapUsed,
      heapTotal: profile.memoryEnd.heapTotal - profile.memoryStart.heapTotal,
      external: profile.memoryEnd.external - profile.memoryStart.external,
      rss: profile.memoryEnd.rss - profile.memoryStart.rss
    };

    // Stop memory monitoring
    if (this.memoryInterval) {
      clearInterval(this.memoryInterval);
      this.memoryInterval = null;
    }

    this.currentProfile = null;
    console.log(chalk.green.bold(`✅ Finished profiling: ${profile.name} (${profile.duration.toFixed(2)}ms)`));
    
    return profile;
  }

  /**
   * Add performance mark
   */
  mark(name, data = {}) {
    if (!this.currentProfile) {
      throw new Error('No active profile to mark');
    }

    const mark = {
      name,
      time: performance.now(),
      data,
      memory: process.memoryUsage()
    };

    this.currentProfile.marks.push(mark);
    console.log(chalk.cyan(`📍 Mark: ${name}`));
  }

  /**
   * Profile TuskLang parsing operation
   */
  async profileParsing(configContent, options = {}) {
    const profileId = this.startProfile('TuskLang Parsing', options);
    
    try {
      this.mark('parse-start');
      
      const tusk = new TuskLangEnhanced();
      const config = tusk.parse(configContent);
      
      this.mark('parse-complete', { sections: Object.keys(config).length });
      
      // Profile validation
      if (options.validate) {
        this.mark('validation-start');
        const validation = tusk.validate(config);
        this.mark('validation-complete', { isValid: validation.isValid });
      }
      
      // Profile stringification
      if (options.stringify) {
        this.mark('stringify-start');
        const stringified = tusk.stringify(config);
        this.mark('stringify-complete', { length: stringified.length });
      }
      
      const profile = this.endProfile();
      return { profile, config };
      
    } catch (error) {
      this.currentProfile.errors.push(error.message);
      this.endProfile();
      throw error;
    }
  }

  /**
   * Profile Peanut binary operations
   */
  async profilePeanutOperations(configContent, options = {}) {
    const profileId = this.startProfile('Peanut Binary Operations', options);
    
    try {
      this.mark('peanut-compile-start');
      
      const peanut = new PeanutConfig();
      const compiled = peanut.compile(configContent);
      
      this.mark('peanut-compile-complete', { size: compiled.length });
      
      // Profile execution
      if (options.execute) {
        this.mark('peanut-execute-start');
        const result = peanut.execute(compiled);
        this.mark('peanut-execute-complete', { sections: Object.keys(result).length });
      }
      
      // Profile validation
      if (options.validate) {
        this.mark('peanut-validate-start');
        const validation = peanut.validate(compiled);
        this.mark('peanut-validate-complete', { isValid: validation.isValid });
      }
      
      const profile = this.endProfile();
      return { profile, compiled };
      
    } catch (error) {
      this.currentProfile.errors.push(error.message);
      this.endProfile();
      throw error;
    }
  }

  /**
   * Profile database operations
   */
  async profileDatabaseOperations(config, options = {}) {
    const profileId = this.startProfile('Database Operations', options);
    
    try {
      const tusk = new TuskLangEnhanced();
      
      // Set up database adapter
      if (options.databaseAdapter) {
        this.mark('adapter-setup-start');
        tusk.setDatabaseAdapter(options.databaseAdapter);
        this.mark('adapter-setup-complete');
      }
      
      // Profile variable resolution with database
      if (options.resolveVariables) {
        this.mark('variable-resolution-start');
        const resolved = await tusk.resolveVariables(config);
        this.mark('variable-resolution-complete', { 
          variables: Object.keys(resolved.variables || {}).length 
        });
      }
      
      // Profile database queries
      if (options.runQueries) {
        this.mark('database-queries-start');
        const queries = this.extractDatabaseQueries(config);
        
        for (const query of queries) {
          this.mark(`query-${query.name}-start`);
          // Simulate query execution
          await new Promise(resolve => setTimeout(resolve, Math.random() * 10));
          this.mark(`query-${query.name}-complete`);
        }
        
        this.mark('database-queries-complete', { queryCount: queries.length });
      }
      
      const profile = this.endProfile();
      return { profile };
      
    } catch (error) {
      this.currentProfile.errors.push(error.message);
      this.endProfile();
      throw error;
    }
  }

  /**
   * Start memory monitoring
   */
  startMemoryMonitoring(profileId) {
    this.memoryInterval = setInterval(() => {
      if (this.currentProfile && this.currentProfile.id === profileId) {
        const memory = process.memoryUsage();
        this.currentProfile.operations.push({
          type: 'memory-snapshot',
          time: performance.now(),
          memory
        });
      }
    }, this.config.sampleInterval);
  }

  /**
   * Extract database queries from configuration
   */
  extractDatabaseQueries(config) {
    const queries = [];
    
    const extractQueries = (obj, path = '') => {
      if (typeof obj === 'object' && obj !== null) {
        Object.entries(obj).forEach(([key, value]) => {
          const currentPath = path ? `${path}.${key}` : key;
          
          if (typeof value === 'string' && value.toLowerCase().includes('select')) {
            queries.push({
              name: currentPath,
              query: value,
              type: 'select'
            });
          } else if (typeof value === 'string' && value.toLowerCase().includes('insert')) {
            queries.push({
              name: currentPath,
              query: value,
              type: 'insert'
            });
          } else if (typeof value === 'string' && value.toLowerCase().includes('update')) {
            queries.push({
              name: currentPath,
              query: value,
              type: 'update'
            });
          } else if (typeof value === 'string' && value.toLowerCase().includes('delete')) {
            queries.push({
              name: currentPath,
              query: value,
              type: 'delete'
            });
          }
          
          extractQueries(value, currentPath);
        });
      }
    };
    
    extractQueries(config);
    return queries;
  }

  /**
   * Generate comprehensive performance report
   */
  generateReport(profileId = null) {
    console.log(chalk.blue.bold('📊 Performance Report'));
    console.log(chalk.gray('=' * 50));
    
    const profiles = profileId ? [this.profiles.get(profileId)] : Array.from(this.profiles.values());
    
    profiles.forEach(profile => {
      if (!profile) return;
      
      console.log(chalk.yellow.bold(`\n🔍 Profile: ${profile.name}`));
      console.log(chalk.gray(`ID: ${profile.id}`));
      console.log(chalk.gray(`Duration: ${profile.duration.toFixed(2)}ms`));
      
      // Memory analysis
      if (profile.memoryDelta) {
        console.log(chalk.cyan('\n💾 Memory Analysis:'));
        console.log(`  Heap Used: ${(profile.memoryDelta.heapUsed / 1024 / 1024).toFixed(2)} MB`);
        console.log(`  Heap Total: ${(profile.memoryDelta.heapTotal / 1024 / 1024).toFixed(2)} MB`);
        console.log(`  External: ${(profile.memoryDelta.external / 1024 / 1024).toFixed(2)} MB`);
        console.log(`  RSS: ${(profile.memoryDelta.rss / 1024 / 1024).toFixed(2)} MB`);
      }
      
      // Performance marks
      if (profile.marks.length > 0) {
        console.log(chalk.magenta('\n📍 Performance Marks:'));
        profile.marks.forEach((mark, index) => {
          const timeFromStart = mark.time - profile.startTime;
          console.log(`  ${index + 1}. ${mark.name}: ${timeFromStart.toFixed(2)}ms`);
          if (mark.data && Object.keys(mark.data).length > 0) {
            console.log(`     Data: ${JSON.stringify(mark.data)}`);
          }
        });
      }
      
      // Errors
      if (profile.errors.length > 0) {
        console.log(chalk.red('\n❌ Errors:'));
        profile.errors.forEach(error => {
          console.log(`  - ${error}`);
        });
      }
      
      // Performance analysis
      this.analyzePerformance(profile);
    });
    
    // Overall analysis
    this.generateOverallAnalysis();
  }

  /**
   * Analyze performance of a specific profile
   */
  analyzePerformance(profile) {
    console.log(chalk.green('\n💡 Performance Analysis:'));
    
    const analysis = [];
    
    // Duration analysis
    if (profile.duration > 1000) {
      analysis.push('Slow operation detected (>1s)');
    } else if (profile.duration > 100) {
      analysis.push('Moderate operation time (100ms-1s)');
    } else {
      analysis.push('Fast operation (<100ms)');
    }
    
    // Memory analysis
    if (profile.memoryDelta && profile.memoryDelta.heapUsed > 50 * 1024 * 1024) {
      analysis.push('High memory usage detected (>50MB)');
    }
    
    // Mark analysis
    if (profile.marks.length > 0) {
      const markDurations = [];
      for (let i = 1; i < profile.marks.length; i++) {
        const duration = profile.marks[i].time - profile.marks[i-1].time;
        markDurations.push({ name: profile.marks[i].name, duration });
      }
      
      const slowestMark = markDurations.reduce((max, current) => 
        current.duration > max.duration ? current : max
      );
      
      if (slowestMark.duration > 50) {
        analysis.push(`Slowest operation: ${slowestMark.name} (${slowestMark.duration.toFixed(2)}ms)`);
      }
    }
    
    analysis.forEach(item => console.log(`  - ${item}`));
  }

  /**
   * Generate overall analysis across all profiles
   */
  generateOverallAnalysis() {
    const profiles = Array.from(this.profiles.values());
    if (profiles.length === 0) return;
    
    console.log(chalk.blue.bold('\n📈 Overall Analysis:'));
    
    const totalDuration = profiles.reduce((sum, p) => sum + p.duration, 0);
    const avgDuration = totalDuration / profiles.length;
    const slowestProfile = profiles.reduce((max, p) => p.duration > max.duration ? p : max);
    const fastestProfile = profiles.reduce((min, p) => p.duration < min.duration ? p : min);
    
    console.log(chalk.cyan('  Performance Summary:'));
    console.log(`    Total Profiles: ${profiles.length}`);
    console.log(`    Total Duration: ${totalDuration.toFixed(2)}ms`);
    console.log(`    Average Duration: ${avgDuration.toFixed(2)}ms`);
    console.log(`    Slowest: ${slowestProfile.name} (${slowestProfile.duration.toFixed(2)}ms)`);
    console.log(`    Fastest: ${fastestProfile.name} (${fastestProfile.duration.toFixed(2)}ms)`);
    
    // Recommendations
    this.generateOptimizationRecommendations(profiles);
  }

  /**
   * Generate optimization recommendations
   */
  generateOptimizationRecommendations(profiles) {
    console.log(chalk.green('\n🚀 Optimization Recommendations:'));
    
    const recommendations = [];
    
    // Check for slow operations
    const slowProfiles = profiles.filter(p => p.duration > 100);
    if (slowProfiles.length > 0) {
      recommendations.push(`Consider optimizing ${slowProfiles.length} slow operations`);
    }
    
    // Check for memory issues
    const memoryProfiles = profiles.filter(p => 
      p.memoryDelta && p.memoryDelta.heapUsed > 10 * 1024 * 1024
    );
    if (memoryProfiles.length > 0) {
      recommendations.push(`Monitor memory usage in ${memoryProfiles.length} operations`);
    }
    
    // Check for parsing performance
    const parsingProfiles = profiles.filter(p => p.name.includes('Parsing'));
    if (parsingProfiles.length > 0) {
      const avgParsingTime = parsingProfiles.reduce((sum, p) => sum + p.duration, 0) / parsingProfiles.length;
      if (avgParsingTime > 50) {
        recommendations.push('Consider using binary compilation for faster parsing');
      }
    }
    
    if (recommendations.length > 0) {
      recommendations.forEach(rec => console.log(`  - ${rec}`));
    } else {
      console.log('  - All operations are well-optimized');
    }
  }

  /**
   * Export profile data to file
   */
  exportProfile(profileId, outputPath) {
    const profile = this.profiles.get(profileId);
    if (!profile) {
      throw new Error(`Profile ${profileId} not found`);
    }
    
    const exportData = {
      profile,
      summary: {
        duration: profile.duration,
        memoryDelta: profile.memoryDelta,
        markCount: profile.marks.length,
        errorCount: profile.errors.length
      },
      timestamp: new Date().toISOString()
    };
    
    fs.writeFileSync(outputPath, JSON.stringify(exportData, null, 2));
    console.log(chalk.green(`📊 Profile exported to: ${outputPath}`));
  }

  /**
   * Clear all profiles
   */
  clearProfiles() {
    this.profiles.clear();
    console.log(chalk.yellow('🗑️  All profiles cleared'));
  }

  /**
   * Get profile by ID
   */
  getProfile(profileId) {
    return this.profiles.get(profileId);
  }

  /**
   * List all profiles
   */
  listProfiles() {
    console.log(chalk.blue.bold('📋 Available Profiles:'));
    
    if (this.profiles.size === 0) {
      console.log(chalk.gray('  No profiles available'));
      return;
    }
    
    Array.from(this.profiles.values()).forEach(profile => {
      const status = profile.endTime ? '✅' : '🔄';
      console.log(`  ${status} ${profile.name} (${profile.duration ? profile.duration.toFixed(2) + 'ms' : 'running'})`);
    });
  }
}

// CLI interface
if (require.main === module) {
  const args = process.argv.slice(2);
  const profiler = new TuskLangProfiler();
  
  if (args.length === 0) {
    console.log(chalk.red('Usage: node profiler.js <command> [options]'));
    console.log(chalk.gray('Commands:'));
    console.log(chalk.gray('  parse <file>     Profile parsing operation'));
    console.log(chalk.gray('  peanut <file>    Profile Peanut operations'));
    console.log(chalk.gray('  db <file>        Profile database operations'));
    console.log(chalk.gray('  report [id]      Generate performance report'));
    console.log(chalk.gray('  list             List all profiles'));
    console.log(chalk.gray('  clear            Clear all profiles'));
    process.exit(1);
  }
  
  const command = args[0];
  
  switch (command) {
    case 'parse':
      if (args.length < 2) {
        console.log(chalk.red('Usage: node profiler.js parse <config-file>'));
        process.exit(1);
      }
      const configContent = fs.readFileSync(args[1], 'utf8');
      profiler.profileParsing(configContent, { validate: true, stringify: true });
      break;
      
    case 'peanut':
      if (args.length < 2) {
        console.log(chalk.red('Usage: node profiler.js peanut <config-file>'));
        process.exit(1);
      }
      const peanutContent = fs.readFileSync(args[1], 'utf8');
      profiler.profilePeanutOperations(peanutContent, { execute: true, validate: true });
      break;
      
    case 'db':
      if (args.length < 2) {
        console.log(chalk.red('Usage: node profiler.js db <config-file>'));
        process.exit(1);
      }
      const dbContent = fs.readFileSync(args[1], 'utf8');
      const tusk = new TuskLangEnhanced();
      const dbConfig = tusk.parse(dbContent);
      profiler.profileDatabaseOperations(dbConfig, { resolveVariables: true, runQueries: true });
      break;
      
    case 'report':
      profiler.generateReport(args[1]);
      break;
      
    case 'list':
      profiler.listProfiles();
      break;
      
    case 'clear':
      profiler.clearProfiles();
      break;
      
    default:
      console.log(chalk.red(`Unknown command: ${command}`));
      process.exit(1);
  }
}

module.exports = TuskLangProfiler; 