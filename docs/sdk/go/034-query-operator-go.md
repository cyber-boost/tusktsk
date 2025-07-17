# @query Operator in TuskLang - Go Guide

## 🗄️ **Database Power in Your Config: @query Unleashed**

TuskLang's `@query` operator is pure database rebellion. We don't bow to any king—not even SQL. Here's how to use `@query` in Go projects to inject live database data directly into your configuration.

## 📋 **Table of Contents**
- [What is @query?](#what-is-query)
- [Basic Usage](#basic-usage)
- [Database Connections](#database-connections)
- [Query Types](#query-types)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🔍 **What is @query?**

The `@query` operator executes SQL queries and injects the results directly into your config. No more static data—just pure, live database power.

## 🛠️ **Basic Usage**

```go
[stats]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE status = 'active'")
```

## 🔌 **Database Connections**

```go
[database]
connection: "postgres://user:pass@localhost/db"
user_count: @query("SELECT COUNT(*) FROM users", connection)
```

## 📊 **Query Types**

### **Count Queries**
```go
[metrics]
total_users: @query("SELECT COUNT(*) FROM users")
```

### **Single Value Queries**
```go
[settings]
max_connections: @query("SELECT value FROM settings WHERE key = 'max_connections'")
```

### **JSON Queries**
```go
[config]
user_stats: @query("SELECT json_agg(u.*) FROM users u WHERE active = true")
```

## 🔗 **Go Integration**

```go
userCount := config.GetInt("user_count")
maxConnections := config.GetInt("max_connections")
```

### **Database Connection Handling**
```go
db, err := sql.Open("postgres", connectionString)
if err != nil {
    log.Fatal(err)
}
defer db.Close()

var count int
err = db.QueryRow("SELECT COUNT(*) FROM users").Scan(&count)
```

## 🥇 **Best Practices**
- Use parameterized queries for security
- Cache expensive queries with @cache
- Handle database connection errors gracefully
- Use transactions for complex operations

---

**TuskLang: Your config is alive with @query.** 