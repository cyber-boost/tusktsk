# 🟨 TuskLang JavaScript Installation Guide

**"We don't bow to any king" - JavaScript Edition**

Welcome to the most powerful configuration language for JavaScript developers. TuskLang brings database queries, executable functions, and cross-file communication directly into your configuration files.

## 🚀 Quick Installation

### Option 1: NPM Installation (Recommended)

```bash
# Install TuskLang JavaScript SDK
npm install tusklang

# Or with yarn
yarn add tusklang

# Verify installation
node -e "console.log(require('tusklang').version)"
```

### Option 2: Direct Installation

```bash
# One-line install
curl -sSL https://js.tusklang.org | node

# Or with wget
wget -qO- https://js.tusklang.org | node
```

### Option 3: Global Installation

```bash
# Install globally for CLI access
npm install -g tusklang

# Verify global installation
tsk --version
```

## 📦 Package Manager Support

### NPM Configuration

Add to your `package.json`:

```json
{
  "dependencies": {
    "tusklang": "^2.0.0"
  },
  "devDependencies": {
    "@types/tusklang": "^2.0.0"
  }
}
```

### Yarn Configuration

```bash
# Add to dependencies
yarn add tusklang

# Add TypeScript types
yarn add -D @types/tusklang
```

### PNPM Support

```bash
# Install with pnpm
pnpm add tusklang

# Add TypeScript types
pnpm add -D @types/tusklang
```

## 🔧 Development Setup

### TypeScript Support

```bash
# Install TypeScript types
npm install -D @types/tusklang

# Or with yarn
yarn add -D @types/tusklang
```

### ESLint Configuration

Add to your `.eslintrc.js`:

```javascript
module.exports = {
  extends: [
    // ... other extends
  ],
  plugins: [
    // ... other plugins
    'tusklang'
  ],
  rules: {
    'tusklang/valid-syntax': 'error',
    'tusklang/no-deprecated-features': 'warn'
  }
};
```

### VS Code Extension

Install the TuskLang extension for VS Code:

```bash
code --install-extension tusklang.tusk-vscode
```

## 🐳 Docker Support

### Dockerfile Example

```dockerfile
FROM node:18-alpine

# Install TuskLang
RUN npm install -g tusklang

# Set working directory
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy application files
COPY . .

# Expose port
EXPOSE 3000

# Start application
CMD ["npm", "start"]
```

### Docker Compose

```yaml
version: '3.8'
services:
  app:
    build: .
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=production
      - TUSK_CONFIG_PATH=/app/config
    volumes:
      - ./config:/app/config
```

## 🔍 Verification

### Basic Verification

```javascript
const TuskLang = require('tusklang');

// Test basic parsing
const config = TuskLang.parse(`
app {
    name: "TestApp"
    version: "1.0.0"
}
`);

console.log('✅ TuskLang is working!');
console.log('App name:', config.app.name);
```

### Advanced Verification

```javascript
const TuskLang = require('tusklang');

// Test @ operators
const config = TuskLang.parse(`
[api]
endpoint: @env("API_ENDPOINT", "https://api.example.com")
timestamp: @date.now()
`);

// Test database integration
const SQLiteAdapter = require('tusklang/adapters/sqlite');
const sqlite = new SQLiteAdapter({ filename: ':memory:' });

const tsk = new TuskLang.Enhanced();
tsk.setDatabaseAdapter(sqlite);

console.log('✅ All TuskLang features working!');
```

## 🛠️ Troubleshooting

### Common Issues

#### 1. Module Not Found Error

```bash
# If you get "Cannot find module 'tusklang'"
npm cache clean --force
npm install tusklang
```

#### 2. TypeScript Errors

```bash
# Install TypeScript types
npm install -D @types/tusklang

# Or update tsconfig.json
{
  "compilerOptions": {
    "types": ["node", "tusklang"]
  }
}
```

#### 3. Permission Errors (Global Installation)

```bash
# Fix npm permissions
sudo chown -R $USER:$GROUP ~/.npm
sudo chown -R $USER:$GROUP ~/.config

# Or use nvm for Node.js management
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash
```

#### 4. Database Adapter Issues

```bash
# Install required database drivers
npm install sqlite3 pg mysql2 mongodb redis

# For PostgreSQL
npm install pg

# For MySQL
npm install mysql2

# For MongoDB
npm install mongodb

# For Redis
npm install redis
```

### Environment Setup

#### Node.js Version Requirements

```bash
# Check Node.js version (requires 16+)
node --version

# If using nvm, install latest LTS
nvm install --lts
nvm use --lts
```

#### Package Manager Verification

```bash
# Verify npm
npm --version

# Verify yarn
yarn --version

# Verify pnpm
pnpm --version
```

## 🔒 Security Considerations

### Environment Variables

```javascript
// Use secure environment variable loading
const config = TuskLang.parse(`
[security]
api_key: @env.secure("API_KEY")
database_password: @env.secure("DB_PASSWORD")
`);

// Set environment variables securely
export API_KEY="your-secure-api-key"
export DB_PASSWORD="your-secure-password"
```

### File Permissions

```bash
# Set proper file permissions for config files
chmod 600 config/peanu.tsk
chmod 600 config/database.tsk

# For production
chown www-data:www-data config/
chmod 750 config/
```

## 📚 Next Steps

After installation, proceed to:

1. **[Quick Start Guide](002-quick-start-javascript.md)** - Get up and running in minutes
2. **[Basic Syntax](003-basic-syntax-javascript.md)** - Learn TuskLang syntax with JavaScript examples
3. **[Database Integration](004-database-integration-javascript.md)** - Connect to your databases
4. **[Advanced Features](005-advanced-features-javascript.md)** - Master @ operators and FUJSEN

## 🎯 Why TuskLang for JavaScript?

- **Database Queries in Config**: `user_count: @query("SELECT COUNT(*) FROM users")`
- **Executable Functions**: JavaScript functions embedded in configuration
- **Cross-File Communication**: Share variables across multiple `.tsk` files
- **TypeScript Support**: Full type safety and IntelliSense
- **5 Database Adapters**: SQLite, PostgreSQL, MySQL, MongoDB, Redis
- **@ Operator System**: Environment variables, caching, metrics, and more

**Ready to revolutionize your JavaScript configuration? Let's get started!** 