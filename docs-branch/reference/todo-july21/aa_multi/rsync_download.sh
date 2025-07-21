#!/bin/bash

# RSync Download Script
# Downloads any folder from remote server using rsync (much better than SCP)
# Usage: ./rsync_download.sh [remote_spec] [local_destination]

# Default configuration (can be overridden)
DEFAULT_USER="user"
DEFAULT_HOST="80.54.67.216"
DEFAULT_BASE_PATH="/home/user"

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

# Function to parse remote path
parse_remote_path() {
    local input="$1"
    local user="$DEFAULT_USER"
    local host="$DEFAULT_HOST"
    local path="$input"
    
    # Check if input contains user@host:path format
    if [[ "$input" =~ ^([^@]+)@([^:]+):(.+)$ ]]; then
        user="${BASH_REMATCH[1]}"
        host="${BASH_REMATCH[2]}"
        path="${BASH_REMATCH[3]}"
    elif [[ "$input" =~ ^([^:]+):(.+)$ ]]; then
        host="${BASH_REMATCH[1]}"
        path="${BASH_REMATCH[2]}"
    fi
    
    echo "$user|$host|$path"
}

# Function to show usage
show_usage() {
    echo "RSync Download Script (Better than SCP!)"
    echo ""
    echo "Usage: $0 [remote_spec] [local_destination] [options]"
    echo ""
    echo "Remote specification formats:"
    echo "  1. user@host:path    - Full specification"
    echo "  2. host:path         - Uses default user"
    echo "  3. path              - Uses default user and host"
    echo ""
    echo "Arguments:"
    echo "  remote_spec         Remote path specification"
    echo "  local_destination   Local destination path (optional, defaults to current directory)"
    echo ""
    echo "Options:"
    echo "  --dry-run           Show what would be transferred without actually doing it"
    echo "  --delete            Delete files in destination that don't exist in source"
    echo "  --exclude=PATTERN   Exclude files matching pattern"
    echo "  --include=PATTERN   Include files matching pattern"
    echo "  --compress          Use compression during transfer"
    echo "  --partial           Keep partially transferred files"
    echo "  --progress          Show progress during transfer"
    echo ""
    echo "Examples:"
    echo "  $0 user@80.54.67.216:/home/user/sdk4/javascript"
    echo "  $0 80.54.67.216:/var/www/html --dry-run"
    echo "  $0 sdk4/python /tmp/downloads --progress"
    echo "  $0 /etc/nginx /opt/backup --delete"
    echo "  $0 user@other-server:/opt/apps ./apps --exclude='*.log'"
    echo ""
    echo "Default configuration:"
    echo "  User: $DEFAULT_USER"
    echo "  Host: $DEFAULT_HOST"
    echo "  Base Path: $DEFAULT_BASE_PATH"
    echo ""
    echo "Commands:"
    echo "  $0 list [user@host]              # List folders on remote server"
    echo "  $0 explore [user@host:path] [depth]  # Explore directory structure"
    echo "  $0 help                          # Show this help"
    echo ""
    echo "RSync Advantages over SCP:"
    echo "  ✓ Resume interrupted transfers"
    echo "  ✓ Only transfer changed files"
    echo "  ✓ Better progress reporting"
    echo "  ✓ Compression support"
    echo "  ✓ File filtering and exclusions"
    echo "  ✓ Dry-run capability"
    echo "  ✓ Partial file handling"
}

# Function to list available remote folders
list_remote_folders() {
    local remote_spec="${1:-$DEFAULT_USER@$DEFAULT_HOST}"
    local user host path
    
    # Parse remote specification
    if [[ "$remote_spec" =~ ^([^@]+)@([^:]+)$ ]]; then
        user="${BASH_REMATCH[1]}"
        host="${BASH_REMATCH[2]}"
        path="$DEFAULT_BASE_PATH"
    elif [[ "$remote_spec" =~ ^([^:]+)$ ]]; then
        user="$DEFAULT_USER"
        host="$remote_spec"
        path="$DEFAULT_BASE_PATH"
    else
        print_error "Invalid remote specification: $remote_spec"
        exit 1
    fi
    
    print_status "Listing available folders on remote server..."
    print_status "Server: $user@$host"
    print_status "Path: $path"
    echo ""
    
    if ssh -o ConnectTimeout=10 -o BatchMode=yes "$user@$host" "ls -la $path" 2>/dev/null; then
        print_success "Successfully connected to remote server"
    else
        print_error "Failed to connect to remote server. Please check:"
        print_error "1. SSH key authentication is set up"
        print_error "2. Remote server is accessible"
        print_error "3. User has permissions to access $path"
        exit 1
    fi
}

# Function to explore remote directory structure
explore_remote() {
    local remote_spec="${1:-$DEFAULT_USER@$DEFAULT_HOST:$DEFAULT_BASE_PATH}"
    local max_depth="${2:-2}"
    local user host path
    
    # Parse remote specification
    local parsed=$(parse_remote_path "$remote_spec")
    IFS='|' read -r user host path <<< "$parsed"
    
    print_status "Exploring remote directory structure..."
    print_status "Server: $user@$host"
    print_status "Path: $path"
    print_status "Max depth: $max_depth"
    echo ""
    
    if ssh -o ConnectTimeout=10 -o BatchMode=yes "$user@$host" "find $path -type d -maxdepth $max_depth 2>/dev/null | sort"; then
        print_success "Directory exploration completed"
    else
        print_error "Failed to explore remote directory"
        exit 1
    fi
}

# Function to validate remote folder exists
validate_remote_folder() {
    local user="$1"
    local host="$2"
    local path="$3"
    
    print_status "Validating remote folder: $user@$host:$path"
    
    if ssh -o ConnectTimeout=10 -o BatchMode=yes "$user@$host" "test -d $path" 2>/dev/null; then
        print_success "Remote folder exists: $user@$host:$path"
        return 0
    else
        print_error "Remote folder does not exist: $user@$host:$path"
        return 1
    fi
}

# Function to build rsync options
build_rsync_options() {
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
            --exclude=*)
                options+=("$1")
                shift
                ;;
            --include=*)
                options+=("$1")
                shift
                ;;
            --compress)
                options+=("--compress")
                shift
                ;;
            --partial)
                options+=("--partial")
                shift
                ;;
            --progress)
                options+=("--progress")
                shift
                ;;
            *)
                break
                ;;
        esac
    done
    
    # Default options for better experience
    options+=(
        "--archive"           # Archive mode (preserves permissions, timestamps, etc.)
        "--recursive"         # Recursive copy
        "--human-readable"    # Human-readable output
        "--stats"             # Show transfer statistics
        "--verbose"           # Verbose output
        "--update"            # Skip files that are newer on receiver
        "--protect-args"      # Protect arguments from shell expansion
    )
    
    echo "${options[@]}"
}

# Function to perform the download using rsync
download_folder() {
    local user="$1"
    local host="$2"
    local remote_path="$3"
    local local_dest="$4"
    shift 4
    local rsync_options=("$@")
    
    print_status "Starting rsync download..."
    print_status "Remote: $user@$host:$remote_path"
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
        "$user@$host:$remote_path/"
        "$local_dest/"
    )
    
    print_status "Executing: ${rsync_cmd[*]}"
    echo ""
    
    # Perform the rsync download
    if "${rsync_cmd[@]}"; then
        print_success "RSync download completed successfully!"
        print_status "Downloaded to: $local_dest"
        
        # Show download summary
        local folder_name=$(basename "$remote_path")
        if [ -d "$local_dest" ]; then
            local file_count=$(find "$local_dest" -type f | wc -l)
            local dir_count=$(find "$local_dest" -type d | wc -l)
            local total_size=$(du -sh "$local_dest" | cut -f1)
            
            print_success "Download Summary:"
            print_status "  Files: $file_count"
            print_status "  Directories: $dir_count"
            print_status "  Total Size: $total_size"
        fi
    else
        print_error "RSync download failed!"
        print_warning "You can resume the transfer by running the same command again"
        exit 1
    fi
}

# Function to show progress
show_progress() {
    local user="$1"
    local host="$2"
    local remote_path="$3"
    local local_dest="$4"
    
    print_status "Preparing rsync download..."
    print_status "Remote server: $user@$host"
    print_status "Remote path: $remote_path"
    print_status "Local destination: $local_dest"
    echo ""
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
        list_remote_folders "$2"
        exit 0
    fi
    
    # Check if explore is requested
    if [ "$1" = "-e" ] || [ "$1" = "--explore" ] || [ "$1" = "explore" ]; then
        explore_remote "$2" "$3"
        exit 0
    fi
    
    # Validate arguments
    if [ $# -lt 1 ]; then
        print_error "Missing required argument: remote_spec"
        echo ""
        show_usage
        exit 1
    fi
    
    local remote_spec="$1"
    local local_dest="${2:-.}"
    shift 2
    
    # Parse remote specification
    local parsed=$(parse_remote_path "$remote_spec")
    IFS='|' read -r user host path <<< "$parsed"
    
    # Build rsync options from remaining arguments
    local rsync_options=($(build_rsync_options "$@"))
    
    # Show progress
    show_progress "$user" "$host" "$path" "$local_dest"
    
    # Validate remote folder exists
    if ! validate_remote_folder "$user" "$host" "$path"; then
        print_error "Please use '$0 list [user@host]' to see available folders"
        print_error "Or use '$0 explore [user@host:path] [depth]' to explore directory structure"
        exit 1
    fi
    
    # Perform the download
    download_folder "$user" "$host" "$path" "$local_dest" "${rsync_options[@]}"
}

# Run main function with all arguments
main "$@" 