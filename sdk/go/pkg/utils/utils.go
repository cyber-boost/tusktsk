// Package utils provides utility functions for the TuskLang SDK
package utils

import (
	"crypto/md5"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// Utils provides utility functions
type Utils struct{}

// New creates a new Utils instance
func New() *Utils {
	return &Utils{}
}

// FileExists checks if a file exists
func (u *Utils) FileExists(path string) bool {
	_, err := os.Stat(path)
	return !os.IsNotExist(err)
}

// ReadFile reads a file and returns its contents
func (u *Utils) ReadFile(path string) (string, error) {
	content, err := os.ReadFile(path)
	if err != nil {
		return "", err
	}
	return string(content), nil
}

// WriteFile writes content to a file
func (u *Utils) WriteFile(path string, content string) error {
	return os.WriteFile(path, []byte(content), 0644)
}

// CreateDir creates a directory if it doesn't exist
func (u *Utils) CreateDir(path string) error {
	return os.MkdirAll(path, 0755)
}

// GetFileHash returns the MD5 hash of a file
func (u *Utils) GetFileHash(path string) (string, error) {
	file, err := os.Open(path)
	if err != nil {
		return "", err
	}
	defer file.Close()

	hash := md5.New()
	if _, err := io.Copy(hash, file); err != nil {
		return "", err
	}

	return hex.EncodeToString(hash.Sum(nil)), nil
}

// ToJSON converts an interface to JSON string
func (u *Utils) ToJSON(v interface{}) (string, error) {
	bytes, err := json.MarshalIndent(v, "", "  ")
	if err != nil {
		return "", err
	}
	return string(bytes), nil
}

// FromJSON converts JSON string to interface
func (u *Utils) FromJSON(data string, v interface{}) error {
	return json.Unmarshal([]byte(data), v)
}

// FormatTimestamp formats a timestamp
func (u *Utils) FormatTimestamp(t time.Time) string {
	return t.Format("2006-01-02 15:04:05")
}

// SanitizeFilename sanitizes a filename
func (u *Utils) SanitizeFilename(filename string) string {
	// Remove or replace invalid characters
	invalid := []string{"<", ">", ":", "\"", "/", "\\", "|", "?", "*"}
	result := filename
	for _, char := range invalid {
		result = strings.ReplaceAll(result, char, "_")
	}
	return result
}

// GetFileExtension returns the file extension
func (u *Utils) GetFileExtension(path string) string {
	return strings.ToLower(filepath.Ext(path))
}

// IsDirectory checks if a path is a directory
func (u *Utils) IsDirectory(path string) bool {
	info, err := os.Stat(path)
	if err != nil {
		return false
	}
	return info.IsDir()
}

// ListFiles lists files in a directory
func (u *Utils) ListFiles(dir string) ([]string, error) {
	var files []string
	err := filepath.Walk(dir, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}
		if !info.IsDir() {
			files = append(files, path)
		}
		return nil
	})
	return files, err
}

// CopyFile copies a file from src to dst
func (u *Utils) CopyFile(src, dst string) error {
	sourceFile, err := os.Open(src)
	if err != nil {
		return err
	}
	defer sourceFile.Close()

	destFile, err := os.Create(dst)
	if err != nil {
		return err
	}
	defer destFile.Close()

	_, err = io.Copy(destFile, sourceFile)
	return err
}

// GetFileSize returns the size of a file in bytes
func (u *Utils) GetFileSize(path string) (int64, error) {
	info, err := os.Stat(path)
	if err != nil {
		return 0, err
	}
	return info.Size(), nil
}

// FormatFileSize formats file size in human readable format
func (u *Utils) FormatFileSize(bytes int64) string {
	const unit = 1024
	if bytes < unit {
		return fmt.Sprintf("%d B", bytes)
	}
	div, exp := int64(unit), 0
	for n := bytes / unit; n >= unit; n /= unit {
		div *= unit
		exp++
	}
	return fmt.Sprintf("%.1f %cB", float64(bytes)/float64(div), "KMGTPE"[exp])
} 