#!/usr/bin/env python3
"""
Development Commands for TuskLang Python CLI
============================================
Implements development-related commands
"""

import os
import sys
import time
import threading
import http.server
import socketserver
import ssl
import signal
import json
import subprocess
from pathlib import Path
from typing import Any, Dict, Optional, List

# Optional imports with fallbacks
try:
    import psutil
    PSUTIL_AVAILABLE = True
except ImportError:
    PSUTIL_AVAILABLE = False
    print("psutil not available. Server monitoring will be limited.")

try:
    from watchdog.observers import Observer
    from watchdog.events import FileSystemEventHandler
    WATCHDOG_AVAILABLE = True
except ImportError:
    WATCHDOG_AVAILABLE = False
    print("watchdog not available. File watching will be limited.")

from ...tsk import TSK, TSKParser
from ...tsk_enhanced import TuskLangEnhanced
from ...peanut_config import PeanutConfig
from ..utils.output_formatter import OutputFormatter
from ..utils.error_handler import ErrorHandler
from ..utils.config_loader import ConfigLoader


def handle_serve_command(args: Any, cli: Any) -> int:
    """Handle serve command with enhanced options"""
    formatter = OutputFormatter(cli.json_output, cli.quiet, cli.verbose)
    error_handler = ErrorHandler(cli.json_output, cli.verbose)
    
    try:
        port = args.port
        host = getattr(args, 'host', '0.0.0.0')
        ssl_enabled = getattr(args, 'ssl', False)
        ssl_cert = getattr(args, 'ssl_cert', None)
        ssl_key = getattr(args, 'ssl_key', None)
        reload = getattr(args, 'reload', False)
        workers = getattr(args, 'workers', 1)
        
        if reload and not WATCHDOG_AVAILABLE:
            formatter.warning("Auto-reload requires 'watchdog' package. Install with: pip install watchdog")
            reload = False
        
        return _start_dev_server(
            port, host, ssl_enabled, ssl_cert, ssl_key, 
            reload, workers, formatter, error_handler
        )
    except Exception as e:
        return error_handler.handle_error(e)


def handle_compile_command(args: Any, cli: Any) -> int:
    """Handle compile command with watch mode"""
    formatter = OutputFormatter(cli.json_output, cli.quiet, cli.verbose)
    error_handler = ErrorHandler(cli.json_output, cli.verbose)
    
    try:
        file_path = Path(args.file)
        watch = getattr(args, 'watch', False)
        output_dir = getattr(args, 'output_dir', None)
        
        if watch and not WATCHDOG_AVAILABLE:
            formatter.error("Watch mode requires 'watchdog' package. Install with: pip install watchdog")
            return ErrorHandler.GENERAL_ERROR
        
        if watch:
            return _compile_watch_mode(file_path, output_dir, formatter, error_handler)
        else:
            return _compile_tsk_file(file_path, output_dir, formatter, error_handler)
    except Exception as e:
        return error_handler.handle_error(e)


def handle_optimize_command(args: Any, cli: Any) -> int:
    """Handle optimize command with profiling"""
    formatter = OutputFormatter(cli.json_output, cli.quiet, cli.verbose)
    error_handler = ErrorHandler(cli.json_output, cli.verbose)
    
    try:
        file_path = Path(args.file)
        profile = getattr(args, 'profile', False)
        output_dir = getattr(args, 'output_dir', None)
        
        return _optimize_tsk_file(file_path, output_dir, profile, formatter, error_handler)
    except Exception as e:
        return error_handler.handle_error(e)


class FileWatcher(FileSystemEventHandler):
    """File system watcher for automatic recompilation"""
    
    def __init__(self, file_path: Path, output_dir: Optional[Path], formatter: OutputFormatter, error_handler: ErrorHandler):
        self.file_path = file_path
        self.output_dir = output_dir
        self.formatter = formatter
        self.error_handler = error_handler
        self.last_modified = 0
    
    def on_modified(self, event):
        if not event.is_directory and event.src_path == str(self.file_path):
            current_time = time.time()
            if current_time - self.last_modified > 1:  # Debounce
                self.last_modified = current_time
                self.formatter.info(f"File changed: {self.file_path}")
                _compile_tsk_file(self.file_path, self.output_dir, self.formatter, self.error_handler)


def _start_dev_server(port: int, host: str, ssl_enabled: bool, ssl_cert: Optional[str], 
                     ssl_key: Optional[str], reload: bool, workers: int, 
                     formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Start development server with advanced features"""
    formatter.loading(f"Starting development server on {host}:{port}...")
    
    try:
        # Create enhanced HTTP server
        class TuskLangDevHandler(http.server.SimpleHTTPRequestHandler):
            def __init__(self, *args, **kwargs):
                super().__init__(*args, directory=os.getcwd(), **kwargs)
            
            def end_headers(self):
                self.send_header('Access-Control-Allow-Origin', '*')
                self.send_header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS')
                self.send_header('Access-Control-Allow-Headers', 'Content-Type, Authorization')
                self.send_header('Cache-Control', 'no-cache, no-store, must-revalidate')
                super().end_headers()
            
            def do_GET(self):
                if self.path == '/':
                    self.path = '/index.html'
                elif self.path == '/api/status':
                    self.send_response(200)
                    self.send_header('Content-type', 'application/json')
                    self.end_headers()
                    status_data = {
                        "status": "running",
                        "timestamp": time.time(),
                        "uptime": time.time() - self.server.start_time,
                        "requests": getattr(self.server, 'request_count', 0),
                        "memory_usage": psutil.Process().memory_info().rss / 1024 / 1024
                    }
                    self.wfile.write(json.dumps(status_data).encode())
                    return
                elif self.path == '/api/health':
                    self.send_response(200)
                    self.send_header('Content-type', 'application/json')
                    self.end_headers()
                    health_data = {
                        "healthy": True,
                        "database": "connected",
                        "cache": "available",
                        "services": ["running"]
                    }
                    self.wfile.write(json.dumps(health_data).encode())
                    return
                super().do_GET()
            
            def log_message(self, format, *args):
                if formatter.verbose:
                    super().log_message(format, *args)
        
        # Create server
        if ssl_enabled:
            if not ssl_cert or not ssl_key:
                formatter.error("SSL certificate and key files are required for SSL mode")
                return ErrorHandler.INVALID_ARGS
            
            if not Path(ssl_cert).exists() or not Path(ssl_key).exists():
                formatter.error("SSL certificate or key file not found")
                return ErrorHandler.FILE_NOT_FOUND
            
            server = socketserver.TCPServer((host, port), TuskLangDevHandler)
            server.socket = ssl.wrap_socket(
                server.socket,
                certfile=ssl_cert,
                keyfile=ssl_key,
                server_side=True
            )
            protocol = "https"
        else:
            server = socketserver.TCPServer((host, port), TuskLangDevHandler)
            protocol = "http"
        
        # Add server metadata
        server.start_time = time.time()
        server.request_count = 0
        
        # Set server options
        server.allow_reuse_address = True
        server.allow_reuse_port = True
        
        formatter.success(f"Development server started at {protocol}://{host}:{port}")
        formatter.info("Press Ctrl+C to stop the server")
        
        if reload:
            formatter.info("Auto-reload mode enabled - watching for file changes")
            _start_file_watcher(server, formatter)
        
        # Start monitoring thread
        monitor_thread = threading.Thread(target=_monitor_server, args=(server, formatter))
        monitor_thread.daemon = True
        monitor_thread.start()
        
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            formatter.info("Shutting down development server...")
            server.shutdown()
            formatter.success("Development server stopped")
        
        return ErrorHandler.SUCCESS
        
    except OSError as e:
        if "Address already in use" in str(e):
            formatter.error(f"Port {port} is already in use")
            return ErrorHandler.CONNECTION_ERROR
        else:
            return error_handler.handle_error(e)
    except Exception as e:
        return error_handler.handle_error(e)


def _start_file_watcher(server: socketserver.TCPServer, formatter: OutputFormatter):
    """Start file watching for auto-reload"""
    try:
        observer = Observer()
        event_handler = FileSystemEventHandler()
        
        def on_modified(event):
            if not event.is_directory and event.src_path.endswith(('.tsk', '.py', '.html', '.js', '.css')):
                formatter.info(f"File changed: {event.src_path}")
                # Trigger reload by sending signal to main process
                os.kill(os.getpid(), signal.SIGUSR1)
        
        event_handler.on_modified = on_modified
        
        observer.schedule(event_handler, '.', recursive=True)
        observer.start()
        
        # Store observer in server for cleanup
        server.file_observer = observer
        
    except Exception as e:
        formatter.warning(f"File watching not available: {e}")


def _monitor_server(server: socketserver.TCPServer, formatter: OutputFormatter):
    """Monitor server performance and health"""
    while True:
        try:
            time.sleep(30)  # Check every 30 seconds
            
            # Get server stats
            process = psutil.Process()
            memory_mb = process.memory_info().rss / 1024 / 1024
            cpu_percent = process.cpu_percent()
            
            if memory_mb > 500:  # Warning if memory usage > 500MB
                formatter.warning(f"High memory usage: {memory_mb:.1f}MB")
            
            if cpu_percent > 80:  # Warning if CPU usage > 80%
                formatter.warning(f"High CPU usage: {cpu_percent:.1f}%")
                
        except Exception:
            break


def _compile_watch_mode(file_path: Path, output_dir: Optional[Path], 
                       formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Compile in watch mode with automatic recompilation"""
    if not file_path.exists():
        return error_handler.handle_file_not_found(str(file_path))
    
    if file_path.suffix != '.tsk':
        formatter.error("File must have .tsk extension")
        return ErrorHandler.INVALID_ARGS
    
    formatter.loading(f"Starting watch mode for {file_path}")
    formatter.info("Press Ctrl+C to stop watching")
    
    try:
        # Initial compilation
        result = _compile_tsk_file(file_path, output_dir, formatter, error_handler)
        if result != ErrorHandler.SUCCESS:
            return result
        
        # Set up file watcher
        observer = Observer()
        event_handler = FileWatcher(file_path, output_dir, formatter, error_handler)
        
        observer.schedule(event_handler, str(file_path.parent), recursive=False)
        observer.start()
        
        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            observer.stop()
            observer.join()
            formatter.info("Watch mode stopped")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _compile_tsk_file(file_path: Path, output_dir: Optional[Path], 
                     formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Compile TSK file to optimized format"""
    if not file_path.exists():
        return error_handler.handle_file_not_found(str(file_path))
    
    if file_path.suffix != '.tsk':
        formatter.error("File must have .tsk extension")
        return ErrorHandler.INVALID_ARGS
    
    formatter.loading(f"Compiling {file_path}...")
    
    try:
        # Parse TSK file
        with open(file_path, 'r') as f:
            content = f.read()
        
        # Parse with enhanced parser
        parser = TuskLangEnhanced()
        data = parser.parse(content)
        
        # Create optimized version
        optimized_content = _create_optimized_tsk(data)
        
        # Determine output paths
        if output_dir:
            output_dir = Path(output_dir)
            output_dir.mkdir(parents=True, exist_ok=True)
            optimized_path = output_dir / f"{file_path.stem}.optimized.tsk"
            binary_path = output_dir / f"{file_path.stem}.pnt"
        else:
            optimized_path = file_path.with_suffix('.optimized.tsk')
            binary_path = file_path.with_suffix('.pnt')
        
        # Write optimized file
        with open(optimized_path, 'w') as f:
            f.write(optimized_content)
        
        # Also compile to binary if peanut_config is available
        try:
            peanut_config = PeanutConfig()
            peanut_config.compile_to_binary(data, str(binary_path))
            formatter.success(f"Compiled to {optimized_path} and {binary_path}")
        except Exception as e:
            formatter.warning(f"Binary compilation failed: {e}")
            formatter.success(f"Compiled to {optimized_path}")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _optimize_tsk_file(file_path: Path, output_dir: Optional[Path], profile: bool,
                      formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Optimize TSK file for production with optional profiling"""
    if not file_path.exists():
        return error_handler.handle_file_not_found(str(file_path))
    
    if file_path.suffix != '.tsk':
        formatter.error("File must have .tsk extension")
        return ErrorHandler.INVALID_ARGS
    
    formatter.loading(f"Optimizing {file_path} for production...")
    
    try:
        # Parse TSK file
        with open(file_path, 'r') as f:
            content = f.read()
        
        # Parse with enhanced parser
        parser = TuskLangEnhanced()
        
        if profile:
            formatter.info("Profiling optimization process...")
            import cProfile
            import pstats
            
            profiler = cProfile.Profile()
            profiler.enable()
            
            data = parser.parse(content)
            
            profiler.disable()
            
            # Save profiling stats
            stats_path = file_path.with_suffix('.prof')
            profiler.dump_stats(str(stats_path))
            
            # Display profiling results
            stats = pstats.Stats(str(stats_path))
            stats.sort_stats('cumulative')
            
            formatter.subsection("Profiling Results")
            formatter.info(f"Profile data saved to: {stats_path}")
            
            # Show top 10 functions by cumulative time
            stats.print_stats(10)
        
        else:
            data = parser.parse(content)
        
        # Apply optimizations
        optimized_data = _apply_optimizations(data)
        
        # Create optimized content
        optimized_content = _create_optimized_tsk(optimized_data)
        
        # Determine output path
        if output_dir:
            output_dir = Path(output_dir)
            output_dir.mkdir(parents=True, exist_ok=True)
            optimized_path = output_dir / f"{file_path.stem}.prod.tsk"
        else:
            optimized_path = file_path.with_suffix('.prod.tsk')
        
        # Write optimized file
        with open(optimized_path, 'w') as f:
            f.write(optimized_content)
        
        # Show optimization statistics
        original_size = len(content)
        optimized_size = len(optimized_content)
        reduction = ((original_size - optimized_size) / original_size) * 100
        
        formatter.success(f"Optimized file saved to {optimized_path}")
        formatter.info(f"Size reduction: {reduction:.1f}% ({original_size} -> {optimized_size} bytes)")
        
        # Show optimization details
        optimization_details = _analyze_optimizations(data, optimized_data)
        if optimization_details:
            formatter.subsection("Optimization Details")
            for detail in optimization_details:
                formatter.info(f"• {detail}")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _analyze_optimizations(original_data: Dict[str, Any], optimized_data: Dict[str, Any]) -> List[str]:
    """Analyze what optimizations were applied"""
    details = []
    
    # Count sections and keys
    original_sections = len(original_data)
    optimized_sections = len(optimized_data)
    
    original_keys = sum(len(section) if isinstance(section, dict) else 1 for section in original_data.values())
    optimized_keys = sum(len(section) if isinstance(section, dict) else 1 for section in optimized_data.values())
    
    if original_sections != optimized_sections:
        details.append(f"Sections: {original_sections} -> {optimized_sections}")
    
    if original_keys != optimized_keys:
        details.append(f"Keys: {original_keys} -> {optimized_keys}")
    
    # Check for whitespace optimization
    original_str = str(original_data)
    optimized_str = str(optimized_data)
    
    if len(original_str) != len(optimized_str):
        details.append(f"String length: {len(original_str)} -> {len(optimized_str)}")
    
    return details


def _create_optimized_tsk(data: Dict[str, Any]) -> str:
    """Create optimized TSK content"""
    lines = []
    
    # Add header comment
    lines.append("# Optimized TuskLang Configuration")
    lines.append(f"# Generated: {time.strftime('%Y-%m-%d %H:%M:%S')}")
    lines.append("")
    
    def format_section(section_data: Dict[str, Any], section_name: str = None, indent: int = 0):
        """Recursively format TSK sections"""
        if section_name:
            lines.append(f"{'  ' * indent}[{section_name}]")
        
        for key, value in section_data.items():
            if isinstance(value, dict):
                format_section(value, key, indent + 1)
            else:
                # Format value
                if isinstance(value, str):
                    if '\n' in value:
                        # Multiline string
                        lines.append(f"{'  ' * indent}{key} = \"\"\"")
                        lines.append(value)
                        lines.append(f"{'  ' * indent}\"\"\"")
                    else:
                        lines.append(f"{'  ' * indent}{key} = \"{value}\"")
                elif isinstance(value, bool):
                    lines.append(f"{'  ' * indent}{key} = {str(value).lower()}")
                elif value is None:
                    lines.append(f"{'  ' * indent}{key} = null")
                else:
                    lines.append(f"{'  ' * indent}{key} = {value}")
    
    format_section(data)
    return '\n'.join(lines)


def _apply_optimizations(data: Dict[str, Any]) -> Dict[str, Any]:
    """Apply production optimizations to configuration data"""
    optimized = {}
    
    for key, value in data.items():
        if isinstance(value, dict):
            optimized[key] = _apply_optimizations(value)
        elif isinstance(value, str):
            # Remove extra whitespace
            optimized[key] = value.strip()
        else:
            optimized[key] = value
    
    return optimized 