#!/usr/bin/env node

/**
 * TuskLang Security Scanner
 * Comprehensive security analysis tool for TuskLang configurations
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { TuskLang, TuskLangEnhanced } = require('../src/index');
const chalk = require('chalk');

class TuskLangSecurityScanner {
  constructor() {
    this.scanResults = {
      vulnerabilities: [],
      warnings: [],
      info: [],
      score: 100,
      timestamp: new Date().toISOString()
    };
    
    this.securityRules = {
      sensitivePatterns: [
        { pattern: /password/i, severity: 'high', description: 'Password field detected' },
        { pattern: /secret/i, severity: 'high', description: 'Secret field detected' },
        { pattern: /key/i, severity: 'medium', description: 'Key field detected' },
        { pattern: /token/i, severity: 'high', description: 'Token field detected' },
        { pattern: /credential/i, severity: 'high', description: 'Credential field detected' },
        { pattern: /api_key/i, severity: 'high', description: 'API key detected' },
        { pattern: /private_key/i, severity: 'critical', description: 'Private key detected' },
        { pattern: /access_token/i, severity: 'high', description: 'Access token detected' }
      ],
      
      dangerousValues: [
        { pattern: /^sk-/, severity: 'critical', description: 'OpenAI API key detected' },
        { pattern: /^pk_/, severity: 'high', description: 'Stripe public key detected' },
        { pattern: /^sk_/, severity: 'critical', description: 'Stripe secret key detected' },
        { pattern: /^ghp_/, severity: 'critical', description: 'GitHub personal access token detected' },
        { pattern: /^gho_/, severity: 'critical', description: 'GitHub OAuth token detected' },
        { pattern: /^ghu_/, severity: 'critical', description: 'GitHub user-to-server token detected' },
        { pattern: /^ghs_/, severity: 'critical', description: 'GitHub server-to-server token detected' },
        { pattern: /^ghr_/, severity: 'critical', description: 'GitHub refresh token detected' },
        { pattern: /^AKIA/, severity: 'critical', description: 'AWS access key detected' },
        { pattern: /^AIza/, severity: 'high', description: 'Google API key detected' }
      ],
      
      sqlInjectionPatterns: [
        { pattern: /SELECT.*FROM.*WHERE.*\$\{/, severity: 'high', description: 'Potential SQL injection in SELECT' },
        { pattern: /INSERT.*INTO.*VALUES.*\$\{/, severity: 'high', description: 'Potential SQL injection in INSERT' },
        { pattern: /UPDATE.*SET.*WHERE.*\$\{/, severity: 'high', description: 'Potential SQL injection in UPDATE' },
        { pattern: /DELETE.*FROM.*WHERE.*\$\{/, severity: 'high', description: 'Potential SQL injection in DELETE' }
      ],
      
      filePathPatterns: [
        { pattern: /\.\.\//, severity: 'high', description: 'Directory traversal attempt detected' },
        { pattern: /\/etc\/passwd/, severity: 'critical', description: 'System file access attempt' },
        { pattern: /\/etc\/shadow/, severity: 'critical', description: 'Password file access attempt' },
        { pattern: /\/proc\//, severity: 'high', description: 'Process file access attempt' }
      ],
      
      networkPatterns: [
        { pattern: /http:\/\//, severity: 'medium', description: 'Insecure HTTP connection' },
        { pattern: /ftp:\/\//, severity: 'medium', description: 'Insecure FTP connection' },
        { pattern: /telnet:\/\//, severity: 'high', description: 'Insecure telnet connection' }
      ]
    };
  }

  /**
   * Scan a TuskLang configuration file for security issues
   */
  async scan(configPath, options = {}) {
    console.log(chalk.blue.bold('🔒 TuskLang Security Scanner'));
    console.log(chalk.gray(`Scanning: ${configPath}\n`));

    try {
      // Read configuration file
      const configContent = fs.readFileSync(configPath, 'utf8');
      
      // Parse configuration
      const tusk = new TuskLangEnhanced();
      const config = tusk.parse(configContent);
      
      // Run security scans
      await this.scanSensitiveData(config, configPath);
      await this.scanSqlInjection(config);
      await this.scanFilePaths(config);
      await this.scanNetworkConnections(config);
      await this.scanVariableInjection(config);
      await this.scanEnvironmentVariables(config);
      await this.scanDatabaseConnections(config);
      
      // Calculate security score
      this.calculateSecurityScore();
      
      // Generate report
      this.generateSecurityReport();
      
      // Export results if requested
      if (options.export) {
        this.exportResults(options.export);
      }
      
      return this.scanResults;
      
    } catch (error) {
      console.error(chalk.red.bold('❌ Security scan failed:'), error.message);
      throw error;
    }
  }

  /**
   * Scan for sensitive data exposure
   */
  async scanSensitiveData(config, configPath) {
    console.log(chalk.yellow('🔍 Scanning for sensitive data...'));
    
    const scanObject = (obj, path = '') => {
      if (typeof obj === 'object' && obj !== null) {
        Object.entries(obj).forEach(([key, value]) => {
          const currentPath = path ? `${path}.${key}` : key;
          
          // Check key names
          this.securityRules.sensitivePatterns.forEach(rule => {
            if (rule.pattern.test(key)) {
              this.addVulnerability(
                rule.severity,
                rule.description,
                currentPath,
                `Key name matches pattern: ${rule.pattern}`
              );
            }
          });
          
          // Check values
          if (typeof value === 'string') {
            // Check for dangerous values
            this.securityRules.dangerousValues.forEach(rule => {
              if (rule.pattern.test(value)) {
                this.addVulnerability(
                  rule.severity,
                  rule.description,
                  currentPath,
                  `Value matches pattern: ${rule.pattern}`
                );
              }
            });
            
            // Check for long values (potential secrets)
            if (value.length > 100 && !value.includes(' ')) {
              this.addWarning(
                'Long value detected',
                currentPath,
                'Value appears to be a long string without spaces (potential secret)'
              );
            }
          }
          
          // Recursively scan nested objects
          scanObject(value, currentPath);
        });
      }
    };
    
    scanObject(config);
    console.log(chalk.green(`  ✅ Sensitive data scan complete`));
  }

  /**
   * Scan for SQL injection vulnerabilities
   */
  async scanSqlInjection(config) {
    console.log(chalk.yellow('🔍 Scanning for SQL injection...'));
    
    const scanForSqlInjection = (obj, path = '') => {
      if (typeof obj === 'object' && obj !== null) {
        Object.entries(obj).forEach(([key, value]) => {
          const currentPath = path ? `${path}.${key}` : key;
          
          if (typeof value === 'string') {
            this.securityRules.sqlInjectionPatterns.forEach(rule => {
              if (rule.pattern.test(value)) {
                this.addVulnerability(
                  rule.severity,
                  rule.description,
                  currentPath,
                  `SQL query with variable interpolation detected`
                );
              }
            });
          }
          
          scanForSqlInjection(value, currentPath);
        });
      }
    };
    
    scanForSqlInjection(config);
    console.log(chalk.green(`  ✅ SQL injection scan complete`));
  }

  /**
   * Scan for dangerous file paths
   */
  async scanFilePaths(config) {
    console.log(chalk.yellow('🔍 Scanning for dangerous file paths...'));
    
    const scanForFilePaths = (obj, path = '') => {
      if (typeof obj === 'object' && obj !== null) {
        Object.entries(obj).forEach(([key, value]) => {
          const currentPath = path ? `${path}.${key}` : key;
          
          if (typeof value === 'string') {
            this.securityRules.filePathPatterns.forEach(rule => {
              if (rule.pattern.test(value)) {
                this.addVulnerability(
                  rule.severity,
                  rule.description,
                  currentPath,
                  `Dangerous file path detected: ${value}`
                );
              }
            });
          }
          
          scanForFilePaths(value, currentPath);
        });
      }
    };
    
    scanForFilePaths(config);
    console.log(chalk.green(`  ✅ File path scan complete`));
  }

  /**
   * Scan for insecure network connections
   */
  async scanNetworkConnections(config) {
    console.log(chalk.yellow('🔍 Scanning for insecure network connections...'));
    
    const scanForNetworkConnections = (obj, path = '') => {
      if (typeof obj === 'object' && obj !== null) {
        Object.entries(obj).forEach(([key, value]) => {
          const currentPath = path ? `${path}.${key}` : key;
          
          if (typeof value === 'string') {
            this.securityRules.networkPatterns.forEach(rule => {
              if (rule.pattern.test(value)) {
                this.addVulnerability(
                  rule.severity,
                  rule.description,
                  currentPath,
                  `Insecure connection detected: ${value}`
                );
              }
            });
          }
          
          scanForNetworkConnections(value, currentPath);
        });
      }
    };
    
    scanForNetworkConnections(config);
    console.log(chalk.green(`  ✅ Network connection scan complete`));
  }

  /**
   * Scan for variable injection vulnerabilities
   */
  async scanVariableInjection(config) {
    console.log(chalk.yellow('🔍 Scanning for variable injection...'));
    
    const scanForVariableInjection = (obj, path = '') => {
      if (typeof obj === 'object' && obj !== null) {
        Object.entries(obj).forEach(([key, value]) => {
          const currentPath = path ? `${path}.${key}` : key;
          
          if (typeof value === 'string') {
            // Check for unescaped variable interpolation
            const variableMatches = value.match(/\$\{[^}]+\}/g);
            if (variableMatches) {
              variableMatches.forEach(match => {
                const variableName = match.slice(2, -1);
                
                // Check for potentially dangerous variable names
                if (variableName.includes('password') || 
                    variableName.includes('secret') || 
                    variableName.includes('key') ||
                    variableName.includes('token')) {
                  this.addWarning(
                    'Sensitive variable interpolation',
                    currentPath,
                    `Variable interpolation detected: ${match}`
                  );
                }
              });
            }
          }
          
          scanForVariableInjection(value, currentPath);
        });
      }
    };
    
    scanForVariableInjection(config);
    console.log(chalk.green(`  ✅ Variable injection scan complete`));
  }

  /**
   * Scan environment variable usage
   */
  async scanEnvironmentVariables(config) {
    console.log(chalk.yellow('🔍 Scanning environment variable usage...'));
    
    const scanForEnvVars = (obj, path = '') => {
      if (typeof obj === 'object' && obj !== null) {
        Object.entries(obj).forEach(([key, value]) => {
          const currentPath = path ? `${path}.${key}` : key;
          
          if (typeof value === 'string') {
            // Check for environment variable usage
            const envMatches = value.match(/\$ENV\{[^}]+\}/g);
            if (envMatches) {
              envMatches.forEach(match => {
                const envVar = match.slice(5, -1);
                
                // Check for sensitive environment variables
                if (envVar.includes('PASSWORD') || 
                    envVar.includes('SECRET') || 
                    envVar.includes('KEY') ||
                    envVar.includes('TOKEN')) {
                  this.addInfo(
                    'Sensitive environment variable',
                    currentPath,
                    `Environment variable detected: ${envVar}`
                  );
                }
              });
            }
          }
          
          scanForEnvVars(value, currentPath);
        });
      }
    };
    
    scanForEnvVars(config);
    console.log(chalk.green(`  ✅ Environment variable scan complete`));
  }

  /**
   * Scan database connection configurations
   */
  async scanDatabaseConnections(config) {
    console.log(chalk.yellow('🔍 Scanning database connections...'));
    
    const dbSections = ['database', 'db', 'postgres', 'mysql', 'sqlite', 'mongodb'];
    
    dbSections.forEach(section => {
      if (config[section]) {
        const dbConfig = config[section];
        
        // Check for hardcoded credentials
        if (dbConfig.password && typeof dbConfig.password === 'string') {
          this.addVulnerability(
            'high',
            'Hardcoded database password',
            `${section}.password`,
            'Database password should be stored in environment variables'
          );
        }
        
        if (dbConfig.user && typeof dbConfig.user === 'string') {
          this.addInfo(
            'Database user configuration',
            `${section}.user`,
            `Database user: ${dbConfig.user}`
          );
        }
        
        // Check for insecure connection settings
        if (dbConfig.ssl === false) {
          this.addVulnerability(
            'medium',
            'Insecure database connection',
            `${section}.ssl`,
            'SSL should be enabled for database connections'
          );
        }
        
        // Check for default ports
        if (dbConfig.port) {
          const defaultPorts = {
            postgres: 5432,
            mysql: 3306,
            mongodb: 27017
          };
          
          if (defaultPorts[section] && dbConfig.port === defaultPorts[section]) {
            this.addInfo(
              'Default database port',
              `${section}.port`,
              `Using default port: ${dbConfig.port}`
            );
          }
        }
      }
    });
    
    console.log(chalk.green(`  ✅ Database connection scan complete`));
  }

  /**
   * Add vulnerability to scan results
   */
  addVulnerability(severity, description, location, details) {
    this.scanResults.vulnerabilities.push({
      severity,
      description,
      location,
      details,
      timestamp: new Date().toISOString()
    });
  }

  /**
   * Add warning to scan results
   */
  addWarning(description, location, details) {
    this.scanResults.warnings.push({
      description,
      location,
      details,
      timestamp: new Date().toISOString()
    });
  }

  /**
   * Add info to scan results
   */
  addInfo(description, location, details) {
    this.scanResults.info.push({
      description,
      location,
      details,
      timestamp: new Date().toISOString()
    });
  }

  /**
   * Calculate security score
   */
  calculateSecurityScore() {
    let score = 100;
    
    // Deduct points for vulnerabilities
    this.scanResults.vulnerabilities.forEach(vuln => {
      switch (vuln.severity) {
        case 'critical':
          score -= 25;
          break;
        case 'high':
          score -= 15;
          break;
        case 'medium':
          score -= 10;
          break;
        case 'low':
          score -= 5;
          break;
      }
    });
    
    // Deduct points for warnings
    this.scanResults.warnings.forEach(() => {
      score -= 2;
    });
    
    // Ensure score doesn't go below 0
    this.scanResults.score = Math.max(0, score);
  }

  /**
   * Generate security report
   */
  generateSecurityReport() {
    console.log(chalk.blue.bold('\n📊 Security Report'));
    console.log(chalk.gray('=' * 50));
    
    // Security score
    const score = this.scanResults.score;
    let scoreColor = chalk.green;
    let scoreEmoji = '🟢';
    
    if (score < 50) {
      scoreColor = chalk.red;
      scoreEmoji = '🔴';
    } else if (score < 75) {
      scoreColor = chalk.yellow;
      scoreEmoji = '🟡';
    }
    
    console.log(chalk.cyan('\n🔒 Security Score:'));
    console.log(`  ${scoreEmoji} ${scoreColor.bold(score)}/100`);
    
    // Vulnerabilities
    if (this.scanResults.vulnerabilities.length > 0) {
      console.log(chalk.red('\n❌ Vulnerabilities:'));
      this.scanResults.vulnerabilities.forEach((vuln, index) => {
        const severityColor = this.getSeverityColor(vuln.severity);
        console.log(`  ${index + 1}. ${severityColor.bold(vuln.severity.toUpperCase())}: ${vuln.description}`);
        console.log(`     Location: ${vuln.location}`);
        console.log(`     Details: ${vuln.details}`);
        console.log('');
      });
    }
    
    // Warnings
    if (this.scanResults.warnings.length > 0) {
      console.log(chalk.yellow('\n⚠️  Warnings:'));
      this.scanResults.warnings.forEach((warning, index) => {
        console.log(`  ${index + 1}. ${warning.description}`);
        console.log(`     Location: ${warning.location}`);
        console.log(`     Details: ${warning.details}`);
        console.log('');
      });
    }
    
    // Info
    if (this.scanResults.info.length > 0) {
      console.log(chalk.blue('\nℹ️  Information:'));
      this.scanResults.info.forEach((info, index) => {
        console.log(`  ${index + 1}. ${info.description}`);
        console.log(`     Location: ${info.location}`);
        console.log(`     Details: ${info.details}`);
        console.log('');
      });
    }
    
    // Summary
    console.log(chalk.cyan('\n📈 Summary:'));
    console.log(`  Vulnerabilities: ${this.scanResults.vulnerabilities.length}`);
    console.log(`  Warnings: ${this.scanResults.warnings.length}`);
    console.log(`  Info: ${this.scanResults.info.length}`);
    
    // Recommendations
    this.generateSecurityRecommendations();
  }

  /**
   * Generate security recommendations
   */
  generateSecurityRecommendations() {
    console.log(chalk.green('\n💡 Security Recommendations:'));
    
    const recommendations = [];
    
    // Check for hardcoded secrets
    const hasHardcodedSecrets = this.scanResults.vulnerabilities.some(v => 
      v.description.includes('password') || v.description.includes('secret') || v.description.includes('key')
    );
    
    if (hasHardcodedSecrets) {
      recommendations.push('Move sensitive data to environment variables');
    }
    
    // Check for SQL injection
    const hasSqlInjection = this.scanResults.vulnerabilities.some(v => 
      v.description.includes('SQL injection')
    );
    
    if (hasSqlInjection) {
      recommendations.push('Use parameterized queries to prevent SQL injection');
    }
    
    // Check for insecure connections
    const hasInsecureConnections = this.scanResults.vulnerabilities.some(v => 
      v.description.includes('Insecure')
    );
    
    if (hasInsecureConnections) {
      recommendations.push('Use secure connections (HTTPS, SSL/TLS)');
    }
    
    // Check for file path issues
    const hasFilePathIssues = this.scanResults.vulnerabilities.some(v => 
      v.description.includes('file path') || v.description.includes('traversal')
    );
    
    if (hasFilePathIssues) {
      recommendations.push('Validate and sanitize file paths');
    }
    
    if (recommendations.length > 0) {
      recommendations.forEach(rec => console.log(`  - ${rec}`));
    } else {
      console.log('  - Configuration appears secure');
    }
  }

  /**
   * Get color for severity level
   */
  getSeverityColor(severity) {
    switch (severity) {
      case 'critical':
        return chalk.red;
      case 'high':
        return chalk.magenta;
      case 'medium':
        return chalk.yellow;
      case 'low':
        return chalk.blue;
      default:
        return chalk.gray;
    }
  }

  /**
   * Export scan results to file
   */
  exportResults(outputPath) {
    const report = {
      ...this.scanResults,
      summary: {
        totalVulnerabilities: this.scanResults.vulnerabilities.length,
        totalWarnings: this.scanResults.warnings.length,
        totalInfo: this.scanResults.info.length,
        securityScore: this.scanResults.score
      }
    };
    
    fs.writeFileSync(outputPath, JSON.stringify(report, null, 2));
    console.log(chalk.green(`📊 Security report exported to: ${outputPath}`));
  }
}

// CLI interface
if (require.main === module) {
  const args = process.argv.slice(2);
  const scanner = new TuskLangSecurityScanner();
  
  if (args.length === 0) {
    console.log(chalk.red('Usage: node security-scanner.js <config-file> [options]'));
    console.log(chalk.gray('Options:'));
    console.log(chalk.gray('  --export <file>  Export results to JSON file'));
    process.exit(1);
  }
  
  const configPath = args[0];
  const options = {
    export: args.includes('--export') ? args[args.indexOf('--export') + 1] : null
  };
  
  scanner.scan(configPath, options);
}

module.exports = TuskLangSecurityScanner; 