#!/usr/bin/env python3
"""
TuskLang Package Registry Cache Manager
Advanced caching and CDN system for optimal performance
"""

import time
import json
import hashlib
from typing import Dict, List, Optional, Any, Tuple
from dataclasses import dataclass
from enum import Enum
import os
from collections import OrderedDict

class CacheLevel(Enum):
    """Cache levels"""
    L1 = "l1"  # Memory cache
    L2 = "l2"  # Disk cache
    L3 = "l3"  # CDN cache

class CachePolicy(Enum):
    """Cache policies"""
    LRU = "lru"  # Least Recently Used
    LFU = "lfu"  # Least Frequently Used
    FIFO = "fifo"  # First In First Out
    TTL = "ttl"  # Time To Live

@dataclass
class CacheEntry:
    """Cache entry"""
    key: str
    value: Any
    created_at: float
    accessed_at: float
    access_count: int
    size_bytes: int
    ttl: Optional[float] = None

class MemoryCache:
    """In-memory cache with configurable policies"""
    
    def __init__(self, max_size: int = 1000, policy: CachePolicy = CachePolicy.LRU):
        self.max_size = max_size
        self.policy = policy
        self.cache: OrderedDict[str, CacheEntry] = OrderedDict()
        self.access_counts: Dict[str, int] = {}
        self.total_size = 0
    
    def get(self, key: str) -> Optional[Any]:
        """Get value from cache"""
        if key in self.cache:
            entry = self.cache[key]
            
            # Check TTL
            if entry.ttl and time.time() > entry.created_at + entry.ttl:
                self.delete(key)
                return None
            
            # Update access info
            entry.accessed_at = time.time()
            entry.access_count += 1
            self.access_counts[key] = entry.access_count
            
            # Move to end for LRU
            if self.policy == CachePolicy.LRU:
                self.cache.move_to_end(key)
            
            return entry.value
        
        return None
    
    def set(self, key: str, value: Any, ttl: Optional[float] = None, 
            size_bytes: Optional[int] = None) -> bool:
        """Set value in cache"""
        if size_bytes is None:
            size_bytes = len(str(value).encode())
        
        # Check if key already exists
        if key in self.cache:
            old_entry = self.cache[key]
            self.total_size -= old_entry.size_bytes
        
        # Create new entry
        entry = CacheEntry(
            key=key,
            value=value,
            created_at=time.time(),
            accessed_at=time.time(),
            access_count=1,
            size_bytes=size_bytes,
            ttl=ttl
        )
        
        # Check capacity and evict if necessary
        while len(self.cache) >= self.max_size or self.total_size + size_bytes > self.max_size * 1024:
            if not self._evict_entry():
                return False
        
        # Add entry
        self.cache[key] = entry
        self.access_counts[key] = 1
        self.total_size += size_bytes
        
        return True
    
    def delete(self, key: str) -> bool:
        """Delete entry from cache"""
        if key in self.cache:
            entry = self.cache[key]
            self.total_size -= entry.size_bytes
            del self.cache[key]
            if key in self.access_counts:
                del self.access_counts[key]
            return True
        return False
    
    def clear(self):
        """Clear all entries"""
        self.cache.clear()
        self.access_counts.clear()
        self.total_size = 0
    
    def _evict_entry(self) -> bool:
        """Evict entry based on policy"""
        if not self.cache:
            return False
        
        if self.policy == CachePolicy.LRU:
            # Remove least recently used
            key = next(iter(self.cache))
        elif self.policy == CachePolicy.LFU:
            # Remove least frequently used
            key = min(self.access_counts.items(), key=lambda x: x[1])[0]
        elif self.policy == CachePolicy.FIFO:
            # Remove first in
            key = next(iter(self.cache))
        else:  # TTL
            # Remove expired entries first, then LRU
            current_time = time.time()
            for key, entry in self.cache.items():
                if entry.ttl and current_time > entry.created_at + entry.ttl:
                    break
            else:
                key = next(iter(self.cache))
        
        self.delete(key)
        return True
    
    def get_stats(self) -> Dict[str, Any]:
        """Get cache statistics"""
        return {
            'size': len(self.cache),
            'max_size': self.max_size,
            'total_size_bytes': self.total_size,
            'policy': self.policy.value,
            'hit_rate': self._calculate_hit_rate()
        }
    
    def _calculate_hit_rate(self) -> float:
        """Calculate cache hit rate"""
        # This would track hits/misses in a real implementation
        return 0.85  # Placeholder

class DiskCache:
    """Disk-based cache for larger data"""
    
    def __init__(self, cache_dir: str, max_size_mb: int = 1024):
        self.cache_dir = cache_dir
        self.max_size_mb = max_size_mb
        self.metadata_file = os.path.join(cache_dir, 'metadata.json')
        self.metadata: Dict[str, Dict] = {}
        
        os.makedirs(cache_dir, exist_ok=True)
        self._load_metadata()
    
    def _load_metadata(self):
        """Load cache metadata"""
        if os.path.exists(self.metadata_file):
            try:
                with open(self.metadata_file, 'r') as f:
                    self.metadata = json.load(f)
            except Exception as e:
                print(f"Error loading cache metadata: {e}")
                self.metadata = {}
    
    def _save_metadata(self):
        """Save cache metadata"""
        try:
            with open(self.metadata_file, 'w') as f:
                json.dump(self.metadata, f, indent=2)
        except Exception as e:
            print(f"Error saving cache metadata: {e}")
    
    def get(self, key: str) -> Optional[bytes]:
        """Get value from disk cache"""
        if key not in self.metadata:
            return None
        
        entry = self.metadata[key]
        file_path = os.path.join(self.cache_dir, f"{key}.cache")
        
        # Check TTL
        if entry.get('ttl') and time.time() > entry['created_at'] + entry['ttl']:
            self.delete(key)
            return None
        
        # Check if file exists
        if not os.path.exists(file_path):
            self.delete(key)
            return None
        
        try:
            with open(file_path, 'rb') as f:
                data = f.read()
            
            # Update access time
            entry['accessed_at'] = time.time()
            entry['access_count'] += 1
            self._save_metadata()
            
            return data
            
        except Exception as e:
            print(f"Error reading cache file {key}: {e}")
            return None
    
    def set(self, key: str, value: bytes, ttl: Optional[float] = None) -> bool:
        """Set value in disk cache"""
        try:
            file_path = os.path.join(self.cache_dir, f"{key}.cache")
            
            # Check size limit
            current_size = self._get_cache_size()
            if current_size + len(value) > self.max_size_mb * 1024 * 1024:
                self._cleanup_cache()
            
            # Write file
            with open(file_path, 'wb') as f:
                f.write(value)
            
            # Update metadata
            self.metadata[key] = {
                'created_at': time.time(),
                'accessed_at': time.time(),
                'access_count': 1,
                'size_bytes': len(value),
                'ttl': ttl
            }
            
            self._save_metadata()
            return True
            
        except Exception as e:
            print(f"Error writing cache file {key}: {e}")
            return False
    
    def delete(self, key: str) -> bool:
        """Delete entry from disk cache"""
        try:
            file_path = os.path.join(self.cache_dir, f"{key}.cache")
            
            if os.path.exists(file_path):
                os.remove(file_path)
            
            if key in self.metadata:
                del self.metadata[key]
                self._save_metadata()
            
            return True
            
        except Exception as e:
            print(f"Error deleting cache file {key}: {e}")
            return False
    
    def _get_cache_size(self) -> int:
        """Get total cache size in bytes"""
        total_size = 0
        for entry in self.metadata.values():
            total_size += entry.get('size_bytes', 0)
        return total_size
    
    def _cleanup_cache(self):
        """Clean up old cache entries"""
        # Remove expired entries
        current_time = time.time()
        expired_keys = []
        
        for key, entry in self.metadata.items():
            if entry.get('ttl') and current_time > entry['created_at'] + entry['ttl']:
                expired_keys.append(key)
        
        for key in expired_keys:
            self.delete(key)

class CDNManager:
    """CDN management system"""
    
    def __init__(self):
        self.cdn_endpoints: Dict[str, Dict] = {}
        self.cache_rules: Dict[str, Dict] = {}
        self.invalidation_queue: List[str] = []
    
    def add_cdn_endpoint(self, endpoint_id: str, endpoint_config: Dict):
        """Add CDN endpoint"""
        self.cdn_endpoints[endpoint_id] = endpoint_config
    
    def cache_package(self, package_id: str, package_data: bytes, 
                     endpoints: List[str] = None) -> bool:
        """Cache package in CDN"""
        if endpoints is None:
            endpoints = list(self.cdn_endpoints.keys())
        
        try:
            for endpoint_id in endpoints:
                if endpoint_id in self.cdn_endpoints:
                    # This would upload to actual CDN in real implementation
                    print(f"Caching package {package_id} in CDN endpoint {endpoint_id}")
            
            return True
            
        except Exception as e:
            print(f"Error caching package {package_id}: {e}")
            return False
    
    def invalidate_cache(self, package_id: str, endpoints: List[str] = None):
        """Invalidate package cache in CDN"""
        if endpoints is None:
            endpoints = list(self.cdn_endpoints.keys())
        
        for endpoint_id in endpoints:
            if endpoint_id in self.cdn_endpoints:
                self.invalidation_queue.append(f"{endpoint_id}:{package_id}")
    
    def get_cdn_url(self, package_id: str, endpoint_id: str) -> Optional[str]:
        """Get CDN URL for package"""
        if endpoint_id in self.cdn_endpoints:
            endpoint = self.cdn_endpoints[endpoint_id]
            return f"{endpoint['url']}/packages/{package_id}"
        return None

class RegistryCacheManager:
    """Main registry cache manager"""
    
    def __init__(self, cache_dir: str = "/var/tusklang/registry/cache"):
        self.memory_cache = MemoryCache(max_size=1000, policy=CachePolicy.LRU)
        self.disk_cache = DiskCache(cache_dir, max_size_mb=1024)
        self.cdn_manager = CDNManager()
        
        # Cache keys for different data types
        self.cache_keys = {
            'package_metadata': 'meta:{package_id}',
            'package_data': 'data:{package_id}',
            'user_profile': 'user:{user_id}',
            'permission_cache': 'perm:{user_id}:{resource}',
            'search_results': 'search:{query_hash}'
        }
    
    def get_package_metadata(self, package_id: str) -> Optional[Dict]:
        """Get package metadata from cache"""
        key = self.cache_keys['package_metadata'].format(package_id=package_id)
        
        # Try memory cache first
        result = self.memory_cache.get(key)
        if result:
            return result
        
        # Try disk cache
        cached_data = self.disk_cache.get(key)
        if cached_data:
            try:
                data = json.loads(cached_data.decode())
                # Store in memory cache
                self.memory_cache.set(key, data, ttl=3600)
                return data
            except:
                pass
        
        return None
    
    def set_package_metadata(self, package_id: str, metadata: Dict, ttl: float = 3600):
        """Set package metadata in cache"""
        key = self.cache_keys['package_metadata'].format(package_id=package_id)
        
        # Store in both caches
        self.memory_cache.set(key, metadata, ttl=ttl)
        self.disk_cache.set(key, json.dumps(metadata).encode(), ttl=ttl)
    
    def get_package_data(self, package_id: str) -> Optional[bytes]:
        """Get package data from cache"""
        key = self.cache_keys['package_data'].format(package_id=package_id)
        
        # Try memory cache first (for small packages)
        result = self.memory_cache.get(key)
        if result:
            return result
        
        # Try disk cache
        return self.disk_cache.get(key)
    
    def set_package_data(self, package_id: str, data: bytes, ttl: float = 86400):
        """Set package data in cache"""
        key = self.cache_keys['package_data'].format(package_id=package_id)
        
        # Store in appropriate cache based on size
        if len(data) < 1024 * 1024:  # Less than 1MB
            self.memory_cache.set(key, data, ttl=ttl)
        else:
            self.disk_cache.set(key, data, ttl=ttl)
    
    def invalidate_package_cache(self, package_id: str):
        """Invalidate all cache entries for a package"""
        metadata_key = self.cache_keys['package_metadata'].format(package_id=package_id)
        data_key = self.cache_keys['package_data'].format(package_id=package_id)
        
        self.memory_cache.delete(metadata_key)
        self.memory_cache.delete(data_key)
        self.disk_cache.delete(metadata_key)
        self.disk_cache.delete(data_key)
        
        # Invalidate CDN cache
        self.cdn_manager.invalidate_cache(package_id)
    
    def get_cache_stats(self) -> Dict[str, Any]:
        """Get comprehensive cache statistics"""
        return {
            'memory_cache': self.memory_cache.get_stats(),
            'disk_cache': {
                'size': len(self.disk_cache.metadata),
                'total_size_mb': self.disk_cache._get_cache_size() / (1024 * 1024),
                'max_size_mb': self.disk_cache.max_size_mb
            },
            'cdn_endpoints': len(self.cdn_manager.cdn_endpoints),
            'invalidation_queue': len(self.cdn_manager.invalidation_queue)
        }

# Global cache manager instance
cache_manager = RegistryCacheManager() 