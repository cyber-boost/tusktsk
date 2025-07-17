#!/bin/bash
# TuskLang License Server Setup Script

echo "🐘 Setting up TuskLang License Server..."

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
  echo "Please run as root (use sudo)"
  exit 1
fi

# 1. Copy systemd service file
echo "📋 Installing systemd service..."
cp /opt/tsk_git/server/tusklang-license.service /etc/systemd/system/
systemctl daemon-reload

# 2. Start and enable the service
echo "🚀 Starting license server..."
systemctl start tusklang-license
systemctl enable tusklang-license

# 3. Check service status
echo "📊 Checking service status..."
systemctl status tusklang-license --no-pager

# 4. Copy nginx configuration
echo "🌐 Setting up Nginx..."
cp /opt/tsk_git/nginx/lic.tusklang.org.conf /etc/nginx/sites-available/
ln -sf /etc/nginx/sites-available/lic.tusklang.org.conf /etc/nginx/sites-enabled/

# 5. Test nginx configuration
echo "🔍 Testing Nginx configuration..."
nginx -t

# 6. Reload nginx
echo "🔄 Reloading Nginx..."
systemctl reload nginx

# 7. Set up SSL with Let's Encrypt
echo "🔒 Setting up SSL certificate..."
certbot --nginx -d lic.tusklang.org --non-interactive --agree-tos --email zoo@phptu.sk --redirect

# 8. Create log directory with proper permissions
echo "📁 Setting up log directory..."
mkdir -p /opt/tsk_git/server/logs
chown www-data:www-data /opt/tsk_git/server/logs
chmod 755 /opt/tsk_git/server/logs

# 9. Set proper permissions
echo "🔐 Setting permissions..."
chown -R www-data:www-data /opt/tsk_git/server
chmod 600 /opt/tsk_git/server/.env

echo "✅ Setup complete!"
echo ""
echo "📌 Important next steps:"
echo "1. Update DNS records to point lic.tusklang.org to this server"
echo "2. Verify the service is running: systemctl status tusklang-license"
echo "3. Check logs: journalctl -u tusklang-license -f"
echo "4. Test the API: curl https://lic.tusklang.org/health"
echo ""
echo "🔑 Don't forget to:"
echo "- Change JWT_SECRET in .env file"
echo "- Create admin API keys in the database"
echo "- Set up monitoring and alerts"