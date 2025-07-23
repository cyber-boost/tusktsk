package adapters

import (
	"context"
	"database/sql"
	"fmt"
	"strings"
	"time"

	"github.com/cyber-boost/tusktsk/pkg/databasetypes"
	_ "github.com/mattn/go-sqlite3"
)

// SQLiteAdapter implements DatabaseAdapter for SQLite
type SQLiteAdapter struct {
	db     *sql.DB
	config *databasetypes.Config
	connected bool
}

// NewSQLiteAdapter creates a new SQLite adapter
func NewSQLiteAdapter() *SQLiteAdapter {
	return &SQLiteAdapter{
		config: &databasetypes.Config{
			MaxOpenConns:    100,
			MaxIdleConns:    25,
			ConnMaxLifetime: 5 * time.Minute,
			ConnMaxIdleTime: 5 * time.Minute,
		},
	}
}

// Connect establishes connection to SQLite database
func (sa *SQLiteAdapter) Connect(config string) error {
	// Parse SQLite connection string: sqlite:/path/to/databasetypes.db
	if !strings.HasPrefix(config, "sqlite:") {
		return fmt.Errorf("invalid SQLite connection string: %s", config)
	}
	
	dbPath := strings.TrimPrefix(config, "sqlite:")
	
	// Open SQLite database
	db, err := sql.Open("sqlite3", dbPath)
	if err != nil {
		return fmt.Errorf("failed to open SQLite database: %w", err)
	}
	
	// Configure connection pool
	db.SetMaxOpenConns(sa.config.MaxOpenConns)
	db.SetMaxIdleConns(sa.config.MaxIdleConns)
	db.SetConnMaxLifetime(sa.config.ConnMaxLifetime)
	db.SetConnMaxIdleTime(sa.config.ConnMaxIdleTime)
	
	// Test connection
	if err := db.Ping(); err != nil {
		db.Close()
		return fmt.Errorf("failed to ping SQLite database: %w", err)
	}
	
	sa.db = db
	sa.connected = true
	
	return nil
}

// Disconnect closes the database connection
func (sa *SQLiteAdapter) Disconnect() error {
	if sa.db != nil {
		sa.connected = false
		return sa.db.Close()
	}
	return nil
}

// IsConnected returns connection status
func (sa *SQLiteAdapter) IsConnected() bool {
	return sa.connected && sa.db != nil
}

// Ping tests the database connection
func (sa *SQLiteAdapter) Ping() error {
	if sa.db == nil {
		return fmt.Errorf("database not connected")
	}
	return sa.db.Ping()
}

// Query executes a SELECT query
func (sa *SQLiteAdapter) Query(query string, args ...interface{}) (*databasetypes.Result, error) {
	if sa.db == nil {
		return nil, fmt.Errorf("database not connected")
	}
	
	rows, err := sa.db.Query(query, args...)
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
	result := &databasetypes.Result{
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
func (sa *SQLiteAdapter) Execute(query string, args ...interface{}) error {
	if sa.db == nil {
		return fmt.Errorf("database not connected")
	}
	
	result, err := sa.db.Exec(query, args...)
	if err != nil {
		return fmt.Errorf("execute failed: %w", err)
	}
	
	// Store affected rows count for potential future use
	_, _ = result.RowsAffected()
	
	return nil
}

// QueryRow executes a query that returns a single row
func (sa *SQLiteAdapter) QueryRow(query string, args ...interface{}) (*databasetypes.Row, error) {
	if sa.db == nil {
		return nil, fmt.Errorf("database not connected")
	}
	
	row := sa.db.QueryRow(query, args...)
	
	// Get column names (we need to execute a query to get this)
	columns, err := sa.getColumnsFromQuery(query)
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
		return &databasetypes.Row{Error: err}, nil
	}
	
	// Convert to map
	rowData := make(map[string]interface{})
	for i, col := range columns {
		val := values[i]
		rowData[col] = val
	}
	
	return &databasetypes.Row{Data: rowData}, nil
}

// BeginTransaction starts a new transaction
func (sa *SQLiteAdapter) BeginTransaction() (databasetypes.Transaction, error) {
	return sa.BeginTransactionWithContext(nil)
}

// BeginTransactionWithContext starts a new transaction with context
func (sa *SQLiteAdapter) BeginTransactionWithContext(ctx context.Context) (databasetypes.Transaction, error) {
	if sa.db == nil {
		return nil, fmt.Errorf("database not connected")
	}
	
	var tx *sql.Tx
	var err error
	
	if ctx != nil {
		tx, err = sa.db.BeginTx(ctx, nil)
	} else {
		tx, err = sa.db.Begin()
	}
	
	if err != nil {
		return nil, fmt.Errorf("failed to begin transaction: %w", err)
	}
	
	return &SQLiteTransaction{tx: tx}, nil
}

// SetMaxOpenConns sets maximum open connections
func (sa *SQLiteAdapter) SetMaxOpenConns(n int) {
	sa.config.MaxOpenConns = n
	if sa.db != nil {
		sa.db.SetMaxOpenConns(n)
	}
}

// SetMaxIdleConns sets maximum idle connections
func (sa *SQLiteAdapter) SetMaxIdleConns(n int) {
	sa.config.MaxIdleConns = n
	if sa.db != nil {
		sa.db.SetMaxIdleConns(n)
	}
}

// SetConnMaxLifetime sets connection max lifetime
func (sa *SQLiteAdapter) SetConnMaxLifetime(d time.Duration) {
	sa.config.ConnMaxLifetime = d
	if sa.db != nil {
		sa.db.SetConnMaxLifetime(d)
	}
}

// SetConnMaxIdleTime sets connection max idle time
func (sa *SQLiteAdapter) SetConnMaxIdleTime(d time.Duration) {
	sa.config.ConnMaxIdleTime = d
	if sa.db != nil {
		sa.db.SetConnMaxIdleTime(d)
	}
}

// GetStats returns database statistics
func (sa *SQLiteAdapter) GetStats() *databasetypes.Stats {
	if sa.db == nil {
		return &databasetypes.Stats{}
	}
	
	stats := sa.db.Stats()
	return &databasetypes.Stats{
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
func (sa *SQLiteAdapter) Close() error {
	return sa.Disconnect()
}

// getColumnsFromQuery extracts column names from a SELECT query
func (sa *SQLiteAdapter) getColumnsFromQuery(query string) ([]string, error) {
	// This is a simplified approach - in production, you might want to use a SQL parser
	// For now, we'll execute a LIMIT 1 query to get column information
	limitedQuery := query
	if !strings.Contains(strings.ToUpper(query), "LIMIT") {
		limitedQuery = query + " LIMIT 1"
	}
	
	rows, err := sa.db.Query(limitedQuery)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	
	return rows.Columns()
}

// SQLiteTransaction implements Transaction for SQLite
type SQLiteTransaction struct {
	tx *sql.Tx
}

// Commit commits the transaction
func (st *SQLiteTransaction) Commit() error {
	return st.tx.Commit()
}

// Rollback rolls back the transaction
func (st *SQLiteTransaction) Rollback() error {
	return st.tx.Rollback()
}

// Query executes a SELECT query within the transaction
func (st *SQLiteTransaction) Query(query string, args ...interface{}) (*databasetypes.Result, error) {
	rows, err := st.tx.Query(query, args...)
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
	result := &databasetypes.Result{
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
func (st *SQLiteTransaction) Execute(query string, args ...interface{}) error {
	_, err := st.tx.Exec(query, args...)
	if err != nil {
		return fmt.Errorf("transaction execute failed: %w", err)
	}
	return nil
}

// QueryRow executes a query that returns a single row within the transaction
func (st *SQLiteTransaction) QueryRow(query string, args ...interface{}) (*databasetypes.Row, error) {
	row := st.tx.QueryRow(query, args...)
	
	// Simplified approach - in production, use proper column detection
	columns := []string{"column1"} // Placeholder
	
	values := make([]interface{}, len(columns))
	valuePtrs := make([]interface{}, len(columns))
	for i := range values {
		valuePtrs[i] = &values[i]
	}
	
	if err := row.Scan(valuePtrs...); err != nil {
		return &databasetypes.Row{Error: err}, nil
	}
	
	rowData := make(map[string]interface{})
	for i, col := range columns {
		rowData[col] = values[i]
	}
	
	return &databasetypes.Row{Data: rowData}, nil
} 