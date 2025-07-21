// Package config provides configuration functionality for the TuskLang SDK
package config

import (
	"encoding/json"
	"fmt"
	"os"
	"strconv"
	"strings"
)

// Config represents a configuration manager
type Config struct {
	values map[string]interface{}
	file   string
}

// New creates a new Config instance
func New() *Config {
	return &Config{
		values: make(map[string]interface{}),
	}
}

// LoadFromFile loads configuration from a file
func (c *Config) LoadFromFile(filename string) error {
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read config file: %w", err)
	}
	
	c.file = filename
	
	// Determine file type and parse accordingly
	if strings.HasSuffix(filename, ".json") {
		return c.parseJSON(content)
	} else if strings.HasSuffix(filename, ".tsk") {
		return c.parseTSK(content)
	} else {
		// Default to TSK format
		return c.parseTSK(content)
	}
}

// SaveToFile saves configuration to a file
func (c *Config) SaveToFile(filename string) error {
	var content []byte
	var err error
	
	if strings.HasSuffix(filename, ".json") {
		content, err = json.MarshalIndent(c.values, "", "  ")
		if err != nil {
			return fmt.Errorf("failed to marshal JSON: %w", err)
		}
	} else {
		content = c.toTSK()
	}
	
	err = os.WriteFile(filename, content, 0644)
	if err != nil {
		return fmt.Errorf("failed to write config file: %w", err)
	}
	
	c.file = filename
	return nil
}

// Get gets a configuration value
func (c *Config) Get(key string) interface{} {
	return c.values[key]
}

// GetString gets a string configuration value
func (c *Config) GetString(key string) string {
	value := c.Get(key)
	if value == nil {
		return ""
	}
	
	switch v := value.(type) {
	case string:
		return v
	default:
		return fmt.Sprintf("%v", v)
	}
}

// GetInt gets an integer configuration value
func (c *Config) GetInt(key string) int {
	value := c.Get(key)
	if value == nil {
		return 0
	}
	
	switch v := value.(type) {
	case int:
		return v
	case float64:
		return int(v)
	case string:
		if num, err := strconv.Atoi(v); err == nil {
			return num
		}
	}
	
	return 0
}

// GetBool gets a boolean configuration value
func (c *Config) GetBool(key string) bool {
	value := c.Get(key)
	if value == nil {
		return false
	}
	
	switch v := value.(type) {
	case bool:
		return v
	case string:
		return strings.ToLower(v) == "true"
	case int:
		return v != 0
	}
	
	return false
}

// GetFloat gets a float configuration value
func (c *Config) GetFloat(key string) float64 {
	value := c.Get(key)
	if value == nil {
		return 0.0
	}
	
	switch v := value.(type) {
	case float64:
		return v
	case int:
		return float64(v)
	case string:
		if num, err := strconv.ParseFloat(v, 64); err == nil {
			return num
		}
	}
	
	return 0.0
}

// Set sets a configuration value
func (c *Config) Set(key string, value interface{}) {
	c.values[key] = value
}

// Has checks if a configuration key exists
func (c *Config) Has(key string) bool {
	_, exists := c.values[key]
	return exists
}

// Delete deletes a configuration key
func (c *Config) Delete(key string) {
	delete(c.values, key)
}

// Keys returns all configuration keys
func (c *Config) Keys() []string {
	keys := make([]string, 0, len(c.values))
	for key := range c.values {
		keys = append(keys, key)
	}
	return keys
}

// Values returns all configuration values
func (c *Config) Values() map[string]interface{} {
	return c.values
}

// Clear clears all configuration values
func (c *Config) Clear() {
	c.values = make(map[string]interface{})
}

// Merge merges another configuration into this one
func (c *Config) Merge(other *Config) {
	for key, value := range other.values {
		c.values[key] = value
	}
}

// parseJSON parses JSON configuration
func (c *Config) parseJSON(content []byte) error {
	return json.Unmarshal(content, &c.values)
}

// parseTSK parses TSK configuration
func (c *Config) parseTSK(content []byte) error {
	lines := strings.Split(string(content), "\n")
	
	for lineNum, line := range lines {
		lineNum++ // 1-based line numbers
		line = strings.TrimSpace(line)
		
		// Skip empty lines and comments
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}
		
		// Parse key-value pair
		colonIndex := strings.Index(line, ":")
		if colonIndex == -1 {
			continue // Skip invalid lines
		}
		
		key := strings.TrimSpace(line[:colonIndex])
		valueStr := strings.TrimSpace(line[colonIndex+1:])
		
		// Parse value
		value := c.parseValue(valueStr)
		c.values[key] = value
	}
	
	return nil
}

// parseValue parses a TSK value string
func (c *Config) parseValue(valueStr string) interface{} {
	// Remove quotes if present
	valueStr = strings.Trim(valueStr, `"'`)
	
	// Try to parse as number
	if num, err := strconv.Atoi(valueStr); err == nil {
		return num
	}
	
	if num, err := strconv.ParseFloat(valueStr, 64); err == nil {
		return num
	}
	
	// Try to parse as boolean
	switch strings.ToLower(valueStr) {
	case "true":
		return true
	case "false":
		return false
	}
	
	// Return as string
	return valueStr
}

// toTSK converts configuration to TSK format
func (c *Config) toTSK() []byte {
	var sb strings.Builder
	
	sb.WriteString("# TuskLang Configuration\n")
	sb.WriteString("# Generated by TuskLang Go SDK\n\n")
	
	for key, value := range c.values {
		sb.WriteString(fmt.Sprintf("%s: %v\n", key, value))
	}
	
	return []byte(sb.String())
}

// GetDefaultConfig returns default configuration
func GetDefaultConfig() *Config {
	config := New()
	
	// Set default values
	config.Set("version", "1.0.0")
	config.Set("debug", false)
	config.Set("log_level", "info")
	config.Set("max_file_size", 10485760) // 10MB
	config.Set("timeout", 30)
	config.Set("cache_enabled", true)
	config.Set("cache_size", 1000)
	
	return config
} 