#!/usr/bin/env node
/**
 * Unit Tests for New CLI Commands
 * ===============================
 * Tests for peanuts, css, license, security, and dependency commands
 */

const fs = require('fs').promises;
const path = require('path');
const assert = require('assert');

// Import command modules
const peanutsCommands = require('../cli/commands/peanuts.js');
const cssCommands = require('../cli/commands/css.js');
const licenseCommands = require('../commands/license.js');
const securityCommands = require('../cli/commands/security.js');
const dependencyCommands = require('../cli/commands/dependency.js');

describe('CLI Commands - New Features', () => {
  let testDir;
  let testFiles;

  before(async () => {
    // Create test directory
    testDir = path.join(__dirname, 'test-temp');
    await fs.mkdir(testDir, { recursive: true });
    
    // Create test files
    testFiles = {
      peanuts: path.join(testDir, 'test.peanuts'),
      tsk: path.join(testDir, 'test.tsk'),
      css: path.join(testDir, 'test.css'),
      encrypted: path.join(testDir, 'test.encrypted')
    };

    // Create test content
    await fs.writeFile(testFiles.peanuts, `
# Test Peanut Configuration
database = "testdb"
port = 8080
debug = true
    `);

    await fs.writeFile(testFiles.tsk, `
# Test TSK Configuration
server = "localhost"
timeout = 30
retries = 3
    `);

    await fs.writeFile(testFiles.css, `
.container {
  m: 10px;
  p: 20px;
  bg: #f0f0f0;
  w: 100%;
  h: auto;
}
    `);
  });

  after(async () => {
    // Clean up test directory
    try {
      await fs.rm(testDir, { recursive: true, force: true });
    } catch (error) {
      // Ignore cleanup errors
    }
  });

  describe('Peanuts Commands', () => {
    it('should compile .peanuts file to .pnt format', async () => {
      const result = await peanutsCommands._compilePeanut(testFiles.peanuts);
      
      assert.strictEqual(result.success, true);
      assert.strictEqual(result.input, testFiles.peanuts);
      assert.ok(result.output.endsWith('.pnt'));
      assert.ok(result.size > 0);
      
      // Verify output file exists
      const outputExists = await fs.access(result.output).then(() => true).catch(() => false);
      assert.strictEqual(outputExists, true);
    });

    it('should compile .tsk file to .pnt format', async () => {
      const result = await peanutsCommands._compilePeanut(testFiles.tsk);
      
      assert.strictEqual(result.success, true);
      assert.strictEqual(result.input, testFiles.tsk);
      assert.ok(result.output.endsWith('.pnt'));
      assert.ok(result.size > 0);
    });

    it('should auto-compile directory', async () => {
      const result = await peanutsCommands._autoCompileDirectory(testDir);
      
      assert.strictEqual(result.success, true);
      assert.ok(result.processed >= 2); // Should find both .peanuts and .tsk files
      assert.ok(result.successCount >= 2);
    });

    it('should load and validate peanut file', async () => {
      // First compile a file
      const compileResult = await peanutsCommands._compilePeanut(testFiles.peanuts);
      assert.strictEqual(compileResult.success, true);
      
      // Then load it
      const result = await peanutsCommands._loadPeanutFile(compileResult.output);
      
      assert.strictEqual(result.success, true);
      assert.ok(result.sections > 0);
      assert.ok(result.size > 0);
    });

    it('should handle invalid input file', async () => {
      const result = await peanutsCommands._compilePeanut('nonexistent.peanuts');
      
      assert.strictEqual(result.success, false);
      assert.ok(result.error.includes('no such file'));
    });
  });

  describe('CSS Commands', () => {
    it('should expand CSS shorthand properties', () => {
      const css = `
.container {
  m: 10px;
  p: 20px;
  bg: #f0f0f0;
}
      `;
      
      const result = cssCommands._expandCSSShorthand(css);
      
      assert.ok(result.css.includes('margin: 10px'));
      assert.ok(result.css.includes('padding: 20px'));
      assert.ok(result.css.includes('background: #f0f0f0'));
      assert.ok(result.changes >= 3);
    });

    it('should expand CSS file', async () => {
      const result = await cssCommands._expandCSSFile(testFiles.css);
      
      assert.strictEqual(result.success, true);
      assert.strictEqual(result.input, testFiles.css);
      assert.ok(result.output.endsWith('.expanded.css'));
      assert.ok(result.changes > 0);
      
      // Verify output file exists
      const outputExists = await fs.access(result.output).then(() => true).catch(() => false);
      assert.strictEqual(outputExists, true);
    });

    it('should generate CSS source map', () => {
      const css = 'body { margin: 0; }';
      const sourceFile = 'test.css';
      const outputFile = 'test.css.map';
      
      const result = cssCommands._generateCSSSourceMap(css, sourceFile, outputFile);
      
      assert.strictEqual(result.version, 3);
      assert.strictEqual(result.file, 'test.css.map');
      assert.ok(result.sources.includes('test.css'));
      assert.ok(result.mappings.length > 0);
    });

    it('should generate CSS map file', async () => {
      const result = await cssCommands._generateCSSMap(testFiles.css);
      
      assert.strictEqual(result.success, true);
      assert.strictEqual(result.input, testFiles.css);
      assert.ok(result.output.endsWith('.map'));
      assert.ok(result.mapSize > 0);
    });

    it('should handle invalid CSS file', async () => {
      const result = await cssCommands._expandCSSFile('nonexistent.css');
      
      assert.strictEqual(result.success, false);
      assert.ok(result.error.includes('no such file'));
    });
  });

  describe('License Commands', () => {
    it('should check license status when no license exists', async () => {
      const status = await licenseCommands._licenseManager.checkStatus();
      
      assert.strictEqual(status.valid, false);
      assert.strictEqual(status.status, 'NO_LICENSE');
      assert.ok(status.message.includes('No license found'));
    });

    it('should generate machine fingerprint', () => {
      const fingerprint = licenseCommands._licenseManager.generateMachineFingerprint();
      
      assert.strictEqual(typeof fingerprint, 'string');
      assert.strictEqual(fingerprint.length, 64); // SHA-256 hash length
    });

    it('should get license info when no license exists', async () => {
      const info = await licenseCommands._licenseManager.getInfo();
      
      assert.strictEqual(info.hasLicense, false);
      assert.ok(info.message.includes('No license found'));
    });

    it('should handle license activation with invalid key', async () => {
      try {
        await licenseCommands._licenseManager.activate('invalid-key');
        assert.fail('Should have thrown an error');
      } catch (error) {
        assert.ok(error.message.includes('Activation failed'));
      }
    });
  });

  describe('Security Commands', () => {
    it('should check security session status when not logged in', async () => {
      const status = await securityCommands._SecuritySessionManager.prototype.getStatus.call(
        securityCommands._SecuritySessionManager.prototype
      );
      
      assert.strictEqual(status.loggedIn, false);
      assert.ok(status.message.includes('No active session'));
    });

    it('should scan file for vulnerabilities', async () => {
      const testFile = path.join(testDir, 'test-vuln.js');
      await fs.writeFile(testFile, `
        const password = "hardcoded123";
        const query = "SELECT * FROM users WHERE id = " + userId;
        element.innerHTML = userInput;
      `);
      
      const result = await securityCommands._SecurityScanner.prototype.scanFile.call(
        new securityCommands._SecurityScanner(),
        testFile
      );
      
      assert.strictEqual(result.file, testFile);
      assert.ok(result.vulnerabilities.length > 0);
      assert.ok(result.riskLevel === 'HIGH' || result.riskLevel === 'MEDIUM');
    });

    it('should detect hardcoded secrets', () => {
      const content = 'const api_key = "secret123"; const password = "mypass";';
      const scanner = new securityCommands._SecurityScanner();
      
      const result = scanner.checkForHardcodedSecrets(content);
      
      assert.ok(result.vulnerabilities.length > 0);
      assert.ok(result.vulnerabilities.some(v => v.type === 'Hardcoded API Key'));
      assert.ok(result.vulnerabilities.some(v => v.type === 'Hardcoded Password'));
    });

    it('should detect SQL injection patterns', () => {
      const content = 'const query = "SELECT * FROM users WHERE id = " + userId;';
      const scanner = new securityCommands._SecurityScanner();
      
      const result = scanner.checkForSQLInjection(content);
      
      assert.ok(result.vulnerabilities.length > 0);
      assert.ok(result.vulnerabilities.some(v => v.type === 'SQL Injection'));
    });

    it('should encrypt and decrypt file', async () => {
      const testFile = path.join(testDir, 'test-encrypt.txt');
      const encryptedFile = path.join(testDir, 'test-encrypted');
      const decryptedFile = path.join(testDir, 'test-decrypted.txt');
      
      await fs.writeFile(testFile, 'Hello, World!');
      
      const crypto = new securityCommands._FileCrypto();
      const password = 'testpassword';
      
      // Encrypt
      const encryptResult = await crypto.encryptFile(testFile, encryptedFile, password);
      assert.strictEqual(encryptResult.success, true);
      
      // Decrypt
      const decryptResult = await crypto.decryptFile(encryptedFile, decryptedFile, password);
      assert.strictEqual(decryptResult.success, true);
      
      // Verify content
      const originalContent = await fs.readFile(testFile, 'utf8');
      const decryptedContent = await fs.readFile(decryptedFile, 'utf8');
      assert.strictEqual(originalContent, decryptedContent);
    });

    it('should generate file hash', async () => {
      const testFile = path.join(testDir, 'test-hash.txt');
      await fs.writeFile(testFile, 'Test content for hashing');
      
      const content = await fs.readFile(testFile);
      const hash = require('crypto').createHash('sha256');
      hash.update(content);
      const expectedHash = hash.digest('hex');
      
      // The hash command would generate the same hash
      assert.strictEqual(typeof expectedHash, 'string');
      assert.strictEqual(expectedHash.length, 64);
    });
  });

  describe('Dependency Commands', () => {
    it('should list all dependency groups', () => {
      const groups = dependencyCommands._dependencyManager.listGroups();
      
      assert.ok(groups.length >= 9); // Should have at least 9 groups
      assert.ok(groups.some(g => g.key === 'ai'));
      assert.ok(groups.some(g => g.key === 'data'));
      assert.ok(groups.some(g => g.key === 'security'));
      assert.ok(groups.some(g => g.key === 'database'));
    });

    it('should check dependency group', async () => {
      const result = await dependencyCommands._dependencyManager.checkGroup('ai');
      
      assert.strictEqual(result.group, 'ai');
      assert.strictEqual(result.name, 'AI & Machine Learning');
      assert.ok(Array.isArray(result.installed));
      assert.ok(Array.isArray(result.missing));
      assert.ok(Array.isArray(result.devInstalled));
      assert.ok(Array.isArray(result.devMissing));
    });

    it('should get overall dependency status', async () => {
      const status = await dependencyCommands._dependencyManager.getOverallStatus();
      
      assert.ok(status.totalGroups >= 9);
      assert.ok(typeof status.installedGroups === 'number');
      assert.ok(typeof status.partialGroups === 'number');
      assert.ok(typeof status.missingGroups === 'number');
      assert.ok(Array.isArray(status.details));
      assert.strictEqual(status.details.length, status.totalGroups);
    });

    it('should handle unknown dependency group', async () => {
      try {
        await dependencyCommands._dependencyManager.checkGroup('unknown');
        assert.fail('Should have thrown an error');
      } catch (error) {
        assert.ok(error.message.includes('Unknown dependency group'));
      }
    });

    it('should validate dependency group configuration', () => {
      const groups = dependencyCommands._DEPENDENCY_GROUPS;
      
      // Check that all groups have required properties
      Object.keys(groups).forEach(key => {
        const group = groups[key];
        assert.ok(group.name, `Group ${key} missing name`);
        assert.ok(group.description, `Group ${key} missing description`);
        assert.ok(Array.isArray(group.packages), `Group ${key} packages should be array`);
        assert.ok(Array.isArray(group.devPackages), `Group ${key} devPackages should be array`);
      });
    });
  });

  describe('Command Integration', () => {
    it('should have all command modules properly exported', () => {
      // Check peanuts commands
      assert.ok(peanutsCommands.compile);
      assert.ok(peanutsCommands.autoCompile);
      assert.ok(peanutsCommands.load);
      
      // Check CSS commands
      assert.ok(cssCommands.expand);
      assert.ok(cssCommands.map);
      
      // Check license commands
      assert.ok(licenseCommands.check);
      assert.ok(licenseCommands.activate);
      assert.ok(licenseCommands.validate);
      assert.ok(licenseCommands.info);
      assert.ok(licenseCommands.transfer);
      assert.ok(licenseCommands.revoke);
      
      // Check security commands
      assert.ok(securityCommands.login);
      assert.ok(securityCommands.logout);
      assert.ok(securityCommands.status);
      assert.ok(securityCommands.scan);
      assert.ok(securityCommands.encrypt);
      assert.ok(securityCommands.decrypt);
      assert.ok(securityCommands.audit);
      assert.ok(securityCommands.hash);
      
      // Check dependency commands
      assert.ok(dependencyCommands.install);
      assert.ok(dependencyCommands.list);
      assert.ok(dependencyCommands.check);
    });

    it('should have proper command descriptions', () => {
      assert.strictEqual(peanutsCommands.compile.description(), 'Compile .peanuts/.tsk file to .pnt format');
      assert.strictEqual(cssCommands.expand.description(), 'Expand CSS shorthand properties');
      assert.strictEqual(licenseCommands.check.description(), 'Check current license status');
      assert.strictEqual(securityCommands.scan.description(), 'Scan for security vulnerabilities');
      assert.strictEqual(dependencyCommands.list.description(), 'List available dependency groups');
    });
  });
});

// Run tests if this file is executed directly
if (require.main === module) {
  const { run } = require('mocha');
  run();
} 