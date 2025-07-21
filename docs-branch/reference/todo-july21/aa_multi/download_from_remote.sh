#!/bin/bash

# Reverse SCP Download Script
# Downloads any folder from remote server to local machine
# Usage: ./download_from_remote.sh [remote_path] [local_destination]

# Configuration
REMOTE_USER="user"
REMOTE_HOST="80.54.67.216"
REMOTE_BASE_PATH="/home/user"  # Changed from /home/user/sdk4 to /home/user

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
    echo "Reverse SCP Download Script"
    echo ""
    echo "Usage: $0 [remote_path] [local_destination]"
    echo ""
    echo "Arguments:"
    echo "  remote_path        Full path to folder on remote server (relative to $REMOTE_BASE_PATH)"
    echo "  local_destination  Local destination path (optional, defaults to current directory)"
    echo ""
    echo "Examples:"
    echo "  $0 sdk4/javascript                    # Download sdk4/javascript folder"
    echo "  $0 sdk4/python /tmp/downloads        # Download sdk4/python folder to /tmp/downloads"
    echo "  $0 sdk4 /opt/backup                  # Download entire sdk4 folder"
    echo "  $0 documents /home/user/backup       # Download documents folder"
    echo "  $0 projects/myproject ./             # Download projects/myproject folder"
    echo ""
    echo "Common remote paths:"
    echo "  - sdk4/javascript"
    echo "  - sdk4/python"
    echo "  - sdk4/bash"
    echo "  - sdk4/php"
    echo "  - sdk4/go"
    echo "  - sdk4/ruby"
    echo "  - sdk4/rust"
    echo "  - sdk4/java"
    echo "  - sdk4/csharp"
    echo "  - sdk4 (entire sdk4 folder)"
    echo "  - documents"
    echo "  - projects"
    echo "  - any other folder in /home/user/"
}

# Function to list available remote folders
list_remote_folders() {
    print_status "Listing available folders on remote server..."
    print_status "Base path: $REMOTE_BASE_PATH"
    echo ""
    
    if ssh -o ConnectTimeout=10 -o BatchMode=yes "$REMOTE_USER@$REMOTE_HOST" "ls -la $REMOTE_BASE_PATH" 2>/dev/null; then
        print_success "Successfully connected to remote server"
        echo ""
        print_status "To explore subdirectories, use:"
        print_status "  ssh $REMOTE_USER@$REMOTE_HOST 'find $REMOTE_BASE_PATH -type d -maxdepth 2'"
    else
        print_error "Failed to connect to remote server. Please check:"
        print_error "1. SSH key authentication is set up"
        print_error "2. Remote server is accessible"
        print_error "3. User has permissions to access $REMOTE_BASE_PATH"
        exit 1
    fi
}

# Function to explore remote directory structure
explore_remote() {
    local path="${1:-.}"
    local max_depth="${2:-2}"
    
    print_status "Exploring remote directory structure..."
    print_status "Path: $REMOTE_BASE_PATH/$path"
    print_status "Max depth: $max_depth"
    echo ""
    
    if ssh -o ConnectTimeout=10 -o BatchMode=yes "$REMOTE_USER@$REMOTE_HOST" "find $REMOTE_BASE_PATH/$path -type d -maxdepth $max_depth 2>/dev/null | sort"; then
        print_success "Directory exploration completed"
    else
        print_error "Failed to explore remote directory"
        exit 1
    fi
}

# Function to validate remote folder exists
validate_remote_folder() {
    local folder="$1"
    local remote_path="$REMOTE_BASE_PATH/$folder"
    
    print_status "Validating remote folder: $remote_path"
    
    if ssh -o ConnectTimeout=10 -o BatchMode=yes "$REMOTE_USER@$REMOTE_HOST" "test -d $remote_path" 2>/dev/null; then
        print_success "Remote folder exists: $remote_path"
        return 0
    else
        print_error "Remote folder does not exist: $remote_path"
        return 1
    fi
}

# Function to perform the download
download_folder() {
    local remote_folder="$1"
    local local_dest="$2"
    local remote_path="$REMOTE_BASE_PATH/$remote_folder"
    
    print_status "Starting download..."
    print_status "Remote: $REMOTE_USER@$REMOTE_HOST:$remote_path"
    print_status "Local: $local_dest"
    
    # Create local destination if it doesn't exist
    if [ ! -d "$local_dest" ]; then
        print_status "Creating local destination directory: $local_dest"
        mkdir -p "$local_dest"
    fi
    
    # Perform the SCP download
    if scp -r -o ConnectTimeout=10 "$REMOTE_USER@$REMOTE_HOST:$remote_path" "$local_dest"; then
        print_success "Download completed successfully!"
        print_status "Downloaded to: $local_dest"
        
        # Show download summary
        if [ -d "$local_dest/$(basename $remote_folder)" ]; then
            local downloaded_path="$local_dest/$(basename $remote_folder)"
            local file_count=$(find "$downloaded_path" -type f | wc -l)
            local dir_count=$(find "$downloaded_path" -type d | wc -l)
            local total_size=$(du -sh "$downloaded_path" | cut -f1)
            
            print_success "Download Summary:"
            print_status "  Files: $file_count"
            print_status "  Directories: $dir_count"
            print_status "  Total Size: $total_size"
        fi
    else
        print_error "Download failed!"
        exit 1
    fi
}

# Function to show progress
show_progress() {
    local remote_folder="$1"
    local local_dest="$2"
    
    print_status "Preparing download..."
    print_status "Remote folder: $remote_folder"
    print_status "Local destination: $local_dest"
    print_status "Remote server: $REMOTE_USER@$REMOTE_HOST"
    print_status "Full remote path: $REMOTE_BASE_PATH/$remote_folder"
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
        list_remote_folders
        exit 0
    fi
    
    # Check if explore is requested
    if [ "$1" = "-e" ] || [ "$1" = "--explore" ] || [ "$1" = "explore" ]; then
        explore_remote "${2:-.}" "${3:-2}"
        exit 0
    fi
    
    # Validate arguments
    if [ $# -lt 1 ]; then
        print_error "Missing required argument: remote_path"
        echo ""
        show_usage
        exit 1
    fi
    
    local remote_folder="$1"
    local local_dest="${2:-.}"
    
    # Show progress
    show_progress "$remote_folder" "$local_dest"
    
    # Validate remote folder exists
    if ! validate_remote_folder "$remote_folder"; then
        print_error "Please use '$0 list' to see available folders"
        print_error "Or use '$0 explore [path] [depth]' to explore directory structure"
        exit 1
    fi
    
    # Perform the download
    download_folder "$remote_folder" "$local_dest"
}

# Run main function with all arguments
main "$@" 