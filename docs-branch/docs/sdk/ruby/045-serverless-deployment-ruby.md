# Serverless Deployment with TuskLang and Ruby

## ☁️ **Deploy to the Cloud Without Servers**

TuskLang enables seamless serverless deployment with Ruby, allowing you to run your applications without managing infrastructure. Deploy to AWS Lambda, Azure Functions, or Google Cloud Functions with minimal configuration and maximum performance.

## 🚀 **Quick Start: AWS Lambda**

### Basic Lambda Function

```ruby
# lambda_function.rb
require 'json'
require 'tusk'

def lambda_handler(event:, context:)
  # Load TuskLang configuration
  config = Tusk.load('config/lambda.tsk')
  
  # Process the event
  result = process_event(event, config)
  
  {
    statusCode: 200,
    headers: {
      'Content-Type' => 'application/json'
    },
    body: JSON.generate(result)
  }
rescue => e
  {
    statusCode: 500,
    headers: {
      'Content-Type' => 'application/json'
    },
    body: JSON.generate({ error: e.message })
  }
end

def process_event(event, config)
  case event['httpMethod']
  when 'GET'
    handle_get_request(event, config)
  when 'POST'
    handle_post_request(event, config)
  else
    { error: 'Unsupported method' }
  end
end

def handle_get_request(event, config)
  # Handle GET requests
  { message: 'Hello from Lambda!', config: config['lambda']['environment'] }
end

def handle_post_request(event, config)
  # Handle POST requests
  body = JSON.parse(event['body'])
  { received: body, processed: true }
end
```

### Lambda Configuration

```ruby
# config/lambda.tsk
[lambda]
function_name: @env("LAMBDA_FUNCTION_NAME", "tusklang-ruby-function")
runtime: @env("LAMBDA_RUNTIME", "ruby3.2")
timeout: @env("LAMBDA_TIMEOUT", "30")
memory_size: @env("LAMBDA_MEMORY_SIZE", "512")
environment: @env("LAMBDA_ENVIRONMENT", "production")

[api_gateway]
stage: @env("API_GATEWAY_STAGE", "prod")
cors_enabled: @env("API_GATEWAY_CORS_ENABLED", "true")
authorization: @env("API_GATEWAY_AUTHORIZATION", "none")

[database]
connection_string: @env.secure("DATABASE_CONNECTION_STRING")
pool_size: @env("DB_POOL_SIZE", "5")

[caching]
redis_url: @env.secure("REDIS_URL")
ttl: @env("CACHE_TTL", "300")
```

### Deployment with SAM

```yaml
# template.yaml
AWSTemplateFormatVersion: '2010-09-09'
Transform: AWS::Serverless-2016-10-31

Parameters:
  Environment:
    Type: String
    Default: production
    AllowedValues: [development, staging, production]

Globals:
  Function:
    Timeout: 30
    Runtime: ruby3.2
    Environment:
      Variables:
        LAMBDA_ENVIRONMENT: !Ref Environment
        LAMBDA_FUNCTION_NAME: !Ref AWS::StackName

Resources:
  TusklangFunction:
    Type: AWS::Serverless::Function
    Properties:
      CodeUri: ./
      Handler: lambda_function.lambda_handler
      MemorySize: 512
      Environment:
        Variables:
          DATABASE_CONNECTION_STRING: !Ref DatabaseConnectionString
          REDIS_URL: !Ref RedisUrl
      Events:
        ApiEvent:
          Type: Api
          Properties:
            Path: /{proxy+}
            Method: ANY
      Policies:
        - S3ReadPolicy:
            BucketName: !Ref ConfigBucket
        - DynamoDBCrudPolicy:
            TableName: !Ref DataTable

  ConfigBucket:
    Type: AWS::S3::Bucket
    Properties:
      BucketName: !Sub "${AWS::StackName}-config-${AWS::AccountId}"
      VersioningConfiguration:
        Status: Enabled

  DataTable:
    Type: AWS::DynamoDB::Table
    Properties:
      TableName: !Sub "${AWS::StackName}-data"
      AttributeDefinitions:
        - AttributeName: id
          AttributeType: S
      KeySchema:
        - AttributeName: id
          KeyType: HASH
      BillingMode: PAY_PER_REQUEST

Parameters:
  DatabaseConnectionString:
    Type: String
    NoEcho: true
  RedisUrl:
    Type: String
    NoEcho: true

Outputs:
  FunctionArn:
    Description: Lambda Function ARN
    Value: !GetAtt TusklangFunction.Arn
  ApiUrl:
    Description: API Gateway URL
    Value: !Sub "https://${ServerlessRestApi}.execute-api.${AWS::Region}.amazonaws.com/Prod/"
```

## 🔧 **Azure Functions Integration**

### Function Configuration

```ruby
# host.json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      }
    }
  },
  "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle",
    "version": "[3.*, 4.0.0)"
  }
}
```

### HTTP Trigger Function

```ruby
# HttpTrigger/function.json
{
  "bindings": [
    {
      "authLevel": "anonymous",
      "type": "httpTrigger",
      "direction": "in",
      "name": "req",
      "methods": [
        "get",
        "post"
      ],
      "route": "{*route}"
    },
    {
      "type": "http",
      "direction": "out",
      "name": "res"
    }
  ]
}
```

### Function Implementation

```ruby
# HttpTrigger/run.rb
require 'json'
require 'tusk'

def main(req, res)
  # Load TuskLang configuration
  config = Tusk.load('config/azure.tsk')
  
  # Parse request
  body = req.body.read
  query = req.query
  
  # Process request
  result = process_request(req.method, body, query, config)
  
  # Return response
  res.status = result[:status] || 200
  res.headers['Content-Type'] = 'application/json'
  res.body = result[:body].to_json
rescue => e
  res.status = 500
  res.headers['Content-Type'] = 'application/json'
  res.body = { error: e.message }.to_json
end

def process_request(method, body, query, config)
  case method.downcase
  when 'get'
    handle_get_request(query, config)
  when 'post'
    handle_post_request(body, config)
  else
    { status: 405, body: { error: 'Method not allowed' } }
  end
end

def handle_get_request(query, config)
  {
    body: {
      message: 'Hello from Azure Functions!',
      query: query,
      config: config['azure']['environment']
    }
  }
end

def handle_post_request(body, config)
  parsed_body = JSON.parse(body) rescue {}
  {
    body: {
      received: parsed_body,
      processed: true,
      timestamp: Time.now.iso8601
    }
  }
end
```

### Azure Configuration

```ruby
# config/azure.tsk
[azure]
function_name: @env("AZURE_FUNCTION_NAME", "tusklang-ruby-function")
environment: @env("AZURE_ENVIRONMENT", "production")
region: @env("AZURE_REGION", "eastus")

[storage]
connection_string: @env.secure("AZURE_STORAGE_CONNECTION_STRING")
container_name: @env("AZURE_STORAGE_CONTAINER", "tusklang-data")

[cosmos_db]
endpoint: @env.secure("COSMOS_DB_ENDPOINT")
key: @env.secure("COSMOS_DB_KEY")
database: @env("COSMOS_DB_DATABASE", "tusklang")
container: @env("COSMOS_DB_CONTAINER", "data")

[application_insights]
instrumentation_key: @env.secure("APP_INSIGHTS_INSTRUMENTATION_KEY")
```

## 🌐 **Google Cloud Functions**

### Function Configuration

```ruby
# main.rb
require 'functions_framework'
require 'json'
require 'tusk'

FunctionsFramework.http 'tusklang_function' do |request|
  # Load TuskLang configuration
  config = Tusk.load('config/gcp.tsk')
  
  # Process request
  result = process_http_request(request, config)
  
  # Return response
  [result[:status] || 200, 
   { 'Content-Type' => 'application/json' }, 
   result[:body].to_json]
rescue => e
  [500, 
   { 'Content-Type' => 'application/json' }, 
   { error: e.message }.to_json]
end

def process_http_request(request, config)
  case request.method
  when 'GET'
    handle_get_request(request, config)
  when 'POST'
    handle_post_request(request, config)
  else
    { status: 405, body: { error: 'Method not allowed' } }
  end
end

def handle_get_request(request, config)
  {
    body: {
      message: 'Hello from Google Cloud Functions!',
      query: request.query,
      config: config['gcp']['environment']
    }
  }
end

def handle_post_request(request, config)
  body = JSON.parse(request.body.read) rescue {}
  {
    body: {
      received: body,
      processed: true,
      timestamp: Time.now.iso8601
    }
  }
end
```

### GCP Configuration

```ruby
# config/gcp.tsk
[gcp]
function_name: @env("GCP_FUNCTION_NAME", "tusklang-ruby-function")
environment: @env("GCP_ENVIRONMENT", "production")
region: @env("GCP_REGION", "us-central1")
project_id: @env("GCP_PROJECT_ID")

[firestore]
project_id: @env("GCP_PROJECT_ID")
collection: @env("FIRESTORE_COLLECTION", "tusklang-data")

[cloud_storage]
bucket_name: @env("CLOUD_STORAGE_BUCKET", "tusklang-data")
```

### Deployment Configuration

```yaml
# .gcloudignore
.gcloudignore
.git
.gitignore
README.md
.env
*.log
```

```yaml
# cloudbuild.yaml
steps:
  - name: 'gcr.io/cloud-builders/docker'
    args: ['build', '-t', 'gcr.io/$PROJECT_ID/tusklang-function', '.']
  
  - name: 'gcr.io/cloud-builders/docker'
    args: ['push', 'gcr.io/$PROJECT_ID/tusklang-function']
  
  - name: 'gcr.io/google.com/cloudsdktool/cloud-sdk'
    entrypoint: gcloud
    args:
      - functions
      - deploy
      - tusklang-function
      - --runtime=ruby32
      - --trigger-http
      - --allow-unauthenticated
      - --region=us-central1
      - --image=gcr.io/$PROJECT_ID/tusklang-function

images:
  - 'gcr.io/$PROJECT_ID/tusklang-function'
```

## 🔄 **Event-Driven Functions**

### SQS Trigger (AWS)

```ruby
# sqs_trigger.rb
require 'json'
require 'tusk'

def lambda_handler(event:, context:)
  config = Tusk.load('config/lambda.tsk')
  
  results = []
  
  event['Records'].each do |record|
    begin
      result = process_sqs_message(record, config)
      results << { success: true, message_id: record['messageId'], result: result }
    rescue => e
      results << { success: false, message_id: record['messageId'], error: e.message }
    end
  end
  
  {
    statusCode: 200,
    body: JSON.generate({ processed: results.length, results: results })
  }
end

def process_sqs_message(record, config)
  body = JSON.parse(record['body'])
  
  case body['event_type']
  when 'user.created'
    handle_user_created(body['data'], config)
  when 'payment.processed'
    handle_payment_processed(body['data'], config)
  else
    Rails.logger.warn "Unknown event type: #{body['event_type']}"
  end
end

def handle_user_created(data, config)
  # Process user creation event
  Rails.logger.info "Processing user creation: #{data['user_id']}"
  { processed: true, user_id: data['user_id'] }
end

def handle_payment_processed(data, config)
  # Process payment event
  Rails.logger.info "Processing payment: #{data['payment_id']}"
  { processed: true, payment_id: data['payment_id'] }
end
```

### Event Grid Trigger (Azure)

```ruby
# EventGridTrigger/function.json
{
  "bindings": [
    {
      "type": "eventGridTrigger",
      "name": "eventGridEvent",
      "direction": "in"
    }
  ]
}
```

```ruby
# EventGridTrigger/run.rb
require 'json'
require 'tusk'

def main(eventGridEvent, context)
  config = Tusk.load('config/azure.tsk')
  
  begin
    result = process_event_grid_event(eventGridEvent, config)
    Rails.logger.info "Event processed successfully: #{result}"
  rescue => e
    Rails.logger.error "Error processing event: #{e.message}"
    raise e
  end
end

def process_event_grid_event(event, config)
  case event['eventType']
  when 'Microsoft.Storage.BlobCreated'
    handle_blob_created(event['data'], config)
  when 'Microsoft.Storage.BlobDeleted'
    handle_blob_deleted(event['data'], config)
  else
    Rails.logger.warn "Unknown event type: #{event['eventType']}"
  end
end

def handle_blob_created(data, config)
  Rails.logger.info "Blob created: #{data['url']}"
  { processed: true, blob_url: data['url'] }
end

def handle_blob_deleted(data, config)
  Rails.logger.info "Blob deleted: #{data['url']}"
  { processed: true, blob_url: data['url'] }
end
```

### Pub/Sub Trigger (GCP)

```ruby
# pubsub_trigger.rb
require 'functions_framework'
require 'json'
require 'tusk'

FunctionsFramework.cloud_event 'process_pubsub_message' do |event|
  config = Tusk.load('config/gcp.tsk')
  
  begin
    result = process_pubsub_message(event, config)
    Rails.logger.info "Message processed successfully: #{result}"
  rescue => e
    Rails.logger.error "Error processing message: #{e.message}"
    raise e
  end
end

def process_pubsub_message(event, config)
  data = JSON.parse(event.data) rescue {}
  
  case data['event_type']
  when 'user.created'
    handle_user_created(data['data'], config)
  when 'payment.processed'
    handle_payment_processed(data['data'], config)
  else
    Rails.logger.warn "Unknown event type: #{data['event_type']}"
  end
end

def handle_user_created(data, config)
  Rails.logger.info "Processing user creation: #{data['user_id']}"
  { processed: true, user_id: data['user_id'] }
end

def handle_payment_processed(data, config)
  Rails.logger.info "Processing payment: #{data['payment_id']}"
  { processed: true, payment_id: data['payment_id'] }
end
```

## 🗄️ **Database Integration**

### DynamoDB Integration (AWS)

```ruby
# lib/dynamodb_client.rb
require 'aws-sdk-dynamodb'
require 'tusk'

class DynamoDBClient
  def initialize(config_path = 'config/lambda.tsk')
    @config = Tusk.load(config_path)
    @client = Aws::DynamoDB::Client.new(
      region: @config['aws']['region'],
      credentials: Aws::Credentials.new(
        @config['aws']['access_key_id'],
        @config['aws']['secret_access_key']
      )
    )
    @table_name = @config['dynamodb']['table_name']
  end

  def get_item(key)
    response = @client.get_item(
      table_name: @table_name,
      key: key
    )
    response.item
  end

  def put_item(item)
    @client.put_item(
      table_name: @table_name,
      item: item
    )
  end

  def update_item(key, update_expression, expression_values)
    @client.update_item(
      table_name: @table_name,
      key: key,
      update_expression: update_expression,
      expression_attribute_values: expression_values
    )
  end

  def query(key_condition_expression, expression_values)
    response = @client.query(
      table_name: @table_name,
      key_condition_expression: key_condition_expression,
      expression_attribute_values: expression_values
    )
    response.items
  end

  def scan(filter_expression = nil, expression_values = nil)
    params = { table_name: @table_name }
    params[:filter_expression] = filter_expression if filter_expression
    params[:expression_attribute_values] = expression_values if expression_values
    
    response = @client.scan(params)
    response.items
  end
end
```

### Cosmos DB Integration (Azure)

```ruby
# lib/cosmos_db_client.rb
require 'azure_cosmos'
require 'tusk'

class CosmosDBClient
  def initialize(config_path = 'config/azure.tsk')
    @config = Tusk.load(config_path)
    @client = Azure::Cosmos::Client.new(
      endpoint: @config['cosmos_db']['endpoint'],
      key: @config['cosmos_db']['key']
    )
    @database = @client.database(@config['cosmos_db']['database'])
    @container = @database.container(@config['cosmos_db']['container'])
  end

  def get_item(id, partition_key)
    @container.read_item(id, partition_key)
  end

  def create_item(item)
    @container.create_item(item)
  end

  def replace_item(id, item, partition_key)
    @container.replace_item(id, item, partition_key)
  end

  def delete_item(id, partition_key)
    @container.delete_item(id, partition_key)
  end

  def query_items(query, parameters = [])
    @container.query_items(query, parameters)
  end
end
```

### Firestore Integration (GCP)

```ruby
# lib/firestore_client.rb
require 'google/cloud/firestore'
require 'tusk'

class FirestoreClient
  def initialize(config_path = 'config/gcp.tsk')
    @config = Tusk.load(config_path)
    @firestore = Google::Cloud::Firestore.new(
      project_id: @config['firestore']['project_id']
    )
    @collection = @firestore.col(@config['firestore']['collection'])
  end

  def get_document(id)
    doc = @collection.doc(id)
    doc.get
  end

  def create_document(data, id = nil)
    if id
      @collection.doc(id).set(data)
    else
      @collection.add(data)
    end
  end

  def update_document(id, data)
    @collection.doc(id).update(data)
  end

  def delete_document(id)
    @collection.doc(id).delete
  end

  def query_documents(field, operator, value)
    @collection.where(field, operator, value).get
  end
end
```

## 🔄 **State Management**

### Redis Integration

```ruby
# lib/redis_client.rb
require 'redis'
require 'json'
require 'tusk'

class RedisClient
  def initialize(config_path = 'config/lambda.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['redis']['url'])
  end

  def get(key)
    value = @redis.get(key)
    return nil unless value
    
    JSON.parse(value)
  rescue JSON::ParserError
    value
  end

  def set(key, value, ttl = nil)
    serialized_value = value.is_a?(String) ? value : value.to_json
    
    if ttl
      @redis.setex(key, ttl, serialized_value)
    else
      @redis.set(key, serialized_value)
    end
  end

  def delete(key)
    @redis.del(key)
  end

  def exists(key)
    @redis.exists(key)
  end

  def expire(key, ttl)
    @redis.expire(key, ttl)
  end

  def increment(key)
    @redis.incr(key)
  end

  def decrement(key)
    @redis.decr(key)
  end
end
```

## 🔐 **Security and Authentication**

### JWT Authentication

```ruby
# lib/jwt_auth.rb
require 'jwt'
require 'tusk'

class JWTAuth
  def initialize(config_path = 'config/lambda.tsk')
    @config = Tusk.load(config_path)
  end

  def generate_token(payload, expiration = 3600)
    payload[:exp] = Time.now.to_i + expiration
    payload[:iat] = Time.now.to_i
    
    JWT.encode(payload, @config['jwt']['secret'], 'HS256')
  end

  def verify_token(token)
    decoded = JWT.decode(token, @config['jwt']['secret'], true, { algorithm: 'HS256' })
    decoded[0]
  rescue JWT::DecodeError => e
    raise AuthenticationError, "Invalid token: #{e.message}"
  end

  def authenticate_request(event)
    auth_header = event['headers']['Authorization']
    return nil unless auth_header&.start_with?('Bearer ')
    
    token = auth_header.split(' ').last
    verify_token(token)
  end
end
```

### API Key Authentication

```ruby
# lib/api_key_auth.rb
require 'tusk'

class APIKeyAuth
  def initialize(config_path = 'config/lambda.tsk')
    @config = Tusk.load(config_path)
  end

  def authenticate_request(event)
    api_key = extract_api_key(event)
    return false unless api_key
    
    valid_keys = @config['api_keys']['valid_keys']
    valid_keys.include?(api_key)
  end

  private

  def extract_api_key(event)
    # Check headers first
    api_key = event['headers']['X-API-Key'] || event['headers']['x-api-key']
    return api_key if api_key
    
    # Check query parameters
    event['queryStringParameters']&.dig('api_key')
  end
end
```

## 📊 **Monitoring and Logging**

### CloudWatch Logging (AWS)

```ruby
# lib/cloudwatch_logger.rb
require 'aws-sdk-cloudwatchlogs'
require 'json'
require 'tusk'

class CloudWatchLogger
  def initialize(config_path = 'config/lambda.tsk')
    @config = Tusk.load(config_path)
    @client = Aws::CloudWatchLogs::Client.new(
      region: @config['aws']['region']
    )
    @log_group = @config['cloudwatch']['log_group']
    @log_stream = "#{Time.now.strftime('%Y/%m/%d')}/#{SecureRandom.uuid}"
  end

  def log(level, message, context = {})
    log_entry = {
      timestamp: Time.now.to_i,
      level: level,
      message: message,
      context: context
    }

    @client.put_log_events(
      log_group_name: @log_group,
      log_stream_name: @log_stream,
      log_events: [
        {
          timestamp: log_entry[:timestamp] * 1000,
          message: log_entry.to_json
        }
      ]
    )
  rescue => e
    # Fallback to standard logging if CloudWatch fails
    Rails.logger.error "CloudWatch logging failed: #{e.message}"
    Rails.logger.send(level.downcase, message)
  end

  def info(message, context = {})
    log('INFO', message, context)
  end

  def error(message, context = {})
    log('ERROR', message, context)
  end

  def warn(message, context = {})
    log('WARN', message, context)
  end

  def debug(message, context = {})
    log('DEBUG', message, context)
  end
end
```

### Application Insights (Azure)

```ruby
# lib/app_insights_logger.rb
require 'application_insights'
require 'tusk'

class AppInsightsLogger
  def initialize(config_path = 'config/azure.tsk')
    @config = Tusk.load(config_path)
    @client = ApplicationInsights::Client.new(
      instrumentation_key: @config['application_insights']['instrumentation_key']
    )
  end

  def log_event(name, properties = {})
    @client.track_event(name, properties)
  end

  def log_exception(exception, properties = {})
    @client.track_exception(exception, properties)
  end

  def log_metric(name, value, properties = {})
    @client.track_metric(name, value, properties)
  end

  def log_trace(message, severity = 'Info', properties = {})
    @client.track_trace(message, severity, properties)
  end
end
```

### Stackdriver Logging (GCP)

```ruby
# lib/stackdriver_logger.rb
require 'google/cloud/logging'
require 'tusk'

class StackdriverLogger
  def initialize(config_path = 'config/gcp.tsk')
    @config = Tusk.load(config_path)
    @logging = Google::Cloud::Logging.new(
      project_id: @config['gcp']['project_id']
    )
    @logger = @logging.logger(@config['stackdriver']['log_name'])
  end

  def log(severity, message, labels = {})
    entry = @logger.entry(
      severity: severity,
      payload: message,
      labels: labels
    )
    @logger.write(entry)
  end

  def info(message, labels = {})
    log('INFO', message, labels)
  end

  def error(message, labels = {})
    log('ERROR', message, labels)
  end

  def warn(message, labels = {})
    log('WARNING', message, labels)
  end

  def debug(message, labels = {})
    log('DEBUG', message, labels)
  end
end
```

## 🚀 **Performance Optimization**

### Cold Start Optimization

```ruby
# lib/cold_start_optimizer.rb
require 'tusk'

class ColdStartOptimizer
  def initialize(config_path = 'config/lambda.tsk')
    @config = Tusk.load(config_path)
    @initialized = false
  end

  def ensure_initialized
    return if @initialized
    
    # Pre-load frequently used configurations
    preload_configurations
    
    # Initialize database connections
    initialize_database_connections
    
    # Warm up caches
    warm_up_caches
    
    @initialized = true
  end

  private

  def preload_configurations
    # Load all configuration files at startup
    Tusk.load('config/lambda.tsk')
    Tusk.load('config/database.tsk')
    Tusk.load('config/cache.tsk')
  end

  def initialize_database_connections
    # Initialize connection pools
    ActiveRecord::Base.connection_pool.checkout
  end

  def warm_up_caches
    # Pre-populate frequently accessed cache entries
    redis = RedisClient.new
    redis.set('warmup', 'ready', 300)
  end
end
```

### Connection Pooling

```ruby
# lib/connection_pool_manager.rb
require 'connection_pool'
require 'redis'
require 'tusk'

class ConnectionPoolManager
  def initialize(config_path = 'config/lambda.tsk')
    @config = Tusk.load(config_path)
    @pools = {}
  end

  def redis_pool
    @pools[:redis] ||= ConnectionPool.new(size: 5, timeout: 5) do
      Redis.new(url: @config['redis']['url'])
    end
  end

  def database_pool
    @pools[:database] ||= ConnectionPool.new(size: 5, timeout: 5) do
      # Initialize database connection
      ActiveRecord::Base.connection
    end
  end

  def with_redis(&block)
    redis_pool.with(&block)
  end

  def with_database(&block)
    database_pool.with(&block)
  end
end
```

## 🧪 **Testing Serverless Functions**

### Unit Testing

```ruby
# spec/lambda_function_spec.rb
require 'rspec'
require 'json'
require_relative '../lambda_function'

RSpec.describe 'Lambda Function' do
  let(:config) { { 'lambda' => { 'environment' => 'test' } } }
  
  before do
    allow(Tusk).to receive(:load).and_return(config)
  end

  describe '#lambda_handler' do
    context 'with GET request' do
      let(:event) do
        {
          'httpMethod' => 'GET',
          'path' => '/test',
          'queryStringParameters' => { 'param' => 'value' }
        }
      end

      it 'returns successful response' do
        result = lambda_handler(event: event, context: {})
        
        expect(result[:statusCode]).to eq(200)
        expect(JSON.parse(result[:body])).to include('message')
      end
    end

    context 'with POST request' do
      let(:event) do
        {
          'httpMethod' => 'POST',
          'body' => '{"test": "data"}'
        }
      end

      it 'processes request body' do
        result = lambda_handler(event: event, context: {})
        
        expect(result[:statusCode]).to eq(200)
        body = JSON.parse(result[:body])
        expect(body['received']).to include('test' => 'data')
      end
    end

    context 'with error' do
      let(:event) { { 'invalid' => 'event' } }

      it 'returns error response' do
        result = lambda_handler(event: event, context: {})
        
        expect(result[:statusCode]).to eq(500)
        expect(JSON.parse(result[:body])).to include('error')
      end
    end
  end
end
```

### Integration Testing

```ruby
# spec/integration/serverless_integration_spec.rb
require 'rspec'
require 'aws-sdk-lambda'
require 'json'

RSpec.describe 'Serverless Integration', type: :integration do
  let(:lambda_client) { Aws::Lambda::Client.new(region: 'us-east-1') }
  let(:function_name) { ENV['LAMBDA_FUNCTION_NAME'] }

  describe 'function invocation' do
    it 'invokes function successfully' do
      payload = {
        httpMethod: 'GET',
        path: '/test'
      }

      response = lambda_client.invoke(
        function_name: function_name,
        payload: payload.to_json
      )

      expect(response.status_code).to eq(200)
      
      result = JSON.parse(response.payload.string)
      expect(result['statusCode']).to eq(200)
    end
  end

  describe 'error handling' do
    it 'handles invalid requests gracefully' do
      payload = {
        httpMethod: 'INVALID',
        path: '/test'
      }

      response = lambda_client.invoke(
        function_name: function_name,
        payload: payload.to_json
      )

      expect(response.status_code).to eq(200)
      
      result = JSON.parse(response.payload.string)
      expect(result['statusCode']).to eq(500)
    end
  end
end
```

## 🚀 **Deployment Automation**

### GitHub Actions Workflow

```yaml
# .github/workflows/serverless-deploy.yml
name: Deploy Serverless Function

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - name: Set up Ruby
      uses: ruby/setup-ruby@v1
      with:
        ruby-version: 3.2
    - name: Install dependencies
      run: bundle install
    - name: Run tests
      run: bundle exec rspec

  deploy-aws:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
    - uses: actions/checkout@v2
    - name: Configure AWS credentials
      uses: aws-actions/configure-aws-credentials@v1
      with:
        aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
        aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        aws-region: us-east-1
    - name: Deploy to AWS Lambda
      run: |
        sam build
        sam deploy --no-confirm-changeset --no-fail-on-empty-changeset

  deploy-azure:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
    - uses: actions/checkout@v2
    - name: Deploy to Azure Functions
      uses: azure/functions-action@v1
      with:
        app-name: tusklang-function
        package: '.'
        publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}

  deploy-gcp:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
    - uses: actions/checkout@v2
    - name: Set up Cloud SDK
      uses: google-github-actions/setup-gcloud@v0
      with:
        project_id: ${{ secrets.GCP_PROJECT_ID }}
        service_account_key: ${{ secrets.GCP_SA_KEY }}
    - name: Deploy to Google Cloud Functions
      run: |
        gcloud functions deploy tusklang-function \
          --runtime=ruby32 \
          --trigger-http \
          --allow-unauthenticated \
          --region=us-central1
```

## 🎯 **Best Practices**

### Configuration Management

```ruby
# config/serverless.tsk
[environment]
stage: @env("STAGE", "development")
region: @env("REGION", "us-east-1")

[performance]
timeout: @env("FUNCTION_TIMEOUT", "30")
memory: @env("FUNCTION_MEMORY", "512")
concurrency: @env("FUNCTION_CONCURRENCY", "100")

[monitoring]
log_level: @env("LOG_LEVEL", "INFO")
metrics_enabled: @env("METRICS_ENABLED", "true")
tracing_enabled: @env("TRACING_ENABLED", "true")

[security]
encryption_enabled: @env("ENCRYPTION_ENABLED", "true")
api_key_required: @env("API_KEY_REQUIRED", "true")
cors_enabled: @env("CORS_ENABLED", "true")
```

### Error Handling

```ruby
# lib/serverless_error_handler.rb
class ServerlessErrorHandler
  def self.handle_error(error, context = {})
    case error
    when AuthenticationError
      handle_authentication_error(error, context)
    when ValidationError
      handle_validation_error(error, context)
    when DatabaseError
      handle_database_error(error, context)
    when ExternalServiceError
      handle_external_service_error(error, context)
    else
      handle_unknown_error(error, context)
    end
  end

  private

  def self.handle_authentication_error(error, context)
    {
      statusCode: 401,
      headers: { 'Content-Type' => 'application/json' },
      body: { error: 'Authentication failed', details: error.message }
    }
  end

  def self.handle_validation_error(error, context)
    {
      statusCode: 400,
      headers: { 'Content-Type' => 'application/json' },
      body: { error: 'Validation failed', details: error.message }
    }
  end

  def self.handle_database_error(error, context)
    Rails.logger.error "Database error: #{error.message}"
    {
      statusCode: 500,
      headers: { 'Content-Type' => 'application/json' },
      body: { error: 'Database error occurred' }
    }
  end

  def self.handle_external_service_error(error, context)
    Rails.logger.error "External service error: #{error.message}"
    {
      statusCode: 502,
      headers: { 'Content-Type' => 'application/json' },
      body: { error: 'External service unavailable' }
    }
  end

  def self.handle_unknown_error(error, context)
    Rails.logger.error "Unknown error: #{error.message}"
    {
      statusCode: 500,
      headers: { 'Content-Type' => 'application/json' },
      body: { error: 'Internal server error' }
    }
  end
end
```

## 🎯 **Summary**

This comprehensive guide covers serverless deployment with TuskLang and Ruby, including:

- **AWS Lambda**: Function development, SAM deployment, and event handling
- **Azure Functions**: HTTP triggers, Event Grid integration, and configuration
- **Google Cloud Functions**: Pub/Sub triggers, deployment automation, and monitoring
- **Database Integration**: DynamoDB, Cosmos DB, and Firestore clients
- **State Management**: Redis integration for caching and session management
- **Security**: JWT authentication and API key validation
- **Monitoring**: CloudWatch, Application Insights, and Stackdriver logging
- **Performance**: Cold start optimization and connection pooling
- **Testing**: Unit and integration testing strategies
- **Deployment**: Automated CI/CD pipelines for all platforms

The serverless architecture with TuskLang provides a scalable, cost-effective solution for running Ruby applications without managing infrastructure, while maintaining the flexibility and power of TuskLang's configuration system. 