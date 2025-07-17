#!/bin/bash
# Test script for TuskLang License API

API_URL="http://localhost:3000"

echo "🧪 Testing TuskLang License API..."
echo ""

# Test 1: Health Check
echo "1️⃣ Testing health endpoint..."
curl -s $API_URL/health | jq .
echo ""

# Test 2: License Validation (will fail without valid license)
echo "2️⃣ Testing license validation..."
curl -s -X POST $API_URL/api/v1/validate \
  -H "Content-Type: application/json" \
  -d '{
    "license_key": "TEST-1234-5678-9012",
    "machine_id": "test-machine-001",
    "platform": "linux",
    "version": "2.0.0"
  }' | jq .
echo ""

# Test 3: Installation tracking (will fail without valid license)
echo "3️⃣ Testing installation tracking..."
curl -s -X POST $API_URL/api/v1/install \
  -H "Content-Type: application/json" \
  -d '{
    "license_key": "TEST-1234-5678-9012",
    "machine_id": "test-machine-001",
    "platform": "linux",
    "os_version": "Ubuntu 22.04",
    "hostname": "test-server",
    "version": "2.0.0"
  }' | jq .
echo ""

echo "✅ API tests complete!"
echo ""
echo "📝 Note: The validation and installation tests will fail with 'Invalid license'"
echo "    This is expected behavior until valid licenses are created in the database."