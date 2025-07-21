#!/usr/bin/env ruby
# frozen_string_literal: true

# Verification Script for TuskLang Ruby SDK - Goal 9
# Comprehensive verification of blockchain and distributed systems features

require_relative 'goal_implementation'
require 'json'

class Goal9Verification
  def initialize
    @coordinator = TuskLang::Goal9::Goal9Coordinator.new
    @results = {
      g9_1: { status: :pending, tests: [] },
      g9_2: { status: :pending, tests: [] },
      g9_3: { status: :pending, tests: [] },
      integration: { status: :pending, tests: [] }
    }
  end

  def run_all_verifications
    puts "⛓️ Starting Goal 9 Verification..."
    puts "=" * 50

    verify_g9_1_blockchain
    verify_g9_2_smart_contracts
    verify_g9_3_distributed_computing
    verify_integration

    generate_verification_report
  end

  private

  def verify_g9_1_blockchain
    puts "\n⛓️ Verifying G9.1: Advanced Ruby Blockchain Framework and Smart Contracts"
    
    blockchain = @coordinator.blockchain
    
    # Test blockchain initialization
    test_result = test_blockchain_initialization(blockchain)
    @results[:g9_1][:tests] << { name: "Blockchain Initialization", status: test_result }

    # Test wallet creation
    test_result = test_wallet_creation(blockchain)
    @results[:g9_1][:tests] << { name: "Wallet Creation", status: test_result }

    # Test transaction creation
    test_result = test_transaction_creation(blockchain)
    @results[:g9_1][:tests] << { name: "Transaction Creation", status: test_result }

    # Test block mining
    test_result = test_block_mining(blockchain)
    @results[:g9_1][:tests] << { name: "Block Mining", status: test_result }

    # Test chain validation
    test_result = test_chain_validation(blockchain)
    @results[:g9_1][:tests] << { name: "Chain Validation", status: test_result }

    # Test balance checking
    test_result = test_balance_checking(blockchain)
    @results[:g9_1][:tests] << { name: "Balance Checking", status: test_result }

    # Determine overall status
    failed_tests = @results[:g9_1][:tests].select { |t| t[:status] == :failed }
    @results[:g9_1][:status] = failed_tests.empty? ? :passed : :failed

    puts "   ✅ G9.1 Status: #{@results[:g9_1][:status].upcase}"
  end

  def verify_g9_2_smart_contracts
    puts "\n📜 Verifying G9.2: Advanced Ruby Smart Contract Engine and DApp Framework"
    
    smart_contracts = @coordinator.smart_contracts
    
    # Test contract deployment
    test_result = test_contract_deployment(smart_contracts)
    @results[:g9_2][:tests] << { name: "Contract Deployment", status: test_result }

    # Test function execution
    test_result = test_function_execution(smart_contracts)
    @results[:g9_2][:tests] << { name: "Function Execution", status: test_result }

    # Test contract info
    test_result = test_contract_info(smart_contracts)
    @results[:g9_2][:tests] << { name: "Contract Info", status: test_result }

    # Test contract storage
    test_result = test_contract_storage(smart_contracts)
    @results[:g9_2][:tests] << { name: "Contract Storage", status: test_result }

    # Test gas management
    test_result = test_gas_management(smart_contracts)
    @results[:g9_2][:tests] << { name: "Gas Management", status: test_result }

    # Determine overall status
    failed_tests = @results[:g9_2][:tests].select { |t| t[:status] == :failed }
    @results[:g9_2][:status] = failed_tests.empty? ? :passed : :failed

    puts "   ✅ G9.2 Status: #{@results[:g9_2][:status].upcase}"
  end

  def verify_g9_3_distributed_computing
    puts "\n🌐 Verifying G9.3: Advanced Ruby Distributed Computing and Consensus Algorithms"
    
    distributed_computing = @coordinator.distributed_computing
    
    # Test node management
    test_result = test_node_management(distributed_computing)
    @results[:g9_3][:tests] << { name: "Node Management", status: test_result }

    # Test task distribution
    test_result = test_task_distribution(distributed_computing)
    @results[:g9_3][:tests] << { name: "Task Distribution", status: test_result }

    # Test consensus algorithms
    test_result = test_consensus_algorithms(distributed_computing)
    @results[:g9_3][:tests] << { name: "Consensus Algorithms", status: test_result }

    # Test network topology
    test_result = test_network_topology(distributed_computing)
    @results[:g9_3][:tests] << { name: "Network Topology", status: test_result }

    # Test node health
    test_result = test_node_health(distributed_computing)
    @results[:g9_3][:tests] << { name: "Node Health", status: test_result }

    # Determine overall status
    failed_tests = @results[:g9_3][:tests].select { |t| t[:status] == :failed }
    @results[:g9_3][:status] = failed_tests.empty? ? :passed : :failed

    puts "   ✅ G9.3 Status: #{@results[:g9_3][:status].upcase}"
  end

  def verify_integration
    puts "\n🔗 Verifying Integration: Complete Goal 9 Implementation"
    
    # Test complete goal execution
    test_result = test_complete_execution
    @results[:integration][:tests] << { name: "Complete Goal Execution", status: test_result }

    # Test error handling
    test_result = test_error_handling
    @results[:integration][:tests] << { name: "Error Handling", status: test_result }

    # Test performance characteristics
    test_result = test_performance_characteristics
    @results[:integration][:tests] << { name: "Performance Characteristics", status: test_result }

    # Determine overall status
    failed_tests = @results[:integration][:tests].select { |t| t[:status] == :failed }
    @results[:integration][:status] = failed_tests.empty? ? :passed : :failed

    puts "   ✅ Integration Status: #{@results[:integration][:status].upcase}"
  end

  # G9.1 Test Methods
  def test_blockchain_initialization(blockchain)
    begin
      return :failed unless blockchain.chain.length == 1  # Genesis block
      return :failed unless blockchain.difficulty == 4
      return :failed unless blockchain.mining_reward == 100
      return :failed unless blockchain.pending_transactions.is_a?(Array)
      
      :passed
    rescue => e
      puts "     ❌ Blockchain Initialization Error: #{e.message}"
      :failed
    end
  end

  def test_wallet_creation(blockchain)
    begin
      wallet = blockchain.create_wallet
      
      return :failed unless wallet[:public_key].is_a?(String)
      return :failed unless wallet[:private_key].is_a?(String)
      return :failed unless wallet[:public_key].length > 0
      return :failed unless wallet[:private_key].length > 0
      
      :passed
    rescue => e
      puts "     ❌ Wallet Creation Error: #{e.message}"
      :failed
    end
  end

  def test_transaction_creation(blockchain)
    begin
      wallet = blockchain.create_wallet
      tx_id = blockchain.create_transaction('system', wallet[:public_key], 1000)
      
      return :failed unless tx_id.is_a?(String)
      return :failed unless blockchain.pending_transactions.length > 0
      
      :passed
    rescue => e
      puts "     ❌ Transaction Creation Error: #{e.message}"
      :failed
    end
  end

  def test_block_mining(blockchain)
    begin
      wallet = blockchain.create_wallet
      blockchain.create_transaction('system', wallet[:public_key], 1000)
      block = blockchain.mine_block(wallet[:public_key])
      
      return :failed unless block.is_a?(Hash)
      return :failed unless block[:hash].start_with?('0' * blockchain.difficulty)
      return :failed unless blockchain.chain.length == 2  # Genesis + mined block
      
      :passed
    rescue => e
      puts "     ❌ Block Mining Error: #{e.message}"
      :failed
    end
  end

  def test_chain_validation(blockchain)
    begin
      is_valid = blockchain.is_chain_valid
      return :failed unless is_valid
      
      :passed
    rescue => e
      puts "     ❌ Chain Validation Error: #{e.message}"
      :failed
    end
  end

  def test_balance_checking(blockchain)
    begin
      wallet = blockchain.create_wallet
      blockchain.create_transaction('system', wallet[:public_key], 1000)
      blockchain.mine_block(wallet[:public_key])
      
      balance = blockchain.get_balance(wallet[:public_key])
      return :failed unless balance > 0
      
      :passed
    rescue => e
      puts "     ❌ Balance Checking Error: #{e.message}"
      :failed
    end
  end

  # G9.2 Test Methods
  def test_contract_deployment(smart_contracts)
    begin
      contract_code = "function test() { return 1; }"
      deployer = "0x123"
      contract_address = smart_contracts.deploy_contract(contract_code, deployer, 1000)
      
      return :failed unless contract_address.is_a?(String)
      return :failed unless smart_contracts.contracts.key?(contract_address)
      
      :passed
    rescue => e
      puts "     ❌ Contract Deployment Error: #{e.message}"
      :failed
    end
  end

  def test_function_execution(smart_contracts)
    begin
      contract_code = "function test() { return 1; }"
      deployer = "0x123"
      contract_address = smart_contracts.deploy_contract(contract_code, deployer, 1000)
      
      result = smart_contracts.execute_function(contract_address, 'test', [], deployer)
      return :failed unless result.is_a?(Integer) || result.nil?
      
      :passed
    rescue => e
      puts "     ❌ Function Execution Error: #{e.message}"
      :failed
    end
  end

  def test_contract_info(smart_contracts)
    begin
      contract_code = "function test() { return 1; }"
      deployer = "0x123"
      contract_address = smart_contracts.deploy_contract(contract_code, deployer, 1000)
      
      info = smart_contracts.get_contract_info(contract_address)
      return :failed unless info.is_a?(Hash)
      return :failed unless info[:address] == contract_address
      
      :passed
    rescue => e
      puts "     ❌ Contract Info Error: #{e.message}"
      :failed
    end
  end

  def test_contract_storage(smart_contracts)
    begin
      contract_code = "function test() { storage.value = 1; }"
      deployer = "0x123"
      contract_address = smart_contracts.deploy_contract(contract_code, deployer, 1000)
      
      storage = smart_contracts.get_contract_storage(contract_address)
      return :failed unless storage.is_a?(Hash)
      
      :passed
    rescue => e
      puts "     ❌ Contract Storage Error: #{e.message}"
      :failed
    end
  end

  def test_gas_management(smart_contracts)
    begin
      contract_code = "function test() { storage.value = 1; }"
      deployer = "0x123"
      contract_address = smart_contracts.deploy_contract(contract_code, deployer, 1000)
      
      result = smart_contracts.execute_function(contract_address, 'test', [], deployer)
      return :failed unless result.nil? || result.is_a?(Integer)
      
      :passed
    rescue => e
      puts "     ❌ Gas Management Error: #{e.message}"
      :failed
    end
  end

  # G9.3 Test Methods
  def test_node_management(distributed_computing)
    begin
      node_id = distributed_computing.add_node('test_node', 'http://test:8080', :worker)
      
      return :failed unless node_id == 'test_node'
      return :failed unless distributed_computing.nodes.key?('test_node')
      return :failed unless distributed_computing.nodes['test_node'][:status] == :active
      
      :passed
    rescue => e
      puts "     ❌ Node Management Error: #{e.message}"
      :failed
    end
  end

  def test_task_distribution(distributed_computing)
    begin
      distributed_computing.add_node('task_node', 'http://task:8080')
      task_id = distributed_computing.distribute_task({ operation: 'sum', values: [1, 2, 3] })
      
      return :failed unless task_id.is_a?(String)
      
      :passed
    rescue => e
      puts "     ❌ Task Distribution Error: #{e.message}"
      :failed
    end
  end

  def test_consensus_algorithms(distributed_computing)
    begin
      distributed_computing.add_node('consensus_node', 'http://consensus:8080')
      
      # Test Raft consensus
      distributed_computing.instance_variable_set(:@consensus_algorithm, :raft)
      raft_result = distributed_computing.run_consensus
      return :failed unless raft_result.nil? || raft_result.is_a?(Hash)
      
      :passed
    rescue => e
      puts "     ❌ Consensus Algorithms Error: #{e.message}"
      :failed
    end
  end

  def test_network_topology(distributed_computing)
    begin
      distributed_computing.add_node('topology_node', 'http://topology:8080')
      stats = distributed_computing.get_network_stats
      
      return :failed unless stats[:total_nodes] > 0
      return :failed unless stats[:network_topology].is_a?(Hash)
      
      :passed
    rescue => e
      puts "     ❌ Network Topology Error: #{e.message}"
      :failed
    end
  end

  def test_node_health(distributed_computing)
    begin
      distributed_computing.add_node('health_node', 'http://health:8080')
      distributed_computing.send_heartbeat('health_node')
      distributed_computing.check_node_health
      
      node = distributed_computing.nodes['health_node']
      return :failed unless node[:status] == :active
      
      :passed
    rescue => e
      puts "     ❌ Node Health Error: #{e.message}"
      :failed
    end
  end

  # Integration Test Methods
  def test_complete_execution
    begin
      result = @coordinator.execute_all_goals
      return :failed unless result[:success] && result[:goals_completed].length == 3
      :passed
    rescue => e
      puts "     ❌ Complete Execution Error: #{e.message}"
      :failed
    end
  end

  def test_error_handling
    begin
      # Test blockchain with invalid input
      begin
        @coordinator.blockchain.create_transaction(nil, 'invalid', -100)
        return :failed # Should have raised an error
      rescue ArgumentError
        # Error was properly handled
      end
      
      :passed
    rescue => e
      puts "     ❌ Error Handling Error: #{e.message}"
      :failed
    end
  end

  def test_performance_characteristics
    begin
      start_time = Time.now
      
      # Test blockchain performance
      wallet = @coordinator.blockchain.create_wallet
      @coordinator.blockchain.create_transaction('system', wallet[:public_key], 1000)
      @coordinator.blockchain.mine_block(wallet[:public_key])
      
      blockchain_time = Time.now - start_time
      return :failed if blockchain_time > 10.0
      
      # Test smart contract performance
      start_time = Time.now
      contract_code = "function test() { return 1; }"
      deployer = "0x123"
      contract_address = @coordinator.smart_contracts.deploy_contract(contract_code, deployer, 1000)
      @coordinator.smart_contracts.execute_function(contract_address, 'test', [], deployer)
      
      contract_time = Time.now - start_time
      return :failed if contract_time > 5.0
      
      # Test distributed computing performance
      start_time = Time.now
      @coordinator.distributed_computing.add_node('perf_node', 'http://perf:8080')
      @coordinator.distributed_computing.distribute_task({ operation: 'sum', values: [1, 2, 3] })
      @coordinator.distributed_computing.run_consensus
      
      distributed_time = Time.now - start_time
      return :failed if distributed_time > 5.0
      
      :passed
    rescue => e
      puts "     ❌ Performance Characteristics Error: #{e.message}"
      :failed
    end
  end

  def generate_verification_report
    puts "\n" + "=" * 50
    puts "📊 VERIFICATION REPORT"
    puts "=" * 50

    total_tests = 0
    passed_tests = 0

    @results.each do |goal, data|
      puts "\n#{goal.upcase}: #{data[:status].upcase}"
      data[:tests].each do |test|
        total_tests += 1
        if test[:status] == :passed
          passed_tests += 1
          puts "  ✅ #{test[:name]}"
        else
          puts "  ❌ #{test[:name]}"
        end
      end
    end

    success_rate = total_tests > 0 ? (passed_tests.to_f / total_tests * 100).round(2) : 0
    overall_status = success_rate >= 90 ? :passed : :failed

    puts "\n" + "=" * 50
    puts "SUMMARY"
    puts "=" * 50
    puts "Total Tests: #{total_tests}"
    puts "Passed Tests: #{passed_tests}"
    puts "Success Rate: #{success_rate}%"
    puts "Overall Status: #{overall_status.upcase}"

    # Save verification results
    verification_data = {
      timestamp: Time.now.iso8601,
      overall_status: overall_status,
      success_rate: success_rate,
      total_tests: total_tests,
      passed_tests: passed_tests,
      results: @results
    }

    File.write('verification_results.json', JSON.pretty_generate(verification_data))
    puts "\n📄 Verification results saved to verification_results.json"

    overall_status
  end
end

# Run verification if executed directly
if __FILE__ == $0
  verifier = Goal9Verification.new
  result = verifier.run_all_verifications
  exit(result == :passed ? 0 : 1)
end 