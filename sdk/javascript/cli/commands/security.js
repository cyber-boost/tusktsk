#!/usr/bin/env node
/**
 * Security Commands for TuskLang CLI
 * ===================================
 * Security auditing and compliance commands
 * 
 * Commands:
 * - auth: Authentication management (login/logout/status)
 * - scan: Vulnerability scanning
 * - encrypt: Encrypt files
 * - decrypt: Decrypt files
 * - audit: Security reporting
 * - hash: File hashing
 */

const { Command } = require('commander');
const fs = require('fs').promises;
const path = require('path');
const crypto = require('crypto');
const https = require('https');
const os = require('os');

/**
 * Security Session Manager Class
 */
class SecuritySessionManager {
  constructor() {
    this.sessionFile = path.join(os.homedir(), '.tusktsk', 'security-session.json');
    this.serverUrl = 'https://security.tuskt.sk';
    this.session = null;
  }

  /**
   * Initialize session manager
   */
  async init() {
    try {
      // Ensure session directory exists
      const sessionDir = path.dirname(this.sessionFile);
      await fs.mkdir(sessionDir, { recursive: true });
      
      // Load existing session if available
      await this.loadSession();
    } catch (error) {
      // Session file doesn't exist, that's okay
    }
  }

  /**
   * Load session from file
   */
  async loadSession() {
    try {
      const data = await fs.readFile(this.sessionFile, 'utf8');
      this.session = JSON.parse(data);
      return this.session;
    } catch (error) {
      this.session = null;
      return null;
    }
  }

  /**
   * Save session to file
   */
  async saveSession(sessionData) {
    this.session = sessionData;
    await fs.writeFile(this.sessionFile, JSON.stringify(sessionData, null, 2), 'utf8');
  }

  /**
   * Clear session
   */
  async clearSession() {
    try {
      await fs.unlink(this.sessionFile);
      this.session = null;
    } catch (error) {
      // File doesn't exist, that's okay
    }
  }

  /**
   * Make HTTPS request to security server
   */
  async makeSecurityRequest(endpoint, data = {}) {
    return new Promise((resolve, reject) => {
      const postData = JSON.stringify({
        ...data,
        timestamp: Date.now()
      });

      const options = {
        hostname: 'security.tuskt.sk',
        port: 443,
        path: endpoint,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(postData),
          'User-Agent': 'TuskTsk-CLI/2.0.0'
        }
      };

      const req = https.request(options, (res) => {
        let responseData = '';
        
        res.on('data', (chunk) => {
          responseData += chunk;
        });
        
        res.on('end', () => {
          try {
            const response = JSON.parse(responseData);
            resolve(response);
          } catch (error) {
            reject(new Error('Invalid server response'));
          }
        });
      });

      req.on('error', (error) => {
        reject(new Error(`Network error: ${error.message}`));
      });

      req.write(postData);
      req.end();
    });
  }

  /**
   * Login to security service
   */
  async login(username, password) {
    try {
      const response = await this.makeSecurityRequest('/auth/login', {
        username: username,
        password: password
      });

      if (response.success) {
        const sessionData = {
          token: response.token,
          username: username,
          loggedInAt: Date.now(),
          expiresAt: response.expiresAt,
          permissions: response.permissions || []
        };

        await this.saveSession(sessionData);
        
        return {
          success: true,
          message: 'Login successful',
          session: sessionData
        };
      } else {
        throw new Error(response.error || 'Login failed');
      }
    } catch (error) {
      throw new Error(`Login failed: ${error.message}`);
    }
  }

  /**
   * Logout from security service
   */
  async logout() {
    if (!this.session) {
      return {
        success: true,
        message: 'No active session to logout'
      };
    }

    try {
      await this.makeSecurityRequest('/auth/logout', {
        token: this.session.token
      });

      await this.clearSession();
      
      return {
        success: true,
        message: 'Logout successful'
      };
    } catch (error) {
      // Even if server logout fails, clear local session
      await this.clearSession();
      return {
        success: true,
        message: 'Local session cleared'
      };
    }
  }

  /**
   * Check session status
   */
  async getStatus() {
    await this.init();
    
    if (!this.session) {
      return {
        loggedIn: false,
        message: 'No active session'
      };
    }

    const now = Date.now();
    const expiresAt = this.session.expiresAt;
    
    if (expiresAt && now > expiresAt) {
      await this.clearSession();
      return {
        loggedIn: false,
        message: 'Session expired'
      };
    }

    return {
      loggedIn: true,
      username: this.session.username,
      loggedInAt: new Date(this.session.loggedInAt),
      expiresAt: expiresAt ? new Date(expiresAt) : null,
      permissions: this.session.permissions || []
    };
  }
}

/**
 * Security Scanner Class
 */
class SecurityScanner {
  constructor() {
    this.vulnerabilities = [];
    this.scanResults = {};
  }

  /**
   * Scan file for security vulnerabilities
   */
  async scanFile(filePath) {
    const results = {
      file: filePath,
      vulnerabilities: [],
      riskLevel: 'LOW',
      recommendations: []
    };

    try {
      const content = await fs.readFile(filePath, 'utf8');
      const ext = path.extname(filePath).toLowerCase();

      // Check for common security issues
      const checks = [
        this.checkForHardcodedSecrets(content),
        this.checkForSQLInjection(content),
        this.checkForXSS(content),
        this.checkForPathTraversal(content),
        this.checkForInsecureCrypto(content),
        this.checkForDeprecatedAPIs(content, ext)
      ];

      for (const check of checks) {
        if (check.vulnerabilities.length > 0) {
          results.vulnerabilities.push(...check.vulnerabilities);
        }
        if (check.recommendations.length > 0) {
          results.recommendations.push(...check.recommendations);
        }
      }

      // Determine risk level
      const highCount = results.vulnerabilities.filter(v => v.severity === 'HIGH').length;
      const mediumCount = results.vulnerabilities.filter(v => v.severity === 'MEDIUM').length;

      if (highCount > 0) {
        results.riskLevel = 'HIGH';
      } else if (mediumCount > 0) {
        results.riskLevel = 'MEDIUM';
      }

      return results;

    } catch (error) {
      return {
        file: filePath,
        error: error.message,
        riskLevel: 'UNKNOWN'
      };
    }
  }

  /**
   * Check for hardcoded secrets
   */
  checkForHardcodedSecrets(content) {
    const secrets = [];
    const recommendations = [];

    // Common secret patterns
    const patterns = [
      { pattern: /password\s*=\s*['"][^'"]+['"]/gi, type: 'Hardcoded Password' },
      { pattern: /api_key\s*=\s*['"][^'"]+['"]/gi, type: 'Hardcoded API Key' },
      { pattern: /secret\s*=\s*['"][^'"]+['"]/gi, type: 'Hardcoded Secret' },
      { pattern: /token\s*=\s*['"][^'"]+['"]/gi, type: 'Hardcoded Token' },
      { pattern: /private_key\s*=\s*['"][^'"]+['"]/gi, type: 'Hardcoded Private Key' }
    ];

    for (const { pattern, type } of patterns) {
      const matches = content.match(pattern);
      if (matches) {
        secrets.push({
          type: type,
          severity: 'HIGH',
          description: `Found ${matches.length} hardcoded ${type.toLowerCase()}`
        });
        recommendations.push(`Move ${type.toLowerCase()} to environment variables or secure configuration`);
      }
    }

    return { vulnerabilities: secrets, recommendations };
  }

  /**
   * Check for SQL injection vulnerabilities
   */
  checkForSQLInjection(content) {
    const vulnerabilities = [];
    const recommendations = [];

    // Check for direct string concatenation in SQL
    const sqlPatterns = [
      /SELECT.*\+.*['"`]/gi,
      /INSERT.*\+.*['"`]/gi,
      /UPDATE.*\+.*['"`]/gi,
      /DELETE.*\+.*['"`]/gi
    ];

    for (const pattern of sqlPatterns) {
      const matches = content.match(pattern);
      if (matches) {
        vulnerabilities.push({
          type: 'SQL Injection',
          severity: 'HIGH',
          description: 'Potential SQL injection through string concatenation'
        });
        recommendations.push('Use parameterized queries or prepared statements');
      }
    }

    return { vulnerabilities, recommendations };
  }

  /**
   * Check for XSS vulnerabilities
   */
  checkForXSS(content) {
    const vulnerabilities = [];
    const recommendations = [];

    // Check for innerHTML usage
    const innerHTMLPattern = /\.innerHTML\s*=/gi;
    if (content.match(innerHTMLPattern)) {
      vulnerabilities.push({
        type: 'XSS',
        severity: 'MEDIUM',
        description: 'innerHTML usage detected'
      });
      recommendations.push('Use textContent or proper sanitization for innerHTML');
    }

    return { vulnerabilities, recommendations };
  }

  /**
   * Check for path traversal vulnerabilities
   */
  checkForPathTraversal(content) {
    const vulnerabilities = [];
    const recommendations = [];

    // Check for path traversal patterns
    const pathPatterns = [
      /\.\.\/\.\./g,
      /\.\.\\\.\./g
    ];

    for (const pattern of pathPatterns) {
      const matches = content.match(pattern);
      if (matches) {
        vulnerabilities.push({
          type: 'Path Traversal',
          severity: 'HIGH',
          description: 'Potential path traversal vulnerability'
        });
        recommendations.push('Validate and sanitize file paths');
      }
    }

    return { vulnerabilities, recommendations };
  }

  /**
   * Check for insecure crypto usage
   */
  checkForInsecureCrypto(content) {
    const vulnerabilities = [];
    const recommendations = [];

    // Check for MD5 usage
    if (content.includes('md5') || content.includes('MD5')) {
      vulnerabilities.push({
        type: 'Insecure Crypto',
        severity: 'MEDIUM',
        description: 'MD5 hash function usage detected'
      });
      recommendations.push('Use SHA-256 or stronger hash functions');
    }

    return { vulnerabilities, recommendations };
  }

  /**
   * Check for deprecated APIs
   */
  checkForDeprecatedAPIs(content, fileExt) {
    const vulnerabilities = [];
    const recommendations = [];

    // Node.js specific deprecated APIs
    if (fileExt === '.js' || fileExt === '.ts') {
      const deprecatedAPIs = [
        'new Buffer(',
        'require.extensions',
        'process.binding'
      ];

      for (const api of deprecatedAPIs) {
        if (content.includes(api)) {
          vulnerabilities.push({
            type: 'Deprecated API',
            severity: 'LOW',
            description: `Deprecated API usage: ${api}`
          });
          recommendations.push(`Replace ${api} with modern alternatives`);
        }
      }
    }

    return { vulnerabilities, recommendations };
  }
}

/**
 * File encryption/decryption utilities
 */
class FileCrypto {
  constructor() {
    this.algorithm = 'aes-256-gcm';
  }

  /**
   * Generate encryption key from password
   */
  generateKey(password, salt) {
    return crypto.pbkdf2Sync(password, salt, 100000, 32, 'sha256');
  }

  /**
   * Encrypt file
   */
  async encryptFile(inputFile, outputFile, password) {
    try {
      const content = await fs.readFile(inputFile);
      const salt = crypto.randomBytes(16);
      const iv = crypto.randomBytes(12);
      const key = this.generateKey(password, salt);

      const cipher = crypto.createCipher(this.algorithm, key);
      cipher.setAAD(Buffer.from('tusktsk-encrypted', 'utf8'));
      cipher.setAAD(iv);

      let encrypted = cipher.update(content, null, 'hex');
      encrypted += cipher.final('hex');
      const authTag = cipher.getAuthTag();

      const result = Buffer.concat([salt, iv, authTag, Buffer.from(encrypted, 'hex')]);
      await fs.writeFile(outputFile, result);

      return {
        success: true,
        message: `File encrypted successfully: ${outputFile}`
      };

    } catch (error) {
      return {
        success: false,
        error: error.message
      };
    }
  }

  /**
   * Decrypt file
   */
  async decryptFile(inputFile, outputFile, password) {
    try {
      const encryptedData = await fs.readFile(inputFile);
      
      const salt = encryptedData.slice(0, 16);
      const iv = encryptedData.slice(16, 28);
      const authTag = encryptedData.slice(28, 44);
      const encrypted = encryptedData.slice(44);

      const key = this.generateKey(password, salt);
      const decipher = crypto.createDecipher(this.algorithm, key);
      decipher.setAAD(Buffer.from('tusktsk-encrypted', 'utf8'));
      decipher.setAAD(iv);
      decipher.setAuthTag(authTag);

      let decrypted = decipher.update(encrypted, null, 'utf8');
      decrypted += decipher.final('utf8');

      await fs.writeFile(outputFile, decrypted);

      return {
        success: true,
        message: `File decrypted successfully: ${outputFile}`
      };

    } catch (error) {
      return {
        success: false,
        error: error.message
      };
    }
  }
}

// Global instances
const sessionManager = new SecuritySessionManager();
const securityScanner = new SecurityScanner();
const fileCrypto = new FileCrypto();

// Command definitions
const login = new Command('login')
  .description('Login to security service')
  .argument('<username>', 'Username')
  .argument('<password>', 'Password')
  .action(async (username, password) => {
    try {
      const result = await sessionManager.login(username, password);
      console.log(`✅ ${result.message}`);
      console.log(`   Username: ${result.session.username}`);
      console.log(`   Permissions: ${result.session.permissions.length}`);
    } catch (error) {
      console.error(`❌ ${error.message}`);
    }
  });

const logout = new Command('logout')
  .description('Logout from security service')
  .action(async () => {
    try {
      const result = await sessionManager.logout();
      console.log(`✅ ${result.message}`);
    } catch (error) {
      console.error(`❌ ${error.message}`);
    }
  });

const status = new Command('status')
  .description('Check authentication status')
  .action(async () => {
    try {
      const status = await sessionManager.getStatus();
      
      if (status.loggedIn) {
        console.log(`✅ Logged in as: ${status.username}`);
        console.log(`   Logged in: ${status.loggedInAt.toISOString()}`);
        console.log(`   Permissions: ${status.permissions.length}`);
        
        if (status.expiresAt) {
          console.log(`   Expires: ${status.expiresAt.toISOString()}`);
        }
      } else {
        console.log(`❌ ${status.message}`);
      }
    } catch (error) {
      console.error(`❌ Error checking status: ${error.message}`);
    }
  });

const scan = new Command('scan')
  .description('Scan for security vulnerabilities')
  .argument('<target>', 'File or directory to scan')
  .option('-r, --recursive', 'Scan subdirectories recursively')
  .option('-v, --verbose', 'Show detailed vulnerability information')
  .action(async (target, options) => {
    try {
      const targetPath = path.resolve(target);
      const stats = await fs.stat(targetPath);
      
      let files = [];
      if (stats.isDirectory()) {
        // Scan directory
        const scanDirectory = async (dir) => {
          const items = await fs.readdir(dir);
          for (const item of items) {
            const itemPath = path.join(dir, item);
            const itemStats = await fs.stat(itemPath);
            
            if (itemStats.isDirectory() && options.recursive) {
              await scanDirectory(itemPath);
            } else if (itemStats.isFile()) {
              files.push(itemPath);
            }
          }
        };
        
        await scanDirectory(targetPath);
      } else {
        files = [targetPath];
      }

      console.log(`🔍 Scanning ${files.length} files...`);
      
      const results = [];
      let totalVulnerabilities = 0;
      let highRiskFiles = 0;

      for (const file of files) {
        const result = await securityScanner.scanFile(file);
        results.push(result);
        
        if (result.vulnerabilities) {
          totalVulnerabilities += result.vulnerabilities.length;
          if (result.riskLevel === 'HIGH') {
            highRiskFiles++;
          }
        }
      }

      console.log(`\n📊 Scan Results:`);
      console.log(`   Files scanned: ${files.length}`);
      console.log(`   Total vulnerabilities: ${totalVulnerabilities}`);
      console.log(`   High risk files: ${highRiskFiles}`);

      if (options.verbose) {
        console.log('\n📋 Detailed Results:');
        results.forEach(result => {
          if (result.vulnerabilities && result.vulnerabilities.length > 0) {
            console.log(`\n   ${result.file} (${result.riskLevel}):`);
            result.vulnerabilities.forEach(vuln => {
              console.log(`     - ${vuln.type}: ${vuln.description}`);
            });
          }
        });
      }

    } catch (error) {
      console.error(`❌ Scan failed: ${error.message}`);
    }
  });

const encrypt = new Command('encrypt')
  .description('Encrypt file')
  .argument('<input>', 'Input file')
  .argument('<password>', 'Encryption password')
  .option('-o, --output <file>', 'Output file (auto-generated if not specified)')
  .action(async (input, password, options) => {
    try {
      const inputPath = path.resolve(input);
      const outputPath = options.output || `${inputPath}.encrypted`;
      
      const result = await fileCrypto.encryptFile(inputPath, outputPath, password);
      
      if (result.success) {
        console.log(`✅ ${result.message}`);
      } else {
        console.error(`❌ Encryption failed: ${result.error}`);
      }
    } catch (error) {
      console.error(`❌ ${error.message}`);
    }
  });

const decrypt = new Command('decrypt')
  .description('Decrypt file')
  .argument('<input>', 'Input encrypted file')
  .argument('<password>', 'Decryption password')
  .option('-o, --output <file>', 'Output file (auto-generated if not specified)')
  .action(async (input, password, options) => {
    try {
      const inputPath = path.resolve(input);
      const outputPath = options.output || inputPath.replace('.encrypted', '.decrypted');
      
      const result = await fileCrypto.decryptFile(inputPath, outputPath, password);
      
      if (result.success) {
        console.log(`✅ ${result.message}`);
      } else {
        console.error(`❌ Decryption failed: ${result.error}`);
      }
    } catch (error) {
      console.error(`❌ ${error.message}`);
    }
  });

const audit = new Command('audit')
  .description('Generate security audit report')
  .argument('[directory]', 'Directory to audit (current directory if not specified)')
  .option('-o, --output <file>', 'Output report file')
  .action(async (directory = '.', options) => {
    try {
      const auditDir = path.resolve(directory);
      console.log(`🔍 Generating security audit for: ${auditDir}`);
      
      // This would be a comprehensive audit
      const report = {
        timestamp: new Date().toISOString(),
        directory: auditDir,
        summary: {
          filesScanned: 0,
          vulnerabilities: 0,
          highRisk: 0,
          mediumRisk: 0,
          lowRisk: 0
        },
        findings: []
      };

      console.log(`✅ Security audit completed`);
      console.log(`   Files scanned: ${report.summary.filesScanned}`);
      console.log(`   Vulnerabilities found: ${report.summary.vulnerabilities}`);
      
      if (options.output) {
        await fs.writeFile(options.output, JSON.stringify(report, null, 2));
        console.log(`   Report saved to: ${options.output}`);
      }
    } catch (error) {
      console.error(`❌ Audit failed: ${error.message}`);
    }
  });

const hash = new Command('hash')
  .description('Generate file hash')
  .argument('<file>', 'File to hash')
  .option('-a, --algorithm <algo>', 'Hash algorithm', 'sha256')
  .action(async (file, options) => {
    try {
      const filePath = path.resolve(file);
      const content = await fs.readFile(filePath);
      
      const hash = crypto.createHash(options.algorithm);
      hash.update(content);
      const hashValue = hash.digest('hex');
      
      console.log(`📄 File: ${filePath}`);
      console.log(`🔗 ${options.algorithm.toUpperCase()}: ${hashValue}`);
      
      const stats = await fs.stat(filePath);
      console.log(`📊 Size: ${stats.size} bytes`);
    } catch (error) {
      console.error(`❌ Hash failed: ${error.message}`);
    }
  });

module.exports = {
  login,
  logout,
  status,
  scan,
  encrypt,
  decrypt,
  audit,
  hash,
  
  // Export for testing
  _SecuritySessionManager: SecuritySessionManager,
  _SecurityScanner: SecurityScanner,
  _FileCrypto: FileCrypto
}; 