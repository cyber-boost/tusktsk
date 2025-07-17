#!/usr/bin/env python3
"""
TuskLang Health Check System
Comprehensive health monitoring for all TuskLang services
"""

import asyncio
import aiohttp
import psutil
import redis
import psycopg2
import json
import time
import logging
from typing import Dict, List, Any
from dataclasses import dataclass
from enum import Enum

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class HealthStatus(Enum):
    HEALTHY = "healthy"
    DEGRADED = "degraded"
    UNHEALTHY = "unhealthy"
    UNKNOWN = "unknown"

@dataclass
class HealthCheck:
    name: str
    status: HealthStatus
    message: str
    details: Dict[str, Any] = None
    timestamp: float = None

class TuskLangHealthChecker:
    def __init__(self, config: Dict[str, Any]):
        self.config = config
        self.checks: List[HealthCheck] = []
        
    async def check_system_resources(self) -> HealthCheck:
        """Check system resource usage"""
        try:
            cpu_percent = psutil.cpu_percent(interval=1)
            memory = psutil.virtual_memory()
            disk = psutil.disk_usage('/')
            
            status = HealthStatus.HEALTHY
            message = "System resources are normal"
            
            if cpu_percent > 80:
                status = HealthStatus.DEGRADED
                message = f"High CPU usage: {cpu_percent}%"
            elif memory.percent > 85:
                status = HealthStatus.DEGRADED
                message = f"High memory usage: {memory.percent}%"
            elif disk.percent > 90:
                status = HealthStatus.DEGRADED
                message = f"High disk usage: {disk.percent}%"
                
            return HealthCheck(
                name="system_resources",
                status=status,
                message=message,
                details={
                    "cpu_percent": cpu_percent,
                    "memory_percent": memory.percent,
                    "disk_percent": disk.percent,
                    "memory_available": memory.available,
                    "disk_free": disk.free
                },
                timestamp=time.time()
            )
        except Exception as e:
            logger.error(f"System resource check failed: {e}")
            return HealthCheck(
                name="system_resources",
                status=HealthStatus.UNHEALTHY,
                message=f"System resource check failed: {str(e)}",
                timestamp=time.time()
            )

    async def check_database(self) -> HealthCheck:
        """Check database connectivity and performance"""
        try:
            conn = psycopg2.connect(
                host=self.config.get('database', {}).get('host', 'localhost'),
                port=self.config.get('database', {}).get('port', 5432),
                database=self.config.get('database', {}).get('name', 'tusklang'),
                user=self.config.get('database', {}).get('user', 'postgres'),
                password=self.config.get('database', {}).get('password', '')
            )
            
            cursor = conn.cursor()
            
            # Check basic connectivity
            cursor.execute("SELECT 1")
            result = cursor.fetchone()
            
            if result[0] != 1:
                raise Exception("Database query failed")
            
            # Check active connections
            cursor.execute("SELECT count(*) FROM pg_stat_activity")
            active_connections = cursor.fetchone()[0]
            
            # Check database size
            cursor.execute("SELECT pg_database_size(current_database())")
            db_size = cursor.fetchone()[0]
            
            cursor.close()
            conn.close()
            
            status = HealthStatus.HEALTHY
            message = "Database is healthy"
            
            if active_connections > 100:
                status = HealthStatus.DEGRADED
                message = f"High number of active connections: {active_connections}"
                
            return HealthCheck(
                name="database",
                status=status,
                message=message,
                details={
                    "active_connections": active_connections,
                    "database_size_bytes": db_size,
                    "database_size_mb": round(db_size / 1024 / 1024, 2)
                },
                timestamp=time.time()
            )
        except Exception as e:
            logger.error(f"Database check failed: {e}")
            return HealthCheck(
                name="database",
                status=HealthStatus.UNHEALTHY,
                message=f"Database check failed: {str(e)}",
                timestamp=time.time()
            )

    async def check_redis(self) -> HealthCheck:
        """Check Redis connectivity and performance"""
        try:
            r = redis.Redis(
                host=self.config.get('redis', {}).get('host', 'localhost'),
                port=self.config.get('redis', {}).get('port', 6379),
                password=self.config.get('redis', {}).get('password', None),
                decode_responses=True
            )
            
            # Test basic connectivity
            r.ping()
            
            # Get Redis info
            info = r.info()
            
            # Check memory usage
            used_memory = info.get('used_memory', 0)
            max_memory = info.get('maxmemory', 0)
            memory_usage_percent = (used_memory / max_memory * 100) if max_memory > 0 else 0
            
            # Check connected clients
            connected_clients = info.get('connected_clients', 0)
            
            status = HealthStatus.HEALTHY
            message = "Redis is healthy"
            
            if memory_usage_percent > 80:
                status = HealthStatus.DEGRADED
                message = f"High Redis memory usage: {memory_usage_percent:.1f}%"
            elif connected_clients > 100:
                status = HealthStatus.DEGRADED
                message = f"High number of Redis clients: {connected_clients}"
                
            return HealthCheck(
                name="redis",
                status=status,
                message=message,
                details={
                    "memory_usage_percent": memory_usage_percent,
                    "connected_clients": connected_clients,
                    "used_memory_mb": round(used_memory / 1024 / 1024, 2),
                    "max_memory_mb": round(max_memory / 1024 / 1024, 2) if max_memory > 0 else 0
                },
                timestamp=time.time()
            )
        except Exception as e:
            logger.error(f"Redis check failed: {e}")
            return HealthCheck(
                name="redis",
                status=HealthStatus.UNHEALTHY,
                message=f"Redis check failed: {str(e)}",
                timestamp=time.time()
            )

    async def check_package_registry(self) -> HealthCheck:
        """Check package registry service health"""
        try:
            registry_url = self.config.get('registry', {}).get('url', 'http://localhost:8000')
            
            async with aiohttp.ClientSession() as session:
                # Check health endpoint
                async with session.get(f"{registry_url}/health") as response:
                    if response.status != 200:
                        raise Exception(f"Health endpoint returned status {response.status}")
                    
                    health_data = await response.json()
                    
                # Check metrics endpoint
                async with session.get(f"{registry_url}/metrics") as response:
                    if response.status != 200:
                        raise Exception(f"Metrics endpoint returned status {response.status}")
                    
                    metrics_data = await response.text()
                    
            status = HealthStatus.HEALTHY
            message = "Package registry is healthy"
            
            # Parse metrics for additional checks
            if "tusklang_package_upload_failures_total" in metrics_data:
                status = HealthStatus.DEGRADED
                message = "Package upload failures detected"
                
            return HealthCheck(
                name="package_registry",
                status=status,
                message=message,
                details=health_data,
                timestamp=time.time()
            )
        except Exception as e:
            logger.error(f"Package registry check failed: {e}")
            return HealthCheck(
                name="package_registry",
                status=HealthStatus.UNHEALTHY,
                message=f"Package registry check failed: {str(e)}",
                timestamp=time.time()
            )

    async def check_cdn(self) -> HealthCheck:
        """Check CDN health and synchronization"""
        try:
            cdn_config = self.config.get('cdn', {})
            cdn_url = cdn_config.get('url', 'https://cdn.tusklang.org')
            
            async with aiohttp.ClientSession() as session:
                # Check CDN health
                async with session.get(f"{cdn_url}/health") as response:
                    if response.status != 200:
                        raise Exception(f"CDN health check returned status {response.status}")
                    
                    cdn_health = await response.json()
                    
                # Check sync status
                async with session.get(f"{cdn_url}/sync/status") as response:
                    if response.status != 200:
                        raise Exception(f"CDN sync status check returned status {response.status}")
                    
                    sync_status = await response.json()
                    
            status = HealthStatus.HEALTHY
            message = "CDN is healthy"
            
            if not sync_status.get('synced', False):
                status = HealthStatus.DEGRADED
                message = "CDN synchronization issues detected"
                
            return HealthCheck(
                name="cdn",
                status=status,
                message=message,
                details={
                    "cdn_health": cdn_health,
                    "sync_status": sync_status
                },
                timestamp=time.time()
            )
        except Exception as e:
            logger.error(f"CDN check failed: {e}")
            return HealthCheck(
                name="cdn",
                status=HealthStatus.UNHEALTHY,
                message=f"CDN check failed: {str(e)}",
                timestamp=time.time()
            )

    async def check_security_scanning(self) -> HealthCheck:
        """Check security scanning service health"""
        try:
            security_url = self.config.get('security', {}).get('url', 'http://localhost:9000')
            
            async with aiohttp.ClientSession() as session:
                async with session.get(f"{security_url}/health") as response:
                    if response.status != 200:
                        raise Exception(f"Security service returned status {response.status}")
                    
                    security_health = await response.json()
                    
            status = HealthStatus.HEALTHY
            message = "Security scanning is healthy"
            
            if not security_health.get('scanner_active', False):
                status = HealthStatus.DEGRADED
                message = "Security scanner is not active"
                
            return HealthCheck(
                name="security_scanning",
                status=status,
                message=message,
                details=security_health,
                timestamp=time.time()
            )
        except Exception as e:
            logger.error(f"Security scanning check failed: {e}")
            return HealthCheck(
                name="security_scanning",
                status=HealthStatus.UNHEALTHY,
                message=f"Security scanning check failed: {str(e)}",
                timestamp=time.time()
            )

    async def run_all_checks(self) -> Dict[str, Any]:
        """Run all health checks and return comprehensive report"""
        logger.info("Starting comprehensive health check...")
        
        checks = [
            self.check_system_resources(),
            self.check_database(),
            self.check_redis(),
            self.check_package_registry(),
            self.check_cdn(),
            self.check_security_scanning()
        ]
        
        results = await asyncio.gather(*checks, return_exceptions=True)
        
        # Process results
        health_checks = []
        overall_status = HealthStatus.HEALTHY
        
        for result in results:
            if isinstance(result, Exception):
                health_checks.append(HealthCheck(
                    name="unknown",
                    status=HealthStatus.UNKNOWN,
                    message=f"Check failed with exception: {str(result)}",
                    timestamp=time.time()
                ))
                overall_status = HealthStatus.UNHEALTHY
            else:
                health_checks.append(result)
                if result.status == HealthStatus.UNHEALTHY:
                    overall_status = HealthStatus.UNHEALTHY
                elif result.status == HealthStatus.DEGRADED and overall_status == HealthStatus.HEALTHY:
                    overall_status = HealthStatus.DEGRADED
        
        # Generate report
        report = {
            "timestamp": time.time(),
            "overall_status": overall_status.value,
            "checks": [
                {
                    "name": check.name,
                    "status": check.status.value,
                    "message": check.message,
                    "details": check.details,
                    "timestamp": check.timestamp
                }
                for check in health_checks
            ],
            "summary": {
                "total_checks": len(health_checks),
                "healthy": len([c for c in health_checks if c.status == HealthStatus.HEALTHY]),
                "degraded": len([c for c in health_checks if c.status == HealthStatus.DEGRADED]),
                "unhealthy": len([c for c in health_checks if c.status == HealthStatus.UNHEALTHY]),
                "unknown": len([c for c in health_checks if c.status == HealthStatus.UNKNOWN])
            }
        }
        
        logger.info(f"Health check completed. Overall status: {overall_status.value}")
        return report

async def main():
    """Main function to run health checks"""
    # Configuration (in production, load from environment or config file)
    config = {
        "database": {
            "host": "localhost",
            "port": 5432,
            "name": "tusklang",
            "user": "postgres",
            "password": ""
        },
        "redis": {
            "host": "localhost",
            "port": 6379,
            "password": None
        },
        "registry": {
            "url": "http://localhost:8000"
        },
        "cdn": {
            "url": "https://cdn.tusklang.org"
        },
        "security": {
            "url": "http://localhost:9000"
        }
    }
    
    checker = TuskLangHealthChecker(config)
    report = await checker.run_all_checks()
    
    # Output report
    print(json.dumps(report, indent=2))
    
    # Exit with appropriate code
    if report["overall_status"] == "unhealthy":
        exit(1)
    elif report["overall_status"] == "degraded":
        exit(2)
    else:
        exit(0)

if __name__ == "__main__":
    asyncio.run(main()) 