# Database Transactions in TuskLang for Rust

TuskLang's Rust transaction system provides type-safe, ACID-compliant database transactions with compile-time guarantees, async/await support, and comprehensive error handling for complex database operations.

## 🚀 **Why Rust Database Transactions?**

Rust's ownership model and type system make it the perfect language for database transactions:

- **ACID Compliance**: Guaranteed atomicity, consistency, isolation, and durability
- **Type Safety**: Compile-time validation of transaction operations
- **Ownership Safety**: Automatic resource management and cleanup
- **Async/Await**: Non-blocking transaction execution
- **Error Handling**: Comprehensive error types with rollback guarantees

## Basic Transaction Operations

```rust
use tusk_db::{Transaction, Database, Result};
use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

#[derive(Debug, Serialize, Deserialize, Clone)]
struct User {
    pub id: Option<i32>,
    pub name: String,
    pub email: String,
    pub balance: f64,
    pub created_at: Option<DateTime<Utc>>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct Order {
    pub id: Option<i32>,
    pub user_id: i32,
    pub total: f64,
    pub status: String,
    pub created_at: Option<DateTime<Utc>>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
struct OrderItem {
    pub id: Option<i32>,
    pub order_id: i32,
    pub product_id: i32,
    pub quantity: i32,
    pub price: f64,
}

// Basic transaction with automatic rollback
async fn create_user_with_order(user_data: User, order_data: Order) -> Result<(User, Order)> {
    let mut tx = @db.begin_transaction().await?;
    
    // Use try-catch pattern for automatic rollback
    let result = async {
        // Create user
        let user_id = tx.insert(
            "INSERT INTO users (name, email, balance) VALUES (?, ?, ?)",
            &[&user_data.name, &user_data.email, &user_data.balance]
        ).await?;
        
        // Create order
        let order_id = tx.insert(
            "INSERT INTO orders (user_id, total, status) VALUES (?, ?, ?)",
            &[&user_id, &order_data.total, &"pending"]
        ).await?;
        
        // Fetch created records
        let user = tx.query_one::<User>(
            "SELECT * FROM users WHERE id = ?",
            &[&user_id]
        ).await?;
        
        let order = tx.query_one::<Order>(
            "SELECT * FROM orders WHERE id = ?",
            &[&order_id]
        ).await?;
        
        Ok((user, order))
    }.await;
    
    match result {
        Ok(data) => {
            tx.commit().await?;
            Ok(data)
        }
        Err(e) => {
            tx.rollback().await?;
            Err(e)
        }
    }
}

// Transaction with explicit error handling
async fn transfer_money(from_user_id: i32, to_user_id: i32, amount: f64) -> Result<()> {
    let mut tx = @db.begin_transaction().await?;
    
    // Check sender balance
    let sender_balance = tx.query_one::<f64>(
        "SELECT balance FROM users WHERE id = ?",
        &[&from_user_id]
    ).await?;
    
    if sender_balance < amount {
        tx.rollback().await?;
        return Err("Insufficient funds".into());
    }
    
    // Deduct from sender
    tx.update(
        "UPDATE users SET balance = balance - ? WHERE id = ?",
        &[&amount, &from_user_id]
    ).await?;
    
    // Add to receiver
    tx.update(
        "UPDATE users SET balance = balance + ? WHERE id = ?",
        &[&amount, &to_user_id]
    ).await?;
    
    // Log the transaction
    tx.insert(
        "INSERT INTO transactions (from_user_id, to_user_id, amount, created_at) VALUES (?, ?, ?, ?)",
        &[&from_user_id, &to_user_id, &amount, &Utc::now()]
    ).await?;
    
    tx.commit().await?;
    Ok(())
}
```

## Advanced Transaction Patterns

```rust
use tusk_db::{TransactionBuilder, Savepoint, IsolationLevel};

// Transaction with savepoints
async fn complex_order_processing(order_data: Order, items: Vec<OrderItem>) -> Result<Order> {
    let mut tx = @db.begin_transaction().await?;
    
    // Set isolation level
    tx.set_isolation_level(IsolationLevel::Serializable).await?;
    
    let result = async {
        // Create order
        let order_id = tx.insert(
            "INSERT INTO orders (user_id, total, status) VALUES (?, ?, ?)",
            &[&order_data.user_id, &order_data.total, &"processing"]
        ).await?;
        
        // Create savepoint for items
        let items_savepoint = tx.create_savepoint("items_creation").await?;
        
        // Add order items
        for item in &items {
            // Check inventory
            let available_stock = tx.query_one::<i32>(
                "SELECT stock FROM inventory WHERE product_id = ?",
                &[&item.product_id]
            ).await?;
            
            if available_stock < item.quantity {
                // Rollback to savepoint and try alternative
                tx.rollback_to_savepoint(&items_savepoint).await?;
                
                // Try with reduced quantity
                let reduced_quantity = available_stock;
                tx.insert(
                    "INSERT INTO order_items (order_id, product_id, quantity, price) VALUES (?, ?, ?, ?)",
                    &[&order_id, &item.product_id, &reduced_quantity, &item.price]
                ).await?;
            } else {
                tx.insert(
                    "INSERT INTO order_items (order_id, product_id, quantity, price) VALUES (?, ?, ?, ?)",
                    &[&order_id, &item.product_id, &item.quantity, &item.price]
                ).await?;
                
                // Update inventory
                tx.update(
                    "UPDATE inventory SET stock = stock - ? WHERE product_id = ?",
                    &[&item.quantity, &item.product_id]
                ).await?;
            }
        }
        
        // Update order status
        tx.update(
            "UPDATE orders SET status = ? WHERE id = ?",
            &[&"completed", &order_id]
        ).await?;
        
        // Fetch final order
        let order = tx.query_one::<Order>(
            "SELECT * FROM orders WHERE id = ?",
            &[&order_id]
        ).await?;
        
        Ok(order)
    }.await;
    
    match result {
        Ok(order) => {
            tx.commit().await?;
            Ok(order)
        }
        Err(e) => {
            tx.rollback().await?;
            Err(e)
        }
    }
}

// Nested transactions with savepoints
async fn nested_transaction_example() -> Result<()> {
    let mut outer_tx = @db.begin_transaction().await?;
    
    let result = async {
        // Outer transaction operations
        outer_tx.insert("INSERT INTO logs (message) VALUES (?)", &["Outer transaction started"]).await?;
        
        // Create savepoint for nested operations
        let nested_savepoint = outer_tx.create_savepoint("nested_ops").await?;
        
        // Nested operations
        let nested_result = async {
            outer_tx.insert("INSERT INTO logs (message) VALUES (?)", &["Nested operation 1"]).await?;
            outer_tx.insert("INSERT INTO logs (message) VALUES (?)", &["Nested operation 2"]).await?;
            
            // Simulate nested failure
            if true {
                return Err("Nested operation failed".into());
            }
            
            Ok(())
        }.await;
        
        match nested_result {
            Ok(_) => {
                // Commit nested operations
                outer_tx.release_savepoint(&nested_savepoint).await?;
            }
            Err(e) => {
                // Rollback nested operations but continue outer transaction
                outer_tx.rollback_to_savepoint(&nested_savepoint).await?;
                outer_tx.insert("INSERT INTO logs (message) VALUES (?)", &["Nested operations rolled back"]).await?;
            }
        }
        
        // Continue with outer transaction
        outer_tx.insert("INSERT INTO logs (message) VALUES (?)", &["Outer transaction completed"]).await?;
        
        Ok(())
    }.await;
    
    match result {
        Ok(_) => {
            outer_tx.commit().await?;
            Ok(())
        }
        Err(e) => {
            outer_tx.rollback().await?;
            Err(e)
        }
    }
}
```

## Transaction Isolation Levels

```rust
use tusk_db::{IsolationLevel, TransactionConfig};

// Configure transaction with specific isolation level
async fn configure_transaction_isolation() -> Result<()> {
    let config = TransactionConfig {
        isolation_level: IsolationLevel::Serializable,
        read_only: false,
        deferrable: false,
    };
    
    let mut tx = @db.begin_transaction_with_config(config).await?;
    
    let result = async {
        // Perform operations with specified isolation level
        tx.insert("INSERT INTO critical_data (value) VALUES (?)", &["important"]).await?;
        
        // Read with serializable isolation
        let data = tx.query::<String>("SELECT value FROM critical_data").await?;
        
        Ok(data)
    }.await;
    
    match result {
        Ok(data) => {
            tx.commit().await?;
            Ok(())
        }
        Err(e) => {
            tx.rollback().await?;
            Err(e)
        }
    }
}

// Different isolation levels for different use cases
async fn isolation_level_examples() -> Result<()> {
    // Read committed for most operations
    let mut read_tx = @db.begin_transaction().await?;
    read_tx.set_isolation_level(IsolationLevel::ReadCommitted).await?;
    
    // Serializable for critical operations
    let mut critical_tx = @db.begin_transaction().await?;
    critical_tx.set_isolation_level(IsolationLevel::Serializable).await?;
    
    // Read uncommitted for analytics (be careful!)
    let mut analytics_tx = @db.begin_transaction().await?;
    analytics_tx.set_isolation_level(IsolationLevel::ReadUncommitted).await?;
    
    // Clean up
    read_tx.rollback().await?;
    critical_tx.rollback().await?;
    analytics_tx.rollback().await?;
    
    Ok(())
}
```

## Error Handling and Recovery

```rust
use tusk_db::{TransactionError, RetryStrategy};
use thiserror::Error;

#[derive(Error, Debug)]
pub enum TransactionError {
    #[error("Transaction failed: {0}")]
    Database(#[from] DatabaseError),
    
    #[error("Deadlock detected")]
    Deadlock,
    
    #[error("Serialization failure")]
    SerializationFailure,
    
    #[error("Connection lost")]
    ConnectionLost,
}

// Retry logic for transient failures
async fn retry_transaction<F, T>(mut f: F, max_attempts: u32) -> Result<T>
where
    F: FnMut() -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<T>> + Send>>,
{
    let mut attempts = 0;
    
    loop {
        match f().await {
            Ok(result) => return Ok(result),
            Err(TransactionError::Deadlock) if attempts < max_attempts => {
                attempts += 1;
                let delay = std::time::Duration::from_millis(100 * 2_u64.pow(attempts));
                tokio::time::sleep(delay).await;
                continue;
            }
            Err(TransactionError::SerializationFailure) if attempts < max_attempts => {
                attempts += 1;
                let delay = std::time::Duration::from_millis(50 * 2_u64.pow(attempts));
                tokio::time::sleep(delay).await;
                continue;
            }
            Err(e) => return Err(e),
        }
    }
}

// Robust transaction with retry logic
async fn robust_money_transfer(from_user_id: i32, to_user_id: i32, amount: f64) -> Result<()> {
    retry_transaction(|| async {
        let mut tx = @db.begin_transaction().await?;
        
        let result = async {
            // Check sender balance
            let sender_balance = tx.query_one::<f64>(
                "SELECT balance FROM users WHERE id = ? FOR UPDATE",
                &[&from_user_id]
            ).await?;
            
            if sender_balance < amount {
                return Err(TransactionError::InsufficientFunds);
            }
            
            // Deduct from sender
            tx.update(
                "UPDATE users SET balance = balance - ? WHERE id = ?",
                &[&amount, &from_user_id]
            ).await?;
            
            // Add to receiver
            tx.update(
                "UPDATE users SET balance = balance + ? WHERE id = ?",
                &[&amount, &to_user_id]
            ).await?;
            
            // Log transaction
            tx.insert(
                "INSERT INTO transactions (from_user_id, to_user_id, amount) VALUES (?, ?, ?)",
                &[&from_user_id, &to_user_id, &amount]
            ).await?;
            
            Ok(())
        }.await;
        
        match result {
            Ok(_) => {
                tx.commit().await?;
                Ok(())
            }
            Err(e) => {
                tx.rollback().await?;
                Err(e)
            }
        }
    }, 3).await
}
```

## Transaction Monitoring and Logging

```rust
use tusk_db::{TransactionMonitor, TransactionMetrics};
use std::time::Instant;

// Transaction monitoring
async fn monitored_transaction() -> Result<()> {
    let start = Instant::now();
    let mut tx = @db.begin_transaction().await?;
    
    // Monitor transaction
    let monitor = @TransactionMonitor::new(&tx).await?;
    
    let result = async {
        // Perform operations
        tx.insert("INSERT INTO users (name, email) VALUES (?, ?)", &["John", "john@example.com"]).await?;
        tx.update("UPDATE users SET name = ? WHERE email = ?", &["John Doe", "john@example.com"]).await?;
        
        Ok(())
    }.await;
    
    let duration = start.elapsed();
    
    // Log transaction metrics
    @log::info!("Transaction completed", {
        duration: duration.as_millis(),
        success: result.is_ok(),
        operations: monitor.operation_count(),
        locks: monitor.lock_count(),
    });
    
    match result {
        Ok(_) => {
            tx.commit().await?;
            Ok(())
        }
        Err(e) => {
            tx.rollback().await?;
            Err(e)
        }
    }
}

// Transaction performance tracking
async fn track_transaction_performance() -> Result<()> {
    let metrics = @TransactionMetrics::new().await?;
    
    let mut tx = @db.begin_transaction().await?;
    
    let result = async {
        // Perform operations
        for i in 0..100 {
            tx.insert(
                "INSERT INTO test_data (value, created_at) VALUES (?, ?)",
                &[&i, &Utc::now()]
            ).await?;
        }
        
        Ok(())
    }.await;
    
    match result {
        Ok(_) => {
            tx.commit().await?;
            
            // Record successful transaction
            metrics.record_success(duration).await;
            Ok(())
        }
        Err(e) => {
            tx.rollback().await?;
            
            // Record failed transaction
            metrics.record_failure(duration, &e).await;
            Err(e)
        }
    }
}
```

## Distributed Transactions

```rust
use tusk_db::{DistributedTransaction, TwoPhaseCommit};

// Two-phase commit for distributed transactions
async fn distributed_transaction_example() -> Result<()> {
    let mut dtx = @DistributedTransaction::new().await?;
    
    // Add participants
    dtx.add_participant("database1", @db.connection("db1")).await?;
    dtx.add_participant("database2", @db.connection("db2")).await?;
    
    let result = async {
        // Phase 1: Prepare
        dtx.prepare().await?;
        
        // Phase 2: Commit
        dtx.commit().await?;
        
        Ok(())
    }.await;
    
    match result {
        Ok(_) => {
            @log::info!("Distributed transaction committed successfully");
            Ok(())
        }
        Err(e) => {
            dtx.rollback().await?;
            @log::error!("Distributed transaction failed", { error: &e });
            Err(e)
        }
    }
}

// Saga pattern for complex distributed transactions
async fn saga_transaction_example() -> Result<()> {
    let mut saga = @SagaTransaction::new().await?;
    
    // Define saga steps
    saga.add_step("create_user", |tx| async {
        tx.insert("INSERT INTO users (name, email) VALUES (?, ?)", &["John", "john@example.com"]).await
    }).await?;
    
    saga.add_step("create_profile", |tx| async {
        tx.insert("INSERT INTO profiles (user_id, bio) VALUES (?, ?)", &[1, "New user"]).await
    }).await?;
    
    saga.add_step("send_welcome_email", |tx| async {
        // This step might fail, but we can compensate
        @send_email("john@example.com", "Welcome!").await
    }).await?;
    
    // Define compensation actions
    saga.add_compensation("create_user", |tx| async {
        tx.delete("DELETE FROM users WHERE email = ?", &["john@example.com"]).await
    }).await?;
    
    saga.add_compensation("create_profile", |tx| async {
        tx.delete("DELETE FROM profiles WHERE user_id = ?", &[1]).await
    }).await?;
    
    // Execute saga
    let result = saga.execute().await;
    
    match result {
        Ok(_) => {
            @log::info!("Saga completed successfully");
            Ok(())
        }
        Err(e) => {
            @log::error!("Saga failed, compensating", { error: &e });
            saga.compensate().await?;
            Err(e)
        }
    }
}
```

## Testing Transactions

```rust
use tusk_db::test_utils::{TestTransaction, TestDatabase};

// Test transaction with test database
#[tokio::test]
async fn test_money_transfer() -> Result<()> {
    let test_db = @TestDatabase::new().await?;
    let mut tx = test_db.begin_transaction().await?;
    
    // Setup test data
    let user1_id = tx.insert(
        "INSERT INTO users (name, email, balance) VALUES (?, ?, ?)",
        &["Alice", "alice@example.com", &100.0]
    ).await?;
    
    let user2_id = tx.insert(
        "INSERT INTO users (name, email, balance) VALUES (?, ?, ?)",
        &["Bob", "bob@example.com", &50.0]
    ).await?;
    
    // Test transfer
    let transfer_result = transfer_money(user1_id, user2_id, 30.0).await;
    assert!(transfer_result.is_ok());
    
    // Verify balances
    let alice_balance = tx.query_one::<f64>(
        "SELECT balance FROM users WHERE id = ?",
        &[&user1_id]
    ).await?;
    assert_eq!(alice_balance, 70.0);
    
    let bob_balance = tx.query_one::<f64>(
        "SELECT balance FROM users WHERE id = ?",
        &[&user2_id]
    ).await?;
    assert_eq!(bob_balance, 80.0);
    
    tx.rollback().await?;
    Ok(())
}

// Test transaction rollback
#[tokio::test]
async fn test_transaction_rollback() -> Result<()> {
    let test_db = @TestDatabase::new().await?;
    let mut tx = test_db.begin_transaction().await?;
    
    // Insert data
    let user_id = tx.insert(
        "INSERT INTO users (name, email) VALUES (?, ?)",
        &["Test User", "test@example.com"]
    ).await?;
    
    // Verify data exists
    let user = tx.query_one::<User>(
        "SELECT * FROM users WHERE id = ?",
        &[&user_id]
    ).await?;
    assert_eq!(user.name, "Test User");
    
    // Rollback transaction
    tx.rollback().await?;
    
    // Verify data was rolled back
    let user_after_rollback = tx.query_one::<Option<User>>(
        "SELECT * FROM users WHERE id = ?",
        &[&user_id]
    ).await?;
    assert!(user_after_rollback.is_none());
    
    Ok(())
}

// Test concurrent transactions
#[tokio::test]
async fn test_concurrent_transactions() -> Result<()> {
    let test_db = @TestDatabase::new().await?;
    
    // Start two concurrent transactions
    let tx1 = test_db.begin_transaction().await?;
    let tx2 = test_db.begin_transaction().await?;
    
    // Transaction 1: Read and update
    let balance1 = tx1.query_one::<f64>(
        "SELECT balance FROM users WHERE id = 1 FOR UPDATE",
        &[]
    ).await?;
    
    tx1.update(
        "UPDATE users SET balance = ? WHERE id = 1",
        &[&(balance1 + 10.0)]
    ).await?;
    
    // Transaction 2: Try to read (should wait or fail depending on isolation)
    let balance2 = tx2.query_one::<f64>(
        "SELECT balance FROM users WHERE id = 1 FOR UPDATE",
        &[]
    ).await?;
    
    // Commit transactions
    tx1.commit().await?;
    tx2.commit().await?;
    
    Ok(())
}
```

## Best Practices for Rust Transactions

1. **Use Strong Types**: Leverage Rust's type system for transaction safety
2. **Handle Errors**: Use proper error types and rollback guarantees
3. **Async/Await**: Use non-blocking transaction operations
4. **Isolation Levels**: Choose appropriate isolation levels for your use case
5. **Savepoints**: Use savepoints for complex nested operations
6. **Retry Logic**: Implement retry logic for transient failures
7. **Monitoring**: Monitor transaction performance and errors
8. **Testing**: Test all transaction scenarios thoroughly
9. **Deadlock Prevention**: Use consistent ordering to prevent deadlocks
10. **Resource Management**: Ensure proper cleanup of transaction resources

## Related Topics

- `database-overview-rust` - Database integration overview
- `query-builder-rust` - Fluent query interface
- `orm-models-rust` - Model definition and usage
- `migrations-rust` - Database schema versioning
- `relationships-rust` - Model relationships

---

**Ready to build type-safe, ACID-compliant database transactions with Rust and TuskLang?** 