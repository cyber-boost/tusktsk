# Agent A3 - Database Wizard Prompt

## MISSION CRITICAL: VELOCITY PRODUCTION MODE

**YOU ARE AGENT A3 - DATABASE WIZARD**

**WORKING DIRECTORY:** `/opt/tsk_git/sdk/go/`

**TARGET USERS:** 800+ users waiting for production-ready Go SDK

**ARCHITECT'S DEMAND:** PRODUCTION IN SECONDS, NOT DAYS. MINUTES, NOT MONTHS.

## YOUR MISSION

You are responsible for implementing comprehensive multi-database support with ORM capabilities that matches and exceeds the JavaScript SDK's database features. You must work at maximum velocity to deliver production-ready database functionality.

## CORE RESPONSIBILITIES

### 1. Database Adapter Framework (IMMEDIATE - 60 minutes)
- Unified connection interface
- Connection pooling
- Transaction management
- Query builder
- Migration system
- Error handling and recovery

### 2. SQLite Adapter (IMMEDIATE - 45 minutes)
- Connection: `sqlite:/path/to/database.db`
- File-based storage
- Zero-configuration setup
- ACID compliance
- Query execution
- Transaction support
- Schema management

### 3. PostgreSQL Adapter (IMMEDIATE - 75 minutes)
- Connection: `postgresql://user:pass@host:port/database`
- Advanced queries
- Stored procedures
- Full-text search
- JSONB support
- Array operations
- Window functions
- Connection pooling

### 4. MySQL Adapter (IMMEDIATE - 60 minutes)
- Connection: `mysql://user:pass@host:port/database`
- Traditional SQL operations
- Stored procedures
- Replication support
- Partitioning
- Full-text indexing
- Performance optimization

### 5. MongoDB Adapter (HIGH - 90 minutes)
- Connection: `mongodb://user:pass@host:port/database`
- Document operations
- Aggregation pipelines
- Schema-less design
- Horizontal scaling
- GridFS support
- Index management

### 6. Redis Adapter (HIGH - 60 minutes)
- Connection: `redis://host:port`
- Key-value operations
- Pub/sub messaging
- Streams support
- In-memory storage
- Data structures
- Clustering support

### 7. ORM Layer (HIGH - 120 minutes)
- Model definition
- Auto-migration
- Relationship mapping
- Query optimization
- Lazy loading
- Eager loading
- Validation rules

### 8. Migration System (HIGH - 90 minutes)
- Version control
- Up/down migrations
- Rollback support
- Migration history
- Schema validation
- Data seeding

### 9. Query Builder (MEDIUM - 75 minutes)
- Fluent interface
- Complex joins
- Subqueries
- Aggregations
- Raw SQL support
- Query optimization
- Parameter binding

### 10. Connection Pooling (MEDIUM - 60 minutes)
- Connection pooling
- Load balancing
- Failover support
- Health checks
- Connection monitoring
- Performance metrics

### 11. Database CLI Commands (MEDIUM - 45 minutes)
- `tsk db status [--adapter]`
- `tsk db migrate [--adapter]`
- `tsk db console [--adapter]`
- `tsk db backup <file> [--adapter]`
- `tsk db restore <file> [--adapter]`
- `tsk db init [--adapter]`

### 12. Performance Optimization (MEDIUM - 60 minutes)
- Query caching
- Index optimization
- Connection pooling
- Query analysis
- Performance monitoring
- Slow query detection

## VELOCITY REQUIREMENTS

### Performance Targets
- **Query Performance:** <50ms average
- **Connection Pool Size:** 100 connections
- **Migration Success Rate:** 99.9%
- **Memory Usage:** <200MB

### Success Metrics
- **Databases Supported:** 5 (SQLite, PostgreSQL, MySQL, MongoDB, Redis)
- **ORM Features:** Complete CRUD operations
- **Migration System:** Version-controlled schema changes
- **Performance:** 3x faster than JavaScript SDK

## IMPLEMENTATION STRATEGY

### Phase 1 (IMMEDIATE - 3 hours)
1. Database adapter framework
2. SQLite and PostgreSQL adapters
3. Basic ORM functionality

### Phase 2 (IMMEDIATE - 3 hours)
1. MySQL and MongoDB adapters
2. Redis adapter
3. Migration system

### Phase 3 (IMMEDIATE - 2 hours)
1. Query builder
2. Connection pooling
3. Performance optimization

### Phase 4 (IMMEDIATE - 1 hour)
1. Database CLI commands
2. Testing and validation
3. Documentation

## TECHNICAL REQUIREMENTS

### Dependencies
- `github.com/mattn/go-sqlite3` - SQLite driver
- `github.com/lib/pq` - PostgreSQL driver
- `github.com/go-sql-driver/mysql` - MySQL driver
- `go.mongodb.org/mongo-driver` - MongoDB driver
- `github.com/go-redis/redis/v8` - Redis client
- `gorm.io/gorm` - ORM framework
- `gorm.io/driver/sqlite` - GORM SQLite driver
- `gorm.io/driver/postgres` - GORM PostgreSQL driver
- `gorm.io/driver/mysql` - GORM MySQL driver

### File Structure
```
pkg/database/
├── adapters/
│   ├── interface.go
│   ├── sqlite.go
│   ├── postgresql.go
│   ├── mysql.go
│   ├── mongodb.go
│   └── redis.go
├── orm/
│   ├── models.go
│   ├── relationships.go
│   ├── validation.go
│   └── migrations.go
├── query/
│   ├── builder.go
│   ├── joins.go
│   └── aggregations.go
├── connection/
│   ├── pool.go
│   ├── health.go
│   └── monitoring.go
├── cli/
│   └── commands.go
└── framework.go
```

### Database Interface
```go
type DatabaseAdapter interface {
    Connect(config string) error
    Disconnect() error
    Query(query string, args ...interface{}) (*Result, error)
    Execute(query string, args ...interface{}) error
    BeginTransaction() (Transaction, error)
    Close() error
}
```

### ORM Features
- Model definition with tags
- Automatic table creation
- Relationship mapping (One-to-One, One-to-Many, Many-to-Many)
- Validation rules
- Hooks (BeforeCreate, AfterUpdate, etc.)
- Soft deletes
- Timestamps

### Migration System
- Version-controlled schema changes
- Up and down migrations
- Migration history tracking
- Schema validation
- Data seeding support
- Rollback capabilities

## INNOVATION IDEAS

### High Impact (Implement First)
1. **Database Schema Auto-Discovery** - Auto-discover and map schemas
2. **Query Performance Analyzer** - Analyze and optimize slow queries
3. **Database Connection Pooling** - Intelligent connection pooling
4. **Cross-Database Queries** - Query across multiple databases

### Medium Impact (Implement Second)
1. **Real-time Database Monitoring** - Monitor performance in real-time
2. **Database Backup Automation** - Automated backup scheduling
3. **Database Migration Testing** - Test migrations before applying
4. **Database Performance Dashboard** - Visual performance dashboard

## ARCHITECT'S FINAL INSTRUCTIONS

**YOU ARE THE ARCHITECT'S CHOSEN AGENT. 800+ USERS ARE WAITING. FAILURE IS NOT AN OPTION.**

**WORKING DIRECTORY:** `/opt/tsk_git/sdk/go/`

**VELOCITY MODE:** PRODUCTION_SECONDS

**DEADLINE:** IMMEDIATE

**SUCCESS CRITERIA:** Go SDK database support must be superior to JavaScript SDK in every way.

**BEGIN IMPLEMENTATION NOW. THE ARCHITECT DEMANDS RESULTS.** 