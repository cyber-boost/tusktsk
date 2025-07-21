#!/bin/bash

# Goal 1.3 Implementation - Simple Data Processing Pipeline
# Priority: Low
# Description: Goal 3 for Bash agent a9 goal 1

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_1_3"
DATA_DIR="/tmp/goal_1_3_data"
INPUT_DIR="$DATA_DIR/input"
OUTPUT_DIR="$DATA_DIR/output"
PROCESSED_DIR="$DATA_DIR/processed"

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

# Data processing functions
create_sample_data() {
    log_info "Creating sample data for processing"
    mkdir -p "$INPUT_DIR"
    
    # Create sample CSV files
    for i in {1..3}; do
        cat > "$INPUT_DIR/data_$i.csv" << EOF
id,name,value,category
1,item_${i}_1,$((RANDOM % 100)),category_a
2,item_${i}_2,$((RANDOM % 100)),category_b
3,item_${i}_3,$((RANDOM % 100)),category_c
4,item_${i}_4,$((RANDOM % 100)),category_a
5,item_${i}_5,$((RANDOM % 100)),category_b
EOF
        log_success "Created data_$i.csv"
    done
    
    # Create sample text files
    for i in {1..2}; do
        cat > "$INPUT_DIR/text_$i.txt" << EOF
Sample text file $i
This is a sample text file for processing.
It contains multiple lines of text data.
The goal is to process this data and extract useful information.
EOF
        log_success "Created text_$i.txt"
    done
}

process_csv_files() {
    log_info "Processing CSV files"
    mkdir -p "$OUTPUT_DIR"
    
    local processed_count=0
    for csv_file in "$INPUT_DIR"/*.csv; do
        if [[ -f "$csv_file" ]]; then
            local filename=$(basename "$csv_file")
            local output_file="$OUTPUT_DIR/processed_$filename"
            
            # Process CSV: add timestamp and calculate totals
            {
                echo "timestamp,filename,original_id,name,value,category,processed_value"
                local total=0
                while IFS=',' read -r id name value category; do
                    if [[ "$id" != "id" ]]; then  # Skip header
                        local processed_value=$((value * 2))
                        total=$((total + value))
                        echo "$(date '+%Y-%m-%d %H:%M:%S'),$filename,$id,$name,$value,$category,$processed_value"
                    fi
                done < "$csv_file"
                echo "$(date '+%Y-%m-%d %H:%M:%S'),$filename,summary,total,$total,all,$((total * 2))"
            } > "$output_file"
            
            log_success "Processed $filename"
            ((processed_count++))
        fi
    done
    
    log_info "Processed $processed_count CSV files"
    return $processed_count
}

process_text_files() {
    log_info "Processing text files"
    
    local processed_count=0
    for text_file in "$INPUT_DIR"/*.txt; do
        if [[ -f "$text_file" ]]; then
            local filename=$(basename "$text_file")
            local output_file="$OUTPUT_DIR/processed_$filename"
            
            # Process text: count words, lines, and characters
            local line_count=$(wc -l < "$text_file")
            local word_count=$(wc -w < "$text_file")
            local char_count=$(wc -c < "$text_file")
            
            {
                echo "=== Text Analysis Report ==="
                echo "File: $filename"
                echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
                echo "Lines: $line_count"
                echo "Words: $word_count"
                echo "Characters: $char_count"
                echo ""
                echo "=== Original Content ==="
                cat "$text_file"
            } > "$output_file"
            
            log_success "Processed $filename"
            ((processed_count++))
        fi
    done
    
    log_info "Processed $processed_count text files"
    return $processed_count
}

generate_summary_report() {
    log_info "Generating summary report"
    
    local summary_file="$OUTPUT_DIR/processing_summary.txt"
    local csv_count=$(find "$OUTPUT_DIR" -name "processed_*.csv" | wc -l)
    local text_count=$(find "$OUTPUT_DIR" -name "processed_*.txt" | wc -l)
    local total_files=$((csv_count + text_count))
    
    {
        echo "=== Data Processing Summary Report ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Script: $SCRIPT_NAME"
        echo ""
        echo "Processing Statistics:"
        echo "- CSV files processed: $csv_count"
        echo "- Text files processed: $text_count"
        echo "- Total files processed: $total_files"
        echo ""
        echo "Output Directory: $OUTPUT_DIR"
        echo ""
        echo "Files Generated:"
        find "$OUTPUT_DIR" -type f -name "processed_*" | sort
    } > "$summary_file"
    
    log_success "Summary report generated: $summary_file"
}

cleanup_processed_files() {
    log_info "Moving processed files to archive"
    mkdir -p "$PROCESSED_DIR"
    
    # Move input files to processed directory
    if [[ -d "$INPUT_DIR" ]] && [[ "$(ls -A "$INPUT_DIR")" ]]; then
        mv "$INPUT_DIR"/* "$PROCESSED_DIR/"
        log_success "Input files moved to processed directory"
    else
        log_warning "No input files to move"
    fi
}

validate_results() {
    log_info "Validating processing results"
    
    local csv_files=$(find "$OUTPUT_DIR" -name "processed_*.csv" | wc -l)
    local text_files=$(find "$OUTPUT_DIR" -name "processed_*.txt" | wc -l)
    local summary_files=$(find "$OUTPUT_DIR" -name "processing_summary.txt" | wc -l)
    local total_files=$((csv_files + text_files + summary_files))
    
    if [[ $csv_files -eq 3 ]] && [[ $text_files -eq 2 ]] && [[ $summary_files -eq 1 ]]; then
        log_success "All expected output files created (CSV: $csv_files, Text: $text_files, Summary: $summary_files)"
        return 0
    else
        log_error "Expected 3 CSV, 2 text, and 1 summary file, but found CSV: $csv_files, Text: $text_files, Summary: $summary_files"
        return 1
    fi
}

# Main execution function
main() {
    log_info "Starting Goal 1.3 implementation"
    
    # Create sample data
    create_sample_data
    
    # Process CSV files
    local csv_processed=0
    if process_csv_files; then
        csv_processed=$?
    fi
    
    # Process text files
    local text_processed=0
    if process_text_files; then
        text_processed=$?
    fi
    
    # Generate summary report
    generate_summary_report
    
    # Validate results
    if validate_results; then
        log_success "Data processing validation passed"
    else
        log_error "Data processing validation failed"
        exit 1
    fi
    
    # Cleanup
    cleanup_processed_files
    
    log_info "Processing summary:"
    log_info "- CSV files processed: $csv_processed"
    log_info "- Text files processed: $text_processed"
    log_info "- Total files processed: $((csv_processed + text_processed))"
    
    log_success "Goal 1.3 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi 