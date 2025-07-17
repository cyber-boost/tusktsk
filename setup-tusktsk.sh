#!/bin/bash

# TuskT.sk Domain Setup Script
# ============================

set -e

echo "🚀 Setting up TuskT.sk domain with SSL certificates..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   print_error "This script must be run as root"
   exit 1
fi

# Step 1: Create temporary Nginx configuration without SSL
print_status "Creating temporary Nginx configuration for SSL certificate generation..."
cat > /etc/nginx/sites-available/tusktsk-temp << 'EOF'
# Temporary configuration for SSL certificate generation
server {
    listen 80;
    server_name tuskt.sk www.tuskt.sk lic.tuskt.sk api.tuskt.sk;

    # ACME challenge for Let's Encrypt
    location ^~ /.well-known/acme-challenge/ {
        alias /var/www/html/.well-known/acme-challenge/;
        try_files $uri =404;
    }

    # Redirect all other traffic to HTTPS (will work after SSL is set up)
    location / {
        return 301 https://$server_name$request_uri;
    }
}
EOF

# Step 2: Enable temporary site
print_status "Enabling temporary Nginx site..."
ln -sf /etc/nginx/sites-available/tusktsk-temp /etc/nginx/sites-enabled/

# Step 3: Create necessary directories for SSL
print_status "Creating SSL directories..."
mkdir -p /var/www/html/.well-known/acme-challenge

# Step 4: Test and reload Nginx with temporary config
print_status "Testing Nginx configuration..."
if nginx -t; then
    print_success "Nginx configuration is valid"
    systemctl reload nginx
else
    print_error "Nginx configuration test failed"
    exit 1
fi

# Step 5: Set up SSL certificates with Certbot
print_status "Setting up SSL certificates for tuskt.sk..."

# Check if certbot is installed
if ! command -v certbot &> /dev/null; then
    print_warning "Certbot not found. Installing..."
    apt update
    apt install -y certbot python3-certbot-nginx
fi

# Check if certificates already exist
if [[ -f "/etc/letsencrypt/live/tuskt.sk/fullchain.pem" ]]; then
    print_warning "SSL certificates for tuskt.sk already exist"
    print_status "Renewing certificates..."
    certbot renew --quiet
else
    print_status "Obtaining SSL certificates for tuskt.sk..."
    certbot certonly --webroot \
        --webroot-path=/var/www/html \
        --email admin@tusklang.org \
        --agree-tos \
        --no-eff-email \
        --domains tuskt.sk,www.tuskt.sk,lic.tuskt.sk,api.tuskt.sk
fi

# Step 6: Replace with full configuration
print_status "Installing full Nginx configuration..."
cp nginx/tusktsk.conf /etc/nginx/sites-available/tusktsk.sk
rm -f /etc/nginx/sites-enabled/tusktsk-temp
ln -sf /etc/nginx/sites-available/tusktsk.sk /etc/nginx/sites-enabled/

# Step 7: Test and reload Nginx with full config
print_status "Testing full Nginx configuration..."
if nginx -t; then
    print_success "Full Nginx configuration is valid"
    systemctl reload nginx
else
    print_error "Full Nginx configuration test failed"
    exit 1
fi

# Step 8: Set up automatic renewal
print_status "Setting up automatic SSL renewal..."
if ! crontab -l 2>/dev/null | grep -q "certbot renew"; then
    (crontab -l 2>/dev/null; echo "0 12 * * * /usr/bin/certbot renew --quiet") | crontab -
    print_success "Added SSL renewal to crontab"
fi

# Step 9: Verify SSL certificates
print_status "Verifying SSL certificates..."
if certbot certificates | grep -q "tuskt.sk"; then
    print_success "SSL certificates verified successfully"
else
    print_error "SSL certificate verification failed"
    exit 1
fi

# Step 10: Test domain accessibility
print_status "Testing domain accessibility..."
if curl -s -o /dev/null -w "%{http_code}" https://tuskt.sk | grep -q "200\|301\|302"; then
    print_success "Domain tuskt.sk is accessible"
else
    print_warning "Domain accessibility test failed - this might be normal if DNS is not yet configured"
fi

# Step 11: Display final status
echo ""
print_success "TuskT.sk domain setup completed!"
echo ""
echo "📋 Summary:"
echo "  • Nginx configuration: /etc/nginx/sites-available/tusktsk.sk"
echo "  • SSL certificates: /etc/letsencrypt/live/tuskt.sk/"
echo "  • Log files: /var/log/nginx/tuskt.sk-*.log"
echo ""
echo "🌐 Domains configured:"
echo "  • https://tuskt.sk (main site)"
echo "  • https://www.tuskt.sk (main site)"
echo "  • https://lic.tuskt.sk (license server)"
echo "  • https://api.tuskt.sk (API endpoint)"
echo ""
echo "🔧 Next steps:"
echo "  1. Configure DNS records to point tuskt.sk to this server"
echo "  2. Test all endpoints after DNS propagation"
echo "  3. Monitor logs: tail -f /var/log/nginx/tuskt.sk-error.log"
echo ""
print_success "Setup complete! 🎉" 