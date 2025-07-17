# 💎 Database Transactions in TuskLang - Ruby Edition

**"We don't bow to any king" - Transaction Management with Ruby Grace**

Database transactions ensure data integrity by grouping multiple database operations into a single atomic unit. In Ruby, TuskLang provides elegant transaction handling that integrates seamlessly with Rails and ActiveRecord patterns.

## 🚀 Basic Transactions

### Automatic Transaction Handling

```ruby
require 'tusklang'

# TuskLang configuration with transaction support
tsk_content = <<~TSK
  [database]
  # Automatic transaction handling
  create_user_with_account: @db.transaction((tx) => {
      # All operations use the transaction connection
      user: tx.table("users").insert({
          name: "John Doe",
          email: "john@example.com",
          created_at: @now()
      })
      
      account: tx.table("accounts").insert({
          user_id: user.id,
          balance: 1000.00,
          currency: "USD"
      })
      
      tx.table("logs").insert({
          action: "user_created",
          user_id: user.id,
          created_at: @now()
      })
      
      # If any operation fails, all are rolled back
      return {user_id: user.id, account_id: account.id}
  })
TSK

# Ruby implementation
class UserService
  include TuskLang::Transactionable
  
  def create_user_with_account(user_data, account_data)
    tusk_config = Rails.application.config.tusk_config
    
    # Execute the transaction from TuskLang config
    result = tusk_config.execute_transaction('create_user_with_account', {
      user_data: user_data,
      account_data: account_data
    })
    
    result
  end
end
```

### With Return Values and Error Handling

```ruby
# TuskLang configuration
tsk_content = <<~TSK
  [transactions]
  create_user_profile: @db.transaction((tx) => {
      user: @User.create({
          name: "Jane Doe",
          email: "jane@example.com",
          status: "active"
      })
      
      profile: user.profile().create({
          bio: "New user profile",
          avatar_url: @env("DEFAULT_AVATAR_URL"),
          preferences: {
              notifications: true,
              theme: "dark"
          }
      })
      
      # Return structured data
      return {
          user: user,
          profile: profile,
          success: true
      }
  })
TSK

# Ruby service class
class UserProfileService
  include TuskLang::Transactionable
  
  def create_user_with_profile(user_attrs, profile_attrs)
    begin
      result = tusk_config.execute_transaction('create_user_profile', {
        user_attrs: user_attrs,
        profile_attrs: profile_attrs
      })
      
      # Handle successful result
      OpenStruct.new(
        user: result[:user],
        profile: result[:profile],
        success: result[:success]
      )
      
    rescue ActiveRecord::RecordInvalid => e
      Rails.logger.error("Transaction failed: #{e.message}")
      OpenStruct.new(success: false, error: e.message)
    end
  end
end
```

## 🔧 Manual Transaction Control

### Explicit Transaction Management

```ruby
# TuskLang configuration
tsk_content = <<~TSK
  [manual_transactions]
  process_payment: {
      # Begin transaction manually
      @db.beginTransaction()
      
      try {
          # Perform operations
          payment: @Payment.create({
              amount: @request.amount,
              user_id: @request.user_id,
              status: "processing"
          })
          
          @User.where("id", @request.user_id)
               .decrement("balance", @request.amount)
          
          @TransactionLog.create({
              payment_id: payment.id,
              action: "payment_processed",
              amount: @request.amount
          })
          
          # Commit if successful
          @db.commit()
          
          return {success: true, payment_id: payment.id}
          
      } catch (Exception e) {
          # Rollback on error
          @db.rollback()
          @log.error("Payment failed: " + e.message)
          throw e
      }
  }
TSK

# Ruby implementation with explicit control
class PaymentProcessor
  include TuskLang::Transactionable
  
  def process_payment(payment_data)
    tusk_config = Rails.application.config.tusk_config
    
    ActiveRecord::Base.transaction do
      begin
        # Execute the manual transaction logic
        result = tusk_config.execute_manual_transaction('process_payment', {
          amount: payment_data[:amount],
          user_id: payment_data[:user_id]
        })
        
        result
        
      rescue => e
        Rails.logger.error("Payment processing failed: #{e.message}")
        raise ActiveRecord::Rollback
      end
    end
  end
end
```

### Using Specific Database Connections

```ruby
# TuskLang configuration
tsk_content = <<~TSK
  [multi_database]
  sync_user_data: {
      # Using specific connection
      mysql_connection: @db.connection("mysql")
      postgres_connection: @db.connection("postgres")
      
      mysql_connection.beginTransaction()
      postgres_connection.beginTransaction()
      
      try {
          # Sync to MySQL
          mysql_connection.table("users").insert({
              id: @request.user_id,
              name: @request.name,
              email: @request.email
          })
          
          # Sync to PostgreSQL
          postgres_connection.table("user_profiles").insert({
              user_id: @request.user_id,
              bio: @request.bio,
              preferences: @request.preferences
          })
          
          mysql_connection.commit()
          postgres_connection.commit()
          
      } catch (e) {
          mysql_connection.rollback()
          postgres_connection.rollback()
          throw e
      }
  }
TSK

# Ruby implementation with multiple databases
class UserSyncService
  include TuskLang::Transactionable
  
  def sync_user_data(user_data)
    tusk_config = Rails.application.config.tusk_config
    
    # Use multiple database connections
    ActiveRecord::Base.connected_to(database: { writing: :mysql }) do
      ActiveRecord::Base.transaction do
        ActiveRecord::Base.connected_to(database: { writing: :postgres }) do
          ActiveRecord::Base.transaction do
            result = tusk_config.execute_manual_transaction('sync_user_data', {
              user_id: user_data[:id],
              name: user_data[:name],
              email: user_data[:email],
              bio: user_data[:bio],
              preferences: user_data[:preferences]
            })
            
            result
          end
        end
      end
    end
  end
end
```

## 🛡️ Transaction Isolation Levels

### Setting Isolation Levels

```ruby
# TuskLang configuration
tsk_content = <<~TSK
  [isolation_levels]
  # Set isolation level
  high_isolation_transaction: @db.transaction((tx) => {
      # Operations with specific isolation
      balance: tx.table("accounts")
          .where("id", @request.account_id)
          .lockForUpdate()
          .value("balance")
      
      if (balance >= @request.amount) {
          tx.table("accounts")
              .where("id", @request.account_id)
              .decrement("balance", @request.amount)
          
          tx.table("transactions").insert({
              account_id: @request.account_id,
              amount: -@request.amount,
              type: "withdrawal",
              created_at: @now()
          })
      } else {
          throw "Insufficient funds"
      }
  }, {isolation: "serializable"})
  
  # Available isolation levels:
  # - read_uncommitted
  # - read_committed (default)
  # - repeatable_read  
  # - serializable
TSK

# Ruby implementation with isolation levels
class AccountService
  include TuskLang::Transactionable
  
  def withdraw_funds(account_id, amount)
    tusk_config = Rails.application.config.tusk_config
    
    # Execute with serializable isolation
    result = tusk_config.execute_transaction_with_isolation(
      'high_isolation_transaction',
      { isolation: :serializable },
      {
        account_id: account_id,
        amount: amount
      }
    )
    
    result
  end
end
```

### Example with Serializable Isolation

```ruby
# TuskLang configuration for inventory management
tsk_content = <<~TSK
  [inventory]
  reserve_product: @db.transaction((tx) => {
      # Highest isolation level - prevents all phenomena
      product: tx.table("products")
          .where("id", @request.product_id)
          .lockForUpdate()
          .first()
      
      if (product.stock >= @request.quantity) {
          tx.table("products")
              .where("id", @request.product_id)
              .decrement("stock", @request.quantity)
          
          reservation: tx.table("reservations").insert({
              product_id: @request.product_id,
              user_id: @request.user_id,
              quantity: @request.quantity,
              expires_at: @date.add("1h")
          })
          
          return {success: true, reservation_id: reservation.id}
      } else {
          throw "Insufficient stock"
      }
  }, {isolation: "serializable"})
TSK

# Ruby implementation
class InventoryService
  include TuskLang::Transactionable
  
  def reserve_product(product_id, user_id, quantity)
    tusk_config = Rails.application.config.tusk_config
    
    begin
      result = tusk_config.execute_transaction_with_isolation(
        'reserve_product',
        { isolation: :serializable },
        {
          product_id: product_id,
          user_id: user_id,
          quantity: quantity
        }
      )
      
      result
      
    rescue => e
      Rails.logger.error("Product reservation failed: #{e.message}")
      { success: false, error: e.message }
    end
  end
end
```

## 🔄 Nested Transactions

### Using Savepoints for Nested Transactions

```ruby
# TuskLang configuration
tsk_content = <<~TSK
  [nested_transactions]
  complex_operation: @db.transaction((tx) => {
      tx.table("users").insert({name: "Parent User"})
      
      # Nested transaction using savepoint
      try {
          tx.transaction((nested) => {
              nested.table("posts").insert({title: "Nested Post"})
              
              # This might fail
              if (@request.should_fail) {
                  throw "Nested transaction failed"
              }
          })
      } catch (e) {
          # Only nested transaction is rolled back
          @log.error("Nested failed: " + e.message)
      }
      
      # Parent transaction continues
      tx.table("logs").insert({message: "Parent continues"})
  })
TSK

# Ruby implementation with nested transactions
class ComplexOperationService
  include TuskLang::Transactionable
  
  def perform_complex_operation(should_fail = false)
    tusk_config = Rails.application.config.tusk_config
    
    ActiveRecord::Base.transaction do
      result = tusk_config.execute_transaction('complex_operation', {
        should_fail: should_fail
      })
      
      result
    end
  end
end
```

### Manual Savepoint Management

```ruby
# TuskLang configuration
tsk_content = <<~TSK
  [savepoints]
  risky_operation: {
      @db.beginTransaction()
      
      @db.table("users").insert({name: "User 1"})
      
      # Create savepoint
      @db.savepoint("before_risky_operation")
      
      try {
          # Risky operation
          @db.table("critical").update({status: "processing"})
          
          if (@request.operation_failed) {
              # Rollback to savepoint
              @db.rollbackToSavepoint("before_risky_operation")
          }
      } catch (e) {
          @db.rollbackToSavepoint("before_risky_operation")
      }
      
      @db.commit()
  }
TSK

# Ruby implementation with savepoints
class RiskyOperationService
  include TuskLang::Transactionable
  
  def perform_risky_operation(operation_failed = false)
    tusk_config = Rails.application.config.tusk_config
    
    ActiveRecord::Base.transaction do
      result = tusk_config.execute_manual_transaction('risky_operation', {
        operation_failed: operation_failed
      })
      
      result
    end
  end
end
```

## 🔒 Pessimistic Locking

### Lock for Update

```ruby
# TuskLang configuration
tsk_content = <<~TSK
  [locking]
  # Lock for update - prevents other transactions from reading
  update_user_balance: @db.transaction((tx) => {
      user: tx.table("users")
          .where("id", @request.user_id)
          .lockForUpdate()
          .first()
      
      # Other transactions wait until this completes
      new_balance: user.balance + @request.amount
      
      tx.table("users")
          .where("id", @request.user_id)
          .update({balance: new_balance})
      
      return {success: true, new_balance: new_balance}
  })
TSK

# Ruby implementation with pessimistic locking
class UserBalanceService
  include TuskLang::Transactionable
  
  def update_user_balance(user_id, amount)
    tusk_config = Rails.application.config.tusk_config
    
    result = tusk_config.execute_transaction('update_user_balance', {
      user_id: user_id,
      amount: amount
    })
    
    result
  end
end
```

### Shared Lock

```ruby
# TuskLang configuration
tsk_content = <<~TSK
  [shared_locks]
  # Shared lock - allows reads but prevents updates
  generate_report: @db.transaction((tx) => {
      products: tx.table("products")
          .where("category_id", @request.category_id)
          .sharedLock()
          .get()
      
      # Can read but not update these products
      total_value: products.sum(p => p.price * p.stock)
      
      report: tx.table("reports").insert({
          category_id: @request.category_id,
          total_value: total_value,
          product_count: products.length,
          generated_at: @now()
      })
      
      return {report_id: report.id, total_value: total_value}
  })
TSK

# Ruby implementation with shared locks
class ReportService
  include TuskLang::Transactionable
  
  def generate_category_report(category_id)
    tusk_config = Rails.application.config.tusk_config
    
    result = tusk_config.execute_transaction('generate_report', {
      category_id: category_id
    })
    
    result
  end
end
```

## 🚀 Advanced Transaction Patterns

### Distributed Transactions

```ruby
# TuskLang configuration for distributed transactions
tsk_content = <<~TSK
  [distributed]
  # Two-phase commit pattern
  distributed_payment: {
      # Phase 1: Prepare
      mysql_prepared: @db.connection("mysql").prepare("payment_prepare", {
          user_id: @request.user_id,
          amount: @request.amount
      })
      
      postgres_prepared: @db.connection("postgres").prepare("balance_prepare", {
          user_id: @request.user_id,
          amount: @request.amount
      })
      
      if (mysql_prepared && postgres_prepared) {
          # Phase 2: Commit
          @db.connection("mysql").commit("payment_prepare")
          @db.connection("postgres").commit("balance_prepare")
          
          return {success: true}
      } else {
          # Rollback both
          @db.connection("mysql").rollback("payment_prepare")
          @db.connection("postgres").rollback("balance_prepare")
          
          throw "Distributed transaction failed"
      }
  }
TSK

# Ruby implementation with distributed transactions
class DistributedPaymentService
  include TuskLang::Transactionable
  
  def process_distributed_payment(user_id, amount)
    tusk_config = Rails.application.config.tusk_config
    
    begin
      result = tusk_config.execute_manual_transaction('distributed_payment', {
        user_id: user_id,
        amount: amount
      })
      
      result
      
    rescue => e
      Rails.logger.error("Distributed payment failed: #{e.message}")
      { success: false, error: e.message }
    end
  end
end
```

### Optimistic Locking

```ruby
# TuskLang configuration
tsk_content = <<~TSK
  [optimistic_locking]
  # Optimistic locking with version checking
  update_with_version: @db.transaction((tx) => {
      user: tx.table("users")
          .where("id", @request.user_id)
          .where("version", @request.expected_version)
          .first()
      
      if (!user) {
          throw "User not found or version mismatch"
      }
      
      updated_user: tx.table("users")
          .where("id", @request.user_id)
          .where("version", @request.expected_version)
          .update({
              name: @request.name,
              email: @request.email,
              version: @request.expected_version + 1
          })
      
      if (updated_user.affected_rows == 0) {
          throw "Concurrent modification detected"
      }
      
      return {success: true, new_version: @request.expected_version + 1}
  })
TSK

# Ruby implementation with optimistic locking
class OptimisticLockingService
  include TuskLang::Transactionable
  
  def update_user_with_version(user_id, expected_version, user_data)
    tusk_config = Rails.application.config.tusk_config
    
    begin
      result = tusk_config.execute_transaction('update_with_version', {
        user_id: user_id,
        expected_version: expected_version,
        name: user_data[:name],
        email: user_data[:email]
      })
      
      result
      
    rescue => e
      Rails.logger.error("Optimistic locking failed: #{e.message}")
      { success: false, error: e.message }
    end
  end
end
```

## 🧪 Testing Transactions

### Transaction Testing in Ruby

```ruby
# RSpec tests for transaction handling
require 'rspec'
require 'tusklang'

RSpec.describe UserService do
  let(:user_service) { UserService.new }
  let(:tusk_config) { Rails.application.config.tusk_config }
  
  describe '#create_user_with_account' do
    it 'creates user and account in a single transaction' do
      user_data = { name: 'John Doe', email: 'john@example.com' }
      account_data = { balance: 1000.00, currency: 'USD' }
      
      result = user_service.create_user_with_account(user_data, account_data)
      
      expect(result[:user_id]).to be_present
      expect(result[:account_id]).to be_present
      
      # Verify both records exist
      user = User.find(result[:user_id])
      account = Account.find(result[:account_id])
      
      expect(user.name).to eq('John Doe')
      expect(account.balance).to eq(1000.00)
    end
    
    it 'rolls back on validation errors' do
      user_data = { name: '', email: 'invalid-email' } # Invalid data
      account_data = { balance: 1000.00 }
      
      expect {
        user_service.create_user_with_account(user_data, account_data)
      }.to raise_error(ActiveRecord::RecordInvalid)
      
      # Verify no records were created
      expect(User.count).to eq(0)
      expect(Account.count).to eq(0)
    end
  end
end

# Integration tests for isolation levels
RSpec.describe AccountService do
  let(:account_service) { AccountService.new }
  
  describe '#withdraw_funds' do
    it 'prevents race conditions with serializable isolation' do
      account = create(:account, balance: 1000.00)
      
      # Simulate concurrent withdrawals
      threads = []
      5.times do
        threads << Thread.new do
          account_service.withdraw_funds(account.id, 200.00)
        end
      end
      
      threads.each(&:join)
      
      # Only one withdrawal should succeed
      account.reload
      expect(account.balance).to be >= 0
      expect(account.balance).to be <= 1000.00
    end
  end
end
```

## 🔧 Configuration and Setup

### Rails Integration

```ruby
# config/initializers/tusk_transactions.rb
Rails.application.config.after_initialize do
  TuskLang.configure do |config|
    # Configure transaction settings
    config.transaction_settings = {
      default_isolation: :read_committed,
      timeout: 30.seconds,
      retry_attempts: 3,
      deadlock_retry_delay: 100.milliseconds
    }
    
    # Configure database adapters
    config.database_adapters = {
      mysql: TuskLang::Adapters::MySQLAdapter.new(
        host: ENV['MYSQL_HOST'],
        database: ENV['MYSQL_DATABASE'],
        user: ENV['MYSQL_USER'],
        password: ENV['MYSQL_PASSWORD']
      ),
      postgres: TuskLang::Adapters::PostgreSQLAdapter.new(
        host: ENV['POSTGRES_HOST'],
        database: ENV['POSTGRES_DATABASE'],
        user: ENV['POSTGRES_USER'],
        password: ENV['POSTGRES_PASSWORD']
      )
    }
  end
end

# app/models/concerns/tusk_transactionable.rb
module TuskTransactionable
  extend ActiveSupport::Concern
  
  included do
    include TuskLang::Transactionable
  end
  
  private
  
  def tusk_config
    Rails.application.config.tusk_config
  end
end
```

## 🚀 Performance Considerations

### Transaction Performance Tips

```ruby
# Optimize transaction performance
class OptimizedTransactionService
  include TuskTransactionable
  
  def bulk_create_users(users_data)
    # Use bulk operations within transactions
    tusk_config.execute_transaction('bulk_create_users', {
      users: users_data
    })
  end
  
  def process_with_batching(records, batch_size: 1000)
    # Process large datasets in batches
    records.each_slice(batch_size) do |batch|
      tusk_config.execute_transaction('process_batch', {
        records: batch
      })
    end
  end
end

# TuskLang configuration for bulk operations
tsk_content = <<~TSK
  [bulk_operations]
  bulk_create_users: @db.transaction((tx) => {
      # Bulk insert for better performance
      user_ids: tx.table("users").bulkInsert(@request.users)
      
      # Bulk insert related records
      profiles: @request.users.map((user, index) => ({
          user_id: user_ids[index],
          bio: user.bio || "",
          created_at: @now()
      }))
      
      tx.table("profiles").bulkInsert(profiles)
      
      return {user_ids: user_ids}
  })
TSK
```

## 🔒 Security Best Practices

### Secure Transaction Handling

```ruby
# Security-focused transaction service
class SecureTransactionService
  include TuskTransactionable
  
  def secure_payment_processing(payment_data)
    # Validate input before transaction
    validate_payment_data(payment_data)
    
    # Use specific isolation level for financial transactions
    result = tusk_config.execute_transaction_with_isolation(
      'secure_payment',
      { isolation: :serializable },
      payment_data
    )
    
    # Audit logging
    log_transaction_audit(result)
    
    result
  end
  
  private
  
  def validate_payment_data(data)
    raise ArgumentError, "Invalid payment data" unless data[:amount]&.positive?
    raise ArgumentError, "Invalid user ID" unless data[:user_id]&.present?
  end
  
  def log_transaction_audit(result)
    Rails.logger.info("Transaction completed: #{result[:transaction_id]}")
  end
end
```

## 🎯 Summary

TuskLang's transaction system in Ruby provides:

- **Automatic transaction management** with seamless Rails integration
- **Multiple isolation levels** for different concurrency requirements
- **Nested transactions** with savepoint support
- **Pessimistic and optimistic locking** strategies
- **Distributed transaction support** for multi-database scenarios
- **Comprehensive testing** capabilities
- **Performance optimization** features
- **Security best practices** for sensitive operations

The Ruby implementation maintains the rebellious spirit of TuskLang while providing enterprise-grade transaction management that "doesn't bow to any king" - not even the constraints of traditional database transaction patterns.

**Ready to revolutionize your Ruby application's transaction handling with TuskLang?** 🚀 