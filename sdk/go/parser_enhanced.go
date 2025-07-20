package main

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"regexp"
	"strconv"
	"strings"
	"time"
)

// EnhancedParser - TuskLang Enhanced for Go
// "We don't bow to any king" - Support ALL syntax styles
//
// Features:
// - Multiple grouping: [], {}, <>
// - $global vs section-local variables
// - Cross-file communication
// - Database queries (placeholder adapters)
// - All @ operators
// - Maximum flexibility
//
// DEFAULT CONFIG: peanut.tsk (the bridge of language grace)
type EnhancedParser struct {
	data               map[string]interface{}
	globalVariables    map[string]interface{}
	sectionVariables   map[string]interface{}
	cache              map[string]interface{}
	crossFileCache     map[string]interface{}
	currentSection     string
	inObject          bool
	objectKey         string
	peanutLoaded      bool
	
	// Standard peanut.tsk locations
	peanutLocations []string
}

// NewEnhancedParser creates a new enhanced TuskLang parser
func NewEnhancedParser() *EnhancedParser {
	return &EnhancedParser{
		data:             make(map[string]interface{}),
		globalVariables:  make(map[string]interface{}),
		sectionVariables: make(map[string]interface{}),
		cache:            make(map[string]interface{}),
		crossFileCache:   make(map[string]interface{}),
		peanutLocations: []string{
			"./peanut.tsk",
			"../peanut.tsk",
			"../../peanut.tsk",
			"/etc/tusklang/peanut.tsk",
			filepath.Join(os.Getenv("HOME"), ".config/tusklang/peanut.tsk"),
			os.Getenv("TUSKLANG_CONFIG"),
		},
	}
}

// LoadPeanut loads peanut.tsk if available
func (p *EnhancedParser) LoadPeanut() error {
	if p.peanutLoaded {
		return nil
	}
	
	p.peanutLoaded = true // Mark first to prevent recursion
	
	for _, location := range p.peanutLocations {
		if location == "" {
			continue
		}
		
		if _, err := os.Stat(location); err == nil {
			fmt.Printf("# Loading universal config from: %s\n", location)
			return p.ParseFile(location)
		}
	}
	
	return nil
}

// ParseValue parses TuskLang value with all syntax support
func (p *EnhancedParser) ParseValue(value string) interface{} {
	value = strings.TrimSpace(value)
	
	// Remove optional semicolon
	if strings.HasSuffix(value, ";") {
		value = strings.TrimSuffix(value, ";")
		value = strings.TrimSpace(value)
	}
	
	// Basic types
	switch value {
	case "true":
		return true
	case "false":
		return false
	case "null":
		return nil
	}
	
	// Numbers
	if num, err := strconv.Atoi(value); err == nil {
		return num
	}
	if num, err := strconv.ParseFloat(value, 64); err == nil {
		return num
	}
	
	// $variable references (global)
	if matched, _ := regexp.MatchString(`^\$[a-zA-Z_][a-zA-Z0-9_]*$`, value); matched {
		varName := value[1:]
		if val, exists := p.globalVariables[varName]; exists {
			return val
		}
		return ""
	}
	
	// Section-local variable references
	if p.currentSection != "" {
		if matched, _ := regexp.MatchString(`^[a-zA-Z_][a-zA-Z0-9_]*$`, value); matched {
			sectionKey := p.currentSection + "." + value
			if val, exists := p.sectionVariables[sectionKey]; exists {
				return val
			}
		}
	}
	
	// @date function
	dateRe := regexp.MustCompile(`^@date\(["'](.*)["']\)$`)
	if matches := dateRe.FindStringSubmatch(value); matches != nil {
		formatStr := matches[1]
		return p.executeDate(formatStr)
	}
	
	// @env function with default
	envRe := regexp.MustCompile(`^@env\(["']([^"']*)["'](?:,\s*(.+))?\)$`)
	if matches := envRe.FindStringSubmatch(value); matches != nil {
		envVar := matches[1]
		defaultVal := ""
		if len(matches) > 2 && matches[2] != "" {
			defaultVal = strings.Trim(matches[2], `"'`)
		}
		if envVal := os.Getenv(envVar); envVal != "" {
			return envVal
		}
		return defaultVal
	}
	
	// Ranges: 8000-9000
	rangeRe := regexp.MustCompile(`^(\d+)-(\d+)$`)
	if matches := rangeRe.FindStringSubmatch(value); matches != nil {
		min, _ := strconv.Atoi(matches[1])
		max, _ := strconv.Atoi(matches[2])
		return map[string]interface{}{
			"min":  min,
			"max":  max,
			"type": "range",
		}
	}
	
	// Arrays
	if strings.HasPrefix(value, "[") && strings.HasSuffix(value, "]") {
		return p.parseArray(value)
	}
	
	// Objects
	if strings.HasPrefix(value, "{") && strings.HasSuffix(value, "}") {
		return p.parseObject(value)
	}
	
	// Cross-file references: @file.tsk.get('key')
	crossGetRe := regexp.MustCompile(`^@([a-zA-Z0-9_-]+)\.tsk\.get\(["'](.*)["']\)$`)
	if matches := crossGetRe.FindStringSubmatch(value); matches != nil {
		fileName := matches[1]
		key := matches[2]
		return p.crossFileGet(fileName, key)
	}
	
	// Cross-file set: @file.tsk.set('key', value)
	crossSetRe := regexp.MustCompile(`^@([a-zA-Z0-9_-]+)\.tsk\.set\(["']([^"']*)["'],\s*(.+)\)$`)
	if matches := crossSetRe.FindStringSubmatch(value); matches != nil {
		fileName := matches[1]
		key := matches[2]
		val := matches[3]
		return p.crossFileSet(fileName, key, val)
	}
	
	// @query function
	queryRe := regexp.MustCompile(`^@query\(["'](.*)["'](.*)\)$`)
	if matches := queryRe.FindStringSubmatch(value); matches != nil {
		query := matches[1]
		return p.executeQuery(query)
	}
	
	// @ operators
	operatorRe := regexp.MustCompile(`^@([a-zA-Z_][a-zA-Z0-9_]*)\((.+)\)$`)
	if matches := operatorRe.FindStringSubmatch(value); matches != nil {
		operator := matches[1]
		params := matches[2]
		return p.executeOperator(operator, params)
	}
	
	// String concatenation
	if strings.Contains(value, " + ") {
		parts := strings.Split(value, " + ")
		result := ""
		for _, part := range parts {
			part = strings.TrimSpace(part)
			part = strings.Trim(part, `"'`)
			if !strings.HasPrefix(part, `"`) {
				parsedPart := p.ParseValue(part)
				result += fmt.Sprintf("%v", parsedPart)
			} else {
				result += part[1 : len(part)-1]
			}
		}
		return result
	}
	
	// Conditional/ternary: condition ? true_val : false_val
	ternaryRe := regexp.MustCompile(`(.+?)\s*\?\s*(.+?)\s*:\s*(.+)`)
	if matches := ternaryRe.FindStringSubmatch(value); matches != nil {
		condition := strings.TrimSpace(matches[1])
		trueVal := strings.TrimSpace(matches[2])
		falseVal := strings.TrimSpace(matches[3])
		
		if p.evaluateCondition(condition) {
			return p.ParseValue(trueVal)
		}
		return p.ParseValue(falseVal)
	}
	
	// Remove quotes from strings
	if (strings.HasPrefix(value, `"`) && strings.HasSuffix(value, `"`)) ||
		(strings.HasPrefix(value, `'`) && strings.HasSuffix(value, `'`)) {
		return value[1 : len(value)-1]
	}
	
	// Return as-is
	return value
}

// parseArray parses array syntax
func (p *EnhancedParser) parseArray(value string) []interface{} {
	content := strings.TrimSpace(value[1 : len(value)-1])
	if content == "" {
		return []interface{}{}
	}
	
	var items []interface{}
	current := ""
	depth := 0
	inString := false
	var quoteChar byte
	
	for i := 0; i < len(content); i++ {
		char := content[i]
		
		if (char == '"' || char == '\'') && !inString {
			inString = true
			quoteChar = char
		} else if char == quoteChar && inString {
			inString = false
			quoteChar = 0
		}
		
		if !inString {
			if char == '[' || char == '{' {
				depth++
			} else if char == ']' || char == '}' {
				depth--
			} else if char == ',' && depth == 0 {
				items = append(items, p.ParseValue(strings.TrimSpace(current)))
				current = ""
				continue
			}
		}
		
		current += string(char)
	}
	
	if strings.TrimSpace(current) != "" {
		items = append(items, p.ParseValue(strings.TrimSpace(current)))
	}
	
	return items
}

// parseObject parses object syntax
func (p *EnhancedParser) parseObject(value string) map[string]interface{} {
	content := strings.TrimSpace(value[1 : len(value)-1])
	if content == "" {
		return map[string]interface{}{}
	}
	
	var pairs []string
	current := ""
	depth := 0
	inString := false
	var quoteChar byte
	
	for i := 0; i < len(content); i++ {
		char := content[i]
		
		if (char == '"' || char == '\'') && !inString {
			inString = true
			quoteChar = char
		} else if char == quoteChar && inString {
			inString = false
			quoteChar = 0
		}
		
		if !inString {
			if char == '[' || char == '{' {
				depth++
			} else if char == ']' || char == '}' {
				depth--
			} else if char == ',' && depth == 0 {
				pairs = append(pairs, strings.TrimSpace(current))
				current = ""
				continue
			}
		}
		
		current += string(char)
	}
	
	if strings.TrimSpace(current) != "" {
		pairs = append(pairs, strings.TrimSpace(current))
	}
	
	obj := make(map[string]interface{})
	for _, pair := range pairs {
		if strings.Contains(pair, ":") {
			parts := strings.SplitN(pair, ":", 2)
			key := strings.TrimSpace(strings.Trim(parts[0], `"'`))
			val := strings.TrimSpace(parts[1])
			obj[key] = p.ParseValue(val)
		} else if strings.Contains(pair, "=") {
			parts := strings.SplitN(pair, "=", 2)
			key := strings.TrimSpace(strings.Trim(parts[0], `"'`))
			val := strings.TrimSpace(parts[1])
			obj[key] = p.ParseValue(val)
		}
	}
	
	return obj
}

// evaluateCondition evaluates conditions for ternary expressions
func (p *EnhancedParser) evaluateCondition(condition string) bool {
	condition = strings.TrimSpace(condition)
	
	// Simple equality check
	eqRe := regexp.MustCompile(`(.+?)\s*==\s*(.+)`)
	if matches := eqRe.FindStringSubmatch(condition); matches != nil {
		left := p.ParseValue(strings.TrimSpace(matches[1]))
		right := p.ParseValue(strings.TrimSpace(matches[2]))
		return fmt.Sprintf("%v", left) == fmt.Sprintf("%v", right)
	}
	
	// Not equal
	neRe := regexp.MustCompile(`(.+?)\s*!=\s*(.+)`)
	if matches := neRe.FindStringSubmatch(condition); matches != nil {
		left := p.ParseValue(strings.TrimSpace(matches[1]))
		right := p.ParseValue(strings.TrimSpace(matches[2]))
		return fmt.Sprintf("%v", left) != fmt.Sprintf("%v", right)
	}
	
	// Greater than
	gtRe := regexp.MustCompile(`(.+?)\s*>\s*(.+)`)
	if matches := gtRe.FindStringSubmatch(condition); matches != nil {
		left := p.ParseValue(strings.TrimSpace(matches[1]))
		right := p.ParseValue(strings.TrimSpace(matches[2]))
		
		if leftFloat, ok := left.(float64); ok {
			if rightFloat, ok := right.(float64); ok {
				return leftFloat > rightFloat
			}
		}
		return fmt.Sprintf("%v", left) > fmt.Sprintf("%v", right)
	}
	
	// Default: check if truthy
	value := p.ParseValue(condition)
	switch v := value.(type) {
	case bool:
		return v
	case string:
		return v != "" && v != "false" && v != "null" && v != "0"
	case int:
		return v != 0
	case float64:
		return v != 0.0
	}
	return value != nil
}

// crossFileGet gets value from another TSK file
func (p *EnhancedParser) crossFileGet(fileName, key string) interface{} {
	cacheKey := fileName + ":" + key
	
	// Check cache
	if val, exists := p.crossFileCache[cacheKey]; exists {
		return val
	}
	
	// Find file
	directories := []string{".", "./config", "..", "../config"}
	var filePath string
	
	for _, directory := range directories {
		potentialPath := filepath.Join(directory, fileName+".tsk")
		if _, err := os.Stat(potentialPath); err == nil {
			filePath = potentialPath
			break
		}
	}
	
	if filePath == "" {
		return ""
	}
	
	// Parse file and get value
	tempParser := NewEnhancedParser()
	if err := tempParser.ParseFile(filePath); err != nil {
		return ""
	}
	
	value := tempParser.Get(key)
	
	// Cache result
	p.crossFileCache[cacheKey] = value
	
	return value
}

// crossFileSet sets value in another TSK file (cache only for now)
func (p *EnhancedParser) crossFileSet(fileName, key, value string) interface{} {
	cacheKey := fileName + ":" + key
	parsedValue := p.ParseValue(value)
	p.crossFileCache[cacheKey] = parsedValue
	return parsedValue
}

// executeDate executes @date function
func (p *EnhancedParser) executeDate(formatStr string) string {
	now := time.Now()
	
	// Convert PHP-style format to Go
	formatMap := map[string]string{
		"Y":           "2006",
		"Y-m-d":       "2006-01-02",
		"Y-m-d H:i:s": "2006-01-02 15:04:05",
		"c":           time.RFC3339,
	}
	
	if goFormat, exists := formatMap[formatStr]; exists {
		return now.Format(goFormat)
	}
	return now.Format("2006-01-02 15:04:05")
}

// executeQuery executes database query (placeholder for now)
func (p *EnhancedParser) executeQuery(query string) interface{} {
	p.LoadPeanut()
	
	// Determine database type
	dbType := "sqlite"
	if val := p.Get("database.default"); val != nil {
		dbType = fmt.Sprintf("%v", val)
	}
	
	// Placeholder implementation
	// In a real implementation, this would use database adapters
	return fmt.Sprintf("[Query: %s on %s]", query, dbType)
}

// executeOperator executes @ operators
func (p *EnhancedParser) executeOperator(operator, params string) interface{} {
	switch operator {
	case "cache":
		// Simple cache implementation
		parts := strings.SplitN(params, ",", 2)
		if len(parts) == 2 {
			ttl := strings.TrimSpace(strings.Trim(parts[0], `"'`))
			value := strings.TrimSpace(parts[1])
			parsedValue := p.ParseValue(value)
			return parsedValue
		}
		return ""
	case "learn", "optimize", "metrics", "feature":
		// Placeholders for advanced features
		return fmt.Sprintf("@%s(%s)", operator, params)
	default:
		return fmt.Sprintf("@%s(%s)", operator, params)
	}
}

// ParseLine parses a single line
func (p *EnhancedParser) ParseLine(line string) {
	trimmed := strings.TrimSpace(line)
	
	// Skip empty lines and comments
	if trimmed == "" || strings.HasPrefix(trimmed, "#") {
		return
	}
	
	// Remove optional semicolon
	if strings.HasSuffix(trimmed, ";") {
		trimmed = strings.TrimSuffix(trimmed, ";")
		trimmed = strings.TrimSpace(trimmed)
	}
	
	// Check for section declaration []
	sectionRe := regexp.MustCompile(`^\[([a-zA-Z_][a-zA-Z0-9_]*)\]$`)
	if matches := sectionRe.FindStringSubmatch(trimmed); matches != nil {
		p.currentSection = matches[1]
		p.inObject = false
		return
	}
	
	// Check for angle bracket object >
	angleOpenRe := regexp.MustCompile(`^([a-zA-Z_][a-zA-Z0-9_]*)\s*>$`)
	if matches := angleOpenRe.FindStringSubmatch(trimmed); matches != nil {
		p.inObject = true
		p.objectKey = matches[1]
		return
	}
	
	// Check for closing angle bracket <
	if trimmed == "<" {
		p.inObject = false
		p.objectKey = ""
		return
	}
	
	// Check for curly brace object {
	braceOpenRe := regexp.MustCompile(`^([a-zA-Z_][a-zA-Z0-9_]*)\s*\{$`)
	if matches := braceOpenRe.FindStringSubmatch(trimmed); matches != nil {
		p.inObject = true
		p.objectKey = matches[1]
		return
	}
	
	// Check for closing curly brace }
	if trimmed == "}" {
		p.inObject = false
		p.objectKey = ""
		return
	}
	
	// Parse key-value pairs (both : and = supported)
	kvRe := regexp.MustCompile(`^([\$]?[a-zA-Z_][a-zA-Z0-9_-]*)\s*[:=]\s*(.+)$`)
	if matches := kvRe.FindStringSubmatch(trimmed); matches != nil {
		key := matches[1]
		value := matches[2]
		parsedValue := p.ParseValue(value)
		
		// Determine storage location
		var storageKey string
		if p.inObject && p.objectKey != "" {
			if p.currentSection != "" {
				storageKey = p.currentSection + "." + p.objectKey + "." + key
			} else {
				storageKey = p.objectKey + "." + key
			}
		} else if p.currentSection != "" {
			storageKey = p.currentSection + "." + key
		} else {
			storageKey = key
		}
		
		// Store the value
		p.data[storageKey] = parsedValue
		
		// Handle global variables
		if strings.HasPrefix(key, "$") {
			varName := key[1:]
			p.globalVariables[varName] = parsedValue
		} else if p.currentSection != "" && !strings.HasPrefix(key, "$") {
			// Store section-local variable
			sectionKey := p.currentSection + "." + key
			p.sectionVariables[sectionKey] = parsedValue
		}
	}
}

// Parse parses TuskLang content
func (p *EnhancedParser) Parse(content string) map[string]interface{} {
	lines := strings.Split(content, "\n")
	
	for _, line := range lines {
		p.ParseLine(line)
	}
	
	return p.data
}

// ParseFile parses a TSK file
func (p *EnhancedParser) ParseFile(filePath string) error {
	content, err := os.ReadFile(filePath)
	if err != nil {
		return err
	}
	
	p.Parse(string(content))
	return nil
}

// Get gets a value by key
func (p *EnhancedParser) Get(key string) interface{} {
	return p.data[key]
}

// Set sets a value
func (p *EnhancedParser) Set(key string, value interface{}) {
	p.data[key] = value
}

// Keys gets all keys
func (p *EnhancedParser) Keys() []string {
	keys := make([]string, 0, len(p.data))
	for key := range p.data {
		keys = append(keys, key)
	}
	return keys
}

// Items gets all key-value pairs
func (p *EnhancedParser) Items() map[string]interface{} {
	result := make(map[string]interface{})
	for key, value := range p.data {
		result[key] = value
	}
	return result
}

// LoadFromPeanut loads configuration from peanut.tsk
func LoadFromPeanut() *EnhancedParser {
	parser := NewEnhancedParser()
	parser.LoadPeanut()
	return parser
}