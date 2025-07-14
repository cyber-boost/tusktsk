# Database Security in TuskLang

Database security is critical for protecting your application's data. TuskLang provides multiple layers of security to prevent common vulnerabilities and protect sensitive information.

## SQL Injection Prevention

```tusk
# ALWAYS use parameter binding
# Good: Automatic escaping with query builder
email: @request.input("email")
user: @User.where("email", email).first()  # Safe

# Good: Parameter binding with raw queries
user_id: @request.input("user_id")
results: @db.select(
    "SELECT * FROM users WHERE id = ? AND active = ?",
    [user_id, true]
)  # Safe

# Bad: NEVER concatenate user input
// DON'T DO THIS
// query: "SELECT * FROM users WHERE email = '" + email + "'"
// results: @db.select(query)  # VULNERABLE!

# Good: Named bindings
results: @db.select(
    "SELECT * FROM posts WHERE user_id = :user AND status = :status",
    {
        user: user_id,
        status: "published"
    }
)

# Safe use of raw expressions
sort_column: @request.input("sort", "created_at")

# Validate column name against whitelist
allowed_columns: ["created_at", "updated_at", "name"]
if (!allowed_columns.includes(sort_column)) {
    sort_column: "created_at"
}

users: @User.orderBy(@db.raw(sort_column)).get()  # Safe after validation
```

## Mass Assignment Protection

```tusk
# Define fillable attributes in models
class User extends Model {
    # Only these can be mass assigned
    fillable: ["name", "email", "bio"]
    
    # Never mass assignable
    guarded: ["id", "is_admin", "password"]
    
    # Or guard everything except specified
    guarded: ["*"]  # Guard all
    fillable: ["name", "email"]  # Except these
}

# Safe mass assignment
user: @User.create(@request.only(["name", "email"]))  # Safe

# Unsafe mass assignment attempts are blocked
// user: @User.create(@request.all())  # Could include is_admin!

# Force fill for admin operations only
if (@auth.user.isAdmin()) {
    user: new User()
    user.forceFill({
        name: "Admin User",
        is_admin: true  # Bypasses protection
    })
    user.save()
}

# Validation before mass assignment
validated: @validate(@request.all(), {
    name: "required|string|max:255",
    email: "required|email|unique:users",
    password: "required|min:8|confirmed"
})

user: @User.create(validated)  # Only validated data
```

## Data Encryption

```tusk
# Encrypt sensitive data at rest
class User extends Model {
    # Automatically encrypt/decrypt attributes
    encrypted: ["ssn", "credit_card", "bank_account"]
    
    # Custom encryption
    setApiKeyAttribute(value) {
        this.attributes.api_key: @encrypt(value)
    }
    
    getApiKeyAttribute(value) {
        return value ? @decrypt(value) : null
    }
}

# Database field encryption
#migration add_encrypted_fields {
    up() {
        @schema.table("users", (table) => {
            # Store encrypted data as text
            table.text("ssn_encrypted").nullable()
            table.text("credit_card_encrypted").nullable()
            
            # Store encryption metadata
            table.string("encryption_key_id").nullable()
        })
    }
}

# Searchable encryption
class Document extends Model {
    # Blind index for searching encrypted data
    blindIndexes: {
        email: {
            column: "email_bidx",
            key: @env("BLIND_INDEX_KEY")
        }
    }
    
    # Search encrypted fields
    scopeWhereEmail(query, email) {
        bidx: @generate_blind_index(email)
        return query.where("email_bidx", bidx)
    }
}

# Full database encryption (transparent)
config.database.connections.mysql.options: {
    PDO::MYSQL_ATTR_SSL_CA: "/path/to/ca.pem",
    PDO::MYSQL_ATTR_SSL_CERT: "/path/to/cert.pem",
    PDO::MYSQL_ATTR_SSL_KEY: "/path/to/key.pem"
}
```

## Access Control

```tusk
# Row-level security with global scopes
class Document extends Model {
    boot() {
        super.boot()
        
        # Only show documents user has access to
        @addGlobalScope("access", (query) => {
            if (!@auth.check()) {
                query.where("public", true)
            } else if (!@auth.user.isAdmin()) {
                query.where((q) => {
                    q.where("user_id", @auth.id)
                        .orWhere("public", true)
                        .orWhereHas("sharedWith", (q2) => {
                            q2.where("user_id", @auth.id)
                        })
                })
            }
            # Admins see everything
        })
    }
}

# Policy-based authorization
class PostPolicy {
    view(user, post) {
        return post.published || 
               post.user_id == user.id ||
               user.isAdmin()
    }
    
    update(user, post) {
        return post.user_id == user.id && !post.locked
    }
    
    delete(user, post) {
        return user.isAdmin() || 
               (post.user_id == user.id && post.created_at > @days_ago(1))
    }
}

# Use policies in routes
#web /posts/{id}/edit {
    post: @Post.findOrFail(@params.id)
    
    @authorize("update", post)  # Throws 403 if unauthorized
    
    @render("posts.edit", {post})
}
```

## Audit Logging

```tusk
# Audit trait for models
trait Auditable {
    boot() {
        # Log creates
        @creating((model) => {
            @audit.log("create", {
                model: model.constructor.name,
                user_id: @auth.id,
                ip: @request.ip,
                data: model.toArray()
            })
        })
        
        # Log updates with changes
        @updating((model) => {
            @audit.log("update", {
                model: model.constructor.name,
                model_id: model.id,
                user_id: @auth.id,
                ip: @request.ip,
                changes: model.getDirty(),
                original: model.getOriginal()
            })
        })
        
        # Log deletes
        @deleting((model) => {
            @audit.log("delete", {
                model: model.constructor.name,
                model_id: model.id,
                user_id: @auth.id,
                ip: @request.ip,
                data: model.toArray()
            })
        })
    }
}

# Audit log model
class AuditLog extends Model {
    fillable: ["action", "model", "model_id", "user_id", "ip", "data", "created_at"]
    
    casts: {
        data: "json"
    }
    
    # Query scopes
    scopeForModel(query, model) {
        return query.where("model", model.constructor.name)
            .where("model_id", model.id)
    }
    
    scopeByUser(query, user_id) {
        return query.where("user_id", user_id)
    }
    
    scopeSuspicious(query) {
        return query.where((q) => {
            q.where("action", "delete")
                .orWhere("ip", "in", @get_suspicious_ips())
                .orWhereRaw("created_at > NOW() - INTERVAL 1 MINUTE")
                .having(@db.raw("COUNT(*)"), ">", 100)
        })
    }
}
```

## Sensitive Data Handling

```tusk
# Hide sensitive attributes in responses
class User extends Model {
    hidden: ["password", "remember_token", "api_secret", "ssn"]
    
    # Conditionally hide attributes
    makeVisible(attributes) {
        if (@auth.user?.isAdmin()) {
            return super.makeVisible(["api_secret"])
        }
        return this
    }
    
    # Redact sensitive data
    toArray() {
        data: super.toArray()
        
        # Partially mask sensitive data
        if (data.ssn) {
            data.ssn: "***-**-" + data.ssn.slice(-4)
        }
        
        if (data.credit_card) {
            data.credit_card: "**** **** **** " + data.credit_card.slice(-4)
        }
        
        return data
    }
}

# Secure password handling
class AuthController {
    register(request) {
        # Validate password strength
        validated: @validate(request.all(), {
            password: [
                "required",
                "min:12",  # Minimum length
                "regex:/[a-z]/",  # Lowercase
                "regex:/[A-Z]/",  # Uppercase  
                "regex:/[0-9]/",  # Number
                "regex:/[@$!%*?&]/",  # Special char
                "not_in:" + @get_common_passwords(),  # Not common
                "different:email"  # Not same as email
            ]
        })
        
        # Hash password with strong algorithm
        user: @User.create({
            ...validated,
            password: @password.hash(validated.password, {
                algorithm: PASSWORD_ARGON2ID,
                memory_cost: 65536,
                time_cost: 4,
                threads: 3
            })
        })
        
        # Don't return password in response
        return user.makeHidden(["password"])
    }
}
```

## Database Connection Security

```tusk
# Secure connection configuration
config.database: {
    connections: {
        mysql: {
            # Use environment variables
            host: @env("DB_HOST"),
            username: @env("DB_USERNAME"),
            password: @env("DB_PASSWORD"),
            
            # SSL/TLS encryption
            options: {
                PDO::MYSQL_ATTR_SSL_CA: @env("DB_SSL_CA"),
                PDO::MYSQL_ATTR_SSL_VERIFY_SERVER_CERT: true,
                PDO::ATTR_TIMEOUT: 10,
                PDO::ATTR_PERSISTENT: false  # Avoid persistent connections
            },
            
            # Strict mode
            strict: true,
            
            # Character set
            charset: "utf8mb4",
            collation: "utf8mb4_unicode_ci"
        }
    }
}

# Connection lifecycle management
class DatabaseSecurity {
    static configure() {
        # Limit connections
        @db.setMaxConnections(100)
        
        # Close idle connections
        @db.setIdleTimeout(300)  # 5 minutes
        
        # Monitor failed connections
        @db.failed((connection, exception) => {
            @log.critical("Database connection failed", {
                host: connection.getHost(),
                error: exception.message
            })
            
            # Alert on repeated failures
            if (@cache.increment("db.failures", 1) > 5) {
                @alert.database_down()
            }
        })
    }
}
```

## Query Monitoring and Anomaly Detection

```tusk
# Monitor for suspicious queries
class QuerySecurityMonitor {
    static monitor() {
        @db.listen((query) => {
            # Check for suspicious patterns
            if (@is_suspicious_query(query.sql)) {
                @log.warning("Suspicious query detected", {
                    sql: query.sql,
                    bindings: query.bindings,
                    user: @auth.id,
                    ip: @request.ip,
                    trace: @debug_backtrace()
                })
                
                # Block if critical
                if (@is_critical_threat(query.sql)) {
                    throw "Security violation: Query blocked"
                }
            }
            
            # Monitor data access patterns
            @track_data_access(query)
        })
    }
    
    static is_suspicious_query(sql) {
        patterns: [
            /UNION.*SELECT/i,  # Union-based injection
            /OR.*=.*/i,  # OR-based injection
            /';\s*DROP/i,  # Drop table attempts
            /xp_cmdshell/i,  # Command execution
            /LOAD_FILE/i,  # File access
            /INTO\s+OUTFILE/i  # File write
        ]
        
        for (pattern in patterns) {
            if (pattern.test(sql)) {
                return true
            }
        }
        
        return false
    }
    
    static track_data_access(query) {
        # Track access to sensitive tables
        sensitive_tables: ["users", "payments", "api_keys"]
        
        for (table in sensitive_tables) {
            if (query.sql.includes(table)) {
                @metrics.increment("db.sensitive_access", {
                    table: table,
                    user: @auth.id
                })
                
                # Alert on unusual access patterns
                access_count: @cache.increment(
                    "user." + @auth.id + ".access." + table,
                    1,
                    300  # 5 minute window
                )
                
                if (access_count > 100) {
                    @alert.excessive_data_access({
                        user: @auth.id,
                        table: table,
                        count: access_count
                    })
                }
            }
        }
    }
}
```

## Security Best Practices

```tusk
# Regular security tasks
#cron "0 2 * * *" {  # Daily at 2 AM
    # Rotate encryption keys
    @security.rotate_encryption_keys()
    
    # Clean up old audit logs
    @AuditLog.where("created_at", "<", @days_ago(90))
        .chunk(1000, (logs) => {
            # Archive before deleting
            @archive.store("audit-logs", logs)
            logs.delete()
        })
    
    # Check for weak passwords
    users_with_weak_passwords: @db.select("""
        SELECT id, email 
        FROM users 
        WHERE password IN (
            SELECT password 
            FROM users 
            GROUP BY password 
            HAVING COUNT(*) > 1
        )
    """)
    
    if (users_with_weak_passwords.length > 0) {
        @alert.weak_passwords_found(users_with_weak_passwords)
    }
}

# Security headers for database exports
#web /admin/export/users {
    #auth role: "admin"
    
    # Log export access
    @audit.log("export", {
        model: "User",
        user_id: @auth.id,
        ip: @request.ip
    })
    
    # Rate limit exports
    #rate_limit 5 per: "hour"
    
    # Export with redacted sensitive data
    users: @User.all().map(user => {
        data: user.toArray()
        delete data.password
        delete data.remember_token
        delete data.api_secret
        return data
    })
    
    @response.headers["Content-Security-Policy"]: "default-src 'none'"
    @response.download("users.csv", @generate_csv(users))
}
```

## Best Practices Summary

1. **Always use parameter binding** - Never concatenate user input into queries
2. **Implement mass assignment protection** - Use fillable/guarded properties
3. **Encrypt sensitive data** - Both in transit and at rest
4. **Use row-level security** - Implement proper access control
5. **Audit database access** - Log all sensitive operations
6. **Monitor for anomalies** - Detect unusual query patterns
7. **Rotate credentials regularly** - Database passwords and encryption keys
8. **Validate all input** - Before it reaches the database
9. **Use least privilege** - Database users should have minimal permissions
10. **Regular security audits** - Review and update security measures

## Related Topics

- `authentication` - User authentication
- `authorization` - Access control
- `encryption` - Data encryption
- `validation` - Input validation
- `security-headers` - HTTP security