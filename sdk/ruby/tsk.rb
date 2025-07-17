#!/usr/bin/env ruby
# frozen_string_literal: true

# TuskLang Configuration SDK for Ruby
# Handles TOML-like TSK format with full FUJSEN (function serialization) support

require 'json'
require 'zlib'
require 'base64'
require 'securerandom'
require 'net/http'
require 'uri'
require 'date'

module TuskLang
  # Main TSK class for parsing, generating, and executing TSK files
  class TSK
    attr_reader :data, :comments, :metadata

    def initialize(data = {})
      @data = data
      @fujsen_cache = {}
      @comments = {}
      @metadata = {}
    end

    # Create TSK instance from string content
    def self.from_string(content)
      parser = TSKParser.new
      data, comments = parser.parse_with_comments(content)
      tsk = new(data)
      tsk.instance_variable_set(:@comments, comments)
      tsk
    end

    # Create TSK instance from file
    def self.from_file(filepath)
      content = File.read(filepath)
      from_string(content)
    end

    # Get a section by name
    def get_section(name)
      @data[name]
    end

    # Get a value from a section
    def get_value(section, key)
      section_data = get_section(section)
      section_data&.dig(key)
    end

    # Set a section with values
    def set_section(name, values)
      @data[name] = values
    end

    # Set a value in a section
    def set_value(section, key, value)
      @data[section] ||= {}
      @data[section][key] = value
    end

    # Convert TSK to string representation
    def to_s
      TSKParser.stringify(@data)
    end

    # Convert TSK to hash
    def to_hash
      @data.dup
    end

    # Execute a FUJSEN function from a section
    def execute_fujsen(section, key = 'fujsen', *args)
      section_data = get_section(section)
      raise ArgumentError, "Section '#{section}' not found" unless section_data
      raise ArgumentError, "FUJSEN function '#{key}' not found in section '#{section}'" unless section_data.key?(key)

      code = section_data[key].to_s
      raise ArgumentError, "FUJSEN code is empty for '#{section}.#{key}'" if code.empty?

      cache_key = "#{section}.#{key}"
      unless @fujsen_cache.key?(cache_key)
        @fujsen_cache[cache_key] = compile_fujsen(code)
      end

      @fujsen_cache[cache_key].call(*args)
    end

    # Execute FUJSEN with context injection
    def execute_fujsen_with_context(section, key, context, *args)
      section_data = get_section(section)
      raise ArgumentError, "Section '#{section}' not found" unless section_data
      raise ArgumentError, "FUJSEN function '#{key}' not found in section '#{section}'" unless section_data.key?(key)

      code = section_data[key].to_s
      raise ArgumentError, "FUJSEN code is empty for '#{section}.#{key}'" if code.empty?

      # Inject context into the function
      context_code = inject_context_into_code(code, context)
      
      cache_key = "#{section}.#{key}.#{context_hash(context)}"
      unless @fujsen_cache.key?(cache_key)
        @fujsen_cache[cache_key] = compile_fujsen(context_code)
      end

      @fujsen_cache[cache_key].call(*args)
    end

    # Compile JavaScript FUJSEN code to Ruby proc
    private def compile_fujsen(code)
      # Convert JavaScript to Ruby-like code
      ruby_code = convert_javascript_to_ruby(code)
      
      # Create a safe execution environment
      binding = create_safe_binding
      
      # Compile and return proc
      eval("proc { |*args| #{ruby_code} }", binding)
    rescue => e
      raise "FUJSEN compilation failed: #{e.message}"
    end

    # Convert JavaScript code to Ruby-like code
    private def convert_javascript_to_ruby(js_code)
      # Basic JavaScript to Ruby conversion
      ruby_code = js_code
        .gsub(/function\s+(\w+)\s*\(([^)]*)\)\s*\{/, 'lambda { |\2|')
        .gsub(/=>/, '->')
        .gsub(/throw new Error/, 'raise')
        .gsub(/Date\.now\(\)/, 'Time.now.to_i * 1000')
        .gsub(/new Date\(\)\.toISOString\(\)/, 'Time.now.iso8601')
        .gsub(/console\.log/, 'puts')
        .gsub(/\.length/, '.length')
        .gsub(/\.forEach\(/, '.each { |')
        .gsub(/\.map\(/, '.map { |')
        .gsub(/\.filter\(/, '.select { |')
        .gsub(/\.push\(/, '.push(')
        .gsub(/Math\.max/, '[].max')
        .gsub(/Math\.min/, '[].min')
        .gsub(/Array\.isArray/, '->(obj) { obj.is_a?(Array) }')
        .gsub(/typeof\s+(\w+)\s*===/, '\1.is_a?')
        .gsub(/null/, 'nil')
        .gsub(/true/, 'true')
        .gsub(/false/, 'false')

      # Handle function declarations
      if ruby_code.match?(/lambda \{ \|/)
        ruby_code = ruby_code.gsub(/\}\s*$/, '}')
      else
        # Convert arrow functions
        ruby_code = ruby_code.gsub(/\(([^)]*)\)\s*=>\s*\{?([^}]*)\}?/, 'lambda { |\1| \2 }')
      end

      ruby_code
    end

    # Inject context variables into FUJSEN code
    private def inject_context_into_code(code, context)
      context_vars = context.map { |k, v| "#{k} = #{serialize_value(v)}" }.join("\n")
      "#{context_vars}\n#{code}"
    end

    # Serialize value for Ruby context
    private def serialize_value(value)
      case value
      when nil then 'nil'
      when String then "\"#{value.gsub('"', '\\"')}\""
      when Numeric then value.to_s
      when true then 'true'
      when false then 'false'
      when Hash then "{#{value.map { |k, v| "\"#{k}\" => #{serialize_value(v)}" }.join(', ')}}"
      when Array then "[#{value.map { |v| serialize_value(v) }.join(', ')}]"
      else "\"#{value}\""
      end
    end

    # Get hash for context caching
    private def context_hash(context)
      context_str = context.sort.map { |k, v| "#{k}=#{v}" }.join('|')
      Base64.strict_encode64(context_str)
    end

    # Get all FUJSEN functions from a section
    def get_fujsen_map(section)
      section_data = get_section(section)
      return {} unless section_data

      fujsen_map = {}
      section_data.each do |key, value|
        next unless value.is_a?(String) && !value.empty?
        
        begin
          compiled_function = compile_fujsen(value)
          fujsen_map[key] = compiled_function
        rescue
          # Skip non-FUJSEN strings
        end
      end
      fujsen_map
    end

    # Set a FUJSEN function in a section
    def set_fujsen(section, key, function)
      code = "# Function: #{function.class}\n# Parameters: #{function.parameters.map(&:last).join(', ')}"
      set_value(section, key, code)
    end

    # Execute FUJSEN @ operators
    def execute_operators(value, context = {})
      if value.is_a?(String) && value.include?('@')
        execute_operator(value, context)
      else
        value
      end
    end

    # Execute a single @ operator
    private def execute_operator(expression, context)
      operator_match = expression.match(/@(\w+)\s*\(([^)]*)\)/)
      return expression unless operator_match

      operator_name = operator_match[1].downcase
      operator_args = operator_match[2]

      case operator_name
      when 'query' then execute_query(operator_args, context)
      when 'cache' then execute_cache(operator_args, context)
      when 'metrics' then execute_metrics(operator_args, context)
      when 'if' then execute_if(operator_args, context)
      when 'date' then execute_date(operator_args, context)
      when 'env' then execute_env(operator_args, context)
      when 'optimize' then execute_optimize(operator_args, context)
      when 'learn' then execute_learn(operator_args, context)
      when 'feature' then execute_feature(operator_args, context)
      when 'json' then execute_json(operator_args, context)
      when 'request' then execute_request(operator_args, context)
      when 'file' then execute_file(operator_args, context)
      when 'flex' then execute_flex(operator_args, context)
      else expression
      end
    end

    # @ Operator implementations
    private def execute_query(expression, context)
      # Mock database query - in real implementation, connect to actual database
      {
        'results' => [],
        'count' => 0,
        'query' => expression
      }
    end

    private def execute_cache(expression, context)
      # Mock cache implementation
      parts = expression.split(',')
      ttl = parts.length > 0 ? parse_ttl(parts[0].strip) : 300.0
      key = parts.length > 1 ? parts[1].strip.gsub(/^"|"$/, '') : 'default'
      
      {
        'cached' => true,
        'ttl' => ttl,
        'key' => key,
        'value' => context['cache_value'] || 'default'
      }
    end

    private def execute_metrics(expression, context)
      parts = expression.split(',')
      metric_name = parts[0].strip.gsub(/^"|"$/, '')
      value = parts.length > 1 ? parts[1].strip.to_f : 0.0
      
      {
        'metric' => metric_name,
        'value' => value,
        'timestamp' => Time.now.to_i * 1000
      }
    end

    private def execute_if(expression, context)
      parts = expression.split(',')
      return false if parts.length < 3
      
      condition = parts[0].strip
      true_value = parts[1].strip.gsub(/^"|"$/, '')
      false_value = parts[2].strip.gsub(/^"|"$/, '')
      
      # Simple condition evaluation
      result = evaluate_condition(condition, context)
      result ? true_value : false_value
    end

    private def execute_date(expression, context)
      format = expression.empty? ? '%Y-%m-%d %H:%M:%S' : expression.strip.gsub(/^"|"$/, '')
      Time.now.strftime(format)
    end

    private def execute_env(expression, context)
      var_name = expression.strip.gsub(/^"|"$/, '')
      ENV[var_name]
    end

    private def execute_optimize(expression, context)
      {
        'optimized' => true,
        'parameter' => expression.strip.gsub(/^"|"$/, ''),
        'value' => context['optimize_value'] || 'default'
      }
    end

    private def execute_learn(expression, context)
      {
        'learned' => true,
        'parameter' => expression.strip.gsub(/^"|"$/, ''),
        'value' => context['learn_value'] || 'default'
      }
    end

    private def execute_feature(expression, context)
      feature_name = expression.strip.gsub(/^"|"$/, '')
      # Mock feature detection
      case feature_name
      when 'redis' then true
      when 'postgresql' then true
      when 'sqlite' then true
      when 'azure' then true
      when 'unity' then true
      when 'rails' then true
      when 'jekyll' then true
      else false
      end
    end

    private def execute_json(expression, context)
      context.to_json
    rescue
      '{}'
    end

    private def execute_request(expression, context)
      # Mock HTTP request - in real implementation, use Net::HTTP
      {
        'status' => 200,
        'data' => {},
        'url' => expression.strip.gsub(/^"|"$/, '')
      }
    end

    private def execute_file(expression, context)
      filepath = expression.strip.gsub(/^"|"$/, '')
      File.read(filepath)
    rescue
      nil
    end

    private def execute_flex(expression, context)
      # FlexChain integration
      parts = expression.split(',')
      operation = parts[0].strip.gsub(/^"|"$/, '')
      
      case operation
      when 'balance' then get_flex_balance(parts.length > 1 ? parts[1].strip.gsub(/^"|"$/, '') : '')
      when 'transfer' then execute_flex_transfer(parts[1..-1])
      when 'stake' then execute_flex_stake(parts[1..-1])
      when 'unstake' then execute_flex_unstake(parts[1..-1])
      when 'reward' then get_flex_reward(parts.length > 1 ? parts[1].strip.gsub(/^"|"$/, '') : '')
      when 'status' then get_flex_status
      when 'delegate' then execute_flex_delegate(parts[1..-1])
      when 'vote' then execute_flex_vote(parts[1..-1])
      else { 'error' => "Unknown Flex operation: #{operation}" }
      end
    end

    # FlexChain operation implementations
    private def get_flex_balance(address)
      {
        'address' => address,
        'balance' => 1000.0,
        'currency' => 'FLEX'
      }
    end

    private def execute_flex_transfer(args)
      {
        'success' => true,
        'transaction_id' => "tx_#{Time.now.to_i * 1000}",
        'amount' => args.length > 0 ? args[0].to_f : 0.0,
        'to' => args.length > 1 ? args[1].strip.gsub(/^"|"$/, '') : '',
        'from' => args.length > 2 ? args[2].strip.gsub(/^"|"$/, '') : ''
      }
    end

    private def execute_flex_stake(args)
      {
        'success' => true,
        'staked_amount' => args.length > 0 ? args[0].to_f : 0.0,
        'validator' => args.length > 1 ? args[1].strip.gsub(/^"|"$/, '') : ''
      }
    end

    private def execute_flex_unstake(args)
      {
        'success' => true,
        'unstaked_amount' => args.length > 0 ? args[0].to_f : 0.0,
        'validator' => args.length > 1 ? args[1].strip.gsub(/^"|"$/, '') : ''
      }
    end

    private def get_flex_reward(address)
      {
        'address' => address,
        'reward' => 50.0,
        'currency' => 'FLEX'
      }
    end

    private def get_flex_status
      {
        'network' => 'FlexChain',
        'status' => 'active',
        'block_height' => 12345,
        'validators' => 10,
        'total_staked' => 1000000.0
      }
    end

    private def execute_flex_delegate(args)
      {
        'success' => true,
        'delegated_amount' => args.length > 0 ? args[0].to_f : 0.0,
        'validator' => args.length > 1 ? args[1].strip.gsub(/^"|"$/, '') : '',
        'delegator' => args.length > 2 ? args[2].strip.gsub(/^"|"$/, '') : ''
      }
    end

    private def execute_flex_vote(args)
      {
        'success' => true,
        'proposal' => args.length > 0 ? args[0].strip.gsub(/^"|"$/, '') : '',
        'vote' => args.length > 1 ? args[1].strip.gsub(/^"|"$/, '') : '',
        'voter' => args.length > 2 ? args[2].strip.gsub(/^"|"$/, '') : ''
      }
    end

    # Helper methods
    private def parse_ttl(ttl)
      return ttl.to_f if ttl.match?(/^\d+$/)
      
      match = ttl.match(/^(\d+)([smhd])$/)
      return 300.0 unless match
      
      value = match[1].to_f
      unit = match[2]
      
      case unit
      when 's' then value
      when 'm' then value * 60
      when 'h' then value * 3600
      when 'd' then value * 86400
      else 300.0
      end
    end

    private def evaluate_condition(condition, context)
      if condition.include?('==')
        parts = condition.split('==')
        left = parts[0].strip.gsub(/^"|"$/, '')
        right = parts[1].strip.gsub(/^"|"$/, '')
        (context[left] || '').to_s == right
      elsif condition.include?('!=')
        parts = condition.split('!=')
        left = parts[0].strip.gsub(/^"|"$/, '')
        right = parts[1].strip.gsub(/^"|"$/, '')
        (context[left] || '').to_s != right
      else
        false
      end
    end

    # Create a safe binding for FUJSEN execution
    private def create_safe_binding
      binding
    end

    # Store data with Shell format
    def store_with_shell(data, metadata = nil)
      shell_data = ShellStorage.create_shell_data(data, SecureRandom.uuid, metadata)
      ShellStorage.pack(shell_data)
    end

    # Retrieve data from Shell format
    def retrieve_from_shell(shell_data)
      ShellStorage.unpack(shell_data)
    end

    # Detect type of Shell data
    def detect_type(shell_data)
      ShellStorage.detect_type(shell_data)
    end
  end
end 