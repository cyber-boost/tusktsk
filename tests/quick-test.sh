#!/bin/bash
# Quick test to verify TuskLang unified installation

echo "🐘 TuskLang Quick Test"
echo "===================="

# Test 1: Check if files exist
echo -n "Checking core files... "
if [ -f "/var/www/tusklang.tsk/tsk/lib/TuskLang.php" ] && \
   [ -f "/var/www/tusklang.tsk/tsk/native/tusklang-native.php" ] && \
   [ -f "/var/www/tusklang.tsk/tsk/sdk/js/tsk.js" ]; then
    echo "✓"
else
    echo "✗"
    exit 1
fi

# Test 2: Test native execution
echo -n "Testing native execution... "
cat > /tmp/test.tsk << 'EOF'
# Test TuskLang file
app_name: "Test App"
version: "1.0.0"
features: ["native", "unified"]
EOF

if php -r "require_once '/var/www/tusklang.tsk/tsk/lib/TuskLang.php'; use TuskPHP\Utils\TuskLang; \$p = TuskLang::parse(file_get_contents('/tmp/test.tsk')); echo \$p['app_name'];" | grep -q "Test App"; then
    echo "✓"
else
    echo "✗"
    exit 1
fi

# Test 3: Check FUJSEN
echo -n "Checking FUJSEN service... "
if systemctl is-active --quiet fujsen; then
    echo "✓ (running on port 8874)"
else
    echo "✗ (not running)"
fi

# Test 4: Test @ operators
echo -n "Testing @ operators... "
cat > /tmp/test-ops.tsk << 'EOF'
test_date: @date("Y-m-d")
test_if: @if(true, "yes", "no")
EOF

if php -r "require_once '/var/www/tusklang.tsk/tsk/lib/TuskLang.php'; use TuskPHP\Utils\TuskLang; \$p = TuskLang::parse(file_get_contents('/tmp/test-ops.tsk')); echo \$p['test_if'];" | grep -q "yes"; then
    echo "✓"
else
    echo "✗"
fi

echo ""
echo "✅ Quick test complete!"
echo ""
echo "Next steps:"
echo "1. Run full installer: sudo bash /var/www/tusklang.tsk/tsk/tsk.sh"
echo "2. Run full test suite: /var/www/tusklang.tsk/tsk/tests/run-all-tests.sh"
echo "3. Visit https://tusklang.org to see the unified platform"