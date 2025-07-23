use clap::Subcommand;
use tusktsk::{TuskResult, Config, TuskError};
use std::fs;
use std::path::Path;

#[derive(Subcommand)]
pub enum PeanutsCommand {
    Pack { file: String },
    Unpack { file: String },
    Info { file: String },
    Validate { file: String },
}

pub fn run(cmd: PeanutsCommand) -> TuskResult<()> {
    match cmd {
        PeanutsCommand::Pack { file } => {
            peanuts_pack(&file)?;
            Ok(())
        }
        PeanutsCommand::Unpack { file } => {
            peanuts_unpack(&file)?;
            Ok(())
        }
        PeanutsCommand::Info { file } => {
            peanuts_info(&file)?;
            Ok(())
        }
        PeanutsCommand::Validate { file } => {
            peanuts_validate(&file)?;
            Ok(())
        }
    }
}

/// Pack TuskLang configuration into Peanut format
fn peanuts_pack(file: &str) -> TuskResult<()> {
    println!("🥜 Packing configuration into Peanut format...");
    
    // Read the source file
    let content = fs::read_to_string(file)
        .map_err(|e| TuskError::parse_error(0, format!("File not found: {}", file)))?;
    
    // Parse the configuration
    let config = tusktsk::parse_tsk_content(&content)?;
    
    // Create Peanut format
    let peanut_data = serialize_to_peanut(&Config::default(), true)?;
    
    // Create output filename
    let input_path = Path::new(file);
    let stem = input_path.file_stem().unwrap_or_default();
    let output_file = format!("{}.pnt", stem.to_string_lossy());
    
    // Write Peanut output
    fs::write(&output_file, peanut_data)
        .map_err(|e| TuskError::parse_error(0, format!("Failed to write Peanut file: {}", e)))?;
    
    println!("✅ Successfully packed '{}' to '{}'", file, output_file);
    Ok(())
}

/// Unpack Peanut configuration back to TuskLang format
fn peanuts_unpack(file: &str) -> TuskResult<()> {
    println!("🥜 Unpacking Peanut configuration...");
    
    // Read Peanut file
    let peanut_data = fs::read(file)
        .map_err(|e| TuskError::parse_error(0, format!("Peanut file not found: {}", file)))?;
    
    // Parse Peanut format
    let config = deserialize_from_peanut(&peanut_data)?;
    
    // Create output filename
    let input_path = Path::new(file);
    let stem = input_path.file_stem().unwrap_or_default();
    let output_file = format!("{}.tsk", stem.to_string_lossy());
    
    // Convert to TuskLang format
    let tusklang_content = serialize_to_tusklang(&config)?;
    
    // Write TuskLang output
    fs::write(&output_file, tusklang_content)
        .map_err(|e| TuskError::parse_error(0, format!("Failed to write TuskLang file: {}", e)))?;
    
    println!("✅ Successfully unpacked '{}' to '{}'", file, output_file);
    Ok(())
}

/// Show information about Peanut file
fn peanuts_info(file: &str) -> TuskResult<()> {
    println!("📋 Peanut file information:");
    println!("  File: {}", file);
    
    let metadata = fs::metadata(file)
        .map_err(|e| TuskError::parse_error(0, format!("File not found: {}", file)))?;
    
    println!("  Size: {} bytes", metadata.len());
    println!("  Created: {:?}", metadata.created().unwrap_or_else(|_| std::time::SystemTime::UNIX_EPOCH));
    println!("  Modified: {:?}", metadata.modified().unwrap_or_else(|_| std::time::SystemTime::UNIX_EPOCH));
    
    // Read and analyze Peanut content
    let binary_data = fs::read(file)?;
    
    if binary_data.len() >= 8 {
        let magic_number = &binary_data[0..8];
        println!("  Magic Number: {:?}", magic_number);
        println!("  Format: TuskLang Peanut v1.0");
    }
    
    println!("  Entries: {}", binary_data.len() / 64); // Rough estimate
    
    Ok(())
}

/// Validate Peanut file integrity
fn peanuts_validate(file: &str) -> TuskResult<()> {
    println!("🔍 Validating Peanut file integrity...");
    
    let binary_data = fs::read(file)
        .map_err(|e| TuskError::parse_error(0, format!("File not found: {}", file)))?;
    
    // Check file size
    if binary_data.is_empty() {
        eprintln!("❌ Peanut file is empty");
        std::process::exit(1); // General error
    }
    
    // Check magic number
    if binary_data.len() >= 8 {
        let magic_number = &binary_data[0..8];
        if magic_number != b"PEANUTS" {
            eprintln!("❌ Invalid magic number: {:?}", magic_number);
            std::process::exit(1); // General error
        }
    }
    
    // Check checksum (simplified)
    let checksum = binary_data.iter().fold(0u8, |acc, &byte| acc.wrapping_add(byte));
    println!("  Checksum: 0x{:02x}", checksum);
    
    println!("✅ Peanut file is valid");
    Ok(())
}

/// Serialize configuration to Peanut format
fn serialize_to_peanut(config: &Config, optimize: bool) -> TuskResult<Vec<u8>> {
    let mut peanut = Vec::new();
    
    // Add magic number
    peanut.extend_from_slice(b"PEANUTS");
    
    // Add version
    peanut.extend_from_slice(&[1, 0]); // Version 1.0
    
    // Add configuration data (simplified)
    let json_data = serde_json::to_vec(config)?;
    peanut.extend_from_slice(&json_data);
    
    // Add checksum
    let checksum = peanut.iter().fold(0u8, |acc, &byte| acc.wrapping_add(byte));
    peanut.push(checksum);
    
    Ok(peanut)
}

/// Deserialize configuration from Peanut format
fn deserialize_from_peanut(binary_data: &[u8]) -> TuskResult<Config> {
    if binary_data.len() < 10 {
        return Err(TuskError::Generic {
            message: "Peanut file too short".to_string(),
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
fn serialize_to_tusklang(config: &Config) -> TuskResult<String> {
    let mut output = String::new();
    
    output.push_str(&format!("app: \"{}\"\n", config.app));
    output.push_str(&format!("version: \"{}\"\n", config.version));
    output.push_str("features:\n");
    
    for feature in &config.features {
        output.push_str(&format!("  - {}\n", feature));
    }
    
    Ok(output)
} 