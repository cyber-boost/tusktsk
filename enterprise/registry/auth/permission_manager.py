#!/usr/bin/env python3
"""
TuskLang Package Registry Permission Management System
Advanced permission management with hierarchical access control
"""

import time
from typing import Dict, List, Set, Optional
from dataclasses import dataclass
from enum import Enum
import json
import hashlib

class ResourceType(Enum):
    """Types of registry resources"""
    PACKAGE = "package"
    NAMESPACE = "namespace"
    USER = "user"
    REGISTRY = "registry"
    SECURITY = "security"

class Action(Enum):
    """Actions that can be performed"""
    READ = "read"
    WRITE = "write"
    DELETE = "delete"
    PUBLISH = "publish"
    ADMIN = "admin"
    MANAGE = "manage"

@dataclass
class PermissionRule:
    """Permission rule definition"""
    rule_id: str
    resource_type: ResourceType
    resource_id: str
    action: Action
    granted_to: str  # user_id or group_id
    granted_by: str  # user_id who granted permission
    granted_at: float
    expires_at: Optional[float] = None
    conditions: Dict = None

@dataclass
class PermissionGroup:
    """Permission group for managing multiple permissions"""
    group_id: str
    name: str
    description: str
    permissions: List[PermissionRule]
    members: Set[str]  # user_ids
    created_by: str
    created_at: float

class PermissionManager:
    """Advanced permission management system"""
    
    def __init__(self):
        self.permission_rules: Dict[str, PermissionRule] = {}
        self.permission_groups: Dict[str, PermissionGroup] = {}
        self.user_groups: Dict[str, Set[str]] = {}  # user_id -> set of group_ids
        self.resource_owners: Dict[str, str] = {}  # resource_id -> owner_user_id
        self.permission_cache: Dict[str, Dict] = {}  # Cache for permission checks
        
    def create_permission_rule(self, resource_type: ResourceType, resource_id: str,
                             action: Action, granted_to: str, granted_by: str,
                             expires_at: Optional[float] = None,
                             conditions: Dict = None) -> str:
        """Create a new permission rule"""
        rule_id = self._generate_rule_id(resource_type, resource_id, action, granted_to)
        
        rule = PermissionRule(
            rule_id=rule_id,
            resource_type=resource_type,
            resource_id=resource_id,
            action=action,
            granted_to=granted_to,
            granted_by=granted_by,
            granted_at=time.time(),
            expires_at=expires_at,
            conditions=conditions or {}
        )
        
        self.permission_rules[rule_id] = rule
        self._clear_permission_cache(granted_to)
        
        return rule_id
    
    def remove_permission_rule(self, rule_id: str) -> bool:
        """Remove a permission rule"""
        if rule_id in self.permission_rules:
            rule = self.permission_rules[rule_id]
            del self.permission_rules[rule_id]
            self._clear_permission_cache(rule.granted_to)
            return True
        return False
    
    def check_permission(self, user_id: str, resource_type: ResourceType,
                        resource_id: str, action: Action) -> bool:
        """Check if user has permission for specific action"""
        cache_key = f"{user_id}:{resource_type.value}:{resource_id}:{action.value}"
        
        # Check cache first
        if cache_key in self.permission_cache:
            return self.permission_cache[cache_key]
        
        current_time = time.time()
        
        # Check direct permissions
        for rule in self.permission_rules.values():
            if (rule.resource_type == resource_type and
                rule.resource_id == resource_id and
                rule.action == action and
                rule.granted_to == user_id and
                (rule.expires_at is None or rule.expires_at > current_time)):
                
                # Check conditions
                if self._evaluate_conditions(rule.conditions, user_id, resource_id):
                    self.permission_cache[cache_key] = True
                    return True
        
        # Check group permissions
        user_groups = self.user_groups.get(user_id, set())
        for group_id in user_groups:
            for rule in self.permission_rules.values():
                if (rule.resource_type == resource_type and
                    rule.resource_id == resource_id and
                    rule.action == action and
                    rule.granted_to == group_id and
                    (rule.expires_at is None or rule.expires_at > current_time)):
                    
                    if self._evaluate_conditions(rule.conditions, user_id, resource_id):
                        self.permission_cache[cache_key] = True
                        return True
        
        # Check resource ownership
        if resource_id in self.resource_owners and self.resource_owners[resource_id] == user_id:
            self.permission_cache[cache_key] = True
            return True
        
        self.permission_cache[cache_key] = False
        return False
    
    def create_permission_group(self, name: str, description: str, created_by: str) -> str:
        """Create a new permission group"""
        group_id = self._generate_group_id(name)
        
        group = PermissionGroup(
            group_id=group_id,
            name=name,
            description=description,
            permissions=[],
            members=set(),
            created_by=created_by,
            created_at=time.time()
        )
        
        self.permission_groups[group_id] = group
        return group_id
    
    def add_user_to_group(self, user_id: str, group_id: str) -> bool:
        """Add user to permission group"""
        if group_id not in self.permission_groups:
            return False
        
        self.permission_groups[group_id].members.add(user_id)
        
        if user_id not in self.user_groups:
            self.user_groups[user_id] = set()
        self.user_groups[user_id].add(group_id)
        
        self._clear_permission_cache(user_id)
        return True
    
    def remove_user_from_group(self, user_id: str, group_id: str) -> bool:
        """Remove user from permission group"""
        if group_id in self.permission_groups:
            self.permission_groups[group_id].members.discard(user_id)
        
        if user_id in self.user_groups:
            self.user_groups[user_id].discard(group_id)
        
        self._clear_permission_cache(user_id)
        return True
    
    def add_permission_to_group(self, group_id: str, resource_type: ResourceType,
                               resource_id: str, action: Action, granted_by: str) -> bool:
        """Add permission to a group"""
        if group_id not in self.permission_groups:
            return False
        
        rule_id = self.create_permission_rule(
            resource_type, resource_id, action, group_id, granted_by
        )
        
        rule = self.permission_rules[rule_id]
        self.permission_groups[group_id].permissions.append(rule)
        
        # Clear cache for all group members
        for user_id in self.permission_groups[group_id].members:
            self._clear_permission_cache(user_id)
        
        return True
    
    def set_resource_owner(self, resource_id: str, owner_user_id: str):
        """Set the owner of a resource"""
        self.resource_owners[resource_id] = owner_user_id
    
    def get_resource_owner(self, resource_id: str) -> Optional[str]:
        """Get the owner of a resource"""
        return self.resource_owners.get(resource_id)
    
    def get_user_permissions(self, user_id: str) -> List[PermissionRule]:
        """Get all permissions for a user"""
        permissions = []
        
        # Direct permissions
        for rule in self.permission_rules.values():
            if rule.granted_to == user_id:
                permissions.append(rule)
        
        # Group permissions
        user_groups = self.user_groups.get(user_id, set())
        for group_id in user_groups:
            for rule in self.permission_rules.values():
                if rule.granted_to == group_id:
                    permissions.append(rule)
        
        return permissions
    
    def get_resource_permissions(self, resource_type: ResourceType, 
                               resource_id: str) -> List[PermissionRule]:
        """Get all permissions for a resource"""
        permissions = []
        
        for rule in self.permission_rules.values():
            if (rule.resource_type == resource_type and 
                rule.resource_id == resource_id):
                permissions.append(rule)
        
        return permissions
    
    def _generate_rule_id(self, resource_type: ResourceType, resource_id: str,
                         action: Action, granted_to: str) -> str:
        """Generate unique rule ID"""
        data = f"{resource_type.value}:{resource_id}:{action.value}:{granted_to}:{time.time()}"
        return hashlib.sha256(data.encode()).hexdigest()[:16]
    
    def _generate_group_id(self, name: str) -> str:
        """Generate unique group ID"""
        data = f"{name}:{time.time()}"
        return hashlib.sha256(data.encode()).hexdigest()[:16]
    
    def _evaluate_conditions(self, conditions: Dict, user_id: str, resource_id: str) -> bool:
        """Evaluate permission conditions"""
        if not conditions:
            return True
        
        # Example conditions:
        # - time_based: {"start_time": 1234567890, "end_time": 1234567890}
        # - ip_based: {"allowed_ips": ["192.168.1.0/24"]}
        # - rate_based: {"max_requests_per_hour": 100}
        
        current_time = time.time()
        
        # Time-based conditions
        if "start_time" in conditions and current_time < conditions["start_time"]:
            return False
        
        if "end_time" in conditions and current_time > conditions["end_time"]:
            return False
        
        return True
    
    def _clear_permission_cache(self, user_id: str):
        """Clear permission cache for user"""
        keys_to_remove = [key for key in self.permission_cache.keys() 
                         if key.startswith(f"{user_id}:")]
        for key in keys_to_remove:
            del self.permission_cache[key]

# Global permission manager instance
permission_manager = PermissionManager() 