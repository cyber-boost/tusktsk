use clap::Subcommand;
use crate::{TuskResult, parse, Config};
use std::fs;
use std::path::Path;

#[derive(Subcommand)]
pub enum ConfigCommand {
    Get { key_path: String, dir: Option<String> },
    Check { path: Option<String> },
    Validate { path: Option<String> },
    Compile { path: Option<String> },
    Docs { path: Option<String> },
    ClearCache { path: Option<String> },
    Stats,
}

pub fn run(cmd: ConfigCommand) -> TuskResult<()> {
    match cmd {
        ConfigCommand::Get { key_path, dir } => {
            config_get(&key_path, dir.as_deref())?;
            Ok(())
        }
        ConfigCommand::Check { path } => { 
            println!("[config check {:?}] stub", path); 
            Ok(()) 
        }
        ConfigCommand::Validate { path } => { 
            println!("[config validate {:?}] stub", path); 
            Ok(()) 
        }
        ConfigCommand::Compile { path } => { 
            println!("[config compile {:?}] stub", path); 
            Ok(()) 
        }
        ConfigCommand::Docs { path } => { 
            println!("[config docs {:?}] stub", path); 
            Ok(()) 
        }
        ConfigCommand::ClearCache { path } => { 
            println!("[config clear-cache {:?}] stub", path); 
            Ok(()) 
        }
        ConfigCommand::Stats => { 
            println!("[config stats] stub"); 
            Ok(()) 
        }
    }
}

/// Get configuration value by key path
fn config_get(key_path: &str, dir: Option<&str>) -> TuskResult<()> {
    // Determine search directory
    let search_dir = dir.unwrap_or(".");
    
    // Look for configuration files
    let config_files = ["peanu.pnt", "peanu.tsk"];
    let mut config: Option<Config> = None;
    
    for filename in &config_files {
        let config_path = Path::new(search_dir).join(filename);
        if let Ok(content) = fs::read_to_string(config_path) {
            config = Some(parse(&content)?);
            break;
        }
    }
    
    if let Some(config) = config {
        // Resolve key path (e.g., "server.port" -> nested value)
        if let Some(value) = get_value_by_path(&config, key_path) {
            println!("✅ {}", value);
        } else {
            eprintln!("❌ Key '{}' not found in configuration", key_path);
            std::process::exit(3); // File not found
        }
    } else {
        eprintln!("❌ No configuration file found in '{}'", search_dir);
        std::process::exit(3); // File not found
    }
    
    Ok(())
}

/// Get value by dot-separated key path
fn get_value_by_path(config: &Config, key_path: &str) -> Option<String> {
    let keys: Vec<&str> = key_path.split('.').collect();
    let mut current: &serde_json::Value = &serde_json::to_value(config).ok()?;
    
    for key in keys {
        current = current.get(key)?;
    }
    
    Some(current.to_string())
} 