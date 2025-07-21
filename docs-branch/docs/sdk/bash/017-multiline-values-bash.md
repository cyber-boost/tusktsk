# 📄 TuskLang Bash Multiline Values Guide

**"We don't bow to any king" – Multiline values, the TuskLang way.**

Multiline values in TuskLang allow you to store complex text content, templates, scripts, and structured data within your configuration files. Whether you're embedding HTML templates, storing shell scripts, or creating complex JSON structures, multiline values provide the flexibility you need.

## 🎯 What are Multiline Values?
Multiline values are text content that spans multiple lines. In TuskLang, they're defined using triple quotes (`"""`) and can contain:
- **Templates and HTML**
- **Shell scripts**
- **JSON/XML content**
- **Configuration blocks**
- **Documentation**
- **Any text content**

## 📝 Syntax Styles

### Basic Multiline String
```ini
[template]
html_template: """
<!DOCTYPE html>
<html>
<head>
    <title>Welcome</title>
</head>
<body>
    <h1>Hello, World!</h1>
</body>
</html>
"""
```

### JSON-like Multiline
```json
{
  "script": """
#!/bin/bash
echo "Starting application..."
./start.sh
echo "Application started"
"""
}
```

### XML-inspired Multiline
```xml
<config>
  <template>
    <html>"""
<!DOCTYPE html>
<html>
<head>
    <title>${title}</title>
</head>
<body>
    <h1>${heading}</h1>
</body>
</html>
    """</html>
  </template>
</config>
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > multiline-quickstart.tsk << 'EOF'
[app]
welcome_message: """
Welcome to TuskLang!

This is a powerful configuration language
that adapts to your preferred syntax.

Features:
- Dynamic configuration
- Database integration
- @ operator system
"""

deployment_script: """
#!/bin/bash
echo "Deploying application..."
git pull origin main
npm install
npm run build
echo "Deployment complete!"
"""
EOF

config=$(tusk_parse multiline-quickstart.tsk)
echo "Welcome Message:"
echo "$(tusk_get "$config" app.welcome_message)"
echo ""
echo "Deployment Script:"
echo "$(tusk_get "$config" app.deployment_script)"
```

## 🔗 Real-World Use Cases

### 1. HTML Templates
```ini
[email]
welcome_template: """
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Welcome to ${app_name}</title>
</head>
<body>
    <h1>Welcome, ${user_name}!</h1>
    <p>Thank you for joining ${app_name}.</p>
    <p>Your account has been created successfully.</p>
    <a href="${login_url}">Login to your account</a>
</body>
</html>
"""
```

### 2. Shell Scripts
```ini
[automation]
backup_script: """
#!/bin/bash
set -e

BACKUP_DIR="/var/backups/$(date +%Y%m%d)"
mkdir -p "$BACKUP_DIR"

# Backup database
pg_dump -h localhost -U postgres myapp > "$BACKUP_DIR/database.sql"

# Backup configuration
cp -r /etc/myapp "$BACKUP_DIR/config"

# Compress backup
tar -czf "$BACKUP_DIR.tar.gz" "$BACKUP_DIR"
rm -rf "$BACKUP_DIR"

echo "Backup completed: $BACKUP_DIR.tar.gz"
"""
```

### 3. Configuration Templates
```ini
[nginx]
server_config: """
server {
    listen 80;
    server_name ${domain};
    
    location / {
        proxy_pass http://localhost:${app_port};
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
    
    location /static {
        alias /var/www/static;
        expires 1y;
    }
}
"""
```

### 4. JSON/XML Content
```ini
[api]
response_template: """
{
    "status": "success",
    "data": {
        "user_id": "${user_id}",
        "username": "${username}",
        "email": "${email}",
        "created_at": "${created_at}"
    },
    "message": "User created successfully"
}
"""
```

## 🧠 Advanced Multiline Patterns

### Dynamic Content Generation
```bash
#!/bin/bash
source tusk-bash.sh

cat > dynamic-multiline.tsk << 'EOF'
[template]
environment: @env("APP_ENV", "development")

config_template: """
# Generated configuration for ${environment}
APP_NAME="${app_name}"
APP_ENV="${environment}"
DEBUG=${debug}
PORT=${port}

# Database configuration
DB_HOST="${db_host}"
DB_PORT="${db_port}"
DB_NAME="${db_name}"
"""
EOF

config=$(tusk_parse dynamic-multiline.tsk)
template=$(tusk_get "$config" template.config_template)

# Replace placeholders
final_config=$(echo "$template" | sed "s/\${environment}/production/g")
echo "$final_config"
```

### Nested Multiline Content
```ini
[complex]
main_template: """
# Main configuration
${database_config}

# Application settings
${app_config}

# Security settings
${security_config}
"""

database_config: """
# Database configuration
DB_HOST=localhost
DB_PORT=5432
DB_NAME=myapp
"""

app_config: """
# Application configuration
APP_NAME=MyApp
APP_PORT=8080
DEBUG=true
"""
```

### Multiline with Interpolation
```ini
[email]
notification_template: """
Dear ${user_name},

Your account has been ${action} successfully.

Account Details:
- Username: ${username}
- Email: ${email}
- Status: ${status}

If you have any questions, please contact support.

Best regards,
The ${app_name} Team
"""
```

## 🛡️ Security & Performance Notes
- **Content validation:** Validate multiline content, especially when using @shell or external data.
- **Size limits:** Large multiline values can impact parsing performance.
- **Escaping:** Be careful with special characters and escaping in multiline content.
- **Sensitive data:** Never store secrets directly in multiline values; use @env or @encrypt.

## 🐞 Troubleshooting
- **Parsing errors:** Check for balanced triple quotes and proper escaping.
- **Interpolation issues:** Ensure variable names are correct and values are available.
- **Performance:** Large multiline values can slow parsing; consider external files for very large content.
- **Encoding:** Ensure proper character encoding for international content.

## 💡 Best Practices
- **Use descriptive names:** Name multiline values clearly to indicate their purpose.
- **Keep it readable:** Format multiline content for readability with proper indentation.
- **Document structure:** Document the expected structure and variables in multiline content.
- **External files:** For very large content, consider using external files with @file.read().
- **Validation:** Validate multiline content structure and required variables.

## 🔗 Cross-References
- [Strings](008-strings-bash.md)
- [File Operations](056-file-function-bash.md)
- [Templates](035-at-render-function-bash.md)

---

**Master multiline values in TuskLang and store complex content with ease. 📄** 