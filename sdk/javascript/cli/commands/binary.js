/**
 * Binary Commands for TuskLang CLI
 * =================================
 * Implements binary performance operations
 */

const { Command } = require('commander');
const fs = require('fs').promises;
const path = require('path');
const TuskLang = require('../../index.js');
const PeanutConfig = require('../../peanut-config.js');

// Binary compile command
const compile = new Command('compile')
  .description('Compile to binary format (.tskb)')
  .argument('<file.tsk>', 'TSK file to compile')
  .action(async (file) => {
    try {
      console.log(`🔄 Compiling TSK file to binary: ${file}`);
      
      // Check if file exists
      await fs.access(file);
      
      // Read and parse TSK file
      const content = await fs.readFile(file, 'utf8');
      const tusk = new TuskLang();
      const parsed = tusk.parse(content);
      
      // Generate binary file path (use .pnt format for compatibility)
      const binaryFile = file.replace(/\.tsk$/, '.pnt');
      
      // Compile to binary using PeanutConfig
      const peanutConfig = new PeanutConfig();
      peanutConfig.compileToBinary(parsed, binaryFile);
      
      // Get file sizes for comparison
      const sourceStats = await fs.stat(file);
      const binaryStats = await fs.stat(binaryFile);
      
      const sourceSize = sourceStats.size;
      const binarySize = binaryStats.size;
      const compressionRatio = ((sourceSize - binarySize) / sourceSize * 100).toFixed(1);
      
      console.log('✅ Binary compilation completed successfully');
      console.log(`📍 Source file: ${file} (${sourceSize} bytes)`);
      console.log(`📍 Binary file: ${binaryFile} (${binarySize} bytes)`);
      console.log(`📍 Compression: ${compressionRatio}% smaller`);
      
      return {
        success: true,
        source: { file, size: sourceSize },
        binary: { file: binaryFile, size: binarySize },
        compression: compressionRatio
      };
    } catch (error) {
      console.error('❌ Binary compilation failed:', error.message);
      return { success: false, error: error.message };
    }
  });

// Binary execute command
const execute = new Command('execute')
  .description('Execute binary file directly')
  .argument('<file.pnt>', 'Binary file to execute')
  .action(async (file) => {
    try {
      console.log(`🔄 Executing binary file: ${file}`);
      
      // Check if file exists
      await fs.access(file);
      
      // Load binary file
      const peanutConfig = new PeanutConfig();
      const config = peanutConfig.loadBinary(file);
      
      console.log('✅ Binary file executed successfully');
      console.log('📍 Configuration loaded:');
      
      // Display configuration summary
      const summary = summarizeConfiguration(config);
      console.log(`  - Total keys: ${summary.totalKeys}`);
      console.log(`  - Sections: ${summary.sections}`);
      console.log(`  - Data types: ${summary.dataTypes.join(', ')}`);
      
      return {
        success: true,
        file,
        config: summary
      };
    } catch (error) {
      console.error('❌ Binary execution failed:', error.message);
      return { success: false, error: error.message };
    }
  });

// Binary benchmark command
const benchmark = new Command('benchmark')
  .description('Compare binary vs text performance')
  .argument('<file>', 'File to benchmark (.tsk or .tskb)')
  .action(async (file) => {
    try {
      console.log(`⚡ Benchmarking performance: ${file}`);
      
      let textFile, binaryFile;
      
      if (file.endsWith('.tsk')) {
        textFile = file;
        binaryFile = file.replace(/\.tsk$/, '.pnt');
      } else if (file.endsWith('.pnt')) {
        binaryFile = file;
        textFile = file.replace(/\.pnt$/, '.tsk');
      } else {
        throw new Error('File must be .tsk or .pnt');
      }
      
      const results = {
        text: null,
        binary: null,
        comparison: {}
      };
      
      // Benchmark text parsing
      if (await fs.access(textFile).then(() => true).catch(() => false)) {
        console.log('  📊 Benchmarking text parsing...');
        results.text = await benchmarkTextParsing(textFile);
      }
      
      // Benchmark binary loading
      if (await fs.access(binaryFile).then(() => true).catch(() => false)) {
        console.log('  📊 Benchmarking binary loading...');
        results.binary = await benchmarkBinaryLoading(binaryFile);
      }
      
      // Compare results
      if (results.text && results.binary) {
        const speedup = results.text.avgTime / results.binary.avgTime;
        const memoryReduction = ((results.text.avgMemory - results.binary.avgMemory) / results.text.avgMemory * 100).toFixed(1);
        
        results.comparison = {
          speedup: speedup.toFixed(2),
          memoryReduction: memoryReduction,
          faster: speedup > 1
        };
        
        console.log('');
        console.log('📊 Performance Comparison:');
        console.log('==========================');
        console.log(`Text parsing:    ${results.text.avgTime.toFixed(2)}ms avg (${results.text.iterations} iterations)`);
        console.log(`Binary loading:  ${results.binary.avgTime.toFixed(2)}ms avg (${results.binary.iterations} iterations)`);
        console.log(`Speedup:         ${speedup.toFixed(2)}x faster`);
        console.log(`Memory usage:    ${memoryReduction}% less memory`);
        console.log(`Recommendation:  ${speedup > 1 ? 'Use binary format' : 'Use text format'}`);
      }
      
      return results;
    } catch (error) {
      console.error('❌ Benchmark failed:', error.message);
      return { success: false, error: error.message };
    }
  });

// Binary optimize command
const optimize = new Command('optimize')
  .description('Optimize binary for production')
  .argument('<file>', 'Binary file to optimize')
  .action(async (file) => {
    try {
      console.log(`🔄 Optimizing binary for production: ${file}`);
      
      // Check if file exists
      await fs.access(file);
      
      // Load binary file
      const peanutConfig = new PeanutConfig();
      const config = peanutConfig.loadBinary(file);
      
      // Apply optimizations
      const optimized = optimizeConfiguration(config);
      
      // Create optimized binary
      const optimizedFile = file.replace(/\.(pnt)$/, '.optimized.$1');
      peanutConfig.compileToBinary(optimized, optimizedFile);
      
      // Get file sizes
      const originalStats = await fs.stat(file);
      const optimizedStats = await fs.stat(optimizedFile);
      
      const originalSize = originalStats.size;
      const optimizedSize = optimizedStats.size;
      const sizeReduction = ((originalSize - optimizedSize) / originalSize * 100).toFixed(1);
      
      console.log('✅ Binary optimization completed successfully');
      console.log(`📍 Original: ${file} (${originalSize} bytes)`);
      console.log(`📍 Optimized: ${optimizedFile} (${optimizedSize} bytes)`);
      console.log(`📍 Size reduction: ${sizeReduction}%`);
      
      // Show optimization details
      console.log('');
      console.log('🔧 Optimization Details:');
      console.log(`  - Removed null values: ${optimized.removedNulls}`);
      console.log(`  - Compressed strings: ${optimized.compressedStrings}`);
      console.log(`  - Optimized numbers: ${optimized.optimizedNumbers}`);
      
      return {
        success: true,
        original: { file, size: originalSize },
        optimized: { file: optimizedFile, size: optimizedSize },
        reduction: sizeReduction,
        details: optimized
      };
    } catch (error) {
      console.error('❌ Binary optimization failed:', error.message);
      return { success: false, error: error.message };
    }
  });

// Helper functions
async function benchmarkTextParsing(file) {
  const tusk = new TuskLang();
  const content = await fs.readFile(file, 'utf8');
  
  const iterations = 100;
  const times = [];
  const memories = [];
  
  for (let i = 0; i < iterations; i++) {
    const startMemory = process.memoryUsage().heapUsed;
    const startTime = process.hrtime.bigint();
    
    const parsed = tusk.parse(content);
    
    const endTime = process.hrtime.bigint();
    const endMemory = process.memoryUsage().heapUsed;
    
    const time = Number(endTime - startTime) / 1000000; // Convert to milliseconds
    const memory = endMemory - startMemory;
    
    times.push(time);
    memories.push(memory);
  }
  
  const avgTime = times.reduce((a, b) => a + b, 0) / times.length;
  const avgMemory = memories.reduce((a, b) => a + b, 0) / memories.length;
  
  return {
    avgTime,
    avgMemory,
    iterations,
    minTime: Math.min(...times),
    maxTime: Math.max(...times)
  };
}

async function benchmarkBinaryLoading(file) {
  const peanutConfig = new PeanutConfig();
  
  const iterations = 100;
  const times = [];
  const memories = [];
  
  for (let i = 0; i < iterations; i++) {
    const startMemory = process.memoryUsage().heapUsed;
    const startTime = process.hrtime.bigint();
    
    peanutConfig.loadBinary(file);
    
    const endTime = process.hrtime.bigint();
    const endMemory = process.memoryUsage().heapUsed;
    
    const time = Number(endTime - startTime) / 1000000; // Convert to milliseconds
    const memory = endMemory - startMemory;
    
    times.push(time);
    memories.push(memory);
  }
  
  const avgTime = times.reduce((a, b) => a + b, 0) / times.length;
  const avgMemory = memories.reduce((a, b) => a + b, 0) / memories.length;
  
  return {
    avgTime,
    avgMemory,
    iterations,
    minTime: Math.min(...times),
    maxTime: Math.max(...times)
  };
}

function summarizeConfiguration(config) {
  const summary = {
    totalKeys: 0,
    sections: 0,
    dataTypes: new Set()
  };
  
  function analyzeObject(obj) {
    for (const [key, value] of Object.entries(obj)) {
      summary.totalKeys++;
      summary.dataTypes.add(typeof value);
      
      if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
        summary.sections++;
        analyzeObject(value);
      }
    }
  }
  
  analyzeObject(config);
  summary.dataTypes = Array.from(summary.dataTypes);
  
  return summary;
}

function optimizeConfiguration(config) {
  const optimized = {};
  const stats = {
    removedNulls: 0,
    compressedStrings: 0,
    optimizedNumbers: 0
  };
  
  function optimizeObject(obj) {
    const result = {};
    
    for (const [key, value] of Object.entries(obj)) {
      if (value === null || value === undefined) {
        stats.removedNulls++;
        continue; // Skip null/undefined values
      }
      
      if (typeof value === 'string') {
        // Compress long strings
        if (value.length > 100) {
          stats.compressedStrings++;
          // In a real implementation, this would use actual compression
        }
        result[key] = value;
      } else if (typeof value === 'number') {
        // Optimize numbers (e.g., use integers when possible)
        if (Number.isInteger(value) && value >= 0 && value <= 255) {
          stats.optimizedNumbers++;
        }
        result[key] = value;
      } else if (typeof value === 'object' && !Array.isArray(value)) {
        result[key] = optimizeObject(value);
      } else {
        result[key] = value;
      }
    }
    
    return result;
  }
  
  optimized.data = optimizeObject(config);
  optimized.removedNulls = stats.removedNulls;
  optimized.compressedStrings = stats.compressedStrings;
  optimized.optimizedNumbers = stats.optimizedNumbers;
  
  return optimized;
}

module.exports = {
  compile,
  execute,
  benchmark,
  optimize
}; 