#!/usr/bin/env ruby
# frozen_string_literal: true

# TuskLang Ruby SDK Example
# Demonstrates basic usage of the TuskLang Ruby SDK

require_relative 'lib/tusk_lang'

puts "🐘 TuskLang Ruby SDK Example"
puts "=" * 50

# Example 1: Basic TSK parsing
puts "\n1. Basic TSK Parsing"
puts "-" * 30

tsk_content = <<~TSK
  [app]
  name = "Example App"
  version = "1.0.0"
  debug = true
  port = 8080

  [config]
  host = "localhost"
  timeout = 30.5
  features = ["redis", "postgresql"]
  settings = { "cache" = true, "log_level" = "info" }
TSK

tsk = TuskLang::TSK.from_string(tsk_content)

puts "App name: #{tsk.get_value('app', 'name')}"
puts "Version: #{tsk.get_value('app', 'version')}"
puts "Debug mode: #{tsk.get_value('app', 'debug')}"
puts "Port: #{tsk.get_value('app', 'port')}"
puts "Host: #{tsk.get_value('config', 'host')}"
puts "Features: #{tsk.get_value('config', 'features')}"

# Example 2: FUJSEN function execution
puts "\n2. FUJSEN Function Execution"
puts "-" * 30

fujsen_content = <<~TSK
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

  process_data_fujsen = """
  function process(data) {
    return {
      sum: data.reduce((acc, val) => acc + val, 0),
      count: data.length,
      average: data.reduce((acc, val) => acc + val, 0) / data.length
    };
  }
  """
TSK

fujsen_tsk = TuskLang::TSK.from_string(fujsen_content)

sum = fujsen_tsk.execute_fujsen("calculator", "add", 5, 3)
product = fujsen_tsk.execute_fujsen("calculator", "multiply", 4, 7)

puts "5 + 3 = #{sum}"
puts "4 * 7 = #{product}"

data = [1, 2, 3, 4, 5]
result = fujsen_tsk.execute_fujsen("calculator", "process", data)
puts "Data processing:"
puts "  Sum: #{result['sum']}"
puts "  Count: #{result['count']}"
puts "  Average: #{result['average']}"

# Example 3: @ Operator system
puts "\n3. @ Operator System"
puts "-" * 30

operator_tsk = TuskLang::TSK.new

# Date operator
date_result = operator_tsk.execute_operators("@date('%Y-%m-%d')")
puts "Current date: #{date_result}"

# Cache operator
context = { 'cache_value' => 'test_data' }
cache_result = operator_tsk.execute_operators("@cache('5m', 'test_key')", context)
puts "Cache result: #{cache_result['cached']} (TTL: #{cache_result['ttl']}s)"

# Metrics operator
metrics_result = operator_tsk.execute_operators("@metrics('test_metric', 42.5)")
puts "Metric: #{metrics_result['metric']} = #{metrics_result['value']}"

# Feature detection
feature_result = operator_tsk.execute_operators("@feature('rails')")
puts "Rails feature available: #{feature_result}"

# FlexChain operations
flex_balance = operator_tsk.execute_operators("@flex('balance', '0x123...')")
puts "Flex balance: #{flex_balance['balance']} #{flex_balance['currency']}"

# Example 4: Shell storage
puts "\n4. Shell Storage"
puts "-" * 30

data = "Hello, TuskLang!"
shell_data = TuskLang::ShellStorage.create_shell_data(data, "test_id")
binary = TuskLang::ShellStorage.pack(shell_data)
retrieved = TuskLang::ShellStorage.unpack(binary)
type = TuskLang::ShellStorage.detect_type(binary)

puts "Original data: #{data}"
puts "Retrieved data: #{retrieved[:data]}"
puts "Data type: #{type}"
puts "Storage ID: #{retrieved[:id]}"

# Example 5: Rails-like configuration
puts "\n5. Rails-like Configuration"
puts "-" * 30

rails_config = <<~TSK
  [app]
  name = "Rails Example"
  environment = "@env('RAILS_ENV')"
  secret_key_base = "@env('SECRET_KEY_BASE')"

  [database]
  adapter = "postgresql"
  host = "localhost"
  database = "example_db"

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

rails_tsk = TuskLang::TSK.from_string(rails_config)

# Set environment variables for demo
ENV['RAILS_ENV'] = 'production'
ENV['SECRET_KEY_BASE'] = 'demo_secret_key'

puts "App name: #{rails_tsk.get_value('app', 'name')}"
puts "Environment: #{rails_tsk.execute_operators("@env('RAILS_ENV')")}"
puts "Database adapter: #{rails_tsk.get_value('database', 'adapter')}"

# Test FUJSEN processing
test_data = [
  { "id" => 1, "value" => 10 },
  { "id" => 2, "value" => 20 }
]

result = rails_tsk.execute_fujsen("processing", "transform", test_data)
puts "Processing result:"
puts "  Processed: #{result['processed']}"
puts "  Count: #{result['count']}"
puts "  Items: #{result['items'].length}"

# Example 6: Smart contract simulation
puts "\n6. Smart Contract Simulation"
puts "-" * 30

contract_content = <<~TSK
  [payment]
  name = "Payment Processor"
  fee_rate = 0.025
  max_amount = 100000

  process_payment_fujsen = """
  function processPayment(amount, recipient, paymentMethod) {
    if (amount <= 0) {
      return {
        success: false,
        error: 'Invalid amount'
      };
    }
    
    if (amount > 100000) {
      return {
        success: false,
        error: 'Amount exceeds maximum limit'
      };
    }
    
    const fee = amount * 0.025;
    const netAmount = amount - fee;
    
    return {
      success: true,
      transactionId: 'pay_' + Date.now(),
      amount: amount,
      fee: fee,
      netAmount: netAmount,
      recipient: recipient,
      paymentMethod: paymentMethod,
      timestamp: new Date().toISOString()
    };
  }
  """
TSK

contract_tsk = TuskLang::TSK.from_string(contract_content)

# Process a payment
payment_result = contract_tsk.execute_fujsen("payment", "process", 100.50, "alice@example.com", "credit_card")
puts "Payment processing:"
puts "  Success: #{payment_result['success']}"
puts "  Amount: $#{payment_result['amount']}"
puts "  Fee: $#{payment_result['fee']}"
puts "  Net amount: $#{payment_result['netAmount']}"
puts "  Recipient: #{payment_result['recipient']}"
puts "  Transaction ID: #{payment_result['transactionId']}"

puts "\n" + "=" * 50
puts "✅ TuskLang Ruby SDK Example Complete!"
puts "The SDK provides powerful configuration management with FUJSEN functions and @ operators." 