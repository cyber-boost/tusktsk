#!/usr/bin/env node
/**
 * Peanuts Commands for TuskLang CLI
 * ==================================
 * Peanut configuration management commands
 * 
 * Commands:
 * - compile: Compile .peanuts/.tsk files to .pnt format
 * - auto-compile: Automatically compile all files in directory
 * - load: Load and validate peanut files
 */

const { Command } = require('commander');
const fs = require('fs').promises;
const path = require('path');
const PeanutConfig = require('../../peanut-config.js');

// PeanutConfig instance
const peanutConfig = new PeanutConfig();

/**
 * Compile .peanuts/.tsk file to .pnt format
 */
async function compilePeanut(inputFile, outputFile = null) {
  try {
    // Validate input file
    if (!inputFile) {
      throw new Error('Input file is required');
    }

    const inputPath = path.resolve(inputFile);
    const inputExt = path.extname(inputPath);
    
    if (!['.peanuts', '.tsk'].includes(inputExt)) {
      throw new Error('Input file must have .peanuts or .tsk extension');
    }

    // Generate output filename if not provided
    if (!outputFile) {
      const baseName = path.basename(inputPath, inputExt);
      const outputPath = path.join(path.dirname(inputPath), `${baseName}.pnt`);
      outputFile = outputPath;
    }

    // Read and parse input file
    const content = await fs.readFile(inputPath, 'utf8');
    
    // Compile using PeanutConfig
    const compiled = await peanutConfig.compile(content, {
      sourceFile: inputPath,
      targetFile: outputFile,
      format: 'pnt'
    });

    // Write compiled output
    await fs.writeFile(outputFile, compiled, 'utf8');

    return {
      success: true,
      input: inputPath,
      output: outputFile,
      size: compiled.length,
      message: `✅ Successfully compiled ${inputPath} to ${outputFile}`
    };

  } catch (error) {
    return {
      success: false,
      error: error.message,
      message: `❌ Failed to compile ${inputFile}: ${error.message}`
    };
  }
}

/**
 * Auto-compile all .peanuts/.tsk files in directory
 */
async function autoCompileDirectory(directory) {
  try {
    const dirPath = path.resolve(directory);
    
    // Check if directory exists
    const stats = await fs.stat(dirPath);
    if (!stats.isDirectory()) {
      throw new Error(`${directory} is not a directory`);
    }

    // Find all .peanuts and .tsk files
    const files = await fs.readdir(dirPath);
    const targetFiles = files.filter(file => {
      const ext = path.extname(file);
      return ['.peanuts', '.tsk'].includes(ext);
    });

    if (targetFiles.length === 0) {
      return {
        success: true,
        message: `📍 No .peanuts or .tsk files found in ${directory}`,
        processed: 0
      };
    }

    // Compile each file
    const results = [];
    let successCount = 0;
    let errorCount = 0;

    for (const file of targetFiles) {
      const inputPath = path.join(dirPath, file);
      const result = await compilePeanut(inputPath);
      
      results.push(result);
      if (result.success) {
        successCount++;
      } else {
        errorCount++;
      }
    }

    return {
      success: true,
      message: `✅ Auto-compiled ${successCount} files, ${errorCount} errors`,
      processed: targetFiles.length,
      successCount,
      errorCount,
      results
    };

  } catch (error) {
    return {
      success: false,
      error: error.message,
      message: `❌ Auto-compile failed: ${error.message}`
    };
  }
}

/**
 * Load and validate peanut file
 */
async function loadPeanutFile(filePath) {
  try {
    const resolvedPath = path.resolve(filePath);
    
    // Check if file exists
    await fs.access(resolvedPath);
    
    // Read file content
    const content = await fs.readFile(resolvedPath, 'utf8');
    
    // Validate using PeanutConfig
    const validation = await peanutConfig.validate(content, {
      sourceFile: resolvedPath,
      strict: true
    });

    if (!validation.isValid) {
      throw new Error(`Validation failed: ${validation.errors.join(', ')}`);
    }

    // Parse and load configuration
    const config = await peanutConfig.load(resolvedPath);
    
    return {
      success: true,
      file: resolvedPath,
      size: content.length,
      sections: Object.keys(config).length,
      validation: validation,
      message: `✅ Successfully loaded ${resolvedPath} (${Object.keys(config).length} sections)`
    };

  } catch (error) {
    return {
      success: false,
      error: error.message,
      message: `❌ Failed to load ${filePath}: ${error.message}`
    };
  }
}

// Command definitions
const compile = new Command('compile')
  .description('Compile .peanuts/.tsk file to .pnt format')
  .argument('<input>', 'Input .peanuts or .tsk file')
  .option('-o, --output <file>', 'Output .pnt file (auto-generated if not specified)')
  .action(async (input, options) => {
    const result = await compilePeanut(input, options.output);
    console.log(result.message);
    
    if (result.success) {
      console.log(`   Input: ${result.input}`);
      console.log(`   Output: ${result.output}`);
      console.log(`   Size: ${result.size} bytes`);
    }
  });

const autoCompile = new Command('auto-compile')
  .description('Automatically compile all .peanuts/.tsk files in directory')
  .argument('<directory>', 'Directory to scan for files')
  .option('-r, --recursive', 'Process subdirectories recursively')
  .action(async (directory, options) => {
    const result = await autoCompileDirectory(directory);
    console.log(result.message);
    
    if (result.success && result.processed > 0) {
      console.log(`   Processed: ${result.processed} files`);
      console.log(`   Success: ${result.successCount}`);
      console.log(`   Errors: ${result.errorCount}`);
      
      if (result.errorCount > 0) {
        console.log('\nError details:');
        result.results
          .filter(r => !r.success)
          .forEach(r => console.log(`   ${r.message}`));
      }
    }
  });

const load = new Command('load')
  .description('Load and validate peanut file')
  .argument('<file>', 'Peanut file to load (.pnt, .peanuts, .tsk)')
  .option('-v, --verbose', 'Show detailed validation information')
  .action(async (file, options) => {
    const result = await loadPeanutFile(file);
    console.log(result.message);
    
    if (result.success) {
      console.log(`   File: ${result.file}`);
      console.log(`   Size: ${result.size} bytes`);
      console.log(`   Sections: ${result.sections}`);
      
      if (options.verbose && result.validation) {
        console.log('\nValidation details:');
        console.log(`   Valid: ${result.validation.isValid}`);
        console.log(`   Warnings: ${result.validation.warnings.length}`);
        if (result.validation.warnings.length > 0) {
          result.validation.warnings.forEach(warning => {
            console.log(`     - ${warning}`);
          });
        }
      }
    }
  });

module.exports = {
  compile,
  autoCompile,
  load,
  
  // Export functions for testing
  _compilePeanut: compilePeanut,
  _autoCompileDirectory: autoCompileDirectory,
  _loadPeanutFile: loadPeanutFile
}; 