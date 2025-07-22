#!/usr/bin/env python3
"""
Testing Commands for TuskLang Python CLI
========================================
Implements testing-related commands
"""

import os
import sys
import time
import subprocess
import unittest
import threading
from pathlib import Path
from typing import Any, Dict, List, Optional

# Optional imports with fallbacks
try:
    import coverage
    COVERAGE_AVAILABLE = True
except ImportError:
    COVERAGE_AVAILABLE = False
    print("coverage not available. Coverage analysis will be limited.")

try:
    from watchdog.observers import Observer
    from watchdog.events import FileSystemEventHandler
    WATCHDOG_AVAILABLE = True
except ImportError:
    WATCHDOG_AVAILABLE = False
    print("watchdog not available. Watch mode will be limited.")

from ...tsk import TSK, TSKParser
from ...peanut_config import PeanutConfig

# Import tsk_enhanced with fallback
try:
    from ...tsk_enhanced import TuskLangEnhanced
    TSK_ENHANCED_AVAILABLE = True
except ImportError:
    TSK_ENHANCED_AVAILABLE = False
    # Create dummy class for when tsk_enhanced is not available
    class TuskLangEnhanced:
        def __init__(self): pass
        def parse(self, content): return {}
        def parse_file(self, file_path): return {}

# Import adapters with try/except to handle missing dependencies gracefully
try:
    from ...adapters import SQLiteAdapter, PostgreSQLAdapter, MongoDBAdapter, RedisAdapter
    TEST_ADAPTERS_AVAILABLE = True
except ImportError:
    TEST_ADAPTERS_AVAILABLE = False
    # Create dummy classes for when adapters are not available
    class SQLiteAdapter:
        def __init__(self, config): pass
        def is_connected(self): return False
    class PostgreSQLAdapter:
        def __init__(self, config): pass
        def is_connected(self): return False
    class MongoDBAdapter:
        def __init__(self, config): pass
        def is_connected(self): return False
    class RedisAdapter:
        def __init__(self, config): pass
        def is_connected(self): return False

from ..utils.output_formatter import OutputFormatter
from ..utils.error_handler import ErrorHandler
from ..utils.config_loader import ConfigLoader


def handle_test_command(args: Any, cli: Any) -> int:
    """Handle test command with enhanced options"""
    formatter = OutputFormatter(cli.json_output, cli.quiet, cli.verbose)
    error_handler = ErrorHandler(cli.json_output, cli.verbose)
    
    try:
        if args.all:
            return _run_all_tests(formatter, error_handler)
        elif args.unit:
            return _run_unit_tests(formatter, error_handler)
        elif args.integration:
            return _run_integration_tests(formatter, error_handler)
        elif args.coverage:
            if not COVERAGE_AVAILABLE:
                formatter.error("Coverage analysis requires 'coverage' package. Install with: pip install coverage")
                return ErrorHandler.GENERAL_ERROR
            return _run_coverage_analysis(formatter, error_handler)
        elif args.watch:
            if not WATCHDOG_AVAILABLE:
                formatter.error("Watch mode requires 'watchdog' package. Install with: pip install watchdog")
                return ErrorHandler.GENERAL_ERROR
            return _run_test_watch_mode(formatter, error_handler)
        elif args.parser:
            return _test_parser(formatter, error_handler)
        elif args.fujsen:
            return _test_fujsen(formatter, error_handler)
        elif args.sdk:
            return _test_sdk(formatter, error_handler)
        elif args.performance:
            return _test_performance(formatter, error_handler)
        elif args.suite:
            return _run_test_suite(args.suite, formatter, error_handler)
        else:
            return _run_basic_tests(formatter, error_handler)
            
    except Exception as e:
        return error_handler.handle_error(e)


class TestWatcher(FileSystemEventHandler):
    """File system watcher for continuous testing"""
    
    def __init__(self, formatter: OutputFormatter, error_handler: ErrorHandler):
        self.formatter = formatter
        self.error_handler = error_handler
        self.last_run = 0
        self.test_files = []
        self._discover_test_files()
    
    def _discover_test_files(self):
        """Discover all test files in the project"""
        test_patterns = ['test_*.py', '*_test.py', 'tests.py']
        
        # Get the TuskLang project root directory
        current_dir = Path.cwd()
        project_root = None
        
        # Look for the tusktsk directory in the current path
        for parent in [current_dir] + list(current_dir.parents):
            if (parent / 'tusktsk').exists() and (parent / 'tusktsk' / 'cli').exists():
                project_root = parent
                break
        
        if not project_root:
            # Fallback to current directory if we can't find the project root
            project_root = current_dir
        
        for pattern in test_patterns:
            for test_file in project_root.rglob(pattern):
                if test_file.is_file():
                    # Skip virtual environment directories
                    if any(part in ['venv', 'env', 'tusktsk_env', '__pycache__', '.git'] for part in test_file.parts):
                        continue
                    # Skip site-packages and other Python package directories
                    if any(part in ['site-packages', 'dist-packages', 'lib', 'lib64'] for part in test_file.parts):
                        continue
                    self.test_files.append(test_file)
    
    def on_modified(self, event):
        if not event.is_directory:
            current_time = time.time()
            if current_time - self.last_run > 2:  # Debounce
                self.last_run = current_time
                
                # Check if modified file is a test file or source file
                modified_file = Path(event.src_path)
                if (modified_file.suffix == '.py' and 
                    (any(pattern in modified_file.name for pattern in ['test_', '_test.py', 'tests.py']) or
                     'test' in modified_file.parts)):
                    
                    self.formatter.info(f"Test file changed: {modified_file}")
                    _run_unit_tests(self.formatter, self.error_handler, silent=True)


def _run_all_tests(formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Run all test suites"""
    formatter.section("Running All Test Suites")
    
    test_results = []
    
    # Run each test suite
    test_suites = [
        ("Parser", _test_parser),
        ("FUJSEN", _test_fujsen),
        ("SDK", _test_sdk),
        ("Performance", _test_performance),
        ("Database Adapters", _test_database_adapters),
        ("Configuration", _test_configuration)
    ]
    
    for suite_name, test_func in test_suites:
        formatter.subsection(f"Testing {suite_name}")
        try:
            result = test_func(formatter, error_handler, silent=True)
            status = "✅ PASS" if result == ErrorHandler.SUCCESS else "❌ FAIL"
            test_results.append([suite_name, status])
        except Exception as e:
            test_results.append([suite_name, f"❌ ERROR: {str(e)}"])
    
    # Display results
    formatter.table(
        ['Test Suite', 'Status'],
        test_results,
        'Test Results Summary'
    )
    
    # Check if all tests passed
    failed_tests = [result for result in test_results if '❌' in result[1]]
    if failed_tests:
        formatter.error(f"{len(failed_tests)} test suites failed")
        return ErrorHandler.GENERAL_ERROR
    else:
        formatter.success("All test suites passed!")
        return ErrorHandler.SUCCESS


def _run_basic_tests(formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Run basic tests"""
    formatter.section("Running Basic Tests")
    
    test_results = []
    
    # Test parser
    try:
        result = _test_parser(formatter, error_handler, silent=True)
        test_results.append(["Parser", "✅ PASS" if result == ErrorHandler.SUCCESS else "❌ FAIL"])
    except Exception as e:
        test_results.append(["Parser", f"❌ ERROR: {str(e)}"])
    
    # Test basic functionality
    try:
        result = _test_basic_functionality(formatter, error_handler)
        test_results.append(["Basic Functionality", "✅ PASS" if result == ErrorHandler.SUCCESS else "❌ FAIL"])
    except Exception as e:
        test_results.append(["Basic Functionality", f"❌ ERROR: {str(e)}"])
    
    # Display results
    formatter.table(
        ['Test', 'Status'],
        test_results,
        'Basic Test Results'
    )
    
    failed_tests = [result for result in test_results if '❌' in result[1]]
    if failed_tests:
        formatter.error(f"{len(failed_tests)} tests failed")
        return ErrorHandler.GENERAL_ERROR
    else:
        formatter.success("All basic tests passed!")
        return ErrorHandler.SUCCESS


def _run_test_suite(suite_name: str, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Run specific test suite"""
    formatter.section(f"Running Test Suite: {suite_name}")
    
    test_suites = {
        'parser': _test_parser,
        'fujsen': _test_fujsen,
        'sdk': _test_sdk,
        'performance': _test_performance,
        'database': _test_database_adapters,
        'config': _test_configuration
    }
    
    if suite_name.lower() not in test_suites:
        formatter.error(f"Unknown test suite: {suite_name}")
        formatter.info(f"Available suites: {', '.join(test_suites.keys())}")
        return ErrorHandler.INVALID_ARGS
    
    return test_suites[suite_name.lower()](formatter, error_handler)


def _test_parser(formatter: OutputFormatter, error_handler: ErrorHandler, silent: bool = False) -> int:
    """Test parser functionality"""
    if not silent:
        formatter.subsection("Testing Parser")
    
    try:
        # Test basic TSK parsing
        test_content = """
[database]
host = "localhost"
port = 5432
debug = true

[server]
host = "0.0.0.0"
port = 8080
workers = 4
"""
        
        # Test basic parser
        data = TSKParser.parse(test_content)
        if not isinstance(data, dict):
            raise Exception("Parser did not return dictionary")
        
        if 'database' not in data or 'server' not in data:
            raise Exception("Parser did not parse sections correctly")
        
        # Test enhanced parser
        enhanced_parser = TuskLangEnhanced()
        enhanced_data = enhanced_parser.parse(test_content)
        
        if not isinstance(enhanced_data, dict):
            raise Exception("Enhanced parser did not return dictionary")
        
        # Test complex parsing
        complex_content = """
$app_name: "Test App"
$version: "1.0.0"

app_name: $app_name
version: $version

server {
    host: "127.0.0.1"
    port: 3000
}

cache >
    driver: "redis"
    ttl: "5m"
<
"""
        
        complex_data = enhanced_parser.parse(complex_content)
        if not isinstance(complex_data, dict):
            raise Exception("Complex parsing failed")
        
        if not silent:
            formatter.success("Parser tests passed")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        if not silent:
            formatter.error(f"Parser test failed: {e}")
        return ErrorHandler.GENERAL_ERROR


def _test_fujsen(formatter: OutputFormatter, error_handler: ErrorHandler, silent: bool = False) -> int:
    """Test FUJSEN functionality"""
    if not silent:
        formatter.subsection("Testing FUJSEN")
    
    try:
        # Test FUJSEN execution
        tsk = TSK()
        
        # Add test function
        def test_function(x, y):
            return x + y
        
        tsk.set_fujsen('test', 'add_fujsen', test_function)
        
        # Execute function
        result = tsk.execute_fujsen('test', 'add_fujsen', 5, 3)
        if result != 8:
            raise Exception(f"FUJSEN execution failed: expected 8, got {result}")
        
        # Test lambda function
        tsk.set_value('test', 'multiply_fujsen', """
lambda x, y: x * y
""")
        
        result = tsk.execute_fujsen('test', 'multiply_fujsen', 4, 6)
        if result != 24:
            raise Exception(f"Lambda FUJSEN failed: expected 24, got {result}")
        
        # Test JavaScript-style function
        tsk.set_value('test', 'js_style_fujsen', """
(x, y) => {
  if (x > y) return x;
  return y;
}
""")
        
        result = tsk.execute_fujsen('test', 'js_style_fujsen', 10, 15)
        if result != 15:
            raise Exception(f"JS-style FUJSEN failed: expected 15, got {result}")
        
        if not silent:
            formatter.success("FUJSEN tests passed")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        if not silent:
            formatter.error(f"FUJSEN test failed: {e}")
        return ErrorHandler.GENERAL_ERROR


def _test_sdk(formatter: OutputFormatter, error_handler: ErrorHandler, silent: bool = False) -> int:
    """Test SDK-specific features"""
    if not silent:
        formatter.subsection("Testing SDK Features")
    
    try:
        # Test file operations
        test_file = Path('test_sdk.tsk')
        test_content = """
[test]
value = "hello"
number = 42
"""
        
        # Create test file
        with open(test_file, 'w') as f:
            f.write(test_content)
        
        # Test loading from file
        tsk = TSK.from_file(str(test_file))
        value = tsk.get_value('test', 'value')
        if value != "hello":
            raise Exception(f"File loading failed: expected 'hello', got '{value}'")
        
        # Test saving to file
        tsk.set_value('test', 'new_value', 'world')
        tsk.to_file(str(test_file))
        
        # Verify save
        tsk2 = TSK.from_file(str(test_file))
        new_value = tsk2.get_value('test', 'new_value')
        if new_value != "world":
            raise Exception(f"File saving failed: expected 'world', got '{new_value}'")
        
        # Clean up
        test_file.unlink()
        
        if not silent:
            formatter.success("SDK tests passed")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        if not silent:
            formatter.error(f"SDK test failed: {e}")
        return ErrorHandler.GENERAL_ERROR


def _test_performance(formatter: OutputFormatter, error_handler: ErrorHandler, silent: bool = False) -> int:
    """Test performance benchmarks"""
    if not silent:
        formatter.subsection("Performance Benchmarks")
    
    try:
        # Create large test data
        large_content = []
        for i in range(1000):
            large_content.append(f'[section_{i}]')
            large_content.append(f'key_{i} = "value_{i}"')
            large_content.append(f'number_{i} = {i}')
        
        large_content = '\n'.join(large_content)
        
        # Benchmark parsing
        start_time = time.time()
        data = TSKParser.parse(large_content)
        parse_time = time.time() - start_time
        
        # Benchmark enhanced parsing
        enhanced_parser = TuskLangEnhanced()
        start_time = time.time()
        enhanced_data = enhanced_parser.parse(large_content)
        enhanced_parse_time = time.time() - start_time
        
        # Benchmark binary compilation
        peanut_config = PeanutConfig()
        start_time = time.time()
        peanut_config.compile_to_binary(data, 'test_performance.pnt')
        compile_time = time.time() - start_time
        
        # Benchmark binary loading
        start_time = time.time()
        loaded_data = peanut_config.load_binary('test_performance.pnt')
        load_time = time.time() - start_time
        
        # Clean up
        Path('test_performance.pnt').unlink()
        
        # Display results
        if not silent:
            formatter.table(
                ['Operation', 'Time (ms)', 'Performance'],
                [
                    ['Basic Parse', f"{parse_time*1000:.2f}", 'Baseline'],
                    ['Enhanced Parse', f"{enhanced_parse_time*1000:.2f}", f"{parse_time/enhanced_parse_time:.1f}x"],
                    ['Binary Compile', f"{compile_time*1000:.2f}", 'N/A'],
                    ['Binary Load', f"{load_time*1000:.2f}", f"{parse_time/load_time:.1f}x faster"]
                ],
                'Performance Results'
            )
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        if not silent:
            formatter.error(f"Performance test failed: {e}")
        return ErrorHandler.GENERAL_ERROR


def _test_database_adapters(formatter: OutputFormatter, error_handler: ErrorHandler, silent: bool = False) -> int:
    """Test database adapters"""
    if not silent:
        formatter.subsection("Testing Database Adapters")
    
    try:
        # Test SQLite adapter
        adapter = SQLiteAdapter({'database': ':memory:'})
        adapter.connect()
        
        # Create test table
        adapter.query("""
            CREATE TABLE test_table (
                id INTEGER PRIMARY KEY,
                name TEXT,
                value INTEGER
            )
        """)
        
        # Insert test data
        adapter.query("INSERT INTO test_table (name, value) VALUES (?, ?)", ["test", 42])
        
        # Query test data
        result = adapter.query("SELECT * FROM test_table")
        if len(result) != 1 or result[0]['name'] != 'test':
            raise Exception("SQLite adapter test failed")
        
        adapter.close()
        
        if not silent:
            formatter.success("Database adapter tests passed")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        if not silent:
            formatter.error(f"Database adapter test failed: {e}")
        return ErrorHandler.GENERAL_ERROR


def _test_configuration(formatter: OutputFormatter, error_handler: ErrorHandler, silent: bool = False) -> int:
    """Test configuration system"""
    if not silent:
        formatter.subsection("Testing Configuration System")
    
    try:
        # Test PeanutConfig
        peanut_config = PeanutConfig()
        
        # Create test config
        test_config = {
            'app': {
                'name': 'Test App',
                'version': '1.0.0'
            },
            'database': {
                'host': 'localhost',
                'port': 5432
            }
        }
        
        # Test binary compilation
        peanut_config.compile_to_binary(test_config, 'test_config.pnt')
        
        # Test binary loading
        loaded_config = peanut_config.load_binary('test_config.pnt')
        
        if loaded_config != test_config:
            raise Exception("Configuration binary round-trip failed")
        
        # Clean up
        Path('test_config.pnt').unlink()
        
        if not silent:
            formatter.success("Configuration tests passed")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        if not silent:
            formatter.error(f"Configuration test failed: {e}")
        return ErrorHandler.GENERAL_ERROR


def _test_basic_functionality(formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Test basic functionality"""
    try:
        # Test TSK class
        tsk = TSK()
        tsk.set_section('test', {'key': 'value'})
        
        value = tsk.get_value('test', 'key')
        if value != 'value':
            raise Exception("Basic TSK functionality failed")
        
        # Test string conversion
        tsk_string = tsk.to_string()
        if 'key = "value"' not in tsk_string:
            raise Exception("TSK string conversion failed")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        formatter.error(f"Basic functionality test failed: {e}")
        return ErrorHandler.GENERAL_ERROR 


def _run_unit_tests(formatter: OutputFormatter, error_handler: ErrorHandler, silent: bool = False) -> int:
    """Run unit tests with proper discovery and reporting"""
    if not silent:
        formatter.section("Running Unit Tests")
    
    try:
        # Discover test files - limit to TuskLang project directory
        test_files = []
        test_patterns = ['test_*.py', '*_test.py', 'tests.py']
        
        # Get the TuskLang project root directory
        current_dir = Path.cwd()
        project_root = None
        
        # Look for the tusktsk directory in the current path
        for parent in [current_dir] + list(current_dir.parents):
            if (parent / 'tusktsk').exists() and (parent / 'tusktsk' / 'cli').exists():
                project_root = parent
                break
        
        if not project_root:
            # Fallback to current directory if we can't find the project root
            project_root = current_dir
        
        if not silent:
            formatter.info(f"Searching for tests in: {project_root}")
        
        for pattern in test_patterns:
            for test_file in project_root.rglob(pattern):
                if test_file.is_file():
                    # Skip virtual environment directories
                    if any(part in ['venv', 'env', 'tusktsk_env', '__pycache__', '.git'] for part in test_file.parts):
                        continue
                    # Skip site-packages and other Python package directories
                    if any(part in ['site-packages', 'dist-packages', 'lib', 'lib64'] for part in test_file.parts):
                        continue
                    test_files.append(test_file)
        
        if not test_files:
            if not silent:
                formatter.warning("No test files found in TuskLang project")
            return ErrorHandler.SUCCESS
        
        if not silent:
            formatter.info(f"Found {len(test_files)} test files")
        
        # Run tests using unittest
        loader = unittest.TestLoader()
        suite = unittest.TestSuite()
        
        for test_file in test_files:
            try:
                # Import test module
                module_name = str(test_file.relative_to(project_root)).replace('/', '.').replace('.py', '')
                test_module = __import__(module_name, fromlist=['*'])
                
                # Add tests to suite
                tests = loader.loadTestsFromModule(test_module)
                suite.addTests(tests)
                
            except Exception as e:
                if not silent:
                    formatter.warning(f"Failed to load tests from {test_file}: {e}")
        
        # Run tests
        runner = unittest.TextTestRunner(verbosity=2 if not silent else 1)
        result = runner.run(suite)
        
        # Report results
        if not silent:
            if result.wasSuccessful():
                formatter.success(f"Unit tests passed: {result.testsRun} tests run")
            else:
                formatter.error(f"Unit tests failed: {len(result.failures)} failures, {len(result.errors)} errors")
        
        return ErrorHandler.SUCCESS if result.wasSuccessful() else ErrorHandler.GENERAL_ERROR
        
    except Exception as e:
        if not silent:
            formatter.error(f"Unit test execution failed: {e}")
        return ErrorHandler.GENERAL_ERROR 


def _run_integration_tests(formatter: OutputFormatter, error_handler: ErrorHandler, silent: bool = False) -> int:
    """Run integration tests for database adapters and configuration system"""
    if not silent:
        formatter.section("Running Integration Tests")
    
    try:
        test_results = []
        
        # Test database adapters
        if TEST_ADAPTERS_AVAILABLE:
            try:
                result = _test_database_adapters(formatter, error_handler, silent=True)
                test_results.append(["Database Adapters", "✅ PASS" if result == ErrorHandler.SUCCESS else "❌ FAIL"])
            except Exception as e:
                test_results.append(["Database Adapters", f"❌ ERROR: {str(e)}"])
        else:
            test_results.append(["Database Adapters", "⚠️ SKIP (adapters not available)"])
        
        # Test configuration system
        try:
            result = _test_configuration(formatter, error_handler, silent=True)
            test_results.append(["Configuration System", "✅ PASS" if result == ErrorHandler.SUCCESS else "❌ FAIL"])
        except Exception as e:
            test_results.append(["Configuration System", f"❌ ERROR: {str(e)}"])
        
        # Test TSK parser integration
        try:
            result = _test_parser(formatter, error_handler, silent=True)
            test_results.append(["TSK Parser", "✅ PASS" if result == ErrorHandler.SUCCESS else "❌ FAIL"])
        except Exception as e:
            test_results.append(["TSK Parser", f"❌ ERROR: {str(e)}"])
        
        # Display results
        if not silent:
            formatter.table(
                ['Integration Test', 'Status'],
                test_results,
                'Integration Test Results'
            )
        
        # Check if all tests passed
        failed_tests = [result for result in test_results if '❌' in result[1]]
        if failed_tests:
            if not silent:
                formatter.error(f"{len(failed_tests)} integration tests failed")
            return ErrorHandler.GENERAL_ERROR
        else:
            if not silent:
                formatter.success("All integration tests passed!")
            return ErrorHandler.SUCCESS
            
    except Exception as e:
        if not silent:
            formatter.error(f"Integration test execution failed: {e}")
        return ErrorHandler.GENERAL_ERROR


def _run_coverage_analysis(formatter: OutputFormatter, error_handler: ErrorHandler, silent: bool = False) -> int:
    """Run coverage analysis using the coverage library"""
    if not silent:
        formatter.section("Running Coverage Analysis")
    
    if not COVERAGE_AVAILABLE:
        formatter.error("Coverage analysis requires 'coverage' package. Install with: pip install coverage")
        return ErrorHandler.GENERAL_ERROR
    
    try:
        # Initialize coverage
        cov = coverage.Coverage()
        cov.start()
        
        # Run unit tests to collect coverage data
        if not silent:
            formatter.info("Running tests to collect coverage data...")
        
        _run_unit_tests(formatter, error_handler, silent=True)
        
        # Stop coverage collection
        cov.stop()
        cov.save()
        
        # Generate reports
        if not silent:
            formatter.info("Generating coverage reports...")
        
        # Text report
        text_report = cov.report()
        
        # HTML report
        cov.html_report(directory='htmlcov')
        
        # XML report for CI/CD
        cov.xml_report(outfile='coverage.xml')
        
        # Get coverage percentage
        total_coverage = cov.report(show_missing=False)
        
        if not silent:
            formatter.success(f"Coverage analysis completed: {total_coverage:.1f}%")
            formatter.info("HTML report generated in 'htmlcov/' directory")
            formatter.info("XML report generated as 'coverage.xml'")
        
        # Check coverage threshold (80% default)
        if total_coverage < 80:
            if not silent:
                formatter.warning(f"Coverage below threshold: {total_coverage:.1f}% < 80%")
            return ErrorHandler.GENERAL_ERROR
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        if not silent:
            formatter.error(f"Coverage analysis failed: {e}")
        return ErrorHandler.GENERAL_ERROR


def _run_test_watch_mode(formatter: OutputFormatter, error_handler: ErrorHandler, silent: bool = False) -> int:
    """Run tests in watch mode, automatically re-running when files change"""
    if not silent:
        formatter.section("Starting Test Watch Mode")
    
    if not WATCHDOG_AVAILABLE:
        formatter.error("Watch mode requires 'watchdog' package. Install with: pip install watchdog")
        return ErrorHandler.GENERAL_ERROR
    
    try:
        # Get the TuskLang project root directory
        current_dir = Path.cwd()
        project_root = None
        
        # Look for the tusktsk directory in the current path
        for parent in [current_dir] + list(current_dir.parents):
            if (parent / 'tusktsk').exists() and (parent / 'tusktsk' / 'cli').exists():
                project_root = parent
                break
        
        if not project_root:
            project_root = current_dir
        
        if not silent:
            formatter.info(f"Watching for changes in: {project_root}")
            formatter.info("Press Ctrl+C to stop watch mode")
        
        # Create and start file watcher
        event_handler = TestWatcher(formatter, error_handler)
        observer = Observer()
        observer.schedule(event_handler, str(project_root), recursive=True)
        observer.start()
        
        try:
            # Run initial tests
            if not silent:
                formatter.info("Running initial tests...")
            _run_unit_tests(formatter, error_handler, silent=True)
            
            # Keep running until interrupted
            while True:
                time.sleep(1)
                
        except KeyboardInterrupt:
            if not silent:
                formatter.info("Stopping watch mode...")
            observer.stop()
            observer.join()
            return ErrorHandler.SUCCESS
            
    except Exception as e:
        if not silent:
            formatter.error(f"Watch mode failed: {e}")
        return ErrorHandler.GENERAL_ERROR 