use crate::{EnhancedParser, load_from_peanut, TuskResult};
use std::env;
use std::process;

/// Enhanced CLI for TuskLang Rust SDK
pub fn run_enhanced_cli() {
    let args: Vec<String> = env::args().collect();
    
    if args.len() < 2 {
        show_help();
        return;
    }
    
    let command = &args[1];
    
    match command.as_str() {
        "parse" => {
            if args.len() < 3 {
                eprintln!("Error: File path required");
                process::exit(1);
            }
            
            let mut parser = EnhancedParser::new();
            if let Err(e) = parser.parse_file(&args[2]) {
                eprintln!("Error parsing file: {}", e);
                process::exit(1);
            }
            
            for key in parser.keys() {
                if let Some(value) = parser.get(&key) {
                    println!("{} = {}", key, value);
                }
            }
        }
        
        "get" => {
            if args.len() < 4 {
                eprintln!("Error: File path and key required");
                process::exit(1);
            }
            
            let mut parser = EnhancedParser::new();
            if let Err(e) = parser.parse_file(&args[2]) {
                eprintln!("Error parsing file: {}", e);
                process::exit(1);
            }
            
            if let Some(value) = parser.get(&args[3]) {
                println!("{}", value);
            }
        }
        
        "keys" => {
            if args.len() < 3 {
                eprintln!("Error: File path required");
                process::exit(1);
            }
            
            let mut parser = EnhancedParser::new();
            if let Err(e) = parser.parse_file(&args[2]) {
                eprintln!("Error parsing file: {}", e);
                process::exit(1);
            }
            
            for key in parser.keys() {
                println!("{}", key);
            }
        }
        
        "peanut" => {
            match load_from_peanut() {
                Ok(parser) => {
                    println!("Loaded {} configuration items", parser.items().len());
                    
                    for key in parser.keys() {
                        if let Some(value) = parser.get(&key) {
                            println!("{} = {}", key, value);
                        }
                    }
                }
                Err(e) => {
                    eprintln!("Error loading peanut.tsk: {}", e);
                    process::exit(1);
                }
            }
        }
        
        "json" => {
            if args.len() < 3 {
                eprintln!("Error: File path required");
                process::exit(1);
            }
            
            let mut parser = EnhancedParser::new();
            if let Err(e) = parser.parse_file(&args[2]) {
                eprintln!("Error parsing file: {}", e);
                process::exit(1);
            }
            
            match serde_json::to_string_pretty(&parser.items()) {
                Ok(json) => println!("{}", json),
                Err(e) => {
                    eprintln!("Error converting to JSON: {}", e);
                    process::exit(1);
                }
            }
        }
        
        "validate" => {
            if args.len() < 3 {
                eprintln!("Error: File path required");
                process::exit(1);
            }
            
            let mut parser = EnhancedParser::new();
            match parser.parse_file(&args[2]) {
                Ok(_) => println!("✅ File is valid TuskLang syntax"),
                Err(e) => {
                    eprintln!("❌ Validation failed: {}", e);
                    process::exit(1);
                }
            }
        }
        
        _ => {
            eprintln!("Error: Unknown command: {}", command);
            show_help();
            process::exit(1);
        }
    }
}

fn show_help() {
    println!(r#"
TuskLang Enhanced for Rust - The Freedom Parser
==============================================

Usage: tusklang-rust [command] [options]

Commands:
    parse <file>     Parse a .tsk file and show all key-value pairs
    get <file> <key> Get a specific value by key
    keys <file>      List all keys in the file
    json <file>      Convert .tsk file to JSON format
    validate <file>  Validate .tsk file syntax
    peanut           Load configuration from peanut.tsk
    
Examples:
    tusklang-rust parse config.tsk
    tusklang-rust get config.tsk database.host
    tusklang-rust keys config.tsk
    tusklang-rust json config.tsk
    tusklang-rust validate config.tsk
    tusklang-rust peanut

Features:
    - Multiple syntax styles: [], {}, <>
    - Global variables with $
    - Cross-file references: @file.tsk.get()
    - Database queries: @query()
    - Date functions: @date()
    - Environment variables: @env()
    - Conditional expressions (ternary operator)
    - Range syntax: 8000-9000
    - String concatenation with +
    - Optional semicolons
    - Ultra-fast parsing with zero-copy operations
    - WebAssembly support

Default config file: peanut.tsk
"We don't bow to any king" - Maximum syntax flexibility
"#);
}