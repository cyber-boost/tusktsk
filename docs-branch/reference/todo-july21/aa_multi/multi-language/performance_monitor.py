#!/usr/bin/env python3
"""
TuskLang Multi-Language Performance Monitor
Real-time performance monitoring and metrics collection across all language SDKs
"""

import os
import json
import time
import sqlite3
import threading
import psutil
import asyncio
from pathlib import Path
from typing import Dict, List, Optional, Any, Tuple
from dataclasses import dataclass, asdict
from datetime import datetime, timedelta
import logging
import tempfile
import subprocess
import signal
import socket
import platform
from collections import defaultdict, deque

logger = logging.getLogger(__name__)

@dataclass
class PerformanceMetrics:
    """Performance metrics for a language process"""
    language: str
    process_id: int
    timestamp: datetime
    cpu_percent: float
    memory_percent: float
    memory_rss: int  # Resident Set Size in bytes
    memory_vms: int  # Virtual Memory Size in bytes
    io_read_count: int
    io_write_count: int
    io_read_bytes: int
    io_write_bytes: int
    num_threads: int
    num_fds: int
    status: str
    create_time: float

@dataclass
class SystemMetrics:
    """System-wide performance metrics"""
    timestamp: datetime
    cpu_percent: float
    memory_percent: float
    memory_available: int
    memory_total: int
    disk_usage_percent: float
    disk_read_bytes: int
    disk_write_bytes: int
    network_bytes_sent: int
    network_bytes_recv: int
    load_average: Tuple[float, float, float]
    num_processes: int

@dataclass
class PerformanceAlert:
    """Performance alert configuration and trigger"""
    alert_id: str
    language: str
    metric_type: str  # 'cpu', 'memory', 'io', 'network'
    threshold: float
    operator: str  # '>', '<', '>=', '<=', '=='
    duration: int  # seconds
    message: str
    severity: str  # 'info', 'warning', 'critical'
    enabled: bool = True

@dataclass
class PerformanceReport:
    """Performance analysis report"""
    language: str
    time_range: str
    avg_cpu_percent: float
    max_cpu_percent: float
    avg_memory_percent: float
    max_memory_percent: float
    total_io_read: int
    total_io_write: int
    bottleneck_analysis: List[str]
    recommendations: List[str]
    performance_score: float

class MultiLanguagePerformanceMonitor:
    """Real-time performance monitoring across all TuskLang language SDKs"""
    
    def __init__(self, monitor_dir: Path = None):
        if monitor_dir is None:
            self.monitor_dir = Path(tempfile.mkdtemp(prefix='tsk_perf_monitor_'))
        else:
            self.monitor_dir = monitor_dir
        
        self.db_path = self.monitor_dir / 'performance_monitor.db'
        self.logs_dir = self.monitor_dir / 'logs'
        self.dashboards_dir = self.monitor_dir / 'dashboards'
        
        # Create directories
        self.logs_dir.mkdir(exist_ok=True)
        self.dashboards_dir.mkdir(exist_ok=True)
        
        # Initialize database
        self._init_database()
        
        # Monitoring state
        self.monitoring_active = False
        self.monitoring_thread = None
        self.stop_monitoring = threading.Event()
        
        # Performance data storage
        self.metrics_buffer = defaultdict(lambda: deque(maxlen=1000))
        self.system_metrics_buffer = deque(maxlen=1000)
        
        # Alert configurations
        self.alerts = self._load_default_alerts()
        
        # Language process tracking
        self.language_processes = {}
        self.process_metrics = {}
    
    def _init_database(self):
        """Initialize SQLite database for performance monitoring"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Create tables
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS performance_metrics (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                language TEXT,
                process_id INTEGER,
                timestamp TEXT,
                cpu_percent REAL,
                memory_percent REAL,
                memory_rss INTEGER,
                memory_vms INTEGER,
                io_read_count INTEGER,
                io_write_count INTEGER,
                io_read_bytes INTEGER,
                io_write_bytes INTEGER,
                num_threads INTEGER,
                num_fds INTEGER,
                status TEXT,
                create_time REAL
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS system_metrics (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp TEXT,
                cpu_percent REAL,
                memory_percent REAL,
                memory_available INTEGER,
                memory_total INTEGER,
                disk_usage_percent REAL,
                disk_read_bytes INTEGER,
                disk_write_bytes INTEGER,
                network_bytes_sent INTEGER,
                network_bytes_recv INTEGER,
                load_average_1 REAL,
                load_average_5 REAL,
                load_average_15 REAL,
                num_processes INTEGER
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS performance_alerts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                alert_id TEXT UNIQUE,
                language TEXT,
                metric_type TEXT,
                threshold REAL,
                operator TEXT,
                duration INTEGER,
                message TEXT,
                severity TEXT,
                enabled BOOLEAN,
                created_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS alert_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                alert_id TEXT,
                language TEXT,
                metric_value REAL,
                threshold REAL,
                message TEXT,
                severity TEXT,
                timestamp TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS performance_reports (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                language TEXT,
                time_range TEXT,
                avg_cpu_percent REAL,
                max_cpu_percent REAL,
                avg_memory_percent REAL,
                max_memory_percent REAL,
                total_io_read INTEGER,
                total_io_write INTEGER,
                bottleneck_analysis TEXT,
                recommendations TEXT,
                performance_score REAL,
                generated_at TEXT
            )
        ''')
        
        conn.commit()
        conn.close()
    
    def _load_default_alerts(self) -> List[PerformanceAlert]:
        """Load default performance alert configurations"""
        default_alerts = [
            PerformanceAlert(
                alert_id="high_cpu_python",
                language="python",
                metric_type="cpu",
                threshold=80.0,
                operator=">",
                duration=60,
                message="Python process CPU usage is high",
                severity="warning"
            ),
            PerformanceAlert(
                alert_id="high_memory_python",
                language="python",
                metric_type="memory",
                threshold=85.0,
                operator=">",
                duration=120,
                message="Python process memory usage is high",
                severity="critical"
            ),
            PerformanceAlert(
                alert_id="high_cpu_rust",
                language="rust",
                metric_type="cpu",
                threshold=90.0,
                operator=">",
                duration=60,
                message="Rust process CPU usage is very high",
                severity="warning"
            ),
            PerformanceAlert(
                alert_id="high_io_all",
                language="all",
                metric_type="io",
                threshold=1000000,  # 1MB/s
                operator=">",
                duration=30,
                message="High I/O activity detected",
                severity="info"
            )
        ]
        
        # Save default alerts to database
        for alert in default_alerts:
            self.save_alert(alert)
        
        return default_alerts
    
    def start_monitoring(self, languages: List[str] = None) -> bool:
        """Start performance monitoring for specified languages"""
        if self.monitoring_active:
            logger.warning("Performance monitoring is already active")
            return False
        
        if languages is None:
            languages = ['python', 'rust', 'javascript', 'ruby', 'csharp', 'go', 'php', 'java', 'bash']
        
        try:
            self.monitoring_active = True
            self.stop_monitoring.clear()
            
            # Start monitoring thread
            self.monitoring_thread = threading.Thread(
                target=self._monitoring_loop,
                args=(languages,)
            )
            self.monitoring_thread.daemon = True
            self.monitoring_thread.start()
            
            logger.info(f"Started performance monitoring for languages: {languages}")
            return True
            
        except Exception as e:
            logger.error(f"Failed to start performance monitoring: {e}")
            self.monitoring_active = False
            return False
    
    def stop_monitoring(self):
        """Stop performance monitoring"""
        if not self.monitoring_active:
            return
        
        self.stop_monitoring.set()
        self.monitoring_active = False
        
        if self.monitoring_thread:
            self.monitoring_thread.join(timeout=5)
        
        logger.info("Stopped performance monitoring")
    
    def _monitoring_loop(self, languages: List[str]):
        """Main monitoring loop"""
        while not self.stop_monitoring.is_set():
            try:
                # Collect system metrics
                system_metrics = self._collect_system_metrics()
                self.system_metrics_buffer.append(system_metrics)
                self._save_system_metrics(system_metrics)
                
                # Collect language-specific metrics
                for language in languages:
                    self._collect_language_metrics(language)
                
                # Check alerts
                self._check_alerts()
                
                # Wait for next collection interval
                time.sleep(5)  # 5-second collection interval
                
            except Exception as e:
                logger.error(f"Error in monitoring loop: {e}")
                time.sleep(10)  # Wait before retrying
    
    def _collect_system_metrics(self) -> SystemMetrics:
        """Collect system-wide performance metrics"""
        try:
            # CPU metrics
            cpu_percent = psutil.cpu_percent(interval=1)
            
            # Memory metrics
            memory = psutil.virtual_memory()
            
            # Disk metrics
            disk = psutil.disk_io_counters()
            disk_usage = psutil.disk_usage('/')
            
            # Network metrics
            network = psutil.net_io_counters()
            
            # Load average
            load_avg = psutil.getloadavg()
            
            # Process count
            num_processes = len(psutil.pids())
            
            return SystemMetrics(
                timestamp=datetime.now(),
                cpu_percent=cpu_percent,
                memory_percent=memory.percent,
                memory_available=memory.available,
                memory_total=memory.total,
                disk_usage_percent=disk_usage.percent,
                disk_read_bytes=disk.read_bytes if disk else 0,
                disk_write_bytes=disk.write_bytes if disk else 0,
                network_bytes_sent=network.bytes_sent if network else 0,
                network_bytes_recv=network.bytes_recv if network else 0,
                load_average=load_avg,
                num_processes=num_processes
            )
            
        except Exception as e:
            logger.error(f"Failed to collect system metrics: {e}")
            return SystemMetrics(
                timestamp=datetime.now(),
                cpu_percent=0.0,
                memory_percent=0.0,
                memory_available=0,
                memory_total=0,
                disk_usage_percent=0.0,
                disk_read_bytes=0,
                disk_write_bytes=0,
                network_bytes_sent=0,
                network_bytes_recv=0,
                load_average=(0.0, 0.0, 0.0),
                num_processes=0
            )
    
    def _collect_language_metrics(self, language: str):
        """Collect performance metrics for a specific language"""
        try:
            # Find processes for this language
            processes = self._find_language_processes(language)
            
            for process in processes:
                try:
                    # Get process metrics
                    with process.oneshot():
                        cpu_percent = process.cpu_percent()
                        memory_percent = process.memory_percent()
                        memory_info = process.memory_info()
                        io_counters = process.io_counters()
                        num_threads = process.num_threads()
                        num_fds = process.num_fds() if hasattr(process, 'num_fds') else 0
                        status = process.status()
                        create_time = process.create_time()
                    
                    metrics = PerformanceMetrics(
                        language=language,
                        process_id=process.pid,
                        timestamp=datetime.now(),
                        cpu_percent=cpu_percent,
                        memory_percent=memory_percent,
                        memory_rss=memory_info.rss,
                        memory_vms=memory_info.vms,
                        io_read_count=io_counters.read_count if io_counters else 0,
                        io_write_count=io_counters.write_count if io_counters else 0,
                        io_read_bytes=io_counters.read_bytes if io_counters else 0,
                        io_write_bytes=io_counters.write_bytes if io_counters else 0,
                        num_threads=num_threads,
                        num_fds=num_fds,
                        status=status,
                        create_time=create_time
                    )
                    
                    # Store in buffer
                    self.metrics_buffer[language].append(metrics)
                    
                    # Save to database
                    self._save_performance_metrics(metrics)
                    
                except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess):
                    # Process no longer exists or access denied
                    continue
                except Exception as e:
                    logger.error(f"Error collecting metrics for process {process.pid}: {e}")
                    continue
                    
        except Exception as e:
            logger.error(f"Failed to collect metrics for language {language}: {e}")
    
    def _find_language_processes(self, language: str) -> List[psutil.Process]:
        """Find processes for a specific language"""
        processes = []
        
        try:
            for proc in psutil.process_iter(['pid', 'name', 'cmdline']):
                try:
                    # Check if process matches language
                    if self._is_language_process(proc, language):
                        processes.append(proc)
                except (psutil.NoSuchProcess, psutil.AccessDenied):
                    continue
                    
        except Exception as e:
            logger.error(f"Error finding processes for {language}: {e}")
        
        return processes
    
    def _is_language_process(self, proc: psutil.Process, language: str) -> bool:
        """Check if a process belongs to a specific language"""
        try:
            name = proc.info['name'].lower()
            cmdline = ' '.join(proc.info['cmdline']).lower()
            
            if language == 'python':
                return 'python' in name or 'python' in cmdline
            elif language == 'rust':
                return any(x in name for x in ['rust', 'cargo']) or 'cargo' in cmdline
            elif language == 'javascript':
                return any(x in name for x in ['node', 'npm']) or 'node' in cmdline
            elif language == 'ruby':
                return 'ruby' in name or 'ruby' in cmdline
            elif language == 'csharp':
                return any(x in name for x in ['dotnet', 'mono']) or 'dotnet' in cmdline
            elif language == 'go':
                return 'go' in name or 'go' in cmdline
            elif language == 'php':
                return 'php' in name or 'php' in cmdline
            elif language == 'java':
                return 'java' in name or 'java' in cmdline
            elif language == 'bash':
                return 'bash' in name or 'sh' in name
            
            return False
            
        except Exception:
            return False
    
    def _save_performance_metrics(self, metrics: PerformanceMetrics):
        """Save performance metrics to database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO performance_metrics 
                (language, process_id, timestamp, cpu_percent, memory_percent, memory_rss, memory_vms,
                 io_read_count, io_write_count, io_read_bytes, io_write_bytes, num_threads, num_fds, status, create_time)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                metrics.language,
                metrics.process_id,
                metrics.timestamp.isoformat(),
                metrics.cpu_percent,
                metrics.memory_percent,
                metrics.memory_rss,
                metrics.memory_vms,
                metrics.io_read_count,
                metrics.io_write_count,
                metrics.io_read_bytes,
                metrics.io_write_bytes,
                metrics.num_threads,
                metrics.num_fds,
                metrics.status,
                metrics.create_time
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"Failed to save performance metrics: {e}")
    
    def _save_system_metrics(self, metrics: SystemMetrics):
        """Save system metrics to database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO system_metrics 
                (timestamp, cpu_percent, memory_percent, memory_available, memory_total,
                 disk_usage_percent, disk_read_bytes, disk_write_bytes, network_bytes_sent, network_bytes_recv,
                 load_average_1, load_average_5, load_average_15, num_processes)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                metrics.timestamp.isoformat(),
                metrics.cpu_percent,
                metrics.memory_percent,
                metrics.memory_available,
                metrics.memory_total,
                metrics.disk_usage_percent,
                metrics.disk_read_bytes,
                metrics.disk_write_bytes,
                metrics.network_bytes_sent,
                metrics.network_bytes_recv,
                metrics.load_average[0],
                metrics.load_average[1],
                metrics.load_average[2],
                metrics.num_processes
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"Failed to save system metrics: {e}")
    
    def save_alert(self, alert: PerformanceAlert) -> bool:
        """Save performance alert configuration"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO performance_alerts 
                (alert_id, language, metric_type, threshold, operator, duration, message, severity, enabled, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                alert.alert_id,
                alert.language,
                alert.metric_type,
                alert.threshold,
                alert.operator,
                alert.duration,
                alert.message,
                alert.severity,
                alert.enabled,
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            logger.error(f"Failed to save alert: {e}")
            return False
    
    def _check_alerts(self):
        """Check if any alerts should be triggered"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('SELECT * FROM performance_alerts WHERE enabled = 1')
            alerts = cursor.fetchall()
            
            for alert_row in alerts:
                alert_id, language, metric_type, threshold, operator, duration, message, severity, enabled, created_at = alert_row
                
                # Check if alert should be triggered
                if self._should_trigger_alert(language, metric_type, threshold, operator, duration):
                    self._trigger_alert(alert_id, language, threshold, message, severity)
            
            conn.close()
            
        except Exception as e:
            logger.error(f"Error checking alerts: {e}")
    
    def _should_trigger_alert(self, language: str, metric_type: str, threshold: float, 
                            operator: str, duration: int) -> bool:
        """Check if an alert should be triggered based on recent metrics"""
        try:
            # Get recent metrics for the language
            cutoff_time = datetime.now() - timedelta(seconds=duration)
            
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            if language == 'all':
                cursor.execute('''
                    SELECT AVG(cpu_percent), AVG(memory_percent), AVG(io_read_bytes + io_write_bytes)
                    FROM performance_metrics 
                    WHERE timestamp > ?
                ''', (cutoff_time.isoformat(),))
            else:
                cursor.execute('''
                    SELECT AVG(cpu_percent), AVG(memory_percent), AVG(io_read_bytes + io_write_bytes)
                    FROM performance_metrics 
                    WHERE language = ? AND timestamp > ?
                ''', (language, cutoff_time.isoformat()))
            
            result = cursor.fetchone()
            conn.close()
            
            if not result or result[0] is None:
                return False
            
            avg_cpu, avg_memory, avg_io = result
            
            # Check metric based on type
            if metric_type == 'cpu':
                metric_value = avg_cpu
            elif metric_type == 'memory':
                metric_value = avg_memory
            elif metric_type == 'io':
                metric_value = avg_io
            else:
                return False
            
            # Apply operator
            if operator == '>':
                return metric_value > threshold
            elif operator == '<':
                return metric_value < threshold
            elif operator == '>=':
                return metric_value >= threshold
            elif operator == '<=':
                return metric_value <= threshold
            elif operator == '==':
                return metric_value == threshold
            
            return False
            
        except Exception as e:
            logger.error(f"Error checking alert trigger: {e}")
            return False
    
    def _trigger_alert(self, alert_id: str, language: str, threshold: float, 
                      message: str, severity: str):
        """Trigger a performance alert"""
        try:
            # Log alert
            logger.warning(f"PERFORMANCE ALERT [{severity.upper()}]: {message}")
            
            # Save to alert history
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO alert_history 
                (alert_id, language, metric_value, threshold, message, severity, timestamp)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            ''', (
                alert_id,
                language,
                0.0,  # Will be updated with actual value
                threshold,
                message,
                severity,
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"Error triggering alert: {e}")
    
    def get_performance_metrics(self, language: str = None, 
                              time_range: timedelta = None) -> List[PerformanceMetrics]:
        """Get performance metrics from database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            query = 'SELECT * FROM performance_metrics'
            params = []
            
            conditions = []
            if language:
                conditions.append('language = ?')
                params.append(language)
            
            if time_range:
                cutoff_time = datetime.now() - time_range
                conditions.append('timestamp > ?')
                params.append(cutoff_time.isoformat())
            
            if conditions:
                query += ' WHERE ' + ' AND '.join(conditions)
            
            query += ' ORDER BY timestamp DESC'
            
            cursor.execute(query, params)
            
            metrics = []
            for row in cursor.fetchall():
                id, language, process_id, timestamp, cpu_percent, memory_percent, memory_rss, memory_vms, io_read_count, io_write_count, io_read_bytes, io_write_bytes, num_threads, num_fds, status, create_time = row
                
                metrics.append(PerformanceMetrics(
                    language=language,
                    process_id=process_id,
                    timestamp=datetime.fromisoformat(timestamp),
                    cpu_percent=cpu_percent,
                    memory_percent=memory_percent,
                    memory_rss=memory_rss,
                    memory_vms=memory_vms,
                    io_read_count=io_read_count,
                    io_write_count=io_write_count,
                    io_read_bytes=io_read_bytes,
                    io_write_bytes=io_write_bytes,
                    num_threads=num_threads,
                    num_fds=num_fds,
                    status=status,
                    create_time=create_time
                ))
            
            conn.close()
            return metrics
            
        except Exception as e:
            logger.error(f"Failed to get performance metrics: {e}")
            return []
    
    def generate_performance_report(self, language: str, 
                                  time_range: timedelta = timedelta(hours=1)) -> PerformanceReport:
        """Generate a performance report for a language"""
        try:
            metrics = self.get_performance_metrics(language, time_range)
            
            if not metrics:
                return PerformanceReport(
                    language=language,
                    time_range=str(time_range),
                    avg_cpu_percent=0.0,
                    max_cpu_percent=0.0,
                    avg_memory_percent=0.0,
                    max_memory_percent=0.0,
                    total_io_read=0,
                    total_io_write=0,
                    bottleneck_analysis=[],
                    recommendations=[],
                    performance_score=0.0
                )
            
            # Calculate averages and maximums
            cpu_percents = [m.cpu_percent for m in metrics]
            memory_percents = [m.memory_percent for m in metrics]
            io_reads = [m.io_read_bytes for m in metrics]
            io_writes = [m.io_write_bytes for m in metrics]
            
            avg_cpu = sum(cpu_percents) / len(cpu_percents)
            max_cpu = max(cpu_percents)
            avg_memory = sum(memory_percents) / len(memory_percents)
            max_memory = max(memory_percents)
            total_io_read = sum(io_reads)
            total_io_write = sum(io_writes)
            
            # Analyze bottlenecks
            bottlenecks = []
            if avg_cpu > 80:
                bottlenecks.append("High CPU usage detected")
            if avg_memory > 85:
                bottlenecks.append("High memory usage detected")
            if total_io_read + total_io_write > 1000000000:  # 1GB
                bottlenecks.append("High I/O activity detected")
            
            # Generate recommendations
            recommendations = []
            if avg_cpu > 80:
                recommendations.append("Consider optimizing CPU-intensive operations")
            if avg_memory > 85:
                recommendations.append("Consider implementing memory pooling or garbage collection optimization")
            if total_io_read + total_io_write > 1000000000:
                recommendations.append("Consider implementing I/O buffering or caching")
            
            # Calculate performance score (0-100)
            cpu_score = max(0, 100 - avg_cpu)
            memory_score = max(0, 100 - avg_memory)
            performance_score = (cpu_score + memory_score) / 2
            
            return PerformanceReport(
                language=language,
                time_range=str(time_range),
                avg_cpu_percent=avg_cpu,
                max_cpu_percent=max_cpu,
                avg_memory_percent=avg_memory,
                max_memory_percent=max_memory,
                total_io_read=total_io_read,
                total_io_write=total_io_write,
                bottleneck_analysis=bottlenecks,
                recommendations=recommendations,
                performance_score=performance_score
            )
            
        except Exception as e:
            logger.error(f"Failed to generate performance report: {e}")
            return PerformanceReport(
                language=language,
                time_range=str(time_range),
                avg_cpu_percent=0.0,
                max_cpu_percent=0.0,
                avg_memory_percent=0.0,
                max_memory_percent=0.0,
                total_io_read=0,
                total_io_write=0,
                bottleneck_analysis=[],
                recommendations=[],
                performance_score=0.0
            )

def main():
    """CLI for performance monitor"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang Performance Monitor')
    parser.add_argument('--start', nargs='*', help='Start monitoring for languages')
    parser.add_argument('--stop', action='store_true', help='Stop monitoring')
    parser.add_argument('--metrics', help='Get metrics for language')
    parser.add_argument('--report', nargs=2, metavar=('LANGUAGE', 'HOURS'), help='Generate performance report')
    parser.add_argument('--alerts', action='store_true', help='List alerts')
    parser.add_argument('--add-alert', nargs=7, metavar=('ID', 'LANG', 'TYPE', 'THRESHOLD', 'OP', 'DURATION', 'MESSAGE'), help='Add alert')
    
    args = parser.parse_args()
    
    monitor = MultiLanguagePerformanceMonitor()
    
    if args.start is not None:
        languages = args.start if args.start else None
        success = monitor.start_monitoring(languages)
        print(f"Monitoring started: {success}")
    
    elif args.stop:
        monitor.stop_monitoring()
        print("Monitoring stopped")
    
    elif args.metrics:
        metrics = monitor.get_performance_metrics(args.metrics)
        print(json.dumps([asdict(m) for m in metrics], indent=2, default=str))
    
    elif args.report:
        language, hours = args.report
        time_range = timedelta(hours=int(hours))
        report = monitor.generate_performance_report(language, time_range)
        print(json.dumps(asdict(report), indent=2, default=str))
    
    elif args.alerts:
        for alert in monitor.alerts:
            print(f"{alert.alert_id}: {alert.message} ({alert.severity})")
    
    elif args.add_alert:
        alert_id, language, metric_type, threshold, operator, duration, message = args.add_alert
        alert = PerformanceAlert(
            alert_id=alert_id,
            language=language,
            metric_type=metric_type,
            threshold=float(threshold),
            operator=operator,
            duration=int(duration),
            message=message,
            severity="warning"
        )
        success = monitor.save_alert(alert)
        print(f"Alert added: {success}")
    
    else:
        parser.print_help()

if __name__ == '__main__':
    main() 