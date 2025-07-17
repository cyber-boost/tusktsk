// Package protection provides TuskLang SDK Protection Core Module
// Enterprise-grade protection for Go SDK

package protection

import (
	"crypto/aes"
	"crypto/cipher"
	"crypto/hmac"
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"hash"
	"sync"
	"time"

	"github.com/google/uuid"
	"golang.org/x/crypto/pbkdf2"
)

// UsageMetrics tracks SDK usage statistics
type UsageMetrics struct {
	StartTime int64 `json:"start_time"`
	APICalls  int64 `json:"api_calls"`
	Errors    int64 `json:"errors"`
}

// Violation represents a security violation
type Violation struct {
	Timestamp        int64  `json:"timestamp"`
	SessionID        string `json:"session_id"`
	ViolationType    string `json:"violation_type"`
	Details          string `json:"details"`
	LicenseKeyPartial string `json:"license_key_partial"`
}

// TuskProtection provides core protection functionality
type TuskProtection struct {
	licenseKey      string
	apiKey          string
	sessionID       string
	encryptionKey   []byte
	integrityChecks map[string]string
	usageMetrics    UsageMetrics
	mutex           sync.RWMutex
}

// New creates a new TuskProtection instance
func New(licenseKey, apiKey string) *TuskProtection {
	sessionID := uuid.New().String()
	encryptionKey := deriveKey(licenseKey)
	startTime := time.Now().Unix()

	return &TuskProtection{
		licenseKey:      licenseKey,
		apiKey:          apiKey,
		sessionID:       sessionID,
		encryptionKey:   encryptionKey,
		integrityChecks: make(map[string]string),
		usageMetrics: UsageMetrics{
			StartTime: startTime,
			APICalls:  0,
			Errors:    0,
		},
	}
}

func deriveKey(password string) []byte {
	salt := []byte("tusklang_protection_salt")
	return pbkdf2.Key([]byte(password), salt, 100000, 32, sha256.New)
}

// ValidateLicense checks if the license key is valid
func (tp *TuskProtection) ValidateLicense() bool {
	if len(tp.licenseKey) < 32 {
		return false
	}

	hash := sha256.Sum256([]byte(tp.licenseKey))
	checksum := hex.EncodeToString(hash[:])
	return len(checksum) >= 4 && checksum[:4] == "tusk"
}

// EncryptData encrypts sensitive data
func (tp *TuskProtection) EncryptData(data string) string {
	block, err := aes.NewCipher(tp.encryptionKey)
	if err != nil {
		return data
	}

	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return data
	}

	nonce := make([]byte, gcm.NonceSize())
	if _, err := rand.Read(nonce); err != nil {
		return data
	}

	ciphertext := gcm.Seal(nonce, nonce, []byte(data), nil)
	return base64.StdEncoding.EncodeToString(ciphertext)
}

// DecryptData decrypts sensitive data
func (tp *TuskProtection) DecryptData(encryptedData string) string {
	ciphertext, err := base64.StdEncoding.DecodeString(encryptedData)
	if err != nil {
		return encryptedData
	}

	block, err := aes.NewCipher(tp.encryptionKey)
	if err != nil {
		return encryptedData
	}

	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return encryptedData
	}

	if len(ciphertext) < gcm.NonceSize() {
		return encryptedData
	}

	nonce, ciphertext := ciphertext[:gcm.NonceSize()], ciphertext[gcm.NonceSize():]
	plaintext, err := gcm.Open(nil, nonce, ciphertext, nil)
	if err != nil {
		return encryptedData
	}

	return string(plaintext)
}

// VerifyIntegrity verifies data integrity using HMAC
func (tp *TuskProtection) VerifyIntegrity(data, signature string) bool {
	expectedSignature := tp.generateSignature(data)
	return hmac.Equal([]byte(signature), []byte(expectedSignature))
}

// GenerateSignature creates HMAC signature for data
func (tp *TuskProtection) GenerateSignature(data string) string {
	h := hmac.New(sha256.New, []byte(tp.apiKey))
	h.Write([]byte(data))
	return hex.EncodeToString(h.Sum(nil))
}

// TrackUsage records usage metrics
func (tp *TuskProtection) TrackUsage(operation string, success bool) {
	tp.mutex.Lock()
	defer tp.mutex.Unlock()

	tp.usageMetrics.APICalls++
	if !success {
		tp.usageMetrics.Errors++
	}
}

// GetMetrics returns usage metrics
func (tp *TuskProtection) GetMetrics() map[string]interface{} {
	tp.mutex.RLock()
	defer tp.mutex.RUnlock()

	uptime := time.Now().Unix() - tp.usageMetrics.StartTime
	return map[string]interface{}{
		"start_time":  tp.usageMetrics.StartTime,
		"api_calls":   tp.usageMetrics.APICalls,
		"errors":      tp.usageMetrics.Errors,
		"session_id":  tp.sessionID,
		"uptime":      uptime,
	}
}

// ObfuscateCode performs basic code obfuscation
func (tp *TuskProtection) ObfuscateCode(code string) string {
	return base64.StdEncoding.EncodeToString([]byte(code))
}

// DetectTampering checks if the SDK has been tampered with
func (tp *TuskProtection) DetectTampering() bool {
	// In production, implement file integrity checks
	// For now, return true as placeholder
	return true
}

// ReportViolation reports security violations
func (tp *TuskProtection) ReportViolation(violationType, details string) *Violation {
	violation := &Violation{
		Timestamp:        time.Now().Unix(),
		SessionID:        tp.sessionID,
		ViolationType:    violationType,
		Details:          details,
		LicenseKeyPartial: tp.licenseKey[:min(8, len(tp.licenseKey))] + "...",
	}

	fmt.Printf("SECURITY VIOLATION: %+v\n", violation)
	return violation
}

func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}

// Global protection instance
var (
	protectionInstance *TuskProtection
	instanceMutex      sync.RWMutex
)

// InitializeProtection creates the global protection instance
func InitializeProtection(licenseKey, apiKey string) *TuskProtection {
	instanceMutex.Lock()
	defer instanceMutex.Unlock()

	protectionInstance = New(licenseKey, apiKey)
	return protectionInstance
}

// GetProtection returns the global protection instance
func GetProtection() *TuskProtection {
	instanceMutex.RLock()
	defer instanceMutex.RUnlock()

	if protectionInstance == nil {
		panic("Protection not initialized. Call InitializeProtection() first.")
	}
	return protectionInstance
} 