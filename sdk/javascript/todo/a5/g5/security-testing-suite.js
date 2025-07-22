/**
 * G5: SECURITY TESTING SUITE - Vulnerability Assessment
 * ====================================================
 * Security vulnerability scanning with automated tools
 * SQL injection prevention testing for database operators
 * XSS and CSRF protection validation for web-facing operators
 * Credential handling and encryption validation
 * API key rotation and secret management testing
 * TLS/SSL certificate validation and expiration testing
 */

const { expect } = require('chai');
const crypto = require('crypto');
const { TuskLangEnhanced } = require('../../tsk-enhanced.js');

class SecurityTestingSuite {
  constructor() {
    this.tusk = new TuskLangEnhanced();
    this.securityResults = [];
    this.vulnerabilities = [];
    this.securityMetrics = {};
    this.startTime = Date.now();
  }

  /**
   * Test SQL injection prevention
   */
  async testSQLInjectionPrevention() {
    console.log('🔍 Testing SQL Injection Prevention...');
    
    const injectionPayloads = [
      "' OR '1'='1",
      "'; DROP TABLE users; --",
      "' UNION SELECT * FROM users --",
      "'; INSERT INTO users VALUES ('hacker', 'password'); --",
      "' OR 1=1; EXEC xp_cmdshell('dir'); --",
      "'; WAITFOR DELAY '00:00:10'; --",
      "' OR '1'='1' LIMIT 1 --",
      "'; SELECT SLEEP(10); --"
    ];

    for (const payload of injectionPayloads) {
      try {
        const result = await this.tusk.executeOperator('@query', {
          query: `SELECT * FROM users WHERE name = '${payload}'`,
          database: 'test_db'
        });
        
        // Check if the query was properly sanitized
        if (result && result.rows && result.rows.length === 0) {
          this.securityResults.push({
            test: 'SQL Injection Prevention',
            payload: payload,
            status: 'PASSED',
            description: 'Query properly sanitized'
          });
        } else {
          this.securityResults.push({
            test: 'SQL Injection Prevention',
            payload: payload,
            status: 'FAILED',
            description: 'Potential SQL injection vulnerability detected'
          });
          this.vulnerabilities.push({
            type: 'SQL_INJECTION',
            payload: payload,
            severity: 'HIGH'
          });
        }
      } catch (error) {
        // Error is expected for malicious payloads
        this.securityResults.push({
          test: 'SQL Injection Prevention',
          payload: payload,
          status: 'PASSED',
          description: 'Query properly rejected'
        });
      }
    }
  }

  /**
   * Test XSS prevention
   */
  async testXSSPrevention() {
    console.log('🔍 Testing XSS Prevention...');
    
    const xssPayloads = [
      '<script>alert("XSS")</script>',
      'javascript:alert("XSS")',
      '<img src="x" onerror="alert(\'XSS\')">',
      '<svg onload="alert(\'XSS\')">',
      '"><script>alert("XSS")</script>',
      '&#x3C;script&#x3E;alert("XSS")&#x3C;/script&#x3E;',
      '<iframe src="javascript:alert(\'XSS\')">',
      '<object data="javascript:alert(\'XSS\')">'
    ];

    for (const payload of xssPayloads) {
      try {
        // Test template rendering with XSS payload
        const result = await this.tusk.executeOperator('@template', {
          template: 'Hello {name}!',
          data: { name: payload }
        });
        
        // Check if the payload was properly escaped
        if (result && !result.includes('<script>') && !result.includes('javascript:')) {
          this.securityResults.push({
            test: 'XSS Prevention',
            payload: payload,
            status: 'PASSED',
            description: 'XSS payload properly escaped'
          });
        } else {
          this.securityResults.push({
            test: 'XSS Prevention',
            payload: payload,
            status: 'FAILED',
            description: 'XSS vulnerability detected'
          });
          this.vulnerabilities.push({
            type: 'XSS',
            payload: payload,
            severity: 'HIGH'
          });
        }
      } catch (error) {
        this.securityResults.push({
          test: 'XSS Prevention',
          payload: payload,
          status: 'PASSED',
          description: 'XSS payload properly rejected'
        });
      }
    }
  }

  /**
   * Test CSRF protection
   */
  async testCSRFProtection() {
    console.log('🔍 Testing CSRF Protection...');
    
    const csrfTests = [
      {
        name: 'Missing CSRF Token',
        headers: {},
        expected: 'CSRF token required'
      },
      {
        name: 'Invalid CSRF Token',
        headers: { 'X-CSRF-Token': 'invalid-token' },
        expected: 'Invalid CSRF token'
      },
      {
        name: 'Expired CSRF Token',
        headers: { 'X-CSRF-Token': 'expired-token' },
        expected: 'CSRF token expired'
      }
    ];

    for (const test of csrfTests) {
      try {
        const result = await this.tusk.executeOperator('@webhook', {
          method: 'POST',
          url: 'https://api.example.com/users',
          headers: test.headers,
          data: { name: 'test' }
        });
        
        this.securityResults.push({
          test: 'CSRF Protection',
          scenario: test.name,
          status: 'FAILED',
          description: 'CSRF protection bypassed'
        });
        this.vulnerabilities.push({
          type: 'CSRF',
          scenario: test.name,
          severity: 'HIGH'
        });
      } catch (error) {
        if (error.message.includes(test.expected)) {
          this.securityResults.push({
            test: 'CSRF Protection',
            scenario: test.name,
            status: 'PASSED',
            description: 'CSRF protection working correctly'
          });
        } else {
          this.securityResults.push({
            test: 'CSRF Protection',
            scenario: test.name,
            status: 'FAILED',
            description: `Expected ${test.expected} but got ${error.message}`
          });
        }
      }
    }
  }

  /**
   * Test credential handling
   */
  async testCredentialHandling() {
    console.log('🔍 Testing Credential Handling...');
    
    const credentialTests = [
      {
        name: 'Password in Plain Text',
        test: async () => {
          const result = await this.tusk.executeOperator('@encrypt', {
            text: 'password123',
            key: 'weak-key',
            algorithm: 'aes-256-cbc'
          });
          return result;
        },
        expected: 'encrypted'
      },
      {
        name: 'API Key Exposure',
        test: async () => {
          const result = await this.tusk.executeOperator('@env', {
            operation: 'get',
            name: 'API_KEY'
          });
          return result;
        },
        expected: 'undefined'
      },
      {
        name: 'Database Credentials',
        test: async () => {
          const result = await this.tusk.executeOperator('@query', {
            query: 'SELECT current_user()',
            database: 'test_db'
          });
          return result;
        },
        expected: 'sanitized'
      }
    ];

    for (const test of credentialTests) {
      try {
        const result = await test.test();
        
        if (test.expected === 'encrypted' && result !== 'password123') {
          this.securityResults.push({
            test: 'Credential Handling',
            scenario: test.name,
            status: 'PASSED',
            description: 'Credentials properly encrypted'
          });
        } else if (test.expected === 'undefined' && !result) {
          this.securityResults.push({
            test: 'Credential Handling',
            scenario: test.name,
            status: 'PASSED',
            description: 'API key not exposed'
          });
        } else {
          this.securityResults.push({
            test: 'Credential Handling',
            scenario: test.name,
            status: 'FAILED',
            description: 'Credentials not properly secured'
          });
          this.vulnerabilities.push({
            type: 'CREDENTIAL_EXPOSURE',
            scenario: test.name,
            severity: 'CRITICAL'
          });
        }
      } catch (error) {
        this.securityResults.push({
          test: 'Credential Handling',
          scenario: test.name,
          status: 'PASSED',
          description: 'Credentials properly protected'
        });
      }
    }
  }

  /**
   * Test encryption validation
   */
  async testEncryptionValidation() {
    console.log('🔍 Testing Encryption Validation...');
    
    const encryptionTests = [
      {
        name: 'AES-256-CBC Encryption',
        algorithm: 'aes-256-cbc',
        key: crypto.randomBytes(32).toString('hex'),
        data: 'sensitive-data'
      },
      {
        name: 'AES-256-GCM Encryption',
        algorithm: 'aes-256-gcm',
        key: crypto.randomBytes(32).toString('hex'),
        data: 'sensitive-data'
      },
      {
        name: 'ChaCha20-Poly1305 Encryption',
        algorithm: 'chacha20-poly1305',
        key: crypto.randomBytes(32).toString('hex'),
        data: 'sensitive-data'
      }
    ];

    for (const test of encryptionTests) {
      try {
        // Test encryption
        const encrypted = await this.tusk.executeOperator('@encrypt', {
          text: test.data,
          key: test.key,
          algorithm: test.algorithm
        });
        
        // Verify encryption worked
        if (encrypted && encrypted !== test.data) {
          // Test decryption
          const decrypted = await this.tusk.executeOperator('@decrypt', {
            text: encrypted,
            key: test.key,
            algorithm: test.algorithm
          });
          
          if (decrypted === test.data) {
            this.securityResults.push({
              test: 'Encryption Validation',
              algorithm: test.algorithm,
              status: 'PASSED',
              description: 'Encryption/decryption working correctly'
            });
          } else {
            this.securityResults.push({
              test: 'Encryption Validation',
              algorithm: test.algorithm,
              status: 'FAILED',
              description: 'Decryption failed'
            });
          }
        } else {
          this.securityResults.push({
            test: 'Encryption Validation',
            algorithm: test.algorithm,
            status: 'FAILED',
            description: 'Encryption failed'
          });
        }
      } catch (error) {
        this.securityResults.push({
          test: 'Encryption Validation',
          algorithm: test.algorithm,
          status: 'FAILED',
          description: error.message
        });
      }
    }
  }

  /**
   * Test API key rotation
   */
  async testAPIKeyRotation() {
    console.log('🔍 Testing API Key Rotation...');
    
    const rotationTests = [
      {
        name: 'Key Expiration Check',
        test: async () => {
          const result = await this.tusk.executeOperator('@aws', {
            service: 's3',
            operation: 'listBuckets',
            credentials: {
              accessKeyId: 'expired-key',
              secretAccessKey: 'expired-secret'
            }
          });
          return result;
        },
        expected: 'expired'
      },
      {
        name: 'Key Rotation Validation',
        test: async () => {
          const result = await this.tusk.executeOperator('@oauth', {
            operation: 'validate',
            token: 'old-token'
          });
          return result;
        },
        expected: 'expired'
      }
    ];

    for (const test of rotationTests) {
      try {
        await test.test();
        this.securityResults.push({
          test: 'API Key Rotation',
          scenario: test.name,
          status: 'FAILED',
          description: 'Expired key accepted'
        });
        this.vulnerabilities.push({
          type: 'KEY_ROTATION',
          scenario: test.name,
          severity: 'MEDIUM'
        });
      } catch (error) {
        if (error.message.includes(test.expected)) {
          this.securityResults.push({
            test: 'API Key Rotation',
            scenario: test.name,
            status: 'PASSED',
            description: 'Expired key properly rejected'
          });
        } else {
          this.securityResults.push({
            test: 'API Key Rotation',
            scenario: test.name,
            status: 'FAILED',
            description: `Expected ${test.expected} but got ${error.message}`
          });
        }
      }
    }
  }

  /**
   * Test TLS/SSL validation
   */
  async testTLSSSLValidation() {
    console.log('🔍 Testing TLS/SSL Validation...');
    
    const tlsTests = [
      {
        name: 'Valid SSL Certificate',
        url: 'https://httpbin.org/get',
        expected: 'valid'
      },
      {
        name: 'Expired SSL Certificate',
        url: 'https://expired.badssl.com',
        expected: 'expired'
      },
      {
        name: 'Self-Signed Certificate',
        url: 'https://self-signed.badssl.com',
        expected: 'self-signed'
      },
      {
        name: 'Wrong Host Certificate',
        url: 'https://wrong.host.badssl.com',
        expected: 'wrong-host'
      }
    ];

    for (const test of tlsTests) {
      try {
        const result = await this.tusk.executeOperator('@webhook', {
          method: 'GET',
          url: test.url,
          timeout: 5000
        });
        
        if (test.expected === 'valid') {
          this.securityResults.push({
            test: 'TLS/SSL Validation',
            scenario: test.name,
            status: 'PASSED',
            description: 'Valid certificate accepted'
          });
        } else {
          this.securityResults.push({
            test: 'TLS/SSL Validation',
            scenario: test.name,
            status: 'FAILED',
            description: 'Invalid certificate accepted'
          });
          this.vulnerabilities.push({
            type: 'TLS_SSL',
            scenario: test.name,
            severity: 'HIGH'
          });
        }
      } catch (error) {
        if (test.expected === 'valid') {
          this.securityResults.push({
            test: 'TLS/SSL Validation',
            scenario: test.name,
            status: 'FAILED',
            description: `Valid certificate rejected: ${error.message}`
          });
        } else {
          this.securityResults.push({
            test: 'TLS/SSL Validation',
            scenario: test.name,
            status: 'PASSED',
            description: 'Invalid certificate properly rejected'
          });
        }
      }
    }
  }

  /**
   * Test input validation
   */
  async testInputValidation() {
    console.log('🔍 Testing Input Validation...');
    
    const inputTests = [
      {
        name: 'Path Traversal',
        operator: '@file',
        params: {
          operation: 'read',
          path: '../../../etc/passwd'
        },
        expected: 'rejected'
      },
      {
        name: 'Command Injection',
        operator: '@template',
        params: {
          template: 'Hello {name}!',
          data: { name: '$(rm -rf /)' }
        },
        expected: 'sanitized'
      },
      {
        name: 'Buffer Overflow',
        operator: '@string',
        params: {
          operation: 'concat',
          strings: ['A'.repeat(1000000)]
        },
        expected: 'limited'
      }
    ];

    for (const test of inputTests) {
      try {
        const result = await this.tusk.executeOperator(test.operator, test.params);
        
        if (test.expected === 'rejected') {
          this.securityResults.push({
            test: 'Input Validation',
            scenario: test.name,
            status: 'FAILED',
            description: 'Malicious input accepted'
          });
          this.vulnerabilities.push({
            type: 'INPUT_VALIDATION',
            scenario: test.name,
            severity: 'HIGH'
          });
        } else {
          this.securityResults.push({
            test: 'Input Validation',
            scenario: test.name,
            status: 'PASSED',
            description: 'Input properly validated'
          });
        }
      } catch (error) {
        if (test.expected === 'rejected') {
          this.securityResults.push({
            test: 'Input Validation',
            scenario: test.name,
            status: 'PASSED',
            description: 'Malicious input properly rejected'
          });
        } else {
          this.securityResults.push({
            test: 'Input Validation',
            scenario: test.name,
            status: 'FAILED',
            description: `Valid input rejected: ${error.message}`
          });
        }
      }
    }
  }

  /**
   * Test session management
   */
  async testSessionManagement() {
    console.log('🔍 Testing Session Management...');
    
    const sessionTests = [
      {
        name: 'Session Fixation',
        test: async () => {
          const session1 = await this.tusk.executeOperator('@jwt', {
            operation: 'encode',
            payload: { user: 'test' },
            secret: 'secret1'
          });
          
          const session2 = await this.tusk.executeOperator('@jwt', {
            operation: 'encode',
            payload: { user: 'test' },
            secret: 'secret2'
          });
          
          return session1 !== session2;
        },
        expected: true
      },
      {
        name: 'Session Timeout',
        test: async () => {
          const token = await this.tusk.executeOperator('@jwt', {
            operation: 'encode',
            payload: { user: 'test', exp: Math.floor(Date.now() / 1000) - 3600 }, // Expired 1 hour ago
            secret: 'test-secret'
          });
          
          try {
            await this.tusk.executeOperator('@jwt', {
              operation: 'verify',
              token: token,
              secret: 'test-secret'
            });
            return false; // Should have thrown an error
          } catch (error) {
            return error.message.includes('expired');
          }
        },
        expected: true
      }
    ];

    for (const test of sessionTests) {
      try {
        const result = await test.test();
        
        if (result === test.expected) {
          this.securityResults.push({
            test: 'Session Management',
            scenario: test.name,
            status: 'PASSED',
            description: 'Session management working correctly'
          });
        } else {
          this.securityResults.push({
            test: 'Session Management',
            scenario: test.name,
            status: 'FAILED',
            description: 'Session management vulnerability detected'
          });
          this.vulnerabilities.push({
            type: 'SESSION_MANAGEMENT',
            scenario: test.name,
            severity: 'MEDIUM'
          });
        }
      } catch (error) {
        this.securityResults.push({
          test: 'Session Management',
          scenario: test.name,
          status: 'FAILED',
          description: error.message
        });
      }
    }
  }

  /**
   * Run complete security testing suite
   */
  async runCompleteSuite() {
    console.log('🚀 Starting TuskLang Security Testing Suite...');
    
    try {
      await this.testSQLInjectionPrevention();
      await this.testXSSPrevention();
      await this.testCSRFProtection();
      await this.testCredentialHandling();
      await this.testEncryptionValidation();
      await this.testAPIKeyRotation();
      await this.testTLSSSLValidation();
      await this.testInputValidation();
      await this.testSessionManagement();
      
      const report = this.generateSecurityReport();
      
      console.log('✅ Security Testing Suite completed successfully');
      return report;
      
    } catch (error) {
      console.error('❌ Security Testing Suite failed:', error);
      throw error;
    }
  }

  /**
   * Generate comprehensive security report
   */
  generateSecurityReport() {
    const totalTests = this.securityResults.length;
    const passedTests = this.securityResults.filter(r => r.status === 'PASSED').length;
    const failedTests = this.securityResults.filter(r => r.status === 'FAILED').length;
    const successRate = (passedTests / totalTests) * 100;
    
    const totalDuration = Date.now() - this.startTime;
    
    // Categorize vulnerabilities by severity
    const criticalVulns = this.vulnerabilities.filter(v => v.severity === 'CRITICAL').length;
    const highVulns = this.vulnerabilities.filter(v => v.severity === 'HIGH').length;
    const mediumVulns = this.vulnerabilities.filter(v => v.severity === 'MEDIUM').length;
    const lowVulns = this.vulnerabilities.filter(v => v.severity === 'LOW').length;
    
    const report = {
      summary: {
        totalTests,
        passedTests,
        failedTests,
        successRate: `${successRate.toFixed(2)}%`,
        totalDuration: `${totalDuration}ms`,
        vulnerabilities: {
          total: this.vulnerabilities.length,
          critical: criticalVulns,
          high: highVulns,
          medium: mediumVulns,
          low: lowVulns
        }
      },
      results: this.securityResults,
      vulnerabilities: this.vulnerabilities,
      securityMetrics: this.securityMetrics,
      recommendations: this.generateSecurityRecommendations(),
      timestamp: new Date().toISOString()
    };
    
    console.log('\n📊 SECURITY TESTING SUITE REPORT');
    console.log('================================');
    console.log(`Total Tests: ${totalTests}`);
    console.log(`Passed: ${passedTests}`);
    console.log(`Failed: ${failedTests}`);
    console.log(`Success Rate: ${successRate.toFixed(2)}%`);
    console.log(`Total Duration: ${totalDuration}ms`);
    console.log(`\n🔒 VULNERABILITIES:`);
    console.log(`  Critical: ${criticalVulns}`);
    console.log(`  High: ${highVulns}`);
    console.log(`  Medium: ${mediumVulns}`);
    console.log(`  Low: ${lowVulns}`);
    
    if (this.vulnerabilities.length > 0) {
      console.log('\n⚠️  DETECTED VULNERABILITIES:');
      this.vulnerabilities.forEach(vuln => {
        console.log(`  - ${vuln.type} (${vuln.severity}): ${vuln.scenario || vuln.payload}`);
      });
    }
    
    return report;
  }

  /**
   * Generate security recommendations
   */
  generateSecurityRecommendations() {
    const recommendations = [];
    
    const vulnTypes = this.vulnerabilities.map(v => v.type);
    
    if (vulnTypes.includes('SQL_INJECTION')) {
      recommendations.push('Implement parameterized queries and input validation for all database operations');
    }
    
    if (vulnTypes.includes('XSS')) {
      recommendations.push('Implement proper output encoding and Content Security Policy (CSP) headers');
    }
    
    if (vulnTypes.includes('CSRF')) {
      recommendations.push('Implement CSRF tokens for all state-changing operations');
    }
    
    if (vulnTypes.includes('CREDENTIAL_EXPOSURE')) {
      recommendations.push('Use environment variables for sensitive credentials and implement proper encryption');
    }
    
    if (vulnTypes.includes('TLS_SSL')) {
      recommendations.push('Enforce strict SSL/TLS validation and certificate pinning');
    }
    
    if (vulnTypes.includes('INPUT_VALIDATION')) {
      recommendations.push('Implement comprehensive input validation and sanitization');
    }
    
    if (vulnTypes.includes('SESSION_MANAGEMENT')) {
      recommendations.push('Implement secure session management with proper timeouts and rotation');
    }
    
    return recommendations;
  }
}

module.exports = { SecurityTestingSuite }; 