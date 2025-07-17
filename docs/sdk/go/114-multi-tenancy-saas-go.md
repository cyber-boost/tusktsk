# 🏢 Multi-Tenancy & SaaS Patterns with TuskLang & Go

## Introduction
Multi-tenancy is the foundation of scalable SaaS applications. TuskLang and Go let you build robust multi-tenant systems with config-driven tenant isolation, data partitioning, and tenant-specific features that scale from startup to enterprise.

## Key Features
- **Tenant isolation strategies (database, schema, row-level)**
- **Data partitioning and sharding**
- **Tenant-specific configuration**
- **Billing and usage tracking**
- **Tenant onboarding and provisioning**
- **Security isolation and compliance**
- **Performance optimization**

## Example: Multi-Tenant Config
```ini
[multi_tenant]
isolation: database_per_tenant
default_plan: @env("DEFAULT_PLAN", "starter")
billing: @go("billing.TrackUsage")
provisioning: @go("tenant.Provision")
metrics: @metrics("tenant_requests", 0)
```

## Go: Tenant Context Management
```go
package tenant

import (
    "context"
    "database/sql"
    "sync"
)

type TenantContext struct {
    TenantID   string
    Plan       string
    Database   *sql.DB
    Config     map[string]interface{}
}

type TenantManager struct {
    tenants map[string]*TenantContext
    mu      sync.RWMutex
    db      *sql.DB
}

func (tm *TenantManager) GetTenantContext(ctx context.Context, tenantID string) (*TenantContext, error) {
    tm.mu.RLock()
    if tenant, exists := tm.tenants[tenantID]; exists {
        tm.mu.RUnlock()
        return tenant, nil
    }
    tm.mu.RUnlock()
    
    // Load tenant from database
    tm.mu.Lock()
    defer tm.mu.Unlock()
    
    tenant, err := tm.loadTenant(ctx, tenantID)
    if err != nil {
        return nil, err
    }
    
    tm.tenants[tenantID] = tenant
    return tenant, nil
}

func (tm *TenantManager) loadTenant(ctx context.Context, tenantID string) (*TenantContext, error) {
    // Query tenant information
    var plan string
    var configJSON string
    
    err := tm.db.QueryRowContext(ctx, 
        "SELECT plan, config FROM tenants WHERE id = ?", tenantID).Scan(&plan, &configJSON)
    if err != nil {
        return nil, err
    }
    
    var config map[string]interface{}
    if err := json.Unmarshal([]byte(configJSON), &config); err != nil {
        return nil, err
    }
    
    // Get tenant-specific database connection
    db, err := tm.getTenantDB(ctx, tenantID)
    if err != nil {
        return nil, err
    }
    
    return &TenantContext{
        TenantID: tenantID,
        Plan:     plan,
        Database: db,
        Config:   config,
    }, nil
}
```

## Database Per Tenant Strategy
```go
func (tm *TenantManager) getTenantDB(ctx context.Context, tenantID string) (*sql.DB, error) {
    // Create or connect to tenant-specific database
    dbName := fmt.Sprintf("tenant_%s", tenantID)
    
    // Check if database exists
    var exists int
    err := tm.db.QueryRowContext(ctx, 
        "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = ?", dbName).Scan(&exists)
    if err != nil {
        return nil, err
    }
    
    if exists == 0 {
        // Create new tenant database
        if err := tm.createTenantDatabase(ctx, dbName); err != nil {
            return nil, err
        }
    }
    
    // Connect to tenant database
    return sql.Open("postgres", fmt.Sprintf("postgres://user:pass@localhost/%s?sslmode=disable", dbName))
}

func (tm *TenantManager) createTenantDatabase(ctx context.Context, dbName string) error {
    // Create database
    _, err := tm.db.ExecContext(ctx, fmt.Sprintf("CREATE DATABASE %s", dbName))
    if err != nil {
        return err
    }
    
    // Run migrations for new tenant
    return tm.runTenantMigrations(ctx, dbName)
}
```

## Schema Per Tenant Strategy
```go
func (tm *TenantManager) getTenantSchema(ctx context.Context, tenantID string) (string, error) {
    // Use tenant ID as schema name
    schemaName := fmt.Sprintf("tenant_%s", tenantID)
    
    // Create schema if it doesn't exist
    _, err := tm.db.ExecContext(ctx, fmt.Sprintf("CREATE SCHEMA IF NOT EXISTS %s", schemaName))
    if err != nil {
        return "", err
    }
    
    return schemaName, nil
}

func (tm *TenantManager) QueryWithSchema(ctx context.Context, tenantID, query string, args ...interface{}) (*sql.Rows, error) {
    schema, err := tm.getTenantSchema(ctx, tenantID)
    if err != nil {
        return nil, err
    }
    
    // Prefix table names with schema
    prefixedQuery := strings.ReplaceAll(query, "FROM ", fmt.Sprintf("FROM %s.", schema))
    return tm.db.QueryContext(ctx, prefixedQuery, args...)
}
```

## Row-Level Security
```go
func (tm *TenantManager) setupRowLevelSecurity(ctx context.Context, tenantID string) error {
    // Enable RLS on tables
    tables := []string{"users", "orders", "products"}
    
    for _, table := range tables {
        // Enable RLS
        _, err := tm.db.ExecContext(ctx, fmt.Sprintf("ALTER TABLE %s ENABLE ROW LEVEL SECURITY", table))
        if err != nil {
            return err
        }
        
        // Create policy
        policy := fmt.Sprintf(`
            CREATE POLICY tenant_isolation_%s ON %s
            FOR ALL
            USING (tenant_id = '%s')
        `, table, table, tenantID)
        
        _, err = tm.db.ExecContext(ctx, policy)
        if err != nil {
            return err
        }
    }
    
    return nil
}
```

## Tenant-Specific Configuration
```go
func (tm *TenantManager) GetTenantConfig(ctx context.Context, tenantID, key string) (interface{}, error) {
    tenant, err := tm.GetTenantContext(ctx, tenantID)
    if err != nil {
        return nil, err
    }
    
    // Check tenant-specific config first
    if value, exists := tenant.Config[key]; exists {
        return value, nil
    }
    
    // Fall back to plan-based config
    return tm.getPlanConfig(tenant.Plan, key)
}

func (tm *TenantManager) SetTenantConfig(ctx context.Context, tenantID, key string, value interface{}) error {
    tenant, err := tm.GetTenantContext(ctx, tenantID)
    if err != nil {
        return err
    }
    
    // Update tenant config
    tenant.Config[key] = value
    
    // Persist to database
    configJSON, err := json.Marshal(tenant.Config)
    if err != nil {
        return err
    }
    
    _, err = tm.db.ExecContext(ctx, 
        "UPDATE tenants SET config = ? WHERE id = ?", configJSON, tenantID)
    return err
}
```

## Billing and Usage Tracking
```go
package billing

import (
    "context"
    "time"
)

type UsageTracker struct {
    db *sql.DB
}

type UsageEvent struct {
    TenantID    string
    EventType   string
    Quantity    int64
    Timestamp   time.Time
    Metadata    map[string]interface{}
}

func (ut *UsageTracker) TrackUsage(ctx context.Context, event UsageEvent) error {
    // Record usage event
    _, err := ut.db.ExecContext(ctx, `
        INSERT INTO usage_events (tenant_id, event_type, quantity, timestamp, metadata)
        VALUES (?, ?, ?, ?, ?)
    `, event.TenantID, event.EventType, event.Quantity, event.Timestamp, event.Metadata)
    
    if err != nil {
        return err
    }
    
    // Check if tenant has exceeded limits
    return ut.checkLimits(ctx, event.TenantID)
}

func (ut *UsageTracker) checkLimits(ctx context.Context, tenantID string) error {
    // Get tenant plan and current usage
    var plan string
    var monthlyLimit int64
    
    err := ut.db.QueryRowContext(ctx, 
        "SELECT plan FROM tenants WHERE id = ?", tenantID).Scan(&plan)
    if err != nil {
        return err
    }
    
    // Get monthly usage
    var currentUsage int64
    err = ut.db.QueryRowContext(ctx, `
        SELECT COALESCE(SUM(quantity), 0) 
        FROM usage_events 
        WHERE tenant_id = ? AND timestamp >= DATE_TRUNC('month', NOW())
    `, tenantID).Scan(&currentUsage)
    if err != nil {
        return err
    }
    
    // Check if usage exceeds limit
    if currentUsage > monthlyLimit {
        return ErrUsageLimitExceeded
    }
    
    return nil
}
```

## Tenant Onboarding
```go
func (tm *TenantManager) ProvisionTenant(ctx context.Context, tenantID, plan string) error {
    // Create tenant record
    _, err := tm.db.ExecContext(ctx, `
        INSERT INTO tenants (id, plan, created_at, status)
        VALUES (?, ?, NOW(), 'active')
    `, tenantID, plan)
    if err != nil {
        return err
    }
    
    // Create tenant database/schema
    if err := tm.createTenantInfrastructure(ctx, tenantID); err != nil {
        return err
    }
    
    // Run tenant-specific setup
    if err := tm.runTenantSetup(ctx, tenantID); err != nil {
        return err
    }
    
    // Send welcome email
    return tm.sendWelcomeEmail(ctx, tenantID)
}

func (tm *TenantManager) createTenantInfrastructure(ctx context.Context, tenantID string) error {
    // Create database/schema based on isolation strategy
    switch tm.isolationStrategy {
    case "database_per_tenant":
        return tm.createTenantDatabase(ctx, fmt.Sprintf("tenant_%s", tenantID))
    case "schema_per_tenant":
        _, err := tm.getTenantSchema(ctx, tenantID)
        return err
    case "row_level":
        return tm.setupRowLevelSecurity(ctx, tenantID)
    default:
        return fmt.Errorf("unknown isolation strategy: %s", tm.isolationStrategy)
    }
}
```

## Best Practices
- **Choose appropriate isolation strategy based on requirements**
- **Implement proper tenant context propagation**
- **Use connection pooling for tenant databases**
- **Monitor tenant resource usage**
- **Implement tenant-specific rate limiting**
- **Use secure tenant identification**

## Performance Optimization
- **Cache tenant contexts**
- **Use connection pooling**
- **Implement tenant-specific caching**
- **Monitor per-tenant performance metrics**

## Security Considerations
- **Validate tenant access on every request**
- **Implement proper tenant isolation**
- **Use secure tenant identification**
- **Audit tenant access patterns**

## Troubleshooting
- **Monitor tenant provisioning failures**
- **Check tenant database connectivity**
- **Verify tenant context propagation**
- **Monitor tenant resource usage**

## Conclusion
TuskLang + Go = multi-tenancy that scales from startup to enterprise. Build once, serve many. 