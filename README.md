# TuskTsk Python SDK

[![Python CI/CD](https://github.com/cyber-boost/tusktsk/workflows/Python%20SDK%20CI%2FCD/badge.svg)](https://github.com/cyber-boost/tusktsk/actions)
[![PyPI version](https://badge.fury.io/py/tusktsk.svg)](https://badge.fury.io/py/tusktsk)
[![Python versions](https://img.shields.io/pypi/pyversions/tusktsk.svg)](https://pypi.org/project/tusktsk/)

Advanced Python SDK for TuskTsk - Configuration with a Heartbeat

## 🚀 Quick Start

### Installation

```bash
# From PyPI
pip install tusktsk

# From source
git clone https://github.com/cyber-boost/tusktsk.git
cd tusktsk/sdk/python
pip install -e .
```

### Usage

```python
import tusktsk

# Basic usage
config = tusktsk.load_config('config.tsk')
print(config.get('database.host'))

# CLI usage
# tusk --help
```

## 📦 Features

- **Advanced Operators**: 150+ Python operators for various integrations
- **CLI Tools**: Comprehensive command-line interface with 12 command modules
- **Database Adapters**: Support for 6 different database systems
- **AI/ML Engines**: Machine learning and AI processing capabilities
- **Enterprise Security**: Advanced security and compliance features
- **Platform Integrations**: Multi-platform support and integrations

## 🔧 Requirements

- Python >= 3.8
- msgpack >= 1.0.0
- watchdog >= 2.0.0

## 📚 Documentation

- [Installation Guide](docs/installation.md)
- [API Reference](docs/api.md)
- [CLI Commands](docs/cli.md)
- [Examples](docs/examples.md)

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Issues**: [GitHub Issues](https://github.com/cyber-boost/tusktsk/issues)
- **Discussions**: [GitHub Discussions](https://github.com/cyber-boost/tusktsk/discussions)
- **Documentation**: [https://tuskt.sk](https://tuskt.sk)

## 🏗️ Development

```bash
# Clone repository
git clone https://github.com/cyber-boost/tusktsk.git
cd tusktsk

# Install development dependencies
cd sdk/python
pip install -e .[dev]

# Run tests
pytest tests/ -v

# Run linting
flake8 tusktsk/ aa_python/ adapters/ cli/ --max-line-length=120
```

---

**Made with ❤️ by the TuskTsk Team**
