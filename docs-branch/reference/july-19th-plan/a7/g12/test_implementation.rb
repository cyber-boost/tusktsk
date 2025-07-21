#!/usr/bin/env ruby

require 'test/unit'
require 'rack/test'
require_relative 'goal_implementation'

class TestAdvancedAPIFramework < Test::Unit::TestCase
  include Rack::Test::Methods

  def app
    AdvancedAPIFramework
  end

  def test_post_user
    post '/api/v1/users', { name: 'John', email: 'john@example.com' }.to_json, 'CONTENT_TYPE' => 'application/json'
    assert_equal 201, last_response.status
    assert_equal 'user created', JSON.parse(last_response.body)['status']
  end

  def test_get_users
    get '/api/v1/users'
    assert_equal 200, last_response.status
    assert_equal 2, JSON.parse(last_response.body).length
  end

  def test_graphql_query
    query = <<~GRAPHQL
      query {
        users {
          id
          name
        }
      }
    GRAPHQL
    post '/graphql', { query: query }.to_json, 'CONTENT_TYPE' => 'application/json'
    assert_equal 200, last_response.status
    assert JSON.parse(last_response.body)['data']['users'].any?
  end

  def test_graphql_mutation
    query = <<~GRAPHQL
      mutation {
        createUser(name: "Test", email: "test@example.com") {
          id
          name
          email
        }
      }
    GRAPHQL
    post '/graphql', { query: query }.to_json, 'CONTENT_TYPE' => 'application/json'
    assert_equal 200, last_response.status
    assert JSON.parse(last_response.body)['data']['createUser']['id']
  end
end

class TestServiceRegistry < Test::Unit::TestCase
  def setup
    @registry = ServiceRegistry.new
  end

  def test_register_service
    @registry.register_service('test_service', { url: 'http://localhost:3000' })
    assert_not_nil @registry.discover_service('test_service')
    assert_equal 'http://localhost:3000', @registry.discover_service('test_service')[:url]
  end

  def test_heartbeat
    @registry.register_service('test_service', { url: 'http://localhost:3000' })
    @registry.heartbeat('test_service')
    assert_equal 'healthy', @registry.discover_service('test_service')[:health]
  end

  def test_check_health
    @registry.register_service('stale_service', { url: 'http://localhost:3000' })
    @registry.services['stale_service'][:last_heartbeat] = Time.now - 60
    @registry.check_health
    assert_equal 'unhealthy', @registry.discover_service('stale_service')[:health]
  end

  def test_get_healthy_services
    @registry.register_service('healthy1', { url: 'http://localhost:1' })
    @registry.register_service('healthy2', { url: 'http://localhost:2' })
    assert_equal 2, @registry.get_healthy_services.length
  end
end

class TestInterServiceCommunicator < Test::Unit::TestCase
  def setup
    @registry = ServiceRegistry.new
    @communicator = InterServiceCommunicator.new(@registry)
    @registry.register_service('test_service', { url: 'http://localhost:3000', health: 'healthy' })
  end

  def test_call_service
    # Mock Net::HTTP
    def Net::HTTP.post(uri, data, headers)
      OpenStruct.new(body: { success: true }.to_json)
    end
    result = @communicator.call_service('test_service', 'endpoint', { param: 'value' })
    assert_equal true, result['success']
  end

  def test_call_unavailable_service
    @registry.services['test_service'][:health] = 'unhealthy'
    result = @communicator.call_service('test_service', 'endpoint')
    assert_equal 'Service unavailable', result[:error]
  end
end

class TestAPIGateway < Test::Unit::TestCase
  include Rack::Test::Methods

  def app
    registry = ServiceRegistry.new
    APIGateway.new(registry)
  end

  def test_gateway_routing
    # Would need to mock communicator
    get '/gateway/test/endpoint'
    assert_equal 200, last_response.status # Assuming default behavior
  end
end

class TestLoadBalancer < Test::Unit::TestCase
  def setup
    @registry = ServiceRegistry.new
    @load_balancer = LoadBalancer.new(@registry)
    @registry.register_service('service1', { url: 'http://1', health: 'healthy' })
    @registry.register_service('service2', { url: 'http://2', health: 'healthy' })
  end

  def test_balance
    balanced = @load_balancer.balance('service1')
    assert ['service1', 'service2'].include?(balanced) # Since only one, but to test logic
  end
end

if __FILE__ == $0
  puts "Running Goal 12 Tests..."
end 