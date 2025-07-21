#!/bin/bash

# Firewall Operator Implementation
# Provides firewall rule management

# Global variables
FIREWALL_TYPE="iptables"
FIREWALL_DEFAULT_POLICY="DROP"
FIREWALL_LOG_ENABLED="true"
FIREWALL_BACKUP_PATH="/tmp/firewall_backup"

# Initialize Firewall operator
firewall_init() {
    local type="$1"
    local default_policy="$2"
    local log_enabled="$3"
    local backup_path="$4"
    
    FIREWALL_TYPE="${type:-iptables}"
    FIREWALL_DEFAULT_POLICY="${default_policy:-DROP}"
    FIREWALL_LOG_ENABLED="${log_enabled:-true}"
    FIREWALL_BACKUP_PATH="${backup_path:-/tmp/firewall_backup}"
    
    # Create backup directory
    mkdir -p "$FIREWALL_BACKUP_PATH"
    
    echo "{\"status\":\"success\",\"message\":\"Firewall operator initialized\",\"type\":\"$FIREWALL_TYPE\",\"default_policy\":\"$FIREWALL_DEFAULT_POLICY\",\"log_enabled\":\"$FIREWALL_LOG_ENABLED\"}"
}

# Add firewall rule
firewall_add_rule() {
    local chain="$1"
    local action="$2"
    local protocol="$3"
    local port="$4"
    local source="$5"
    local destination="$6"
    
    if [[ -z "$chain" || -z "$action" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Chain and action are required\"}"
        return 1
    fi
    
    case "$FIREWALL_TYPE" in
        "iptables")
            firewall_add_iptables_rule "$chain" "$action" "$protocol" "$port" "$source" "$destination"
            ;;
        "ufw")
            firewall_add_ufw_rule "$chain" "$action" "$protocol" "$port" "$source" "$destination"
            ;;
        "firewalld")
            firewall_add_firewalld_rule "$chain" "$action" "$protocol" "$port" "$source" "$destination"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported firewall type: $FIREWALL_TYPE\"}"
            return 1
            ;;
    esac
}

# Add iptables rule
firewall_add_iptables_rule() {
    local chain="$1"
    local action="$2"
    local protocol="$3"
    local port="$4"
    local source="$5"
    local destination="$6"
    
    if ! command -v iptables >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"iptables not found\"}"
        return 1
    fi
    
    local rule_cmd="iptables -A $chain"
    
    if [[ -n "$protocol" ]]; then
        rule_cmd="$rule_cmd -p $protocol"
    fi
    
    if [[ -n "$port" ]]; then
        rule_cmd="$rule_cmd --dport $port"
    fi
    
    if [[ -n "$source" ]]; then
        rule_cmd="$rule_cmd -s $source"
    fi
    
    if [[ -n "$destination" ]]; then
        rule_cmd="$rule_cmd -d $destination"
    fi
    
    rule_cmd="$rule_cmd -j $action"
    
    if [[ "$FIREWALL_LOG_ENABLED" == "true" && "$action" == "DROP" ]]; then
        local log_rule_cmd="${rule_cmd//-j $action/-j LOG --log-prefix \"FIREWALL_DROP: \"}"
        eval "$log_rule_cmd" 2>&1
    fi
    
    eval "$rule_cmd" 2>&1
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"iptables rule added\",\"chain\":\"$chain\",\"action\":\"$action\",\"rule\":\"$rule_cmd\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to add iptables rule\",\"error_code\":$exit_code}"
        return 1
    fi
}

# Add UFW rule
firewall_add_ufw_rule() {
    local chain="$1"
    local action="$2"
    local protocol="$3"
    local port="$4"
    local source="$5"
    local destination="$6"
    
    if ! command -v ufw >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"UFW not found\"}"
        return 1
    fi
    
    local rule_cmd="ufw"
    
    case "$action" in
        "ACCEPT")
            rule_cmd="$rule_cmd allow"
            ;;
        "DROP"|"REJECT")
            rule_cmd="$rule_cmd deny"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported UFW action: $action\"}"
            return 1
            ;;
    esac
    
    if [[ -n "$source" ]]; then
        rule_cmd="$rule_cmd from $source"
    fi
    
    if [[ -n "$destination" ]]; then
        rule_cmd="$rule_cmd to $destination"
    fi
    
    if [[ -n "$port" ]]; then
        rule_cmd="$rule_cmd port $port"
    fi
    
    if [[ -n "$protocol" ]]; then
        rule_cmd="$rule_cmd proto $protocol"
    fi
    
    eval "$rule_cmd" 2>&1
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"UFW rule added\",\"action\":\"$action\",\"rule\":\"$rule_cmd\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to add UFW rule\",\"error_code\":$exit_code}"
        return 1
    fi
}

# Add firewalld rule
firewall_add_firewalld_rule() {
    local chain="$1"
    local action="$2"
    local protocol="$3"
    local port="$4"
    local source="$5"
    local destination="$6"
    
    if ! command -v firewall-cmd >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"firewalld not found\"}"
        return 1
    fi
    
    local rule_cmd=""
    
    case "$action" in
        "ACCEPT")
            if [[ -n "$port" && -n "$protocol" ]]; then
                rule_cmd="firewall-cmd --add-port=${port}/${protocol} --permanent"
            elif [[ -n "$source" ]]; then
                rule_cmd="firewall-cmd --add-source=$source --permanent"
            fi
            ;;
        "DROP"|"REJECT")
            if [[ -n "$source" ]]; then
                rule_cmd="firewall-cmd --add-rich-rule='rule source address=\"$source\" drop' --permanent"
            fi
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported firewalld action: $action\"}"
            return 1
            ;;
    esac
    
    if [[ -z "$rule_cmd" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Unable to construct firewalld rule\"}"
        return 1
    fi
    
    eval "$rule_cmd" 2>&1
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        firewall-cmd --reload 2>&1
        echo "{\"status\":\"success\",\"message\":\"firewalld rule added\",\"action\":\"$action\",\"rule\":\"$rule_cmd\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to add firewalld rule\",\"error_code\":$exit_code}"
        return 1
    fi
}

# Remove firewall rule
firewall_remove_rule() {
    local rule_spec="$1"
    
    if [[ -z "$rule_spec" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Rule specification is required\"}"
        return 1
    fi
    
    case "$FIREWALL_TYPE" in
        "iptables")
            firewall_remove_iptables_rule "$rule_spec"
            ;;
        "ufw")
            firewall_remove_ufw_rule "$rule_spec"
            ;;
        "firewalld")
            firewall_remove_firewalld_rule "$rule_spec"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported firewall type: $FIREWALL_TYPE\"}"
            return 1
            ;;
    esac
}

# Remove iptables rule
firewall_remove_iptables_rule() {
    local rule_spec="$1"
    
    # Convert -A to -D for deletion
    local delete_rule=$(echo "$rule_spec" | sed 's/-A /-D /')
    
    eval "iptables $delete_rule" 2>&1
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"iptables rule removed\",\"rule\":\"$delete_rule\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to remove iptables rule\",\"error_code\":$exit_code}"
        return 1
    fi
}

# Remove UFW rule
firewall_remove_ufw_rule() {
    local rule_spec="$1"
    
    ufw delete "$rule_spec" 2>&1
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"UFW rule removed\",\"rule\":\"$rule_spec\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to remove UFW rule\",\"error_code\":$exit_code}"
        return 1
    fi
}

# Remove firewalld rule
firewall_remove_firewalld_rule() {
    local rule_spec="$1"
    
    # Convert add to remove
    local remove_rule=$(echo "$rule_spec" | sed 's/--add-/--remove-/')
    
    eval "firewall-cmd $remove_rule" 2>&1
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        firewall-cmd --reload 2>&1
        echo "{\"status\":\"success\",\"message\":\"firewalld rule removed\",\"rule\":\"$remove_rule\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to remove firewalld rule\",\"error_code\":$exit_code}"
        return 1
    fi
}

# List firewall rules
firewall_list_rules() {
    local chain="$1"
    
    case "$FIREWALL_TYPE" in
        "iptables")
            firewall_list_iptables_rules "$chain"
            ;;
        "ufw")
            firewall_list_ufw_rules
            ;;
        "firewalld")
            firewall_list_firewalld_rules
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported firewall type: $FIREWALL_TYPE\"}"
            return 1
            ;;
    esac
}

# List iptables rules
firewall_list_iptables_rules() {
    local chain="$1"
    
    local list_cmd="iptables -L"
    if [[ -n "$chain" ]]; then
        list_cmd="$list_cmd $chain"
    fi
    list_cmd="$list_cmd -n --line-numbers"
    
    local rules=$(eval "$list_cmd" 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"iptables rules listed\",\"rules\":\"$rules\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to list iptables rules\",\"error_code\":$exit_code}"
        return 1
    fi
}

# List UFW rules
firewall_list_ufw_rules() {
    local rules=$(ufw status numbered 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"UFW rules listed\",\"rules\":\"$rules\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to list UFW rules\",\"error_code\":$exit_code}"
        return 1
    fi
}

# List firewalld rules
firewall_list_firewalld_rules() {
    local rules=$(firewall-cmd --list-all 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"firewalld rules listed\",\"rules\":\"$rules\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to list firewalld rules\",\"error_code\":$exit_code}"
        return 1
    fi
}

# Backup firewall rules
firewall_backup() {
    local backup_file="$1"
    
    backup_file="${backup_file:-$FIREWALL_BACKUP_PATH/firewall_backup_$(date +%Y%m%d_%H%M%S)}"
    
    case "$FIREWALL_TYPE" in
        "iptables")
            iptables-save > "$backup_file" 2>&1
            ;;
        "ufw")
            cp /etc/ufw/user.rules "$backup_file" 2>&1
            ;;
        "firewalld")
            firewall-cmd --list-all > "$backup_file" 2>&1
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported firewall type: $FIREWALL_TYPE\"}"
            return 1
            ;;
    esac
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Firewall rules backed up\",\"backup_file\":\"$backup_file\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to backup firewall rules\",\"error_code\":$exit_code}"
        return 1
    fi
}

# Restore firewall rules
firewall_restore() {
    local backup_file="$1"
    
    if [[ -z "$backup_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Backup file is required\"}"
        return 1
    fi
    
    if [[ ! -f "$backup_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Backup file not found: $backup_file\"}"
        return 1
    fi
    
    case "$FIREWALL_TYPE" in
        "iptables")
            iptables-restore < "$backup_file" 2>&1
            ;;
        "ufw")
            cp "$backup_file" /etc/ufw/user.rules 2>&1
            ufw reload 2>&1
            ;;
        "firewalld")
            echo "{\"status\":\"error\",\"message\":\"firewalld restore not implemented\"}"
            return 1
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported firewall type: $FIREWALL_TYPE\"}"
            return 1
            ;;
    esac
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Firewall rules restored\",\"backup_file\":\"$backup_file\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to restore firewall rules\",\"error_code\":$exit_code}"
        return 1
    fi
}

# Get firewall configuration
firewall_config() {
    echo "{\"status\":\"success\",\"config\":{\"type\":\"$FIREWALL_TYPE\",\"default_policy\":\"$FIREWALL_DEFAULT_POLICY\",\"log_enabled\":\"$FIREWALL_LOG_ENABLED\",\"backup_path\":\"$FIREWALL_BACKUP_PATH\"}}"
}

# Main Firewall operator function
execute_firewall() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local type=$(echo "$params" | grep -o 'type=[^,]*' | cut -d'=' -f2)
            local default_policy=$(echo "$params" | grep -o 'default_policy=[^,]*' | cut -d'=' -f2)
            local log_enabled=$(echo "$params" | grep -o 'log_enabled=[^,]*' | cut -d'=' -f2)
            local backup_path=$(echo "$params" | grep -o 'backup_path=[^,]*' | cut -d'=' -f2)
            firewall_init "$type" "$default_policy" "$log_enabled" "$backup_path"
            ;;
        "add_rule")
            local chain=$(echo "$params" | grep -o 'chain=[^,]*' | cut -d'=' -f2)
            local action_param=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            local protocol=$(echo "$params" | grep -o 'protocol=[^,]*' | cut -d'=' -f2)
            local port=$(echo "$params" | grep -o 'port=[^,]*' | cut -d'=' -f2)
            local source=$(echo "$params" | grep -o 'source=[^,]*' | cut -d'=' -f2)
            local destination=$(echo "$params" | grep -o 'destination=[^,]*' | cut -d'=' -f2)
            firewall_add_rule "$chain" "$action_param" "$protocol" "$port" "$source" "$destination"
            ;;
        "remove_rule")
            local rule_spec=$(echo "$params" | grep -o 'rule_spec=[^,]*' | cut -d'=' -f2)
            firewall_remove_rule "$rule_spec"
            ;;
        "list_rules")
            local chain=$(echo "$params" | grep -o 'chain=[^,]*' | cut -d'=' -f2)
            firewall_list_rules "$chain"
            ;;
        "backup")
            local backup_file=$(echo "$params" | grep -o 'backup_file=[^,]*' | cut -d'=' -f2)
            firewall_backup "$backup_file"
            ;;
        "restore")
            local backup_file=$(echo "$params" | grep -o 'backup_file=[^,]*' | cut -d'=' -f2)
            firewall_restore "$backup_file"
            ;;
        "config")
            firewall_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, add_rule, remove_rule, list_rules, backup, restore, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_firewall firewall_init firewall_add_rule firewall_remove_rule firewall_list_rules firewall_backup firewall_restore firewall_config 