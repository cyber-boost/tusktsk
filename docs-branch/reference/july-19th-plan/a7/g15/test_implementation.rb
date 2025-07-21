#!/usr/bin/env ruby
require_relative 'goal_implementation'
require 'test/unit'

class TestNetworkFramework < Test::Unit::TestCase
  def setup
    @framework = UnifiedNetworkSystem.new
  end

  def test_network_framework_initialization
    assert_not_nil @framework.network
    assert_instance_of NetworkFramework, @framework.network
  end

  def test_http2_client_connection
    client = @framework.network.http2_client('example.com', 443)
    assert_not_nil client
  end

  def test_websocket_connection
    ws = @framework.network.websocket_connect('example.com/ws')
    assert_equal 'ws://example.com/ws', ws
  end

  def test_grpc_client
    grpc = @framework.network.grpc_client('user-service')
    assert_equal 'grpc://user-service', grpc
  end

  def test_connection_pool
    pool = @framework.network.connection_pool('test-pool', 5)
    assert_equal 5, pool.length
    assert_equal 'conn_0', pool[0]
    assert_equal 'conn_4', pool[4]
  end

  def test_real_time_messaging
    topic_received = nil
    message_received = nil
    
    @framework.messaging.subscribe('test-topic') do |msg|
      topic_received = 'test-topic'
      message_received = msg
    end
    
    @framework.messaging.publish('test-topic', 'Hello World')
    assert_equal 'Hello World', message_received
  end

  def test_message_queue_operations
    @framework.messaging.create_queue('test-queue')
    @framework.messaging.enqueue('test-queue', 'message1')
    @framework.messaging.enqueue('test-queue', 'message2')
    
    first_msg = @framework.messaging.dequeue('test-queue')
    second_msg = @framework.messaging.dequeue('test-queue')
    
    assert_equal 'message1', first_msg
    assert_equal 'message2', second_msg
  end

  def test_streaming
    @framework.messaging.stream('data-stream', [1, 2, 3, 4, 5])
    streams = @framework.messaging.instance_variable_get(:@streams)
    assert_equal [1, 2, 3, 4, 5], streams['data-stream']
  end

  def test_ssl_context_creation
    ctx = @framework.security.create_ssl_context
    assert_instance_of OpenSSL::SSL::SSLContext, ctx
    assert_equal OpenSSL::SSL::VERIFY_NONE, ctx.verify_mode
  end

  def test_certificate_generation
    @framework.security.generate_certificate('test-cert')
    certificates = @framework.security.instance_variable_get(:@certificates)
    keys = @framework.security.instance_variable_get(:@keys)
    
    assert_not_nil certificates['test-cert']
    assert_not_nil keys['test-cert']
    assert_instance_of OpenSSL::X509::Certificate, certificates['test-cert']
    assert_instance_of OpenSSL::PKey::RSA, keys['test-cert']
  end

  def test_message_encryption
    encrypted = @framework.security.encrypt_message('secret message', 'key123')
    assert_instance_of String, encrypted
    assert_equal 64, encrypted.length # SHA256 hex length
    
    # Same message + key should produce same hash
    encrypted2 = @framework.security.encrypt_message('secret message', 'key123')
    assert_equal encrypted, encrypted2
  end

  def test_secure_publish
    received_message = nil
    @framework.messaging.subscribe('secure-topic') do |msg|
      received_message = msg
    end
    
    @framework.secure_publish('secure-topic', 'confidential data')
    assert_not_nil received_message
    assert_not_equal 'confidential data', received_message # Should be encrypted
    assert_instance_of String, received_message
  end

  def test_multiple_subscribers
    messages = []
    
    @framework.messaging.subscribe('multi-topic') { |msg| messages << "sub1: #{msg}" }
    @framework.messaging.subscribe('multi-topic') { |msg| messages << "sub2: #{msg}" }
    
    @framework.messaging.publish('multi-topic', 'broadcast')
    
    assert_equal 2, messages.length
    assert_includes messages, 'sub1: broadcast'
    assert_includes messages, 'sub2: broadcast'
  end

  def test_queue_empty_dequeue
    @framework.messaging.create_queue('empty-queue')
    result = @framework.messaging.dequeue('empty-queue')
    assert_nil result
  end
end

if __FILE__ == $0
  puts "🔥 RUNNING G15 PRODUCTION TESTS..."
  Test::Unit::AutoRunner.run
end 