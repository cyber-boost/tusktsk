#!/bin/bash

# While and Each Operators Implementation
# Provides loop control and iteration

# Global variables
WHILE_MAX_ITERATIONS="1000"
WHILE_TIMEOUT="300"
WHILE_CHECK_INTERVAL="1"
EACH_BATCH_SIZE="100"
EACH_PARALLEL="false"
EACH_MAX_WORKERS="4"

# Initialize While operator
while_init() {
    local max_iterations="$1"
    local timeout="$2"
    local check_interval="$3"
    
    WHILE_MAX_ITERATIONS="${max_iterations:-1000}"
    WHILE_TIMEOUT="${timeout:-300}"
    WHILE_CHECK_INTERVAL="${check_interval:-1}"
    
    echo "{\"status\":\"success\",\"message\":\"While operator initialized\",\"max_iterations\":\"$WHILE_MAX_ITERATIONS\",\"timeout\":\"$WHILE_TIMEOUT\",\"check_interval\":\"$WHILE_CHECK_INTERVAL\"}"
}

# Execute while loop with condition
while_condition() {
    local condition="$1"
    local action="$2"
    local break_condition="$3"
    
    if [[ -z "$condition" || -z "$action" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Condition and action are required\"}"
        return 1
    fi
    
    local iterations=0
    local results=()
    local start_time=$(date +%s)
    local last_check_time=0
    
    # Execute while loop
    while true; do
        # Check timeout
        local current_time=$(date +%s)
        if [[ $((current_time - start_time)) -gt $WHILE_TIMEOUT ]]; then
            echo "{\"status\":\"error\",\"message\":\"While loop timeout exceeded\",\"iterations\":$iterations,\"timeout\":$WHILE_TIMEOUT}"
            return 1
        fi
        
        # Check max iterations
        if [[ $iterations -ge $WHILE_MAX_ITERATIONS ]]; then
            echo "{\"status\":\"error\",\"message\":\"While loop max iterations exceeded\",\"iterations\":$iterations,\"max_iterations\":$WHILE_MAX_ITERATIONS}"
            return 1
        fi
        
        # Check condition (only if enough time has passed)
        if [[ $((current_time - last_check_time)) -ge $WHILE_CHECK_INTERVAL ]]; then
            local condition_result=$(eval "$condition" 2>/dev/null)
            local condition_exit_code=$?
            
            # Break if condition is false
            if [[ $condition_exit_code -ne 0 ]]; then
                break
            fi
            
            last_check_time=$current_time
        fi
        
        # Execute action
        local result=$(eval "$action" 2>&1)
        local action_exit_code=$?
        
        results+=("$result")
        ((iterations++))
        
        # Check break condition
        if [[ -n "$break_condition" ]]; then
            local break_result=$(eval "$break_condition" 2>/dev/null)
            if [[ $? -eq 0 ]]; then
                break
            fi
        fi
        
        # Break if action failed
        if [[ $action_exit_code -ne 0 ]]; then
            echo "{\"status\":\"warning\",\"message\":\"While loop action failed\",\"iterations\":$iterations,\"last_result\":\"$result\"}"
            break
        fi
        
        # Small delay to prevent CPU overload
        sleep 0.1
    done
    
    echo "{\"status\":\"success\",\"message\":\"While loop completed\",\"iterations\":$iterations,\"condition\":\"$condition\",\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Execute while loop with file monitoring
while_file() {
    local file_path="$1"
    local action="$2"
    local trigger_condition="$3"
    
    if [[ -z "$file_path" || -z "$action" ]]; then
        echo "{\"status\":\"error\",\"message\":\"File path and action are required\"}"
        return 1
    fi
    
    local iterations=0
    local results=()
    local start_time=$(date +%s)
    local last_modified=0
    
    # Get initial file modification time
    if [[ -f "$file_path" ]]; then
        last_modified=$(stat -c %Y "$file_path" 2>/dev/null || echo "0")
    fi
    
    # Execute while loop
    while true; do
        # Check timeout
        local current_time=$(date +%s)
        if [[ $((current_time - start_time)) -gt $WHILE_TIMEOUT ]]; then
            echo "{\"status\":\"error\",\"message\":\"While loop timeout exceeded\",\"iterations\":$iterations,\"timeout\":$WHILE_TIMEOUT}"
            return 1
        fi
        
        # Check max iterations
        if [[ $iterations -ge $WHILE_MAX_ITERATIONS ]]; then
            echo "{\"status\":\"error\",\"message\":\"While loop max iterations exceeded\",\"iterations\":$iterations,\"max_iterations\":$WHILE_MAX_ITERATIONS}"
            return 1
        fi
        
        # Check if file exists and has been modified
        if [[ -f "$file_path" ]]; then
            local current_modified=$(stat -c %Y "$file_path" 2>/dev/null || echo "0")
            
            if [[ $current_modified -gt $last_modified ]]; then
                # Execute action
                local result=$(eval "$action" 2>&1)
                results+=("$result")
                ((iterations++))
                
                last_modified=$current_modified
                
                # Check trigger condition
                if [[ -n "$trigger_condition" ]]; then
                    local trigger_result=$(eval "$trigger_condition" 2>/dev/null)
                    if [[ $? -eq 0 ]]; then
                        break
                    fi
                fi
            fi
        else
            # File doesn't exist, check if we should break
            if [[ -n "$trigger_condition" ]]; then
                local trigger_result=$(eval "$trigger_condition" 2>/dev/null)
                if [[ $? -eq 0 ]]; then
                    break
                fi
            fi
        fi
        
        sleep $WHILE_CHECK_INTERVAL
    done
    
    echo "{\"status\":\"success\",\"message\":\"While file loop completed\",\"iterations\":$iterations,\"file\":\"$file_path\",\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Execute while loop with command output
while_command() {
    local command="$1"
    local action="$2"
    local break_condition="$3"
    
    if [[ -z "$command" || -z "$action" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Command and action are required\"}"
        return 1
    fi
    
    local iterations=0
    local results=()
    local start_time=$(date +%s)
    
    # Execute while loop
    while true; do
        # Check timeout
        local current_time=$(date +%s)
        if [[ $((current_time - start_time)) -gt $WHILE_TIMEOUT ]]; then
            echo "{\"status\":\"error\",\"message\":\"While loop timeout exceeded\",\"iterations\":$iterations,\"timeout\":$WHILE_TIMEOUT}"
            return 1
        fi
        
        # Check max iterations
        if [[ $iterations -ge $WHILE_MAX_ITERATIONS ]]; then
            echo "{\"status\":\"error\",\"message\":\"While loop max iterations exceeded\",\"iterations\":$iterations,\"max_iterations\":$WHILE_MAX_ITERATIONS}"
            return 1
        fi
        
        # Execute command
        local command_result=$(eval "$command" 2>&1)
        local command_exit_code=$?
        
        # Break if command fails
        if [[ $command_exit_code -ne 0 ]]; then
            break
        fi
        
        # Execute action with command result
        local current_action=$(echo "$action" | sed "s/{result}/$command_result/g")
        local result=$(eval "$current_action" 2>&1)
        results+=("$result")
        ((iterations++))
        
        # Check break condition
        if [[ -n "$break_condition" ]]; then
            local break_result=$(eval "$break_condition" 2>/dev/null)
            if [[ $? -eq 0 ]]; then
                break
            fi
        fi
        
        sleep $WHILE_CHECK_INTERVAL
    done
    
    echo "{\"status\":\"success\",\"message\":\"While command loop completed\",\"iterations\":$iterations,\"command\":\"$command\",\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Initialize Each operator
each_init() {
    local batch_size="$1"
    local parallel="$2"
    local max_workers="$3"
    
    EACH_BATCH_SIZE="${batch_size:-100}"
    EACH_PARALLEL="${parallel:-false}"
    EACH_MAX_WORKERS="${max_workers:-4}"
    
    echo "{\"status\":\"success\",\"message\":\"Each operator initialized\",\"batch_size\":\"$EACH_BATCH_SIZE\",\"parallel\":\"$EACH_PARALLEL\",\"max_workers\":\"$EACH_MAX_WORKERS\"}"
}

# Execute each iteration on list
each_list() {
    local items="$1"
    local action="$2"
    local delimiter="$3"
    
    if [[ -z "$items" || -z "$action" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Items and action are required\"}"
        return 1
    fi
    
    delimiter="${delimiter:-,}"
    local results=()
    local start_time=$(date +%s)
    
    # Split items by delimiter
    IFS="$delimiter" read -ra ITEM_ARRAY <<< "$items"
    local total_items=${#ITEM_ARRAY[@]}
    
    if [[ "$EACH_PARALLEL" == "true" ]]; then
        # Parallel execution
        local pids=()
        local temp_dir=$(mktemp -d)
        local worker_count=0
        
        for i in "${!ITEM_ARRAY[@]}"; do
            local item="${ITEM_ARRAY[$i]}"
            
            # Limit concurrent workers
            while [[ ${#pids[@]} -ge $EACH_MAX_WORKERS ]]; do
                for j in "${!pids[@]}"; do
                    if ! kill -0 "${pids[$j]}" 2>/dev/null; then
                        unset "pids[$j]"
                    fi
                done
                pids=("${pids[@]}")  # Reindex array
                sleep 0.1
            done
            
            # Execute action in background
            (
                local current_action=$(echo "$action" | sed "s/{item}/$item/g;s/{index}/$i/g")
                local result=$(eval "$current_action" 2>&1)
                echo "$result" > "$temp_dir/result_$i"
            ) &
            
            pids+=($!)
            ((worker_count++))
        done
        
        # Wait for all workers to complete
        for pid in "${pids[@]}"; do
            wait "$pid"
        done
        
        # Collect results
        for i in "${!ITEM_ARRAY[@]}"; do
            if [[ -f "$temp_dir/result_$i" ]]; then
                local result=$(cat "$temp_dir/result_$i")
                results+=("$result")
            fi
        done
        
        rm -rf "$temp_dir"
    else
        # Sequential execution
        for i in "${!ITEM_ARRAY[@]}"; do
            local item="${ITEM_ARRAY[$i]}"
            local current_action=$(echo "$action" | sed "s/{item}/$item/g;s/{index}/$i/g")
            local result=$(eval "$current_action" 2>&1)
            results+=("$result")
        done
    fi
    
    local execution_time=$(( $(date +%s) - start_time ))
    
    echo "{\"status\":\"success\",\"message\":\"Each list completed\",\"total_items\":$total_items,\"execution_time\":$execution_time,\"parallel\":$EACH_PARALLEL,\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Execute each iteration on file
each_file() {
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
    
    local results=()
    local line_number=0
    local start_time=$(date +%s)
    
    if [[ "$EACH_PARALLEL" == "true" ]]; then
        # Parallel execution
        local pids=()
        local temp_dir=$(mktemp -d)
        
        while IFS= read -r line; do
            # Limit concurrent workers
            while [[ ${#pids[@]} -ge $EACH_MAX_WORKERS ]]; do
                for j in "${!pids[@]}"; do
                    if ! kill -0 "${pids[$j]}" 2>/dev/null; then
                        unset "pids[$j]"
                    fi
                done
                pids=("${pids[@]}")  # Reindex array
                sleep 0.1
            done
            
            # Execute action in background
            (
                local current_action=$(echo "$action" | sed "s/{line}/$line/g;s/{line_number}/$line_number/g")
                local result=$(eval "$current_action" 2>&1)
                echo "$result" > "$temp_dir/result_$line_number"
            ) &
            
            pids+=($!)
            ((line_number++))
        done < "$file_path"
        
        # Wait for all workers to complete
        for pid in "${pids[@]}"; do
            wait "$pid"
        done
        
        # Collect results
        for i in $(seq 0 $((line_number - 1))); do
            if [[ -f "$temp_dir/result_$i" ]]; then
                local result=$(cat "$temp_dir/result_$i")
                results+=("$result")
            fi
        done
        
        rm -rf "$temp_dir"
    else
        # Sequential execution
        while IFS= read -r line; do
            local current_action=$(echo "$action" | sed "s/{line}/$line/g;s/{line_number}/$line_number/g")
            local result=$(eval "$current_action" 2>&1)
            results+=("$result")
            ((line_number++))
        done < "$file_path"
    fi
    
    local execution_time=$(( $(date +%s) - start_time ))
    
    echo "{\"status\":\"success\",\"message\":\"Each file completed\",\"total_lines\":$line_number,\"execution_time\":$execution_time,\"parallel\":$EACH_PARALLEL,\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Execute each iteration on command output
each_command() {
    local command="$1"
    local action="$2"
    
    if [[ -z "$command" || -z "$action" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Command and action are required\"}"
        return 1
    fi
    
    local results=()
    local line_number=0
    local start_time=$(date +%s)
    
    if [[ "$EACH_PARALLEL" == "true" ]]; then
        # Parallel execution
        local pids=()
        local temp_dir=$(mktemp -d)
        
        while IFS= read -r line; do
            # Limit concurrent workers
            while [[ ${#pids[@]} -ge $EACH_MAX_WORKERS ]]; do
                for j in "${!pids[@]}"; do
                    if ! kill -0 "${pids[$j]}" 2>/dev/null; then
                        unset "pids[$j]"
                    fi
                done
                pids=("${pids[@]}")  # Reindex array
                sleep 0.1
            done
            
            # Execute action in background
            (
                local current_action=$(echo "$action" | sed "s/{line}/$line/g;s/{line_number}/$line_number/g")
                local result=$(eval "$current_action" 2>&1)
                echo "$result" > "$temp_dir/result_$line_number"
            ) &
            
            pids+=($!)
            ((line_number++))
        done < <(eval "$command" 2>/dev/null)
        
        # Wait for all workers to complete
        for pid in "${pids[@]}"; do
            wait "$pid"
        done
        
        # Collect results
        for i in $(seq 0 $((line_number - 1))); do
            if [[ -f "$temp_dir/result_$i" ]]; then
                local result=$(cat "$temp_dir/result_$i")
                results+=("$result")
            fi
        done
        
        rm -rf "$temp_dir"
    else
        # Sequential execution
        while IFS= read -r line; do
            local current_action=$(echo "$action" | sed "s/{line}/$line/g;s/{line_number}/$line_number/g")
            local result=$(eval "$current_action" 2>&1)
            results+=("$result")
            ((line_number++))
        done < <(eval "$command" 2>/dev/null)
    fi
    
    local execution_time=$(( $(date +%s) - start_time ))
    
    echo "{\"status\":\"success\",\"message\":\"Each command completed\",\"total_lines\":$line_number,\"execution_time\":$execution_time,\"parallel\":$EACH_PARALLEL,\"results\":[$(printf '"%s"' "${results[@]}" | tr '\n' ',' | sed 's/,$//')]}"
}

# Get while configuration
while_config() {
    echo "{\"status\":\"success\",\"config\":{\"max_iterations\":\"$WHILE_MAX_ITERATIONS\",\"timeout\":\"$WHILE_TIMEOUT\",\"check_interval\":\"$WHILE_CHECK_INTERVAL\"}}"
}

# Get each configuration
each_config() {
    echo "{\"status\":\"success\",\"config\":{\"batch_size\":\"$EACH_BATCH_SIZE\",\"parallel\":\"$EACH_PARALLEL\",\"max_workers\":\"$EACH_MAX_WORKERS\"}}"
}

# Main While operator function
execute_while() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local max_iterations=$(echo "$params" | grep -o 'max_iterations=[^,]*' | cut -d'=' -f2)
            local timeout=$(echo "$params" | grep -o 'timeout=[^,]*' | cut -d'=' -f2)
            local check_interval=$(echo "$params" | grep -o 'check_interval=[^,]*' | cut -d'=' -f2)
            while_init "$max_iterations" "$timeout" "$check_interval"
            ;;
        "condition")
            local condition=$(echo "$params" | grep -o 'condition=[^,]*' | cut -d'=' -f2)
            local action_cmd=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            local break_condition=$(echo "$params" | grep -o 'break_condition=[^,]*' | cut -d'=' -f2)
            while_condition "$condition" "$action_cmd" "$break_condition"
            ;;
        "file")
            local file_path=$(echo "$params" | grep -o 'file_path=[^,]*' | cut -d'=' -f2)
            local action_cmd=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            local trigger_condition=$(echo "$params" | grep -o 'trigger_condition=[^,]*' | cut -d'=' -f2)
            while_file "$file_path" "$action_cmd" "$trigger_condition"
            ;;
        "command")
            local command=$(echo "$params" | grep -o 'command=[^,]*' | cut -d'=' -f2)
            local action_cmd=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            local break_condition=$(echo "$params" | grep -o 'break_condition=[^,]*' | cut -d'=' -f2)
            while_command "$command" "$action_cmd" "$break_condition"
            ;;
        "config")
            while_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, condition, file, command, config\"}"
            return 1
            ;;
    esac
}

# Main Each operator function
execute_each() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local batch_size=$(echo "$params" | grep -o 'batch_size=[^,]*' | cut -d'=' -f2)
            local parallel=$(echo "$params" | grep -o 'parallel=[^,]*' | cut -d'=' -f2)
            local max_workers=$(echo "$params" | grep -o 'max_workers=[^,]*' | cut -d'=' -f2)
            each_init "$batch_size" "$parallel" "$max_workers"
            ;;
        "list")
            local items=$(echo "$params" | grep -o 'items=[^,]*' | cut -d'=' -f2)
            local action_cmd=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            local delimiter=$(echo "$params" | grep -o 'delimiter=[^,]*' | cut -d'=' -f2)
            each_list "$items" "$action_cmd" "$delimiter"
            ;;
        "file")
            local file_path=$(echo "$params" | grep -o 'file_path=[^,]*' | cut -d'=' -f2)
            local action_cmd=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            each_file "$file_path" "$action_cmd"
            ;;
        "command")
            local command=$(echo "$params" | grep -o 'command=[^,]*' | cut -d'=' -f2)
            local action_cmd=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            each_command "$command" "$action_cmd"
            ;;
        "config")
            each_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, list, file, command, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_while execute_each while_init while_condition while_file while_command each_init each_list each_file each_command while_config each_config 