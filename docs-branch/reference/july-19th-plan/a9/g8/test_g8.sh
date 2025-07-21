#!/bin/bash

# Test script for g8 Communication operators
# Tests @email, @sms, and @slack operators

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

echo -e "${BLUE}=== Testing g8 Communication Operators ===${NC}"
echo

# Load operator functions
echo -e "${YELLOW}Loading operator functions...${NC}"
source ./goal_8_1.sh
source ./goal_8_2.sh
source ./goal_8_3.sh
echo -e "${GREEN}✓ Operators loaded${NC}"
echo

# Test Email Operator
echo -e "${BLUE}--- Testing @email Operator ---${NC}"

# Test Email initialization
echo "Testing Email initialization..."
result=$(execute_email "init" "smtp_host=smtp.gmail.com,smtp_port=587,username=test@example.com,password=testpass,from_address=test@example.com,from_name=Test User")
test_result "Email Init" "$result" "success"

# Test Email validation
echo "Testing Email validation..."
result=$(execute_email "validate" "email=test@example.com")
test_result "Email Validate Valid" "$result" "success"

result=$(execute_email "validate" "email=invalid-email")
test_error_result "Email Validate Invalid" "$result"

# Test Email configuration
echo "Testing Email configuration..."
result=$(execute_email "config" "")
test_result "Email Config" "$result" "success"

# Test Email error cases
echo "Testing Email error cases..."
result=$(execute_email "init" "smtp_host=")
test_error_result "Email Init Empty Host" "$result"

result=$(execute_email "send" "to_address=")
test_error_result "Email Send Empty To" "$result"

# Test Email send (expected to fail without real SMTP)
echo "Testing Email send (expected to fail without real SMTP)..."
result=$(execute_email "send" "to_address=test@example.com,subject=Test,body=Test message")
test_error_result "Email Send (expected error due to no real SMTP)" "$result"

echo

# Test SMS Operator
echo -e "${BLUE}--- Testing @sms Operator ---${NC}"

# Test SMS initialization
echo "Testing SMS initialization..."
result=$(execute_sms "init" "provider=twilio,account_sid=test_sid,auth_token=test_token,from_number=+1234567890")
test_result "SMS Init Twilio" "$result" "success"

result=$(execute_sms "init" "provider=aws,api_key=test_key,api_secret=test_secret")
test_result "SMS Init AWS" "$result" "success"

# Test SMS number validation
echo "Testing SMS number validation..."
result=$(execute_sms "validate_number" "phone_number=1234567890")
test_result "SMS Validate Valid Number" "$result" "success"

result=$(execute_sms "validate_number" "phone_number=123")
test_error_result "SMS Validate Invalid Number" "$result"

# Test SMS number formatting
echo "Testing SMS number formatting..."
result=$(execute_sms "format_number" "phone_number=1234567890,provider=twilio")
test_result "SMS Format Number" "$result" "success"

# Test SMS configuration
echo "Testing SMS configuration..."
result=$(execute_sms "config" "")
test_result "SMS Config" "$result" "success"

# Test SMS error cases
echo "Testing SMS error cases..."
result=$(execute_sms "init" "provider=")
test_error_result "SMS Init Empty Provider" "$result"

result=$(execute_sms "send" "to_number=")
test_error_result "SMS Send Empty Number" "$result"

# Test SMS send (expected to fail without real credentials)
echo "Testing SMS send (expected to fail without real credentials)..."
result=$(execute_sms "send" "to_number=+1234567890,message=Test message")
test_error_result "SMS Send (expected error due to no real credentials)" "$result"

echo

# Test Slack Operator
echo -e "${BLUE}--- Testing @slack Operator ---${NC}"

# Test Slack initialization
echo "Testing Slack initialization..."
result=$(execute_slack "init" "webhook_url=https://hooks.slack.com/services/test,default_channel=general,username=Test Bot")
test_result "Slack Init Webhook" "$result" "success"

result=$(execute_slack "init" "bot_token=xoxb-test-token,default_channel=general")
test_result "Slack Init Bot Token" "$result" "success"

# Test Slack configuration
echo "Testing Slack configuration..."
result=$(execute_slack "config" "")
test_result "Slack Config" "$result" "success"

# Test Slack error cases
echo "Testing Slack error cases..."
result=$(execute_slack "init" "webhook_url=,bot_token=")
test_error_result "Slack Init Empty Credentials" "$result"

result=$(execute_slack "send" "message=")
test_error_result "Slack Send Empty Message" "$result"

# Test Slack send (expected to fail without real webhook/token)
echo "Testing Slack send (expected to fail without real webhook/token)..."
result=$(execute_slack "send" "channel=general,message=Test message")
test_error_result "Slack Send (expected error due to no real webhook/token)" "$result"

# Test Slack list functions (expected to fail without real token)
echo "Testing Slack list functions (expected to fail without real token)..."
result=$(execute_slack "list_channels" "")
test_error_result "Slack List Channels (expected error due to no real token)" "$result"

result=$(execute_slack "list_users" "")
test_error_result "Slack List Users (expected error due to no real token)" "$result"

echo

# Test operator function availability
echo -e "${BLUE}--- Testing Function Availability ---${NC}"

# Test Email functions
echo "Testing Email function availability..."
if command -v email_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Email functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Email functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test SMS functions
echo "Testing SMS function availability..."
if command -v sms_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ SMS functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ SMS functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test Slack functions
echo "Testing Slack function availability..."
if command -v slack_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Slack functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Slack functions not available${NC}"
    ((FAILED_TESTS++))
fi

echo

# Test parameter parsing
echo -e "${BLUE}--- Testing Parameter Parsing ---${NC}"

# Test Email parameter parsing
echo "Testing Email parameter parsing..."
result=$(execute_email "init" "smtp_host=smtp.example.com,smtp_port=465,username=user@example.com,password=pass123")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ Email parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Email parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test SMS parameter parsing
echo "Testing SMS parameter parsing..."
result=$(execute_sms "init" "provider=generic,api_key=key123,api_secret=secret123,from_number=+1234567890")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ SMS parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ SMS parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test Slack parameter parsing
echo "Testing Slack parameter parsing..."
result=$(execute_slack "init" "webhook_url=https://hooks.slack.com/services/test,default_channel=test,username=TestBot")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ Slack parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Slack parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

echo

# Test advanced features
echo -e "${BLUE}--- Testing Advanced Features ---${NC}"

# Test Email external providers
echo "Testing Email external providers..."
result=$(execute_email "send_external" "provider=gmail,to_address=test@example.com,subject=Test,body=Test message")
test_error_result "Email External Provider (expected error due to no real credentials)" "$result"

# Test SMS multiple providers
echo "Testing SMS multiple providers..."
result=$(execute_sms "init" "provider=aws,api_key=test_key,api_secret=test_secret,aws_region=us-west-2")
test_result "SMS AWS Region" "$result" "success"

# Test Slack attachment creation
echo "Testing Slack attachment creation..."
if command -v slack_create_attachment >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Slack attachment functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Slack attachment functions not available${NC}"
    ((FAILED_TESTS++))
fi

echo

# Final results
echo -e "${BLUE}=== Test Results ===${NC}"
echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [[ $FAILED_TESTS -eq 0 ]]; then
    echo -e "${GREEN}🎉 All tests passed! g8 Communication operators are working correctly.${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed. Please review the implementation.${NC}"
    exit 1
fi 