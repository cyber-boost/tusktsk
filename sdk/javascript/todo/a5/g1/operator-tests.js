/**
 * G1: Operator Test Implementations
 * =================================
 * Focused test implementations for core operators
 */

const { expect } = require('chai');
const sinon = require('sinon');

class OperatorTests {
  constructor(tusk) {
    this.tusk = tusk;
  }

  async testQueryOperator() {
    const result = await this.tusk.executeOperator('@query', {
      query: 'SELECT * FROM users WHERE age > 25',
      database: 'test_db'
    });
    expect(result).to.be.an('object');
    expect(result.rows).to.be.an('array');
  }

  async testStringOperator() {
    const result = await this.tusk.executeOperator('@string', {
      operation: 'concat',
      strings: ['Hello', ' ', 'World']
    });
    expect(result).to.equal('Hello World');
  }

  async testJsonOperator() {
    const result = await this.tusk.executeOperator('@json', {
      operation: 'parse',
      input: '{"name": "John", "age": 30}'
    });
    expect(result).to.deep.equal({ name: 'John', age: 30 });
  }

  async testIfOperator() {
    const result = await this.tusk.executeOperator('@if', {
      condition: 'true',
      then: 'Hello',
      else: 'World'
    });
    expect(result).to.equal('Hello');
  }

  async testForOperator() {
    const result = await this.tusk.executeOperator('@for', {
      array: [1, 2, 3],
      template: 'Item: {item}'
    });
    expect(result).to.be.an('array');
    expect(result).to.have.length(3);
  }

  async testEachOperator() {
    const result = await this.tusk.executeOperator('@each', {
      array: [1, 2, 3],
      action: 'multiply',
      factor: 2
    });
    expect(result).to.deep.equal([2, 4, 6]);
  }

  async testFilterOperator() {
    const result = await this.tusk.executeOperator('@filter', {
      array: [1, 2, 3, 4, 5],
      condition: 'item > 3'
    });
    expect(result).to.deep.equal([4, 5]);
  }

  async testCacheOperator() {
    await this.tusk.executeOperator('@cache', {
      operation: 'set',
      key: 'test-key',
      value: 'test-value'
    });
    
    const result = await this.tusk.executeOperator('@cache', {
      operation: 'get',
      key: 'test-key'
    });
    expect(result).to.equal('test-value');
  }

  async testEncryptOperator() {
    const result = await this.tusk.executeOperator('@encrypt', {
      text: 'Hello World',
      key: 'test-key-32-chars-long-secret',
      algorithm: 'aes-256-cbc'
    });
    expect(result).to.be.a('string');
    expect(result).to.not.equal('Hello World');
  }

  async testDecryptOperator() {
    const encrypted = await this.tusk.executeOperator('@encrypt', {
      text: 'Hello World',
      key: 'test-key-32-chars-long-secret',
      algorithm: 'aes-256-cbc'
    });
    
    const decrypted = await this.tusk.executeOperator('@decrypt', {
      text: encrypted,
      key: 'test-key-32-chars-long-secret',
      algorithm: 'aes-256-cbc'
    });
    expect(decrypted).to.equal('Hello World');
  }

  async testHashOperator() {
    const result = await this.tusk.executeOperator('@hash', {
      text: 'Hello World',
      algorithm: 'sha256'
    });
    expect(result).to.be.a('string');
    expect(result).to.have.length(64);
  }

  async testBase64Operator() {
    const encoded = await this.tusk.executeOperator('@base64', {
      operation: 'encode',
      text: 'Hello World'
    });
    
    const decoded = await this.tusk.executeOperator('@base64', {
      operation: 'decode',
      text: encoded
    });
    expect(decoded).to.equal('Hello World');
  }

  async testTemplateOperator() {
    const result = await this.tusk.executeOperator('@template', {
      template: 'Hello {name}!',
      data: { name: 'World' }
    });
    expect(result).to.equal('Hello World!');
  }

  async testVariableOperator() {
    await this.tusk.executeOperator('@variable', {
      operation: 'set',
      name: 'testVar',
      value: 'testValue'
    });
    
    const result = await this.tusk.executeOperator('@variable', {
      operation: 'get',
      name: 'testVar'
    });
    expect(result).to.equal('testValue');
  }

  async testEnvOperator() {
    const result = await this.tusk.executeOperator('@env', {
      operation: 'get',
      name: 'NODE_ENV'
    });
    expect(result).to.be.a('string');
  }

  async testFileOperator() {
    const testContent = 'Hello World Test Content';
    const testFile = '/tmp/tusk-test-file.txt';
    
    await this.tusk.executeOperator('@file', {
      operation: 'write',
      path: testFile,
      content: testContent
    });
    
    const result = await this.tusk.executeOperator('@file', {
      operation: 'read',
      path: testFile
    });
    expect(result).to.equal(testContent);
    
    await this.tusk.executeOperator('@file', {
      operation: 'delete',
      path: testFile
    });
  }

  async runAllTests() {
    const tests = [
      this.testQueryOperator.bind(this),
      this.testStringOperator.bind(this),
      this.testJsonOperator.bind(this),
      this.testIfOperator.bind(this),
      this.testForOperator.bind(this),
      this.testEachOperator.bind(this),
      this.testFilterOperator.bind(this),
      this.testCacheOperator.bind(this),
      this.testEncryptOperator.bind(this),
      this.testDecryptOperator.bind(this),
      this.testHashOperator.bind(this),
      this.testBase64Operator.bind(this),
      this.testTemplateOperator.bind(this),
      this.testVariableOperator.bind(this),
      this.testEnvOperator.bind(this),
      this.testFileOperator.bind(this)
    ];

    const results = [];
    for (const test of tests) {
      try {
        await test();
        results.push({ status: 'PASSED', test: test.name });
      } catch (error) {
        results.push({ status: 'FAILED', test: test.name, error: error.message });
      }
    }
    
    return results;
  }
}

module.exports = { OperatorTests }; 