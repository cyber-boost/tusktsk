# 🐍 Installing TuskLang for Python

**"We don't bow to any king" - Python Edition**

TuskLang brings revolutionary configuration capabilities to Python applications. This guide covers everything you need to get TuskLang running in your Python environment.

## 🚀 Quick Installation

### One-Line Install (Recommended)

```bash
# Direct install from official source
curl -sSL https://python.tusklang.org | python3 -

# Alternative with wget
wget -qO- https://python.tusklang.org | python3 -

# Verify installation
python3 -c "import tsk; print(f'TuskLang {tsk.__version__} installed successfully!')"
```

### PyPI Installation

```bash
# Install from PyPI
pip install tusklang

# Install with specific version
pip install tusklang==1.0.0

# Install with all optional dependencies
pip install tusklang[all]

# Verify installation
python -c "import tsk; print(tsk.__version__)"
```

### Source Installation

```bash
# Clone the repository
git clone https://github.com/cyber-boost/python.git
cd python

# Install in development mode
pip install -e .

# Run tests to verify
python -m pytest tests/
```

## 🔧 System Requirements

### Python Versions
- **Python 3.8+** (Recommended: Python 3.11+)
- **pip** package manager
- **setuptools** for development installations

### Optional Dependencies
- **PostgreSQL**: `pip install psycopg2-binary`
- **MongoDB**: `pip install pymongo`
- **Redis**: `pip install redis`
- **Async support**: `pip install asyncio`

### Operating System Support
- **Linux**: Ubuntu 18.04+, CentOS 7+, RHEL 7+
- **macOS**: 10.14+ (Mojave and later)
- **Windows**: Windows 10+ with Python 3.8+

## 📦 Installation Methods

### 1. Virtual Environment (Recommended)

```bash
# Create virtual environment
python3 -m venv tusk-env

# Activate environment
# On Linux/macOS:
source tusk-env/bin/activate
# On Windows:
tusk-env\Scripts\activate

# Install TuskLang
pip install tusklang

# Verify installation
python -c "import tsk; print('TuskLang ready!')"
```

### 2. Conda Environment

```bash
# Create conda environment
conda create -n tusk-env python=3.11

# Activate environment
conda activate tusk-env

# Install TuskLang
pip install tusklang

# Verify installation
python -c "import tsk; print('TuskLang ready!')"
```

### 3. Docker Installation

```dockerfile
# Dockerfile
FROM python:3.11-slim

WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    gcc \
    && rm -rf /var/lib/apt/lists/*

# Install TuskLang
RUN pip install tusklang

# Copy your application
COPY . .

# Run your app
CMD ["python", "app.py"]
```

```bash
# Build and run
docker build -t tusk-app .
docker run -it tusk-app
```

### 4. Poetry Installation

```bash
# Initialize poetry project
poetry init

# Add TuskLang dependency
poetry add tusklang

# Install dependencies
poetry install

# Run in poetry environment
poetry run python -c "import tsk; print('TuskLang ready!')"
```

## 🔍 Verification Steps

### Basic Verification

```python
# Test basic import
import tsk
print(f"TuskLang version: {tsk.__version__}")

# Test TSK object creation
from tsk import TSK
config = TSK.from_string("""
$app_name: "TestApp"
$version: "1.0.0"
""")
print("TSK object created successfully!")
```

### Advanced Verification

```python
# Test database adapter
from tsk.adapters import SQLiteAdapter
db = SQLiteAdapter(':memory:')
result = db.query("SELECT 1 as test")
print(f"Database test: {result[0]['test']}")

# Test FUJSEN execution
config = TSK.from_string("""
[test]
hello_fujsen = '''
def hello(name):
    return f"Hello, {name}!"
'''
""")

result = config.execute_fujsen('test', 'hello', 'TuskLang')
print(f"FUJSEN test: {result}")
```

### CLI Verification

```bash
# Test CLI installation
tsk --version

# Test basic parsing
echo 'app_name: "TestApp"' > test.tsk
tsk parse test.tsk

# Test validation
tsk validate test.tsk
```

## 🛠️ Troubleshooting

### Common Installation Issues

#### 1. Import Error: No module named 'tsk'

```bash
# Check if TuskLang is installed
pip list | grep tusklang

# Reinstall if needed
pip uninstall tusklang
pip install tusklang

# Check Python path
python -c "import sys; print(sys.path)"
```

#### 2. Permission Errors

```bash
# Install with user flag
pip install --user tusklang

# Or use virtual environment
python3 -m venv tusk-env
source tusk-env/bin/activate
pip install tusklang
```

#### 3. Compilation Errors

```bash
# Install build dependencies
# On Ubuntu/Debian:
sudo apt-get install python3-dev build-essential

# On CentOS/RHEL:
sudo yum install python3-devel gcc

# On macOS:
xcode-select --install

# Reinstall
pip install --force-reinstall tusklang
```

#### 4. Database Adapter Issues

```bash
# Install database drivers
pip install psycopg2-binary  # PostgreSQL
pip install pymongo          # MongoDB
pip install redis            # Redis

# Test database connection
python -c "
from tsk.adapters import SQLiteAdapter
db = SQLiteAdapter(':memory:')
print('Database adapter working!')
"
```

### Version Compatibility

```python
# Check version compatibility
import sys
import tsk

print(f"Python version: {sys.version}")
print(f"TuskLang version: {tsk.__version__}")

# Minimum requirements
if sys.version_info < (3, 8):
    print("Warning: Python 3.8+ recommended")
```

### Environment Issues

```bash
# Check environment variables
echo $PYTHONPATH
echo $PATH

# Reset environment
unset PYTHONPATH
export PATH="/usr/local/bin:/usr/bin:/bin:$PATH"

# Test in clean environment
python3 -c "import tsk; print('Clean environment test passed')"
```

## 🔧 Development Setup

### Setting Up Development Environment

```bash
# Clone repository
git clone https://github.com/cyber-boost/python.git
cd python

# Create virtual environment
python3 -m venv venv
source venv/bin/activate

# Install development dependencies
pip install -e ".[dev]"

# Install pre-commit hooks
pre-commit install

# Run tests
pytest tests/

# Run linting
flake8 src/
black src/
```

### IDE Configuration

#### VS Code
```json
{
    "python.defaultInterpreterPath": "./venv/bin/python",
    "python.linting.enabled": true,
    "python.linting.flake8Enabled": true,
    "python.formatting.provider": "black"
}
```

#### PyCharm
1. Go to Settings → Project → Python Interpreter
2. Add new interpreter → Virtual Environment
3. Select existing environment → Choose your venv
4. Apply and OK

## 📊 Performance Testing

### Installation Performance

```bash
# Time installation
time pip install tusklang

# Check package size
pip show tusklang

# Benchmark import time
python -c "
import time
start = time.time()
import tsk
end = time.time()
print(f'Import time: {(end-start)*1000:.2f}ms')
"
```

### Basic Performance Test

```python
import time
from tsk import TSK

# Test parsing performance
config_content = """
$app_name: "PerformanceTest"
$version: "1.0.0"

[database]
host: "localhost"
port: 5432
name: "testdb"

[api]
endpoint: "https://api.example.com"
timeout: 30
retries: 3
"""

start = time.time()
config = TSK.from_string(config_content)
parse_time = (time.time() - start) * 1000

print(f"Parsing time: {parse_time:.2f}ms")
```

## 🚀 Next Steps

After successful installation:

1. **Create your first TSK file** - See [002-quick-start-python.md](002-quick-start-python.md)
2. **Learn basic syntax** - See [003-basic-syntax-python.md](003-basic-syntax-python.md)
3. **Explore FUJSEN capabilities** - See [004-fujsen-python.md](004-fujsen-python.md)
4. **Integrate with databases** - See [005-database-integration-python.md](005-database-integration-python.md)

## 📚 Additional Resources

- **Official Documentation**: [docs.tusklang.org/python](https://docs.tusklang.org/python)
- **GitHub Repository**: [github.com/tusklang/python](https://github.com/cyber-boost/python)
- **PyPI Package**: [pypi.org/project/tusklang](https://pypi.org/project/tusklang)
- **Community Support**: [community.tusklang.org](https://community.tusklang.org)

---

**"We don't bow to any king"** - TuskLang gives you the power to write configuration with a heartbeat. Install it, verify it, and start building revolutionary applications! 