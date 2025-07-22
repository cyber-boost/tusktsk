#!/usr/bin/env node
/**
 * Dependency Commands for TuskLang CLI
 * =====================================
 * Dependency management commands
 * 
 * Commands:
 * - install: Install dependency groups
 * - list: List available dependency groups
 * - check: Check installed dependencies
 */

const { Command } = require('commander');
const fs = require('fs').promises;
const path = require('path');
const { exec } = require('child_process');
const { promisify } = require('util');

const execAsync = promisify(exec);

/**
 * Dependency Groups Configuration
 */
const DEPENDENCY_GROUPS = {
  ai: {
    name: 'AI & Machine Learning',
    description: 'Artificial Intelligence and Machine Learning dependencies',
    packages: [
      'tensorflow',
      '@tensorflow/tfjs-node',
      'brain.js',
      'ml-matrix',
      'ml-regression',
      'natural',
      'compromise',
      'wink-nlp',
      'compromise-numbers',
      'compromise-dates'
    ],
    devPackages: [
      '@types/tensorflow',
      '@tensorflow/tfjs-node-gpu'
    ]
  },
  
  data: {
    name: 'Data Processing',
    description: 'Data processing and manipulation libraries',
    packages: [
      'lodash',
      'ramda',
      'underscore',
      'moment',
      'date-fns',
      'csv-parser',
      'csv-writer',
      'xlsx',
      'papaparse',
      'js-yaml',
      'xml2js',
      'cheerio'
    ],
    devPackages: [
      '@types/lodash',
      '@types/ramda',
      '@types/underscore',
      '@types/moment',
      '@types/js-yaml',
      '@types/xml2js'
    ]
  },
  
  analytics: {
    name: 'Analytics & Metrics',
    description: 'Analytics, metrics, and monitoring tools',
    packages: [
      'analytics-node',
      'mixpanel',
      'google-analytics',
      'keen-tracking',
      'statsd-client',
      'prom-client',
      'node-statsd',
      'winston',
      'bunyan',
      'pino'
    ],
    devPackages: [
      '@types/node-statsd'
    ]
  },
  
  database: {
    name: 'Database & ORM',
    description: 'Database drivers and ORM libraries',
    packages: [
      'mongoose',
      'sequelize',
      'prisma',
      'typeorm',
      'knex',
      'bookshelf',
      'objection',
      'waterline',
      'better-sqlite3',
      'pg',
      'mysql2',
      'mongodb',
      'redis',
      'ioredis'
    ],
    devPackages: [
      '@types/better-sqlite3',
      '@types/pg',
      '@types/mysql',
      '@types/redis'
    ]
  },
  
  communication: {
    name: 'Communication & APIs',
    description: 'HTTP clients, WebSockets, and API tools',
    packages: [
      'axios',
      'node-fetch',
      'got',
      'superagent',
      'ws',
      'socket.io',
      'socket.io-client',
      'graphql',
      'apollo-server',
      'apollo-client',
      'express-graphql',
      'swagger-jsdoc',
      'swagger-ui-express'
    ],
    devPackages: [
      '@types/ws',
      '@types/socket.io',
      '@types/socket.io-client'
    ]
  },
  
  cloud: {
    name: 'Cloud & Infrastructure',
    description: 'Cloud platform SDKs and infrastructure tools',
    packages: [
      '@aws-sdk/client-s3',
      '@aws-sdk/client-dynamodb',
      '@aws-sdk/client-lambda',
      '@google-cloud/storage',
      '@google-cloud/firestore',
      'azure-storage',
      '@azure/functions',
      'heroku',
      'vercel',
      'netlify',
      'docker',
      'kubernetes-client'
    ],
    devPackages: [
      '@types/docker'
    ]
  },
  
  security: {
    name: 'Security & Authentication',
    description: 'Security, authentication, and encryption libraries',
    packages: [
      'bcrypt',
      'jsonwebtoken',
      'passport',
      'passport-jwt',
      'passport-local',
      'helmet',
      'cors',
      'express-rate-limit',
      'express-validator',
      'joi',
      'yup',
      'zod',
      'crypto-js',
      'node-forge'
    ],
    devPackages: [
      '@types/bcrypt',
      '@types/jsonwebtoken',
      '@types/passport',
      '@types/passport-jwt',
      '@types/passport-local',
      '@types/cors',
      '@types/crypto-js'
    ]
  },
  
  monitoring: {
    name: 'Monitoring & Observability',
    description: 'Application monitoring and observability tools',
    packages: [
      'newrelic',
      'datadog',
      'sentry',
      'bugsnag',
      'rollbar',
      'logrocket',
      'opentelemetry-api',
      'opentelemetry-sdk-node',
      'jaeger-client',
      'zipkin',
      'prometheus-client',
      'grafana-api'
    ],
    devPackages: [
      '@types/newrelic'
    ]
  },
  
  platform: {
    name: 'Platform & Framework',
    description: 'Core platform and framework dependencies',
    packages: [
      'express',
      'koa',
      'fastify',
      'hapi',
      'restify',
      'connect',
      'body-parser',
      'multer',
      'compression',
      'serve-static',
      'express-session',
      'cookie-parser',
      'morgan',
      'dotenv',
      'config'
    ],
    devPackages: [
      '@types/express',
      '@types/koa',
      '@types/hapi',
      '@types/restify',
      '@types/connect',
      '@types/body-parser',
      '@types/multer',
      '@types/compression',
      '@types/serve-static',
      '@types/express-session',
      '@types/cookie-parser',
      '@types/morgan'
    ]
  }
};

/**
 * Dependency Manager Class
 */
class DependencyManager {
  constructor() {
    this.packageJsonPath = path.join(process.cwd(), 'package.json');
    this.nodeModulesPath = path.join(process.cwd(), 'node_modules');
  }

  /**
   * Check if package.json exists
   */
  async checkPackageJson() {
    try {
      await fs.access(this.packageJsonPath);
      return true;
    } catch (error) {
      return false;
    }
  }

  /**
   * Read package.json
   */
  async readPackageJson() {
    try {
      const content = await fs.readFile(this.packageJsonPath, 'utf8');
      return JSON.parse(content);
    } catch (error) {
      throw new Error('Failed to read package.json');
    }
  }

  /**
   * Write package.json
   */
  async writePackageJson(packageJson) {
    try {
      await fs.writeFile(this.packageJsonPath, JSON.stringify(packageJson, null, 2), 'utf8');
    } catch (error) {
      throw new Error('Failed to write package.json');
    }
  }

  /**
   * Check if package is installed
   */
  async isPackageInstalled(packageName) {
    try {
      const packagePath = path.join(this.nodeModulesPath, packageName);
      await fs.access(packagePath);
      return true;
    } catch (error) {
      return false;
    }
  }

  /**
   * Install packages using npm
   */
  async installPackages(packages, isDev = false) {
    try {
      const flag = isDev ? '--save-dev' : '--save';
      const command = `npm install ${flag} ${packages.join(' ')}`;
      
      console.log(`📦 Installing packages: ${packages.join(', ')}`);
      const { stdout, stderr } = await execAsync(command, { cwd: process.cwd() });
      
      if (stderr && !stderr.includes('npm WARN')) {
        throw new Error(stderr);
      }
      
      return {
        success: true,
        message: `Successfully installed ${packages.length} packages`,
        output: stdout
      };
    } catch (error) {
      return {
        success: false,
        error: error.message
      };
    }
  }

  /**
   * Install dependency group
   */
  async installGroup(groupName) {
    const group = DEPENDENCY_GROUPS[groupName];
    if (!group) {
      throw new Error(`Unknown dependency group: ${groupName}`);
    }

    // Check if package.json exists
    const hasPackageJson = await this.checkPackageJson();
    if (!hasPackageJson) {
      throw new Error('No package.json found. Run "npm init" first.');
    }

    const results = {
      group: groupName,
      name: group.name,
      installed: [],
      failed: [],
      devInstalled: [],
      devFailed: []
    };

    // Install regular packages
    if (group.packages && group.packages.length > 0) {
      const result = await this.installPackages(group.packages, false);
      if (result.success) {
        results.installed = group.packages;
      } else {
        results.failed = group.packages;
      }
    }

    // Install dev packages
    if (group.devPackages && group.devPackages.length > 0) {
      const result = await this.installPackages(group.devPackages, true);
      if (result.success) {
        results.devInstalled = group.devPackages;
      } else {
        results.devFailed = group.devPackages;
      }
    }

    return results;
  }

  /**
   * List all available dependency groups
   */
  listGroups() {
    return Object.keys(DEPENDENCY_GROUPS).map(key => ({
      key: key,
      name: DEPENDENCY_GROUPS[key].name,
      description: DEPENDENCY_GROUPS[key].description,
      packageCount: DEPENDENCY_GROUPS[key].packages ? DEPENDENCY_GROUPS[key].packages.length : 0,
      devPackageCount: DEPENDENCY_GROUPS[key].devPackages ? DEPENDENCY_GROUPS[key].devPackages.length : 0
    }));
  }

  /**
   * Check installed dependencies for a group
   */
  async checkGroup(groupName) {
    const group = DEPENDENCY_GROUPS[groupName];
    if (!group) {
      throw new Error(`Unknown dependency group: ${groupName}`);
    }

    const results = {
      group: groupName,
      name: group.name,
      installed: [],
      missing: [],
      devInstalled: [],
      devMissing: []
    };

    // Check regular packages
    if (group.packages) {
      for (const pkg of group.packages) {
        const isInstalled = await this.isPackageInstalled(pkg);
        if (isInstalled) {
          results.installed.push(pkg);
        } else {
          results.missing.push(pkg);
        }
      }
    }

    // Check dev packages
    if (group.devPackages) {
      for (const pkg of group.devPackages) {
        const isInstalled = await this.isPackageInstalled(pkg);
        if (isInstalled) {
          results.devInstalled.push(pkg);
        } else {
          results.devMissing.push(pkg);
        }
      }
    }

    return results;
  }

  /**
   * Get overall dependency status
   */
  async getOverallStatus() {
    const groups = this.listGroups();
    const status = {
      totalGroups: groups.length,
      installedGroups: 0,
      partialGroups: 0,
      missingGroups: 0,
      details: []
    };

    for (const group of groups) {
      const checkResult = await this.checkGroup(group.key);
      const totalPackages = checkResult.installed.length + checkResult.missing.length + 
                           checkResult.devInstalled.length + checkResult.devMissing.length;
      const installedPackages = checkResult.installed.length + checkResult.devInstalled.length;

      if (installedPackages === totalPackages && totalPackages > 0) {
        status.installedGroups++;
      } else if (installedPackages > 0) {
        status.partialGroups++;
      } else {
        status.missingGroups++;
      }

      status.details.push({
        group: group.key,
        name: group.name,
        installed: installedPackages,
        total: totalPackages,
        percentage: totalPackages > 0 ? Math.round((installedPackages / totalPackages) * 100) : 0
      });
    }

    return status;
  }
}

// Global dependency manager instance
const dependencyManager = new DependencyManager();

// Command definitions
const install = new Command('install')
  .description('Install dependency group')
  .argument('<group>', 'Dependency group to install')
  .option('-f, --force', 'Force reinstall existing packages')
  .action(async (group, options) => {
    try {
      console.log(`🚀 Installing dependency group: ${group}`);
      
      const result = await dependencyManager.installGroup(group);
      
      console.log(`\n📊 Installation Results for ${result.name}:`);
      
      if (result.installed.length > 0) {
        console.log(`✅ Installed packages: ${result.installed.length}`);
        result.installed.forEach(pkg => console.log(`   - ${pkg}`));
      }
      
      if (result.devInstalled.length > 0) {
        console.log(`✅ Installed dev packages: ${result.devInstalled.length}`);
        result.devInstalled.forEach(pkg => console.log(`   - ${pkg}`));
      }
      
      if (result.failed.length > 0) {
        console.log(`❌ Failed packages: ${result.failed.length}`);
        result.failed.forEach(pkg => console.log(`   - ${pkg}`));
      }
      
      if (result.devFailed.length > 0) {
        console.log(`❌ Failed dev packages: ${result.devFailed.length}`);
        result.devFailed.forEach(pkg => console.log(`   - ${pkg}`));
      }
      
    } catch (error) {
      console.error(`❌ Installation failed: ${error.message}`);
    }
  });

const list = new Command('list')
  .description('List available dependency groups')
  .option('-v, --verbose', 'Show detailed package information')
  .action(async (options) => {
    try {
      const groups = dependencyManager.listGroups();
      
      console.log(`📋 Available Dependency Groups (${groups.length}):\n`);
      
      groups.forEach(group => {
        console.log(`🔹 ${group.key} - ${group.name}`);
        console.log(`   ${group.description}`);
        console.log(`   Packages: ${group.packageCount} regular, ${group.devPackageCount} dev`);
        
        if (options.verbose) {
          const fullGroup = DEPENDENCY_GROUPS[group.key];
          if (fullGroup.packages && fullGroup.packages.length > 0) {
            console.log(`   Regular packages: ${fullGroup.packages.join(', ')}`);
          }
          if (fullGroup.devPackages && fullGroup.devPackages.length > 0) {
            console.log(`   Dev packages: ${fullGroup.devPackages.join(', ')}`);
          }
        }
        console.log('');
      });
      
    } catch (error) {
      console.error(`❌ Failed to list groups: ${error.message}`);
    }
  });

const check = new Command('check')
  .description('Check installed dependencies')
  .argument('[group]', 'Specific group to check (checks all if not specified)')
  .option('-s, --summary', 'Show summary only')
  .action(async (group, options) => {
    try {
      if (group) {
        // Check specific group
        const result = await dependencyManager.checkGroup(group);
        
        console.log(`📊 Dependency Status for ${result.name}:\n`);
        
        if (result.installed.length > 0) {
          console.log(`✅ Installed (${result.installed.length}):`);
          result.installed.forEach(pkg => console.log(`   - ${pkg}`));
        }
        
        if (result.devInstalled.length > 0) {
          console.log(`✅ Dev Installed (${result.devInstalled.length}):`);
          result.devInstalled.forEach(pkg => console.log(`   - ${pkg}`));
        }
        
        if (result.missing.length > 0) {
          console.log(`❌ Missing (${result.missing.length}):`);
          result.missing.forEach(pkg => console.log(`   - ${pkg}`));
        }
        
        if (result.devMissing.length > 0) {
          console.log(`❌ Dev Missing (${result.devMissing.length}):`);
          result.devMissing.forEach(pkg => console.log(`   - ${pkg}`));
        }
        
        const total = result.installed.length + result.missing.length + 
                     result.devInstalled.length + result.devMissing.length;
        const installed = result.installed.length + result.devInstalled.length;
        const percentage = total > 0 ? Math.round((installed / total) * 100) : 0;
        
        console.log(`\n📈 Overall: ${installed}/${total} packages installed (${percentage}%)`);
        
      } else {
        // Check all groups
        const status = await dependencyManager.getOverallStatus();
        
        if (options.summary) {
          console.log(`📊 Dependency Summary:`);
          console.log(`   Total groups: ${status.totalGroups}`);
          console.log(`   Fully installed: ${status.installedGroups}`);
          console.log(`   Partially installed: ${status.partialGroups}`);
          console.log(`   Not installed: ${status.missingGroups}`);
        } else {
          console.log(`📊 Dependency Status for All Groups:\n`);
          
          status.details.forEach(detail => {
            const statusIcon = detail.percentage === 100 ? '✅' : 
                              detail.percentage > 0 ? '⚠️' : '❌';
            console.log(`${statusIcon} ${detail.name}: ${detail.installed}/${detail.total} (${detail.percentage}%)`);
          });
          
          console.log(`\n📈 Overall: ${status.installedGroups} complete, ${status.partialGroups} partial, ${status.missingGroups} missing`);
        }
      }
      
    } catch (error) {
      console.error(`❌ Check failed: ${error.message}`);
    }
  });

module.exports = {
  install,
  list,
  check,
  
  // Export for testing
  _DependencyManager: DependencyManager,
  _dependencyManager: dependencyManager,
  _DEPENDENCY_GROUPS: DEPENDENCY_GROUPS
}; 