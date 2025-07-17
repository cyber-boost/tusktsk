#!/usr/bin/env bash

# TuskLang CLI Output Utilities
# =============================
# Consistent logging and output formatting

set -euo pipefail

# Load helpers
source "$(dirname "${BASH_SOURCE[0]}")/helpers.sh"

##
# Log a message with timestamp and level
#
# @param $1 Log level (INFO, WARN, ERROR, DEBUG)
# @param $2 Message
#
log_message() {
    local level="$1"
    local message="$2"
    local timestamp
    timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    
    if [[ $TSK_QUIET -eq 0 ]]; then
        case "$level" in
            "INFO")
                echo -e "${COLOR_BLUE}[$timestamp] INFO:${COLOR_RESET} $message"
                ;;
            "WARN")
                echo -e "${COLOR_YELLOW}[$timestamp] WARN:${COLOR_RESET} $message"
                ;;
            "ERROR")
                echo -e "${COLOR_RED}[$timestamp] ERROR:${COLOR_RESET} $message" >&2
                ;;
            "DEBUG")
                if [[ $TSK_VERBOSE -eq 1 ]]; then
                    echo -e "${COLOR_CYAN}[$timestamp] DEBUG:${COLOR_RESET} $message"
                fi
                ;;
        esac
    fi
}

##
# Log info message
#
# @param $* Message
#
log_info() {
    log_message "INFO" "$*"
}

##
# Log warning message
#
# @param $* Message
#
log_warn() {
    log_message "WARN" "$*"
}

##
# Log error message
#
# @param $* Message
#
log_error() {
    log_message "ERROR" "$*"
    TSK_EXIT_CODE=1
}

##
# Log debug message
#
# @param $* Message
#
log_debug() {
    log_message "DEBUG" "$*"
}

##
# Print success message with status symbol
#
# @param $* Message
#
print_success() {
    if [[ $TSK_QUIET -eq 0 ]]; then
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            echo "{\"status\": \"success\", \"message\": \"$*\"}"
        else
            echo -e "${STATUS_SUCCESS} $*"
        fi
    fi
}

##
# Print error message with status symbol
#
# @param $* Message
#
print_error() {
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        echo "{\"status\": \"error\", \"message\": \"$*\"}" >&2
    else
        echo -e "${STATUS_ERROR} $*" >&2
    fi
    TSK_EXIT_CODE=1
}

##
# Print warning message with status symbol
#
# @param $* Message
#
print_warning() {
    if [[ $TSK_QUIET -eq 0 ]]; then
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            echo "{\"status\": \"warning\", \"message\": \"$*\"}"
        else
            echo -e "${STATUS_WARNING} $*"
        fi
    fi
}

##
# Print info message with status symbol
#
# @param $* Message
#
print_info() {
    if [[ $TSK_QUIET -eq 0 ]]; then
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            echo "{\"status\": \"info\", \"message\": \"$*\"}"
        else
            echo -e "${STATUS_INFO} $*"
        fi
    fi
}

##
# Print running message with status symbol
#
# @param $* Message
#
print_running() {
    if [[ $TSK_QUIET -eq 0 ]]; then
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            echo "{\"status\": \"running\", \"message\": \"$*\"}"
        else
            echo -e "${STATUS_RUNNING} $*"
        fi
    fi
}

##
# Print a table with headers
#
# @param $1 Headers (comma-separated)
# @param $2 Data rows (one per line, comma-separated)
#
print_table() {
    local headers="$1"
    local data="$2"
    
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        print_json_table "$headers" "$data"
    else
        print_text_table "$headers" "$data"
    fi
}

##
# Print text table
#
# @param $1 Headers
# @param $2 Data
#
print_text_table() {
    local headers="$1"
    local data="$2"
    
    # Parse headers
    IFS=',' read -ra header_array <<< "$headers"
    
    # Calculate column widths
    local -a widths
    for i in "${!header_array[@]}"; do
        widths[$i]=${#header_array[$i]}
    done
    
    # Check data rows for max widths
    while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            IFS=',' read -ra row <<< "$line"
            for i in "${!row[@]}"; do
                if [[ $i -lt ${#widths[@]} ]]; then
                    local cell_length=${#row[$i]}
                    if [[ $cell_length -gt ${widths[$i]} ]]; then
                        widths[$i]=$cell_length
                    fi
                fi
            done
        fi
    done <<< "$data"
    
    # Print header
    echo -n "|"
    for i in "${!header_array[@]}"; do
        printf " %-${widths[$i]}s |" "${header_array[$i]}"
    done
    echo
    
    # Print separator
    echo -n "|"
    for i in "${!header_array[@]}"; do
        printf " %-${widths[$i]}s |" "$(printf '%*s' "${widths[$i]}" '' | tr ' ' '-')"
    done
    echo
    
    # Print data rows
    while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            echo -n "|"
            IFS=',' read -ra row <<< "$line"
            for i in "${!row[@]}"; do
                if [[ $i -lt ${#widths[@]} ]]; then
                    printf " %-${widths[$i]}s |" "${row[$i]}"
                fi
            done
            echo
        fi
    done <<< "$data"
}

##
# Print JSON table
#
# @param $1 Headers
# @param $2 Data
#
print_json_table() {
    local headers="$1"
    local data="$2"
    
    # Parse headers
    IFS=',' read -ra header_array <<< "$headers"
    
    echo "["
    local first_row=true
    
    while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            if [[ "$first_row" == "true" ]]; then
                first_row=false
            else
                echo ","
            fi
            
            echo -n "  {"
            IFS=',' read -ra row <<< "$line"
            local first_cell=true
            
            for i in "${!row[@]}"; do
                if [[ $i -lt ${#header_array[@]} ]]; then
                    if [[ "$first_cell" == "true" ]]; then
                        first_cell=false
                    else
                        echo -n ","
                    fi
                    echo -n " \"${header_array[$i]}\": \"${row[$i]}\""
                fi
            done
            echo -n " }"
        fi
    done <<< "$data"
    echo
    echo "]"
}

##
# Print progress bar
#
# @param $1 Current value
# @param $2 Total value
# @param $3 Width (default: 50)
# @param $4 Label (optional)
#
print_progress() {
    local current="$1"
    local total="$2"
    local width="${3:-50}"
    local label="${4:-}"
    
    if [[ $TSK_QUIET -eq 0 ]]; then
        local percentage
        percentage=$((current * 100 / total))
        local filled
        filled=$((current * width / total))
        local empty
        empty=$((width - filled))
        
        local bar=""
        for ((i=0; i<filled; i++)); do
            bar+="█"
        done
        for ((i=0; i<empty; i++)); do
            bar+="░"
        done
        
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            echo "{\"progress\": {\"current\": $current, \"total\": $total, \"percentage\": $percentage, \"label\": \"$label\"}}"
        else
            printf "\r[%-${width}s] %d%% %s" "$bar" "$percentage" "$label"
            if [[ $current -eq $total ]]; then
                echo
            fi
        fi
    fi
}

##
# Print a key-value pair
#
# @param $1 Key
# @param $2 Value
#
print_kv() {
    local key="$1"
    local value="$2"
    
    if [[ $TSK_QUIET -eq 0 ]]; then
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            echo "{\"$key\": \"$value\"}"
        else
            echo "$key: $value"
        fi
    fi
}

##
# Print a section header
#
# @param $1 Section title
#
print_section() {
    local title="$1"
    
    if [[ $TSK_QUIET -eq 0 ]]; then
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            echo "{\"section\": \"$title\"}"
        else
            echo
            echo "=== $title ==="
            echo
        fi
    fi
}

##
# Print a subsection header
#
# @param $1 Subsection title
#
print_subsection() {
    local title="$1"
    
    if [[ $TSK_QUIET -eq 0 ]]; then
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            echo "{\"subsection\": \"$title\"}"
        else
            echo
            echo "--- $title ---"
            echo
        fi
    fi
}

##
# Print a list of items
#
# @param $1 Title
# @param $2 Items (one per line)
#
print_list() {
    local title="$1"
    local items="$2"
    
    if [[ $TSK_QUIET -eq 0 ]]; then
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            echo "{\"$title\": ["
            local first=true
            while IFS= read -r item; do
                if [[ -n "$item" ]]; then
                    if [[ "$first" == "true" ]]; then
                        first=false
                    else
                        echo ","
                    fi
                    echo -n "    \"$item\""
                fi
            done <<< "$items"
            echo
            echo "  ]}"
        else
            echo "$title:"
            while IFS= read -r item; do
                if [[ -n "$item" ]]; then
                    echo "  • $item"
                fi
            done <<< "$items"
        fi
    fi
}

##
# Print a code block
#
# @param $1 Language (optional)
# @param $2 Code content
#
print_code() {
    local language="${1:-}"
    local code="$2"
    if [[ $TSK_QUIET -eq 0 ]]; then
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            echo "{\"code\": {\"language\": \"$language\", \"content\": \"$code\"}}"
        else
            if [[ -n "$language" ]]; then
                echo "====CODE ($language)===="
            else
                echo "====CODE===="
            fi
            echo "$code"
            echo "====ENDCODE===="
        fi
    fi
}

##
# Print a summary with counts
#
# @param $1 Title
# @param $2 Success count
# @param $3 Error count
# @param $4 Warning count (optional)
#
print_summary() {
    local title="$1"
    local success="$2"
    local errors="$3"
    local warnings="${4:-0}"
    if [[ $TSK_QUIET -eq 0 ]]; then
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            echo "{"
            echo "  \"summary\": {"
            echo "    \"title\": \"$title\"," 
            echo "    \"success\": $success,"
            echo "    \"errors\": $errors,"
            echo "    \"warnings\": $warnings,"
            echo "    \"total\": $((success + errors + warnings))"
            echo "  }"
            echo "}"
        else
            echo
            echo "Summary: $title"
            echo "  ${STATUS_SUCCESS} Success: $success"
            if [[ $warnings -gt 0 ]]; then
                echo "  ${STATUS_WARNING} Warnings: $warnings"
            fi
            if [[ $errors -gt 0 ]]; then
                echo "  ${STATUS_ERROR} Errors: $errors"
            fi
            echo "  Total: $((success + errors + warnings))"
        fi
    fi
}

##
# Print a result object
#
# @param $1 Status (success, error, warning)
# @param $2 Message
# @param $3 Data (optional JSON string)
#
print_result() {
    local status="$1"
    local message="$2"
    local data="${3:-}"
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        if [[ -n "$data" ]]; then
            echo "{\"status\": \"$status\", \"message\": \"$message\", \"data\": $data}"
        else
            echo "{\"status\": \"$status\", \"message\": \"$message\"}"
        fi
    else
        case "$status" in
            "success")
                print_success "$message"
                ;;
            "error")
                print_error "$message"
                ;;
            "warning")
                print_warning "$message"
                ;;
            *)
                print_info "$message"
                ;;
        esac
        if [[ -n "$data" ]]; then
            echo "$data"
        fi
    fi
}

##
# Clear the current line
#
clear_line() {
    if [[ $TSK_QUIET -eq 0 && $TSK_JSON_OUTPUT -eq 0 ]]; then
        printf "\r%*s\r" "$(tput cols)" ""
    fi
}

##
# Print a spinner animation
#
# @param $1 Message
#
print_spinner() {
    local message="$1"
    local spinner_chars=("⠋" "⠙" "⠹" "⠸" "⠼" "⠴" "⠦" "⠧" "⠇" "⠏")
    local i=0
    if [[ $TSK_QUIET -eq 0 && $TSK_JSON_OUTPUT -eq 0 ]]; then
        while true; do
            printf "\r%s %s" "${spinner_chars[$i]}" "$message"
            sleep 0.1
            i=$(((i + 1) % ${#spinner_chars[@]}))
        done
    fi
} 