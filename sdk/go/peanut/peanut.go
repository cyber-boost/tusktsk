// Package peanut provides hierarchical configuration with binary compilation
// Part of TuskLang Go SDK
package peanut

import (
	"bytes"
	"crypto/sha256"
	"encoding/binary"
	"encoding/gob"
	"encoding/json"
	"errors"
	"fmt"
	"io/ioutil"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"
)

const (
	// Magic number for binary format
	Magic = "PNUT"
	// Current binary version
	Version = 1
)

// ConfigFile represents a configuration file in the hierarchy
type ConfigFile struct {
	Path  string
	Type  string // "binary", "tsk", "text"
	MTime time.Time
}

// Config provides hierarchical configuration with binary compilation
type Config struct {
	cache       map[string]interface{}
	cacheMutex  sync.RWMutex
	autoCompile bool
	watch       bool
}

// New creates a new PeanutConfig instance
func New(autoCompile, watch bool) *Config {
	return &Config{
		cache:       make(map[string]interface{}),
		autoCompile: autoCompile,
		watch:       watch,
	}
}

// FindConfigHierarchy finds peanut configuration files in directory hierarchy
func (c *Config) FindConfigHierarchy(startDir string) ([]ConfigFile, error) {
	var configs []ConfigFile
	
	absDir, err := filepath.Abs(startDir)
	if err != nil {
		return nil, err
	}
	
	// Walk up directory tree
	for dir := absDir; dir != filepath.Dir(dir); dir = filepath.Dir(dir) {
		// Check for config files
		binaryPath := filepath.Join(dir, "peanu.pnt")
		tskPath := filepath.Join(dir, "peanu.tsk")
		textPath := filepath.Join(dir, "peanu.peanuts")
		
		if info, err := os.Stat(binaryPath); err == nil {
			configs = append(configs, ConfigFile{
				Path:  binaryPath,
				Type:  "binary",
				MTime: info.ModTime(),
			})
		} else if info, err := os.Stat(tskPath); err == nil {
			configs = append(configs, ConfigFile{
				Path:  tskPath,
				Type:  "tsk",
				MTime: info.ModTime(),
			})
		} else if info, err := os.Stat(textPath); err == nil {
			configs = append(configs, ConfigFile{
				Path:  textPath,
				Type:  "text",
				MTime: info.ModTime(),
			})
		}
	}
	
	// Check for global peanut.tsk
	globalConfig := filepath.Join(".", "peanut.tsk")
	if info, err := os.Stat(globalConfig); err == nil {
		configs = append([]ConfigFile{{
			Path:  globalConfig,
			Type:  "tsk",
			MTime: info.ModTime(),
		}}, configs...)
	}
	
	// Reverse to get root->current order
	for i, j := 0, len(configs)-1; i < j; i, j = i+1, j-1 {
		configs[i], configs[j] = configs[j], configs[i]
	}
	
	return configs, nil
}

// ParseTextConfig parses text-based peanut configuration
func (c *Config) ParseTextConfig(content string) (map[string]interface{}, error) {
	config := make(map[string]interface{})
	currentSection := config
	
	lines := strings.Split(content, "\n")
	for _, line := range lines {
		line = strings.TrimSpace(line)
		
		// Skip comments and empty lines
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}
		
		// Section header
		if strings.HasPrefix(line, "[") && strings.HasSuffix(line, "]") {
			sectionName := line[1 : len(line)-1]
			newSection := make(map[string]interface{})
			config[sectionName] = newSection
			currentSection = newSection
			continue
		}
		
		// Key-value pair
		if idx := strings.Index(line, ":"); idx > 0 {
			key := strings.TrimSpace(line[:idx])
			value := strings.TrimSpace(line[idx+1:])
			currentSection[key] = c.parseValue(value)
		}
	}
	
	return config, nil
}

// parseValue parses a value with type inference
func (c *Config) parseValue(value string) interface{} {
	// Remove quotes
	if (strings.HasPrefix(value, `"`) && strings.HasSuffix(value, `"`)) ||
		(strings.HasPrefix(value, `'`) && strings.HasSuffix(value, `'`)) {
		return value[1 : len(value)-1]
	}
	
	// Boolean
	if value == "true" {
		return true
	}
	if value == "false" {
		return false
	}
	
	// Number
	var intVal int64
	if _, err := fmt.Sscan(value, &intVal); err == nil {
		return intVal
	}
	
	var floatVal float64
	if _, err := fmt.Sscan(value, &floatVal); err == nil {
		return floatVal
	}
	
	// Null
	if strings.ToLower(value) == "null" {
		return nil
	}
	
	// Array (simple comma-separated)
	if strings.Contains(value, ",") {
		parts := strings.Split(value, ",")
		result := make([]interface{}, len(parts))
		for i, part := range parts {
			result[i] = c.parseValue(strings.TrimSpace(part))
		}
		return result
	}
	
	return value
}

// CompileToBinary compiles configuration to binary format
func (c *Config) CompileToBinary(config map[string]interface{}, outputPath string) error {
	// Create header
	header := make([]byte, 16)
	copy(header[0:4], []byte(Magic))
	binary.LittleEndian.PutUint32(header[4:8], Version)
	binary.LittleEndian.PutUint64(header[8:16], uint64(time.Now().Unix()))
	
	// Serialize config with gob
	var configBuf bytes.Buffer
	encoder := gob.NewEncoder(&configBuf)
	if err := encoder.Encode(config); err != nil {
		return err
	}
	configData := configBuf.Bytes()
	
	// Create checksum
	hash := sha256.Sum256(configData)
	checksum := hash[:8]
	
	// Write to file
	file, err := os.Create(outputPath)
	if err != nil {
		return err
	}
	defer file.Close()
	
	if _, err := file.Write(header); err != nil {
		return err
	}
	if _, err := file.Write(checksum); err != nil {
		return err
	}
	if _, err := file.Write(configData); err != nil {
		return err
	}
	
	// Also create intermediate .shell format
	shellPath := strings.Replace(outputPath, ".pnt", ".shell", 1)
	return c.compileToShell(config, shellPath)
}

// compileToShell compiles to intermediate shell format (70% faster than text)
func (c *Config) compileToShell(config map[string]interface{}, outputPath string) error {
	shellData := map[string]interface{}{
		"version":   Version,
		"timestamp": time.Now().Unix(),
		"data":      config,
	}
	
	data, err := json.MarshalIndent(shellData, "", "  ")
	if err != nil {
		return err
	}
	
	return ioutil.WriteFile(outputPath, data, 0644)
}

// LoadBinary loads binary configuration
func (c *Config) LoadBinary(filePath string) (map[string]interface{}, error) {
	data, err := ioutil.ReadFile(filePath)
	if err != nil {
		return nil, err
	}
	
	if len(data) < 24 {
		return nil, errors.New("invalid binary file: too short")
	}
	
	// Verify magic number
	if string(data[0:4]) != Magic {
		return nil, errors.New("invalid peanut binary file")
	}
	
	// Check version
	version := binary.LittleEndian.Uint32(data[4:8])
	if version > Version {
		return nil, fmt.Errorf("unsupported binary version: %d", version)
	}
	
	// Verify checksum
	storedChecksum := data[16:24]
	configData := data[24:]
	
	hash := sha256.Sum256(configData)
	calculatedChecksum := hash[:8]
	
	if !bytes.Equal(storedChecksum, calculatedChecksum) {
		return nil, errors.New("binary file corrupted (checksum mismatch)")
	}
	
	// Decode configuration
	decoder := gob.NewDecoder(bytes.NewReader(configData))
	var config map[string]interface{}
	if err := decoder.Decode(&config); err != nil {
		return nil, err
	}
	
	return config, nil
}

// deepMerge performs deep merge of configurations (CSS-like cascading)
func (c *Config) deepMerge(target, source map[string]interface{}) map[string]interface{} {
	output := make(map[string]interface{})
	
	// Copy target
	for k, v := range target {
		output[k] = v
	}
	
	// Merge source
	for k, v := range source {
		if existing, ok := output[k]; ok {
			// If both are maps, merge recursively
			if existingMap, ok1 := existing.(map[string]interface{}); ok1 {
				if sourceMap, ok2 := v.(map[string]interface{}); ok2 {
					output[k] = c.deepMerge(existingMap, sourceMap)
					continue
				}
			}
		}
		output[k] = v
	}
	
	return output
}

// Load loads configuration with inheritance
func (c *Config) Load(directory string) (map[string]interface{}, error) {
	if directory == "" {
		directory = "."
	}
	
	absDir, err := filepath.Abs(directory)
	if err != nil {
		return nil, err
	}
	
	// Check cache
	c.cacheMutex.RLock()
	if cached, ok := c.cache[absDir]; ok {
		c.cacheMutex.RUnlock()
		return cached.(map[string]interface{}), nil
	}
	c.cacheMutex.RUnlock()
	
	hierarchy, err := c.FindConfigHierarchy(directory)
	if err != nil {
		return nil, err
	}
	
	mergedConfig := make(map[string]interface{})
	
	// Load and merge configs from root to current
	for _, configFile := range hierarchy {
		var config map[string]interface{}
		
		switch configFile.Type {
		case "binary":
			config, err = c.LoadBinary(configFile.Path)
		case "tsk", "text":
			content, err := ioutil.ReadFile(configFile.Path)
			if err != nil {
				fmt.Printf("Error reading %s: %v\n", configFile.Path, err)
				continue
			}
			config, err = c.ParseTextConfig(string(content))
		}
		
		if err != nil {
			fmt.Printf("Error loading %s: %v\n", configFile.Path, err)
			continue
		}
		
		// Merge with CSS-like cascading
		mergedConfig = c.deepMerge(mergedConfig, config)
	}
	
	// Cache the result
	c.cacheMutex.Lock()
	c.cache[absDir] = mergedConfig
	c.cacheMutex.Unlock()
	
	// Auto-compile if enabled
	if c.autoCompile {
		c.autoCompileConfigs(hierarchy)
	}
	
	return mergedConfig, nil
}

// autoCompileConfigs auto-compiles text configs to binary
func (c *Config) autoCompileConfigs(hierarchy []ConfigFile) {
	for _, configFile := range hierarchy {
		if configFile.Type == "text" || configFile.Type == "tsk" {
			binaryPath := strings.Replace(configFile.Path, ".peanuts", ".pnt", 1)
			binaryPath = strings.Replace(binaryPath, ".tsk", ".pnt", 1)
			
			// Check if binary is outdated
			needCompile := false
			if info, err := os.Stat(binaryPath); err != nil {
				needCompile = true
			} else if configFile.MTime.After(info.ModTime()) {
				needCompile = true
			}
			
			if needCompile {
				content, err := ioutil.ReadFile(configFile.Path)
				if err != nil {
					fmt.Printf("Failed to read %s: %v\n", configFile.Path, err)
					continue
				}
				
				config, err := c.ParseTextConfig(string(content))
				if err != nil {
					fmt.Printf("Failed to parse %s: %v\n", configFile.Path, err)
					continue
				}
				
				if err := c.CompileToBinary(config, binaryPath); err != nil {
					fmt.Printf("Failed to compile %s: %v\n", configFile.Path, err)
					continue
				}
				
				fmt.Printf("Compiled %s to binary format\n", filepath.Base(configFile.Path))
			}
		}
	}
}

// Get retrieves a configuration value by path
func (c *Config) Get(keyPath string, defaultValue interface{}, directory string) interface{} {
	config, err := c.Load(directory)
	if err != nil {
		return defaultValue
	}
	
	keys := strings.Split(keyPath, ".")
	current := config
	
	for i, key := range keys {
		if val, ok := current[key]; ok {
			if i == len(keys)-1 {
				return val
			}
			if nextMap, ok := val.(map[string]interface{}); ok {
				current = nextMap
			} else {
				return defaultValue
			}
		} else {
			return defaultValue
		}
	}
	
	return defaultValue
}