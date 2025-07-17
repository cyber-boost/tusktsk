# 🔒 Advanced Encryption with TuskLang Ruby

**"We don't bow to any king" - Ruby Edition**

Implement enterprise-grade encryption with TuskLang's advanced encryption features. From data-at-rest encryption to secure key management, TuskLang provides the security and flexibility you need to protect sensitive data in your Ruby applications.

## 🚀 Quick Start

### Basic Encryption Setup
```ruby
require 'tusklang'
require 'tusklang/encryption'

# Initialize encryption system
encryption_system = TuskLang::Encryption::System.new

# Configure encryption
encryption_system.configure do |config|
  config.default_algorithm = 'AES-256-GCM'
  config.key_rotation_enabled = true
  config.key_rotation_interval = 30.days
  config.master_key_source = 'aws-kms'
end

# Register encryption strategies
encryption_system.register_strategy(:aes, TuskLang::Encryption::Strategies::AESStrategy.new)
encryption_system.register_strategy(:rsa, TuskLang::Encryption::Strategies::RSAStrategy.new)
encryption_system.register_strategy(:chacha20, TuskLang::Encryption::Strategies::ChaCha20Strategy.new)
```

### TuskLang Configuration
```tsk
# config/encryption.tsk
[encryption]
enabled: true
default_algorithm: "AES-256-GCM"
key_rotation_enabled: true
key_rotation_interval: "30d"
master_key_source: "aws-kms"

[encryption.keys]
master_key_id: @env("MASTER_KEY_ID", "alias/myapp-master-key")
data_key_id: @env("DATA_KEY_ID", "alias/myapp-data-key")
backup_key_id: @env("BACKUP_KEY_ID", "alias/myapp-backup-key")

[encryption.algorithms]
aes_256_gcm: {
    key_size: 256,
    iv_size: 12,
    tag_size: 16,
    mode: "GCM"
}
aes_256_cbc: {
    key_size: 256,
    iv_size: 16,
    mode: "CBC"
}
chacha20_poly1305: {
    key_size: 256,
    nonce_size: 12,
    tag_size: 16,
    mode: "ChaCha20-Poly1305"
}

[encryption.key_management]
aws_kms: {
    region: @env("AWS_REGION", "us-east-1"),
    access_key_id: @env("AWS_ACCESS_KEY_ID"),
    secret_access_key: @env("AWS_SECRET_ACCESS_KEY")
}
vault: {
    url: @env("VAULT_URL", "http://localhost:8200"),
    token: @env("VAULT_TOKEN"),
    mount_path: "transit"
}
```

## 🎯 Core Features

### 1. AES Encryption Strategy
```ruby
require 'tusklang/encryption'
require 'openssl'

class AESStrategy
  include TuskLang::Encryption::Strategy
  
  def initialize(algorithm: 'AES-256-GCM')
    @algorithm = algorithm
    @config = TuskLang.parse_file('config/encryption.tsk')
    @key_manager = KeyManager.new
  end
  
  def encrypt(data, context = {})
    # Generate or retrieve encryption key
    key = @key_manager.get_encryption_key(context)
    
    case @algorithm
    when 'AES-256-GCM'
      encrypt_aes_gcm(data, key)
    when 'AES-256-CBC'
      encrypt_aes_cbc(data, key)
    else
      raise "Unsupported AES algorithm: #{@algorithm}"
    end
  end
  
  def decrypt(encrypted_data, context = {})
    # Parse encrypted data
    parsed_data = parse_encrypted_data(encrypted_data)
    
    # Retrieve decryption key
    key = @key_manager.get_decryption_key(parsed_data[:key_id], context)
    
    case parsed_data[:algorithm]
    when 'AES-256-GCM'
      decrypt_aes_gcm(parsed_data, key)
    when 'AES-256-CBC'
      decrypt_aes_cbc(parsed_data, key)
    else
      raise "Unsupported AES algorithm: #{parsed_data[:algorithm]}"
    end
  end
  
  private
  
  def encrypt_aes_gcm(data, key)
    cipher = OpenSSL::Cipher.new('aes-256-gcm')
    cipher.encrypt
    cipher.key = key
    
    # Generate random IV
    iv = cipher.random_iv
    
    # Encrypt data
    encrypted = cipher.update(data) + cipher.final
    tag = cipher.auth_tag
    
    # Create encrypted data structure
    {
      algorithm: 'AES-256-GCM',
      key_id: key[:key_id],
      iv: Base64.strict_encode64(iv),
      encrypted_data: Base64.strict_encode64(encrypted),
      auth_tag: Base64.strict_encode64(tag),
      version: '1.0'
    }
  end
  
  def decrypt_aes_gcm(parsed_data, key)
    cipher = OpenSSL::Cipher.new('aes-256-gcm')
    cipher.decrypt
    cipher.key = key[:key]
    cipher.iv = Base64.strict_decode64(parsed_data[:iv])
    cipher.auth_tag = Base64.strict_decode64(parsed_data[:auth_tag])
    cipher.auth_data = ''
    
    # Decrypt data
    encrypted = Base64.strict_decode64(parsed_data[:encrypted_data])
    cipher.update(encrypted) + cipher.final
  rescue OpenSSL::Cipher::CipherError => e
    raise EncryptionError, "Decryption failed: #{e.message}"
  end
  
  def encrypt_aes_cbc(data, key)
    cipher = OpenSSL::Cipher.new('aes-256-cbc')
    cipher.encrypt
    cipher.key = key
    
    # Generate random IV
    iv = cipher.random_iv
    
    # Pad data to block size
    padded_data = pad_data(data, cipher.block_size)
    
    # Encrypt data
    encrypted = cipher.update(padded_data) + cipher.final
    
    # Create encrypted data structure
    {
      algorithm: 'AES-256-CBC',
      key_id: key[:key_id],
      iv: Base64.strict_encode64(iv),
      encrypted_data: Base64.strict_encode64(encrypted),
      version: '1.0'
    }
  end
  
  def decrypt_aes_cbc(parsed_data, key)
    cipher = OpenSSL::Cipher.new('aes-256-cbc')
    cipher.decrypt
    cipher.key = key[:key]
    cipher.iv = Base64.strict_decode64(parsed_data[:iv])
    
    # Decrypt data
    encrypted = Base64.strict_decode64(parsed_data[:encrypted_data])
    decrypted = cipher.update(encrypted) + cipher.final
    
    # Remove padding
    unpad_data(decrypted)
  rescue OpenSSL::Cipher::CipherError => e
    raise EncryptionError, "Decryption failed: #{e.message}"
  end
  
  def parse_encrypted_data(encrypted_data)
    if encrypted_data.is_a?(String)
      JSON.parse(encrypted_data, symbolize_names: true)
    else
      encrypted_data
    end
  end
  
  def pad_data(data, block_size)
    padding_length = block_size - (data.bytesize % block_size)
    data + padding_length.chr * padding_length
  end
  
  def unpad_data(data)
    padding_length = data.bytes.last
    data[0...-padding_length]
  end
end
```

### 2. RSA Encryption Strategy
```ruby
require 'tusklang/encryption'
require 'openssl'

class RSAStrategy
  include TuskLang::Encryption::Strategy
  
  def initialize(key_size: 2048)
    @key_size = key_size
    @config = TuskLang.parse_file('config/encryption.tsk')
    @key_manager = KeyManager.new
  end
  
  def encrypt(data, context = {})
    # RSA is typically used for key encryption, not data encryption
    # For large data, we'll use hybrid encryption (RSA + AES)
    
    if data.bytesize > max_data_size
      return hybrid_encrypt(data, context)
    else
      return rsa_encrypt(data, context)
    end
  end
  
  def decrypt(encrypted_data, context = {})
    parsed_data = parse_encrypted_data(encrypted_data)
    
    case parsed_data[:encryption_type]
    when 'hybrid'
      hybrid_decrypt(parsed_data, context)
    when 'rsa'
      rsa_decrypt(parsed_data, context)
    else
      raise "Unknown encryption type: #{parsed_data[:encryption_type]}"
    end
  end
  
  def generate_key_pair
    private_key = OpenSSL::PKey::RSA.new(@key_size)
    public_key = private_key.public_key
    
    {
      private_key: private_key.to_pem,
      public_key: public_key.to_pem,
      key_id: SecureRandom.uuid
    }
  end
  
  private
  
  def max_data_size
    (@key_size / 8) - 42 # RSA padding overhead
  end
  
  def hybrid_encrypt(data, context)
    # Generate AES key for data encryption
    aes_key = SecureRandom.random_bytes(32)
    
    # Encrypt data with AES
    aes_strategy = AESStrategy.new(algorithm: 'AES-256-GCM')
    encrypted_data = aes_strategy.encrypt(data, { key: aes_key })
    
    # Encrypt AES key with RSA
    rsa_key = @key_manager.get_rsa_public_key(context)
    encrypted_key = rsa_key.public_encrypt(aes_key, OpenSSL::PKey::RSA::PKCS1_OAEP_PADDING)
    
    {
      encryption_type: 'hybrid',
      algorithm: 'RSA-AES-256-GCM',
      key_id: rsa_key[:key_id],
      encrypted_key: Base64.strict_encode64(encrypted_key),
      encrypted_data: encrypted_data,
      version: '1.0'
    }
  end
  
  def hybrid_decrypt(parsed_data, context)
    # Decrypt AES key with RSA
    rsa_key = @key_manager.get_rsa_private_key(parsed_data[:key_id], context)
    encrypted_key = Base64.strict_decode64(parsed_data[:encrypted_key])
    aes_key = rsa_key.private_decrypt(encrypted_key, OpenSSL::PKey::RSA::PKCS1_OAEP_PADDING)
    
    # Decrypt data with AES
    aes_strategy = AESStrategy.new(algorithm: 'AES-256-GCM')
    aes_strategy.decrypt(parsed_data[:encrypted_data], { key: aes_key })
  end
  
  def rsa_encrypt(data, context)
    rsa_key = @key_manager.get_rsa_public_key(context)
    encrypted = rsa_key.public_encrypt(data, OpenSSL::PKey::RSA::PKCS1_OAEP_PADDING)
    
    {
      encryption_type: 'rsa',
      algorithm: 'RSA',
      key_id: rsa_key[:key_id],
      encrypted_data: Base64.strict_encode64(encrypted),
      version: '1.0'
    }
  end
  
  def rsa_decrypt(parsed_data, context)
    rsa_key = @key_manager.get_rsa_private_key(parsed_data[:key_id], context)
    encrypted = Base64.strict_decode64(parsed_data[:encrypted_data])
    rsa_key.private_decrypt(encrypted, OpenSSL::PKey::RSA::PKCS1_OAEP_PADDING)
  end
  
  def parse_encrypted_data(encrypted_data)
    if encrypted_data.is_a?(String)
      JSON.parse(encrypted_data, symbolize_names: true)
    else
      encrypted_data
    end
  end
end
```

### 3. ChaCha20-Poly1305 Strategy
```ruby
require 'tusklang/encryption'

class ChaCha20Strategy
  include TuskLang::Encryption::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/encryption.tsk')
    @key_manager = KeyManager.new
  end
  
  def encrypt(data, context = {})
    key = @key_manager.get_encryption_key(context)
    nonce = SecureRandom.random_bytes(12)
    
    # Use ChaCha20-Poly1305 for encryption
    cipher = OpenSSL::Cipher.new('chacha20-poly1305')
    cipher.encrypt
    cipher.key = key
    cipher.iv = nonce
    
    encrypted = cipher.update(data) + cipher.final
    tag = cipher.auth_tag
    
    {
      algorithm: 'ChaCha20-Poly1305',
      key_id: key[:key_id],
      nonce: Base64.strict_encode64(nonce),
      encrypted_data: Base64.strict_encode64(encrypted),
      auth_tag: Base64.strict_encode64(tag),
      version: '1.0'
    }
  end
  
  def decrypt(encrypted_data, context = {})
    parsed_data = parse_encrypted_data(encrypted_data)
    key = @key_manager.get_decryption_key(parsed_data[:key_id], context)
    
    cipher = OpenSSL::Cipher.new('chacha20-poly1305')
    cipher.decrypt
    cipher.key = key[:key]
    cipher.iv = Base64.strict_decode64(parsed_data[:nonce])
    cipher.auth_tag = Base64.strict_decode64(parsed_data[:auth_tag])
    cipher.auth_data = ''
    
    encrypted = Base64.strict_decode64(parsed_data[:encrypted_data])
    cipher.update(encrypted) + cipher.final
  rescue OpenSSL::Cipher::CipherError => e
    raise EncryptionError, "Decryption failed: #{e.message}"
  end
  
  private
  
  def parse_encrypted_data(encrypted_data)
    if encrypted_data.is_a?(String)
      JSON.parse(encrypted_data, symbolize_names: true)
    else
      encrypted_data
    end
  end
end
```

### 4. Key Management System
```ruby
require 'tusklang/encryption'
require 'aws-sdk-kms'

class KeyManager
  def initialize
    @config = TuskLang.parse_file('config/encryption.tsk')
    @cache = {}
    setup_key_sources
  end
  
  def get_encryption_key(context = {})
    key_source = context[:key_source] || @config['encryption']['master_key_source']
    
    case key_source
    when 'aws-kms'
      get_aws_kms_key(context)
    when 'vault'
      get_vault_key(context)
    when 'local'
      get_local_key(context)
    else
      raise "Unknown key source: #{key_source}"
    end
  end
  
  def get_decryption_key(key_id, context = {})
    cache_key = "decrypt_key:#{key_id}"
    
    # Check cache first
    return @cache[cache_key] if @cache[cache_key]
    
    # Retrieve key from source
    key = retrieve_key_from_source(key_id, context)
    
    # Cache key
    @cache[cache_key] = key
    
    key
  end
  
  def rotate_keys
    # Generate new keys
    new_keys = generate_new_keys
    
    # Update key references
    update_key_references(new_keys)
    
    # Clean up old keys
    cleanup_old_keys
    
    new_keys
  end
  
  private
  
  def setup_key_sources
    case @config['encryption']['master_key_source']
    when 'aws-kms'
      @kms_client = Aws::KMS::Client.new(
        region: @config['encryption']['key_management']['aws_kms']['region'],
        credentials: Aws::Credentials.new(
          @config['encryption']['key_management']['aws_kms']['access_key_id'],
          @config['encryption']['key_management']['aws_kms']['secret_access_key']
        )
      )
    when 'vault'
      @vault_client = Vault::Client.new(
        address: @config['encryption']['key_management']['vault']['url'],
        token: @config['encryption']['key_management']['vault']['token']
      )
    end
  end
  
  def get_aws_kms_key(context)
    key_id = context[:key_id] || @config['encryption']['keys']['data_key_id']
    
    # Generate data key from KMS
    response = @kms_client.generate_data_key(
      key_id: key_id,
      key_spec: 'AES_256'
    )
    
    {
      key: response.plaintext,
      key_id: key_id,
      encrypted_key: response.ciphertext_blob
    }
  end
  
  def get_vault_key(context)
    key_id = context[:key_id] || 'default'
    
    # Get key from Vault
    response = @vault_client.logical.read("transit/keys/#{key_id}")
    
    {
      key: response.data[:keys][:1],
      key_id: key_id
    }
  end
  
  def get_local_key(context)
    key_id = context[:key_id] || 'default'
    
    # Get key from local storage
    key_file = "config/keys/#{key_id}.key"
    
    if File.exist?(key_file)
      {
        key: File.read(key_file),
        key_id: key_id
      }
    else
      raise "Local key not found: #{key_id}"
    end
  end
  
  def retrieve_key_from_source(key_id, context)
    # Implementation depends on key source
    case @config['encryption']['master_key_source']
    when 'aws-kms'
      retrieve_aws_kms_key(key_id)
    when 'vault'
      retrieve_vault_key(key_id)
    when 'local'
      retrieve_local_key(key_id)
    end
  end
  
  def generate_new_keys
    case @config['encryption']['master_key_source']
    when 'aws-kms'
      generate_aws_kms_keys
    when 'vault'
      generate_vault_keys
    when 'local'
      generate_local_keys
    end
  end
  
  def update_key_references(new_keys)
    # Update configuration with new key references
    # Implementation depends on storage mechanism
  end
  
  def cleanup_old_keys
    # Remove old keys from storage
    # Implementation depends on storage mechanism
  end
end
```

### 5. Database Field Encryption
```ruby
require 'tusklang/encryption'

module Encryptable
  extend ActiveSupport::Concern
  
  included do
    before_save :encrypt_sensitive_fields
    after_find :decrypt_sensitive_fields
  end
  
  class_methods do
    def encrypts(*fields, **options)
      @encrypted_fields ||= []
      @encrypted_fields.concat(fields.map(&:to_s))
      
      fields.each do |field|
        define_method("#{field}=") do |value|
          instance_variable_set("@#{field}_encrypted", true)
          super(value)
        end
      end
    end
    
    def encrypted_fields
      @encrypted_fields || []
    end
  end
  
  private
  
  def encrypt_sensitive_fields
    self.class.encrypted_fields.each do |field|
      value = send(field)
      next if value.nil? || instance_variable_get("@#{field}_encrypted")
      
      encrypted_value = encrypt_field_value(value, field)
      write_attribute(field, encrypted_value)
    end
  end
  
  def decrypt_sensitive_fields
    self.class.encrypted_fields.each do |field|
      value = read_attribute(field)
      next if value.nil?
      
      decrypted_value = decrypt_field_value(value, field)
      write_attribute(field, decrypted_value)
    end
  end
  
  def encrypt_field_value(value, field)
    encryption_system = TuskLang::Encryption::System.new
    encrypted = encryption_system.encrypt(value.to_s, { field: field })
    encrypted.to_json
  end
  
  def decrypt_field_value(value, field)
    return value unless value.is_a?(String)
    
    begin
      encrypted_data = JSON.parse(value)
      encryption_system = TuskLang::Encryption::System.new
      encryption_system.decrypt(encrypted_data, { field: field })
    rescue JSON::ParserError
      # Value is not encrypted, return as-is
      value
    end
  end
end

# Usage in models
class User < ApplicationRecord
  include Encryptable
  
  encrypts :ssn, :credit_card_number, :medical_records
end

class Order < ApplicationRecord
  include Encryptable
  
  encrypts :customer_notes, :payment_details
end
```

### 6. File Encryption
```ruby
require 'tusklang/encryption'

class FileEncryption
  def initialize
    @config = TuskLang.parse_file('config/encryption.tsk')
    @encryption_system = TuskLang::Encryption::System.new
  end
  
  def encrypt_file(input_path, output_path, context = {})
    # Read file in chunks to handle large files
    chunk_size = 1024 * 1024 # 1MB chunks
    
    File.open(output_path, 'wb') do |output_file|
      # Write encryption header
      header = generate_encryption_header(context)
      output_file.write(header.to_json + "\n")
      
      # Encrypt file content
      File.open(input_path, 'rb') do |input_file|
        while chunk = input_file.read(chunk_size)
          encrypted_chunk = @encryption_system.encrypt(chunk, context)
          output_file.write(encrypted_chunk.to_json + "\n")
        end
      end
    end
  end
  
  def decrypt_file(input_path, output_path, context = {})
    File.open(output_path, 'wb') do |output_file|
      File.open(input_path, 'r') do |input_file|
        # Skip header
        input_file.readline
        
        # Decrypt file content
        input_file.each_line do |line|
          encrypted_chunk = JSON.parse(line.strip, symbolize_names: true)
          decrypted_chunk = @encryption_system.decrypt(encrypted_chunk, context)
          output_file.write(decrypted_chunk)
        end
      end
    end
  end
  
  def encrypt_stream(input_stream, output_stream, context = {})
    # Write encryption header
    header = generate_encryption_header(context)
    output_stream.write(header.to_json + "\n")
    
    # Encrypt stream content
    input_stream.each_line do |line|
      encrypted_line = @encryption_system.encrypt(line, context)
      output_stream.write(encrypted_line.to_json + "\n")
    end
  end
  
  private
  
  def generate_encryption_header(context)
    {
      algorithm: @config['encryption']['default_algorithm'],
      version: '1.0',
      timestamp: Time.now.iso8601,
      context: context
    }
  end
end
```

## 🔧 Advanced Configuration

### Encryption Middleware
```ruby
require 'tusklang/encryption'

class EncryptionMiddleware
  def initialize(app)
    @app = app
    @config = TuskLang.parse_file('config/encryption.tsk')
    @encryption_system = TuskLang::Encryption::System.new
  end
  
  def call(env)
    request = Rack::Request.new(env)
    
    # Encrypt sensitive response data
    status, headers, response = @app.call(env)
    
    if should_encrypt_response?(request, headers)
      encrypted_response = encrypt_response(response, request)
      [status, headers, encrypted_response]
    else
      [status, headers, response]
    end
  end
  
  private
  
  def should_encrypt_response?(request, headers)
    # Check if response contains sensitive data
    content_type = headers['Content-Type']
    return false unless content_type&.include?('application/json')
    
    # Check request path for sensitive endpoints
    sensitive_paths = @config['encryption']['sensitive_paths'] || []
    sensitive_paths.any? { |path| request.path.start_with?(path) }
  end
  
  def encrypt_response(response, request)
    # Parse response body
    body = response.join
    data = JSON.parse(body)
    
    # Encrypt sensitive fields
    encrypted_data = encrypt_sensitive_fields(data, request)
    
    # Return encrypted response
    [encrypted_data.to_json]
  end
  
  def encrypt_sensitive_fields(data, request)
    if data.is_a?(Hash)
      data.transform_values { |value| encrypt_sensitive_fields(value, request) }
    elsif data.is_a?(Array)
      data.map { |item| encrypt_sensitive_fields(item, request) }
    else
      # Check if field should be encrypted
      if should_encrypt_field?(data, request)
        @encryption_system.encrypt(data.to_s, { request: request })
      else
        data
      end
    end
  end
  
  def should_encrypt_field?(value, request)
    # Implementation to determine if field should be encrypted
    # based on field name, value type, or request context
    false
  end
end
```

## 🚀 Performance Optimization

### Encryption Caching
```ruby
require 'tusklang/encryption'

class EncryptionCache
  def initialize
    @cache = TuskLang::Cache::RedisCache.new
    @config = TuskLang.parse_file('config/encryption.tsk')
  end
  
  def cache_encrypted_value(key, value, context_hash)
    cache_key = generate_cache_key(key, context_hash)
    ttl = parse_duration(@config['encryption']['cache_ttl'])
    
    @cache.set(cache_key, value, ttl)
  end
  
  def get_cached_encrypted_value(key, context_hash)
    cache_key = generate_cache_key(key, context_hash)
    @cache.get(cache_key)
  end
  
  def invalidate_key_cache(key_id)
    pattern = "encrypt:#{key_id}:*"
    @cache.delete_pattern(pattern)
  end
  
  private
  
  def generate_cache_key(key, context_hash)
    "encrypt:#{key}:#{context_hash}"
  end
  
  def parse_duration(duration_string)
    case duration_string
    when /(\d+)h/
      $1.to_i * 3600
    when /(\d+)m/
      $1.to_i * 60
    else
      3600 # Default 1 hour
    end
  end
end
```

## 📊 Monitoring and Analytics

### Encryption Analytics
```ruby
require 'tusklang/encryption'

class EncryptionAnalytics
  def initialize
    @metrics = TuskLang::Metrics::Collector.new
  end
  
  def track_encryption_operation(operation, algorithm, data_size, success)
    @metrics.increment("encryption.operations.total")
    @metrics.increment("encryption.operations.#{operation}")
    @metrics.increment("encryption.operations.#{operation}.#{success ? 'success' : 'failure'}")
    @metrics.increment("encryption.algorithms.#{algorithm}")
    
    @metrics.histogram("encryption.data_size", data_size)
  end
  
  def track_key_usage(key_id, operation)
    @metrics.increment("encryption.keys.#{key_id}.#{operation}")
  end
  
  def get_encryption_stats
    {
      total_operations: @metrics.get("encryption.operations.total"),
      success_rate: @metrics.get_success_rate("encryption.operations"),
      popular_algorithms: @metrics.get_top("encryption.algorithms", 5),
      average_data_size: @metrics.get_average("encryption.data_size")
    }
  end
end
```

This comprehensive encryption system provides enterprise-grade security features while maintaining the flexibility and power of TuskLang. The combination of multiple encryption algorithms, key management systems, and performance optimizations creates a robust foundation for protecting sensitive data in Ruby applications. 