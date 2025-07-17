use crate::{parse, serialize, Config, TuskResult};
use clap::{Parser as ClapParser, Subcommand, ValueEnum};
use serde_json;
use std::fs;
use std::path::Path;
use std::process;

mod commands;
use commands::*;

#[derive(ClapParser)]
#[command(name = "tusk-rust")]
#[command(about = "Ultra-fast Rust TuskLang parser and CLI tool")]
#[command(version = "0.1.0")]
pub struct Cli {
    /// Enable verbose output
    #[arg(short, long)]
    verbose: bool,

    /// Suppress non-error output
    #[arg(short, long)]
    quiet: bool,

    /// Use alternate config file
    #[arg(long)]
    config: Option<String>,

    /// Output in JSON format
    #[arg(long)]
    json: bool,

    #[command(subcommand)]
    command: Option<Commands>,
}

#[derive(Subcommand)]
enum Commands {
    // Core commands (existing)
    Parse { file: String, format: String, pretty: bool },
    Validate { file: String, verbose: bool },
    Gen { file: String, language: String, output: Option<String> },
    Convert { input: String, from: String, to: String, output: Option<String> },
    Bench { file: String, iterations: usize },

    // Universal CLI Command Spec stubs
    Db(commands::db::DbCommand),
    Dev(commands::dev::DevCommand),
    Test(commands::test::TestCommand),
    Services(commands::services::ServicesCommand),
    Cache(commands::cache::CacheCommand),
    Config(commands::config::ConfigCommand),
    Binary(commands::binary::BinaryCommand),
    Ai(commands::ai::AiCommand),
    Utility(commands::utility::UtilityCommand),
    Css(commands::css::CssCommand),
    License(commands::license::LicenseCommand),
    Peanuts(commands::peanuts::PeanutsCommand),
}

/// Run the CLI application
pub fn run() -> TuskResult<()> {
    let cli = Cli::parse();

    // Handle global options
    if cli.verbose {
        println!("🔍 Verbose mode enabled");
    }

    // Load configuration hierarchy
    let config = load_configuration(&cli.config)?;

    match cli.command {
        Some(cmd) => {
            let result = match cmd {
                Commands::Parse { file, format, pretty } => parse_command(&file, &format, pretty),
                Commands::Validate { file, verbose } => validate_command(&file, verbose),
                Commands::Gen { file, language, output } => gen_command(&file, &language, output.as_deref()),
                Commands::Convert { input, from, to, output } => convert_command(&input, &from, &to, output.as_deref()),
                Commands::Bench { file, iterations } => bench_command(&file, iterations),
                Commands::Db(cmd) => commands::db::run(cmd),
                Commands::Dev(cmd) => commands::dev::run(cmd),
                Commands::Test(cmd) => commands::test::run(cmd),
                Commands::Services(cmd) => commands::services::run(cmd),
                Commands::Cache(cmd) => commands::cache::run(cmd),
                Commands::Config(cmd) => commands::config::run(cmd),
                Commands::Binary(cmd) => commands::binary::run(cmd),
                Commands::Ai(cmd) => commands::ai::run(cmd),
                Commands::Utility(cmd) => commands::utility::run(cmd),
                Commands::Css(cmd) => commands::css::run(cmd),
                Commands::License(cmd) => commands::license::run(cmd),
                Commands::Peanuts(cmd) => commands::peanuts::run(cmd),
            };

            match result {
                Ok(_) => {
                    process::exit(0); // Success
                }
                Err(e) => {
                    if !cli.quiet {
                        eprintln!("❌ Error: {}", e);
                    }
                    process::exit(1); // General error
                }
            }
        }
        None => {
            // Interactive REPL mode
            interactive_mode()?;
        }
    }

    Ok(())
}

/// Load configuration following hierarchical order
fn load_configuration(cli_config: &Option<String>) -> TuskResult<Option<Config>> {
    // 1. Command-line specified config
    if let Some(config_path) = cli_config {
        if let Ok(content) = fs::read_to_string(config_path) {
            return Ok(Some(parse(&content)?));
        }
    }

    // 2. Current directory peanu.pnt or peanu.tsk
    for filename in &["peanu.pnt", "peanu.tsk"] {
        if let Ok(content) = fs::read_to_string(filename) {
            return Ok(Some(parse(&content)?));
        }
    }

    // 3. Parent directories (walking up)
    let mut current_dir = std::env::current_dir()?;
    for _ in 0..10 { // Limit depth to prevent infinite loops
        for filename in &["peanu.pnt", "peanu.tsk"] {
            let config_path = current_dir.join(filename);
            if let Ok(content) = fs::read_to_string(config_path) {
                return Ok(Some(parse(&content)?));
            }
        }
        
        if !current_dir.pop() {
            break;
        }
    }

    // 4. User home directory ~/.tusklang/config.tsk
    if let Some(home) = dirs::home_dir() {
        let config_path = home.join(".tusklang").join("config.tsk");
        if let Ok(content) = fs::read_to_string(config_path) {
            return Ok(Some(parse(&content)?));
        }
    }

    // 5. System-wide /etc/tusklang/config.tsk
    let system_config = Path::new("/etc/tusklang/config.tsk");
    if let Ok(content) = fs::read_to_string(system_config) {
        return Ok(Some(parse(&content)?));
    }

    Ok(None)
}

/// Interactive REPL mode
fn interactive_mode() -> TuskResult<()> {
    println!("TuskLang v0.1.0 - Interactive Mode");
    println!("Type 'help' for commands, 'exit' to quit");
    
    use std::io::{self, Write};
    
    loop {
        print!("tsk> ");
        io::stdout().flush()?;
        
        let mut input = String::new();
        io::stdin().read_line(&mut input)?;
        
        let input = input.trim();
        
        match input {
            "exit" | "quit" => break,
            "help" => {
                println!("Available commands:");
                println!("  db status|migrate|console|backup|restore|init");
                println!("  dev serve|compile|optimize");
                println!("  test all|parser|fujsen|sdk|performance");
                println!("  services start|stop|restart|status");
                println!("  cache clear|status|warm|memcached|distributed");
                println!("  config get|check|validate|compile|docs|clear-cache|stats");
                println!("  binary compile|execute|benchmark|optimize");
                println!("  ai claude|chatgpt|analyze|optimize|security");
                println!("  utility parse|validate|convert|get|set");
                println!("  css compile|watch|optimize|validate|lint|format|stats");
                println!("  license generate|validate|check|add|remove|list|info");
                println!("  peanuts compile|execute|validate|decompile|info|list|sign|verify");
                println!("  exit - Exit interactive mode");
            }
            "" => continue,
            _ => {
                // Parse and execute command
                let args: Vec<&str> = input.split_whitespace().collect();
                if !args.is_empty() {
                    println!("🔄 Executing: {}", input);
                    // TODO: Implement command parsing and execution
                    println!("⚠️  Command execution not yet implemented in interactive mode");
                }
            }
        }
    }
    
    println!("👋 Goodbye!");
    Ok(())
}

/// Parse command implementation
fn parse_command(file: &str, format: &str, pretty: bool) -> TuskResult<()> {
    let content = fs::read_to_string(file)
        .map_err(|e| crate::error::TuskError::io_error(e.to_string()))?;

    let config = parse(&content)?;

    let output = match format.to_lowercase().as_str() {
        "json" => {
            if pretty {
                serde_json::to_string_pretty(&config)?
            } else {
                serde_json::to_string(&config)?
            }
        }
        "yaml" => serde_yaml::to_string(&config)?,
        "tsk" => serialize(&config)?,
        _ => return Err(crate::error::TuskError::validation_error(
            format!("Unsupported output format: {}", format)
        )),
    };

    println!("{}", output);
    Ok(())
}

/// Validate command implementation
fn validate_command(file: &str, verbose: bool) -> TuskResult<()> {
    let content = fs::read_to_string(file)
        .map_err(|e| crate::error::TuskError::io_error(e.to_string()))?;

    match parse(&content) {
        Ok(_) => {
            if verbose {
                println!("✅ File '{}' is valid TuskLang syntax", file);
            } else {
                println!("✅ Valid");
            }
            Ok(())
        }
        Err(e) => {
            if verbose {
                eprintln!("❌ Validation failed: {}", e);
                if let Some(line) = e.line_number() {
                    eprintln!("   Error occurred at line {}", line);
                }
            } else {
                eprintln!("❌ Invalid");
            }
            Err(e)
        }
    }
}

/// Generate command implementation
fn gen_command(file: &str, language: &str, output_file: Option<&str>) -> TuskResult<()> {
    let content = fs::read_to_string(file)
        .map_err(|e| crate::error::TuskError::io_error(e.to_string()))?;

    let config = parse(&content)?;
    let file_name = Path::new(file).file_stem().unwrap_or_default().to_string_lossy();

    let generated_code = match language.to_lowercase().as_str() {
        "rust" => generate_rust_struct(&file_name, &config)?,
        "json" => serde_json::to_string_pretty(&config)?,
        "yaml" => serde_yaml::to_string(&config)?,
        _ => return Err(crate::error::TuskError::validation_error(
            format!("Unsupported language: {}", language)
        )),
    };

    if let Some(output_path) = output_file {
        fs::write(output_path, generated_code)
            .map_err(|e| crate::error::TuskError::io_error(e.to_string()))?;
        println!("Generated code written to: {}", output_path);
    } else {
        println!("{}", generated_code);
    }

    Ok(())
}

/// Convert command implementation
fn convert_command(input: &str, from: &str, to: &str, output_file: Option<&str>) -> TuskResult<()> {
    let content = fs::read_to_string(input)
        .map_err(|e| crate::error::TuskError::io_error(e.to_string()))?;

    // Parse input format
    let config = match from.to_lowercase().as_str() {
        "tsk" => parse(&content)?,
        "json" => serde_json::from_str(&content)?,
        "yaml" => serde_yaml::from_str(&content)?,
        _ => return Err(crate::error::TuskError::validation_error(
            format!("Unsupported input format: {}", from)
        )),
    };

    // Convert to output format
    let output = match to.to_lowercase().as_str() {
        "tsk" => serialize(&config)?,
        "json" => serde_json::to_string_pretty(&config)?,
        "yaml" => serde_yaml::to_string(&config)?,
        _ => return Err(crate::error::TuskError::validation_error(
            format!("Unsupported output format: {}", to)
        )),
    };

    if let Some(output_path) = output_file {
        fs::write(output_path, output)
            .map_err(|e| crate::error::TuskError::io_error(e.to_string()))?;
        println!("Converted file written to: {}", output_path);
    } else {
        println!("{}", output);
    }

    Ok(())
}

/// Benchmark command implementation
fn bench_command(file: &str, iterations: usize) -> TuskResult<()> {
    let content = fs::read_to_string(file)
        .map_err(|e| crate::error::TuskError::io_error(e.to_string()))?;

    println!("Running benchmark with {} iterations...", iterations);
    
    let start = std::time::Instant::now();
    
    for _ in 0..iterations {
        parse(&content)?;
    }
    
    let duration = start.elapsed();
    let avg_time = duration.as_nanos() as f64 / iterations as f64;
    
    println!("Results:");
    println!("  Total time: {:?}", duration);
    println!("  Average time per parse: {:.2} ns", avg_time);
    println!("  Parses per second: {:.0}", 1_000_000_000.0 / avg_time);
    
    Ok(())
}

/// Generate Rust struct from TuskLang config
fn generate_rust_struct(struct_name: &str, config: &Config) -> TuskResult<String> {
    let mut code = String::new();
    
    // Convert to PascalCase
    let struct_name = to_pascal_case(struct_name);
    
    code.push_str(&format!("#[derive(Debug, Clone, Serialize, Deserialize)]\n"));
    code.push_str(&format!("pub struct {} {{\n", struct_name));
    
    for (key, value) in config {
        let field_name = to_snake_case(key);
        let field_type = get_rust_type(value);
        code.push_str(&format!("    #[serde(rename = \"{}\")]\n", key));
        code.push_str(&format!("    pub {}: {},\n", field_name, field_type));
    }
    
    code.push_str("}\n");
    
    Ok(code)
}

/// Convert string to PascalCase
fn to_pascal_case(s: &str) -> String {
    s.split(|c| c == '_' || c == '-')
        .map(|word| {
            let mut chars = word.chars();
            match chars.next() {
                None => String::new(),
                Some(first) => first.to_uppercase().chain(chars).collect(),
            }
        })
        .collect()
}

/// Convert string to snake_case
fn to_snake_case(s: &str) -> String {
    s.replace('-', "_").to_lowercase()
}

/// Get Rust type for a TuskLang value
fn get_rust_type(value: &crate::value::Value) -> String {
    match value {
        crate::value::Value::String(_) => "String".to_string(),
        crate::value::Value::Number(n) => {
            if n.fract() == 0.0 {
                "i64".to_string()
            } else {
                "f64".to_string()
            }
        }
        crate::value::Value::Boolean(_) => "bool".to_string(),
        crate::value::Value::Array(arr) => {
            if arr.is_empty() {
                "Vec<serde_json::Value>".to_string()
            } else {
                format!("Vec<{}>", get_rust_type(&arr[0]))
            }
        }
        crate::value::Value::Object(_) => "serde_json::Value".to_string(),
        crate::value::Value::Null => "Option<serde_json::Value>".to_string(),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_to_pascal_case() {
        assert_eq!(to_pascal_case("app_name"), "AppName");
        assert_eq!(to_pascal_case("database_config"), "DatabaseConfig");
        assert_eq!(to_pascal_case("api-v1"), "ApiV1");
    }

    #[test]
    fn test_to_snake_case() {
        assert_eq!(to_snake_case("appName"), "appname");
        assert_eq!(to_snake_case("database-config"), "database_config");
        assert_eq!(to_snake_case("API_V1"), "api_v1");
    }
} 