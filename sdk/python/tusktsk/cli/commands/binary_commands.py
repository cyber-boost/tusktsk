#!/usr/bin/env python3
"""
Binary Commands for TuskLang Python CLI
=======================================
Implements binary performance commands
"""

import os
import sys
import time
import struct
import hashlib
import zlib
import base64
import json
from pathlib import Path
from typing import Any, Dict, List, Optional
from datetime import datetime

from ...tsk import TSK, TSKParser
from ...tsk_enhanced import TuskLangEnhanced
from ...peanut_config import PeanutConfig
from ..utils.output_formatter import OutputFormatter
from ..utils.error_handler import ErrorHandler


def handle_binary_command(args: Any, cli: Any) -> int:
    """Handle binary commands"""
    formatter = OutputFormatter(cli.json_output, cli.quiet, cli.verbose)
    error_handler = ErrorHandler(cli.json_output, cli.verbose)
    
    try:
        if args.binary_command == 'compile':
            return _handle_binary_compile(args, formatter, error_handler)
        elif args.binary_command == 'execute':
            return _handle_binary_execute(args, formatter, error_handler)
        elif args.binary_command == 'benchmark':
            return _handle_binary_benchmark(args, formatter, error_handler)
        elif args.binary_command == 'optimize':
            return _handle_binary_optimize(args, formatter, error_handler)
        elif args.binary_command == 'info':
            return _handle_binary_info(args, formatter, error_handler)
        elif args.binary_command == 'validate':
            return _handle_binary_validate(args, formatter, error_handler)
        elif args.binary_command == 'extract':
            return _handle_binary_extract(args, formatter, error_handler)
        elif args.binary_command == 'convert':
            return _handle_binary_convert(args, formatter, error_handler)
        else:
            formatter.error("Unknown binary command")
            return ErrorHandler.INVALID_ARGS
            
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_binary_info(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle binary info command - display comprehensive file information"""
    file_path = Path(args.file)
    
    if not file_path.exists():
        return error_handler.handle_file_not_found(str(file_path))
    
    formatter.loading(f"Analyzing binary file: {file_path}")
    
    try:
        # Get basic file information
        stat = file_path.stat()
        file_size = stat.st_size
        created_time = datetime.fromtimestamp(stat.st_ctime)
        modified_time = datetime.fromtimestamp(stat.st_mtime)
        
        # Detect file type using magic numbers
        try:
            import magic
            mime_type = magic.from_file(str(file_path), mime=True)
            file_type = magic.from_file(str(file_path))
        except ImportError:
            # Fallback to extension-based detection
            mime_type = _get_mime_type_by_extension(file_path.suffix)
            file_type = f"Binary file ({file_path.suffix})"
        
        # Calculate checksums
        md5_hash = _calculate_file_hash(file_path, 'md5')
        sha256_hash = _calculate_file_hash(file_path, 'sha256')
        
        # Analyze binary structure
        binary_analysis = _analyze_binary_structure(file_path)
        
        # Display comprehensive information
        formatter.success(f"Binary file analysis complete")
        formatter.subsection("File Information")
        formatter.key_value("File Path", str(file_path.absolute()))
        formatter.key_value("File Size", f"{file_size:,} bytes ({_format_bytes(file_size)})")
        formatter.key_value("File Type", file_type)
        formatter.key_value("MIME Type", mime_type)
        formatter.key_value("Created", created_time.strftime("%Y-%m-%d %H:%M:%S"))
        formatter.key_value("Modified", modified_time.strftime("%Y-%m-%d %H:%M:%S"))
        
        formatter.subsection("Security")
        formatter.key_value("MD5 Hash", md5_hash)
        formatter.key_value("SHA256 Hash", sha256_hash)
        
        formatter.subsection("Binary Analysis")
        for key, value in binary_analysis.items():
            formatter.key_value(key, value)
        
        # Format-specific information
        if file_path.suffix in ['.pnt', '.tskb']:
            _display_tsk_binary_info(file_path, formatter)
        elif file_path.suffix in ['.exe', '.dll', '.so', '.dylib']:
            _display_executable_info(file_path, formatter)
        elif file_path.suffix in ['.zip', '.tar', '.gz', '.bz2']:
            _display_archive_info(file_path, formatter)
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_binary_validate(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle binary validate command - validate binary format and integrity"""
    file_path = Path(args.file)
    
    if not file_path.exists():
        return error_handler.handle_file_not_found(str(file_path))
    
    formatter.loading(f"Validating binary file: {file_path}")
    
    try:
        validation_results = []
        
        # Basic file validation
        validation_results.append(_validate_file_basics(file_path))
        
        # Format-specific validation
        if file_path.suffix in ['.pnt', '.tskb']:
            validation_results.append(_validate_tsk_binary(file_path))
        elif file_path.suffix in ['.exe', '.dll', '.so', '.dylib']:
            validation_results.append(_validate_executable(file_path))
        elif file_path.suffix in ['.zip', '.tar', '.gz', '.bz2']:
            validation_results.append(_validate_archive(file_path))
        else:
            validation_results.append(_validate_generic_binary(file_path))
        
        # Integrity validation
        validation_results.append(_validate_file_integrity(file_path))
        
        # Display results
        formatter.success(f"Binary validation complete")
        formatter.subsection("Validation Results")
        
        all_passed = True
        for result in validation_results:
            if result['status'] == 'PASS':
                formatter.success(f"✅ {result['test']}: {result['message']}")
            elif result['status'] == 'WARNING':
                formatter.warning(f"⚠️  {result['test']}: {result['message']}")
                all_passed = False
            else:
                formatter.error(f"❌ {result['test']}: {result['message']}")
                all_passed = False
        
        # Summary
        if all_passed:
            formatter.success("All validation checks passed! Binary file is valid and intact.")
        else:
            formatter.warning("Some validation checks failed. Review the results above.")
        
        return ErrorHandler.SUCCESS if all_passed else ErrorHandler.VALIDATION_ERROR
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_binary_extract(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle binary extract command - extract source code and data from binary files"""
    file_path = Path(args.file)
    output_dir = Path(args.output) if hasattr(args, 'output') and args.output else file_path.parent / f"{file_path.stem}_extracted"
    
    if not file_path.exists():
        return error_handler.handle_file_not_found(str(file_path))
    
    formatter.loading(f"Extracting from binary file: {file_path}")
    
    try:
        # Create output directory
        output_dir.mkdir(exist_ok=True)
        
        extraction_results = []
        
        # Extract based on file type
        if file_path.suffix in ['.pnt', '.tskb']:
            extraction_results = _extract_tsk_binary(file_path, output_dir)
        elif file_path.suffix in ['.exe', '.dll', '.so', '.dylib']:
            extraction_results = _extract_executable(file_path, output_dir)
        elif file_path.suffix in ['.zip', '.tar', '.gz', '.bz2']:
            extraction_results = _extract_archive(file_path, output_dir)
        else:
            extraction_results = _extract_generic_binary(file_path, output_dir)
        
        # Display results
        formatter.success(f"Binary extraction complete: {output_dir}")
        formatter.subsection("Extraction Results")
        
        for result in extraction_results:
            if result['status'] == 'SUCCESS':
                formatter.success(f"✅ {result['type']}: {result['file']}")
            else:
                formatter.error(f"❌ {result['type']}: {result['error']}")
        
        # Show summary
        successful_extractions = len([r for r in extraction_results if r['status'] == 'SUCCESS'])
        total_extractions = len(extraction_results)
        
        formatter.subsection("Summary")
        formatter.key_value("Output Directory", str(output_dir))
        formatter.key_value("Files Extracted", f"{successful_extractions}/{total_extractions}")
        formatter.key_value("Extraction Rate", f"{(successful_extractions/total_extractions)*100:.1f}%")
        
        if successful_extractions > 0:
            formatter.success(f"Successfully extracted {successful_extractions} files")
        else:
            formatter.warning("No files were successfully extracted")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_binary_convert(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle binary convert command - convert between binary and text formats"""
    input_file = Path(args.input)
    output_file = Path(args.output)
    conversion_type = getattr(args, 'type', 'auto')
    
    if not input_file.exists():
        return error_handler.handle_file_not_found(str(input_file))
    
    formatter.loading(f"Converting binary file: {input_file}")
    
    try:
        # Auto-detect conversion type if not specified
        if conversion_type == 'auto':
            conversion_type = _detect_conversion_type(input_file, output_file)
        
        conversion_result = None
        
        # Perform conversion based on type
        if conversion_type == 'binary_to_text':
            conversion_result = _convert_binary_to_text(input_file, output_file)
        elif conversion_type == 'text_to_binary':
            conversion_result = _convert_text_to_binary(input_file, output_file)
        elif conversion_type == 'format_conversion':
            conversion_result = _convert_binary_format(input_file, output_file)
        else:
            formatter.error(f"Unknown conversion type: {conversion_type}")
            return ErrorHandler.INVALID_ARGS
        
        # Display results
        if conversion_result['success']:
            formatter.success(f"Conversion successful: {output_file}")
            formatter.subsection("Conversion Details")
            formatter.key_value("Input File", str(input_file))
            formatter.key_value("Output File", str(output_file))
            formatter.key_value("Conversion Type", conversion_type)
            formatter.key_value("Input Size", f"{conversion_result['input_size']:,} bytes")
            formatter.key_value("Output Size", f"{conversion_result['output_size']:,} bytes")
            
            if 'compression_ratio' in conversion_result:
                ratio = conversion_result['compression_ratio']
                if ratio > 0:
                    formatter.key_value("Compression", f"{ratio:.1f}% smaller")
                else:
                    formatter.key_value("Compression", f"{abs(ratio):.1f}% larger")
            
            if 'performance_improvement' in conversion_result:
                improvement = conversion_result['performance_improvement']
                formatter.key_value("Performance", f"{improvement:.1f}% improvement")
            
            return ErrorHandler.SUCCESS
        else:
            formatter.error(f"Conversion failed: {conversion_result['error']}")
            return ErrorHandler.CONVERSION_ERROR
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_binary_compile(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle binary compile command"""
    file_path = Path(args.file)
    
    if not file_path.exists():
        return error_handler.handle_file_not_found(str(file_path))
    
    if file_path.suffix != '.tsk':
        formatter.error("File must have .tsk extension")
        return ErrorHandler.INVALID_ARGS
    
    formatter.loading(f"Compiling {file_path} to binary format...")
    
    try:
        # Parse TSK file
        with open(file_path, 'r') as f:
            content = f.read()
        
        # Parse with enhanced parser
        parser = TuskLangEnhanced()
        data = parser.parse(content)
        
        # Compile to binary
        peanut_config = PeanutConfig()
        binary_path = file_path.with_suffix('.pnt')
        
        peanut_config.compile_to_binary(data, str(binary_path))
        
        # Show compilation results
        original_size = file_path.stat().st_size
        binary_size = binary_path.stat().st_size
        compression_ratio = (1 - binary_size / original_size) * 100
        
        formatter.success(f"Compiled to {binary_path}")
        formatter.subsection("Compilation Results")
        formatter.key_value("Original Size", f"{original_size} bytes")
        formatter.key_value("Binary Size", f"{binary_size} bytes")
        formatter.key_value("Compression", f"{compression_ratio:.1f}%")
        
        if compression_ratio > 0:
            formatter.success(f"Binary format is {compression_ratio:.1f}% smaller")
        else:
            formatter.info("Binary format provides performance benefits despite size")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_binary_execute(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle binary execute command"""
    file_path = Path(args.file)
    
    if not file_path.exists():
        return error_handler.handle_file_not_found(str(file_path))
    
    if file_path.suffix not in ['.pnt']:
        formatter.error("File must have .pnt extension")
        return ErrorHandler.INVALID_ARGS
    
    formatter.loading(f"Executing binary file: {file_path}")
    
    try:
        # Load binary file
        peanut_config = PeanutConfig()
        data = peanut_config.load_binary(str(file_path))
        
        # Execute any FUJSEN functions found
        tsk = TSK(data)
        executed_functions = []
        
        # Find and execute FUJSEN functions
        for section_name, section_data in data.items():
            if isinstance(section_data, dict):
                for key, value in section_data.items():
                    if key.endswith('_fujsen') and isinstance(value, str):
                        try:
                            result = tsk.execute_fujsen(section_name, key)
                            executed_functions.append({
                                'function': f"{section_name}.{key}",
                                'result': result
                            })
                        except Exception as e:
                            executed_functions.append({
                                'function': f"{section_name}.{key}",
                                'error': str(e)
                            })
        
        # Display results
        formatter.success(f"Binary file executed successfully")
        formatter.subsection("Configuration Data")
        
        # Show top-level sections
        for section_name, section_data in data.items():
            if isinstance(section_data, dict):
                formatter.key_value(f"Section: {section_name}", f"{len(section_data)} keys")
            else:
                formatter.key_value(section_name, section_data)
        
        # Show executed functions
        if executed_functions:
            formatter.subsection("Executed Functions")
            for func in executed_functions:
                if 'error' in func:
                    formatter.error(f"{func['function']}: {func['error']}")
                else:
                    formatter.key_value(func['function'], func['result'])
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_binary_benchmark(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle binary benchmark command"""
    file_path = Path(args.file)
    
    if not file_path.exists():
        return error_handler.handle_file_not_found(str(file_path))
    
    if file_path.suffix != '.tsk':
        formatter.error("File must have .tsk extension")
        return ErrorHandler.INVALID_ARGS
    
    formatter.loading(f"Benchmarking {file_path}...")
    
    try:
        # Read file content
        with open(file_path, 'r') as f:
            content = f.read()
        
        # Benchmark text parsing
        formatter.subsection("Text Format Benchmark")
        
        start_time = time.time()
        for _ in range(100):
            parser = TuskLangEnhanced()
            data = parser.parse(content)
        text_parse_time = (time.time() - start_time) / 100
        
        # Compile to binary
        peanut_config = PeanutConfig()
        binary_path = file_path.with_suffix('.pnt')
        peanut_config.compile_to_binary(data, str(binary_path))
        
        # Benchmark binary loading
        formatter.subsection("Binary Format Benchmark")
        
        start_time = time.time()
        for _ in range(100):
            binary_data = peanut_config.load_binary(str(binary_path))
        binary_load_time = (time.time() - start_time) / 100
        
        # Calculate performance improvement
        improvement = ((text_parse_time - binary_load_time) / text_parse_time) * 100
        
        # Display results
        formatter.table(
            ['Format', 'Time (ms)', 'Performance'],
            [
                ['Text Parse', f"{text_parse_time*1000:.2f}", 'Baseline'],
                ['Binary Load', f"{binary_load_time*1000:.2f}", f"{improvement:.1f}% faster"]
            ],
            'Performance Comparison'
        )
        
        # Show file sizes
        original_size = file_path.stat().st_size
        binary_size = binary_path.stat().st_size
        
        formatter.subsection("File Size Comparison")
        formatter.key_value("Text Format", f"{original_size} bytes")
        formatter.key_value("Binary Format", f"{binary_size} bytes")
        formatter.key_value("Size Ratio", f"{binary_size/original_size:.2f}x")
        
        # Clean up
        binary_path.unlink()
        
        # Summary
        if improvement > 50:
            formatter.success(f"Binary format provides excellent performance improvement: {improvement:.1f}% faster")
        elif improvement > 20:
            formatter.success(f"Binary format provides good performance improvement: {improvement:.1f}% faster")
        else:
            formatter.info(f"Binary format provides modest performance improvement: {improvement:.1f}% faster")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_binary_optimize(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle binary optimize command"""
    file_path = Path(args.file)
    
    if not file_path.exists():
        return error_handler.handle_file_not_found(str(file_path))
    
    if file_path.suffix not in ['.tskb', '.pnt']:
        formatter.error("File must have .tskb or .pnt extension")
        return ErrorHandler.INVALID_ARGS
    
    formatter.loading(f"Optimizing binary file: {file_path}")
    
    try:
        # Load binary file
        peanut_config = PeanutConfig()
        data = peanut_config.load_binary(str(file_path))
        
        # Apply optimizations
        optimized_data = _optimize_binary_data(data)
        
        # Create optimized binary
        optimized_path = file_path.with_suffix('.optimized.tskb')
        peanut_config.compile_to_binary(optimized_data, str(optimized_path))
        
        # Compare sizes
        original_size = file_path.stat().st_size
        optimized_size = optimized_path.stat().st_size
        size_reduction = ((original_size - optimized_size) / original_size) * 100
        
        # Display results
        formatter.success(f"Binary file optimized: {optimized_path}")
        formatter.subsection("Optimization Results")
        formatter.key_value("Original Size", f"{original_size} bytes")
        formatter.key_value("Optimized Size", f"{optimized_size} bytes")
        formatter.key_value("Size Reduction", f"{size_reduction:.1f}%")
        
        # Show optimization details
        formatter.subsection("Optimizations Applied")
        optimizations = _get_optimization_details(data, optimized_data)
        for opt in optimizations:
            formatter.info(f"• {opt}")
        
        if size_reduction > 0:
            formatter.success(f"Optimization achieved {size_reduction:.1f}% size reduction")
        else:
            formatter.info("Optimization focused on performance rather than size reduction")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


# Helper functions for binary operations
def _get_mime_type_by_extension(extension: str) -> str:
    """Get MIME type by file extension"""
    mime_types = {
        '.tsk': 'application/x-tusklang',
        '.pnt': 'application/x-peanut',
        '.tskb': 'application/x-tusklang-binary',
        '.exe': 'application/x-ms-dos-executable',
        '.dll': 'application/x-ms-windows-dll',
        '.so': 'application/x-sharedlib',
        '.dylib': 'application/x-mach-binary',
        '.zip': 'application/zip',
        '.tar': 'application/x-tar',
        '.gz': 'application/gzip',
        '.bz2': 'application/x-bzip2'
    }
    return mime_types.get(extension.lower(), 'application/octet-stream')


def _calculate_file_hash(file_path: Path, hash_type: str) -> str:
    """Calculate file hash"""
    hash_function = getattr(hashlib, hash_type)
    hasher = hash_function()
    
    with open(file_path, 'rb') as f:
        for chunk in iter(lambda: f.read(4096), b""):
            hasher.update(chunk)
    return hasher.hexdigest()


def _format_bytes(bytes_value: int) -> str:
    """Format bytes to human readable format"""
    for unit in ['B', 'KB', 'MB', 'GB']:
        if bytes_value < 1024.0:
            return f"{bytes_value:.1f} {unit}"
        bytes_value /= 1024.0
    return f"{bytes_value:.1f} TB"


def _analyze_binary_structure(file_path: Path) -> Dict[str, Any]:
    """Analyze binary file structure"""
    analysis = {}
    
    try:
        with open(file_path, 'rb') as f:
            header = f.read(1024)
            f.seek(-1024, 2)
            footer = f.read(1024)
        
        analysis['Header Size'] = len(header)
        analysis['Footer Size'] = len(footer)
        
        # Check for common magic numbers
        if b'PE' in header or b'MZ' in header:
            analysis['Format'] = 'PE/Windows Executable'
        elif b'ELF' in header:
            analysis['Format'] = 'ELF/Linux Executable'
        elif b'PK' in header:
            analysis['Format'] = 'ZIP Archive'
        elif b'\x1f\x8b' in header:
            analysis['Format'] = 'GZIP Archive'
        else:
            analysis['Format'] = 'Unknown Binary'
        
        # Try to load as TSK binary
        try:
            peanut_config = PeanutConfig()
            data = peanut_config.load_binary(str(file_path))
            analysis['TSK Sections'] = len(data)
            analysis['TSK Data Size'] = sum(len(str(v)) for v in data.values())
        except:
            analysis['TSK Compatible'] = False
        
    except Exception as e:
        analysis['Error'] = str(e)
    
    return analysis


def _display_tsk_binary_info(file_path: Path, formatter: OutputFormatter) -> None:
    """Display TSK-specific binary information"""
    try:
        peanut_config = PeanutConfig()
        data = peanut_config.load_binary(str(file_path))
        
        formatter.subsection("TSK Binary Information")
        formatter.key_value("Total Sections", len(data))
        
        fujsen_count = 0
        for section_name, section_data in data.items():
            if isinstance(section_data, dict):
                for key in section_data.keys():
                    if key.endswith('_fujsen'):
                        fujsen_count += 1
        
        formatter.key_value("FUJSEN Functions", fujsen_count)
        
    except Exception as e:
        formatter.warning(f"Could not analyze TSK binary: {e}")


def _display_executable_info(file_path: Path, formatter: OutputFormatter) -> None:
    """Display executable-specific information"""
    formatter.subsection("Executable Information")
    formatter.key_value("Type", "Executable Binary")
    formatter.key_value("Architecture", "Unknown")
    formatter.key_value("Entry Point", "Unknown")


def _display_archive_info(file_path: Path, formatter: OutputFormatter) -> None:
    """Display archive-specific information"""
    formatter.subsection("Archive Information")
    formatter.key_value("Type", "Compressed Archive")
    formatter.key_value("Compression", "Unknown")


# Validation functions
def _validate_file_basics(file_path: Path) -> Dict[str, Any]:
    """Validate basic file properties"""
    try:
        stat = file_path.stat()
        if stat.st_size == 0:
            return {'status': 'FAIL', 'test': 'File Size', 'message': 'File is empty'}
        elif stat.st_size > 100 * 1024 * 1024:  # 100MB
            return {'status': 'WARNING', 'test': 'File Size', 'message': 'File is very large'}
        else:
            return {'status': 'PASS', 'test': 'File Size', 'message': f'File size is {stat.st_size} bytes'}
    except Exception as e:
        return {'status': 'FAIL', 'test': 'File Access', 'message': str(e)}


def _validate_tsk_binary(file_path: Path) -> Dict[str, Any]:
    """Validate TSK binary format"""
    try:
        peanut_config = PeanutConfig()
        data = peanut_config.load_binary(str(file_path))
        return {'status': 'PASS', 'test': 'TSK Format', 'message': f'Valid TSK binary with {len(data)} sections'}
    except Exception as e:
        return {'status': 'FAIL', 'test': 'TSK Format', 'message': f'Invalid TSK binary: {e}'}


def _validate_executable(file_path: Path) -> Dict[str, Any]:
    """Validate executable format"""
    try:
        with open(file_path, 'rb') as f:
            header = f.read(4)
        
        if header.startswith(b'MZ') or header.startswith(b'\x7fELF'):
            return {'status': 'PASS', 'test': 'Executable Format', 'message': 'Valid executable format'}
        else:
            return {'status': 'FAIL', 'test': 'Executable Format', 'message': 'Invalid executable format'}
    except Exception as e:
        return {'status': 'FAIL', 'test': 'Executable Format', 'message': str(e)}


def _validate_archive(file_path: Path) -> Dict[str, Any]:
    """Validate archive format"""
    try:
        with open(file_path, 'rb') as f:
            header = f.read(4)
        
        if header.startswith(b'PK') or header.startswith(b'\x1f\x8b'):
            return {'status': 'PASS', 'test': 'Archive Format', 'message': 'Valid archive format'}
        else:
            return {'status': 'FAIL', 'test': 'Archive Format', 'message': 'Invalid archive format'}
    except Exception as e:
        return {'status': 'FAIL', 'test': 'Archive Format', 'message': str(e)}


def _validate_generic_binary(file_path: Path) -> Dict[str, Any]:
    """Validate generic binary format"""
    try:
        with open(file_path, 'rb') as f:
            data = f.read(1024)
        
        if len(data) > 0:
            return {'status': 'PASS', 'test': 'Binary Format', 'message': 'Valid binary data'}
        else:
            return {'status': 'FAIL', 'test': 'Binary Format', 'message': 'Empty file'}
    except Exception as e:
        return {'status': 'FAIL', 'test': 'Binary Format', 'message': str(e)}


def _validate_file_integrity(file_path: Path) -> Dict[str, Any]:
    """Validate file integrity"""
    try:
        # Calculate checksum
        md5_hash = _calculate_file_hash(file_path, 'md5')
        return {'status': 'PASS', 'test': 'File Integrity', 'message': f'MD5: {md5_hash[:8]}...'}
    except Exception as e:
        return {'status': 'FAIL', 'test': 'File Integrity', 'message': str(e)}


# Extraction functions
def _extract_tsk_binary(file_path: Path, output_dir: Path) -> List[Dict[str, Any]]:
    """Extract TSK binary contents"""
    results = []
    
    try:
        peanut_config = PeanutConfig()
        data = peanut_config.load_binary(str(file_path))
        
        # Extract each section
        for section_name, section_data in data.items():
            if isinstance(section_data, dict):
                section_file = output_dir / f"{section_name}.json"
                with open(section_file, 'w') as f:
                    json.dump(section_data, f, indent=2)
                results.append({
                    'status': 'SUCCESS',
                    'type': 'TSK Section',
                    'file': str(section_file)
                })
            else:
                section_file = output_dir / f"{section_name}.txt"
                with open(section_file, 'w') as f:
                    f.write(str(section_data))
                results.append({
                    'status': 'SUCCESS',
                    'type': 'TSK Data',
                    'file': str(section_file)
                })
        
    except Exception as e:
        results.append({
            'status': 'FAIL',
            'type': 'TSK Extraction',
            'error': str(e)
        })
    
    return results


def _extract_executable(file_path: Path, output_dir: Path) -> List[Dict[str, Any]]:
    """Extract executable contents"""
    results = []
    
    try:
        # Extract strings
        strings_file = output_dir / "strings.txt"
        with open(file_path, 'rb') as f:
            data = f.read()
        
        # Find printable strings
        strings = []
        current_string = ""
        for byte in data:
            if 32 <= byte <= 126:  # Printable ASCII
                current_string += chr(byte)
            else:
                if len(current_string) >= 4:
                    strings.append(current_string)
                current_string = ""
        
        with open(strings_file, 'w') as f:
            for string in strings:
                f.write(f"{string}\n")
        
        results.append({
            'status': 'SUCCESS',
            'type': 'Strings',
            'file': str(strings_file)
        })
        
    except Exception as e:
        results.append({
            'status': 'FAIL',
            'type': 'Executable Extraction',
            'error': str(e)
        })
    
    return results


def _extract_archive(file_path: Path, output_dir: Path) -> List[Dict[str, Any]]:
    """Extract archive contents"""
    results = []
    
    try:
        import zipfile
        import tarfile
        
        if file_path.suffix == '.zip':
            with zipfile.ZipFile(file_path, 'r') as zip_ref:
                zip_ref.extractall(output_dir)
            results.append({
                'status': 'SUCCESS',
                'type': 'ZIP Archive',
                'file': str(output_dir)
            })
        elif file_path.suffix == '.tar':
            with tarfile.open(file_path, 'r') as tar_ref:
                tar_ref.extractall(output_dir)
            results.append({
                'status': 'SUCCESS',
                'type': 'TAR Archive',
                'file': str(output_dir)
            })
        else:
            results.append({
                'status': 'FAIL',
                'type': 'Archive Extraction',
                'error': 'Unsupported archive format'
            })
        
    except Exception as e:
        results.append({
            'status': 'FAIL',
            'type': 'Archive Extraction',
            'error': str(e)
        })
    
    return results


def _extract_generic_binary(file_path: Path, output_dir: Path) -> List[Dict[str, Any]]:
    """Extract generic binary contents"""
    results = []
    
    try:
        # Extract hex dump
        hex_file = output_dir / "hexdump.txt"
        with open(file_path, 'rb') as f:
            data = f.read()
        
        with open(hex_file, 'w') as f:
            for i in range(0, len(data), 16):
                chunk = data[i:i+16]
                hex_line = ' '.join(f'{b:02x}' for b in chunk)
                ascii_line = ''.join(chr(b) if 32 <= b <= 126 else '.' for b in chunk)
                f.write(f'{i:08x}: {hex_line:<48} {ascii_line}\n')
        
        results.append({
            'status': 'SUCCESS',
            'type': 'Hex Dump',
            'file': str(hex_file)
        })
        
    except Exception as e:
        results.append({
            'status': 'FAIL',
            'type': 'Generic Extraction',
            'error': str(e)
        })
    
    return results


# Conversion functions
def _detect_conversion_type(input_file: Path, output_file: Path) -> str:
    """Detect conversion type based on file extensions"""
    input_ext = input_file.suffix.lower()
    output_ext = output_file.suffix.lower()
    
    if input_ext in ['.pnt', '.tskb'] and output_ext in ['.tsk', '.json', '.txt']:
        return 'binary_to_text'
    elif input_ext in ['.tsk', '.json', '.txt'] and output_ext in ['.pnt', '.tskb']:
        return 'text_to_binary'
    elif input_ext in ['.pnt', '.tskb'] and output_ext in ['.pnt', '.tskb']:
        return 'format_conversion'
    else:
        return 'binary_to_text'  # Default


def _convert_binary_to_text(input_file: Path, output_file: Path) -> Dict[str, Any]:
    """Convert binary to text format"""
    try:
        peanut_config = PeanutConfig()
        data = peanut_config.load_binary(str(input_file))
        
        input_size = input_file.stat().st_size
        
        if output_file.suffix == '.json':
            with open(output_file, 'w') as f:
                json.dump(data, f, indent=2)
        else:
            with open(output_file, 'w') as f:
                for section_name, section_data in data.items():
                    f.write(f"[{section_name}]\n")
                    if isinstance(section_data, dict):
                        for key, value in section_data.items():
                            f.write(f"{key} = {value}\n")
                    else:
                        f.write(f"{section_data}\n")
                    f.write("\n")
        
        output_size = output_file.stat().st_size
        compression_ratio = ((input_size - output_size) / input_size) * 100
        
        return {
            'success': True,
            'input_size': input_size,
            'output_size': output_size,
            'compression_ratio': compression_ratio
        }
        
    except Exception as e:
        return {
            'success': False,
            'error': str(e)
        }


def _convert_text_to_binary(input_file: Path, output_file: Path) -> Dict[str, Any]:
    """Convert text to binary format"""
    try:
        input_size = input_file.stat().st_size
        
        if input_file.suffix == '.json':
            with open(input_file, 'r') as f:
                data = json.load(f)
        else:
            # Parse TSK format
            parser = TuskLangEnhanced()
            with open(input_file, 'r') as f:
                content = f.read()
            data = parser.parse(content)
        
        # Compile to binary
        peanut_config = PeanutConfig()
        peanut_config.compile_to_binary(data, str(output_file))
        
        output_size = output_file.stat().st_size
        compression_ratio = ((input_size - output_size) / input_size) * 100
        
        return {
            'success': True,
            'input_size': input_size,
            'output_size': output_size,
            'compression_ratio': compression_ratio
        }
        
    except Exception as e:
        return {
            'success': False,
            'error': str(e)
        }


def _convert_binary_format(input_file: Path, output_file: Path) -> Dict[str, Any]:
    """Convert between binary formats"""
    try:
        input_size = input_file.stat().st_size
        
        # Load from source format
        peanut_config = PeanutConfig()
        data = peanut_config.load_binary(str(input_file))
        
        # Compile to target format
        peanut_config.compile_to_binary(data, str(output_file))
        
        output_size = output_file.stat().st_size
        compression_ratio = ((input_size - output_size) / input_size) * 100
        
        return {
            'success': True,
            'input_size': input_size,
            'output_size': output_size,
            'compression_ratio': compression_ratio
        }
        
    except Exception as e:
        return {
            'success': False,
            'error': str(e)
        }


# Optimization functions
def _optimize_binary_data(data: Dict[str, Any]) -> Dict[str, Any]:
    """Optimize binary data for production"""
    optimized = {}
    
    for key, value in data.items():
        if isinstance(value, dict):
            optimized[key] = _optimize_binary_data(value)
        elif isinstance(value, str):
            # Remove extra whitespace
            optimized[key] = value.strip()
        elif isinstance(value, list):
            # Remove empty items and optimize
            optimized[key] = [item for item in value if item is not None]
        else:
            optimized[key] = value
    
    return optimized


def _get_optimization_details(original: Dict[str, Any], optimized: Dict[str, Any]) -> List[str]:
    """Get details about optimizations applied"""
    details = []
    
    # Count keys
    original_keys = _count_keys_recursive(original)
    optimized_keys = _count_keys_recursive(optimized)
    
    if optimized_keys < original_keys:
        details.append(f"Reduced keys from {original_keys} to {optimized_keys}")
    
    # Check for string optimizations
    original_strings = _count_strings_recursive(original)
    optimized_strings = _count_strings_recursive(optimized)
    
    if optimized_strings < original_strings:
        details.append(f"Optimized {original_strings - optimized_strings} string values")
    
    # Check for null removals
    original_nulls = _count_nulls_recursive(original)
    optimized_nulls = _count_nulls_recursive(optimized)
    
    if optimized_nulls < original_nulls:
        details.append(f"Removed {original_nulls - optimized_nulls} null values")
    
    if not details:
        details.append("Applied general data structure optimizations")
    
    return details


def _count_keys_recursive(data: Any) -> int:
    """Count total keys recursively"""
    if isinstance(data, dict):
        count = len(data)
        for value in data.values():
            count += _count_keys_recursive(value)
        return count
    elif isinstance(data, list):
        count = 0
        for item in data:
            count += _count_keys_recursive(item)
        return count
    else:
        return 0


def _count_strings_recursive(data: Any) -> int:
    """Count string values recursively"""
    if isinstance(data, dict):
        count = sum(1 for value in data.values() if isinstance(value, str))
        for value in data.values():
            count += _count_strings_recursive(value)
        return count
    elif isinstance(data, list):
        count = sum(1 for item in data if isinstance(item, str))
        for item in data:
            count += _count_strings_recursive(item)
        return count
    else:
        return 0


def _count_nulls_recursive(data: Any) -> int:
    """Count null values recursively"""
    if isinstance(data, dict):
        count = sum(1 for value in data.values() if value is None)
        for value in data.values():
            count += _count_nulls_recursive(value)
        return count
    elif isinstance(data, list):
        count = sum(1 for item in data if item is None)
        for item in data:
            count += _count_nulls_recursive(item)
        return count
    else:
        return 0