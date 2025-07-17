# ☁️ TuskLang Ruby Serverless Guide

**"We don't bow to any king" - Ruby Edition**

Deploy TuskLang-powered Ruby functions to serverless platforms. Learn AWS Lambda, Google Cloud Functions, and Azure Functions integration.

## 🚀 AWS Lambda

### 1. Lambda Function with TuskLang
```ruby
# lambda_function.rb
require 'json'
require 'tusklang'

def lambda_handler(event:, context:)
  parser = TuskLang.new
  config = parser.parse_file('config/lambda.tsk')
  
  {
    statusCode: 200,
    body: JSON.generate({
      message: "Hello from TuskLang Lambda!",
      config: config
    })
  }
end
```

### 2. Lambda Config
```ruby
# config/lambda.tsk
[lambda]
function_name: "tusklang-ruby-function"
runtime: "ruby3.2"
timeout: 30
memory_size: 512

[environment]
stage: @env("STAGE", "dev")
region: @env("AWS_REGION", "us-east-1")
```

### 3. Deployment with SAM
```yaml
# template.yaml
AWSTemplateFormatVersion: '2010-09-09'
Transform: AWS::Serverless-2016-10-31

Resources:
  TusklangFunction:
    Type: AWS::Serverless::Function
    Properties:
      CodeUri: ./
      Handler: lambda_function.lambda_handler
      Runtime: ruby3.2
      Timeout: 30
      MemorySize: 512
      Environment:
        Variables:
          STAGE: prod
```

## 🌩️ Google Cloud Functions

### 1. Cloud Function with TuskLang
```ruby
# main.rb
require 'functions_framework'
require 'tusklang'

FunctionsFramework.http 'tusklang_function' do |request|
  parser = TuskLang.new
  config = parser.parse_file('config/cloud_function.tsk')
  
  {
    message: "Hello from TuskLang Cloud Function!",
    config: config
  }.to_json
end
```

### 2. Cloud Function Config
```ruby
# config/cloud_function.tsk
[cloud_function]
function_name: "tusklang-ruby-function"
runtime: "ruby32"
timeout: 30
memory: "512Mi"

[environment]
project_id: @env("GOOGLE_CLOUD_PROJECT")
region: @env("FUNCTION_REGION", "us-central1")
```

## ☁️ Azure Functions

### 1. Azure Function with TuskLang
```ruby
# function.rb
require 'azure_functions'

class TusklangFunction
  include Azure::Functions

  def self.run(req, context)
    parser = TuskLang.new
    config = parser.parse_file('config/azure_function.tsk')
    
    {
      message: "Hello from TuskLang Azure Function!",
      config: config
    }.to_json
  end
end
```

### 2. Azure Function Config
```ruby
# config/azure_function.tsk
[azure_function]
function_name: "tusklang-ruby-function"
runtime: "ruby"
timeout: 30
memory: 512

[environment]
function_app_name: @env("FUNCTION_APP_NAME")
region: @env("AZURE_REGION", "East US")
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/serverless_config.rb
require 'tusklang'

class ServerlessConfig
  def self.load_config(platform)
    parser = TuskLang.new
    parser.parse_file("config/#{platform}.tsk")
  end
end

# Usage
lambda_config = ServerlessConfig.load_config('lambda')
cloud_config = ServerlessConfig.load_config('cloud_function')
azure_config = ServerlessConfig.load_config('azure_function')
```

## 🛡️ Best Practices
- Keep configs small for serverless cold starts.
- Use environment variables for platform-specific settings.
- Monitor function performance and costs.
- Test functions locally before deploying.

**Ready to go serverless? Let's Tusk! 🚀** 