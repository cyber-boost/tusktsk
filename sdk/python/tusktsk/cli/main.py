#!/usr/bin/env python3
"""
TuskLang Python CLI - Main Entry Point
======================================
Complete command-line interface following Universal CLI Command Specification
"""

import sys
import argparse
import json
from typing import Dict, Any, Optional
from pathlib import Path

# Import from parent package
from ..tsk import TSK, TSKParser
from ..peanut_config import PeanutConfig

# Import CLI modules
from .commands import (
    db_commands, dev_commands, test_commands, service_commands,
    cache_commands, config_commands, binary_commands
)
from .commands import ai_commands, utility_commands, peanuts_commands, css_commands, license_commands, dependency_commands, security_commands
from .utils import output_formatter, error_handler, config_loader


class TuskLangCLI:
    """Main CLI class for TuskLang Python SDK"""
    
    def __init__(self):
        self.parser = argparse.ArgumentParser(
            prog='tsk',
            description='TuskLang Python SDK - Strong. Secure. Scalable.',
            formatter_class=argparse.RawDescriptionHelpFormatter,
            epilog="""
Examples:
  tsk db status                    # Check database connection
  tsk serve 3000                  # Start development server
  tsk test all                    # Run all tests
  tsk config get server.port      # Get configuration value
  tsk binary compile app.tsk      # Compile to binary format
  tsk binary info app.pnt         # Analyze binary file
  tsk ai models --service openai  # List AI models
  tsk ai usage --days 30          # Show AI usage statistics
            """
        )
        
        # Global options
        self.parser.add_argument('--version', '-v', action='version', version='TuskLang Python SDK 2.0.0')
        self.parser.add_argument('--verbose', action='store_true', help='Enable verbose output')
        self.parser.add_argument('--quiet', '-q', action='store_true', help='Suppress non-error output')
        self.parser.add_argument('--json', action='store_true', help='Output in JSON format')
        self.parser.add_argument('--config', help='Use alternate config file')
        
        # Create subparsers for commands
        subparsers = self.parser.add_subparsers(dest='command', help='Available commands')
        
        # Add all command categories
        self._add_db_commands(subparsers)
        self._add_dev_commands(subparsers)
        self._add_test_commands(subparsers)
        self._add_service_commands(subparsers)
        self._add_cache_commands(subparsers)
        self._add_config_commands(subparsers)
        self._add_binary_commands(subparsers)
        self._add_security_commands(subparsers)
        self._add_peanuts_commands(subparsers)
        self._add_ai_commands(subparsers)
        self._add_css_commands(subparsers)
        self._add_license_commands(subparsers)
        self._add_utility_commands(subparsers)
        self._add_dependency_commands(subparsers)
    
    def _add_db_commands(self, subparsers):
        """Add database commands"""
        db_parser = subparsers.add_parser('db', help='Database operations')
        db_subparsers = db_parser.add_subparsers(dest='db_command', help='Database commands')
        
        # db status
        db_subparsers.add_parser('status', help='Check database connection status')
        
        # db health
        db_subparsers.add_parser('health', help='Comprehensive database health check')
        
        # db query
        query_parser = db_subparsers.add_parser('query', help='Execute SQL query')
        query_parser.add_argument('sql', help='SQL query to execute')
        query_parser.add_argument('--params', help='JSON parameters for the query')
        
        # db migrate
        migrate_parser = db_subparsers.add_parser('migrate', help='Run migration file')
        migrate_parser.add_argument('file', help='Migration file path')
        
        # db rollback
        rollback_parser = db_subparsers.add_parser('rollback', help='Rollback last migration')
        rollback_parser.add_argument('--force', action='store_true', help='Skip confirmation prompt')
        
        # db optimize
        db_subparsers.add_parser('optimize', help='Optimize database performance')
        
        # db vacuum
        db_subparsers.add_parser('vacuum', help='Clean up SQLite database')
        
        # db reindex
        db_subparsers.add_parser('reindex', help='Rebuild database indexes')
        
        # db analyze
        db_subparsers.add_parser('analyze', help='Collect database statistics')
        
        # db connections
        db_subparsers.add_parser('connections', help='Monitor active connections')
        
        # db console
        db_subparsers.add_parser('console', help='Open interactive database console')
        
        # db backup
        backup_parser = db_subparsers.add_parser('backup', help='Backup database')
        backup_parser.add_argument('file', nargs='?', help='Backup file path (optional)')
        
        # db restore
        restore_parser = db_subparsers.add_parser('restore', help='Restore from backup file')
        restore_parser.add_argument('file', help='Backup file path')
        
        # db init
        db_subparsers.add_parser('init', help='Initialize SQLite database')
    
    def _add_dev_commands(self, subparsers):
        """Add development commands"""
        dev_parser = subparsers.add_parser('serve', help='Start development server')
        dev_parser.add_argument('port', nargs='?', type=int, default=8080, help='Port number (default: 8080)')
        dev_parser.add_argument('--host', default='0.0.0.0', help='Host to bind to (default: 0.0.0.0)')
        dev_parser.add_argument('--ssl', action='store_true', help='Enable SSL/HTTPS')
        dev_parser.add_argument('--ssl-cert', help='SSL certificate file path')
        dev_parser.add_argument('--ssl-key', help='SSL private key file path')
        dev_parser.add_argument('--reload', action='store_true', help='Enable auto-reload on file changes')
        dev_parser.add_argument('--workers', type=int, default=1, help='Number of worker processes (default: 1)')
        
        # Web interface command
        web_parser = subparsers.add_parser('web', help='Launch web interface')
        web_parser.add_argument('--port', type=int, default=5000, help='Port number (default: 5000)')
        web_parser.add_argument('--host', default='0.0.0.0', help='Host to bind to (default: 0.0.0.0)')
        web_parser.add_argument('--debug', action='store_true', help='Enable debug mode')
        web_parser.add_argument('--reload', action='store_true', help='Enable auto-reload on file changes')
        
        compile_parser = subparsers.add_parser('compile', help='Compile .tsk file')
        compile_parser.add_argument('file', help='TSK file to compile')
        compile_parser.add_argument('--watch', action='store_true', help='Watch for changes and recompile automatically')
        compile_parser.add_argument('--output-dir', help='Output directory for compiled files')
        
        optimize_parser = subparsers.add_parser('optimize', help='Optimize .tsk file')
        optimize_parser.add_argument('file', help='TSK file to optimize')
        optimize_parser.add_argument('--profile', action='store_true', help='Enable profiling during optimization')
        optimize_parser.add_argument('--output-dir', help='Output directory for optimized files')
    
    def _add_test_commands(self, subparsers):
        """Add testing commands"""
        test_parser = subparsers.add_parser('test', help='Run tests')
        test_parser.add_argument('suite', nargs='?', help='Test suite to run')
        test_parser.add_argument('--all', action='store_true', help='Run all test suites')
        test_parser.add_argument('--unit', action='store_true', help='Run unit tests only')
        test_parser.add_argument('--integration', action='store_true', help='Run integration tests only')
        test_parser.add_argument('--coverage', action='store_true', help='Run coverage analysis')
        test_parser.add_argument('--watch', action='store_true', help='Run tests in watch mode')
        test_parser.add_argument('--parser', action='store_true', help='Test parser functionality only')
        test_parser.add_argument('--fujsen', action='store_true', help='Test FUJSEN operators only')
        test_parser.add_argument('--sdk', action='store_true', help='Test SDK-specific features')
        test_parser.add_argument('--performance', action='store_true', help='Run performance benchmarks')
    
    def _add_service_commands(self, subparsers):
        """Add service commands"""
        services_parser = subparsers.add_parser('services', help='Service management')
        services_subparsers = services_parser.add_subparsers(dest='service_command', help='Service commands')
        
        services_subparsers.add_parser('start', help='Start all TuskLang services')
        services_subparsers.add_parser('stop', help='Stop all TuskLang services')
        services_subparsers.add_parser('restart', help='Restart all services')
        services_subparsers.add_parser('status', help='Show status of all services')
        
        # services logs
        logs_parser = services_subparsers.add_parser('logs', help='Show service logs')
        logs_parser.add_argument('service_name', nargs='?', help='Service name (optional)')
        logs_parser.add_argument('--lines', type=int, default=50, help='Number of lines to show')
        
        # services health
        health_parser = services_subparsers.add_parser('health', help='Check service health')
        health_parser.add_argument('service_name', nargs='?', help='Service name (optional)')
    
    def _add_cache_commands(self, subparsers):
        """Add cache commands"""
        cache_parser = subparsers.add_parser('cache', help='Cache operations')
        cache_subparsers = cache_parser.add_subparsers(dest='cache_command', help='Cache commands')
        
        cache_subparsers.add_parser('clear', help='Clear all caches')
        cache_subparsers.add_parser('status', help='Show cache status and statistics')
        cache_subparsers.add_parser('warm', help='Pre-warm caches')
        
        # Memcached subcommands
        memcached_parser = cache_subparsers.add_parser('memcached', help='Memcached operations')
        memcached_subparsers = memcached_parser.add_subparsers(dest='memcached_command', help='Memcached commands')
        
        memcached_subparsers.add_parser('status', help='Check Memcached connection status')
        memcached_subparsers.add_parser('stats', help='Show detailed Memcached statistics')
        memcached_subparsers.add_parser('flush', help='Flush all Memcached data')
        memcached_subparsers.add_parser('restart', help='Restart Memcached service')
        memcached_subparsers.add_parser('test', help='Test Memcached connection')
        
        cache_subparsers.add_parser('distributed', help='Show distributed cache status')
        
        # Redis subcommands
        redis_parser = cache_subparsers.add_parser('redis', help='Redis operations')
        redis_subparsers = redis_parser.add_subparsers(dest='redis_command', help='Redis commands')
        
        redis_subparsers.add_parser('info', help='Show Redis information and statistics')
    
    def _add_config_commands(self, subparsers):
        """Add configuration commands"""
        config_parser = subparsers.add_parser('config', help='Configuration operations')
        config_subparsers = config_parser.add_subparsers(dest='config_command', help='Configuration commands')
        
        # config get
        get_parser = config_subparsers.add_parser('get', help='Get configuration value by path')
        get_parser.add_argument('key_path', help='Configuration key path')
        get_parser.add_argument('dir', nargs='?', help='Directory to search (optional)')
        
        # config check
        check_parser = config_subparsers.add_parser('check', help='Check configuration hierarchy')
        check_parser.add_argument('path', nargs='?', help='Path to check (optional)')
        
        # config validate
        validate_parser = config_subparsers.add_parser('validate', help='Validate entire configuration chain')
        validate_parser.add_argument('path', nargs='?', help='Path to validate (optional)')
        
        # config compile
        compile_parser = config_subparsers.add_parser('compile', help='Auto-compile all peanu.tsk files')
        compile_parser.add_argument('path', nargs='?', help='Path to compile (optional)')
        
        # config docs
        docs_parser = config_subparsers.add_parser('docs', help='Generate configuration documentation')
        docs_parser.add_argument('path', nargs='?', help='Path for docs (optional)')
        
        # config clear-cache
        clear_cache_parser = config_subparsers.add_parser('clear-cache', help='Clear configuration cache')
        clear_cache_parser.add_argument('path', nargs='?', help='Path to clear cache for (optional)')
        
        # config stats
        config_subparsers.add_parser('stats', help='Show configuration performance statistics')
        
        # config set
        set_parser = config_subparsers.add_parser('set', help='Set configuration value')
        set_parser.add_argument('key_path', help='Configuration key path')
        set_parser.add_argument('value', help='Value to set')
        set_parser.add_argument('--dir', help='Directory to use for configuration')
        
        # config list
        list_parser = config_subparsers.add_parser('list', help='List configuration sections')
        list_parser.add_argument('section', nargs='?', help='Section to list (optional)')
        list_parser.add_argument('--dir', help='Directory to use for configuration')
        
        # config export
        export_parser = config_subparsers.add_parser('export', help='Export configuration to JSON')
        export_parser.add_argument('--output', help='Output file path')
        export_parser.add_argument('--dir', help='Directory to use for configuration')
        
        # config import
        import_parser = config_subparsers.add_parser('import', help='Import configuration from JSON')
        import_parser.add_argument('input_file', help='Input JSON file')
        import_parser.add_argument('--dir', help='Directory to use for configuration')
        
        # config merge
        merge_parser = config_subparsers.add_parser('merge', help='Merge configuration files')
        merge_parser.add_argument('source_file', help='Source configuration file')
        merge_parser.add_argument('--target-file', help='Target file for merged configuration')
        merge_parser.add_argument('--dir', help='Directory to use for configuration')
    
    def _add_security_commands(self, subparsers):
        """Add security commands"""
        security_parser = subparsers.add_parser('security', help='Security operations')
        security_subparsers = security_parser.add_subparsers(dest='security_command', help='Security commands')
        
        # security auth
        auth_parser = security_subparsers.add_parser('auth', help='Authentication operations')
        auth_subparsers = auth_parser.add_subparsers(dest='auth_command', help='Auth commands')
        
        # security auth login
        login_parser = auth_subparsers.add_parser('login', help='Authenticate user')
        login_parser.add_argument('username', help='Username')
        login_parser.add_argument('password', help='Password')
        
        # security auth logout
        auth_subparsers.add_parser('logout', help='Logout user')
        
        # security auth status
        auth_subparsers.add_parser('status', help='Check authentication status')
        
        # security auth refresh
        auth_subparsers.add_parser('refresh', help='Refresh authentication token')
        
        # security scan
        scan_parser = security_subparsers.add_parser('scan', help='Security vulnerability scan')
        scan_parser.add_argument('path', nargs='?', default='.', help='Path to scan (default: current directory)')
        
        # security encrypt
        encrypt_parser = security_subparsers.add_parser('encrypt', help='Encrypt file')
        encrypt_parser.add_argument('file', help='File to encrypt')
        
        # security decrypt
        decrypt_parser = security_subparsers.add_parser('decrypt', help='Decrypt file')
        decrypt_parser.add_argument('file', help='File to decrypt')
        
        # security audit
        audit_parser = security_subparsers.add_parser('audit', help='Security audit')
        audit_parser.add_argument('--hours', type=int, default=24, help='Hours to audit (default: 24)')
        
        # security hash
        hash_parser = security_subparsers.add_parser('hash', help='Generate hash')
        hash_parser.add_argument('--file', help='File to hash')
        hash_parser.add_argument('--string', help='String to hash')
        hash_parser.add_argument('--algorithm', choices=['md5', 'sha1', 'sha256', 'sha512'], default='sha256', help='Hash algorithm')
    
    def _add_binary_commands(self, subparsers):
        """Add binary performance commands"""
        binary_parser = subparsers.add_parser('binary', help='Binary operations')
        binary_subparsers = binary_parser.add_subparsers(dest='binary_command', help='Binary commands')
        
        # binary compile
        compile_parser = binary_subparsers.add_parser('compile', help='Compile to binary format (.tskb)')
        compile_parser.add_argument('file', help='TSK file to compile')
        
        # binary execute
        execute_parser = binary_subparsers.add_parser('execute', help='Execute binary file directly')
        execute_parser.add_argument('file', help='Binary file to execute')
        
        # binary benchmark
        benchmark_parser = binary_subparsers.add_parser('benchmark', help='Compare binary vs text performance')
        benchmark_parser.add_argument('file', help='File to benchmark')
        
        # binary optimize
        optimize_parser = binary_subparsers.add_parser('optimize', help='Optimize binary for production')
        optimize_parser.add_argument('file', help='Binary file to optimize')
        
        # binary info
        info_parser = binary_subparsers.add_parser('info', help='Display comprehensive binary file information')
        info_parser.add_argument('file', help='Binary file to analyze')
        
        # binary validate
        validate_parser = binary_subparsers.add_parser('validate', help='Validate binary format and integrity')
        validate_parser.add_argument('file', help='Binary file to validate')
        
        # binary extract
        extract_parser = binary_subparsers.add_parser('extract', help='Extract source code and data from binary files')
        extract_parser.add_argument('file', help='Binary file to extract from')
        extract_parser.add_argument('--output', '-o', help='Output directory for extracted files')
        
        # binary convert
        convert_parser = binary_subparsers.add_parser('convert', help='Convert between binary and text formats')
        convert_parser.add_argument('input', help='Input file to convert')
        convert_parser.add_argument('output', help='Output file path')
        convert_parser.add_argument('--type', choices=['auto', 'binary_to_text', 'text_to_binary', 'format_conversion'], 
                                  default='auto', help='Conversion type')
    
    def _add_ai_commands(self, subparsers):
        """Add AI commands"""
        ai_parser = subparsers.add_parser('ai', help='AI operations')
        ai_subparsers = ai_parser.add_subparsers(dest='ai_command', help='AI commands')
        
        # ai claude
        claude_parser = ai_subparsers.add_parser('claude', help='Query Claude AI with prompt')
        claude_parser.add_argument('prompt', help='Prompt to send to Claude')
        
        # ai chatgpt
        chatgpt_parser = ai_subparsers.add_parser('chatgpt', help='Query ChatGPT with prompt')
        chatgpt_parser.add_argument('prompt', help='Prompt to send to ChatGPT')
        
        # ai custom
        custom_parser = ai_subparsers.add_parser('custom', help='Query custom AI API endpoint')
        custom_parser.add_argument('api', help='API endpoint')
        custom_parser.add_argument('prompt', help='Prompt to send')
        
        # ai config
        ai_subparsers.add_parser('config', help='Show current AI configuration')
        
        # ai setup
        ai_subparsers.add_parser('setup', help='Interactive AI API key setup')
        
        # ai test
        ai_subparsers.add_parser('test', help='Test all configured AI connections')
        
        # ai complete
        complete_parser = ai_subparsers.add_parser('complete', help='Get AI-powered auto-completion')
        complete_parser.add_argument('file', help='File to complete')
        complete_parser.add_argument('line', nargs='?', type=int, help='Line number (optional)')
        complete_parser.add_argument('column', nargs='?', type=int, help='Column number (optional)')
        
        # ai analyze
        analyze_parser = ai_subparsers.add_parser('analyze', help='Analyze file for errors and improvements')
        analyze_parser.add_argument('file', help='File to analyze')
        
        # ai optimize
        optimize_parser = ai_subparsers.add_parser('optimize', help='Get performance optimization suggestions')
        optimize_parser.add_argument('file', help='File to optimize')
        
        # ai security
        security_parser = ai_subparsers.add_parser('security', help='Scan for security vulnerabilities')
        security_parser.add_argument('file', help='File to scan')
        
        # ai models
        models_parser = ai_subparsers.add_parser('models', help='List available AI models and capabilities')
        models_parser.add_argument('--service', help='Filter by service (openai, anthropic, etc.)')
        
        # ai usage
        usage_parser = ai_subparsers.add_parser('usage', help='Show AI usage statistics and costs')
        usage_parser.add_argument('--days', type=int, default=30, help='Number of days to show (default: 30)')
        
        # ai cache
        cache_parser = ai_subparsers.add_parser('cache', help='Manage AI response cache')
        cache_parser.add_argument('--clear', action='store_true', help='Clear cache entries')
        cache_parser.add_argument('--service', help='Service to clear cache for')
        cache_parser.add_argument('--older-than-days', type=int, help='Clear entries older than N days')
        
        # ai benchmark
        benchmark_parser = ai_subparsers.add_parser('benchmark', help='Benchmark AI model performance')
        benchmark_parser.add_argument('--service', required=True, help='AI service to benchmark')
        benchmark_parser.add_argument('--model', help='Model to benchmark (default: gpt-4)')
        benchmark_parser.add_argument('--prompts', help='Comma-separated test prompts')
        
        # ai rotate
        rotate_parser = ai_subparsers.add_parser('rotate', help='Rotate API keys')
        rotate_parser.add_argument('--service', required=True, help='Service to rotate key for')
        rotate_parser.add_argument('--reason', help='Reason for rotation')
        
        # ai clear
        clear_parser = ai_subparsers.add_parser('clear', help='Clear AI cache and usage data')
        clear_parser.add_argument('--cache', action='store_true', help='Clear AI cache')
        clear_parser.add_argument('--usage', action='store_true', help='Clear usage statistics')
        clear_parser.add_argument('--all', action='store_true', help='Clear both cache and usage')
    
    def _add_peanuts_commands(self, subparsers):
        """Add peanuts commands"""
        peanuts_parser = subparsers.add_parser('peanuts', help='Peanut configuration operations')
        peanuts_subparsers = peanuts_parser.add_subparsers(dest='peanuts_command', help='Peanuts commands')
        
        # peanuts compile
        compile_parser = peanuts_subparsers.add_parser('compile', help='Compile .peanuts to binary .pnt format')
        compile_parser.add_argument('file', help='Peanuts file to compile')
        
        # peanuts auto-compile
        auto_compile_parser = peanuts_subparsers.add_parser('auto-compile', help='Auto-compile all .peanuts files')
        auto_compile_parser.add_argument('path', nargs='?', help='Path to compile (optional)')
        
        # peanuts load
        load_parser = peanuts_subparsers.add_parser('load', help='Load and display binary peanuts file')
        load_parser.add_argument('file', help='Binary file to load')
    
    def _add_css_commands(self, subparsers):
        """Add CSS commands"""
        css_parser = subparsers.add_parser('css', help='CSS utilities')
        css_subparsers = css_parser.add_subparsers(dest='css_command', help='CSS commands')
        
        # css expand
        expand_parser = css_subparsers.add_parser('expand', help='Expand CSS shorthand properties')
        expand_parser.add_argument('file', help='CSS file to expand')
        
        # css map
        map_parser = css_subparsers.add_parser('map', help='Generate CSS source maps')
        map_parser.add_argument('file', help='CSS file to map')
    
    def _add_license_commands(self, subparsers):
        """Add license commands"""
        license_parser = subparsers.add_parser('license', help='License management')
        license_subparsers = license_parser.add_subparsers(dest='license_command', help='License commands')
        
        # license check
        license_subparsers.add_parser('check', help='Check license status')
        
        # license activate
        activate_parser = license_subparsers.add_parser('activate', help='Activate license')
        activate_parser.add_argument('key', help='License key')
        
        # license validate
        validate_parser = license_subparsers.add_parser('validate', help='Validate license with cryptographic verification')
        validate_parser.add_argument('key', nargs='?', help='License key to validate (optional, uses current if not provided)')
        
        # license info
        license_subparsers.add_parser('info', help='Show detailed license information')
        
        # license transfer
        transfer_parser = license_subparsers.add_parser('transfer', help='Transfer license to another system')
        transfer_parser.add_argument('target_system', help='Target system identifier')
        
        # license revoke
        revoke_parser = license_subparsers.add_parser('revoke', help='Revoke current license')
        revoke_parser.add_argument('--force', action='store_true', help='Force revocation without confirmation')
    
    def _add_utility_commands(self, subparsers):
        """Add utility commands"""
        # parse
        parse_parser = subparsers.add_parser('parse', help='Parse and display TSK file contents')
        parse_parser.add_argument('file', help='TSK file to parse')
        
        # validate
        validate_parser = subparsers.add_parser('validate', help='Validate TSK file syntax')
        validate_parser.add_argument('file', help='TSK file to validate')
        
        # convert
        convert_parser = subparsers.add_parser('convert', help='Convert between formats')
        convert_parser.add_argument('-i', '--input', required=True, help='Input file')
        convert_parser.add_argument('-o', '--output', required=True, help='Output file')
        
        # get
        get_parser = subparsers.add_parser('get', help='Get specific value by key path')
        get_parser.add_argument('file', help='TSK file')
        get_parser.add_argument('key_path', help='Key path')
        
        # set
        set_parser = subparsers.add_parser('set', help='Set value by key path')
        set_parser.add_argument('file', help='TSK file')
        set_parser.add_argument('key_path', help='Key path')
        set_parser.add_argument('value', help='Value to set')
        
        # version
        subparsers.add_parser('version', help='Show version information')
        
        # help
        help_parser = subparsers.add_parser('help', help='Show help for command')
        help_parser.add_argument('command', nargs='?', help='Command to get help for')
    
    def _add_dependency_commands(self, subparsers):
        """Add dependency management commands"""
        deps_parser = subparsers.add_parser('deps', help='Dependency management')
        deps_subparsers = deps_parser.add_subparsers(dest='deps_command', help='Dependency commands')
        
        # deps install
        install_parser = deps_subparsers.add_parser('install', help='Install optional dependencies')
        install_parser.add_argument('group', choices=['ai', 'data', 'analytics', 'full'], 
                                   help='Dependency group to install')
        install_parser.add_argument('--verbose', '-v', action='store_true', help='Verbose output')
        
        # deps list
        deps_subparsers.add_parser('list', help='List available dependency groups')
        
        # deps check
        deps_subparsers.add_parser('check', help='Check installed dependencies')
    
    def run(self, args=None):
        """Run the CLI with given arguments"""
        if args is None:
            args = sys.argv[1:]
        
        # Handle no arguments (interactive mode)
        if not args:
            return self._interactive_mode()
        
        parsed_args = self.parser.parse_args(args)
        
        # Set global flags
        self.verbose = parsed_args.verbose
        self.quiet = parsed_args.quiet
        self.json_output = parsed_args.json
        self.config_path = parsed_args.config
        
        try:
            # Route to appropriate command handler
            if parsed_args.command == 'db':
                return db_commands.handle_db_command(parsed_args, self)
            elif parsed_args.command == 'serve':
                return dev_commands.handle_serve_command(parsed_args, self)
            elif parsed_args.command == 'web':
                return dev_commands.handle_web_command(parsed_args, self)
            elif parsed_args.command == 'compile':
                return dev_commands.handle_compile_command(parsed_args, self)
            elif parsed_args.command == 'optimize':
                return dev_commands.handle_optimize_command(parsed_args, self)
            elif parsed_args.command == 'test':
                return test_commands.handle_test_command(parsed_args, self)
            elif parsed_args.command == 'services':
                return service_commands.handle_service_command(parsed_args, self)
            elif parsed_args.command == 'cache':
                return cache_commands.handle_cache_command(parsed_args, self)
            elif parsed_args.command == 'config':
                return config_commands.handle_config_command(parsed_args, self)
            elif parsed_args.command == 'security':
                return security_commands.handle_security_command(parsed_args, self)
            elif parsed_args.command == 'binary':
                return binary_commands.handle_binary_command(parsed_args, self)
            elif parsed_args.command == 'peanuts':
                return peanuts_commands.handle_peanuts_command(parsed_args, self)
            elif parsed_args.command == 'ai':
                return ai_commands.handle_ai_command(parsed_args, self)
            elif parsed_args.command == 'css':
                return css_commands.handle_css_command(parsed_args, self)
            elif parsed_args.command == 'license':
                return license_commands.handle_license_command(parsed_args, self)
            elif parsed_args.command == 'deps':
                return dependency_commands.handle_dependency_command(parsed_args, self)
            elif parsed_args.command in ['parse', 'validate', 'convert', 'get', 'set', 'version', 'help']:
                return utility_commands.handle_utility_command(parsed_args, self)
            else:
                self.parser.print_help()
                return 1
                
        except Exception as e:
            return error_handler.handle_error(e, self)
    
    def _interactive_mode(self):
        """Enter interactive REPL mode"""
        print("TuskLang v2.0.0 - Interactive Mode")
        print("Type 'exit' to quit, 'help' for commands")
        
        while True:
            try:
                command = input("tsk> ").strip()
                if command.lower() in ['exit', 'quit']:
                    break
                elif command.lower() == 'help':
                    print("Available commands: db status, serve, test, config get, etc.")
                    continue
                elif not command:
                    continue
                
                # Parse and execute command
                args = command.split()
                result = self.run(args)
                if result != 0:
                    break
                    
            except KeyboardInterrupt:
                print("\nExiting...")
                break
            except Exception as e:
                print(f"Error: {e}")
        
        return 0


def main():
    """Main entry point"""
    cli = TuskLangCLI()
    return cli.run()


if __name__ == '__main__':
    sys.exit(main()) 