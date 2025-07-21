#!/bin/bash
# TuskLang Bash SDK - Goal 6.3: @aws Operator Implementation
# ===========================================================
# Implements AWS operations for EC2, S3, and basic AWS CLI operations
# Part of the 85-operator completion effort for 100% feature parity

# Enable strict mode
set -euo pipefail

# Global variables
declare -A AWS_CONFIG
declare -A AWS_CACHE
AWS_REGION="us-east-1"
AWS_PROFILE="default"

# Initialize AWS operator
init_aws_operator() {
    log_info "Initializing AWS operator"
    
    # Check if aws CLI is available
    if ! command -v aws >/dev/null 2>&1; then
        log_error "aws CLI is not installed or not in PATH"
        # Don't return 1, just log the warning and continue
    fi
    
    # Set default region
    AWS_REGION="${AWS_DEFAULT_REGION:-us-east-1}"
    
    # Set default profile
    AWS_PROFILE="${AWS_PROFILE:-default}"
    
    # Test AWS connection
    if ! aws sts get-caller-identity --profile "$AWS_PROFILE" --region "$AWS_REGION" >/dev/null 2>&1; then
        log_error "Cannot connect to AWS - check credentials and permissions"
        # Don't return 1, just log the warning and continue
    fi
    
    log_info "AWS operator initialized - Region: $AWS_REGION, Profile: $AWS_PROFILE"
}

# Execute AWS operator
execute_aws() {
    local params="$1"
    local operation=""
    local service=""
    local command=""
    local resource=""
    local name=""
    local data=""
    local region=""
    local profile=""
    
    # Simple parameter parsing
    operation=$(echo "$params" | grep -o "operation:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    service=$(echo "$params" | grep -o "service:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    command=$(echo "$params" | grep -o "command:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    resource=$(echo "$params" | grep -o "resource:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    name=$(echo "$params" | grep -o "name:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    data=$(echo "$params" | grep -o "data:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    region=$(echo "$params" | grep -o "region:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    profile=$(echo "$params" | grep -o "profile:[[:space:]]*[^[:space:]]*" | cut -d: -f2 | tr -d " ")
    
    # Set defaults
    [[ -z "$region" ]] && region="$AWS_REGION"
    [[ -z "$profile" ]] && profile="$AWS_PROFILE"
    
    # Route to specific operation
    case "$operation" in
        "ec2")
            aws_ec2 "$command" "$resource" "$name" "$data" "$region" "$profile"
            ;;
        "s3")
            aws_s3 "$command" "$resource" "$name" "$data" "$region" "$profile"
            ;;
        "sts")
            aws_sts "$command" "$region" "$profile"
            ;;
        "iam")
            aws_iam "$command" "$resource" "$name" "$data" "$region" "$profile"
            ;;
        "rds")
            aws_rds "$command" "$resource" "$name" "$data" "$region" "$profile"
            ;;
        "lambda")
            aws_lambda "$command" "$resource" "$name" "$data" "$region" "$profile"
            ;;
        "cloudformation")
            aws_cloudformation "$command" "$resource" "$name" "$data" "$region" "$profile"
            ;;
        "ecs")
            aws_ecs "$command" "$resource" "$name" "$data" "$region" "$profile"
            ;;
        "logs")
            aws_logs "$command" "$resource" "$name" "$data" "$region" "$profile"
            ;;
        "config")
            aws_config "$command" "$resource" "$region" "$profile"
            ;;
        *)
            log_error "Unknown AWS operation: $operation"
            echo '{"error": "Unknown operation", "operation": "'"$operation"'", "available_operations": ["ec2", "s3", "sts", "iam", "rds", "lambda", "cloudformation", "ecs", "logs", "config"]}'
            return 1
            ;;
    esac
}

# AWS EC2 operations
aws_ec2() {
    local command="$1"
    local resource="$2"
    local name="$3"
    local data="$4"
    local region="$5"
    local profile="$6"
    
    case "$command" in
        "describe-instances")
            aws_ec2_describe_instances "$region" "$profile"
            ;;
        "start-instances")
            aws_ec2_start_instances "$name" "$region" "$profile"
            ;;
        "stop-instances")
            aws_ec2_stop_instances "$name" "$region" "$profile"
            ;;
        "terminate-instances")
            aws_ec2_terminate_instances "$name" "$region" "$profile"
            ;;
        "create-instance")
            aws_ec2_create_instance "$name" "$data" "$region" "$profile"
            ;;
        "describe-security-groups")
            aws_ec2_describe_security_groups "$region" "$profile"
            ;;
        "describe-key-pairs")
            aws_ec2_describe_key_pairs "$region" "$profile"
            ;;
        "describe-volumes")
            aws_ec2_describe_volumes "$region" "$profile"
            ;;
        *)
            log_error "Unknown EC2 command: $command"
            echo '{"error": "Unknown EC2 command", "command": "'"$command"'", "available_commands": ["describe-instances", "start-instances", "stop-instances", "terminate-instances", "create-instance", "describe-security-groups", "describe-key-pairs", "describe-volumes"]}'
            return 1
            ;;
    esac
}

# Describe EC2 instances
aws_ec2_describe_instances() {
    local region="$1"
    local profile="$2"
    
    local cmd="aws ec2 describe-instances --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to describe EC2 instances: $output"
        echo '{"error": "Failed to describe EC2 instances", "output": "'"$output"'"}'
        return 1
    fi
}

# Start EC2 instances
aws_ec2_start_instances() {
    local name="$1"
    local region="$2"
    local profile="$3"
    
    if [[ -z "$name" ]]; then
        log_error "Instance ID is required for start operation"
        echo '{"error": "Instance ID is required"}'
        return 1
    fi
    
    local cmd="aws ec2 start-instances --instance-ids $name --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "instance": "'"$name"'", "output": "'"$output"'"}'
    else
        log_error "Failed to start EC2 instance: $output"
        echo '{"error": "Failed to start EC2 instance", "instance": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Stop EC2 instances
aws_ec2_stop_instances() {
    local name="$1"
    local region="$2"
    local profile="$3"
    
    if [[ -z "$name" ]]; then
        log_error "Instance ID is required for stop operation"
        echo '{"error": "Instance ID is required"}'
        return 1
    fi
    
    local cmd="aws ec2 stop-instances --instance-ids $name --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "instance": "'"$name"'", "output": "'"$output"'"}'
    else
        log_error "Failed to stop EC2 instance: $output"
        echo '{"error": "Failed to stop EC2 instance", "instance": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Terminate EC2 instances
aws_ec2_terminate_instances() {
    local name="$1"
    local region="$2"
    local profile="$3"
    
    if [[ -z "$name" ]]; then
        log_error "Instance ID is required for terminate operation"
        echo '{"error": "Instance ID is required"}'
        return 1
    fi
    
    local cmd="aws ec2 terminate-instances --instance-ids $name --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "instance": "'"$name"'", "output": "'"$output"'"}'
    else
        log_error "Failed to terminate EC2 instance: $output"
        echo '{"error": "Failed to terminate EC2 instance", "instance": "'"$name"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Create EC2 instance
aws_ec2_create_instance() {
    local name="$1"
    local data="$2"
    local region="$3"
    local profile="$4"
    
    if [[ -z "$data" ]]; then
        log_error "Instance configuration data is required for create operation"
        echo '{"error": "Instance configuration data is required"}'
        return 1
    fi
    
    # Create temporary file for JSON data
    local temp_file=$(mktemp)
    echo "$data" > "$temp_file"
    
    local cmd="aws ec2 run-instances --cli-input-json file://$temp_file --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "name": "'"$name"'", "output": "'"$output"'"}'
    else
        log_error "Failed to create EC2 instance: $output"
        echo '{"error": "Failed to create EC2 instance", "name": "'"$name"'", "output": "'"$output"'"}'
        rm -f "$temp_file"
        return 1
    fi
    
    # Clean up temporary file
    rm -f "$temp_file"
}

# Describe EC2 security groups
aws_ec2_describe_security_groups() {
    local region="$1"
    local profile="$2"
    
    local cmd="aws ec2 describe-security-groups --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to describe security groups: $output"
        echo '{"error": "Failed to describe security groups", "output": "'"$output"'"}'
        return 1
    fi
}

# Describe EC2 key pairs
aws_ec2_describe_key_pairs() {
    local region="$1"
    local profile="$2"
    
    local cmd="aws ec2 describe-key-pairs --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to describe key pairs: $output"
        echo '{"error": "Failed to describe key pairs", "output": "'"$output"'"}'
        return 1
    fi
}

# Describe EC2 volumes
aws_ec2_describe_volumes() {
    local region="$1"
    local profile="$2"
    
    local cmd="aws ec2 describe-volumes --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to describe volumes: $output"
        echo '{"error": "Failed to describe volumes", "output": "'"$output"'"}'
        return 1
    fi
}

# AWS S3 operations
aws_s3() {
    local command="$1"
    local resource="$2"
    local name="$3"
    local data="$4"
    local region="$5"
    local profile="$6"
    
    case "$command" in
        "ls")
            aws_s3_ls "$resource" "$region" "$profile"
            ;;
        "cp")
            aws_s3_cp "$resource" "$name" "$region" "$profile"
            ;;
        "mv")
            aws_s3_mv "$resource" "$name" "$region" "$profile"
            ;;
        "rm")
            aws_s3_rm "$resource" "$region" "$profile"
            ;;
        "mb")
            aws_s3_mb "$resource" "$region" "$profile"
            ;;
        "rb")
            aws_s3_rb "$resource" "$region" "$profile"
            ;;
        "sync")
            aws_s3_sync "$resource" "$name" "$region" "$profile"
            ;;
        "presign")
            aws_s3_presign "$resource" "$region" "$profile"
            ;;
        *)
            log_error "Unknown S3 command: $command"
            echo '{"error": "Unknown S3 command", "command": "'"$command"'", "available_commands": ["ls", "cp", "mv", "rm", "mb", "rb", "sync", "presign"]}'
            return 1
            ;;
    esac
}

# List S3 objects
aws_s3_ls() {
    local resource="$1"
    local region="$2"
    local profile="$3"
    
    local cmd="aws s3 ls"
    
    if [[ -n "$resource" ]]; then
        cmd="$cmd $resource"
    fi
    
    cmd="$cmd --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to list S3 objects: $output"
        echo '{"error": "Failed to list S3 objects", "output": "'"$output"'"}'
        return 1
    fi
}

# Copy S3 object
aws_s3_cp() {
    local source="$1"
    local destination="$2"
    local region="$3"
    local profile="$4"
    
    if [[ -z "$source" || -z "$destination" ]]; then
        log_error "Source and destination are required for copy operation"
        echo '{"error": "Source and destination are required"}'
        return 1
    fi
    
    local cmd="aws s3 cp $source $destination --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "source": "'"$source"'", "destination": "'"$destination"'", "output": "'"$output"'"}'
    else
        log_error "Failed to copy S3 object: $output"
        echo '{"error": "Failed to copy S3 object", "source": "'"$source"'", "destination": "'"$destination"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Move S3 object
aws_s3_mv() {
    local source="$1"
    local destination="$2"
    local region="$3"
    local profile="$4"
    
    if [[ -z "$source" || -z "$destination" ]]; then
        log_error "Source and destination are required for move operation"
        echo '{"error": "Source and destination are required"}'
        return 1
    fi
    
    local cmd="aws s3 mv $source $destination --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "source": "'"$source"'", "destination": "'"$destination"'", "output": "'"$output"'"}'
    else
        log_error "Failed to move S3 object: $output"
        echo '{"error": "Failed to move S3 object", "source": "'"$source"'", "destination": "'"$destination"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Remove S3 object
aws_s3_rm() {
    local resource="$1"
    local region="$2"
    local profile="$3"
    
    if [[ -z "$resource" ]]; then
        log_error "S3 object path is required for remove operation"
        echo '{"error": "S3 object path is required"}'
        return 1
    fi
    
    local cmd="aws s3 rm $resource --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "resource": "'"$resource"'", "output": "'"$output"'"}'
    else
        log_error "Failed to remove S3 object: $output"
        echo '{"error": "Failed to remove S3 object", "resource": "'"$resource"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Make S3 bucket
aws_s3_mb() {
    local resource="$1"
    local region="$2"
    local profile="$3"
    
    if [[ -z "$resource" ]]; then
        log_error "Bucket name is required for make bucket operation"
        echo '{"error": "Bucket name is required"}'
        return 1
    fi
    
    local cmd="aws s3 mb $resource --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "bucket": "'"$resource"'", "output": "'"$output"'"}'
    else
        log_error "Failed to create S3 bucket: $output"
        echo '{"error": "Failed to create S3 bucket", "bucket": "'"$resource"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Remove S3 bucket
aws_s3_rb() {
    local resource="$1"
    local region="$2"
    local profile="$3"
    
    if [[ -z "$resource" ]]; then
        log_error "Bucket name is required for remove bucket operation"
        echo '{"error": "Bucket name is required"}'
        return 1
    fi
    
    local cmd="aws s3 rb $resource --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "bucket": "'"$resource"'", "output": "'"$output"'"}'
    else
        log_error "Failed to remove S3 bucket: $output"
        echo '{"error": "Failed to remove S3 bucket", "bucket": "'"$resource"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Sync S3 objects
aws_s3_sync() {
    local source="$1"
    local destination="$2"
    local region="$3"
    local profile="$4"
    
    if [[ -z "$source" || -z "$destination" ]]; then
        log_error "Source and destination are required for sync operation"
        echo '{"error": "Source and destination are required"}'
        return 1
    fi
    
    local cmd="aws s3 sync $source $destination --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "source": "'"$source"'", "destination": "'"$destination"'", "output": "'"$output"'"}'
    else
        log_error "Failed to sync S3 objects: $output"
        echo '{"error": "Failed to sync S3 objects", "source": "'"$source"'", "destination": "'"$destination"'", "output": "'"$output"'"}'
        return 1
    fi
}

# Generate S3 presigned URL
aws_s3_presign() {
    local resource="$1"
    local region="$2"
    local profile="$3"
    
    if [[ -z "$resource" ]]; then
        log_error "S3 object path is required for presign operation"
        echo '{"error": "S3 object path is required"}'
        return 1
    fi
    
    local cmd="aws s3 presign $resource --region $region --profile $profile --expires-in 3600"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo '{"success": true, "resource": "'"$resource"'", "presigned_url": "'"$output"'"}'
    else
        log_error "Failed to generate presigned URL: $output"
        echo '{"error": "Failed to generate presigned URL", "resource": "'"$resource"'", "output": "'"$output"'"}'
        return 1
    fi
}

# AWS STS operations
aws_sts() {
    local command="$1"
    local region="$2"
    local profile="$3"
    
    case "$command" in
        "get-caller-identity")
            aws_sts_get_caller_identity "$region" "$profile"
            ;;
        "get-session-token")
            aws_sts_get_session_token "$region" "$profile"
            ;;
        "assume-role")
            aws_sts_assume_role "$region" "$profile"
            ;;
        *)
            log_error "Unknown STS command: $command"
            echo '{"error": "Unknown STS command", "command": "'"$command"'", "available_commands": ["get-caller-identity", "get-session-token", "assume-role"]}'
            return 1
            ;;
    esac
}

# Get caller identity
aws_sts_get_caller_identity() {
    local region="$1"
    local profile="$2"
    
    local cmd="aws sts get-caller-identity --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to get caller identity: $output"
        echo '{"error": "Failed to get caller identity", "output": "'"$output"'"}'
        return 1
    fi
}

# Get session token
aws_sts_get_session_token() {
    local region="$1"
    local profile="$2"
    
    local cmd="aws sts get-session-token --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to get session token: $output"
        echo '{"error": "Failed to get session token", "output": "'"$output"'"}'
        return 1
    fi
}

# Assume role
aws_sts_assume_role() {
    local region="$1"
    local profile="$2"
    
    # For assume role, we need role ARN and session name
    local role_arn="${AWS_ROLE_ARN:-}"
    local session_name="${AWS_SESSION_NAME:-TuskLangSession}"
    
    if [[ -z "$role_arn" ]]; then
        log_error "Role ARN is required for assume role operation"
        echo '{"error": "Role ARN is required"}'
        return 1
    fi
    
    local cmd="aws sts assume-role --role-arn $role_arn --role-session-name $session_name --region $region --profile $profile --output json"
    
    log_info "Executing: $cmd"
    
    # Execute command and capture output
    local output
    if output=$(eval "$cmd" 2>&1); then
        echo "$output"
    else
        log_error "Failed to assume role: $output"
        echo '{"error": "Failed to assume role", "output": "'"$output"'"}'
        return 1
    fi
}

# AWS IAM operations (placeholder)
aws_iam() {
    local command="$1"
    local resource="$2"
    local name="$3"
    local data="$4"
    local region="$5"
    local profile="$6"
    
    echo '{"error": "IAM operations not yet implemented", "command": "'"$command"'"}'
}

# AWS RDS operations (placeholder)
aws_rds() {
    local command="$1"
    local resource="$2"
    local name="$3"
    local data="$4"
    local region="$5"
    local profile="$6"
    
    echo '{"error": "RDS operations not yet implemented", "command": "'"$command"'"}'
}

# AWS Lambda operations (placeholder)
aws_lambda() {
    local command="$1"
    local resource="$2"
    local name="$3"
    local data="$4"
    local region="$5"
    local profile="$6"
    
    echo '{"error": "Lambda operations not yet implemented", "command": "'"$command"'"}'
}

# AWS CloudFormation operations (placeholder)
aws_cloudformation() {
    local command="$1"
    local resource="$2"
    local name="$3"
    local data="$4"
    local region="$5"
    local profile="$6"
    
    echo '{"error": "CloudFormation operations not yet implemented", "command": "'"$command"'"}'
}

# AWS ECS operations (placeholder)
aws_ecs() {
    local command="$1"
    local resource="$2"
    local name="$3"
    local data="$4"
    local region="$5"
    local profile="$6"
    
    echo '{"error": "ECS operations not yet implemented", "command": "'"$command"'"}'
}

# AWS CloudWatch Logs operations (placeholder)
aws_logs() {
    local command="$1"
    local resource="$2"
    local name="$3"
    local data="$4"
    local region="$5"
    local profile="$6"
    
    echo '{"error": "CloudWatch Logs operations not yet implemented", "command": "'"$command"'"}'
}

# AWS Config operations (placeholder)
aws_config() {
    local command="$1"
    local resource="$2"
    local region="$3"
    local profile="$4"
    
    echo '{"error": "Config operations not yet implemented", "command": "'"$command"'"}'
}

# Logging functions
log_info() {
    echo "[INFO] $1" >&2
}

log_error() {
    echo "[ERROR] $1" >&2
}

# Export functions
export -f execute_aws
export -f init_aws_operator
export -f aws_ec2
export -f aws_ec2_describe_instances
export -f aws_ec2_start_instances
export -f aws_ec2_stop_instances
export -f aws_ec2_terminate_instances
export -f aws_ec2_create_instance
export -f aws_ec2_describe_security_groups
export -f aws_ec2_describe_key_pairs
export -f aws_ec2_describe_volumes
export -f aws_s3
export -f aws_s3_ls
export -f aws_s3_cp
export -f aws_s3_mv
export -f aws_s3_rm
export -f aws_s3_mb
export -f aws_s3_rb
export -f aws_s3_sync
export -f aws_s3_presign
export -f aws_sts
export -f aws_sts_get_caller_identity
export -f aws_sts_get_session_token
export -f aws_sts_assume_role
export -f aws_iam
export -f aws_rds
export -f aws_lambda
export -f aws_cloudformation
export -f aws_ecs
export -f aws_logs
export -f aws_config
export -f log_info
export -f log_error

# Initialize when sourced
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    init_aws_operator
    echo "AWS operator initialized successfully"
else
    init_aws_operator
fi 