# Bash Cheat Sheet

## Table of Contents
- [Basic Commands](#basic-commands)
- [File Operations](#file-operations)
- [Text Processing](#text-processing)
- [System Information](#system-information)
- [Process Management](#process-management)
- [Network Commands](#network-commands)
- [Variables and Environment](#variables-and-environment)
- [Control Structures](#control-structures)
- [Functions](#functions)
- [Advanced Features](#advanced-features)
- [Error Handling](#error-handling)
- [Best Practices](#best-practices)

## Basic Commands

### Navigation
```bash
pwd                    # Print working directory
cd /path/to/directory  # Change directory
cd ~                   # Go to home directory
cd -                   # Go to previous directory
cd ..                  # Go to parent directory
ls                     # List files
ls -la                 # List all files with details
ls -lh                 # List files with human-readable sizes
tree                   # Show directory tree
```

### File Viewing
```bash
cat filename           # Display file content
less filename          # View file with pagination
head -n 10 filename    # Show first 10 lines
tail -n 10 filename    # Show last 10 lines
tail -f filename       # Follow file in real-time
more filename          # View file page by page
```

### Help and Documentation
```bash
man command            # Manual page for command
help command           # Built-in help for bash commands
command --help         # Command-specific help
info command           # Info page for command
whatis command         # Brief description of command
```

## File Operations

### Creating Files and Directories
```bash
touch filename         # Create empty file
mkdir directory        # Create directory
mkdir -p path/to/dir   # Create nested directories
mktemp                 # Create temporary file
mktemp -d              # Create temporary directory
```

### Copying and Moving
```bash
cp source dest         # Copy file
cp -r source dest      # Copy directory recursively
mv source dest         # Move/rename file
mv -i source dest      # Move with confirmation
rsync -av source dest  # Synchronize directories
```

### Removing
```bash
rm filename            # Remove file
rm -f filename         # Force remove (no confirmation)
rm -r directory        # Remove directory recursively
rm -rf directory       # Force remove directory
rmdir directory        # Remove empty directory
```

### File Permissions
```bash
chmod 755 filename     # Set permissions (rwxr-xr-x)
chmod +x filename      # Make executable
chmod -w filename      # Remove write permission
chown user:group file  # Change owner and group
chgrp group file       # Change group
umask 022              # Set default permissions
```

### File Attributes
```bash
lsattr filename        # List file attributes
chattr +i filename     # Make file immutable
chattr -i filename     # Remove immutable attribute
chattr +a filename     # Append only
chattr -a filename     # Remove append only
```

## Text Processing

### Searching and Filtering
```bash
grep "pattern" file    # Search for pattern in file
grep -i "pattern" file # Case-insensitive search
grep -r "pattern" dir  # Recursive search
grep -v "pattern" file # Invert match
grep -n "pattern" file # Show line numbers
egrep "pattern" file   # Extended regex
fgrep "pattern" file   # Fixed string search
```

### Text Manipulation
```bash
sed 's/old/new/g' file # Replace text
sed -i 's/old/new/g' file # Replace in place
awk '{print $1}' file  # Print first field
cut -d: -f1 file       # Cut by delimiter
sort file              # Sort lines
sort -n file           # Numeric sort
uniq file              # Remove duplicates
wc -l file             # Count lines
```

### File Comparison
```bash
diff file1 file2       # Compare files
diff -u file1 file2    # Unified diff format
cmp file1 file2        # Compare files byte by byte
comm file1 file2       # Compare sorted files
```

## System Information

### System Status
```bash
uname -a               # System information
hostname               # Show hostname
whoami                 # Current user
id                     # User and group info
uptime                 # System uptime
top                    # Process monitor
htop                   # Interactive process viewer
free -h                # Memory usage
df -h                  # Disk usage
```

### Hardware Information
```bash
lscpu                  # CPU information
lsmem                  # Memory information
lspci                  # PCI devices
lsusb                  # USB devices
lshw                   # Hardware list
dmidecode              # DMI table info
```

### System Monitoring
```bash
ps aux                 # All processes
ps -ef                 # Process list
ps aux | grep process  # Find specific process
kill PID               # Kill process
killall process        # Kill all processes by name
nice -n 10 command     # Run with priority
renice 10 PID          # Change priority
```

## Process Management

### Background Jobs
```bash
command &              # Run in background
nohup command &        # Run immune to hangups
jobs                   # List background jobs
fg %1                  # Bring job to foreground
bg %1                  # Send job to background
kill %1                # Kill background job
```

### Process Control
```bash
Ctrl+C                 # Interrupt process
Ctrl+Z                 # Suspend process
Ctrl+D                 # End of input
Ctrl+L                 # Clear screen
Ctrl+U                 # Clear line
Ctrl+W                 # Delete word
```

## Network Commands

### Network Information
```bash
ip addr                # Show IP addresses
ip route               # Show routing table
netstat -tuln          # Show listening ports
ss -tuln               # Socket statistics
ping host              # Ping host
traceroute host        # Trace route to host
nslookup domain        # DNS lookup
dig domain             # DNS information
```

### Network Connections
```bash
ssh user@host          # SSH connection
scp file user@host:    # Copy file via SSH
rsync -avz src/ dest/  # Synchronize over network
wget url               # Download file
curl url               # Transfer data
nc -l port             # Listen on port
nc host port           # Connect to port
```

## Variables and Environment

### Variable Assignment
```bash
variable=value         # Set variable
echo $variable         # Print variable
export variable=value  # Export to environment
unset variable         # Unset variable
readonly variable=value # Make variable readonly
```

### Special Variables
```bash
$0                     # Script name
$1, $2, ...            # Command line arguments
$#                     # Number of arguments
$@                     # All arguments
$*                     # All arguments as single string
$?                     # Exit status of last command
$$                     # Current process ID
$!                     # Last background process ID
```

### Environment Variables
```bash
HOME                   # Home directory
PATH                   # Command search path
USER                   # Current user
SHELL                  # Current shell
PWD                    # Current directory
OLDPWD                 # Previous directory
PS1                    # Primary prompt
PS2                    # Secondary prompt
```

## Control Structures

### Conditional Statements
```bash
if [ condition ]; then
    commands
elif [ condition ]; then
    commands
else
    commands
fi

# Test conditions
[ -f file ]            # File exists
[ -d dir ]             # Directory exists
[ -r file ]            # File is readable
[ -w file ]            # File is writable
[ -x file ]            # File is executable
[ -z string ]          # String is empty
[ -n string ]          # String is not empty
[ str1 = str2 ]        # Strings are equal
[ num1 -eq num2 ]      # Numbers are equal
[ num1 -lt num2 ]      # Number1 less than number2
```

### Loops
```bash
# For loop
for item in list; do
    commands
done

# While loop
while [ condition ]; do
    commands
done

# Until loop
until [ condition ]; do
    commands
done

# C-style for loop
for ((i=0; i<10; i++)); do
    commands
done
```

### Case Statement
```bash
case $variable in
    pattern1)
        commands
        ;;
    pattern2)
        commands
        ;;
    *)
        commands
        ;;
esac
```

## Functions

### Function Definition
```bash
function_name() {
    local var=value    # Local variable
    commands
    return value       # Return value
}

# Or
function function_name {
    commands
}
```

### Function Usage
```bash
function_name arg1 arg2  # Call function with arguments
result=$(function_name)  # Capture output
```

## Advanced Features

### Command Substitution
```bash
$(command)             # Command substitution
`command`              # Legacy command substitution
$(date)                # Get current date
$(whoami)              # Get current user
```

### Process Substitution
```bash
<(command)             # Input process substitution
>(command)             # Output process substitution
diff <(sort file1) <(sort file2)  # Compare sorted files
```

### Here Documents
```bash
cat << EOF > file.txt
Line 1
Line 2
EOF

# With variable expansion
cat << 'EOF' > file.txt
$variable will not be expanded
EOF
```

### Arrays
```bash
array=(item1 item2 item3)  # Create array
echo ${array[0]}           # Access element
echo ${array[@]}           # All elements
echo ${#array[@]}          # Array length
array+=(item4)             # Append to array
```

### Associative Arrays (Bash 4+)
```bash
declare -A assoc_array
assoc_array[key]=value
echo ${assoc_array[key]}
echo ${!assoc_array[@]}    # All keys
echo ${assoc_array[@]}     # All values
```

## Error Handling

### Exit Codes
```bash
exit 0                  # Success
exit 1                  # General error
exit 2                  # Misuse of shell builtins
exit 126                # Command not executable
exit 127                # Command not found
exit 128+n              # Fatal error signal n
```

### Error Handling Patterns
```bash
# Check exit status
if command; then
    echo "Success"
else
    echo "Failed"
fi

# Set error handling
set -e                  # Exit on error
set -u                  # Exit on undefined variable
set -o pipefail         # Exit on pipe failure
trap 'echo "Error occurred"' ERR  # Error trap
```

### Logging
```bash
# Redirect output
command > output.log 2>&1    # Redirect stdout and stderr
command >> output.log 2>&1   # Append to log
exec 1> >(tee -a logfile)    # Tee output to log
exec 2> >(tee -a error.log)  # Tee errors to log
```

## Best Practices

### Script Structure
```bash
#!/bin/bash
# Script description
# Author: Name
# Date: YYYY-MM-DD

set -euo pipefail       # Strict error handling

# Constants
readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_NAME="$(basename "$0")"

# Functions
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

error() {
    log "ERROR: $*" >&2
    exit 1
}

# Main script
main() {
    log "Starting $SCRIPT_NAME"
    # Your code here
    log "Finished $SCRIPT_NAME"
}

# Run main if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
```

### Security Best Practices
```bash
# Quote variables
echo "$variable"        # Good
echo $variable          # Bad (can break with spaces)

# Use [[ ]] for tests
[[ -f "$file" ]]       # Good
[ -f "$file" ]         # Less good

# Check file existence
[[ -f "$file" ]] || error "File $file not found"

# Validate input
[[ $# -eq 1 ]] || error "Usage: $0 <filename>"
```

### Performance Tips
```bash
# Use builtins when possible
echo "text"             # Good
/bin/echo "text"        # Slower

# Avoid subshells
var=$(command)          # Creates subshell
command > temp && var=$(cat temp) && rm temp  # Avoid if possible

# Use arrays for multiple values
files=(file1 file2 file3)
for file in "${files[@]}"; do
    process "$file"
done
```

### Debugging
```bash
# Enable debug mode
set -x                  # Print commands before execution
set -v                  # Print shell input lines

# Debug function
debug() {
    [[ "${DEBUG:-false}" == "true" ]] && echo "DEBUG: $*" >&2
}

# Use with: DEBUG=true ./script.sh
```

## Common Patterns

### File Processing
```bash
# Process all files in directory
for file in *.txt; do
    [[ -f "$file" ]] || continue
    process "$file"
done

# Read file line by line
while IFS= read -r line; do
    echo "Processing: $line"
done < "filename"
```

### Argument Parsing
```bash
while [[ $# -gt 0 ]]; do
    case $1 in
        -f|--file)
            file="$2"
            shift 2
            ;;
        -v|--verbose)
            verbose=true
            shift
            ;;
        --)
            shift
            break
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done
```

### Temporary Files
```bash
# Create temporary file
temp_file=$(mktemp)
trap 'rm -f "$temp_file"' EXIT

# Use temporary file
echo "data" > "$temp_file"
process "$temp_file"
```

This bash cheat sheet covers the most essential commands and patterns for effective shell scripting. Remember to always test your scripts thoroughly and follow security best practices when writing production code. 