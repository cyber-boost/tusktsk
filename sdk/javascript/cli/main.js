#!/usr/bin/env node
/**
 * TuskLang CLI for JavaScript/Node.js
 * ===================================
 * Universal CLI implementation following the TuskLang CLI Command Specification
 * 
 * "Strong. Secure. Scalable."
 */

const { Command } = require('commander');
const path = require('path');
const fs = require('fs').promises;
const TuskLang = require('../index.js');
const PeanutConfig = require('../peanut-config.js');

// Import command modules
const dbCommands = require('./commands/db.js');
const devCommands = require('./commands/dev.js');
const testCommands = require('./commands/test.js');
const serviceCommands = require('./commands/service.js');
const cacheCommands = require('./commands/cache.js');
const configCommands = require('./commands/config.js');
const aiCommands = require('./commands/ai.js');
const binaryCommands = require('./commands/binary.js');
const utilityCommands = require('./commands/utility.js');

// CLI version
const VERSION = '2.0.0';

// Create CLI program
const program = new Command();

// Global options
program
  .name('tsk')
  .description('TuskLang CLI - The Freedom Configuration Language')
  .version(VERSION)
  .option('--verbose', 'Enable verbose output')
  .option('--quiet, -q', 'Suppress non-error output')
  .option('--config <path>', 'Use alternate config file')
  .option('--json', 'Output in JSON format');

// Helper function for output formatting
function output(data, options = {}) {
  if (options.json) {
    console.log(JSON.stringify(data, null, 2));
  } else {
    if (typeof data === 'object') {
      console.log(JSON.stringify(data, null, 2));
    } else {
      console.log(data);
    }
  }
}

// Helper function for status symbols
function status(success, message) {
  const symbol = success ? '✅' : '❌';
  return `${symbol} ${message}`;
}

// Helper function for warning
function warning(message) {
  return `⚠️ ${message}`;
}

// Helper function for info
function info(message) {
  return `📍 ${message}`;
}

// Helper function for loading
function loading(message) {
  return `🔄 ${message}`;
}

// Global error handler
program.exitOverride();
process.on('uncaughtException', (err) => {
  console.error('❌ Uncaught Exception:', err.message);
  process.exit(1);
});

process.on('unhandledRejection', (reason, promise) => {
  console.error('❌ Unhandled Rejection at:', promise, 'reason:', reason);
  process.exit(1);
});

// Database Commands
program
  .command('db')
  .description('Database operations')
  .addCommand(dbCommands.status)
  .addCommand(dbCommands.migrate)
  .addCommand(dbCommands.console)
  .addCommand(dbCommands.backup)
  .addCommand(dbCommands.restore)
  .addCommand(dbCommands.init);

// Development Commands
program
  .command('serve')
  .description('Start development server')
  .argument('[port]', 'Port number', '8080')
  .action(devCommands.serve);

program
  .command('compile')
  .description('Compile .tsk file')
  .argument('<file>', 'TSK file to compile')
  .action(devCommands.compile);

program
  .command('optimize')
  .description('Optimize .tsk file for production')
  .argument('<file>', 'TSK file to optimize')
  .action(devCommands.optimize);

// Testing Commands
program
  .command('test')
  .description('Run tests')
  .argument('[suite]', 'Test suite to run', 'all')
  .action(testCommands.run);

// Service Commands
program
  .command('services')
  .description('Service management')
  .addCommand(serviceCommands.start)
  .addCommand(serviceCommands.stop)
  .addCommand(serviceCommands.restart)
  .addCommand(serviceCommands.status);

// Cache Commands
program
  .command('cache')
  .description('Cache operations')
  .addCommand(cacheCommands.clear)
  .addCommand(cacheCommands.status)
  .addCommand(cacheCommands.warm)
  .addCommand(cacheCommands.memcached);

// Configuration Commands
program
  .command('config')
  .description('Configuration management')
  .addCommand(configCommands.get)
  .addCommand(configCommands.check)
  .addCommand(configCommands.validate)
  .addCommand(configCommands.compile)
  .addCommand(configCommands.docs)
  .addCommand(configCommands.clearCache)
  .addCommand(configCommands.stats);

// Binary Commands
program
  .command('binary')
  .description('Binary performance operations')
  .addCommand(binaryCommands.compile)
  .addCommand(binaryCommands.execute)
  .addCommand(binaryCommands.benchmark)
  .addCommand(binaryCommands.optimize);

// AI Commands
program
  .command('ai')
  .description('AI operations')
  .addCommand(aiCommands.claude)
  .addCommand(aiCommands.chatgpt)
  .addCommand(aiCommands.analyze)
  .addCommand(aiCommands.optimize)
  .addCommand(aiCommands.security);

// Utility Commands
program
  .command('parse')
  .description('Parse TSK file')
  .argument('<file>', 'TSK file to parse')
  .action(utilityCommands.parse);

program
  .command('validate')
  .description('Validate TSK file syntax')
  .argument('<file>', 'TSK file to validate')
  .action(utilityCommands.validate);

program
  .command('convert')
  .description('Convert between formats')
  .option('-i, --input <file>', 'Input file')
  .option('-o, --output <file>', 'Output file')
  .action(utilityCommands.convert);

program
  .command('get')
  .description('Get specific value by key path')
  .argument('<file>', 'TSK file')
  .argument('<key.path>', 'Key path')
  .action(utilityCommands.get);

program
  .command('set')
  .description('Set value by key path')
  .argument('<file>', 'TSK file')
  .argument('<key.path>', 'Key path')
  .argument('<value>', 'Value to set')
  .action(utilityCommands.set);

// Interactive mode when no command provided
if (process.argv.length === 2) {
  console.log(`🐘 TuskLang v${VERSION} - Interactive Mode`);
  console.log('Type "help" for commands, "exit" to quit\n');
  
  const readline = require('readline');
  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    prompt: 'tsk> '
  });

  rl.prompt();

  rl.on('line', async (line) => {
    const input = line.trim();
    
    if (input === 'exit' || input === 'quit') {
      rl.close();
      return;
    }
    
    if (input === 'help') {
      console.log('Available commands:');
      console.log('  db status          - Check database connection');
      console.log('  config get <path>  - Get configuration value');
      console.log('  parse <file>       - Parse TSK file');
      console.log('  test all           - Run all tests');
      console.log('  exit               - Exit interactive mode');
      console.log('');
      rl.prompt();
      return;
    }
    
    if (input) {
      try {
        // Parse the command
        const args = input.split(' ');
        const command = args[0];
        
        switch (command) {
          case 'db':
            if (args[1] === 'status') {
              const result = await dbCommands.status.action();
              console.log(result);
            }
            break;
            
          case 'config':
            if (args[1] === 'get' && args[2]) {
              const result = await configCommands.get.action(args[2]);
              console.log(result);
            }
            break;
            
          case 'parse':
            if (args[1]) {
              const result = await utilityCommands.parse(args[1]);
              console.log(result);
            }
            break;
            
          case 'test':
            if (args[1] === 'all') {
              const result = await testCommands.run('all');
              console.log(result);
            }
            break;
            
          default:
            console.log(`Unknown command: ${command}`);
            console.log('Type "help" for available commands');
        }
      } catch (error) {
        console.error('❌ Error:', error.message);
      }
    }
    
    rl.prompt();
  });

  rl.on('close', () => {
    console.log('Goodbye!');
    process.exit(0);
  });
} else {
  // Parse command line arguments
  program.parse();
}

module.exports = {
  program,
  output,
  status,
  warning,
  info,
  loading,
  VERSION
}; 