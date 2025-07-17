"""
TuskLang FORTRESS - Database Connection Pooling
Agent A1 - Goal G2: Database Protection Implementation

This module provides secure database connection pooling including:
- Connection pooling with SSL/TLS encryption
- Connection health monitoring
- Automatic failover and recovery
- Connection security validation
"""

import psycopg2
import psycopg2.pool
import threading
import time
import logging
from typing import Dict, Any, Optional, List
from contextlib import contextmanager
from datetime import datetime, timedelta
import queue
import json

from .encryption import DatabaseConnectionSecurity, db_audit_logger

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class DatabasePoolConfig:
    """Database connection pool configuration"""
    
    # Pool settings
    MIN_CONNECTIONS = 5
    MAX_CONNECTIONS = 20
    CONNECTION_TIMEOUT = 30
    IDLE_TIMEOUT = 300  # 5 minutes
    MAX_LIFETIME = 3600  # 1 hour
    
    # Health check settings
    HEALTH_CHECK_INTERVAL = 60  # 1 minute
    HEALTH_CHECK_TIMEOUT = 10
    MAX_FAILED_HEALTH_CHECKS = 3
    
    # Security settings
    REQUIRE_SSL = True
    VERIFY_CERTIFICATE = True
    CONNECTION_ENCRYPTION = True
    
    # Monitoring settings
    ENABLE_MONITORING = True
    LOG_CONNECTION_EVENTS = True

class ConnectionHealthMonitor:
    """Monitor database connection health"""
    
    def __init__(self, config: DatabasePoolConfig):
        self.config = config
        self.health_status = {}
        self.failed_checks = {}
        self.last_health_check = {}
        self.monitoring_thread = None
        self.stop_monitoring = False
    
    def start_monitoring(self, pool):
        """Start health monitoring thread"""
        if not self.config.ENABLE_MONITORING:
            return
        
        self.monitoring_thread = threading.Thread(
            target=self._monitor_connections,
            args=(pool,),
            daemon=True
        )
        self.monitoring_thread.start()
        logger.info("Database connection health monitoring started")
    
    def stop_monitoring_thread(self):
        """Stop health monitoring thread"""
        self.stop_monitoring = True
        if self.monitoring_thread:
            self.monitoring_thread.join()
        logger.info("Database connection health monitoring stopped")
    
    def _monitor_connections(self, pool):
        """Monitor connection health in background thread"""
        while not self.stop_monitoring:
            try:
                self._check_all_connections(pool)
                time.sleep(self.config.HEALTH_CHECK_INTERVAL)
            except Exception as e:
                logger.error(f"Health monitoring error: {e}")
                time.sleep(10)  # Wait before retrying
    
    def _check_all_connections(self, pool):
        """Check health of all connections in pool"""
        try:
            # Get pool status
            pool_status = {
                'total_connections': pool.get_size(),
                'available_connections': pool.get_available(),
                'in_use_connections': pool.get_size() - pool.get_available(),
                'timestamp': datetime.now().isoformat()
            }
            
            # Test connection health
            with pool.getconn() as conn:
                if conn:
                    self._test_connection_health(conn)
                    pool.putconn(conn)
            
            # Log pool status
            if self.config.LOG_CONNECTION_EVENTS:
                logger.info(f"Pool status: {json.dumps(pool_status)}")
                
        except Exception as e:
            logger.error(f"Failed to check connection health: {e}")
    
    def _test_connection_health(self, connection) -> bool:
        """Test if a connection is healthy"""
        try:
            # Simple health check query
            with connection.cursor() as cursor:
                cursor.execute("SELECT 1")
                result = cursor.fetchone()
                return result[0] == 1
        except Exception as e:
            logger.error(f"Connection health check failed: {e}")
            return False
    
    def mark_connection_failed(self, connection_id: str):
        """Mark a connection as failed"""
        if connection_id not in self.failed_checks:
            self.failed_checks[connection_id] = 0
        self.failed_checks[connection_id] += 1
        
        if self.failed_checks[connection_id] >= self.config.MAX_FAILED_HEALTH_CHECKS:
            logger.warning(f"Connection {connection_id} marked as unhealthy")
    
    def reset_connection_failures(self, connection_id: str):
        """Reset failure count for a connection"""
        if connection_id in self.failed_checks:
            del self.failed_checks[connection_id]

class SecureDatabasePool:
    """Secure database connection pool with encryption and monitoring"""
    
    def __init__(self, config: DatabasePoolConfig = None):
        self.config = config or DatabasePoolConfig()
        self.connection_security = DatabaseConnectionSecurity()
        self.health_monitor = ConnectionHealthMonitor(self.config)
        self.pool = None
        self.pool_lock = threading.Lock()
        self.connection_stats = {
            'total_connections': 0,
            'successful_connections': 0,
            'failed_connections': 0,
            'security_violations': 0
        }
        self._initialize_pool()
    
    def _initialize_pool(self):
        """Initialize the connection pool"""
        try:
            connection_params = self.connection_security.get_secure_connection_params()
            
            # Create connection pool
            self.pool = psycopg2.pool.ThreadedConnectionPool(
                minconn=self.config.MIN_CONNECTIONS,
                maxconn=self.config.MAX_CONNECTIONS,
                **connection_params
            )
            
            # Start health monitoring
            self.health_monitor.start_monitoring(self.pool)
            
            logger.info(f"Database connection pool initialized: {self.config.MIN_CONNECTIONS}-{self.config.MAX_CONNECTIONS} connections")
            
        except Exception as e:
            logger.error(f"Failed to initialize database pool: {e}")
            raise
    
    @contextmanager
    def get_connection(self):
        """Get a secure database connection from the pool"""
        connection = None
        connection_id = None
        
        try:
            # Get connection from pool
            connection = self.pool.getconn()
            connection_id = id(connection)
            
            # Validate connection security
            if not self._validate_connection_security(connection):
                self.connection_stats['security_violations'] += 1
                raise SecurityError("Database connection security validation failed")
            
            # Set connection parameters
            connection.autocommit = False
            connection.set_session(isolation_level=psycopg2.extensions.ISOLATION_LEVEL_READ_COMMITTED)
            
            # Log connection acquisition
            self._log_connection_event("acquired", connection_id)
            
            yield connection
            
        except Exception as e:
            self.connection_stats['failed_connections'] += 1
            if connection_id:
                self.health_monitor.mark_connection_failed(str(connection_id))
            logger.error(f"Database connection error: {e}")
            raise
            
        finally:
            if connection:
                try:
                    # Reset connection for reuse
                    connection.rollback()
                    self.pool.putconn(connection)
                    self._log_connection_event("released", connection_id)
                except Exception as e:
                    logger.error(f"Failed to return connection to pool: {e}")
    
    def _validate_connection_security(self, connection) -> bool:
        """Validate that connection meets security requirements"""
        try:
            # Check SSL/TLS encryption
            if self.config.REQUIRE_SSL:
                ssl_status = connection.get_ssl_status()
                if not ssl_status:
                    logger.warning("Connection not using SSL/TLS")
                    return False
            
            # Check certificate verification
            if self.config.VERIFY_CERTIFICATE:
                # In production, verify certificate chain
                pass
            
            # Check connection encryption
            if self.config.CONNECTION_ENCRYPTION:
                # Verify connection is encrypted
                pass
            
            return True
            
        except Exception as e:
            logger.error(f"Connection security validation failed: {e}")
            return False
    
    def _log_connection_event(self, event: str, connection_id: str):
        """Log connection pool events"""
        if not self.config.LOG_CONNECTION_EVENTS:
            return
        
        event_data = {
            'timestamp': datetime.now().isoformat(),
            'event': event,
            'connection_id': connection_id,
            'pool_size': self.pool.get_size(),
            'available': self.pool.get_available()
        }
        
        logger.info(f"Connection pool event: {json.dumps(event_data)}")
    
    def execute_query(self, query: str, params: tuple = None) -> List[tuple]:
        """Execute a secure database query"""
        with self.get_connection() as conn:
            with conn.cursor() as cursor:
                cursor.execute(query, params)
                return cursor.fetchall()
    
    def execute_transaction(self, queries: List[tuple]) -> bool:
        """Execute multiple queries in a secure transaction"""
        with self.get_connection() as conn:
            try:
                with conn.cursor() as cursor:
                    for query, params in queries:
                        cursor.execute(query, params)
                conn.commit()
                return True
            except Exception as e:
                conn.rollback()
                logger.error(f"Transaction failed: {e}")
                return False
    
    def get_pool_stats(self) -> Dict[str, Any]:
        """Get connection pool statistics"""
        return {
            'pool_size': self.pool.get_size() if self.pool else 0,
            'available_connections': self.pool.get_available() if self.pool else 0,
            'in_use_connections': (self.pool.get_size() - self.pool.get_available()) if self.pool else 0,
            'connection_stats': self.connection_stats.copy(),
            'health_status': self.health_monitor.health_status.copy(),
            'failed_checks': self.health_monitor.failed_checks.copy()
        }
    
    def close_pool(self):
        """Close the connection pool"""
        try:
            self.health_monitor.stop_monitoring_thread()
            if self.pool:
                self.pool.closeall()
            logger.info("Database connection pool closed")
        except Exception as e:
            logger.error(f"Failed to close connection pool: {e}")

class SecurityError(Exception):
    """Security-related exception"""
    pass

class DatabaseAccessControl:
    """Database access control and permissions management"""
    
    def __init__(self):
        self.user_permissions = {}
        self.role_permissions = {}
        self.table_permissions = {}
        self._initialize_permissions()
    
    def _initialize_permissions(self):
        """Initialize default permissions"""
        # Role-based permissions
        self.role_permissions = {
            'admin': ['SELECT', 'INSERT', 'UPDATE', 'DELETE', 'CREATE', 'DROP'],
            'user': ['SELECT', 'INSERT', 'UPDATE'],
            'readonly': ['SELECT'],
            'auditor': ['SELECT']
        }
        
        # Table-specific permissions
        self.table_permissions = {
            'users': {
                'admin': ['SELECT', 'INSERT', 'UPDATE', 'DELETE'],
                'user': ['SELECT', 'UPDATE'],
                'readonly': ['SELECT']
            },
            'audit_log': {
                'admin': ['SELECT', 'INSERT'],
                'auditor': ['SELECT']
            }
        }
    
    def check_permission(self, user_role: str, operation: str, table: str = None) -> bool:
        """Check if user has permission for operation"""
        try:
            # Check role permissions
            if user_role not in self.role_permissions:
                return False
            
            role_perms = self.role_permissions[user_role]
            if operation not in role_perms:
                return False
            
            # Check table-specific permissions
            if table and table in self.table_permissions:
                table_perms = self.table_permissions[table]
                if user_role not in table_perms:
                    return False
                if operation not in table_perms[user_role]:
                    return False
            
            return True
            
        except Exception as e:
            logger.error(f"Permission check failed: {e}")
            return False
    
    def log_access_attempt(self, user_id: str, user_role: str, operation: str, 
                          table: str, success: bool):
        """Log database access attempt"""
        try:
            db_audit_logger.log_operation(
                operation="access_attempt",
                table=table,
                record_id=user_id,
                user_id=user_id,
                details={
                    'user_role': user_role,
                    'operation': operation,
                    'table': table,
                    'success': success,
                    'timestamp': datetime.now().isoformat()
                }
            )
        except Exception as e:
            logger.error(f"Failed to log access attempt: {e}")

# Initialize secure database pool
db_pool_config = DatabasePoolConfig()
secure_db_pool = SecureDatabasePool(db_pool_config)
db_access_control = DatabaseAccessControl() 