#!/bin/bash

# Script to find JSON summary files in SDK folders, convert to markdown, and move to main summaries
# Searches for files with "summary" or "summaries" in the name (including misspellings)

MAIN_SUMMARIES_DIR="summaries"
TODAY_DATE="07-20-2025"

# Function to convert JSON to markdown
convert_json_to_markdown() {
    local json_file=$1
    local folder_name=$2
    
    # Generate hash of the file content for unique naming
    local file_hash=$(md5sum "$json_file" | cut -d' ' -f1 | head -c 8)
    
    # Create markdown filename
    local base_name=$(basename "$json_file" .json)
    local md_filename="${folder_name}-${TODAY_DATE}-${base_name}-${file_hash}.md"
    
    echo "Converting: $json_file -> $MAIN_SUMMARIES_DIR/$md_filename"
    
    # Create markdown content
    cat > "$MAIN_SUMMARIES_DIR/$md_filename" << EOF
# JSON Summary Conversion - ${base_name}

**Source File**: \`$json_file\`  
**Original Folder**: \`$folder_name\`  
**Conversion Date**: ${TODAY_DATE}  
**File Hash**: \`${file_hash}\`

## JSON Content

\`\`\`json
$(cat "$json_file")
\`\`\`

## Conversion Notes

This file was automatically converted from JSON format to Markdown for inclusion in the main summaries directory.

EOF
    
    echo "✅ Converted: $md_filename"
}

# Function to search and process JSON files
search_and_convert() {
    local sdk_folder=$1
    local folder_name=$(basename "$sdk_folder")
    
    echo "🔍 Searching for JSON summary files in $folder_name..."
    
    # Search for JSON files with "summary" or "summaries" in the name
    # Using case-insensitive search and common misspellings
    local found_files=0
    
    # Search patterns for summary-related JSON files
    patterns=(
        "*summary*.json"
        "*summaries*.json"
        "*summery*.json"  # Common misspelling
        "*summry*.json"   # Common misspelling
        "*sumary*.json"   # Common misspelling
        "*summ*.json"     # Partial match
    )
    
    for pattern in "${patterns[@]}"; do
        while IFS= read -r -d '' file; do
            if [ -f "$file" ]; then
                echo "📄 Found JSON file: $file"
                convert_json_to_markdown "$file" "$folder_name"
                ((found_files++))
            fi
        done < <(find "$sdk_folder" -type f -iname "$pattern" -print0 2>/dev/null)
    done
    
    # Also search for files with "summary" in the content (first 1000 chars)
    echo "🔍 Searching for files with 'summary' in content..."
    while IFS= read -r -d '' file; do
        if [ -f "$file" ] && [[ "$file" == *.json ]]; then
            # Check if file contains "summary" in first 1000 characters
            if head -c 1000 "$file" 2>/dev/null | grep -qi "summary"; then
                echo "📄 Found JSON with summary content: $file"
                convert_json_to_markdown "$file" "$folder_name"
                ((found_files++))
            fi
        fi
    done < <(find "$sdk_folder" -type f -name "*.json" -print0 2>/dev/null)
    
    if [ $found_files -eq 0 ]; then
        echo "❌ No JSON summary files found in $folder_name"
    else
        echo "✅ Found and converted $found_files JSON files in $folder_name"
    fi
}

# Main execution
echo "🚀 Starting JSON summary file search and conversion..."
echo "📁 Target directory: $MAIN_SUMMARIES_DIR"
echo "📅 Date: $TODAY_DATE"
echo ""

# Ensure main summaries directory exists
mkdir -p "$MAIN_SUMMARIES_DIR"

# Process each SDK folder
total_converted=0
for sdk_folder in sdk/*/; do
    if [ -d "$sdk_folder" ]; then
        search_and_convert "$sdk_folder"
        echo ""
    fi
done

echo "🎉 JSON summary conversion completed!"
echo "📊 Total files processed: $total_converted"
echo "📁 All converted files are now in: $MAIN_SUMMARIES_DIR" 