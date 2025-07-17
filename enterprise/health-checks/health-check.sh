#!/bin/bash

# TuskLang Health Check System - Bash Implementation
# Comprehensive health monitoring for all TuskLang services

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Health status constants
HEALTHY="healthy"
DEGRADED="degraded"
UNHEALTHY="unhealthy"
UNKNOWN="unknown"

# Configuration
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-tusklang}"
DB_USER="${DB_USER:-postgres}"
DB_PASSWORD="${DB_PASSWORD:-}"
REDIS_HOST="${REDIS_HOST:-localhost}"
REDIS_PORT="${REDIS_PORT:-6379}"
REGISTRY_URL="${REGISTRY_URL:-http://localhost:8000}"
CDN_URL="${CDN_URL:-https://cdn.tusklang.org}"
SECURITY_URL="${SECURITY_URL:-http://localhost:9000}"

# Initialize report
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
OVERALL_STATUS="$HEALTHY"
TOTAL_CHECKS=0
HEALTHY_CHECKS=0
DEGRADED_CHECKS=0
UNHEALTHY_CHECKS=0
UNKNOWN_CHECKS=0

# Array to store check results
declare -a CHECK_RESULTS

# Function to add a health check result
add_check() {
    local name="$1"
    local status="$2"
    local message="$3"
    local details="$4"
    
    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
    
    case "$status" in
        "$HEALTHY")
            HEALTHY_CHECKS=$((HEALTHY_CHECKS + 1))
            ;;
        "$DEGRADED")
            DEGRADED_CHECKS=$((DEGRADED_CHECKS + 1))
            if [ "$OVERALL_STATUS" = "$HEALTHY" ]; then
                OVERALL_STATUS="$DEGRADED"
            fi
            ;;
        "$UNHEALTHY"|"$UNKNOWN")
            UNHEALTHY_CHECKS=$((UNHEALTHY_CHECKS + 1))
            OVERALL_STATUS="$UNHEALTHY"
            ;;
    esac
    
    # Store check result
    CHECK_RESULTS+=("$name|$status|$message|$details")
}

# Function to check system resources
check_system_resources() {
    echo -e "${BLUE}Checking system resources...${NC}"
    
    # Get CPU usage (simplified)
    CPU_USAGE=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
    
    # Get memory usage
    MEMORY_INFO=$(free | grep Mem)
    MEMORY_TOTAL=$(echo $MEMORY_INFO | awk '{print $2}')
    MEMORY_USED=$(echo $MEMORY_INFO | awk '{print $3}')
    MEMORY_USAGE=$(awk "BEGIN {printf \"%.1f\", ($MEMORY_USED/$MEMORY_TOTAL)*100}")
    
    # Get disk usage
    DISK_USAGE=$(df / | tail -1 | awk '{print $5}' | sed 's/%//')
    
    # Determine status
    STATUS="$HEALTHY"
    MESSAGE="System resources are normal"
    
    if (( $(echo "$CPU_USAGE > 80" | bc -l) )) || \
       (( $(echo "$MEMORY_USAGE > 85" | bc -l) )) || \
       [ "$DISK_USAGE" -gt 90 ]; then
        STATUS="$DEGRADED"
        MESSAGE="High resource usage - CPU: ${CPU_USAGE}%, Memory: ${MEMORY_USAGE}%, Disk: ${DISK_USAGE}%"
    fi
    
    DETAILS="{\"cpu_percent\":$CPU_USAGE,\"memory_percent\":$MEMORY_USAGE,\"disk_percent\":$DISK_USAGE,\"memory_available\":$((MEMORY_TOTAL - MEMORY_USED))}"
    
    add_check "system_resources" "$STATUS" "$MESSAGE" "$DETAILS"
}

# Function to check database connectivity
check_database() {
    echo -e "${BLUE}Checking database connectivity...${NC}"
    
    if ! command -v psql &> /dev/null; then
        add_check "database" "$UNHEALTHY" "PostgreSQL client not found" "{}"
        return
    fi
    
    # Test database connection
    if PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "SELECT 1;" >/dev/null 2>&1; then
        # Get active connections
        ACTIVE_CONNECTIONS=$(PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT count(*) FROM pg_stat_activity;" 2>/dev/null | xargs)
        
        # Get database size
        DB_SIZE=$(PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT pg_database_size(current_database());" 2>/dev/null | xargs)
        
        STATUS="$HEALTHY"
        MESSAGE="Database is healthy"
        
        if [ "$ACTIVE_CONNECTIONS" -gt 100 ]; then
            STATUS="$DEGRADED"
            MESSAGE="High number of active connections: $ACTIVE_CONNECTIONS"
        fi
        
        DETAILS="{\"active_connections\":$ACTIVE_CONNECTIONS,\"database_size_bytes\":$DB_SIZE,\"database_size_mb\":$((DB_SIZE / 1024 / 1024))}"
    else
        STATUS="$UNHEALTHY"
        MESSAGE="Database connection failed"
        DETAILS="{}"
    fi
    
    add_check "database" "$STATUS" "$MESSAGE" "$DETAILS"
}

# Function to check Redis connectivity
check_redis() {
    echo -e "${BLUE}Checking Redis connectivity...${NC}"
    
    if ! command -v redis-cli &> /dev/null; then
        add_check "redis" "$UNHEALTHY" "Redis client not found" "{}"
        return
    fi
    
    # Test Redis connection
    if redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" ping >/dev/null 2>&1; then
        # Get Redis info
        REDIS_INFO=$(redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" info 2>/dev/null)
        
        # Parse memory usage
        USED_MEMORY=$(echo "$REDIS_INFO" | grep "used_memory:" | cut -d: -f2)
        MAX_MEMORY=$(echo "$REDIS_INFO" | grep "maxmemory:" | cut -d: -f2)
        CONNECTED_CLIENTS=$(echo "$REDIS_INFO" | grep "connected_clients:" | cut -d: -f2)
        
        if [ "$MAX_MEMORY" -gt 0 ]; then
            MEMORY_USAGE_PERCENT=$(awk "BEGIN {printf \"%.1f\", ($USED_MEMORY/$MAX_MEMORY)*100}")
        else
            MEMORY_USAGE_PERCENT=0
        fi
        
        STATUS="$HEALTHY"
        MESSAGE="Redis is healthy"
        
        if (( $(echo "$MEMORY_USAGE_PERCENT > 80" | bc -l) )); then
            STATUS="$DEGRADED"
            MESSAGE="High Redis memory usage: ${MEMORY_USAGE_PERCENT}%"
        elif [ "$CONNECTED_CLIENTS" -gt 100 ]; then
            STATUS="$DEGRADED"
            MESSAGE="High number of Redis clients: $CONNECTED_CLIENTS"
        fi
        
        DETAILS="{\"memory_usage_percent\":$MEMORY_USAGE_PERCENT,\"connected_clients\":$CONNECTED_CLIENTS,\"used_memory_mb\":$((USED_MEMORY / 1024 / 1024)),\"max_memory_mb\":$((MAX_MEMORY / 1024 / 1024))}"
    else
        STATUS="$UNHEALTHY"
        MESSAGE="Redis connection failed"
        DETAILS="{}"
    fi
    
    add_check "redis" "$STATUS" "$MESSAGE" "$DETAILS"
}

# Function to check package registry
check_package_registry() {
    echo -e "${BLUE}Checking package registry...${NC}"
    
    # Test registry health endpoint
    if HTTP_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/registry_response "$REGISTRY_URL/health" 2>/dev/null); then
        HTTP_CODE="${HTTP_RESPONSE: -3}"
        
        if [ "$HTTP_CODE" = "200" ]; then
            HEALTH_DATA=$(cat /tmp/registry_response)
            add_check "package_registry" "$HEALTHY" "Package registry is healthy" "$HEALTH_DATA"
        else
            add_check "package_registry" "$UNHEALTHY" "Registry health endpoint returned status $HTTP_CODE" "{}"
        fi
    else
        add_check "package_registry" "$UNHEALTHY" "Package registry health check failed" "{}"
    fi
    
    rm -f /tmp/registry_response
}

# Function to check CDN
check_cdn() {
    echo -e "${BLUE}Checking CDN...${NC}"
    
    # Test CDN health endpoint
    if HTTP_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/cdn_health "$CDN_URL/health" 2>/dev/null); then
        HTTP_CODE="${HTTP_RESPONSE: -3}"
        
        if [ "$HTTP_CODE" = "200" ]; then
            CDN_HEALTH=$(cat /tmp/cdn_health)
            
            # Check sync status
            if SYNC_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/cdn_sync "$CDN_URL/sync/status" 2>/dev/null); then
                SYNC_CODE="${SYNC_RESPONSE: -3}"
                
                if [ "$SYNC_CODE" = "200" ]; then
                    SYNC_STATUS=$(cat /tmp/cdn_sync)
                    
                    # Check if synced
                    if echo "$SYNC_STATUS" | grep -q '"synced":\s*true'; then
                        STATUS="$HEALTHY"
                        MESSAGE="CDN is healthy"
                    else
                        STATUS="$DEGRADED"
                        MESSAGE="CDN synchronization issues detected"
                    fi
                    
                    DETAILS="{\"cdn_health\":$CDN_HEALTH,\"sync_status\":$SYNC_STATUS}"
                else
                    STATUS="$DEGRADED"
                    MESSAGE="CDN sync status check failed"
                    DETAILS="{\"cdn_health\":$CDN_HEALTH}"
                fi
            else
                STATUS="$DEGRADED"
                MESSAGE="CDN sync status check failed"
                DETAILS="{\"cdn_health\":$CDN_HEALTH}"
            fi
        else
            STATUS="$UNHEALTHY"
            MESSAGE="CDN health check returned status $HTTP_CODE"
            DETAILS="{}"
        fi
    else
        STATUS="$UNHEALTHY"
        MESSAGE="CDN health check failed"
        DETAILS="{}"
    fi
    
    add_check "cdn" "$STATUS" "$MESSAGE" "$DETAILS"
    
    rm -f /tmp/cdn_health /tmp/cdn_sync
}

# Function to check security scanning
check_security_scanning() {
    echo -e "${BLUE}Checking security scanning...${NC}"
    
    # Test security service health endpoint
    if HTTP_RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/security_response "$SECURITY_URL/health" 2>/dev/null); then
        HTTP_CODE="${HTTP_RESPONSE: -3}"
        
        if [ "$HTTP_CODE" = "200" ]; then
            SECURITY_HEALTH=$(cat /tmp/security_response)
            
            # Check if scanner is active
            if echo "$SECURITY_HEALTH" | grep -q '"scanner_active":\s*true'; then
                STATUS="$HEALTHY"
                MESSAGE="Security scanning is healthy"
            else
                STATUS="$DEGRADED"
                MESSAGE="Security scanner is not active"
            fi
        else
            STATUS="$UNHEALTHY"
            MESSAGE="Security service returned status $HTTP_CODE"
            SECURITY_HEALTH="{}"
        fi
    else
        STATUS="$UNHEALTHY"
        MESSAGE="Security scanning check failed"
        SECURITY_HEALTH="{}"
    fi
    
    add_check "security_scanning" "$STATUS" "$MESSAGE" "$SECURITY_HEALTH"
    
    rm -f /tmp/security_response
}

# Function to generate JSON report
generate_report() {
    echo "{"
    echo "  \"timestamp\": \"$TIMESTAMP\","
    echo "  \"overall_status\": \"$OVERALL_STATUS\","
    echo "  \"checks\": ["
    
    for i in "${!CHECK_RESULTS[@]}"; do
        IFS='|' read -r name status message details <<< "${CHECK_RESULTS[$i]}"
        
        echo "    {"
        echo "      \"name\": \"$name\","
        echo "      \"status\": \"$status\","
        echo "      \"message\": \"$message\","
        echo "      \"details\": $details,"
        echo "      \"timestamp\": \"$TIMESTAMP\""
        echo "    }"
        
        if [ $i -lt $((${#CHECK_RESULTS[@]} - 1)) ]; then
            echo "    ,"
        fi
    done
    
    echo "  ],"
    echo "  \"summary\": {"
    echo "    \"total\": $TOTAL_CHECKS,"
    echo "    \"healthy\": $HEALTHY_CHECKS,"
    echo "    \"degraded\": $DEGRADED_CHECKS,"
    echo "    \"unhealthy\": $UNHEALTHY_CHECKS,"
    echo "    \"unknown\": $UNKNOWN_CHECKS"
    echo "  }"
    echo "}"
}

# Function to print colored status
print_status() {
    local status="$1"
    case "$status" in
        "$HEALTHY")
            echo -e "${GREEN}✓ $status${NC}"
            ;;
        "$DEGRADED")
            echo -e "${YELLOW}⚠ $status${NC}"
            ;;
        "$UNHEALTHY"|"$UNKNOWN")
            echo -e "${RED}✗ $status${NC}"
            ;;
    esac
}

# Main execution
main() {
    echo -e "${BLUE}TuskLang Health Check System${NC}"
    echo "=================================="
    echo
    
    # Run all checks
    check_system_resources
    check_database
    check_redis
    check_package_registry
    check_cdn
    check_security_scanning
    
    echo
    echo -e "${BLUE}Health Check Summary:${NC}"
    echo "=========================="
    
    # Print individual check results
    for check in "${CHECK_RESULTS[@]}"; do
        IFS='|' read -r name status message <<< "$check"
        printf "%-20s " "$name"
        print_status "$status"
        echo "    $message"
    done
    
    echo
    echo -e "${BLUE}Overall Status:${NC} "
    print_status "$OVERALL_STATUS"
    echo
    echo "Total: $TOTAL_CHECKS | Healthy: $HEALTHY_CHECKS | Degraded: $DEGRADED_CHECKS | Unhealthy: $UNHEALTHY_CHECKS"
    echo
    
    # Generate JSON report
    if [ "${1:-}" = "--json" ]; then
        generate_report
    fi
    
    # Exit with appropriate code
    case "$OVERALL_STATUS" in
        "$UNHEALTHY"|"$UNKNOWN")
            exit 1
            ;;
        "$DEGRADED")
            exit 2
            ;;
        *)
            exit 0
            ;;
    esac
}

# Run main function
main "$@" 