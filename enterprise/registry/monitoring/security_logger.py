#!/usr/bin/env python3
"""
TuskLang Package Registry Security Event Logging System
Comprehensive security event logging and monitoring
"""

import time
import json
from typing import Dict, List, Optional, Any
from dataclasses import dataclass
from enum import Enum
import os
import hashlib
from typing import Set

class SecurityEventType(Enum):
    """Types of security events"""
    AUTHENTICATION_FAILURE = "authentication_failure"
    AUTHORIZATION_FAILURE = "authorization_failure"
    SUSPICIOUS_ACTIVITY = "suspicious_activity"
    RATE_LIMIT_EXCEEDED = "rate_limit_exceeded"
    INVALID_SIGNATURE = "invalid_signature"
    MALWARE_DETECTED = "malware_detected"
    UNAUTHORIZED_ACCESS = "unauthorized_access"
    SESSION_HIJACKING = "session_hijacking"
    API_ABUSE = "api_abuse"
    DATA_BREACH = "data_breach"

class SecurityLevel(Enum):
    """Security event levels"""
    LOW = "low"
    MEDIUM = "medium"
    HIGH = "high"
    CRITICAL = "critical"

@dataclass
class SecurityEvent:
    """Security event record"""
    event_id: str
    event_type: SecurityEventType
    security_level: SecurityLevel
    timestamp: float
    user_id: Optional[str]
    ip_address: Optional[str]
    user_agent: Optional[str]
    details: Dict[str, Any]
    resolved: bool = False
    resolution_notes: Optional[str] = None

class SecurityLogger:
    """Security event logging system"""
    
    def __init__(self, data_path: str):
        self.data_path = data_path
        self.security_events: List[SecurityEvent] = []
        self.alert_thresholds: Dict[SecurityLevel, int] = {
            SecurityLevel.LOW: 100,
            SecurityLevel.MEDIUM: 50,
            SecurityLevel.HIGH: 10,
            SecurityLevel.CRITICAL: 1
        }
        self.active_alerts: List[Dict] = []
        
        # Ensure data directory exists
        os.makedirs(data_path, exist_ok=True)
        self._load_existing_events()
    
    def _load_existing_events(self):
        """Load existing security events from storage"""
        events_file = os.path.join(self.data_path, 'security_events.json')
        if os.path.exists(events_file):
            try:
                with open(events_file, 'r') as f:
                    data = json.load(f)
                    for event_data in data:
                        event = SecurityEvent(
                            event_id=event_data['event_id'],
                            event_type=SecurityEventType(event_data['event_type']),
                            security_level=SecurityLevel(event_data['security_level']),
                            timestamp=event_data['timestamp'],
                            user_id=event_data.get('user_id'),
                            ip_address=event_data.get('ip_address'),
                            user_agent=event_data.get('user_agent'),
                            details=event_data.get('details', {}),
                            resolved=event_data.get('resolved', False),
                            resolution_notes=event_data.get('resolution_notes')
                        )
                        self.security_events.append(event)
            except Exception as e:
                print(f"Error loading security events: {e}")
    
    def _save_events(self):
        """Save security events to storage"""
        events_file = os.path.join(self.data_path, 'security_events.json')
        try:
            data = [asdict(event) for event in self.security_events[-10000:]]  # Keep last 10k events
            with open(events_file, 'w') as f:
                json.dump(data, f, indent=2)
        except Exception as e:
            print(f"Error saving security events: {e}")
    
    def log_security_event(self, event_type: SecurityEventType, security_level: SecurityLevel,
                          user_id: Optional[str] = None, ip_address: Optional[str] = None,
                          user_agent: Optional[str] = None, details: Dict[str, Any] = None) -> str:
        """Log a security event"""
        event_id = self._generate_event_id()
        
        event = SecurityEvent(
            event_id=event_id,
            event_type=event_type,
            security_level=security_level,
            timestamp=time.time(),
            user_id=user_id,
            ip_address=ip_address,
            user_agent=user_agent,
            details=details or {}
        )
        
        self.security_events.append(event)
        self._check_alert_thresholds(event)
        self._save_events()
        
        return event_id
    
    def log_authentication_failure(self, username: str, ip_address: str, 
                                 user_agent: str, reason: str):
        """Log authentication failure"""
        self.log_security_event(
            SecurityEventType.AUTHENTICATION_FAILURE,
            SecurityLevel.MEDIUM,
            user_id=username,
            ip_address=ip_address,
            user_agent=user_agent,
            details={'reason': reason, 'username': username}
        )
    
    def log_authorization_failure(self, user_id: str, ip_address: str,
                                resource: str, action: str):
        """Log authorization failure"""
        self.log_security_event(
            SecurityEventType.AUTHORIZATION_FAILURE,
            SecurityLevel.HIGH,
            user_id=user_id,
            ip_address=ip_address,
            details={'resource': resource, 'action': action}
        )
    
    def log_suspicious_activity(self, user_id: str, ip_address: str,
                              activity_type: str, details: Dict[str, Any]):
        """Log suspicious activity"""
        self.log_security_event(
            SecurityEventType.SUSPICIOUS_ACTIVITY,
            SecurityLevel.HIGH,
            user_id=user_id,
            ip_address=ip_address,
            details={'activity_type': activity_type, **details}
        )
    
    def log_rate_limit_exceeded(self, ip_address: str, endpoint: str, limit: int):
        """Log rate limit exceeded"""
        self.log_security_event(
            SecurityEventType.RATE_LIMIT_EXCEEDED,
            SecurityLevel.MEDIUM,
            ip_address=ip_address,
            details={'endpoint': endpoint, 'limit': limit}
        )
    
    def log_invalid_signature(self, package_id: str, user_id: str, signature_details: Dict[str, Any]):
        """Log invalid package signature"""
        self.log_security_event(
            SecurityEventType.INVALID_SIGNATURE,
            SecurityLevel.HIGH,
            user_id=user_id,
            details={'package_id': package_id, **signature_details}
        )
    
    def log_malware_detected(self, package_id: str, user_id: str, malware_type: str):
        """Log malware detection"""
        self.log_security_event(
            SecurityEventType.MALWARE_DETECTED,
            SecurityLevel.CRITICAL,
            user_id=user_id,
            details={'package_id': package_id, 'malware_type': malware_type}
        )
    
    def get_security_events(self, hours: int = 24, security_level: Optional[SecurityLevel] = None) -> List[SecurityEvent]:
        """Get security events from the last N hours"""
        cutoff_time = time.time() - (hours * 3600)
        
        events = [
            event for event in self.security_events
            if event.timestamp >= cutoff_time
        ]
        
        if security_level:
            events = [event for event in events if event.security_level == security_level]
        
        return events
    
    def get_security_statistics(self, hours: int = 24) -> Dict[str, Any]:
        """Get security statistics"""
        events = self.get_security_events(hours)
        
        event_counts = {}
        for event_type in SecurityEventType:
            event_counts[event_type.value] = len([
                event for event in events if event.event_type == event_type
            ])
        
        level_counts = {}
        for level in SecurityLevel:
            level_counts[level.value] = len([
                event for event in events if event.security_level == level
            ])
        
        return {
            'total_events': len(events),
            'event_type_counts': event_counts,
            'security_level_counts': level_counts,
            'resolved_events': len([event for event in events if event.resolved]),
            'unresolved_events': len([event for event in events if not event.resolved])
        }
    
    def get_active_alerts(self) -> List[Dict]:
        """Get active security alerts"""
        return self.active_alerts
    
    def resolve_event(self, event_id: str, resolution_notes: str) -> bool:
        """Mark a security event as resolved"""
        for event in self.security_events:
            if event.event_id == event_id:
                event.resolved = True
                event.resolution_notes = resolution_notes
                self._save_events()
                return True
        return False
    
    def _check_alert_thresholds(self, event: SecurityEvent):
        """Check if event triggers an alert"""
        threshold = self.alert_thresholds[event.security_level]
        
        # Count recent events of same type and level
        recent_events = [
            e for e in self.security_events
            if (e.event_type == event.event_type and 
                e.security_level == event.security_level and
                time.time() - e.timestamp < 3600)  # Last hour
        ]
        
        if len(recent_events) >= threshold:
            alert = {
                'alert_id': self._generate_alert_id(),
                'event_type': event.event_type.value,
                'security_level': event.security_level.value,
                'count': len(recent_events),
                'threshold': threshold,
                'timestamp': time.time(),
                'details': {
                    'recent_events': [e.event_id for e in recent_events[-5:]]  # Last 5 events
                }
            }
            
            self.active_alerts.append(alert)
    
    def _generate_event_id(self) -> str:
        """Generate unique event ID"""
        data = f"security:{time.time()}:{os.urandom(8).hex()}"
        return hashlib.sha256(data.encode()).hexdigest()[:16]
    
    def _generate_alert_id(self) -> str:
        """Generate unique alert ID"""
        data = f"alert:{time.time()}:{os.urandom(8).hex()}"
        return hashlib.sha256(data.encode()).hexdigest()[:16]

class SecurityMonitor:
    """Security monitoring system"""
    
    def __init__(self, security_logger: SecurityLogger):
        self.security_logger = security_logger
        self.ip_blacklist: Set[str] = set()
        self.user_suspension: Dict[str, float] = {}  # user_id -> suspension_until
    
    def check_ip_blacklist(self, ip_address: str) -> bool:
        """Check if IP is blacklisted"""
        return ip_address in self.ip_blacklist
    
    def blacklist_ip(self, ip_address: str, reason: str):
        """Blacklist an IP address"""
        self.ip_blacklist.add(ip_address)
        self.security_logger.log_security_event(
            SecurityEventType.UNAUTHORIZED_ACCESS,
            SecurityLevel.HIGH,
            ip_address=ip_address,
            details={'reason': reason, 'action': 'blacklisted'}
        )
    
    def check_user_suspension(self, user_id: str) -> bool:
        """Check if user is suspended"""
        if user_id in self.user_suspension:
            if time.time() > self.user_suspension[user_id]:
                del self.user_suspension[user_id]
                return False
            return True
        return False
    
    def suspend_user(self, user_id: str, duration_hours: int, reason: str):
        """Suspend a user"""
        suspension_until = time.time() + (duration_hours * 3600)
        self.user_suspension[user_id] = suspension_until
        
        self.security_logger.log_security_event(
            SecurityEventType.UNAUTHORIZED_ACCESS,
            SecurityLevel.HIGH,
            user_id=user_id,
            details={'reason': reason, 'suspension_until': suspension_until}
        )

# Global security logging instances
security_logger = SecurityLogger("/var/tusklang/registry/security")
security_monitor = SecurityMonitor(security_logger) 