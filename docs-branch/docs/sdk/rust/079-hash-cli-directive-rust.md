# 🦀 #cli - Command-Line Interface Directive - Rust Edition

**"We don't bow to any king" - Rust Edition**

The `#cli` directive in Rust creates powerful command-line interfaces with zero-copy argument parsing, subcommand support, and seamless integration with Rust's CLI ecosystem.

## Basic Syntax

```rust
use tusklang_rust::{parse, CliDirective, CliHandler};
use clap::{App, Arg, SubCommand};

// Simple CLI command with Rust handler
let cli_config = r#"
#cli hello {
    handler: "HelloCommand::execute"
    description: "Say hello to the world"
    usage: "tusk hello [OPTIONS]"
}
"#;

// CLI command with arguments
let process_command = r#"
#cli process --file {
    handler: "ProcessCommand::execute"
    args: ["file_path"]
    options: ["verbose", "output"]
    description: "Process a file"
}
"#;

// CLI with subcommands
let app_cli = r#"
#cli app {
    handler: "AppCommand::execute"
    subcommands: ["init", "build", "deploy"]
    description: "Application management tool"
}
"#;
```

## Argument Parsing with Rust Types

```rust
use tusklang_rust::{CliDirective, CliArgs, CliOptions};
use serde::{Deserialize, Serialize};

#[derive(Debug, Deserialize, Serialize)]
struct CliArguments {
    file_path: String,
    verbose: bool,
    output: Option<String>,
    config: Option<String>,
}

// Positional arguments with type validation
let positional_args = r#"
#cli process {file_path} {
    handler: "ProcessCommand::execute"
    args: {
        file_path: "string|required|exists"
    }
    description: "Process a file"
    usage: "tusk process <FILE_PATH>"
}
"#;

// Optional arguments with defaults
let optional_args = r#"
#cli backup --source --destination {
    handler: "BackupCommand::execute"
    args: {
        source: "string|required|exists"
        destination: "string|optional|default:./backup"
    }
    options: {
        verbose: "bool|default:false"
        compress: "bool|default:true"
        exclude: "string|multiple"
    }
}
"#;

// Flag arguments with Rust bool types
let flag_args = r#"
#cli build --release --debug {
    handler: "BuildCommand::execute"
    flags: {
        release: "bool|default:false|help:Build in release mode"
        debug: "bool|default:false|help:Enable debug symbols"
        verbose: "bool|default:false|help:Verbose output"
    }
    options: {
        target: "string|default:debug"
        features: "string|multiple"
    }
}
"#;
```

## Subcommand Support with Rust

```rust
use tusklang_rust::{CliDirective, SubCommand, CommandGroup};

// Main command with subcommands
let main_cli = r#"
#cli tusk {
    handler: "TuskCommand::execute"
    description: "TuskLang CLI tool"
    version: "1.0.0"
    
    subcommands: {
        init: {
            handler: "InitCommand::execute"
            description: "Initialize a new TuskLang project"
            args: {
                name: "string|required"
                template: "string|optional|default:basic"
            }
        }
        
        build: {
            handler: "BuildCommand::execute"
            description: "Build the project"
            options: {
                target: "string|default:release"
                clean: "bool|default:false"
            }
        }
        
        deploy: {
            handler: "DeployCommand::execute"
            description: "Deploy the project"
            args: {
                environment: "string|required|in:dev,staging,prod"
            }
            options: {
                force: "bool|default:false"
                dry_run: "bool|default:false"
            }
        }
    }
}
"#;

// Nested subcommands
let nested_cli = r#"
#cli database {
    handler: "DatabaseCommand::execute"
    description: "Database management"
    
    subcommands: {
        migrate: {
            handler: "MigrateCommand::execute"
            description: "Run database migrations"
            options: {
                up: "bool|default:true"
                down: "bool|default:false"
                steps: "u32|default:1"
            }
        }
        
        seed: {
            handler: "SeedCommand::execute"
            description: "Seed the database"
            args: {
                seeder: "string|required"
            }
        }
        
        backup: {
            handler: "BackupCommand::execute"
            description: "Backup the database"
            options: {
                format: "string|default:sql|in:sql,json,csv"
                compress: "bool|default:true"
            }
        }
    }
}
"#;
```

## Input/Output Handling with Rust

```rust
use tusklang_rust::{CliDirective, CliIO, InputHandler, OutputHandler};
use std::io::{self, Write, Read};

// STDIN/STDOUT handling
let io_command = r#"
#cli process-stdin {
    handler: "ProcessStdinCommand::execute"
    description: "Process input from STDIN"
    
    input: {
        source: "stdin"
        format: "text"
        encoding: "utf-8"
    }
    
    output: {
        destination: "stdout"
        format: "json"
        pretty: true
    }
    
    options: {
        filter: "string|optional"
        transform: "string|optional"
    }
}
"#;

// File input/output
let file_io = r#"
#cli convert --input --output {
    handler: "ConvertCommand::execute"
    description: "Convert file format"
    
    args: {
        input: "string|required|exists"
        output: "string|required"
    }
    
    input: {
        source: "file"
        path: "@args.input"
        format: "auto"
    }
    
    output: {
        destination: "file"
        path: "@args.output"
        format: "@args.output_format"
        overwrite: "@args.force"
    }
    
    options: {
        input_format: "string|optional"
        output_format: "string|required"
        force: "bool|default:false"
    }
}
"#;

// Interactive input
let interactive = r#"
#cli setup {
    handler: "SetupCommand::execute"
    description: "Interactive setup wizard"
    
    interactive: {
        enabled: true
        prompts: {
            project_name: {
                message: "Enter project name:"
                default: "my-project"
                validation: "required|string|min:3"
            }
            
            database_url: {
                message: "Enter database URL:"
                default: "sqlite://./app.db"
                validation: "required|url"
            }
            
            api_key: {
                message: "Enter API key:"
                secret: true
                validation: "required|string|min:32"
            }
        }
    }
}
"#;
```

## Error Handling with Rust Result Types

```rust
use tusklang_rust::{CliDirective, CliError, ErrorHandler};
use thiserror::Error;

#[derive(Error, Debug)]
pub enum CliError {
    #[error("Invalid argument: {0}")]
    InvalidArgument(String),
    
    #[error("File not found: {0}")]
    FileNotFound(String),
    
    #[error("Permission denied: {0}")]
    PermissionDenied(String),
    
    #[error("Command failed: {0}")]
    CommandFailed(String),
    
    #[error("Validation failed: {0}")]
    ValidationError(String),
}

// Error handling in CLI commands
let error_handling = r#"
#cli risky-command {
    handler: "RiskyCommand::execute"
    description: "A command that might fail"
    
    error_handling: {
        file_not_found: {
            message: "File not found: {file}"
            exit_code: 1
            suggest: "Check if the file exists and try again"
        }
        
        permission_denied: {
            message: "Permission denied: {operation}"
            exit_code: 13
            suggest: "Try running with elevated privileges"
        }
        
        validation_error: {
            message: "Validation failed: {field}"
            exit_code: 2
            suggest: "Check your input and try again"
        }
        
        command_failed: {
            message: "Command failed: {reason}"
            exit_code: 1
            log: true
        }
    }
    
    return: @execute_risky_operation()
}
"#;
```

## Progress and Status Reporting

```rust
use tusklang_rust::{CliDirective, ProgressReporter, StatusBar};

// Progress reporting with Rust
let progress_command = r#"
#cli download --url {
    handler: "DownloadCommand::execute"
    description: "Download a file with progress"
    
    args: {
        url: "string|required|url"
    }
    
    progress: {
        enabled: true
        style: "bar"
        update_interval: "100ms"
        
        stages: {
            connecting: "Connecting to server..."
            downloading: "Downloading {filename}..."
            processing: "Processing file..."
            complete: "Download complete!"
        }
    }
    
    status: {
        show_speed: true
        show_eta: true
        show_percentage: true
    }
}
"#;

// Multi-stage progress
let multi_stage = r#"
#cli build {
    handler: "BuildCommand::execute"
    description: "Build project with progress"
    
    progress: {
        enabled: true
        style: "spinner"
        
        stages: {
            prepare: "Preparing build environment..."
            compile: "Compiling source files..."
            link: "Linking objects..."
            test: "Running tests..."
            package: "Creating package..."
        }
    }
    
    options: {
        verbose: "bool|default:false"
        parallel: "bool|default:true"
    }
}
"#;
```

## Configuration Management

```rust
use tusklang_rust::{CliDirective, ConfigManager, ConfigFile};

// Configuration file handling
let config_command = r#"
#cli config {
    handler: "ConfigCommand::execute"
    description: "Manage configuration"
    
    subcommands: {
        init: {
            handler: "ConfigInitCommand::execute"
            description: "Initialize configuration file"
            config_file: {
                path: "./tusk.config"
                format: "toml"
                template: "default"
            }
        }
        
        set: {
            handler: "ConfigSetCommand::execute"
            description: "Set configuration value"
            args: {
                key: "string|required"
                value: "string|required"
            }
        }
        
        get: {
            handler: "ConfigGetCommand::execute"
            description: "Get configuration value"
            args: {
                key: "string|required"
            }
        }
        
        list: {
            handler: "ConfigListCommand::execute"
            description: "List all configuration"
            options: {
                format: "string|default:table|in:table,json,yaml"
            }
        }
    }
}
"#;
```

## Integration with Rust CLI Frameworks

```rust
use clap::{App, Arg, SubCommand};
use tusklang_rust::{CliDirective, ClapIntegration};

// Clap integration
fn create_clap_app() -> App<'static, 'static> {
    let cli_directives = parse(r#"
#cli tusk {
    handler: "TuskCommand::execute"
    description: "TuskLang CLI tool"
    version: "1.0.0"
    
    subcommands: {
        init: {
            handler: "InitCommand::execute"
            description: "Initialize a new project"
        }
        
        build: {
            handler: "BuildCommand::execute"
            description: "Build the project"
        }
    }
}
"#)?;
    
    CliDirective::create_clap_app(cli_directives)
}

// Structopt integration
use structopt::StructOpt;
use tusklang_rust::StructOptIntegration;

#[derive(StructOpt)]
#[structopt(name = "tusk", about = "TuskLang CLI tool")]
struct Opt {
    #[structopt(subcommand)]
    cmd: Option<SubCommand>,
}

// Command-line argument parsing
let cli_args = r#"
#cli parse-args {
    handler: "ParseArgsCommand::execute"
    description: "Parse command line arguments"
    
    args: {
        input: "string|required"
        output: "string|optional"
    }
    
    options: {
        verbose: "bool|default:false"
        quiet: "bool|default:false"
        config: "string|optional"
    }
    
    flags: {
        help: "bool|default:false"
        version: "bool|default:false"
    }
    
    validation: {
        input: "required|string|exists"
        output: "optional|string|writable"
        verbose: "exclusive:quiet"
    }
}
"#;
```

## Testing CLI Directives with Rust

```rust
use tusklang_rust::{CliDirectiveTester, TestCommand, TestOutput};
use tokio::test;

#[tokio::test]
async fn test_cli_directive() {
    let cli_directive = r#"
#cli test-command {
    handler: "TestCommand::execute"
    description: "Test command"
    args: {
        name: "string|required"
    }
    options: {
        verbose: "bool|default:false"
    }
}
"#;
    
    let tester = CliDirectiveTester::new();
    let result = tester
        .test_cli_directive(cli_directive, &["test-command", "world"])
        .option("verbose", "true")
        .execute()
        .await?;
    
    assert_eq!(result.exit_code, 0);
    assert!(result.stdout.contains("Hello, world!"));
}

#[tokio::test]
async fn test_cli_with_subcommands() {
    let cli_directive = r#"
#cli app {
    handler: "AppCommand::execute"
    subcommands: {
        init: {
            handler: "InitCommand::execute"
            description: "Initialize project"
        }
    }
}
"#;
    
    let tester = CliDirectiveTester::new();
    let result = tester
        .test_cli_directive(cli_directive, &["app", "init"])
        .execute()
        .await?;
    
    assert_eq!(result.exit_code, 0);
}
```

## Performance Optimization with Rust

```rust
use tusklang_rust::{CliDirective, PerformanceOptimizer};
use std::sync::Arc;
use tokio::sync::RwLock;

// Zero-copy CLI processing
fn process_cli_zero_copy<'a>(directive: &'a str) -> CliDirectiveResult<CliContext<'a>> {
    let context = CliContext::from_str(directive)?;
    Ok(context)
}

// Async CLI processing with Rust futures
async fn process_cli_async(directive: &CliDirective) -> CliDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// Command caching
let cached_command = r#"
#cli expensive-operation {
    handler: "ExpensiveCommand::execute"
    description: "An expensive operation with caching"
    
    cache: {
        enabled: true
        ttl: "1h"
        key: "expensive:{@args.input}"
        strategy: "file"
    }
    
    args: {
        input: "string|required"
    }
}
"#;
```

## Security Best Practices with Rust

```rust
use tusklang_rust::{CliDirective, SecurityValidator};
use std::collections::HashSet;

// Security validation for CLI directives
struct CliSecurityValidator {
    allowed_handlers: HashSet<String>,
    allowed_commands: HashSet<String>,
    max_args: usize,
    restricted_options: HashSet<String>,
}

impl CliSecurityValidator {
    fn validate_cli_directive(&self, directive: &CliDirective) -> CliDirectiveResult<()> {
        // Validate handler
        if !self.allowed_handlers.contains(&directive.handler) {
            return Err(CliError::SecurityError(
                format!("Handler not allowed: {}", directive.handler)
            ));
        }
        
        // Validate command name
        if !self.allowed_commands.contains(&directive.command) {
            return Err(CliError::SecurityError(
                format!("Command not allowed: {}", directive.command)
            ));
        }
        
        // Validate argument count
        if directive.args.len() > self.max_args {
            return Err(CliError::SecurityError(
                format!("Too many arguments: {}", directive.args.len())
            ));
        }
        
        Ok(())
    }
}
```

## Best Practices for Rust CLI Directives

```rust
// 1. Use strong typing for CLI configurations
#[derive(Debug, Deserialize, Serialize)]
struct CliDirectiveConfig {
    command: String,
    handler: String,
    description: String,
    args: Vec<CliArg>,
    options: Vec<CliOption>,
    subcommands: Vec<SubCommand>,
}

// 2. Implement proper error handling
fn process_cli_directive_safe(directive: &str) -> Result<CliDirective, Box<dyn std::error::Error>> {
    let parsed = parse(directive)?;
    
    // Validate directive
    let validator = CliSecurityValidator::new();
    validator.validate_cli_directive(&parsed)?;
    
    Ok(parsed)
}

// 3. Use async/await for I/O operations
async fn execute_cli_directive_async(directive: &CliDirective) -> CliDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// 4. Implement proper logging and monitoring
use tracing::{info, warn, error};

fn log_cli_execution(directive: &CliDirective, result: &CliDirectiveResult<()>) {
    match result {
        Ok(_) => info!("CLI directive executed successfully: {}", directive.command),
        Err(e) => error!("CLI directive execution failed: {} - {}", directive.command, e),
    }
}
```

## Next Steps

Now that you understand the `#cli` directive in Rust, explore other directive types:

- **[#cron Directive](./080-hash-cron-directive-rust.md)** - Scheduled task execution
- **[#middleware Directive](./081-hash-middleware-directive-rust.md)** - Request processing pipeline
- **[#auth Directive](./082-hash-auth-directive-rust.md)** - Authentication and authorization
- **[#cache Directive](./083-hash-cache-directive-rust.md)** - Caching strategies

**Ready to build powerful CLI tools with Rust and TuskLang? Let's continue with the next directive!** 