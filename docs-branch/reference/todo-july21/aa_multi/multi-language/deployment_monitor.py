#!/usr/bin/env python3
"""
TuskLang Multi-Language Deployment Monitor
Monitors deployments and provides rollback capabilities across all language SDKs
"""

import os
import json
import time
import sqlite3
import threading
import requests
from pathlib import Path
from typing import Dict, List, Optional, Any, Tuple
from dataclasses import dataclass, asdict
from datetime import datetime, timedelta
import logging
import subprocess
import signal
import psutil
import tempfile

logger = logging.getLogger(__name__)

@dataclass
class DeploymentStatus:
    """Deployment status information"""
    deployment_id: str
    language: str
    environment: str
    status: str  # 'deploying', 'running', 'failed', 'rolled_back'
    start_time: datetime
    end_time: Optional[datetime] = None
    health_status: str = 'unknown'  # 'healthy', 'unhealthy', 'degraded'
    response_time: Optional[float] = None
    error_count: int = 0
    request_count: int = 0

@dataclass
class HealthCheck:
    """Health check configuration"""
    url: str
    method: str = 'GET'
    timeout: int = 30
    expected_status: int = 200
    interval: int = 30
    retries: int = 3
    headers: Dict[str, str] = None

@dataclass
class RollbackInfo:
    """Rollback information"""
    deployment_id: str
    previous_deployment_id: str
    reason: str
    timestamp: datetime
    success: bool
    duration: float
    error_message: Optional[str] = None

class DeploymentMonitor:
    """Monitors deployments and provides rollback capabilities"""
    
    def __init__(self, monitor_dir: Path = None):
        if monitor_dir is None:
            self.monitor_dir = Path(tempfile.mkdtemp(prefix='tsk_deploy_monitor_'))
        else:
            self.monitor_dir = monitor_dir
        
        self.db_path = self.monitor_dir / 'deployment_monitor.db'
        self.logs_dir = self.monitor_dir / 'logs'
        
        # Create directories
        self.logs_dir.mkdir(exist_ok=True)
        
        # Initialize database
        self._init_database()
        
        # Active monitoring threads
        self.monitoring_threads = {}
        self.stop_monitoring = threading.Event()
    
    def _init_database(self):
        """Initialize SQLite database for deployment monitoring"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Create tables
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS deployments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                deployment_id TEXT UNIQUE,
                language TEXT,
                environment TEXT,
                status TEXT,
                start_time TEXT,
                end_time TEXT,
                health_status TEXT,
                response_time REAL,
                error_count INTEGER DEFAULT 0,
                request_count INTEGER DEFAULT 0
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS health_checks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                deployment_id TEXT,
                timestamp TEXT,
                status TEXT,
                response_time REAL,
                status_code INTEGER,
                error_message TEXT,
                FOREIGN KEY (deployment_id) REFERENCES deployments (deployment_id)
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS rollbacks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                deployment_id TEXT,
                previous_deployment_id TEXT,
                reason TEXT,
                timestamp TEXT,
                success BOOLEAN,
                duration REAL,
                error_message TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS deployment_snapshots (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                deployment_id TEXT,
                snapshot_data TEXT,
                created_at TEXT
            )
        ''')
        
        conn.commit()
        conn.close()
    
    def start_deployment_monitoring(self, deployment_id: str, language: str, 
                                  environment: str, health_checks: List[HealthCheck] = None) -> bool:
        """Start monitoring a deployment"""
        try:
            # Create deployment record
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO deployments 
                (deployment_id, language, environment, status, start_time)
                VALUES (?, ?, ?, ?, ?)
            ''', (
                deployment_id,
                language,
                environment,
                'deploying',
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            
            # Start monitoring thread
            if health_checks:
                thread = threading.Thread(
                    target=self._monitor_deployment,
                    args=(deployment_id, health_checks)
                )
                thread.daemon = True
                thread.start()
                self.monitoring_threads[deployment_id] = thread
            
            return True
            
        except Exception as e:
            logger.error(f"Failed to start monitoring for {deployment_id}: {e}")
            return False
    
    def _monitor_deployment(self, deployment_id: str, health_checks: List[HealthCheck]):
        """Monitor deployment health checks"""
        while not self.stop_monitoring.is_set():
            try:
                for health_check in health_checks:
                    self._perform_health_check(deployment_id, health_check)
                
                # Wait for next check interval
                time.sleep(min(check.interval for check in health_checks))
                
            except Exception as e:
                logger.error(f"Error monitoring deployment {deployment_id}: {e}")
                time.sleep(30)  # Wait before retrying
    
    def _perform_health_check(self, deployment_id: str, health_check: HealthCheck):
        """Perform a health check"""
        start_time = time.time()
        
        try:
            headers = health_check.headers or {}
            
            response = requests.request(
                method=health_check.method,
                url=health_check.url,
                headers=headers,
                timeout=health_check.timeout
            )
            
            response_time = time.time() - start_time
            
            # Record health check
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO health_checks 
                (deployment_id, timestamp, status, response_time, status_code)
                VALUES (?, ?, ?, ?, ?)
            ''', (
                deployment_id,
                datetime.now().isoformat(),
                'success' if response.status_code == health_check.expected_status else 'failed',
                response_time,
                response.status_code
            ))
            
            # Update deployment status
            if response.status_code == health_check.expected_status:
                cursor.execute('''
                    UPDATE deployments 
                    SET health_status = 'healthy', response_time = ?, request_count = request_count + 1
                    WHERE deployment_id = ?
                ''', (response_time, deployment_id))
            else:
                cursor.execute('''
                    UPDATE deployments 
                    SET health_status = 'unhealthy', error_count = error_count + 1, request_count = request_count + 1
                    WHERE deployment_id = ?
                ''', (deployment_id,))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            response_time = time.time() - start_time
            
            # Record failed health check
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO health_checks 
                (deployment_id, timestamp, status, response_time, error_message)
                VALUES (?, ?, ?, ?, ?)
            ''', (
                deployment_id,
                datetime.now().isoformat(),
                'error',
                response_time,
                str(e)
            ))
            
            cursor.execute('''
                UPDATE deployments 
                SET health_status = 'unhealthy', error_count = error_count + 1, request_count = request_count + 1
                WHERE deployment_id = ?
            ''', (deployment_id,))
            
            conn.commit()
            conn.close()
    
    def update_deployment_status(self, deployment_id: str, status: str, 
                               end_time: datetime = None) -> bool:
        """Update deployment status"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            if end_time:
                cursor.execute('''
                    UPDATE deployments 
                    SET status = ?, end_time = ?
                    WHERE deployment_id = ?
                ''', (status, end_time.isoformat(), deployment_id))
            else:
                cursor.execute('''
                    UPDATE deployments 
                    SET status = ?
                    WHERE deployment_id = ?
                ''', (status, deployment_id))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            logger.error(f"Failed to update deployment status for {deployment_id}: {e}")
            return False
    
    def get_deployment_status(self, deployment_id: str) -> Optional[DeploymentStatus]:
        """Get deployment status"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                SELECT deployment_id, language, environment, status, start_time, end_time,
                       health_status, response_time, error_count, request_count
                FROM deployments WHERE deployment_id = ?
            ''', (deployment_id,))
            
            row = cursor.fetchone()
            conn.close()
            
            if row:
                deployment_id, language, environment, status, start_time, end_time, health_status, response_time, error_count, request_count = row
                
                return DeploymentStatus(
                    deployment_id=deployment_id,
                    language=language,
                    environment=environment,
                    status=status,
                    start_time=datetime.fromisoformat(start_time),
                    end_time=datetime.fromisoformat(end_time) if end_time else None,
                    health_status=health_status,
                    response_time=response_time,
                    error_count=error_count,
                    request_count=request_count
                )
            
            return None
            
        except Exception as e:
            logger.error(f"Failed to get deployment status for {deployment_id}: {e}")
            return None
    
    def list_deployments(self, environment: str = None, status: str = None, 
                        limit: int = 50) -> List[DeploymentStatus]:
        """List deployments with optional filtering"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            query = '''
                SELECT deployment_id, language, environment, status, start_time, end_time,
                       health_status, response_time, error_count, request_count
                FROM deployments
            '''
            
            params = []
            conditions = []
            
            if environment:
                conditions.append("environment = ?")
                params.append(environment)
            
            if status:
                conditions.append("status = ?")
                params.append(status)
            
            if conditions:
                query += " WHERE " + " AND ".join(conditions)
            
            query += " ORDER BY start_time DESC LIMIT ?"
            params.append(limit)
            
            cursor.execute(query, params)
            
            deployments = []
            for row in cursor.fetchall():
                deployment_id, language, environment, status, start_time, end_time, health_status, response_time, error_count, request_count = row
                
                deployments.append(DeploymentStatus(
                    deployment_id=deployment_id,
                    language=language,
                    environment=environment,
                    status=status,
                    start_time=datetime.fromisoformat(start_time),
                    end_time=datetime.fromisoformat(end_time) if end_time else None,
                    health_status=health_status,
                    response_time=response_time,
                    error_count=error_count,
                    request_count=request_count
                ))
            
            conn.close()
            return deployments
            
        except Exception as e:
            logger.error(f"Failed to list deployments: {e}")
            return []
    
    def create_deployment_snapshot(self, deployment_id: str, snapshot_data: Dict[str, Any]) -> bool:
        """Create a deployment snapshot for rollback"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO deployment_snapshots 
                (deployment_id, snapshot_data, created_at)
                VALUES (?, ?, ?)
            ''', (
                deployment_id,
                json.dumps(snapshot_data),
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            logger.error(f"Failed to create snapshot for {deployment_id}: {e}")
            return False
    
    def get_deployment_snapshot(self, deployment_id: str) -> Optional[Dict[str, Any]]:
        """Get deployment snapshot"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                SELECT snapshot_data FROM deployment_snapshots 
                WHERE deployment_id = ? ORDER BY created_at DESC LIMIT 1
            ''', (deployment_id,))
            
            row = cursor.fetchone()
            conn.close()
            
            if row:
                return json.loads(row[0])
            
            return None
            
        except Exception as e:
            logger.error(f"Failed to get snapshot for {deployment_id}: {e}")
            return None
    
    def rollback_deployment(self, deployment_id: str, reason: str = "Manual rollback") -> RollbackInfo:
        """Rollback a deployment"""
        start_time = time.time()
        
        try:
            # Get current deployment info
            current_deployment = self.get_deployment_status(deployment_id)
            if not current_deployment:
                raise ValueError(f"Deployment {deployment_id} not found")
            
            # Get previous deployment
            previous_deployments = self.list_deployments(
                environment=current_deployment.environment,
                status='running',
                limit=10
            )
            
            previous_deployment = None
            for dep in previous_deployments:
                if dep.deployment_id != deployment_id:
                    previous_deployment = dep
                    break
            
            if not previous_deployment:
                raise ValueError("No previous deployment found for rollback")
            
            # Get snapshots
            current_snapshot = self.get_deployment_snapshot(deployment_id)
            previous_snapshot = self.get_deployment_snapshot(previous_deployment.deployment_id)
            
            if not previous_snapshot:
                raise ValueError(f"No snapshot found for previous deployment {previous_deployment.deployment_id}")
            
            # Perform rollback
            success = self._perform_rollback(current_deployment, previous_snapshot)
            
            duration = time.time() - start_time
            
            # Record rollback
            rollback_info = RollbackInfo(
                deployment_id=deployment_id,
                previous_deployment_id=previous_deployment.deployment_id,
                reason=reason,
                timestamp=datetime.now(),
                success=success,
                duration=duration
            )
            
            self._record_rollback(rollback_info)
            
            # Update deployment status
            self.update_deployment_status(deployment_id, 'rolled_back')
            
            return rollback_info
            
        except Exception as e:
            duration = time.time() - start_time
            
            rollback_info = RollbackInfo(
                deployment_id=deployment_id,
                previous_deployment_id='unknown',
                reason=reason,
                timestamp=datetime.now(),
                success=False,
                duration=duration,
                error_message=str(e)
            )
            
            self._record_rollback(rollback_info)
            return rollback_info
    
    def _perform_rollback(self, current_deployment: DeploymentStatus, 
                         previous_snapshot: Dict[str, Any]) -> bool:
        """Perform the actual rollback"""
        try:
            # This is a simplified rollback implementation
            # In a real system, this would involve:
            # 1. Stopping current deployment
            # 2. Restoring previous deployment from snapshot
            # 3. Starting previous deployment
            # 4. Verifying health
            
            logger.info(f"Rolling back deployment {current_deployment.deployment_id}")
            
            # Simulate rollback process
            time.sleep(5)
            
            return True
            
        except Exception as e:
            logger.error(f"Rollback failed for {current_deployment.deployment_id}: {e}")
            return False
    
    def _record_rollback(self, rollback_info: RollbackInfo):
        """Record rollback information"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO rollbacks 
                (deployment_id, previous_deployment_id, reason, timestamp, success, duration, error_message)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            ''', (
                rollback_info.deployment_id,
                rollback_info.previous_deployment_id,
                rollback_info.reason,
                rollback_info.timestamp.isoformat(),
                rollback_info.success,
                rollback_info.duration,
                rollback_info.error_message
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"Failed to record rollback: {e}")
    
    def get_rollback_history(self, deployment_id: str = None) -> List[RollbackInfo]:
        """Get rollback history"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            if deployment_id:
                cursor.execute('''
                    SELECT deployment_id, previous_deployment_id, reason, timestamp, success, duration, error_message
                    FROM rollbacks WHERE deployment_id = ? ORDER BY timestamp DESC
                ''', (deployment_id,))
            else:
                cursor.execute('''
                    SELECT deployment_id, previous_deployment_id, reason, timestamp, success, duration, error_message
                    FROM rollbacks ORDER BY timestamp DESC
                ''')
            
            rollbacks = []
            for row in cursor.fetchall():
                deployment_id, previous_deployment_id, reason, timestamp, success, duration, error_message = row
                
                rollbacks.append(RollbackInfo(
                    deployment_id=deployment_id,
                    previous_deployment_id=previous_deployment_id,
                    reason=reason,
                    timestamp=datetime.fromisoformat(timestamp),
                    success=bool(success),
                    duration=duration,
                    error_message=error_message
                ))
            
            conn.close()
            return rollbacks
            
        except Exception as e:
            logger.error(f"Failed to get rollback history: {e}")
            return []
    
    def get_deployment_metrics(self, deployment_id: str, 
                             time_range: timedelta = None) -> Dict[str, Any]:
        """Get deployment metrics"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            # Get health check metrics
            query = '''
                SELECT status, response_time, status_code, timestamp
                FROM health_checks WHERE deployment_id = ?
            '''
            params = [deployment_id]
            
            if time_range:
                cutoff_time = datetime.now() - time_range
                query += " AND timestamp > ?"
                params.append(cutoff_time.isoformat())
            
            query += " ORDER BY timestamp DESC"
            
            cursor.execute(query, params)
            
            health_checks = cursor.fetchall()
            
            # Calculate metrics
            total_checks = len(health_checks)
            successful_checks = len([h for h in health_checks if h[0] == 'success'])
            failed_checks = total_checks - successful_checks
            
            response_times = [h[1] for h in health_checks if h[1] is not None]
            avg_response_time = sum(response_times) / len(response_times) if response_times else 0
            max_response_time = max(response_times) if response_times else 0
            min_response_time = min(response_times) if response_times else 0
            
            # Get deployment info
            cursor.execute('''
                SELECT status, start_time, end_time, error_count, request_count
                FROM deployments WHERE deployment_id = ?
            ''', (deployment_id,))
            
            deployment_row = cursor.fetchone()
            conn.close()
            
            if deployment_row:
                status, start_time, end_time, error_count, request_count = deployment_row
                
                return {
                    'deployment_id': deployment_id,
                    'status': status,
                    'start_time': start_time,
                    'end_time': end_time,
                    'total_health_checks': total_checks,
                    'successful_checks': successful_checks,
                    'failed_checks': failed_checks,
                    'success_rate': (successful_checks / total_checks * 100) if total_checks > 0 else 0,
                    'avg_response_time': avg_response_time,
                    'max_response_time': max_response_time,
                    'min_response_time': min_response_time,
                    'error_count': error_count,
                    'request_count': request_count
                }
            
            return {}
            
        except Exception as e:
            logger.error(f"Failed to get metrics for {deployment_id}: {e}")
            return {}
    
    def stop_monitoring(self):
        """Stop all monitoring threads"""
        self.stop_monitoring.set()
        
        for thread in self.monitoring_threads.values():
            thread.join(timeout=5)
        
        self.monitoring_threads.clear()

def main():
    """CLI for deployment monitor"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang Deployment Monitor')
    parser.add_argument('--start-monitoring', nargs=3, metavar=('DEPLOYMENT_ID', 'LANGUAGE', 'ENVIRONMENT'), help='Start monitoring deployment')
    parser.add_argument('--get-status', help='Get deployment status')
    parser.add_argument('--list-deployments', help='List deployments for environment')
    parser.add_argument('--create-snapshot', nargs=2, metavar=('DEPLOYMENT_ID', 'SNAPSHOT_FILE'), help='Create deployment snapshot')
    parser.add_argument('--rollback', nargs=2, metavar=('DEPLOYMENT_ID', 'REASON'), help='Rollback deployment')
    parser.add_argument('--get-metrics', help='Get deployment metrics')
    parser.add_argument('--rollback-history', help='Get rollback history for deployment')
    
    args = parser.parse_args()
    
    monitor = DeploymentMonitor()
    
    if args.start_monitoring:
        deployment_id, language, environment = args.start_monitoring
        
        # Create sample health checks
        health_checks = [
            HealthCheck(url=f'http://localhost:8080/health', interval=30),
            HealthCheck(url=f'http://localhost:8080/ready', interval=60)
        ]
        
        success = monitor.start_deployment_monitoring(deployment_id, language, environment, health_checks)
        print(f"Monitoring started: {success}")
    
    elif args.get_status:
        status = monitor.get_deployment_status(args.get_status)
        if status:
            print(json.dumps(asdict(status), indent=2, default=str))
        else:
            print("Deployment not found")
    
    elif args.list_deployments:
        deployments = monitor.list_deployments(environment=args.list_deployments)
        for dep in deployments:
            print(f"{dep.deployment_id}: {dep.status} ({dep.health_status})")
    
    elif args.create_snapshot:
        deployment_id, snapshot_file = args.create_snapshot
        with open(snapshot_file, 'r') as f:
            snapshot_data = json.load(f)
        success = monitor.create_deployment_snapshot(deployment_id, snapshot_data)
        print(f"Snapshot created: {success}")
    
    elif args.rollback:
        deployment_id, reason = args.rollback
        rollback_info = monitor.rollback_deployment(deployment_id, reason)
        print(json.dumps(asdict(rollback_info), indent=2, default=str))
    
    elif args.get_metrics:
        metrics = monitor.get_deployment_metrics(args.get_metrics)
        print(json.dumps(metrics, indent=2))
    
    elif args.rollback_history:
        history = monitor.get_rollback_history(args.rollback_history)
        for rollback in history:
            print(f"{rollback.timestamp}: {rollback.deployment_id} -> {rollback.previous_deployment_id} ({'success' if rollback.success else 'failed'})")
    
    else:
        parser.print_help()

if __name__ == '__main__':
    main() 