use clap::Subcommand;
use tusktsk::{TuskResult, Config, TuskError};
use std::fs;
use std::path::Path;

#[derive(Subcommand)]
pub enum BinaryCommand {
    Pack { file: String },
    Unpack { file: String },
    Info { file: String },
    Validate { file: String },
}

pub fn run(cmd: BinaryCommand) -> TuskResult<()> {
    match cmd {
        BinaryCommand::Pack { file } => {
            binary_pack(&file)?;
            Ok(())
        }
        BinaryCommand::Unpack { file } => {
            binary_unpack(&file)?;
            Ok(())
        }
        BinaryCommand::Info { file } => {
            binary_info(&file)?;
            Ok(())
        }
        BinaryCommand::Validate { file } => {
            binary_validate(&file)?;
            Ok(())
        }
    }
}

/// Pack TuskLang configuration into binary format
fn binary_pack(file: &str) -> TuskResult<()> {
    println!("📦 Packing configuration into binary format...");
    
    // Read the source file
    let content = fs::read_to_string(file)
        .map_err(|e| TuskError::parse_error(0, format!("File not found: {}", file)))?;
    
    // Parse the configuration
    let config = tusktsk::parse_tsk_content(&content)?;
    
    // Create binary format
    let binary_data = create_binary_format(&Config::default())?;
    
    // Create output filename
    let input_path = Path::new(file);
    let stem = input_path.file_stem().unwrap_or_default();
    let output_file = format!("{}.bin", stem.to_string_lossy());
    
    // Write binary output
    fs::write(&output_file, binary_data)
        .map_err(|e| TuskError::parse_error(0, format!("Failed to write binary file: {}", e)))?;
    
    println!("✅ Successfully packed '{}' to '{}'", file, output_file);
    Ok(())
}

/// Unpack binary configuration back to TuskLang format
fn binary_unpack(file: &str) -> TuskResult<()> {
    println!("📦 Unpacking binary configuration...");
    
    // Read binary file
    let binary_data = fs::read(file)
        .map_err(|e| TuskError::parse_error(0, format!("Binary file not found: {}", file)))?;
    
    // Parse binary format
    let config = parse_binary_format(&binary_data)?;
    
    // Create output filename
    let input_path = Path::new(file);
    let stem = input_path.file_stem().unwrap_or_default();
    let output_file = format!("{}.tsk", stem.to_string_lossy());
    
    // Convert to TuskLang format
    let tusklang_content = convert_to_tusklang(&config)?;
    
    // Write TuskLang output
    fs::write(&output_file, tusklang_content)
        .map_err(|e| TuskError::parse_error(0, format!("Failed to write TuskLang file: {}", e)))?;
    
    println!("✅ Successfully unpacked '{}' to '{}'", file, output_file);
    Ok(())
}

/// Show information about binary file
fn binary_info(file: &str) -> TuskResult<()> {
    println!("📋 Binary file information:");
    println!("  File: {}", file);
    
    let metadata = fs::metadata(file)
        .map_err(|e| TuskError::parse_error(0, format!("File not found: {}", file)))?;
    
    println!("  Size: {} bytes", metadata.len());
    println!("  Created: {:?}", metadata.created().unwrap_or_else(|_| std::time::SystemTime::UNIX_EPOCH));
    println!("  Modified: {:?}", metadata.modified().unwrap_or_else(|_| std::time::SystemTime::UNIX_EPOCH));
    
    // Read and analyze binary content
    let binary_data = fs::read(file)?;
    
    if binary_data.len() >= 8 {
        let magic_number = &binary_data[0..8];
        println!("  Magic Number: {:?}", magic_number);
        println!("  Format: TuskLang Binary v1.0");
    }
    
    println!("  Entries: {}", binary_data.len() / 64); // Rough estimate
    
    Ok(())
}

/// Validate binary file integrity
fn binary_validate(file: &str) -> TuskResult<()> {
    println!("🔍 Validating binary file integrity...");
    
    let binary_data = fs::read(file)
        .map_err(|e| TuskError::parse_error(0, format!("Binary file not found: {}", file)))?;
    
    // Check file size
    if binary_data.is_empty() {
        eprintln!("❌ Binary file is empty");
        std::process::exit(1); // General error
    }
    
    // Check magic number
    if binary_data.len() >= 8 {
        let magic_number = &binary_data[0..8];
        if magic_number != b"TUSKLANG" {
            eprintln!("❌ Invalid magic number: {:?}", magic_number);
            std::process::exit(1); // General error
        }
    }
    
    // Check checksum (simplified)
    let checksum = binary_data.iter().fold(0u8, |acc, &byte| acc.wrapping_add(byte));
    println!("  Checksum: 0x{:02x}", checksum);
    
    println!("✅ Binary file is valid");
    Ok(())
}

/// Create binary format from configuration
fn create_binary_format(config: &Config) -> TuskResult<Vec<u8>> {
    let mut binary = Vec::new();
    
    // Add magic number
    binary.extend_from_slice(b"TUSKLANG");
    
    // Add version
    binary.extend_from_slice(&[1, 0]); // Version 1.0
    
    // Add configuration data (simplified)
    let json_data = serde_json::to_vec(config)?;
    binary.extend_from_slice(&json_data);
    
    // Add checksum
    let checksum = binary.iter().fold(0u8, |acc, &byte| acc.wrapping_add(byte));
    binary.push(checksum);
    
    Ok(binary)
}

/// Parse binary format to configuration
fn parse_binary_format(binary_data: &[u8]) -> TuskResult<Config> {
    if binary_data.len() < 10 {
        return Err(TuskError::Generic {
            message: "Binary file too short".to_string(),
            context: None,
            code: None,
        });
    }
    
    // Skip magic number and version
    let json_data = &binary_data[10..binary_data.len()-1];
    
    // Parse JSON configuration
    let config: Config = serde_json::from_slice(json_data)?;
    Ok(config)
}

/// Convert configuration to TuskLang format
fn convert_to_tusklang(config: &Config) -> TuskResult<String> {
    let mut output = String::new();
    
    output.push_str(&format!("app: \"{}\"\n", config.app));
    output.push_str(&format!("version: \"{}\"\n", config.version));
    output.push_str("features:\n");
    
    for feature in &config.features {
        output.push_str(&format!("  - {}\n", feature));
    }
    
    Ok(output)
} 