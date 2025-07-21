#!/usr/bin/env python3
"""
TuskLang Cross-Language Performance Optimizer
Automated performance optimization with resource allocation and intelligent scaling
"""

import os
import json
import time
import sqlite3
import threading
import psutil
import subprocess
from pathlib import Path
from typing import Dict, List, Optional, Any, Tuple
from dataclasses import dataclass, asdict
from datetime import datetime, timedelta
import logging
import tempfile
import signal
import shutil
from collections import defaultdict

logger = logging.getLogger(__name__)

@dataclass
class ResourceAllocation:
    """Resource allocation configuration"""
    language: str
    cpu_limit: float  # CPU percentage limit
    memory_limit: int  # Memory limit in bytes
    io_limit: int  # I/O operations per second
    network_limit: int  # Network bandwidth in bytes/s
    priority: int  # Process priority (lower = higher priority)
    max_processes: int  # Maximum number of processes

@dataclass
class OptimizationAction:
    """Performance optimization action"""
    action_id: str
    language: str
    action_type: str  # 'scale_up', 'scale_down', 'restart', 'optimize', 'migrate'
    target_process_id: Optional[int] = None
    parameters: Dict[str, Any] = None
    reason: str = ""
    timestamp: datetime = None
    success: bool = False
    duration: float = 0.0

@dataclass
class LoadBalancerConfig:
    """Load balancer configuration"""
    language: str
    algorithm: str  # 'round_robin', 'least_connections', 'weighted', 'adaptive'
    health_check_interval: int  # seconds
    max_failures: int
    backoff_time: int  # seconds
    weights: Dict[str, float] = None  # For weighted algorithms

@dataclass
class ScalingPolicy:
    """Auto-scaling policy configuration"""
    language: str
    min_instances: int
    max_instances: int
    cpu_threshold_up: float
    cpu_threshold_down: float
    memory_threshold_up: float
    memory_threshold_down: float
    scale_up_cooldown: int  # seconds
    scale_down_cooldown: int  # seconds
    enabled: bool = True

class MultiLanguagePerformanceOptimizer:
    """Cross-language performance optimization and resource management"""
    
    def __init__(self, optimizer_dir: Path = None):
        if optimizer_dir is None:
            self.optimizer_dir = Path(tempfile.mkdtemp(prefix='tsk_perf_optimizer_'))
        else:
            self.optimizer_dir = optimizer_dir
        
        self.db_path = self.optimizer_dir / 'performance_optimizer.db'
        self.logs_dir = self.optimizer_dir / 'logs'
        self.configs_dir = self.optimizer_dir / 'configs'
        
        # Create directories
        self.logs_dir.mkdir(exist_ok=True)
        self.configs_dir.mkdir(exist_ok=True)
        
        # Initialize database
        self._init_database()
        
        # Optimization state
        self.optimization_active = False
        self.optimization_thread = None
        self.stop_optimization = threading.Event()
        
        # Resource allocations
        self.resource_allocations = self._load_default_allocations()
        
        # Load balancers
        self.load_balancers = {}
        
        # Scaling policies
        self.scaling_policies = self._load_default_scaling_policies()
        
        # Process tracking
        self.process_groups = defaultdict(list)
        self.optimization_history = []
    
    def _init_database(self):
        """Initialize SQLite database for performance optimization"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Create tables
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS resource_allocations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                language TEXT UNIQUE,
                cpu_limit REAL,
                memory_limit INTEGER,
                io_limit INTEGER,
                network_limit INTEGER,
                priority INTEGER,
                max_processes INTEGER,
                created_at TEXT,
                updated_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS optimization_actions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                action_id TEXT,
                language TEXT,
                action_type TEXT,
                target_process_id INTEGER,
                parameters TEXT,
                reason TEXT,
                timestamp TEXT,
                success BOOLEAN,
                duration REAL
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS load_balancer_configs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                language TEXT UNIQUE,
                algorithm TEXT,
                health_check_interval INTEGER,
                max_failures INTEGER,
                backoff_time INTEGER,
                weights TEXT,
                created_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS scaling_policies (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                language TEXT UNIQUE,
                min_instances INTEGER,
                max_instances INTEGER,
                cpu_threshold_up REAL,
                cpu_threshold_down REAL,
                memory_threshold_up REAL,
                memory_threshold_down REAL,
                scale_up_cooldown INTEGER,
                scale_down_cooldown INTEGER,
                enabled BOOLEAN,
                created_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS performance_baselines (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                language TEXT,
                metric_type TEXT,
                baseline_value REAL,
                created_at TEXT
            )
        ''')
        
        conn.commit()
        conn.close()
    
    def _load_default_allocations(self) -> Dict[str, ResourceAllocation]:
        """Load default resource allocations"""
        default_allocations = {
            'python': ResourceAllocation(
                language='python',
                cpu_limit=80.0,
                memory_limit=1024 * 1024 * 1024,  # 1GB
                io_limit=1000,
                network_limit=1024 * 1024,  # 1MB/s
                priority=0,
                max_processes=10
            ),
            'rust': ResourceAllocation(
                language='rust',
                cpu_limit=90.0,
                memory_limit=512 * 1024 * 1024,  # 512MB
                io_limit=2000,
                network_limit=2048 * 1024,  # 2MB/s
                priority=-1,  # Higher priority
                max_processes=5
            ),
            'javascript': ResourceAllocation(
                language='javascript',
                cpu_limit=75.0,
                memory_limit=2048 * 1024 * 1024,  # 2GB
                io_limit=500,
                network_limit=512 * 1024,  # 512KB/s
                priority=1,
                max_processes=15
            ),
            'ruby': ResourceAllocation(
                language='ruby',
                cpu_limit=70.0,
                memory_limit=1024 * 1024 * 1024,  # 1GB
                io_limit=800,
                network_limit=1024 * 1024,  # 1MB/s
                priority=2,
                max_processes=8
            ),
            'csharp': ResourceAllocation(
                language='csharp',
                cpu_limit=85.0,
                memory_limit=1536 * 1024 * 1024,  # 1.5GB
                io_limit=1500,
                network_limit=1536 * 1024,  # 1.5MB/s
                priority=0,
                max_processes=6
            ),
            'go': ResourceAllocation(
                language='go',
                cpu_limit=95.0,
                memory_limit=512 * 1024 * 1024,  # 512MB
                io_limit=3000,
                network_limit=4096 * 1024,  # 4MB/s
                priority=-2,  # Highest priority
                max_processes=20
            ),
            'php': ResourceAllocation(
                language='php',
                cpu_limit=65.0,
                memory_limit=2048 * 1024 * 1024,  # 2GB
                io_limit=600,
                network_limit=1024 * 1024,  # 1MB/s
                priority=3,
                max_processes=12
            ),
            'java': ResourceAllocation(
                language='java',
                cpu_limit=80.0,
                memory_limit=4096 * 1024 * 1024,  # 4GB
                io_limit=1200,
                network_limit=2048 * 1024,  # 2MB/s
                priority=1,
                max_processes=8
            ),
            'bash': ResourceAllocation(
                language='bash',
                cpu_limit=60.0,
                memory_limit=256 * 1024 * 1024,  # 256MB
                io_limit=400,
                network_limit=512 * 1024,  # 512KB/s
                priority=5,
                max_processes=25
            )
        }
        
        # Save default allocations to database
        for allocation in default_allocations.values():
            self.save_resource_allocation(allocation)
        
        return default_allocations
    
    def _load_default_scaling_policies(self) -> Dict[str, ScalingPolicy]:
        """Load default scaling policies"""
        default_policies = {
            'python': ScalingPolicy(
                language='python',
                min_instances=1,
                max_instances=10,
                cpu_threshold_up=80.0,
                cpu_threshold_down=30.0,
                memory_threshold_up=85.0,
                memory_threshold_down=40.0,
                scale_up_cooldown=300,
                scale_down_cooldown=600,
                enabled=True
            ),
            'rust': ScalingPolicy(
                language='rust',
                min_instances=1,
                max_instances=5,
                cpu_threshold_up=90.0,
                cpu_threshold_down=20.0,
                memory_threshold_up=90.0,
                memory_threshold_down=30.0,
                scale_up_cooldown=180,
                scale_down_cooldown=900,
                enabled=True
            ),
            'javascript': ScalingPolicy(
                language='javascript',
                min_instances=2,
                max_instances=15,
                cpu_threshold_up=75.0,
                cpu_threshold_down=25.0,
                memory_threshold_up=80.0,
                memory_threshold_down=35.0,
                scale_up_cooldown=240,
                scale_down_cooldown=720,
                enabled=True
            )
        }
        
        # Save default policies to database
        for policy in default_policies.values():
            self.save_scaling_policy(policy)
        
        return default_policies
    
    def start_optimization(self) -> bool:
        """Start performance optimization"""
        if self.optimization_active:
            logger.warning("Performance optimization is already active")
            return False
        
        try:
            self.optimization_active = True
            self.stop_optimization.clear()
            
            # Start optimization thread
            self.optimization_thread = threading.Thread(
                target=self._optimization_loop
            )
            self.optimization_thread.daemon = True
            self.optimization_thread.start()
            
            logger.info("Started performance optimization")
            return True
            
        except Exception as e:
            logger.error(f"Failed to start optimization: {e}")
            self.optimization_active = False
            return False
    
    def stop_optimization(self):
        """Stop performance optimization"""
        if not self.optimization_active:
            return
        
        self.stop_optimization.set()
        self.optimization_active = False
        
        if self.optimization_thread:
            self.optimization_thread.join(timeout=5)
        
        logger.info("Stopped performance optimization")
    
    def _optimization_loop(self):
        """Main optimization loop"""
        while not self.stop_optimization.is_set():
            try:
                # Apply resource limits
                self._apply_resource_limits()
                
                # Check scaling policies
                self._check_scaling_policies()
                
                # Optimize process priorities
                self._optimize_process_priorities()
                
                # Clean up dead processes
                self._cleanup_dead_processes()
                
                # Wait for next optimization cycle
                time.sleep(30)  # 30-second optimization interval
                
            except Exception as e:
                logger.error(f"Error in optimization loop: {e}")
                time.sleep(60)  # Wait before retrying
    
    def _apply_resource_limits(self):
        """Apply resource limits to processes"""
        for language, allocation in self.resource_allocations.items():
            try:
                processes = self._get_language_processes(language)
                
                for process in processes:
                    try:
                        # Apply CPU limit using cgroups or nice
                        if allocation.cpu_limit < 100:
                            self._limit_cpu_usage(process, allocation.cpu_limit)
                        
                        # Apply memory limit
                        if allocation.memory_limit > 0:
                            self._limit_memory_usage(process, allocation.memory_limit)
                        
                        # Apply process priority
                        if allocation.priority != 0:
                            self._set_process_priority(process, allocation.priority)
                        
                    except (psutil.NoSuchProcess, psutil.AccessDenied):
                        continue
                    except Exception as e:
                        logger.error(f"Error applying limits to process {process.pid}: {e}")
                        
            except Exception as e:
                logger.error(f"Error applying resource limits for {language}: {e}")
    
    def _get_language_processes(self, language: str) -> List[psutil.Process]:
        """Get processes for a specific language"""
        processes = []
        
        try:
            for proc in psutil.process_iter(['pid', 'name', 'cmdline']):
                try:
                    if self._is_language_process(proc, language):
                        processes.append(proc)
                except (psutil.NoSuchProcess, psutil.AccessDenied):
                    continue
        except Exception as e:
            logger.error(f"Error getting processes for {language}: {e}")
        
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
    
    def _limit_cpu_usage(self, process: psutil.Process, cpu_limit: float):
        """Limit CPU usage for a process"""
        try:
            # Use nice to adjust process priority (simplified approach)
            if cpu_limit < 50:
                process.nice(10)  # Lower priority
            elif cpu_limit < 80:
                process.nice(5)   # Medium priority
            else:
                process.nice(0)   # Normal priority
        except Exception as e:
            logger.error(f"Error limiting CPU for process {process.pid}: {e}")
    
    def _limit_memory_usage(self, process: psutil.Process, memory_limit: int):
        """Limit memory usage for a process"""
        try:
            # This is a simplified approach - in production, use cgroups
            memory_info = process.memory_info()
            if memory_info.rss > memory_limit:
                logger.warning(f"Process {process.pid} exceeded memory limit")
                # Could implement memory pressure handling here
        except Exception as e:
            logger.error(f"Error limiting memory for process {process.pid}: {e}")
    
    def _set_process_priority(self, process: psutil.Process, priority: int):
        """Set process priority"""
        try:
            process.nice(priority)
        except Exception as e:
            logger.error(f"Error setting priority for process {process.pid}: {e}")
    
    def _check_scaling_policies(self):
        """Check and apply scaling policies"""
        for language, policy in self.scaling_policies.items():
            if not policy.enabled:
                continue
            
            try:
                current_metrics = self._get_current_metrics(language)
                if not current_metrics:
                    continue
                
                current_instances = len(self._get_language_processes(language))
                
                # Check if scaling up is needed
                if (current_metrics['cpu_percent'] > policy.cpu_threshold_up or 
                    current_metrics['memory_percent'] > policy.memory_threshold_up):
                    if current_instances < policy.max_instances:
                        self._scale_up(language, policy)
                
                # Check if scaling down is needed
                elif (current_metrics['cpu_percent'] < policy.cpu_threshold_down and 
                      current_metrics['memory_percent'] < policy.memory_threshold_down):
                    if current_instances > policy.min_instances:
                        self._scale_down(language, policy)
                        
            except Exception as e:
                logger.error(f"Error checking scaling policy for {language}: {e}")
    
    def _get_current_metrics(self, language: str) -> Optional[Dict[str, float]]:
        """Get current performance metrics for a language"""
        try:
            processes = self._get_language_processes(language)
            if not processes:
                return None
            
            cpu_percents = []
            memory_percents = []
            
            for process in processes:
                try:
                    with process.oneshot():
                        cpu_percents.append(process.cpu_percent())
                        memory_percents.append(process.memory_percent())
                except (psutil.NoSuchProcess, psutil.AccessDenied):
                    continue
            
            if not cpu_percents:
                return None
            
            return {
                'cpu_percent': sum(cpu_percents) / len(cpu_percents),
                'memory_percent': sum(memory_percents) / len(memory_percents)
            }
            
        except Exception as e:
            logger.error(f"Error getting metrics for {language}: {e}")
            return None
    
    def _scale_up(self, language: str, policy: ScalingPolicy):
        """Scale up language processes"""
        try:
            action = OptimizationAction(
                action_id=f"scale_up_{language}_{int(time.time())}",
                language=language,
                action_type="scale_up",
                reason=f"High resource usage detected (CPU: {policy.cpu_threshold_up}%, Memory: {policy.memory_threshold_up}%)",
                timestamp=datetime.now()
            )
            
            # In a real implementation, this would start new processes
            logger.info(f"Scaling up {language} processes")
            
            # Record the action
            self._record_optimization_action(action)
            
        except Exception as e:
            logger.error(f"Error scaling up {language}: {e}")
    
    def _scale_down(self, language: str, policy: ScalingPolicy):
        """Scale down language processes"""
        try:
            action = OptimizationAction(
                action_id=f"scale_down_{language}_{int(time.time())}",
                language=language,
                action_type="scale_down",
                reason=f"Low resource usage detected (CPU: {policy.cpu_threshold_down}%, Memory: {policy.memory_threshold_down}%)",
                timestamp=datetime.now()
            )
            
            # In a real implementation, this would stop excess processes
            logger.info(f"Scaling down {language} processes")
            
            # Record the action
            self._record_optimization_action(action)
            
        except Exception as e:
            logger.error(f"Error scaling down {language}: {e}")
    
    def _optimize_process_priorities(self):
        """Optimize process priorities based on resource usage"""
        try:
            # Get all language processes and their resource usage
            process_metrics = {}
            
            for language in self.resource_allocations.keys():
                processes = self._get_language_processes(language)
                for process in processes:
                    try:
                        with process.oneshot():
                            cpu_percent = process.cpu_percent()
                            memory_percent = process.memory_percent()
                        
                        process_metrics[process.pid] = {
                            'process': process,
                            'language': language,
                            'cpu_percent': cpu_percent,
                            'memory_percent': memory_percent,
                            'score': cpu_percent + memory_percent
                        }
                    except (psutil.NoSuchProcess, psutil.AccessDenied):
                        continue
            
            # Sort by performance score and adjust priorities
            sorted_processes = sorted(process_metrics.items(), key=lambda x: x[1]['score'], reverse=True)
            
            for i, (pid, metrics) in enumerate(sorted_processes):
                try:
                    # Higher performing processes get higher priority
                    new_priority = max(-10, min(10, 10 - i))
                    metrics['process'].nice(new_priority)
                except Exception as e:
                    logger.error(f"Error optimizing priority for process {pid}: {e}")
                    
        except Exception as e:
            logger.error(f"Error optimizing process priorities: {e}")
    
    def _cleanup_dead_processes(self):
        """Clean up dead or zombie processes"""
        try:
            for proc in psutil.process_iter(['pid', 'status']):
                try:
                    if proc.info['status'] == 'zombie':
                        logger.info(f"Cleaning up zombie process {proc.pid}")
                        # In a real implementation, this would properly clean up the process
                except (psutil.NoSuchProcess, psutil.AccessDenied):
                    continue
        except Exception as e:
            logger.error(f"Error cleaning up dead processes: {e}")
    
    def save_resource_allocation(self, allocation: ResourceAllocation) -> bool:
        """Save resource allocation configuration"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO resource_allocations 
                (language, cpu_limit, memory_limit, io_limit, network_limit, priority, max_processes, created_at, updated_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                allocation.language,
                allocation.cpu_limit,
                allocation.memory_limit,
                allocation.io_limit,
                allocation.network_limit,
                allocation.priority,
                allocation.max_processes,
                datetime.now().isoformat(),
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            logger.error(f"Failed to save resource allocation: {e}")
            return False
    
    def save_scaling_policy(self, policy: ScalingPolicy) -> bool:
        """Save scaling policy configuration"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO scaling_policies 
                (language, min_instances, max_instances, cpu_threshold_up, cpu_threshold_down,
                 memory_threshold_up, memory_threshold_down, scale_up_cooldown, scale_down_cooldown, enabled, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                policy.language,
                policy.min_instances,
                policy.max_instances,
                policy.cpu_threshold_up,
                policy.cpu_threshold_down,
                policy.memory_threshold_up,
                policy.memory_threshold_down,
                policy.scale_up_cooldown,
                policy.scale_down_cooldown,
                policy.enabled,
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            logger.error(f"Failed to save scaling policy: {e}")
            return False
    
    def _record_optimization_action(self, action: OptimizationAction):
        """Record optimization action"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO optimization_actions 
                (action_id, language, action_type, target_process_id, parameters, reason, timestamp, success, duration)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                action.action_id,
                action.language,
                action.action_type,
                action.target_process_id,
                json.dumps(action.parameters) if action.parameters else None,
                action.reason,
                action.timestamp.isoformat(),
                action.success,
                action.duration
            ))
            
            conn.commit()
            conn.close()
            
            # Store in memory
            self.optimization_history.append(action)
            
        except Exception as e:
            logger.error(f"Failed to record optimization action: {e}")
    
    def get_optimization_history(self, language: str = None, 
                               time_range: timedelta = None) -> List[OptimizationAction]:
        """Get optimization action history"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            query = 'SELECT * FROM optimization_actions'
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
            
            actions = []
            for row in cursor.fetchall():
                id, action_id, language, action_type, target_process_id, parameters, reason, timestamp, success, duration = row
                
                actions.append(OptimizationAction(
                    action_id=action_id,
                    language=language,
                    action_type=action_type,
                    target_process_id=target_process_id,
                    parameters=json.loads(parameters) if parameters else None,
                    reason=reason,
                    timestamp=datetime.fromisoformat(timestamp),
                    success=bool(success),
                    duration=duration
                ))
            
            conn.close()
            return actions
            
        except Exception as e:
            logger.error(f"Failed to get optimization history: {e}")
            return []

def main():
    """CLI for performance optimizer"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang Performance Optimizer')
    parser.add_argument('--start', action='store_true', help='Start optimization')
    parser.add_argument('--stop', action='store_true', help='Stop optimization')
    parser.add_argument('--status', action='store_true', help='Show optimization status')
    parser.add_argument('--history', help='Show optimization history for language')
    parser.add_argument('--set-allocation', nargs=7, metavar=('LANG', 'CPU', 'MEM', 'IO', 'NET', 'PRIO', 'MAX'), help='Set resource allocation')
    parser.add_argument('--set-scaling', nargs=10, metavar=('LANG', 'MIN', 'MAX', 'CPU_UP', 'CPU_DOWN', 'MEM_UP', 'MEM_DOWN', 'UP_COOLDOWN', 'DOWN_COOLDOWN', 'ENABLED'), help='Set scaling policy')
    
    args = parser.parse_args()
    
    optimizer = MultiLanguagePerformanceOptimizer()
    
    if args.start:
        success = optimizer.start_optimization()
        print(f"Optimization started: {success}")
    
    elif args.stop:
        optimizer.stop_optimization()
        print("Optimization stopped")
    
    elif args.status:
        print(f"Optimization active: {optimizer.optimization_active}")
        print(f"Resource allocations: {len(optimizer.resource_allocations)}")
        print(f"Scaling policies: {len(optimizer.scaling_policies)}")
    
    elif args.history:
        history = optimizer.get_optimization_history(args.history)
        for action in history:
            print(f"{action.timestamp}: {action.action_type} for {action.language} - {action.reason}")
    
    elif args.set_allocation:
        lang, cpu, mem, io, net, prio, max_proc = args.set_allocation
        allocation = ResourceAllocation(
            language=lang,
            cpu_limit=float(cpu),
            memory_limit=int(mem),
            io_limit=int(io),
            network_limit=int(net),
            priority=int(prio),
            max_processes=int(max_proc)
        )
        success = optimizer.save_resource_allocation(allocation)
        print(f"Resource allocation saved: {success}")
    
    elif args.set_scaling:
        lang, min_inst, max_inst, cpu_up, cpu_down, mem_up, mem_down, up_cooldown, down_cooldown, enabled = args.set_scaling
        policy = ScalingPolicy(
            language=lang,
            min_instances=int(min_inst),
            max_instances=int(max_inst),
            cpu_threshold_up=float(cpu_up),
            cpu_threshold_down=float(cpu_down),
            memory_threshold_up=float(mem_up),
            memory_threshold_down=float(mem_down),
            scale_up_cooldown=int(up_cooldown),
            scale_down_cooldown=int(down_cooldown),
            enabled=enabled.lower() == 'true'
        )
        success = optimizer.save_scaling_policy(policy)
        print(f"Scaling policy saved: {success}")
    
    else:
        parser.print_help()

if __name__ == '__main__':
    main() 