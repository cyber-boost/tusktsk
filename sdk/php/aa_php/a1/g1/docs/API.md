# API Documentation - Enhanced Database Operators

## Enhanced MySQL Operator API

### Class: `EnhancedMySqlOperator`

#### Constructor

```php
public function __construct(array $config = [])
```

**Parameters:**
- `$config` (array): Configuration options
  - `debug` (bool): Enable debug logging (default: false)
  - `max_connections` (int): Maximum pool connections (default: 20)
  - `connection_timeout` (int): Connection timeout in seconds (default: 30)

#### Connection Methods

##### `connect(array $params): bool`

Establish a pooled database connection.

**Parameters:**
- `host` (string): Database host (default: 'localhost')
- `port` (int): Database port (default: 3306)
- `database` (string): Database name
- `username` (string): Database username
- `password` (string): Database password
- `charset` (string): Character set (default: 'utf8mb4')
- `ssl` (bool): Enable SSL (default: false)

**Returns:** `bool` - Success status

**Example:**
```php
$success = $mysql->execute('connect', [
    'host' => 'localhost',
    'database' => 'myapp',
    'username' => 'user',
    'password' => 'pass'
]);
```

##### `disconnect(array $params): bool`

Release connection back to pool.

**Returns:** `bool` - Success status

##### `health_check(): array`

Check connection and pool health.

**Returns:** `array` - Health status information
- `status` (string): 'healthy' or 'unhealthy'
- `connection_test` (bool): Connection test result
- `pool_status` (array): Pool statistics
- `errors` (array): Any health errors

##### `pool_status(): array`

Get connection pool statistics.

**Returns:** `array` - Pool status for each connection key
- `total_connections` (int): Total connections in pool
- `active_connections` (int): Currently active connections
- `available_connections` (int): Available connections

#### Query Methods

##### `query(array $params): array`

Execute SELECT query with optional parameter binding.

**Parameters:**
- `sql` (string): SQL query
- `bindings` (array): Parameter bindings (optional)
- `fetch_mode` (int): PDO fetch mode (default: PDO::FETCH_ASSOC)

**Returns:** `array` - Query results

**Example:**
```php
$results = $mysql->execute('query', [
    'sql' => 'SELECT * FROM users WHERE status = ? AND created_at > ?',
    'bindings' => ['active', '2024-01-01']
]);
```

##### `scalar(array $params): mixed`

Execute query and return single scalar value.

**Parameters:**
- `sql` (string): SQL query
- `bindings` (array): Parameter bindings (optional)

**Returns:** `mixed` - Single scalar value

##### `execute(array $params): int`

Execute non-SELECT query (INSERT, UPDATE, DELETE).

**Parameters:**
- `sql` (string): SQL statement
- `bindings` (array): Parameter bindings (optional)

**Returns:** `int` - Number of affected rows

##### `prepare(array $params): PDOStatement`

Prepare a statement for multiple executions.

**Parameters:**
- `sql` (string): SQL statement to prepare

**Returns:** `PDOStatement` - Prepared statement object

#### Transaction Methods

##### `begin(array $params = []): bool`

Start a new transaction.

**Parameters:**
- `timeout` (int): Transaction timeout in seconds (default: 30)

**Returns:** `bool` - Success status

##### `commit(): bool`

Commit the current transaction.

**Returns:** `bool` - Success status

##### `rollback(): bool`

Rollback the current transaction.

**Returns:** `bool` - Success status

##### `savepoint(string $name = null): string`

Create a transaction savepoint.

**Parameters:**
- `$name` (string): Savepoint name (auto-generated if not provided)

**Returns:** `string` - Savepoint name

##### `rollback_to(array $params): bool`

Rollback to a specific savepoint.

**Parameters:**
- `savepoint` (string): Savepoint name

**Returns:** `bool` - Success status

#### JSON Methods

##### `json_extract(array $params): mixed`

Extract values from JSON column.

**Parameters:**
- `table` (string): Table name
- `column` (string): JSON column name
- `path` (string): JSON path (e.g., '$.user.name')
- `where` (array): WHERE conditions (optional)

**Returns:** `mixed` - Extracted JSON value

##### `json_contains(array $params): bool`

Check if JSON column contains a value.

**Parameters:**
- `table` (string): Table name
- `column` (string): JSON column name
- `value` (mixed): Value to search for
- `where` (array): WHERE conditions (optional)

**Returns:** `bool` - Contains result

##### `json_set(array $params): bool`

Update JSON column value.

**Parameters:**
- `table` (string): Table name
- `column` (string): JSON column name
- `path` (string): JSON path
- `value` (mixed): New value
- `where` (array): WHERE conditions

**Returns:** `bool` - Success status

#### Search Methods

##### `fulltext_search(array $params): array`

Perform full-text search.

**Parameters:**
- `table` (string): Table name
- `column` (string): Column to search
- `query` (string): Search query
- `where` (array): Additional WHERE conditions (optional)
- `limit` (int): Result limit (default: 100)

**Returns:** `array` - Search results

##### `like_search(array $params): array`

Perform LIKE-based search.

**Parameters:**
- `table` (string): Table name
- `column` (string): Column to search
- `pattern` (string): LIKE pattern
- `where` (array): Additional conditions (optional)

**Returns:** `array` - Search results

#### Maintenance Methods

##### `optimize_table(array $params): bool`

Optimize a table.

**Parameters:**
- `table` (string): Table name

**Returns:** `bool` - Success status

##### `repair_table(array $params): bool`

Repair a table.

**Parameters:**
- `table` (string): Table name

**Returns:** `bool` - Success status

##### `show_status(array $params = []): array`

Show MySQL server status.

**Returns:** `array` - Status variables

##### `show_variables(array $params = []): array`

Show MySQL server variables.

**Returns:** `array` - Server variables

#### Monitoring Methods

##### `performance_metrics(): array`

Get performance metrics.

**Returns:** `array` - Performance data by date and operation
- Per operation: count, success_count, total_time, min_time, max_time, avg_time

##### `clear_cache(): array`

Clear all caches.

**Returns:** `array` - Cache clear statistics
- `prepared_statements` (int): Number cleared
- `performance_metrics` (int): Metrics cleared

## Enhanced PostgreSQL Operator API

### Class: `EnhancedPostgreSqlOperator`

#### Constructor

```php
public function __construct(array $config = [])
```

**Parameters:** Same as MySQL operator

#### JSON/JSONB Methods

##### `json_get(array $params): mixed`

Extract value from JSON column using path.

**Parameters:**
- `table` (string): Table name
- `column` (string): JSON column name
- `path` (string): JSON path (e.g., 'name' or 'user.profile.name')
- `where` (array): WHERE conditions (optional)

**Returns:** `mixed` - Extracted value

##### `jsonb_contains(array $params): bool`

Check if JSONB column contains a value using the @> operator.

**Parameters:**
- `table` (string): Table name
- `column` (string): JSONB column name
- `value` (mixed): Value to check for containment
- `where` (array): WHERE conditions (optional)

**Returns:** `bool` - Containment result

##### `jsonb_path(array $params): array`

Execute JSONPath query on JSONB column.

**Parameters:**
- `table` (string): Table name
- `column` (string): JSONB column name
- `path` (string): JSONPath expression
- `where` (array): WHERE conditions (optional)

**Returns:** `array` - Query results

##### `json_each(array $params): array`

Expand JSON object to key-value pairs.

**Parameters:**
- `table` (string): Table name
- `column` (string): JSON column name

**Returns:** `array` - Key-value pairs

##### `json_object_keys(array $params): array`

Get keys from JSON object.

**Parameters:**
- `table` (string): Table name
- `column` (string): JSON column name

**Returns:** `array` - Object keys

#### Array Methods

##### `array_contains(array $params): bool`

Check if array contains a value.

**Parameters:**
- `table` (string): Table name
- `column` (string): Array column name
- `value` (mixed): Value to search for
- `where` (array): WHERE conditions (optional)

**Returns:** `bool` - Contains result

##### `array_length(array $params): int`

Get array length.

**Parameters:**
- `table` (string): Table name
- `column` (string): Array column name
- `dimension` (int): Array dimension (default: 1)
- `where` (array): WHERE conditions (optional)

**Returns:** `int` - Array length

##### `array_append(array $params): bool`

Append value to array.

**Parameters:**
- `table` (string): Table name
- `column` (string): Array column name
- `value` (mixed): Value to append
- `where` (array): WHERE conditions

**Returns:** `bool` - Success status

##### `array_position(array $params): int`

Find position of value in array.

**Parameters:**
- `table` (string): Table name
- `column` (string): Array column name
- `value` (mixed): Value to find
- `where` (array): WHERE conditions (optional)

**Returns:** `int` - Position (1-based) or 0 if not found

##### `unnest(array $params): array`

Expand array to rows.

**Parameters:**
- `table` (string): Table name
- `column` (string): Array column name
- `where` (array): WHERE conditions (optional)

**Returns:** `array` - Expanded rows

#### LISTEN/NOTIFY Methods

##### `listen(array $params): bool`

Listen for notifications on a channel.

**Parameters:**
- `channel` (string): Channel name
- `callback` (callable): Callback function (optional)

**Returns:** `bool` - Success status

##### `unlisten(array $params): bool`

Stop listening to a channel.

**Parameters:**
- `channel` (string): Channel name or '*' for all

**Returns:** `bool` - Success status

##### `notify(array $params): bool`

Send notification to a channel.

**Parameters:**
- `channel` (string): Channel name
- `payload` (string): Message payload (optional)

**Returns:** `bool` - Success status

##### `get_notifications(array $params = []): array`

Get pending notifications.

**Parameters:**
- `timeout` (int): Timeout in seconds (optional)

**Returns:** `array` - Array of notifications
- Each notification: `[channel, pid, payload, timestamp]`

##### `poll_notifications(array $params = []): array`

Poll for notifications with timeout.

**Parameters:**
- `timeout` (int): Timeout in seconds (default: 10)

**Returns:** `array` - Array of notifications received during timeout

#### Full-Text Search Methods

##### `fulltext_search(array $params): array`

Enhanced full-text search with ranking.

**Parameters:**
- `table` (string): Table name
- `column` (string): Column to search
- `query` (string): Search query
- `config` (string): Text search configuration (default: 'english')
- `rank` (bool): Include relevance ranking (default: false)
- `where` (array): Additional conditions (optional)
- `limit` (int): Result limit (default: 100)

**Returns:** `array` - Search results with optional ranking

##### `trigram_search(array $params): array`

Similarity search using trigrams.

**Parameters:**
- `table` (string): Table name
- `column` (string): Column to search
- `query` (string): Search query
- `threshold` (float): Similarity threshold (default: 0.3)
- `limit` (int): Result limit (default: 100)

**Returns:** `array` - Results with similarity scores

#### Window Function Methods

##### `window_function(array $params): array`

Execute query with window functions.

**Parameters:**
- `sql` (string): SQL query with window functions
- `bindings` (array): Parameter bindings (optional)

**Returns:** `array` - Query results

#### Common Table Expression Methods

##### `with_query(array $params): array`

Execute WITH query (CTE).

**Parameters:**
- `sql` (string): SQL query with WITH clause
- `bindings` (array): Parameter bindings (optional)

**Returns:** `array` - Query results

##### `recursive_query(array $params): array`

Execute recursive WITH query.

**Parameters:**
- `sql` (string): SQL query with recursive WITH clause
- `bindings` (array): Parameter bindings (optional)

**Returns:** `array` - Query results

#### Type Management Methods

##### `create_enum(array $params): bool`

Create an enum type.

**Parameters:**
- `name` (string): Enum type name
- `values` (array): Array of enum values

**Returns:** `bool` - Success status

##### `create_domain(array $params): bool`

Create a domain type.

**Parameters:**
- `name` (string): Domain name
- `type` (string): Base type
- `constraint` (string): Domain constraint (optional)

**Returns:** `bool` - Success status

#### Monitoring Methods

##### `pg_stat_activity(array $params = []): array`

Get PostgreSQL activity statistics.

**Returns:** `array` - Active connections and queries

##### `pg_stat_database(array $params = []): array`

Get database statistics.

**Returns:** `array` - Database performance statistics

##### `pg_locks(array $params = []): array`

Get lock information.

**Returns:** `array` - Current locks and blocked queries

## Database Transaction Manager API

### Class: `DatabaseTransactionManager`

#### Constructor

```php
public function __construct(PDO $connection, array $config = [])
```

**Parameters:**
- `$connection` (PDO): Database connection
- `$config` (array): Configuration options
  - `timeout` (int): Transaction timeout in seconds (default: 30)
  - `isolation_level` (string): Isolation level (default: 'READ_COMMITTED')
  - `auto_retry_deadlock` (bool): Auto-retry on deadlocks (default: true)

#### Transaction Control Methods

##### `begin(): bool`

Start a new transaction.

**Returns:** `bool` - Success status

##### `commit(): bool`

Commit the current transaction.

**Returns:** `bool` - Success status

##### `rollback(): bool`

Rollback the current transaction.

**Returns:** `bool` - Success status

##### `savepoint(string $savepointName = null): string`

Create a savepoint.

**Parameters:**
- `$savepointName` (string): Savepoint name (auto-generated if null)

**Returns:** `string` - Savepoint name

##### `rollbackToSavepoint(string $savepointName): bool`

Rollback to a specific savepoint.

**Parameters:**
- `$savepointName` (string): Savepoint name

**Returns:** `bool` - Success status

##### `releaseSavepoint(string $savepointName): bool`

Release a savepoint.

**Parameters:**
- `$savepointName` (string): Savepoint name

**Returns:** `bool` - Success status

#### Execution Methods

##### `executeWithRetry(callable $operation, int $maxRetries = null): mixed`

Execute operation with automatic deadlock retry.

**Parameters:**
- `$operation` (callable): Operation to execute
- `$maxRetries` (int): Maximum retry attempts (default: 3)

**Returns:** `mixed` - Operation result

**Example:**
```php
$result = $txn->executeWithRetry(function() use ($pdo) {
    return $pdo->prepare('UPDATE accounts SET balance = balance - ? WHERE id = ?')
               ->execute([100, $accountId]);
}, 5);
```

#### Monitoring Methods

##### `getStatus(): array`

Get current transaction status.

**Returns:** `array` - Status information
- `transaction_id` (string): Unique transaction ID
- `is_active` (bool): Transaction active status
- `start_time` (float): Transaction start timestamp
- `duration` (float): Current duration in seconds
- `timeout_seconds` (int): Timeout setting
- `time_remaining` (float): Time remaining before timeout
- `isolation_level` (string): Isolation level
- `savepoint_count` (int): Number of active savepoints
- `operation_count` (int): Number of logged operations
- `savepoints` (array): Savepoint details
- `has_timed_out` (bool): Timeout status

##### `getOperationLog(): array`

Get transaction operation log.

**Returns:** `array` - Array of logged operations
- Each entry: `[timestamp, operation, message, context, transaction_id]`

##### `static getGlobalStatistics(): array`

Get global transaction statistics.

**Returns:** `array` - Global statistics
- `active_transactions` (int): Currently active transactions
- `total_transactions` (int): Total transactions created
- `transaction_history_count` (int): Completed transactions
- `deadlock_statistics` (array): Deadlock statistics by date
- `active_transaction_details` (array): Details of active transactions

##### `static cleanupTimedOutTransactions(): int`

Clean up timed-out transactions.

**Returns:** `int` - Number of transactions cleaned up

## Error Handling

All methods throw appropriate exceptions:

- `PDOException` - Database-related errors
- `Exception` - General operation errors
- `InvalidArgumentException` - Invalid parameters

### Common Error Codes

#### MySQL Error Codes
- `1213` - Deadlock found
- `2002` - Connection refused
- `2006` - Server has gone away

#### PostgreSQL Error Codes
- `40001` - Serialization failure
- `40P01` - Deadlock detected
- `08000` - Connection exception

## Usage Examples

### Basic MySQL Usage

```php
$mysql = new EnhancedMySqlOperator(['debug' => true]);

$mysql->execute('connect', [
    'host' => 'localhost',
    'database' => 'myapp',
    'username' => 'user',
    'password' => 'pass'
]);

// Simple query
$users = $mysql->execute('query', [
    'sql' => 'SELECT * FROM users WHERE active = ?',
    'bindings' => [1]
]);

// Transaction with savepoints
$mysql->execute('begin');
try {
    $mysql->execute('execute', [
        'sql' => 'UPDATE accounts SET balance = balance - ? WHERE id = ?',
        'bindings' => [100, 1]
    ]);
    
    $sp = $mysql->execute('savepoint');
    
    $mysql->execute('execute', [
        'sql' => 'UPDATE accounts SET balance = balance + ? WHERE id = ?',
        'bindings' => [100, 2]
    ]);
    
    $mysql->execute('commit');
} catch (Exception $e) {
    $mysql->execute('rollback');
    throw $e;
}
```

### PostgreSQL with JSON and Arrays

```php
$postgres = new EnhancedPostgreSqlOperator();

// JSON operations
$userProfile = $postgres->execute('jsonb_path', [
    'table' => 'users',
    'column' => 'profile',
    'path' => '$.preferences.notifications'
]);

// Array operations
$tags = $postgres->execute('array_append', [
    'table' => 'posts',
    'column' => 'tags',
    'value' => 'featured',
    'where' => ['id' => 123]
]);

// LISTEN/NOTIFY
$postgres->execute('listen', [
    'channel' => 'notifications',
    'callback' => function($notification) {
        echo "Received notification: " . $notification['payload'];
    }
]);

$postgres->execute('notify', [
    'channel' => 'notifications',
    'payload' => 'New order received'
]);
```

### Transaction Manager Usage

```php
$txn = new DatabaseTransactionManager($pdo, [
    'timeout' => 60,
    'isolation_level' => 'SERIALIZABLE'
]);

$txn->begin();

$result = $txn->executeWithRetry(function() use ($pdo) {
    // Complex operation that might deadlock
    $stmt1 = $pdo->prepare('UPDATE table1 SET value = ? WHERE id = ?');
    $stmt1->execute([10, 1]);
    
    $stmt2 = $pdo->prepare('UPDATE table2 SET value = ? WHERE id = ?');
    $stmt2->execute([20, 2]);
    
    return true;
});

$txn->commit();

// Check performance
$stats = DatabaseTransactionManager::getGlobalStatistics();
echo "Active transactions: " . $stats['active_transactions'];
``` 