#!/usr/bin/env python3
"""
TuskLang Python SDK Setup
"""
from setuptools import setup, find_packages
import pathlib

here = pathlib.Path(__file__).parent.resolve()

# Get the long description from the README file
long_description = (here / "README.md").read_text(encoding="utf-8")

setup(
    name="tusklang",
    version="2.0.0",
    description="TuskLang - Configuration with a Heartbeat. Query databases, use any syntax, never bow to any king!",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/tusklang/python",
    author="TuskLang Team",
    author_email="zoo@phptu.sk",
    classifiers=[
        "Development Status :: 5 - Production/Stable",
        "Intended Audience :: Developers",
        "Topic :: Software Development :: Libraries :: Python Modules",
        "Topic :: Software Development :: Configuration",
        "License :: Other/Proprietary License",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
    ],
    keywords="configuration, config, tusklang, tusk, database, query, dynamic",
    package_dir={"": "."},
    py_modules=["tsk", "tsk_enhanced", "peanut_config"],
    packages=find_packages(where="."),
    python_requires=">=3.8, <4",
    install_requires=[
        "msgpack>=1.0.0",
        "watchdog>=2.0.0",
    ],
    extras_require={
        "dev": ["pytest>=6.0", "black", "flake8"],
        "databases": [
            "psycopg2-binary>=2.8",
            "pymongo>=3.11",
            "redis>=3.5",
            "sqlite3",
        ],
    },
    entry_points={
        "console_scripts": [
            "tusk=tsk:main",
            "tusk-peanut=peanut_config:main",
        ],
    },
    project_urls={
        "Bug Reports": "https://github.com/tusklang/python/issues",
        "Documentation": "https://tusklang.org/docs/python",
        "Source": "https://github.com/tusklang/python",
    },
)