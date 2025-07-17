#!/usr/bin/env node
/**
 * Test TuskLang Enhanced for JavaScript
 * =====================================
 * Demonstrates all the freedom features
 */

const TuskLangEnhanced = require('./tsk-enhanced.js');

console.log('🐘 Testing TuskLang Enhanced for JavaScript');
console.log('===========================================\n');

// Test 1: Multiple syntax styles
console.log('Test 1: Multiple Syntax Styles');
console.log('------------------------------');

const config1 = `
# Global variables
$app_name: "TestApp JS"
$port: 8080

# Flat style
name: $app_name
version: "1.0"

# TOML style section
[database]
host: "localhost"
port: 5432

# Angle bracket style
server>
    host: "0.0.0.0"
    port: $port
<

# Curly brace style
cache {
    driver: "redis"
    ttl: 300
}
`;

const parser1 = new TuskLangEnhanced();
try {
  const result1 = parser1.parse(config1);
  console.log('✅ Multiple syntax styles parsed:');
  console.log(JSON.stringify(result1, null, 2));
  console.log();
} catch (error) {
  console.log('❌ Error:', error.message);
}

// Test 2: Variables and references
console.log('\nTest 2: Variable Scoping');
console.log('------------------------');

const config2 = `
# Global variable
$timeout: 30

# Top level uses global
default_timeout: $timeout

[api]
# Section-local variable
timeout: 10
endpoint: "/api/v1"
# Reference section variable
config_timeout: timeout

[web]
# Uses global
timeout: $timeout
`;

const parser2 = new TuskLangEnhanced();
try {
  const result2 = parser2.parse(config2);
  console.log('✅ Variable scoping works:');
  console.log(`API timeout (local): ${result2.api.config_timeout}`);
  console.log(`Web timeout (global): ${result2.web.timeout}`);
  console.log();
} catch (error) {
  console.log('❌ Error:', error.message);
}

// Test 3: Date and ranges
console.log('\nTest 3: Date Functions and Ranges');
console.log('---------------------------------');

const config3 = `
year: @date('Y')
timestamp: @date('Y-m-d H:i:s')
iso_date: @date('c')
port_range: 8000-9000
worker_range: 1-10
`;

const parser3 = new TuskLangEnhanced();
try {
  const result3 = parser3.parse(config3);
  console.log('✅ Date and ranges work:');
  console.log(`Year: ${result3.year}`);
  console.log(`Timestamp: ${result3.timestamp}`);
  console.log(`Port range: ${JSON.stringify(result3.port_range)}`);
  console.log();
} catch (error) {
  console.log('❌ Error:', error.message);
}

// Test 4: Optional semicolons
console.log('\nTest 4: Optional Semicolons');
console.log('---------------------------');

const config4 = `
# With semicolons
name: "App";
port: 8080;

# Without semicolons
debug: true
cache: false

# Mixed
timeout: 30;
retry: 3
`;

const parser4 = new TuskLangEnhanced();
try {
  const result4 = parser4.parse(config4);
  console.log('✅ Optional semicolons work:');
  console.log('All values parsed correctly');
  console.log(JSON.stringify(result4, null, 2));
  console.log();
} catch (error) {
  console.log('❌ Error:', error.message);
}

// Test 5: Complex nested structures
console.log('\nTest 5: Complex Nested Structures');
console.log('---------------------------------');

const config5 = `
$company: "TuskJS Inc"

app>
    name: $company
    version: "2.0"
    
    features: ["auth", "api", "websocket"]
    
    database>
        host: "localhost"
        credentials>
            user: "admin"
            pass: "secret"
        <
        
        options {
            pool_size: 10
            timeout: 30
        }
    <
    
    cache {
        driver: "redis"
        ttl: @cache("5m", 300)
    }
<
`;

const parser5 = new TuskLangEnhanced();
try {
  const result5 = parser5.parse(config5);
  console.log('✅ Complex nesting works:');
  console.log(JSON.stringify(result5, null, 2));
  console.log();
} catch (error) {
  console.log('❌ Error:', error.message);
}

// Test 6: Conditional logic
console.log('\nTest 6: Conditional Logic');
console.log('-------------------------');

const config6 = `
$env: "production"

[server]
debug: $env == "development" ? true : false
workers: $env == "production" ? 8 : 2
log_level: $env == "production" ? "error" : "debug"
`;

const parser6 = new TuskLangEnhanced();
try {
  const result6 = parser6.parse(config6);
  console.log('✅ Conditional logic works:');
  console.log(`Debug: ${result6.server.debug}`);
  console.log(`Workers: ${result6.server.workers}`);
  console.log(`Log level: ${result6.server.log_level}`);
  console.log();
} catch (error) {
  console.log('❌ Error:', error.message);
}

// Test 7: String concatenation
console.log('\nTest 7: String Concatenation');
console.log('----------------------------');

const config7 = `
$base: "TuskLang"
$version: "2.0"

full_name: $base + " v" + $version
api_url: "https://api.example.com/" + $base
`;

const parser7 = new TuskLangEnhanced();
try {
  const result7 = parser7.parse(config7);
  console.log('✅ String concatenation works:');
  console.log(`Full name: ${result7.full_name}`);
  console.log(`API URL: ${result7.api_url}`);
  console.log();
} catch (error) {
  console.log('❌ Error:', error.message);
}

// Demo database adapter
console.log('\nTest 8: Database Queries (Mock)');
console.log('-------------------------------');

// Mock database adapter
const mockDbAdapter = {
  async query(sql, params) {
    console.log(`Mock query: ${sql}`, params);
    if (sql.includes('COUNT')) return 42;
    if (sql.includes('rate_limit')) return 1000;
    return [];
  }
};

const config8 = `
[database]
user_count: @query("SELECT COUNT(*) FROM users")
rate_limit: @query("SELECT rate_limit FROM plans WHERE id = ?", 1)
`;

const parser8 = new TuskLangEnhanced();
parser8.setDatabaseAdapter(mockDbAdapter);

(async () => {
  try {
    const result8 = await parser8.parse(config8);
    console.log('✅ Database queries work:');
    console.log(`User count: ${result8.database.user_count}`);
    console.log(`Rate limit: ${result8.database.rate_limit}`);
    console.log();
  } catch (error) {
    console.log('❌ Error:', error.message);
  }

  console.log('🎉 TuskLang Enhanced for JavaScript: Complete freedom!');
  console.log('Use [], use >, use {}, or mix them all!');
})();