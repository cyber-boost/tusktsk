#!/usr/bin/env ruby
require_relative 'goal_implementation'
require 'test/unit'

class TestBlockchainFramework < Test::Unit::TestCase
  def setup
    @blockchain = BlockchainFramework.new
  end

  def test_block_creation
    block = Block.new(1, "Test transaction", "previous_hash_123")
    
    assert_equal 1, block.index
    assert_equal "Test transaction", block.data
    assert_equal "previous_hash_123", block.previous_hash
    assert_instance_of Float, block.timestamp
    assert_equal 0, block.nonce
    assert_instance_of String, block.hash
    assert_equal 64, block.hash.length # SHA256 hex length
  end

  def test_block_hash_calculation
    block = Block.new(0, "Genesis", "0")
    hash1 = block.calculate_hash
    hash2 = block.calculate_hash
    
    assert_equal hash1, hash2, "Hash should be deterministic"
    assert_equal block.hash, hash1
  end

  def test_block_mining
    block = Block.new(1, "Test", "prev")
    original_nonce = block.nonce
    
    block.mine_block(2) # Difficulty 2
    
    assert block.nonce > original_nonce, "Nonce should increase during mining"
    assert block.hash.start_with?("00"), "Hash should start with 00 for difficulty 2"
  end

  def test_blockchain_initialization
    chain = Blockchain.new
    
    assert_equal 1, chain.instance_variable_get(:@chain).length
    assert_equal "Genesis Block", chain.instance_variable_get(:@chain)[0].data
    assert_equal 2, chain.instance_variable_get(:@difficulty)
    assert_equal 100, chain.instance_variable_get(:@mining_reward)
    assert_empty chain.instance_variable_get(:@pending_transactions)
  end

  def test_blockchain_add_transaction
    chain = Blockchain.new
    transaction = {from: 'alice', to: 'bob', amount: 50}
    
    chain.add_transaction(transaction)
    pending = chain.instance_variable_get(:@pending_transactions)
    
    assert_equal 1, pending.length
    assert_equal transaction, pending[0]
  end

  def test_blockchain_mining
    chain = Blockchain.new
    chain.add_transaction({from: 'alice', to: 'bob', amount: 30})
    chain.add_transaction({from: 'bob', to: 'charlie', amount: 20})
    
    initial_length = chain.instance_variable_get(:@chain).length
    chain.mine_pending_transactions('miner1')
    
    assert_equal initial_length + 1, chain.instance_variable_get(:@chain).length
    assert_empty chain.instance_variable_get(:@pending_transactions)
    
    # Check mining reward was added
    latest_block = chain.get_latest_block
    assert_equal 3, latest_block.data.length # 2 transactions + 1 reward
    
    reward_tx = latest_block.data.find { |tx| tx[:from].nil? }
    assert_not_nil reward_tx
    assert_equal 'miner1', reward_tx[:to]
    assert_equal 100, reward_tx[:amount]
  end

  def test_blockchain_balance_calculation
    chain = Blockchain.new
    
    # Add some transactions
    chain.add_transaction({from: nil, to: 'alice', amount: 100}) # Initial funding
    chain.add_transaction({from: 'alice', to: 'bob', amount: 30})
    chain.add_transaction({from: 'bob', to: 'alice', amount: 10})
    
    chain.mine_pending_transactions('miner1')
    
    alice_balance = chain.get_balance('alice')
    bob_balance = chain.get_balance('bob')
    miner_balance = chain.get_balance('miner1')
    
    assert_equal 80, alice_balance # 100 - 30 + 10
    assert_equal 20, bob_balance   # 30 - 10
    assert_equal 100, miner_balance # Mining reward
  end

  def test_blockchain_validation
    chain = Blockchain.new
    chain.add_transaction({from: 'alice', to: 'bob', amount: 50})
    chain.mine_pending_transactions('miner1')
    
    assert chain.is_chain_valid?, "Valid chain should return true"
    
    # Tamper with a block
    chain.instance_variable_get(:@chain)[1].data = "tampered"
    
    refute chain.is_chain_valid?, "Tampered chain should return false"
  end

  def test_cryptocurrency_initialization
    crypto = Cryptocurrency.new("TestCoin", "TST")
    
    assert_equal "TestCoin", crypto.instance_variable_get(:@name)
    assert_equal "TST", crypto.instance_variable_get(:@symbol)
    assert_instance_of Blockchain, crypto.instance_variable_get(:@blockchain)
    assert_empty crypto.instance_variable_get(:@wallets)
  end

  def test_cryptocurrency_wallet_creation
    crypto = Cryptocurrency.new("TestCoin", "TST")
    
    wallet = crypto.create_wallet('user1')
    wallets = crypto.instance_variable_get(:@wallets)
    
    assert_not_nil wallet
    assert_equal 'user1', wallet[:address]
    assert_equal 0, wallet[:balance]
    assert_instance_of String, wallet[:private_key]
    assert_equal wallet, wallets['user1']
  end

  def test_cryptocurrency_transfer
    crypto = Cryptocurrency.new("TestCoin", "TST")
    crypto.create_wallet('alice')
    crypto.create_wallet('bob')
    
    # Give alice some initial coins
    blockchain = crypto.instance_variable_get(:@blockchain)
    blockchain.add_transaction({from: nil, to: 'alice', amount: 100})
    blockchain.mine_pending_transactions('system')
    
    # Test transfer
    result = crypto.transfer('alice', 'bob', 30)
    assert result, "Transfer should succeed"
    
    # Test insufficient balance
    result = crypto.transfer('alice', 'bob', 200)
    refute result, "Transfer should fail with insufficient balance"
  end

  def test_smart_contract_initialization
    contract = SmartContract.new("contract code", "0x123")
    
    assert_equal "contract code", contract.instance_variable_get(:@code)
    assert_equal "0x123", contract.instance_variable_get(:@address)
    assert_empty contract.instance_variable_get(:@state)
    assert_equal 1000000, contract.instance_variable_get(:@gas_limit)
  end

  def test_smart_contract_token_operations
    contract = SmartContract.new("token contract", "0x456")
    
    # Test minting
    result = contract.execute('mint', {to: 'alice', amount: 100})
    assert result[:success], "Minting should succeed"
    assert_equal 100, contract.instance_variable_get(:@state)['alice']
    
    # Test balance check
    result = contract.execute('get_balance', {address: 'alice'})
    assert_equal 100, result[:balance]
    
    # Test transfer
    contract.execute('mint', {to: 'bob', amount: 50}) # Give bob some tokens first
    result = contract.execute('transfer', {from: 'alice', to: 'bob', amount: 30})
    assert result[:success], "Transfer should succeed"
    assert_equal 70, contract.instance_variable_get(:@state)['alice']
    assert_equal 80, contract.instance_variable_get(:@state)['bob']
  end

  def test_smart_contract_insufficient_balance
    contract = SmartContract.new("token contract", "0x789")
    
    result = contract.execute('transfer', {from: 'alice', to: 'bob', amount: 100})
    assert result.key?(:error), "Should return error for insufficient balance"
    assert_includes result[:error], "Insufficient balance"
  end

  def test_smart_contract_gas_limit
    contract = SmartContract.new("expensive contract", "0xabc")
    
    result = contract.execute('mint', {to: 'alice', amount: 100}, 2000000)
    assert result.key?(:error), "Should return error for exceeding gas limit"
    assert_includes result[:error], "Insufficient gas"
  end

  def test_smart_contract_unknown_method
    contract = SmartContract.new("simple contract", "0xdef")
    
    result = contract.execute('unknown_method', {})
    assert result.key?(:error), "Should return error for unknown method"
    assert_includes result[:error], "Unknown method"
  end

  def test_blockchain_framework_integration
    assert_instance_of Blockchain, @blockchain.blockchain
    assert_instance_of Cryptocurrency, @blockchain.cryptocurrency
    assert_empty @blockchain.contracts
  end

  def test_contract_deployment
    code = "def transfer(from, to, amount); end"
    result = @blockchain.deploy_contract('token', code)
    
    contracts = @blockchain.contracts
    assert_not_nil contracts['token']
    assert_instance_of SmartContract, contracts['token']
  end

  def test_contract_execution
    @blockchain.deploy_contract('token', 'token contract code')
    
    result = @blockchain.execute_contract('token', 'mint', {to: 'alice', amount: 50})
    assert result[:success], "Contract execution should succeed"
    
    result = @blockchain.execute_contract('nonexistent', 'mint', {})
    assert result.key?(:error), "Should return error for nonexistent contract"
  end

  def test_full_transaction_flow
    @blockchain.cryptocurrency.create_wallet('alice')
    @blockchain.cryptocurrency.create_wallet('bob')
    
    # Give alice initial balance through mining
    @blockchain.cryptocurrency.mine_block('alice')
    
    # Perform full transaction
    result = @blockchain.full_transaction('alice', 'bob', 25)
    
    # Should not raise errors
    assert_nothing_raised do
      @blockchain.full_transaction('alice', 'bob', 10)
    end
  end

  def test_multiple_blocks_chain_integrity
    chain = Blockchain.new
    
    # Add multiple blocks
    5.times do |i|
      chain.add_transaction({from: 'user1', to: 'user2', amount: 10 + i})
      chain.mine_pending_transactions("miner#{i}")
    end
    
    assert_equal 6, chain.instance_variable_get(:@chain).length # Genesis + 5 blocks
    assert chain.is_chain_valid?, "Multi-block chain should be valid"
    
    # Verify chain links
    blocks = chain.instance_variable_get(:@chain)
    (1...blocks.length).each do |i|
      assert_equal blocks[i-1].hash, blocks[i].previous_hash
    end
  end

  def test_concurrent_mining_simulation
    chain = Blockchain.new
    
    # Simulate concurrent transactions
    threads = []
    10.times do |i|
      threads << Thread.new do
        chain.add_transaction({from: "user#{i}", to: "user#{i+1}", amount: i+1})
      end
    end
    
    threads.each(&:join)
    
    chain.mine_pending_transactions('miner')
    
    latest_block = chain.get_latest_block
    assert_equal 11, latest_block.data.length # 10 transactions + 1 reward
  end
end

if __FILE__ == $0
  puts "🔥 RUNNING G18 PRODUCTION TESTS..."
  Test::Unit::AutoRunner.run
end 