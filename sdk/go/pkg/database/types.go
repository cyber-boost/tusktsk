package database

import (
	"time"
)

// DatabaseType represents supported database types
type DatabaseType string

const (
	SQLite     DatabaseType = "sqlite"
	PostgreSQL DatabaseType = "postgresql"
	MySQL      DatabaseType = "mysql"
	MongoDB    DatabaseType = "mongodb"
	Redis      DatabaseType = "redis"
)

// ConnectionStatus represents the status of a database connection
type ConnectionStatus string

const (
	StatusDisconnected ConnectionStatus = "disconnected"
	StatusConnecting   ConnectionStatus = "connecting"
	StatusConnected    ConnectionStatus = "connected"
	StatusError        ConnectionStatus = "error"
)

// QueryType represents the type of database query
type QueryType string

const (
	QuerySelect QueryType = "select"
	QueryInsert QueryType = "insert"
	QueryUpdate QueryType = "update"
	QueryDelete QueryType = "delete"
	QueryRaw    QueryType = "raw"
)

// MigrationStatus represents the status of a database migration
type MigrationStatus string

const (
	MigrationPending   MigrationStatus = "pending"
	MigrationRunning   MigrationStatus = "running"
	MigrationCompleted MigrationStatus = "completed"
	MigrationFailed    MigrationStatus = "failed"
	MigrationRolledBack MigrationStatus = "rolled_back"
)

// Migration represents a database migration
type Migration struct {
	ID          int64           `json:"id"`
	Version     string          `json:"version"`
	Name        string          `json:"name"`
	Description string          `json:"description"`
	SQL         string          `json:"sql"`
	Status      MigrationStatus `json:"status"`
	CreatedAt   time.Time       `json:"created_at"`
	ExecutedAt  *time.Time      `json:"executed_at,omitempty"`
	Duration    time.Duration   `json:"duration,omitempty"`
	Error       string          `json:"error,omitempty"`
}

// TableInfo represents information about a database table
type TableInfo struct {
	Name        string            `json:"name"`
	Schema      string            `json:"schema"`
	Columns     []ColumnInfo      `json:"columns"`
	Indexes     []IndexInfo       `json:"indexes"`
	Constraints []ConstraintInfo  `json:"constraints"`
	RowCount    int64             `json:"row_count"`
	Size        int64             `json:"size"`
	CreatedAt   time.Time         `json:"created_at"`
	UpdatedAt   time.Time         `json:"updated_at"`
}

// ColumnInfo represents information about a database column
type ColumnInfo struct {
	Name         string      `json:"name"`
	Type         string      `json:"type"`
	Size         int         `json:"size"`
	Nullable     bool        `json:"nullable"`
	DefaultValue interface{} `json:"default_value"`
	PrimaryKey   bool        `json:"primary_key"`
	Unique       bool        `json:"unique"`
	Indexed      bool        `json:"indexed"`
	Comment      string      `json:"comment"`
}

// IndexInfo represents information about a database index
type IndexInfo struct {
	Name      string   `json:"name"`
	Columns   []string `json:"columns"`
	Type      string   `json:"type"`
	Unique    bool     `json:"unique"`
	Primary   bool     `json:"primary"`
	Comment   string   `json:"comment"`
}

// ConstraintInfo represents information about a database constraint
type ConstraintInfo struct {
	Name      string `json:"name"`
	Type      string `json:"type"`
	Columns   []string `json:"columns"`
	Reference string `json:"reference"`
	Comment   string `json:"comment"`
}

// QueryPlan represents a database query execution plan
type QueryPlan struct {
	Query       string        `json:"query"`
	Plan        string        `json:"plan"`
	Cost        float64       `json:"cost"`
	Rows        int64         `json:"rows"`
	Width       int           `json:"width"`
	ActualTime  time.Duration `json:"actual_time"`
	PlanningTime time.Duration `json:"planning_time"`
	ExecutionTime time.Duration `json:"execution_time"`
}

// BackupInfo represents database backup information
type BackupInfo struct {
	ID          string    `json:"id"`
	Database    string    `json:"database"`
	Size        int64     `json:"size"`
	Format      string    `json:"format"`
	Compressed  bool      `json:"compressed"`
	Encrypted   bool      `json:"encrypted"`
	CreatedAt   time.Time `json:"created_at"`
	Duration    time.Duration `json:"duration"`
	Status      string    `json:"status"`
	Checksum    string    `json:"checksum"`
	Path        string    `json:"path"`
}

// PerformanceMetrics represents database performance metrics
type PerformanceMetrics struct {
	Timestamp       time.Time `json:"timestamp"`
	QueriesPerSec   float64   `json:"queries_per_sec"`
	SlowQueries     int64     `json:"slow_queries"`
	AvgQueryTime    time.Duration `json:"avg_query_time"`
	MaxQueryTime    time.Duration `json:"max_query_time"`
	ActiveConnections int      `json:"active_connections"`
	IdleConnections  int      `json:"idle_connections"`
	CacheHitRate     float64   `json:"cache_hit_rate"`
	DiskUsage        int64     `json:"disk_usage"`
	MemoryUsage      int64     `json:"memory_usage"`
}

// HealthCheck represents a database health check result
type HealthCheck struct {
	Status      string                 `json:"status"`
	Message     string                 `json:"message"`
	Timestamp   time.Time              `json:"timestamp"`
	Duration    time.Duration          `json:"duration"`
	Details     map[string]interface{} `json:"details"`
	Warnings    []string               `json:"warnings"`
	Errors      []string               `json:"errors"`
}

// DatabaseEvent represents a database event
type DatabaseEvent struct {
	Type      string                 `json:"type"`
	Database  string                 `json:"database"`
	Table     string                 `json:"table,omitempty"`
	Operation string                 `json:"operation,omitempty"`
	Data      map[string]interface{} `json:"data,omitempty"`
	Timestamp time.Time              `json:"timestamp"`
	User      string                 `json:"user,omitempty"`
	IP        string                 `json:"ip,omitempty"`
}

// ConnectionPool represents database connection pool configuration
type ConnectionPool struct {
	MaxOpenConnections int           `json:"max_open_connections"`
	MaxIdleConnections int           `json:"max_idle_connections"`
	ConnMaxLifetime    time.Duration `json:"conn_max_lifetime"`
	ConnMaxIdleTime    time.Duration `json:"conn_max_idle_time"`
	MaxRetries         int           `json:"max_retries"`
	RetryDelay         time.Duration `json:"retry_delay"`
}

// DatabaseConfig represents comprehensive database configuration
type DatabaseConfig struct {
	Type            DatabaseType   `json:"type"`
	Host            string         `json:"host"`
	Port            int            `json:"port"`
	Database        string         `json:"database"`
	Username        string         `json:"username"`
	Password        string         `json:"password"`
	SSLMode         string         `json:"ssl_mode"`
	Charset         string         `json:"charset"`
	Timezone        string         `json:"timezone"`
	Options         map[string]string `json:"options"`
	ConnectionPool  ConnectionPool `json:"connection_pool"`
	Migrations      MigrationConfig `json:"migrations"`
	Backup          BackupConfig   `json:"backup"`
	Monitoring      MonitoringConfig `json:"monitoring"`
}

// MigrationConfig represents migration configuration
type MigrationConfig struct {
	Enabled     bool   `json:"enabled"`
	TableName   string `json:"table_name"`
	Path        string `json:"path"`
	AutoMigrate bool   `json:"auto_migrate"`
}

// BackupConfig represents backup configuration
type BackupConfig struct {
	Enabled     bool   `json:"enabled"`
	Schedule    string `json:"schedule"`
	Path        string `json:"path"`
	Compression bool   `json:"compression"`
	Encryption  bool   `json:"encryption"`
	Retention   int    `json:"retention"`
}

// MonitoringConfig represents monitoring configuration
type MonitoringConfig struct {
	Enabled     bool   `json:"enabled"`
	Metrics     bool   `json:"metrics"`
	Logging     bool   `json:"logging"`
	Alerting    bool   `json:"alerting"`
	HealthCheck bool   `json:"health_check"`
} 