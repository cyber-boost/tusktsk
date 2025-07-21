#!/bin/bash

# SDK Sync Script
# Optimized rsync script for syncing SDK folders with development-friendly options
# Usage: ./sync_sdk.sh [folder_name] [local_destination]

# Configuration
REMOTE_USER="user"
REMOTE_HOST="80.54.67.216"
REMOTE_SDK_PATH="/home/user/sdk4"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to show usage
show_usage() {
    echo "SDK Sync Script (Optimized for Development)"
    echo ""
    echo "Usage: $0 [folder_name] [local_destination] [options]"
    echo ""
    echo "Arguments:"
    echo "  folder_name         SDK folder to sync (e.g., javascript, python, bash)"
    echo "  local_destination   Local destination path (optional, defaults to current directory)"
    echo ""
    echo "Options:"
    echo "  --dry-run           Show what would be synced without actually doing it"
    echo "  --delete            Delete files in destination that don't exist in source"
    echo "  --compress          Use compression during transfer"
    echo "  --progress          Show progress during transfer"
    echo "  --watch             Watch for changes and auto-sync (experimental)"
    echo ""
    echo "Examples:"
    echo "  $0 javascript                    # Sync javascript folder"
    echo "  $0 python /tmp/sdk              # Sync python folder to /tmp/sdk"
    echo "  $0 . /opt/backup --dry-run      # Preview sync of entire SDK"
    echo "  $0 bash --progress              # Sync bash folder with progress"
    echo ""
    echo "Available SDK folders:"
    echo "  - javascript"
    echo "  - python"
    echo "  - bash"
    echo "  - php"
    echo "  - go"
    echo "  - ruby"
    echo "  - rust"
    echo "  - java"
    echo "  - csharp"
    echo "  - . (entire SDK)"
    echo ""
    echo "Development Optimizations:"
    echo "  ✓ Excludes common development artifacts"
    echo "  ✓ Preserves file permissions and timestamps"
    echo "  ✓ Resumes interrupted transfers"
    echo "  ✓ Only syncs changed files"
    echo "  ✓ Handles large file transfers efficiently"
}

# Function to build development-optimized rsync options
build_dev_options() {
    local options=()
    
    # Parse command line options
    while [[ $# -gt 0 ]]; do
        case $1 in
            --dry-run)
                options+=("--dry-run")
                shift
                ;;
            --delete)
                options+=("--delete")
                shift
                ;;
            --compress)
                options+=("--compress")
                shift
                ;;
            --progress)
                options+=("--progress")
                shift
                ;;
            --watch)
                options+=("--watch")
                shift
                ;;
            *)
                break
                ;;
        esac
    done
    
    # Development-optimized default options
    options+=(
        "--archive"           # Archive mode (preserves permissions, timestamps, etc.)
        "--recursive"         # Recursive copy
        "--human-readable"    # Human-readable output
        "--stats"             # Show transfer statistics
        "--verbose"           # Verbose output
        "--update"            # Skip files that are newer on receiver
        "--protect-args"      # Protect arguments from shell expansion
        "--partial"           # Keep partially transferred files
        "--exclude=*.pyc"     # Exclude Python bytecode
        "--exclude=__pycache__"  # Exclude Python cache
        "--exclude=*.log"     # Exclude log files
        "--exclude=*.tmp"     # Exclude temporary files
        "--exclude=.git"      # Exclude git directory
        "--exclude=node_modules"  # Exclude node modules
        "--exclude=target"    # Exclude Rust/Java build artifacts
        "--exclude=dist"      # Exclude distribution files
        "--exclude=build"     # Exclude build directories
        "--exclude=.DS_Store" # Exclude macOS files
        "--exclude=Thumbs.db" # Exclude Windows files
    )
    
    echo "${options[@]}"
}

# Function to validate remote folder exists
validate_remote_folder() {
    local folder="$1"
    local remote_path="$REMOTE_SDK_PATH/$folder"
    
    print_status "Validating remote folder: $REMOTE_USER@$REMOTE_HOST:$remote_path"
    
    if ssh -o ConnectTimeout=10 -o BatchMode=yes "$REMOTE_USER@$REMOTE_HOST" "test -d $remote_path" 2>/dev/null; then
        print_success "Remote folder exists: $remote_path"
        return 0
    else
        print_error "Remote folder does not exist: $remote_path"
        return 1
    fi
}

# Function to perform the sync
sync_folder() {
    local folder="$1"
    local local_dest="$2"
    shift 2
    local rsync_options=("$@")
    
    local remote_path="$REMOTE_SDK_PATH/$folder"
    
    print_status "Starting SDK sync..."
    print_status "Remote: $REMOTE_USER@$REMOTE_HOST:$remote_path"
    print_status "Local: $local_dest"
    print_status "Options: ${rsync_options[*]}"
    echo ""
    
    # Create local destination if it doesn't exist
    if [ ! -d "$local_dest" ]; then
        print_status "Creating local destination directory: $local_dest"
        mkdir -p "$local_dest"
    fi
    
    # Build rsync command
    local rsync_cmd=(
        "rsync"
        "${rsync_options[@]}"
        "-e" "ssh -o ConnectTimeout=10"
        "$REMOTE_USER@$REMOTE_HOST:$remote_path/"
        "$local_dest/"
    )
    
    print_status "Executing: ${rsync_cmd[*]}"
    echo ""
    
    # Perform the rsync sync
    if "${rsync_cmd[@]}"; then
        print_success "SDK sync completed successfully!"
        print_status "Synced to: $local_dest"
        
        # Show sync summary
        if [ -d "$local_dest" ]; then
            local file_count=$(find "$local_dest" -type f | wc -l)
            local dir_count=$(find "$local_dest" -type d | wc -l)
            local total_size=$(du -sh "$local_dest" | cut -f1)
            
            print_success "Sync Summary:"
            print_status "  Files: $file_count"
            print_status "  Directories: $dir_count"
            print_status "  Total Size: $total_size"
        fi
    else
        print_error "SDK sync failed!"
        print_warning "You can resume the sync by running the same command again"
        exit 1
    fi
}

# Function to list available SDK folders
list_sdk_folders() {
    print_status "Listing available SDK folders on remote server..."
    print_status "Server: $REMOTE_USER@$REMOTE_HOST"
    print_status "Path: $REMOTE_SDK_PATH"
    echo ""
    
    if ssh -o ConnectTimeout=10 -o BatchMode=yes "$REMOTE_USER@$REMOTE_HOST" "ls -la $REMOTE_SDK_PATH" 2>/dev/null; then
        print_success "Successfully connected to remote server"
    else
        print_error "Failed to connect to remote server. Please check:"
        print_error "1. SSH key authentication is set up"
        print_error "2. Remote server is accessible"
        print_error "3. User has permissions to access $REMOTE_SDK_PATH"
        exit 1
    fi
}

# Main script logic
main() {
    # Check if help is requested
    if [ "$1" = "-h" ] || [ "$1" = "--help" ] || [ "$1" = "help" ]; then
        show_usage
        exit 0
    fi
    
    # Check if list is requested
    if [ "$1" = "-l" ] || [ "$1" = "--list" ] || [ "$1" = "list" ]; then
        list_sdk_folders
        exit 0
    fi
    
    # Validate arguments
    if [ $# -lt 1 ]; then
        print_error "Missing required argument: folder_name"
        echo ""
        show_usage
        exit 1
    fi
    
    local folder="$1"
    local local_dest="${2:-.}"
    shift 2
    
    # Build rsync options from remaining arguments
    local rsync_options=($(build_dev_options "$@"))
    
    print_status "Preparing SDK sync..."
    print_status "Folder: $folder"
    print_status "Local destination: $local_dest"
    print_status "Remote server: $REMOTE_USER@$REMOTE_HOST"
    echo ""
    
    # Validate remote folder exists
    if ! validate_remote_folder "$folder"; then
        print_error "Please use '$0 list' to see available SDK folders"
        exit 1
    fi
    
    # Perform the sync
    sync_folder "$folder" "$local_dest" "${rsync_options[@]}"
}

# Run main function with all arguments
main "$@" 