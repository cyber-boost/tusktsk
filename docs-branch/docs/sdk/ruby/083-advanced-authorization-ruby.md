# 🛡️ Advanced Authorization with TuskLang Ruby

**"We don't bow to any king" - Ruby Edition**

Build sophisticated authorization systems with TuskLang's advanced authorization features. From role-based access control to attribute-based authorization, TuskLang provides the flexibility and power you need to implement complex permission systems in your Ruby applications.

## 🚀 Quick Start

### Basic Authorization Setup
```ruby
require 'tusklang'
require 'tusklang/auth'

# Initialize authorization system
auth_system = TuskLang::Auth::AuthorizationSystem.new

# Configure authorization
auth_system.configure do |config|
  config.default_policy = 'deny'
  config.cache_enabled = true
  config.cache_ttl = 1.hour
  config.audit_enabled = true
end

# Register authorization strategies
auth_system.register_strategy(:rbac, TuskLang::Auth::Strategies::RBACStrategy.new)
auth_system.register_strategy(:abac, TuskLang::Auth::Strategies::ABACStrategy.new)
auth_system.register_strategy(:policies, TuskLang::Auth::Strategies::PolicyStrategy.new)
```

### TuskLang Configuration
```tsk
# config/authorization.tsk
[authorization]
enabled: true
default_policy: "deny"
cache_enabled: true
cache_ttl: "1h"
audit_enabled: true

[authorization.roles]
admin: {
    permissions: ["*"],
    inherits: []
}
manager: {
    permissions: ["users.read", "users.update", "reports.*"],
    inherits: ["user"]
}
user: {
    permissions: ["profile.read", "profile.update", "orders.*"],
    inherits: ["guest"]
}
guest: {
    permissions: ["public.read"],
    inherits: []
}

[authorization.policies]
user_management: {
    rules: [
        {
            effect: "allow",
            action: "users.*",
            resource: "users",
            condition: "user.role == 'admin' OR user.id == resource.owner_id"
        },
        {
            effect: "allow",
            action: "users.read",
            resource: "users",
            condition: "user.role IN ['admin', 'manager']"
        }
    ]
}
order_management: {
    rules: [
        {
            effect: "allow",
            action: "orders.*",
            resource: "orders",
            condition: "user.role == 'admin' OR user.id == resource.user_id"
        },
        {
            effect: "allow",
            action: "orders.read",
            resource: "orders",
            condition: "user.role == 'manager' AND resource.status != 'draft'"
        }
    ]
}

[authorization.attributes]
user_attributes: ["id", "role", "department", "location", "security_clearance"]
resource_attributes: ["id", "owner_id", "user_id", "status", "visibility", "department"]
environment_attributes: ["time", "ip_address", "user_agent", "location"]
```

## 🎯 Core Features

### 1. Role-Based Access Control (RBAC)
```ruby
require 'tusklang/auth'

class RBACStrategy
  include TuskLang::Auth::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/authorization.tsk')
    @roles = @config['authorization']['roles']
    @role_cache = {}
  end
  
  def authorize(user, action, resource, context = {})
    user_roles = get_user_roles(user)
    
    # Check each role for permissions
    user_roles.each do |role|
      if has_permission?(role, action, resource)
        return AuthorizationResult.allow("Role #{role} has permission")
      end
    end
    
    AuthorizationResult.deny("No role has permission for #{action} on #{resource}")
  end
  
  def has_permission?(role, action, resource)
    role_config = @roles[role]
    return false unless role_config
    
    permissions = role_config['permissions']
    
    # Check wildcard permissions
    return true if permissions.include?('*')
    return true if permissions.include?("#{action}.*")
    return true if permissions.include?("*.#{resource}")
    
    # Check specific permission
    permissions.include?("#{action}.#{resource}")
  end
  
  def get_user_roles(user)
    roles = [user.role]
    
    # Get inherited roles
    roles.each do |role|
      inherited_roles = get_inherited_roles(role)
      roles.concat(inherited_roles)
    end
    
    roles.uniq
  end
  
  def get_inherited_roles(role)
    role_config = @roles[role]
    return [] unless role_config
    
    inherited = role_config['inherits'] || []
    
    # Recursively get inherited roles
    inherited.each do |inherited_role|
      inherited.concat(get_inherited_roles(inherited_role))
    end
    
    inherited.uniq
  end
  
  def assign_role(user, role)
    return false unless @roles[role]
    
    user.update!(role: role)
    clear_user_cache(user.id)
    
    true
  end
  
  def create_role(name, permissions: [], inherits: [])
    @roles[name] = {
      'permissions' => permissions,
      'inherits' => inherits
    }
    
    # Save to configuration
    save_roles_config
  end
  
  private
  
  def clear_user_cache(user_id)
    @role_cache.delete("user_roles:#{user_id}")
  end
  
  def save_roles_config
    # Implementation to save roles back to TuskLang config
  end
end
```

### 2. Attribute-Based Access Control (ABAC)
```ruby
require 'tusklang/auth'

class ABACStrategy
  include TuskLang::Auth::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/authorization.tsk')
    @policies = @config['authorization']['policies']
  end
  
  def authorize(user, action, resource, context = {})
    # Get applicable policies
    policies = get_applicable_policies(action, resource)
    
    policies.each do |policy_name, policy_config|
      result = evaluate_policy(policy_name, policy_config, user, action, resource, context)
      return result unless result.undetermined?
    end
    
    # Default to deny if no policies match
    AuthorizationResult.deny("No applicable policies found")
  end
  
  def evaluate_policy(policy_name, policy_config, user, action, resource, context)
    rules = policy_config['rules']
    
    rules.each do |rule|
      if rule_matches?(rule, user, action, resource, context)
        if rule['effect'] == 'allow'
          return AuthorizationResult.allow("Policy #{policy_name} allows access")
        else
          return AuthorizationResult.deny("Policy #{policy_name} denies access")
        end
      end
    end
    
    AuthorizationResult.undetermined("No matching rules in policy #{policy_name}")
  end
  
  def rule_matches?(rule, user, action, resource, context)
    # Check action match
    return false unless action_matches?(rule['action'], action)
    
    # Check resource match
    return false unless resource_matches?(rule['resource'], resource)
    
    # Check condition if present
    if rule['condition']
      return false unless evaluate_condition(rule['condition'], user, action, resource, context)
    end
    
    true
  end
  
  def action_matches?(rule_action, action)
    return true if rule_action == '*'
    return true if rule_action == action
    
    # Check wildcard patterns
    if rule_action.end_with?('.*')
      base_action = rule_action[0..-3]
      return action.start_with?(base_action + '.')
    end
    
    false
  end
  
  def resource_matches?(rule_resource, resource)
    return true if rule_resource == '*'
    return true if rule_resource == resource
    
    # Check wildcard patterns
    if rule_resource.end_with?('.*')
      base_resource = rule_resource[0..-3]
      return resource.start_with?(base_resource + '.')
    end
    
    false
  end
  
  def evaluate_condition(condition, user, action, resource, context)
    # Create evaluation context
    eval_context = {
      'user' => user_attributes(user),
      'resource' => resource_attributes(resource),
      'action' => action,
      'environment' => environment_attributes(context)
    }
    
    # Evaluate condition using TuskLang expression evaluator
    TuskLang::ExpressionEvaluator.evaluate(condition, eval_context)
  rescue => e
    Rails.logger.error "Condition evaluation error: #{e.message}"
    false
  end
  
  def user_attributes(user)
    {
      'id' => user.id,
      'role' => user.role,
      'department' => user.department,
      'location' => user.location,
      'security_clearance' => user.security_clearance
    }
  end
  
  def resource_attributes(resource)
    if resource.is_a?(ActiveRecord::Base)
      {
        'id' => resource.id,
        'owner_id' => resource.owner_id,
        'user_id' => resource.user_id,
        'status' => resource.status,
        'visibility' => resource.visibility,
        'department' => resource.department
      }
    else
      resource
    end
  end
  
  def environment_attributes(context)
    {
      'time' => Time.now,
      'ip_address' => context[:ip_address],
      'user_agent' => context[:user_agent],
      'location' => context[:location]
    }
  end
end
```

### 3. Policy-Based Authorization
```ruby
require 'tusklang/auth'

class PolicyStrategy
  include TuskLang::Auth::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/authorization.tsk')
    @policies = {}
    load_policies
  end
  
  def authorize(user, action, resource, context = {})
    # Find applicable policies
    applicable_policies = find_applicable_policies(action, resource)
    
    applicable_policies.each do |policy|
      result = policy.evaluate(user, action, resource, context)
      return result unless result.undetermined?
    end
    
    AuthorizationResult.deny("No applicable policies")
  end
  
  def create_policy(name, rules)
    policy = Policy.new(name, rules)
    @policies[name] = policy
    
    # Save to configuration
    save_policy_config(name, rules)
    
    policy
  end
  
  def update_policy(name, rules)
    return false unless @policies[name]
    
    @policies[name] = Policy.new(name, rules)
    save_policy_config(name, rules)
    
    true
  end
  
  def delete_policy(name)
    return false unless @policies[name]
    
    @policies.delete(name)
    remove_policy_config(name)
    
    true
  end
  
  private
  
  def load_policies
    @config['authorization']['policies'].each do |name, config|
      @policies[name] = Policy.new(name, config['rules'])
    end
  end
  
  def find_applicable_policies(action, resource)
    @policies.values.select do |policy|
      policy.applies_to?(action, resource)
    end
  end
  
  def save_policy_config(name, rules)
    # Implementation to save policy to TuskLang config
  end
  
  def remove_policy_config(name)
    # Implementation to remove policy from TuskLang config
  end
end

class Policy
  def initialize(name, rules)
    @name = name
    @rules = rules.map { |rule| Rule.new(rule) }
  end
  
  def evaluate(user, action, resource, context)
    @rules.each do |rule|
      if rule.matches?(user, action, resource, context)
        return rule.result
      end
    end
    
    AuthorizationResult.undetermined("No matching rules")
  end
  
  def applies_to?(action, resource)
    @rules.any? { |rule| rule.applies_to?(action, resource) }
  end
end

class Rule
  def initialize(rule_config)
    @effect = rule_config['effect']
    @action = rule_config['action']
    @resource = rule_config['resource']
    @condition = rule_config['condition']
  end
  
  def matches?(user, action, resource, context)
    return false unless action_matches?(action)
    return false unless resource_matches?(resource)
    return false unless condition_matches?(user, action, resource, context)
    
    true
  end
  
  def applies_to?(action, resource)
    action_matches?(action) && resource_matches?(resource)
  end
  
  def result
    if @effect == 'allow'
      AuthorizationResult.allow("Rule allows access")
    else
      AuthorizationResult.deny("Rule denies access")
    end
  end
  
  private
  
  def action_matches?(action)
    @action == '*' || @action == action || wildcard_match?(@action, action)
  end
  
  def resource_matches?(resource)
    @resource == '*' || @resource == resource || wildcard_match?(@resource, resource)
  end
  
  def condition_matches?(user, action, resource, context)
    return true unless @condition
    
    # Evaluate condition using TuskLang expression evaluator
    eval_context = {
      'user' => user_attributes(user),
      'resource' => resource_attributes(resource),
      'action' => action,
      'environment' => environment_attributes(context)
    }
    
    TuskLang::ExpressionEvaluator.evaluate(@condition, eval_context)
  rescue => e
    Rails.logger.error "Condition evaluation error: #{e.message}"
    false
  end
  
  def wildcard_match?(pattern, value)
    return false unless pattern.end_with?('.*')
    
    base = pattern[0..-3]
    value.start_with?(base + '.')
  end
  
  def user_attributes(user)
    {
      'id' => user.id,
      'role' => user.role,
      'department' => user.department,
      'location' => user.location
    }
  end
  
  def resource_attributes(resource)
    if resource.is_a?(ActiveRecord::Base)
      {
        'id' => resource.id,
        'owner_id' => resource.owner_id,
        'user_id' => resource.user_id,
        'status' => resource.status
      }
    else
      resource
    end
  end
  
  def environment_attributes(context)
    {
      'time' => Time.now,
      'ip_address' => context[:ip_address],
      'user_agent' => context[:user_agent]
    }
  end
end
```

### 4. Authorization Middleware
```ruby
require 'tusklang/auth'

class AuthorizationMiddleware
  def initialize(app)
    @app = app
    @auth_system = TuskLang::Auth::AuthorizationSystem.new
    @config = TuskLang.parse_file('config/authorization.tsk')
  end
  
  def call(env)
    request = Rack::Request.new(env)
    
    # Skip authorization for certain paths
    return @app.call(env) if skip_authorization?(request.path)
    
    # Extract authorization context
    context = extract_context(request)
    
    # Check authorization
    result = authorize_request(request, context)
    
    if result.allowed?
      @app.call(env)
    else
      unauthorized_response(result.reason)
    end
  end
  
  private
  
  def skip_authorization?(path)
    skip_paths = @config['authorization']['skip_paths'] || []
    skip_paths.any? { |skip_path| path.start_with?(skip_path) }
  end
  
  def extract_context(request)
    {
      ip_address: request.ip,
      user_agent: request.user_agent,
      method: request.request_method,
      path: request.path,
      params: request.params
    }
  end
  
  def authorize_request(request, context)
    # Extract user from request (assuming JWT token in header)
    user = extract_user(request)
    return AuthorizationResult.deny("Authentication required") unless user
    
    # Determine action and resource from request
    action = determine_action(request)
    resource = determine_resource(request)
    
    # Check authorization
    @auth_system.authorize(user, action, resource, context)
  end
  
  def extract_user(request)
    token = request.env['HTTP_AUTHORIZATION']&.gsub('Bearer ', '')
    return nil unless token
    
    # Decode JWT token and find user
    begin
      decoded = JWT.decode(token, ENV['JWT_SECRET'], true, { algorithm: 'HS256' })
      user_id = decoded[0]['user_id']
      User.find(user_id)
    rescue JWT::DecodeError, ActiveRecord::RecordNotFound
      nil
    end
  end
  
  def determine_action(request)
    method = request.request_method.downcase
    path = request.path
    
    case
    when method == 'get'
      'read'
    when method == 'post'
      'create'
    when method == 'put' || method == 'patch'
      'update'
    when method == 'delete'
      'delete'
    else
      method
    end
  end
  
  def determine_resource(request)
    path = request.path
    path_parts = path.split('/').reject(&:empty?)
    
    case path_parts.first
    when 'users'
      'users'
    when 'orders'
      'orders'
    when 'reports'
      'reports'
    else
      path_parts.first
    end
  end
  
  def unauthorized_response(reason)
    [
      403,
      { 'Content-Type' => 'application/json' },
      [{ error: 'Forbidden', reason: reason }.to_json]
    ]
  end
end
```

### 5. Authorization Decorators
```ruby
require 'tusklang/auth'

module AuthorizationDecorators
  def self.included(base)
    base.extend(ClassMethods)
  end
  
  module ClassMethods
    def requires_permission(action, resource, options = {})
      before_action :check_permission, only: options[:only], except: options[:except]
      
      define_method :permission_context do
        { action: action, resource: resource }
      end
    end
    
    def requires_role(role, options = {})
      before_action :check_role, only: options[:only], except: options[:except]
      
      define_method :required_role do
        role
      end
    end
  end
  
  private
  
  def check_permission
    context = permission_context
    user = current_user
    
    result = TuskLang::Auth::AuthorizationSystem.new.authorize(
      user,
      context[:action],
      context[:resource],
      request_context
    )
    
    unless result.allowed?
      render json: { error: 'Forbidden', reason: result.reason }, status: :forbidden
    end
  end
  
  def check_role
    user = current_user
    required_role = self.required_role
    
    unless user.role == required_role || user.has_role?(required_role)
      render json: { error: 'Forbidden', reason: "Role #{required_role} required" }, status: :forbidden
    end
  end
  
  def request_context
    {
      ip_address: request.remote_ip,
      user_agent: request.user_agent,
      method: request.method,
      path: request.path
    }
  end
end

# Usage in controllers
class UsersController < ApplicationController
  include AuthorizationDecorators
  
  requires_permission 'read', 'users', only: [:index, :show]
  requires_permission 'create', 'users', only: [:create]
  requires_permission 'update', 'users', only: [:update]
  requires_permission 'delete', 'users', only: [:destroy]
  
  requires_role 'admin', only: [:destroy]
  
  def index
    @users = User.all
    render json: @users
  end
  
  def show
    @user = User.find(params[:id])
    render json: @user
  end
  
  def create
    @user = User.create!(user_params)
    render json: @user, status: :created
  end
  
  def update
    @user = User.find(params[:id])
    @user.update!(user_params)
    render json: @user
  end
  
  def destroy
    @user = User.find(params[:id])
    @user.destroy
    head :no_content
  end
  
  private
  
  def user_params
    params.require(:user).permit(:name, :email, :role)
  end
end
```

## 🔧 Advanced Configuration

### Dynamic Policy Loading
```ruby
require 'tusklang/auth'

class DynamicPolicyLoader
  def initialize
    @config = TuskLang.parse_file('config/authorization.tsk')
    @policy_cache = {}
    @last_modified = {}
  end
  
  def load_policies
    policy_files = Dir.glob('config/policies/*.tsk')
    
    policy_files.each do |file|
      load_policy_file(file)
    end
  end
  
  def reload_policies
    policy_files = Dir.glob('config/policies/*.tsk')
    
    policy_files.each do |file|
      if file_modified?(file)
        load_policy_file(file)
      end
    end
  end
  
  private
  
  def load_policy_file(file)
    policy_name = File.basename(file, '.tsk')
    config = TuskLang.parse_file(file)
    
    @policy_cache[policy_name] = Policy.new(policy_name, config['rules'])
    @last_modified[file] = File.mtime(file)
  end
  
  def file_modified?(file)
    return true unless @last_modified[file]
    
    File.mtime(file) > @last_modified[file]
  end
end
```

### Authorization Caching
```ruby
require 'tusklang/auth'

class AuthorizationCache
  def initialize
    @cache = TuskLang::Cache::RedisCache.new
    @config = TuskLang.parse_file('config/authorization.tsk')
  end
  
  def cache_authorization(user_id, action, resource, context_hash, result)
    cache_key = generate_cache_key(user_id, action, resource, context_hash)
    ttl = parse_duration(@config['authorization']['cache_ttl'])
    
    @cache.set(cache_key, result, ttl)
  end
  
  def get_cached_authorization(user_id, action, resource, context_hash)
    cache_key = generate_cache_key(user_id, action, resource, context_hash)
    @cache.get(cache_key)
  end
  
  def invalidate_user_cache(user_id)
    pattern = "auth:#{user_id}:*"
    @cache.delete_pattern(pattern)
  end
  
  def invalidate_resource_cache(resource)
    pattern = "auth:*:*:#{resource}:*"
    @cache.delete_pattern(pattern)
  end
  
  private
  
  def generate_cache_key(user_id, action, resource, context_hash)
    "auth:#{user_id}:#{action}:#{resource}:#{context_hash}"
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

## 🚀 Performance Optimization

### Authorization Optimization
```ruby
require 'tusklang/auth'

class OptimizedAuthorizationSystem
  def initialize
    @config = TuskLang.parse_file('config/authorization.tsk')
    @cache = AuthorizationCache.new
    @policy_index = build_policy_index
  end
  
  def authorize(user, action, resource, context = {})
    # Check cache first
    context_hash = Digest::MD5.hexdigest(context.to_json)
    cached_result = @cache.get_cached_authorization(user.id, action, resource, context_hash)
    return cached_result if cached_result
    
    # Find applicable policies using index
    applicable_policies = @policy_index.find_policies(action, resource)
    
    # Evaluate policies
    result = evaluate_policies(applicable_policies, user, action, resource, context)
    
    # Cache result
    @cache.cache_authorization(user.id, action, resource, context_hash, result)
    
    result
  end
  
  private
  
  def build_policy_index
    PolicyIndex.new(@config['authorization']['policies'])
  end
  
  def evaluate_policies(policies, user, action, resource, context)
    policies.each do |policy|
      result = policy.evaluate(user, action, resource, context)
      return result unless result.undetermined?
    end
    
    AuthorizationResult.deny("No applicable policies")
  end
end

class PolicyIndex
  def initialize(policies)
    @action_index = {}
    @resource_index = {}
    build_indexes(policies)
  end
  
  def find_policies(action, resource)
    action_policies = @action_index[action] || []
    resource_policies = @resource_index[resource] || []
    
    # Find intersection of policies that match both action and resource
    action_policies & resource_policies
  end
  
  private
  
  def build_indexes(policies)
    policies.each do |name, config|
      policy = Policy.new(name, config['rules'])
      
      # Index by action
      policy.actions.each do |action|
        @action_index[action] ||= []
        @action_index[action] << policy
      end
      
      # Index by resource
      policy.resources.each do |resource|
        @resource_index[resource] ||= []
        @resource_index[resource] << policy
      end
    end
  end
end
```

## 📊 Monitoring and Analytics

### Authorization Analytics
```ruby
require 'tusklang/auth'

class AuthorizationAnalytics
  def initialize
    @metrics = TuskLang::Metrics::Collector.new
  end
  
  def track_authorization_attempt(user, action, resource, result)
    @metrics.increment("auth.authorization_attempts.total")
    @metrics.increment("auth.authorization_attempts.#{result.allowed? ? 'allowed' : 'denied'}")
    @metrics.increment("auth.authorization_attempts.action.#{action}")
    @metrics.increment("auth.authorization_attempts.resource.#{resource}")
    
    if result.allowed?
      @metrics.increment("auth.authorization_attempts.user.#{user.role}")
    end
  end
  
  def track_policy_evaluation(policy_name, result)
    @metrics.increment("auth.policy_evaluations.#{policy_name}")
    @metrics.increment("auth.policy_evaluations.#{policy_name}.#{result.allowed? ? 'allowed' : 'denied'}")
  end
  
  def get_authorization_stats
    {
      total_attempts: @metrics.get("auth.authorization_attempts.total"),
      allow_rate: @metrics.get_rate("auth.authorization_attempts.allowed", "auth.authorization_attempts.total"),
      top_actions: @metrics.get_top("auth.authorization_attempts.action", 10),
      top_resources: @metrics.get_top("auth.authorization_attempts.resource", 10),
      role_permissions: @metrics.get_top("auth.authorization_attempts.user", 5)
    }
  end
end
```

This comprehensive authorization system provides enterprise-grade access control features while maintaining the flexibility and power of TuskLang. The combination of RBAC, ABAC, and policy-based authorization creates a robust foundation for implementing complex permission systems in Ruby applications. 