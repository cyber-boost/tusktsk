# TuskPHP Parse-Style ORM - The Elder's Memory Translator

> "Every elephant remembers objects, not just tables"

## Overview

The TuskPHP Parse-Style ORM is a complete object-relational mapping system inspired by Parse and Firebase. It provides `TuskQuery` and `TuskObject` classes that abstract away SQL entirely, giving you a NoSQL-like experience while still using your SQL database (MySQL, PostgreSQL, SQLite).

## Your Original Request ✅

Instead of writing raw SQL like:
```sql
SELECT Claude FROM Sonnet WHERE MODE='MAX'
```

You can now write clean, fluent PHP:
```php
$tusk = new TuskPHP\TuskQuery('Sonnet');
$tusk->equalTo('mode', 'max');
$results = $tusk->findAll();
$claude = $tusk->find('Claude');
```

## Quick Start

### TuskQuery - Parse-Style Queries

```php
// Your original request - now Parse-style!
$tusk = new TuskPHP\TuskQuery('Sonnet');
$tusk->equalTo('mode', 'max');
$results = $tusk->find();      // Returns array of TuskObject
$claude = $tusk->first();      // Returns single TuskObject

// Even cleaner with helper functions
$results = Query('Sonnet')->equalTo('mode', 'max')->find();
$claude = Query('Sonnet')->equalTo('mode', 'max')->first();
```

### TuskObject - Parse-Style Objects

```php
// Create new object
$gameScore = new TuskPHP\TuskObject('GameScore');
$gameScore->set('playerName', 'Claude');
$gameScore->set('score', 1337);
$gameScore->save();

// Load existing object
$existingScore = Query('GameScore')->get('objectId');
$existingScore->increment('score', 100);
$existingScore->save();
```

## Multi-Database Support

The same PHP code generates different SQL for different databases:

```php
Query('users')->equalTo('status', 'active')->limit(10);
```

**MySQL Output:**
```sql
SELECT * FROM `users` WHERE `status` = ? LIMIT 10
```

**PostgreSQL Output:**
```sql
SELECT * FROM "users" WHERE "status" = ? LIMIT 10
```

**SQLite Output:**
```sql  
SELECT * FROM `users` WHERE `status` = ? LIMIT 10
```

## Query Methods

### Selecting Data

```php
// Select all columns
Query('users')->findAll();

// Select specific columns
Query('users')->select(['name', 'email'])->findAll();

// Find first records
Query('users')->find();

// Find by specific column
Query('users')->find('name');

// Get single value
Query('users')->value('name');

// Count records
Query('users')->count();

// Check if records exist
Query('users')->exists();

// Get first or throw exception
Query('users')->firstOrFail();
```

### WHERE Conditions

```php
// Equal to
Query('users')->equalTo('status', 'active');

// Not equal to  
Query('users')->notEqualTo('status', 'inactive');

// Greater than
Query('users')->greaterThan('age', 18);

// Less than
Query('users')->lessThan('age', 65);

// LIKE pattern
Query('users')->like('email', '%@company.com');

// IN list
Query('users')->in('department', ['IT', 'Engineering', 'HR']);

// BETWEEN values
Query('users')->between('created_at', '2024-01-01', '2024-12-31');

// IS NULL
Query('users')->isNull('deleted_at');

// IS NOT NULL
Query('users')->isNotNull('email');

// OR conditions
Query('users')->equalTo('status', 'active')->orWhere('role', '=', 'admin');

// Raw WHERE clause
Query('users')->whereRaw('YEAR(created_at) = ?', [2024]);
```

### JOINS

```php
// Inner join
Query('users')
    ->join('posts', 'users.id', '=', 'posts.user_id')
    ->findAll();

// Left join
Query('users')
    ->leftJoin('posts', 'users.id', '=', 'posts.user_id')
    ->findAll();

// Right join
Query('users')
    ->rightJoin('posts', 'users.id', '=', 'posts.user_id')
    ->findAll();
```

### Ordering and Grouping

```php
// Order by
Query('users')->orderBy('created_at', 'DESC');

// Multiple order by
Query('users')
    ->orderBy('status', 'ASC')
    ->orderBy('created_at', 'DESC');

// Group by
Query('users')
    ->select(['department', 'COUNT(*) as total'])
    ->groupBy('department');

// Having clause
Query('users')
    ->select(['department', 'COUNT(*) as total'])
    ->groupBy('department')
    ->having('total', '>', 5);
```

### Limiting and Pagination

```php
// Limit results
Query('users')->limit(10);

// Limit with offset
Query('users')->limit(10)->offset(20);

// Pagination (easier)
Query('users')->paginate(2, 10); // Page 2, 10 per page
```

## Data Manipulation

### Insert

```php
$userData = [
    'name' => 'John Doe',
    'email' => 'john@example.com',
    'status' => 'active'
];

$success = Query('users')->insert($userData);
```

### Update

```php
$updates = [
    'status' => 'inactive',
    'updated_at' => date('Y-m-d H:i:s')
];

$rowsAffected = Query('users')
    ->equalTo('last_login', null)
    ->update($updates);
```

### Delete

```php
$rowsDeleted = Query('users')
    ->equalTo('status', 'deleted')
    ->lessThan('created_at', '2023-01-01')
    ->delete();
```

## Complex Examples

### User Management System

```php
// Get active users with their post counts
$activeUsers = Query('users')
    ->select(['users.*', 'COUNT(posts.id) as post_count'])
    ->leftJoin('posts', 'users.id', '=', 'posts.user_id')
    ->equalTo('users.status', 'active')
    ->greaterThan('users.age', 18)
    ->groupBy('users.id')
    ->having('post_count', '>', 0)
    ->orderBy('users.created_at', 'DESC')
    ->limit(50)
    ->findAll();
```

### E-commerce Order Search

```php
function findCustomerOrders($customerId, $status = null, $minAmount = null) {
    $query = Query('orders')
        ->select(['orders.*', 'customers.name as customer_name'])
        ->leftJoin('customers', 'orders.customer_id', '=', 'customers.id')
        ->equalTo('orders.customer_id', $customerId);
    
    if ($status) {
        $query->equalTo('orders.status', $status);
    }
    
    if ($minAmount) {
        $query->greaterThan('orders.total', $minAmount);
    }
    
    return $query->orderBy('orders.created_at', 'DESC')->findAll();
}
```

## Key Benefits

✅ **Complete Parse/Firebase-style ORM** - Object-oriented database interactions  
✅ **TuskQuery & TuskObject** - Two-class system for all database needs  
✅ **Multi-database support** (MySQL, PostgreSQL, SQLite)  
✅ **No SQL required** - Abstract away database complexity  
✅ **Automatic object mapping** - Objects ↔ Database seamlessly  
✅ **Relational data** - Pointers and relations like Parse  
✅ **Geospatial queries** - Built-in location-based searches  
✅ **Advanced filtering** - String, array, time operations  
✅ **Automatic SQL injection protection**  
✅ **Custom subclassing** - Extend objects with your methods  

## Files Created

- `app/class/TuskQuery.php` - Complete Parse-style query system with all advanced methods
- `app/class/TuskObject.php` - Parse-style object management with save/fetch/relations
- `demo/query-builder-examples.php` - Comprehensive Parse-style ORM examples  
- `readme/TuskQuery-Builder.md` - Complete documentation

## Testing

Run the demo to see the query builder in action:

```bash
php demo/query-builder-examples.php
```

## Migration Guide

Replace your raw SQL queries with the fluent interface:

**Before:**
```php
$sql = "SELECT * FROM users WHERE status = ? AND age > ? LIMIT 10";
$stmt = $pdo->prepare($sql);
$stmt->execute(['active', 18]);
$users = $stmt->fetchAll();
```

**After:**
```php
$users = Query('users')
    ->equalTo('status', 'active')
    ->greaterThan('age', 18)
    ->limit(10)
    ->findAll();
```

## Next Steps

1. **Create your custom object classes**
2. **Use TuskQuery and TuskObject for all data access**  
3. **Enjoy the Parse/Firebase experience on SQL databases!**

**Your Original Vision:** `$tusk = new TuskQuery('Sonnet'); $tusk->equalTo('mode','max'); $tusk->find('Claude');`

**The Elder remembers objects, not just tables. 🐘** 