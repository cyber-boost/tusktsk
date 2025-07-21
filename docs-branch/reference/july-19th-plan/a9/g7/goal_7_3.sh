#!/bin/bash

# Vault Operator Implementation
# Provides secrets management and secure storage with encryption

# Global variables
VAULT_STORAGE_DIR="/tmp/vault"
VAULT_MASTER_KEY=""
VAULT_ENCRYPTION_ALGO="aes-256-gcm"
VAULT_KEY_ROTATION_DAYS="90"
VAULT_BACKUP_DIR="/tmp/vault/backups"

# Initialize Vault operator
vault_init() {
    local master_key="$1"
    local storage_dir="$2"
    local backup_dir="$3"
    
    # Set storage directory
    if [[ -n "$storage_dir" ]]; then
        VAULT_STORAGE_DIR="$storage_dir"
    fi
    
    # Set backup directory
    if [[ -n "$backup_dir" ]]; then
        VAULT_BACKUP_DIR="$backup_dir"
    fi
    
    # Create storage directories
    mkdir -p "$VAULT_STORAGE_DIR"
    mkdir -p "$VAULT_BACKUP_DIR"
    
    # Set master key
    if [[ -n "$master_key" ]]; then
        VAULT_MASTER_KEY="$master_key"
    else
        # Generate a random master key if none provided
        VAULT_MASTER_KEY=$(openssl rand -base64 32 2>/dev/null || echo "default-master-key-$(date +%s)")
    fi
    
    # Create vault metadata file
    local metadata_file="$VAULT_STORAGE_DIR/.vault_metadata"
    cat > "$metadata_file" <<EOF
{
  "created_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "encryption_algorithm": "$VAULT_ENCRYPTION_ALGO",
  "key_rotation_days": $VAULT_KEY_ROTATION_DAYS,
  "version": "1.0"
}
EOF
    
    echo "{\"status\":\"success\",\"message\":\"Vault initialized\",\"storage_dir\":\"$VAULT_STORAGE_DIR\",\"backup_dir\":\"$VAULT_BACKUP_DIR\"}"
}

# Generate encryption key
vault_generate_key() {
    local key_name="$1"
    local key_type="$2"
    
    if [[ -z "$key_name" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Key name is required\"}"
        return 1
    fi
    
    local key_file="$VAULT_STORAGE_DIR/${key_name}.key"
    local key_data=""
    
    case "$key_type" in
        "aes")
            key_data=$(openssl rand -base64 32 2>/dev/null || echo "aes-key-$(date +%s)")
            ;;
        "rsa")
            # Generate RSA key pair
            local private_key="$VAULT_STORAGE_DIR/${key_name}_private.pem"
            local public_key="$VAULT_STORAGE_DIR/${key_name}_public.pem"
            openssl genrsa -out "$private_key" 2048 2>/dev/null
            openssl rsa -in "$private_key" -pubout -out "$public_key" 2>/dev/null
            key_data="RSA_KEY_PAIR"
            ;;
        "hmac")
            key_data=$(openssl rand -hex 32 2>/dev/null || echo "hmac-key-$(date +%s)")
            ;;
        *)
            key_data=$(openssl rand -base64 32 2>/dev/null || echo "key-$(date +%s)")
            ;;
    esac
    
    # Encrypt and store the key
    echo "$key_data" | openssl enc -aes-256-gcm -a -salt -pass pass:"$VAULT_MASTER_KEY" > "$key_file" 2>/dev/null
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Key generated\",\"key_name\":\"$key_name\",\"key_type\":\"$key_type\",\"key_file\":\"$key_file\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to generate key\"}"
        return 1
    fi
}

# Store secret
vault_store() {
    local secret_name="$1"
    local secret_value="$2"
    local secret_type="$3"
    local ttl="$4"
    
    if [[ -z "$secret_name" || -z "$secret_value" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Secret name and value are required\"}"
        return 1
    fi
    
    local secret_file="$VAULT_STORAGE_DIR/${secret_name}.secret"
    local metadata_file="$VAULT_STORAGE_DIR/${secret_name}.meta"
    
    # Create metadata
    local created_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)
    local expires_at=""
    if [[ -n "$ttl" ]]; then
        local expiry_timestamp=$(date -d "+$ttl seconds" -u +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || echo "")
        expires_at="$expiry_timestamp"
    fi
    
    # Store metadata
    cat > "$metadata_file" <<EOF
{
  "name": "$secret_name",
  "type": "$secret_type",
  "created_at": "$created_at",
  "expires_at": "$expires_at",
  "encrypted": true
}
EOF
    
    # Encrypt and store secret
    echo "$secret_value" | openssl enc -aes-256-gcm -a -salt -pass pass:"$VAULT_MASTER_KEY" > "$secret_file" 2>/dev/null
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Secret stored\",\"secret_name\":\"$secret_name\",\"created_at\":\"$created_at\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to store secret\"}"
        return 1
    fi
}

# Retrieve secret
vault_retrieve() {
    local secret_name="$1"
    
    if [[ -z "$secret_name" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Secret name is required\"}"
        return 1
    fi
    
    local secret_file="$VAULT_STORAGE_DIR/${secret_name}.secret"
    local metadata_file="$VAULT_STORAGE_DIR/${secret_name}.meta"
    
    if [[ ! -f "$secret_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Secret not found: $secret_name\"}"
        return 1
    fi
    
    # Check if secret is expired
    if [[ -f "$metadata_file" ]]; then
        local expires_at=$(grep -o '"expires_at":"[^"]*"' "$metadata_file" | cut -d'"' -f4)
        if [[ -n "$expires_at" && "$expires_at" != "null" ]]; then
            local current_time=$(date -u +%Y-%m-%dT%H:%M:%SZ)
            if [[ "$current_time" > "$expires_at" ]]; then
                echo "{\"status\":\"error\",\"message\":\"Secret has expired\",\"expired_at\":\"$expires_at\"}"
                return 1
            fi
        fi
    fi
    
    # Decrypt and retrieve secret
    local secret_value=$(openssl enc -aes-256-gcm -a -d -salt -pass pass:"$VAULT_MASTER_KEY" < "$secret_file" 2>/dev/null)
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"secret_name\":\"$secret_name\",\"secret_value\":\"$secret_value\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to decrypt secret\"}"
        return 1
    fi
}

# List secrets
vault_list() {
    local pattern="$1"
    
    local secrets=()
    for secret_file in "$VAULT_STORAGE_DIR"/*.secret; do
        if [[ -f "$secret_file" ]]; then
            local secret_name=$(basename "$secret_file" .secret)
            if [[ -z "$pattern" || "$secret_name" == *"$pattern"* ]]; then
                secrets+=("$secret_name")
            fi
        fi
    done
    
    if [[ ${#secrets[@]} -eq 0 ]]; then
        echo "{\"status\":\"success\",\"secrets\":[],\"count\":0}"
    else
        local secrets_json=$(printf '"%s",' "${secrets[@]}" | sed 's/,$//')
        echo "{\"status\":\"success\",\"secrets\":[$secrets_json],\"count\":${#secrets[@]}}"
    fi
}

# Delete secret
vault_delete() {
    local secret_name="$1"
    
    if [[ -z "$secret_name" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Secret name is required\"}"
        return 1
    fi
    
    local secret_file="$VAULT_STORAGE_DIR/${secret_name}.secret"
    local metadata_file="$VAULT_STORAGE_DIR/${secret_name}.meta"
    
    if [[ ! -f "$secret_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Secret not found: $secret_name\"}"
        return 1
    fi
    
    # Create backup before deletion
    local backup_file="$VAULT_BACKUP_DIR/${secret_name}_$(date +%Y%m%d_%H%M%S).backup"
    cp "$secret_file" "$backup_file" 2>/dev/null
    
    # Delete files
    rm -f "$secret_file" "$metadata_file"
    
    echo "{\"status\":\"success\",\"message\":\"Secret deleted\",\"secret_name\":\"$secret_name\",\"backup_created\":\"$backup_file\"}"
}

# Rotate secret
vault_rotate() {
    local secret_name="$1"
    local new_value="$2"
    
    if [[ -z "$secret_name" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Secret name is required\"}"
        return 1
    fi
    
    # Retrieve current secret
    local current_result=$(vault_retrieve "$secret_name")
    if [[ $? -ne 0 ]]; then
        echo "$current_result"
        return 1
    fi
    
    local current_value=$(echo "$current_result" | grep -o '"secret_value":"[^"]*"' | cut -d'"' -f4)
    
    # Use current value if no new value provided
    if [[ -z "$new_value" ]]; then
        new_value="$current_value"
    fi
    
    # Store new value
    local store_result=$(vault_store "$secret_name" "$new_value" "rotated" "")
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Secret rotated\",\"secret_name\":\"$secret_name\"}"
    else
        echo "$store_result"
        return 1
    fi
}

# Backup vault
vault_backup() {
    local backup_name="$1"
    
    if [[ -z "$backup_name" ]]; then
        backup_name="vault_backup_$(date +%Y%m%d_%H%M%S)"
    fi
    
    local backup_file="$VAULT_BACKUP_DIR/${backup_name}.tar.gz"
    
    # Create backup archive
    tar -czf "$backup_file" -C "$VAULT_STORAGE_DIR" . 2>/dev/null
    
    if [[ $? -eq 0 ]]; then
        local backup_size=$(stat -c%s "$backup_file" 2>/dev/null || echo "0")
        echo "{\"status\":\"success\",\"message\":\"Vault backed up\",\"backup_file\":\"$backup_file\",\"size_bytes\":$backup_size}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to create backup\"}"
        return 1
    fi
}

# Restore vault
vault_restore() {
    local backup_file="$1"
    
    if [[ -z "$backup_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Backup file is required\"}"
        return 1
    fi
    
    if [[ ! -f "$backup_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Backup file not found: $backup_file\"}"
        return 1
    fi
    
    # Create backup of current vault
    vault_backup "pre_restore_backup_$(date +%Y%m%d_%H%M%S)"
    
    # Restore from backup
    tar -xzf "$backup_file" -C "$VAULT_STORAGE_DIR" 2>/dev/null
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Vault restored\",\"backup_file\":\"$backup_file\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to restore vault\"}"
        return 1
    fi
}

# Get vault status
vault_status() {
    local secret_count=0
    local key_count=0
    
    # Count secrets
    for secret_file in "$VAULT_STORAGE_DIR"/*.secret; do
        if [[ -f "$secret_file" ]]; then
            ((secret_count++))
        fi
    done
    
    # Count keys
    for key_file in "$VAULT_STORAGE_DIR"/*.key; do
        if [[ -f "$key_file" ]]; then
            ((key_count++))
        fi
    done
    
    # Count backups
    local backup_count=0
    for backup_file in "$VAULT_BACKUP_DIR"/*.tar.gz; do
        if [[ -f "$backup_file" ]]; then
            ((backup_count++))
        fi
    done
    
    echo "{\"status\":\"success\",\"vault_status\":{\"secrets\":$secret_count,\"keys\":$key_count,\"backups\":$backup_count,\"storage_dir\":\"$VAULT_STORAGE_DIR\",\"backup_dir\":\"$VAULT_BACKUP_DIR\"}}"
}

# Main Vault operator function
execute_vault() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local master_key=$(echo "$params" | grep -o 'master_key=[^,]*' | cut -d'=' -f2)
            local storage_dir=$(echo "$params" | grep -o 'storage_dir=[^,]*' | cut -d'=' -f2)
            local backup_dir=$(echo "$params" | grep -o 'backup_dir=[^,]*' | cut -d'=' -f2)
            vault_init "$master_key" "$storage_dir" "$backup_dir"
            ;;
        "generate_key")
            local key_name=$(echo "$params" | grep -o 'key_name=[^,]*' | cut -d'=' -f2)
            local key_type=$(echo "$params" | grep -o 'key_type=[^,]*' | cut -d'=' -f2)
            vault_generate_key "$key_name" "$key_type"
            ;;
        "store")
            local secret_name=$(echo "$params" | grep -o 'secret_name=[^,]*' | cut -d'=' -f2)
            local secret_value=$(echo "$params" | grep -o 'secret_value=[^,]*' | cut -d'=' -f2)
            local secret_type=$(echo "$params" | grep -o 'secret_type=[^,]*' | cut -d'=' -f2)
            local ttl=$(echo "$params" | grep -o 'ttl=[^,]*' | cut -d'=' -f2)
            vault_store "$secret_name" "$secret_value" "$secret_type" "$ttl"
            ;;
        "retrieve")
            local secret_name=$(echo "$params" | grep -o 'secret_name=[^,]*' | cut -d'=' -f2)
            vault_retrieve "$secret_name"
            ;;
        "list")
            local pattern=$(echo "$params" | grep -o 'pattern=[^,]*' | cut -d'=' -f2)
            vault_list "$pattern"
            ;;
        "delete")
            local secret_name=$(echo "$params" | grep -o 'secret_name=[^,]*' | cut -d'=' -f2)
            vault_delete "$secret_name"
            ;;
        "rotate")
            local secret_name=$(echo "$params" | grep -o 'secret_name=[^,]*' | cut -d'=' -f2)
            local new_value=$(echo "$params" | grep -o 'new_value=[^,]*' | cut -d'=' -f2)
            vault_rotate "$secret_name" "$new_value"
            ;;
        "backup")
            local backup_name=$(echo "$params" | grep -o 'backup_name=[^,]*' | cut -d'=' -f2)
            vault_backup "$backup_name"
            ;;
        "restore")
            local backup_file=$(echo "$params" | grep -o 'backup_file=[^,]*' | cut -d'=' -f2)
            vault_restore "$backup_file"
            ;;
        "status")
            vault_status
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, generate_key, store, retrieve, list, delete, rotate, backup, restore, status\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_vault vault_init vault_generate_key vault_store vault_retrieve vault_list vault_delete vault_rotate vault_backup vault_restore vault_status 