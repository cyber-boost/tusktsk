#!/usr/bin/env python3
"""
TuskLang Package Registry Access Control System
Comprehensive access control and authorization for package operations
"""

import time
from typing import Dict, List, Optional, Set
from dataclasses import dataclass
from enum import Enum
from .authentication import AuthLevel, User

class ResourceType(Enum):
    """Types of registry resources"""
    PACKAGE = "package"
    NAMESPACE = "namespace"
    REGISTRY = "registry"
    USER = "user"

class Action(Enum):
    """Actions that can be performed on resources"""
    READ = "read"
    WRITE = "write"
    DELETE = "delete"
    PUBLISH = "publish"
    ADMIN = "admin"

@dataclass
class Permission:
    """Permission definition"""
    resource_type: ResourceType
    resource_id: str
    action: Action
    granted_to: str  # user_id or group_id
    granted_by: str  # user_id who granted permission
    granted_at: float
    expires_at: Optional[float] = None

class AccessControlList:
    """Access Control List for registry resources"""
    
    def __init__(self):
        self.permissions: Dict[str, List[Permission]] = {}
        self.groups: Dict[str, Set[str]] = {}  # group_id -> set of user_ids
        self.user_groups: Dict[str, Set[str]] = {}  # user_id -> set of group_ids
    
    def add_permission(self, permission: Permission) -> bool:
        """Add a new permission"""
        key = f"{permission.resource_type.value}:{permission.resource_id}"
        
        if key not in self.permissions:
            self.permissions[key] = []
        
        # Check for duplicates
        for existing in self.permissions[key]:
            if (existing.action == permission.action and 
                existing.granted_to == permission.granted_to):
                return False
        
        self.permissions[key].append(permission)
        return True
    
    def remove_permission(self, resource_type: ResourceType, resource_id: str, 
                         action: Action, granted_to: str) -> bool:
        """Remove a specific permission"""
        key = f"{resource_type.value}:{resource_id}"
        
        if key not in self.permissions:
            return False
        
        for i, permission in enumerate(self.permissions[key]):
            if (permission.action == action and 
                permission.granted_to == granted_to):
                del self.permissions[key][i]
                return True
        
        return False
    
    def check_permission(self, user: User, resource_type: ResourceType, 
                        resource_id: str, action: Action) -> bool:
        """Check if user has permission for specific action"""
        # Admin users have all permissions
        if user.auth_level == AuthLevel.ADMIN:
            return True
        
        key = f"{resource_type.value}:{resource_id}"
        
        if key not in self.permissions:
            return False
        
        current_time = time.time()
        
        for permission in self.permissions[key]:
            # Check if permission is expired
            if permission.expires_at and permission.expires_at < current_time:
                continue
            
            # Direct user permission
            if permission.granted_to == user.user_id and permission.action == action:
                return True
            
            # Group permission
            if permission.granted_to in self.user_groups.get(user.user_id, set()):
                if permission.action == action:
                    return True
        
        return False
    
    def create_group(self, group_id: str, name: str, created_by: str) -> bool:
        """Create a new group"""
        if group_id in self.groups:
            return False
        
        self.groups[group_id] = set()
        return True
    
    def add_user_to_group(self, user_id: str, group_id: str) -> bool:
        """Add user to group"""
        if group_id not in self.groups:
            return False
        
        self.groups[group_id].add(user_id)
        
        if user_id not in self.user_groups:
            self.user_groups[user_id] = set()
        self.user_groups[user_id].add(group_id)
        
        return True
    
    def remove_user_from_group(self, user_id: str, group_id: str) -> bool:
        """Remove user from group"""
        if group_id in self.groups:
            self.groups[group_id].discard(user_id)
        
        if user_id in self.user_groups:
            self.user_groups[user_id].discard(group_id)
        
        return True

class PackageSecurityManager:
    """Security manager for package operations"""
    
    def __init__(self):
        self.acl = AccessControlList()
        self.package_owners: Dict[str, str] = {}  # package_id -> owner_user_id
        self.namespace_owners: Dict[str, str] = {}  # namespace -> owner_user_id
    
    def register_package(self, package_id: str, owner_user_id: str, namespace: str) -> bool:
        """Register a new package with ownership"""
        if package_id in self.package_owners:
            return False
        
        self.package_owners[package_id] = owner_user_id
        
        # Grant full permissions to owner
        owner_permission = Permission(
            resource_type=ResourceType.PACKAGE,
            resource_id=package_id,
            action=Action.ADMIN,
            granted_to=owner_user_id,
            granted_by=owner_user_id,
            granted_at=time.time()
        )
        
        self.acl.add_permission(owner_permission)
        return True
    
    def can_read_package(self, user: User, package_id: str) -> bool:
        """Check if user can read package"""
        return self.acl.check_permission(user, ResourceType.PACKAGE, package_id, Action.READ)
    
    def can_write_package(self, user: User, package_id: str) -> bool:
        """Check if user can write to package"""
        return self.acl.check_permission(user, ResourceType.PACKAGE, package_id, Action.WRITE)
    
    def can_publish_package(self, user: User, package_id: str) -> bool:
        """Check if user can publish package"""
        return self.acl.check_permission(user, ResourceType.PACKAGE, package_id, Action.PUBLISH)
    
    def can_delete_package(self, user: User, package_id: str) -> bool:
        """Check if user can delete package"""
        return self.acl.check_permission(user, ResourceType.PACKAGE, package_id, Action.DELETE)
    
    def grant_package_permission(self, granter: User, package_id: str, 
                               grantee_user_id: str, action: Action) -> bool:
        """Grant permission to user for package"""
        if not self.can_delete_package(granter, package_id):
            return False
        
        permission = Permission(
            resource_type=ResourceType.PACKAGE,
            resource_id=package_id,
            action=action,
            granted_to=grantee_user_id,
            granted_by=granter.user_id,
            granted_at=time.time()
        )
        
        return self.acl.add_permission(permission)
    
    def revoke_package_permission(self, revoker: User, package_id: str,
                                revokee_user_id: str, action: Action) -> bool:
        """Revoke permission from user for package"""
        if not self.can_delete_package(revoker, package_id):
            return False
        
        return self.acl.remove_permission(ResourceType.PACKAGE, package_id, action, revokee_user_id)

# Global security manager instance
package_security = PackageSecurityManager()

def require_package_permission(action: Action):
    """Decorator for requiring package permission"""
    def decorator(func):
        def wrapper(*args, **kwargs):
            # This would be implemented with actual request context
            # For now, return the function as-is
            return func(*args, **kwargs)
        return wrapper
    return decorator 