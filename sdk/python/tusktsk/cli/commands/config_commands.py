#!/usr/bin/env python3
"""
Configuration Commands for TuskLang Python CLI
==============================================
Implements configuration-related commands
"""

import os
import sys
import time
import json
from pathlib import Path
from typing import Any, Dict, List, Optional

from ...tsk import TSK, TSKParser
from ...tsk_enhanced import TuskLangEnhanced
from ...peanut_config import PeanutConfig
from ..utils.output_formatter import OutputFormatter
from ..utils.error_handler import ErrorHandler
from ..utils.config_loader import ConfigLoader


def handle_config_command(args: Any, cli: Any) -> int:
    """Handle configuration commands"""
    formatter = OutputFormatter(cli.json_output, cli.quiet, cli.verbose)
    error_handler = ErrorHandler(cli.json_output, cli.verbose)
    config_loader = ConfigLoader(cli.config_path)
    
    try:
        if args.config_command == 'get':
            return _handle_config_get(args, formatter, error_handler, config_loader)
        elif args.config_command == 'set':
            return _handle_config_set(args, formatter, error_handler, config_loader)
        elif args.config_command == 'list':
            return _handle_config_list(args, formatter, error_handler, config_loader)
        elif args.config_command == 'export':
            return _handle_config_export(args, formatter, error_handler, config_loader)
        elif args.config_command == 'import':
            return _handle_config_import(args, formatter, error_handler, config_loader)
        elif args.config_command == 'merge':
            return _handle_config_merge(args, formatter, error_handler, config_loader)
        elif args.config_command == 'check':
            return _handle_config_check(args, formatter, error_handler, config_loader)
        elif args.config_command == 'validate':
            return _handle_config_validate(args, formatter, error_handler, config_loader)
        elif args.config_command == 'compile':
            return _handle_config_compile(args, formatter, error_handler, config_loader)
        elif args.config_command == 'docs':
            return _handle_config_docs(args, formatter, error_handler, config_loader)
        elif args.config_command == 'clear-cache':
            return _handle_config_clear_cache(args, formatter, error_handler, config_loader)
        elif args.config_command == 'stats':
            return _handle_config_stats(formatter, error_handler, config_loader)
        else:
            formatter.error("Unknown configuration command")
            return ErrorHandler.INVALID_ARGS
            
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_config_get(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config get command"""
    key_path = args.key_path
    directory = args.dir
    
    formatter.loading(f"Getting configuration value: {key_path}")
    
    try:
        value = config_loader.get_value(key_path, directory=directory)
        
        if value is None:
            formatter.warning(f"Configuration key not found: {key_path}")
            return ErrorHandler.CONFIG_ERROR
        
        # Display the value
        if isinstance(value, (dict, list)):
            formatter.key_value(key_path, json.dumps(value, indent=2))
        else:
            formatter.key_value(key_path, value)
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_config_check(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config check command"""
    path = args.path
    
    formatter.loading("Checking configuration hierarchy...")
    
    try:
        # Load configuration hierarchy
        peanut_config = PeanutConfig()
        hierarchy = peanut_config.find_config_hierarchy(path or os.getcwd())
        
        if not hierarchy:
            formatter.warning("No configuration files found in hierarchy")
            return ErrorHandler.CONFIG_ERROR
        
        # Display hierarchy
        formatter.section("Configuration Hierarchy")
        
        hierarchy_results = []
        for i, config_file in enumerate(hierarchy, 1):
            file_path = Path(config_file.path)
            relative_path = file_path.relative_to(Path.cwd()) if file_path.is_relative_to(Path.cwd()) else file_path
            
            hierarchy_results.append([
                i,
                config_file.type.upper(),
                str(relative_path),
                time.strftime('%Y-%m-%d %H:%M:%S', time.localtime(config_file.mtime))
            ])
        
        formatter.table(
            ['Order', 'Type', 'Path', 'Modified'],
            hierarchy_results,
            'Configuration Files (Root → Current)'
        )
        
        # Show inheritance explanation
        formatter.subsection("Inheritance Order")
        formatter.info("Configuration values are merged in order from root to current directory")
        formatter.info("Later files override values from earlier files (CSS-like cascading)")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_config_validate(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config validate command"""
    path = args.path
    
    formatter.loading("Validating configuration...")
    
    try:
        # Validate configuration
        validation_result = config_loader.validate_config(path)
        
        # Display validation results
        if validation_result['valid']:
            formatter.success("Configuration is valid")
        else:
            formatter.error("Configuration validation failed")
        
        # Show details
        formatter.subsection("Validation Details")
        
        # Sections
        formatter.key_value("Total Sections", validation_result['total_keys'])
        formatter.key_value("Configuration Keys", validation_result['total_keys'])
        
        # Errors
        if validation_result['errors']:
            formatter.subsection("Errors")
            for error in validation_result['errors']:
                formatter.error(error)
        
        # Warnings
        if validation_result['warnings']:
            formatter.subsection("Warnings")
            for warning in validation_result['warnings']:
                formatter.warning(warning)
        
        # Sections found
        if validation_result['sections']:
            formatter.subsection("Sections Found")
            formatter.list_items(validation_result['sections'])
        
        return ErrorHandler.SUCCESS if validation_result['valid'] else ErrorHandler.CONFIG_ERROR
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_config_compile(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config compile command"""
    path = args.path
    
    formatter.loading("Compiling configuration files...")
    
    try:
        # Compile configurations
        compile_result = config_loader.compile_configs(path)
        
        # Display results
        if compile_result['compiled']:
            formatter.success(f"Compiled {len(compile_result['compiled'])} files")
            formatter.list_items(compile_result['compiled'], "Compiled Files")
        
        if compile_result['skipped']:
            formatter.info(f"Skipped {len(compile_result['skipped'])} files")
            formatter.list_items(compile_result['skipped'], "Skipped Files")
        
        if compile_result['errors']:
            formatter.error(f"Failed to compile {len(compile_result['errors'])} files")
            formatter.list_items(compile_result['errors'], "Compilation Errors")
            return ErrorHandler.CONFIG_ERROR
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_config_docs(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config docs command"""
    path = args.path
    
    formatter.loading("Generating configuration documentation...")
    
    try:
        # Load configuration
        config = config_loader.load_config(path)
        
        # Generate documentation
        docs_path = Path(path or os.getcwd()) / 'config_docs.md'
        
        with open(docs_path, 'w') as f:
            f.write("# TuskLang Configuration Documentation\n\n")
            f.write(f"Generated: {time.strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            
            f.write("## Configuration Structure\n\n")
            _write_config_docs_recursive(config, f, 0)
            
            f.write("\n## Usage Examples\n\n")
            f.write("```bash\n")
            f.write("# Get configuration values\n")
            f.write("tsk config get database.host\n")
            f.write("tsk config get server.port\n")
            f.write("tsk config get app.name\n")
            f.write("```\n\n")
            
            f.write("## File Formats\n\n")
            f.write("- `.tsk` - TuskLang configuration format\n")
            f.write("- `.peanuts` - Simplified configuration format\n")
            f.write("- `.pnt` - Binary configuration format (compiled)\n\n")
            
            f.write("## Inheritance\n\n")
            f.write("Configuration files are loaded in hierarchy order:\n")
            f.write("1. Root directory configuration\n")
            f.write("2. Parent directory configurations\n")
            f.write("3. Current directory configuration\n\n")
            f.write("Later files override values from earlier files.\n")
        
        formatter.success(f"Configuration documentation generated: {docs_path}")
        
        # Show summary
        formatter.subsection("Documentation Summary")
        formatter.key_value("Output File", str(docs_path))
        formatter.key_value("Sections Documented", len(config))
        formatter.key_value("Total Keys", _count_config_keys(config))
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_config_clear_cache(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config clear-cache command"""
    path = args.path
    
    formatter.loading("Clearing configuration cache...")
    
    try:
        # Clear cache
        success = config_loader.clear_cache(path)
        
        if success:
            formatter.success("Configuration cache cleared successfully")
        else:
            formatter.warning("No configuration cache found to clear")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_config_stats(formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config stats command"""
    formatter.loading("Getting configuration statistics...")
    
    try:
        # Get configuration statistics
        stats = config_loader.get_config_stats()
        
        # Display statistics
        formatter.section("Configuration Performance Statistics")
        
        formatter.table(
            ['Metric', 'Value'],
            [
                ['Total Sections', stats['total_sections']],
                ['Total Keys', stats['total_keys']],
                ['File Size', f"{stats['file_size'] / 1024:.1f}KB"],
                ['Load Time', f"{stats['load_time']*1000:.2f}ms"],
                ['Hierarchy Depth', stats['hierarchy_depth']]
            ],
            'Configuration Statistics'
        )
        
        # Performance analysis
        formatter.subsection("Performance Analysis")
        
        if stats['load_time'] < 0.1:
            formatter.success("Configuration loading is fast")
        elif stats['load_time'] < 0.5:
            formatter.info("Configuration loading is acceptable")
        else:
            formatter.warning("Configuration loading is slow - consider using binary format")
        
        if stats['hierarchy_depth'] > 5:
            formatter.warning("Deep configuration hierarchy detected - consider flattening")
        
        # Recommendations
        formatter.subsection("Recommendations")
        
        if stats['file_size'] > 1024 * 1024:  # 1MB
            formatter.info("Large configuration detected - consider splitting into multiple files")
        
        if stats['total_keys'] > 1000:
            formatter.info("Many configuration keys - consider using binary format for better performance")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _write_config_docs_recursive(config: Dict[str, Any], file, level: int):
    """Write configuration documentation recursively"""
    indent = "  " * level
    
    for key, value in config.items():
        if isinstance(value, dict):
            file.write(f"{indent}- **{key}** (object)\n")
            _write_config_docs_recursive(value, file, level + 1)
        else:
            value_type = type(value).__name__
            if isinstance(value, str):
                value_str = f'"{value}"'
            elif isinstance(value, bool):
                value_str = str(value).lower()
            elif value is None:
                value_str = "null"
            else:
                value_str = str(value)
            
            file.write(f"{indent}- **{key}** ({value_type}) = {value_str}\n")


def _count_config_keys(config: Dict[str, Any]) -> int:
    """Count total number of keys in configuration"""
    count = 0
    for key, value in config.items():
        count += 1
        if isinstance(value, dict):
            count += _count_config_keys(value)
    return count


def _handle_config_set(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config set command"""
    key_path = args.key_path
    value = args.value
    directory = getattr(args, 'dir', None)
    
    formatter.loading(f"Setting configuration value: {key_path} = {value}")
    
    try:
        # Parse value based on type
        if value.lower() in ('true', 'false'):
            parsed_value = value.lower() == 'true'
        elif value.isdigit():
            parsed_value = int(value)
        elif value.replace('.', '').isdigit() and value.count('.') == 1:
            parsed_value = float(value)
        else:
            parsed_value = value
        
        # Set the value
        success = config_loader.set_value(key_path, parsed_value, directory=directory)
        
        if success:
            formatter.success(f"Configuration value set: {key_path} = {parsed_value}")
            return ErrorHandler.SUCCESS
        else:
            formatter.error(f"Failed to set configuration value: {key_path}")
            return ErrorHandler.CONFIG_ERROR
            
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_config_list(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config list command"""
    section = getattr(args, 'section', None)
    directory = getattr(args, 'dir', None)
    
    formatter.loading("Listing configuration sections...")
    
    try:
        # Get configuration data
        config_data = config_loader.get_config(directory=directory)
        
        if section:
            # List specific section
            if section in config_data:
                section_data = config_data[section]
                if isinstance(section_data, dict):
                    formatter.section(f"Configuration Section: {section}")
                    for key, value in section_data.items():
                        formatter.key_value(f"{section}.{key}", value)
                else:
                    formatter.key_value(section, section_data)
            else:
                formatter.warning(f"Configuration section not found: {section}")
                return ErrorHandler.CONFIG_ERROR
        else:
            # List all sections
            formatter.section("Configuration Sections")
            for section_name, section_data in config_data.items():
                if isinstance(section_data, dict):
                    key_count = len(section_data)
                    formatter.info(f"{section_name}: {key_count} keys")
                else:
                    formatter.info(f"{section_name}: {section_data}")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_config_export(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config export command"""
    output_file = getattr(args, 'output', 'config_export.json')
    directory = getattr(args, 'dir', None)
    
    formatter.loading(f"Exporting configuration to: {output_file}")
    
    try:
        # Get configuration data
        config_data = config_loader.get_config(directory=directory)
        
        # Export to JSON
        with open(output_file, 'w') as f:
            json.dump(config_data, f, indent=2)
        
        formatter.success(f"Configuration exported to: {output_file}")
        formatter.info(f"Total keys: {_count_config_keys(config_data)}")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_config_import(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config import command"""
    input_file = args.input_file
    directory = getattr(args, 'dir', None)
    
    formatter.loading(f"Importing configuration from: {input_file}")
    
    try:
        # Read JSON file
        with open(input_file, 'r') as f:
            import_data = json.load(f)
        
        # Import configuration
        success = config_loader.import_config(import_data, directory=directory)
        
        if success:
            formatter.success(f"Configuration imported from: {input_file}")
            formatter.info(f"Total keys imported: {_count_config_keys(import_data)}")
            return ErrorHandler.SUCCESS
        else:
            formatter.error(f"Failed to import configuration from: {input_file}")
            return ErrorHandler.CONFIG_ERROR
            
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_config_merge(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler, config_loader: ConfigLoader) -> int:
    """Handle config merge command"""
    source_file = args.source_file
    target_file = getattr(args, 'target_file', None)
    directory = getattr(args, 'dir', None)
    
    formatter.loading(f"Merging configuration from: {source_file}")
    
    try:
        # Read source configuration
        with open(source_file, 'r') as f:
            source_data = json.load(f)
        
        # Get current configuration
        current_data = config_loader.get_config(directory=directory)
        
        # Merge configurations
        merged_data = _merge_configs(current_data, source_data)
        
        # Save merged configuration
        if target_file:
            with open(target_file, 'w') as f:
                json.dump(merged_data, f, indent=2)
            formatter.success(f"Configuration merged to: {target_file}")
        else:
            # Update current configuration
            success = config_loader.import_config(merged_data, directory=directory)
            if success:
                formatter.success("Configuration merged successfully")
            else:
                formatter.error("Failed to merge configuration")
                return ErrorHandler.CONFIG_ERROR
        
        formatter.info(f"Total keys after merge: {_count_config_keys(merged_data)}")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _merge_configs(base_config: Dict[str, Any], merge_config: Dict[str, Any]) -> Dict[str, Any]:
    """Merge two configuration dictionaries"""
    result = base_config.copy()
    
    for key, value in merge_config.items():
        if key in result and isinstance(result[key], dict) and isinstance(value, dict):
            result[key] = _merge_configs(result[key], value)
        else:
            result[key] = value
    
    return result 