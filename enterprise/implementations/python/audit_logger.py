#!/usr/bin/env python3
"""
TuskLang Audit Logging System
Comprehensive audit logging for compliance and monitoring
"""

import os
import json
import time
import uuid
import hashlib
import threading
from typing import Dict, List, Optional, Any, Union
from dataclasses import dataclass, asdict
from pathlib import Path
from datetime import datetime, timezone, timedelta
import logging
import sqlite3
import queue
from concurrent.futures import ThreadPoolExecutor
import gzip
import shutil

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@dataclass
class AuditEvent:
    """Audit event structure"""
    event_id: str
    timestamp: float
    user_id: str
    session_id: str
    action: str
    resource: str
    resource_type: str
    resource_id: str
    details: Dict[str, Any]
    ip_address: str
    user_agent: str
    success: bool
    error_message: Optional[str] = None
    metadata: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.metadata is None:
            self.metadata = {}

@dataclass
class AuditConfig:
    """Audit logging configuration"""
    enabled: bool = True
    log_level: str = 'INFO'
    storage_type: str = 'file'  # 'file', 'database', 'syslog'
    storage_path: str = '/var/log/tusklang/audit'
    max_file_size: int = 100 * 1024 * 1024  # 100MB
    max_files: int = 10
    retention_days: int = 90
    compression: bool = True
    encryption: bool = False
    encryption_key: Optional[str] = None
    batch_size: int = 100
    flush_interval: int = 5  # seconds
    sensitive_fields: List[str] = None
    excluded_actions: List[str] = None
    included_actions: List[str] = None
    
    def __post_init__(self):
        if self.sensitive_fields is None:
            self.sensitive_fields = ['password', 'token', 'secret', 'key']
        if self.excluded_actions is None:
            self.excluded_actions = []
        if self.included_actions is None:
            self.included_actions = []

class AuditLogger:
    """Comprehensive audit logging system"""
    
    def __init__(self, config: AuditConfig):
        self.config = config
        self.event_queue = queue.Queue()
        self.batch_events = []
        self.last_flush = time.time()
        self.lock = threading.Lock()
        self.running = True
        
        # Initialize storage
        self._init_storage()
        
        # Start background processing
        self._start_background_processing()
    
    def _init_storage(self):
        """Initialize storage backend"""
        if self.config.storage_type == 'file':
            self._init_file_storage()
        elif self.config.storage_type == 'database':
            self._init_database_storage()
        elif self.config.storage_type == 'syslog':
            self._init_syslog_storage()
        else:
            raise ValueError(f"Unsupported storage type: {self.config.storage_type}")
    
    def _init_file_storage(self):
        """Initialize file-based storage"""
        self.storage_path = Path(self.config.storage_path)
        self.storage_path.mkdir(parents=True, exist_ok=True)
        
        # Create current log file
        self.current_log_file = self.storage_path / f"audit_{datetime.now().strftime('%Y%m%d')}.log"
        
        # Create index file for quick lookups
        self.index_file = self.storage_path / "audit_index.json"
        self._load_index()
    
    def _init_database_storage(self):
        """Initialize database storage"""
        db_path = Path(self.config.storage_path) / "audit.db"
        db_path.parent.mkdir(parents=True, exist_ok=True)
        
        self.db_conn = sqlite3.connect(str(db_path), check_same_thread=False)
        self._create_audit_tables()
    
    def _init_syslog_storage(self):
        """Initialize syslog storage"""
        import syslog
        syslog.openlog('TuskLang-Audit', syslog.LOG_PID, syslog.LOG_LOCAL0)
        self.syslog = syslog
    
    def _create_audit_tables(self):
        """Create audit database tables"""
        cursor = self.db_conn.cursor()
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS audit_events (
                event_id TEXT PRIMARY KEY,
                timestamp REAL NOT NULL,
                user_id TEXT NOT NULL,
                session_id TEXT NOT NULL,
                action TEXT NOT NULL,
                resource TEXT NOT NULL,
                resource_type TEXT NOT NULL,
                resource_id TEXT NOT NULL,
                details TEXT NOT NULL,
                ip_address TEXT NOT NULL,
                user_agent TEXT NOT NULL,
                success INTEGER NOT NULL,
                error_message TEXT,
                metadata TEXT NOT NULL,
                created_at REAL NOT NULL
            )
        ''')
        
        cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_audit_timestamp ON audit_events(timestamp)
        ''')
        
        cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_audit_user_id ON audit_events(user_id)
        ''')
        
        cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_audit_action ON audit_events(action)
        ''')
        
        cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_audit_resource ON audit_events(resource_type, resource_id)
        ''')
        
        self.db_conn.commit()
    
    def _load_index(self):
        """Load audit index"""
        if self.index_file.exists():
            try:
                with open(self.index_file, 'r') as f:
                    self.audit_index = json.load(f)
            except Exception as e:
                logger.warning(f"Failed to load audit index: {e}")
                self.audit_index = {}
        else:
            self.audit_index = {}
    
    def _save_index(self):
        """Save audit index"""
        try:
            with open(self.index_file, 'w') as f:
                json.dump(self.audit_index, f, indent=2)
        except Exception as e:
            logger.error(f"Failed to save audit index: {e}")
    
    def _start_background_processing(self):
        """Start background processing thread"""
        self.background_thread = threading.Thread(target=self._background_processor, daemon=True)
        self.background_thread.start()
    
    def _background_processor(self):
        """Background processor for audit events"""
        while self.running:
            try:
                # Process events from queue
                while not self.event_queue.empty():
                    event = self.event_queue.get_nowait()
                    self.batch_events.append(event)
                
                # Flush batch if full or time elapsed
                current_time = time.time()
                if (len(self.batch_events) >= self.config.batch_size or 
                    current_time - self.last_flush >= self.config.flush_interval):
                    self._flush_events()
                
                time.sleep(0.1)  # Small delay to prevent busy waiting
                
            except Exception as e:
                logger.error(f"Background processor error: {e}")
                time.sleep(1)
    
    def _flush_events(self):
        """Flush batched events to storage"""
        if not self.batch_events:
            return
        
        with self.lock:
            try:
                if self.config.storage_type == 'file':
                    self._flush_to_file()
                elif self.config.storage_type == 'database':
                    self._flush_to_database()
                elif self.config.storage_type == 'syslog':
                    self._flush_to_syslog()
                
                self.batch_events.clear()
                self.last_flush = time.time()
                
            except Exception as e:
                logger.error(f"Failed to flush audit events: {e}")
    
    def _flush_to_file(self):
        """Flush events to file"""
        for event in self.batch_events:
            event_dict = asdict(event)
            
            # Sanitize sensitive data
            event_dict = self._sanitize_event(event_dict)
            
            # Write to current log file
            with open(self.current_log_file, 'a') as f:
                f.write(json.dumps(event_dict) + '\n')
            
            # Update index
            self._update_index(event)
            
            # Check if file rotation is needed
            if self.current_log_file.stat().st_size > self.config.max_file_size:
                self._rotate_log_file()
        
        # Save index
        self._save_index()
    
    def _flush_to_database(self):
        """Flush events to database"""
        cursor = self.db_conn.cursor()
        
        for event in self.batch_events:
            cursor.execute('''
                INSERT INTO audit_events 
                (event_id, timestamp, user_id, session_id, action, resource, 
                 resource_type, resource_id, details, ip_address, user_agent, 
                 success, error_message, metadata, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                event.event_id, event.timestamp, event.user_id, event.session_id,
                event.action, event.resource, event.resource_type, event.resource_id,
                json.dumps(event.details), event.ip_address, event.user_agent,
                1 if event.success else 0, event.error_message,
                json.dumps(event.metadata), time.time()
            ))
        
        self.db_conn.commit()
    
    def _flush_to_syslog(self):
        """Flush events to syslog"""
        for event in self.batch_events:
            level = self.syslog.LOG_INFO if event.success else self.syslog.LOG_WARNING
            message = f"User {event.user_id} performed {event.action} on {event.resource_type}:{event.resource_id}"
            
            if not event.success and event.error_message:
                message += f" - Error: {event.error_message}"
            
            self.syslog.syslog(level, message)
    
    def _sanitize_event(self, event_dict: Dict[str, Any]) -> Dict[str, Any]:
        """Sanitize sensitive data from event"""
        def sanitize_value(value):
            if isinstance(value, str):
                for field in self.config.sensitive_fields:
                    if field.lower() in value.lower():
                        return '[REDACTED]'
            elif isinstance(value, dict):
                return {k: sanitize_value(v) for k, v in value.items()}
            elif isinstance(value, list):
                return [sanitize_value(v) for v in value]
            return value
        
        return sanitize_value(event_dict)
    
    def _update_index(self, event: AuditEvent):
        """Update audit index"""
        date_key = datetime.fromtimestamp(event.timestamp).strftime('%Y%m%d')
        
        if date_key not in self.audit_index:
            self.audit_index[date_key] = {
                'files': [],
                'event_count': 0,
                'users': set(),
                'actions': set()
            }
        
        self.audit_index[date_key]['event_count'] += 1
        self.audit_index[date_key]['users'].add(event.user_id)
        self.audit_index[date_key]['actions'].add(event.action)
        
        # Convert sets to lists for JSON serialization
        self.audit_index[date_key]['users'] = list(self.audit_index[date_key]['users'])
        self.audit_index[date_key]['actions'] = list(self.audit_index[date_key]['actions'])
    
    def _rotate_log_file(self):
        """Rotate log file"""
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        rotated_file = self.storage_path / f"audit_{timestamp}.log"
        
        # Move current file
        shutil.move(self.current_log_file, rotated_file)
        
        # Compress if enabled
        if self.config.compression:
            with open(rotated_file, 'rb') as f_in:
                with gzip.open(f"{rotated_file}.gz", 'wb') as f_out:
                    shutil.copyfileobj(f_in, f_out)
            rotated_file.unlink()  # Remove uncompressed file
        
        # Update index
        date_key = datetime.now().strftime('%Y%m%d')
        if date_key not in self.audit_index:
            self.audit_index[date_key] = {'files': [], 'event_count': 0, 'users': [], 'actions': []}
        
        self.audit_index[date_key]['files'].append(str(rotated_file))
        
        # Create new current file
        self.current_log_file = self.storage_path / f"audit_{datetime.now().strftime('%Y%m%d')}.log"
        
        # Cleanup old files
        self._cleanup_old_files()
    
    def _cleanup_old_files(self):
        """Clean up old audit files"""
        cutoff_date = datetime.now() - timedelta(days=self.config.retention_days)
        cutoff_timestamp = cutoff_date.timestamp()
        
        # Clean up old files
        for log_file in self.storage_path.glob('audit_*.log*'):
            if log_file.stat().st_mtime < cutoff_timestamp:
                log_file.unlink()
                logger.info(f"Removed old audit file: {log_file}")
        
        # Clean up old index entries
        old_keys = [k for k in self.audit_index.keys() if k < cutoff_date.strftime('%Y%m%d')]
        for key in old_keys:
            del self.audit_index[key]
    
    def log_event(self, user_id: str, session_id: str, action: str, resource: str,
                  resource_type: str, resource_id: str, details: Dict[str, Any],
                  ip_address: str, user_agent: str, success: bool = True,
                  error_message: Optional[str] = None, metadata: Optional[Dict[str, Any]] = None):
        """Log an audit event"""
        if not self.config.enabled:
            return
        
        # Check if action should be logged
        if self.config.excluded_actions and action in self.config.excluded_actions:
            return
        
        if self.config.included_actions and action not in self.config.included_actions:
            return
        
        # Create audit event
        event = AuditEvent(
            event_id=str(uuid.uuid4()),
            timestamp=time.time(),
            user_id=user_id,
            session_id=session_id,
            action=action,
            resource=resource,
            resource_type=resource_type,
            resource_id=resource_id,
            details=details,
            ip_address=ip_address,
            user_agent=user_agent,
            success=success,
            error_message=error_message,
            metadata=metadata or {}
        )
        
        # Add to queue for background processing
        self.event_queue.put(event)
    
    def query_events(self, filters: Dict[str, Any] = None, limit: int = 1000,
                    offset: int = 0, order_by: str = 'timestamp', order_dir: str = 'DESC') -> List[AuditEvent]:
        """Query audit events"""
        if self.config.storage_type == 'database':
            return self._query_database(filters, limit, offset, order_by, order_dir)
        elif self.config.storage_type == 'file':
            return self._query_file(filters, limit, offset, order_by, order_dir)
        else:
            raise ValueError("Query not supported for this storage type")
    
    def _query_database(self, filters: Dict[str, Any], limit: int, offset: int,
                       order_by: str, order_dir: str) -> List[AuditEvent]:
        """Query events from database"""
        query = "SELECT * FROM audit_events WHERE 1=1"
        params = []
        
        if filters:
            if 'user_id' in filters:
                query += " AND user_id = ?"
                params.append(filters['user_id'])
            
            if 'action' in filters:
                query += " AND action = ?"
                params.append(filters['action'])
            
            if 'resource_type' in filters:
                query += " AND resource_type = ?"
                params.append(filters['resource_type'])
            
            if 'resource_id' in filters:
                query += " AND resource_id = ?"
                params.append(filters['resource_id'])
            
            if 'start_time' in filters:
                query += " AND timestamp >= ?"
                params.append(filters['start_time'])
            
            if 'end_time' in filters:
                query += " AND timestamp <= ?"
                params.append(filters['end_time'])
            
            if 'success' in filters:
                query += " AND success = ?"
                params.append(1 if filters['success'] else 0)
        
        query += f" ORDER BY {order_by} {order_dir}"
        query += f" LIMIT {limit} OFFSET {offset}"
        
        cursor = self.db_conn.cursor()
        cursor.execute(query, params)
        
        events = []
        for row in cursor.fetchall():
            event = AuditEvent(
                event_id=row[0],
                timestamp=row[1],
                user_id=row[2],
                session_id=row[3],
                action=row[4],
                resource=row[5],
                resource_type=row[6],
                resource_id=row[7],
                details=json.loads(row[8]),
                ip_address=row[9],
                user_agent=row[10],
                success=bool(row[11]),
                error_message=row[12],
                metadata=json.loads(row[13])
            )
            events.append(event)
        
        return events
    
    def _query_file(self, filters: Dict[str, Any], limit: int, offset: int,
                   order_by: str, order_dir: str) -> List[AuditEvent]:
        """Query events from file storage"""
        events = []
        
        # Get all log files
        log_files = sorted(self.storage_path.glob('audit_*.log'))
        
        for log_file in log_files:
            with open(log_file, 'r') as f:
                for line in f:
                    try:
                        event_data = json.loads(line.strip())
                        event = AuditEvent(**event_data)
                        
                        # Apply filters
                        if self._matches_filters(event, filters):
                            events.append(event)
                    except Exception as e:
                        logger.warning(f"Failed to parse audit event: {e}")
        
        # Sort events
        reverse = order_dir.upper() == 'DESC'
        if order_by == 'timestamp':
            events.sort(key=lambda x: x.timestamp, reverse=reverse)
        elif order_by == 'user_id':
            events.sort(key=lambda x: x.user_id, reverse=reverse)
        elif order_by == 'action':
            events.sort(key=lambda x: x.action, reverse=reverse)
        
        # Apply limit and offset
        return events[offset:offset + limit]
    
    def _matches_filters(self, event: AuditEvent, filters: Dict[str, Any]) -> bool:
        """Check if event matches filters"""
        if not filters:
            return True
        
        if 'user_id' in filters and event.user_id != filters['user_id']:
            return False
        
        if 'action' in filters and event.action != filters['action']:
            return False
        
        if 'resource_type' in filters and event.resource_type != filters['resource_type']:
            return False
        
        if 'resource_id' in filters and event.resource_id != filters['resource_id']:
            return False
        
        if 'start_time' in filters and event.timestamp < filters['start_time']:
            return False
        
        if 'end_time' in filters and event.timestamp > filters['end_time']:
            return False
        
        if 'success' in filters and event.success != filters['success']:
            return False
        
        return True
    
    def get_statistics(self, start_time: float = None, end_time: float = None) -> Dict[str, Any]:
        """Get audit statistics"""
        if self.config.storage_type == 'database':
            return self._get_database_statistics(start_time, end_time)
        else:
            return self._get_file_statistics(start_time, end_time)
    
    def _get_database_statistics(self, start_time: float, end_time: float) -> Dict[str, Any]:
        """Get statistics from database"""
        cursor = self.db_conn.cursor()
        
        where_clause = "WHERE 1=1"
        params = []
        
        if start_time:
            where_clause += " AND timestamp >= ?"
            params.append(start_time)
        
        if end_time:
            where_clause += " AND timestamp <= ?"
            params.append(end_time)
        
        # Total events
        cursor.execute(f"SELECT COUNT(*) FROM audit_events {where_clause}", params)
        total_events = cursor.fetchone()[0]
        
        # Success/failure ratio
        cursor.execute(f"SELECT success, COUNT(*) FROM audit_events {where_clause} GROUP BY success", params)
        success_counts = dict(cursor.fetchall())
        success_rate = (success_counts.get(1, 0) / total_events * 100) if total_events > 0 else 0
        
        # Top users
        cursor.execute(f"SELECT user_id, COUNT(*) FROM audit_events {where_clause} GROUP BY user_id ORDER BY COUNT(*) DESC LIMIT 10", params)
        top_users = dict(cursor.fetchall())
        
        # Top actions
        cursor.execute(f"SELECT action, COUNT(*) FROM audit_events {where_clause} GROUP BY action ORDER BY COUNT(*) DESC LIMIT 10", params)
        top_actions = dict(cursor.fetchall())
        
        # Top resources
        cursor.execute(f"SELECT resource_type, COUNT(*) FROM audit_events {where_clause} GROUP BY resource_type ORDER BY COUNT(*) DESC LIMIT 10", params)
        top_resources = dict(cursor.fetchall())
        
        return {
            'total_events': total_events,
            'success_rate': success_rate,
            'top_users': top_users,
            'top_actions': top_actions,
            'top_resources': top_resources,
            'time_range': {
                'start': start_time,
                'end': end_time
            }
        }
    
    def _get_file_statistics(self, start_time: float, end_time: float) -> Dict[str, Any]:
        """Get statistics from file storage"""
        # This would implement file-based statistics
        # For brevity, returning basic structure
        return {
            'total_events': 0,
            'success_rate': 0,
            'top_users': {},
            'top_actions': {},
            'top_resources': {},
            'time_range': {
                'start': start_time,
                'end': end_time
            }
        }
    
    def export_events(self, output_file: str, filters: Dict[str, Any] = None,
                     format: str = 'json') -> str:
        """Export audit events to file"""
        events = self.query_events(filters, limit=1000000)  # Large limit for export
        
        if format.lower() == 'json':
            with open(output_file, 'w') as f:
                json.dump([asdict(event) for event in events], f, indent=2)
        elif format.lower() == 'csv':
            import csv
            with open(output_file, 'w', newline='') as f:
                writer = csv.writer(f)
                writer.writerow(['event_id', 'timestamp', 'user_id', 'action', 'resource', 'success'])
                for event in events:
                    writer.writerow([
                        event.event_id,
                        datetime.fromtimestamp(event.timestamp).isoformat(),
                        event.user_id,
                        event.action,
                        f"{event.resource_type}:{event.resource_id}",
                        event.success
                    ])
        else:
            raise ValueError(f"Unsupported export format: {format}")
        
        return output_file
    
    def shutdown(self):
        """Shutdown audit logger"""
        self.running = False
        
        # Flush remaining events
        self._flush_events()
        
        # Close database connection
        if hasattr(self, 'db_conn'):
            self.db_conn.close()

# Convenience functions for common audit events
def audit_login(audit_logger: AuditLogger, user_id: str, session_id: str,
                ip_address: str, user_agent: str, success: bool, error_message: str = None):
    """Audit login event"""
    audit_logger.log_event(
        user_id=user_id,
        session_id=session_id,
        action='login',
        resource='authentication',
        resource_type='auth',
        resource_id='login',
        details={'method': 'password'},
        ip_address=ip_address,
        user_agent=user_agent,
        success=success,
        error_message=error_message
    )

def audit_logout(audit_logger: AuditLogger, user_id: str, session_id: str,
                 ip_address: str, user_agent: str):
    """Audit logout event"""
    audit_logger.log_event(
        user_id=user_id,
        session_id=session_id,
        action='logout',
        resource='authentication',
        resource_type='auth',
        resource_id='logout',
        details={},
        ip_address=ip_address,
        user_agent=user_agent,
        success=True
    )

def audit_file_access(audit_logger: AuditLogger, user_id: str, session_id: str,
                      action: str, file_path: str, ip_address: str, user_agent: str,
                      success: bool, error_message: str = None):
    """Audit file access event"""
    audit_logger.log_event(
        user_id=user_id,
        session_id=session_id,
        action=action,
        resource=file_path,
        resource_type='file',
        resource_id=file_path,
        details={'file_size': os.path.getsize(file_path) if os.path.exists(file_path) else 0},
        ip_address=ip_address,
        user_agent=user_agent,
        success=success,
        error_message=error_message
    )

def audit_data_access(audit_logger: AuditLogger, user_id: str, session_id: str,
                      action: str, table_name: str, record_id: str, ip_address: str,
                      user_agent: str, success: bool, error_message: str = None):
    """Audit data access event"""
    audit_logger.log_event(
        user_id=user_id,
        session_id=session_id,
        action=action,
        resource=table_name,
        resource_type='data',
        resource_id=record_id,
        details={'table': table_name, 'record_id': record_id},
        ip_address=ip_address,
        user_agent=user_agent,
        success=success,
        error_message=error_message
    )

# Example usage
if __name__ == '__main__':
    # Create audit configuration
    config = AuditConfig(
        enabled=True,
        storage_type='database',
        storage_path='/var/log/tusklang/audit',
        retention_days=90,
        compression=True,
        batch_size=100,
        flush_interval=5
    )
    
    # Initialize audit logger
    audit_logger = AuditLogger(config)
    
    try:
        # Example audit events
        audit_login(audit_logger, 'user123', 'session456', '192.168.1.100', 
                   'Mozilla/5.0...', True)
        
        audit_file_access(audit_logger, 'user123', 'session456', 'read', 
                         '/path/to/file.txt', '192.168.1.100', 'Mozilla/5.0...', True)
        
        audit_data_access(audit_logger, 'user123', 'session456', 'update', 
                         'users', 'user123', '192.168.1.100', 'Mozilla/5.0...', True)
        
        # Query events
        events = audit_logger.query_events(
            filters={'user_id': 'user123'},
            limit=10
        )
        
        print(f"Found {len(events)} events for user123")
        
        # Get statistics
        stats = audit_logger.get_statistics()
        print(f"Total events: {stats['total_events']}")
        print(f"Success rate: {stats['success_rate']:.1f}%")
        
    finally:
        audit_logger.shutdown() 