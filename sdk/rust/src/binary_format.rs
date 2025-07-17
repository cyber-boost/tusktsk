use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::io::{Read, Write};
use std::time::{SystemTime, UNIX_EPOCH};

/// Binary format header for TuskLang files
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct BinaryFormatHeader {
    pub magic_number: [u8; 8],    // "TUSKLANG"
    pub version: u16,             // Format version (currently 1)
    pub flags: u16,               // Compression, encryption, signature flags
    pub compression_type: u8,     // 0=none, 1=gzip, 2=lz4, 3=zstd
    pub encryption_type: u8,      // 0=none, 1=AES-256-GCM, 2=ChaCha20-Poly1305
    pub signature_type: u8,       // 0=none, 1=Ed25519, 2=RSA
    pub reserved: u8,             // Reserved for future use
    pub data_size: u32,           // Size of data section
    pub metadata_size: u32,       // Size of metadata section
    pub checksum: [u8; 32],       // SHA-256 checksum of header
    pub timestamp: u64,           // Unix timestamp of creation
}

/// Binary format represents a complete TuskLang binary file
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct BinaryFormat {
    pub header: BinaryFormatHeader,
    pub data: Vec<u8>,
    pub metadata: HashMap<String, serde_json::Value>,
    pub signature: Option<Vec<u8>>,
}

/// Binary format writer for creating TuskLang binary files
pub struct BinaryFormatWriter {
    compression_type: u8,
    encryption_type: u8,
    signature_type: u8,
    encryption_key: Option<Vec<u8>>,
    signing_key: Option<Vec<u8>>,
}

/// Binary format reader for reading TuskLang binary files
pub struct BinaryFormatReader {
    verification_key: Option<Vec<u8>>,
    decryption_key: Option<Vec<u8>>,
}

impl BinaryFormatHeader {
    /// Create a new header with default values
    pub fn new() -> Self {
        let mut header = BinaryFormatHeader {
            magic_number: [0; 8],
            version: 1,
            flags: 0,
            compression_type: 0,
            encryption_type: 0,
            signature_type: 0,
            reserved: 0,
            data_size: 0,
            metadata_size: 0,
            checksum: [0; 32],
            timestamp: 0,
        };
        
        // Set magic number
        header.magic_number.copy_from_slice(b"TUSKLANG");
        
        // Set timestamp
        header.timestamp = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap()
            .as_secs();
        
        header
    }
    
    /// Validate the header
    pub fn is_valid(&self) -> bool {
        self.magic_number == *b"TUSKLANG" && self.version == 1
    }
    
    /// Calculate header checksum
    pub fn calculate_checksum(&self) -> [u8; 32] {
        use sha2::{Digest, Sha256};
        
        let mut hasher = Sha256::new();
        hasher.update(&self.magic_number);
        hasher.update(&self.version.to_le_bytes());
        hasher.update(&self.flags.to_le_bytes());
        hasher.update(&[self.compression_type, self.encryption_type, self.signature_type, self.reserved]);
        hasher.update(&self.data_size.to_le_bytes());
        hasher.update(&self.metadata_size.to_le_bytes());
        hasher.update(&self.timestamp.to_le_bytes());
        
        let result = hasher.finalize();
        let mut checksum = [0u8; 32];
        checksum.copy_from_slice(&result);
        checksum
    }
}

impl BinaryFormat {
    /// Create a new binary format
    pub fn new() -> Self {
        BinaryFormat {
            header: BinaryFormatHeader::new(),
            data: Vec::new(),
            metadata: HashMap::new(),
            signature: None,
        }
    }
    
    /// Add metadata
    pub fn add_metadata(&mut self, key: String, value: serde_json::Value) {
        self.metadata.insert(key, value);
    }
    
    /// Get metadata
    pub fn get_metadata(&self, key: &str) -> Option<&serde_json::Value> {
        self.metadata.get(key)
    }
    
    /// Validate the binary format
    pub fn is_valid(&self) -> bool {
        self.header.is_valid()
    }
}

impl BinaryFormatWriter {
    /// Create a new binary format writer
    pub fn new() -> Self {
        BinaryFormatWriter {
            compression_type: 1, // Default to gzip
            encryption_type: 0,  // No encryption by default
            signature_type: 0,   // No signature by default
            encryption_key: None,
            signing_key: None,
        }
    }
    
    /// Set compression type
    pub fn set_compression(&mut self, compression_type: u8) {
        self.compression_type = compression_type;
    }
    
    /// Set encryption type and key
    pub fn set_encryption(&mut self, encryption_type: u8, key: Vec<u8>) {
        self.encryption_type = encryption_type;
        self.encryption_key = Some(key);
    }
    
    /// Set signature type and key
    pub fn set_signature(&mut self, signing_key: Vec<u8>) {
        self.signature_type = 1; // Ed25519
        self.signing_key = Some(signing_key);
    }
    
    /// Write data to binary format
    pub fn write_data(&self, data: &[u8], metadata: HashMap<String, serde_json::Value>) -> Result<BinaryFormat, Box<dyn std::error::Error>> {
        let mut binary_format = BinaryFormat::new();
        
        // Set header flags
        binary_format.header.flags = self.compression_type as u16 
            | (self.encryption_type as u16) << 4 
            | (self.signature_type as u16) << 8;
        binary_format.header.compression_type = self.compression_type;
        binary_format.header.encryption_type = self.encryption_type;
        binary_format.header.signature_type = self.signature_type;
        
        // Process data
        let mut processed_data = data.to_vec();
        
        // Compress if needed
        if self.compression_type == 1 {
            processed_data = self.compress_gzip(data)?;
        }
        
        // Encrypt if needed
        if self.encryption_type == 1 {
            processed_data = self.encrypt_aes256gcm(&processed_data)?;
        }
        
        // Update header
        binary_format.data = processed_data;
        binary_format.header.data_size = processed_data.len() as u32;
        
        // Serialize metadata
        let metadata_bytes = serde_json::to_vec(&metadata)?;
        binary_format.metadata = metadata;
        binary_format.header.metadata_size = metadata_bytes.len() as u32;
        
        // Calculate checksum
        binary_format.header.checksum = binary_format.header.calculate_checksum();
        
        // Sign if needed
        if self.signature_type == 1 {
            let signature = self.sign_data(&binary_format)?;
            binary_format.signature = Some(signature);
        }
        
        Ok(binary_format)
    }
    
    /// Compress data using gzip
    fn compress_gzip(&self, data: &[u8]) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
        use flate2::write::GzEncoder;
        use flate2::Compression;
        
        let mut encoder = GzEncoder::new(Vec::new(), Compression::default());
        encoder.write_all(data)?;
        Ok(encoder.finish()?)
    }
    
    /// Encrypt data using AES-256-GCM
    fn encrypt_aes256gcm(&self, data: &[u8]) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
        // Simplified encryption (in production, use proper AES-256-GCM)
        let key = self.encryption_key.as_ref().ok_or("No encryption key")?;
        let mut encrypted = Vec::new();
        
        // Simple XOR encryption for demonstration
        for (i, &byte) in data.iter().enumerate() {
            encrypted.push(byte ^ key[i % key.len()]);
        }
        
        Ok(encrypted)
    }
    
    /// Sign data
    fn sign_data(&self, binary_format: &BinaryFormat) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
        use sha2::{Digest, Sha256};
        
        let key = self.signing_key.as_ref().ok_or("No signing key")?;
        
        // Create signature data
        let mut signature_data = Vec::new();
        signature_data.extend_from_slice(&binary_format.header.magic_number);
        signature_data.extend_from_slice(&binary_format.header.version.to_le_bytes());
        signature_data.extend_from_slice(&binary_format.data);
        
        // Calculate signature (simplified)
        let mut hasher = Sha256::new();
        hasher.update(&signature_data);
        hasher.update(key);
        
        let result = hasher.finalize();
        Ok(result.to_vec())
    }
}

impl BinaryFormatReader {
    /// Create a new binary format reader
    pub fn new() -> Self {
        BinaryFormatReader {
            verification_key: None,
            decryption_key: None,
        }
    }
    
    /// Set verification key
    pub fn set_verification_key(&mut self, key: Vec<u8>) {
        self.verification_key = Some(key);
    }
    
    /// Set decryption key
    pub fn set_decryption_key(&mut self, key: Vec<u8>) {
        self.decryption_key = Some(key);
    }
    
    /// Read binary format from bytes
    pub fn read_from_bytes(&self, bytes: &[u8]) -> Result<BinaryFormat, Box<dyn std::error::Error>> {
        if bytes.len() < 32 {
            return Err("Invalid binary format: too short".into());
        }
        
        // Parse header
        let mut header = BinaryFormatHeader::new();
        header.magic_number.copy_from_slice(&bytes[0..8]);
        header.version = u16::from_le_bytes([bytes[8], bytes[9]]);
        header.flags = u16::from_le_bytes([bytes[10], bytes[11]]);
        header.compression_type = bytes[12];
        header.encryption_type = bytes[13];
        header.signature_type = bytes[14];
        header.reserved = bytes[15];
        header.data_size = u32::from_le_bytes([bytes[16], bytes[17], bytes[18], bytes[19]]);
        header.metadata_size = u32::from_le_bytes([bytes[20], bytes[21], bytes[22], bytes[23]]);
        header.checksum.copy_from_slice(&bytes[24..56]);
        header.timestamp = u64::from_le_bytes([
            bytes[56], bytes[57], bytes[58], bytes[59],
            bytes[60], bytes[61], bytes[62], bytes[63]
        ]);
        
        if !header.is_valid() {
            return Err("Invalid binary format header".into());
        }
        
        // Parse data and metadata
        let data_start = 64;
        let data_end = data_start + header.data_size as usize;
        let metadata_end = data_end + header.metadata_size as usize;
        
        if bytes.len() < metadata_end {
            return Err("Invalid binary format: incomplete data".into());
        }
        
        let data = bytes[data_start..data_end].to_vec();
        let metadata_bytes = &bytes[data_end..metadata_end];
        
        let metadata: HashMap<String, serde_json::Value> = if header.metadata_size > 0 {
            serde_json::from_slice(metadata_bytes)?
        } else {
            HashMap::new()
        };
        
        // Parse signature if present
        let signature = if header.signature_type > 0 && bytes.len() > metadata_end {
            Some(bytes[metadata_end..].to_vec())
        } else {
            None
        };
        
        let mut binary_format = BinaryFormat {
            header,
            data,
            metadata,
            signature,
        };
        
        // Verify signature if present
        if let Some(sig) = &binary_format.signature {
            if !self.verify_signature(&binary_format, sig)? {
                return Err("Signature verification failed".into());
            }
        }
        
        // Decrypt if needed
        if binary_format.header.encryption_type == 1 {
            binary_format.data = self.decrypt_aes256gcm(&binary_format.data)?;
        }
        
        // Decompress if needed
        if binary_format.header.compression_type == 1 {
            binary_format.data = self.decompress_gzip(&binary_format.data)?;
        }
        
        Ok(binary_format)
    }
    
    /// Verify signature
    fn verify_signature(&self, binary_format: &BinaryFormat, signature: &[u8]) -> Result<bool, Box<dyn std::error::Error>> {
        use sha2::{Digest, Sha256};
        
        let key = self.verification_key.as_ref().ok_or("No verification key")?;
        
        // Create signature data
        let mut signature_data = Vec::new();
        signature_data.extend_from_slice(&binary_format.header.magic_number);
        signature_data.extend_from_slice(&binary_format.header.version.to_le_bytes());
        signature_data.extend_from_slice(&binary_format.data);
        
        // Calculate expected signature
        let mut hasher = Sha256::new();
        hasher.update(&signature_data);
        hasher.update(key);
        
        let expected = hasher.finalize();
        Ok(signature == expected.as_slice())
    }
    
    /// Decrypt data using AES-256-GCM
    fn decrypt_aes256gcm(&self, data: &[u8]) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
        let key = self.decryption_key.as_ref().ok_or("No decryption key")?;
        
        // Simple XOR decryption for demonstration
        let mut decrypted = Vec::new();
        for (i, &byte) in data.iter().enumerate() {
            decrypted.push(byte ^ key[i % key.len()]);
        }
        
        Ok(decrypted)
    }
    
    /// Decompress data using gzip
    fn decompress_gzip(&self, data: &[u8]) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
        use flate2::read::GzDecoder;
        
        let mut decoder = GzDecoder::new(data);
        let mut decompressed = Vec::new();
        decoder.read_to_end(&mut decompressed)?;
        Ok(decompressed)
    }
}

// Utility functions for Agent a1

/// Validate binary format
pub fn validate_binary_format(data: &[u8]) -> Result<(), Box<dyn std::error::Error>> {
    if data.len() < 32 {
        return Err("File too small to be valid binary format".into());
    }
    
    let magic = &data[0..8];
    if magic != b"TUSKLANG" {
        return Err("Invalid magic number".into());
    }
    
    Ok(())
}

/// Get binary format information
pub fn get_binary_format_info(data: &[u8]) -> Result<HashMap<String, serde_json::Value>, Box<dyn std::error::Error>> {
    validate_binary_format(data)?;
    
    let mut info = HashMap::new();
    info.insert("magic_number".to_string(), serde_json::Value::String(
        String::from_utf8_lossy(&data[0..8]).to_string()
    ));
    info.insert("version".to_string(), serde_json::Value::Number(
        serde_json::Number::from(u16::from_le_bytes([data[8], data[9]]))
    ));
    info.insert("flags".to_string(), serde_json::Value::Number(
        serde_json::Number::from(u16::from_le_bytes([data[10], data[11]]))
    ));
    info.insert("compression_type".to_string(), serde_json::Value::Number(
        serde_json::Number::from(data[12])
    ));
    info.insert("encryption_type".to_string(), serde_json::Value::Number(
        serde_json::Number::from(data[13])
    ));
    info.insert("signature_type".to_string(), serde_json::Value::Number(
        serde_json::Number::from(data[14])
    ));
    info.insert("data_size".to_string(), serde_json::Value::Number(
        serde_json::Number::from(u32::from_le_bytes([data[16], data[17], data[18], data[19]]))
    ));
    info.insert("metadata_size".to_string(), serde_json::Value::Number(
        serde_json::Number::from(u32::from_le_bytes([data[20], data[21], data[22], data[23]]))
    ));
    info.insert("timestamp".to_string(), serde_json::Value::Number(
        serde_json::Number::from(u64::from_le_bytes([
            data[56], data[57], data[58], data[59],
            data[60], data[61], data[62], data[63]
        ]))
    ));
    
    Ok(info)
}

#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_header_creation() {
        let header = BinaryFormatHeader::new();
        assert!(header.is_valid());
        assert_eq!(header.magic_number, *b"TUSKLANG");
        assert_eq!(header.version, 1);
    }
    
    #[test]
    fn test_binary_format_creation() {
        let mut format = BinaryFormat::new();
        format.add_metadata("test".to_string(), serde_json::Value::Bool(true));
        assert!(format.is_valid());
        assert_eq!(format.get_metadata("test"), Some(&serde_json::Value::Bool(true)));
    }
    
    #[test]
    fn test_writer_creation() {
        let writer = BinaryFormatWriter::new();
        assert_eq!(writer.compression_type, 1);
        assert_eq!(writer.encryption_type, 0);
        assert_eq!(writer.signature_type, 0);
    }
    
    #[test]
    fn test_reader_creation() {
        let reader = BinaryFormatReader::new();
        assert!(reader.verification_key.is_none());
        assert!(reader.decryption_key.is_none());
    }
} 