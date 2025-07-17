# 🤖 HOW-TO-AI.md - FUJSEN & TuskLang Guide for AI Systems

## 🎯 QUICK START FOR AI ASSISTANTS

When working with this codebase, you are dealing with **FUJSEN** - an intelligent configuration system that extends **TuskLang** with database integration and web endpoints.

### 🔥 CORE PHILOSOPHY
**"FUCK JSON, FUCK ENV, FUCK YAML"** - We use `.tsk` files (TuskLang) for ALL configuration because they are:
- **Human-readable** like YAML but better
- **Intelligent** with @ operators for dynamic data
- **Web-enabled** with `#!api` directive for HTTP endpoints
- **Database-connected** with Parse-style queries

## 📁 PROJECT STRUCTURE

```
fujsen/                    # FUJSEN Sprint Implementation
├── src/
│   ├── TuskLang.php      # Core parser (857 lines)
│   ├── TuskLangQueryBridge.php    # Database integration
│   └── TuskLangWebHandler.php     # Web endpoint engine
├── api/                  # .tsk files as HTTP endpoints
│   ├── echo.tsk         # Demo endpoint
│   ├── status.tsk       # System health
│   └── users.tsk        # Database-driven API
├── api-router.php       # Routes HTTP → .tsk files
└── autoload.php         # Component loader

reference/               # Original TuskLang files
├── TuskLang.php        # Original parser
├── install-tusklang.sh # Installation script
├── CLAUDE.md           # AI instructions (legacy)
└── db-energy/          # Parse-style ORM
```

## 🚀 FUJSEN @ OPERATORS (The Magic!)

### Database Operations
```tsk
users: @Query("users").equalTo("status", "active").findAll()
user_count: @Query("users").count()
posts: @Query("posts").greaterThan("created_at", "2025-01-01").limit(10)
```

### Intelligence & Caching
```tsk
cached_data: @cache("5m", expensive_operation)
performance: @metrics("api_response_time", 150)
learned_setting: @learn("optimal_workers", 4)
optimized_value: @optimize("cache_size", 1024)
```

### Web Functions (FUJSEN Extension)
```tsk
#!api
method: @request.method
params: @request.query
response: @json({message: "Hello from .tsk file!"})
```

## 🌐 WEB ENDPOINTS

FUJSEN allows `.tsk` files to serve HTTP requests:

1. **Add `#!api` directive** at top of .tsk file
2. **Use @ operators** for dynamic data
3. **Return responses** with `@json()`, `@render()`, `@redirect()`
4. **Access via HTTP** through api-router.php

### Example API Endpoint (api/hello.tsk):
```tsk
#!api
name: @request.query.name || "World"
time: php(time())

@json({
    message: "Hello " + name + "!",
    timestamp: time,
    server: "FUJSEN"
})
```

## 🗄️ DATABASE INTEGRATION

FUJSEN uses **Parse-style ORM** (db-energy):
- **TuskQuery** - Fluent query builder
- **TuskObject** - Object management
- **No raw SQL** - Everything through @ operators

### Query Examples:
```tsk
# Instead of: SELECT * FROM users WHERE active = 1
active_users: @Query("users").equalTo("active", true).findAll()

# Instead of: SELECT COUNT(*) FROM posts WHERE author_id = 5
post_count: @Query("posts").equalTo("author_id", 5).count()

# Complex queries
recent_posts: @Query("posts")
    .greaterThan("created_at", "2025-01-01")
    .lessThan("created_at", "2025-12-31")
    .limit(10)
    .findAll()
```

## 🔧 DEVELOPMENT WORKFLOW

### 1. Running FUJSEN Server
```bash
cd fujsen/
php -S localhost:8874 api-router.php
```

### 2. Testing Endpoints
```bash
curl http://localhost:8874/echo
curl http://localhost:8874/status
curl http://localhost:8874/users
```

### 3. Creating New Endpoints
1. Create `api/myendpoint.tsk`
2. Add `#!api` directive
3. Use @ operators for logic
4. Access via `http://localhost:8874/myendpoint`

### 4. Testing Integration
```bash
php test-fujsen.php        # Test @ operators
php test-web-endpoints.php # Test web functionality
php test-database.php      # Test database queries
```

## 🎯 COMMON TASKS FOR AI

### Adding New @ Operator
1. **Edit** `src/TuskLangQueryBridge.php`
2. **Add handler** in `handleOperator()` method
3. **Test** with `test-fujsen.php`

### Creating API Endpoint
1. **Create** `api/newendpoint.tsk`
2. **Add** `#!api` directive
3. **Use** @ operators for logic
4. **Test** via HTTP

### Database Queries
- **Use** `@Query("table").method().findAll()`
- **Chain** methods like Parse/Firebase
- **No SQL** - everything through @ operators

## 🚨 CRITICAL CONCEPTS

### 1. File Extensions Matter
- `.tsk` = TuskLang configuration
- `.php` = PHP code
- `.md` = Documentation

### 2. @ Operators vs PHP
- `@Query()` = FUJSEN database operator
- `php(time())` = Execute PHP expression
- `env("VAR")` = Environment variable

### 3. Web vs Config
- `#!api` = Web endpoint
- No directive = Configuration file

### 4. Architecture Layers
1. **TuskLang** - Base parser (.tsk syntax)
2. **QueryBridge** - Database integration
3. **WebHandler** - HTTP functionality
4. **Router** - Request routing

## 🔥 FUJSEN PHILOSOPHY

**FUJSEN** = **F**uck **U** **J**SON **S**QL **E**nv **N**odes

We believe configuration should be:
- **Intelligent** (learns and adapts)
- **Live** (pulls data from databases)
- **Web-enabled** (serves HTTP requests)
- **Human-readable** (developers love it)

## 🎉 SUCCESS METRICS

If you see these working, FUJSEN is successful:
- `.tsk` files serving HTTP requests
- Database queries in configuration syntax
- Intelligent caching and learning
- 90%+ test success rate

## 🚀 INTEGRATION WITH TUSK FRAMEWORK

**FUJSEN** enhances the existing **TUSK** ecosystem:
- **TUSK** (port 8875) = Web application compiler
- **FUJSEN** (port 8874) = Intelligent configuration system
- **TSK** first, then TUSK framework integration
- **Backward compatible** with existing .tsk files

**FUJSEN is the configuration brain, TUSK is the application muscle!** 💪🧠 