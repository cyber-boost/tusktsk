# Database Commands

Database operations for TuskLang JavaScript SDK.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk db status](./status.md) | Check database connection |
| [tsk db migrate](./migrate.md) | Run migration file |
| [tsk db console](./console.md) | Interactive database console |
| [tsk db backup](./backup.md) | Backup database |
| [tsk db restore](./restore.md) | Restore from backup |
| [tsk db init](./init.md) | Initialize SQLite database |

## Common Use Cases

- **Development Setup**: Initialize and migrate databases for development
- **Production Deployment**: Backup and restore production databases
- **Testing**: Create test databases and run migrations
- **Monitoring**: Check database health and connection status
- **Debugging**: Use interactive console for database queries

## JavaScript Specific Notes

### Database Drivers

The JavaScript SDK supports multiple database drivers:

```javascript
// PostgreSQL
const { Client } = require('pg');
const client = new Client({
    host: config.get('database.host'),
    port: config.get('database.port'),
    database: config.get('database.name'),
    user: config.get('database.user'),
    password: config.get('database.password')
});

// MySQL
const mysql = require('mysql2/promise');
const connection = await mysql.createConnection({
    host: config.get('database.host'),
    port: config.get('database.port'),
    database: config.get('database.name'),
    user: config.get('database.user'),
    password: config.get('database.password')
});

// SQLite
const sqlite3 = require('sqlite3');
const db = new sqlite3.Database(config.get('database.path'));
```

### Configuration Integration

```javascript
// Load database configuration
const { PeanutConfig } = require('tusklang');
const config = PeanutConfig.load();

// Use in database operations
const dbConfig = {
    host: config.get('database.host'),
    port: config.get('database.port'),
    database: config.get('database.name'),
    user: config.get('database.user'),
    password: config.get('database.password')
};
```

### Migration System

```javascript
// Migration file example
const { Migration } = require('tusklang');

class CreateUsersTable extends Migration {
    async up() {
        await this.query(`
            CREATE TABLE users (
                id SERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                email VARCHAR(255) UNIQUE NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        `);
    }

    async down() {
        await this.query('DROP TABLE users');
    }
}

module.exports = CreateUsersTable;
```

### CLI Integration

```javascript
// Use CLI programmatically
const { execSync } = require('child_process');

// Check database status
try {
    execSync('tsk db status', { stdio: 'inherit' });
} catch (error) {
    console.error('Database connection failed');
}

// Run migrations
execSync('tsk db migrate migrations/', { stdio: 'inherit' });
```

## Examples

### Development Workflow

```bash
# 1. Initialize database
tsk db init

# 2. Run migrations
tsk db migrate migrations/

# 3. Check status
tsk db status

# 4. Use console for testing
tsk db console
```

### Production Workflow

```bash
# 1. Backup database
tsk db backup backup.sql

# 2. Deploy new version
npm run deploy

# 3. Run migrations
tsk db migrate migrations/

# 4. Verify status
tsk db status
```

### Testing Workflow

```bash
# 1. Create test database
tsk db init --database test_db

# 2. Run test migrations
tsk db migrate test-migrations/

# 3. Run tests
npm test

# 4. Clean up
tsk db drop --database test_db
``` 