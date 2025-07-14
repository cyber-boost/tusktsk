#!/bin/bash

echo "🐳 Starting FUJSEN in Docker Container"
echo "======================================"

# Run tests in background after a delay to not interfere with server startup
(sleep 5 && php /app/test-all-functions.php > /app/logs/test-results.log 2>&1) &

echo ""
echo "🌐 Starting HTTP server on port 8874..."
echo "Access endpoints at: http://localhost:8874/"
echo ""
echo "Available endpoints:"
echo "- /echo-simple?format=json&test=123"
echo "- /status"
echo "- /users"
echo ""
echo "Test results will be written to /app/logs/test-results.log"
echo ""

# Start the HTTP server
cd /app && php -S 0.0.0.0:8874 api-router.php 