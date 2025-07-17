# Installation Guide

Complete installation guide for TuskLang Ruby SDK and CLI.

## Prerequisites

- **Ruby**: Version 2.7 or higher
- **RubyGems**: Latest version
- **Git**: For version control (optional)

### Check Your Ruby Version

```bash
ruby --version
```

**Expected output:**
```
ruby 3.2.2 (2023-03-30 revision e51014f9c0) [x86_64-linux]
```

If you need to install or update Ruby, see [Ruby Installation](#ruby-installation) below.

## Installation Methods

### Method 1: RubyGems (Recommended)

```bash
# Install the gem
gem install tusk_lang

# Verify installation
tsk --version
```

**Expected output:**
```
TuskLang Ruby SDK v2.0.0
```

### Method 2: Bundler (For Projects)

Add to your `Gemfile`:

```ruby
gem 'tusk_lang', '~> 2.0'
```

Then install:

```bash
bundle install
```

### Method 3: From Source

```bash
# Clone the repository
git clone https://github.com/tusklang/tusk-lang-ruby.git
cd tusk-lang-ruby

# Install dependencies
bundle install

# Build and install the gem
gem build tusk_lang.gemspec
gem install tusk_lang-2.0.0.gem
```

## Ruby Installation

### Linux (Ubuntu/Debian)

```bash
# Install Ruby using rbenv
sudo apt update
sudo apt install git curl libssl-dev libreadline-dev zlib1g-dev

# Install rbenv
curl -fsSL https://github.com/rbenv/rbenv-installer/raw/master/bin/rbenv-installer | bash
echo 'export PATH="$HOME/.rbenv/bin:$PATH"' >> ~/.bashrc
echo 'eval "$(rbenv init -)"' >> ~/.bashrc
source ~/.bashrc

# Install Ruby
rbenv install 3.2.2
rbenv global 3.2.2
```

### macOS

```bash
# Using Homebrew
brew install ruby

# Or using rbenv
brew install rbenv ruby-build
rbenv install 3.2.2
rbenv global 3.2.2
```

### Windows

1. Download Ruby from [rubyinstaller.org](https://rubyinstaller.org/)
2. Run the installer
3. Add Ruby to your PATH

## Dependencies

The TuskLang Ruby SDK includes these dependencies:

### Required Dependencies

- **sqlite3**: SQLite database support
- **json**: JSON parsing and generation
- **net/http**: HTTP client for AI integrations
- **fileutils**: File operations
- **optparse**: Command-line argument parsing

### Optional Dependencies

- **webrick**: Web server for development (Ruby 3.0+)
- **filewatcher**: File watching for development
- **rspec**: Testing framework

## Verification

After installation, verify everything is working:

```bash
# Check CLI installation
tsk --version

# Check database support
tsk db status

# Check configuration support
tsk config get --help

# Check AI support
tsk ai test
```

## Configuration

### Initial Setup

```bash
# Create initial configuration
mkdir -p config
cat > config/peanu.peanuts << EOF
[app]
name: "My TuskLang App"
version: "1.0.0"

[cli]
default_commands: ["help", "version"]
EOF
```

### Environment Variables

Set up environment variables for AI integrations:

```bash
# Add to your ~/.bashrc or ~/.zshrc
export CLAUDE_API_KEY="your-claude-api-key"
export OPENAI_API_KEY="your-openai-api-key"
```

## Troubleshooting

### Common Issues

#### Permission Denied
```bash
# Fix gem installation permissions
sudo gem install tusk_lang
```

#### Missing Dependencies
```bash
# Install missing dependencies
sudo apt install sqlite3 libsqlite3-dev  # Ubuntu/Debian
brew install sqlite3                     # macOS
```

#### Ruby Version Issues
```bash
# Check Ruby version
ruby --version

# Update Ruby if needed
rbenv install 3.2.2
rbenv global 3.2.2
```

#### Gem Installation Fails
```bash
# Update RubyGems
gem update --system

# Clear gem cache
gem cleanup
```

### Getting Help

```bash
# Show help
tsk --help

# Show version
tsk --version

# Check installation
tsk doctor
```

## Next Steps

After successful installation:

1. [Quick Start Guide](./quickstart.md) - Get started with basic commands
2. [Command Reference](./commands/README.md) - Explore all available commands
3. [Examples](./examples/README.md) - See real-world usage examples
4. [PNT Guide](../ruby/docs/PNT_GUIDE.md) - Learn about configuration system

## Uninstallation

To remove TuskLang Ruby SDK:

```bash
# Remove the gem
gem uninstall tusk_lang

# Remove configuration files (optional)
rm -rf ~/.tusk_lang
rm -f tusklang.db
``` 