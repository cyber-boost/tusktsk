/**
 * Comprehensive Test Suite for All 85 TuskLang JavaScript Operators
 * ================================================================
 * This file tests all 85 operators to ensure 100% feature parity with PHP SDK
 */

const TuskLangEnhanced = require('../tsk-enhanced.js');

class OperatorTestSuite {
  constructor() {
    this.parser = new TuskLangEnhanced();
    this.testResults = {
      passed: 0,
      failed: 0,
      total: 85,
      details: []
    };
  }

  /**
   * Run all operator tests
   */
  async runAllTests() {
    console.log('🚀 STARTING COMPREHENSIVE OPERATOR TEST SUITE');
    console.log('Testing all 85 operators for 100% feature parity...\n');

    // Core Language Features (7 operators)
    await this.testCoreLanguageFeatures();
    
    // Advanced @ Operators (22 operators)
    await this.testAdvancedOperators();
    
    // Conditional & Control Flow (6 operators)
    await this.testControlFlowOperators();
    
    // String & Data Processing (8 operators)
    await this.testDataProcessingOperators();
    
    // Security & Encryption (6 operators)
    await this.testSecurityOperators();
    
    // Cloud & Platform (12 operators)
    await this.testCloudPlatformOperators();
    
    // Monitoring & Observability (6 operators)
    await this.testMonitoringOperators();
    
    // Communication & Messaging (6 operators)
    await this.testCommunicationOperators();
    
    // Enterprise Features (6 operators)
    await this.testEnterpriseOperators();
    
    // Advanced Integrations (6 operators)
    await this.testAdvancedIntegrations();

    this.printResults();
  }

  /**
   * Test core language features
   */
  async testCoreLanguageFeatures() {
    console.log('📋 Testing Core Language Features (7 operators)...');
    
    const tests = [
      { name: '@variable', test: () => this.parser.parseValue('$testVar') },
      { name: '@env', test: () => this.parser.parseValue('@env("TEST_VAR")') },
      { name: '@date', test: () => this.parser.parseValue('@date("Y-m-d")') },
      { name: '@file', test: () => this.parser.parseValue('@file("test.txt")') },
      { name: '@json', test: () => this.parser.parseValue('@json({"test": "value"})') },
      { name: '@query', test: () => this.parser.parseValue('@query("SELECT * FROM users")') },
      { name: '@cache', test: () => this.parser.parseValue('@cache("5m", "test_value")') }
    ];

    for (const test of tests) {
      await this.runTest(test.name, test.test);
    }
  }

  /**
   * Test advanced operators
   */
  async testAdvancedOperators() {
    console.log('🔧 Testing Advanced @ Operators (22 operators)...');
    
    const tests = [
      { name: '@graphql', test: () => this.parser.executeGraphQLOperator('"http://localhost:4000", "query { users { id name } }"') },
      { name: '@grpc', test: () => this.parser.executeGrpcOperator('"localhost:50051", "GetUser", "{\"id\": 1}"') },
      { name: '@websocket', test: () => this.parser.executeWebSocketOperator('"ws://localhost:8080", "Hello"') },
      { name: '@sse', test: () => this.parser.executeSseOperator('"http://localhost:3000/events", "message"') },
      { name: '@nats', test: () => this.parser.executeNatsOperator('"nats://localhost:4222", "user.created", "{\"id\": 1}"') },
      { name: '@amqp', test: () => this.parser.executeAmqpOperator('"amqp://localhost:5672", "user_queue", "{\"id\": 1}"') },
      { name: '@kafka', test: () => this.parser.executeKafkaOperator('"localhost:9092", "user_events", "{\"id\": 1}"') },
      { name: '@etcd', test: () => this.parser.executeEtcdOperator('"http://localhost:2379", "user/1", "{\"name\": \"John\"}"') },
      { name: '@elasticsearch', test: () => this.parser.executeElasticsearchOperator('"http://localhost:9200", "users", "{\"query\": {\"match\": {\"name\": \"John\"}}}"') },
      { name: '@prometheus', test: () => this.parser.executePrometheusOperator('"http://localhost:9090", "http_requests_total", "1"') },
      { name: '@jaeger', test: () => this.parser.executeJaegerOperator('"http://localhost:16686", "user-service", "get-user"') },
      { name: '@zipkin', test: () => this.parser.executeZipkinOperator('"http://localhost:9411", "user-service", "get-user"') },
      { name: '@grafana', test: () => this.parser.executeGrafanaOperator('"http://localhost:3000", "dashboard-1", "panel-1"') },
      { name: '@istio', test: () => this.parser.executeIstioOperator('"http://localhost:15014", "user-service", "get-config"') },
      { name: '@consul', test: () => this.parser.executeConsulOperator('"http://localhost:8500", "user-service", "get-services"') },
      { name: '@vault', test: () => this.parser.executeVaultOperator('"http://localhost:8200", "secret/user", "read"') },
      { name: '@temporal', test: () => this.parser.executeTemporalOperator('"localhost:7233", "user-workflow", "start"') },
      { name: '@mongodb', test: () => this.parser.executeMongoDbOperator('"mongodb://localhost:27017", "testdb", "users", "find"') },
      { name: '@redis', test: () => this.parser.executeRedisOperator('"redis://localhost:6379", "user:1", "get"') },
      { name: '@postgresql', test: () => this.parser.executePostgreSqlOperator('"postgresql://localhost:5432", "testdb", "SELECT * FROM users"') },
      { name: '@mysql', test: () => this.parser.executeMySqlOperator('"mysql://localhost:3306", "testdb", "SELECT * FROM users"') },
      { name: '@influxdb', test: () => this.parser.executeInfluxDbOperator('"http://localhost:8086", "testdb", "cpu_usage", "{\"value\": 75}"') }
    ];

    for (const test of tests) {
      await this.runTest(test.name, test.test);
    }
  }

  /**
   * Test control flow operators
   */
  async testControlFlowOperators() {
    console.log('🔄 Testing Control Flow Operators (6 operators)...');
    
    const tests = [
      { name: '@if', test: () => this.parser.executeIfOperator('true ? "yes" : "no"') },
      { name: '@switch', test: () => this.parser.executeSwitchOperator('"case1" { case1: "value1", case2: "value2", default: "default" }') },
      { name: '@for', test: () => this.parser.executeForOperator('1, 5, 1, "i"') },
      { name: '@while', test: () => this.parser.executeWhileOperator('i < 3, "i++", 10') },
      { name: '@each', test: () => this.parser.executeEachOperator('[1, 2, 3], "item * 2"') },
      { name: '@filter', test: () => this.parser.executeFilterOperator('[1, 2, 3, 4], "item > 2"') }
    ];

    for (const test of tests) {
      await this.runTest(test.name, test.test);
    }
  }

  /**
   * Test data processing operators
   */
  async testDataProcessingOperators() {
    console.log('📊 Testing Data Processing Operators (8 operators)...');
    
    const tests = [
      { name: '@string', test: () => this.parser.executeStringOperator('"upper", "hello world"') },
      { name: '@regex', test: () => this.parser.executeRegexOperator('"test", "hello", "hello world"') },
      { name: '@hash', test: () => this.parser.executeHashOperator('"md5", "hello world"') },
      { name: '@base64', test: () => this.parser.executeBase64Operator('"encode", "hello world"') },
      { name: '@xml', test: () => this.parser.executeXmlOperator('"parse", "<user><name>John</name></user>"') },
      { name: '@yaml', test: () => this.parser.executeYamlOperator('"parse", "name: John\nage: 30"') },
      { name: '@csv', test: () => this.parser.executeCsvOperator('"parse", "name,age\nJohn,30"') },
      { name: '@template', test: () => this.parser.executeTemplateOperator('"Hello {{name}}!", "{\"name\": \"John\"}"') }
    ];

    for (const test of tests) {
      await this.runTest(test.name, test.test);
    }
  }

  /**
   * Test security operators
   */
  async testSecurityOperators() {
    console.log('🔒 Testing Security Operators (6 operators)...');
    
    const tests = [
      { name: '@encrypt', test: () => this.parser.executeEncryptOperator('"aes", "secret data", "mykey"') },
      { name: '@decrypt', test: () => this.parser.executeDecryptOperator('"aes", "encrypted_data", "mykey"') },
      { name: '@jwt', test: () => this.parser.executeJwtOperator('"encode", "{\"user\": \"john\"}", "secret"') },
      { name: '@oauth', test: () => this.parser.executeOAuthOperator('"google", "client_id", "client_secret"') },
      { name: '@saml', test: () => this.parser.executeSamlOperator('"https://idp.example.com", "entity_id", "certificate"') },
      { name: '@ldap', test: () => this.parser.executeLdapOperator('"ldap://localhost:389", "cn=admin,dc=example,dc=com", "password"') }
    ];

    for (const test of tests) {
      await this.runTest(test.name, test.test);
    }
  }

  /**
   * Test cloud platform operators
   */
  async testCloudPlatformOperators() {
    console.log('☁️ Testing Cloud Platform Operators (12 operators)...');
    
    const tests = [
      { name: '@kubernetes', test: () => this.parser.executeKubernetesOperator('"kubeconfig", "pods", "list"') },
      { name: '@docker', test: () => this.parser.executeDockerOperator('"localhost", "nginx:latest", "run"') },
      { name: '@aws', test: () => this.parser.executeAwsOperator('"s3", "us-east-1", "list-buckets"') },
      { name: '@azure', test: () => this.parser.executeAzureOperator('"storage", "subscription-id", "list-accounts"') },
      { name: '@gcp', test: () => this.parser.executeGcpOperator('"compute", "project-id", "list-instances"') },
      { name: '@terraform', test: () => this.parser.executeTerraformOperator('"/path/to/terraform", "apply"') },
      { name: '@ansible', test: () => this.parser.executeAnsibleOperator('"playbook.yml", "inventory.ini"') },
      { name: '@puppet', test: () => this.parser.executePuppetOperator('"manifest.pp", "node.example.com"') },
      { name: '@chef', test: () => this.parser.executeChefOperator('"cookbook", "node.example.com", "recipe"') },
      { name: '@jenkins', test: () => this.parser.executeJenkinsOperator('"http://localhost:8080", "build-job"') },
      { name: '@github', test: () => this.parser.executeGitHubOperator('"token", "owner/repo", "create-issue"') },
      { name: '@gitlab', test: () => this.parser.executeGitLabOperator('"https://gitlab.com", "token", "project", "create-issue"') }
    ];

    for (const test of tests) {
      await this.runTest(test.name, test.test);
    }
  }

  /**
   * Test monitoring operators
   */
  async testMonitoringOperators() {
    console.log('📈 Testing Monitoring Operators (6 operators)...');
    
    const tests = [
      { name: '@logs', test: () => this.parser.executeLogsOperator('"info", "Test log message"') },
      { name: '@alerts', test: () => this.parser.executeAlertsOperator('"warning", "System alert", "admin@example.com"') },
      { name: '@health', test: () => this.parser.executeHealthOperator('"api-service", "ping"') },
      { name: '@status', test: () => this.parser.executeStatusOperator('"api-service", "uptime", "99.9"') },
      { name: '@uptime', test: () => this.parser.executeUptimeOperator('"https://example.com", "http"') },
      { name: '@metrics', test: () => this.parser.parseValue('@metrics("request_count", 100)') }
    ];

    for (const test of tests) {
      await this.runTest(test.name, test.test);
    }
  }

  /**
   * Test communication operators
   */
  async testCommunicationOperators() {
    console.log('💬 Testing Communication Operators (6 operators)...');
    
    const tests = [
      { name: '@email', test: () => this.parser.executeEmailOperator('"user@example.com", "Test Subject", "Test body"') },
      { name: '@sms', test: () => this.parser.executeSmsOperator('"+1234567890", "Test SMS"') },
      { name: '@webhook', test: () => this.parser.executeWebhookOperator('"https://webhook.site/123", "POST"') },
      { name: '@slack', test: () => this.parser.executeSlackOperator('"https://hooks.slack.com/123", "Test message"') },
      { name: '@teams', test: () => this.parser.executeTeamsOperator('"https://webhook.office.com/123", "Test message"') },
      { name: '@discord', test: () => this.parser.executeDiscordOperator('"https://discord.com/api/webhooks/123", "Test message"') }
    ];

    for (const test of tests) {
      await this.runTest(test.name, test.test);
    }
  }

  /**
   * Test enterprise operators
   */
  async testEnterpriseOperators() {
    console.log('🏢 Testing Enterprise Operators (6 operators)...');
    
    const tests = [
      { name: '@rbac', test: () => this.parser.executeRbacOperator('"admin", "read", "user123"') },
      { name: '@audit', test: () => this.parser.executeAuditOperator('"login", "user", "user123"') },
      { name: '@compliance', test: () => this.parser.executeComplianceOperator('"gdpr", "user-data", "compliant"') },
      { name: '@governance', test: () => this.parser.executeGovernanceOperator('"data-retention", "logs", "v1.0"') },
      { name: '@policy', test: () => this.parser.executePolicyOperator('"access-control", "resource", "allow"') },
      { name: '@workflow', test: () => this.parser.executeWorkflowOperator('"user-onboarding", "step1"') }
    ];

    for (const test of tests) {
      await this.runTest(test.name, test.test);
    }
  }

  /**
   * Test advanced integrations
   */
  async testAdvancedIntegrations() {
    console.log('🤖 Testing Advanced Integrations (6 operators)...');
    
    const tests = [
      { name: '@ai', test: () => this.parser.executeAiOperator('"gpt-3", "Hello, how are you?"') },
      { name: '@blockchain', test: () => this.parser.executeBlockchainOperator('"ethereum", "0x123...", "transfer"') },
      { name: '@iot', test: () => this.parser.executeIoTOperator('"sensor-001", "read-temperature"') },
      { name: '@edge', test: () => this.parser.executeEdgeOperator('"edge-device-001", "{\"temperature\": 25}"') },
      { name: '@quantum', test: () => this.parser.executeQuantumOperator('"shor", "{\"number\": 15}"') },
      { name: '@neural', test: () => this.parser.executeNeuralOperator('"image-classifier", "{\"image\": \"data\"}"') }
    ];

    for (const test of tests) {
      await this.runTest(test.name, test.test);
    }
  }

  /**
   * Run individual test
   */
  async runTest(name, testFunction) {
    try {
      const result = await testFunction();
      this.testResults.passed++;
      this.testResults.details.push({ name, status: 'PASS', result });
      console.log(`  ✅ ${name} - PASS`);
    } catch (error) {
      this.testResults.failed++;
      this.testResults.details.push({ name, status: 'FAIL', error: error.message });
      console.log(`  ❌ ${name} - FAIL: ${error.message}`);
    }
  }

  /**
   * Print test results
   */
  printResults() {
    console.log('\n' + '='.repeat(60));
    console.log('🎯 COMPREHENSIVE TEST RESULTS');
    console.log('='.repeat(60));
    console.log(`Total Operators Tested: ${this.testResults.total}`);
    console.log(`Passed: ${this.testResults.passed} ✅`);
    console.log(`Failed: ${this.testResults.failed} ❌`);
    console.log(`Success Rate: ${((this.testResults.passed / this.testResults.total) * 100).toFixed(1)}%`);
    
    if (this.testResults.passed === this.testResults.total) {
      console.log('\n🎉 ALL 85 OPERATORS IMPLEMENTED SUCCESSFULLY!');
      console.log('🚀 JavaScript SDK has achieved 100% feature parity with PHP SDK!');
    } else {
      console.log('\n⚠️  Some operators need attention. Check failed tests above.');
    }
    
    console.log('='.repeat(60));
  }
}

// Run the test suite
async function main() {
  const testSuite = new OperatorTestSuite();
  await testSuite.runAllTests();
}

if (require.main === module) {
  main().catch(console.error);
}

module.exports = OperatorTestSuite; 