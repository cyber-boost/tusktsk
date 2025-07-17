use clap::Subcommand;
use crate::{TuskResult, parse, serialize};
use std::fs;
use std::path::Path;

#[derive(Subcommand)]
pub enum UtilityCommand {
    Parse { file: String },
    Validate { file: String },
    Convert { input: String, output: String },
    Get { file: String, key_path: String },
    Set { file: String, key_path: String, value: String },
}

pub fn run(cmd: UtilityCommand) -> TuskResult<()> {
    match cmd {
        UtilityCommand::Parse { file } => {
            utility_parse(&file)?;
            Ok(())
        }
        UtilityCommand::Validate { file } => {
            utility_validate(&file)?;
            Ok(())
        }
        UtilityCommand::Convert { input, output } => { 
            println!("[utility convert {} -> {}] stub", input, output); 
            Ok(()) 
        }
        UtilityCommand::Get { file, key_path } => { 
            println!("[utility get {} {}] stub", file, key_path); 
            Ok(()) 
        }
        UtilityCommand::Set { file, key_path, value } => { 
            println!("[utility set {} {} {}] stub", file, key_path, value); 
            Ok(()) 
        }
    }
}

/// Parse TuskLang file and display contents
fn utility_parse(file: &str) -> TuskResult<()> {
    let content = fs::read_to_string(file)
        .map_err(|e| {
            eprintln!("❌ File not found: {}", file);
            std::process::exit(3); // File not found
        })?;

    match parse(&content) {
        Ok(config) => {
            println!("✅ Successfully parsed '{}'", file);
            println!("📄 File contents:");
            println!("{}", serialize(&config)?);
            Ok(())
        }
        Err(e) => {
            eprintln!("❌ Parse error: {}", e);
            std::process::exit(1); // General error
        }
    }
}

/// Validate TuskLang file syntax
fn utility_validate(file: &str) -> TuskResult<()> {
    let content = fs::read_to_string(file)
        .map_err(|e| {
            eprintln!("❌ File not found: {}", file);
            std::process::exit(3); // File not found
        })?;

    match parse(&content) {
        Ok(_) => {
            println!("✅ File '{}' is valid TuskLang syntax", file);
            Ok(())
        }
        Err(e) => {
            eprintln!("❌ Validation failed: {}", e);
            if let Some(line) = e.line_number() {
                eprintln!("   Error occurred at line {}", line);
            }
            std::process::exit(1); // General error
        }
    }
} 