# AI Commands

AI-powered operations and integrations for enhanced development workflows.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk ai claude](./claude.md) | Query Claude AI with prompt |
| [tsk ai chatgpt](./chatgpt.md) | Query ChatGPT with prompt |
| [tsk ai custom](./custom.md) | Query custom AI API endpoint |
| [tsk ai config](./config.md) | Show current AI configuration |
| [tsk ai setup](./setup.md) | Interactive AI API key setup |
| [tsk ai test](./test.md) | Test all configured AI connections |
| [tsk ai complete](./complete.md) | Get AI-powered auto-completion |
| [tsk ai analyze](./analyze.md) | Analyze file for errors and improvements |
| [tsk ai optimize](./optimize.md) | Get performance optimization suggestions |
| [tsk ai security](./security.md) | Scan for security vulnerabilities |

## Common Use Cases

- **Code Completion**: Get intelligent code suggestions
- **Code Analysis**: Identify bugs and improvement opportunities
- **Performance Optimization**: Get suggestions for faster code
- **Security Scanning**: Find potential security vulnerabilities
- **Documentation**: Generate documentation from code
- **Debugging**: Get help with error messages and debugging

## Python-Specific Notes

### API Key Management

AI services require API keys stored securely in `~/.tsk/ai_config.json`:

```python
# Configuration file structure
{
  "claude_api_key": "your-claude-api-key",
  "chatgpt_api_key": "your-chatgpt-api-key"
}
```

### Async Support

All AI commands support async operations for better performance:

```python
import asyncio
from cli.commands.ai_commands import ClaudeService, AIConfig

async def query_claude():
    config = AIConfig()
    service = ClaudeService(config)
    result = await service.query_async("Hello, Claude!")
    return result

# Usage
result = asyncio.run(query_claude())
```

### Type Hints

AI commands provide full type hint support:

```python
from typing import Dict, Any, Optional
from cli.commands.ai_commands import AIService

def analyze_code(file_path: str) -> Dict[str, Any]:
    """Analyze code using AI"""
    service = AIService()
    return service.analyze(file_path)
```

### Error Handling

Robust error handling for API failures:

```python
try:
    result = tsk.ai.claude("Analyze this code")
except Exception as e:
    print(f"AI service error: {e}")
    # Fallback to local analysis
```

## Examples

### Basic AI Usage

```bash
# Query Claude
tsk ai claude "Explain TuskLang configuration"

# Query ChatGPT
tsk ai chatgpt "How do I optimize Python code?"

# Custom AI API
tsk ai custom https://api.example.com/ai "Custom prompt"
```

### Development Workflow

```bash
# Get code completion
tsk ai complete app.py 15 10

# Analyze code for issues
tsk ai analyze app.py

# Get optimization suggestions
tsk ai optimize app.py

# Security scan
tsk ai security app.py
```

### Configuration Management

```bash
# Set up AI services
tsk ai setup

# Check configuration
tsk ai config

# Test connections
tsk ai test
```

## Integration with Python Development

### IDE Integration

```python
# VS Code extension integration
import subprocess

def get_ai_completion(file_path: str, line: int, column: int) -> str:
    result = subprocess.run([
        'tsk', 'ai', 'complete', file_path, str(line), str(column)
    ], capture_output=True, text=True)
    return result.stdout
```

### Jupyter Notebook Integration

```python
# Use in Jupyter notebooks
import subprocess
import json

def ai_analyze_notebook(code_cell: str) -> dict:
    result = subprocess.run([
        'tsk', 'ai', 'analyze', '-', '--json'
    ], input=code_cell, capture_output=True, text=True)
    return json.loads(result.stdout)
```

### Testing Integration

```python
# Use AI for test generation
def generate_tests_with_ai(function_code: str) -> str:
    result = subprocess.run([
        'tsk', 'ai', 'claude', f"Generate unit tests for this Python function: {function_code}"
    ], capture_output=True, text=True)
    return result.stdout
```

## Performance Considerations

### Caching

AI responses are cached to improve performance:

```python
# Cache is automatically managed
# Responses cached for 1 hour by default
# Cache location: ~/.tsk/ai_cache/
```

### Rate Limiting

Respect API rate limits:

```python
# Automatic rate limiting
# Claude: 50 requests/minute
# ChatGPT: 60 requests/minute
# Custom APIs: Configurable
```

### Batch Processing

Process multiple files efficiently:

```python
import asyncio
from pathlib import Path

async def analyze_multiple_files(files: list) -> dict:
    tasks = []
    for file in files:
        task = asyncio.create_task(
            subprocess.run(['tsk', 'ai', 'analyze', file], 
                         capture_output=True, text=True)
        )
        tasks.append(task)
    
    results = await asyncio.gather(*tasks)
    return {file: result.stdout for file, result in zip(files, results)}
```

## Security Best Practices

### API Key Security

```python
# Store keys securely
import os
from pathlib import Path

# Use environment variables
api_key = os.getenv('CLAUDE_API_KEY')

# Or use secure key storage
key_path = Path.home() / '.tsk' / 'ai_config.json'
key_path.chmod(0o600)  # Restrict permissions
```

### Input Validation

```python
# Validate inputs before sending to AI
def safe_ai_query(prompt: str) -> str:
    # Sanitize input
    if len(prompt) > 10000:
        raise ValueError("Prompt too long")
    
    # Check for sensitive data
    sensitive_patterns = ['password', 'api_key', 'secret']
    for pattern in sensitive_patterns:
        if pattern in prompt.lower():
            raise ValueError(f"Sensitive data detected: {pattern}")
    
    return prompt
```

### Output Validation

```python
# Validate AI responses
def validate_ai_response(response: str) -> bool:
    # Check for code injection
    dangerous_patterns = ['eval(', 'exec(', '__import__']
    for pattern in dangerous_patterns:
        if pattern in response:
            return False
    
    return True
```

## Troubleshooting

### Common Issues

1. **API Key Not Configured**
   ```bash
   tsk ai setup
   ```

2. **Rate Limit Exceeded**
   ```bash
   # Wait and retry
   sleep 60
   tsk ai claude "Your prompt"
   ```

3. **Network Issues**
   ```bash
   # Check connectivity
   curl -I https://api.anthropic.com
   ```

### Debug Mode

```bash
# Enable verbose output
tsk --verbose ai claude "Test prompt"

# Check configuration
tsk ai config --json
```

## Related Commands

- [Development Commands](../dev/README.md) - Development tools
- [Testing Commands](../test/README.md) - Test execution
- [Configuration Commands](../config/README.md) - Config management
- [Utility Commands](../utility/README.md) - General utilities 