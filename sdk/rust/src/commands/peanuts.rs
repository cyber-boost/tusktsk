use clap::Subcommand;
use crate::TuskResult;
use std::fs;
use std::path::Path;

#[derive(Subcommand)]
pub enum PeanutsCommand {
    Compile { input: String, output: Option<String>, optimize: bool },
    Execute { file: String, args: Vec<String> },
    Validate { file: String },
    Decompile { file: String, output: Option<String> },
    Info { file: String },
    List { path: Option<String> },
    Sign { file: String, key: Option<String> },
    Verify { file: String, signature: Option<String> },
}

pub fn run(cmd: PeanutsCommand) -> TuskResult<()> {
    match cmd {
        PeanutsCommand::Compile { input, output, optimize } => {
            peanuts_compile(&input, output.as_deref(), optimize)?;
            Ok(())
        }
        PeanutsCommand::Execute { file, args } => {
            peanuts_execute(&file, &args)?;
            Ok(())
        }
        PeanutsCommand::Validate { file } => {
            peanuts_validate(&file)?;
            Ok(())
        }
        PeanutsCommand::Decompile { file, output } => {
            peanuts_decompile(&file, output.as_deref())?;
            Ok(())
        }
        PeanutsCommand::Info { file } => {
            peanuts_info(&file)?;
            Ok(())
        }
        PeanutsCommand::List { path } => {
            peanuts_list(path.as_deref())?;
            Ok(())
        }
        PeanutsCommand::Sign { file, key } => {
            peanuts_sign(&file, key.as_deref())?;
            Ok(())
        }
        PeanutsCommand::Verify { file, signature } => {
            peanuts_verify(&file, signature.as_deref())?;
            Ok(())
        }
    }
}

/// Compile TuskLang source to Peanut binary format
fn peanuts_compile(input: &str, output: Option<&str>, optimize: bool) -> TuskResult<()> {
    let output_path = output.unwrap_or(&format!("{}.pnt", input.trim_end_matches(".tsk")));
    
    if !Path::new(input).exists() {
        eprintln!("❌ Input file '{}' not found", input);
        std::process::exit(3);
    }
    
    // Read and parse TuskLang source
    let source = fs::read_to_string(input)?;
    
    // Parse the source (using the existing parser)
    let config = crate::parse(&source)?;
    
    // Convert to Peanut binary format
    let binary_data = serialize_to_peanut(&config, optimize)?;
    
    // Write binary file
    fs::write(output_path, binary_data)?;
    println!("✅ Compiled '{}' to '{}'", input, output_path);
    
    Ok(())
}

/// Execute a Peanut binary file
fn peanuts_execute(file: &str, args: &[String]) -> TuskResult<()> {
    if !Path::new(file).exists() {
        eprintln!("❌ Peanut file '{}' not found", file);
        std::process::exit(3);
    }
    
    // Read binary file
    let binary_data = fs::read(file)?;
    
    // Parse binary format
    let config = deserialize_from_peanut(&binary_data)?;
    
    // Execute with arguments
    println!("🚀 Executing Peanut binary: {}", file);
    if !args.is_empty() {
        println!("📝 Arguments: {}", args.join(" "));
    }
    
    // Convert config to JSON for output
    let json_output = serde_json::to_string_pretty(&config)?;
    println!("📋 Configuration:");
    println!("{}", json_output);
    
    Ok(())
}

/// Validate a Peanut binary file
fn peanuts_validate(file: &str) -> TuskResult<()> {
    if !Path::new(file).exists() {
        eprintln!("❌ Peanut file '{}' not found", file);
        std::process::exit(3);
    }
    
    let binary_data = fs::read(file)?;
    
    // Check file header
    if binary_data.len() < 8 {
        eprintln!("❌ Invalid Peanut file: too short");
        std::process::exit(1);
    }
    
    let header = &binary_data[0..8];
    if header != b"PEANUT\0\0" {
        eprintln!("❌ Invalid Peanut file: wrong header");
        std::process::exit(1);
    }
    
    // Try to deserialize
    match deserialize_from_peanut(&binary_data) {
        Ok(_) => {
            println!("✅ Peanut file '{}' is valid", file);
            Ok(())
        }
        Err(e) => {
            eprintln!("❌ Peanut file validation failed: {}", e);
            std::process::exit(1);
        }
    }
}

/// Decompile a Peanut binary to TuskLang source
fn peanuts_decompile(file: &str, output: Option<&str>) -> TuskResult<()> {
    let output_path = output.unwrap_or(&format!("{}.tsk", file.trim_end_matches(".pnt")));
    
    if !Path::new(file).exists() {
        eprintln!("❌ Peanut file '{}' not found", file);
        std::process::exit(3);
    }
    
    let binary_data = fs::read(file)?;
    let config = deserialize_from_peanut(&binary_data)?;
    
    // Convert back to TuskLang format
    let source = serialize_to_tusklang(&config)?;
    
    fs::write(output_path, source)?;
    println!("✅ Decompiled '{}' to '{}'", file, output_path);
    
    Ok(())
}

/// Get information about a Peanut binary file
fn peanuts_info(file: &str) -> TuskResult<()> {
    if !Path::new(file).exists() {
        eprintln!("❌ Peanut file '{}' not found", file);
        std::process::exit(3);
    }
    
    let metadata = fs::metadata(file)?;
    let binary_data = fs::read(file)?;
    
    println!("📊 Peanut File Information:");
    println!("   File: {}", file);
    println!("   Size: {} bytes", metadata.len());
    println!("   Created: {:?}", metadata.created().unwrap_or_default());
    println!("   Modified: {:?}", metadata.modified().unwrap_or_default());
    
    if binary_data.len() >= 8 {
        let header = &binary_data[0..8];
        if header == b"PEANUT\0\0" {
            println!("   Format: Peanut Binary v1.0");
            
            // Parse version info if available
            if binary_data.len() >= 16 {
                let version = &binary_data[8..16];
                println!("   Version: {:02x}.{:02x}.{:02x}.{:02x}", 
                    version[0], version[1], version[2], version[3]);
            }
        } else {
            println!("   Format: Unknown");
        }
    }
    
    // Try to get config info
    if let Ok(config) = deserialize_from_peanut(&binary_data) {
        let json_config = serde_json::to_value(config)?;
        if let Some(obj) = json_config.as_object() {
            println!("   Keys: {}", obj.keys().count());
            println!("   Top-level keys: {}", obj.keys().cloned().collect::<Vec<_>>().join(", "));
        }
    }
    
    Ok(())
}

/// List Peanut files in a directory
fn peanuts_list(path: Option<&str>) -> TuskResult<()> {
    let search_path = path.unwrap_or(".");
    let mut found_files = Vec::new();
    
    if let Ok(entries) = fs::read_dir(search_path) {
        for entry in entries {
            if let Ok(entry) = entry {
                let path = entry.path();
                if let Some(extension) = path.extension() {
                    if extension == "pnt" {
                        found_files.push(path);
                    }
                }
            }
        }
    }
    
    if found_files.is_empty() {
        println!("📁 No .pnt files found in '{}'", search_path);
    } else {
        println!("📁 Peanut files in '{}':", search_path);
        for file in found_files {
            if let Some(name) = file.file_name() {
                println!("   {}", name.to_string_lossy());
            }
        }
    }
    
    Ok(())
}

/// Sign a Peanut binary file
fn peanuts_sign(file: &str, key: Option<&str>) -> TuskResult<()> {
    if !Path::new(file).exists() {
        eprintln!("❌ Peanut file '{}' not found", file);
        std::process::exit(3);
    }
    
    let key_path = key.unwrap_or("peanut.key");
    let signature_path = format!("{}.sig", file);
    
    // Read file data
    let file_data = fs::read(file)?;
    
    // Generate signature (placeholder implementation)
    let signature = generate_signature(&file_data, key_path)?;
    
    // Write signature file
    fs::write(&signature_path, signature)?;
    println!("✅ Signed '{}' with signature '{}'", file, signature_path);
    
    Ok(())
}

/// Verify a Peanut binary file signature
fn peanuts_verify(file: &str, signature: Option<&str>) -> TuskResult<()> {
    if !Path::new(file).exists() {
        eprintln!("❌ Peanut file '{}' not found", file);
        std::process::exit(3);
    }
    
    let signature_path = signature.unwrap_or(&format!("{}.sig", file));
    
    if !Path::new(signature_path).exists() {
        eprintln!("❌ Signature file '{}' not found", signature_path);
        std::process::exit(3);
    }
    
    let file_data = fs::read(file)?;
    let signature_data = fs::read(signature_path)?;
    
    // Verify signature (placeholder implementation)
    if verify_signature(&file_data, &signature_data)? {
        println!("✅ Signature verification passed for '{}'", file);
    } else {
        eprintln!("❌ Signature verification failed for '{}'", file);
        std::process::exit(1);
    }
    
    Ok(())
}

// Helper functions for Peanut binary format

/// Serialize config to Peanut binary format
fn serialize_to_peanut(config: &crate::Config, optimize: bool) -> TuskResult<Vec<u8>> {
    let mut binary = Vec::new();
    
    // Peanut binary header
    binary.extend_from_slice(b"PEANUT\0\0");
    
    // Version info
    binary.extend_from_slice(&[1, 0, 0, 0]); // v1.0.0.0
    
    // Flags
    let flags = if optimize { 1u8 } else { 0u8 };
    binary.push(flags);
    
    // Convert config to JSON and compress
    let json_data = serde_json::to_vec(config)?;
    
    // Add length and data
    binary.extend_from_slice(&(json_data.len() as u32).to_le_bytes());
    binary.extend_from_slice(&json_data);
    
    Ok(binary)
}

/// Deserialize from Peanut binary format
fn deserialize_from_peanut(binary_data: &[u8]) -> TuskResult<crate::Config> {
    if binary_data.len() < 13 {
        return Err("Invalid Peanut binary: too short".into());
    }
    
    // Check header
    if &binary_data[0..8] != b"PEANUT\0\0" {
        return Err("Invalid Peanut binary: wrong header".into());
    }
    
    // Skip version and flags
    let data_start = 13;
    if binary_data.len() < data_start + 4 {
        return Err("Invalid Peanut binary: missing data length".into());
    }
    
    let data_len = u32::from_le_bytes([
        binary_data[data_start],
        binary_data[data_start + 1],
        binary_data[data_start + 2],
        binary_data[data_start + 3],
    ]) as usize;
    
    if binary_data.len() < data_start + 4 + data_len {
        return Err("Invalid Peanut binary: data truncated".into());
    }
    
    let json_data = &binary_data[data_start + 4..data_start + 4 + data_len];
    let config: crate::Config = serde_json::from_slice(json_data)?;
    
    Ok(config)
}

/// Serialize config back to TuskLang format
fn serialize_to_tusklang(config: &crate::Config) -> TuskResult<String> {
    // Convert to TuskLang format (simplified)
    let json_value = serde_json::to_value(config)?;
    let mut output = String::new();
    
    serialize_value_to_tusklang(&json_value, &mut output, 0)?;
    
    Ok(output)
}

/// Recursively serialize JSON value to TuskLang format
fn serialize_value_to_tusklang(value: &serde_json::Value, output: &mut String, indent: usize) -> TuskResult<()> {
    match value {
        serde_json::Value::Object(obj) => {
            for (key, val) in obj {
                output.push_str(&"    ".repeat(indent));
                output.push_str(key);
                output.push_str(" = ");
                
                match val {
                    serde_json::Value::String(s) => {
                        output.push('"');
                        output.push_str(s);
                        output.push('"');
                    }
                    serde_json::Value::Number(n) => {
                        output.push_str(&n.to_string());
                    }
                    serde_json::Value::Bool(b) => {
                        output.push_str(&b.to_string());
                    }
                    serde_json::Value::Object(_) => {
                        output.push_str("{\n");
                        serialize_value_to_tusklang(val, output, indent + 1)?;
                        output.push_str(&"    ".repeat(indent));
                        output.push_str("}");
                    }
                    _ => {
                        output.push_str(&val.to_string());
                    }
                }
                output.push('\n');
            }
        }
        _ => {
            output.push_str(&value.to_string());
        }
    }
    
    Ok(())
}

/// Generate signature for file data
fn generate_signature(data: &[u8], key_path: &str) -> TuskResult<Vec<u8>> {
    // Placeholder implementation - would use actual cryptographic signing
    let mut signature = Vec::new();
    signature.extend_from_slice(b"SIGNATURE");
    signature.extend_from_slice(&(data.len() as u32).to_le_bytes());
    
    // Simple hash-based signature
    let mut hash = 0u32;
    for &byte in data {
        hash = hash.wrapping_add(byte as u32).wrapping_mul(31);
    }
    signature.extend_from_slice(&hash.to_le_bytes());
    
    Ok(signature)
}

/// Verify signature for file data
fn verify_signature(data: &[u8], signature: &[u8]) -> TuskResult<bool> {
    if signature.len() < 16 {
        return Ok(false);
    }
    
    if &signature[0..9] != b"SIGNATURE" {
        return Ok(false);
    }
    
    let expected_len = u32::from_le_bytes([
        signature[9], signature[10], signature[11], signature[12],
    ]) as usize;
    
    if data.len() != expected_len {
        return Ok(false);
    }
    
    let expected_hash = u32::from_le_bytes([
        signature[13], signature[14], signature[15], signature[16],
    ]);
    
    let mut actual_hash = 0u32;
    for &byte in data {
        actual_hash = actual_hash.wrapping_add(byte as u32).wrapping_mul(31);
    }
    
    Ok(actual_hash == expected_hash)
} 