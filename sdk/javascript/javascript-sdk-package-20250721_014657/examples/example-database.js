#!/usr/bin/env node
/**
 * TuskLang Database Example
 * =========================
 * Demonstrates the KILLER FEATURE: Database queries in config files!
 */

const TuskLang = require('./index.js');
const SQLiteAdapter = require('./adapters/sqlite.js');

console.log('🐘 TuskLang Database Integration Demo');
console.log('====================================\n');

// Create TuskLang instance
const tusk = new TuskLang();

// Set up SQLite database
console.log('Setting up test database...');
const dbAdapter = new SQLiteAdapter({ filename: ':memory:' });
dbAdapter.createTestData();

tusk.setDatabase({
  type: 'sqlite',
  filename: ':memory:'
});

// Actually, we need to set the adapter directly since we created it
tusk.parser.setDatabaseAdapter(dbAdapter);

// Configuration with database queries
const config = `
# 🐘 TuskLang Configuration with Live Database Queries!
# ====================================================

$app_name: "TuskLang Demo"
$environment: "production"

# Basic configuration
app_name: $app_name
version: "2.0.0"
started_at: @date('Y-m-d H:i:s')

# User statistics from database
[statistics]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
newest_user: @query("SELECT name FROM users ORDER BY created_at DESC LIMIT 1")

# Dynamic rate limiting based on plan
[rate_limits]
# Each plan has different limits
basic_limit: @query("SELECT rate_limit FROM plans WHERE name = 'basic'")
premium_limit: @query("SELECT rate_limit FROM plans WHERE name = 'premium'")
enterprise_limit: @query("SELECT rate_limit FROM plans WHERE name = 'enterprise'")

# Feature flags from database
[features]
# Get all enabled features
enabled_features: @query("SELECT name FROM feature_flags WHERE enabled = 1")
dark_mode: @query("SELECT enabled FROM feature_flags WHERE name = 'dark_mode'")
new_dashboard: @query("SELECT enabled FROM feature_flags WHERE name = 'new_dashboard'")

# Pricing information
[pricing]
plans: @query("SELECT name, price FROM plans ORDER BY price")
cheapest_plan: @query("SELECT name FROM plans ORDER BY price ASC LIMIT 1")
most_expensive: @query("SELECT price FROM plans ORDER BY price DESC LIMIT 1")

# Dynamic configuration based on environment
[server]
# Different settings for different environments
max_connections: $environment == "production" ? 100 : 10
workers: $environment == "production" ? @query("SELECT COUNT(*) FROM users") / 100 : 2
cache_size: @cache("5m", @query("SELECT SUM(rate_limit) FROM plans"))

# Conditional features based on data
[conditional]
# Enable premium features if we have premium users
premium_features: @query("SELECT COUNT(*) FROM plans WHERE name = 'premium'") > 0
# Scale based on load
needs_scaling: @query("SELECT COUNT(*) FROM users") > 100
`;

// Parse the configuration
console.log('Parsing configuration with live queries...\n');
const parsed = tusk.parse(config);

// Display results
console.log('📊 Configuration Results:');
console.log('========================\n');

console.log('Basic Info:');
console.log(`- App: ${parsed.app_name}`);
console.log(`- Version: ${parsed.version}`);
console.log(`- Started: ${parsed.started_at}`);
console.log();

console.log('User Statistics:');
console.log(`- Total Users: ${parsed.statistics.total_users}`);
console.log(`- Active Users: ${parsed.statistics.active_users}`);
console.log(`- Newest User: ${parsed.statistics.newest_user}`);
console.log();

console.log('Rate Limits by Plan:');
console.log(`- Basic: ${parsed.rate_limits.basic_limit} requests/hour`);
console.log(`- Premium: ${parsed.rate_limits.premium_limit} requests/hour`);
console.log(`- Enterprise: ${parsed.rate_limits.enterprise_limit} requests/hour`);
console.log();

console.log('Feature Flags:');
console.log(`- Enabled Features: ${JSON.stringify(parsed.features.enabled_features)}`);
console.log(`- Dark Mode: ${parsed.features.dark_mode ? 'ON' : 'OFF'}`);
console.log(`- New Dashboard: ${parsed.features.new_dashboard ? 'ON' : 'OFF'}`);
console.log();

console.log('Pricing:');
console.log(`- All Plans: ${JSON.stringify(parsed.pricing.plans)}`);
console.log(`- Cheapest: ${parsed.pricing.cheapest_plan}`);
console.log(`- Most Expensive: $${parsed.pricing.most_expensive}`);
console.log();

console.log('Dynamic Server Config:');
console.log(`- Max Connections: ${parsed.server.max_connections}`);
console.log(`- Workers: ${parsed.server.workers}`);
console.log(`- Cache Size: ${parsed.server.cache_size}`);
console.log();

// Show the power of dynamic updates
console.log('💡 The Power of TuskLang:');
console.log('========================');
console.log('Your configuration automatically updates as your database changes!');
console.log('No more hardcoded values - let your config adapt to your data.');
console.log();

// Clean up
dbAdapter.close();

console.log('🎉 TuskLang: Where configuration meets intelligence!');
console.log('Try it yourself: npm install tusklang');