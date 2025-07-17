#!/usr/bin/env ruby
# frozen_string_literal: true

# Test suite for TuskLang Ruby SDK
# Run with: ruby test_tsk.rb

require_relative 'tsk'
require_relative 'tsk_parser'
require_relative 'shell_storage'

# Test runner
class TuskLangTest
  def self.run_all_tests
    puts "🐘 TuskLang Ruby SDK Test Suite"
    puts "=" * 50

    tests = [
      :test_basic_parsing,
      :test_fujsen_execution,
      :test_operator_system,
      :test_shell_storage,
      :test_rails_integration,
      :test_jekyll_integration,
      :test_devops_automation,
      :test_smart_contracts
    ]

    passed = 0
    failed = 0

    tests.each do |test|
      begin
        send(test)
        puts "✅ #{test}"
        passed += 1
      rescue => e
        puts "❌ #{test}: #{e.message}"
        failed += 1
      end
    end

    puts "\n" + "=" * 50
    puts "Results: #{passed} passed, #{failed} failed"
    puts "Total: #{tests.length} tests"
  end

  # Test basic TSK parsing
  def self.test_basic_parsing
    content = <<~TSK
      [app]
      name = "Test Application"
      version = "1.0.0"
      debug = true
      port = 8080

      [config]
      host = "localhost"
      timeout = 30.5
      features = ["redis", "postgresql"]
      settings = { "cache" = true, "log_level" = "info" }
    TSK

    tsk = TuskLang::TSK.from_string(content)

    # Test section access
    assert_equal("Test Application", tsk.get_value("app", "name"))
    assert_equal("1.0.0", tsk.get_value("app", "version"))
    assert_equal(true, tsk.get_value("app", "debug"))
    assert_equal(8080, tsk.get_value("app", "port"))

    # Test nested data
    assert_equal("localhost", tsk.get_value("config", "host"))
    assert_equal(30.5, tsk.get_value("config", "timeout"))
    assert_equal(["redis", "postgresql"], tsk.get_value("config", "features"))
    assert_equal({ "cache" => true, "log_level" => "info" }, tsk.get_value("config", "settings"))

    # Test stringification
    stringified = tsk.to_s
    assert(stringified.include?("[app]"))
    assert(stringified.include?('name = "Test Application"'))
  end

  # Test FUJSEN function execution
  def self.test_fujsen_execution
    content = <<~TSK
      [calculator]
      add_fujsen = """
      function add(a, b) {
        return a + b;
      }
      """

      multiply_fujsen = """
      function multiply(a, b) {
        return a * b;
      }
      """

      complex_fujsen = """
      function process(data) {
        return {
          sum: data.reduce((acc, val) => acc + val, 0),
          count: data.length,
          average: data.reduce((acc, val) => acc + val, 0) / data.length
        };
      }
      """
    TSK

    tsk = TuskLang::TSK.from_string(content)

    # Test basic arithmetic
    assert_equal(8, tsk.execute_fujsen("calculator", "add", 5, 3))
    assert_equal(28, tsk.execute_fujsen("calculator", "multiply", 4, 7))

    # Test complex function
    data = [1, 2, 3, 4, 5]
    result = tsk.execute_fujsen("calculator", "complex", data)
    assert_equal(15, result["sum"])
    assert_equal(5, result["count"])
    assert_equal(3.0, result["average"])
  end

  # Test @ operator system
  def self.test_operator_system
    tsk = TuskLang::TSK.new

    # Test date operator
    date_result = tsk.execute_operators("@date('%Y-%m-%d')")
    assert(date_result.match?(/^\d{4}-\d{2}-\d{2}$/))

    # Test cache operator
    context = { 'cache_value' => 'test_data' }
    cache_result = tsk.execute_operators("@cache('5m', 'test_key')", context)
    assert_equal(true, cache_result["cached"])
    assert_equal(300.0, cache_result["ttl"])
    assert_equal("test_key", cache_result["key"])

    # Test metrics operator
    metrics_result = tsk.execute_operators("@metrics('test_metric', 42.5)")
    assert_equal("test_metric", metrics_result["metric"])
    assert_equal(42.5, metrics_result["value"])

    # Test if operator
    if_result = tsk.execute_operators("@if(true, 'yes', 'no')")
    assert_equal("yes", if_result)

    # Test env operator
    ENV['TEST_VAR'] = 'test_value'
    env_result = tsk.execute_operators("@env('TEST_VAR')")
    assert_equal("test_value", env_result)

    # Test feature operator
    feature_result = tsk.execute_operators("@feature('rails')")
    assert_equal(true, feature_result)

    # Test FlexChain operators
    flex_balance = tsk.execute_operators("@flex('balance', '0x123...')")
    assert_equal("0x123...", flex_balance["address"])
    assert_equal("FLEX", flex_balance["currency"])

    flex_transfer = tsk.execute_operators("@flex('transfer', 100, '0x123...', '0x456...')")
    assert_equal(true, flex_transfer["success"])
    assert_equal(100.0, flex_transfer["amount"])
  end

  # Test Shell storage
  def self.test_shell_storage
    # Test data creation
    data = "Hello, TuskLang!"
    shell_data = TuskLang::ShellStorage.create_shell_data(data, "test_id")
    assert_equal("test_id", shell_data[:id])
    assert_equal(data, shell_data[:data])

    # Test packing and unpacking
    binary = TuskLang::ShellStorage.pack(shell_data)
    retrieved = TuskLang::ShellStorage.unpack(binary)
    assert_equal(data, retrieved[:data])
    assert_equal("test_id", retrieved[:id])

    # Test type detection
    type = TuskLang::ShellStorage.detect_type(binary)
    assert_equal("text", type)

    # Test with JSON data
    json_data = '{"key": "value", "number": 42}'
    json_shell = TuskLang::ShellStorage.create_shell_data(json_data, "json_test")
    json_binary = TuskLang::ShellStorage.pack(json_shell)
    json_type = TuskLang::ShellStorage.detect_type(json_binary)
    assert_equal("json", json_type)
  end

  # Test Rails integration
  def self.test_rails_integration
    # Simulate Rails configuration
    config_content = <<~TSK
      [app]
      name = "Rails Test App"
      debug = true
      environment = "test"

      [database]
      adapter = "sqlite3"
      database = "test.sqlite3"

      [api]
      key = "@env('API_KEY')"
      endpoint = "https://api.example.com"

      [processing]
      transform_fujsen = """
      function transform(data) {
        return {
          processed: true,
          timestamp: new Date().toISOString(),
          count: data.length,
          items: data.map(item => ({
            id: item.id,
            value: item.value * 2
          }))
        };
      }
      """
    TSK

    tsk = TuskLang::TSK.from_string(config_content)

    # Test configuration loading
    assert_equal("Rails Test App", tsk.get_value("app", "name"))
    assert_equal(true, tsk.get_value("app", "debug"))
    assert_equal("sqlite3", tsk.get_value("database", "adapter"))

    # Test FUJSEN processing
    test_data = [
      { "id" => 1, "value" => 10 },
      { "id" => 2, "value" => 20 },
      { "id" => 3, "value" => 30 }
    ]

    result = tsk.execute_fujsen("processing", "transform", test_data)
    assert_equal(true, result["processed"])
    assert_equal(3, result["count"])
    assert_equal(20, result["items"][0]["value"])
    assert_equal(40, result["items"][1]["value"])
  end

  # Test Jekyll integration
  def self.test_jekyll_integration
    # Simulate Jekyll configuration
    jekyll_config = <<~TSK
      [site]
      title = "My Jekyll Blog"
      description = "Built with Jekyll and TuskLang"
      url = "https://example.com"
      author = "John Doe"

      [build]
      destination = "_site"
      plugins = ["jekyll-feed", "jekyll-seo-tag"]

      [content]
      generate_posts_fujsen = """
      function generate_posts() {
        return [
          {
            title: "First Post",
            date: "2024-01-15",
            slug: "first-post",
            content: "This is the first post content."
          },
          {
            title: "Second Post", 
            date: "2024-01-16",
            slug: "second-post",
            content: "This is the second post content."
          }
        ];
      }
      """
    TSK

    tsk = TuskLang::TSK.from_string(jekyll_config)

    # Test site configuration
    assert_equal("My Jekyll Blog", tsk.get_value("site", "title"))
    assert_equal("https://example.com", tsk.get_value("site", "url"))
    assert_equal("_site", tsk.get_value("build", "destination"))

    # Test content generation
    posts = tsk.execute_fujsen("content", "generate_posts")
    assert_equal(2, posts.length)
    assert_equal("First Post", posts[0]["title"])
    assert_equal("second-post", posts[1]["slug"])
  end

  # Test DevOps automation
  def self.test_devops_automation
    # Simulate deployment configuration
    deploy_config = <<~TSK
      [deploy]
      environment = "production"
      region = "us-west-2"

      [aws]
      instance_type = "t3.micro"
      ami_id = "ami-12345678"

      [deploy]
      prepare_fujsen = """
      function prepare(context) {
        return {
          target: context.environment === 'production' ? 'prod-server' : 'staging-server',
          steps: [
            { name: 'Backup database', command: 'pg_dump' },
            { name: 'Deploy code', command: 'git pull' },
            { name: 'Restart services', command: 'systemctl restart app' }
          ],
          rollback: context.environment === 'production'
        };
      }
      """

      [monitoring]
      check_health_fujsen = """
      function checkHealth() {
        return {
          status: 'healthy',
          uptime: 99.9,
          services: ['web', 'database', 'cache'],
          timestamp: new Date().toISOString()
        };
      }
      """
    TSK

    tsk = TuskLang::TSK.from_string(deploy_config)

    # Test deployment preparation
    context = {
      'environment' => 'production',
      'branch' => 'main'
    }

    deploy_plan = tsk.execute_fujsen("deploy", "prepare", context)
    assert_equal("prod-server", deploy_plan["target"])
    assert_equal(true, deploy_plan["rollback"])
    assert_equal(3, deploy_plan["steps"].length)

    # Test health monitoring
    health = tsk.execute_fujsen("monitoring", "checkHealth")
    assert_equal("healthy", health["status"])
    assert_equal(99.9, health["uptime"])
    assert_equal(3, health["services"].length)
  end

  # Test smart contracts
  def self.test_smart_contracts
    # Simulate DeFi liquidity pool contract
    pool_contract = <<~TSK
      [pool]
      token_a = "FLEX"
      token_b = "USDT"
      reserve_a = 100000
      reserve_b = 50000
      fee_rate = 0.003

      [pool]
      swap_fujsen = """
      function swap(amountIn, tokenIn) {
        const k = 100000 * 50000;
        const fee = amountIn * 0.003;
        const amountInWithFee = amountIn - fee;
        
        if (tokenIn === 'FLEX') {
          const amountOut = (amountInWithFee * 50000) / (100000 + amountInWithFee);
          return { 
            amountOut: amountOut,
            fee: fee,
            priceImpact: (amountIn / 100000) * 100,
            success: true
          };
        } else {
          const amountOut = (amountInWithFee * 100000) / (50000 + amountInWithFee);
          return { 
            amountOut: amountOut,
            fee: fee,
            priceImpact: (amountIn / 50000) * 100,
            success: true
          };
        }
      }
      """

      [pool]
      add_liquidity_fujsen = """
      function addLiquidity(amountA, amountB) {
        const totalSupply = 1000; // LP tokens
        const liquidity = Math.min(amountA * totalSupply / 100000, amountB * totalSupply / 50000);
        
        return {
          liquidity: liquidity,
          lpTokens: liquidity,
          success: true
        };
      }
      """
    TSK

    tsk = TuskLang::TSK.from_string(pool_contract)

    # Test swap function
    swap_result = tsk.execute_fujsen("pool", "swap", 1000, "FLEX")
    assert_equal(true, swap_result["success"])
    assert(swap_result["amountOut"] > 0)
    assert(swap_result["fee"] > 0)
    assert(swap_result["priceImpact"] > 0)

    # Test add liquidity function
    liquidity_result = tsk.execute_fujsen("pool", "addLiquidity", 5000, 2500)
    assert_equal(true, liquidity_result["success"])
    assert(liquidity_result["liquidity"] > 0)
    assert_equal(liquidity_result["liquidity"], liquidity_result["lpTokens"])
  end

  # Helper assertion methods
  def self.assert_equal(expected, actual)
    raise "Expected #{expected}, got #{actual}" unless expected == actual
  end

  def self.assert(condition)
    raise "Assertion failed" unless condition
  end
end

# Run tests if this file is executed directly
if __FILE__ == $0
  TuskLangTest.run_all_tests
end 