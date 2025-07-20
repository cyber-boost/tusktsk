#!/bin/bash

# TuskLang Git Workflow Script - Enhanced Version (g.sh)
# Easy repo updates with intelligent prompts and learning capabilities

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
REPO_NAME="tusklang"
PRIVATE_REPO_URL="https://github.com/cyber-boost/tusktsk"
CONFIG_FILE="$HOME/.g_config"
HISTORY_FILE="$HOME/.g_history"

# Language definitions with their directories and files
declare -A LANGUAGES=(
    ["php"]="PHP"
    ["javascript"]="JavaScript" 
    ["python"]="Python"
    ["bash"]="Bash"
    ["csharp"]="C#"
    ["go"]="Go"
    ["rust"]="Rust"
    ["ruby"]="Ruby"
    ["java"]="Java"
)

# Common directories that might need attention
COMMON_DIRS=(
    "summaries"
    "legal"
    "svg"
    "icons"
    "how-to-tusk"
    "tsk_examples"
    "fujsen"
    "templates"
    "plugins"
    "tests"
    "bin"
    "reference"
)

# Function to print colored output
print_status() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

print_prompt() {
    echo -e "${CYAN}?${NC} $1"
}

# Function to load configuration
load_config() {
    if [ -f "$CONFIG_FILE" ]; then
        source "$CONFIG_FILE"
    fi
}

# Function to save configuration
save_config() {
    cat > "$CONFIG_FILE" << EOF
# g.sh Configuration File
# Auto-generated - do not edit manually

# Learning preferences
ALWAYS_CHECK_GITIGNORE=${ALWAYS_CHECK_GITIGNORE:-true}
ALWAYS_INCLUDE_SUMMARIES=${ALWAYS_INCLUDE_SUMMARIES:-true}
DEFAULT_COMMIT_TYPE=${DEFAULT_COMMIT_TYPE:-feat}
AUTO_PUSH=${AUTO_PUSH:-true}

# Recent selections (for learning)
RECENT_DIRS=${RECENT_DIRS:-""}
RECENT_FILES=${RECENT_FILES:-""}
EOF
}

# Function to save to history
save_history() {
    local action="$1"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo "[$timestamp] $action" >> "$HISTORY_FILE"
}

# Function to get user preference with learning
get_user_preference() {
    local question="$1"
    local config_key="$2"
    local default="${3:-y}"
    
    # Check if we have a saved preference
    if [ -f "$CONFIG_FILE" ] && grep -q "^$config_key=" "$CONFIG_FILE"; then
        local saved_value=$(grep "^$config_key=" "$CONFIG_FILE" | cut -d'=' -f2)
        if [ "$saved_value" = "true" ]; then
            print_info "Using saved preference: $question (y/n)? [y] y"
            return 0
        else
            print_info "Using saved preference: $question (y/n)? [n] n"
            return 1
        fi
    fi
    
    # Ask user and save preference
    print_prompt "$question (y/n)? [$default]"
    read -r response
    response=${response:-$default}
    
    if [[ "$response" =~ ^[Yy]$ ]]; then
        echo "$config_key=true" >> "$CONFIG_FILE"
        return 0
    else
        echo "$config_key=false" >> "$CONFIG_FILE"
        return 1
    fi
}

# Function to check if git repository exists
check_git_repo() {
    if [ ! -d ".git" ]; then
        print_error "Not a git repository. Run 'git init' first or use gitTrick.sh initial"
        exit 1
    fi
}

# Function to get staged files
get_staged_files() {
    git diff --cached --name-only 2>/dev/null || echo ""
}

# Function to get unstaged changes
get_unstaged_changes() {
    git diff --name-only 2>/dev/null || echo ""
}

# Function to check .gitignore suggestions
check_gitignore_suggestions() {
    local files_to_check="$1"
    local suggestions=()
    
    for file in $files_to_check; do
        if [[ "$file" =~ \.(log|tmp|cache|tar\.gz|zip|rar|7z)$ ]]; then
            suggestions+=("$file")
        elif [[ "$file" =~ (node_modules|vendor|__pycache__|build|dist)/ ]]; then
            suggestions+=("$file")
        fi
    done
    
    if [ ${#suggestions[@]} -gt 0 ]; then
        print_warning "Consider adding to .gitignore:"
        printf '%s\n' "${suggestions[@]}" | sed 's/^/  - /'
        
        if get_user_preference "Add these files to .gitignore" "ALWAYS_CHECK_GITIGNORE"; then
            for file in "${suggestions[@]}"; do
                echo "$file" >> .gitignore
                print_status "Added $file to .gitignore"
            done
        fi
    fi
}

# Function to find summary file
find_summary_file() {
    local filename="$1"
    local search_paths=("summaries/$filename" "summaries/$filename.md" "$filename" "$filename.md")
    
    for path in "${search_paths[@]}"; do
        if [ -f "$path" ]; then
            echo "$path"
            return 0
        fi
    done
    
    return 1
}

# Function to extract commit message from summary file
extract_commit_message() {
    local file_path="$1"
    
    if [ ! -f "$file_path" ]; then
        return 1
    fi
    
    # Try to extract title from markdown
    local title=$(head -n 20 "$file_path" | grep -E "^# " | head -n 1 | sed 's/^# //')
    
    if [ -n "$title" ]; then
        echo "$title"
    else
        # Fallback to filename
        basename "$file_path" .md
    fi
}

# Function to interactive file selection
interactive_file_selection() {
    local files=("$@")
    local selected=()
    
    if [ ${#files[@]} -eq 0 ]; then
        print_warning "No files to select"
        return
    fi
    
    print_info "Select files to commit:"
    for i in "${!files[@]}"; do
        echo "  $((i+1)). ${files[$i]}"
    done
    echo "  a. All files"
    echo "  n. None"
    
    print_prompt "Enter selection (comma-separated numbers, 'a' for all, 'n' for none):"
    read -r selection
    
    if [[ "$selection" =~ ^[Aa]$ ]]; then
        selected=("${files[@]}")
    elif [[ "$selection" =~ ^[Nn]$ ]]; then
        return
    else
        IFS=',' read -ra choices <<< "$selection"
        for choice in "${choices[@]}"; do
            choice=$(echo "$choice" | tr -d ' ')
            if [[ "$choice" =~ ^[0-9]+$ ]] && [ "$choice" -ge 1 ] && [ "$choice" -le ${#files[@]} ]; then
                selected+=("${files[$((choice-1))]}")
            fi
        done
    fi
    
    # Add selected files
    for file in "${selected[@]}"; do
        git add "$file"
        print_status "Added $file"
    done
}

# Function to interactive directory selection
interactive_directory_selection() {
    local dirs=("$@")
    local selected=()
    
    if [ ${#dirs[@]} -eq 0 ]; then
        print_warning "No directories to select"
        return
    fi
    
    print_info "Select directories to commit:"
    for i in "${!dirs[@]}"; do
        if [ -d "${dirs[$i]}" ]; then
            local file_count=$(find "${dirs[$i]}" -type f | wc -l)
            echo "  $((i+1)). ${dirs[$i]} ($file_count files)"
        fi
    done
    echo "  a. All directories"
    echo "  n. None"
    
    print_prompt "Enter selection (comma-separated numbers, 'a' for all, 'n' for none):"
    read -r selection
    
    if [[ "$selection" =~ ^[Aa]$ ]]; then
        selected=("${dirs[@]}")
    elif [[ "$selection" =~ ^[Nn]$ ]]; then
        return
    else
        IFS=',' read -ra choices <<< "$selection"
        for choice in "${choices[@]}"; do
            choice=$(echo "$choice" | tr -d ' ')
            if [[ "$choice" =~ ^[0-9]+$ ]] && [ "$choice" -ge 1 ] && [ "$choice" -le ${#dirs[@]} ]; then
                selected+=("${dirs[$((choice-1))]}")
            fi
        done
    fi
    
    # Add selected directories
    for dir in "${selected[@]}"; do
        if [ -d "$dir" ]; then
            git add "$dir/"
            print_status "Added $dir/ directory"
        fi
    done
}

# Function to commit with summary file
commit_with_summary() {
    local summary_file="$1"
    local commit_type="${2:-feat}"
    
    if [ ! -f "$summary_file" ]; then
        print_error "Summary file not found: $summary_file"
        return 1
    fi
    
    local commit_message=$(extract_commit_message "$summary_file")
    if [ -z "$commit_message" ]; then
        commit_message=$(basename "$summary_file" .md)
    fi
    
    # Check if we have staged changes
    if git diff --cached --quiet; then
        print_warning "No staged changes to commit"
        return 1
    fi
    
    local full_message="$commit_type: $commit_message"
    git commit -m "$full_message"
    print_status "Committed: $full_message"
    
    save_history "commit: $full_message"
    
    # Auto push if enabled
    if [ "$AUTO_PUSH" = "true" ]; then
        if git remote get-url origin >/dev/null 2>&1; then
            git push origin main
            print_status "Pushed to remote"
        else
            print_warning "No remote repository configured"
        fi
    fi
}

# Function to commit language-specific components
commit_language() {
    local lang="$1"
    local lang_name=${LANGUAGES[$lang]}
    
    if [ -z "$lang_name" ]; then
        print_error "Unknown language: $lang"
        return 1
    fi
    
    print_info "Committing $lang_name components..."
    
    # Reset any existing staged changes
    git reset HEAD >/dev/null 2>&1 || true
    
    local added_something=false
    
    # Add SDK
    if [ -d "sdk/$lang" ]; then
        git add -f "sdk/$lang/"
        print_status "Added $lang_name SDK directory"
        added_something=true
    elif [ -f "sdk/tusk-me-hard-$lang.md" ]; then
        git add -f "sdk/tusk-me-hard-$lang.md"
        print_status "Added $lang_name SDK documentation"
        added_something=true
    fi
    
    # Add docs
    if [ "$lang" = "javascript" ] && [ -d "docs/sdk/js" ]; then
        git add -f "docs/sdk/js/"
        print_status "Added $lang_name documentation"
        added_something=true
    elif [ -d "docs/sdk/$lang" ]; then
        git add -f "docs/sdk/$lang/"
        print_status "Added $lang_name documentation"
        added_something=true
    fi
    
    # Add cheat sheet
    for file in "docs/cheat-sheet/$lang-cheat-sheet.md" "docs/cheat-sheet/${lang^}-cheat-sheet.md"; do
        if [ -f "$file" ]; then
            git add -f "$file"
            print_status "Added $lang_name cheat sheet"
            added_something=true
            break
        fi
    done
    
    if [ "$added_something" = false ]; then
        print_warning "No $lang_name components found"
        return 1
    fi
    
    # Commit
    local commit_message="feat: Add $lang_name SDK, docs, and cheat sheet"
    git commit -m "$commit_message"
    print_status "Committed $lang_name components"
    
    save_history "commit_language: $lang"
    
    # Auto push if enabled
    if [ "$AUTO_PUSH" = "true" ]; then
        if git remote get-url origin >/dev/null 2>&1; then
            git push origin main
            print_status "Pushed to remote"
        fi
    fi
}

# Function to show status
show_status() {
    print_info "Git Status:"
    echo
    
    # Staged changes
    local staged=$(get_staged_files)
    if [ -n "$staged" ]; then
        print_info "Staged changes:"
        echo "$staged" | sed 's/^/  + /'
        echo
    fi
    
    # Unstaged changes
    local unstaged=$(get_unstaged_changes)
    if [ -n "$unstaged" ]; then
        print_info "Unstaged changes:"
        echo "$unstaged" | sed 's/^/  ~ /'
        echo
    fi
    
    # Recent commits
    print_info "Recent commits:"
    git log --oneline -5 2>/dev/null || print_warning "No commits yet"
    echo
}

# Function to show usage
show_usage() {
    cat << EOF
Usage: g.sh [COMMAND] [OPTIONS]
   or: g.sh [SUMMARY_FILE]       # Shortcut: auto add-all & commit

Commands:
    status               Show git status and recent commits
    add [file/dir]       Add specific file or directory
    add-all              Add all changes interactively
    commit [summary]     Commit staged changes using summary file
    lang [language]      Commit specific language components
    list-langs           List all available languages
    history              Show recent g.sh actions
    config               Show current configuration
    help                 Show this help message

Shortcut Mode:
    g.sh july-20th-recap-1.md     # Auto add-all & commit with summary file
    g.sh my-summary.md            # Looks in summaries/ directory automatically
    g.sh filename                 # Works with or without .md extension

Examples:
    g.sh status                    # Show current status
    g.sh add my-file.md           # Add specific file
    g.sh add summaries/           # Add summaries directory
    g.sh add-all                  # Interactive file selection
    g.sh commit my-summary.md     # Commit using summary file
    g.sh lang php                 # Commit PHP components
    g.sh lang all                 # Commit all languages

Options:
    --force                       # Skip all prompts
    --no-push                     # Don't auto-push
    --type [type]                 # Commit type (feat, fix, docs, etc.)

Languages: ${!LANGUAGES[*]}

Configuration: $CONFIG_FILE
History: $HISTORY_FILE
EOF
}

# Function to show configuration
show_config() {
    print_info "Current Configuration:"
    echo
    
    if [ -f "$CONFIG_FILE" ]; then
        cat "$CONFIG_FILE" | grep -v "^#" | sed 's/^/  /'
    else
        print_warning "No configuration file found"
    fi
    echo
}

# Function to show history
show_history() {
    print_info "Recent g.sh Actions:"
    echo
    
    if [ -f "$HISTORY_FILE" ]; then
        tail -20 "$HISTORY_FILE" | sed 's/^/  /'
    else
        print_warning "No history file found"
    fi
    echo
}

# Main script logic
main() {
    # Load configuration
    load_config
    
    # Check if we're in a git repo
    check_git_repo
    
    local command=${1:-help}
    local force_mode=false
    local no_push=false
    local commit_type="feat"
    
    # Check if first argument is a filename (shortcut mode)
    if [[ "$command" =~ \.(md|txt)$ ]] || [ -f "summaries/$command" ] || [ -f "summaries/$command.md" ]; then
        print_info "Shortcut mode: auto add-all and commit with summary file"
        
        # Force add all changes
        git add -A
        print_status "Added all changes (force mode)"
        save_history "add-all (shortcut)"
        
        # Find and commit with summary file
        local summary_file=$(find_summary_file "$command")
        if [ -n "$summary_file" ]; then
            commit_with_summary "$summary_file" "$commit_type"
            return 0
        else
            print_error "Summary file not found: $command"
            exit 1
        fi
    fi
    
    # Parse options
    shift
    while [[ $# -gt 0 ]]; do
        case $1 in
            --force)
                force_mode=true
                shift
                ;;
            --no-push)
                no_push=true
                shift
                ;;
            --type)
                commit_type="$2"
                shift 2
                ;;
            *)
                break
                ;;
        esac
    done
    
    case "$command" in
        "status")
            show_status
            ;;
        "add")
            if [ $# -eq 0 ]; then
                print_error "No file/directory specified"
                exit 1
            fi
            for item in "$@"; do
                if [ -e "$item" ]; then
                    git add "$item"
                    print_status "Added $item"
                    save_history "add: $item"
                else
                    print_error "File/directory not found: $item"
                fi
            done
            ;;
        "add-all")
            local unstaged=$(get_unstaged_changes)
            if [ -n "$unstaged" ]; then
                if [ "$force_mode" = true ]; then
                    git add .
                    print_status "Added all changes (force mode)"
                else
                    interactive_file_selection $unstaged
                fi
                save_history "add-all"
            else
                print_warning "No unstaged changes found"
            fi
            ;;
        "commit")
            if [ $# -eq 0 ]; then
                print_error "No summary file specified"
                exit 1
            fi
            local summary_file=$(find_summary_file "$1")
            if [ -n "$summary_file" ]; then
                commit_with_summary "$summary_file" "$commit_type"
            else
                print_error "Summary file not found: $1"
                exit 1
            fi
            ;;
        "lang")
            if [ $# -eq 0 ]; then
                print_error "No language specified"
                exit 1
            fi
            if [ "$1" = "all" ]; then
                for lang in "${!LANGUAGES[@]}"; do
                    commit_language "$lang"
                done
            else
                commit_language "$1"
            fi
            ;;
        "list-langs")
            print_info "Available languages:"
            for lang in "${!LANGUAGES[@]}"; do
                echo "  $lang - ${LANGUAGES[$lang]}"
            done
            ;;
        "history")
            show_history
            ;;
        "config")
            show_config
            ;;
        "help"|"-h"|"--help")
            show_usage
            ;;
        *)
            print_error "Unknown command: $command"
            echo
            show_usage
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@" 