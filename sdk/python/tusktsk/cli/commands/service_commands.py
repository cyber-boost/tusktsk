#!/usr/bin/env python3
"""
Service Commands for TuskLang Python CLI
========================================
Implements service management commands
"""

import os
import sys
import time
import subprocess
import signal
# Import psutil with fallback
try:
    import psutil
    PSUTIL_AVAILABLE = True
except ImportError:
    PSUTIL_AVAILABLE = False
    # Create dummy psutil module for when it's not available
    class DummyProcess:
        def __init__(self): pass
        def name(self): return "unknown"
        def pid(self): return 0
        def status(self): return "unknown"
        def memory_info(self): return type('obj', (object,), {'rss': 0})()
        def create_time(self): return 0
        def terminate(self): return None
        def kill(self): return None
    
    class psutil:
        @staticmethod
        def process_iter(): return []
        @staticmethod
        def Process(pid): return DummyProcess()
from pathlib import Path
from typing import Any, Dict, List, Optional

from ..utils.output_formatter import OutputFormatter
from ..utils.error_handler import ErrorHandler
from ..utils.config_loader import ConfigLoader


def handle_service_command(args: Any, cli: Any) -> int:
    """Handle service commands"""
    formatter = OutputFormatter(cli.json_output, cli.quiet, cli.verbose)
    error_handler = ErrorHandler(cli.json_output, cli.verbose)
    
    try:
        if args.service_command == 'start':
            return _handle_service_start(formatter, error_handler)
        elif args.service_command == 'stop':
            return _handle_service_stop(formatter, error_handler)
        elif args.service_command == 'restart':
            return _handle_service_restart(formatter, error_handler)
        elif args.service_command == 'status':
            return _handle_service_status(formatter, error_handler)
        elif args.service_command == 'logs':
            return _handle_service_logs(args, formatter, error_handler)
        elif args.service_command == 'health':
            return _handle_service_health(args, formatter, error_handler)
        else:
            formatter.error("Unknown service command")
            return ErrorHandler.INVALID_ARGS
            
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_service_start(formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle service start command"""
    formatter.loading("Starting TuskLang services...")
    
    try:
        services = _get_tusklang_services()
        started_services = []
        failed_services = []
        
        for service_name, service_config in services.items():
            try:
                if _start_service(service_name, service_config):
                    started_services.append(service_name)
                else:
                    failed_services.append(service_name)
            except Exception as e:
                failed_services.append(f"{service_name}: {str(e)}")
        
        # Display results
        if started_services:
            formatter.success(f"Started {len(started_services)} services")
            formatter.list_items(started_services, "Started Services")
        
        if failed_services:
            formatter.error(f"Failed to start {len(failed_services)} services")
            formatter.list_items(failed_services, "Failed Services")
            return ErrorHandler.GENERAL_ERROR
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_service_stop(formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle service stop command"""
    formatter.loading("Stopping TuskLang services...")
    
    try:
        services = _get_running_tusklang_services()
        stopped_services = []
        failed_services = []
        
        for service_name, process in services.items():
            try:
                if _stop_service(service_name, process):
                    stopped_services.append(service_name)
                else:
                    failed_services.append(service_name)
            except Exception as e:
                failed_services.append(f"{service_name}: {str(e)}")
        
        # Display results
        if stopped_services:
            formatter.success(f"Stopped {len(stopped_services)} services")
            formatter.list_items(stopped_services, "Stopped Services")
        
        if failed_services:
            formatter.error(f"Failed to stop {len(failed_services)} services")
            formatter.list_items(failed_services, "Failed Services")
            return ErrorHandler.GENERAL_ERROR
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_service_restart(formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle service restart command"""
    formatter.loading("Restarting TuskLang services...")
    
    try:
        # Stop services first
        stop_result = _handle_service_stop(formatter, error_handler)
        if stop_result != ErrorHandler.SUCCESS:
            formatter.warning("Some services failed to stop")
        
        # Wait a moment
        time.sleep(2)
        
        # Start services
        start_result = _handle_service_start(formatter, error_handler)
        if start_result != ErrorHandler.SUCCESS:
            formatter.error("Some services failed to start")
            return ErrorHandler.GENERAL_ERROR
        
        formatter.success("All services restarted successfully")
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_service_status(formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle service status command"""
    formatter.loading("Checking TuskLang service status...")
    
    try:
        services = _get_tusklang_services()
        running_services = _get_running_tusklang_services()
        
        status_results = []
        
        for service_name, service_config in services.items():
            if service_name in running_services:
                process = running_services[service_name]
                status = "✅ Running"
                pid = process.pid
                uptime = _get_process_uptime(process)
                memory = _get_process_memory(process)
                status_results.append([service_name, status, pid, uptime, memory])
            else:
                status_results.append([service_name, "❌ Stopped", "N/A", "N/A", "N/A"])
        
        # Display results
        formatter.table(
            ['Service', 'Status', 'PID', 'Uptime', 'Memory'],
            status_results,
            'TuskLang Service Status'
        )
        
        # Summary
        running_count = len(running_services)
        total_count = len(services)
        
        if running_count == total_count:
            formatter.success(f"All {total_count} services are running")
        elif running_count > 0:
            formatter.warning(f"{running_count}/{total_count} services are running")
        else:
            formatter.error("No services are running")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _get_tusklang_services() -> Dict[str, Dict[str, Any]]:
    """Get list of TuskLang services"""
    return {
        'tusklang-api': {
            'command': 'python -m tsk.api',
            'port': 8080,
            'description': 'TuskLang API Server'
        },
        'tusklang-worker': {
            'command': 'python -m tsk.worker',
            'port': None,
            'description': 'TuskLang Background Worker'
        },
        'tusklang-cache': {
            'command': 'python -m tsk.cache',
            'port': 6379,
            'description': 'TuskLang Cache Service'
        },
        'tusklang-db': {
            'command': 'python -m tsk.database',
            'port': 5432,
            'description': 'TuskLang Database Service'
        }
    }


def _get_running_tusklang_services() -> Dict[str, psutil.Process]:
    """Get currently running TuskLang services"""
    running_services = {}
    
    for proc in psutil.process_iter(['pid', 'name', 'cmdline']):
        try:
            cmdline = ' '.join(proc.info['cmdline']) if proc.info['cmdline'] else ''
            if 'tsk' in cmdline or 'tusklang' in cmdline:
                service_name = _identify_service_from_process(proc)
                if service_name:
                    running_services[service_name] = proc
        except (psutil.NoSuchProcess, psutil.AccessDenied):
            continue
    
    return running_services


def _identify_service_from_process(process: psutil.Process) -> Optional[str]:
    """Identify service name from process"""
    try:
        cmdline = ' '.join(process.cmdline())
        
        if 'tsk.api' in cmdline:
            return 'tusklang-api'
        elif 'tsk.worker' in cmdline:
            return 'tusklang-worker'
        elif 'tsk.cache' in cmdline:
            return 'tusklang-cache'
        elif 'tsk.database' in cmdline:
            return 'tusklang-db'
        else:
            return None
    except (psutil.NoSuchProcess, psutil.AccessDenied):
        return None


def _start_service(service_name: str, service_config: Dict[str, Any]) -> bool:
    """Start a service"""
    try:
        # Check if service is already running
        running_services = _get_running_tusklang_services()
        if service_name in running_services:
            return True  # Already running
        
        # Start service
        cmd = service_config['command'].split()
        process = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            start_new_session=True
        )
        
        # Wait a moment to see if it starts successfully
        time.sleep(1)
        
        if process.poll() is None:
            return True  # Process is running
        else:
            return False  # Process failed to start
            
    except Exception:
        return False


def _stop_service(service_name: str, process: psutil.Process) -> bool:
    """Stop a service"""
    try:
        # Try graceful shutdown first
        process.terminate()
        
        # Wait for graceful shutdown
        try:
            process.wait(timeout=10)
            return True
        except psutil.TimeoutExpired:
            # Force kill if graceful shutdown fails
            process.kill()
            process.wait()
            return True
            
    except (psutil.NoSuchProcess, psutil.AccessDenied):
        return True  # Process already stopped
    except Exception:
        return False


def _get_process_uptime(process: psutil.Process) -> str:
    """Get process uptime as string"""
    try:
        create_time = process.create_time()
        uptime_seconds = time.time() - create_time
        
        if uptime_seconds < 60:
            return f"{int(uptime_seconds)}s"
        elif uptime_seconds < 3600:
            return f"{int(uptime_seconds // 60)}m"
        else:
            hours = int(uptime_seconds // 3600)
            minutes = int((uptime_seconds % 3600) // 60)
            return f"{hours}h {minutes}m"
    except (psutil.NoSuchProcess, psutil.AccessDenied):
        return "N/A"


def _get_process_memory(process: psutil.Process) -> str:
    """Get process memory usage as string"""
    try:
        memory_info = process.memory_info()
        memory_mb = memory_info.rss / 1024 / 1024
        
        if memory_mb < 1024:
            return f"{memory_mb:.1f}MB"
        else:
            return f"{memory_mb / 1024:.1f}GB"
    except (psutil.NoSuchProcess, psutil.AccessDenied):
        return "N/A"


def _check_port_availability(port: int) -> bool:
    """Check if a port is available"""
    import socket
    
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.bind(('localhost', port))
            return True
    except OSError:
        return False


def _get_service_logs(service_name: str, lines: int = 50) -> List[str]:
    """Get service logs (placeholder implementation)"""
    # This would typically read from log files
    # For now, return placeholder
    return [
        f"[{time.strftime('%Y-%m-%d %H:%M:%S')}] {service_name}: Service started",
        f"[{time.strftime('%Y-%m-%d %H:%M:%S')}] {service_name}: Ready to accept connections"
    ]


def _handle_service_logs(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle service logs command"""
    formatter.loading("Retrieving service logs...")
    
    try:
        service_name = getattr(args, 'service_name', None)
        lines = getattr(args, 'lines', 50)
        
        if service_name:
            # Get logs for specific service
            logs = _get_service_logs(service_name, lines)
            formatter.info(f"Logs for {service_name}:")
            for log in logs:
                formatter.info(f"  {log}")
        else:
            # Get logs for all services
            services = _get_tusklang_services()
            for service_name in services:
                logs = _get_service_logs(service_name, lines)
                formatter.info(f"Logs for {service_name}:")
                for log in logs:
                    formatter.info(f"  {log}")
                formatter.info("")  # Empty line between services
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e)


def _handle_service_health(args: Any, formatter: OutputFormatter, error_handler: ErrorHandler) -> int:
    """Handle service health command"""
    formatter.loading("Checking service health...")
    
    try:
        services = _get_tusklang_services()
        running_services = _get_running_tusklang_services()
        
        health_results = []
        overall_health = "healthy"
        
        for service_name, service_config in services.items():
            if service_name in running_services:
                process = running_services[service_name]
                
                # Check process health
                try:
                    # Check if process is responsive
                    cpu_percent = process.cpu_percent()
                    memory_info = process.memory_info()
                    memory_mb = memory_info.rss / 1024 / 1024
                    
                    # Determine health status
                    if cpu_percent > 80:
                        health_status = "⚠️ High CPU"
                        overall_health = "degraded"
                    elif memory_mb > 1024:  # 1GB
                        health_status = "⚠️ High Memory"
                        overall_health = "degraded"
                    else:
                        health_status = "✅ Healthy"
                    
                    health_results.append([
                        service_name,
                        health_status,
                        f"{cpu_percent:.1f}%",
                        f"{memory_mb:.1f}MB",
                        process.pid
                    ])
                    
                except (psutil.NoSuchProcess, psutil.AccessDenied):
                    health_results.append([
                        service_name,
                        "❌ Error",
                        "N/A",
                        "N/A",
                        "N/A"
                    ])
                    overall_health = "unhealthy"
            else:
                health_results.append([
                    service_name,
                    "❌ Stopped",
                    "N/A",
                    "N/A",
                    "N/A"
                ])
                overall_health = "unhealthy"
        
        # Display results
        formatter.table(
            ['Service', 'Health', 'CPU', 'Memory', 'PID'],
            health_results,
            'Service Health Status'
        )
        
        # Overall health summary
        if overall_health == "healthy":
            formatter.success("All services are healthy")
        elif overall_health == "degraded":
            formatter.warning("Some services show performance issues")
        else:
            formatter.error("Some services are unhealthy or stopped")
        
        return ErrorHandler.SUCCESS
        
    except Exception as e:
        return error_handler.handle_error(e) 