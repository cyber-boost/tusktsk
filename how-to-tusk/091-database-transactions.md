# Database Transactions in TuskLang

Database transactions ensure data integrity by grouping multiple database operations into a single atomic unit. If any operation fails, all changes are rolled back.

## Basic Transactions

```tusk
# Automatic transaction handling
@db.transaction((tx) => {
    # All operations use the transaction connection
    user: tx.table("users").insert({
        name: "John Doe",
        email: "john@example.com"
    })
    
    account: tx.table("accounts").insert({
        user_id: user.id,
        balance: 1000.00
    })
    
    tx.table("logs").insert({
        action: "user_created",
        user_id: user.id,
        created_at: @now()
    })
    
    # If any operation fails, all are rolled back
})

# With return value
result: @db.transaction((tx) => {
    user: @User.create({
        name: "Jane Doe",
        email: "jane@example.com"
    })
    
    profile: user.profile().create({
        bio: "New user"
    })
    
    return {user, profile}
})
```

## Manual Transaction Control

```tusk
# Begin transaction manually
@db.beginTransaction()

try {
    # Perform operations
    @User.create({name: "Test User"})
    @Post.create({title: "Test Post"})
    
    # Commit if successful
    @db.commit()
    
} catch (Exception e) {
    # Rollback on error
    @db.rollback()
    throw e
}

# Using specific connection
connection: @db.connection("mysql")
connection.beginTransaction()

try {
    connection.table("users").insert(data)
    connection.commit()
} catch (e) {
    connection.rollback()
}
```

## Transaction Isolation Levels

```tusk
# Set isolation level
@db.transaction((tx) => {
    # Operations with specific isolation
}, {isolation: "read_committed"})

# Available isolation levels:
# - read_uncommitted
# - read_committed (default)
# - repeatable_read  
# - serializable

# Example with serializable isolation
@db.transaction((tx) => {
    # Highest isolation level - prevents all phenomena
    balance: tx.table("accounts")
        .where("id", account_id)
        .lockForUpdate()
        .value("balance")
    
    if (balance >= amount) {
        tx.table("accounts")
            .where("id", account_id)
            .decrement("balance", amount)
        
        tx.table("transactions").insert({
            account_id: account_id,
            amount: -amount,
            type: "withdrawal"
        })
    } else {
        throw "Insufficient funds"
    }
}, {isolation: "serializable"})
```

## Nested Transactions

```tusk
# Using savepoints for nested transactions
@db.transaction((tx) => {
    tx.table("users").insert({name: "Parent"})
    
    # Nested transaction using savepoint
    try {
        tx.transaction((nested) => {
            nested.table("posts").insert({title: "Nested"})
            
            # This might fail
            if (some_condition) {
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

# Manual savepoint management
@db.beginTransaction()

@db.table("users").insert({name: "User 1"})

# Create savepoint
@db.savepoint("before_risky_operation")

try {
    # Risky operation
    @db.table("critical").update({status: "processing"})
    
    if (operation_failed) {
        # Rollback to savepoint
        @db.rollbackToSavepoint("before_risky_operation")
    }
} catch (e) {
    @db.rollbackToSavepoint("before_risky_operation")
}

@db.commit()
```

## Pessimistic Locking

```tusk
# Lock for update - prevents other transactions from reading
@db.transaction((tx) => {
    user: tx.table("users")
        .where("id", user_id)
        .lockForUpdate()
        .first()
    
    # Other transactions wait until this completes
    user.balance += amount
    
    tx.table("users")
        .where("id", user_id)
        .update({balance: user.balance})
})

# Shared lock - allows reads but prevents updates
@db.transaction((tx) => {
    products: tx.table("products")
        .where("category_id", category_id)
        .sharedLock()
        .get()
    
    # Can read but not update these products
    total_value: products.sum(p => p.price * p.stock)
    
    tx.table("reports").insert({
        category_id: category_id,
        total_value: total_value
    })
})

# Skip locked rows
@db.transaction((tx) => {
    # Get next available job, skip locked ones
    job: tx.table("jobs")
        .where("status", "pending")
        .lockForUpdate()
        .skipLocked()
        .first()
    
    if (job) {
        # Process job
        tx.table("jobs")
            .where("id", job.id)
            .update({status: "processing"})
    }
})
```

## Optimistic Locking

```tusk
# Using version column
class Product extends Model {
    # Enable optimistic locking
    uses: [OptimisticLocking]
    
    # Version column (default: 'version')
    versionColumn: "version"
}

# Usage
product: @Product.find(1)
original_version: product.version

# Simulate concurrent update
product.price: 99.99

try {
    product.save()  # Checks version hasn't changed
} catch (StaleModelException e) {
    # Another process updated the record
    # Reload and retry
    product.refresh()
    product.price: 99.99
    product.save()
}

# Manual optimistic locking
@db.transaction((tx) => {
    product: tx.table("products")
        .where("id", product_id)
        .first()
    
    original_version: product.version
    
    # Perform calculations
    new_price: calculate_price(product)
    
    # Update with version check
    affected: tx.table("products")
        .where("id", product_id)
        .where("version", original_version)
        .update({
            price: new_price,
            version: original_version + 1
        })
    
    if (affected == 0) {
        throw "Product was modified by another process"
    }
})
```

## Distributed Transactions

```tusk
# Two-phase commit across multiple databases
@db.distributedTransaction([
    "mysql" => (tx) => {
        tx.table("orders").insert(order_data)
    },
    
    "postgres" => (tx) => {
        tx.table("inventory").decrement("stock", quantity)
    },
    
    "mongodb" => (tx) => {
        tx.collection("analytics").insert(analytics_data)
    }
])

# Manual distributed transaction
mysql_tx: @db.connection("mysql").beginTransaction()
postgres_tx: @db.connection("postgres").beginTransaction()

try {
    # Phase 1: Prepare
    mysql_tx.prepare()
    postgres_tx.prepare()
    
    # Phase 2: Commit
    mysql_tx.commit()
    postgres_tx.commit()
    
} catch (e) {
    # Rollback all
    mysql_tx.rollback()
    postgres_tx.rollback()
    throw e
}
```

## Transaction Events and Hooks

```tusk
# Transaction events
@db.beforeTransaction((connection) => {
    @log.info("Transaction starting", {
        connection: connection.getName()
    })
})

@db.afterCommit((connection) => {
    @log.info("Transaction committed")
    @cache.flush()  # Clear cache after successful transaction
})

@db.afterRollback((connection, exception) => {
    @log.error("Transaction rolled back", {
        error: exception.message
    })
    @alert.send("Transaction failure", exception)
})

# Model transaction hooks
class Order extends Model {
    boot() {
        super.boot()
        
        # Only execute after transaction commits
        @afterCommit((order) => {
            @queue.push("process_order", order)
            @email.send("order_confirmation", order)
        })
        
        # Clean up on rollback
        @afterRollback((order) => {
            @storage.delete(order.temp_files)
        })
    }
}
```

## Deadlock Handling

```tusk
# Automatic retry on deadlock
@db.transactionWithRetry((tx) => {
    # Complex operations that might deadlock
    account1: tx.table("accounts")
        .where("id", from_id)
        .lockForUpdate()
        .first()
    
    account2: tx.table("accounts")
        .where("id", to_id)
        .lockForUpdate()
        .first()
    
    # Transfer funds
    tx.table("accounts")
        .where("id", from_id)
        .decrement("balance", amount)
    
    tx.table("accounts")
        .where("id", to_id)
        .increment("balance", amount)
}, {
    retries: 3,
    delay: 100,  # milliseconds
    on_retry: (attempt, error) => {
        @log.warning("Deadlock detected, retry " + attempt)
    }
})

# Manual deadlock handling
max_retries: 3
attempt: 0

while (attempt < max_retries) {
    try {
        @db.transaction((tx) => {
            # Transaction logic
        })
        break  # Success
        
    } catch (DeadlockException e) {
        attempt++
        if (attempt >= max_retries) {
            throw e
        }
        @sleep(attempt * 100)  # Exponential backoff
    }
}
```

## Transaction Performance

```tusk
# Batch operations in transaction
@db.transaction((tx) => {
    # Bad: Individual inserts
    // for (user in users) {
    //     tx.table("users").insert(user)
    // }
    
    # Good: Batch insert
    tx.table("users").insert(users)
    
    # For large datasets, chunk
    users.chunk(1000).each(chunk => {
        tx.table("users").insert(chunk)
    })
})

# Connection pooling for transactions
@db.transactionPool({
    connections: 5,
    handler: (tx, item) => {
        # Process item in parallel transactions
        tx.table("processed").insert({
            data: @process(item),
            processed_at: @now()
        })
    },
    items: large_dataset
})

# Read-only transactions
@db.readTransaction((tx) => {
    # Optimized for read operations
    # May use read replicas
    report_data: tx.table("sales")
        .select(@db.raw("DATE(created_at) as date, SUM(amount) as total"))
        .groupBy("date")
        .get()
    
    return report_data
})
```

## Testing with Transactions

```tusk
# Wrap tests in transactions
#test "User creation" {
    @db.beginTransaction()
    
    user: @User.create({
        name: "Test User",
        email: "test@example.com"
    })
    
    @assert.true(user.exists)
    @assert.equals(user.name, "Test User")
    
    # Rollback to keep database clean
    @db.rollback()
}

# Test helper
testInTransaction: (callback) => {
    @db.beginTransaction()
    
    try {
        result: callback()
        return result
    } finally {
        @db.rollback()
    }
}

#test "Complex operation" {
    testInTransaction(() => {
        # All database changes will be rolled back
        order: @create_order_with_items()
        @assert.equals(order.items.count(), 3)
    })
}
```

## Best Practices

1. **Keep transactions short** - Hold locks for minimal time
2. **Order operations consistently** - Prevent deadlocks
3. **Use appropriate isolation level** - Balance consistency and performance
4. **Handle failures gracefully** - Always have rollback logic
5. **Avoid user interaction** - Don't wait for input during transaction
6. **Test transaction boundaries** - Ensure atomicity
7. **Monitor long transactions** - Set timeouts and alerts
8. **Use read replicas** - For read-only transactions

## Related Topics

- `database-overview` - Database configuration
- `query-builder` - Building queries
- `orm-models` - Model transactions
- `pessimistic-locking` - Locking strategies
- `database-performance` - Optimization tips