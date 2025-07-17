#!/bin/bash

# Setup script for Cyberboost/Tusktsk organization
# Copyright (c) 2024 Cyberboost LLC

echo "=== Cyberboost/Tusktsk Setup Script ==="
echo

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Step 1: Create GitHub Organization
echo -e "${YELLOW}Step 1: GitHub Organization Setup${NC}"
echo "1. Go to: https://github.com/organizations/new"
echo "2. Organization name: cyberboost"
echo "3. Contact email: admin@tuskt.sk"
echo "4. Organization type: Business"
echo "5. Create these repositories:"
echo "   - cyberboost/tusktsk (public)"
echo "   - cyberboost/tusktsk-dev (private)"
echo "   - cyberboost/tusktsk-enterprise (private)"
echo
read -p "Press enter when GitHub organization is created..."

# Step 2: Package Registry Accounts
echo -e "${YELLOW}Step 2: Package Registry Accounts${NC}"
echo "Create accounts with these details:"
echo
echo "NPM (https://www.npmjs.com/signup):"
echo "  - Username: cyberboost"
echo "  - Organization: @cyberboost"
echo "  - Email: packages@tuskt.sk"
echo
echo "PyPI (https://pypi.org/account/register/):"
echo "  - Username: cyberboost"
echo "  - Email: packages@tuskt.sk"
echo
echo "Continue with other registries..."
read -p "Press enter when accounts are created..."

# Step 3: Configure domains
echo -e "${YELLOW}Step 3: Domain Configuration${NC}"
echo "Add these DNS records for tuskt.sk:"
echo "  A     @       → Your server IP"
echo "  A     lic     → Your server IP"
echo "  A     www     → Your server IP"
echo "  CNAME api     → @"
echo "  CNAME docs    → @"
echo
read -p "Press enter when DNS is configured..."

# Step 4: SSL Certificate for tuskt.sk
echo -e "${YELLOW}Step 4: SSL Certificate Setup${NC}"
echo "Running certbot for tuskt.sk domains..."
sudo certbot --nginx -d tuskt.sk -d www.tuskt.sk -d lic.tuskt.sk -d api.tuskt.sk

# Step 5: Update nginx configuration
echo -e "${YELLOW}Step 5: Nginx Configuration${NC}"
cat > /tmp/tusktsk-nginx.conf << 'EOF'
# Tuskt.sk configuration
server {
    listen 80;
    server_name tuskt.sk www.tuskt.sk;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl;
    server_name tuskt.sk www.tuskt.sk;
    
    ssl_certificate /etc/letsencrypt/live/tuskt.sk/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/tuskt.sk/privkey.pem;
    
    root /var/www/tusklang;
    index index.html index.htm;
    
    location / {
        try_files $uri $uri/ =404;
    }
}

# License server
server {
    listen 443 ssl;
    server_name lic.tuskt.sk;
    
    ssl_certificate /etc/letsencrypt/live/tuskt.sk/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/tuskt.sk/privkey.pem;
    
    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF

echo "Nginx config created at /tmp/tusktsk-nginx.conf"
echo "Copy to: sudo cp /tmp/tusktsk-nginx.conf /etc/nginx/sites-available/tusktsk"
echo "Enable: sudo ln -s /etc/nginx/sites-available/tusktsk /etc/nginx/sites-enabled/"
echo "Test: sudo nginx -t"
echo "Reload: sudo systemctl reload nginx"
echo

# Step 6: Update branding
echo -e "${YELLOW}Step 6: Update Branding${NC}"
echo "Update all SDK package.json, setup.py, Cargo.toml, etc. with:"
echo "  - Name: tusktsk"
echo "  - Author: Cyberboost LLC"
echo "  - Homepage: https://tuskt.sk"
echo "  - Repository: https://github.com/cyberboost/tusktsk"
echo

echo -e "${GREEN}=== Setup Complete ===${NC}"
echo "Next steps:"
echo "1. Create enterprise/ folder and move enterprise directories"
echo "2. Initialize cyberboost/tusktsk-dev repository"
echo "3. Configure gitTrick.sh for automated transfers"
echo "4. Test license server at https://lic.tuskt.sk"