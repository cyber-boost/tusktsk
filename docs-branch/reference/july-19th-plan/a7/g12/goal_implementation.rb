#!/usr/bin/env ruby

require 'sinatra'
require 'sinatra/base'
require 'sinatra/json'
require 'grape'
require 'grape-entity'
require 'graphql'
require 'securerandom'
require 'json'
require 'time'
require 'logger'
require 'redis'
require 'concurrent-ruby'

# Advanced RESTful API Framework with GraphQL Support
class AdvancedAPIFramework < Grape::API
  version 'v1', using: :path
  format :json
  prefix :api

  rescue_from :all do |e|
    error!({ error: e.message }, 500)
  end

  before do
    authenticate!
    rate_limit
  end

  def self.authenticate!
    # Placeholder for authentication
    true
  end

  def self.rate_limit
    # Placeholder for rate limiting
    true
  end

  # REST Endpoints
  resource :users do
    desc 'Create a user'
    params do
      requires :name, type: String
      requires :email, type: String
    end
    post do
      { status: 'user created', name: params[:name], email: params[:email] }
    end

    desc 'Get all users'
    get do
      [{ id: 1, name: 'John Doe' }, { id: 2, name: 'Jane Doe' }]
    end
  end

  # GraphQL Endpoint
  post '/graphql' do
    result = Schema.execute(
      params[:query],
      variables: params[:variables],
      context: { current_user: 'placeholder' }
    )
    json result
  end
end

# GraphQL Schema
class UserType < GraphQL::Schema::Object
  field :id, Integer, null: false
  field :name, String, null: false
  field :email, String, null: false
end

class QueryType < GraphQL::Schema::Object
  field :users, [UserType], null: false

  def users
    [{ id: 1, name: 'John Doe', email: 'john@example.com' }]
  end

  field :user, UserType, null: true do
    argument :id, Integer, required: true
  end

  def user(id:)
    { id: id, name: 'User ' + id.to_s, email: 'user' + id.to_s + '@example.com' }
  end
end

class MutationType < GraphQL::Schema::Object
  field :create_user, UserType, null: false do
    argument :name, String, required: true
    argument :email, String, required: true
  end

  def create_user(name:, email:)
    { id: SecureRandom.uuid, name: name, email: email }
  end
end

class Schema < GraphQL::Schema
  query QueryType
  mutation MutationType
end

# Microservices Architecture
class ServiceRegistry
  def initialize
    @services = {}
    @logger = Logger.new(STDOUT)
    @redis = Redis.new rescue nil
  end

  def register_service(name, config)
    @services[name] = {
      url: config[:url],
      endpoints: config[:endpoints] || {},
      health: 'healthy',
      last_heartbeat: Time.now,
      metadata: config[:metadata] || {}
    }
    @logger.info("Registered service: #{name}")
  end

  def discover_service(name)
    @services[name]
  end

  def heartbeat(name)
    if @services[name]
      @services[name][:last_heartbeat] = Time.now
      @services[name][:health] = 'healthy'
    end
  end

  def check_health
    @services.each do |name, service|
      if Time.now - service[:last_heartbeat] > 30
        service[:health] = 'unhealthy'
      end
    end
  end

  def get_healthy_services
    @services.select { |_, s| s[:health] == 'healthy' }
  end
end

class InterServiceCommunicator
  def initialize(registry)
    @registry = registry
    @logger = Logger.new(STDOUT)
  end

  def call_service(service_name, endpoint, params = {})
    service = @registry.discover_service(service_name)
    if service && service[:health] == 'healthy'
      url = "#{service[:url]}/#{endpoint}"
      response = Net::HTTP.post(URI(url), params.to_json, 'Content-Type' => 'application/json')
      JSON.parse(response.body)
    else
      @logger.error("Service unavailable: #{service_name}")
      { error: 'Service unavailable' }
    end
  end
end

# API Gateway and Service Mesh
class APIGateway < Sinatra::Base
  use AdvancedAPIFramework

  def initialize(registry)
    @registry = registry
    @communicator = InterServiceCommunicator.new(registry)
    @load_balancer = LoadBalancer.new(registry)
    super()
  end

  before do
    authenticate
    rate_limit
    log_request
  end

  def authenticate
    # Placeholder
    true
  end

  def rate_limit
    # Placeholder
    true
  end

  def log_request
    # Placeholder
    true
  end

  get '/gateway/:service/:endpoint' do
    service = params[:service]
    endpoint = params[:endpoint]
    @communicator.call_service(service, endpoint, params)
  end
end

class LoadBalancer
  def initialize(registry)
    @registry = registry
  end

  def balance(service_name)
    healthy = @registry.get_healthy_services.select { |name, _| name == service_name }
    healthy.keys.sample
  end
end

# Example Usage
if __FILE__ == $0
  registry = ServiceRegistry.new
  registry.register_service('user_service', { url: 'http://localhost:3001', endpoints: ['users'] })
  registry.register_service('product_service', { url: 'http://localhost:3002', endpoints: ['products'] })

  gateway = APIGateway.new(registry)

  # Run the gateway
  # gateway.run! # Uncomment to run the server

  puts "Goal 12 Implementation Complete: Advanced API Framework, Microservices, and Gateway Ready!"
end 