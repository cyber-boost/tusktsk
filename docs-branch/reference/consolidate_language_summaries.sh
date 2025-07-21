#!/bin/bash

# Script to consolidate language-specific summary files into professional recaps
# Removes agent references and creates clean, professional documentation

MAIN_SUMMARIES_DIR="summaries"
TODAY_DATE="07-20-2025"

# Function to clean and professionalize content
clean_content() {
    local content="$1"
    
    # Remove agent references and technical jargon
    content=$(echo "$content" | sed 's/agent-[a-z0-9]*//gi')
    content=$(echo "$content" | sed 's/a[0-9]//gi')
    content=$(echo "$content" | sed 's/b[0-9]//gi')
    content=$(echo "$content" | sed 's/g[0-9]*//gi')
    content=$(echo "$content" | sed 's/Agent [A-Z][0-9]//gi')
    content=$(echo "$content" | sed 's/Agent-[A-Z][0-9]//gi')
    content=$(echo "$content" | sed 's/AGENT-[A-Z][0-9]//gi')
    content=$(echo "$content" | sed 's/agent [a-z0-9]*//gi')
    
    # Clean up extra spaces and formatting
    content=$(echo "$content" | sed 's/  */ /g')
    content=$(echo "$content" | sed 's/^ *//g')
    content=$(echo "$content" | sed 's/ *$//g')
    
    echo "$content"
}

# Function to extract meaningful content from JSON
extract_json_content() {
    local json_content="$1"
    
    # Try to extract meaningful fields from JSON
    local title=$(echo "$json_content" | jq -r '.title // .name // .summary // .description // empty' 2>/dev/null)
    local status=$(echo "$json_content" | jq -r '.status // .state // empty' 2>/dev/null)
    local progress=$(echo "$json_content" | jq -r '.progress // .completion // empty' 2>/dev/null)
    local notes=$(echo "$json_content" | jq -r '.notes // .description // .details // empty' 2>/dev/null)
    
    local extracted=""
    [ -n "$title" ] && [ "$title" != "null" ] && extracted+="**Title**: $title\n\n"
    [ -n "$status" ] && [ "$status" != "null" ] && extracted+="**Status**: $status\n\n"
    [ -n "$progress" ] && [ "$progress" != "null" ] && extracted+="**Progress**: $progress\n\n"
    [ -n "$notes" ] && [ "$notes" != "null" ] && extracted+="**Notes**: $notes\n\n"
    
    echo "$extracted"
}

# Function to consolidate language files
consolidate_language() {
    local language=$1
    local language_name=$2
    
    echo "📝 Consolidating $language_name files..."
    
    # Generate hash for the consolidated file
    local timestamp=$(date +%s)
    local hash=$(echo "$language-$timestamp" | md5sum | cut -d' ' -f1 | head -c 8)
    local output_file="${language}-recap-7-20-${hash}.md"
    
    # Create the consolidated file
    cat > "$MAIN_SUMMARIES_DIR/$output_file" << EOF
# $language_name SDK Development Recap - July 20, 2025

**Language**: $language_name  
**Consolidation Date**: $TODAY_DATE  
**File Hash**: \`$hash\`

## Overview

This document consolidates all development summaries and progress reports for the $language_name SDK implementation within the TuskLang ecosystem. The following sections provide a comprehensive overview of completed work, current status, and key achievements.

## Development Progress

EOF
    
    # Process all files for this language
    local file_count=0
    for file in "$MAIN_SUMMARIES_DIR"/${language}-07-20-2025-*.md; do
        if [ -f "$file" ]; then
            ((file_count++))
            local filename=$(basename "$file")
            local base_name=$(echo "$filename" | sed "s/${language}-07-20-2025-//" | sed 's/-[a-f0-9]*\.md$//')
            
            echo "Processing: $filename"
            
            # Extract content from the markdown file
            local content=$(cat "$file")
            
            # Extract JSON content if it exists
            local json_start=$(echo "$content" | grep -n '```json' | head -1 | cut -d: -f1)
            if [ -n "$json_start" ]; then
                local json_end=$(echo "$content" | grep -n '```' | tail -1 | cut -d: -f1)
                if [ -n "$json_end" ] && [ "$json_end" -gt "$json_start" ]; then
                    local json_content=$(echo "$content" | sed -n "$((json_start+1)),$((json_end-1))p")
                    local extracted=$(extract_json_content "$json_content")
                    if [ -n "$extracted" ]; then
                        content="$extracted"
                    fi
                fi
            fi
            
            # Clean the content
            content=$(clean_content "$content")
            
            # Add to consolidated file
            cat >> "$MAIN_SUMMARIES_DIR/$output_file" << EOF

### $base_name

$content

---
EOF
        fi
    done
    
    # Add summary section
    cat >> "$MAIN_SUMMARIES_DIR/$output_file" << EOF

## Summary

The $language_name SDK implementation has made significant progress with $file_count distinct development areas documented. The work encompasses core functionality, testing frameworks, performance optimizations, and integration capabilities.

### Key Achievements

- **Comprehensive Coverage**: Multiple development areas have been addressed
- **Documentation Quality**: Detailed progress tracking and status reporting
- **Technical Depth**: Implementation covers both basic and advanced features
- **Integration Ready**: Framework prepared for ecosystem integration

### Next Steps

The $language_name SDK is positioned for continued development with a solid foundation of documented progress and clear development pathways.

---

*This document was automatically generated from individual development summaries on $TODAY_DATE.*
EOF
    
    echo "✅ Created: $output_file ($file_count files consolidated)"
}

# Main execution
echo "🚀 Starting language summary consolidation..."
echo "📁 Target directory: $MAIN_SUMMARIES_DIR"
echo "📅 Date: $TODAY_DATE"
echo ""

# Define languages and their display names
declare -A languages=(
    ["bash"]="Bash"
    ["csharp"]="C#"
    ["go"]="Go"
    ["java"]="Java"
    ["javascript"]="JavaScript"
    ["php"]="PHP"
    ["python"]="Python"
    ["reference"]="Reference"
    ["ruby"]="Ruby"
    ["rust"]="Rust"
)

# Process each language
for lang in "${!languages[@]}"; do
    # Check if there are files for this language
    if ls "$MAIN_SUMMARIES_DIR"/${lang}-07-20-2025-*.md 1> /dev/null 2>&1; then
        consolidate_language "$lang" "${languages[$lang]}"
        echo ""
    else
        echo "⚠️  No files found for $lang"
    fi
done

echo "🎉 Language consolidation completed!"
echo "📁 All consolidated files are in: $MAIN_SUMMARIES_DIR" 