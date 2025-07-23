// Package core provides core TuskLang operators
package core

import (
	"crypto/md5"
	"crypto/sha1"
	"crypto/sha256"
	"encoding/base64"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"regexp"
	"strconv"
	"strings"
	"unicode"

	"github.com/google/uuid"
)

// StringOperator handles string and data operations
type StringOperator struct{}

// NewStringOperator creates a new string operator
func NewStringOperator() *StringOperator {
	return &StringOperator{}
}

// String executes @string operator
func (so *StringOperator) String(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return "", nil
	}
	
	if len(args) == 1 {
		return fmt.Sprintf("%v", args[0]), nil
	}
	
	// Multiple arguments - concatenate
	var result strings.Builder
	for _, arg := range args {
		result.WriteString(fmt.Sprintf("%v", arg))
	}
	return result.String(), nil
}

// Regex executes @regex operator
func (so *StringOperator) Regex(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@regex requires at least 2 arguments")
	}
	
	pattern, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@regex pattern must be string")
	}
	
	text, ok := args[1].(string)
	if !ok {
		return nil, fmt.Errorf("@regex text must be string")
	}
	
	regex, err := regexp.Compile(pattern)
	if err != nil {
		return nil, fmt.Errorf("invalid regex pattern: %v", err)
	}
	
	if len(args) == 2 {
		// Just check if matches
		return regex.MatchString(text), nil
	}
	
	action, ok := args[2].(string)
	if !ok {
		return nil, fmt.Errorf("@regex action must be string")
	}
	
	switch strings.ToLower(action) {
	case "match":
		return regex.MatchString(text), nil
	case "find":
		return regex.FindString(text), nil
	case "findall":
		return regex.FindAllString(text, -1), nil
	case "replace":
		if len(args) < 4 {
			return nil, fmt.Errorf("@regex replace requires replacement string")
		}
		replacement := fmt.Sprintf("%v", args[3])
		return regex.ReplaceAllString(text, replacement), nil
	case "split":
		return regex.Split(text, -1), nil
	default:
		return nil, fmt.Errorf("unknown regex action: %s", action)
	}
}

// JSON executes @json operator
func (so *StringOperator) JSON(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("@json requires at least 1 argument")
	}
	
	if len(args) == 1 {
		// Parse JSON
		jsonStr, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@json input must be string")
		}
		
		var result interface{}
		err := json.Unmarshal([]byte(jsonStr), &result)
		if err != nil {
			return nil, fmt.Errorf("invalid JSON: %v", err)
		}
		return result, nil
	}
	
	action, ok := args[1].(string)
	if !ok {
		return nil, fmt.Errorf("@json action must be string")
	}
	
	switch strings.ToLower(action) {
	case "parse":
		jsonStr, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@json input must be string")
		}
		
		var result interface{}
		err := json.Unmarshal([]byte(jsonStr), &result)
		if err != nil {
			return nil, fmt.Errorf("invalid JSON: %v", err)
		}
		return result, nil
	case "stringify":
		data, err := json.Marshal(args[0])
		if err != nil {
			return nil, fmt.Errorf("failed to marshal JSON: %v", err)
		}
		return string(data), nil
	case "pretty":
		data, err := json.MarshalIndent(args[0], "", "  ")
		if err != nil {
			return nil, fmt.Errorf("failed to marshal JSON: %v", err)
		}
		return string(data), nil
	case "get":
		if len(args) < 3 {
			return nil, fmt.Errorf("@json get requires path")
		}
		path, ok := args[2].(string)
		if !ok {
			return nil, fmt.Errorf("@json path must be string")
		}
		
		// Simple path extraction (e.g., "user.name")
		jsonStr, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@json input must be string")
		}
		
		var data interface{}
		err := json.Unmarshal([]byte(jsonStr), &data)
		if err != nil {
			return nil, fmt.Errorf("invalid JSON: %v", err)
		}
		
		return so.extractJSONPath(data, path), nil
	default:
		return nil, fmt.Errorf("unknown JSON action: %s", action)
	}
}

// extractJSONPath extracts value from JSON using dot notation
func (so *StringOperator) extractJSONPath(data interface{}, path string) interface{} {
	keys := strings.Split(path, ".")
	current := data
	
	for _, key := range keys {
		switch v := current.(type) {
		case map[string]interface{}:
			if val, exists := v[key]; exists {
				current = val
			} else {
				return nil
			}
		case map[interface{}]interface{}:
			if val, exists := v[key]; exists {
				current = val
			} else {
				return nil
			}
		default:
			return nil
		}
	}
	
	return current
}

// Base64 executes @base64 operator
func (so *StringOperator) Base64(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@base64 requires at least 2 arguments")
	}
	
	action, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@base64 action must be string")
	}
	
	data, ok := args[1].(string)
	if !ok {
		return nil, fmt.Errorf("@base64 data must be string")
	}
	
	switch strings.ToLower(action) {
	case "encode":
		return base64.StdEncoding.EncodeToString([]byte(data)), nil
	case "decode":
		decoded, err := base64.StdEncoding.DecodeString(data)
		if err != nil {
			return nil, fmt.Errorf("invalid base64: %v", err)
		}
		return string(decoded), nil
	case "encodeurl":
		return base64.URLEncoding.EncodeToString([]byte(data)), nil
	case "decodeurl":
		decoded, err := base64.URLEncoding.DecodeString(data)
		if err != nil {
			return nil, fmt.Errorf("invalid base64url: %v", err)
		}
		return string(decoded), nil
	default:
		return nil, fmt.Errorf("unknown base64 action: %s", action)
	}
}

// URL executes @url operator
func (so *StringOperator) URL(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@url requires at least 2 arguments")
	}
	
	action, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@url action must be string")
	}
	
	data, ok := args[1].(string)
	if !ok {
		return nil, fmt.Errorf("@url data must be string")
	}
	
	switch strings.ToLower(action) {
	case "encode":
		return so.urlEncode(data), nil
	case "decode":
		return so.urlDecode(data), nil
	case "encodecomponent":
		return so.urlEncodeComponent(data), nil
	case "decodecomponent":
		return so.urlDecodeComponent(data), nil
	default:
		return nil, fmt.Errorf("unknown URL action: %s", action)
	}
}

// urlEncode encodes URL
func (so *StringOperator) urlEncode(s string) string {
	return strings.ReplaceAll(s, " ", "%20")
}

// urlDecode decodes URL
func (so *StringOperator) urlDecode(s string) string {
	return strings.ReplaceAll(s, "%20", " ")
}

// urlEncodeComponent encodes URL component
func (so *StringOperator) urlEncodeComponent(s string) string {
	// Simple implementation - replace common characters
	replacements := map[string]string{
		" ": "%20", "!": "%21", "\"": "%22", "#": "%23", "$": "%24",
		"%": "%25", "&": "%26", "'": "%27", "(": "%28", ")": "%29",
		"*": "%2A", "+": "%2B", ",": "%2C", "-": "%2D", ".": "%2E",
		"/": "%2F", "0": "%30", "1": "%31", "2": "%32", "3": "%33",
		"4": "%34", "5": "%35", "6": "%36", "7": "%37", "8": "%38",
		"9": "%39", ":": "%3A", ";": "%3B", "<": "%3C", "=": "%3D",
		">": "%3E", "?": "%3F", "@": "%40", "A": "%41", "B": "%42",
		"C": "%43", "D": "%44", "E": "%45", "F": "%46", "G": "%47",
		"H": "%48", "I": "%49", "J": "%4A", "K": "%4B", "L": "%4C",
		"M": "%4D", "N": "%4E", "O": "%4F", "P": "%50", "Q": "%51",
		"R": "%52", "S": "%53", "T": "%54", "U": "%55", "V": "%56",
		"W": "%57", "X": "%58", "Y": "%59", "Z": "%5A", "[": "%5B",
		"\\": "%5C", "]": "%5D", "^": "%5E", "_": "%5F", "`": "%60",
		"a": "%61", "b": "%62", "c": "%63", "d": "%64", "e": "%65",
		"f": "%66", "g": "%67", "h": "%68", "i": "%69", "j": "%6A",
		"k": "%6B", "l": "%6C", "m": "%6D", "n": "%6E", "o": "%6F",
		"p": "%70", "q": "%71", "r": "%72", "s": "%73", "t": "%74",
		"u": "%75", "v": "%76", "w": "%77", "x": "%78", "y": "%79",
		"z": "%7A", "{": "%7B", "|": "%7C", "}": "%7D", "~": "%7E",
	}
	
	result := s
	for char, encoded := range replacements {
		result = strings.ReplaceAll(result, char, encoded)
	}
	return result
}

// urlDecodeComponent decodes URL component
func (so *StringOperator) urlDecodeComponent(s string) string {
	// Simple implementation - replace common encoded characters
	replacements := map[string]string{
		"%20": " ", "%21": "!", "%22": "\"", "%23": "#", "%24": "$",
		"%25": "%", "%26": "&", "%27": "'", "%28": "(", "%29": ")",
		"%2A": "*", "%2B": "+", "%2C": ",", "%2D": "-", "%2E": ".",
		"%2F": "/", "%30": "0", "%31": "1", "%32": "2", "%33": "3",
		"%34": "4", "%35": "5", "%36": "6", "%37": "7", "%38": "8",
		"%39": "9", "%3A": ":", "%3B": ";", "%3C": "<", "%3D": "=",
		"%3E": ">", "%3F": "?", "%40": "@", "%41": "A", "%42": "B",
		"%43": "C", "%44": "D", "%45": "E", "%46": "F", "%47": "G",
		"%48": "H", "%49": "I", "%4A": "J", "%4B": "K", "%4C": "L",
		"%4D": "M", "%4E": "N", "%4F": "O", "%50": "P", "%51": "Q",
		"%52": "R", "%53": "S", "%54": "T", "%55": "U", "%56": "V",
		"%57": "W", "%58": "X", "%59": "Y", "%5A": "Z", "%5B": "[",
		"%5C": "\\", "%5D": "]", "%5E": "^", "%5F": "_", "%60": "`",
		"%61": "a", "%62": "b", "%63": "c", "%64": "d", "%65": "e",
		"%66": "f", "%67": "g", "%68": "h", "%69": "i", "%6A": "j",
		"%6B": "k", "%6C": "l", "%6D": "m", "%6E": "n", "%6F": "o",
		"%70": "p", "%71": "q", "%72": "r", "%73": "s", "%74": "t",
		"%75": "u", "%76": "v", "%77": "w", "%78": "x", "%79": "y",
		"%7A": "z", "%7B": "{", "%7C": "|", "%7D": "}", "%7E": "~",
	}
	
	result := s
	for encoded, char := range replacements {
		result = strings.ReplaceAll(result, encoded, char)
	}
	return result
}

// Hash executes @hash operator
func (so *StringOperator) Hash(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@hash requires at least 2 arguments")
	}
	
	algorithm, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@hash algorithm must be string")
	}
	
	data, ok := args[1].(string)
	if !ok {
		return nil, fmt.Errorf("@hash data must be string")
	}
	
	switch strings.ToLower(algorithm) {
	case "md5":
		hash := md5.Sum([]byte(data))
		return hex.EncodeToString(hash[:]), nil
	case "sha1":
		hash := sha1.Sum([]byte(data))
		return hex.EncodeToString(hash[:]), nil
	case "sha256":
		hash := sha256.Sum256([]byte(data))
		return hex.EncodeToString(hash[:]), nil
	default:
		return nil, fmt.Errorf("unknown hash algorithm: %s", algorithm)
	}
}

// UUID executes @uuid operator
func (so *StringOperator) UUID(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return uuid.New().String(), nil
	}
	
	version, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@uuid version must be string")
	}
	
	switch strings.ToLower(version) {
	case "v4":
		return uuid.New().String(), nil
	case "v1":
		return uuid.New().String(), nil // Simplified - always returns v4
	case "nil":
		return uuid.Nil.String(), nil
	default:
		return nil, fmt.Errorf("unknown UUID version: %s", version)
	}
}

// String utility methods
func (so *StringOperator) ToUpper(s string) string {
	return strings.ToUpper(s)
}

func (so *StringOperator) ToLower(s string) string {
	return strings.ToLower(s)
}

func (so *StringOperator) Trim(s string) string {
	return strings.TrimSpace(s)
}

func (so *StringOperator) TrimLeft(s string) string {
	return strings.TrimLeftFunc(s, unicode.IsSpace)
}

func (so *StringOperator) TrimRight(s string) string {
	return strings.TrimRightFunc(s, unicode.IsSpace)
}

func (so *StringOperator) Replace(s, old, new string) string {
	return strings.ReplaceAll(s, old, new)
}

func (so *StringOperator) ReplaceN(s, old, new string, n int) string {
	return strings.Replace(s, old, new, n)
}

func (so *StringOperator) Split(s, sep string) []string {
	return strings.Split(s, sep)
}

func (so *StringOperator) Join(slice []string, sep string) string {
	return strings.Join(slice, sep)
}

func (so *StringOperator) Contains(s, substr string) bool {
	return strings.Contains(s, substr)
}

func (so *StringOperator) HasPrefix(s, prefix string) bool {
	return strings.HasPrefix(s, prefix)
}

func (so *StringOperator) HasSuffix(s, suffix string) bool {
	return strings.HasSuffix(s, suffix)
}

func (so *StringOperator) Index(s, substr string) int {
	return strings.Index(s, substr)
}

func (so *StringOperator) LastIndex(s, substr string) int {
	return strings.LastIndex(s, substr)
}

func (so *StringOperator) Count(s, substr string) int {
	return strings.Count(s, substr)
}

func (so *StringOperator) Repeat(s string, count int) string {
	return strings.Repeat(s, count)
}

func (so *StringOperator) Title(s string) string {
	return strings.Title(s)
}

func (so *StringOperator) ToTitle(s string) string {
	return strings.ToTitle(s)
}

func (so *StringOperator) Capitalize(s string) string {
	if len(s) == 0 {
		return s
	}
	return strings.ToUpper(s[:1]) + strings.ToLower(s[1:])
}

func (so *StringOperator) Reverse(s string) string {
	runes := []rune(s)
	for i, j := 0, len(runes)-1; i < j; i, j = i+1, j-1 {
		runes[i], runes[j] = runes[j], runes[i]
	}
	return string(runes)
}

func (so *StringOperator) IsNumeric(s string) bool {
	_, err := strconv.ParseFloat(s, 64)
	return err == nil
}

func (so *StringOperator) IsAlpha(s string) bool {
	for _, r := range s {
		if !unicode.IsLetter(r) {
			return false
		}
	}
	return true
}

func (so *StringOperator) IsAlphaNumeric(s string) bool {
	for _, r := range s {
		if !unicode.IsLetter(r) && !unicode.IsDigit(r) {
			return false
		}
	}
	return true
}

func (so *StringOperator) IsEmpty(s string) bool {
	return len(strings.TrimSpace(s)) == 0
}

func (so *StringOperator) Length(s string) int {
	return len(s)
}

func (so *StringOperator) Substring(s string, start, end int) string {
	if start < 0 {
		start = 0
	}
	if end > len(s) {
		end = len(s)
	}
	if start >= end {
		return ""
	}
	return s[start:end]
} 