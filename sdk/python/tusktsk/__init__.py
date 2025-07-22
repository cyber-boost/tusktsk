#!/usr/bin/env python3
"""
TuskLang Python SDK
===================
Configuration with a Heartbeat. Query databases, use any syntax, never bow to any king!

Complete SDK with advanced operators, database adapters, AI/ML engines, and enterprise features.
"""

# Core TuskLang functionality
from .tsk import TSK, TSKParser, parse, stringify, load, save
from .tsk_enhanced import TuskLangEnhanced
from .peanut_config import PeanutConfig
from .shell_storage import ShellStorage
from .fujsen.fujsen import FUJSEN
from .license import License
from .protection import Protection

# Import CLI main function
try:
    from .cli.main import main
    __all__ = ['main']
except ImportError as e:
    # If CLI fails to import due to missing dependencies, create a fallback
    def main():
        print("TuskLang CLI is not available due to missing dependencies.")
        print("Please install required packages: pip install aiohttp redis psycopg2-binary pymongo")
        return 1
    __all__ = ['main']

# Complete exports
__all__ = [
    # Core classes
    'TSK',
    'TSKParser', 
    'parse',
    'stringify',
    'load',
    'save',
    'TuskLangEnhanced',
    'PeanutConfig',
    'ShellStorage',
    'FUJSEN',
    'License',
    'Protection',
    'main'
]

# Version info
__version__ = "2.0.2"
__author__ = "Cyberboost LLC"
__email__ = "packages@tuskt.sk"
