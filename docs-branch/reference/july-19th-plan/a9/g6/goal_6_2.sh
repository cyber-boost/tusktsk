#!/bin/bash
# TuskLang Bash SDK - Goal 6.2: @docker Operator Implementation
# =============================================================
# Implements Docker operations for container lifecycle, image management, and volume operations
# Part of the 85-operator completion effort for 100% feature parity

# Enable strict mode
set -euo pipefail

# Global variables
declare -A DOCKER_CONFIG
declare -A DOCKER_CACHE
DOCKER_HOST=""

# Initialize Docker operator
init_docker_operator() {
    log_info "Initializing Docker operator"
    
    # Check if docker is available
    if ! command -v docker >/dev/null 2>&1; then
        log_error "docker is not installed or not in PATH"
        # Don't return 1, just log the warning and continue
    fi
    
    # Get Docker host
    DOCKER_HOST="${DOCKER_HOST:-unix:///var/run/docker.sock}"
    
    # Test Docker connection
    if ! docker info >/dev/null 2>&1; then
        log_error "Cannot connect to Docker daemon"
        return 1
    fi
    
    log_info "Docker operator initialized - Host: $DOCKER_HOST"
}

# Execute Docker operator
execute_docker() {
    local params="$1"
    local operation=""
    local resource=""
    local name=""
    local image=""
    local command=""
    local port=""
    local volume=""
    
    # Simple parameter parsing
    operation=$(echo "$params" | grep -o "operation:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    resource=$(echo "$params" | grep -o "resource:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    name=$(echo "$params" | grep -o "name:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    image=$(echo "$params" | grep -o "image:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    command=$(echo "$params" | grep -o "command:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    port=$(echo "$params" | grep -o "port:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    volume=$(echo "$params" | grep -o "volume:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    
    # Route to specific operation
    case "$operation" in
        "run")
            docker_run "$image" "$name" "$command" "$port" "$volume"
            ;;
        "start")
            docker_start "$name"
            ;;
        "stop")
            docker_stop "$name"
            ;;
        "restart")
            docker_restart "$name"
            ;;
        "rm")
            docker_rm "$name"
            ;;
        "ps")
            docker_ps "$resource"
            ;;
        "logs")
            docker_logs "$name"
            ;;
        "exec")
            docker_exec "$name" "$command"
            ;;
        "inspect")
            docker_inspect "$resource" "$name"
            ;;
        "pull")
            docker_pull "$image"
            ;;
        "push")
            docker_push "$image"
            ;;
        "build")
            docker_build "$name" "$params"
            ;;
        "tag")
            docker_tag "$image" "$name"
            ;;
        "rmi")
            docker_rmi "$image"
            ;;
        "images")
            docker_images
            ;;
        "volume")
            docker_volume "$params"
            ;;
        "network")
            docker_network "$params"
            ;;
        "stats")
            docker_stats "$name"
            ;;
        "info")
            docker_info
            ;;
        *)
            log_error "Unknown Docker operation: $operation"
            echo '{"error": "Unknown operation", "operation": "'"$operation"'", "available_operations": ["run", "start", "stop", "restart", "rm", "ps", "logs", "exec", "inspect", "pull", "push", "build", "tag", "rmi", "images", "volume", "network", "stats", "info"]}'
            return 1
            ;;
    esac
}

# Run Docker container
docker_run() {
    local image="$1"
    local name="$2"
    local command="$3"
    local port="$4"
    local volume="$5"
    
    if [[ -z "$image" ]]; then
        log_error "Image is required for run operation"
        echo '{"error": "Image is required"}'
        return 1
    fi
    
    local cmd="docker run -d"
    
    if [[ -n "$name" ]]; then
        cmd="$cmd --name $name"
    fi
    
    if [[ -n "$port" ]]; then
        cmd="$cmd -p $port"
    fi
    
    if [[ -n "$volume" ]]; then
        cmd="$cmd -v $volume"
    fi
    
    cmd="$cmd $image"
    
    if [[ -n "$command" ]]; then
        cmd="$cmd $command"
    fi
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "container_id": "'"$output"'", "image": "'"$image"'", "name": "'"$name"'"}'
    else
        log_error "Failed to run container: $output"
        echo '{"error": "Failed to run container", "image": "'"$image"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Start Docker container
docker_start() {
    local name="$1"
    
    if [[ -z "$name" ]]; then
        log_error "Container name is required for start operation"
        echo '{"error": "Container name is required"}'
        return 1
    fi
    
    local cmd="docker start $name"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "container": "'"$name"'", "output": "'"$output"'"}'
    else
        log_error "Failed to start container: $output"
        echo '{"error": "Failed to start container", "container": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Stop Docker container
docker_stop() {
    local name="$1"
    
    if [[ -z "$name" ]]; then
        log_error "Container name is required for stop operation"
        echo '{"error": "Container name is required"}'
        return 1
    fi
    
    local cmd="docker stop $name"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "container": "'"$name"'", "output": "'"$output"'"}'
    else
        log_error "Failed to stop container: $output"
        echo '{"error": "Failed to stop container", "container": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Restart Docker container
docker_restart() {
    local name="$1"
    
    if [[ -z "$name" ]]; then
        log_error "Container name is required for restart operation"
        echo '{"error": "Container name is required"}'
        return 1
    fi
    
    local cmd="docker restart $name"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "container": "'"$name"'", "output": "'"$output"'"}'
    else
        log_error "Failed to restart container: $output"
        echo '{"error": "Failed to restart container", "container": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Remove Docker container
docker_rm() {
    local name="$1"
    
    if [[ -z "$name" ]]; then
        log_error "Container name is required for rm operation"
        echo '{"error": "Container name is required"}'
        return 1
    fi
    
    local cmd="docker rm $name"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "container": "'"$name"'", "output": "'"$output"'"}'
    else
        log_error "Failed to remove container: $output"
        echo '{"error": "Failed to remove container", "container": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# List Docker containers
docker_ps() {
    local resource="$1"
    
    local cmd="docker ps"
    
    if [[ "$resource" == "all" ]]; then
        cmd="$cmd -a"
    fi
    
    cmd="$cmd --format json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to list containers: $output"
        echo '{"error": "Failed to list containers", "output": "'"$output"'"}'
        return 1
    fi
}

# Get Docker container logs
docker_logs() {
    local name="$1"
    
    if [[ -z "$name" ]]; then
        log_error "Container name is required for logs operation"
        echo '{"error": "Container name is required"}'
        return 1
    fi
    
    local cmd="docker logs $name"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to get logs: $output"
        echo '{"error": "Failed to get logs", "container": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Execute command in Docker container
docker_exec() {
    local name="$1"
    local command="$2"
    
    if [[ -z "$name" ]]; then
        log_error "Container name is required for exec operation"
        echo '{"error": "Container name is required"}'
        return 1
    fi
    
    if [[ -z "$command" ]]; then
        log_error "Command is required for exec operation"
        echo '{"error": "Command is required"}'
        return 1
    fi
    
    local cmd="docker exec $name $command"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "container": "'"$name"'", "command": "'"$command"'", "output": "'"$output"'"}'
    else
        log_error "Failed to exec command: $output"
        echo '{"error": "Failed to exec command", "container": "'"$name"'", "command": "'"$command"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Inspect Docker resource
docker_inspect() {
    local resource="$1"
    local name="$2"
    
    if [[ -z "$resource" || -z "$name" ]]; then
        log_error "Resource type and name are required for inspect operation"
        echo '{"error": "Resource type and name are required"}'
        return 1
    fi
    
    local cmd="docker inspect $name"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to inspect $resource: $output"
        echo '{"error": "Failed to inspect resource", "resource": "'"$resource"'", "name": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Pull Docker image
docker_pull() {
    local image="$1"
    
    if [[ -z "$image" ]]; then
        log_error "Image is required for pull operation"
        echo '{"error": "Image is required"}'
        return 1
    fi
    
    local cmd="docker pull $image"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "image": "'"$image"'", "output": "'"$output"'"}'
    else
        log_error "Failed to pull image: $output"
        echo '{"error": "Failed to pull image", "image": "'"$image"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Push Docker image
docker_push() {
    local image="$1"
    
    if [[ -z "$image" ]]; then
        log_error "Image is required for push operation"
        echo '{"error": "Image is required"}'
        return 1
    fi
    
    local cmd="docker push $image"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "image": "'"$image"'", "output": "'"$output"'"}'
    else
        log_error "Failed to push image: $output"
        echo '{"error": "Failed to push image", "image": "'"$image"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Build Docker image
docker_build() {
    local name="$1"
    local params="$2"
    
    if [[ -z "$name" ]]; then
        log_error "Image name is required for build operation"
        echo '{"error": "Image name is required"}'
        return 1
    fi
    
    # Extract path from params
    local path=$(echo "$params" | grep -o "path:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    [[ -z "$path" ]] && path="."
    
    local cmd="docker build -t $name $path"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "image": "'"$name"'", "path": "'"$path"'", "output": "'"$output"'"}'
    else
        log_error "Failed to build image: $output"
        echo '{"error": "Failed to build image", "image": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Tag Docker image
docker_tag() {
    local image="$1"
    local name="$2"
    
    if [[ -z "$image" || -z "$name" ]]; then
        log_error "Source image and target name are required for tag operation"
        echo '{"error": "Source image and target name are required"}'
        return 1
    fi
    
    local cmd="docker tag $image $name"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "source": "'"$image"'", "target": "'"$name"'"}'
    else
        log_error "Failed to tag image: $output"
        echo '{"error": "Failed to tag image", "source": "'"$image"'", "target": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Remove Docker image
docker_rmi() {
    local image="$1"
    
    if [[ -z "$image" ]]; then
        log_error "Image is required for rmi operation"
        echo '{"error": "Image is required"}'
        return 1
    fi
    
    local cmd="docker rmi $image"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "image": "'"$image"'", "output": "'"$output"'"}'
    else
        log_error "Failed to remove image: $output"
        echo '{"error": "Failed to remove image", "image": "'"$image"'", "output": "'"$output"'"}'
        return 1
    fi
}

# List Docker images
docker_images() {
    local cmd="docker images --format json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to list images: $output"
        echo '{"error": "Failed to list images", "output": "'"$output"'"}'
        return 1
    fi
}

# Docker volume operations
docker_volume() {
    local params="$1"
    
    # Extract operation from params
    local operation=$(echo "$params" | grep -o "volume_operation:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    
    # Extract volume name from params
    local volume_name=$(echo "$params" | grep -o "volume_name:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    
    case "$operation" in
        "create")
            if [[ -z "$volume_name" ]]; then
                log_error "Volume name is required for create operation"
                echo '{"error": "Volume name is required"}'
                return 1
            fi
            local cmd="docker volume create $volume_name"
            log_info "Executing: $cmd"
            local output
            if output=$(eval "$cmd" 2>&1); then
                echo '{"success": true, "volume": "'"$volume_name"'", "output": "'"$output"'"}'
            else
                log_error "Failed to create volume: $output"
                echo '{"error": "Failed to create volume", "volume": "'"$volume_name"'", "output": "'"$output"'"}'
                return 1
            fi
            ;;
        "ls")
            local cmd="docker volume ls --format json"
            log_info "Executing: $cmd"
            local output
            if output=$(eval "$cmd" 2>&1); then
                echo "$output"
            else
                log_error "Failed to list volumes: $output"
                echo '{"error": "Failed to list volumes", "output": "'"$output"'"}'
                return 1
            fi
            ;;
        "rm")
            if [[ -z "$volume_name" ]]; then
                log_error "Volume name is required for rm operation"
                echo '{"error": "Volume name is required"}'
                return 1
            fi
            local cmd="docker volume rm $volume_name"
            log_info "Executing: $cmd"
            local output
            if output=$(eval "$cmd" 2>&1); then
                echo '{"success": true, "volume": "'"$volume_name"'", "output": "'"$output"'"}'
            else
                log_error "Failed to remove volume: $output"
                echo '{"error": "Failed to remove volume", "volume": "'"$volume_name"'", "output": "'"$output"'"}'
                return 1
            fi
            ;;
        *)
            log_error "Unknown volume operation: $operation"
            echo '{"error": "Unknown volume operation", "operation": "'"$operation"'", "available_operations": ["create", "ls", "rm"]}'
            return 1
            ;;
    esac
}

# Docker network operations
docker_network() {
    local params="$1"
    
    # Extract operation from params
    local operation=$(echo "$params" | grep -o "network_operation:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    
    # Extract network name from params
    local network_name=$(echo "$params" | grep -o "network_name:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    
    case "$operation" in
        "create")
            if [[ -z "$network_name" ]]; then
                log_error "Network name is required for create operation"
                echo '{"error": "Network name is required"}'
                return 1
            fi
            local cmd="docker network create $network_name"
            log_info "Executing: $cmd"
            local output
            if output=$(eval "$cmd" 2>&1); then
                echo '{"success": true, "network": "'"$network_name"'", "output": "'"$output"'"}'
            else
                log_error "Failed to create network: $output"
                echo '{"error": "Failed to create network", "network": "'"$network_name"'", "output": "'"$output"'"}'
                return 1
            fi
            ;;
        "ls")
            local cmd="docker network ls --format json"
            log_info "Executing: $cmd"
            local output
            if output=$(eval "$cmd" 2>&1); then
                echo "$output"
            else
                log_error "Failed to list networks: $output"
                echo '{"error": "Failed to list networks", "output": "'"$output"'"}'
                return 1
            fi
            ;;
        "rm")
            if [[ -z "$network_name" ]]; then
                log_error "Network name is required for rm operation"
                echo '{"error": "Network name is required"}'
                return 1
            fi
            local cmd="docker network rm $network_name"
            log_info "Executing: $cmd"
            local output
            if output=$(eval "$cmd" 2>&1); then
                echo '{"success": true, "network": "'"$network_name"'", "output": "'"$output"'"}'
            else
                log_error "Failed to remove network: $output"
                echo '{"error": "Failed to remove network", "network": "'"$network_name"'", "output": "'"$output"'"}'
                return 1
            fi
            ;;
        *)
            log_error "Unknown network operation: $operation"
            echo '{"error": "Unknown network operation", "operation": "'"$operation"'", "available_operations": ["create", "ls", "rm"]}'
            return 1
            ;;
    esac
}

# Get Docker container stats
docker_stats() {
    local name="$1"
    
    local cmd="docker stats"
    
    if [[ -n "$name" ]]; then
        cmd="$cmd $name"
    fi
    
    cmd="$cmd --no-stream --format json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to get stats: $output"
        echo '{"error": "Failed to get stats", "container": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Get Docker system info
docker_info() {
    local cmd="docker info --format json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to get Docker info: $output"
        echo '{"error": "Failed to get Docker info", "output": "'"$output"'"}'
        return 1
    fi
}

# Logging functions
log_info() {
    echo "[INFO] $1" >&2
}

log_error() {
    echo "[ERROR] $1" >&2
}

# Export functions
export -f execute_docker
export -f init_docker_operator
export -f docker_run
export -f docker_start
export -f docker_stop
export -f docker_restart
export -f docker_rm
export -f docker_ps
export -f docker_logs
export -f docker_exec
export -f docker_inspect
export -f docker_pull
export -f docker_push
export -f docker_build
export -f docker_tag
export -f docker_rmi
export -f docker_images
export -f docker_volume
export -f docker_network
export -f docker_stats
export -f docker_info
export -f log_info
export -f log_error

# Initialize when sourced
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    init_docker_operator
    echo "Docker operator initialized successfully"
else
    init_docker_operator
fi 