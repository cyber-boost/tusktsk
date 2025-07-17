#!/bin/bash
# 🐘 TuskAuth Test Runner
# =======================
# Comprehensive test suite for OAuth2/OIDC authentication system

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Test configuration
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$(dirname "$TEST_DIR")")"
LIB_PATH="$PROJECT_ROOT/lib"
TEST_DB="$TEST_DIR/test_auth.db"

echo -e "${BLUE}🐘 TuskAuth Test Runner${NC}"
echo -e "${CYAN}========================${NC}"
echo ""

# Check PHP version
PHP_VERSION=$(php -r "echo PHP_VERSION;")
echo -e "${BLUE}PHP Version:${NC} $PHP_VERSION"

# Check required extensions
echo -e "${BLUE}Checking required extensions...${NC}"
REQUIRED_EXTENSIONS=("pdo" "pdo_sqlite" "curl" "json" "openssl")

for ext in "${REQUIRED_EXTENSIONS[@]}"; do
    if php -m | grep -q "^$ext$"; then
        echo -e "  ${GREEN}✓${NC} $ext"
    else
        echo -e "  ${RED}✗${NC} $ext (missing)"
        exit 1
    fi
done

echo ""

# Clean up previous test database
if [ -f "$TEST_DB" ]; then
    echo -e "${YELLOW}Cleaning up previous test database...${NC}"
    rm -f "$TEST_DB"
fi

# Set up test environment
export TSK_LIB_PATH="$LIB_PATH"
export TSK_HOME="$PROJECT_ROOT"

echo -e "${BLUE}Running OAuth2 Authentication Tests...${NC}"
echo ""

# Run the test file
cd "$TEST_DIR"
php TuskAuthTest.php

# Check test results
if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✅ All OAuth2 authentication tests passed!${NC}"
    echo ""
    echo -e "${CYAN}Test Summary:${NC}"
    echo -e "  ${GREEN}✓${NC} OAuth2 authorization URL generation"
    echo -e "  ${GREEN}✓${NC} PKCE code verifier and challenge generation"
    echo -e "  ${GREEN}✓${NC} OAuth2 callback handling"
    echo -e "  ${GREEN}✓${NC} Session validation"
    echo -e "  ${GREEN}✓${NC} Permission checking"
    echo -e "  ${GREEN}✓${NC} Role-based access control"
    echo -e "  ${GREEN}✓${NC} Token refresh functionality"
    echo -e "  ${GREEN}✓${NC} Security features"
    echo -e "  ${GREEN}✓${NC} Database initialization"
    echo ""
    echo -e "${GREEN}🎉 OAuth2/OIDC integration is ready for production!${NC}"
else
    echo ""
    echo -e "${RED}❌ Some tests failed. Please check the output above.${NC}"
    exit 1
fi

# Clean up test database
if [ -f "$TEST_DB" ]; then
    echo -e "${YELLOW}Cleaning up test database...${NC}"
    rm -f "$TEST_DB"
fi

echo ""
echo -e "${GREEN}Test run completed successfully!${NC}" 