# CLI Hash Directives in TuskLang for Go

## Overview

CLI hash directives in TuskLang provide powerful command-line interface capabilities directly in your configuration files. These directives enable you to create sophisticated CLI tools, automate tasks, and build interactive command-line applications with Go integration.

## Basic CLI Directive Syntax

```go
// TuskLang CLI directive
#cli {
    command: "myapp"
    version: "1.0.0"
    description: "My awesome CLI application"
    
    flags: {
        verbose: {
            type: "bool"
            short: "v"
            description: "Enable verbose output"
        }
        config: {
            type: "string"
            short: "c"
            description: "Configuration file path"
            default: "config.tsk"
        }
    }
    
    commands: {
        build: {
            description: "Build the application"
            action: "build_command"
        }
        deploy: {
            description: "Deploy to production"
            action: "deploy_command"
        }
    }
}
```

## Go Integration

```go
package main

import (
    "fmt"
    "os"
    "github.com/tusklang/go-sdk"
)

type CLIDirective struct {
    Command     string            `tsk:"command"`
    Version     string            `tsk:"version"`
    Description string            `tsk:"description"`
    Flags       map[string]Flag   `tsk:"flags"`
    Commands    map[string]Cmd    `tsk:"commands"`
}

type Flag struct {
    Type        string `tsk:"type"`
    Short       string `tsk:"short"`
    Description string `tsk:"description"`
    Default     string `tsk:"default"`
}

type Cmd struct {
    Description string `tsk:"description"`
    Action      string `tsk:"action"`
}

func main() {
    // Load CLI configuration
    config, err := tusk.LoadFile("cli-config.tsk")
    if err != nil {
        fmt.Printf("Error loading CLI config: %v\n", err)
        os.Exit(1)
    }
    
    var cliDirective CLIDirective
    if err := config.Get("#cli", &cliDirective); err != nil {
        fmt.Printf("Error parsing CLI directive: %v\n", err)
        os.Exit(1)
    }
    
    // Initialize CLI application
    app := NewCLIApp(cliDirective)
    app.Run()
}

type CLIApp struct {
    config CLIDirective
    flags  map[string]interface{}
}

func NewCLIApp(config CLIDirective) *CLIApp {
    return &CLIApp{
        config: config,
        flags:  make(map[string]interface{}),
    }
}

func (app *CLIApp) Run() {
    // Parse command line arguments
    app.parseFlags()
    
    // Execute command
    if len(os.Args) < 2 {
        app.showHelp()
        return
    }
    
    command := os.Args[1]
    if cmd, exists := app.config.Commands[command]; exists {
        app.executeCommand(cmd)
    } else {
        fmt.Printf("Unknown command: %s\n", command)
        app.showHelp()
    }
}

func (app *CLIApp) parseFlags() {
    // Parse flags based on configuration
    for flagName, flag := range app.config.Flags {
        // Set default value
        app.flags[flagName] = flag.Default
        
        // Parse command line flags
        // Implementation depends on your flag parsing library
    }
}

func (app *CLIApp) executeCommand(cmd Cmd) {
    switch cmd.Action {
    case "build_command":
        app.buildCommand()
    case "deploy_command":
        app.deployCommand()
    default:
        fmt.Printf("Unknown action: %s\n", cmd.Action)
    }
}

func (app *CLIApp) buildCommand() {
    fmt.Println("Building application...")
    // Build logic here
}

func (app *CLIApp) deployCommand() {
    fmt.Println("Deploying to production...")
    // Deploy logic here
}

func (app *CLIApp) showHelp() {
    fmt.Printf("Usage: %s [command] [flags]\n\n", app.config.Command)
    fmt.Printf("Commands:\n")
    for cmdName, cmd := range app.config.Commands {
        fmt.Printf("  %s\t%s\n", cmdName, cmd.Description)
    }
    fmt.Printf("\nFlags:\n")
    for flagName, flag := range app.config.Flags {
        fmt.Printf("  -%s, --%s\t%s\n", flag.Short, flagName, flag.Description)
    }
}
```

## Advanced CLI Features

### Subcommands with Arguments

```go
// TuskLang configuration
#cli {
    command: "docker-manager"
    
    commands: {
        build: {
            description: "Build Docker images"
            arguments: ["image", "tag"]
            flags: {
                no-cache: {
                    type: "bool"
                    description: "Build without cache"
                }
                platform: {
                    type: "string"
                    description: "Target platform"
                    default: "linux/amd64"
                }
            }
        }
        
        deploy: {
            description: "Deploy containers"
            arguments: ["service"]
            flags: {
                replicas: {
                    type: "int"
                    description: "Number of replicas"
                    default: "1"
                }
                environment: {
                    type: "string"
                    description: "Environment"
                    default: "production"
                }
            }
        }
    }
}
```

### Interactive Prompts

```go
// TuskLang configuration with interactive prompts
#cli {
    command: "setup"
    
    prompts: {
        database_url: {
            type: "string"
            message: "Enter database URL"
            default: "postgresql://localhost:5432/myapp"
            validate: "url"
        }
        
        api_key: {
            type: "password"
            message: "Enter API key"
            required: true
        }
        
        environment: {
            type: "select"
            message: "Select environment"
            options: ["development", "staging", "production"]
            default: "development"
        }
    }
}
```

### Command Aliases

```go
// TuskLang configuration with aliases
#cli {
    command: "git-helper"
    
    commands: {
        commit: {
            description: "Commit changes"
            aliases: ["c", "commit-changes"]
            flags: {
                message: {
                    type: "string"
                    short: "m"
                    description: "Commit message"
                    required: true
                }
                amend: {
                    type: "bool"
                    description: "Amend previous commit"
                }
            }
        }
        
        push: {
            description: "Push to remote"
            aliases: ["p", "push-remote"]
            flags: {
                force: {
                    type: "bool"
                    short: "f"
                    description: "Force push"
                }
            }
        }
    }
}
```

## Validation and Error Handling

```go
func (app *CLIApp) validateCommand(command string, args []string) error {
    cmd, exists := app.config.Commands[command]
    if !exists {
        return fmt.Errorf("unknown command: %s", command)
    }
    
    // Validate required arguments
    if len(args) < len(cmd.Arguments) {
        return fmt.Errorf("missing required arguments for command %s", command)
    }
    
    // Validate flags
    for flagName, flag := range cmd.Flags {
        if flag.Required && app.flags[flagName] == nil {
            return fmt.Errorf("required flag --%s is missing", flagName)
        }
    }
    
    return nil
}

func (app *CLIApp) handleError(err error) {
    fmt.Fprintf(os.Stderr, "Error: %v\n", err)
    os.Exit(1)
}
```

## Performance Considerations

- **Lazy Loading**: Load command implementations only when needed
- **Caching**: Cache parsed configurations to avoid repeated parsing
- **Concurrent Execution**: Use goroutines for long-running commands
- **Memory Management**: Clean up resources after command execution

## Security Notes

- **Input Validation**: Always validate user input and command arguments
- **Permission Checks**: Verify user permissions before executing sensitive commands
- **Secure Defaults**: Use secure default values for sensitive configurations
- **Audit Logging**: Log all CLI operations for security auditing

## Best Practices

1. **Clear Command Structure**: Organize commands logically with clear hierarchies
2. **Consistent Naming**: Use consistent naming conventions across commands and flags
3. **Helpful Messages**: Provide clear error messages and usage examples
4. **Progressive Disclosure**: Show basic help by default, detailed help on request
5. **Configuration Files**: Support configuration files for complex setups
6. **Environment Integration**: Respect environment variables and system settings

## Integration Examples

### With Cobra Library

```go
import (
    "github.com/spf13/cobra"
    "github.com/tusklang/go-sdk"
)

func createCobraCommand(config tusk.Config) *cobra.Command {
    var cliDirective CLIDirective
    config.Get("#cli", &cliDirective)
    
    rootCmd := &cobra.Command{
        Use:   cliDirective.Command,
        Short: cliDirective.Description,
        Version: cliDirective.Version,
    }
    
    // Add commands from configuration
    for cmdName, cmd := range cliDirective.Commands {
        cobraCmd := &cobra.Command{
            Use:   cmdName,
            Short: cmd.Description,
            Run:   func(cmd *cobra.Command, args []string) {
                executeCommand(cmd.Action, args)
            },
        }
        rootCmd.AddCommand(cobraCmd)
    }
    
    return rootCmd
}
```

### With Viper Configuration

```go
import (
    "github.com/spf13/viper"
    "github.com/tusklang/go-sdk"
)

func loadCLIConfig() {
    // Load TuskLang configuration
    config, _ := tusk.LoadFile("cli-config.tsk")
    
    // Convert to Viper configuration
    var cliDirective CLIDirective
    config.Get("#cli", &cliDirective)
    
    // Set Viper defaults
    for flagName, flag := range cliDirective.Flags {
        viper.SetDefault(flagName, flag.Default)
    }
    
    // Bind environment variables
    viper.AutomaticEnv()
}
```

This comprehensive CLI hash directives documentation provides Go developers with everything they need to build sophisticated command-line applications using TuskLang's powerful directive system. 