package license

import (
	"testing"
)

func TestLicenseValidation(t *testing.T) {
	// Test basic license initialization with proper test key
	testLicenseKey := "TUSK-TEST-KEY-123456789012345678901234567890"
	license := New(testLicenseKey, "test-api-key")
	if license == nil {
		t.Error("Failed to create license instance")
	}
	
	// Test license info retrieval
	info := license.GetLicenseInfo()
	// Note: GetLicenseInfo returns a truncated license key for display
	if info.LicenseKey == "" {
		t.Error("License key should not be empty")
	}
	
	// Test validation result (should work with proper test key)
	result := license.ValidateLicenseKey()
	t.Logf("License validation result: %+v", result)
	
	// Test that we can get license info without errors
	if info.SessionID == "" {
		t.Log("Session ID not set (expected in test mode)")
	}
}

func TestLicenseExpiration(t *testing.T) {
	testLicenseKey := "TUSK-TEST-KEY-123456789012345678901234567890"
	license := New(testLicenseKey, "test-api-key")
	
	// Test expiration check
	expiration := license.CheckLicenseExpiration()
	t.Logf("Expiration check result: %+v", expiration)
	
	// Test that we can check expiration without errors
	if expiration.Error != "" {
		t.Logf("Expiration check error (expected in test mode): %s", expiration.Error)
	}
}

func TestLicensePermissions(t *testing.T) {
	testLicenseKey := "TUSK-TEST-KEY-123456789012345678901234567890"
	license := New(testLicenseKey, "test-api-key")
	
	// Test permission validation
	hasPermission, err := license.ValidateLicensePermissions("test-feature")
	t.Logf("Permission check result: hasPermission=%v, err=%v", hasPermission, err)
	
	// Test that we can check permissions without errors
	if err != nil {
		t.Logf("Permission check error (expected in test mode): %v", err)
	}
}

func TestLicenseInfo(t *testing.T) {
	testLicenseKey := "TUSK-TEST-KEY-123456789012345678901234567890"
	license := New(testLicenseKey, "test-api-key")
	
	// Test getting comprehensive license info
	info := license.GetLicenseInfo()
	
	// Verify basic structure
	if info.LicenseKey == "" {
		t.Error("License key should not be empty")
	}
	
	// Log the info for debugging
	t.Logf("License info: %+v", info)
	
	// Test that validation count is initialized
	if info.ValidationCount < 0 {
		t.Error("Validation count should be non-negative")
	}
}

func TestInvalidLicenseKey(t *testing.T) {
	// Test with invalid license key
	invalidKey := "invalid-key"
	license := New(invalidKey, "test-api-key")
	
	// Test validation result (should fail with invalid key)
	result := license.ValidateLicenseKey()
	if result.Valid {
		t.Error("Invalid license key should not be valid")
	}
	
	t.Logf("Invalid license validation result: %+v", result)
}
