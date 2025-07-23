package adapters

import (
	"context"
	"database/sql"
	"fmt"
	"net/url"
	"strconv"
	"strings"
	"time"

	"github.com/cyber-boost/tusktsk/pkg/databasetypes"
	_ "github.com/lib/pq"
)

// PostgreSQLAdapter implements DatabaseAdapter for PostgreSQL
type PostgreSQLAdapter struct {
	db     *sql.DB
	config *databasetypes.DatabaseConfig
	connected bool
}

// NewPostgreSQLAdapter creates a new PostgreSQL adapter
func NewPostgreSQLAdapter() *PostgreSQLAdapter {
	return &PostgreSQLAdapter{
		config: &databasetypes.DatabaseConfig{
			MaxOpenConns:    100,
			MaxIdleConns:    25,
			ConnMaxLifetime: 5 * time.Minute,
			ConnMaxIdleTime: 5 * time.Minute,
		},
	}
}

// Connect establishes connection to PostgreSQL database
func (pa *PostgreSQLAdapter) Connect(config string) error {
	// Parse PostgreSQL connection string: postgresql://user:pass@host:port/database
	if !strings.HasPrefix(config, "postgresql://") {
		return fmt.Errorf("invalid PostgreSQL connection string: %s", config)
	}
	
	// Parse URL
	parsedURL, err := url.Parse(config)
	if err != nil {
		return fmt.Errorf("failed to parse PostgreSQL URL: %w", err)
	}
	
	// Extract components
	host := parsedURL.Hostname()
	port := parsedURL.Port()
	if port == "" {
		port = "5432" // Default PostgreSQL port
	}
	
	username := parsedURL.User.Username()
	password, _ := parsedURL.User.Password()
	database := strings.TrimPrefix(parsedURL.Path, "/")
	
	// Build connection string for lib/pq
	connStr := fmt.Sprintf("host=%s port=%s user=%s password=%s dbname=%s sslmode=disable",
		host, port, username, password, database)
	
	// Add query parameters as connection options
	if parsedURL.RawQuery != "" {
		queryParams, err := url.ParseQuery(parsedURL.RawQuery)
		if err == nil {
			for key, values := range queryParams {
				if len(values) > 0 {
					connStr += fmt.Sprintf(" %s=%s", key, values[0])
				}
			}
		}
	}
	
	// Open PostgreSQL database
	db, err := sql.Open("postgres", connStr)
	if err != nil {
		return fmt.Errorf("failed to open PostgreSQL database: %w", err)
	}
	
	// Configure connection pool
	db.SetMaxOpenConns(pa.config.MaxOpenConns)
	db.SetMaxIdleConns(pa.config.MaxIdleConns)
	db.SetConnMaxLifetime(pa.config.ConnMaxLifetime)
	db.SetConnMaxIdleTime(pa.config.ConnMaxIdleTime)
	
	// Test connection
	if err := db.Ping(); err != nil {
		db.Close()
		return fmt.Errorf("failed to ping PostgreSQL database: %w", err)
	}
	
	pa.db = db
	pa.connected = true
	
	return nil
}

// Disconnect closes the database connection
func (pa *PostgreSQLAdapter) Disconnect() error {
	if pa.db != nil {
		pa.connected = false
		return pa.db.Close()
	}
	return nil
}

// IsConnected returns connection status
func (pa *PostgreSQLAdapter) IsConnected() bool {
	return pa.connected && pa.db != nil
}

// Ping tests the database connection
func (pa *PostgreSQLAdapter) Ping() error {
	if pa.db == nil {
		return fmt.Errorf("database not connected")
	}
	return pa.db.Ping()
}

// Query executes a SELECT query
func (pa *PostgreSQLAdapter) Query(query string, args ...interface{}) (*databasetypes.Result, error) {
	if pa.db == nil {
		return nil, fmt.Errorf("database not connected")
	}
	
	rows, err := pa.db.Query(query, args...)
	if err != nil {
		return nil, fmt.Errorf("query failed: %w", err)
	}
	defer rows.Close()
	
	// Get column names
	columns, err := rows.Columns()
	if err != nil {
		return nil, fmt.Errorf("failed to get columns: %w", err)
	}
	
	// Prepare result
	result := &database.Resultdatabasetypes.Result{
		Columns: columns,
		Rows:    make([]map[string]interface{}, 0),
	}
	
	// Scan rows
	for rows.Next() {
		// Create slice to hold values
		values := make([]interface{}, len(columns))
		valuePtrs := make([]interface{}, len(columns))
		for i := range values {
			valuePtrs[i] = &values[i]
		}
		
		// Scan row
		if err := rows.Scan(valuePtrs...); err != nil {
			return nil, fmt.Errorf("failed to scan row: %w", err)
		}
		
		// Convert to map
		row := make(map[string]interface{})
		for i, col := range columns {
			val := values[i]
			row[col] = val
		}
		
		result.Rows = append(result.Rows, row)
	}
	
	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("error iterating rows: %w", err)
	}
	
	return result, nil
}

// Execute executes a non-SELECT query (INSERT, UPDATE, DELETE)
func (pa *PostgreSQLAdapter) Execute(query string, args ...interface{}) error {
	if pa.db == nil {
		return fmt.Errorf("database not connected")
	}
	
	result, err := pa.db.Exec(query, args...)
	if err != nil {
		return fmt.Errorf("execute failed: %w", err)
	}
	
	// Store affected rows count for potential future use
	_, _ = result.RowsAffected()
	
	return nil
}

// QueryRow executes a query that returns a single row
func (pa *PostgreSQLAdapter) QueryRow(query string, args ...interface{}) (*databasetypes.Row, error) {
	if pa.db == nil {
		return nil, fmt.Errorf("database not connected")
	}
	
	row := pa.db.QueryRow(query, args...)
	
	// Get column names (we need to execute a query to get this)
	columns, err := pa.getColumnsFromQuery(query)
	if err != nil {
		return nil, fmt.Errorf("failed to get columns: %w", err)
	}
	
	// Create slice to hold values
	values := make([]interface{}, len(columns))
	valuePtrs := make([]interface{}, len(columns))
	for i := range values {
		valuePtrs[i] = &values[i]
	}
	
	// Scan row
	if err := row.Scan(valuePtrs...); err != nil {
		return &database.Rowdatabasetypes.Row{Error: err}, nil
	}
	
	// Convert to map
	rowData := make(map[string]interface{})
	for i, col := range columns {
		val := values[i]
		rowData[col] = val
	}
	
	return &database.Rowdatabasetypes.Row{Data: rowData}, nil
}

// BeginTransaction starts a new transaction
func (pa *PostgreSQLAdapter) BeginTransaction() (databasetypes.Transaction, error) {
	return pa.BeginTransactionWithContext(nil)
}

// BeginTransactionWithContext starts a new transaction with context
func (pa *PostgreSQLAdapter) BeginTransactionWithContext(ctx context.Context) (databasetypes.Transaction, error) {
	if pa.db == nil {
		return nil, fmt.Errorf("database not connected")
	}
	
	var tx *sql.Tx
	var err error
	
	if ctx != nil {
		tx, err = pa.db.BeginTx(ctx, nil)
	} else {
		tx, err = pa.db.Begin()
	}
	
	if err != nil {
		return nil, fmt.Errorf("failed to begin transaction: %w", err)
	}
	
	return &PostgreSQLTransaction{tx: tx}, nil
}

// SetMaxOpenConns sets maximum open connections
func (pa *PostgreSQLAdapter) SetMaxOpenConns(n int) {
	pa.config.MaxOpenConns = n
	if pa.db != nil {
		pa.db.SetMaxOpenConns(n)
	}
}

// SetMaxIdleConns sets maximum idle connections
func (pa *PostgreSQLAdapter) SetMaxIdleConns(n int) {
	pa.config.MaxIdleConns = n
	if pa.db != nil {
		pa.db.SetMaxIdleConns(n)
	}
}

// SetConnMaxLifetime sets connection max lifetime
func (pa *PostgreSQLAdapter) SetConnMaxLifetime(d time.Duration) {
	pa.config.ConnMaxLifetime = d
	if pa.db != nil {
		pa.db.SetConnMaxLifetime(d)
	}
}

// SetConnMaxIdleTime sets connection max idle time
func (pa *PostgreSQLAdapter) SetConnMaxIdleTime(d time.Duration) {
	pa.config.ConnMaxIdleTime = d
	if pa.db != nil {
		pa.db.SetConnMaxIdleTime(d)
	}
}

// GetStats returns database statistics
func (pa *PostgreSQLAdapter) GetStats() *databasetypes.Stats {
	if pa.db == nil {
		return &databasetypes.Stats{}
	}
	
	stats := pa.db.Stats()
	return &database.Stats{
		MaxOpenConnections: stats.MaxOpenConnections,
		OpenConnections:    stats.OpenConnections,
		InUse:              stats.InUse,
		Idle:               stats.Idle,
		WaitCount:          stats.WaitCount,
		WaitDuration:       stats.WaitDuration,
		MaxIdleClosed:      stats.MaxIdleClosed,
		MaxLifetimeClosed:  stats.MaxLifetimeClosed,
	}
}

// Close closes the database connection
func (pa *PostgreSQLAdapter) Close() error {
	return pa.Disconnect()
}

// getColumnsFromQuery extracts column names from a SELECT query
func (pa *PostgreSQLAdapter) getColumnsFromQuery(query string) ([]string, error) {
	// This is a simplified approach - in production, you might want to use a SQL parser
	// For now, we'll execute a LIMIT 1 query to get column information
	limitedQuery := query
	if !strings.Contains(strings.ToUpper(query), "LIMIT") {
		limitedQuery = query + " LIMIT 1"
	}
	
	rows, err := pa.db.Query(limitedQuery)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	
	return rows.Columns()
}

// PostgreSQL-specific methods

// CreateFullTextIndex creates a full-text search index
func (pa *PostgreSQLAdapter) CreateFullTextIndex(tableName, columnName, indexName string) error {
	query := fmt.Sprintf(`
		CREATE INDEX IF NOT EXISTS %s 
		ON %s 
		USING gin(to_tsvector('english', %s))
	`, indexName, tableName, columnName)
	
	return pa.Execute(query)
}

// FullTextSearch performs a full-text search
func (pa *PostgreSQLAdapter) FullTextSearch(tableName, columnName, searchTerm string) (*databasetypes.Result, error) {
	query := fmt.Sprintf(`
		SELECT *, ts_rank(to_tsvector('english', %s), plainto_tsquery('english', $1)) as rank
		FROM %s 
		WHERE to_tsvector('english', %s) @@ plainto_tsquery('english', $1)
		ORDER BY rank DESC
	`, columnName, tableName, columnName)
	
	return pa.Query(query, searchTerm)
}

// JSONBQuery performs a JSONB query
func (pa *PostgreSQLAdapter) JSONBQuery(tableName, jsonbColumn, jsonPath, value string) (*databasetypes.Result, error) {
	query := fmt.Sprintf(`
		SELECT * FROM %s 
		WHERE %s->>'%s' = $1
	`, tableName, jsonbColumn, jsonPath)
	
	return pa.Query(query, value)
}

// ArrayQuery performs an array query
func (pa *PostgreSQLAdapter) ArrayQuery(tableName, arrayColumn string, value interface{}) (*databasetypes.Result, error) {
	query := fmt.Sprintf(`
		SELECT * FROM %s 
		WHERE $1 = ANY(%s)
	`, tableName, arrayColumn)
	
	return pa.Query(query, value)
}

// WindowFunctionQuery performs a query with window functions
func (pa *PostgreSQLAdapter) WindowFunctionQuery(tableName, partitionBy, orderBy string) (*databasetypes.Result, error) {
	query := fmt.Sprintf(`
		SELECT *, 
		       ROW_NUMBER() OVER (PARTITION BY %s ORDER BY %s) as row_num,
		       RANK() OVER (PARTITION BY %s ORDER BY %s) as rank_num
		FROM %s
	`, partitionBy, orderBy, partitionBy, orderBy, tableName)
	
	return pa.Query(query)
}

// PostgreSQLTransaction implements Transaction for PostgreSQL
type PostgreSQLTransaction struct {
	tx *sql.Tx
}

// Commit commits the transaction
func (pt *PostgreSQLTransaction) Commit() error {
	return pt.tx.Commit()
}

// Rollback rolls back the transaction
func (pt *PostgreSQLTransaction) Rollback() error {
	return pt.tx.Rollback()
}

// Query executes a SELECT query within the transaction
func (pt *PostgreSQLTransaction) Query(query string, args ...interface{}) (*databasetypes.Result, error) {
	rows, err := pt.tx.Query(query, args...)
	if err != nil {
		return nil, fmt.Errorf("transaction query failed: %w", err)
	}
	defer rows.Close()
	
	// Get column names
	columns, err := rows.Columns()
	if err != nil {
		return nil, fmt.Errorf("failed to get columns: %w", err)
	}
	
	// Prepare result
	result := &database.Resultdatabasetypes.Result{
		Columns: columns,
		Rows:    make([]map[string]interface{}, 0),
	}
	
	// Scan rows
	for rows.Next() {
		values := make([]interface{}, len(columns))
		valuePtrs := make([]interface{}, len(columns))
		for i := range values {
			valuePtrs[i] = &values[i]
		}
		
		if err := rows.Scan(valuePtrs...); err != nil {
			return nil, fmt.Errorf("failed to scan row: %w", err)
		}
		
		row := make(map[string]interface{})
		for i, col := range columns {
			row[col] = values[i]
		}
		
		result.Rows = append(result.Rows, row)
	}
	
	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("error iterating rows: %w", err)
	}
	
	return result, nil
}

// Execute executes a non-SELECT query within the transaction
func (pt *PostgreSQLTransaction) Execute(query string, args ...interface{}) error {
	_, err := pt.tx.Exec(query, args...)
	if err != nil {
		return fmt.Errorf("transaction execute failed: %w", err)
	}
	return nil
}

// QueryRow executes a query that returns a single row within the transaction
func (pt *PostgreSQLTransaction) QueryRow(query string, args ...interface{}) (*databasetypes.Row, error) {
	row := pt.tx.QueryRow(query, args...)
	
	// Simplified approach - in production, use proper column detection
	columns := []string{"column1"} // Placeholder
	
	values := make([]interface{}, len(columns))
	valuePtrs := make([]interface{}, len(columns))
	for i := range values {
		valuePtrs[i] = &values[i]
	}
	
	if err := row.Scan(valuePtrs...); err != nil {
		return &database.Rowdatabasetypes.Row{Error: err}, nil
	}
	
	rowData := make(map[string]interface{})
	for i, col := range columns {
		rowData[col] = values[i]
	}
	
	return &database.Rowdatabasetypes.Row{Data: rowData}, nil
} 