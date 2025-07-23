package database

import (
	"context"
	"time"
	
	"github.com/cyber-boost/tusktsk/pkg/databasetypes"
)

// DatabaseAdapter defines the unified interface for all database adapters
type DatabaseAdapter interface {
	// Connection Management
	Connect(config string) error
	Disconnect() error
	IsConnected() bool
	Ping() error
	
	// Query Operations
	Query(query string, args ...interface{}) (*databasetypes.Result, error)
	Execute(query string, args ...interface{}) error
	QueryRow(query string, args ...interface{}) (*databasetypes.Row, error)
	
	// Transaction Management
	BeginTransaction() (databasetypes.Transaction, error)
	BeginTransactionWithContext(ctx context.Context) (databasetypes.Transaction, error)
	
	// Connection Pool Management
	SetMaxOpenConns(n int)
	SetMaxIdleConns(n int)
	SetConnMaxLifetime(d time.Duration)
	SetConnMaxIdleTime(d time.Duration)
	
	// Utility Methods
	GetStats() *databasetypes.Stats
	Close() error
}

// Transaction interface for database transactions
type Transaction interface {
	Commit() error
	Rollback() error
	Query(query string, args ...interface{}) (*databasetypes.Result, error)
	Execute(query string, args ...interface{}) error
	QueryRow(query string, args ...interface{}) (*databasetypes.Row, error)
}

// Types are now defined in pkg/databasetypes package

// DatabaseManager manages multiple database adapters
type DatabaseManager struct {
	adapters map[string]DatabaseAdapter
	defaultAdapter string
}

// NewDatabaseManager creates a new database manager
func NewDatabaseManager() *DatabaseManager {
	return &DatabaseManager{
		adapters: make(map[string]DatabaseAdapter),
	}
}

// RegisterAdapter registers a database adapter
func (dm *DatabaseManager) RegisterAdapter(name string, adapter DatabaseAdapter) {
	dm.adapters[name] = adapter
	if dm.defaultAdapter == "" {
		dm.defaultAdapter = name
	}
}

// GetAdapter returns a database adapter by name
func (dm *DatabaseManager) GetAdapter(name string) (DatabaseAdapter, bool) {
	adapter, exists := dm.adapters[name]
	return adapter, exists
}

// GetDefaultAdapter returns the default database adapter
func (dm *DatabaseManager) GetDefaultAdapter() DatabaseAdapter {
	if dm.defaultAdapter == "" {
		return nil
	}
	return dm.adapters[dm.defaultAdapter]
}

// SetDefaultAdapter sets the default database adapter
func (dm *DatabaseManager) SetDefaultAdapter(name string) {
	if _, exists := dm.adapters[name]; exists {
		dm.defaultAdapter = name
	}
}

// CloseAll closes all database adapters
func (dm *DatabaseManager) CloseAll() error {
	var lastError error
	for name, adapter := range dm.adapters {
		if err := adapter.Close(); err != nil {
			lastError = err
		}
		delete(dm.adapters, name)
	}
	return lastError
} 