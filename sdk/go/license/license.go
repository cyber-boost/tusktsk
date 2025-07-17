// Package license provides TuskLang SDK License Validation Module
// Enterprise-grade license validation for Go SDK

package license

import (
	"crypto/hmac"
	"crypto/md5"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"sync"
	"time"

	"github.com/google/uuid"
)

// LicenseInfo represents comprehensive license information
type LicenseInfo struct {
	LicenseKey      string                 `json:"license_key"`
	SessionID       string                 `json:"session_id"`
	Validation      ValidationResult       `json:"validation"`
	Expiration      ExpirationResult       `json:"expiration"`
	CacheStatus     string                 `json:"cache_status"`
	ValidationCount int                    `json:"validation_count"`
	Warnings        int                    `json:"warnings"`
	CachedData      map[string]interface{} `json:"cached_data,omitempty"`
	CacheAge        int64                  `json:"cache_age,omitempty"`
}

// ValidationResult represents license validation result
type ValidationResult struct {
	Valid    bool   `json:"valid"`
	Error    string `json:"error,omitempty"`
	Checksum string `json:"checksum,omitempty"`
}

// ExpirationResult represents license expiration information
type ExpirationResult struct {
	Expired        bool   `json:"expired"`
	Error          string `json:"error,omitempty"`
	ExpirationDate string `json:"expiration_date,omitempty"`
	DaysRemaining  int64  `json:"days_remaining,omitempty"`
	DaysOverdue    int64  `json:"days_overdue,omitempty"`
	Warning        bool   `json:"warning,omitempty"`
}

// ValidationAttempt represents a license validation attempt
type ValidationAttempt struct {
	Timestamp int64  `json:"timestamp"`
	Success   bool   `json:"success"`
	Details   string `json:"details"`
	SessionID string `json:"session_id"`
}

// ExpirationWarning represents an expiration warning
type ExpirationWarning struct {
	Timestamp      int64 `json:"timestamp"`
	DaysRemaining  int64 `json:"days_remaining"`
}

// TuskLicense provides license validation functionality
type TuskLicense struct {
	licenseKey         string
	apiKey             string
	sessionID          string
	licenseCache       map[string]LicenseCacheEntry
	validationHistory  []ValidationAttempt
	expirationWarnings []ExpirationWarning
	mutex              sync.RWMutex
	httpClient         *http.Client
	cacheDir           string
	cacheFile          string
	offlineCache       *OfflineCacheData
	logger             *log.Logger
}

// OfflineCacheData represents offline cached license data
type OfflineCacheData struct {
	LicenseKeyHash string                 `json:"license_key_hash"`
	LicenseData    map[string]interface{} `json:"license_data"`
	Timestamp      int64                  `json:"timestamp"`
	Expiration     ExpirationResult       `json:"expiration"`
}

// LicenseCacheEntry represents cached license data
type LicenseCacheEntry struct {
	Data      map[string]interface{} `json:"data"`
	Timestamp int64                  `json:"timestamp"`
	Expires   int64                  `json:"expires"`
}

// New creates a new TuskLicense instance
func New(licenseKey, apiKey string) *TuskLicense {
	return NewWithCacheDir(licenseKey, apiKey, "")
}

// NewWithCacheDir creates a new TuskLicense instance with custom cache directory
func NewWithCacheDir(licenseKey, apiKey, cacheDir string) *TuskLicense {
	// Set up cache directory
	if cacheDir == "" {
		homeDir, _ := os.UserHomeDir()
		cacheDir = filepath.Join(homeDir, ".tusk", "license_cache")
	}

	// Create cache directory if it doesn't exist
	os.MkdirAll(cacheDir, 0755)

	// Generate cache file name based on license key hash
	keyHash := md5.Sum([]byte(licenseKey))
	cacheFile := filepath.Join(cacheDir, fmt.Sprintf("%x.cache", keyHash))

	tl := &TuskLicense{
		licenseKey:         licenseKey,
		apiKey:             apiKey,
		sessionID:          uuid.New().String(),
		licenseCache:       make(map[string]LicenseCacheEntry),
		validationHistory:  make([]ValidationAttempt, 0),
		expirationWarnings: make([]ExpirationWarning, 0),
		httpClient:         &http.Client{Timeout: 10 * time.Second},
		cacheDir:           cacheDir,
		cacheFile:          cacheFile,
		logger:             log.New(os.Stderr, "[TuskLicense] ", log.LstdFlags),
	}

	// Load offline cache if exists
	tl.loadOfflineCache()

	return tl
}

// ValidateLicenseKey validates license key format and checksum
func (tl *TuskLicense) ValidateLicenseKey() ValidationResult {
	if len(tl.licenseKey) < 32 {
		return ValidationResult{
			Valid: false,
			Error: "Invalid license key format",
		}
	}

	if !strings.HasPrefix(tl.licenseKey, "TUSK-") {
		return ValidationResult{
			Valid: false,
			Error: "Invalid license key prefix",
		}
	}

	hash := sha256.Sum256([]byte(tl.licenseKey))
	checksum := hex.EncodeToString(hash[:])

	if !strings.HasPrefix(checksum, "tusk") {
		return ValidationResult{
			Valid: false,
			Error: "Invalid license key checksum",
		}
	}

	return ValidationResult{
		Valid:    true,
		Checksum: checksum,
	}
}

// VerifyLicenseServer verifies license with remote server
func (tl *TuskLicense) VerifyLicenseServer(serverURL string) (map[string]interface{}, error) {
	if serverURL == "" {
		serverURL = "https://api.tusklang.org/v1/license"
	}

	timestamp := time.Now().Unix()
	data := map[string]interface{}{
		"license_key": tl.licenseKey,
		"session_id":  tl.sessionID,
		"timestamp":   timestamp,
	}

	// Generate signature
	jsonData, err := json.Marshal(data)
	if err != nil {
		return nil, fmt.Errorf("failed to marshal data: %w", err)
	}

	h := hmac.New(sha256.New, []byte(tl.apiKey))
	h.Write(jsonData)
	signature := hex.EncodeToString(h.Sum(nil))
	data["signature"] = signature

	// Make HTTP request
	jsonPayload, err := json.Marshal(data)
	if err != nil {
		return nil, fmt.Errorf("failed to marshal payload: %w", err)
	}

	req, err := http.NewRequest("POST", serverURL, strings.NewReader(string(jsonPayload)))
	if err != nil {
		return nil, fmt.Errorf("failed to create request: %w", err)
	}

	req.Header.Set("Authorization", "Bearer "+tl.apiKey)
	req.Header.Set("Content-Type", "application/json")

	resp, err := tl.httpClient.Do(req)
	if err != nil {
		tl.logger.Printf("Network error during license validation: %v\n", err)
		return tl.fallbackToOfflineCache(fmt.Sprintf("network error: %v", err))
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		tl.logger.Printf("Server returned error: %d\n", resp.StatusCode)
		return tl.fallbackToOfflineCache(fmt.Sprintf("server error: %d", resp.StatusCode))
	}

	var result map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, fmt.Errorf("failed to decode response: %w", err)
	}

	// Cache the result
	tl.mutex.Lock()
	tl.licenseCache[tl.licenseKey] = LicenseCacheEntry{
		Data:      result,
		Timestamp: timestamp,
		Expires:   timestamp + 3600, // 1 hour cache
	}
	tl.mutex.Unlock()

	// Save to offline cache
	tl.saveOfflineCache(result)

	return result, nil
}

// CheckLicenseExpiration checks if license is expired or expiring soon
func (tl *TuskLicense) CheckLicenseExpiration() ExpirationResult {
	parts := strings.Split(tl.licenseKey, "-")
	if len(parts) < 4 {
		return ExpirationResult{
			Expired: true,
			Error:   "Invalid license key format",
		}
	}

	expirationStr := parts[len(parts)-1]
	expirationTimestamp, err := strconv.ParseInt(expirationStr, 16, 64)
	if err != nil {
		return ExpirationResult{
			Expired: true,
			Error:   "Invalid expiration timestamp",
		}
	}

	expirationDate := time.Unix(expirationTimestamp, 0)
	currentTime := time.Now()

	if expirationDate.Before(currentTime) {
		daysOverdue := int64(currentTime.Sub(expirationDate).Hours() / 24)
		return ExpirationResult{
			Expired:        true,
			ExpirationDate: expirationDate.Format(time.RFC3339),
			DaysOverdue:    daysOverdue,
		}
	}

	daysRemaining := int64(expirationDate.Sub(currentTime).Hours() / 24)

	if daysRemaining <= 30 {
		tl.mutex.Lock()
		tl.expirationWarnings = append(tl.expirationWarnings, ExpirationWarning{
			Timestamp:     currentTime.Unix(),
			DaysRemaining: daysRemaining,
		})
		tl.mutex.Unlock()
	}

	return ExpirationResult{
		Expired:        false,
		ExpirationDate: expirationDate.Format(time.RFC3339),
		DaysRemaining:  daysRemaining,
		Warning:        daysRemaining <= 30,
	}
}

// ValidateLicensePermissions validates if license allows specific feature
func (tl *TuskLicense) ValidateLicensePermissions(feature string) (bool, error) {
	tl.mutex.RLock()
	if cacheEntry, exists := tl.licenseCache[tl.licenseKey]; exists {
		if time.Now().Unix() < cacheEntry.Expires {
			if features, ok := cacheEntry.Data["features"].([]interface{}); ok {
				for _, f := range features {
					if fStr, ok := f.(string); ok && fStr == feature {
						tl.mutex.RUnlock()
						return true, nil
					}
				}
				tl.mutex.RUnlock()
				return false, fmt.Errorf("feature not licensed")
			}
		}
	}
	tl.mutex.RUnlock()

	// Fallback validation
	switch feature {
	case "basic", "core", "standard":
		return true, nil
	case "premium", "enterprise":
		upperKey := strings.ToUpper(tl.licenseKey)
		if strings.Contains(upperKey, "PREMIUM") || strings.Contains(upperKey, "ENTERPRISE") {
			return true, nil
		}
		return false, fmt.Errorf("premium license required")
	default:
		return false, fmt.Errorf("unknown feature")
	}
}

// GetLicenseInfo returns comprehensive license information
func (tl *TuskLicense) GetLicenseInfo() LicenseInfo {
	validationResult := tl.ValidateLicenseKey()
	expirationResult := tl.CheckLicenseExpiration()

	tl.mutex.RLock()
	cacheStatus := "not_cached"
	var cachedData map[string]interface{}
	var cacheAge int64

	if cacheEntry, exists := tl.licenseCache[tl.licenseKey]; exists {
		cacheStatus = "cached"
		cachedData = cacheEntry.Data
		cacheAge = time.Now().Unix() - cacheEntry.Timestamp
	}

	info := LicenseInfo{
		LicenseKey:      fmt.Sprintf("%s...%s", tl.licenseKey[:8], tl.licenseKey[len(tl.licenseKey)-4:]),
		SessionID:       tl.sessionID,
		Validation:      validationResult,
		Expiration:      expirationResult,
		CacheStatus:     cacheStatus,
		ValidationCount: len(tl.validationHistory),
		Warnings:        len(tl.expirationWarnings),
		CachedData:      cachedData,
		CacheAge:        cacheAge,
	}
	tl.mutex.RUnlock()

	return info
}

// LogValidationAttempt logs a license validation attempt
func (tl *TuskLicense) LogValidationAttempt(success bool, details string) {
	tl.mutex.Lock()
	tl.validationHistory = append(tl.validationHistory, ValidationAttempt{
		Timestamp: time.Now().Unix(),
		Success:   success,
		Details:   details,
		SessionID: tl.sessionID,
	})
	tl.mutex.Unlock()
}

// GetValidationHistory returns validation history
func (tl *TuskLicense) GetValidationHistory() []ValidationAttempt {
	tl.mutex.RLock()
	defer tl.mutex.RUnlock()
	
	history := make([]ValidationAttempt, len(tl.validationHistory))
	copy(history, tl.validationHistory)
	return history
}

// ClearValidationHistory clears validation history
func (tl *TuskLicense) ClearValidationHistory() {
	tl.mutex.Lock()
	tl.validationHistory = tl.validationHistory[:0]
	tl.mutex.Unlock()
}

// Global license instance
var (
	licenseInstance *TuskLicense
	instanceMutex   sync.RWMutex
)

// InitializeLicense creates the global license instance
func InitializeLicense(licenseKey, apiKey string) *TuskLicense {
	instanceMutex.Lock()
	defer instanceMutex.Unlock()

	licenseInstance = New(licenseKey, apiKey)
	return licenseInstance
}

// GetLicense returns the global license instance
func GetLicense() *TuskLicense {
	instanceMutex.RLock()
	defer instanceMutex.RUnlock()

	if licenseInstance == nil {
		panic("License not initialized. Call InitializeLicense() first.")
	}
	return licenseInstance
}

// loadOfflineCache loads offline license cache from disk
func (tl *TuskLicense) loadOfflineCache() {
	data, err := ioutil.ReadFile(tl.cacheFile)
	if err != nil {
		// Cache file doesn't exist or can't be read
		tl.offlineCache = nil
		return
	}

	var cached OfflineCacheData
	if err := json.Unmarshal(data, &cached); err != nil {
		tl.logger.Printf("Failed to parse offline cache: %v\n", err)
		tl.offlineCache = nil
		return
	}

	// Verify the cache is for the correct license key
	hash := sha256.Sum256([]byte(tl.licenseKey))
	keyHash := hex.EncodeToString(hash[:])
	if cached.LicenseKeyHash == keyHash {
		tl.offlineCache = &cached
		tl.logger.Println("Loaded offline license cache")
	} else {
		tl.logger.Println("Offline cache key mismatch")
		tl.offlineCache = nil
	}
}

// saveOfflineCache saves license data to offline cache
func (tl *TuskLicense) saveOfflineCache(licenseData map[string]interface{}) {
	hash := sha256.Sum256([]byte(tl.licenseKey))
	keyHash := hex.EncodeToString(hash[:])

	cacheData := OfflineCacheData{
		LicenseKeyHash: keyHash,
		LicenseData:    licenseData,
		Timestamp:      time.Now().Unix(),
		Expiration:     tl.CheckLicenseExpiration(),
	}

	data, err := json.MarshalIndent(cacheData, "", "  ")
	if err != nil {
		tl.logger.Printf("Failed to marshal cache data: %v\n", err)
		return
	}

	if err := ioutil.WriteFile(tl.cacheFile, data, 0600); err != nil {
		tl.logger.Printf("Failed to save offline cache: %v\n", err)
		return
	}

	tl.offlineCache = &cacheData
	tl.logger.Println("Saved license data to offline cache")
}

// fallbackToOfflineCache fallback to offline cache when server is unreachable
func (tl *TuskLicense) fallbackToOfflineCache(errorMsg string) (map[string]interface{}, error) {
	if tl.offlineCache != nil && tl.offlineCache.LicenseData != nil {
		cacheAge := time.Now().Unix() - tl.offlineCache.Timestamp
		cacheAgeDays := float64(cacheAge) / 86400.0

		// Check if cached license is not expired
		if !tl.offlineCache.Expiration.Expired {
			tl.logger.Printf("Using offline license cache (age: %.1f days)\n", cacheAgeDays)
			result := make(map[string]interface{})
			for k, v := range tl.offlineCache.LicenseData {
				result[k] = v
			}
			result["offline_mode"] = true
			result["cache_age_days"] = cacheAgeDays
			result["warning"] = fmt.Sprintf("Operating in offline mode due to: %s", errorMsg)
			return result, nil
		}
		return nil, fmt.Errorf("license expired and server unreachable: %s", errorMsg)
	}
	return nil, fmt.Errorf("no offline cache available: %s", errorMsg)
} 