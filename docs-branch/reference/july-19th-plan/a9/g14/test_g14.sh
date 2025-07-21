#!/bin/bash

# Production Test Script for g14 Learning/AI Operators
# Tests @learn, @predict, @classify, @optimize

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

# Test counters
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

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
        echo "  Expected: $expected"
        echo "  Got: $result"
        ((FAILED_TESTS++))
    fi
}

echo -e "${BLUE}=== PRODUCTION TESTING g14 Learning/AI Operators ===${NC}"

# Load operators
source ./goal_14_1.sh
source ./goal_14_2.sh
source ./goal_14_3.sh

# Create test data
echo "name,age,score,category" > training_data.csv
echo "John,25,85,good" >> training_data.csv
echo "Jane,30,92,excellent" >> training_data.csv
echo "Bob,35,78,good" >> training_data.csv
echo "Alice,28,95,excellent" >> training_data.csv
echo "Charlie,32,72,average" >> training_data.csv

# Test @learn operator
echo -e "\n${BLUE}--- Testing @learn Operator ---${NC}"

result=$(execute_learn "init" "algorithm=linear,features=age,target=score")
test_result "Learn Init" "$result" "success"

result=$(execute_learn "train" "training_data=training_data.csv,features=age,target=score")
test_result "Learn Train Linear" "$result" "success"

result=$(execute_learn "config" "")
test_result "Learn Config" "$result" "success"

# Test @predict operator
echo -e "\n${BLUE}--- Testing @predict Operator ---${NC}"

result=$(execute_predict "init" "model_path=/tmp/model.txt")
test_result "Predict Init" "$result" "success"

result=$(execute_predict "run" "input_data=27")
test_result "Predict Single Value" "$result" "success"

result=$(execute_predict "config" "")
test_result "Predict Config" "$result" "success"

# Test @classify operator
echo -e "\n${BLUE}--- Testing @classify Operator ---${NC}"

result=$(execute_classify "init" "model_type=text,categories=positive,negative,neutral")
test_result "Classify Init" "$result" "success"

result=$(execute_classify "text" "text=This is amazing! I love it!,method=sentiment")
test_result "Classify Sentiment Positive" "$result" "positive"

result=$(execute_classify "text" "text=This is terrible and awful,method=sentiment")
test_result "Classify Sentiment Negative" "$result" "negative"

result=$(execute_classify "text" "text=Hello world how are you today,method=language")
test_result "Classify Language English" "$result" "english"

result=$(execute_classify "text" "text=FREE MONEY WIN NOW URGENT,method=spam")
test_result "Classify Spam Detection" "$result" "spam"

# Test @optimize operator
echo -e "\n${BLUE}--- Testing @optimize Operator ---${NC}"

result=$(execute_optimize "init" "method=grid_search,objective=maximize")
test_result "Optimize Init" "$result" "success"

result=$(execute_optimize "parameters" "parameters=x,bounds=x:0:10:1")
test_result "Optimize Grid Search" "$result" "success"

result=$(execute_optimize "hyperparameters" "model_type=linear_regression")
test_result "Optimize Hyperparameters" "$result" "success"

result=$(execute_optimize "performance" "target_system=database,metrics=cpu,memory")
test_result "Optimize Performance" "$result" "success"

# Test error cases
echo -e "\n${BLUE}--- Testing Error Cases ---${NC}"

result=$(execute_learn "train" "training_data=nonexistent.csv")
test_result "Learn Train Nonexistent File" "$result" "error"

result=$(execute_predict "run" "input_data=")
test_result "Predict Empty Input" "$result" "error"

result=$(execute_classify "text" "text=,method=sentiment")
test_result "Classify Empty Text" "$result" "error"

# Clean up
rm -f training_data.csv /tmp/model.txt

echo -e "\n${BLUE}=== Test Results ===${NC}"
echo "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"

if [[ $FAILED_TESTS -eq 0 ]]; then
    echo -e "${GREEN}🎉 ALL TESTS PASSED! g14 is production-ready.${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed. Review implementation.${NC}"
    exit 1
fi 