#!/bin/bash

# Test script for g7 Security & Authentication operators
# Tests @jwt, @oauth, and @vault operators

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

echo -e "${BLUE}=== Testing g7 Security & Authentication Operators ===${NC}"
echo

# Load operator functions
echo -e "${YELLOW}Loading operator functions...${NC}"
source ./goal_7_1.sh
source ./goal_7_2.sh
source ./goal_7_3.sh
echo -e "${GREEN}✓ Operators loaded${NC}"
echo

# Test JWT Operator
echo -e "${BLUE}--- Testing @jwt Operator ---${NC}"

# Test JWT initialization
echo "Testing JWT initialization..."
result=$(execute_jwt "init" "secret_key=test-secret,algorithm=HS256,expiry=3600")
test_result "JWT Init" "$result" "success"

# Test JWT token generation
echo "Testing JWT token generation..."
result=$(execute_jwt "generate" "payload={\"user_id\":123,\"role\":\"admin\"},expiry=3600")
test_result "JWT Generate" "$result" "success"

# Extract token for further tests
token=$(echo "$result" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

# Test JWT token decode
echo "Testing JWT token decode..."
result=$(execute_jwt "decode" "token=$token")
test_result "JWT Decode" "$result" "success"

# Test JWT token verification
echo "Testing JWT token verification..."
result=$(execute_jwt "verify" "token=$token")
test_result "JWT Verify" "$result" "success"

# Test JWT token refresh
echo "Testing JWT token refresh..."
result=$(execute_jwt "refresh" "token=$token,expiry=7200")
test_result "JWT Refresh" "$result" "success"

# Test JWT error cases
echo "Testing JWT error cases..."
result=$(execute_jwt "generate" "payload=")
test_error_result "JWT Generate Empty Payload" "$result"

result=$(execute_jwt "verify" "token=invalid-token")
test_error_result "JWT Verify Invalid Token" "$result"

echo

# Test OAuth Operator
echo -e "${BLUE}--- Testing @oauth Operator ---${NC}"

# Test OAuth initialization
echo "Testing OAuth initialization..."
result=$(execute_oauth "init" "client_id=test-client,client_secret=test-secret,redirect_uri=http://localhost/callback,auth_url=https://auth.example.com/oauth/authorize,token_url=https://auth.example.com/oauth/token,scope=read write")
test_result "OAuth Init" "$result" "success"

# Test OAuth authorization URL generation
echo "Testing OAuth authorization URL generation..."
result=$(execute_oauth "auth_url" "")
test_result "OAuth Auth URL" "$result" "success"

# Test OAuth client credentials flow
echo "Testing OAuth client credentials flow..."
result=$(execute_oauth "client_credentials" "")
test_error_result "OAuth Client Credentials (expected error due to no real endpoint)" "$result"

# Test OAuth password flow
echo "Testing OAuth password flow..."
result=$(execute_oauth "password" "username=testuser,password=testpass")
test_error_result "OAuth Password Flow (expected error due to no real endpoint)" "$result"

# Test OAuth token info
echo "Testing OAuth token info..."
result=$(execute_oauth "token_info" "")
test_error_result "OAuth Token Info (expected error due to no token)" "$result"

# Test OAuth error cases
echo "Testing OAuth error cases..."
result=$(execute_oauth "init" "client_id=")
test_error_result "OAuth Init Empty Client ID" "$result"

result=$(execute_oauth "exchange_code" "auth_code=")
test_error_result "OAuth Exchange Empty Code" "$result"

echo

# Test Vault Operator
echo -e "${BLUE}--- Testing @vault Operator ---${NC}"

# Test Vault initialization
echo "Testing Vault initialization..."
result=$(execute_vault "init" "master_key=test-master-key,storage_dir=/tmp/test-vault,backup_dir=/tmp/test-vault/backups")
test_result "Vault Init" "$result" "success"

# Test Vault key generation
echo "Testing Vault key generation..."
result=$(execute_vault "generate_key" "key_name=test-key,key_type=aes")
test_result "Vault Generate Key" "$result" "success"

# Test Vault secret storage
echo "Testing Vault secret storage..."
result=$(execute_vault "store" "secret_name=test-secret,secret_value=my-secret-value,secret_type=password,ttl=3600")
test_result "Vault Store Secret" "$result" "success"

# Test Vault secret retrieval
echo "Testing Vault secret retrieval..."
result=$(execute_vault "retrieve" "secret_name=test-secret")
test_result "Vault Retrieve Secret" "$result" "success"

# Test Vault secret listing
echo "Testing Vault secret listing..."
result=$(execute_vault "list" "pattern=test")
test_result "Vault List Secrets" "$result" "success"

# Test Vault secret rotation
echo "Testing Vault secret rotation..."
result=$(execute_vault "rotate" "secret_name=test-secret,new_value=new-secret-value")
test_result "Vault Rotate Secret" "$result" "success"

# Test Vault backup
echo "Testing Vault backup..."
result=$(execute_vault "backup" "backup_name=test-backup")
test_result "Vault Backup" "$result" "success"

# Test Vault status
echo "Testing Vault status..."
result=$(execute_vault "status" "")
test_result "Vault Status" "$result" "success"

# Test Vault error cases
echo "Testing Vault error cases..."
result=$(execute_vault "store" "secret_name=,secret_value=test")
test_error_result "Vault Store Empty Name" "$result"

result=$(execute_vault "retrieve" "secret_name=nonexistent-secret")
test_error_result "Vault Retrieve Nonexistent Secret" "$result"

result=$(execute_vault "delete" "secret_name=nonexistent-secret")
test_error_result "Vault Delete Nonexistent Secret" "$result"

# Test Vault secret deletion
echo "Testing Vault secret deletion..."
result=$(execute_vault "delete" "secret_name=test-secret")
test_result "Vault Delete Secret" "$result" "success"

echo

# Test operator function availability
echo -e "${BLUE}--- Testing Function Availability ---${NC}"

# Test JWT functions
echo "Testing JWT function availability..."
if command -v jwt_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ JWT functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ JWT functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test OAuth functions
echo "Testing OAuth function availability..."
if command -v oauth_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ OAuth functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ OAuth functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test Vault functions
echo "Testing Vault function availability..."
if command -v vault_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Vault functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Vault functions not available${NC}"
    ((FAILED_TESTS++))
fi

echo

# Test parameter parsing
echo -e "${BLUE}--- Testing Parameter Parsing ---${NC}"

# Test JWT parameter parsing
echo "Testing JWT parameter parsing..."
result=$(execute_jwt "init" "secret_key=test123,algorithm=HS512,expiry=1800")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ JWT parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ JWT parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test OAuth parameter parsing
echo "Testing OAuth parameter parsing..."
result=$(execute_oauth "init" "client_id=test123,client_secret=secret123,redirect_uri=http://test.com")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ OAuth parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ OAuth parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test Vault parameter parsing
echo "Testing Vault parameter parsing..."
result=$(execute_vault "init" "master_key=key123,storage_dir=/tmp/vault-test")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ Vault parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Vault parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

echo

# Cleanup test files
echo -e "${YELLOW}Cleaning up test files...${NC}"
rm -rf /tmp/test-vault
rm -rf /tmp/vault-test
echo -e "${GREEN}✓ Cleanup completed${NC}"

echo

# Final results
echo -e "${BLUE}=== Test Results ===${NC}"
echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [[ $FAILED_TESTS -eq 0 ]]; then
    echo -e "${GREEN}🎉 All tests passed! g7 Security & Authentication operators are working correctly.${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed. Please review the implementation.${NC}"
    exit 1
fi 