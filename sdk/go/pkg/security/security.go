// Package security provides security functionality for the TuskLang SDK
package security

import (
	"crypto/aes"
	"crypto/cipher"
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"encoding/hex"
	"fmt"
	"io"
	"strings"
)

// SecurityManager represents a security manager
type SecurityManager struct{}

// New creates a new SecurityManager instance
func New() *SecurityManager {
	return &SecurityManager{}
}

// ValidationResult represents the result of security validation
type ValidationResult struct {
	Valid   bool
	Issues  []ValidationIssue
	Score   int // Security score 0-100
}

// ValidationIssue represents a security validation issue
type ValidationIssue struct {
	Type        IssueType
	Severity    Severity
	Message     string
	Line        int
	Column      int
	Recommendation string
}

// IssueType represents the type of security issue
type IssueType int

const (
	IssueTypeInjection IssueType = iota
	IssueTypeXSS
	IssueTypeCSRF
	IssueTypeAuthentication
	IssueTypeAuthorization
	IssueTypeDataExposure
	IssueTypeInsecureCrypto
	IssueTypeWeakPassword
	IssueTypeSQLInjection
	IssueTypeCommandInjection
)

// String returns the string representation of the issue type
func (it IssueType) String() string {
	switch it {
	case IssueTypeInjection:
		return "Injection"
	case IssueTypeXSS:
		return "XSS"
	case IssueTypeCSRF:
		return "CSRF"
	case IssueTypeAuthentication:
		return "Authentication"
	case IssueTypeAuthorization:
		return "Authorization"
	case IssueTypeDataExposure:
		return "DataExposure"
	case IssueTypeInsecureCrypto:
		return "InsecureCrypto"
	case IssueTypeWeakPassword:
		return "WeakPassword"
	case IssueTypeSQLInjection:
		return "SQLInjection"
	case IssueTypeCommandInjection:
		return "CommandInjection"
	default:
		return "Unknown"
	}
}

// Severity represents the severity of a security issue
type Severity int

const (
	SeverityLow Severity = iota
	SeverityMedium
	SeverityHigh
	SeverityCritical
)

// String returns the string representation of the severity
func (s Severity) String() string {
	switch s {
	case SeverityLow:
		return "Low"
	case SeverityMedium:
		return "Medium"
	case SeverityHigh:
		return "High"
	case SeverityCritical:
		return "Critical"
	default:
		return "Unknown"
	}
}

// ValidateCode validates TuskLang code for security issues
func (sm *SecurityManager) ValidateCode(code string) *ValidationResult {
	result := &ValidationResult{
		Valid:  true,
		Issues: []ValidationIssue{},
		Score:  100,
	}
	
	// Check for common security issues
	sm.checkInjectionVulnerabilities(code, result)
	sm.checkXSSVulnerabilities(code, result)
	sm.checkAuthenticationIssues(code, result)
	sm.checkDataExposure(code, result)
	sm.checkInsecureCrypto(code, result)
	
	// Calculate security score
	result.Score = sm.calculateSecurityScore(result.Issues)
	result.Valid = result.Score >= 70 // Consider valid if score >= 70
	
	return result
}

// Encrypt encrypts data using AES-256-GCM
func (sm *SecurityManager) Encrypt(data []byte, key []byte) ([]byte, error) {
	block, err := aes.NewCipher(key)
	if err != nil {
		return nil, fmt.Errorf("failed to create cipher: %w", err)
	}
	
	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return nil, fmt.Errorf("failed to create GCM: %w", err)
	}
	
	nonce := make([]byte, gcm.NonceSize())
	if _, err := io.ReadFull(rand.Reader, nonce); err != nil {
		return nil, fmt.Errorf("failed to generate nonce: %w", err)
	}
	
	ciphertext := gcm.Seal(nonce, nonce, data, nil)
	return ciphertext, nil
}

// Decrypt decrypts data using AES-256-GCM
func (sm *SecurityManager) Decrypt(data []byte, key []byte) ([]byte, error) {
	block, err := aes.NewCipher(key)
	if err != nil {
		return nil, fmt.Errorf("failed to create cipher: %w", err)
	}
	
	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return nil, fmt.Errorf("failed to create GCM: %w", err)
	}
	
	nonceSize := gcm.NonceSize()
	if len(data) < nonceSize {
		return nil, fmt.Errorf("ciphertext too short")
	}
	
	nonce, ciphertext := data[:nonceSize], data[nonceSize:]
	plaintext, err := gcm.Open(nil, nonce, ciphertext, nil)
	if err != nil {
		return nil, fmt.Errorf("failed to decrypt: %w", err)
	}
	
	return plaintext, nil
}

// HashPassword hashes a password using SHA-256
func (sm *SecurityManager) HashPassword(password string) string {
	hash := sha256.Sum256([]byte(password))
	return hex.EncodeToString(hash[:])
}

// VerifyPassword verifies a password against its hash
func (sm *SecurityManager) VerifyPassword(password, hash string) bool {
	expectedHash := sm.HashPassword(password)
	return expectedHash == hash
}

// GenerateSecureToken generates a secure random token
func (sm *SecurityManager) GenerateSecureToken(length int) (string, error) {
	bytes := make([]byte, length)
	if _, err := io.ReadFull(rand.Reader, bytes); err != nil {
		return "", fmt.Errorf("failed to generate random bytes: %w", err)
	}
	return base64.URLEncoding.EncodeToString(bytes), nil
}

// SanitizeInput sanitizes user input
func (sm *SecurityManager) SanitizeInput(input string) string {
	// Basic sanitization - remove potentially dangerous characters
	sanitized := strings.ReplaceAll(input, "<", "&lt;")
	sanitized = strings.ReplaceAll(sanitized, ">", "&gt;")
	sanitized = strings.ReplaceAll(sanitized, "\"", "&quot;")
	sanitized = strings.ReplaceAll(sanitized, "'", "&#x27;")
	sanitized = strings.ReplaceAll(sanitized, "&", "&amp;")
	return sanitized
}

// checkInjectionVulnerabilities checks for injection vulnerabilities
func (sm *SecurityManager) checkInjectionVulnerabilities(code string, result *ValidationResult) {
	dangerousPatterns := []string{
		"exec(", "system(", "eval(", "sql_", "mysql_", "pg_",
	}
	
	for _, pattern := range dangerousPatterns {
		if strings.Contains(strings.ToLower(code), pattern) {
			result.Issues = append(result.Issues, ValidationIssue{
				Type:        IssueTypeInjection,
				Severity:    SeverityHigh,
				Message:     fmt.Sprintf("Potential injection vulnerability: %s", pattern),
				Recommendation: "Use parameterized queries and avoid dynamic code execution",
			})
		}
	}
}

// checkXSSVulnerabilities checks for XSS vulnerabilities
func (sm *SecurityManager) checkXSSVulnerabilities(code string, result *ValidationResult) {
	xssPatterns := []string{
		"<script", "javascript:", "onload=", "onerror=",
	}
	
	for _, pattern := range xssPatterns {
		if strings.Contains(strings.ToLower(code), pattern) {
			result.Issues = append(result.Issues, ValidationIssue{
				Type:        IssueTypeXSS,
				Severity:    SeverityHigh,
				Message:     fmt.Sprintf("Potential XSS vulnerability: %s", pattern),
				Recommendation: "Sanitize user input and use Content Security Policy",
			})
		}
	}
}

// checkAuthenticationIssues checks for authentication issues
func (sm *SecurityManager) checkAuthenticationIssues(code string, result *ValidationResult) {
	authPatterns := []string{
		"password=", "passwd=", "pwd=", "secret=",
	}
	
	for _, pattern := range authPatterns {
		if strings.Contains(strings.ToLower(code), pattern) {
			result.Issues = append(result.Issues, ValidationIssue{
				Type:        IssueTypeAuthentication,
				Severity:    SeverityMedium,
				Message:     fmt.Sprintf("Potential authentication issue: %s", pattern),
				Recommendation: "Use secure authentication methods and avoid hardcoding credentials",
			})
		}
	}
}

// checkDataExposure checks for data exposure issues
func (sm *SecurityManager) checkDataExposure(code string, result *ValidationResult) {
	exposurePatterns := []string{
		"console.log", "print(", "echo", "debug(",
	}
	
	for _, pattern := range exposurePatterns {
		if strings.Contains(strings.ToLower(code), pattern) {
			result.Issues = append(result.Issues, ValidationIssue{
				Type:        IssueTypeDataExposure,
				Severity:    SeverityLow,
				Message:     fmt.Sprintf("Potential data exposure: %s", pattern),
				Recommendation: "Remove debug statements and sensitive data logging",
			})
		}
	}
}

// checkInsecureCrypto checks for insecure cryptography usage
func (sm *SecurityManager) checkInsecureCrypto(code string, result *ValidationResult) {
	insecurePatterns := []string{
		"md5(", "sha1(", "des(", "rc4(",
	}
	
	for _, pattern := range insecurePatterns {
		if strings.Contains(strings.ToLower(code), pattern) {
			result.Issues = append(result.Issues, ValidationIssue{
				Type:        IssueTypeInsecureCrypto,
				Severity:    SeverityHigh,
				Message:     fmt.Sprintf("Insecure cryptography: %s", pattern),
				Recommendation: "Use modern cryptographic algorithms (AES, SHA-256, etc.)",
			})
		}
	}
}

// calculateSecurityScore calculates the security score based on issues
func (sm *SecurityManager) calculateSecurityScore(issues []ValidationIssue) int {
	score := 100
	
	for _, issue := range issues {
		switch issue.Severity {
		case SeverityCritical:
			score -= 25
		case SeverityHigh:
			score -= 15
		case SeverityMedium:
			score -= 10
		case SeverityLow:
			score -= 5
		}
	}
	
	if score < 0 {
		score = 0
	}
	
	return score
} 