#!/bin/bash

# Switch and For Operators Implementation
# Provides conditional and iterative control flow

# Global variables
SWITCH_DEFAULT_CASE=""
SWITCH_BREAK_ON_MATCH="true"
FOR_MAX_ITERATIONS="1000"
FOR_TIMEOUT="300"

# Initialize Switch operator
switch_init() {
    local default_case="$1"
    local break_on_match="$2"
    
    SWITCH_DEFAULT_CASE="${default_case:-}"
    SWITCH_BREAK_ON_MATCH="${break_on_match:-true}"
    
    echo "{\"status\":\"success\",\"message\":\"Switch operator initialized\",\"default_case\":\"$SWITCH_DEFAULT_CASE\",\"break_on_match\":\"$SWITCH_BREAK_ON_MATCH\"}"
}

# Execute switch statement
switch_execute() {
    local value="$1"
    local cases="$2"
    
    if [[ -z "$value" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Switch value is required\"}"
        return 1
    fi
    
    if [[ -z "$cases" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Switch cases are required\"}"
        return 1
    fi
    
    # Parse cases (format: case1:action1,case2:action2,...)
    local matched=false
    local result=""
    local executed_actions=()
    
    # Split cases by comma
    IFS=',' read -ra CASE_ARRAY <<< "$cases"
    
    for case_item in "${CASE_ARRAY[@]}"; do
        # Split case and action by colon
        local case_value=$(echo "$case_item" | cut -d':' -f1)
        local action=$(echo "$case_item" | cut -d':' -f2-)
        
        # Remove quotes if present
        case_value=$(echo "$case_value" | sed 's/^"//;s/"$//')
        action=$(echo "$action" | sed 's/^"//;s/"$//')
        
        if [[ "$case_value" == "$value" || "$case_value" == "default" ]]; then
            # Execute action
            if [[ -n "$action" ]]; then
                # Handle different action types
                case "$action" in
                    "echo"*)
                        result=$(eval "$action")
                        ;;
                    "return"*)
                        result=$(eval "$action")
                        ;;
                    "exit"*)
                        result=$(eval "$action")
                        ;;
                    *)
                        # Execute as command
                        result=$(eval "$action" 2>&1)
                        ;;
                esac
                
                executed_actions+=("$action")
                matched=true
                
                # Break if configured to do so
                if [[ "$SWITCH_BREAK_ON_MATCH" == "true" && "$case_value" != "default" ]]; then
                    break
                fi
            fi
        fi
    done
    
    # Execute default case if no match found
    if [[ "$matched" == false && -n "$SWITCH_DEFAULT_CASE" ]]; then
        result=$(eval "$SWITCH_DEFAULT_CASE" 2>&1)
        executed_actions+=("$SWITCH_DEFAULT_CASE")
        matched=true
    fi
    
    if [[ "$matched" == true ]]; then
        echo "{\"status\":\"success\",\"message\":\"Switch executed\",\"value\":\"$value\",\"matched\":true,\"actions\":[$(printf '"%s"' "${executed_actions[@]}" | tr '\n' ',' | sed 's/,$//')],\"result\":\"$result\"}"
    else
        echo "{\"status\":\"warning\",\"message\":\"No matching case found\",\"value\":\"$value\",\"matched\":false}"
    fi
}

# Switch with pattern matching
switch_pattern() {
    local value="$1"
    local patterns="$2"
    
    if [[ -z "$value" || -z "$patterns" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Value and patterns are required\"}"
        return 1
    fi
    
    # Parse patterns (format: pattern1:action1,pattern2:action2,...)
    local matched=false
    local result=""
    local executed_actions=()
    
    IFS=',' read -ra PATTERN_ARRAY <<< "$patterns"
    
    for pattern_item in "${PATTERN_ARRAY[@]}"; do
        local pattern=$(echo "$pattern_item" | cut -d':' -f1)
        local action=$(echo "$pattern_item" | cut -d':' -f2-)
        
        # Remove quotes if present
        pattern=$(echo "$pattern" | sed 's/^"//;s/"$//')
        action=$(echo "$action" | sed 's/^"//;s/"$//')
        
        # Check if value matches pattern
        if [[ "$value" =~ $pattern ]]; then
            # Execute action
            if [[ -n "$action" ]]; then
                result=$(eval "$action" 2>&1)
                executed_actions+=("$action")
                matched=true
                
                if [[ "$SWITCH_BREAK_ON_MATCH" == "true" ]]; then
                    break
                fi
            fi
        fi
    done
    
    if [[ "$matched" == true ]]; then
        echo "{\"status\":\"success\",\"message\":\"Pattern switch executed\",\"value\":\"$value\",\"matched\":true,\"actions\":[$(printf '"%s"' "${executed_actions[@]}" | tr '\n' ',' | sed 's/,$//')],\"result\":\"$result\"}"
    else
        echo "{\"status\":\"warning\",\"message\":\"No matching pattern found\",\"value\":\"$value\",\"matched\":false}"
    fi
}

# Initialize For operator
for_init() {
    local max_iterations="$1"
    local timeout="$2"
    
    FOR_MAX_ITERATIONS="${max_iterations:-1000}"
    FOR_TIMEOUT="${timeout:-300}"
    
    echo "{\"status\":\"success\",\"message\":\"For operator initialized\",\"max_iterations\":\"$FOR_MAX_ITERATIONS\",\"timeout\":\"$FOR_TIMEOUT\"}"
}

# Execute for loop with range
for_range() {
    local start="$1"
    local end="$2"
    local step="$3"
    local action="$4"
    
    if [[ -z "$start" || -z "$end" || -z "$action" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Start, end, and action are required\"}"
        return 1
    fi
    
    # Validate numeric inputs
    if ! [[ "$start" =~ ^-?[0-9]+$ ]] || ! [[ "$end" =~ ^-?[0-9]+$ ]]; then
        echo "{\"status\":\"error\",\"message\":\"Start and end must be integers\"}"
        return 1
    fi
    
    step="${step:-1}"
    if ! [[ "$step" =~ ^-?[0-9]+$ ]]; then
        echo "{\"status\":\"error\",\"message\":\"Step must be an integer\"}"
        return 1
    fi
    
    local iterations=0
    local results=()
    local start_time=$(date +%s)
    
    # Execute for loop
    for ((i=start; i<=end; i+=step)); do
        # Check timeout
        local current_time=$(date +%s)
        if [[ $((current_time - start_time)) -gt $FOR_TIMEOUT ]]; then
            echo "{\"status\":\"error\",\"message\":\"For loop timeout exceeded\",\"iterations\":$iterations,\"timeout\":$FOR_TIMEOUT}"
            return 1
        fi
        
        # Check max iterations
        if [[ $iterations -ge $FOR_MAX_ITERATIONS ]]; then
            echo "{\"status\":\"error\",\"message\":\"For loop max iterations exceeded\",\"iterations\":$iterations,\"max_iterations\":$FOR_MAX_ITERATIONS}"
            return 1
        fi
        
        # Execute action with current value
        local current_action=$(echo "$action" | sed "s/{i}/$i/g")
        local result=$(eval "$current_action" 2>&1)
        results+=("$result")
        
        ((iterations++))
    done
    
    echo "{\"status\":\"success\",\"message\":\"For loop completed\",\"iterations\":$iterations,\"start\":$start,\"end\":$end,\"step\":$step,\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Execute for loop with list
for_list() {
    local items="$1"
    local action="$2"
    local delimiter="$3"
    
    if [[ -z "$items" || -z "$action" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Items and action are required\"}"
        return 1
    fi
    
    delimiter="${delimiter:-,}"
    local iterations=0
    local results=()
    local start_time=$(date +%s)
    
    # Split items by delimiter
    IFS="$delimiter" read -ra ITEM_ARRAY <<< "$items"
    
    for item in "${ITEM_ARRAY[@]}"; do
        # Check timeout
        local current_time=$(date +%s)
        if [[ $((current_time - start_time)) -gt $FOR_TIMEOUT ]]; then
            echo "{\"status\":\"error\",\"message\":\"For loop timeout exceeded\",\"iterations\":$iterations,\"timeout\":$FOR_TIMEOUT}"
            return 1
        fi
        
        # Check max iterations
        if [[ $iterations -ge $FOR_MAX_ITERATIONS ]]; then
            echo "{\"status\":\"error\",\"message\":\"For loop max iterations exceeded\",\"iterations\":$iterations,\"max_iterations\":$FOR_MAX_ITERATIONS}"
            return 1
        fi
        
        # Execute action with current item
        local current_action=$(echo "$action" | sed "s/{item}/$item/g")
        local result=$(eval "$current_action" 2>&1)
        results+=("$result")
        
        ((iterations++))
    done
    
    echo "{\"status\":\"success\",\"message\":\"For loop completed\",\"iterations\":$iterations,\"items\":$iterations,\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Execute for loop with file lines
for_file() {
    local file_path="$1"
    local action="$2"
    
    if [[ -z "$file_path" || -z "$action" ]]; then
        echo "{\"status\":\"error\",\"message\":\"File path and action are required\"}"
        return 1
    fi
    
    if [[ ! -f "$file_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"File not found: $file_path\"}"
        return 1
    fi
    
    local iterations=0
    local results=()
    local start_time=$(date +%s)
    
    # Read file line by line
    while IFS= read -r line; do
        # Check timeout
        local current_time=$(date +%s)
        if [[ $((current_time - start_time)) -gt $FOR_TIMEOUT ]]; then
            echo "{\"status\":\"error\",\"message\":\"For loop timeout exceeded\",\"iterations\":$iterations,\"timeout\":$FOR_TIMEOUT}"
            return 1
        fi
        
        # Check max iterations
        if [[ $iterations -ge $FOR_MAX_ITERATIONS ]]; then
            echo "{\"status\":\"error\",\"message\":\"For loop max iterations exceeded\",\"iterations\":$iterations,\"max_iterations\":$FOR_MAX_ITERATIONS}"
            return 1
        fi
        
        # Execute action with current line
        local current_action=$(echo "$action" | sed "s/{line}/$line/g")
        local result=$(eval "$current_action" 2>&1)
        results+=("$result")
        
        ((iterations++))
    done < "$file_path"
    
    echo "{\"status\":\"success\",\"message\":\"For loop completed\",\"iterations\":$iterations,\"file\":\"$file_path\",\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Execute for loop with command output
for_command() {
    local command="$1"
    local action="$2"
    
    if [[ -z "$command" || -z "$action" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Command and action are required\"}"
        return 1
    fi
    
    local iterations=0
    local results=()
    local start_time=$(date +%s)
    
    # Execute command and process output
    while IFS= read -r line; do
        # Check timeout
        local current_time=$(date +%s)
        if [[ $((current_time - start_time)) -gt $FOR_TIMEOUT ]]; then
            echo "{\"status\":\"error\",\"message\":\"For loop timeout exceeded\",\"iterations\":$iterations,\"timeout\":$FOR_TIMEOUT}"
            return 1
        fi
        
        # Check max iterations
        if [[ $iterations -ge $FOR_MAX_ITERATIONS ]]; then
            echo "{\"status\":\"error\",\"message\":\"For loop max iterations exceeded\",\"iterations\":$iterations,\"max_iterations\":$FOR_MAX_ITERATIONS}"
            return 1
        fi
        
        # Execute action with current line
        local current_action=$(echo "$action" | sed "s/{line}/$line/g")
        local result=$(eval "$current_action" 2>&1)
        results+=("$result")
        
        ((iterations++))
    done < <(eval "$command" 2>/dev/null)
    
    echo "{\"status\":\"success\",\"message\":\"For loop completed\",\"iterations\":$iterations,\"command\":\"$command\",\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Get switch configuration
switch_config() {
    echo "{\"status\":\"success\",\"config\":{\"default_case\":\"$SWITCH_DEFAULT_CASE\",\"break_on_match\":\"$SWITCH_BREAK_ON_MATCH\"}}"
}

# Get for configuration
for_config() {
    echo "{\"status\":\"success\",\"config\":{\"max_iterations\":\"$FOR_MAX_ITERATIONS\",\"timeout\":\"$FOR_TIMEOUT\"}}"
}

# Main Switch operator function
execute_switch() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local default_case=$(echo "$params" | grep -o 'default_case=[^,]*' | cut -d'=' -f2)
            local break_on_match=$(echo "$params" | grep -o 'break_on_match=[^,]*' | cut -d'=' -f2)
            switch_init "$default_case" "$break_on_match"
            ;;
        "execute")
            local value=$(echo "$params" | grep -o 'value=[^,]*' | cut -d'=' -f2)
            local cases=$(echo "$params" | grep -o 'cases=[^,]*' | cut -d'=' -f2)
            switch_execute "$value" "$cases"
            ;;
        "pattern")
            local value=$(echo "$params" | grep -o 'value=[^,]*' | cut -d'=' -f2)
            local patterns=$(echo "$params" | grep -o 'patterns=[^,]*' | cut -d'=' -f2)
            switch_pattern "$value" "$patterns"
            ;;
        "config")
            switch_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, execute, pattern, config\"}"
            return 1
            ;;
    esac
}

# Main For operator function
execute_for() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local max_iterations=$(echo "$params" | grep -o 'max_iterations=[^,]*' | cut -d'=' -f2)
            local timeout=$(echo "$params" | grep -o 'timeout=[^,]*' | cut -d'=' -f2)
            for_init "$max_iterations" "$timeout"
            ;;
        "range")
            local start=$(echo "$params" | grep -o 'start=[^,]*' | cut -d'=' -f2)
            local end=$(echo "$params" | grep -o 'end=[^,]*' | cut -d'=' -f2)
            local step=$(echo "$params" | grep -o 'step=[^,]*' | cut -d'=' -f2)
            local action_cmd=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            for_range "$start" "$end" "$step" "$action_cmd"
            ;;
        "list")
            local items=$(echo "$params" | grep -o 'items=[^,]*' | cut -d'=' -f2)
            local action_cmd=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            local delimiter=$(echo "$params" | grep -o 'delimiter=[^,]*' | cut -d'=' -f2)
            for_list "$items" "$action_cmd" "$delimiter"
            ;;
        "file")
            local file_path=$(echo "$params" | grep -o 'file_path=[^,]*' | cut -d'=' -f2)
            local action_cmd=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            for_file "$file_path" "$action_cmd"
            ;;
        "command")
            local command=$(echo "$params" | grep -o 'command=[^,]*' | cut -d'=' -f2)
            local action_cmd=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            for_command "$command" "$action_cmd"
            ;;
        "config")
            for_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, range, list, file, command, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_switch execute_for switch_init switch_execute switch_pattern for_init for_range for_list for_file for_command switch_config for_config 