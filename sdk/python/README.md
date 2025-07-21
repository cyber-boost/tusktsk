# Flask-TSK 🚀

**Revolutionary Flask Extension for TuskLang Integration**

[![PyPI version](https://badge.fury.io/py/flask-tsk.svg)](https://badge.fury.io/py/flask-tsk)
[![Python 3.8+](https://img.shields.io/badge/python-3.8+-blue.svg)](https://www.python.org/downloads/)
[![Flask](https://img.shields.io/badge/Flask-2.0+-green.svg)](https://flask.palletsprojects.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![TuskLang](https://img.shields.io/badge/TuskLang-2.0+-purple.svg)](https://github.com/cyber-boost/tusktsk)

<div align="center">
  <img src="svg/tusk-logo.svg" alt="TuskLang" width="200"/>
  <img src="svg/python-badge.svg" alt="Python TSK" width="100"/>
  <img src="svg/tsk-logo.svg" alt="TSK" width="150"/>
</div>

## ⚡ Performance Revolution

Flask-TSK enhances Flask with our **Turbo Template Engine** and full TuskLang SDK integration:

* **Up to 23x faster template rendering** for simple templates
* **Up to 59x faster template rendering** for complex templates  
* **30,000+ renders per second** vs 1,400 with standard Flask
* **Intelligent caching** and optimization for production workloads
* **Full TuskLang SDK integration** with configuration, functions, and operators

<div align="center">
  <img src="svg/heartbeat.svg" alt="Configuration with Heartbeat" width="600"/>
</div>

## 🚀 Quick Start

### Installation

```bash
# Install Flask-TSK
pip install flask-tsk

# Or with performance optimizations
pip install flask-tsk[performance]

# Or install TuskLang Python SDK separately
pip install tusktsk>=2.0.3
```

**📦 Dedicated Python Installation**: [https://py.tuskt.sk/](https://py.tuskt.sk/)

### Basic Usage

```python
from flask import Flask, render_template, jsonify
from tsk_flask import FlaskTSK

app = Flask(__name__)
tsk = FlaskTSK(app)

@app.route('/')
def index():
    # Get configuration from TuskLang
    app_name = tsk.get_config('app', 'name', 'Flask-TSK')
    theme = tsk.get_config('ui', 'theme', 'light')
    
    return render_template('index.html', 
                         app_name=app_name, 
                         theme=theme)

@app.route('/api/config')
def get_config():
    # Return all configuration as JSON
    return jsonify(tsk.get_all_sections())

@app.route('/execute/<function_name>')
def execute_function(function_name):
    # Execute TuskLang functions dynamically
    if function_name == 'format_date':
        result = tsk.execute_function('@date.format', {'date': '2024-01-01', 'format': 'YYYY-MM-DD'})
    elif function_name == 'math_add':
        result = tsk.execute_function('@math.add', {'a': 5, 'b': 3})
    else:
        result = tsk.execute_function(function_name, {})
    
    return jsonify({'function': function_name, 'result': result})

if __name__ == '__main__':
    app.run(debug=True)
```

### Template Integration

```html
<!-- templates/index.html -->
<!DOCTYPE html>
<html>
<head>
    <title>{{ app_name }}</title>
    <style>
        body { background: {{ tsk_config('ui.theme', 'light') }}; }
    </style>
</head>
<body>
    <h1>{{ tsk_config('app.name', 'Flask-TSK') }}</h1>
    <p>{{ tsk_section('welcome.message', 'Welcome to Flask-TSK!') }}</p>
    
    <!-- Dynamic configuration display -->
    <div class="config-panel">
        <h3>Current Configuration</h3>
        <p>Database: {{ tsk_config('database.type', 'sqlite') }}</p>
        <p>Environment: {{ tsk_config('app.environment', 'development') }}</p>
        <p>Version: {{ tsk_config('app.version', '1.0.0') }}</p>
    </div>
    
    <!-- TuskLang function execution -->
    <div class="functions">
        <p>Current Time: {{ tsk_function('@date.now') }}</p>
        <p>Formatted Date: {{ tsk_function('@date.format', {'date': '2024-01-01', 'format': 'MMMM DD, YYYY'}) }}</p>
        <p>Math Result: {{ tsk_function('@math.add', {'a': 10, 'b': 20}) }}</p>
    </div>
    
    {% if tsk_available %}
        <p>✅ TuskLang v{{ tsk_version }} is available</p>
    {% endif %}
</body>
</html>
```

## 🎯 Key Features

### 🚀 Performance Engine
- **Turbo Template Engine** with intelligent caching
- **Parallel processing** for complex templates
- **Async rendering** capabilities
- **Performance metrics** and monitoring

### 🔧 TuskLang Integration
- **Full SDK access** to all TuskLang features
- **Configuration management** from `peanu.tsk` files
- **Function execution** with `@` operators
- **Database adapters** and async support

### 🛠️ Flask Enhancement
- **Drop-in replacement** for Flask templates
- **Blueprint support** for organized applications
- **REST API endpoints** for TuskLang operations
- **Error handling** and graceful degradation

## 📊 Performance Benchmarks

| Feature | Flask Default | Flask-TSK | Improvement |
|---------|---------------|-----------|-------------|
| Simple Template | 0.69ms | 0.03ms | **23x faster** |
| Complex Template | 2.94ms | 0.05ms | **59x faster** |
| Renders/Second | 1,454 | 30,167 | **21x more** |
| Memory Usage | 100% | 85% | **15% reduction** |

*Benchmarks based on actual testing with 1000 iterations*

## 🔥 The Power of @ Operators

<div align="center">
  <img src="svg/operators.svg" alt="@ Operators - The Secret Sauce" width="600"/>
</div>

TuskLang's `@` operators provide unprecedented power:

- **@query** - Direct database queries
- **@env** - Environment variable access
- **@cache** - Intelligent caching
- **@date** - Time and date functions
- **@learn** - Machine learning integration
- **@optimize** - Performance optimization
- **@metrics** - Real-time monitoring
- **@php** - PHP code execution

## 🌐 Real-time Configuration

<div align="center">
  <img src="svg/realtime.svg" alt="Real-time Configuration" width="500"/>
</div>

Flask-TSK provides real-time configuration management with live updates and intelligent caching.

## 🏆 TuskLang vs The Competition

<div align="center">
  <img src="svg/competition.svg" alt="TuskLang vs The Competition" width="600"/>
</div>

TuskLang outperforms all other configuration formats with superior features and capabilities.

## 🔧 Advanced Usage Examples

### Configuration Management

```python
# Load configuration from peanu.tsk
config = tsk.get_config()
app_name = tsk.get_section('app')['name']

# Save configuration
tsk.save_tsk({'app': {'name': 'My App'}}, 'config.tsk')

# Get specific configuration values
db_host = tsk.get_config('database', 'host', 'localhost')
db_port = tsk.get_config('database', 'port', 5432)
api_key = tsk.get_config('security', 'api_key', '')

# Check if configuration exists
if tsk.has_section('database'):
    db_config = tsk.get_section('database')
    print(f"Database: {db_config.get('type', 'unknown')}")

# Get all keys in a section
database_keys = tsk.get_all_keys('database')
print(f"Database keys: {database_keys}")
```

### Function Execution Examples

```python
# Execute TuskLang functions
result = tsk.execute_function('@math.add', {'a': 5, 'b': 3})

# Date and time functions
current_time = tsk.execute_function('@date.now')
formatted_date = tsk.execute_function('@date.format', {
    'date': '2024-01-01', 
    'format': 'MMMM DD, YYYY'
})

# Environment functions
env_var = tsk.execute_function('@env.get', {'key': 'DATABASE_URL'})

# Cache functions
cached_result = tsk.execute_function('@cache.get', {'key': 'user_data'})
tsk.execute_function('@cache.set', {'key': 'user_data', 'value': user_data})

# Database queries
users = tsk.execute_function('@query.select', {
    'table': 'users',
    'columns': ['id', 'name', 'email'],
    'where': {'active': True}
})

# Use in templates
{{ tsk_function('@math.add', {'a': 10, 'b': 20}) }}
{{ tsk_function('@date.format', {'date': '2024-01-01', 'format': 'YYYY-MM-DD'}) }}
{{ tsk_function('@env.get', {'key': 'APP_ENV'}) }}
```

### Database Integration Examples

```python
# Get database configuration
db_config = tsk.get_database_config()

# Use with SQLAlchemy
from sqlalchemy import create_engine
engine = create_engine(db_config['connection_string'])

# Use with TuskLang adapters
from tusktsk.adapters import get_adapter
adapter = get_adapter(db_config['type'])
connection = adapter.connect()

# Execute database queries through TuskLang
users = tsk.execute_function('@query.select', {
    'table': 'users',
    'columns': ['*'],
    'where': {'status': 'active'},
    'limit': 10
})

# Insert data
tsk.execute_function('@query.insert', {
    'table': 'users',
    'data': {
        'name': 'John Doe',
        'email': 'john@example.com',
        'created_at': '@date.now'
    }
})

# Update data
tsk.execute_function('@query.update', {
    'table': 'users',
    'data': {'status': 'inactive'},
    'where': {'id': 123}
})
```

### Advanced Template Examples

```html
<!-- Advanced template with TuskLang integration -->
<!DOCTYPE html>
<html>
<head>
    <title>{{ tsk_config('app.name', 'Flask-TSK') }}</title>
    <style>
        .theme-{{ tsk_config('ui.theme', 'light') }} {
            background: {{ tsk_config('ui.background_color', '#ffffff') }};
            color: {{ tsk_config('ui.text_color', '#000000') }};
        }
    </style>
</head>
<body class="theme-{{ tsk_config('ui.theme', 'light') }}">
    <header>
        <h1>{{ tsk_config('app.name', 'Flask-TSK') }}</h1>
        <p>Version {{ tsk_config('app.version', '1.0.0') }}</p>
        <p>Environment: {{ tsk_config('app.environment', 'development') }}</p>
    </header>
    
    <main>
        <!-- Dynamic content based on configuration -->
        {% if tsk_config('features.show_welcome', True) %}
            <section class="welcome">
                <h2>{{ tsk_section('welcome.title', 'Welcome!') }}</h2>
                <p>{{ tsk_section('welcome.message', 'Thank you for using Flask-TSK!') }}</p>
            </section>
        {% endif %}
        
        <!-- Real-time data display -->
        <section class="stats">
            <h3>System Statistics</h3>
            <p>Current Time: {{ tsk_function('@date.now') }}</p>
            <p>Uptime: {{ tsk_function('@metrics.uptime') }}</p>
            <p>Memory Usage: {{ tsk_function('@metrics.memory') }}</p>
            <p>Active Users: {{ tsk_function('@metrics.active_users') }}</p>
        </section>
        
        <!-- Database-driven content -->
        <section class="users">
            <h3>Recent Users</h3>
            {% set recent_users = tsk_function('@query.select', {
                'table': 'users',
                'columns': ['name', 'email', 'created_at'],
                'order_by': 'created_at DESC',
                'limit': 5
            }) %}
            
            {% for user in recent_users %}
                <div class="user-card">
                    <h4>{{ user.name }}</h4>
                    <p>{{ user.email }}</p>
                    <small>Joined: {{ tsk_function('@date.format', {
                        'date': user.created_at,
                        'format': 'MMM DD, YYYY'
                    }) }}</small>
                </div>
            {% endfor %}
        </section>
        
        <!-- Configuration panel -->
        <section class="config-panel">
            <h3>Configuration</h3>
            <div class="config-grid">
                <div>
                    <strong>Database:</strong> {{ tsk_config('database.type', 'sqlite') }}
                </div>
                <div>
                    <strong>Cache:</strong> {{ tsk_config('cache.enabled', False) }}
                </div>
                <div>
                    <strong>Debug:</strong> {{ tsk_config('app.debug', False) }}
                </div>
                <div>
                    <strong>API Key:</strong> 
                    {% if tsk_config('security.api_key') %}
                        ✅ Configured
                    {% else %}
                        ❌ Not set
                    {% endif %}
                </div>
            </div>
        </section>
    </main>
    
    <footer>
        <p>&copy; {{ tsk_function('@date.year') }} {{ tsk_config('app.name', 'Flask-TSK') }}</p>
        <p>Powered by <a href="https://github.com/cyber-boost/tusktsk">TuskLang</a></p>
    </footer>
</body>
</html>
```

### API Endpoints Examples

```python
# REST API endpoints for TuskLang operations
@app.route('/api/tsk/config', methods=['GET'])
def get_tsk_config():
    """Get all TuskLang configuration"""
    return jsonify(tsk.get_all_sections())

@app.route('/api/tsk/config/<section>', methods=['GET'])
def get_tsk_section(section):
    """Get specific configuration section"""
    if tsk.has_section(section):
        return jsonify(tsk.get_section(section))
    return jsonify({'error': 'Section not found'}), 404

@app.route('/api/tsk/function', methods=['POST'])
def execute_tsk_function():
    """Execute TuskLang function via API"""
    data = request.get_json()
    function_name = data.get('function')
    args = data.get('args', {})
    
    try:
        result = tsk.execute_function(function_name, args)
        return jsonify({'result': result})
    except Exception as e:
        return jsonify({'error': str(e)}), 400

@app.route('/api/tsk/parse', methods=['POST'])
def parse_tsk_content():
    """Parse TuskLang content"""
    data = request.get_json()
    content = data.get('content', '')
    
    try:
        parsed = tsk.parse_tsk(content)
        return jsonify({'parsed': parsed})
    except Exception as e:
        return jsonify({'error': str(e)}), 400

@app.route('/api/tsk/stringify', methods=['POST'])
def stringify_tsk_data():
    """Convert data to TuskLang format"""
    data = request.get_json()
    tsk_data = data.get('data', {})
    
    try:
        stringified = tsk.stringify_tsk(tsk_data)
        return jsonify({'tsk': stringified})
    except Exception as e:
        return jsonify({'error': str(e)}), 400
```

## 🛠️ API Endpoints

Flask-TSK provides REST API endpoints for TuskLang operations:

- `GET /tsk/config` - Get configuration
- `POST /tsk/parse` - Parse TuskLang content
- `POST /tsk/stringify` - Stringify data to TuskLang
- `GET /tsk/capabilities` - List available features

## 📦 Installation Options

### Basic Installation
```bash
pip install flask-tsk
```

### With Performance Optimizations
```bash
pip install flask-tsk[performance]
```

### With Database Support
```bash
pip install flask-tsk[databases]
```

### With FastAPI Integration
```bash
pip install flask-tsk[fastapi]
```

### Development Installation
```bash
git clone https://github.com/grim-project/flask-tsk.git
cd flask-tsk
pip install -e .
```

## 🧪 Testing

```bash
# Run tests
pytest

# Run with coverage
pytest --cov=tsk_flask

# Run performance benchmarks
python tsk_flask/performance_benchmark.py
```

## 📚 Documentation

- [Deployment Guide](DEPLOYMENT_GUIDE.md)
- [Performance Revolution](PERFORMANCE_REVOLUTION.md)
- [API Reference](API_REFERENCE.md)
- [Examples](examples/)

## 🔗 TuskLang Resources

- **[TuskLang Main Repository](https://github.com/cyber-boost/tusktsk)** - The official TuskLang language repository
- **[Python API Documentation](https://tuskt.sk/api-docs/python)** - Complete Python SDK API reference
- **[Python SDK Guide](https://tuskt.sk/sdk/python)** - Comprehensive Python implementation guide
- **[Dedicated Python Installation](https://py.tuskt.sk/)** - Python-specific installation and setup
- **[TuskLang Website](https://tuskt.sk)** - Official TuskLang language website

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Flask community for the amazing framework
- [TuskLang team](https://github.com/cyber-boost/tusktsk) for the powerful language and SDK
- All contributors and users

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/grim-project/flask-tsk/issues)
- **Discussions**: [GitHub Discussions](https://github.com/grim-project/flask-tsk/discussions)
- **Documentation**: [Read the Docs](https://flask-tsk.readthedocs.io/)
- **TuskLang Support**: [TuskLang Issues](https://github.com/cyber-boost/tusktsk/issues)

---

**Made with ❤️ by the Grim Development Team**

**Powered by [TuskLang](https://github.com/cyber-boost/tusktsk) - The Revolutionary Configuration Language** 🚀