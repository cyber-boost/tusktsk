#!/bin/bash

# Simple Git Push Script for TuskTsk Public Repository
# Usage: ./git.sh [message_file]

set -e

# Get commit message
if [ -n "$1" ]; then
    if [ -f "$1" ]; then
        MESSAGE="$(cat "$1")"
    else
        echo "File not found: $1"
        exit 1
    fi
else
    echo -n "Commit message: "
    read MESSAGE
fi

# Go to public repo
cd /tmp/tusktsk-transfer || exit 1

# Pull latest
git pull origin main 2>/dev/null || git pull origin master 2>/dev/null

# Copy changes (excluding private stuff)
rsync -av --delete \
    --exclude='.git' \
    --exclude='admin/' \
    --exclude='enterprise/' \
    --exclude='universe/' \
    --exclude='server/' \
    --exclude='backup/' \
    --exclude='z_archive/' \
    --exclude='reference/' \
    --exclude='prompts/' \
    --exclude='summaries/' \
    --exclude='pkg/protection/' \
    --exclude='sql/' \
    --exclude='updates/' \
    --exclude='transfer-*.sh' \
    --exclude='CLAUDE.md' \
    --exclude='gitTrick.sh' \
    --exclude='*action-plan*' \
    /opt/tsk_git/ .

# Add, commit, push
git add .
git commit -m "$MESSAGE"
git push origin main 2>/dev/null || git push origin master 2>/dev/null

echo "✅ Updated cyber-boost/tusktsk"