#!/bin/bash
# 🐘 TuskLang Unified Test Suite
# ==============================
# "In the spirit of grace and unity, we test everything as one"

echo "🐘 Running TuskLang Unified Test Suite"
echo "======================================"
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Test counters
TOTAL=0
PASSED=0
FAILED=0
SKIPPED=0

# Test directories
TSK_HOME="/var/www/tusklang.tsk"
TSK_DIR="$TSK_HOME/tsk"
FUJSEN_DIR="/opt/tusklang/fujsen"
INSTALLED_LIB_DIR="/usr/local/lib/tusklang"

run_test() {
    local name=$1
    local cmd=$2
    TOTAL=$((TOTAL + 1))
    
    printf "%-50s" "Testing $name... "
    if eval "$cmd" > /tmp/test.log 2>&1; then
        echo -e "${GREEN}✓ PASSED${NC}"
        PASSED=$((PASSED + 1))
    else
        echo -e "${RED}✗ FAILED${NC}"
        FAILED=$((FAILED + 1))
        if [ -f /tmp/test.log ]; then
            echo "  Error: $(head -1 /tmp/test.log)"
        fi
    fi
}

skip_test() {
    local name=$1
    local reason=$2
    TOTAL=$((TOTAL + 1))
    SKIPPED=$((SKIPPED + 1))
    
    printf "%-50s" "Testing $name... "
    echo -e "${YELLOW}⚠ SKIPPED${NC} ($reason)"
}

# 🔧 Core Parser Tests
echo -e "\n${YELLOW}🔧 Core Parser Tests${NC}"
echo "===================="

run_test "TuskLang Parser Basic" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskLang.php'; use TuskPHP\Utils\TuskLang; TuskLang::parse('test: true');\""
run_test "TuskLang Arrays" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskLang.php'; use TuskPHP\Utils\TuskLang; \\\$p = TuskLang::parse('arr: [1, 2, 3]'); exit(count(\\\$p['arr']) == 3 ? 0 : 1);\""
run_test "TuskLang Objects" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskLang.php'; use TuskPHP\Utils\TuskLang; \\\$p = TuskLang::parse('obj { key: \\\"value\\\" }'); exit(isset(\\\$p['obj']['key']) ? 0 : 1);\""
run_test "TuskLang Comments" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskLang.php'; use TuskPHP\Utils\TuskLang; TuskLang::parse('# comment\ntest: true');\""
run_test "TuskLang Heredoc" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskLang.php'; use TuskPHP\Utils\TuskLang; \\\$p = TuskLang::parse('text: <<<EOT\nHello\nEOT'); exit(\\\$p['text'] == 'Hello' ? 0 : 1);\""

# JIT tests - check if files exist first
if [ -f "$TSK_HOME/test-jit.php" ]; then
    run_test "JIT Optimizer" "php $TSK_HOME/test-jit.php"
else
    skip_test "JIT Optimizer" "test file not found"
fi

if [ -f "$TSK_HOME/jit-performance-test.php" ]; then
    run_test "JIT Performance" "php $TSK_HOME/jit-performance-test.php"
else
    skip_test "JIT Performance" "test file not found"
fi

# ⚡ Native Engine Tests
echo -e "\n${YELLOW}⚡ Native Engine Tests${NC}"
echo "====================="

# Create test file
cat > /tmp/native-test.tsk << 'EOF'
app_name: "Native Test"
version: "1.0.0"
features: ["native", "execution"]
@output(app_name)
EOF

run_test "Native Execution" "php -r \"require_once '$TSK_DIR/native/tusklang-native.php'; \\\$n = new TuskLangNative(); \\\$n->executeAndReturn('/tmp/native-test.tsk');\""
run_test "Native PHP Generation" "php -r \"require_once '$TSK_DIR/native/tusklang-native.php'; \\\$n = new TuskLangNative(); \\\$n->saveAsNativePHP('/tmp/native-test.tsk', '/tmp/native-test.php'); exit(file_exists('/tmp/native-test.php') ? 0 : 1);\""

# 🌐 FUJSEN Tests
echo -e "\n${YELLOW}🌐 FUJSEN Tests${NC}"
echo "==============="

if systemctl is-active --quiet fujsen; then
    run_test "FUJSEN Service Status" "systemctl is-active fujsen"
    run_test "FUJSEN HTTP Response" "curl -s http://localhost:8874/status2 | grep -q 'fujsen_version'"
    
    # Test @ operators via API
    run_test "FUJSEN @date operator" "curl -s http://localhost:8874/test-operators | grep -q 'current_time'"
    run_test "FUJSEN @metrics operator" "curl -s http://localhost:8874/test-operators | grep -q 'page_views'"
    run_test "FUJSEN @feature operator" "curl -s http://localhost:8874/test-operators | grep -q 'has_redis'"
else
    skip_test "FUJSEN Service" "service not running"
    skip_test "FUJSEN HTTP Response" "service not running"
    skip_test "FUJSEN @ operators" "service not running"
fi

# Test FUJSEN files if they exist
if [ -d "$FUJSEN_DIR" ]; then
    [ -f "$FUJSEN_DIR/test-fujsen.php" ] && run_test "FUJSEN Core" "php $FUJSEN_DIR/test-fujsen.php" || skip_test "FUJSEN Core" "test file not found"
    [ -f "$FUJSEN_DIR/test-all-functions.php" ] && run_test "FUJSEN Functions" "php $FUJSEN_DIR/test-all-functions.php" || skip_test "FUJSEN Functions" "test file not found"
    [ -f "$FUJSEN_DIR/test-database.php" ] && run_test "FUJSEN Database" "php $FUJSEN_DIR/test-database.php" || skip_test "FUJSEN Database" "test file not found"
else
    skip_test "FUJSEN Tests" "FUJSEN directory not found"
fi

# 📦 SDK Tests
echo -e "\n${YELLOW}📦 SDK Tests${NC}"
echo "============"

# JavaScript SDK
if [ -f "$TSK_DIR/sdk/js/tsk.js" ]; then
    cat > /tmp/test-js.js << 'EOF'
const TSKParser = require('./tsk.js').TSKParser;
const result = TSKParser.parse('test: "value"');
console.log(result.test === "value" ? "OK" : "FAIL");
EOF
    run_test "JavaScript SDK Parser" "cd $TSK_DIR/sdk/js && node /tmp/test-js.js | grep -q 'OK'"
    
    # Test @ operators
    cat > /tmp/test-js-ops.js << 'EOF'
const TSKParser = require('./tsk.js').TSKParser;
const result = TSKParser.parse('date: @date("now")');
console.log(result.date.__operator === "date" ? "OK" : "FAIL");
EOF
    run_test "JavaScript SDK @ operators" "cd $TSK_DIR/sdk/js && node /tmp/test-js-ops.js | grep -q 'OK'"
else
    skip_test "JavaScript SDK" "SDK file not found"
fi

# Python SDK
if [ -f "$TSK_DIR/sdk/python/tsk.py" ]; then
    run_test "Python SDK Parser" "python3 -c \"import sys; sys.path.insert(0, '$TSK_DIR/sdk/python'); from tsk import parse; r = parse('test: \\\"value\\\"'); exit(0 if r['test'] == 'value' else 1)\""
    run_test "Python SDK @ operators" "python3 -c \"import sys; sys.path.insert(0, '$TSK_DIR/sdk/python'); from tsk import parse; r = parse('date: @date(\\\"now\\\")'); exit(0 if r['date']['__operator'] == 'date' else 1)\""
else
    skip_test "Python SDK" "SDK file not found"
fi

# Bash SDK
if [ -f "$TSK_DIR/sdk/bash/tsk.sh" ]; then
    run_test "Bash SDK Basic" "$TSK_DIR/sdk/bash/tsk.sh validate /tmp/native-test.tsk"
else
    skip_test "Bash SDK" "SDK file not found"
fi

# 🔍 Database Tests
echo -e "\n${YELLOW}🔍 Database Tests${NC}"
echo "================="

if [ -f "$INSTALLED_LIB_DIR/TuskQuery.php" ]; then
    run_test "TuskQuery Class" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskQuery.php'; new TuskPHP\TuskQuery('Users');\""
    run_test "TuskObject Class" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskObject.php'; new TuskPHP\TuskObject('User');\""
    run_test "TuskDb Class" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskDb.php'; TuskPHP\TuskDb::health();\""
else
    skip_test "Database Classes" "files not found"
fi

# 🔬 @ Operator Tests
echo -e "\n${YELLOW}🔬 @ Operator Tests${NC}"
echo "==================="

# Test each operator
cat > /tmp/ops-test.tsk << 'EOF'
query_test: @Query("Users").limit(5).find()
q_test: @q("Posts").where("published", true).find()
cache_test: @cache("5m", "cached_value")
metrics_test: @metrics("test_counter", 1)
if_test: @if(true, "yes", "no")
date_test: @date("Y-m-d")
feature_test: @feature("redis")
json_test: @json({"key": "value"})
env_test: env("HOME", "/tmp")
php_test: php(time())
EOF

run_test "@Query operator" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskLang.php'; use TuskPHP\Utils\TuskLang; \\\$p = TuskLang::parse(file_get_contents('/tmp/ops-test.tsk')); exit(isset(\\\$p['query_test']) ? 0 : 1);\""
run_test "@q operator" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskLang.php'; use TuskPHP\Utils\TuskLang; \\\$p = TuskLang::parse(file_get_contents('/tmp/ops-test.tsk')); exit(isset(\\\$p['q_test']) ? 0 : 1);\""
run_test "@if operator" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskLang.php'; use TuskPHP\Utils\TuskLang; \\\$p = TuskLang::parse(file_get_contents('/tmp/ops-test.tsk')); exit(\\\$p['if_test'] == 'yes' ? 0 : 1);\""
run_test "env() function" "php -r \"require_once '$INSTALLED_LIB_DIR/TuskLang.php'; use TuskPHP\Utils\TuskLang; \\\$p = TuskLang::parse(file_get_contents('/tmp/ops-test.tsk')); exit(!empty(\\\$p['env_test']) ? 0 : 1);\""

# 🚀 Performance Tests
echo -e "\n${YELLOW}🚀 Performance Tests${NC}"
echo "===================="

# Simple performance test
cat > /tmp/perf-test.php << EOF
<?php
require_once '$INSTALLED_LIB_DIR/TuskLang.php';
use TuskPHP\Utils\TuskLang;

\$start = microtime(true);
for (\$i = 0; \$i < 100; \$i++) {
    TuskLang::parse('test: "value"');
}
\$end = microtime(true);
\$time = (\$end - \$start) * 1000;
exit(\$time < 100 ? 0 : 1); // Should complete in under 100ms
EOF

run_test "Parser Performance (100 iterations)" "php /tmp/perf-test.php"

# 📊 Test Summary
echo -e "\n${YELLOW}📊 Test Summary${NC}"
echo "==============="
echo "Total Tests:  $TOTAL"
echo -e "Passed:       ${GREEN}$PASSED${NC}"
echo -e "Failed:       ${RED}$FAILED${NC}"
echo -e "Skipped:      ${YELLOW}$SKIPPED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ All active tests passed!${NC}"
    if [ $SKIPPED -gt 0 ]; then
        echo -e "${YELLOW}⚠️  Some tests were skipped. Run the full installer to enable all features.${NC}"
    fi
    exit 0
else
    echo -e "${RED}❌ Some tests failed. Please check the errors above.${NC}"
    exit 1
fi