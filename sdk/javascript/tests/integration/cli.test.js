#!/usr/bin/env node
/**
 * Integration Tests for TuskLang CLI
 * ===================================
 * Test CLI commands with real file system and database operations
 */

const { execSync, spawn } = require('child_process');
const fs = require('fs');
const path = require('path');
const { Pool } = require('pg');

describe('TuskLang CLI Integration Tests', () => {
  let testDir;
  let dbPool;

  beforeAll(async () => {
    // Create test directory
    testDir = path.join(__dirname, 'test-files');
    if (!fs.existsSync(testDir)) {
      fs.mkdirSync(testDir, { recursive: true });
    }

    // Set up test database
    dbPool = new Pool({
      host: process.env.TEST_DB_HOST || 'localhost',
      port: process.env.TEST_DB_PORT || 5432,
      database: process.env.TEST_DB_NAME || 'tusklang_test',
      user: process.env.TEST_DB_USER || 'postgres',
      password: process.env.TEST_DB_PASSWORD || 'password'
    });

    // Create test tables
    await dbPool.query(`
      CREATE TABLE IF NOT EXISTS users (
        id SERIAL PRIMARY KEY,
        username VARCHAR(50) UNIQUE NOT NULL,
        email VARCHAR(100) UNIQUE NOT NULL,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `);

    await dbPool.query(`
      CREATE TABLE IF NOT EXISTS posts (
        id SERIAL PRIMARY KEY,
        user_id INTEGER REFERENCES users(id),
        title VARCHAR(200) NOT NULL,
        content TEXT,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `);

    // Insert test data
    await dbPool.query(`
      INSERT INTO users (username, email) VALUES 
        ('testuser1', 'test1@example.com'),
        ('testuser2', 'test2@example.com')
      ON CONFLICT (username) DO NOTHING
    `);

    await dbPool.query(`
      INSERT INTO posts (user_id, title, content) VALUES 
        (1, 'Test Post 1', 'This is test content 1'),
        (2, 'Test Post 2', 'This is test content 2')
      ON CONFLICT DO NOTHING
    `);
  });

  afterAll(async () => {
    // Clean up test directory
    if (fs.existsSync(testDir)) {
      fs.rmSync(testDir, { recursive: true, force: true });
    }

    // Close database connection
    if (dbPool) {
      await dbPool.end();
    }
  });

  beforeEach(() => {
    // Clean up test files before each test
    const files = fs.readdirSync(testDir);
    files.forEach(file => {
      if (file.endsWith('.tsk') || file.endsWith('.json') || file.endsWith('.pnt')) {
        fs.unlinkSync(path.join(testDir, file));
      }
    });
  });

  describe('Basic CLI Commands', () => {
    test('should show help information', () => {
      const output = execSync('node ../../cli/main.js --help', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Usage: tsk');
      expect(output).toContain('Commands:');
      expect(output).toContain('parse');
      expect(output).toContain('validate');
    });

    test('should show version information', () => {
      const output = execSync('node ../../cli/main.js --version', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('TuskLang');
      expect(output).toMatch(/\d+\.\d+\.\d+/);
    });
  });

  describe('Parse Command', () => {
    test('should parse configuration file', () => {
      // Create test configuration file
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        
        [database]
        host: "localhost"
        port: 5432
        
        [server]
        host: "0.0.0.0"
        port: 8080
      `;

      fs.writeFileSync(path.join(testDir, 'test-config.tsk'), configContent);

      const output = execSync('node ../../cli/main.js parse test-config.tsk', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      const parsed = JSON.parse(output);
      expect(parsed.name).toBe('TestApp');
      expect(parsed.version).toBe('1.0.0');
      expect(parsed.database.host).toBe('localhost');
      expect(parsed.database.port).toBe(5432);
      expect(parsed.server.host).toBe('0.0.0.0');
      expect(parsed.server.port).toBe(8080);
    });

    test('should handle parse errors gracefully', () => {
      // Create invalid configuration file
      const invalidConfig = `
        name: "TestApp"
        invalid syntax here
      `;

      fs.writeFileSync(path.join(testDir, 'invalid-config.tsk'), invalidConfig);

      expect(() => {
        execSync('node ../../cli/main.js parse invalid-config.tsk', { 
          cwd: testDir,
          encoding: 'utf8'
        });
      }).toThrow();
    });
  });

  describe('Validate Command', () => {
    test('should validate valid configuration', () => {
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        port: 8080
      `;

      fs.writeFileSync(path.join(testDir, 'valid-config.tsk'), configContent);

      const output = execSync('node ../../cli/main.js validate valid-config.tsk', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Configuration is valid');
    });

    test('should report validation errors', () => {
      const configContent = `
        # Missing required fields
        port: 99999  # Invalid port
      `;

      fs.writeFileSync(path.join(testDir, 'invalid-config.tsk'), configContent);

      expect(() => {
        execSync('node ../../cli/main.js validate invalid-config.tsk', { 
          cwd: testDir,
          encoding: 'utf8'
        });
      }).toThrow();
    });
  });

  describe('Get/Set Commands', () => {
    test('should get configuration value by path', () => {
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        
        [database]
        host: "localhost"
        port: 5432
      `;

      fs.writeFileSync(path.join(testDir, 'test-config.tsk'), configContent);

      const output = execSync('node ../../cli/main.js get test-config.tsk database.host', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output.trim()).toBe('localhost');
    });

    test('should set configuration value by path', () => {
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        
        [database]
        host: "localhost"
        port: 5432
      `;

      fs.writeFileSync(path.join(testDir, 'test-config.tsk'), configContent);

      execSync('node ../../cli/main.js set test-config.tsk database.port 5433', { 
        cwd: testDir
      });

      const updatedContent = fs.readFileSync(path.join(testDir, 'test-config.tsk'), 'utf8');
      expect(updatedContent).toContain('port: 5433');
    });
  });

  describe('Convert Command', () => {
    test('should convert TSK to JSON', () => {
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        
        [database]
        host: "localhost"
        port: 5432
      `;

      fs.writeFileSync(path.join(testDir, 'test-config.tsk'), configContent);

      execSync('node ../../cli/main.js convert -i test-config.tsk -o test-config.json', { 
        cwd: testDir
      });

      expect(fs.existsSync(path.join(testDir, 'test-config.json'))).toBe(true);

      const jsonContent = JSON.parse(fs.readFileSync(path.join(testDir, 'test-config.json'), 'utf8'));
      expect(jsonContent.name).toBe('TestApp');
      expect(jsonContent.version).toBe('1.0.0');
      expect(jsonContent.database.host).toBe('localhost');
      expect(jsonContent.database.port).toBe(5432);
    });

    test('should convert JSON to TSK', () => {
      const jsonContent = {
        name: 'TestApp',
        version: '1.0.0',
        database: {
          host: 'localhost',
          port: 5432
        }
      };

      fs.writeFileSync(path.join(testDir, 'test-config.json'), JSON.stringify(jsonContent, null, 2));

      execSync('node ../../cli/main.js convert -i test-config.json -o test-config.tsk', { 
        cwd: testDir
      });

      expect(fs.existsSync(path.join(testDir, 'test-config.tsk'))).toBe(true);

      const tskContent = fs.readFileSync(path.join(testDir, 'test-config.tsk'), 'utf8');
      expect(tskContent).toContain('name: "TestApp"');
      expect(tskContent).toContain('version: "1.0.0"');
      expect(tskContent).toContain('[database]');
      expect(tskContent).toContain('host: "localhost"');
      expect(tskContent).toContain('port: 5432');
    });
  });

  describe('Database Commands', () => {
    test('should check database status', () => {
      const output = execSync('node ../../cli/main.js db status', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Database connection');
    });

    test('should run database migration', () => {
      const migrationContent = `
        CREATE TABLE IF NOT EXISTS test_table (
          id SERIAL PRIMARY KEY,
          name VARCHAR(100) NOT NULL,
          created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
      `;

      fs.writeFileSync(path.join(testDir, 'test-migration.sql'), migrationContent);

      const output = execSync('node ../../cli/main.js db migrate test-migration.sql', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Migration completed');
    });

    test('should backup database', () => {
      const output = execSync('node ../../cli/main.js db backup test-backup.sql', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Backup completed');
      expect(fs.existsSync(path.join(testDir, 'test-backup.sql'))).toBe(true);
    });
  });

  describe('Binary Commands', () => {
    test('should compile configuration to binary', () => {
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        
        [database]
        host: "localhost"
        port: 5432
      `;

      fs.writeFileSync(path.join(testDir, 'test-config.tsk'), configContent);

      const output = execSync('node ../../cli/main.js binary compile test-config.tsk', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Binary compilation completed');
      expect(fs.existsSync(path.join(testDir, 'test-config.pnt'))).toBe(true);
    });

    test('should execute binary configuration', () => {
      // First compile to binary
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        port: 8080
      `;

      fs.writeFileSync(path.join(testDir, 'test-config.tsk'), configContent);
      execSync('node ../../cli/main.js binary compile test-config.tsk', { cwd: testDir });

      // Then execute binary
      const output = execSync('node ../../cli/main.js binary execute test-config.pnt', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      const parsed = JSON.parse(output);
      expect(parsed.name).toBe('TestApp');
      expect(parsed.version).toBe('1.0.0');
      expect(parsed.port).toBe(8080);
    });

    test('should benchmark performance', () => {
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        
        [database]
        host: "localhost"
        port: 5432
        
        [server]
        host: "0.0.0.0"
        port: 8080
        
        [cache]
        ttl: 300
        max_size: 1000
      `;

      fs.writeFileSync(path.join(testDir, 'test-config.tsk'), configContent);

      const output = execSync('node ../../cli/main.js binary benchmark test-config.tsk', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Benchmark results');
      expect(output).toContain('Text parsing time');
      expect(output).toContain('Binary parsing time');
    });
  });

  describe('AI Commands', () => {
    test('should query Claude AI', () => {
      // Mock AI response for testing
      const output = execSync('node ../../cli/main.js ai claude "What is TuskLang?"', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('AI response');
    });

    test('should analyze code with AI', () => {
      const codeContent = `
        const config = {
          name: "TestApp",
          port: 8080
        };
      `;

      fs.writeFileSync(path.join(testDir, 'test-code.js'), codeContent);

      const output = execSync('node ../../cli/main.js ai analyze test-code.js --focus security', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Analysis results');
    });
  });

  describe('Cache Commands', () => {
    test('should clear cache', () => {
      const output = execSync('node ../../cli/main.js cache clear', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Cache cleared');
    });

    test('should show cache status', () => {
      const output = execSync('node ../../cli/main.js cache status', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Cache status');
    });

    test('should warm cache', () => {
      const output = execSync('node ../../cli/main.js cache warm', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Cache warmed');
    });
  });

  describe('Service Commands', () => {
    test('should show service status', () => {
      const output = execSync('node ../../cli/main.js services status', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Service status');
    });

    test('should start services', () => {
      const output = execSync('node ../../cli/main.js services start', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Services started');
    });

    test('should stop services', () => {
      const output = execSync('node ../../cli/main.js services stop', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Services stopped');
    });
  });

  describe('Configuration Commands', () => {
    test('should get configuration value', () => {
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        
        [server]
        port: 8080
      `;

      fs.writeFileSync(path.join(testDir, 'test-config.tsk'), configContent);

      const output = execSync('node ../../cli/main.js config get test-config.tsk server.port', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output.trim()).toBe('8080');
    });

    test('should set configuration value', () => {
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        
        [server]
        port: 8080
      `;

      fs.writeFileSync(path.join(testDir, 'test-config.tsk'), configContent);

      execSync('node ../../cli/main.js config set test-config.tsk server.port 9090', { 
        cwd: testDir
      });

      const updatedContent = fs.readFileSync(path.join(testDir, 'test-config.tsk'), 'utf8');
      expect(updatedContent).toContain('port: 9090');
    });

    test('should validate configuration', () => {
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        port: 8080
      `;

      fs.writeFileSync(path.join(testDir, 'test-config.tsk'), configContent);

      const output = execSync('node ../../cli/main.js config validate test-config.tsk', { 
        cwd: testDir,
        encoding: 'utf8'
      });

      expect(output).toContain('Configuration is valid');
    });

    test('should generate configuration documentation', () => {
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        
        [database]
        host: "localhost"
        port: 5432
        
        [server]
        host: "0.0.0.0"
        port: 8080
      `;

      fs.writeFileSync(path.join(testDir, 'test-config.tsk'), configContent);

      execSync('node ../../cli/main.js config docs test-config.tsk', { 
        cwd: testDir
      });

      expect(fs.existsSync(path.join(testDir, 'test-config.md'))).toBe(true);
    });
  });

  describe('Error Handling', () => {
    test('should handle missing files gracefully', () => {
      expect(() => {
        execSync('node ../../cli/main.js parse missing-file.tsk', { 
          cwd: testDir,
          encoding: 'utf8'
        });
      }).toThrow();
    });

    test('should handle invalid command options', () => {
      expect(() => {
        execSync('node ../../cli/main.js parse --invalid-option', { 
          cwd: testDir,
          encoding: 'utf8'
        });
      }).toThrow();
    });

    test('should handle database connection errors', () => {
      // Temporarily set invalid database credentials
      const originalEnv = process.env.TEST_DB_PASSWORD;
      process.env.TEST_DB_PASSWORD = 'wrong_password';

      expect(() => {
        execSync('node ../../cli/main.js db status', { 
          cwd: testDir,
          encoding: 'utf8'
        });
      }).toThrow();

      // Restore original environment
      process.env.TEST_DB_PASSWORD = originalEnv;
    });
  });

  describe('Performance Tests', () => {
    test('should handle large configuration files efficiently', () => {
      // Generate large configuration file
      let largeConfig = '';
      for (let i = 0; i < 1000; i++) {
        largeConfig += `key_${i}: "value_${i}"\n`;
      }

      fs.writeFileSync(path.join(testDir, 'large-config.tsk'), largeConfig);

      const start = process.hrtime.bigint();
      execSync('node ../../cli/main.js parse large-config.tsk', { 
        cwd: testDir,
        encoding: 'utf8'
      });
      const end = process.hrtime.bigint();

      const duration = Number(end - start) / 1000000; // milliseconds
      expect(duration).toBeLessThan(1000); // Should complete in under 1 second
    });

    test('should handle concurrent operations', async () => {
      const configContent = `
        name: "TestApp"
        version: "1.0.0"
        port: 8080
      `;

      fs.writeFileSync(path.join(testDir, 'concurrent-config.tsk'), configContent);

      // Run multiple parse operations concurrently
      const promises = [];
      for (let i = 0; i < 10; i++) {
        promises.push(
          new Promise((resolve, reject) => {
            try {
              const output = execSync('node ../../cli/main.js parse concurrent-config.tsk', { 
                cwd: testDir,
                encoding: 'utf8'
              });
              resolve(output);
            } catch (error) {
              reject(error);
            }
          })
        );
      }

      const results = await Promise.all(promises);
      expect(results).toHaveLength(10);
      
      results.forEach(result => {
        const parsed = JSON.parse(result);
        expect(parsed.name).toBe('TestApp');
        expect(parsed.version).toBe('1.0.0');
        expect(parsed.port).toBe(8080);
      });
    });
  });
});

// Helper function to run CLI commands asynchronously
function runCliCommand(command, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn('node', ['../../cli/main.js', ...command.split(' ')], {
      cwd: options.cwd || testDir,
      stdio: ['pipe', 'pipe', 'pipe']
    });

    let stdout = '';
    let stderr = '';

    child.stdout.on('data', (data) => {
      stdout += data.toString();
    });

    child.stderr.on('data', (data) => {
      stderr += data.toString();
    });

    child.on('close', (code) => {
      if (code === 0) {
        resolve(stdout);
      } else {
        reject(new Error(`Command failed with code ${code}: ${stderr}`));
      }
    });

    child.on('error', (error) => {
      reject(error);
    });
  });
} 