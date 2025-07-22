#!/bin/bash

# TuskLang Git Workflow Script
# Handles staged commits to private repository with language-specific releases

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
CURRENT_BRANCH=""
MASTER_CONTENT_FILE="README.md"

# Language definitions with their directories and files (in desired order)
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

# Function to check if language has all required components
check_language_components() {
    local lang=$1
    local has_sdk=false
    local has_docs=false
    local has_cheat_sheet=false
    local cheat_sheet_file=""
    
    # Check SDK
    if [ -d "sdk/$lang" ] || [ -f "sdk/tusk-me-hard-$lang.md" ]; then
        has_sdk=true
    fi
    
    # Check docs (handle js/javascript naming)
    if [ -d "docs/$lang" ]; then
        has_docs=true
    elif [ "$lang" = "javascript" ] && [ -d "docs/sdk/js" ]; then
        has_docs=true
    fi
    
    # Check cheat sheet in docs/cheat-sheet/ (handle case sensitivity)
    for file in "docs/cheat-sheet/$lang-cheat-sheet.md" "docs/cheat-sheet/${lang^}-cheat-sheet.md"; do
        if [ -f "$file" ]; then
            has_cheat_sheet=true
            cheat_sheet_file="$file"
            break
        fi
    done
    
    echo "$has_sdk:$has_docs:$has_cheat_sheet:$cheat_sheet_file"
}

# Function to get language-specific commit message
get_language_commit_message() {
    local lang=$1
    local lang_name=${LANGUAGES[$lang]}
    
    # Get content from cheat sheet for commit message
    local cheat_sheet_file=""
    for file in "docs/cheat-sheet/$lang-cheat-sheet.md" "docs/cheat-sheet/${lang^}-cheat-sheet.md"; do
        if [ -f "$file" ]; then
            cheat_sheet_file="$file"
            break
        fi
    done
    
    if [ -n "$cheat_sheet_file" ] && [ -f "$cheat_sheet_file" ]; then
        # Extract first meaningful line from cheat sheet
        local content=$(head -n 50 "$cheat_sheet_file" | grep -E "^# " | head -n 1 | sed 's/^# //')
        if [ -n "$content" ]; then
            echo "feat: Add $lang_name SDK, docs, and cheat sheet - $content"
        else
            echo "feat: Add $lang_name SDK, docs, and cheat sheet"
        fi
    else
        echo "feat: Add $lang_name SDK, docs, and cheat sheet"
    fi
}

# Function to create initial .gitignore
create_gitignore() {
    cat > .gitignore << 'EOF'
# SDK directories (will be committed individually)
sdk/
docs/

# Excluded directories and files
admin/
universe/
z_archive/
.claude/
CLAUDE.md

# Build artifacts and large files
*.log
*.tmp
*.cache
*.tar.gz
*.zip
*.rar
*.7z
node_modules/
vendor/
__pycache__/
*.pyc
*.pyo
*.pyd
.Python
build/
develop-eggs/
dist/
downloads/
eggs/
.eggs/
lib/
lib64/
parts/
sdist/
var/
wheels/
*.egg-info/
.installed.cfg
*.egg

# IDE files
.vscode/
.idea/
*.swp
*.swo
*~

# OS files
.DS_Store
Thumbs.db

# Temporary files
*.tmp
*.temp

# Large build directories
admin/builds/
EOF
    print_status "Created .gitignore excluding SDK, docs, and large files"
}

# Function to initialize git repository
initialize_repo() {
    print_info "Initializing git repository..."
    
    if [ -d ".git" ]; then
        print_warning "Git repository already exists"
        return
    fi
    
    git init
    print_status "Git repository initialized"
    
    # Create .gitignore
    create_gitignore
    
    # Add remote if provided
    if [ -n "$PRIVATE_REPO_URL" ]; then
        git remote add origin "$PRIVATE_REPO_URL"
        print_status "Added remote origin: $PRIVATE_REPO_URL"
    fi
}

# Function to create initial commit
initial_commit() {
    print_info "Creating initial commit..."
    
    # Initialize repo if needed
    if [ ! -d ".git" ]; then
        initialize_repo
    fi
    
    # Add all files except SDK and docs (using git add with patterns)
    git add .gitignore
    git add README.md
    
    # Add files with patterns (only if they exist)
    for pattern in "*.md" "*.sh" "*.svg" "*.tsk" "*.pnt" "*.peanuts"; do
        if ls $pattern >/dev/null 2>&1; then
            git add $pattern
            print_status "Added files matching pattern: $pattern"
        fi
    done
    
    # Add directories that should be included in initial commit
    for dir in summaries legal svg icons how-to-tusk tsk_examples fujsen templates plugins tests bin reference; do
        if [ -d "$dir" ]; then
            # Check if directory is ignored by gitignore
            if ! git check-ignore -q "$dir/"; then
                git add "$dir/"
                print_status "Added $dir directory"
            else
                print_warning "$dir is ignored by .gitignore, skipping."
            fi
        fi
    done
    
    # Commit
    if git diff --cached --quiet; then
        print_warning "No changes to commit"
        return
    fi
    
    git commit -m "Initial commit: TuskLang project setup"
    print_status "Initial commit created"
    
    # Push if remote exists
    if git remote get-url origin >/dev/null 2>&1; then
        git push -u origin master
        print_status "Pushed to remote repository"
    else
        print_warning "No remote repository configured"
    fi
}

# Function to commit a specific language
commit_language() {
    local lang=$1
    local lang_name=${LANGUAGES[$lang]}
    
    if [ -z "$lang_name" ]; then
        print_error "Unknown language: $lang"
        return 1
    fi
    
    print_info "Committing $lang_name components..."
    
    # Check what components exist
    local components=$(check_language_components "$lang")
    IFS=':' read -r has_sdk has_docs has_cheat_sheet cheat_sheet_file <<< "$components"
    
    # Reset any existing staged changes
    git reset HEAD >/dev/null 2>&1 || true
    
    # Add SDK
    if [ "$has_sdk" = "true" ]; then
        if [ -d "sdk/$lang" ]; then
            git add -f "sdk/$lang/"
            print_status "Added $lang_name SDK directory"
        elif [ -f "sdk/tusk-me-hard-$lang.md" ]; then
            git add -f "sdk/tusk-me-hard-$lang.md"
            print_status "Added $lang_name SDK documentation"
        fi
    fi
    
    # Add docs
    if [ "$has_docs" = "true" ]; then
        if [ "$lang" = "javascript" ]; then
            git add -f "docs/sdk/js/"
            print_status "Added $lang_name documentation"
        else
            git add -f "docs/sdk/$lang/"
            print_status "Added $lang_name documentation"
        fi
    fi
    
    # Add cheat sheet
    if [ "$has_cheat_sheet" = "true" ] && [ -n "$cheat_sheet_file" ]; then
        git add -f "$cheat_sheet_file"
        print_status "Added $lang_name cheat sheet from docs/cheat-sheet/"
    fi
    
    # Check if anything was staged
    if git diff --cached --quiet; then
        print_warning "No $lang_name components found to commit"
        return 1
    fi
    
    # Commit
    local commit_message=$(get_language_commit_message "$lang")
    git commit -m "$commit_message"
    print_status "Committed $lang_name components: $commit_message"
    
    # Push
    if git remote get-url origin >/dev/null 2>&1; then
        git push origin master
        print_status "Pushed $lang_name components to remote"
    else
        print_warning "No remote repository configured"
    fi
}

# Function to list available languages and their status
list_languages() {
    print_info "Available languages and their components:"
    echo
    
    for lang in "${!LANGUAGES[@]}"; do
        local lang_name=${LANGUAGES[$lang]}
        local components=$(check_language_components "$lang")
        IFS=':' read -r has_sdk has_docs has_cheat_sheet cheat_sheet_file <<< "$components"
        
        echo -n "${CYAN}$lang_name${NC} ($lang): "
        
        local status_parts=()
        if [ "$has_sdk" = "true" ]; then
            status_parts+=("SDK")
        fi
        if [ "$has_docs" = "true" ]; then
            status_parts+=("Docs")
        fi
        if [ "$has_cheat_sheet" = "true" ]; then
            status_parts+=("Cheat Sheet")
        fi
        
        if [ ${#status_parts[@]} -eq 0 ]; then
            echo "${RED}No components found${NC}"
        else
            echo "${GREEN}${status_parts[*]}${NC}"
        fi
    done
    echo
}

# Function to commit all languages
commit_all() {
    print_info "Committing all available languages..."
    
    local committed_count=0
    local total_languages=${#LANGUAGES[@]}
    
    for lang in "${!LANGUAGES[@]}"; do
        local lang_name=${LANGUAGES[$lang]}
        
        # Check if language has any components
        local components=$(check_language_components "$lang")
        IFS=':' read -r has_sdk has_docs has_cheat_sheet cheat_sheet_file <<< "$components"
        
        if [ "$has_sdk" = "true" ] || [ "$has_docs" = "true" ] || [ "$has_cheat_sheet" = "true" ]; then
            print_info "Processing $lang_name..."
            if commit_language "$lang"; then
                ((committed_count++))
                print_status "Successfully committed $lang_name"
            else
                print_warning "Failed to commit $lang_name (no components found)"
            fi
        else
            print_warning "Skipping $lang_name (no components found)"
        fi
        
        echo
    done
    
    print_status "Completed! Committed $committed_count out of $total_languages languages"
}

# Function to commit a markdown file to summaries directory
commit_summary_file() {
    local file_path=$1
    
    if [ -z "$file_path" ]; then
        print_error "No file specified. Usage: $0 <filename.md>"
        return 1
    fi
    
    if [ ! -f "$file_path" ]; then
        print_error "File not found: $file_path"
        return 1
    fi
    
    # Get file extension
    local file_ext="${file_path##*.}"
    if [ "$file_ext" != "md" ]; then
        print_error "File must be a .md file: $file_path"
        return 1
    fi
    
    # Get filename without path
    local filename=$(basename "$file_path")
    
    # Create summaries directory if it doesn't exist
    if [ ! -d "summaries" ]; then
        mkdir -p summaries
        print_status "Created summaries directory"
    fi
    
    # Copy file to summaries directory
    cp "$file_path" "summaries/$filename"
    print_status "Copied $filename to summaries/"
    
    # Read file content for commit message (first 100 characters)
    local content=$(head -c 100 "$file_path" | tr '\n' ' ' | sed 's/^# //' | sed 's/^## //')
    if [ ${#content} -gt 100 ]; then
        content="${content:0:97}..."
    fi
    
    # Add and commit
    git add "summaries/$filename"
    
    if git diff --cached --quiet; then
        print_warning "No changes to commit"
        return 1
    fi
    
    local commit_message="docs: Add $filename to summaries - $content"
    git commit -m "$commit_message"
    print_status "Committed $filename: $commit_message"
    
    # Push
    if git remote get-url origin >/dev/null 2>&1; then
        git push origin master
        print_status "Pushed $filename to remote repository"
    else
        print_warning "No remote repository configured"
    fi
}

# Function to show usage
show_usage() {
    cat << EOF
Usage: $0 [COMMAND] [OPTIONS]

Commands:
    initial              Create initial commit with project setup
    list                 List all available languages and their components
    [language]           Commit specific language (e.g., php, javascript, python)
    all                  Commit all available languages
    <filename.md>        Commit markdown file to summaries/ directory
    help                 Show this help message

Languages:
    php                  PHP SDK, docs, and cheat sheet
    javascript           JavaScript/Node.js SDK, docs, and cheat sheet
    python               Python SDK, docs, and cheat sheet
    bash                 Bash SDK, docs, and cheat sheet
    csharp               C# SDK, docs, and cheat sheet
    go                   Go SDK, docs, and cheat sheet
    rust                 Rust SDK, docs, and cheat sheet
    ruby                 Ruby SDK, docs, and cheat sheet
    java                 Java SDK, docs, and cheat sheet

Examples:
    $0 initial           # Create initial commit
    $0 list              # List all languages
    $0 php               # Commit PHP components
    $0 all               # Commit all languages
    $0 my-file.md        # Commit my-file.md to summaries/

Configuration:
    PRIVATE_REPO_URL: $PRIVATE_REPO_URL
    REPO_NAME: $REPO_NAME
EOF
}

# Main script logic
main() {
    local command=${1:-help}
    
    case "$command" in
        "initial")
            initial_commit
            ;;
        "list")
            list_languages
            ;;
        "all")
            commit_all
            ;;
        "help"|"-h"|"--help")
            show_usage
            ;;
        *)
            # Check if it's a valid language
            if [[ -n "${LANGUAGES[$command]}" ]]; then
                commit_language "$command"
            else
                # Check if it's a markdown file
                if [[ "$command" == *.md ]]; then
                    commit_summary_file "$command"
                else
                    print_error "Unknown command: $command"
                    echo
                    show_usage
                    exit 1
                fi
            fi
            ;;
    esac
}

# Run main function with all arguments
main "$@"
