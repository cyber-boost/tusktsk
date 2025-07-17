#!/usr/bin/env python3
"""
TuskLang Package Registry Usage Monitoring System
Comprehensive monitoring and analytics for registry usage
"""

import time
import json
from typing import Dict, List, Optional, Any
from dataclasses import dataclass, asdict
from enum import Enum
import os
from collections import defaultdict, deque
import hashlib

class EventType(Enum):
    """Types of registry events"""
    PACKAGE_UPLOAD = "package_upload"
    PACKAGE_DOWNLOAD = "package_download"
    PACKAGE_DELETE = "package_delete"
    USER_LOGIN = "user_login"
    USER_LOGOUT = "user_logout"
    API_CALL = "api_call"
    SECURITY_EVENT = "security_event"
    ERROR_EVENT = "error_event"

@dataclass
class RegistryEvent:
    """Registry event record"""
    event_id: str
    event_type: EventType
    timestamp: float
    user_id: Optional[str]
    package_id: Optional[str]
    ip_address: Optional[str]
    user_agent: Optional[str]
    details: Dict[str, Any]
    session_id: Optional[str] = None

@dataclass
class UsageMetrics:
    """Usage metrics for a time period"""
    period_start: float
    period_end: float
    total_events: int
    unique_users: int
    unique_packages: int
    total_downloads: int
    total_uploads: int
    api_calls: int
    security_events: int
    error_events: int

class RegistryUsageMonitor:
    """Registry usage monitoring system"""
    
    def __init__(self, data_path: str):
        self.data_path = data_path
        self.events: List[RegistryEvent] = []
        self.metrics_cache: Dict[str, UsageMetrics] = {}
        self.real_time_stats: Dict[str, Any] = defaultdict(int)
        
        # Ensure data directory exists
        os.makedirs(data_path, exist_ok=True)
        self._load_existing_events()
    
    def _load_existing_events(self):
        """Load existing events from storage"""
        events_file = os.path.join(self.data_path, 'events.json')
        if os.path.exists(events_file):
            try:
                with open(events_file, 'r') as f:
                    data = json.load(f)
                    for event_data in data:
                        event = RegistryEvent(
                            event_id=event_data['event_id'],
                            event_type=EventType(event_data['event_type']),
                            timestamp=event_data['timestamp'],
                            user_id=event_data.get('user_id'),
                            package_id=event_data.get('package_id'),
                            ip_address=event_data.get('ip_address'),
                            user_agent=event_data.get('user_agent'),
                            details=event_data.get('details', {}),
                            session_id=event_data.get('session_id')
                        )
                        self.events.append(event)
            except Exception as e:
                print(f"Error loading events: {e}")
    
    def _save_events(self):
        """Save events to storage"""
        events_file = os.path.join(self.data_path, 'events.json')
        try:
            data = [asdict(event) for event in self.events[-10000:]]  # Keep last 10k events
            with open(events_file, 'w') as f:
                json.dump(data, f, indent=2)
        except Exception as e:
            print(f"Error saving events: {e}")
    
    def record_event(self, event_type: EventType, user_id: Optional[str] = None,
                    package_id: Optional[str] = None, ip_address: Optional[str] = None,
                    user_agent: Optional[str] = None, details: Dict[str, Any] = None,
                    session_id: Optional[str] = None) -> str:
        """Record a registry event"""
        event_id = self._generate_event_id()
        
        event = RegistryEvent(
            event_id=event_id,
            event_type=event_type,
            timestamp=time.time(),
            user_id=user_id,
            package_id=package_id,
            ip_address=ip_address,
            user_agent=user_agent,
            details=details or {},
            session_id=session_id
        )
        
        self.events.append(event)
        self._update_real_time_stats(event)
        self._save_events()
        
        return event_id
    
    def get_usage_metrics(self, start_time: float, end_time: float) -> UsageMetrics:
        """Get usage metrics for a time period"""
        cache_key = f"{start_time}:{end_time}"
        if cache_key in self.metrics_cache:
            return self.metrics_cache[cache_key]
        
        # Filter events for time period
        period_events = [
            event for event in self.events
            if start_time <= event.timestamp <= end_time
        ]
        
        # Calculate metrics
        unique_users = len(set(event.user_id for event in period_events if event.user_id))
        unique_packages = len(set(event.package_id for event in period_events if event.package_id))
        
        total_downloads = len([e for e in period_events if e.event_type == EventType.PACKAGE_DOWNLOAD])
        total_uploads = len([e for e in period_events if e.event_type == EventType.PACKAGE_UPLOAD])
        api_calls = len([e for e in period_events if e.event_type == EventType.API_CALL])
        security_events = len([e for e in period_events if e.event_type == EventType.SECURITY_EVENT])
        error_events = len([e for e in period_events if e.event_type == EventType.ERROR_EVENT])
        
        metrics = UsageMetrics(
            period_start=start_time,
            period_end=end_time,
            total_events=len(period_events),
            unique_users=unique_users,
            unique_packages=unique_packages,
            total_downloads=total_downloads,
            total_uploads=total_uploads,
            api_calls=api_calls,
            security_events=security_events,
            error_events=error_events
        )
        
        self.metrics_cache[cache_key] = metrics
        return metrics
    
    def get_real_time_stats(self) -> Dict[str, Any]:
        """Get real-time statistics"""
        return dict(self.real_time_stats)
    
    def get_top_packages(self, limit: int = 10) -> List[Dict[str, Any]]:
        """Get top downloaded packages"""
        package_downloads = defaultdict(int)
        
        for event in self.events:
            if event.event_type == EventType.PACKAGE_DOWNLOAD and event.package_id:
                package_downloads[event.package_id] += 1
        
        sorted_packages = sorted(package_downloads.items(), key=lambda x: x[1], reverse=True)
        
        return [
            {'package_id': package_id, 'downloads': count}
            for package_id, count in sorted_packages[:limit]
        ]
    
    def get_top_users(self, limit: int = 10) -> List[Dict[str, Any]]:
        """Get top active users"""
        user_activity = defaultdict(int)
        
        for event in self.events:
            if event.user_id:
                user_activity[event.user_id] += 1
        
        sorted_users = sorted(user_activity.items(), key=lambda x: x[1], reverse=True)
        
        return [
            {'user_id': user_id, 'activity_count': count}
            for user_id, count in sorted_users[:limit]
        ]
    
    def get_security_events(self, hours: int = 24) -> List[RegistryEvent]:
        """Get security events from the last N hours"""
        cutoff_time = time.time() - (hours * 3600)
        
        return [
            event for event in self.events
            if event.event_type == EventType.SECURITY_EVENT and event.timestamp >= cutoff_time
        ]
    
    def get_error_events(self, hours: int = 24) -> List[RegistryEvent]:
        """Get error events from the last N hours"""
        cutoff_time = time.time() - (hours * 3600)
        
        return [
            event for event in self.events
            if event.event_type == EventType.ERROR_EVENT and event.timestamp >= cutoff_time
        ]
    
    def _generate_event_id(self) -> str:
        """Generate unique event ID"""
        data = f"{time.time()}:{os.urandom(8).hex()}"
        return hashlib.sha256(data.encode()).hexdigest()[:16]
    
    def _update_real_time_stats(self, event: RegistryEvent):
        """Update real-time statistics"""
        self.real_time_stats['total_events'] += 1
        
        if event.event_type == EventType.PACKAGE_DOWNLOAD:
            self.real_time_stats['total_downloads'] += 1
        elif event.event_type == EventType.PACKAGE_UPLOAD:
            self.real_time_stats['total_uploads'] += 1
        elif event.event_type == EventType.API_CALL:
            self.real_time_stats['api_calls'] += 1
        elif event.event_type == EventType.SECURITY_EVENT:
            self.real_time_stats['security_events'] += 1
        elif event.event_type == EventType.ERROR_EVENT:
            self.real_time_stats['error_events'] += 1

class DownloadAnalytics:
    """Download analytics system"""
    
    def __init__(self, usage_monitor: RegistryUsageMonitor):
        self.usage_monitor = usage_monitor
        self.download_patterns: Dict[str, List[float]] = defaultdict(list)
    
    def record_download(self, package_id: str, user_id: str, ip_address: str):
        """Record a package download"""
        self.usage_monitor.record_event(
            EventType.PACKAGE_DOWNLOAD,
            user_id=user_id,
            package_id=package_id,
            ip_address=ip_address
        )
        
        self.download_patterns[package_id].append(time.time())
    
    def get_download_trends(self, package_id: str, days: int = 7) -> Dict[str, Any]:
        """Get download trends for a package"""
        cutoff_time = time.time() - (days * 24 * 3600)
        
        recent_downloads = [
            timestamp for timestamp in self.download_patterns[package_id]
            if timestamp >= cutoff_time
        ]
        
        # Group by day
        daily_downloads = defaultdict(int)
        for timestamp in recent_downloads:
            day = int(timestamp // (24 * 3600)) * (24 * 3600)
            daily_downloads[day] += 1
        
        return {
            'package_id': package_id,
            'total_downloads': len(recent_downloads),
            'daily_downloads': dict(daily_downloads),
            'average_daily': len(recent_downloads) / days if days > 0 else 0
        }
    
    def get_popular_packages(self, days: int = 7) -> List[Dict[str, Any]]:
        """Get popular packages in the last N days"""
        cutoff_time = time.time() - (days * 24 * 3600)
        
        package_downloads = defaultdict(int)
        for package_id, timestamps in self.download_patterns.items():
            recent_downloads = [ts for ts in timestamps if ts >= cutoff_time]
            package_downloads[package_id] = len(recent_downloads)
        
        sorted_packages = sorted(package_downloads.items(), key=lambda x: x[1], reverse=True)
        
        return [
            {'package_id': package_id, 'downloads': count}
            for package_id, count in sorted_packages[:10]
        ]

# Global monitoring instances
usage_monitor = RegistryUsageMonitor("/var/tusklang/registry/monitoring")
download_analytics = DownloadAnalytics(usage_monitor) 