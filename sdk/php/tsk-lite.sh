#!/bin/bash
# 🐘 TuskLang Lite Installer - Modular & Intelligent
# ==================================================
# "In the spirit of grace and unity, we bring everything together as one"
# 
# Features:
# - PHP 8+ Support (User choice or current)
# - Modular Installation (ivory, flex, grim)
# - Intelligent Configuration
# - Mother DB Integration
# - SDK Selection
#
# Strong. Secure. Scalable. 🐘

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
INSTALL_PREFIX="/usr/local"
TSK_HOME="/root/.tsk"
TSK_CONFIG="/root/tsk"
IVORY_HOME="/root/.ivory"
FLEX_HOME="/root/.flex"
GRIM_HOME="/root/.grim"
MOTHER_DB_API="https://api.tusklang.org/install"
LATEST_TARBALL="https://tusklang.org/latest.tar.gz"

# Functions
print_header() {
    echo -e "${BLUE}🐘 TuskLang Lite Installer${NC}"
    echo -e "${BLUE}==========================${NC}"
    echo ""
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}→ $1${NC}"
}

print_warning() {
    echo -e "${PURPLE}⚠ $1${NC}"
}

check_root() {
    if [ "$EUID" -ne 0 ]; then 
        print_error "Please run as root (use sudo)"
        exit 1
    fi
}

detect_php_version() {
    if command -v php >/dev/null 2>&1; then
        PHP_VERSION=$(php -r "echo PHP_MAJOR_VERSION.'.'.PHP_MINOR_VERSION;" 2>/dev/null || echo "unknown")
        if [[ "$PHP_VERSION" == "unknown" ]]; then
            print_warning "Could not detect PHP version"
            return 1
        fi
        
        # Check if it's PHP 8+
        MAJOR_VERSION=$(echo "$PHP_VERSION" | cut -d. -f1)
        if [ "$MAJOR_VERSION" -ge 8 ]; then
            print_success "Detected PHP $PHP_VERSION (compatible)"
            return 0
        else
            print_warning "Detected PHP $PHP_VERSION (needs PHP 8+)"
            return 1
        fi
    else
        print_warning "PHP not found"
        return 1
    fi
}

install_php_8_plus() {
    print_info "Installing PHP 8+..."
    
    # Detect OS
    if command -v apt-get >/dev/null 2>&1; then
        # Ubuntu/Debian
        apt-get update
        apt-get install -y software-properties-common
        add-apt-repository -y ppa:ondrej/php
        apt-get update
        
        # Install PHP 8.4 (latest stable)
        apt-get install -y php8.4 php8.4-cli php8.4-fpm php8.4-common \
            php8.4-mysql php8.4-pgsql php8.4-sqlite3 php8.4-xml \
            php8.4-curl php8.4-gd php8.4-mbstring php8.4-zip \
            php8.4-bcmath php8.4-intl php8.4-opcache php8.4-memcached
        
        # Set PHP 8.4 as default
        update-alternatives --set php /usr/bin/php8.4
        
        print_success "PHP 8.4 installed and set as default"
        
    elif command -v yum >/dev/null 2>&1; then
        # RHEL/CentOS
        yum install -y epel-release
        yum install -y https://rpms.remirepo.net/enterprise/remi-release-7.rpm
        yum-config-manager --enable remi-php84
        yum install -y php php-cli php-fpm php-common php-mysql php-pgsql \
            php-sqlite3 php-xml php-curl php-gd php-mbstring php-zip \
            php-bcmath php-intl php-opcache php-memcached
        
        print_success "PHP 8.4 installed via yum"
        
    else
        print_error "Unsupported package manager"
        exit 1
    fi
    
    # Verify installation
    if detect_php_version; then
        print_success "PHP 8+ installation verified"
    else
        print_error "PHP 8+ installation failed"
        exit 1
    fi
}

download_and_extract_tarball() {
    print_info "Downloading latest TuskLang tarball..."
    
    # Create temporary directory
    TEMP_DIR=$(mktemp -d)
    cd "$TEMP_DIR"
    
    # Download tarball
    if curl -L -o latest.tar.gz "$LATEST_TARBALL"; then
        print_success "Downloaded latest.tar.gz"
    else
        print_error "Failed to download latest.tar.gz"
        exit 1
    fi
    
    # Extract tarball
    print_info "Extracting tarball..."
    if tar -xzf latest.tar.gz; then
        print_success "Extracted tarball"
    else
        print_error "Failed to extract tarball"
        exit 1
    fi
    
    # Copy folders to /root/
    print_info "Installing to /root/..."
    
    # Copy ivory to .ivory
    if [ -d "ivory" ]; then
        cp -r ivory "$IVORY_HOME"
        print_success "Installed ivory to $IVORY_HOME"
    fi
    
    # Copy flex to .flex
    if [ -d "flex" ]; then
        cp -r flex "$FLEX_HOME"
        print_success "Installed flex to $FLEX_HOME"
    fi
    
    # Copy grim to .grim
    if [ -d "grim" ]; then
        cp -r grim "$GRIM_HOME"
        print_success "Installed grim to $GRIM_HOME"
    fi
    
    # Copy tsk to .tsk
    if [ -d "tsk" ]; then
        cp -r tsk "$TSK_HOME"
        print_success "Installed tsk to $TSK_HOME"
    fi
    
    # Copy tsk-config to /root/tsk/
    if [ -d "tsk-config" ]; then
        mkdir -p "$TSK_CONFIG"
        cp -r tsk-config/* "$TSK_CONFIG/"
        print_success "Installed tsk-config to $TSK_CONFIG"
    fi
    
    # Clean up
    cd /
    rm -rf "$TEMP_DIR"
}

install_tsk_cli() {
    print_info "Installing TuskLang CLI..."
    
    # Create CLI script
    cat > "$INSTALL_PREFIX/bin/tsk" << 'EOF'
#!/usr/bin/env php
<?php
/**
 * TuskLang CLI - Modular Command Interface
 * Strong. Secure. Scalable. 🐘
 */

// Define paths
define('TSK_HOME', '/root/.tsk');
define('TSK_CONFIG', '/root/tsk');
define('IVORY_HOME', '/root/.ivory');
define('FLEX_HOME', '/root/.flex');
define('GRIM_HOME', '/root/.grim');

// Colors for output
function colorize($text, $color = null) {
    $colors = [
        'red' => "\033[0;31m",
        'green' => "\033[0;32m", 
        'yellow' => "\033[1;33m",
        'blue' => "\033[0;34m",
        'purple' => "\033[0;35m",
        'cyan' => "\033[0;36m",
        'white' => "\033[1;37m",
        'reset' => "\033[0m"
    ];
    
    if (!$color) return $text;
    return ($colors[$color] ?? '') . $text . $colors['reset'];
}

// Parse command line arguments
$command = $argv[1] ?? 'help';
$subcommand = $argv[2] ?? null;
$args = array_slice($argv, 2);

// Main command router
switch ($command) {
    case 'install':
    case 'i':
        handleInstallCommand($subcommand, array_slice($argv, 3));
        break;
        
    case 'status':
        handleStatusCommand($subcommand, array_slice($argv, 3));
        break;
        
    case 'config':
        handleConfigCommand($subcommand, array_slice($argv, 3));
        break;
        
    case 'update':
        handleUpdateCommand($subcommand, array_slice($argv, 3));
        break;
        
    case 'version':
        handleVersionCommand();
        break;
        
    case 'tuskphp':
        handleTuskPHPCommand($subcommand, array_slice($argv, 3));
        break;
            
    case 'grim':
        handleGrimCommand($subcommand, array_slice($argv, 3));
        break;
        
    case 'help':
    default:
        showHelp();
        break;
}

// Install commands
function handleInstallCommand($subcommand, $args) {
    switch ($subcommand) {
        case 'ivory':
            installIvory($args);
            break;
            
        case 'flex':
            installFlex($args);
            break;
            
        case 'grim':
            installGrim($args);
            break;
            
        case 'all':
            installAll($args);
            break;
            
        default:
            echo colorize("Install Commands:\n", 'yellow');
            echo "  tsk install ivory    Install Ivory framework\n";
            echo "  tsk install flex     Install Flex system\n";
            echo "  tsk install grim     Install Grim tools\n";
            echo "  tsk install all      Install all components\n";
            echo "\nShort forms:\n";
            echo "  tsk i ivory          Install Ivory framework\n";
            echo "  tsk i flex           Install Flex system\n";
            echo "  tsk i grim           Install Grim tools\n";
            break;
    }
}

function installIvory($args) {
    echo colorize("🐘 Installing Ivory Framework...\n", 'blue');
    
    if (!is_dir(IVORY_HOME)) {
        echo colorize("❌ Ivory not found at " . IVORY_HOME . "\n", 'red');
        echo colorize("Run: curl -sSL tusklang.org/tsk.sh | sudo bash\n", 'yellow');
        exit(1);
    }
    
    // Run Ivory installation
    $installScript = IVORY_HOME . '/install.sh';
    if (file_exists($installScript)) {
        echo colorize("🚀 Running Ivory installer...\n", 'cyan');
        passthru("bash $installScript");
    } else {
        echo colorize("⚠️  Ivory install script not found\n", 'yellow');
        echo colorize("✅ Ivory files are available at " . IVORY_HOME . "\n", 'green');
    }
    
    echo colorize("✅ Ivory installation completed\n", 'green');
}

function installFlex($args) {
    echo colorize("⚡ Installing Flex System...\n", 'blue');
    
    if (!is_dir(FLEX_HOME)) {
        echo colorize("❌ Flex not found at " . FLEX_HOME . "\n", 'red');
        echo colorize("Run: curl -sSL tusklang.org/tsk.sh | sudo bash\n", 'yellow');
        exit(1);
    }
    
    // Run Flex installation
    $installScript = FLEX_HOME . '/install.sh';
    if (file_exists($installScript)) {
        echo colorize("🚀 Running Flex installer...\n", 'cyan');
        passthru("bash $installScript");
    } else {
        echo colorize("⚠️  Flex install script not found\n", 'yellow');
        echo colorize("✅ Flex files are available at " . FLEX_HOME . "\n", 'green');
    }
    
    echo colorize("✅ Flex installation completed\n", 'green');
}

function installGrim($args) {
    echo colorize("💀 Installing Grim Tools...\n", 'blue');
    
    if (!is_dir(GRIM_HOME)) {
        echo colorize("❌ Grim not found at " . GRIM_HOME . "\n", 'red');
        echo colorize("Run: curl -sSL tusklang.org/tsk.sh | sudo bash\n", 'yellow');
        exit(1);
    }
    
    // Run Grim installation
    $installScript = GRIM_HOME . '/install.sh';
    if (file_exists($installScript)) {
        echo colorize("🚀 Running Grim installer...\n", 'cyan');
        passthru("bash $installScript");
    } else {
        echo colorize("⚠️  Grim install script not found\n", 'yellow');
        echo colorize("✅ Grim files are available at " . GRIM_HOME . "\n", 'green');
    }
    
    echo colorize("✅ Grim installation completed\n", 'green');
}

function installAll($args) {
    echo colorize("🚀 Installing All Components...\n", 'blue');
    
    installIvory([]);
    installFlex([]);
    installGrim([]);
    
    echo colorize("🎉 All components installed successfully!\n", 'green');
}

// Status commands
function handleStatusCommand($subcommand, $args) {
    echo colorize("📊 TuskLang Status\n", 'blue');
    echo colorize("==================\n", 'blue');
    
    // Check PHP version
    $phpVersion = phpversion();
    echo colorize("PHP Version: $phpVersion\n", 'cyan');
    
    // Check component status
    $components = [
        'ivory' => IVORY_HOME,
        'flex' => FLEX_HOME,
        'grim' => GRIM_HOME,
        'tsk' => TSK_HOME,
        'tsk-config' => TSK_CONFIG
    ];
    
    foreach ($components as $name => $path) {
        if (is_dir($path)) {
            echo colorize("✅ $name: " . $path . "\n", 'green');
        } else {
            echo colorize("❌ $name: Not found\n", 'red');
        }
    }
    
    // Check CLI
    if (file_exists('/usr/local/bin/tsk')) {
        echo colorize("✅ CLI: /usr/local/bin/tsk\n", 'green');
    } else {
        echo colorize("❌ CLI: Not found\n", 'red');
    }
}

// Config commands
function handleConfigCommand($subcommand, $args) {
    switch ($subcommand) {
        case 'show':
            showConfig();
            break;
            
        case 'edit':
            editConfig($args);
            break;
            
        case 'reset':
            resetConfig();
            break;
            
        default:
            echo colorize("Config Commands:\n", 'yellow');
            echo "  tsk config show     Show current configuration\n";
            echo "  tsk config edit     Edit configuration\n";
            echo "  tsk config reset    Reset to defaults\n";
            break;
    }
}

function showConfig() {
    echo colorize("⚙️  TuskLang Configuration\n", 'blue');
    echo colorize("==========================\n", 'blue');
    
    $configFile = TSK_CONFIG . '/config.json';
    if (file_exists($configFile)) {
        $config = json_decode(file_get_contents($configFile), true);
        echo json_encode($config, JSON_PRETTY_PRINT) . "\n";
    } else {
        echo colorize("No configuration file found\n", 'yellow');
    }
}

function editConfig($args) {
    $configFile = TSK_CONFIG . '/config.json';
    if (file_exists($configFile)) {
        passthru("nano $configFile");
    } else {
        echo colorize("No configuration file found\n", 'yellow');
    }
}

function resetConfig() {
    echo colorize("🔄 Resetting configuration...\n", 'blue');
    
    $configFile = TSK_CONFIG . '/config.json';
    $defaultConfig = [
        'php_version' => phpversion(),
        'install_date' => date('Y-m-d H:i:s'),
        'components' => ['ivory', 'flex', 'grim'],
        'sdk_language' => 'all'
    ];
    
    file_put_contents($configFile, json_encode($defaultConfig, JSON_PRETTY_PRINT));
    echo colorize("✅ Configuration reset to defaults\n", 'green');
}

// Update commands
function handleUpdateCommand($subcommand, $args) {
    echo colorize("🔄 Updating TuskLang...\n", 'blue');
    
    # Download and extract latest
    passthru("curl -sSL tusklang.org/tsk.sh | sudo bash");
    
    echo colorize("✅ TuskLang updated successfully\n", 'green');
}

function handleVersionCommand() {
    $version = '1.0.0-lite';
    if (file_exists(TSK_HOME . '/VERSION')) {
        $version = trim(file_get_contents(TSK_HOME . '/VERSION'));
    }
    
    echo colorize("🐘 TuskLang CLI v$version\n", 'blue');
    echo colorize("Strong. Secure. Scalable.\n", 'cyan');
}

function showHelp() {
    echo colorize("🐘 TuskLang CLI - Modular Command Interface\n", 'blue');
    echo colorize("Strong. Secure. Scalable.\n\n", 'cyan');
    
    echo colorize("Usage: tsk <command> [options]\n\n", 'white');
    
    echo colorize("🚀 Installation Commands:\n", 'yellow');
    echo "  tsk install ivory    Install Ivory framework\n";
    echo "  tsk install flex     Install Flex system\n";
    echo "  tsk install grim     Install Grim tools\n";
    echo "  tsk install all      Install all components\n\n";
    
    echo colorize("📊 Status Commands:\n", 'yellow');
    echo "  tsk status           Show system status\n\n";
    
    echo colorize("⚙️  Configuration Commands:\n", 'yellow');
    echo "  tsk config show      Show current configuration\n";
    echo "  tsk config edit      Edit configuration\n";
    echo "  tsk config reset     Reset to defaults\n\n";
    
    echo colorize("🔄 Update Commands:\n", 'yellow');
    echo "  tsk update           Update TuskLang\n\n";
    
    echo colorize("🐘 TuskPHP Commands:\n", 'yellow');
    echo "  tsk tuskphp status   Check TuskPHP installation status\n";
    echo "  tsk tuskphp install  Install TuskPHP framework\n";
    echo "  tsk tuskphp logs     Show installation logs\n\n";
    
    echo colorize("💀 Grim Commands:\n", 'yellow');
    echo "  tsk grim status      Check Grim installation status\n";
    echo "  tsk grim install     Install Grim AI backup\n";
    echo "  tsk grim logs        Show installation logs\n\n";
    
    echo colorize("📋 Utility Commands:\n", 'yellow');
    echo "  tsk version          Show version information\n";
    echo "  tsk help             Show this help\n\n";
    
    echo colorize("Short Forms:\n", 'purple');
    echo "  tsk i ivory          Install Ivory framework\n";
    echo "  tsk i flex           Install Flex system\n";
    echo "  tsk i grim           Install Grim tools\n\n";
    
    echo colorize("Examples:\n", 'purple');
    echo "  tsk i ivory          # Install Ivory framework\n";
    echo "  tsk status           # Check system status\n";
    echo "  tsk config show      # Show configuration\n";
}

collect_installation_data() {
    print_info "Collecting installation data for Mother DB..."
    
    # Get IP address
    IP_ADDRESS=$(curl -s ifconfig.me 2>/dev/null || echo "unknown")
    
    # Get system information
    OS_INFO=$(uname -a)
    PHP_VERSION=$(php -r "echo PHP_VERSION;" 2>/dev/null || echo "unknown")
    
    echo ""
    echo -e "${CYAN}🐘 TuskLang Project Setup${NC}"
    echo -e "${CYAN}========================${NC}"
    echo ""
    
    # Question 1: Project Name
    echo -e "${YELLOW}1. What's your project name?${NC}"
    echo -n "Project name (e.g., my-tusk-app): "
    read -r PROJECT_NAME
    
    # Validate project name
    if [[ -z "$PROJECT_NAME" ]]; then
        PROJECT_NAME="my-tusk-app"
        print_info "Using default project name: $PROJECT_NAME"
    fi
    
    # Install TSK in background after project name
    print_info "Installing TSK in background..."
    nohup bash "$0" install_tsk_only > /tmp/tsk-install.log 2>&1 &
    TSK_PID=$!
    print_success "TSK installation started (PID: $TSK_PID)"
    
    # Question 2: Default Environment
    echo -e "${YELLOW}2. What's your default environment?${NC}"
    echo "1. Development"
    echo "2. Staging"
    echo "3. Production"
    echo -n "Enter choice (1-3): "
    read -r DEFAULT_ENVIRONMENT
    
    case $DEFAULT_ENVIRONMENT in
        1) DEFAULT_ENVIRONMENT="development" ;;
        2) DEFAULT_ENVIRONMENT="staging" ;;
        3) DEFAULT_ENVIRONMENT="production" ;;
        *) DEFAULT_ENVIRONMENT="development" ;;
    esac
    
    # Question 3: Error Alerts
    echo -e "${YELLOW}3. Do you want to receive error alerts?${NC}"
    echo "1. Yes"
    echo "2. No"
    echo -n "Enter choice (1-2): "
    read -r ERROR_ALERTS
    
    ALERT_EMAIL=""
    TECH_NEWS="no"
    
    if [[ "$ERROR_ALERTS" == "1" ]]; then
        echo -n "What's your email? "
        read -r ALERT_EMAIL
        
        echo -e "${YELLOW}Want to receive tech news from Tusk team?${NC}"
        echo "1. Yes"
        echo "2. No"
        echo -n "Enter choice (1-2): "
        read -r TECH_NEWS_CHOICE
        
        if [[ "$TECH_NEWS_CHOICE" == "1" ]]; then
            TECH_NEWS="yes"
        fi
    fi
    
    # Question 4: Runtime Evaluation Mode
    echo -e "${YELLOW}4. Preferred Runtime Evaluation Mode${NC}"
    echo "1. Lazy (evaluate when accessed)"
    echo "2. Eager (evaluate on startup)"
    echo -n "Enter choice (1-2): "
    read -r RUNTIME_MODE
    
    case $RUNTIME_MODE in
        1) RUNTIME_MODE="lazy" ;;
        2) RUNTIME_MODE="eager" ;;
        *) RUNTIME_MODE="lazy" ;;
    esac
    
    # Question 5: External Connections
    echo -e "${YELLOW}5. Allow External Connections from Config?${NC}"
    echo "1. No external connections"
    echo "2. Limited (whitelist only)"
    echo "3. Unrestricted"
    echo -n "Enter choice (1-3): "
    read -r EXTERNAL_CONNECTIONS
    
    case $EXTERNAL_CONNECTIONS in
        1) EXTERNAL_CONNECTIONS="none" ;;
        2) EXTERNAL_CONNECTIONS="limited" ;;
        3) EXTERNAL_CONNECTIONS="unrestricted" ;;
        *) EXTERNAL_CONNECTIONS="limited" ;;
    esac
    
    # Question 6: Default Port
    echo -e "${YELLOW}6. Set a default fallback port if none specified${NC}"
    echo -n "Default port (default: 8080): "
    read -r DEFAULT_PORT
    
    if [[ -z "$DEFAULT_PORT" ]]; then
        DEFAULT_PORT="8080"
    fi
    
    # Question 7: Error Reporting Level
    echo -e "${YELLOW}7. Error Reporting Level${NC}"
    echo "1. Verbose (recommended for dev)"
    echo "2. Normal"
    echo "3. Minimal (production-friendly)"
    echo -n "Enter choice (1-3): "
    read -r ERROR_REPORTING
    
    case $ERROR_REPORTING in
        1) ERROR_REPORTING="verbose" ;;
        2) ERROR_REPORTING="normal" ;;
        3) ERROR_REPORTING="minimal" ;;
        *) ERROR_REPORTING="normal" ;;
    esac
    
    # Question 8: Languages
    echo -e "${YELLOW}8. What languages do you want installed? (PHP is a must)${NC}"
    echo "Available languages:"
    echo "1. All languages"
    echo "2. PHP only"
    echo "3. JavaScript/TypeScript"
    echo "4. Python"
    echo "5. Bash/Shell"
    echo "6. Go"
    echo "7. Rust"
    echo "8. Other"
    echo -n "Enter choice (1-8): "
    read -r LANGUAGE_CHOICE
    
    # Question 9: SDK Location
    echo -e "${YELLOW}9. Where do you want the SDKs saved?${NC}"
    CURRENT_DIR=$(pwd)
    echo "Current directory: $CURRENT_DIR"
    echo -n "SDK location (press Enter for current directory): "
    read -r SDK_LOCATION
    
    if [[ -z "$SDK_LOCATION" ]]; then
        SDK_LOCATION="$CURRENT_DIR"
    fi
    
    # Question 10: TuskPHP Framework
    echo -e "${YELLOW}10. Do you want to install TuskPHP framework?${NC}"
    echo "Includes advanced server tools, predictive performance architecture,"
    echo "personal email server, and 70+ more features."
    echo "You have enough disk space to install..."
    echo "1. Yes"
    echo "2. No"
    echo -n "Enter choice (1-2): "
    read -r TUSKPHP_INSTALL
    
    TUSKPHP_STATUS="not_installed"
    if [[ "$TUSKPHP_INSTALL" == "1" ]]; then
        print_info "Installing TuskPHP framework in background..."
        print_warning "This will take some time. Check /var/www/tusk folder in a couple of minutes"
        print_info "Or run 'tsk tuskphp' for status"
        
        # Start TuskPHP installation in background
        nohup bash /root/.ivory/iv.sh > /tmp/tuskphp-install.log 2>&1 &
        TUSKPHP_PID=$!
        print_success "TuskPHP installation started (PID: $TUSKPHP_PID)"
        TUSKPHP_STATUS="installing"
    fi
    
    # Question 11: Grim AI Backup
    echo -e "${YELLOW}11. Do you want Grim AI backup and security monitoring software installed?${NC}"
    echo "1. Yes"
    echo "2. No"
    echo -n "Enter choice (1-2): "
    read -r GRIM_INSTALL
    
    GRIM_STATUS="not_installed"
    if [[ "$GRIM_INSTALL" == "1" ]]; then
        print_info "Installing Grim AI backup in background..."
        
        # Start Grim installation in background
        nohup bash /root/.grim/grim.sh > /tmp/grim-install.log 2>&1 &
        GRIM_PID=$!
        print_success "Grim installation started (PID: $GRIM_PID)"
        GRIM_STATUS="installing"
    fi
    
    # Question 12: Profit-sharing Information
    echo -e "${YELLOW}12. Display TuskLang Profit-sharing & Contributor Information during install?${NC}"
    echo "1. Yes"
    echo "2. No"
    echo -n "Enter choice (1-2): "
    read -r SHOW_PROFIT_SHARING
    
    if [[ "$SHOW_PROFIT_SHARING" == "1" ]]; then
        echo ""
        echo -e "${CYAN}🐘 TuskLang Profit-sharing & Contributor Information${NC}"
        echo -e "${CYAN}==================================================${NC}"
        echo ""
        echo "TuskLang is a community-driven project that believes in sustainable"
        echo "open-source development through profit-sharing with contributors."
        echo ""
        echo "• 30% of profits go to active contributors"
        echo "• 20% to core maintainers"
        echo "• 50% to project development and infrastructure"
        echo ""
        echo "Join our community and contribute to the future of TuskLang!"
        echo ""
    fi
    
    # Check TSK installation status
    print_info "Checking TSK installation status..."
    if [[ -f "$TSK_HOME/elder.tsk" ]]; then
        TSK_STATUS="installed"
        print_success "TSK installation completed"
    else
        TSK_STATUS="failed"
        print_warning "TSK installation may have failed, attempting reinstall..."
        
        # Try to install TSK again (not in background)
        if bash "$0" install_tsk_only; then
            TSK_STATUS="installed"
            print_success "TSK installation completed on retry"
        else
            print_error "TSK installation failed"
        fi
    fi
    
    # Prepare data for Mother DB
    INSTALL_DATA=$(cat <<EOF
{
    "ip_address": "$IP_ADDRESS",
    "os_info": "$OS_INFO",
    "php_version": "$PHP_VERSION",
    "install_timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "project_name": "$PROJECT_NAME",
    "default_environment": "$DEFAULT_ENVIRONMENT",
    "error_alerts": "$ERROR_ALERTS",
    "alert_email": "$ALERT_EMAIL",
    "tech_news": "$TECH_NEWS",
    "runtime_mode": "$RUNTIME_MODE",
    "external_connections": "$EXTERNAL_CONNECTIONS",
    "default_port": "$DEFAULT_PORT",
    "error_reporting": "$ERROR_REPORTING",
    "language_choice": "$LANGUAGE_CHOICE",
    "sdk_location": "$SDK_LOCATION",
    "tuskphp_install": "$TUSKPHP_INSTALL",
    "tuskphp_status": "$TUSKPHP_STATUS",
    "grim_install": "$GRIM_INSTALL",
    "grim_status": "$GRIM_STATUS",
    "tsk_status": "$TSK_STATUS",
    "show_profit_sharing": "$SHOW_PROFIT_SHARING"
}
EOF
)
    
    # Send to Mother DB (placeholder for now)
    print_info "Sending installation data to Mother DB..."
    
    # For now, just save locally
    echo "$INSTALL_DATA" > "$TSK_CONFIG/install-data.json"
    print_success "Installation data saved to $TSK_CONFIG/install-data.json"
    
    # Create elder.tsk file with installation status
    cat > "$TSK_HOME/elder.tsk" << EOF
# TuskLang Elder Configuration
# Generated on $(date)

project_name: "$PROJECT_NAME"
default_environment: "$DEFAULT_ENVIRONMENT"
error_alerts: $([[ "$ERROR_ALERTS" == "1" ]] && echo "true" || echo "false")
alert_email: "$ALERT_EMAIL"
tech_news: $([[ "$TECH_NEWS" == "yes" ]] && echo "true" || echo "false")
runtime_mode: "$RUNTIME_MODE"
external_connections: "$EXTERNAL_CONNECTIONS"
default_port: $DEFAULT_PORT
error_reporting: "$ERROR_REPORTING"
language_choice: "$LANGUAGE_CHOICE"
sdk_location: "$SDK_LOCATION"

# Installation Status
tsk_installed: $([[ "$TSK_STATUS" == "installed" ]] && echo "true" || echo "false")
tuskphp_installed: $([[ "$TUSKPHP_STATUS" == "installed" ]] && echo "true" || echo "false")
grim_installed: $([[ "$GRIM_STATUS" == "installed" ]] && echo "true" || echo "false")

# Installation Timestamps
install_date: "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
php_version: "$PHP_VERSION"
EOF
    
    # TODO: Implement actual API call when api.tusklang.org is ready
    # curl -X POST -H "Content-Type: application/json" -d "$INSTALL_DATA" "$MOTHER_DB_API"
    
    print_info "Mother DB notification will be implemented when API is ready"
    
    # Show installation summary
    echo ""
    echo -e "${GREEN}🐘 TuskLang Project Setup Complete!${NC}"
    echo -e "${GREEN}=====================================${NC}"
    echo ""
    echo "Project: $PROJECT_NAME"
    echo "Environment: $DEFAULT_ENVIRONMENT"
    echo "TSK Status: $TSK_STATUS"
    echo "TuskPHP Status: $TUSKPHP_STATUS"
    echo "Grim Status: $GRIM_STATUS"
    echo ""
    echo "Configuration saved to: $TSK_CONFIG/install-data.json"
    echo "Elder config saved to: $TSK_HOME/elder.tsk"
    echo ""
}

setup_permissions() {
    print_info "Setting up permissions..."
    
    # Set ownership for all TuskLang directories
    chown -R root:root "$TSK_HOME"
    chown -R root:root "$TSK_CONFIG"
    chown -R root:root "$IVORY_HOME"
    chown -R root:root "$FLEX_HOME"
    chown -R root:root "$GRIM_HOME"
    
    # Set permissions
    chmod -R 755 "$TSK_HOME"
    chmod -R 755 "$TSK_CONFIG"
    chmod -R 755 "$IVORY_HOME"
    chmod -R 755 "$FLEX_HOME"
    chmod -R 755 "$GRIM_HOME"
    
    # Make CLI executable
    chmod +x "$INSTALL_PREFIX/bin/tsk"
    
    print_success "Permissions configured"
}

create_default_config() {
    print_info "Creating default configuration..."
    
    # Create default config
    cat > "$TSK_CONFIG/config.json" << 'EOF'
{
    "php_version": "8.4",
    "install_date": "2024-01-01T00:00:00Z",
    "components": {
        "ivory": {
            "installed": false,
            "version": "1.0.0"
        },
        "flex": {
            "installed": false,
            "version": "1.0.0"
        },
        "grim": {
            "installed": false,
            "version": "1.0.0"
        }
    },
    "sdk_language": "all",
    "use_case": "web_development",
    "experience_level": "intermediate",
    "team_size": "solo",
    "environment": "development",
    "integration_needs": "database",
    "performance_requirements": "standard"
}
EOF
    
    print_success "Default configuration created"
}

install_tsk_only() {
    print_info "Installing TSK core components only..."
    
    # Download and extract tarball
    download_and_extract_tarball
    
    # Install CLI
    install_tsk_cli
    
    # Setup permissions
    setup_permissions
    
    # Create default config
    create_default_config
    
    # Create version file
    echo "1.0.0-lite" > "$TSK_HOME/VERSION"
    
    print_success "TSK core installation completed"
    return 0
}

# TuskPHP commands
function handleTuskPHPCommand($subcommand, $args) {
    switch ($subcommand) {
        case 'status':
            echo colorize("🐘 TuskPHP Installation Status\n", 'blue');
            echo colorize("==============================\n", 'blue');
            
            $installLog = '/tmp/tuskphp-install.log';
            $tuskDir = '/var/www/tusk';
            
            if (is_dir($tuskDir)) {
                echo colorize("✅ TuskPHP directory found: $tuskDir\n", 'green');
                
                // Check for key files
                $keyFiles = ['index.php', 'config.php', 'composer.json'];
                foreach ($keyFiles as $file) {
                    if (file_exists($tuskDir . '/' . $file)) {
                        echo colorize("✅ $file found\n", 'green');
                    } else {
                        echo colorize("❌ $file missing\n", 'red');
                    }
                }
            } else {
                echo colorize("❌ TuskPHP directory not found: $tuskDir\n", 'red');
            }
            
            if (file_exists($installLog)) {
                echo colorize("\n📋 Installation Log (last 10 lines):\n", 'cyan');
                $logLines = file($installLog);
                $lastLines = array_slice($logLines, -10);
                foreach ($lastLines as $line) {
                    echo trim($line) . "\n";
                }
            } else {
                echo colorize("⚠️  Installation log not found\n", 'yellow');
            }
            break;
            
        case 'install':
            echo colorize("🚀 Installing TuskPHP framework...\n", 'blue');
            if (file_exists('/root/.ivory/iv.sh')) {
                passthru('bash /root/.ivory/iv.sh');
            } else {
                echo colorize("❌ TuskPHP installer not found\n", 'red');
            }
            break;
            
        case 'logs':
            $logFile = '/tmp/tuskphp-install.log';
            if (file_exists($logFile)) {
                echo colorize("📋 TuskPHP Installation Log:\n", 'blue');
                passthru("cat $logFile");
            } else {
                echo colorize("❌ Installation log not found\n", 'red');
            }
            break;
            
        default:
            echo colorize("TuskPHP Commands:\n", 'yellow');
            echo "  tsk tuskphp status    Check TuskPHP installation status\n";
            echo "  tsk tuskphp install   Install TuskPHP framework\n";
            echo "  tsk tuskphp logs      Show installation logs\n";
            break;
    }
}

// Grim commands
function handleGrimCommand($subcommand, $args) {
    switch ($subcommand) {
        case 'status':
            echo colorize("💀 Grim AI Backup Status\n", 'blue');
            echo colorize("========================\n", 'blue');
            
            $installLog = '/tmp/grim-install.log';
            $grimDir = '/root/.grim';
            
            if (is_dir($grimDir)) {
                echo colorize("✅ Grim directory found: $grimDir\n", 'green');
                
                // Check for key files
                $keyFiles = ['grim.sh', 'config.json', 'README.md'];
                foreach ($keyFiles as $file) {
                    if (file_exists($grimDir . '/' . $file)) {
                        echo colorize("✅ $file found\n", 'green');
                    } else {
                        echo colorize("❌ $file missing\n", 'red');
                    }
                }
            } else {
                echo colorize("❌ Grim directory not found: $grimDir\n", 'red');
            }
            
            if (file_exists($installLog)) {
                echo colorize("\n📋 Installation Log (last 10 lines):\n", 'cyan');
                $logLines = file($installLog);
                $lastLines = array_slice($logLines, -10);
                foreach ($lastLines as $line) {
                    echo trim($line) . "\n";
                }
            } else {
                echo colorize("⚠️  Installation log not found\n", 'yellow');
            }
            break;
            
        case 'install':
            echo colorize("🚀 Installing Grim AI backup...\n", 'blue');
            if (file_exists('/root/.grim/grim.sh')) {
                passthru('bash /root/.grim/grim.sh');
            } else {
                echo colorize("❌ Grim installer not found\n", 'red');
            }
            break;
            
        case 'logs':
            $logFile = '/tmp/grim-install.log';
            if (file_exists($logFile)) {
                echo colorize("📋 Grim Installation Log:\n", 'blue');
                passthru("cat $logFile");
            } else {
                echo colorize("❌ Installation log not found\n", 'red');
            }
            break;
            
        default:
            echo colorize("Grim Commands:\n", 'yellow');
            echo "  tsk grim status    Check Grim installation status\n";
            echo "  tsk grim install   Install Grim AI backup\n";
            echo "  tsk grim logs      Show installation logs\n";
            break;
    }
}

main() {
    print_header
    
    check_root
    
    # Check if this is a TSK-only installation
    if [[ "$1" == "install_tsk_only" ]]; then
        install_tsk_only
        exit 0
    fi
    
    # Check PHP version
    if ! detect_php_version; then
        print_warning "PHP 8+ not detected"
        echo -e "${YELLOW}Do you want to install PHP 8+? (y/N): ${NC}"
        read -r install_php
        
        if [[ "$install_php" =~ ^[Yy]$ ]]; then
            install_php_8_plus
        else
            print_error "TuskLang requires PHP 8+ to function properly"
            exit 1
        fi
    fi
    
    # Download and extract tarball
    download_and_extract_tarball
    
    # Install CLI
    install_tsk_cli
    
    # Collect installation data
    collect_installation_data
    
    # Setup permissions
    setup_permissions
    
    # Create default config
    create_default_config
    
    # Final setup
    print_info "Running final setup..."
    
    # Create version file
    echo "1.0.0-lite" > "$TSK_HOME/VERSION"
    
    print_success "Installation complete!"
    
    # Post-installation validation
    print_info "Running post-installation validation..."
    
    # Test TuskLang CLI
    if command -v tsk >/dev/null 2>&1; then
        print_success "TuskLang CLI installed successfully"
    else
        print_error "TuskLang CLI not found in PATH"
    fi
    
    # Test component directories
    components=("$TSK_HOME" "$TSK_CONFIG" "$IVORY_HOME" "$FLEX_HOME" "$GRIM_HOME")
    for component in "${components[@]}"; do
        if [ -d "$component" ]; then
            print_success "Component found: $component"
        else
            print_error "Component missing: $component"
        fi
    done
    
    echo ""
    echo "🐘 TuskLang Lite is now installed and ready!"
    echo ""
    echo "Quick start commands:"
    echo "  tsk help                    # Show CLI help"
    echo "  tsk status                  # Check system status"
    echo "  tsk i ivory                 # Install Ivory framework"
    echo "  tsk i flex                  # Install Flex system"
    echo "  tsk i grim                  # Install Grim tools"
    echo "  tsk i all                   # Install all components"
    echo ""
    echo "Configuration:"
    echo "  tsk config show             # Show configuration"
    echo "  tsk config edit             # Edit configuration"
    echo ""
    echo "Strong. Secure. Scalable. 🐘"
}

# Run main installation
main "$@"