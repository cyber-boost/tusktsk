#!/bin/bash

# VPN Operator Implementation
# Provides VPN connection management

# Global variables
VPN_TYPE="openvpn"
VPN_CONFIG_PATH=""
VPN_USERNAME=""
VPN_PASSWORD=""
VPN_INTERFACE=""
VPN_TIMEOUT="30"
VPN_RETRY_COUNT="3"

# Initialize VPN operator
vpn_init() {
    local type="$1"
    local config_path="$2"
    local username="$3"
    local password="$4"
    local interface="$5"
    local timeout="$6"
    local retry_count="$7"
    
    VPN_TYPE="${type:-openvpn}"
    VPN_CONFIG_PATH="$config_path"
    VPN_USERNAME="$username"
    VPN_PASSWORD="$password"
    VPN_INTERFACE="$interface"
    VPN_TIMEOUT="${timeout:-30}"
    VPN_RETRY_COUNT="${retry_count:-3}"
    
    echo "{\"status\":\"success\",\"message\":\"VPN operator initialized\",\"type\":\"$VPN_TYPE\",\"config_path\":\"$VPN_CONFIG_PATH\",\"timeout\":\"$VPN_TIMEOUT\"}"
}

# VPN connect
vpn_connect() {
    local config_path="$1"
    local username="$2"
    local password="$3"
    
    config_path="${config_path:-$VPN_CONFIG_PATH}"
    username="${username:-$VPN_USERNAME}"
    password="${password:-$VPN_PASSWORD}"
    
    if [[ -z "$config_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"VPN config path is required\"}"
        return 1
    fi
    
    if [[ ! -f "$config_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"VPN config file not found: $config_path\"}"
        return 1
    fi
    
    case "$VPN_TYPE" in
        "openvpn")
            vpn_connect_openvpn "$config_path" "$username" "$password"
            ;;
        "wireguard")
            vpn_connect_wireguard "$config_path"
            ;;
        "ipsec")
            vpn_connect_ipsec "$config_path" "$username" "$password"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported VPN type: $VPN_TYPE\"}"
            return 1
            ;;
    esac
}

# OpenVPN connect
vpn_connect_openvpn() {
    local config_path="$1"
    local username="$2"
    local password="$3"
    
    if ! command -v openvpn >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"OpenVPN client not found\"}"
        return 1
    fi
    
    # Check if already connected
    if pgrep -f "openvpn.*$config_path" >/dev/null; then
        echo "{\"status\":\"warning\",\"message\":\"OpenVPN already connected with this config\"}"
        return 0
    fi
    
    local auth_file=""
    if [[ -n "$username" && -n "$password" ]]; then
        auth_file=$(mktemp)
        echo "$username" > "$auth_file"
        echo "$password" >> "$auth_file"
        chmod 600 "$auth_file"
    fi
    
    local openvpn_cmd="openvpn --config $config_path --daemon --writepid /tmp/openvpn.pid"
    if [[ -n "$auth_file" ]]; then
        openvpn_cmd="$openvpn_cmd --auth-user-pass $auth_file"
    fi
    
    eval "$openvpn_cmd" 2>&1
    local exit_code=$?
    
    # Clean up auth file
    if [[ -n "$auth_file" ]]; then
        rm -f "$auth_file"
    fi
    
    if [[ $exit_code -eq 0 ]]; then
        sleep 5  # Wait for connection to establish
        echo "{\"status\":\"success\",\"message\":\"OpenVPN connection initiated\",\"config\":\"$config_path\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"OpenVPN connection failed\",\"error_code\":$exit_code}"
        return 1
    fi
}

# WireGuard connect
vpn_connect_wireguard() {
    local config_path="$1"
    
    if ! command -v wg >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"WireGuard client not found\"}"
        return 1
    fi
    
    local interface_name=$(basename "$config_path" .conf)
    
    # Check if already connected
    if wg show "$interface_name" >/dev/null 2>&1; then
        echo "{\"status\":\"warning\",\"message\":\"WireGuard already connected on interface: $interface_name\"}"
        return 0
    fi
    
    wg-quick up "$config_path" 2>&1
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"WireGuard connection established\",\"interface\":\"$interface_name\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"WireGuard connection failed\",\"error_code\":$exit_code}"
        return 1
    fi
}

# IPSec connect
vpn_connect_ipsec() {
    local config_path="$1"
    local username="$2"
    local password="$3"
    
    if ! command -v ipsec >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"IPSec client not found\"}"
        return 1
    fi
    
    # This is a simplified IPSec implementation
    ipsec up "$config_path" 2>&1
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"IPSec connection established\",\"config\":\"$config_path\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"IPSec connection failed\",\"error_code\":$exit_code}"
        return 1
    fi
}

# VPN disconnect
vpn_disconnect() {
    local interface="$1"
    
    case "$VPN_TYPE" in
        "openvpn")
            vpn_disconnect_openvpn
            ;;
        "wireguard")
            vpn_disconnect_wireguard "$interface"
            ;;
        "ipsec")
            vpn_disconnect_ipsec "$interface"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported VPN type: $VPN_TYPE\"}"
            return 1
            ;;
    esac
}

# OpenVPN disconnect
vpn_disconnect_openvpn() {
    if [[ -f "/tmp/openvpn.pid" ]]; then
        local pid=$(cat /tmp/openvpn.pid)
        if kill "$pid" 2>/dev/null; then
            rm -f /tmp/openvpn.pid
            echo "{\"status\":\"success\",\"message\":\"OpenVPN disconnected\"}"
        else
            echo "{\"status\":\"error\",\"message\":\"Failed to disconnect OpenVPN\"}"
            return 1
        fi
    else
        # Try to kill any openvpn processes
        pkill -f openvpn
        echo "{\"status\":\"success\",\"message\":\"OpenVPN processes terminated\"}"
    fi
}

# WireGuard disconnect
vpn_disconnect_wireguard() {
    local interface="$1"
    
    if [[ -z "$interface" ]]; then
        echo "{\"status\":\"error\",\"message\":\"WireGuard interface is required\"}"
        return 1
    fi
    
    wg-quick down "$interface" 2>&1
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"WireGuard disconnected\",\"interface\":\"$interface\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"WireGuard disconnect failed\",\"error_code\":$exit_code}"
        return 1
    fi
}

# IPSec disconnect
vpn_disconnect_ipsec() {
    local connection="$1"
    
    ipsec down "$connection" 2>&1
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"IPSec disconnected\",\"connection\":\"$connection\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"IPSec disconnect failed\",\"error_code\":$exit_code}"
        return 1
    fi
}

# VPN status
vpn_status() {
    case "$VPN_TYPE" in
        "openvpn")
            vpn_status_openvpn
            ;;
        "wireguard")
            vpn_status_wireguard
            ;;
        "ipsec")
            vpn_status_ipsec
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported VPN type: $VPN_TYPE\"}"
            return 1
            ;;
    esac
}

# OpenVPN status
vpn_status_openvpn() {
    local status="disconnected"
    local details=""
    
    if pgrep -f openvpn >/dev/null; then
        status="connected"
        local pid=$(pgrep -f openvpn)
        details="PID: $pid"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"OpenVPN status\",\"connection_status\":\"$status\",\"details\":\"$details\"}"
}

# WireGuard status
vpn_status_wireguard() {
    local interfaces=$(wg show interfaces 2>/dev/null)
    local status="disconnected"
    local details=""
    
    if [[ -n "$interfaces" ]]; then
        status="connected"
        details="Interfaces: $interfaces"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"WireGuard status\",\"connection_status\":\"$status\",\"details\":\"$details\"}"
}

# IPSec status
vpn_status_ipsec() {
    local status_output=$(ipsec status 2>/dev/null)
    local status="disconnected"
    local details=""
    
    if [[ -n "$status_output" ]]; then
        status="connected"
        details="$status_output"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"IPSec status\",\"connection_status\":\"$status\",\"details\":\"$details\"}"
}

# Get VPN configuration
vpn_config() {
    echo "{\"status\":\"success\",\"config\":{\"type\":\"$VPN_TYPE\",\"config_path\":\"$VPN_CONFIG_PATH\",\"interface\":\"$VPN_INTERFACE\",\"timeout\":\"$VPN_TIMEOUT\",\"retry_count\":\"$VPN_RETRY_COUNT\"}}"
}

# Main VPN operator function
execute_vpn() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local type=$(echo "$params" | grep -o 'type=[^,]*' | cut -d'=' -f2)
            local config_path=$(echo "$params" | grep -o 'config_path=[^,]*' | cut -d'=' -f2)
            local username=$(echo "$params" | grep -o 'username=[^,]*' | cut -d'=' -f2)
            local password=$(echo "$params" | grep -o 'password=[^,]*' | cut -d'=' -f2)
            local interface=$(echo "$params" | grep -o 'interface=[^,]*' | cut -d'=' -f2)
            local timeout=$(echo "$params" | grep -o 'timeout=[^,]*' | cut -d'=' -f2)
            local retry_count=$(echo "$params" | grep -o 'retry_count=[^,]*' | cut -d'=' -f2)
            vpn_init "$type" "$config_path" "$username" "$password" "$interface" "$timeout" "$retry_count"
            ;;
        "connect")
            local config_path=$(echo "$params" | grep -o 'config_path=[^,]*' | cut -d'=' -f2)
            local username=$(echo "$params" | grep -o 'username=[^,]*' | cut -d'=' -f2)
            local password=$(echo "$params" | grep -o 'password=[^,]*' | cut -d'=' -f2)
            vpn_connect "$config_path" "$username" "$password"
            ;;
        "disconnect")
            local interface=$(echo "$params" | grep -o 'interface=[^,]*' | cut -d'=' -f2)
            vpn_disconnect "$interface"
            ;;
        "status")
            vpn_status
            ;;
        "config")
            vpn_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, connect, disconnect, status, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_vpn vpn_init vpn_connect vpn_disconnect vpn_status vpn_config 