# TuskLang License Server

## Status ✅

The license server is now **running and accessible** at:
- HTTP: http://lic.tusklang.org (currently working)
- HTTPS: https://lic.tusklang.org (requires SSL setup)

## Current Setup

### ✅ Completed
1. **Node.js Server**: Running on port 3000
2. **Systemd Service**: Auto-starts on boot
3. **Nginx Proxy**: Configured and working
4. **Database**: Connected to PostgreSQL
5. **API Endpoints**: All working

### ⏳ Pending
1. **SSL Certificate**: Run `sudo certbot --nginx -d lic.tusklang.org` and select account 3

## API Endpoints

### Public Endpoints
- `GET /health` - Health check
- `POST /api/v1/validate` - Validate license
- `POST /api/v1/install` - Track installation
- `POST /api/v1/usage` - Log usage analytics

### Admin Endpoints (require API key)
- `POST /api/v1/admin/revoke` - Revoke license

## Testing

```bash
# Test health endpoint
curl http://lic.tusklang.org/health

# Test license validation
curl -X POST http://lic.tusklang.org/api/v1/validate \
  -H "Content-Type: application/json" \
  -d '{
    "license_key": "YOUR-LICENSE-KEY",
    "machine_id": "test-001",
    "platform": "linux",
    "version": "2.0.0"
  }'
```

## Management

```bash
# Check service status
sudo systemctl status tusklang-license

# View logs
sudo journalctl -u tusklang-license -f

# Restart service
sudo systemctl restart tusklang-license

# Stop service
sudo systemctl stop tusklang-license
```

## Security Notes

1. **Change JWT Secret**: Edit `/opt/tsk_git/server/.env` and update `JWT_SECRET`
2. **Database Credentials**: Move to environment variables (already done)
3. **API Keys**: Create admin API keys in the database for admin endpoints
4. **Rate Limiting**: Currently set to 100 requests per 15 minutes per IP

## Database Access

The server connects to the PostgreSQL database with the schema defined in `/opt/tsk_git/sql/license_schema.sql`.

To create a test license:
```sql
INSERT INTO licenses (license_key, customer_name, customer_email, license_type, max_installations)
VALUES ('TEST-1234-5678-9012', 'Test User', 'test@example.com', 'standard', 3);
```

## Monitoring

- Logs: `/opt/tsk_git/server/logs/`
- System logs: `journalctl -u tusklang-license`
- Nginx logs: `/var/log/nginx/lic.tusklang.org.*`