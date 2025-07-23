package orm

import (
	"database/sql"
	"fmt"
	"reflect"
	"strings"
	"time"

	"github.com/cyber-boost/tusktsk/pkg/databasetypes"
)

// Model interface that all models must implement
type Model interface {
	TableName() string
	PrimaryKey() string
	GetID() interface{}
	SetID(interface{})
}

// BaseModel provides common fields for all models
type BaseModel struct {
	ID        uint      `json:"id" db:"id" gorm:"primaryKey;autoIncrement"`
	CreatedAt time.Time `json:"created_at" db:"created_at" gorm:"autoCreateTime"`
	UpdatedAt time.Time `json:"updated_at" db:"updated_at" gorm:"autoUpdateTime"`
	DeletedAt *time.Time `json:"deleted_at,omitempty" db:"deleted_at" gorm:"index"`
}

// TableName returns the table name for BaseModel
func (bm *BaseModel) TableName() string {
	return "base_models"
}

// PrimaryKey returns the primary key field name
func (bm *BaseModel) PrimaryKey() string {
	return "id"
}

// GetID returns the ID value
func (bm *BaseModel) GetID() interface{} {
	return bm.ID
}

// SetID sets the ID value
func (bm *BaseModel) SetID(id interface{}) {
	if uintID, ok := id.(uint); ok {
		bm.ID = uintID
	}
}

// ORM provides the main ORM functionality
type ORM struct {
	db databasetypes.DatabaseAdapter
	models map[string]*ModelInfo
}

// ModelInfo contains metadata about a model
type ModelInfo struct {
	Model       Model
	TableName   string
	Fields      []FieldInfo
	Relations   []RelationInfo
	Indexes     []IndexInfo
	Constraints []ConstraintInfo
}

// FieldInfo contains information about a model field
type FieldInfo struct {
	Name         string
	Type         string
	DBType       string
	IsPrimary    bool
	IsAutoIncr   bool
	IsNullable   bool
	IsUnique     bool
	DefaultValue interface{}
	Size         int
	Precision    int
	Scale        int
	Tags         map[string]string
}

// RelationInfo contains information about model relationships
type RelationInfo struct {
	Name         string
	Type         RelationType
	Model        Model
	ForeignKey   string
	References   string
	OnDelete     string
	OnUpdate     string
	Through      string
}

// RelationType defines the type of relationship
type RelationType int

const (
	HasOne RelationType = iota
	HasMany
	BelongsTo
	ManyToMany
)

// IndexInfo contains information about database indexes
type IndexInfo struct {
	Name    string
	Fields  []string
	Type    string
	Unique  bool
}

// ConstraintInfo contains information about database constraints
type ConstraintInfo struct {
	Name      string
	Type      string
	Fields    []string
	Reference string
}

// NewORM creates a new ORM instance
func NewORM(db databasetypes.DatabaseAdapter) *ORM {
	return &ORM{
		db:     db,
		models: make(map[string]*ModelInfo),
	}
}

// RegisterModel registers a model with the ORM
func (orm *ORM) RegisterModel(model Model) error {
	modelInfo := &ModelInfo{
		Model:     model,
		TableName: model.TableName(),
		Fields:    make([]FieldInfo, 0),
		Relations: make([]RelationInfo, 0),
		Indexes:   make([]IndexInfo, 0),
		Constraints: make([]ConstraintInfo, 0),
	}
	
	// Analyze model structure
	if err := orm.analyzeModel(model, modelInfo); err != nil {
		return fmt.Errorf("failed to analyze model: %w", err)
	}
	
	orm.models[model.TableName()] = modelInfo
	return nil
}

// analyzeModel analyzes the structure of a model using reflection
func (orm *ORM) analyzeModel(model Model, info *ModelInfo) error {
	val := reflect.ValueOf(model)
	if val.Kind() == reflect.Ptr {
		val = val.Elem()
	}
	
	typ := val.Type()
	
	for i := 0; i < val.NumField(); i++ {
		field := val.Field(i)
		fieldType := typ.Field(i)
		
		// Skip unexported fields
		if !field.CanSet() {
			continue
		}
		
		fieldInfo := FieldInfo{
			Name:   fieldType.Name,
			Type:   field.Type().String(),
			Tags:   make(map[string]string),
		}
		
		// Parse struct tags
		tag := fieldType.Tag.Get("db")
		if tag != "" {
			fieldInfo.Tags["db"] = tag
		}
		
		gormTag := fieldType.Tag.Get("gorm")
		if gormTag != "" {
			fieldInfo.Tags["gorm"] = gormTag
			orm.parseGormTag(gormTag, &fieldInfo)
		}
		
		jsonTag := fieldType.Tag.Get("json")
		if jsonTag != "" {
			fieldInfo.Tags["json"] = jsonTag
		}
		
		// Determine database type
		fieldInfo.DBType = orm.getDBType(field.Type())
		
		info.Fields = append(info.Fields, fieldInfo)
	}
	
	return nil
}

// parseGormTag parses GORM-style tags
func (orm *ORM) parseGormTag(tag string, fieldInfo *FieldInfo) {
	parts := strings.Split(tag, ";")
	for _, part := range parts {
		part = strings.TrimSpace(part)
		
		switch {
		case part == "primaryKey":
			fieldInfo.IsPrimary = true
		case part == "autoIncrement":
			fieldInfo.IsAutoIncr = true
		case part == "not null":
			fieldInfo.IsNullable = false
		case part == "unique":
			fieldInfo.IsUnique = true
		case strings.HasPrefix(part, "size:"):
			if size, err := fmt.Sscanf(part, "size:%d", &fieldInfo.Size); err == nil {
				_ = size
			}
		case strings.HasPrefix(part, "default:"):
			defaultVal := strings.TrimPrefix(part, "default:")
			fieldInfo.DefaultValue = defaultVal
		}
	}
}

// getDBType maps Go types to database types
func (orm *ORM) getDBType(typ reflect.Type) string {
	switch typ.Kind() {
	case reflect.String:
		return "VARCHAR(255)"
	case reflect.Int, reflect.Int8, reflect.Int16, reflect.Int32:
		return "INT"
	case reflect.Int64:
		return "BIGINT"
	case reflect.Uint, reflect.Uint8, reflect.Uint16, reflect.Uint32:
		return "INT UNSIGNED"
	case reflect.Uint64:
		return "BIGINT UNSIGNED"
	case reflect.Float32:
		return "FLOAT"
	case reflect.Float64:
		return "DOUBLE"
	case reflect.Bool:
		return "BOOLEAN"
	case reflect.Struct:
		if typ == reflect.TypeOf(time.Time{}) {
			return "TIMESTAMP"
		}
		return "TEXT"
	case reflect.Slice:
		if typ.Elem().Kind() == reflect.Uint8 {
			return "BLOB"
		}
		return "TEXT"
	default:
		return "TEXT"
	}
}

// AutoMigrate automatically creates or updates database tables
func (orm *ORM) AutoMigrate() error {
	for tableName, modelInfo := range orm.models {
		if err := orm.migrateTable(tableName, modelInfo); err != nil {
			return fmt.Errorf("failed to migrate table %s: %w", tableName, err)
		}
	}
	return nil
}

// migrateTable creates or updates a single table
func (orm *ORM) migrateTable(tableName string, modelInfo *ModelInfo) error {
	// Check if table exists
	exists, err := orm.tableExists(tableName)
	if err != nil {
		return err
	}
	
	if !exists {
		// Create new table
		return orm.createTable(tableName, modelInfo)
	} else {
		// Update existing table
		return orm.updateTable(tableName, modelInfo)
	}
}

// tableExists checks if a table exists
func (orm *ORM) tableExists(tableName string) (bool, error) {
	query := "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = ?"
	result, err := orm.db.Query(query, tableName)
	if err != nil {
		return false, err
	}
	
	if len(result.Rows) > 0 {
		count := result.Rows[0]["count"]
		if countInt, ok := count.(int64); ok {
			return countInt > 0, nil
		}
	}
	
	return false, nil
}

// createTable creates a new table
func (orm *ORM) createTable(tableName string, modelInfo *ModelInfo) error {
	columns := make([]string, 0)
	
	for _, field := range modelInfo.Fields {
		columnDef := orm.buildColumnDefinition(field)
		columns = append(columns, columnDef)
	}
	
	// Add primary key constraint
	primaryKeys := make([]string, 0)
	for _, field := range modelInfo.Fields {
		if field.IsPrimary {
			primaryKeys = append(primaryKeys, field.Name)
		}
	}
	
	if len(primaryKeys) > 0 {
		columns = append(columns, fmt.Sprintf("PRIMARY KEY (%s)", strings.Join(primaryKeys, ", ")))
	}
	
	// Add unique constraints
	for _, field := range modelInfo.Fields {
		if field.IsUnique {
			columns = append(columns, fmt.Sprintf("UNIQUE (%s)", field.Name))
		}
	}
	
	query := fmt.Sprintf("CREATE TABLE %s (\n  %s\n)", tableName, strings.Join(columns, ",\n  "))
	
	return orm.db.Execute(query)
}

// buildColumnDefinition builds a column definition string
func (orm *ORM) buildColumnDefinition(field FieldInfo) string {
	parts := []string{field.Name, field.DBType}
	
	if !field.IsNullable {
		parts = append(parts, "NOT NULL")
	}
	
	if field.IsAutoIncr {
		parts = append(parts, "AUTO_INCREMENT")
	}
	
	if field.DefaultValue != nil {
		parts = append(parts, fmt.Sprintf("DEFAULT %v", field.DefaultValue))
	}
	
	return strings.Join(parts, " ")
}

// updateTable updates an existing table
func (orm *ORM) updateTable(tableName string, modelInfo *ModelInfo) error {
	// Get existing columns
	existingColumns, err := orm.getExistingColumns(tableName)
	if err != nil {
		return err
	}
	
	// Add missing columns
	for _, field := range modelInfo.Fields {
		if !orm.columnExists(existingColumns, field.Name) {
			columnDef := orm.buildColumnDefinition(field)
			query := fmt.Sprintf("ALTER TABLE %s ADD COLUMN %s", tableName, columnDef)
			if err := orm.db.Execute(query); err != nil {
				return fmt.Errorf("failed to add column %s: %w", field.Name, err)
			}
		}
	}
	
	return nil
}

// getExistingColumns gets the list of existing columns in a table
func (orm *ORM) getExistingColumns(tableName string) ([]string, error) {
	query := "SELECT column_name FROM information_schema.columns WHERE table_name = ?"
	result, err := orm.db.Query(query, tableName)
	if err != nil {
		return nil, err
	}
	
	columns := make([]string, 0)
	for _, row := range result.Rows {
		if columnName, ok := row["column_name"].(string); ok {
			columns = append(columns, columnName)
		}
	}
	
	return columns, nil
}

// columnExists checks if a column exists in the list
func (orm *ORM) columnExists(columns []string, columnName string) bool {
	for _, col := range columns {
		if col == columnName {
			return true
		}
	}
	return false
}

// Create creates a new record
func (orm *ORM) Create(model Model) error {
	tableName := model.TableName()
	modelInfo, exists := orm.models[tableName]
	if !exists {
		return fmt.Errorf("model %s not registered", tableName)
	}
	
	// Build INSERT query
	fields := make([]string, 0)
	placeholders := make([]string, 0)
	values := make([]interface{}, 0)
	
	val := reflect.ValueOf(model)
	if val.Kind() == reflect.Ptr {
		val = val.Elem()
	}
	
	for _, field := range modelInfo.Fields {
		if field.IsAutoIncr {
			continue // Skip auto-increment fields
		}
		
		fieldVal := val.FieldByName(field.Name)
		if !fieldVal.IsValid() {
			continue
		}
		
		fields = append(fields, field.Name)
		placeholders = append(placeholders, "?")
		values = append(values, fieldVal.Interface())
	}
	
	query := fmt.Sprintf("INSERT INTO %s (%s) VALUES (%s)",
		tableName,
		strings.Join(fields, ", "),
		strings.Join(placeholders, ", "))
	
	return orm.db.Execute(query, values...)
}

// Find finds records by conditions
func (orm *ORM) Find(model Model, conditions map[string]interface{}) ([]Model, error) {
	tableName := model.TableName()
	
	// Build SELECT query
	query := fmt.Sprintf("SELECT * FROM %s", tableName)
	values := make([]interface{}, 0)
	
	if len(conditions) > 0 {
		whereClauses := make([]string, 0)
		for field, value := range conditions {
			whereClauses = append(whereClauses, fmt.Sprintf("%s = ?", field))
			values = append(values, value)
		}
		query += " WHERE " + strings.Join(whereClauses, " AND ")
	}
	
	result, err := orm.db.Query(query, values...)
	if err != nil {
		return nil, err
	}
	
	// Convert results to models
	models := make([]Model, 0)
	for _, row := range result.Rows {
		newModel := orm.createModelInstance(model)
		if err := orm.scanRowToModel(row, newModel); err != nil {
			return nil, err
		}
		models = append(models, newModel)
	}
	
	return models, nil
}

// FindByID finds a record by ID
func (orm *ORM) FindByID(model Model, id interface{}) (Model, error) {
	tableName := model.TableName()
	primaryKey := model.PrimaryKey()
	
	query := fmt.Sprintf("SELECT * FROM %s WHERE %s = ? LIMIT 1", tableName, primaryKey)
	result, err := orm.db.Query(query, id)
	if err != nil {
		return nil, err
	}
	
	if len(result.Rows) == 0 {
		return nil, fmt.Errorf("record not found")
	}
	
	newModel := orm.createModelInstance(model)
	if err := orm.scanRowToModel(result.Rows[0], newModel); err != nil {
		return nil, err
	}
	
	return newModel, nil
}

// Update updates a record
func (orm *ORM) Update(model Model) error {
	tableName := model.TableName()
	primaryKey := model.PrimaryKey()
	id := model.GetID()
	
	// Build UPDATE query
	fields := make([]string, 0)
	values := make([]interface{}, 0)
	
	val := reflect.ValueOf(model)
	if val.Kind() == reflect.Ptr {
		val = val.Elem()
	}
	
	for _, field := range orm.models[tableName].Fields {
		if field.IsPrimary || field.IsAutoIncr {
			continue // Skip primary key and auto-increment fields
		}
		
		fieldVal := val.FieldByName(field.Name)
		if !fieldVal.IsValid() {
			continue
		}
		
		fields = append(fields, fmt.Sprintf("%s = ?", field.Name))
		values = append(values, fieldVal.Interface())
	}
	
	values = append(values, id) // Add ID for WHERE clause
	
	query := fmt.Sprintf("UPDATE %s SET %s WHERE %s = ?",
		tableName,
		strings.Join(fields, ", "),
		primaryKey)
	
	return orm.db.Execute(query, values...)
}

// Delete deletes a record
func (orm *ORM) Delete(model Model) error {
	tableName := model.TableName()
	primaryKey := model.PrimaryKey()
	id := model.GetID()
	
	query := fmt.Sprintf("DELETE FROM %s WHERE %s = ?", tableName, primaryKey)
	return orm.db.Execute(query, id)
}

// createModelInstance creates a new instance of the model type
func (orm *ORM) createModelInstance(model Model) Model {
	typ := reflect.TypeOf(model)
	if typ.Kind() == reflect.Ptr {
		typ = typ.Elem()
	}
	
	newVal := reflect.New(typ)
	newModel := newVal.Interface().(Model)
	return newModel
}

// scanRowToModel scans a database row into a model
func (orm *ORM) scanRowToModel(row map[string]interface{}, model Model) error {
	val := reflect.ValueOf(model)
	if val.Kind() == reflect.Ptr {
		val = val.Elem()
	}
	
	for fieldName, value := range row {
		field := val.FieldByName(fieldName)
		if !field.IsValid() || !field.CanSet() {
			continue
		}
		
		// Convert value to appropriate type
		if err := orm.setFieldValue(field, value); err != nil {
			return fmt.Errorf("failed to set field %s: %w", fieldName, err)
		}
	}
	
	return nil
}

// setFieldValue sets a field value with type conversion
func (orm *ORM) setFieldValue(field reflect.Value, value interface{}) error {
	if value == nil {
		field.Set(reflect.Zero(field.Type()))
		return nil
	}
	
	switch field.Kind() {
	case reflect.String:
		if str, ok := value.(string); ok {
			field.SetString(str)
		}
	case reflect.Int, reflect.Int8, reflect.Int16, reflect.Int32, reflect.Int64:
		if num, ok := value.(int64); ok {
			field.SetInt(num)
		}
	case reflect.Uint, reflect.Uint8, reflect.Uint16, reflect.Uint32, reflect.Uint64:
		if num, ok := value.(int64); ok {
			field.SetUint(uint64(num))
		}
	case reflect.Float32, reflect.Float64:
		if num, ok := value.(float64); ok {
			field.SetFloat(num)
		}
	case reflect.Bool:
		if b, ok := value.(bool); ok {
			field.SetBool(b)
		}
	case reflect.Struct:
		if field.Type() == reflect.TypeOf(time.Time{}) {
			if t, ok := value.(time.Time); ok {
				field.Set(reflect.ValueOf(t))
			}
		}
	}
	
	return nil
} 