# Troubleshooting

Common issues and solutions for TuskLang Ruby SDK.

## Common Issues

### Command Not Found
```bash
# Install the gem
gem install tusk_lang

# Verify installation
tsk --version
```

### Database Connection Failed
```bash
# Initialize database
tsk db init

# Check status
tsk db status
```

### Configuration Not Found
```bash
# Create configuration
echo '[app]' > peanu.peanuts
echo 'name: "My App"' >> peanu.peanuts

# Check configuration
tsk config check
```

### Permission Denied
```bash
# Fix permissions
sudo gem install tusk_lang
```

## Getting Help

```bash
# Show help
tsk --help

# Command-specific help
tsk help [command]

# Version information
tsk --version
```
