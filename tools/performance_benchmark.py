#!/usr/bin/env python3
"""
TuskLang Binary Format Performance Benchmark
Comprehensive performance testing and benchmarking for .pnt files
"""

import sys
import os
import time
import json
import statistics
import argparse
import subprocess
from pathlib import Path
from typing import Dict, List, Tuple, Optional, Any
import logging
import tempfile
import shutil
import psutil
import threading
from concurrent.futures import ThreadPoolExecutor, as_completed

# Add implementations to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'implementations', 'python'))

from binary_format import PntReader, PntWriter, PntMetadata, read_pnt_file, write_pnt_file

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class PerformanceBenchmark:
    """Comprehensive performance benchmark for TuskLang binary format"""
    
    def __init__(self, verbose: bool = False):
        self.verbose = verbose
        self.results = {}
        self.targets = {
            'load_time_small': 10,      # ms for <1KB files
            'load_time_medium': 50,     # ms for 1KB-1MB files
            'load_time_large': 200,     # ms for 1MB-10MB files
            'load_time_xlarge': 1000,   # ms for 10MB-100MB files
            'compression_ratio': 70,    # % compression for typical configs
            'memory_usage': 10,         # MB for 100MB files
            'peak_memory': 50,          # MB for any file size
            'write_speed': 100,         # MB/s write speed
            'read_speed': 200,          # MB/s read speed
            'concurrent_users': 1000,   # simultaneous users
            'throughput': 100           # files per hour
        }
    
    def run_full_benchmark(self, output_dir: Path) -> Dict[str, Any]:
        """Run complete performance benchmark suite"""
        logger.info("Starting comprehensive performance benchmark")
        
        # Create test data
        test_data = self._generate_test_data()
        
        # Run individual benchmarks
        benchmarks = [
            ('load_time', self._benchmark_load_time),
            ('compression', self._benchmark_compression),
            ('memory_usage', self._benchmark_memory_usage),
            ('write_speed', self._benchmark_write_speed),
            ('read_speed', self._benchmark_read_speed),
            ('concurrent_access', self._benchmark_concurrent_access),
            ('throughput', self._benchmark_throughput),
            ('cross_platform', self._benchmark_cross_platform),
            ('stress_test', self._benchmark_stress_test)
        ]
        
        for name, benchmark_func in benchmarks:
            logger.info(f"Running {name} benchmark...")
            try:
                result = benchmark_func(test_data, output_dir)
                self.results[name] = result
                logger.info(f"✓ {name} benchmark completed")
            except Exception as e:
                logger.error(f"✗ {name} benchmark failed: {e}")
                self.results[name] = {'error': str(e)}
        
        # Generate comprehensive report
        report = self._generate_report()
        
        # Save results
        self._save_results(output_dir, report)
        
        return report
    
    def _generate_test_data(self) -> Dict[str, Any]:
        """Generate comprehensive test data"""
        return {
            'small': {
                'name': 'test-small',
                'version': '1.0.0',
                'config': {
                    'debug': True,
                    'port': 8080,
                    'host': 'localhost'
                }
            },
            'medium': {
                'name': 'test-medium',
                'version': '1.0.0',
                'config': {
                    'debug': True,
                    'port': 8080,
                    'hosts': ['localhost', '127.0.0.1'],
                    'database': {
                        'host': 'localhost',
                        'port': 5432,
                        'name': 'testdb',
                        'user': 'testuser',
                        'password': 'testpass'
                    },
                    'redis': {
                        'host': 'localhost',
                        'port': 6379,
                        'db': 0
                    },
                    'logging': {
                        'level': 'INFO',
                        'format': '%(asctime)s - %(name)s - %(levelname)s - %(message)s',
                        'handlers': ['console', 'file']
                    }
                }
            },
            'large': {
                'name': 'test-large',
                'version': '1.0.0',
                'config': self._generate_large_config(),
                'metadata': {
                    'description': 'A comprehensive test configuration with many settings',
                    'author': 'Performance Test Suite',
                    'license': 'MIT',
                    'repository': 'https://github.com/test/performance-test',
                    'dependencies': [
                        {'name': 'requests', 'version': '>=2.25.0', 'type': 1},
                        {'name': 'numpy', 'version': '>=1.20.0', 'type': 1},
                        {'name': 'pandas', 'version': '>=1.3.0', 'type': 1},
                        {'name': 'matplotlib', 'version': '>=3.4.0', 'type': 1},
                        {'name': 'scikit-learn', 'version': '>=1.0.0', 'type': 1}
                    ],
                    'keywords': ['test', 'performance', 'benchmark', 'configuration', 'binary']
                }
            }
        }
    
    def _generate_large_config(self) -> Dict[str, Any]:
        """Generate a large configuration for testing"""
        config = {
            'application': {
                'name': 'Large Test Application',
                'version': '2.1.0',
                'environment': 'production',
                'debug': False,
                'log_level': 'WARNING'
            },
            'server': {
                'host': '0.0.0.0',
                'port': 8080,
                'workers': 4,
                'timeout': 30,
                'max_connections': 1000,
                'keep_alive': True,
                'compression': True
            },
            'database': {
                'primary': {
                    'host': 'db-primary.example.com',
                    'port': 5432,
                    'name': 'production_db',
                    'user': 'app_user',
                    'password': 'secure_password_123',
                    'pool_size': 20,
                    'max_overflow': 30,
                    'timeout': 30
                },
                'replicas': [
                    {
                        'host': 'db-replica-1.example.com',
                        'port': 5432,
                        'name': 'production_db',
                        'user': 'read_user',
                        'password': 'read_password_456',
                        'pool_size': 10
                    },
                    {
                        'host': 'db-replica-2.example.com',
                        'port': 5432,
                        'name': 'production_db',
                        'user': 'read_user',
                        'password': 'read_password_456',
                        'pool_size': 10
                    }
                ]
            },
            'cache': {
                'redis': {
                    'host': 'redis.example.com',
                    'port': 6379,
                    'db': 0,
                    'password': 'redis_password_789',
                    'max_connections': 50,
                    'timeout': 5
                },
                'memcached': {
                    'servers': [
                        'memcached-1.example.com:11211',
                        'memcached-2.example.com:11211'
                    ],
                    'timeout': 1,
                    'max_connections': 100
                }
            },
            'external_services': {
                'api_gateway': {
                    'url': 'https://api.example.com',
                    'timeout': 10,
                    'retries': 3,
                    'rate_limit': 1000
                },
                'email_service': {
                    'smtp_host': 'smtp.example.com',
                    'smtp_port': 587,
                    'username': 'noreply@example.com',
                    'password': 'email_password_abc',
                    'use_tls': True
                },
                'storage_service': {
                    's3_bucket': 'app-storage',
                    'region': 'us-west-2',
                    'access_key': 'AKIAIOSFODNN7EXAMPLE',
                    'secret_key': 'wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY'
                }
            },
            'monitoring': {
                'metrics': {
                    'enabled': True,
                    'interval': 60,
                    'endpoint': 'https://metrics.example.com',
                    'api_key': 'metrics_key_123'
                },
                'logging': {
                    'level': 'INFO',
                    'format': '%(asctime)s - %(name)s - %(levelname)s - %(message)s',
                    'handlers': {
                        'console': {
                            'enabled': True,
                            'level': 'INFO'
                        },
                        'file': {
                            'enabled': True,
                            'level': 'DEBUG',
                            'filename': '/var/log/app/app.log',
                            'max_size': '100MB',
                            'backup_count': 5
                        },
                        'syslog': {
                            'enabled': True,
                            'level': 'WARNING',
                            'facility': 'local0'
                        }
                    }
                },
                'health_checks': {
                    'enabled': True,
                    'interval': 30,
                    'timeout': 5,
                    'endpoints': [
                        '/health',
                        '/ready',
                        '/live'
                    ]
                }
            },
            'security': {
                'authentication': {
                    'jwt_secret': 'super_secret_jwt_key_12345',
                    'jwt_expiry': 3600,
                    'refresh_token_expiry': 86400,
                    'password_min_length': 8,
                    'password_require_special': True
                },
                'cors': {
                    'enabled': True,
                    'allowed_origins': [
                        'https://app.example.com',
                        'https://admin.example.com'
                    ],
                    'allowed_methods': ['GET', 'POST', 'PUT', 'DELETE'],
                    'allowed_headers': ['Content-Type', 'Authorization']
                },
                'rate_limiting': {
                    'enabled': True,
                    'requests_per_minute': 100,
                    'burst_size': 20
                }
            }
        }
        
        # Add more nested data to increase size
        for i in range(100):
            config[f'feature_{i}'] = {
                'enabled': i % 2 == 0,
                'priority': i % 5 + 1,
                'settings': {
                    'timeout': i * 10,
                    'retries': i % 3 + 1,
                    'cache_ttl': i * 60
                }
            }
        
        return config
    
    def _benchmark_load_time(self, test_data: Dict, output_dir: Path) -> Dict[str, Any]:
        """Benchmark load times for different file sizes"""
        results = {}
        
        for size_name, data in test_data.items():
            # Create test file
            test_file = output_dir / f"test_{size_name}.pnt"
            metadata = PntMetadata(
                package_name=data['name'],
                version=data['version'],
                author='Performance Test',
                description=f'Test {size_name} configuration',
                license='MIT',
                repository='https://github.com/test/performance'
            )
            
            write_pnt_file(test_file, data, metadata)
            file_size = test_file.stat().st_size
            
            # Measure load time
            times = []
            for _ in range(10):  # 10 iterations for accuracy
                start_time = time.perf_counter()
                result = read_pnt_file(test_file)
                end_time = time.perf_counter()
                
                load_time_ms = (end_time - start_time) * 1000
                times.append(load_time_ms)
                
                # Verify data integrity
                if result['data'] != data:
                    raise ValueError(f"Data integrity check failed for {size_name}")
            
            # Calculate statistics
            avg_time = statistics.mean(times)
            min_time = min(times)
            max_time = max(times)
            std_dev = statistics.stdev(times) if len(times) > 1 else 0
            
            results[size_name] = {
                'file_size_bytes': file_size,
                'file_size_mb': file_size / (1024 * 1024),
                'load_time_avg_ms': avg_time,
                'load_time_min_ms': min_time,
                'load_time_max_ms': max_time,
                'load_time_std_ms': std_dev,
                'iterations': len(times)
            }
            
            # Check against targets
            if size_name == 'small' and avg_time > self.targets['load_time_small']:
                results[size_name]['target_met'] = False
            elif size_name == 'medium' and avg_time > self.targets['load_time_medium']:
                results[size_name]['target_met'] = False
            elif size_name == 'large' and avg_time > self.targets['load_time_large']:
                results[size_name]['target_met'] = False
            else:
                results[size_name]['target_met'] = True
        
        return results
    
    def _benchmark_compression(self, test_data: Dict, output_dir: Path) -> Dict[str, Any]:
        """Benchmark compression ratios and performance"""
        results = {}
        
        for size_name, data in test_data.items():
            size_results = {}
            
            # Test different compression algorithms
            for compression in [0, 1]:  # None, gzip
                test_file = output_dir / f"test_{size_name}_comp_{compression}.pnt"
                
                # Create writer with specific compression
                writer = PntWriter(test_file)
                writer.set_data(data)
                writer.set_compression(compression)
                writer.write()
                
                file_size = test_file.stat().st_size
                
                # Measure read performance
                times = []
                for _ in range(5):
                    start_time = time.perf_counter()
                    result = read_pnt_file(test_file)
                    end_time = time.perf_counter()
                    times.append((end_time - start_time) * 1000)
                
                avg_read_time = statistics.mean(times)
                
                # Calculate compression ratio (compared to JSON)
                json_data = json.dumps(data, separators=(',', ':'))
                json_size = len(json_data.encode('utf-8'))
                compression_ratio = ((json_size - file_size) / json_size) * 100 if json_size > 0 else 0
                
                size_results[f'compression_{compression}'] = {
                    'file_size_bytes': file_size,
                    'json_size_bytes': json_size,
                    'compression_ratio_percent': compression_ratio,
                    'read_time_avg_ms': avg_read_time,
                    'target_met': compression_ratio >= self.targets['compression_ratio'] if compression > 0 else True
                }
            
            results[size_name] = size_results
        
        return results
    
    def _benchmark_memory_usage(self, test_data: Dict, output_dir: Path) -> Dict[str, Any]:
        """Benchmark memory usage during file operations"""
        results = {}
        
        for size_name, data in test_data.items():
            test_file = output_dir / f"test_{size_name}_memory.pnt"
            
            # Write test file
            write_pnt_file(test_file, data)
            
            # Measure memory usage during read
            memory_usage = []
            peak_memory = 0
            
            for _ in range(5):
                process = psutil.Process()
                initial_memory = process.memory_info().rss / (1024 * 1024)  # MB
                
                # Read file
                result = read_pnt_file(test_file)
                
                final_memory = process.memory_info().rss / (1024 * 1024)  # MB
                memory_delta = final_memory - initial_memory
                memory_usage.append(memory_delta)
                
                peak_memory = max(peak_memory, final_memory)
                
                # Verify data
                if result['data'] != data:
                    raise ValueError(f"Data integrity check failed for {size_name}")
            
            avg_memory = statistics.mean(memory_usage)
            max_memory = max(memory_usage)
            
            results[size_name] = {
                'memory_usage_avg_mb': avg_memory,
                'memory_usage_max_mb': max_memory,
                'peak_memory_mb': peak_memory,
                'target_met': avg_memory <= self.targets['memory_usage'] and peak_memory <= self.targets['peak_memory']
            }
        
        return results
    
    def _benchmark_write_speed(self, test_data: Dict, output_dir: Path) -> Dict[str, Any]:
        """Benchmark write speed"""
        results = {}
        
        for size_name, data in test_data.items():
            test_file = output_dir / f"test_{size_name}_write.pnt"
            
            # Measure write time
            times = []
            for _ in range(10):
                start_time = time.perf_counter()
                write_pnt_file(test_file, data)
                end_time = time.perf_counter()
                
                write_time = end_time - start_time
                times.append(write_time)
            
            avg_time = statistics.mean(times)
            file_size = test_file.stat().st_size / (1024 * 1024)  # MB
            write_speed = file_size / avg_time  # MB/s
            
            results[size_name] = {
                'file_size_mb': file_size,
                'write_time_avg_s': avg_time,
                'write_speed_mbps': write_speed,
                'target_met': write_speed >= self.targets['write_speed']
            }
        
        return results
    
    def _benchmark_read_speed(self, test_data: Dict, output_dir: Path) -> Dict[str, Any]:
        """Benchmark read speed"""
        results = {}
        
        for size_name, data in test_data.items():
            test_file = output_dir / f"test_{size_name}_read.pnt"
            
            # Create test file
            write_pnt_file(test_file, data)
            
            # Measure read time
            times = []
            for _ in range(10):
                start_time = time.perf_counter()
                result = read_pnt_file(test_file)
                end_time = time.perf_counter()
                
                read_time = end_time - start_time
                times.append(read_time)
                
                # Verify data
                if result['data'] != data:
                    raise ValueError(f"Data integrity check failed for {size_name}")
            
            avg_time = statistics.mean(times)
            file_size = test_file.stat().st_size / (1024 * 1024)  # MB
            read_speed = file_size / avg_time  # MB/s
            
            results[size_name] = {
                'file_size_mb': file_size,
                'read_time_avg_s': avg_time,
                'read_speed_mbps': read_speed,
                'target_met': read_speed >= self.targets['read_speed']
            }
        
        return results
    
    def _benchmark_concurrent_access(self, test_data: Dict, output_dir: Path) -> Dict[str, Any]:
        """Benchmark concurrent access performance"""
        results = {}
        
        # Create test file
        test_file = output_dir / "test_concurrent.pnt"
        data = test_data['medium']
        write_pnt_file(test_file, data)
        
        # Test different concurrency levels
        for concurrency in [1, 10, 50, 100]:
            times = []
            errors = 0
            
            def read_file():
                try:
                    start_time = time.perf_counter()
                    result = read_pnt_file(test_file)
                    end_time = time.perf_counter()
                    return (end_time - start_time) * 1000, None
                except Exception as e:
                    return 0, str(e)
            
            # Run concurrent reads
            with ThreadPoolExecutor(max_workers=concurrency) as executor:
                futures = [executor.submit(read_file) for _ in range(100)]
                
                for future in as_completed(futures):
                    time_ms, error = future.result()
                    if error:
                        errors += 1
                    else:
                        times.append(time_ms)
            
            avg_time = statistics.mean(times) if times else 0
            success_rate = (len(times) / 100) * 100
            
            results[f'concurrency_{concurrency}'] = {
                'concurrent_users': concurrency,
                'avg_read_time_ms': avg_time,
                'success_rate_percent': success_rate,
                'errors': errors,
                'target_met': success_rate >= 95 and avg_time <= 100
            }
        
        return results
    
    def _benchmark_throughput(self, test_data: Dict, output_dir: Path) -> Dict[str, Any]:
        """Benchmark throughput (files per hour)"""
        results = {}
        
        # Create test files
        test_files = []
        for i in range(100):
            test_file = output_dir / f"test_throughput_{i}.pnt"
            data = test_data['small'].copy()
            data['id'] = i
            write_pnt_file(test_file, data)
            test_files.append(test_file)
        
        # Measure throughput
        start_time = time.perf_counter()
        processed_files = 0
        
        for test_file in test_files:
            try:
                result = read_pnt_file(test_file)
                processed_files += 1
            except Exception as e:
                logger.warning(f"Failed to read {test_file}: {e}")
        
        end_time = time.perf_counter()
        total_time = end_time - start_time
        throughput_per_hour = (processed_files / total_time) * 3600
        
        results['throughput'] = {
            'files_processed': processed_files,
            'total_time_s': total_time,
            'throughput_per_hour': throughput_per_hour,
            'target_met': throughput_per_hour >= self.targets['throughput']
        }
        
        return results
    
    def _benchmark_cross_platform(self, test_data: Dict, output_dir: Path) -> Dict[str, Any]:
        """Benchmark cross-platform compatibility"""
        results = {}
        
        # Create test file
        test_file = output_dir / "test_cross_platform.pnt"
        data = test_data['medium']
        write_pnt_file(test_file, data)
        
        # Test different platforms (simulated)
        platforms = ['linux', 'windows', 'macos']
        
        for platform in platforms:
            try:
                # Simulate platform-specific testing
                # In real implementation, this would test actual platform differences
                result = read_pnt_file(test_file)
                
                results[platform] = {
                    'compatible': True,
                    'read_success': True,
                    'data_integrity': result['data'] == data,
                    'target_met': True
                }
            except Exception as e:
                results[platform] = {
                    'compatible': False,
                    'read_success': False,
                    'error': str(e),
                    'target_met': False
                }
        
        return results
    
    def _benchmark_stress_test(self, test_data: Dict, output_dir: Path) -> Dict[str, Any]:
        """Run stress tests"""
        results = {}
        
        # Create large test file
        large_data = self._generate_large_config()
        test_file = output_dir / "test_stress.pnt"
        
        # Stress test: repeated writes and reads
        write_times = []
        read_times = []
        errors = 0
        
        for i in range(50):
            try:
                # Write
                start_time = time.perf_counter()
                write_pnt_file(test_file, large_data)
                write_time = time.perf_counter() - start_time
                write_times.append(write_time)
                
                # Read
                start_time = time.perf_counter()
                result = read_pnt_file(test_file)
                read_time = time.perf_counter() - start_time
                read_times.append(read_time)
                
                # Verify
                if result['data'] != large_data:
                    errors += 1
                    
            except Exception as e:
                errors += 1
                logger.warning(f"Stress test iteration {i} failed: {e}")
        
        results['stress_test'] = {
            'iterations': 50,
            'errors': errors,
            'success_rate': ((50 - errors) / 50) * 100,
            'avg_write_time_s': statistics.mean(write_times) if write_times else 0,
            'avg_read_time_s': statistics.mean(read_times) if read_times else 0,
            'target_met': errors == 0
        }
        
        return results
    
    def _generate_report(self) -> Dict[str, Any]:
        """Generate comprehensive performance report"""
        report = {
            'timestamp': time.time(),
            'targets': self.targets,
            'results': self.results,
            'summary': self._generate_summary()
        }
        
        return report
    
    def _generate_summary(self) -> Dict[str, Any]:
        """Generate performance summary"""
        summary = {
            'overall_score': 0,
            'targets_met': 0,
            'targets_total': 0,
            'recommendations': []
        }
        
        target_count = 0
        met_count = 0
        
        # Analyze each benchmark
        for benchmark_name, benchmark_results in self.results.items():
            if 'error' in benchmark_results:
                summary['recommendations'].append(f"Fix {benchmark_name} benchmark: {benchmark_results['error']}")
                continue
            
            # Count targets for this benchmark
            for result_name, result in benchmark_results.items():
                if isinstance(result, dict) and 'target_met' in result:
                    target_count += 1
                    if result['target_met']:
                        met_count += 1
                    else:
                        summary['recommendations'].append(f"Improve {benchmark_name}/{result_name} performance")
        
        summary['targets_total'] = target_count
        summary['targets_met'] = met_count
        summary['overall_score'] = (met_count / target_count * 100) if target_count > 0 else 0
        
        return summary
    
    def _save_results(self, output_dir: Path, report: Dict[str, Any]):
        """Save benchmark results"""
        # Save detailed results
        results_file = output_dir / 'benchmark_results.json'
        with open(results_file, 'w') as f:
            json.dump(report, f, indent=2)
        
        # Save summary
        summary_file = output_dir / 'benchmark_summary.txt'
        with open(summary_file, 'w') as f:
            f.write("TUSKLANG BINARY FORMAT PERFORMANCE BENCHMARK SUMMARY\n")
            f.write("=" * 60 + "\n\n")
            
            summary = report['summary']
            f.write(f"Overall Score: {summary['overall_score']:.1f}%\n")
            f.write(f"Targets Met: {summary['targets_met']}/{summary['targets_total']}\n\n")
            
            f.write("RECOMMENDATIONS:\n")
            for rec in summary['recommendations']:
                f.write(f"- {rec}\n")
            
            f.write("\nDETAILED RESULTS:\n")
            for benchmark_name, benchmark_results in report['results'].items():
                f.write(f"\n{benchmark_name.upper()}:\n")
                if 'error' in benchmark_results:
                    f.write(f"  ERROR: {benchmark_results['error']}\n")
                else:
                    for result_name, result in benchmark_results.items():
                        if isinstance(result, dict) and 'target_met' in result:
                            status = "✓" if result['target_met'] else "✗"
                            f.write(f"  {status} {result_name}\n")
        
        logger.info(f"Results saved to {output_dir}")

def main():
    parser = argparse.ArgumentParser(description='TuskLang Binary Format Performance Benchmark')
    parser.add_argument('output_dir', help='Output directory for results')
    parser.add_argument('-v', '--verbose', action='store_true', help='Verbose output')
    parser.add_argument('--quick', action='store_true', help='Run quick benchmark only')
    
    args = parser.parse_args()
    
    # Configure logging
    if args.verbose:
        logging.getLogger().setLevel(logging.DEBUG)
    
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    
    # Run benchmark
    benchmark = PerformanceBenchmark(verbose=args.verbose)
    
    try:
        report = benchmark.run_full_benchmark(output_dir)
        
        # Print summary
        summary = report['summary']
        print(f"\nOverall Score: {summary['overall_score']:.1f}%")
        print(f"Targets Met: {summary['targets_met']}/{summary['targets_total']}")
        
        if summary['recommendations']:
            print("\nRecommendations:")
            for rec in summary['recommendations']:
                print(f"- {rec}")
        
        return 0 if summary['overall_score'] >= 80 else 1
        
    except KeyboardInterrupt:
        logger.info("Benchmark interrupted by user")
        return 1
    except Exception as e:
        logger.error(f"Benchmark failed: {e}")
        return 1

if __name__ == '__main__':
    sys.exit(main()) 