package database

import (
	"fmt"
	"sync"

	"github.com/cyber-boost/tusktsk/pkg/database/adapters"
	"github.com/cyber-boost/tusktsk/pkg/orm"
)

// Framework provides the main database framework interface
type Framework struct {
	manager *DatabaseManager
	orm     *orm.ORM
	mu      sync.RWMutex
}

// NewFramework creates a new database framework instance
func NewFramework() *Framework {
	manager := NewDatabaseManager()
	
	// Register all available adapters
	manager.RegisterAdapter("sqlite", adapters.NewSQLiteAdapter())
	manager.RegisterAdapter("postgresql", adapters.NewPostgreSQLAdapter())
	
	return &Framework{
		manager: manager,
	}
}

// Connect establishes a connection to a database
func (f *Framework) Connect(adapterName, connectionString string) error {
	f.mu.Lock()
	defer f.mu.Unlock()
	
	// Get or create adapter
	adapter, exists := f.manager.GetAdapter(adapterName)
	if !exists {
		return fmt.Errorf("adapter '%s' not found", adapterName)
	}
	
	// Connect to database
	if err := adapter.Connect(connectionString); err != nil {
		return fmt.Errorf("failed to connect to database: %w", err)
	}
	
	// Initialize ORM with the connected adapter
	f.orm = orm.NewORM(adapter)
	
	return nil
}

// Disconnect closes all database connections
func (f *Framework) Disconnect() error {
	f.mu.Lock()
	defer f.mu.Unlock()
	
	return f.manager.CloseAll()
}

// GetAdapter returns a database adapter by name
func (f *Framework) GetAdapter(name string) (DatabaseAdapter, bool) {
	f.mu.RLock()
	defer f.mu.RUnlock()
	
	return f.manager.GetAdapter(name)
}

// GetORM returns the ORM instance
func (f *Framework) GetORM() *orm.ORM {
	f.mu.RLock()
	defer f.mu.RUnlock()
	
	return f.orm
}

// Query executes a raw SQL query
func (f *Framework) Query(query string, args ...interface{}) (*Result, error) {
	f.mu.RLock()
	defer f.mu.RUnlock()
	
	adapter := f.manager.GetDefaultAdapter()
	if adapter == nil {
		return nil, fmt.Errorf("no database adapter available")
	}
	
	return adapter.Query(query, args...)
}

// Execute executes a raw SQL command
func (f *Framework) Execute(query string, args ...interface{}) error {
	f.mu.RLock()
	defer f.mu.RUnlock()
	
	adapter := f.manager.GetDefaultAdapter()
	if adapter == nil {
		return fmt.Errorf("no database adapter available")
	}
	
	return adapter.Execute(query, args...)
}

// BeginTransaction starts a new transaction
func (f *Framework) BeginTransaction() (Transaction, error) {
	f.mu.RLock()
	defer f.mu.RUnlock()
	
	adapter := f.manager.GetDefaultAdapter()
	if adapter == nil {
		return nil, fmt.Errorf("no database adapter available")
	}
	
	return adapter.BeginTransaction()
}

// RegisterModel registers a model with the ORM
func (f *Framework) RegisterModel(model orm.Model) error {
	f.mu.Lock()
	defer f.mu.Unlock()
	
	if f.orm == nil {
		return fmt.Errorf("ORM not initialized - connect to database first")
	}
	
	return f.orm.RegisterModel(model)
}

// AutoMigrate runs automatic migrations for all registered models
func (f *Framework) AutoMigrate() error {
	f.mu.Lock()
	defer f.mu.Unlock()
	
	if f.orm == nil {
		return fmt.Errorf("ORM not initialized - connect to database first")
	}
	
	return f.orm.AutoMigrate()
}

// Create creates a new record
func (f *Framework) Create(model orm.Model) error {
	f.mu.Lock()
	defer f.mu.Unlock()
	
	if f.orm == nil {
		return fmt.Errorf("ORM not initialized - connect to database first")
	}
	
	return f.orm.Create(model)
}

// Find finds records by conditions
func (f *Framework) Find(model orm.Model, conditions map[string]interface{}) ([]orm.Model, error) {
	f.mu.RLock()
	defer f.mu.RUnlock()
	
	if f.orm == nil {
		return nil, fmt.Errorf("ORM not initialized - connect to database first")
	}
	
	return f.orm.Find(model, conditions)
}

// FindByID finds a record by ID
func (f *Framework) FindByID(model orm.Model, id interface{}) (orm.Model, error) {
	f.mu.RLock()
	defer f.mu.RUnlock()
	
	if f.orm == nil {
		return nil, fmt.Errorf("ORM not initialized - connect to database first")
	}
	
	return f.orm.FindByID(model, id)
}

// Update updates a record
func (f *Framework) Update(model orm.Model) error {
	f.mu.Lock()
	defer f.mu.Unlock()
	
	if f.orm == nil {
		return fmt.Errorf("ORM not initialized - connect to database first")
	}
	
	return f.orm.Update(model)
}

// Delete deletes a record
func (f *Framework) Delete(model orm.Model) error {
	f.mu.Lock()
	defer f.mu.Unlock()
	
	if f.orm == nil {
		return fmt.Errorf("ORM not initialized - connect to database first")
	}
	
	return f.orm.Delete(model)
}

// GetStats returns database statistics
func (f *Framework) GetStats() map[string]*Stats {
	f.mu.RLock()
	defer f.mu.RUnlock()
	
	stats := make(map[string]*Stats)
	for name, adapter := range f.manager.adapters {
		stats[name] = adapter.GetStats()
	}
	
	return stats
}

// IsConnected checks if any database is connected
func (f *Framework) IsConnected() bool {
	f.mu.RLock()
	defer f.mu.RUnlock()
	
	for _, adapter := range f.manager.adapters {
		if adapter.IsConnected() {
			return true
		}
	}
	
	return false
}

// Ping tests database connectivity
func (f *Framework) Ping() error {
	f.mu.RLock()
	defer f.mu.RUnlock()
	
	adapter := f.manager.GetDefaultAdapter()
	if adapter == nil {
		return fmt.Errorf("no database adapter available")
	}
	
	return adapter.Ping()
}

// Example usage functions

// ExampleUser demonstrates how to use the framework with a User model
type ExampleUser struct {
	orm.BaseModel
	Name  string `json:"name" db:"name" gorm:"not null"`
	Email string `json:"email" db:"email" gorm:"unique;not null"`
	Age   int    `json:"age" db:"age"`
}

func (u *ExampleUser) TableName() string {
	return "users"
}

func (u *ExampleUser) PrimaryKey() string {
	return "id"
}

func (u *ExampleUser) GetID() interface{} {
	return u.ID
}

func (u *ExampleUser) SetID(id interface{}) {
	if uintID, ok := id.(uint); ok {
		u.ID = uintID
	}
}

// ExamplePost demonstrates how to use the framework with a Post model
type ExamplePost struct {
	orm.BaseModel
	Title   string `json:"title" db:"title" gorm:"not null"`
	Content string `json:"content" db:"content" gorm:"type:text"`
	UserID  uint   `json:"user_id" db:"user_id" gorm:"not null"`
}

func (p *ExamplePost) TableName() string {
	return "posts"
}

func (p *ExamplePost) PrimaryKey() string {
	return "id"
}

func (p *ExamplePost) GetID() interface{} {
	return p.ID
}

func (p *ExamplePost) SetID(id interface{}) {
	if uintID, ok := id.(uint); ok {
		p.ID = uintID
	}
}

// ExampleUsage demonstrates how to use the database framework
func ExampleUsage() error {
	// Create framework instance
	db := NewFramework()
	defer db.Disconnect()
	
	// Connect to SQLite database
	if err := db.Connect("sqlite", "sqlite:./example.db"); err != nil {
		return fmt.Errorf("failed to connect: %w", err)
	}
	
	// Register models
	if err := db.RegisterModel(&ExampleUser{}); err != nil {
		return fmt.Errorf("failed to register user model: %w", err)
	}
	
	if err := db.RegisterModel(&ExamplePost{}); err != nil {
		return fmt.Errorf("failed to register post model: %w", err)
	}
	
	// Run migrations
	if err := db.AutoMigrate(); err != nil {
		return fmt.Errorf("failed to run migrations: %w", err)
	}
	
	// Create a user
	user := &ExampleUser{
		Name:  "John Doe",
		Email: "john@example.com",
		Age:   30,
	}
	
	if err := db.Create(user); err != nil {
		return fmt.Errorf("failed to create user: %w", err)
	}
	
	// Create a post
	post := &ExamplePost{
		Title:   "Hello World",
		Content: "This is my first post!",
		UserID:  user.ID,
	}
	
	if err := db.Create(post); err != nil {
		return fmt.Errorf("failed to create post: %w", err)
	}
	
	// Find users by condition
	users, err := db.Find(&ExampleUser{}, map[string]interface{}{
		"age": 30,
	})
	if err != nil {
		return fmt.Errorf("failed to find users: %w", err)
	}
	
	fmt.Printf("Found %d users\n", len(users))
	
	// Find user by ID
	foundUser, err := db.FindByID(&ExampleUser{}, user.ID)
	if err != nil {
		return fmt.Errorf("failed to find user: %w", err)
	}
	
	if foundUser != nil {
		fmt.Printf("Found user: %+v\n", foundUser)
	}
	
	// Update user
	user.Age = 31
	if err := db.Update(user); err != nil {
		return fmt.Errorf("failed to update user: %w", err)
	}
	
	// Raw query example
	result, err := db.Query("SELECT COUNT(*) as count FROM users")
	if err != nil {
		return fmt.Errorf("failed to execute query: %w", err)
	}
	
	if len(result.Rows) > 0 {
		count := result.Rows[0]["count"]
		fmt.Printf("Total users: %v\n", count)
	}
	
	return nil
} 