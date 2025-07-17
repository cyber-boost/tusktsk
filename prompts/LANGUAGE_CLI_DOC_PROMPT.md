# TuskLang CLI Documentation Task - [LANGUAGE_NAME]

## Task Overview

Create comprehensive CLI usage documentation for the **[LANGUAGE_NAME]** TuskLang SDK. This documentation should be specific to [LANGUAGE_NAME] while following the universal template structure.

## Your Mission

Transform the generic CLI_USAGE_TEMPLATE.md into a language-specific guide by:
1. Replacing all placeholders with [LANGUAGE_NAME]-specific content
2. Adding language-specific installation instructions
3. Including idiomatic code examples
4. Highlighting language-specific features or limitations

## File to Create

`/opt/tsk_git/sdk-pnt-test/[LANGUAGE_DIR]/docs/CLI_USAGE.md`

Where [LANGUAGE_DIR] is one of: js, go, java, python, ruby, rust, csharp, php, bash

## Required Replacements

### 1. Language Placeholders
- Replace `[LANGUAGE]` with the full language name (e.g., "JavaScript", "Python")
- Replace `[language]` with lowercase version (e.g., "javascript", "python")

### 2. Installation Section
Update the installation commands with language-specific details:

**For Python:**
```bash
# Using pip
pip install tusklang

# From source
git clone https://github.com/tusklang/python-sdk
cd python-sdk
pip install -e .
```

**For JavaScript:**
```bash
# Using npm
npm install -g tusklang

# Using yarn
yarn global add tusklang
```

**For Go:**
```bash
# Using go install
go install github.com/tusklang/go-sdk/cmd/tsk@latest

# From source
git clone https://github.com/tusklang/go-sdk
cd go-sdk
go build -o tsk cmd/tsk/main.go
```

[Continue pattern for other languages...]

### 3. Language-Specific Examples

Add examples showing how to use the SDK programmatically:

**For Python:**
```python
from tusklang import TuskLang, PeanutConfig

# Parse TSK file
parser = TuskLang()
config = parser.parse_file('config.tsk')

# Use PeanutConfig
peanut = PeanutConfig()
db_host = peanut.get('database.host', default='localhost')
```

**For JavaScript:**
```javascript
const { TuskLang, PeanutConfig } = require('tusklang');

// Parse TSK file
const parser = new TuskLang();
const config = await parser.parseFile('config.tsk');

// Use PeanutConfig
const peanut = new PeanutConfig();
const dbHost = peanut.get('database.host', 'localhost');
```

[Continue pattern for other languages...]

### 4. Platform-Specific Notes

Include any platform or language-specific considerations:

**For Windows Users (C#, PowerShell):**
- Note about path separators
- PowerShell execution policy
- .NET Framework requirements

**For Ruby:**
- RVM/rbenv considerations
- Bundler integration
- Gem packaging notes

**For Java:**
- JDK version requirements
- Maven/Gradle integration
- Classpath setup

### 5. Environment Setup

Include language-specific environment setup:

**For Python:**
```bash
# Virtual environment recommended
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate

# Set Python path
export PYTHONPATH="$PYTHONPATH:/path/to/tusklang"
```

**For Node.js:**
```bash
# Check Node version (requires 14+)
node --version

# Set NODE_PATH if needed
export NODE_PATH="$NODE_PATH:/usr/local/lib/node_modules"
```

### 6. Troubleshooting Section

Add language-specific troubleshooting:

**For Go:**
- GOPATH issues
- Module versioning problems
- Binary distribution

**For Rust:**
- Cargo installation
- Linking issues
- Cross-compilation

### 7. Integration Examples

Show how to integrate with popular frameworks:

**For Python:**
- Django settings integration
- Flask configuration
- FastAPI usage

**For JavaScript:**
- Express.js middleware
- Next.js configuration
- React environment setup

## Quality Checklist

Your documentation is complete when:
- [ ] All placeholders are replaced
- [ ] Installation works on major platforms
- [ ] Code examples are tested and working
- [ ] Language idioms are followed
- [ ] Common errors are addressed
- [ ] Framework integrations are shown
- [ ] File location is correct

## Important Notes

1. **Test all commands** before documenting them
2. **Use language conventions** (e.g., camelCase for JS, snake_case for Python)
3. **Include version requirements** (minimum language version, dependencies)
4. **Add performance tips** specific to the language
5. **Reference language-specific tools** (pip, npm, cargo, etc.)

## Example Structure

Your final document should follow this structure:
1. Title and Overview
2. Installation (language-specific)
3. Basic Usage
4. All CLI Commands (with examples)
5. Programmatic Usage (language-specific)
6. Configuration Files
7. Common Workflows
8. Troubleshooting (language-specific)
9. Integration Examples
10. Additional Resources

## File Naming

Create the file at:
- JavaScript: `/opt/tsk_git/sdk-pnt-test/js/docs/CLI_USAGE.md`
- Python: `/opt/tsk_git/sdk-pnt-test/python/docs/CLI_USAGE.md`
- Go: `/opt/tsk_git/sdk-pnt-test/go/docs/CLI_USAGE.md`
- Ruby: `/opt/tsk_git/sdk-pnt-test/ruby/docs/CLI_USAGE.md`
- Java: `/opt/tsk_git/sdk-pnt-test/java/docs/CLI_USAGE.md`
- Rust: `/opt/tsk_git/sdk-pnt-test/rust/docs/CLI_USAGE.md`
- C#: `/opt/tsk_git/sdk-pnt-test/csharp/docs/CLI_USAGE.md`
- PHP: `/opt/tsk_git/sdk-pnt-test/php/docs/CLI_USAGE.md`
- Bash: `/opt/tsk_git/sdk-pnt-test/bash/docs/CLI_USAGE.md`

Remember: This documentation will be the primary reference for developers using [LANGUAGE_NAME] with TuskLang!