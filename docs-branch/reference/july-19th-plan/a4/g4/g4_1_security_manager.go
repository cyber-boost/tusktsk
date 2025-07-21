package main

import (
	"crypto/aes"
	"crypto/cipher"
	"crypto/rand"
	"crypto/rsa"
	"crypto/sha256"
	"crypto/x509"
	"encoding/base64"
	"encoding/json"
	"encoding/pem"
	"fmt"
	"io"
	"time"
)

// SecurityLevel represents the security level for operations
type SecurityLevel int

const (
	SecurityLow SecurityLevel = iota
	SecurityMedium
	SecurityHigh
	SecurityCritical
)

// String returns the string representation of SecurityLevel
func (s SecurityLevel) String() string {
	switch s {
	case SecurityLow:
		return "low"
	case SecurityMedium:
		return "medium"
	case SecurityHigh:
		return "high"
	case SecurityCritical:
		return "critical"
	default:
		return "unknown"
	}
}

// SecurityManager provides comprehensive security capabilities
type SecurityManager struct {
	encryptionKey []byte
	privateKey    *rsa.PrivateKey
	publicKey     *rsa.PublicKey
	accessControl map[string][]string
	auditLog      []AuditEntry
}

// AuditEntry represents a security audit log entry
type AuditEntry struct {
	Timestamp   time.Time              `json:"timestamp"`
	UserID      string                 `json:"user_id"`
	Action      string                 `json:"action"`
	Resource    string                 `json:"resource"`
	Level       SecurityLevel          `json:"level"`
	Success     bool                   `json:"success"`
	Metadata    map[string]interface{} `json:"metadata"`
	IPAddress   string                 `json:"ip_address,omitempty"`
	UserAgent   string                 `json:"user_agent,omitempty"`
}

// NewSecurityManager creates a new security manager instance
func NewSecurityManager() *SecurityManager {
	return &SecurityManager{
		accessControl: make(map[string][]string),
		auditLog:      make([]AuditEntry, 0),
	}
}

// GenerateEncryptionKey generates a new AES encryption key
func (sm *SecurityManager) GenerateEncryptionKey() error {
	key := make([]byte, 32) // AES-256
	if _, err := io.ReadFull(rand.Reader, key); err != nil {
		return fmt.Errorf("failed to generate encryption key: %v", err)
	}
	sm.encryptionKey = key
	return nil
}

// GenerateKeyPair generates a new RSA key pair
func (sm *SecurityManager) GenerateKeyPair(bits int) error {
	privateKey, err := rsa.GenerateKey(rand.Reader, bits)
	if err != nil {
		return fmt.Errorf("failed to generate RSA key pair: %v", err)
	}
	
	sm.privateKey = privateKey
	sm.publicKey = &privateKey.PublicKey
	return nil
}

// EncryptData encrypts data using AES-256-GCM
func (sm *SecurityManager) EncryptData(data []byte) ([]byte, error) {
	if len(sm.encryptionKey) == 0 {
		return nil, fmt.Errorf("encryption key not set")
	}

	block, err := aes.NewCipher(sm.encryptionKey)
	if err != nil {
		return nil, fmt.Errorf("failed to create cipher: %v", err)
	}

	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return nil, fmt.Errorf("failed to create GCM: %v", err)
	}

	nonce := make([]byte, gcm.NonceSize())
	if _, err := io.ReadFull(rand.Reader, nonce); err != nil {
		return nil, fmt.Errorf("failed to generate nonce: %v", err)
	}

	ciphertext := gcm.Seal(nonce, nonce, data, nil)
	return ciphertext, nil
}

// DecryptData decrypts data using AES-256-GCM
func (sm *SecurityManager) DecryptData(ciphertext []byte) ([]byte, error) {
	if len(sm.encryptionKey) == 0 {
		return nil, fmt.Errorf("encryption key not set")
	}

	block, err := aes.NewCipher(sm.encryptionKey)
	if err != nil {
		return nil, fmt.Errorf("failed to create cipher: %v", err)
	}

	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return nil, fmt.Errorf("failed to create GCM: %v", err)
	}

	nonceSize := gcm.NonceSize()
	if len(ciphertext) < nonceSize {
		return nil, fmt.Errorf("ciphertext too short")
	}

	nonce, ciphertext := ciphertext[:nonceSize], ciphertext[nonceSize:]
	plaintext, err := gcm.Open(nil, nonce, ciphertext, nil)
	if err != nil {
		return nil, fmt.Errorf("failed to decrypt: %v", err)
	}

	return plaintext, nil
}

// EncryptWithPublicKey encrypts data using RSA public key
func (sm *SecurityManager) EncryptWithPublicKey(data []byte) ([]byte, error) {
	if sm.publicKey == nil {
		return nil, fmt.Errorf("public key not set")
	}

	hash := sha256.New()
	ciphertext, err := rsa.EncryptOAEP(hash, rand.Reader, sm.publicKey, data, nil)
	if err != nil {
		return nil, fmt.Errorf("failed to encrypt with public key: %v", err)
	}

	return ciphertext, nil
}

// DecryptWithPrivateKey decrypts data using RSA private key
func (sm *SecurityManager) DecryptWithPrivateKey(ciphertext []byte) ([]byte, error) {
	if sm.privateKey == nil {
		return nil, fmt.Errorf("private key not set")
	}

	hash := sha256.New()
	plaintext, err := rsa.DecryptOAEP(hash, rand.Reader, sm.privateKey, ciphertext, nil)
	if err != nil {
		return nil, fmt.Errorf("failed to decrypt with private key: %v", err)
	}

	return plaintext, nil
}

// HashPassword creates a secure hash of a password
func (sm *SecurityManager) HashPassword(password string) (string, error) {
	hash := sha256.Sum256([]byte(password))
	return base64.StdEncoding.EncodeToString(hash[:]), nil
}

// VerifyPassword verifies a password against its hash
func (sm *SecurityManager) VerifyPassword(password, hash string) bool {
	computedHash, err := sm.HashPassword(password)
	if err != nil {
		return false
	}
	return computedHash == hash
}

// AddAccessControl adds access control rules
func (sm *SecurityManager) AddAccessControl(userID string, resources []string) {
	sm.accessControl[userID] = append(sm.accessControl[userID], resources...)
}

// CheckAccess checks if a user has access to a resource
func (sm *SecurityManager) CheckAccess(userID, resource string) bool {
	resources, exists := sm.accessControl[userID]
	if !exists {
		return false
	}

	for _, r := range resources {
		if r == resource {
			return true
		}
	}
	return false
}

// LogAuditEntry logs a security audit entry
func (sm *SecurityManager) LogAuditEntry(entry AuditEntry) {
	entry.Timestamp = time.Now()
	sm.auditLog = append(sm.auditLog, entry)
}

// GetAuditLog returns the audit log
func (sm *SecurityManager) GetAuditLog() []AuditEntry {
	return sm.auditLog
}

// ExportPublicKey exports the public key in PEM format
func (sm *SecurityManager) ExportPublicKey() (string, error) {
	if sm.publicKey == nil {
		return "", fmt.Errorf("public key not set")
	}

	publicKeyBytes, err := x509.MarshalPKIXPublicKey(sm.publicKey)
	if err != nil {
		return "", fmt.Errorf("failed to marshal public key: %v", err)
	}

	publicKeyPEM := pem.EncodeToMemory(&pem.Block{
		Type:  "RSA PUBLIC KEY",
		Bytes: publicKeyBytes,
	})

	return string(publicKeyPEM), nil
}

// ImportPublicKey imports a public key from PEM format
func (sm *SecurityManager) ImportPublicKey(pemData string) error {
	block, _ := pem.Decode([]byte(pemData))
	if block == nil {
		return fmt.Errorf("failed to decode PEM block")
	}

	publicKey, err := x509.ParsePKIXPublicKey(block.Bytes)
	if err != nil {
		return fmt.Errorf("failed to parse public key: %v", err)
	}

	rsaPublicKey, ok := publicKey.(*rsa.PublicKey)
	if !ok {
		return fmt.Errorf("not an RSA public key")
	}

	sm.publicKey = rsaPublicKey
	return nil
}

// Example usage and testing
func main() {
	// Create security manager
	sm := NewSecurityManager()

	// Generate encryption key and key pair
	if err := sm.GenerateEncryptionKey(); err != nil {
		fmt.Printf("Error generating encryption key: %v\n", err)
		return
	}

	if err := sm.GenerateKeyPair(2048); err != nil {
		fmt.Printf("Error generating key pair: %v\n", err)
		return
	}

	// Test encryption/decryption
	originalData := []byte("Hello, Security Manager!")
	fmt.Printf("Original data: %s\n", string(originalData))

	encryptedData, err := sm.EncryptData(originalData)
	if err != nil {
		fmt.Printf("Error encrypting data: %v\n", err)
		return
	}
	fmt.Printf("Encrypted data: %x\n", encryptedData)

	decryptedData, err := sm.DecryptData(encryptedData)
	if err != nil {
		fmt.Printf("Error decrypting data: %v\n", err)
		return
	}
	fmt.Printf("Decrypted data: %s\n", string(decryptedData))

	// Test RSA encryption
	rsaEncrypted, err := sm.EncryptWithPublicKey(originalData)
	if err != nil {
		fmt.Printf("Error with RSA encryption: %v\n", err)
		return
	}

	rsaDecrypted, err := sm.DecryptWithPrivateKey(rsaEncrypted)
	if err != nil {
		fmt.Printf("Error with RSA decryption: %v\n", err)
		return
	}
	fmt.Printf("RSA decrypted: %s\n", string(rsaDecrypted))

	// Test password hashing
	password := "mySecurePassword123"
	hash, err := sm.HashPassword(password)
	if err != nil {
		fmt.Printf("Error hashing password: %v\n", err)
		return
	}
	fmt.Printf("Password hash: %s\n", hash)
	fmt.Printf("Password verification: %t\n", sm.VerifyPassword(password, hash))

	// Test access control
	sm.AddAccessControl("user1", []string{"config.read", "config.write"})
	fmt.Printf("User1 has config.read access: %t\n", sm.CheckAccess("user1", "config.read"))
	fmt.Printf("User1 has admin.access: %t\n", sm.CheckAccess("user1", "admin.access"))

	// Test audit logging
	sm.LogAuditEntry(AuditEntry{
		UserID:   "user1",
		Action:   "config.read",
		Resource: "config.json",
		Level:    SecurityMedium,
		Success:  true,
		Metadata: map[string]interface{}{
			"ip_address": "192.168.1.100",
			"user_agent": "Go-Client/1.0",
		},
	})

	// Export public key
	publicKeyPEM, err := sm.ExportPublicKey()
	if err != nil {
		fmt.Printf("Error exporting public key: %v\n", err)
		return
	}
	fmt.Printf("Public Key:\n%s\n", publicKeyPEM)

	// Display audit log
	fmt.Println("\nAudit Log:")
	for _, entry := range sm.GetAuditLog() {
		if data, err := json.MarshalIndent(entry, "", "  "); err == nil {
			fmt.Printf("%s\n", string(data))
		}
	}
} 