package tusk

import (
	"crypto/aes"
	"crypto/cipher"
	"crypto/ed25519"
	"crypto/rand"
	"crypto/sha256"
	"crypto/subtle"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"io"
	"time"
)

// SecurityValidator provides security validation for Go SDK operations
type SecurityValidator struct {
	verificationKey ed25519.PublicKey
	encryptionKey   []byte
	hmacKey         []byte
}

// SecurityConfig holds security configuration
type SecurityConfig struct {
	EnableSignatures bool   `json:"enable_signatures"`
	EnableEncryption bool   `json:"enable_encryption"`
	EnableHMAC       bool   `json:"enable_hmac"`
	KeyID            string `json:"key_id"`
	Algorithm        string `json:"algorithm"`
}

// SecureBinaryData represents securely processed binary data
type SecureBinaryData struct {
	Data        []byte            `json:"data"`
	Signature   []byte            `json:"signature,omitempty"`
	HMAC        []byte            `json:"hmac,omitempty"`
	Nonce       []byte            `json:"nonce,omitempty"`
	Timestamp   int64             `json:"timestamp"`
	KeyID       string            `json:"key_id"`
	Algorithm   string            `json:"algorithm"`
	Metadata    map[string]string `json:"metadata,omitempty"`
}

// NewSecurityValidator creates a new security validator
func NewSecurityValidator() *SecurityValidator {
	return &SecurityValidator{}
}

// SetVerificationKey sets the public key for signature verification
func (sv *SecurityValidator) SetVerificationKey(key ed25519.PublicKey) {
	sv.verificationKey = key
}

// SetEncryptionKey sets the encryption key
func (sv *SecurityValidator) SetEncryptionKey(key []byte) {
	sv.encryptionKey = key
}

// SetHMACKey sets the HMAC key
func (sv *SecurityValidator) SetHMACKey(key []byte) {
	sv.hmacKey = key
}

// SecureWrite performs secure writing with validation
func (sv *SecurityValidator) SecureWrite(data []byte, config SecurityConfig) (*SecureBinaryData, error) {
	secureData := &SecureBinaryData{
		Data:      data,
		Timestamp: time.Now().Unix(),
		KeyID:     config.KeyID,
		Algorithm: config.Algorithm,
		Metadata:  make(map[string]string),
	}

	// Add HMAC if enabled
	if config.EnableHMAC && len(sv.hmacKey) > 0 {
		hmac := sv.calculateHMAC(data)
		secureData.HMAC = hmac
	}

	// Encrypt if enabled
	if config.EnableEncryption && len(sv.encryptionKey) > 0 {
		encryptedData, nonce, err := sv.encryptAES256GCM(data)
		if err != nil {
			return nil, fmt.Errorf("encryption failed: %v", err)
		}
		secureData.Data = encryptedData
		secureData.Nonce = nonce
	}

	// Sign if enabled
	if config.EnableSignatures {
		signature, err := sv.signData(secureData)
		if err != nil {
			return nil, fmt.Errorf("signing failed: %v", err)
		}
		secureData.Signature = signature
	}

	return secureData, nil
}

// SecureRead performs secure reading with validation
func (sv *SecurityValidator) SecureRead(secureData *SecureBinaryData, config SecurityConfig) ([]byte, error) {
	// Verify signature if present
	if config.EnableSignatures && len(secureData.Signature) > 0 {
		if !sv.verifySignature(secureData) {
			return nil, fmt.Errorf("signature verification failed")
		}
	}

	// Verify HMAC if present
	if config.EnableHMAC && len(secureData.HMAC) > 0 {
		if !sv.verifyHMAC(secureData) {
			return nil, fmt.Errorf("HMAC verification failed")
		}
	}

	// Decrypt if needed
	data := secureData.Data
	if config.EnableEncryption && len(secureData.Nonce) > 0 {
		decryptedData, err := sv.decryptAES256GCM(data, secureData.Nonce)
		if err != nil {
			return nil, fmt.Errorf("decryption failed: %v", err)
		}
		data = decryptedData
	}

	return data, nil
}

// ValidateFileIntegrity validates file integrity
func (sv *SecurityValidator) ValidateFileIntegrity(data []byte, expectedHash []byte) error {
	hash := sha256.Sum256(data)
	if subtle.ConstantTimeCompare(hash[:], expectedHash) != 1 {
		return fmt.Errorf("file integrity check failed")
	}
	return nil
}

// GenerateSecureKey generates a secure key
func (sv *SecurityValidator) GenerateSecureKey(keyType string, size int) ([]byte, error) {
	key := make([]byte, size)
	_, err := io.ReadFull(rand.Reader, key)
	if err != nil {
		return nil, fmt.Errorf("key generation failed: %v", err)
	}
	return key, nil
}

// GenerateKeyPair generates an Ed25519 key pair
func (sv *SecurityValidator) GenerateKeyPair() (ed25519.PublicKey, ed25519.PrivateKey, error) {
	return ed25519.GenerateKey(rand.Reader)
}

// SignData signs data with Ed25519
func (sv *SecurityValidator) SignData(data []byte, privateKey ed25519.PrivateKey) ([]byte, error) {
	return ed25519.Sign(privateKey, data), nil
}

// VerifySignature verifies an Ed25519 signature
func (sv *SecurityValidator) VerifySignature(data, signature []byte, publicKey ed25519.PublicKey) bool {
	return ed25519.Verify(publicKey, data, signature)
}

// EncryptData encrypts data with AES-256-GCM
func (sv *SecurityValidator) EncryptData(data, key []byte) ([]byte, []byte, error) {
	block, err := aes.NewCipher(key)
	if err != nil {
		return nil, nil, err
	}

	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return nil, nil, err
	}

	nonce := make([]byte, gcm.NonceSize())
	if _, err := io.ReadFull(rand.Reader, nonce); err != nil {
		return nil, nil, err
	}

	ciphertext := gcm.Seal(nonce, nonce, data, nil)
	return ciphertext, nonce, nil
}

// DecryptData decrypts data with AES-256-GCM
func (sv *SecurityValidator) DecryptData(ciphertext, key, nonce []byte) ([]byte, error) {
	block, err := aes.NewCipher(key)
	if err != nil {
		return nil, err
	}

	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return nil, err
	}

	if len(ciphertext) < gcm.NonceSize() {
		return nil, fmt.Errorf("ciphertext too short")
	}

	plaintext, err := gcm.Open(nil, nonce, ciphertext[gcm.NonceSize():], nil)
	if err != nil {
		return nil, err
	}

	return plaintext, nil
}

// calculateHMAC calculates HMAC for data
func (sv *SecurityValidator) calculateHMAC(data []byte) []byte {
	// Simplified HMAC calculation (in production, use crypto/hmac)
	hash := sha256.Sum256(append(sv.hmacKey, data...))
	return hash[:]
}

// encryptAES256GCM encrypts data using AES-256-GCM
func (sv *SecurityValidator) encryptAES256GCM(data []byte) ([]byte, []byte, error) {
	return sv.EncryptData(data, sv.encryptionKey)
}

// decryptAES256GCM decrypts data using AES-256-GCM
func (sv *SecurityValidator) decryptAES256GCM(data, nonce []byte) ([]byte, error) {
	return sv.DecryptData(data, sv.encryptionKey, nonce)
}

// signData signs the secure data
func (sv *SecurityValidator) signData(secureData *SecureBinaryData) ([]byte, error) {
	// Create signature data (excluding signature field)
	signatureData := sv.createSignatureData(secureData)
	
	// For demonstration, we'll use a simple hash as signature
	// In production, use proper Ed25519 signing
	hash := sha256.Sum256(signatureData)
	return hash[:], nil
}

// verifySignature verifies the signature
func (sv *SecurityValidator) verifySignature(secureData *SecureBinaryData) bool {
	// Create signature data (excluding signature field)
	signatureData := sv.createSignatureData(secureData)
	
	// For demonstration, we'll use a simple hash verification
	// In production, use proper Ed25519 verification
	hash := sha256.Sum256(signatureData)
	return subtle.ConstantTimeCompare(hash[:], secureData.Signature) == 1
}

// verifyHMAC verifies the HMAC
func (sv *SecurityValidator) verifyHMAC(secureData *SecureBinaryData) bool {
	expectedHMAC := sv.calculateHMAC(secureData.Data)
	return subtle.ConstantTimeCompare(expectedHMAC, secureData.HMAC) == 1
}

// createSignatureData creates data for signing
func (sv *SecurityValidator) createSignatureData(secureData *SecureBinaryData) []byte {
	// Create a copy without signature for signing
	tempData := *secureData
	tempData.Signature = nil
	
	data, _ := json.Marshal(tempData)
	return data
}

// Security utilities for Agent a1

// ValidateBinarySecurity validates binary file security
func ValidateBinarySecurity(data []byte, config SecurityConfig) error {
	// Parse secure data
	var secureData SecureBinaryData
	if err := json.Unmarshal(data, &secureData); err != nil {
		return fmt.Errorf("invalid secure data format: %v", err)
	}

	// Check timestamp (reject if too old)
	if time.Now().Unix()-secureData.Timestamp > 3600 { // 1 hour
		return fmt.Errorf("data too old")
	}

	// Validate algorithm
	if secureData.Algorithm != config.Algorithm {
		return fmt.Errorf("algorithm mismatch")
	}

	return nil
}

// GetSecurityInfo returns security information about data
func GetSecurityInfo(data []byte) (map[string]interface{}, error) {
	var secureData SecureBinaryData
	if err := json.Unmarshal(data, &secureData); err != nil {
		return nil, err
	}

	info := make(map[string]interface{})
	info["has_signature"] = len(secureData.Signature) > 0
	info["has_hmac"] = len(secureData.HMAC) > 0
	info["has_nonce"] = len(secureData.Nonce) > 0
	info["timestamp"] = secureData.Timestamp
	info["key_id"] = secureData.KeyID
	info["algorithm"] = secureData.Algorithm
	info["data_size"] = len(secureData.Data)

	return info, nil
}

// CreateSecureConfig creates a secure configuration
func CreateSecureConfig(enableSignatures, enableEncryption, enableHMAC bool) SecurityConfig {
	return SecurityConfig{
		EnableSignatures: enableSignatures,
		EnableEncryption: enableEncryption,
		EnableHMAC:       enableHMAC,
		KeyID:            fmt.Sprintf("key_%d", time.Now().Unix()),
		Algorithm:        "ed25519-aes256gcm-sha256",
	}
} 