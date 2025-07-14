#!/usr/bin/env bash

# TuskLang CLI for Bash - Universal Command Interface
# ===================================================
# "Strong. Secure. Scalable."
#
# This is the main entry point for the TuskLang CLI that implements
# all commands from the Universal CLI Command Specification.

set -euo pipefail

# Version information
readonly TSK_VERSION="2.0.0"
readonly TSK_TAGLINE="Strong. Secure. Scalable."

# Global variables
declare -g TSK_VERBOSE=0
declare -g TSK_QUIET=0
declare -g TSK_JSON_OUTPUT=0
declare -g TSK_CONFIG_PATH=""
declare -g TSK_EXIT_CODE=0

# Status symbols
readonly STATUS_SUCCESS="✅"
readonly STATUS_ERROR="❌"
readonly STATUS_WARNING="⚠️"
readonly STATUS_RUNNING="🔄"
readonly STATUS_INFO="📍"

# Colors for output
readonly COLOR_RESET="\033[0m"
readonly COLOR_RED="\033[31m"
readonly COLOR_GREEN="\033[32m"
readonly COLOR_YELLOW="\033[33m"
readonly COLOR_BLUE="\033[34m"
readonly COLOR_MAGENTA="\033[35m"
readonly COLOR_CYAN="\033[36m"

# Load core utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
echo "[DEBUG] SCRIPT_DIR is: $SCRIPT_DIR" 1>&2
source "${SCRIPT_DIR}/utils/helpers.sh"
source "${SCRIPT_DIR}/utils/output.sh"
source "${SCRIPT_DIR}/utils/config.sh"

# Load command modules
source "${SCRIPT_DIR}/commands/db.sh"
source "${SCRIPT_DIR}/commands/config.sh"
source "${SCRIPT_DIR}/commands/dev.sh"
source "${SCRIPT_DIR}/commands/test.sh"
source "${SCRIPT_DIR}/commands/service.sh"
source "${SCRIPT_DIR}/commands/cache.sh"
source "${SCRIPT_DIR}/commands/binary.sh"
source "${SCRIPT_DIR}/commands/ai.sh"
source "${SCRIPT_DIR}/commands/utility.sh"

##
# Show version information
#
show_version() {
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        cat << EOF
{
  "version": "$TSK_VERSION",
  "tagline": "$TSK_TAGLINE",
  "language": "bash",
  "platform": "$(uname -s)",
  "architecture": "$(uname -m)"
}
EOF
    else
        echo "🐘 TuskLang CLI v$TSK_VERSION"
        echo "$TSK_TAGLINE"
        echo "Language: Bash"
        echo "Platform: $(uname -s) $(uname -m)"
    fi
}

##
# Show help information
#
show_help() {
    local command="${1:-}"
    
    if [[ -n "$command" ]]; then
        show_command_help "$command"
        return
    fi
    
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        show_json_help
    else
        show_text_help
    fi
}

##
# Show text-based help
#
show_text_help() {
    cat << EOF
🐘 TuskLang CLI v$TSK_VERSION - $TSK_TAGLINE

Usage: tsk [global-options] <command> [command-options] [arguments]

Global Options:
  --help, -h           Show this help message
  --version, -v        Show version information
  --verbose            Enable verbose output
  --quiet, -q          Suppress non-error output
  --config <path>      Use alternate config file
  --json               Output in JSON format

Commands:

🗄️  Database Commands:
  db status                    Check database connection status
  db migrate <file>           Run migration file
  db console                  Open interactive database console
  db backup [file]            Backup database
  db restore <file>           Restore from backup file
  db init                     Initialize SQLite database

🔧 Development Commands:
  serve [port]                Start development server (default: 8080)
  compile <file>              Compile .tsk file to optimized format
  optimize <file>             Optimize .tsk file for production

🧪 Testing Commands:
  test [suite]                Run specific test suite
  test all                    Run all test suites
  test parser                 Test parser functionality only
  test fujsen                 Test FUJSEN operators only
  test sdk                    Test SDK-specific features
  test performance            Run performance benchmarks

⚙️  Service Commands:
  services start              Start all TuskLang services
  services stop               Stop all TuskLang services
  services restart            Restart all services
  services status             Show status of all services

📦 Project Commands:
  init [project-name]         Initialize new TuskLang project
  migrate --from=<format>     Migrate from other formats

🏃 Cache Commands:
  cache clear                 Clear all caches
  cache status                Show cache status and statistics
  cache warm                  Pre-warm caches
  cache memcached [subcommand] Memcached operations

🔐 License Commands:
  license check               Check current license status
  license activate <key>      Activate license with key

🥜 Configuration Commands:
  config get <key.path> [dir] Get configuration value by path
  config check [path]         Check configuration hierarchy
  config validate [path]      Validate entire configuration chain
  config compile [path]       Auto-compile all peanu.tsk files
  config docs [path]          Generate configuration documentation
  config clear-cache [path]   Clear configuration cache
  config stats                Show configuration performance statistics

�� Binary Performance Commands:
  binary compile <file.tsk>   Compile to binary format (.tskb)
  binary execute <file.tskb>  Execute binary file directly
  binary benchmark <file>     Compare binary vs text performance
  binary optimize <file>      Optimize binary for production

🥜 Peanuts Commands:
  peanuts compile <file>      Compile .peanuts to binary .pnt
  peanuts auto-compile [dir]  Auto-compile all peanuts files in directory
  peanuts load <file.pnt>     Load and display binary peanuts file

🎨 CSS Commands:
  css expand <input> [output] Expand CSS shortcodes in file
  css map                     Show all shortcode → property mappings

🤖 AI Commands:
  ai claude <prompt>          Query Claude AI with prompt
  ai chatgpt <prompt>         Query ChatGPT with prompt
  ai custom <api> <prompt>    Query custom AI API endpoint
  ai config                   Show current AI configuration
  ai setup                    Interactive AI API key setup
  ai test                     Test all configured AI connections
  ai complete <file> [line] [column] Get AI-powered auto-completion
  ai analyze <file>           Analyze file for errors and improvements
  ai optimize <file>          Get performance optimization suggestions
  ai security <file>          Scan for security vulnerabilities

🛠️  Utility Commands:
  parse <file>                Parse and display TSK file contents
  validate <file>             Validate TSK file syntax
  convert -i <input> -o <output> Convert between formats
  get <file> <key.path>       Get specific value by key path
  set <file> <key.path> <value> Set value by key path
  version                     Show version information
  help [command]              Show help for command

Examples:
  tsk db status
  tsk serve 3000
  tsk test all
  tsk config get server.port
  tsk binary compile app.tsk
  tsk ai claude "Explain TuskLang"

For more information, visit: https://tusklang.org
EOF
}

##
# Show JSON-based help
#
show_json_help() {
    cat << EOF
{
  "version": "$TSK_VERSION",
  "tagline": "$TSK_TAGLINE",
  "usage": "tsk [global-options] <command> [command-options] [arguments]",
  "global_options": {
    "help": "Show help message",
    "version": "Show version information",
    "verbose": "Enable verbose output",
    "quiet": "Suppress non-error output",
    "config": "Use alternate config file",
    "json": "Output in JSON format"
  },
  "commands": {
    "database": {
      "status": "Check database connection status",
      "migrate": "Run migration file",
      "console": "Open interactive database console",
      "backup": "Backup database",
      "restore": "Restore from backup file",
      "init": "Initialize SQLite database"
    },
    "development": {
      "serve": "Start development server",
      "compile": "Compile .tsk file",
      "optimize": "Optimize .tsk file"
    },
    "testing": {
      "test": "Run test suites",
      "all": "Run all tests",
      "parser": "Test parser only",
      "fujsen": "Test FUJSEN only",
      "sdk": "Test SDK only",
      "performance": "Performance tests"
    },
    "services": {
      "start": "Start all services",
      "stop": "Stop all services",
      "restart": "Restart services",
      "status": "Show service status"
    },
    "cache": {
      "clear": "Clear all caches",
      "status": "Show cache status",
      "warm": "Pre-warm caches",
      "memcached": "Memcached operations"
    },
    "config": {
      "get": "Get configuration value",
      "check": "Check configuration hierarchy",
      "validate": "Validate configuration",
      "compile": "Compile configurations",
      "docs": "Generate documentation",
      "clear-cache": "Clear configuration cache",
      "stats": "Show statistics"
    },
    "binary": {
      "compile": "Compile to binary format",
      "execute": "Execute binary file",
      "benchmark": "Benchmark performance",
      "optimize": "Optimize binary"
    },
    "ai": {
      "claude": "Query Claude AI",
      "chatgpt": "Query ChatGPT",
      "custom": "Query custom AI API",
      "config": "Show AI configuration",
      "setup": "Setup AI API keys",
      "test": "Test AI connections",
      "complete": "AI auto-completion",
      "analyze": "Analyze file",
      "optimize": "Optimization suggestions",
      "security": "Security scan"
    },
    "utility": {
      "parse": "Parse TSK file",
      "validate": "Validate syntax",
      "convert": "Convert formats",
      "get": "Get value by path",
      "set": "Set value by path",
      "version": "Show version",
      "help": "Show help"
    }
  }
}
EOF
}

##
# Show help for specific command
#
show_command_help() {
    local command="$1"
    
    case "$command" in
        "db"|"database")
            show_db_help
            ;;
        "serve"|"compile"|"optimize")
            show_dev_help
            ;;
        "test")
            show_test_help
            ;;
        "services")
            show_service_help
            ;;
        "cache")
            show_cache_help
            ;;
        "config")
            show_config_help
            ;;
        "binary")
            show_binary_help
            ;;
        "ai")
            show_ai_help
            ;;
        "parse"|"validate"|"convert"|"get"|"set")
            show_utility_help
            ;;
        *)
            log_error "Unknown command: $command"
            TSK_EXIT_CODE=2
            ;;
    esac
}

##
# Parse global options
#
parse_global_options() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --help|-h)
                show_help
                exit 0
                ;;
            --version|-v)
                show_version
                exit 0
                ;;
            --verbose)
                TSK_VERBOSE=1
                shift
                ;;
            --quiet|-q)
                TSK_QUIET=1
                shift
                ;;
            --config)
                TSK_CONFIG_PATH="$2"
                shift 2
                ;;
            --json)
                TSK_JSON_OUTPUT=1
                shift
                ;;
            --)
                shift
                break
                ;;
            -*)
                log_error "Unknown global option: $1"
                TSK_EXIT_CODE=2
                exit $TSK_EXIT_CODE
                ;;
            *)
                break
                ;;
        esac
    done
    
    # Return remaining arguments
    echo "$@"
}

##
# Main CLI function
#
main() {
    # Parse global options
    local args
    args=$(parse_global_options "$@")
    
    # Convert args back to array
    eval "set -- $args"
    
    # Get command
    local command="${1:-}"
    
    # If no command provided, enter interactive mode
    if [[ -z "$command" ]]; then
        interactive_mode
        return
    fi
    
    # Shift command from arguments
    shift
    
    # Execute command
    case "$command" in
        # Database commands
        "db")
            execute_db_command "$@"
            ;;
        
        # Development commands
        "serve")
            execute_serve_command "$@"
            ;;
        "compile")
            execute_compile_command "$@"
            ;;
        "optimize")
            execute_optimize_command "$@"
            ;;
        
        # Testing commands
        "test")
            execute_test_command "$@"
            ;;
        
        # Service commands
        "services")
            execute_service_command "$@"
            ;;
        
        # Cache commands
        "cache")
            execute_cache_command "$@"
            ;;
        
        # Configuration commands
        "config")
            execute_config_command "$@"
            ;;
        
        # Binary commands
        "binary")
            execute_binary_command "$@"
            ;;
        
        # AI commands
        "ai")
            execute_ai_command "$@"
            ;;
        
        # Utility commands
        "parse")
            execute_parse_command "$@"
            ;;
        "validate")
            execute_validate_command "$@"
            ;;
        "convert")
            execute_convert_command "$@"
            ;;
        "get")
            execute_get_command "$@"
            ;;
        "set")
            execute_set_command "$@"
            ;;
        "version")
            show_version
            ;;
        "help")
            show_help "$@"
            ;;
        
        # Unknown command
        *)
            log_error "Unknown command: $command"
            log_info "Run 'tsk help' for available commands"
            TSK_EXIT_CODE=2
            ;;
    esac
    
    exit $TSK_EXIT_CODE
}

##
# Interactive REPL mode
#
interactive_mode() {
    echo "🐘 TuskLang v$TSK_VERSION - Interactive Mode"
    echo "$TSK_TAGLINE"
    echo "Type 'exit' to quit, 'help' for commands"
    echo
    
    while true; do
        echo -n "tsk> "
        read -r input
        
        if [[ -z "$input" ]]; then
            continue
        fi
        
        if [[ "$input" == "exit" || "$input" == "quit" ]]; then
            echo "Goodbye!"
            break
        fi
        
        if [[ "$input" == "help" ]]; then
            show_help
            continue
        fi
        
        # Execute the command
        eval "main $input"
    done
}

# Run main function if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi 