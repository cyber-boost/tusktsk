#!/bin/bash

# Test script for g11 Monitoring operators
# Tests @metrics, @logs, @alerts, and @health operators

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counters
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Test result function
test_result() {
    local test_name="$1"
    local result="$2"
    local expected="$3"
    
    ((TOTAL_TESTS++))
    
    if [[ "$result" == *"$expected"* ]]; then
        echo -e "${GREEN}✓ PASS${NC}: $test_name"
        ((PASSED_TESTS++))
    else
        echo -e "${RED}✗ FAIL${NC}: $test_name"
        echo -e "  Expected: $expected"
        echo -e "  Got: $result"
        ((FAILED_TESTS++))
    fi
}

# Test error result function
test_error_result() {
    local test_name="$1"
    local result="$2"
    
    ((TOTAL_TESTS++))
    
    if [[ "$result" == *"error"* ]]; then
        echo -e "${GREEN}✓ PASS${NC}: $test_name (expected error)"
        ((PASSED_TESTS++))
    else
        echo -e "${RED}✗ FAIL${NC}: $test_name (expected error but got success)"
        echo -e "  Got: $result"
        ((FAILED_TESTS++))
    fi
}

echo -e "${BLUE}=== Testing g11 Monitoring Operators ===${NC}"
echo

# Load operator functions
echo -e "${YELLOW}Loading operator functions...${NC}"
source ./goal_11_1.sh
source ./goal_11_2.sh
source ./goal_11_3.sh
echo -e "${GREEN}✓ Operators loaded${NC}"
echo

# Test Metrics Operator
echo -e "${BLUE}--- Testing @metrics Operator ---${NC}"

# Test Metrics initialization
echo "Testing Metrics initialization..."
result=$(execute_metrics "init" "interval=60,retention_days=30,storage_path=/tmp/test_metrics")
test_result "Metrics Init" "$result" "success"

# Test Metrics configuration
echo "Testing Metrics configuration..."
result=$(execute_metrics "config" "")
test_result "Metrics Config" "$result" "success"

# Test Metrics collection
echo "Testing Metrics collection..."
result=$(execute_metrics "collect" "metric_type=system")
test_result "Metrics Collect System" "$result" "success"

result=$(execute_metrics "collect" "metric_type=process")
test_result "Metrics Collect Process" "$result" "success"

result=$(execute_metrics "collect" "metric_type=network")
test_result "Metrics Collect Network" "$result" "success"

result=$(execute_metrics "collect" "metric_type=disk")
test_result "Metrics Collect Disk" "$result" "success"

result=$(execute_metrics "collect" "metric_type=custom,custom_data={\"test\":\"value\"}")
test_result "Metrics Collect Custom" "$result" "success"

# Test Metrics error cases
echo "Testing Metrics error cases..."
result=$(execute_metrics "collect" "metric_type=")
test_error_result "Metrics Collect Empty Type" "$result"

result=$(execute_metrics "collect" "metric_type=unknown")
test_error_result "Metrics Collect Unknown Type" "$result"

# Test Metrics history
echo "Testing Metrics history..."
result=$(execute_metrics "history" "metric_type=system,hours=1")
test_result "Metrics History" "$result" "success"

# Test Metrics cleanup
echo "Testing Metrics cleanup..."
result=$(execute_metrics "cleanup" "days=1")
test_result "Metrics Cleanup" "$result" "success"

echo

# Test Logs Operator
echo -e "${BLUE}--- Testing @logs Operator ---${NC}"

# Test Logs initialization
echo "Testing Logs initialization..."
result=$(execute_logs "init" "path=/tmp,patterns=*.log,max_size=10MB")
test_result "Logs Init" "$result" "success"

# Test Logs configuration
echo "Testing Logs configuration..."
result=$(execute_logs "config" "")
test_result "Logs Config" "$result" "success"

# Test Logs error cases
echo "Testing Logs error cases..."
result=$(execute_logs "init" "path=/nonexistent")
test_error_result "Logs Init Invalid Path" "$result"

result=$(execute_logs "search" "pattern=")
test_error_result "Logs Search Empty Pattern" "$result"

result=$(execute_logs "analyze" "analysis_type=")
test_error_result "Logs Analyze Empty Type" "$result"

# Test Logs search (create test log file)
echo "Testing Logs search..."
echo "test log entry" > /tmp/test.log
echo "error log entry" >> /tmp/test.log
result=$(execute_logs "search" "pattern=test,file_pattern=*.log")
test_result "Logs Search" "$result" "success"

# Test Logs analyze
echo "Testing Logs analyze..."
result=$(execute_logs "analyze" "analysis_type=error_count,file_pattern=*.log")
test_result "Logs Analyze Error Count" "$result" "success"

result=$(execute_logs "analyze" "analysis_type=file_sizes,file_pattern=*.log")
test_result "Logs Analyze File Sizes" "$result" "success"

# Test Logs rotate
echo "Testing Logs rotate..."
result=$(execute_logs "rotate" "file_pattern=*.log,max_size=1B")
test_result "Logs Rotate" "$result" "success"

echo

# Test Alerts Operator
echo -e "${BLUE}--- Testing @alerts Operator ---${NC}"

# Test Alerts initialization
echo "Testing Alerts initialization..."
result=$(execute_alerts "init" "enabled=true,default_channel=console,storage_path=/tmp/test_alerts")
test_result "Alerts Init" "$result" "success"

# Test Alerts configuration
echo "Testing Alerts configuration..."
result=$(execute_alerts "config" "")
test_result "Alerts Config" "$result" "success"

# Test Alerts sending
echo "Testing Alerts sending..."
result=$(execute_alerts "send" "message=Test alert,severity=info,channel=console")
test_result "Alerts Send Console" "$result" "success"

result=$(execute_alerts "send" "message=Test alert,severity=critical,channel=file")
test_result "Alerts Send File" "$result" "success"

# Test Alerts error cases
echo "Testing Alerts error cases..."
result=$(execute_alerts "send" "message=")
test_error_result "Alerts Send Empty Message" "$result"

result=$(execute_alerts "send" "message=test,channel=unknown")
test_error_result "Alerts Send Unknown Channel" "$result"

# Test Alerts rules
echo "Testing Alerts rules..."
result=$(execute_alerts "create_rule" "rule_name=test_rule,condition=test,action=echo,severity=medium")
test_result "Alerts Create Rule" "$result" "success"

result=$(execute_alerts "list_rules" "")
test_result "Alerts List Rules" "$result" "success"

result=$(execute_alerts "delete_rule" "rule_name=test_rule")
test_result "Alerts Delete Rule" "$result" "success"

# Test Alerts history
echo "Testing Alerts history..."
result=$(execute_alerts "history" "hours=1")
test_result "Alerts History" "$result" "success"

# Test Alerts cleanup
echo "Testing Alerts cleanup..."
result=$(execute_alerts "cleanup" "days=1")
test_result "Alerts Cleanup" "$result" "success"

echo

# Test Health Operator
echo -e "${BLUE}--- Testing @health Operator ---${NC}"

# Test Health initialization
echo "Testing Health initialization..."
result=$(execute_health "init" "enabled=true,check_interval=300,storage_path=/tmp/test_health")
test_result "Health Init" "$result" "success"

# Test Health configuration
echo "Testing Health configuration..."
result=$(execute_health "config" "")
test_result "Health Config" "$result" "success"

# Test Health checks
echo "Testing Health checks..."
result=$(execute_health "check" "check_type=system")
test_result "Health Check System" "$result" "success"

result=$(execute_health "check" "check_type=disk")
test_result "Health Check Disk" "$result" "success"

result=$(execute_health "check" "check_type=memory")
test_result "Health Check Memory" "$result" "success"

result=$(execute_health "check" "check_type=cpu")
test_result "Health Check CPU" "$result" "success"

result=$(execute_health "check" "check_type=network")
test_result "Health Check Network" "$result" "success"

result=$(execute_health "check" "check_type=process")
test_result "Health Check Process" "$result" "success"

result=$(execute_health "check" "check_type=port,port=22")
test_result "Health Check Port" "$result" "success"

result=$(execute_health "check" "check_type=custom,command=echo 'test'")
test_result "Health Check Custom" "$result" "success"

# Test Health error cases
echo "Testing Health error cases..."
result=$(execute_health "check" "check_type=")
test_error_result "Health Check Empty Type" "$result"

result=$(execute_health "check" "check_type=unknown")
test_error_result "Health Check Unknown Type" "$result"

result=$(execute_health "check" "check_type=service")
test_error_result "Health Check Service No Name" "$result"

result=$(execute_health "check" "check_type=port")
test_error_result "Health Check Port No Port" "$result"

result=$(execute_health "check" "check_type=url")
test_error_result "Health Check URL No URL" "$result"

result=$(execute_health "check" "check_type=database")
test_error_result "Health Check Database No Type" "$result"

result=$(execute_health "check" "check_type=custom")
test_error_result "Health Check Custom No Command" "$result"

# Test Health status
echo "Testing Health status..."
result=$(execute_health "status" "check_types=system,disk")
test_result "Health Status" "$result" "success"

# Test Health history
echo "Testing Health history..."
result=$(execute_health "history" "hours=1")
test_result "Health History" "$result" "success"

# Test Health cleanup
echo "Testing Health cleanup..."
result=$(execute_health "cleanup" "days=1")
test_result "Health Cleanup" "$result" "success"

echo

# Test operator function availability
echo -e "${BLUE}--- Testing Function Availability ---${NC}"

# Test Metrics functions
echo "Testing Metrics function availability..."
if command -v metrics_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Metrics functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Metrics functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test Logs functions
echo "Testing Logs function availability..."
if command -v logs_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Logs functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Logs functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test Alerts functions
echo "Testing Alerts function availability..."
if command -v alerts_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Alerts functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Alerts functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test Health functions
echo "Testing Health function availability..."
if command -v health_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Health functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Health functions not available${NC}"
    ((FAILED_TESTS++))
fi

echo

# Test parameter parsing
echo -e "${BLUE}--- Testing Parameter Parsing ---${NC}"

# Test Metrics parameter parsing
echo "Testing Metrics parameter parsing..."
result=$(execute_metrics "init" "interval=120,retention_days=60,storage_path=/tmp/custom_metrics,format=csv")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ Metrics parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Metrics parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test Logs parameter parsing
echo "Testing Logs parameter parsing..."
result=$(execute_logs "init" "path=/var/log,patterns=*.log,*.txt,max_size=50MB,compression=bzip2")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ Logs parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Logs parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test Alerts parameter parsing
echo "Testing Alerts parameter parsing..."
result=$(execute_alerts "init" "enabled=true,default_channel=email,retry_count=5,retry_delay=120")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ Alerts parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Alerts parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test Health parameter parsing
echo "Testing Health parameter parsing..."
result=$(execute_health "init" "enabled=true,check_interval=600,timeout=60,retry_count=5")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ Health parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Health parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

echo

# Test advanced features
echo -e "${BLUE}--- Testing Advanced Features ---${NC}"

# Test Metrics advanced features
echo "Testing Metrics advanced features..."
result=$(execute_metrics "collect" "metric_type=system,output_file=/tmp/test_metrics.json")
test_result "Metrics Advanced Collect" "$result" "success"

# Test Logs advanced features
echo "Testing Logs advanced features..."
result=$(execute_logs "analyze" "analysis_type=top_ips,file_pattern=*.log")
test_result "Logs Advanced Analyze" "$result" "success"

# Test Alerts advanced features
echo "Testing Alerts advanced features..."
result=$(execute_alerts "send" "message=Advanced test,severity=high,channel=console,title=Advanced Alert,tags=test,automation")
test_result "Alerts Advanced Send" "$result" "success"

# Test Health advanced features
echo "Testing Health advanced features..."
result=$(execute_health "check" "check_type=url,url=http://localhost,threshold=10")
test_result "Health Advanced Check" "$result" "success"

echo

# Clean up test files
echo "Cleaning up test files..."
rm -f /tmp/test.log* /tmp/test_metrics.json

# Final results
echo -e "${BLUE}=== Test Results ===${NC}"
echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [[ $FAILED_TESTS -eq 0 ]]; then
    echo -e "${GREEN}🎉 All tests passed! g11 Monitoring operators are working correctly.${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed. Please review the implementation.${NC}"
    exit 1
fi 