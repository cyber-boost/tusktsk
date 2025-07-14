# TuskLang JavaScript Documentation: #testing Directive

## Overview

The `#testing` directive in TuskLang defines testing configurations and strategies, enabling declarative test management with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#testing jest
  test_environment: node
  coverage: true
  coverage_threshold: 80
  test_timeout: 5000
  setup_files: ["tests/setup.js"]
  collect_coverage_from:
    - "src/**/*.js"
    - "!src/**/*.test.js"

#testing mocha
  reporter: spec
  timeout: 3000
  require: ["chai/register-expect"]
  files: ["tests/**/*.test.js"]
  recursive: true

#testing cypress
  base_url: http://localhost:3000
  viewport_width: 1280
  viewport_height: 720
  video: true
  screenshots: true
  retries: 2

#testing playwright
  browsers: ["chromium", "firefox", "webkit"]
  headless: true
  timeout: 30000
  screenshot_dir: screenshots
  video_dir: videos
```

## JavaScript Integration

### Jest Testing Handler

```javascript
// jest-testing-handler.js
const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

class JestTestingHandler {
  constructor(config) {
    this.config = config;
    this.testEnvironment = config.test_environment || 'node';
    this.coverage = config.coverage !== false;
    this.coverageThreshold = config.coverage_threshold || 80;
    this.testTimeout = config.test_timeout || 5000;
    this.setupFiles = config.setup_files || [];
    this.collectCoverageFrom = config.collect_coverage_from || ['src/**/*.js'];
  }

  async runTests(testPattern = '**/*.test.js') {
    const jestConfig = this.createJestConfig();
    const configPath = this.writeJestConfig(jestConfig);
    
    try {
      const command = `npx jest --config ${configPath} ${testPattern}`;
      const result = execSync(command, { encoding: 'utf8' });
      
      return {
        success: true,
        output: result,
        coverage: this.parseCoverage()
      };
    } catch (error) {
      return {
        success: false,
        output: error.stdout || error.message,
        error: error.stderr
      };
    }
  }

  createJestConfig() {
    return {
      testEnvironment: this.testEnvironment,
      setupFilesAfterEnv: this.setupFiles,
      testTimeout: this.testTimeout,
      collectCoverage: this.coverage,
      coverageThreshold: {
        global: {
          branches: this.coverageThreshold,
          functions: this.coverageThreshold,
          lines: this.coverageThreshold,
          statements: this.coverageThreshold
        }
      },
      collectCoverageFrom: this.collectCoverageFrom,
      coverageReporters: ['text', 'lcov', 'html'],
      testMatch: ['**/__tests__/**/*.js', '**/?(*.)+(spec|test).js'],
      moduleFileExtensions: ['js', 'json', 'jsx', 'ts', 'tsx'],
      transform: {
        '^.+\\.js$': 'babel-jest'
      }
    };
  }

  writeJestConfig(config) {
    const configPath = path.join(process.cwd(), 'jest.config.json');
    fs.writeFileSync(configPath, JSON.stringify(config, null, 2));
    return configPath;
  }

  parseCoverage() {
    const coveragePath = path.join(process.cwd(), 'coverage', 'lcov-report', 'index.html');
    
    if (fs.existsSync(coveragePath)) {
      const coverageData = fs.readFileSync(coveragePath, 'utf8');
      return this.extractCoverageStats(coverageData);
    }
    
    return null;
  }

  extractCoverageStats(htmlContent) {
    const match = htmlContent.match(/All files\s+(\d+\.\d+)%\s+(\d+\.\d+)%\s+(\d+\.\d+)%\s+(\d+\.\d+)%/);
    
    if (match) {
      return {
        statements: parseFloat(match[1]),
        branches: parseFloat(match[2]),
        functions: parseFloat(match[3]),
        lines: parseFloat(match[4])
      };
    }
    
    return null;
  }

  async watchTests(testPattern = '**/*.test.js') {
    const jestConfig = this.createJestConfig();
    const configPath = this.writeJestConfig(jestConfig);
    
    const command = `npx jest --config ${configPath} --watch ${testPattern}`;
    const child = require('child_process').spawn(command, [], {
      stdio: 'inherit',
      shell: true
    });
    
    return child;
  }
}

module.exports = JestTestingHandler;
```

### Mocha Testing Handler

```javascript
// mocha-testing-handler.js
const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

class MochaTestingHandler {
  constructor(config) {
    this.config = config;
    this.reporter = config.reporter || 'spec';
    this.timeout = config.timeout || 3000;
    this.require = config.require || [];
    this.files = config.files || ['tests/**/*.test.js'];
    this.recursive = config.recursive !== false;
  }

  async runTests(testPattern = 'tests/**/*.test.js') {
    const mochaConfig = this.createMochaConfig();
    const configPath = this.writeMochaConfig(mochaConfig);
    
    try {
      const requireArgs = this.require.map(r => `--require ${r}`).join(' ');
      const command = `npx mocha --config ${configPath} ${requireArgs} ${testPattern}`;
      const result = execSync(command, { encoding: 'utf8' });
      
      return {
        success: true,
        output: result
      };
    } catch (error) {
      return {
        success: false,
        output: error.stdout || error.message,
        error: error.stderr
      };
    }
  }

  createMochaConfig() {
    return {
      spec: this.files,
      recursive: this.recursive,
      timeout: this.timeout,
      reporter: this.reporter,
      require: this.require,
      extension: ['js'],
      ignore: ['node_modules/**/*'],
      ui: 'bdd'
    };
  }

  writeMochaConfig(config) {
    const configPath = path.join(process.cwd(), '.mocharc.json');
    fs.writeFileSync(configPath, JSON.stringify(config, null, 2));
    return configPath;
  }

  async watchTests(testPattern = 'tests/**/*.test.js') {
    const mochaConfig = this.createMochaConfig();
    const configPath = this.writeMochaConfig(mochaConfig);
    
    const requireArgs = this.require.map(r => `--require ${r}`).join(' ');
    const command = `npx mocha --config ${configPath} --watch ${requireArgs} ${testPattern}`;
    const child = require('child_process').spawn(command, [], {
      stdio: 'inherit',
      shell: true
    });
    
    return child;
  }
}

module.exports = MochaTestingHandler;
```

### Cypress Testing Handler

```javascript
// cypress-testing-handler.js
const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

class CypressTestingHandler {
  constructor(config) {
    this.config = config;
    this.baseUrl = config.base_url || 'http://localhost:3000';
    this.viewportWidth = config.viewport_width || 1280;
    this.viewportHeight = config.viewport_height || 720;
    this.video = config.video !== false;
    this.screenshots = config.screenshots !== false;
    this.retries = config.retries || 0;
  }

  async runTests(testPattern = 'cypress/e2e/**/*.cy.js') {
    const cypressConfig = this.createCypressConfig();
    const configPath = this.writeCypressConfig(cypressConfig);
    
    try {
      const command = `npx cypress run --config-file ${configPath} --spec "${testPattern}"`;
      const result = execSync(command, { encoding: 'utf8' });
      
      return {
        success: true,
        output: result,
        screenshots: this.getScreenshots(),
        videos: this.getVideos()
      };
    } catch (error) {
      return {
        success: false,
        output: error.stdout || error.message,
        error: error.stderr,
        screenshots: this.getScreenshots(),
        videos: this.getVideos()
      };
    }
  }

  createCypressConfig() {
    return {
      baseUrl: this.baseUrl,
      viewportWidth: this.viewportWidth,
      viewportHeight: this.viewportHeight,
      video: this.video,
      screenshotOnRunFailure: this.screenshots,
      retries: {
        runMode: this.retries,
        openMode: 0
      },
      e2e: {
        setupNodeEvents(on, config) {
          // Cypress plugins
        }
      }
    };
  }

  writeCypressConfig(config) {
    const configPath = path.join(process.cwd(), 'cypress.config.js');
    const configContent = `module.exports = ${JSON.stringify(config, null, 2)}`;
    fs.writeFileSync(configPath, configContent);
    return configPath;
  }

  getScreenshots() {
    const screenshotsDir = path.join(process.cwd(), 'cypress', 'screenshots');
    
    if (fs.existsSync(screenshotsDir)) {
      return fs.readdirSync(screenshotsDir)
        .filter(file => file.endsWith('.png'))
        .map(file => path.join(screenshotsDir, file));
    }
    
    return [];
  }

  getVideos() {
    const videosDir = path.join(process.cwd(), 'cypress', 'videos');
    
    if (fs.existsSync(videosDir)) {
      return fs.readdirSync(videosDir)
        .filter(file => file.endsWith('.mp4'))
        .map(file => path.join(videosDir, file));
    }
    
    return [];
  }

  async openCypress() {
    const cypressConfig = this.createCypressConfig();
    const configPath = this.writeCypressConfig(cypressConfig);
    
    const command = `npx cypress open --config-file ${configPath}`;
    const child = require('child_process').spawn(command, [], {
      stdio: 'inherit',
      shell: true
    });
    
    return child;
  }
}

module.exports = CypressTestingHandler;
```

### Playwright Testing Handler

```javascript
// playwright-testing-handler.js
const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

class PlaywrightTestingHandler {
  constructor(config) {
    this.config = config;
    this.browsers = config.browsers || ['chromium'];
    this.headless = config.headless !== false;
    this.timeout = config.timeout || 30000;
    this.screenshotDir = config.screenshot_dir || 'screenshots';
    this.videoDir = config.video_dir || 'videos';
  }

  async installBrowsers() {
    try {
      execSync('npx playwright install', { stdio: 'inherit' });
      return { success: true };
    } catch (error) {
      return { success: false, error: error.message };
    }
  }

  async runTests(testPattern = 'tests/**/*.spec.js') {
    const playwrightConfig = this.createPlaywrightConfig();
    const configPath = this.writePlaywrightConfig(playwrightConfig);
    
    try {
      const command = `npx playwright test --config ${configPath} ${testPattern}`;
      const result = execSync(command, { encoding: 'utf8' });
      
      return {
        success: true,
        output: result,
        screenshots: this.getScreenshots(),
        videos: this.getVideos()
      };
    } catch (error) {
      return {
        success: false,
        output: error.stdout || error.message,
        error: error.stderr,
        screenshots: this.getScreenshots(),
        videos: this.getVideos()
      };
    }
  }

  createPlaywrightConfig() {
    return {
      testDir: './tests',
      timeout: this.timeout,
      expect: {
        timeout: 5000
      },
      use: {
        headless: this.headless,
        viewport: { width: 1280, height: 720 },
        screenshot: 'only-on-failure',
        video: 'retain-on-failure'
      },
      projects: this.browsers.map(browser => ({
        name: browser,
        use: { ...require(`@playwright/test`).devices[browser] }
      })),
      reporter: [
        ['html'],
        ['json', { outputFile: 'test-results.json' }]
      ]
    };
  }

  writePlaywrightConfig(config) {
    const configPath = path.join(process.cwd(), 'playwright.config.js');
    const configContent = `module.exports = ${JSON.stringify(config, null, 2)}`;
    fs.writeFileSync(configPath, configContent);
    return configPath;
  }

  getScreenshots() {
    const screenshotsDir = path.join(process.cwd(), this.screenshotDir);
    
    if (fs.existsSync(screenshotsDir)) {
      return this.getAllFiles(screenshotsDir, '.png');
    }
    
    return [];
  }

  getVideos() {
    const videosDir = path.join(process.cwd(), this.videoDir);
    
    if (fs.existsSync(videosDir)) {
      return this.getAllFiles(videosDir, '.webm');
    }
    
    return [];
  }

  getAllFiles(dir, extension) {
    const files = [];
    
    const readDir = (currentDir) => {
      const items = fs.readdirSync(currentDir);
      
      for (const item of items) {
        const fullPath = path.join(currentDir, item);
        const stat = fs.statSync(fullPath);
        
        if (stat.isDirectory()) {
          readDir(fullPath);
        } else if (item.endsWith(extension)) {
          files.push(fullPath);
        }
      }
    };
    
    readDir(dir);
    return files;
  }

  async showReport() {
    const command = 'npx playwright show-report';
    const child = require('child_process').spawn(command, [], {
      stdio: 'inherit',
      shell: true
    });
    
    return child;
  }
}

module.exports = PlaywrightTestingHandler;
```

## TypeScript Implementation

```typescript
// testing-handler.types.ts
export interface TestingConfig {
  test_environment?: string;
  coverage?: boolean;
  coverage_threshold?: number;
  test_timeout?: number;
  setup_files?: string[];
  collect_coverage_from?: string[];
  reporter?: string;
  require?: string[];
  files?: string[];
  recursive?: boolean;
  base_url?: string;
  viewport_width?: number;
  viewport_height?: number;
  video?: boolean;
  screenshots?: boolean;
  retries?: number;
  browsers?: string[];
  headless?: boolean;
  timeout?: number;
  screenshot_dir?: string;
  video_dir?: string;
}

export interface TestResult {
  success: boolean;
  output: string;
  error?: string;
  coverage?: any;
  screenshots?: string[];
  videos?: string[];
}

export interface TestingHandler {
  runTests(testPattern?: string): Promise<TestResult>;
  watchTests?(testPattern?: string): Promise<any>;
}

// testing-handler.ts
import { TestingConfig, TestingHandler, TestResult } from './testing-handler.types';

export class TypeScriptTestingHandler implements TestingHandler {
  protected config: TestingConfig;

  constructor(config: TestingConfig) {
    this.config = config;
  }

  async runTests(testPattern: string = '**/*.test.js'): Promise<TestResult> {
    throw new Error('Method not implemented');
  }
}

export class TypeScriptJestHandler extends TypeScriptTestingHandler {
  private testEnvironment: string;
  private coverage: boolean;
  private coverageThreshold: number;
  private testTimeout: number;

  constructor(config: TestingConfig) {
    super(config);
    this.testEnvironment = config.test_environment || 'node';
    this.coverage = config.coverage !== false;
    this.coverageThreshold = config.coverage_threshold || 80;
    this.testTimeout = config.test_timeout || 5000;
  }

  async runTests(testPattern: string = '**/*.test.js'): Promise<TestResult> {
    const { execSync } = require('child_process');
    
    try {
      const command = `npx jest ${testPattern}`;
      const result = execSync(command, { encoding: 'utf8' });
      
      return {
        success: true,
        output: result
      };
    } catch (error: any) {
      return {
        success: false,
        output: error.stdout || error.message,
        error: error.stderr
      };
    }
  }

  async watchTests(testPattern: string = '**/*.test.js'): Promise<any> {
    const { spawn } = require('child_process');
    const command = `npx jest --watch ${testPattern}`;
    
    return spawn(command, [], {
      stdio: 'inherit',
      shell: true
    });
  }
}
```

## Advanced Usage Scenarios

### Multi-Framework Testing

```javascript
// multi-framework-testing.js
class MultiFrameworkTesting {
  constructor(configs) {
    this.handlers = new Map();
    this.initializeHandlers(configs);
  }

  initializeHandlers(configs) {
    if (configs.jest) {
      const JestHandler = require('./jest-testing-handler');
      this.handlers.set('jest', new JestHandler(configs.jest));
    }

    if (configs.mocha) {
      const MochaHandler = require('./mocha-testing-handler');
      this.handlers.set('mocha', new MochaHandler(configs.mocha));
    }

    if (configs.cypress) {
      const CypressHandler = require('./cypress-testing-handler');
      this.handlers.set('cypress', new CypressHandler(configs.cypress));
    }

    if (configs.playwright) {
      const PlaywrightHandler = require('./playwright-testing-handler');
      this.handlers.set('playwright', new PlaywrightHandler(configs.playwright));
    }
  }

  async runAllTests() {
    const results = {};
    
    for (const [framework, handler] of this.handlers.entries()) {
      results[framework] = await handler.runTests();
    }
    
    return results;
  }

  async runFrameworkTests(framework, testPattern) {
    const handler = this.handlers.get(framework);
    if (!handler) {
      throw new Error(`Framework '${framework}' not found`);
    }
    
    return await handler.runTests(testPattern);
  }

  getHandler(framework) {
    return this.handlers.get(framework);
  }
}
```

### Test Data Management

```javascript
// test-data-manager.js
class TestDataManager {
  constructor() {
    this.fixtures = new Map();
    this.factories = new Map();
  }

  addFixture(name, data) {
    this.fixtures.set(name, data);
  }

  getFixture(name) {
    return this.fixtures.get(name);
  }

  addFactory(name, factory) {
    this.factories.set(name, factory);
  }

  createFromFactory(name, overrides = {}) {
    const factory = this.factories.get(name);
    if (!factory) {
      throw new Error(`Factory '${name}' not found`);
    }
    
    return { ...factory(), ...overrides };
  }

  createUser(overrides = {}) {
    return this.createFromFactory('user', overrides);
  }

  createProduct(overrides = {}) {
    return this.createFromFactory('product', overrides);
  }

  setupDefaultFactories() {
    this.addFactory('user', () => ({
      id: Math.random().toString(36).substr(2, 9),
      email: `user${Date.now()}@example.com`,
      name: 'Test User',
      createdAt: new Date()
    }));

    this.addFactory('product', () => ({
      id: Math.random().toString(36).substr(2, 9),
      name: 'Test Product',
      price: 99.99,
      category: 'electronics',
      createdAt: new Date()
    }));
  }
}
```

## Real-World Examples

### API Testing Setup

```javascript
// api-testing-setup.js
const supertest = require('supertest');
const { expect } = require('chai');

class APITestingSetup {
  constructor(app) {
    this.app = app;
    this.request = supertest(app);
  }

  async testEndpoint(method, path, data = null, expectedStatus = 200) {
    let req = this.request[method.toLowerCase()](path);
    
    if (data) {
      req = req.send(data);
    }
    
    const response = await req.expect(expectedStatus);
    return response;
  }

  async testGET(path, expectedStatus = 200) {
    return await this.testEndpoint('GET', path, null, expectedStatus);
  }

  async testPOST(path, data, expectedStatus = 201) {
    return await this.testEndpoint('POST', path, data, expectedStatus);
  }

  async testPUT(path, data, expectedStatus = 200) {
    return await this.testEndpoint('PUT', path, data, expectedStatus);
  }

  async testDELETE(path, expectedStatus = 204) {
    return await this.testEndpoint('DELETE', path, null, expectedStatus);
  }

  validateResponse(response, schema) {
    expect(response.body).to.have.property('success');
    expect(response.body.success).to.be.true;
    
    if (schema) {
      expect(response.body.data).to.matchSchema(schema);
    }
  }
}

// Usage
const app = require('./app');
const apiTesting = new APITestingSetup(app);

describe('API Tests', () => {
  it('should get users', async () => {
    const response = await apiTesting.testGET('/api/users');
    apiTesting.validateResponse(response);
  });

  it('should create user', async () => {
    const userData = { name: 'Test User', email: 'test@example.com' };
    const response = await apiTesting.testPOST('/api/users', userData);
    apiTesting.validateResponse(response);
  });
});
```

### Database Testing

```javascript
// database-testing.js
const { Pool } = require('pg');

class DatabaseTesting {
  constructor(config) {
    this.config = config;
    this.pool = null;
  }

  async connect() {
    this.pool = new Pool({
      ...this.config,
      database: this.config.test_database || this.config.database
    });
  }

  async disconnect() {
    if (this.pool) {
      await this.pool.end();
    }
  }

  async setupTestData() {
    await this.pool.query(`
      CREATE TABLE IF NOT EXISTS test_users (
        id SERIAL PRIMARY KEY,
        name VARCHAR(100),
        email VARCHAR(255) UNIQUE,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `);
  }

  async cleanupTestData() {
    await this.pool.query('TRUNCATE TABLE test_users RESTART IDENTITY CASCADE');
  }

  async insertTestUser(userData) {
    const { name, email } = userData;
    const result = await this.pool.query(
      'INSERT INTO test_users (name, email) VALUES ($1, $2) RETURNING *',
      [name, email]
    );
    return result.rows[0];
  }

  async getTestUser(id) {
    const result = await this.pool.query('SELECT * FROM test_users WHERE id = $1', [id]);
    return result.rows[0];
  }

  async getAllTestUsers() {
    const result = await this.pool.query('SELECT * FROM test_users ORDER BY id');
    return result.rows;
  }
}
```

## Performance Considerations

### Test Parallelization

```javascript
// test-parallelizer.js
const { Worker } = require('worker_threads');
const path = require('path');

class TestParallelizer {
  constructor(config) {
    this.config = config;
    this.maxWorkers = config.max_workers || 4;
    this.testFiles = [];
  }

  addTestFile(filePath) {
    this.testFiles.push(filePath);
  }

  async runTestsInParallel() {
    const chunks = this.chunkArray(this.testFiles, this.maxWorkers);
    const workers = [];
    
    for (const chunk of chunks) {
      const worker = new Worker(path.join(__dirname, 'test-worker.js'), {
        workerData: { testFiles: chunk }
      });
      
      workers.push(worker);
    }
    
    const results = await Promise.all(
      workers.map(worker => this.runWorker(worker))
    );
    
    return this.mergeResults(results);
  }

  chunkArray(array, size) {
    const chunks = [];
    for (let i = 0; i < array.length; i += size) {
      chunks.push(array.slice(i, i + size));
    }
    return chunks;
  }

  runWorker(worker) {
    return new Promise((resolve, reject) => {
      worker.on('message', resolve);
      worker.on('error', reject);
      worker.on('exit', (code) => {
        if (code !== 0) {
          reject(new Error(`Worker stopped with exit code ${code}`));
        }
      });
    });
  }

  mergeResults(results) {
    return {
      success: results.every(r => r.success),
      totalTests: results.reduce((sum, r) => sum + r.totalTests, 0),
      passedTests: results.reduce((sum, r) => sum + r.passedTests, 0),
      failedTests: results.reduce((sum, r) => sum + r.failedTests, 0),
      output: results.map(r => r.output).join('\n')
    };
  }
}
```

### Test Caching

```javascript
// test-cache.js
const crypto = require('crypto');
const fs = require('fs');

class TestCache {
  constructor() {
    this.cacheDir = '.test-cache';
    this.ensureCacheDir();
  }

  ensureCacheDir() {
    if (!fs.existsSync(this.cacheDir)) {
      fs.mkdirSync(this.cacheDir, { recursive: true });
    }
  }

  getCacheKey(testFile, dependencies) {
    const content = fs.readFileSync(testFile, 'utf8');
    const depsHash = crypto.createHash('md5').update(JSON.stringify(dependencies)).digest('hex');
    const fileHash = crypto.createHash('md5').update(content).digest('hex');
    
    return `${fileHash}-${depsHash}`;
  }

  getCachedResult(cacheKey) {
    const cacheFile = path.join(this.cacheDir, `${cacheKey}.json`);
    
    if (fs.existsSync(cacheFile)) {
      const cached = JSON.parse(fs.readFileSync(cacheFile, 'utf8'));
      
      // Check if cache is still valid
      if (Date.now() - cached.timestamp < 24 * 60 * 60 * 1000) { // 24 hours
        return cached.result;
      }
    }
    
    return null;
  }

  setCachedResult(cacheKey, result) {
    const cacheFile = path.join(this.cacheDir, `${cacheKey}.json`);
    const cacheData = {
      result,
      timestamp: Date.now()
    };
    
    fs.writeFileSync(cacheFile, JSON.stringify(cacheData));
  }

  clearCache() {
    if (fs.existsSync(this.cacheDir)) {
      fs.rmSync(this.cacheDir, { recursive: true, force: true });
    }
  }
}
```

## Security Notes

### Test Data Sanitization

```javascript
// test-data-sanitizer.js
class TestDataSanitizer {
  constructor() {
    this.sensitiveFields = ['password', 'token', 'secret', 'key'];
  }

  sanitizeData(data) {
    if (typeof data === 'object' && data !== null) {
      const sanitized = { ...data };
      
      for (const field of this.sensitiveFields) {
        if (sanitized[field]) {
          sanitized[field] = '[REDACTED]';
        }
      }
      
      return sanitized;
    }
    
    return data;
  }

  sanitizeLogs(logs) {
    return logs.map(log => ({
      ...log,
      data: this.sanitizeData(log.data)
    }));
  }
}
```

## Best Practices

### Testing Configuration Management

```javascript
// testing-config-manager.js
class TestingConfigManager {
  constructor() {
    this.configs = new Map();
  }

  setConfig(environment, config) {
    this.configs.set(environment, this.validateConfig(config));
  }

  getConfig(environment) {
    const config = this.configs.get(environment);
    if (!config) {
      throw new Error(`No configuration found for environment: ${environment}`);
    }
    return config;
  }

  validateConfig(config) {
    if (!config.framework) {
      throw new Error('Testing framework is required');
    }
    
    return config;
  }

  getCurrentConfig() {
    const environment = process.env.NODE_ENV || 'test';
    return this.getConfig(environment);
  }
}
```

### Testing Health Monitoring

```javascript
// testing-health-monitor.js
class TestingHealthMonitor {
  constructor(testingHandler) {
    this.testing = testingHandler;
    this.metrics = {
      totalTests: 0,
      passedTests: 0,
      failedTests: 0,
      testDuration: 0
    };
  }

  async checkHealth() {
    try {
      const start = Date.now();
      const result = await this.testing.runTests('**/*.health.test.js');
      const duration = Date.now() - start;
      
      this.metrics.testDuration = duration;
      
      return {
        status: result.success ? 'healthy' : 'unhealthy',
        metrics: this.metrics,
        result
      };
    } catch (error) {
      return {
        status: 'unhealthy',
        error: error.message,
        metrics: this.metrics
      };
    }
  }

  getMetrics() {
    return this.metrics;
  }
}
```

## Related Topics

- [@test Operator](./54-tsklang-javascript-operator-test.md)
- [@assert Operator](./55-tsklang-javascript-operator-assert.md)
- [@mock Operator](./56-tsklang-javascript-operator-mock.md)
- [@stub Operator](./57-tsklang-javascript-operator-stub.md)
- [@spy Operator](./58-tsklang-javascript-operator-spy.md) 