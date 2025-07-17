use clap::Subcommand;
use crate::{TuskResult, parse, serialize};
use std::fs;
use std::path::Path;

#[derive(Subcommand)]
pub enum DevCommand {
    Serve { port: Option<u16> },
    Compile { file: String },
    Optimize { file: String },
}

pub fn run(cmd: DevCommand) -> TuskResult<()> {
    match cmd {
        DevCommand::Serve { port } => { 
            println!("[serve {:?}] stub", port); 
            Ok(()) 
        }
        DevCommand::Compile { file } => {
            dev_compile(&file)?;
            Ok(())
        }
        DevCommand::Optimize { file } => { 
            println!("[optimize {}] stub", file); 
            Ok(()) 
        }
    }
}

/// Compile TuskLang file to optimized format
fn dev_compile(file: &str) -> TuskResult<()> {
    let content = fs::read_to_string(file)
        .map_err(|e| {
            eprintln!("❌ File not found: {}", file);
            std::process::exit(3); // File not found
        })?;

    match parse(&content) {
        Ok(config) => {
            // Generate optimized output
            let optimized = serialize(&config)?;
            
            // Create output filename
            let input_path = Path::new(file);
            let stem = input_path.file_stem().unwrap_or_default();
            let output_file = format!("{}.compiled.tsk", stem.to_string_lossy());
            
            // Write compiled output
            fs::write(&output_file, optimized)
                .map_err(|e| {
                    eprintln!("❌ Failed to write output file: {}", e);
                    std::process::exit(4); // Permission denied
                })?;
            
            println!("✅ Successfully compiled '{}' to '{}'", file, output_file);
            Ok(())
        }
        Err(e) => {
            eprintln!("❌ Compilation failed: {}", e);
            std::process::exit(1); // General error
        }
    }
} 