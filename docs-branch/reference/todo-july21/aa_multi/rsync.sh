#!/bin/bash

# Simple wrapper for the rsync download script
# Usage: ./rsync.sh [remote_spec] [local_destination] [options]

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Call the main rsync script
"$SCRIPT_DIR/rsync_download.sh" "$@" 