# CLI Directives in TuskLang - Bash Guide

## 🖥️ **Revolutionary Command Line Configuration**

CLI directives in TuskLang transform your configuration files into intelligent command-line interfaces. No more separate CLI frameworks or complex argument parsing - everything lives in your TuskLang configuration with dynamic commands, automatic help generation, and intelligent subcommand handling.

> **"We don't bow to any king"** - TuskLang CLI directives break free from traditional CLI framework constraints and bring modern command-line capabilities to your Bash applications.

## 🚀 **Core CLI Directives**

### **Basic CLI Setup**
```bash
#cli: --help                  # Enable CLI mode
#cli-name: tusk               # CLI application name
#cli-version: 1.0.0           # CLI version
#cli-description: TuskLang CLI # CLI description
#cli-author: TuskLang Team    # CLI author
#cli-usage: tusk [command]    # CLI usage pattern
```

### **Advanced CLI Configuration**
```bash
#cli-color: true              # Enable colored output
#cli-interactive: true        # Enable interactive mode
#cli-completion: bash         # Enable shell completion
#cli-logging: true            # Enable command logging
#cli-history: true            # Enable command history
#cli-config: ~/.tusk/config   # CLI configuration file
```

## 🔧 **Bash CLI Implementation**

### **Basic CLI Router**
```bash
#!/bin/bash

# Load CLI configuration
source <(tsk load cli.tsk)

# CLI configuration
CLI_NAME="${cli_name:-tusk}"
CLI_VERSION="${cli_version:-1.0.0}"
CLI_DESCRIPTION="${cli_description:-TuskLang CLI}"
CLI_COLOR="${cli_color:-true}"
CLI_INTERACTIVE="${cli_interactive:-false}"

# CLI router
main() {
    # Parse command line arguments
    local args=("$@")
    local command="${args[0]}"
    local subcommand="${args[1]}"
    local options=("${args[@]:2}")
    
    # Handle special commands
    case "$command" in
        "--help"|"-h"|"help")
            show_help
            ;;
        "--version"|"-v"|"version")
            show_version
            ;;
        "--config"|"-c")
            show_config
            ;;
        "")
            if [[ "$CLI_INTERACTIVE" == "true" ]]; then
                start_interactive_mode
            else
                show_help
            fi
            ;;
        *)
            # Route to command handler
            route_command "$command" "$subcommand" "${options[@]}"
            ;;
    esac
}

# Show CLI help
show_help() {
    cat << EOF
$CLI_NAME - $CLI_DESCRIPTION

Usage: $CLI_NAME [command] [subcommand] [options]

Commands:
  help, --help, -h           Show this help message
  version, --version, -v     Show version information
  config, --config, -c       Show configuration
  init                       Initialize new TuskLang project
  build                      Build TuskLang project
  deploy                     Deploy TuskLang project
  test                       Run TuskLang tests
  validate                   Validate TuskLang configuration
  serve                      Start development server
  clean                      Clean build artifacts
  update                     Update TuskLang installation

Examples:
  $CLI_NAME init my-project
  $CLI_NAME build --production
  $CLI_NAME deploy --environment staging
  $CLI_NAME test --coverage

For more information, visit: https://tusklang.org
EOF
}

# Show CLI version
show_version() {
    echo "$CLI_NAME version $CLI_VERSION"
    echo "TuskLang - Configuration with a Heartbeat"
    echo "Built with ❤️ by the TuskLang Team"
}

# Show CLI configuration
show_config() {
    echo "CLI Configuration:"
    echo "  Name: $CLI_NAME"
    echo "  Version: $CLI_VERSION"
    echo "  Description: $CLI_DESCRIPTION"
    echo "  Color: $CLI_COLOR"
    echo "  Interactive: $CLI_INTERACTIVE"
    echo "  Config file: ${cli_config:-~/.tusk/config}"
}

# Route commands
route_command() {
    local command="$1"
    local subcommand="$2"
    shift 2
    local options=("$@")
    
    case "$command" in
        "init")
            handle_init "$subcommand" "${options[@]}"
            ;;
        "build")
            handle_build "$subcommand" "${options[@]}"
            ;;
        "deploy")
            handle_deploy "$subcommand" "${options[@]}"
            ;;
        "test")
            handle_test "$subcommand" "${options[@]}"
            ;;
        "validate")
            handle_validate "$subcommand" "${options[@]}"
            ;;
        "serve")
            handle_serve "$subcommand" "${options[@]}"
            ;;
        "clean")
            handle_clean "$subcommand" "${options[@]}"
            ;;
        "update")
            handle_update "$subcommand" "${options[@]}"
            ;;
        *)
            echo "Unknown command: $command"
            echo "Run '$CLI_NAME --help' for available commands"
            exit 1
            ;;
    esac
}
```

### **Command Handlers**
```bash
#!/bin/bash

# Init command handler
handle_init() {
    local subcommand="$1"
    shift
    local options=("$@")
    
    local project_name=""
    local template="default"
    local force=false
    
    # Parse options
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --template|-t)
                template="$2"
                shift 2
                ;;
            --force|-f)
                force=true
                shift
                ;;
            --help|-h)
                show_init_help
                return 0
                ;;
            *)
                if [[ -z "$project_name" ]]; then
                    project_name="$1"
                else
                    echo "Unknown option: $1"
                    show_init_help
                    return 1
                fi
                shift
                ;;
        esac
    done
    
    # Validate project name
    if [[ -z "$project_name" ]]; then
        echo "Error: Project name is required"
        show_init_help
        return 1
    fi
    
    # Initialize project
    init_project "$project_name" "$template" "$force"
}

show_init_help() {
    cat << EOF
Usage: $CLI_NAME init <project-name> [options]

Initialize a new TuskLang project.

Arguments:
  project-name              Name of the project to create

Options:
  -t, --template <name>     Template to use (default: default)
  -f, --force              Force creation even if directory exists
  -h, --help               Show this help message

Templates:
  default                   Basic TuskLang project
  web                       Web application template
  api                       API application template
  cli                       CLI application template
  microservice              Microservice template

Examples:
  $CLI_NAME init my-project
  $CLI_NAME init web-app --template web
  $CLI_NAME init api-service --template api --force
EOF
}

init_project() {
    local project_name="$1"
    local template="$2"
    local force="$3"
    
    echo "Initializing TuskLang project: $project_name"
    echo "Template: $template"
    
    # Check if directory exists
    if [[ -d "$project_name" ]] && [[ "$force" != "true" ]]; then
        echo "Error: Directory '$project_name' already exists"
        echo "Use --force to overwrite"
        return 1
    fi
    
    # Create project directory
    mkdir -p "$project_name"
    cd "$project_name"
    
    # Create project structure based on template
    case "$template" in
        "default")
            create_default_project
            ;;
        "web")
            create_web_project
            ;;
        "api")
            create_api_project
            ;;
        "cli")
            create_cli_project
            ;;
        "microservice")
            create_microservice_project
            ;;
        *)
            echo "Unknown template: $template"
            echo "Using default template"
            create_default_project
            ;;
    esac
    
    echo "Project '$project_name' initialized successfully!"
    echo "Next steps:"
    echo "  cd $project_name"
    echo "  $CLI_NAME serve"
}

create_default_project() {
    # Create basic project structure
    mkdir -p {src,config,tests,docs}
    
    # Create main configuration file
    cat > peanu.tsk << 'EOF'
# TuskLang Project Configuration
project_name: "My TuskLang Project"
version: "1.0.0"
description: "A TuskLang project"

# Database configuration
database:
  type: sqlite
  path: ./data/project.db

# Server configuration
server:
  port: 8080
  host: 0.0.0.0

# Logging configuration
logging:
  level: info
  file: ./logs/app.log
EOF

    # Create main script
    cat > main.sh << 'EOF'
#!/bin/bash

# Load TuskLang configuration
source <(tsk load peanu.tsk)

echo "Hello from TuskLang!"
echo "Project: $project_name"
echo "Version: $version"
EOF

    chmod +x main.sh
    
    # Create README
    cat > README.md << 'EOF'
# My TuskLang Project

A TuskLang project with configuration-driven development.

## Getting Started

1. Run the project:
   ```bash
   ./main.sh
   ```

2. Start development server:
   ```bash
   tusk serve
   ```

3. Run tests:
   ```bash
   tusk test
   ```

## Configuration

Edit `peanu.tsk` to customize your project configuration.
EOF
}

# Build command handler
handle_build() {
    local subcommand="$1"
    shift
    local options=("$@")
    
    local environment="development"
    local output_dir="./dist"
    local clean=false
    local optimize=false
    
    # Parse options
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --environment|-e)
                environment="$2"
                shift 2
                ;;
            --output|-o)
                output_dir="$2"
                shift 2
                ;;
            --clean|-c)
                clean=true
                shift
                ;;
            --optimize|-O)
                optimize=true
                shift
                ;;
            --help|-h)
                show_build_help
                return 0
                ;;
            *)
                echo "Unknown option: $1"
                show_build_help
                return 1
                ;;
        esac
    done
    
    # Build project
    build_project "$environment" "$output_dir" "$clean" "$optimize"
}

show_build_help() {
    cat << EOF
Usage: $CLI_NAME build [options]

Build TuskLang project for deployment.

Options:
  -e, --environment <env>   Build environment (default: development)
  -o, --output <dir>        Output directory (default: ./dist)
  -c, --clean              Clean output directory before building
  -O, --optimize           Enable optimizations
  -h, --help               Show this help message

Environments:
  development               Development build
  staging                   Staging build
  production                Production build

Examples:
  $CLI_NAME build
  $CLI_NAME build --environment production --optimize
  $CLI_NAME build --output ./build --clean
EOF
}

build_project() {
    local environment="$1"
    local output_dir="$2"
    local clean="$3"
    local optimize="$4"
    
    echo "Building TuskLang project for $environment environment"
    echo "Output directory: $output_dir"
    
    # Load project configuration
    if [[ ! -f "peanu.tsk" ]]; then
        echo "Error: No peanu.tsk configuration file found"
        echo "Run '$CLI_NAME init' to create a new project"
        return 1
    fi
    
    source <(tsk load peanu.tsk)
    
    # Clean output directory if requested
    if [[ "$clean" == "true" ]]; then
        echo "Cleaning output directory..."
        rm -rf "$output_dir"
    fi
    
    # Create output directory
    mkdir -p "$output_dir"
    
    # Copy project files
    echo "Copying project files..."
    cp -r src/* "$output_dir/" 2>/dev/null || true
    cp peanu.tsk "$output_dir/"
    cp main.sh "$output_dir/"
    
    # Apply environment-specific configuration
    echo "Applying $environment configuration..."
    apply_environment_config "$environment" "$output_dir"
    
    # Apply optimizations if requested
    if [[ "$optimize" == "true" ]]; then
        echo "Applying optimizations..."
        optimize_build "$output_dir"
    fi
    
    # Create build manifest
    create_build_manifest "$output_dir" "$environment"
    
    echo "Build completed successfully!"
    echo "Output: $output_dir"
}

apply_environment_config() {
    local environment="$1"
    local output_dir="$2"
    
    # Load environment-specific configuration
    local env_config="config/$environment.tsk"
    if [[ -f "$env_config" ]]; then
        echo "Loading environment config: $env_config"
        source <(tsk load "$env_config")
    fi
    
    # Update configuration based on environment
    case "$environment" in
        "production")
            # Production optimizations
            sed -i 's/logging_level: info/logging_level: warn/' "$output_dir/peanu.tsk"
            ;;
        "staging")
            # Staging configuration
            sed -i 's/logging_level: info/logging_level: debug/' "$output_dir/peanu.tsk"
            ;;
    esac
}

optimize_build() {
    local output_dir="$1"
    
    # Optimize configuration files
    echo "Optimizing configuration files..."
    
    # Remove comments and empty lines
    sed -i '/^#/d; /^$/d' "$output_dir/peanu.tsk"
    
    # Compress if possible
    if command -v gzip >/dev/null 2>&1; then
        echo "Compressing build artifacts..."
        find "$output_dir" -name "*.tsk" -exec gzip -9 {} \;
    fi
}

create_build_manifest() {
    local output_dir="$1"
    local environment="$2"
    
    cat > "$output_dir/build.json" << EOF
{
    "build_time": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "environment": "$environment",
    "version": "$CLI_VERSION",
    "files": [
        "$(find "$output_dir" -type f | sed "s|$output_dir/||" | tr '\n' '", "' | sed 's/, "$//')"
    ]
}
EOF
}
```

### **Interactive Mode**
```bash
#!/bin/bash

# Interactive CLI mode
start_interactive_mode() {
    echo "Welcome to $CLI_NAME interactive mode!"
    echo "Type 'help' for available commands, 'exit' to quit."
    echo ""
    
    # Load CLI history if enabled
    if [[ "$cli_history" == "true" ]]; then
        load_cli_history
    fi
    
    # Main interactive loop
    while true; do
        # Show prompt
        show_prompt
        
        # Read command
        read -e -p "$PROMPT" command_line
        
        # Handle empty input
        if [[ -z "$command_line" ]]; then
            continue
        fi
        
        # Save to history
        if [[ "$cli_history" == "true" ]]; then
            save_to_history "$command_line"
        fi
        
        # Parse and execute command
        parse_and_execute "$command_line"
    done
}

show_prompt() {
    local current_dir=$(basename "$PWD")
    local git_branch=$(git branch --show-current 2>/dev/null || echo "")
    
    if [[ -n "$git_branch" ]]; then
        PROMPT="$CLI_NAME:$current_dir:$git_branch> "
    else
        PROMPT="$CLI_NAME:$current_dir> "
    fi
    
    # Add color if enabled
    if [[ "$CLI_COLOR" == "true" ]]; then
        PROMPT="\033[1;32m$PROMPT\033[0m"
    fi
}

parse_and_execute() {
    local command_line="$1"
    
    # Split command line into arguments
    read -ra args <<< "$command_line"
    local command="${args[0]}"
    
    # Handle special commands
    case "$command" in
        "exit"|"quit")
            echo "Goodbye!"
            exit 0
            ;;
        "clear")
            clear
            ;;
        "pwd")
            pwd
            ;;
        "ls"|"dir")
            ls -la
            ;;
        "cd")
            cd "${args[1]:-~}"
            ;;
        "help")
            show_interactive_help
            ;;
        "history")
            show_history
            ;;
        *)
            # Route to command handler
            route_command "${args[@]}"
            ;;
    esac
}

show_interactive_help() {
    cat << EOF
Interactive Mode Commands:

Built-in Commands:
  help                      Show this help message
  exit, quit               Exit interactive mode
  clear                    Clear screen
  pwd                      Show current directory
  ls, dir                  List files
  cd <dir>                 Change directory
  history                  Show command history

TuskLang Commands:
  init <project>           Initialize new project
  build [options]          Build project
  deploy [options]         Deploy project
  test [options]           Run tests
  validate [options]       Validate configuration
  serve [options]          Start development server
  clean [options]          Clean build artifacts
  update [options]         Update installation

Examples:
  init my-project
  build --production
  deploy --environment staging
  test --coverage
EOF
}

# History management
load_cli_history() {
    local history_file="${cli_config:-~/.tusk}/history"
    if [[ -f "$history_file" ]]; then
        export HISTFILE="$history_file"
        export HISTSIZE=1000
        export HISTFILESIZE=2000
        history -r
    fi
}

save_to_history() {
    local command="$1"
    local history_file="${cli_config:-~/.tusk}/history"
    
    # Create history directory if it doesn't exist
    mkdir -p "$(dirname "$history_file")"
    
    # Append command to history file
    echo "$(date '+%Y-%m-%d %H:%M:%S') $command" >> "$history_file"
}

show_history() {
    local history_file="${cli_config:-~/.tusk}/history"
    if [[ -f "$history_file" ]]; then
        echo "Command History:"
        tail -20 "$history_file" | while read -r line; do
            echo "  $line"
        done
    else
        echo "No command history found"
    fi
}
```

## 🎨 **Colored Output**

### **Color Management**
```bash
#!/bin/bash

# Color management
setup_colors() {
    if [[ "$CLI_COLOR" == "true" ]]; then
        # Define colors
        export RED='\033[0;31m'
        export GREEN='\033[0;32m'
        export YELLOW='\033[1;33m'
        export BLUE='\033[0;34m'
        export PURPLE='\033[0;35m'
        export CYAN='\033[0;36m'
        export WHITE='\033[1;37m'
        export NC='\033[0m' # No Color
        
        # Define color functions
        color_red() { echo -e "${RED}$1${NC}"; }
        color_green() { echo -e "${GREEN}$1${NC}"; }
        color_yellow() { echo -e "${YELLOW}$1${NC}"; }
        color_blue() { echo -e "${BLUE}$1${NC}"; }
        color_purple() { echo -e "${PURPLE}$1${NC}"; }
        color_cyan() { echo -e "${CYAN}$1${NC}"; }
        color_white() { echo -e "${WHITE}$1${NC}"; }
    else
        # No color functions
        color_red() { echo "$1"; }
        color_green() { echo "$1"; }
        color_yellow() { echo "$1"; }
        color_blue() { echo "$1"; }
        color_purple() { echo "$1"; }
        color_cyan() { echo "$1"; }
        color_white() { echo "$1"; }
    fi
}

# Success and error messages
show_success() {
    color_green "✓ $1"
}

show_error() {
    color_red "✗ $1"
}

show_warning() {
    color_yellow "⚠ $1"
}

show_info() {
    color_blue "ℹ $1"
}
```

## 🔄 **Shell Completion**

### **Bash Completion**
```bash
#!/bin/bash

# Generate bash completion
generate_bash_completion() {
    cat << 'EOF'
# TuskLang CLI bash completion
_tusk_completion() {
    local cur prev opts
    COMPREPLY=()
    cur="${COMP_WORDS[COMP_CWORD]}"
    prev="${COMP_WORDS[COMP_CWORD-1]}"
    
    # Main commands
    local commands="init build deploy test validate serve clean update help version config"
    
    # Subcommands for each main command
    case "$prev" in
        init)
            local init_opts="--template --force --help"
            COMPREPLY=( $(compgen -W "$init_opts" -- "$cur") )
            ;;
        build)
            local build_opts="--environment --output --clean --optimize --help"
            COMPREPLY=( $(compgen -W "$build_opts" -- "$cur") )
            ;;
        deploy)
            local deploy_opts="--environment --target --dry-run --help"
            COMPREPLY=( $(compgen -W "$deploy_opts" -- "$cur") )
            ;;
        test)
            local test_opts="--coverage --verbose --help"
            COMPREPLY=( $(compgen -W "$test_opts" -- "$cur") )
            ;;
        serve)
            local serve_opts="--port --host --watch --help"
            COMPREPLY=( $(compgen -W "$serve_opts" -- "$cur") )
            ;;
        *)
            COMPREPLY=( $(compgen -W "$commands" -- "$cur") )
            ;;
    esac
}

complete -F _tusk_completion tusk
EOF
}

# Install bash completion
install_bash_completion() {
    local completion_script=$(generate_bash_completion)
    local completion_file="/etc/bash_completion.d/tusk"
    
    echo "Installing bash completion..."
    echo "$completion_script" | sudo tee "$completion_file" >/dev/null
    
    if [[ $? -eq 0 ]]; then
        show_success "Bash completion installed successfully"
        echo "Restart your shell or run 'source $completion_file' to enable"
    else
        show_error "Failed to install bash completion"
        return 1
    fi
}
```

## 📊 **Command Logging**

### **Logging System**
```bash
#!/bin/bash

# Load logging configuration
source <(tsk load logging.tsk)

# CLI logging
setup_cli_logging() {
    if [[ "$cli_logging" == "true" ]]; then
        local log_file="${cli_log_file:-~/.tusk/cli.log}"
        local log_level="${cli_log_level:-info}"
        
        # Create log directory
        mkdir -p "$(dirname "$log_file")"
        
        export CLI_LOG_FILE="$log_file"
        export CLI_LOG_LEVEL="$log_level"
        
        echo "CLI logging enabled: $log_file (level: $log_level)"
    fi
}

log_cli_command() {
    local command="$1"
    local args="$2"
    local exit_code="$3"
    
    if [[ "$cli_logging" == "true" ]]; then
        local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
        local user=$(whoami)
        local hostname=$(hostname)
        local pwd=$(pwd)
        
        echo "$timestamp [$user@$hostname] $pwd: $command $args (exit: $exit_code)" >> "$CLI_LOG_FILE"
    fi
}

# Log all commands
log_command_wrapper() {
    local command="$1"
    shift
    local args="$*"
    
    # Execute command
    "$command" "$@"
    local exit_code=$?
    
    # Log command
    log_cli_command "$command" "$args" "$exit_code"
    
    return $exit_code
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete CLI Configuration**
```bash
# cli-config.tsk
cli_name: "tusk"
cli_version: "1.0.0"
cli_description: "TuskLang Command Line Interface"
cli_author: "TuskLang Team"

#cli: --help
#cli-name: tusk
#cli-version: 1.0.0
#cli-description: TuskLang CLI
#cli-author: TuskLang Team
#cli-usage: tusk [command]

#cli-color: true
#cli-interactive: true
#cli-completion: bash
#cli-logging: true
#cli-history: true
#cli-config: ~/.tusk/config

#cli-commands:
#  - name: init
#    description: Initialize new TuskLang project
#    usage: init <project-name> [options]
#    options:
#      - name: --template
#        short: -t
#        description: Template to use
#        default: default
#      - name: --force
#        short: -f
#        description: Force creation
#        type: boolean
#  - name: build
#    description: Build TuskLang project
#    usage: build [options]
#    options:
#      - name: --environment
#        short: -e
#        description: Build environment
#        default: development
#      - name: --output
#        short: -o
#        description: Output directory
#        default: ./dist
#      - name: --clean
#        short: -c
#        description: Clean output directory
#        type: boolean
#      - name: --optimize
#        short: -O
#        description: Enable optimizations
#        type: boolean
```

### **Development Tools CLI**
```bash
# dev-tools.tsk
tools_name: "TuskLang Dev Tools"
tools_version: "2.0.0"

#cli: --help
#cli-name: tusk-dev
#cli-version: 2.0.0
#cli-description: TuskLang Development Tools
#cli-interactive: true
#cli-color: true

#cli-commands:
#  - name: watch
#    description: Watch files for changes
#    usage: watch [options]
#    options:
#      - name: --pattern
#        short: -p
#        description: File pattern to watch
#        default: "*.tsk"
#      - name: --command
#        short: -c
#        description: Command to run on changes
#        default: "tusk build"
#  - name: debug
#    description: Start debug mode
#    usage: debug [options]
#    options:
#      - name: --port
#        short: -p
#        description: Debug port
#        default: 9229
#      - name: --break
#        short: -b
#        description: Break on first line
#        type: boolean
#  - name: profile
#    description: Profile performance
#    usage: profile [options]
#    options:
#      - name: --duration
#        short: -d
#        description: Profile duration (seconds)
#        default: 30
#      - name: --output
#        short: -o
#        description: Output file
#        default: "profile.json"
```

## 🚨 **Troubleshooting CLI Directives**

### **Common Issues and Solutions**

**1. Command Not Found**
```bash
# Debug command routing
debug_command_routing() {
    local command="$1"
    local subcommand="$2"
    shift 2
    local options=("$@")
    
    echo "Debugging command routing..."
    echo "Command: $command"
    echo "Subcommand: $subcommand"
    echo "Options: ${options[*]}"
    
    # Check if command exists
    case "$command" in
        "init"|"build"|"deploy"|"test"|"validate"|"serve"|"clean"|"update")
            echo "✓ Command '$command' is valid"
            ;;
        *)
            echo "✗ Unknown command: $command"
            echo "Available commands: init, build, deploy, test, validate, serve, clean, update"
            ;;
    esac
}
```

**2. Option Parsing Issues**
```bash
# Debug option parsing
debug_option_parsing() {
    local options=("$@")
    
    echo "Debugging option parsing..."
    echo "Raw options: ${options[*]}"
    
    for option in "${options[@]}"; do
        case "$option" in
            --*)
                echo "Long option: $option"
                ;;
            -*)
                echo "Short option: $option"
                ;;
            *)
                echo "Argument: $option"
                ;;
        esac
    done
}
```

**3. Interactive Mode Issues**
```bash
# Debug interactive mode
debug_interactive_mode() {
    echo "Debugging interactive mode..."
    echo "CLI_INTERACTIVE: $CLI_INTERACTIVE"
    echo "CLI_COLOR: $CLI_COLOR"
    echo "CLI_LOG_FILE: ${CLI_LOG_FILE:-not set}"
    echo "CLI_LOG_LEVEL: ${CLI_LOG_LEVEL:-not set}"
    
    # Check readline support
    if command -v readline >/dev/null 2>&1; then
        echo "✓ Readline support available"
    else
        echo "⚠ Readline support not available"
    fi
    
    # Check history file
    local history_file="${cli_config:-~/.tusk}/history"
    if [[ -f "$history_file" ]]; then
        echo "✓ History file exists: $history_file"
        echo "History entries: $(wc -l < "$history_file")"
    else
        echo "⚠ History file not found: $history_file"
    fi
}
```

## 🔒 **Security Best Practices**

### **CLI Security Checklist**
```bash
# Security validation
validate_cli_security() {
    echo "Validating CLI security configuration..."
    
    # Check configuration file permissions
    local config_file="${cli_config:-~/.tusk/config}"
    if [[ -f "$config_file" ]]; then
        local perms=$(stat -c %a "$config_file")
        if [[ "$perms" == "600" ]]; then
            show_success "Configuration file permissions: $perms"
        else
            show_warning "Configuration file permissions should be 600, got: $perms"
        fi
    fi
    
    # Check history file permissions
    local history_file="${cli_config:-~/.tusk}/history"
    if [[ -f "$history_file" ]]; then
        local perms=$(stat -c %a "$history_file")
        if [[ "$perms" == "600" ]]; then
            show_success "History file permissions: $perms"
        else
            show_warning "History file permissions should be 600, got: $perms"
        fi
    fi
    
    # Check log file permissions
    if [[ -n "$CLI_LOG_FILE" ]] && [[ -f "$CLI_LOG_FILE" ]]; then
        local perms=$(stat -c %a "$CLI_LOG_FILE")
        if [[ "$perms" == "600" ]]; then
            show_success "Log file permissions: $perms"
        else
            show_warning "Log file permissions should be 600, got: $perms"
        fi
    fi
}
```

## 📈 **Performance Optimization Tips**

### **CLI Performance Checklist**
```bash
# Performance validation
validate_cli_performance() {
    echo "Validating CLI performance configuration..."
    
    # Check command completion
    if [[ "$cli_completion" == "bash" ]]; then
        echo "✓ Bash completion enabled"
    else
        echo "⚠ Command completion not configured"
    fi
    
    # Check logging
    if [[ "$cli_logging" == "true" ]]; then
        echo "✓ Command logging enabled"
    else
        echo "⚠ Command logging not enabled"
    fi
    
    # Check history
    if [[ "$cli_history" == "true" ]]; then
        echo "✓ Command history enabled"
    else
        echo "⚠ Command history not enabled"
    fi
    
    # Check interactive mode
    if [[ "$CLI_INTERACTIVE" == "true" ]]; then
        echo "✓ Interactive mode enabled"
    else
        echo "⚠ Interactive mode not enabled"
    fi
}
```

## 🎯 **Next Steps**

- **Cron Directives**: Learn about scheduled task directives
- **Middleware Integration**: Explore CLI middleware
- **Plugin System**: Understand CLI plugins
- **Testing CLI Directives**: Test CLI functionality
- **Performance Tuning**: Optimize CLI performance

---

**CLI directives transform your TuskLang configuration into a powerful command-line interface. They bring modern CLI capabilities to your Bash applications with intelligent command routing, interactive mode, and comprehensive help system!** 