#!/bin/bash

# Test script for g9 Control Flow operators
# Tests @switch, @for, @while, @each, and @filter operators

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

echo -e "${BLUE}=== Testing g9 Control Flow Operators ===${NC}"
echo

# Load operator functions
echo -e "${YELLOW}Loading operator functions...${NC}"
source ./goal_9_1.sh
source ./goal_9_2.sh
source ./goal_9_3.sh
echo -e "${GREEN}✓ Operators loaded${NC}"
echo

# Test Switch Operator
echo -e "${BLUE}--- Testing @switch Operator ---${NC}"

# Test Switch initialization
echo "Testing Switch initialization..."
result=$(execute_switch "init" "default_case=echo 'default',break_on_match=true")
test_result "Switch Init" "$result" "success"

# Test Switch execution
echo "Testing Switch execution..."
result=$(execute_switch "execute" "value=test,cases=test:echo 'matched',other:echo 'other'")
test_result "Switch Execute Match" "$result" "success"

result=$(execute_switch "execute" "value=unknown,cases=test:echo 'matched',other:echo 'other'")
test_result "Switch Execute No Match" "$result" "warning"

# Test Switch pattern matching
echo "Testing Switch pattern matching..."
result=$(execute_switch "pattern" "value=test123,patterns=test[0-9]+:echo 'number',test[a-z]+:echo 'letters'")
test_result "Switch Pattern" "$result" "success"

# Test Switch configuration
echo "Testing Switch configuration..."
result=$(execute_switch "config" "")
test_result "Switch Config" "$result" "success"

# Test Switch error cases
echo "Testing Switch error cases..."
result=$(execute_switch "execute" "value=")
test_error_result "Switch Execute Empty Value" "$result"

result=$(execute_switch "execute" "value=test,cases=")
test_error_result "Switch Execute Empty Cases" "$result"

echo

# Test For Operator
echo -e "${BLUE}--- Testing @for Operator ---${NC}"

# Test For initialization
echo "Testing For initialization..."
result=$(execute_for "init" "max_iterations=100,timeout=30")
test_result "For Init" "$result" "success"

# Test For range
echo "Testing For range..."
result=$(execute_for "range" "start=1,end=5,action=echo 'Iteration {i}'")
test_result "For Range" "$result" "success"

# Test For list
echo "Testing For list..."
result=$(execute_for "list" "items=apple,banana,cherry,action=echo 'Fruit: {item}'")
test_result "For List" "$result" "success"

# Test For list with custom delimiter
echo "Testing For list with custom delimiter..."
result=$(execute_for "list" "items=apple;banana;cherry,action=echo 'Fruit: {item}',delimiter=;")
test_result "For List Custom Delimiter" "$result" "success"

# Test For configuration
echo "Testing For configuration..."
result=$(execute_for "config" "")
test_result "For Config" "$result" "success"

# Test For error cases
echo "Testing For error cases..."
result=$(execute_for "range" "start=,end=5,action=echo 'test'")
test_error_result "For Range Empty Start" "$result"

result=$(execute_for "list" "items=,action=echo 'test'")
test_error_result "For List Empty Items" "$result"

# Test For file (create test file)
echo "Testing For file..."
echo -e "line1\nline2\nline3" > test_file.txt
result=$(execute_for "file" "file_path=test_file.txt,action=echo 'Line: {line}'")
test_result "For File" "$result" "success"

# Test For command
echo "Testing For command..."
result=$(execute_for "command" "command=echo -e 'cmd1\ncmd2\ncmd3',action=echo 'Output: {line}'")
test_result "For Command" "$result" "success"

echo

# Test While Operator
echo -e "${BLUE}--- Testing @while Operator ---${NC}"

# Test While initialization
echo "Testing While initialization..."
result=$(execute_while "init" "max_iterations=10,timeout=30,check_interval=1")
test_result "While Init" "$result" "success"

# Test While condition
echo "Testing While condition..."
result=$(execute_while "condition" "condition=test 1 -lt 5,action=echo 'Loop iteration'")
test_result "While Condition" "$result" "success"

# Test While condition with break
echo "Testing While condition with break..."
result=$(execute_while "condition" "condition=test 1 -eq 1,action=echo 'Loop once',break_condition=test 1 -eq 1")
test_result "While Condition Break" "$result" "success"

# Test While configuration
echo "Testing While configuration..."
result=$(execute_while "config" "")
test_result "While Config" "$result" "success"

# Test While error cases
echo "Testing While error cases..."
result=$(execute_while "condition" "condition=,action=echo 'test'")
test_error_result "While Condition Empty" "$result"

# Test While file monitoring
echo "Testing While file monitoring..."
echo "initial content" > test_monitor.txt
result=$(execute_while "file" "file_path=test_monitor.txt,action=echo 'File changed'")
test_result "While File Monitor" "$result" "success"

# Test While command
echo "Testing While command..."
result=$(execute_while "command" "command=echo 'test',action=echo 'Command executed'")
test_result "While Command" "$result" "success"

echo

# Test Each Operator
echo -e "${BLUE}--- Testing @each Operator ---${NC}"

# Test Each initialization
echo "Testing Each initialization..."
result=$(execute_each "init" "batch_size=50,parallel=false,max_workers=4")
test_result "Each Init" "$result" "success"

# Test Each list
echo "Testing Each list..."
result=$(execute_each "list" "items=item1,item2,item3,action=echo 'Processing: {item}'")
test_result "Each List" "$result" "success"

# Test Each list with index
echo "Testing Each list with index..."
result=$(execute_each "list" "items=first,second,third,action=echo 'Index {index}: {item}'")
test_result "Each List Index" "$result" "success"

# Test Each configuration
echo "Testing Each configuration..."
result=$(execute_each "config" "")
test_result "Each Config" "$result" "success"

# Test Each error cases
echo "Testing Each error cases..."
result=$(execute_each "list" "items=,action=echo 'test'")
test_error_result "Each List Empty Items" "$result"

# Test Each file
echo "Testing Each file..."
result=$(execute_each "file" "file_path=test_file.txt,action=echo 'File line: {line}'")
test_result "Each File" "$result" "success"

# Test Each command
echo "Testing Each command..."
result=$(execute_each "command" "command=echo -e 'output1\noutput2',action=echo 'Command output: {line}'")
test_result "Each Command" "$result" "success"

echo

# Test Filter Operator
echo -e "${BLUE}--- Testing @filter Operator ---${NC}"

# Test Filter initialization
echo "Testing Filter initialization..."
result=$(execute_filter "init" "case_sensitive=true,regex_mode=false,invert_match=false,max_results=100")
test_result "Filter Init" "$result" "success"

# Test Filter text
echo "Testing Filter text..."
test_data="line1\nline2\nline3\napple\nbanana"
result=$(execute_filter "text" "data=$test_data,pattern=line")
test_result "Filter Text" "$result" "success"

# Test Filter text with transformation
echo "Testing Filter text with transformation..."
result=$(execute_filter "text" "data=$test_data,pattern=line,transform=uppercase")
test_result "Filter Text Transform" "$result" "success"

# Test Filter text with invert match
echo "Testing Filter text with invert match..."
result=$(execute_filter "text" "data=$test_data,pattern=line,transform=uppercase")
test_result "Filter Text Invert" "$result" "success"

# Test Filter configuration
echo "Testing Filter configuration..."
result=$(execute_filter "config" "")
test_result "Filter Config" "$result" "success"

# Test Filter error cases
echo "Testing Filter error cases..."
result=$(execute_filter "text" "data=,pattern=test")
test_error_result "Filter Text Empty Data" "$result"

result=$(execute_filter "text" "data=test,pattern=")
test_error_result "Filter Text Empty Pattern" "$result"

# Test Filter file
echo "Testing Filter file..."
result=$(execute_filter "file" "file_path=test_file.txt,pattern=line")
test_result "Filter File" "$result" "success"

# Test Filter command
echo "Testing Filter command..."
result=$(execute_filter "command" "command=echo -e 'test1\ntest2\nother',pattern=test")
test_result "Filter Command" "$result" "success"

# Test Filter advanced
echo "Testing Filter advanced..."
result=$(execute_filter "advanced" "data=$test_data,conditions=line:contains,2:length_gt")
test_result "Filter Advanced" "$result" "success"

echo

# Test operator function availability
echo -e "${BLUE}--- Testing Function Availability ---${NC}"

# Test Switch functions
echo "Testing Switch function availability..."
if command -v switch_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Switch functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Switch functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test For functions
echo "Testing For function availability..."
if command -v for_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ For functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ For functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test While functions
echo "Testing While function availability..."
if command -v while_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ While functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ While functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test Each functions
echo "Testing Each function availability..."
if command -v each_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Each functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Each functions not available${NC}"
    ((FAILED_TESTS++))
fi

# Test Filter functions
echo "Testing Filter function availability..."
if command -v filter_init >/dev/null 2>&1; then
    echo -e "${GREEN}✓ Filter functions available${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Filter functions not available${NC}"
    ((FAILED_TESTS++))
fi

echo

# Test parameter parsing
echo -e "${BLUE}--- Testing Parameter Parsing ---${NC}"

# Test Switch parameter parsing
echo "Testing Switch parameter parsing..."
result=$(execute_switch "init" "default_case=echo 'default',break_on_match=false")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ Switch parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Switch parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test For parameter parsing
echo "Testing For parameter parsing..."
result=$(execute_for "init" "max_iterations=500,timeout=60")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ For parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ For parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test While parameter parsing
echo "Testing While parameter parsing..."
result=$(execute_while "init" "max_iterations=100,timeout=60,check_interval=2")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ While parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ While parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test Each parameter parsing
echo "Testing Each parameter parsing..."
result=$(execute_each "init" "batch_size=200,parallel=true,max_workers=8")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ Each parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Each parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

# Test Filter parameter parsing
echo "Testing Filter parameter parsing..."
result=$(execute_filter "init" "case_sensitive=false,regex_mode=true,invert_match=true,max_results=500")
if [[ "$result" == *"success"* ]]; then
    echo -e "${GREEN}✓ Filter parameter parsing works${NC}"
    ((PASSED_TESTS++))
else
    echo -e "${RED}✗ Filter parameter parsing failed${NC}"
    ((FAILED_TESTS++))
fi

echo

# Test advanced features
echo -e "${BLUE}--- Testing Advanced Features ---${NC}"

# Test Switch pattern matching with regex
echo "Testing Switch pattern matching with regex..."
result=$(execute_switch "pattern" "value=test123,patterns=[0-9]+:echo 'number',[a-z]+:echo 'letters'")
test_result "Switch Regex Pattern" "$result" "success"

# Test For with large range
echo "Testing For with large range..."
result=$(execute_for "range" "start=1,end=10,step=2,action=echo 'Step: {i}'")
test_result "For Large Range" "$result" "success"

# Test While with complex condition
echo "Testing While with complex condition..."
result=$(execute_while "condition" "condition=test 1 -lt 3,action=echo 'Complex loop'")
test_result "While Complex Condition" "$result" "success"

# Test Each with parallel processing
echo "Testing Each with parallel processing..."
result=$(execute_each "list" "items=item1,item2,item3,action=echo 'Parallel: {item}'")
test_result "Each Parallel Processing" "$result" "success"

# Test Filter with multiple transformations
echo "Testing Filter with multiple transformations..."
result=$(execute_filter "text" "data=$test_data,pattern=line,transform=uppercase")
test_result "Filter Multiple Transformations" "$result" "success"

echo

# Clean up test files
echo "Cleaning up test files..."
rm -f test_file.txt test_monitor.txt

# Final results
echo -e "${BLUE}=== Test Results ===${NC}"
echo -e "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [[ $FAILED_TESTS -eq 0 ]]; then
    echo -e "${GREEN}🎉 All tests passed! g9 Control Flow operators are working correctly.${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed. Please review the implementation.${NC}"
    exit 1
fi 