package binaryformat

import (
	"bytes"
	"compress/gzip"
	"crypto/sha256"
	"encoding/binary"
	"encoding/json"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// Constants
const (
	MagicBytes        = "TUSK"
	FormatVersion     = 0x0100
	MaxFileSize       = 1024 * 1024 * 1024 // 1GB
	MaxStringLength   = 65535
	HeaderSize        = 32
)

// Compression algorithms
const (
	CompressionNone = 0
	CompressionGzip = 1
	CompressionLz4  = 2
	CompressionZstd = 3
)

// Encryption algorithms
const (
	EncryptionNone        = 0
	EncryptionAES256GCM   = 1
	EncryptionChaCha20Poly1305 = 2
)

// Flags
const (
	FlagHasMetadata     = 1 << 0
	FlagHasEncryption   = 1 << 1
	FlagHasSignature    = 1 << 2
	FlagIsCompressed    = 1 << 3
	FlagHasDependencies = 1 << 4
	FlagHasKeywords     = 1 << 5
)

// PntHeader represents the header structure of a .pnt file
type PntHeader struct {
	Magic           [4]byte
	Version         uint16
	Flags           uint16
	Compression     uint8
	Encryption      uint8
	Reserved1       uint8
	Reserved2       uint8
	HeaderChecksum  uint32
	DataLength      uint64
	Timestamp       uint64
}

// PntMetadata represents metadata for .pnt files
type PntMetadata struct {
	PackageName  string            `json:"package_name"`
	Version      string            `json:"version"`
	Author       string            `json:"author"`
	Description  string            `json:"description"`
	License      string            `json:"license"`
	Repository   string            `json:"repository"`
	Dependencies []PntDependency   `json:"dependencies"`
	Keywords     []string          `json:"keywords"`
}

// PntDependency represents a package dependency
type PntDependency struct {
	Name    string `json:"name"`
	Version string `json:"version"`
	Type    uint8  `json:"type"`
}

// PntFile represents a complete .pnt file
type PntFile struct {
	Header   PntHeader
	Metadata *PntMetadata
	Data     map[string]interface{}
}

// PntReader reads and parses .pnt files
type PntReader struct {
	filePath string
}

// NewPntReader creates a new PntReader
func NewPntReader(filePath string) *PntReader {
	return &PntReader{filePath: filePath}
}

// Read reads and parses a .pnt file
func (r *PntReader) Read() (*PntFile, error) {
	file, err := os.Open(r.filePath)
	if err != nil {
		return nil, fmt.Errorf("failed to open file: %w", err)
	}
	defer file.Close()

	// Check file size
	stat, err := file.Stat()
	if err != nil {
		return nil, fmt.Errorf("failed to stat file: %w", err)
	}
	if stat.Size() > MaxFileSize {
		return nil, fmt.Errorf("file too large: %d bytes", stat.Size())
	}

	pntFile := &PntFile{}

	// Read header
	if err := r.readHeader(file, &pntFile.Header); err != nil {
		return nil, fmt.Errorf("failed to read header: %w", err)
	}

	// Validate header
	if err := r.validateHeader(&pntFile.Header); err != nil {
		return nil, fmt.Errorf("header validation failed: %w", err)
	}

	// Read metadata if present
	if pntFile.Header.Flags&FlagHasMetadata != 0 {
		metadata, err := r.readMetadata(file)
		if err != nil {
			return nil, fmt.Errorf("failed to read metadata: %w", err)
		}
		pntFile.Metadata = metadata
	}

	// Read data
	data, err := r.readData(file, &pntFile.Header)
	if err != nil {
		return nil, fmt.Errorf("failed to read data: %w", err)
	}
	pntFile.Data = data

	return pntFile, nil
}

// readHeader reads the file header
func (r *PntReader) readHeader(file *os.File, header *PntHeader) error {
	headerData := make([]byte, HeaderSize)
	if _, err := io.ReadFull(file, headerData); err != nil {
		return fmt.Errorf("failed to read header: %w", err)
	}

	// Parse header
	header.Magic = [4]byte{headerData[0], headerData[1], headerData[2], headerData[3]}
	header.Version = binary.LittleEndian.Uint16(headerData[4:6])
	header.Flags = binary.LittleEndian.Uint16(headerData[6:8])
	header.Compression = headerData[8]
	header.Encryption = headerData[9]
	header.Reserved1 = headerData[10]
	header.Reserved2 = headerData[11]
	header.HeaderChecksum = binary.LittleEndian.Uint32(headerData[12:16])
	header.DataLength = binary.LittleEndian.Uint64(headerData[16:24])
	header.Timestamp = binary.LittleEndian.Uint64(headerData[24:32])

	return nil
}

// validateHeader validates the file header
func (r *PntReader) validateHeader(header *PntHeader) error {
	// Check magic bytes
	if string(header.Magic[:]) != MagicBytes {
		return fmt.Errorf("invalid magic bytes: %s", string(header.Magic[:]))
	}

	// Check version
	if header.Version != FormatVersion {
		return fmt.Errorf("unsupported version: %d", header.Version)
	}

	// Check reserved fields
	if header.Reserved1 != 0 || header.Reserved2 != 0 {
		return fmt.Errorf("reserved fields must be zero")
	}

	// Calculate and verify header checksum
	headerData := make([]byte, 12)
	copy(headerData[0:4], header.Magic[:])
	binary.LittleEndian.PutUint16(headerData[4:6], header.Version)
	binary.LittleEndian.PutUint16(headerData[6:8], header.Flags)
	headerData[8] = header.Compression
	headerData[9] = header.Encryption
	headerData[10] = header.Reserved1
	headerData[11] = header.Reserved2

	calculatedChecksum := crc32(headerData)
	if calculatedChecksum != header.HeaderChecksum {
		return fmt.Errorf("header checksum mismatch: expected %d, got %d", header.HeaderChecksum, calculatedChecksum)
	}

	return nil
}

// readMetadata reads the metadata section
func (r *PntReader) readMetadata(file *os.File) (*PntMetadata, error) {
	// Read metadata length and checksum
	var metadataLength, metadataChecksum uint32
	if err := binary.Read(file, binary.LittleEndian, &metadataLength); err != nil {
		return nil, fmt.Errorf("failed to read metadata length: %w", err)
	}
	if err := binary.Read(file, binary.LittleEndian, &metadataChecksum); err != nil {
		return nil, fmt.Errorf("failed to read metadata checksum: %w", err)
	}

	// Read metadata data
	metadataData := make([]byte, metadataLength)
	if _, err := io.ReadFull(file, metadataData); err != nil {
		return nil, fmt.Errorf("failed to read metadata data: %w", err)
	}

	// Verify metadata checksum
	calculatedChecksum := crc32(metadataData)
	if calculatedChecksum != metadataChecksum {
		return nil, fmt.Errorf("metadata checksum mismatch: expected %d, got %d", metadataChecksum, calculatedChecksum)
	}

	// Parse metadata
	return r.parseMetadata(metadataData)
}

// parseMetadata parses metadata from binary data
func (r *PntReader) parseMetadata(data []byte) (*PntMetadata, error) {
	metadata := &PntMetadata{}
	offset := 0

	// Read strings
	strings := make([]string, 6)
	for i := 0; i < 6; i++ {
		str, newOffset, err := r.readString(data, offset)
		if err != nil {
			return nil, fmt.Errorf("failed to read string %d: %w", i, err)
		}
		strings[i] = str
		offset = newOffset
	}

	metadata.PackageName = strings[0]
	metadata.Version = strings[1]
	metadata.Author = strings[2]
	metadata.Description = strings[3]
	metadata.License = strings[4]
	metadata.Repository = strings[5]

	// Read dependencies if present
	if len(data) > offset+4 {
		var depsCount uint32
		if err := binary.Read(bytes.NewReader(data[offset:offset+4]), binary.LittleEndian, &depsCount); err != nil {
			return nil, fmt.Errorf("failed to read dependencies count: %w", err)
		}
		offset += 4

		metadata.Dependencies = make([]PntDependency, depsCount)
		for i := uint32(0); i < depsCount; i++ {
			dep, newOffset, err := r.readDependency(data, offset)
			if err != nil {
				return nil, fmt.Errorf("failed to read dependency %d: %w", i, err)
			}
			metadata.Dependencies[i] = dep
			offset = newOffset
		}
	}

	// Read keywords if present
	if len(data) > offset+4 {
		var keywordsCount uint32
		if err := binary.Read(bytes.NewReader(data[offset:offset+4]), binary.LittleEndian, &keywordsCount); err != nil {
			return nil, fmt.Errorf("failed to read keywords count: %w", err)
		}
		offset += 4

		metadata.Keywords = make([]string, keywordsCount)
		for i := uint32(0); i < keywordsCount; i++ {
			keyword, newOffset, err := r.readString(data, offset)
			if err != nil {
				return nil, fmt.Errorf("failed to read keyword %d: %w", i, err)
			}
			metadata.Keywords[i] = keyword
			offset = newOffset
		}
	}

	return metadata, nil
}

// readString reads a null-terminated string
func (r *PntReader) readString(data []byte, offset int) (string, int, error) {
	if offset >= len(data) {
		return "", offset, fmt.Errorf("offset out of bounds")
	}

	end := offset
	for end < len(data) && data[end] != 0 {
		end++
	}

	if end >= len(data) {
		return "", offset, fmt.Errorf("string not null-terminated")
	}

	return string(data[offset:end]), end + 1, nil
}

// readDependency reads a dependency entry
func (r *PntReader) readDependency(data []byte, offset int) (PntDependency, int, error) {
	dep := PntDependency{}

	// Read name
	name, newOffset, err := r.readString(data, offset)
	if err != nil {
		return dep, offset, fmt.Errorf("failed to read dependency name: %w", err)
	}
	dep.Name = name
	offset = newOffset

	// Read version
	version, newOffset, err := r.readString(data, offset)
	if err != nil {
		return dep, offset, fmt.Errorf("failed to read dependency version: %w", err)
	}
	dep.Version = version
	offset = newOffset

	// Read type
	if offset >= len(data) {
		return dep, offset, fmt.Errorf("dependency type missing")
	}
	dep.Type = data[offset]
	offset++

	return dep, offset, nil
}

// readData reads the data section
func (r *PntReader) readData(file *os.File, header *PntHeader) (map[string]interface{}, error) {
	// Read data length
	var dataLength uint32
	if err := binary.Read(file, binary.LittleEndian, &dataLength); err != nil {
		return nil, fmt.Errorf("failed to read data length: %w", err)
	}

	// Read data
	data := make([]byte, dataLength)
	if _, err := io.ReadFull(file, data); err != nil {
		return nil, fmt.Errorf("failed to read data: %w", err)
	}

	// Read data checksum
	var dataChecksum uint32
	if err := binary.Read(file, binary.LittleEndian, &dataChecksum); err != nil {
		return nil, fmt.Errorf("failed to read data checksum: %w", err)
	}

	// Verify data checksum
	hash := sha256.Sum256(data)
	calculatedChecksum := binary.LittleEndian.Uint32(hash[:4])
	if calculatedChecksum != dataChecksum {
		return nil, fmt.Errorf("data checksum mismatch: expected %d, got %d", dataChecksum, calculatedChecksum)
	}

	// Decompress if needed
	if header.Flags&FlagIsCompressed != 0 {
		decompressed, err := r.decompressData(data, header.Compression)
		if err != nil {
			return nil, fmt.Errorf("failed to decompress data: %w", err)
		}
		data = decompressed
	}

	// Decrypt if needed
	if header.Flags&FlagHasEncryption != 0 {
		decrypted, err := r.decryptData(data, header.Encryption)
		if err != nil {
			return nil, fmt.Errorf("failed to decrypt data: %w", err)
		}
		data = decrypted
	}

	// Parse as JSON
	var result map[string]interface{}
	if err := json.Unmarshal(data, &result); err != nil {
		return nil, fmt.Errorf("failed to parse JSON data: %w", err)
	}

	return result, nil
}

// decompressData decompresses data based on compression algorithm
func (r *PntReader) decompressData(data []byte, compression uint8) ([]byte, error) {
	switch compression {
	case CompressionGzip:
		reader, err := gzip.NewReader(bytes.NewReader(data))
		if err != nil {
			return nil, fmt.Errorf("failed to create gzip reader: %w", err)
		}
		defer reader.Close()

		decompressed, err := io.ReadAll(reader)
		if err != nil {
			return nil, fmt.Errorf("failed to decompress gzip data: %w", err)
		}
		return decompressed, nil

	case CompressionNone:
		return data, nil

	default:
		return nil, fmt.Errorf("unsupported compression: %d", compression)
	}
}

// decryptData decrypts data based on encryption algorithm
func (r *PntReader) decryptData(data []byte, encryption uint8) ([]byte, error) {
	switch encryption {
	case EncryptionNone:
		return data, nil

	default:
		return nil, fmt.Errorf("unsupported encryption: %d", encryption)
	}
}

// PntWriter writes .pnt files
type PntWriter struct {
	filePath string
	header   PntHeader
	metadata *PntMetadata
	data     map[string]interface{}
}

// NewPntWriter creates a new PntWriter
func NewPntWriter(filePath string) *PntWriter {
	return &PntWriter{
		filePath: filePath,
		header: PntHeader{
			Magic:       [4]byte{'T', 'U', 'S', 'K'},
			Version:     FormatVersion,
			Reserved1:   0,
			Reserved2:   0,
		},
	}
}

// SetMetadata sets metadata for the file
func (w *PntWriter) SetMetadata(metadata *PntMetadata) {
	w.metadata = metadata
	w.header.Flags |= FlagHasMetadata

	if len(metadata.Dependencies) > 0 {
		w.header.Flags |= FlagHasDependencies
	}

	if len(metadata.Keywords) > 0 {
		w.header.Flags |= FlagHasKeywords
	}
}

// SetData sets data for the file
func (w *PntWriter) SetData(data map[string]interface{}) {
	w.data = data
}

// SetCompression sets compression algorithm
func (w *PntWriter) SetCompression(compression uint8) {
	w.header.Compression = compression
	if compression != CompressionNone {
		w.header.Flags |= FlagIsCompressed
	}
}

// SetEncryption sets encryption algorithm
func (w *PntWriter) SetEncryption(encryption uint8) {
	w.header.Encryption = encryption
	if encryption != EncryptionNone {
		w.header.Flags |= FlagHasEncryption
	}
}

// Write writes the .pnt file
func (w *PntWriter) Write() error {
	// Prepare data
	dataJSON, err := json.Marshal(w.data)
	if err != nil {
		return fmt.Errorf("failed to marshal data: %w", err)
	}

	// Compress if needed
	if w.header.Flags&FlagIsCompressed != 0 {
		compressed, err := w.compressData(dataJSON)
		if err != nil {
			return fmt.Errorf("failed to compress data: %w", err)
		}
		dataJSON = compressed
	}

	// Encrypt if needed
	if w.header.Flags&FlagHasEncryption != 0 {
		encrypted, err := w.encryptData(dataJSON)
		if err != nil {
			return fmt.Errorf("failed to encrypt data: %w", err)
		}
		dataJSON = encrypted
	}

	// Prepare metadata
	metadataData, err := w.serializeMetadata()
	if err != nil {
		return fmt.Errorf("failed to serialize metadata: %w", err)
	}

	// Update header
	w.header.DataLength = uint64(len(dataJSON))
	w.header.Timestamp = uint64(time.Now().UnixNano())

	// Calculate header checksum
	headerData := make([]byte, 12)
	copy(headerData[0:4], w.header.Magic[:])
	binary.LittleEndian.PutUint16(headerData[4:6], w.header.Version)
	binary.LittleEndian.PutUint16(headerData[6:8], w.header.Flags)
	headerData[8] = w.header.Compression
	headerData[9] = w.header.Encryption
	headerData[10] = w.header.Reserved1
	headerData[11] = w.header.Reserved2

	w.header.HeaderChecksum = crc32(headerData)

	// Create directory if needed
	dir := filepath.Dir(w.filePath)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return fmt.Errorf("failed to create directory: %w", err)
	}

	// Write file
	file, err := os.Create(w.filePath)
	if err != nil {
		return fmt.Errorf("failed to create file: %w", err)
	}
	defer file.Close()

	// Write header
	if err := binary.Write(file, binary.LittleEndian, w.header); err != nil {
		return fmt.Errorf("failed to write header: %w", err)
	}

	// Write metadata
	if w.header.Flags&FlagHasMetadata != 0 {
		metadataChecksum := crc32(metadataData)
		if err := binary.Write(file, binary.LittleEndian, uint32(len(metadataData))); err != nil {
			return fmt.Errorf("failed to write metadata length: %w", err)
		}
		if err := binary.Write(file, binary.LittleEndian, metadataChecksum); err != nil {
			return fmt.Errorf("failed to write metadata checksum: %w", err)
		}
		if _, err := file.Write(metadataData); err != nil {
			return fmt.Errorf("failed to write metadata: %w", err)
		}
	}

	// Write data
	if err := binary.Write(file, binary.LittleEndian, uint32(len(dataJSON))); err != nil {
		return fmt.Errorf("failed to write data length: %w", err)
	}
	if _, err := file.Write(dataJSON); err != nil {
		return fmt.Errorf("failed to write data: %w", err)
	}

	// Write data checksum
	hash := sha256.Sum256(dataJSON)
	dataChecksum := binary.LittleEndian.Uint32(hash[:4])
	if err := binary.Write(file, binary.LittleEndian, dataChecksum); err != nil {
		return fmt.Errorf("failed to write data checksum: %w", err)
	}

	return nil
}

// serializeMetadata serializes metadata to binary format
func (w *PntWriter) serializeMetadata() ([]byte, error) {
	if w.metadata == nil {
		return nil, nil
	}

	var data bytes.Buffer

	// Write strings
	strings := []string{
		w.metadata.PackageName,
		w.metadata.Version,
		w.metadata.Author,
		w.metadata.Description,
		w.metadata.License,
		w.metadata.Repository,
	}

	for _, str := range strings {
		if _, err := data.WriteString(str); err != nil {
			return nil, fmt.Errorf("failed to write string: %w", err)
		}
		if err := data.WriteByte(0); err != nil {
			return nil, fmt.Errorf("failed to write null terminator: %w", err)
		}
	}

	// Write dependencies
	if w.header.Flags&FlagHasDependencies != 0 {
		if err := binary.Write(&data, binary.LittleEndian, uint32(len(w.metadata.Dependencies))); err != nil {
			return nil, fmt.Errorf("failed to write dependencies count: %w", err)
		}

		for _, dep := range w.metadata.Dependencies {
			if _, err := data.WriteString(dep.Name); err != nil {
				return nil, fmt.Errorf("failed to write dependency name: %w", err)
			}
			if err := data.WriteByte(0); err != nil {
				return nil, fmt.Errorf("failed to write null terminator: %w", err)
			}

			if _, err := data.WriteString(dep.Version); err != nil {
				return nil, fmt.Errorf("failed to write dependency version: %w", err)
			}
			if err := data.WriteByte(0); err != nil {
				return nil, fmt.Errorf("failed to write null terminator: %w", err)
			}

			if err := data.WriteByte(dep.Type); err != nil {
				return nil, fmt.Errorf("failed to write dependency type: %w", err)
			}
		}
	}

	// Write keywords
	if w.header.Flags&FlagHasKeywords != 0 {
		if err := binary.Write(&data, binary.LittleEndian, uint32(len(w.metadata.Keywords))); err != nil {
			return nil, fmt.Errorf("failed to write keywords count: %w", err)
		}

		for _, keyword := range w.metadata.Keywords {
			if _, err := data.WriteString(keyword); err != nil {
				return nil, fmt.Errorf("failed to write keyword: %w", err)
			}
			if err := data.WriteByte(0); err != nil {
				return nil, fmt.Errorf("failed to write null terminator: %w", err)
			}
		}
	}

	return data.Bytes(), nil
}

// compressData compresses data based on compression algorithm
func (w *PntWriter) compressData(data []byte) ([]byte, error) {
	switch w.header.Compression {
	case CompressionGzip:
		var buf bytes.Buffer
		writer := gzip.NewWriter(&buf)
		if _, err := writer.Write(data); err != nil {
			return nil, fmt.Errorf("failed to write gzip data: %w", err)
		}
		if err := writer.Close(); err != nil {
			return nil, fmt.Errorf("failed to close gzip writer: %w", err)
		}
		return buf.Bytes(), nil

	case CompressionNone:
		return data, nil

	default:
		return nil, fmt.Errorf("unsupported compression: %d", w.header.Compression)
	}
}

// encryptData encrypts data based on encryption algorithm
func (w *PntWriter) encryptData(data []byte) ([]byte, error) {
	switch w.header.Encryption {
	case EncryptionNone:
		return data, nil

	default:
		return nil, fmt.Errorf("unsupported encryption: %d", w.header.Encryption)
	}
}

// Simple CRC32 implementation (for compatibility)
func crc32(data []byte) uint32 {
	crc := uint32(0xFFFFFFFF)
	for _, b := range data {
		crc ^= uint32(b)
		for i := 0; i < 8; i++ {
			if crc&1 != 0 {
				crc = (crc >> 1) ^ 0xEDB88320
			} else {
				crc >>= 1
			}
		}
	}
	return ^crc
}

// ReadPntFile reads and parses a .pnt file
func ReadPntFile(filePath string) (*PntFile, error) {
	reader := NewPntReader(filePath)
	return reader.Read()
}

// WritePntFile writes a .pnt file
func WritePntFile(filePath string, data map[string]interface{}, metadata *PntMetadata) error {
	writer := NewPntWriter(filePath)
	writer.SetData(data)
	if metadata != nil {
		writer.SetMetadata(metadata)
	}
	return writer.Write()
}

// ValidatePntFile validates a .pnt file
func ValidatePntFile(filePath string) bool {
	_, err := ReadPntFile(filePath)
	return err == nil
}

// GetPntMetadata extracts metadata from a .pnt file
func GetPntMetadata(filePath string) (*PntMetadata, error) {
	pntFile, err := ReadPntFile(filePath)
	if err != nil {
		return nil, err
	}
	return pntFile.Metadata, nil
}

// ConvertToPnt converts from other formats to .pnt
func ConvertToPnt(sourceFormat string, sourceData interface{}, outputPath string) error {
	var data map[string]interface{}

	switch strings.ToLower(sourceFormat) {
	case "json":
		switch v := sourceData.(type) {
		case string:
			if err := json.Unmarshal([]byte(v), &data); err != nil {
				return fmt.Errorf("failed to parse JSON: %w", err)
			}
		case map[string]interface{}:
			data = v
		default:
			return fmt.Errorf("unsupported JSON data type")
		}
	default:
		return fmt.Errorf("unsupported source format: %s", sourceFormat)
	}

	return WritePntFile(outputPath, data, nil)
} 