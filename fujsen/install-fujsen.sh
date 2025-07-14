#!/bin/bash
# 🚀 FUJSEN Installation Script
# =============================
# "Intelligent Configuration Revolution"
# 
# Usage: curl -fsSL https://fujsen.dev/install.sh | bash
# Or: wget -qO- https://fujsen.dev/install.sh | bash

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}"
echo "  🚀 FUJSEN Installer"
echo "  ==================="
echo "  Configuration that thinks, remembers, and adapts"
echo "  FUCK JSON, FUCK ENV, FUCK YAML!"
echo -e "${NC}"

# Configuration
FUJSEN_HOME="/opt/fujsen"
FUJSEN_PORT="8874"
PHP_MIN_VERSION="8.1"

# Check if running as root
if [[ $EUID -ne 0 ]]; then
    echo -e "${RED}✗ This script must be run as root${NC}"
    echo "  Try: sudo bash $0"
    exit 1
fi

# Check PHP
echo -e "${YELLOW}Checking requirements...${NC}"
if command -v php >/dev/null 2>&1; then
    PHP_VERSION=$(php -v | head -n1 | cut -d' ' -f2 | cut -d'.' -f1,2)
    echo -e "${GREEN}✓${NC} PHP ${PHP_VERSION} found"
    
    # Check PHP extensions
    php -m | grep -q sqlite3 && echo -e "${GREEN}✓${NC} SQLite3 extension found" || {
        echo -e "${RED}✗ SQLite3 extension required${NC}"
        echo "  Install with: apt-get install php-sqlite3"
        exit 1
    }
else
    echo -e "${RED}✗ PHP not found${NC}"
    echo "  Install PHP 8.1+ first"
    exit 1
fi

# Create directories
echo -e "${YELLOW}Creating FUJSEN directories...${NC}"
mkdir -p ${FUJSEN_HOME}/{src,api,data,cache,logs}
chmod 755 ${FUJSEN_HOME}

# Download FUJSEN files (in real deployment, this would be from GitHub releases)
echo -e "${YELLOW}Installing FUJSEN core...${NC}"

# For now, we'll create a simple installer that copies from current directory
if [[ -f "src/TuskLang.php" && -f "src/TuskLangQueryBridge.php" ]]; then
    # We're in the FUJSEN directory
    cp -r src/* ${FUJSEN_HOME}/src/
    cp -r api/* ${FUJSEN_HOME}/api/
    cp autoload.php ${FUJSEN_HOME}/
    cp api-router.php ${FUJSEN_HOME}/
    echo -e "${GREEN}✓${NC} FUJSEN files copied from current directory"
else
    # Download from repository (placeholder)
    echo -e "${YELLOW}Downloading FUJSEN from repository...${NC}"
    
    # Create minimal FUJSEN installation
    cat > ${FUJSEN_HOME}/src/TuskLang.php << 'EOF'
<?php
// Minimal TuskLang parser for FUJSEN
namespace TuskPHP\Utils;

class TuskLang {
    public static function parse(string $content): array {
        // Basic .tsk parser
        $lines = explode("\n", $content);
        $result = [];
        
        foreach ($lines as $line) {
            $line = trim($line);
            if (empty($line) || $line[0] === '#') continue;
            
            if (preg_match('/^([a-zA-Z_][a-zA-Z0-9_]*)\s*:\s*(.+)$/', $line, $matches)) {
                $key = $matches[1];
                $value = trim($matches[2], '"\'');
                $result[$key] = $value;
            }
        }
        
        return $result;
    }
}
EOF

    cat > ${FUJSEN_HOME}/api-router.php << 'EOF'
<?php
// Simple FUJSEN router
require_once 'autoload.php';

$path = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
$path = ltrim($path, '/');

if (empty($path)) $path = 'index';
$tskFile = __DIR__ . '/api/' . $path . '.tsk';

if (file_exists($tskFile)) {
    $content = file_get_contents($tskFile);
    
    // Check for #!api directive
    if (strpos($content, '#!api') === 0) {
        header('Content-Type: application/json');
        echo json_encode([
            'message' => 'Hello from FUJSEN!',
            'file' => $path . '.tsk',
            'timestamp' => time()
        ]);
    } else {
        echo "Not an API endpoint";
    }
} else {
    http_response_code(404);
    echo json_encode(['error' => 'Endpoint not found']);
}
EOF

    cat > ${FUJSEN_HOME}/autoload.php << 'EOF'
<?php
// FUJSEN autoloader
foreach (glob(__DIR__ . '/src/*.php') as $file) {
    require_once $file;
}
EOF

    mkdir -p ${FUJSEN_HOME}/api
    cat > ${FUJSEN_HOME}/api/hello.tsk << 'EOF'
#!api
# Hello World FUJSEN endpoint
message: "Hello from FUJSEN!"
timestamp: php(time())
server: "Intelligent Configuration"
EOF

    echo -e "${GREEN}✓${NC} Minimal FUJSEN installation created"
fi

# Create systemd service
echo -e "${YELLOW}Creating FUJSEN service...${NC}"
cat > /etc/systemd/system/fujsen.service << EOF
[Unit]
Description=FUJSEN Development Server
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=${FUJSEN_HOME}
ExecStart=/usr/bin/php -S 0.0.0.0:${FUJSEN_PORT} api-router.php
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

# Create CLI command
echo -e "${YELLOW}Creating FUJSEN CLI...${NC}"
cat > /usr/local/bin/fujsen << EOF
#!/bin/bash
# FUJSEN CLI Tool

case "\$1" in
    start)
        systemctl start fujsen
        echo "🚀 FUJSEN started on port ${FUJSEN_PORT}"
        ;;
    stop)
        systemctl stop fujsen
        echo "🛑 FUJSEN stopped"
        ;;
    restart)
        systemctl restart fujsen
        echo "🔄 FUJSEN restarted"
        ;;
    status)
        systemctl status fujsen
        ;;
    serve)
        cd ${FUJSEN_HOME}
        echo "🚀 Starting FUJSEN development server on port ${FUJSEN_PORT}"
        php -S localhost:${FUJSEN_PORT} api-router.php
        ;;
    test)
        echo "🧪 Testing FUJSEN endpoints..."
        curl -s http://localhost:${FUJSEN_PORT}/hello || echo "FUJSEN not running"
        ;;
    *)
        echo "FUJSEN - Intelligent Configuration System"
        echo "Usage: fujsen {start|stop|restart|status|serve|test}"
        echo ""
        echo "Commands:"
        echo "  start    - Start FUJSEN service"
        echo "  stop     - Stop FUJSEN service"
        echo "  restart  - Restart FUJSEN service"
        echo "  status   - Show FUJSEN status"
        echo "  serve    - Start development server"
        echo "  test     - Test FUJSEN endpoints"
        echo ""
        echo "Endpoints:"
        echo "  http://localhost:${FUJSEN_PORT}/hello"
        ;;
esac
EOF

chmod +x /usr/local/bin/fujsen

# Set permissions
chown -R www-data:www-data ${FUJSEN_HOME}

# Enable and start service
systemctl daemon-reload
systemctl enable fujsen

echo -e "\n${GREEN}🚀 FUJSEN installation complete!${NC}"
echo -e "\nNext steps:"
echo -e "  1. Start FUJSEN: ${BLUE}fujsen start${NC}"
echo -e "  2. Test endpoint: ${BLUE}curl http://localhost:${FUJSEN_PORT}/hello${NC}"
echo -e "  3. Development: ${BLUE}fujsen serve${NC}"
echo -e "  4. Create .tsk files in ${FUJSEN_HOME}/api/"
echo -e "\nFUJSEN is running on port ${FUJSEN_PORT}"
echo -e "TUSK framework typically runs on port 8875"
echo -e "\n${GREEN}FUCK JSON, FUCK ENV, FUCK YAML! 🔥${NC}" 