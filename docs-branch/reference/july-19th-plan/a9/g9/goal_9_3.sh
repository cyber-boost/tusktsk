#!/bin/bash

# Filter Operator Implementation
# Provides data filtering and transformation

# Global variables
FILTER_CASE_SENSITIVE="true"
FILTER_REGEX_MODE="false"
FILTER_INVERT_MATCH="false"
FILTER_MAX_RESULTS="1000"
FILTER_TIMEOUT="60"

# Initialize Filter operator
filter_init() {
    local case_sensitive="$1"
    local regex_mode="$2"
    local invert_match="$3"
    local max_results="$4"
    local timeout="$5"
    
    FILTER_CASE_SENSITIVE="${case_sensitive:-true}"
    FILTER_REGEX_MODE="${regex_mode:-false}"
    FILTER_INVERT_MATCH="${invert_match:-false}"
    FILTER_MAX_RESULTS="${max_results:-1000}"
    FILTER_TIMEOUT="${timeout:-60}"
    
    echo "{\"status\":\"success\",\"message\":\"Filter operator initialized\",\"case_sensitive\":\"$FILTER_CASE_SENSITIVE\",\"regex_mode\":\"$FILTER_REGEX_MODE\",\"invert_match\":\"$FILTER_INVERT_MATCH\",\"max_results\":\"$FILTER_MAX_RESULTS\"}"
}

# Filter text data
filter_text() {
    local data="$1"
    local pattern="$2"
    local transform="$3"
    
    if [[ -z "$data" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Data is required\"}"
        return 1
    fi
    
    if [[ -z "$pattern" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Pattern is required\"}"
        return 1
    fi
    
    local results=()
    local count=0
    local start_time=$(date +%s)
    
    # Split data into lines
    IFS=$'\n' read -ra DATA_ARRAY <<< "$data"
    
    for line in "${DATA_ARRAY[@]}"; do
        # Check timeout
        local current_time=$(date +%s)
        if [[ $((current_time - start_time)) -gt $FILTER_TIMEOUT ]]; then
            echo "{\"status\":\"error\",\"message\":\"Filter timeout exceeded\",\"processed\":$count,\"timeout\":$FILTER_TIMEOUT}"
            return 1
        fi
        
        # Check max results
        if [[ $count -ge $FILTER_MAX_RESULTS ]]; then
            echo "{\"status\":\"warning\",\"message\":\"Filter max results reached\",\"processed\":$count,\"max_results\":$FILTER_MAX_RESULTS}"
            break
        fi
        
        local match=false
        
        if [[ "$FILTER_REGEX_MODE" == "true" ]]; then
            # Regex matching
            if [[ "$FILTER_CASE_SENSITIVE" == "true" ]]; then
                if [[ "$line" =~ $pattern ]]; then
                    match=true
                fi
            else
                if [[ "${line,,}" =~ ${pattern,,} ]]; then
                    match=true
                fi
            fi
        else
            # Simple string matching
            if [[ "$FILTER_CASE_SENSITIVE" == "true" ]]; then
                if [[ "$line" == *"$pattern"* ]]; then
                    match=true
                fi
            else
                if [[ "${line,,}" == *"${pattern,,}"* ]]; then
                    match=true
                fi
            fi
        fi
        
        # Apply invert match logic
        if [[ "$FILTER_INVERT_MATCH" == "true" ]]; then
            match=$([[ "$match" == "true" ]] && echo "false" || echo "true")
        fi
        
        if [[ "$match" == "true" ]]; then
            local result="$line"
            
            # Apply transformation if specified
            if [[ -n "$transform" ]]; then
                case "$transform" in
                    "uppercase")
                        result="${line^^}"
                        ;;
                    "lowercase")
                        result="${line,,}"
                        ;;
                    "trim")
                        result=$(echo "$line" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
                        ;;
                    "reverse")
                        result=$(echo "$line" | rev)
                        ;;
                    "length")
                        result="${#line}"
                        ;;
                    "word_count")
                        result=$(echo "$line" | wc -w)
                        ;;
                    "line_number")
                        result="$count"
                        ;;
                    *)
                        # Custom transformation
                        result=$(echo "$line" | eval "$transform" 2>/dev/null || echo "$line")
                        ;;
                esac
            fi
            
            results+=("$result")
            ((count++))
        fi
    done
    
    echo "{\"status\":\"success\",\"message\":\"Filter completed\",\"total_processed\":${#DATA_ARRAY[@]},\"matches\":$count,\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Filter file content
filter_file() {
    local file_path="$1"
    local pattern="$2"
    local transform="$3"
    local output_file="$4"
    
    if [[ -z "$file_path" || -z "$pattern" ]]; then
        echo "{\"status\":\"error\",\"message\":\"File path and pattern are required\"}"
        return 1
    fi
    
    if [[ ! -f "$file_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"File not found: $file_path\"}"
        return 1
    fi
    
    local results=()
    local count=0
    local start_time=$(date +%s)
    
    # Read file line by line
    while IFS= read -r line; do
        # Check timeout
        local current_time=$(date +%s)
        if [[ $((current_time - start_time)) -gt $FILTER_TIMEOUT ]]; then
            echo "{\"status\":\"error\",\"message\":\"Filter timeout exceeded\",\"processed\":$count,\"timeout\":$FILTER_TIMEOUT}"
            return 1
        fi
        
        # Check max results
        if [[ $count -ge $FILTER_MAX_RESULTS ]]; then
            echo "{\"status\":\"warning\",\"message\":\"Filter max results reached\",\"processed\":$count,\"max_results\":$FILTER_MAX_RESULTS}"
            break
        fi
        
        local match=false
        
        if [[ "$FILTER_REGEX_MODE" == "true" ]]; then
            # Regex matching
            if [[ "$FILTER_CASE_SENSITIVE" == "true" ]]; then
                if [[ "$line" =~ $pattern ]]; then
                    match=true
                fi
            else
                if [[ "${line,,}" =~ ${pattern,,} ]]; then
                    match=true
                fi
            fi
        else
            # Simple string matching
            if [[ "$FILTER_CASE_SENSITIVE" == "true" ]]; then
                if [[ "$line" == *"$pattern"* ]]; then
                    match=true
                fi
            else
                if [[ "${line,,}" == *"${pattern,,}"* ]]; then
                    match=true
                fi
            fi
        fi
        
        # Apply invert match logic
        if [[ "$FILTER_INVERT_MATCH" == "true" ]]; then
            match=$([[ "$match" == "true" ]] && echo "false" || echo "true")
        fi
        
        if [[ "$match" == "true" ]]; then
            local result="$line"
            
            # Apply transformation if specified
            if [[ -n "$transform" ]]; then
                case "$transform" in
                    "uppercase")
                        result="${line^^}"
                        ;;
                    "lowercase")
                        result="${line,,}"
                        ;;
                    "trim")
                        result=$(echo "$line" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
                        ;;
                    "reverse")
                        result=$(echo "$line" | rev)
                        ;;
                    "length")
                        result="${#line}"
                        ;;
                    "word_count")
                        result=$(echo "$line" | wc -w)
                        ;;
                    "line_number")
                        result="$count"
                        ;;
                    *)
                        # Custom transformation
                        result=$(echo "$line" | eval "$transform" 2>/dev/null || echo "$line")
                        ;;
                esac
            fi
            
            results+=("$result")
            ((count++))
        fi
    done < "$file_path"
    
    # Write to output file if specified
    if [[ -n "$output_file" ]]; then
        printf '%s\n' "${results[@]}" > "$output_file"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Filter completed\",\"matches\":$count,\"output_file\":\"$output_file\",\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Filter command output
filter_command() {
    local command="$1"
    local pattern="$2"
    local transform="$3"
    
    if [[ -z "$command" || -z "$pattern" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Command and pattern are required\"}"
        return 1
    fi
    
    local results=()
    local count=0
    local start_time=$(date +%s)
    
    # Execute command and filter output
    while IFS= read -r line; do
        # Check timeout
        local current_time=$(date +%s)
        if [[ $((current_time - start_time)) -gt $FILTER_TIMEOUT ]]; then
            echo "{\"status\":\"error\",\"message\":\"Filter timeout exceeded\",\"processed\":$count,\"timeout\":$FILTER_TIMEOUT}"
            return 1
        fi
        
        # Check max results
        if [[ $count -ge $FILTER_MAX_RESULTS ]]; then
            echo "{\"status\":\"warning\",\"message\":\"Filter max results reached\",\"processed\":$count,\"max_results\":$FILTER_MAX_RESULTS}"
            break
        fi
        
        local match=false
        
        if [[ "$FILTER_REGEX_MODE" == "true" ]]; then
            # Regex matching
            if [[ "$FILTER_CASE_SENSITIVE" == "true" ]]; then
                if [[ "$line" =~ $pattern ]]; then
                    match=true
                fi
            else
                if [[ "${line,,}" =~ ${pattern,,} ]]; then
                    match=true
                fi
            fi
        else
            # Simple string matching
            if [[ "$FILTER_CASE_SENSITIVE" == "true" ]]; then
                if [[ "$line" == *"$pattern"* ]]; then
                    match=true
                fi
            else
                if [[ "${line,,}" == *"${pattern,,}"* ]]; then
                    match=true
                fi
            fi
        fi
        
        # Apply invert match logic
        if [[ "$FILTER_INVERT_MATCH" == "true" ]]; then
            match=$([[ "$match" == "true" ]] && echo "false" || echo "true")
        fi
        
        if [[ "$match" == "true" ]]; then
            local result="$line"
            
            # Apply transformation if specified
            if [[ -n "$transform" ]]; then
                case "$transform" in
                    "uppercase")
                        result="${line^^}"
                        ;;
                    "lowercase")
                        result="${line,,}"
                        ;;
                    "trim")
                        result=$(echo "$line" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
                        ;;
                    "reverse")
                        result=$(echo "$line" | rev)
                        ;;
                    "length")
                        result="${#line}"
                        ;;
                    "word_count")
                        result=$(echo "$line" | wc -w)
                        ;;
                    "line_number")
                        result="$count"
                        ;;
                    *)
                        # Custom transformation
                        result=$(echo "$line" | eval "$transform" 2>/dev/null || echo "$line")
                        ;;
                esac
            fi
            
            results+=("$result")
            ((count++))
        fi
    done < <(eval "$command" 2>/dev/null)
    
    echo "{\"status\":\"success\",\"message\":\"Filter completed\",\"matches\":$count,\"command\":\"$command\",\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Advanced filtering with multiple conditions
filter_advanced() {
    local data="$1"
    local conditions="$2"
    local transform="$3"
    
    if [[ -z "$data" || -z "$conditions" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Data and conditions are required\"}"
        return 1
    fi
    
    local results=()
    local count=0
    local start_time=$(date +%s)
    
    # Parse conditions (format: condition1:operator1,condition2:operator2,...)
    IFS=',' read -ra CONDITION_ARRAY <<< "$conditions"
    
    # Split data into lines
    IFS=$'\n' read -ra DATA_ARRAY <<< "$data"
    
    for line in "${DATA_ARRAY[@]}"; do
        # Check timeout
        local current_time=$(date +%s)
        if [[ $((current_time - start_time)) -gt $FILTER_TIMEOUT ]]; then
            echo "{\"status\":\"error\",\"message\":\"Filter timeout exceeded\",\"processed\":$count,\"timeout\":$FILTER_TIMEOUT}"
            return 1
        fi
        
        # Check max results
        if [[ $count -ge $FILTER_MAX_RESULTS ]]; then
            echo "{\"status\":\"warning\",\"message\":\"Filter max results reached\",\"processed\":$count,\"max_results\":$FILTER_MAX_RESULTS}"
            break
        fi
        
        local all_conditions_met=true
        
        # Check each condition
        for condition_item in "${CONDITION_ARRAY[@]}"; do
            local condition=$(echo "$condition_item" | cut -d':' -f1)
            local operator=$(echo "$condition_item" | cut -d':' -f2)
            
            # Remove quotes if present
            condition=$(echo "$condition" | sed 's/^"//;s/"$//')
            operator=$(echo "$operator" | sed 's/^"//;s/"$//')
            
            local condition_met=false
            
            case "$operator" in
                "contains")
                    if [[ "$line" == *"$condition"* ]]; then
                        condition_met=true
                    fi
                    ;;
                "not_contains")
                    if [[ "$line" != *"$condition"* ]]; then
                        condition_met=true
                    fi
                    ;;
                "starts_with")
                    if [[ "$line" == "$condition"* ]]; then
                        condition_met=true
                    fi
                    ;;
                "ends_with")
                    if [[ "$line" == *"$condition" ]]; then
                        condition_met=true
                    fi
                    ;;
                "equals")
                    if [[ "$line" == "$condition" ]]; then
                        condition_met=true
                    fi
                    ;;
                "regex")
                    if [[ "$line" =~ $condition ]]; then
                        condition_met=true
                    fi
                    ;;
                "length_gt")
                    if [[ ${#line} -gt $condition ]]; then
                        condition_met=true
                    fi
                    ;;
                "length_lt")
                    if [[ ${#line} -lt $condition ]]; then
                        condition_met=true
                    fi
                    ;;
                "word_count_gt")
                    local word_count=$(echo "$line" | wc -w)
                    if [[ $word_count -gt $condition ]]; then
                        condition_met=true
                    fi
                    ;;
                "word_count_lt")
                    local word_count=$(echo "$line" | wc -w)
                    if [[ $word_count -lt $condition ]]; then
                        condition_met=true
                    fi
                    ;;
                *)
                    # Default to contains
                    if [[ "$line" == *"$condition"* ]]; then
                        condition_met=true
                    fi
                    ;;
            esac
            
            if [[ "$condition_met" == false ]]; then
                all_conditions_met=false
                break
            fi
        done
        
        if [[ "$all_conditions_met" == true ]]; then
            local result="$line"
            
            # Apply transformation if specified
            if [[ -n "$transform" ]]; then
                case "$transform" in
                    "uppercase")
                        result="${line^^}"
                        ;;
                    "lowercase")
                        result="${line,,}"
                        ;;
                    "trim")
                        result=$(echo "$line" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
                        ;;
                    "reverse")
                        result=$(echo "$line" | rev)
                        ;;
                    "length")
                        result="${#line}"
                        ;;
                    "word_count")
                        result=$(echo "$line" | wc -w)
                        ;;
                    "line_number")
                        result="$count"
                        ;;
                    *)
                        # Custom transformation
                        result=$(echo "$line" | eval "$transform" 2>/dev/null || echo "$line")
                        ;;
                esac
            fi
            
            results+=("$result")
            ((count++))
        fi
    done
    
    echo "{\"status\":\"success\",\"message\":\"Advanced filter completed\",\"total_processed\":${#DATA_ARRAY[@]},\"matches\":$count,\"conditions\":${#CONDITION_ARRAY[@]},\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Get filter configuration
filter_config() {
    echo "{\"status\":\"success\",\"config\":{\"case_sensitive\":\"$FILTER_CASE_SENSITIVE\",\"regex_mode\":\"$FILTER_REGEX_MODE\",\"invert_match\":\"$FILTER_INVERT_MATCH\",\"max_results\":\"$FILTER_MAX_RESULTS\",\"timeout\":\"$FILTER_TIMEOUT\"}}"
}

# Main Filter operator function
execute_filter() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local case_sensitive=$(echo "$params" | grep -o 'case_sensitive=[^,]*' | cut -d'=' -f2)
            local regex_mode=$(echo "$params" | grep -o 'regex_mode=[^,]*' | cut -d'=' -f2)
            local invert_match=$(echo "$params" | grep -o 'invert_match=[^,]*' | cut -d'=' -f2)
            local max_results=$(echo "$params" | grep -o 'max_results=[^,]*' | cut -d'=' -f2)
            local timeout=$(echo "$params" | grep -o 'timeout=[^,]*' | cut -d'=' -f2)
            filter_init "$case_sensitive" "$regex_mode" "$invert_match" "$max_results" "$timeout"
            ;;
        "text")
            local data=$(echo "$params" | grep -o 'data=[^,]*' | cut -d'=' -f2)
            local pattern=$(echo "$params" | grep -o 'pattern=[^,]*' | cut -d'=' -f2)
            local transform=$(echo "$params" | grep -o 'transform=[^,]*' | cut -d'=' -f2)
            filter_text "$data" "$pattern" "$transform"
            ;;
        "file")
            local file_path=$(echo "$params" | grep -o 'file_path=[^,]*' | cut -d'=' -f2)
            local pattern=$(echo "$params" | grep -o 'pattern=[^,]*' | cut -d'=' -f2)
            local transform=$(echo "$params" | grep -o 'transform=[^,]*' | cut -d'=' -f2)
            local output_file=$(echo "$params" | grep -o 'output_file=[^,]*' | cut -d'=' -f2)
            filter_file "$file_path" "$pattern" "$transform" "$output_file"
            ;;
        "command")
            local command=$(echo "$params" | grep -o 'command=[^,]*' | cut -d'=' -f2)
            local pattern=$(echo "$params" | grep -o 'pattern=[^,]*' | cut -d'=' -f2)
            local transform=$(echo "$params" | grep -o 'transform=[^,]*' | cut -d'=' -f2)
            filter_command "$command" "$pattern" "$transform"
            ;;
        "advanced")
            local data=$(echo "$params" | grep -o 'data=[^,]*' | cut -d'=' -f2)
            local conditions=$(echo "$params" | grep -o 'conditions=[^,]*' | cut -d'=' -f2)
            local transform=$(echo "$params" | grep -o 'transform=[^,]*' | cut -d'=' -f2)
            filter_advanced "$data" "$conditions" "$transform"
            ;;
        "config")
            filter_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, text, file, command, advanced, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_filter filter_init filter_text filter_file filter_command filter_advanced filter_config 