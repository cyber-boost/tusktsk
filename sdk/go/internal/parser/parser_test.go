package parser

import (
	"testing"
)

func TestNew(t *testing.T) {
	parser := New()
	if parser == nil {
		t.Error("New() should return a non-nil parser")
	}
}

func TestParse(t *testing.T) {
	parser := New()
	
	// Test basic parsing
	result, err := parser.Parse("test code")
	if err != nil {
		t.Errorf("Parse() should not return error for basic input: %v", err)
	}
	if result == nil {
		t.Error("Parse() should return a non-nil result")
	}
}

func TestParseEmpty(t *testing.T) {
	parser := New()
	
	// Test empty input
	result, err := parser.Parse("")
	if err != nil {
		t.Errorf("Parse() should handle empty input: %v", err)
	}
	if result == nil {
		t.Error("Parse() should return a non-nil result for empty input")
	}
}
