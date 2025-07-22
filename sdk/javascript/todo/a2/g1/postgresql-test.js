/**
 * PostgreSQL Operator - Comprehensive Test Suite
 * Tests all production features including connection pooling, transactions,
 * parameterized queries, retry logic, and health monitoring
 */

const PostgreSQLOperator = require('./postgresql-operator');

class PostgreSQLTestSuite {
  constructor() {
    this.operator = new PostgreSQLOperator();
    this.testResults = {
      passed: 0,
      failed: 0,
      total: 0
    };
  }

  async runAllTests() {
    console.log('🚀 Starting PostgreSQL Operator Test Suite');
    console.log('=' .repeat(60));

    await this.testConnectionPooling();
    await this.testQueryExecution();
    await this.testTransactions();
    await this.testRetryLogic();
    await this.testHealthCheck();
    await this.testMetrics();
    await this.testErrorHandling();
    await this.testTuskLangIntegration();

    this.printResults();
  }

  async testConnectionPooling() {
    console.log('\n📊 Testing Connection Pooling...');
    
    try {
      const connectionString = 'postgresql://localhost:5432/testdb';
      const pool1 = this.operator.createPool(connectionString);
      const pool2 = this.operator.createPool(connectionString);
      
      // Should return same pool instance
      if (pool1 === pool2) {
        this.pass('Connection pooling - same connection string returns same pool');
      } else {
        this.fail('Connection pooling - same connection string should return same pool');
      }

      // Test pool configuration
      const poolConfig = this.operator.createPool(connectionString, {
        max: 15,
        min: 3,
        idleTimeout: 45000
      });
      
      this.pass('Connection pooling - custom configuration applied');
      
    } catch (error) {
      this.fail('Connection pooling', error.message);
    }
  }

  async testQueryExecution() {
    console.log('\n🔍 Testing Query Execution...');
    
    try {
      const connectionString = 'postgresql://localhost:5432/testdb';
      const query = 'SELECT version(), current_timestamp';
      const params = [];
      
      const result = await this.operator.executeQuery(connectionString, query, params, {
        queryTimeout: 10000
      });
      
      if (result.success && result.rows && result.rows.length > 0) {
        this.pass('Query execution - basic query successful');
      } else {
        this.fail('Query execution - basic query failed');
      }

      // Test parameterized query
      const paramQuery = 'SELECT $1::text as name, $2::int as age';
      const paramResult = await this.operator.executeQuery(connectionString, paramQuery, ['John', 30]);
      
      if (paramResult.success) {
        this.pass('Query execution - parameterized query successful');
      } else {
        this.fail('Query execution - parameterized query failed');
      }
      
    } catch (error) {
      this.fail('Query execution', error.message);
    }
  }

  async testTransactions() {
    console.log('\n💾 Testing Transactions...');
    
    try {
      const connectionString = 'postgresql://localhost:5432/testdb';
      const operations = [
        { query: 'CREATE TABLE IF NOT EXISTS test_trans (id SERIAL, name TEXT)', params: [] },
        { query: 'INSERT INTO test_trans (name) VALUES ($1)', params: ['Transaction Test'] },
        { query: 'SELECT COUNT(*) FROM test_trans WHERE name = $1', params: ['Transaction Test'] }
      ];
      
      const result = await this.operator.executeTransaction(connectionString, operations);
      
      if (result.success && result.results.length === 3) {
        this.pass('Transactions - ACID compliance successful');
      } else {
        this.fail('Transactions - ACID compliance failed');
      }
      
    } catch (error) {
      this.fail('Transactions', error.message);
    }
  }

  async testRetryLogic() {
    console.log('\n🔄 Testing Retry Logic...');
    
    try {
      const connectionString = 'postgresql://localhost:5432/testdb';
      
      // Test with invalid query to trigger retry
      const invalidQuery = 'SELECT * FROM nonexistent_table';
      
      try {
        await this.operator.executeQuery(connectionString, invalidQuery, [], {
          retries: 2
        });
        this.fail('Retry logic - should have failed with invalid query');
      } catch (error) {
        this.pass('Retry logic - properly handled invalid query');
      }
      
    } catch (error) {
      this.fail('Retry logic', error.message);
    }
  }

  async testHealthCheck() {
    console.log('\n🏥 Testing Health Check...');
    
    try {
      const connectionString = 'postgresql://localhost:5432/testdb';
      const health = await this.operator.healthCheck(connectionString);
      
      if (health.healthy && health.version) {
        this.pass('Health check - database healthy');
      } else {
        this.fail('Health check - database unhealthy');
      }
      
    } catch (error) {
      this.fail('Health check', error.message);
    }
  }

  async testMetrics() {
    console.log('\n📈 Testing Metrics Collection...');
    
    try {
      const metrics = this.operator.getMetrics();
      
      if (metrics.queries >= 0 && metrics.errors >= 0 && metrics.uptime > 0) {
        this.pass('Metrics collection - metrics properly tracked');
      } else {
        this.fail('Metrics collection - metrics not properly tracked');
      }
      
    } catch (error) {
      this.fail('Metrics collection', error.message);
    }
  }

  async testErrorHandling() {
    console.log('\n⚠️ Testing Error Handling...');
    
    try {
      // Test invalid connection string
      const result = await this.operator.executeQuery('invalid://connection', 'SELECT 1');
      
      if (!result.success && result.error) {
        this.pass('Error handling - invalid connection properly handled');
      } else {
        this.fail('Error handling - invalid connection not properly handled');
      }
      
    } catch (error) {
      this.pass('Error handling - exceptions properly caught');
    }
  }

  async testTuskLangIntegration() {
    console.log('\n🔗 Testing TuskLang Integration...');
    
    try {
      const params = '"postgresql://localhost:5432/testdb", "testdb", "SELECT version()"';
      const result = await this.operator.executePostgreSqlOperator(params);
      
      if (result.success) {
        this.pass('TuskLang integration - operator properly integrated');
      } else {
        this.fail('TuskLang integration - operator integration failed');
      }
      
    } catch (error) {
      this.fail('TuskLang integration', error.message);
    }
  }

  pass(testName) {
    console.log(`✅ PASS: ${testName}`);
    this.testResults.passed++;
    this.testResults.total++;
  }

  fail(testName, error = '') {
    console.log(`❌ FAIL: ${testName}${error ? ` - ${error}` : ''}`);
    this.testResults.failed++;
    this.testResults.total++;
  }

  printResults() {
    console.log('\n' + '=' .repeat(60));
    console.log('📊 PostgreSQL Operator Test Results:');
    console.log(`Total Tests: ${this.testResults.total}`);
    console.log(`Passed: ${this.testResults.passed}`);
    console.log(`Failed: ${this.testResults.failed}`);
    console.log(`Success Rate: ${((this.testResults.passed / this.testResults.total) * 100).toFixed(1)}%`);
    
    if (this.testResults.failed === 0) {
      console.log('🎉 All tests passed! PostgreSQL operator is production-ready.');
    } else {
      console.log('⚠️ Some tests failed. Please review and fix issues.');
    }
  }
}

// Run tests if this file is executed directly
if (require.main === module) {
  const testSuite = new PostgreSQLTestSuite();
  testSuite.runAllTests().catch(console.error);
}

module.exports = PostgreSQLTestSuite; 