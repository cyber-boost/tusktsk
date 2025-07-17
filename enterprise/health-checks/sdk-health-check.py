#!/usr/bin/env python3
"""
SDK Health Check System for Agent a1
Comprehensive validation of Go, Rust, and Java SDK implementations
"""

import os
import sys
import json
import time
import subprocess
import requests
import platform
import psutil
from typing import Dict, List, Any, Optional
from dataclasses import dataclass
from enum import Enum

class HealthStatus(Enum):
    HEALTHY = "healthy"
    WARNING = "warning"
    CRITICAL = "critical"
    UNKNOWN = "unknown"

@dataclass
class HealthCheckResult:
    name: str
    status: HealthStatus
    message: str
    details: Dict[str, Any]
    duration: float
    timestamp: float

class SDKHealthChecker:
    """Comprehensive health checker for TuskLang SDKs"""
    
    def __init__(self):
        self.registry_url = os.getenv("REGISTRY_URL", "http://localhost:8080")
        self.cdn_url = os.getenv("CDN_URL", "http://localhost:3000")
        self.results: List[HealthCheckResult] = []
        
    def check_system_resources(self) -> HealthCheckResult:
        """Check system resource availability"""
        start_time = time.time()
        
        try:
            # CPU usage
            cpu_percent = psutil.cpu_percent(interval=1)
            
            # Memory usage
            memory = psutil.virtual_memory()
            
            # Disk usage
            disk = psutil.disk_usage('/')
            
            # Network connectivity
            network_ok = self._check_network_connectivity()
            
            # Determine status
            if cpu_percent > 90 or memory.percent > 90 or disk.percent > 90:
                status = HealthStatus.CRITICAL
            elif cpu_percent > 70 or memory.percent > 70 or disk.percent > 70:
                status = HealthStatus.WARNING
            else:
                status = HealthStatus.HEALTHY
            
            message = f"System resources: CPU {cpu_percent}%, Memory {memory.percent}%, Disk {disk.percent}%"
            
            return HealthCheckResult(
                name="system_resources",
                status=status,
                message=message,
                details={
                    "cpu_percent": cpu_percent,
                    "memory_percent": memory.percent,
                    "disk_percent": disk.percent,
                    "network_ok": network_ok
                },
                duration=time.time() - start_time,
                timestamp=time.time()
            )
            
        except Exception as e:
            return HealthCheckResult(
                name="system_resources",
                status=HealthStatus.UNKNOWN,
                message=f"Failed to check system resources: {str(e)}",
                details={"error": str(e)},
                duration=time.time() - start_time,
                timestamp=time.time()
            )
    
    def check_go_sdk(self) -> HealthCheckResult:
        """Check Go SDK health"""
        start_time = time.time()
        
        try:
            # Check if Go is installed
            go_version = subprocess.run(
                ["go", "version"], 
                capture_output=True, 
                text=True, 
                timeout=10
            )
            
            if go_version.returncode != 0:
                return HealthCheckResult(
                    name="go_sdk",
                    status=HealthStatus.CRITICAL,
                    message="Go is not installed or not accessible",
                    details={"error": go_version.stderr},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Check Go SDK directory
            sdk_dir = "sdk/go"
            if not os.path.exists(sdk_dir):
                return HealthCheckResult(
                    name="go_sdk",
                    status=HealthStatus.CRITICAL,
                    message="Go SDK directory not found",
                    details={"sdk_dir": sdk_dir},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Test Go SDK build
            build_result = subprocess.run(
                ["go", "build", "./..."],
                cwd=sdk_dir,
                capture_output=True,
                text=True,
                timeout=60
            )
            
            if build_result.returncode != 0:
                return HealthCheckResult(
                    name="go_sdk",
                    status=HealthStatus.CRITICAL,
                    message="Go SDK build failed",
                    details={"error": build_result.stderr},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Test Go SDK tests
            test_result = subprocess.run(
                ["go", "test", "./..."],
                cwd=sdk_dir,
                capture_output=True,
                text=True,
                timeout=120
            )
            
            if test_result.returncode != 0:
                return HealthCheckResult(
                    name="go_sdk",
                    status=HealthStatus.WARNING,
                    message="Go SDK tests failed",
                    details={"error": test_result.stderr},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Check Go SDK health endpoint
            health_ok = self._check_sdk_health_endpoint("go", 8081)
            
            return HealthCheckResult(
                name="go_sdk",
                status=HealthStatus.HEALTHY,
                message="Go SDK is healthy",
                details={
                    "go_version": go_version.stdout.strip(),
                    "build_ok": True,
                    "tests_ok": True,
                    "health_endpoint_ok": health_ok
                },
                duration=time.time() - start_time,
                timestamp=time.time()
            )
            
        except subprocess.TimeoutExpired:
            return HealthCheckResult(
                name="go_sdk",
                status=HealthStatus.CRITICAL,
                message="Go SDK check timed out",
                details={"error": "Timeout"},
                duration=time.time() - start_time,
                timestamp=time.time()
            )
        except Exception as e:
            return HealthCheckResult(
                name="go_sdk",
                status=HealthStatus.UNKNOWN,
                message=f"Go SDK check failed: {str(e)}",
                details={"error": str(e)},
                duration=time.time() - start_time,
                timestamp=time.time()
            )
    
    def check_rust_sdk(self) -> HealthCheckResult:
        """Check Rust SDK health"""
        start_time = time.time()
        
        try:
            # Check if Rust is installed
            rust_version = subprocess.run(
                ["rustc", "--version"], 
                capture_output=True, 
                text=True, 
                timeout=10
            )
            
            if rust_version.returncode != 0:
                return HealthCheckResult(
                    name="rust_sdk",
                    status=HealthStatus.CRITICAL,
                    message="Rust is not installed or not accessible",
                    details={"error": rust_version.stderr},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Check Rust SDK directory
            sdk_dir = "sdk/rust"
            if not os.path.exists(sdk_dir):
                return HealthCheckResult(
                    name="rust_sdk",
                    status=HealthStatus.CRITICAL,
                    message="Rust SDK directory not found",
                    details={"sdk_dir": sdk_dir},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Test Rust SDK build
            build_result = subprocess.run(
                ["cargo", "build", "--release"],
                cwd=sdk_dir,
                capture_output=True,
                text=True,
                timeout=120
            )
            
            if build_result.returncode != 0:
                return HealthCheckResult(
                    name="rust_sdk",
                    status=HealthStatus.CRITICAL,
                    message="Rust SDK build failed",
                    details={"error": build_result.stderr},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Test Rust SDK tests
            test_result = subprocess.run(
                ["cargo", "test"],
                cwd=sdk_dir,
                capture_output=True,
                text=True,
                timeout=180
            )
            
            if test_result.returncode != 0:
                return HealthCheckResult(
                    name="rust_sdk",
                    status=HealthStatus.WARNING,
                    message="Rust SDK tests failed",
                    details={"error": test_result.stderr},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Check Rust SDK health endpoint
            health_ok = self._check_sdk_health_endpoint("rust", 8082)
            
            return HealthCheckResult(
                name="rust_sdk",
                status=HealthStatus.HEALTHY,
                message="Rust SDK is healthy",
                details={
                    "rust_version": rust_version.stdout.strip(),
                    "build_ok": True,
                    "tests_ok": True,
                    "health_endpoint_ok": health_ok
                },
                duration=time.time() - start_time,
                timestamp=time.time()
            )
            
        except subprocess.TimeoutExpired:
            return HealthCheckResult(
                name="rust_sdk",
                status=HealthStatus.CRITICAL,
                message="Rust SDK check timed out",
                details={"error": "Timeout"},
                duration=time.time() - start_time,
                timestamp=time.time()
            )
        except Exception as e:
            return HealthCheckResult(
                name="rust_sdk",
                status=HealthStatus.UNKNOWN,
                message=f"Rust SDK check failed: {str(e)}",
                details={"error": str(e)},
                duration=time.time() - start_time,
                timestamp=time.time()
            )
    
    def check_java_sdk(self) -> HealthCheckResult:
        """Check Java SDK health"""
        start_time = time.time()
        
        try:
            # Check if Java is installed
            java_version = subprocess.run(
                ["java", "-version"], 
                capture_output=True, 
                text=True, 
                timeout=10
            )
            
            if java_version.returncode != 0:
                return HealthCheckResult(
                    name="java_sdk",
                    status=HealthStatus.CRITICAL,
                    message="Java is not installed or not accessible",
                    details={"error": java_version.stderr},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Check Java SDK directory
            sdk_dir = "sdk/java"
            if not os.path.exists(sdk_dir):
                return HealthCheckResult(
                    name="java_sdk",
                    status=HealthStatus.CRITICAL,
                    message="Java SDK directory not found",
                    details={"sdk_dir": sdk_dir},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Test Java SDK build
            build_result = subprocess.run(
                ["./gradlew", "build"],
                cwd=sdk_dir,
                capture_output=True,
                text=True,
                timeout=180
            )
            
            if build_result.returncode != 0:
                return HealthCheckResult(
                    name="java_sdk",
                    status=HealthStatus.CRITICAL,
                    message="Java SDK build failed",
                    details={"error": build_result.stderr},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Test Java SDK tests
            test_result = subprocess.run(
                ["./gradlew", "test"],
                cwd=sdk_dir,
                capture_output=True,
                text=True,
                timeout=240
            )
            
            if test_result.returncode != 0:
                return HealthCheckResult(
                    name="java_sdk",
                    status=HealthStatus.WARNING,
                    message="Java SDK tests failed",
                    details={"error": test_result.stderr},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Check Java SDK health endpoint
            health_ok = self._check_sdk_health_endpoint("java", 8083)
            
            return HealthCheckResult(
                name="java_sdk",
                status=HealthStatus.HEALTHY,
                message="Java SDK is healthy",
                details={
                    "java_version": java_version.stdout.strip(),
                    "build_ok": True,
                    "tests_ok": True,
                    "health_endpoint_ok": health_ok
                },
                duration=time.time() - start_time,
                timestamp=time.time()
            )
            
        except subprocess.TimeoutExpired:
            return HealthCheckResult(
                name="java_sdk",
                status=HealthStatus.CRITICAL,
                message="Java SDK check timed out",
                details={"error": "Timeout"},
                duration=time.time() - start_time,
                timestamp=time.time()
            )
        except Exception as e:
            return HealthCheckResult(
                name="java_sdk",
                status=HealthStatus.UNKNOWN,
                message=f"Java SDK check failed: {str(e)}",
                details={"error": str(e)},
                duration=time.time() - start_time,
                timestamp=time.time()
            )
    
    def check_package_registry(self) -> HealthCheckResult:
        """Check package registry health"""
        start_time = time.time()
        
        try:
            # Check registry connectivity
            response = requests.get(f"{self.registry_url}/api/v1/health", timeout=10)
            
            if response.status_code != 200:
                return HealthCheckResult(
                    name="package_registry",
                    status=HealthStatus.CRITICAL,
                    message=f"Package registry health check failed: {response.status_code}",
                    details={"status_code": response.status_code},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Check registry functionality
            health_data = response.json()
            
            # Test package search
            search_response = requests.get(
                f"{self.registry_url}/api/v1/packages/search?language=go",
                timeout=10
            )
            
            search_ok = search_response.status_code == 200
            
            return HealthCheckResult(
                name="package_registry",
                status=HealthStatus.HEALTHY,
                message="Package registry is healthy",
                details={
                    "health_data": health_data,
                    "search_functional": search_ok
                },
                duration=time.time() - start_time,
                timestamp=time.time()
            )
            
        except requests.exceptions.RequestException as e:
            return HealthCheckResult(
                name="package_registry",
                status=HealthStatus.CRITICAL,
                message=f"Package registry connection failed: {str(e)}",
                details={"error": str(e)},
                duration=time.time() - start_time,
                timestamp=time.time()
            )
        except Exception as e:
            return HealthCheckResult(
                name="package_registry",
                status=HealthStatus.UNKNOWN,
                message=f"Package registry check failed: {str(e)}",
                details={"error": str(e)},
                duration=time.time() - start_time,
                timestamp=time.time()
            )
    
    def check_cdn(self) -> HealthCheckResult:
        """Check CDN health"""
        start_time = time.time()
        
        try:
            # Check CDN connectivity
            response = requests.get(f"{self.cdn_url}/health", timeout=10)
            
            if response.status_code != 200:
                return HealthCheckResult(
                    name="cdn",
                    status=HealthStatus.CRITICAL,
                    message=f"CDN health check failed: {response.status_code}",
                    details={"status_code": response.status_code},
                    duration=time.time() - start_time,
                    timestamp=time.time()
                )
            
            # Test SDK package availability
            test_packages = [
                "sdk/go/tusk-go-sdk-latest.tar.gz",
                "sdk/rust/tusk-rust-sdk-latest.crate",
                "sdk/java/tusk-java-sdk-latest.jar"
            ]
            
            available_packages = 0
            for package in test_packages:
                try:
                    pkg_response = requests.head(f"{self.cdn_url}/{package}", timeout=5)
                    if pkg_response.status_code == 200:
                        available_packages += 1
                except:
                    pass
            
            cdn_health = available_packages / len(test_packages)
            
            if cdn_health < 0.5:
                status = HealthStatus.CRITICAL
            elif cdn_health < 0.8:
                status = HealthStatus.WARNING
            else:
                status = HealthStatus.HEALTHY
            
            return HealthCheckResult(
                name="cdn",
                status=status,
                message=f"CDN health: {cdn_health:.1%} packages available",
                details={
                    "available_packages": available_packages,
                    "total_packages": len(test_packages),
                    "health_percentage": cdn_health
                },
                duration=time.time() - start_time,
                timestamp=time.time()
            )
            
        except requests.exceptions.RequestException as e:
            return HealthCheckResult(
                name="cdn",
                status=HealthStatus.CRITICAL,
                message=f"CDN connection failed: {str(e)}",
                details={"error": str(e)},
                duration=time.time() - start_time,
                timestamp=time.time()
            )
        except Exception as e:
            return HealthCheckResult(
                name="cdn",
                status=HealthStatus.UNKNOWN,
                message=f"CDN check failed: {str(e)}",
                details={"error": str(e)},
                duration=time.time() - start_time,
                timestamp=time.time()
            )
    
    def _check_network_connectivity(self) -> bool:
        """Check basic network connectivity"""
        try:
            requests.get("https://www.google.com", timeout=5)
            return True
        except:
            return False
    
    def _check_sdk_health_endpoint(self, language: str, port: int) -> bool:
        """Check SDK health endpoint"""
        try:
            response = requests.get(f"http://localhost:{port}/health", timeout=5)
            return response.status_code == 200
        except:
            return False
    
    def run_all_checks(self) -> List[HealthCheckResult]:
        """Run all health checks"""
        checks = [
            self.check_system_resources,
            self.check_go_sdk,
            self.check_rust_sdk,
            self.check_java_sdk,
            self.check_package_registry,
            self.check_cdn
        ]
        
        for check in checks:
            try:
                result = check()
                self.results.append(result)
            except Exception as e:
                self.results.append(HealthCheckResult(
                    name=check.__name__,
                    status=HealthStatus.UNKNOWN,
                    message=f"Check failed: {str(e)}",
                    details={"error": str(e)},
                    duration=0,
                    timestamp=time.time()
                ))
        
        return self.results
    
    def generate_report(self) -> Dict[str, Any]:
        """Generate comprehensive health report"""
        if not self.results:
            self.run_all_checks()
        
        # Calculate overall status
        status_counts = {}
        for result in self.results:
            status_counts[result.status.value] = status_counts.get(result.status.value, 0) + 1
        
        if status_counts.get(HealthStatus.CRITICAL.value, 0) > 0:
            overall_status = HealthStatus.CRITICAL
        elif status_counts.get(HealthStatus.WARNING.value, 0) > 0:
            overall_status = HealthStatus.WARNING
        else:
            overall_status = HealthStatus.HEALTHY
        
        # Calculate total duration
        total_duration = sum(result.duration for result in self.results)
        
        return {
            "overall_status": overall_status.value,
            "timestamp": time.time(),
            "duration": total_duration,
            "status_counts": status_counts,
            "checks": [
                {
                    "name": result.name,
                    "status": result.status.value,
                    "message": result.message,
                    "details": result.details,
                    "duration": result.duration
                }
                for result in self.results
            ]
        }
    
    def print_report(self):
        """Print formatted health report"""
        report = self.generate_report()
        
        print("=" * 60)
        print("TUSKLANG SDK HEALTH CHECK REPORT")
        print("=" * 60)
        print(f"Overall Status: {report['overall_status'].upper()}")
        print(f"Timestamp: {time.strftime('%Y-%m-%d %H:%M:%S', time.localtime(report['timestamp']))}")
        print(f"Total Duration: {report['duration']:.2f}s")
        print()
        
        print("Status Summary:")
        for status, count in report['status_counts'].items():
            print(f"  {status.upper()}: {count}")
        print()
        
        print("Detailed Results:")
        for check in report['checks']:
            status_icon = {
                "healthy": "✅",
                "warning": "⚠️",
                "critical": "❌",
                "unknown": "❓"
            }.get(check['status'], "❓")
            
            print(f"{status_icon} {check['name']}: {check['message']}")
            print(f"    Duration: {check['duration']:.2f}s")
            if check['details']:
                print(f"    Details: {json.dumps(check['details'], indent=4)}")
            print()

def main():
    """Main entry point"""
    checker = SDKHealthChecker()
    
    # Run health checks
    print("Running SDK health checks...")
    results = checker.run_all_checks()
    
    # Generate and print report
    checker.print_report()
    
    # Determine exit code
    report = checker.generate_report()
    if report['overall_status'] == 'critical':
        sys.exit(2)
    elif report['overall_status'] == 'warning':
        sys.exit(1)
    else:
        sys.exit(0)

if __name__ == "__main__":
    main() 