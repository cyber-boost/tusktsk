package tusk

import (
	"bytes"
	"compress/gzip"
	"crypto/aes"
	"crypto/cipher"
	"crypto/ed25519"
	"crypto/rand"
	"crypto/sha256"
	"encoding/binary"
	"encoding/json"
	"fmt"
	"io"
	"time"
)

// BinaryFormatHeader represents the 32-byte header for TuskLang binary files
type BinaryFormatHeader struct {
	MagicNumber    [8]byte  // "TUSKLANG"
	Version        uint16   // Format version (currently 1)
	Flags          uint16   // Compression, encryption, signature flags
	CompressionType uint8   // 0=none, 1=gzip, 2=lz4, 3=zstd
	EncryptionType uint8    // 0=none, 1=AES-256-GCM, 2=ChaCha20-Poly1305
	SignatureType  uint8    // 0=none, 1=Ed25519, 2=RSA
	Reserved       uint8    // Reserved for future use
	DataSize       uint32   // Size of data section
	MetadataSize   uint32   // Size of metadata section
	Checksum       [32]byte // SHA-256 checksum of header
	Timestamp      uint64   // Unix timestamp of creation
}

// BinaryFormat represents a complete TuskLang binary file
type BinaryFormat struct {
	Header   BinaryFormatHeader
	Data     []byte
	Metadata map[string]interface{}
	Signature []byte
}

// BinaryFormatWriter handles writing TuskLang binary files
type BinaryFormatWriter struct {
	compressionType uint8
	encryptionType  uint8
	signatureType   uint8
	encryptionKey   []byte
	signingKey      ed25519.PrivateKey
}

// BinaryFormatReader handles reading TuskLang binary files
type BinaryFormatReader struct {
	verificationKey ed25519.PublicKey
	decryptionKey   []byte
}

// NewBinaryFormatWriter creates a new binary format writer
func NewBinaryFormatWriter() *BinaryFormatWriter {
	return &BinaryFormatWriter{
		compressionType: 1, // Default to gzip
		encryptionType:  0, // No encryption by default
		signatureType:   0, // No signature by default
	}
}

// SetCompression sets the compression type
func (w *BinaryFormatWriter) SetCompression(compressionType uint8) {
	w.compressionType = compressionType
}

// SetEncryption sets the encryption type and key
func (w *BinaryFormatWriter) SetEncryption(encryptionType uint8, key []byte) {
	w.encryptionType = encryptionType
	w.encryptionKey = key
}

// SetSignature sets the signature type and key
func (w *BinaryFormatWriter) SetSignature(signingKey ed25519.PrivateKey) {
	w.signatureType = 1 // Ed25519
	w.signingKey = signingKey
}

// WriteToFile writes data to a TuskLang binary file
func (w *BinaryFormatWriter) WriteToFile(filename string, data []byte, metadata map[string]interface{}) error {
	// Create header
	header := BinaryFormatHeader{}
	copy(header.MagicNumber[:], []byte("TUSKLANG"))
	header.Version = 1
	header.Flags = uint16(w.compressionType) | uint16(w.encryptionType<<4) | uint16(w.signatureType<<8)
	header.CompressionType = w.compressionType
	header.EncryptionType = w.encryptionType
	header.SignatureType = w.signatureType
	header.Timestamp = uint64(time.Now().Unix())

	// Process data
	processedData := data
	var err error

	// Compress if needed
	if w.compressionType == 1 {
		processedData, err = w.compressGzip(data)
		if err != nil {
			return fmt.Errorf("compression failed: %v", err)
		}
	}

	// Encrypt if needed
	if w.encryptionType == 1 {
		processedData, err = w.encryptAES256GCM(processedData)
		if err != nil {
			return fmt.Errorf("encryption failed: %v", err)
		}
	}

	// Update header with processed data size
	header.DataSize = uint32(len(processedData))

	// Serialize metadata
	metadataBytes, err := json.Marshal(metadata)
	if err != nil {
		return fmt.Errorf("metadata serialization failed: %v", err)
	}
	header.MetadataSize = uint32(len(metadataBytes))

	// Calculate header checksum
	headerBytes := w.serializeHeader(header)
	headerChecksum := sha256.Sum256(headerBytes[:len(headerBytes)-32]) // Exclude checksum field
	copy(header.Checksum[:], headerChecksum[:])

	// Create signature if needed
	var signature []byte
	if w.signatureType == 1 {
		signatureData := append(headerBytes, processedData...)
		signatureData = append(signatureData, metadataBytes...)
		signature = ed25519.Sign(w.signingKey, signatureData)
	}

	// Write to file
	return w.writeToFile(filename, header, processedData, metadataBytes, signature)
}

// ReadFromFile reads data from a TuskLang binary file
func (r *BinaryFormatReader) ReadFromFile(filename string) (*BinaryFormat, error) {
	// Implementation for reading binary files
	// This would include header validation, signature verification, decryption, and decompression
	return nil, fmt.Errorf("read implementation pending")
}

// compressGzip compresses data using gzip
func (w *BinaryFormatWriter) compressGzip(data []byte) ([]byte, error) {
	var buf bytes.Buffer
	gw := gzip.NewWriter(&buf)
	_, err := gw.Write(data)
	if err != nil {
		return nil, err
	}
	err = gw.Close()
	if err != nil {
		return nil, err
	}
	return buf.Bytes(), nil
}

// encryptAES256GCM encrypts data using AES-256-GCM
func (w *BinaryFormatWriter) encryptAES256GCM(data []byte) ([]byte, error) {
	block, err := aes.NewCipher(w.encryptionKey)
	if err != nil {
		return nil, err
	}

	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return nil, err
	}

	nonce := make([]byte, gcm.NonceSize())
	if _, err := io.ReadFull(rand.Reader, nonce); err != nil {
		return nil, err
	}

	ciphertext := gcm.Seal(nonce, nonce, data, nil)
	return ciphertext, nil
}

// serializeHeader serializes the header to bytes
func (w *BinaryFormatWriter) serializeHeader(header BinaryFormatHeader) []byte {
	buf := make([]byte, 32)
	copy(buf[0:8], header.MagicNumber[:])
	binary.LittleEndian.PutUint16(buf[8:10], header.Version)
	binary.LittleEndian.PutUint16(buf[10:12], header.Flags)
	buf[12] = header.CompressionType
	buf[13] = header.EncryptionType
	buf[14] = header.SignatureType
	buf[15] = header.Reserved
	binary.LittleEndian.PutUint32(buf[16:20], header.DataSize)
	binary.LittleEndian.PutUint32(buf[20:24], header.MetadataSize)
	copy(buf[24:56], header.Checksum[:])
	binary.LittleEndian.PutUint64(buf[56:64], header.Timestamp)
	return buf
}

// writeToFile writes the complete binary file
func (w *BinaryFormatWriter) writeToFile(filename string, header BinaryFormatHeader, data, metadata, signature []byte) error {
	// Implementation for writing to file
	// This would include proper file handling and error checking
	return fmt.Errorf("file write implementation pending")
}

// Utility functions for Agent a1

// ValidateBinaryFormat validates a binary file format
func ValidateBinaryFormat(data []byte) error {
	if len(data) < 32 {
		return fmt.Errorf("file too small to be valid binary format")
	}

	header := BinaryFormatHeader{}
	copy(header.MagicNumber[:], data[0:8])
	
	if string(header.MagicNumber[:]) != "TUSKLANG" {
		return fmt.Errorf("invalid magic number")
	}

	return nil
}

// GetBinaryFormatInfo returns information about a binary file
func GetBinaryFormatInfo(data []byte) (map[string]interface{}, error) {
	if err := ValidateBinaryFormat(data); err != nil {
		return nil, err
	}

	info := make(map[string]interface{})
	info["magic_number"] = string(data[0:8])
	info["version"] = binary.LittleEndian.Uint16(data[8:10])
	info["flags"] = binary.LittleEndian.Uint16(data[10:12])
	info["compression_type"] = data[12]
	info["encryption_type"] = data[13]
	info["signature_type"] = data[14]
	info["data_size"] = binary.LittleEndian.Uint32(data[16:20])
	info["metadata_size"] = binary.LittleEndian.Uint32(data[20:24])
	info["timestamp"] = binary.LittleEndian.Uint64(data[56:64])

	return info, nil
} 