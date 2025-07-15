package tusklanggo

import (
	"bufio"
	"fmt"
	"io"
	"regexp"
	"strconv"
	"strings"
	"unicode"
)

// Parser parses TuskLang (.tsk) files into Go data structures
// Supports key-value, nesting, arrays, and variable interpolation

type Parser struct {
	lines []string
	pos   int
	variables map[string]interface{}
}

// NewParser creates a new parser from an io.Reader
func NewParser(r io.Reader) *Parser {
	scanner := bufio.NewScanner(r)
	var lines []string
	for scanner.Scan() {
		lines = append(lines, scanner.Text())
	}
	return &Parser{
		lines: lines,
		variables: make(map[string]interface{}),
	}
}

// Parse parses the .tsk file and returns a generic map[string]interface{}
func (p *Parser) Parse() (map[string]interface{}, error) {
	result := make(map[string]interface{})
	
	for p.pos < len(p.lines) {
		line := strings.TrimSpace(p.lines[p.pos])
		
		// Skip empty lines and comments
		if line == "" || strings.HasPrefix(line, "#") {
			p.pos++
			continue
		}
		
		// Parse key-value pair or nested structure
		key, value, err := p.parseLine(line)
		if err != nil {
			return nil, fmt.Errorf("line %d: %w", p.pos+1, err)
		}
		
		if key != "" {
			result[key] = value
		}
		
		p.pos++
	}
	
	return result, nil
}

// parseLine parses a single line and returns key, value, and error
func (p *Parser) parseLine(line string) (string, interface{}, error) {
	// Check if it's an array item
	if strings.HasPrefix(line, "- ") {
		value := strings.TrimPrefix(line, "- ")
		return "", p.parseValue(value), nil
	}
	
	// Parse key-value pair
	colonIndex := strings.Index(line, ":")
	if colonIndex == -1 {
		return "", nil, fmt.Errorf("invalid line format: missing colon")
	}
	
	key := strings.TrimSpace(line[:colonIndex])
	valueStr := strings.TrimSpace(line[colonIndex+1:])
	
	// Handle nested structures
	if valueStr == "" {
		// Check if next line is indented (nested structure)
		if p.pos+1 < len(p.lines) {
			nextLine := p.lines[p.pos+1]
			if p.isIndented(nextLine) {
				nested, err := p.parseNestedStructure()
				if err != nil {
					return "", nil, err
				}
				return key, nested, nil
			}
		}
		return "", nil, fmt.Errorf("empty value for key: %s", key)
	}
	
	value := p.parseValue(valueStr)
	return key, value, nil
}

// parseValue parses a value string and returns the appropriate Go type
func (p *Parser) parseValue(valueStr string) interface{} {
	// Remove quotes if present
	valueStr = strings.Trim(valueStr, `"'`)
	
	// Check for variable interpolation
	if strings.Contains(valueStr, "$") {
		return p.interpolateVariables(valueStr)
	}
	
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

// parseNestedStructure parses indented nested structures
func (p *Parser) parseNestedStructure() (interface{}, error) {
	var result []interface{}
	isArray := false
	
	// Check if this is an array (all items start with "-")
	if p.pos+1 < len(p.lines) {
		nextLine := p.lines[p.pos+1]
		if strings.HasPrefix(strings.TrimSpace(nextLine), "- ") {
			isArray = true
		}
	}
	
	if isArray {
		// Parse as array
		for p.pos+1 < len(p.lines) {
			nextLine := p.lines[p.pos+1]
			if !p.isIndented(nextLine) {
				break
			}
			
			trimmed := strings.TrimSpace(nextLine)
			if strings.HasPrefix(trimmed, "- ") {
				value := strings.TrimPrefix(trimmed, "- ")
				result = append(result, p.parseValue(value))
			}
			p.pos++
		}
	} else {
		// Parse as nested object
		nestedMap := make(map[string]interface{})
		for p.pos+1 < len(p.lines) {
			nextLine := p.lines[p.pos+1]
			if !p.isIndented(nextLine) {
				break
			}
			
			key, value, err := p.parseLine(strings.TrimSpace(nextLine))
			if err != nil {
				return nil, err
			}
			if key != "" {
				nestedMap[key] = value
			}
			p.pos++
		}
		return nestedMap, nil
	}
	
	return result, nil
}

// isIndented checks if a line is indented (nested)
func (p *Parser) isIndented(line string) bool {
	if line == "" {
		return false
	}
	return unicode.IsSpace(rune(line[0]))
}

// interpolateVariables replaces $variable references with actual values
func (p *Parser) interpolateVariables(valueStr string) string {
	// Simple variable interpolation - replace $var with actual values
	re := regexp.MustCompile(`\$(\w+)`)
	return re.ReplaceAllStringFunc(valueStr, func(match string) string {
		varName := strings.TrimPrefix(match, "$")
		if val, exists := p.variables[varName]; exists {
			return fmt.Sprintf("%v", val)
		}
		return match // Keep original if variable not found
	})
}

// SetVariable sets a variable for interpolation
func (p *Parser) SetVariable(name string, value interface{}) {
	p.variables[name] = value
} 