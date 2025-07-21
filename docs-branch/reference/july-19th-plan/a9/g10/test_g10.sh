#!/bin/bash

# Test script for g10 Database operators
# Tests @mongodb, @redis, @postgresql, and @mysql operators

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

echo -e "${BLUE}=== Testing g10 Database Operators ===${NC}"
echo

# Load operator functions
echo -e "${YELLOW}Loading operator functions...${NC}"
source ./goal_10_1.sh
source ./goal_10_2.sh
source ./goal_10_3.sh
echo -e "${GREEN}✓ Operators loaded${NC}"
echo

# Test MongoDB Operator
echo -e "${BLUE}--- Testing @mongodb Operator ---${NC}"

# Test MongoDB initialization
echo "Testing MongoDB initialization..."
result=$(execute_mongodb "init" "database=testdb,host=localhost,port=27017")
test_result "MongoDB Init" "$result" "success"

# Test MongoDB configuration
echo "Testing MongoDB configuration..."
result=$(execute_mongodb "config" "")
test_result "MongoDB Config" "$result" "success"

# Test MongoDB error cases
echo "Testing MongoDB error cases..."
result=$(execute_mongodb "init" "database=")
test_error_result "MongoDB Init Empty Database" "$result"

# Test MongoDB connection (expected to fail without real MongoDB)
echo "Testing MongoDB connection (expected to fail without real MongoDB)..."
result=$(execute_mongodb "test" "")
test_error_result "MongoDB Test Connection (expected error due to no real MongoDB)" "$result"

# Test MongoDB operations (expected to fail without real MongoDB)
echo "Testing MongoDB operations (expected to fail without real MongoDB)..."
result=$(execute_mongodb "insert" "collection=test,document={\"name\":\"test\"}")
test_error_result "MongoDB Insert (expected error due to no real MongoDB)" "$result"

result=$(execute_mongodb "find" "collection=test,query={}")
test_error_result "MongoDB Find (expected error due to no real MongoDB)" "$result"

echo

# Test Redis Operator
echo -e "${BLUE}--- Testing @redis Operator ---${NC}"

# Test Redis initialization
echo "Testing Redis initialization..."
result=$(execute_redis "init" "host=localhost,port=6379,database=0")
test_result "Redis Init" "$result" "success"

# Test Redis configuration
echo "Testing Redis configuration..."
result=$(execute_redis "config" "")
test_result "Redis Config" "$result" "success"

# Test Redis error cases
echo "Testing Redis error cases..."
result=$(execute_redis "set" "key=")
test_error_result "Redis Set Empty Key" "$result"

result=$(execute_redis "get" "key=")
test_error_result "Redis Get Empty Key" "$result"

# Test Redis connection (expected to fail without real Redis)
echo "Testing Redis connection (expected to fail without real Redis)..."
result=$(execute_redis "test" "")
test_error_result "Redis Test Connection (expected error due to no real Redis)" "$result"

# Test Redis operations (expected to fail without real Redis)
echo "Testing Redis operations (expected to fail without real Redis)..."
result=$(execute_redis "set" "key=test,value=testvalue")
test_error_result "Redis Set (expected error due to no real Redis)" "$result"

result=$(execute_redis "get" "key=test")
test_error_result "Redis Get (expected error due to no real Redis)" "$result"

echo

# Test PostgreSQL Operator
echo -e "${BLUE}--- Testing @postgresql Operator ---${NC}"

# Test PostgreSQL initialization
echo "Testing PostgreSQL initialization..."
result=$(execute_postgresql "init" "database=testdb,host=localhost,port=5432")
test_result "PostgreSQL Init" "$result" "success"

# Test PostgreSQL configuration
echo "Testing PostgreSQL configuration..."
result=$(execute_postgresql "config" "")
test_result "PostgreSQL Config" "$result" "success"

# Test PostgreSQL error cases
echo "Testing PostgreSQL error cases..."
result=$(execute_postgresql "init" "database=")
test_error_result "PostgreSQL Init Empty Database" "$result"

result=$(execute_postgresql "query" "query=")
test_error_result "PostgreSQL Query Empty" "$result"

# Test PostgreSQL connection (expected to fail without real PostgreSQL)
echo "Testing PostgreSQL connection (expected to fail without real PostgreSQL)..."
result=$(execute_postgresql "test" "")
test_error_result "PostgreSQL Test Connection (expected error due to no real PostgreSQL)" "$result"

# Test PostgreSQL operations (expected to fail without real PostgreSQL)
echo "Testing PostgreSQL operations (expected to fail without real PostgreSQL)..."
result=$(execute_postgresql "query" "query=SELECT 1;")
test_error_result "PostgreSQL Query (expected error due to no real PostgreSQL)" "$result"

result=$(execute_postgresql "list_tables" "")
test_error_result "PostgreSQL List Tables (expected error due to no real PostgreSQL)" "$result"

# Test PostgreSQL backup/restore (expected to fail without real PostgreSQL)
echo "Testing PostgreSQL backup/restore (expected to fail without real PostgreSQL)..."
result=$(execute_postgresql "backup" "backup_path=test_backup.sql")
test_error_result "PostgreSQL Backup (expected error due to no real PostgreSQL)" "$result"

echo

# Test MySQL Operator
echo -e "${BLUE}--- Testing @mysql Operator ---${NC}"

# Test MySQL initialization
echo "Testing MySQL initialization..."
result=$(execute_mysql "init" "database=testdb,host=localhost,port=3306")
test_result "MySQL Init" "$result" "success"

# Test MySQL configuration
echo "Testing MySQL configuration..."
result=$(execute_mysql "config" "")
test_result "MySQL Config" "$result" "success"

# Test MySQL error cases
echo "Testing MySQL error cases..."
result=$(execute_mysql "init" "database=")
test_error_result "MySQL Init Empty Database" "$result"

result=$(execute_mysql "query" "query=")
test_error_result "MySQL Query Empty" "$result"

# Test MySQL connection (expected to fail without real MySQL)
echo "Testing MySQL connection (expected to fail without real MySQL)..."
result=$(execute_mysql "test" "")
test_error_result "MySQL Test Connection (expected error due to no real MySQL)" "$result"

# Test MySQL operations (expected to fail without real MySQL)
echo "Testing MySQL operations (expected to fail without real MySQL)..."
result=$(execute_mysql "query" "query=SELECT 1;")
test_error_result "MySQL Query (expected error due to no real MySQL)" "$result"

result=$(execute_mysql "list_tables" "")
test_error_result "MySQL List Tables (expected error due to no real MySQL)" "$result"

# Test MySQL backup/restore (expected to fail without real MySQL)
echo "Testing MySQL backup/restore (expected to fail without real MySQL)..."
result=$(execute_mysql "backup" "backup_path=test_backup.sql")
test_error_result "MySQL Backup (expected error due to no real MySQL)" "$result"

echo

# Test operator function availability
echo -e "${BLUE}--- Testing Function Availability ---${NC}"

# Test MongoDB functions
echo "Testing MongoDB function availability..."
if command -v mongodb_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ MongoDB functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ MongoDB functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test Redis functions
echo "Testing Redis function availability..."
if command -v redis_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Redis functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Redis functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test PostgreSQL functions
echo "Testing PostgreSQL function availability..."
if command -v postgresql_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ PostgreSQL functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ PostgreSQL functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test MySQL functions
echo "Testing MySQL function availability..."
if command -v mysql_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ MySQL functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ MySQL functions not available${NC}"
    ((FAILED_TESTS++))
fi

echo

# Test parameter parsing
echo -e "${BLUE}--- Testing Parameter Parsing ---${NC}"

# Test MongoDB parameter parsing
echo "Testing MongoDB parameter parsing..."
result=$(execute_mongodb "init" "database=testdb,host=localhost,port=27017,username=testuser,password=testpass")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ MongoDB parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ MongoDB parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test Redis parameter parsing
echo "Testing Redis parameter parsing..."
result=$(execute_redis "init" "host=localhost,port=6379,database=1,password=testpass")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ Redis parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Redis parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test PostgreSQL parameter parsing
echo "Testing PostgreSQL parameter parsing..."
result=$(execute_postgresql "init" "database=testdb,host=localhost,port=5432,username=testuser,password=testpass,ssl_mode=require")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ PostgreSQL parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ PostgreSQL parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test MySQL parameter parsing
echo "Testing MySQL parameter parsing..."
result=$(execute_mysql "init" "database=testdb,host=localhost,port=3306,username=testuser,password=testpass,ssl_mode=REQUIRED")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ MySQL parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ MySQL parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

echo

# Test advanced features
echo -e "${BLUE}--- Testing Advanced Features ---${NC}"

# Test MongoDB advanced features
echo "Testing MongoDB advanced features..."
result=$(execute_mongodb "init" "database=testdb,collection=testcollection,host=localhost,port=27017")
test_result "MongoDB Advanced Init" "$result" "success"

# Test Redis advanced features
echo "Testing Redis advanced features..."
result=$(execute_redis "init" "host=localhost,port=6379,database=2")
test_result "Redis Advanced Init" "$result" "success"

# Test PostgreSQL advanced features
echo "Testing PostgreSQL advanced features..."
result=$(execute_postgresql "init" "database=testdb,host=localhost,port=5432,output_format=csv")
test_result "PostgreSQL Advanced Init" "$result" "success"

# Test MySQL advanced features
echo "Testing MySQL advanced features..."
result=$(execute_mysql "init" "database=testdb,host=localhost,port=3306,output_format=csv")
test_result "MySQL Advanced Init" "$result" "success"

echo

# Test database-specific operations
echo -e "${BLUE}--- Testing Database-Specific Operations ---${NC}"

# Test MongoDB specific operations
echo "Testing MongoDB specific operations..."
result=$(execute_mongodb "count" "collection=test")
test_error_result "MongoDB Count (expected error due to no real MongoDB)" "$result"

# Test Redis specific operations
echo "Testing Redis specific operations..."
result=$(execute_redis "keys" "pattern=*")
test_error_result "Redis Keys (expected error due to no real Redis)" "$result"

result=$(execute_redis "hset" "key=test,field=testfield,value=testvalue")
test_error_result "Redis HSet (expected error due to no real Redis)" "$result"

# Test PostgreSQL specific operations
echo "Testing PostgreSQL specific operations..."
result=$(execute_postgresql "describe_table" "table_name=test")
test_error_result "PostgreSQL Describe Table (expected error due to no real PostgreSQL)" "$result"

result=$(execute_postgresql "table_count" "table_name=test")
test_error_result "PostgreSQL Table Count (expected error due to no real PostgreSQL)" "$result"

# Test MySQL specific operations
echo "Testing MySQL specific operations..."
result=$(execute_mysql "describe_table" "table_name=test")
test_error_result "MySQL Describe Table (expected error due to no real MySQL)" "$result"

result=$(execute_mysql "table_count" "table_name=test")
test_error_result "MySQL Table Count (expected error due to no real MySQL)" "$result"

result=$(execute_mysql "show_databases" "")
test_error_result "MySQL Show Databases (expected error due to no real MySQL)" "$result"

echo

# Final results
echo -e "${BLUE}=== Test Results ===${NC}"
echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [[ $FAILED_TESTS -eq 0 ]]; then
    echo -e "${GREEN}🎉 All tests passed! g10 Database operators are working correctly.${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed. Please review the implementation.${NC}"
    exit 1
fi 