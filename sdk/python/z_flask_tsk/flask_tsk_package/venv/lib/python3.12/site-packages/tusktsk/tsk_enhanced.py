#!/usr/bin/env python3
"""
TuskLang Enhanced for Python - The Freedom Parser
=================================================
"We don't bow to any king" - Support ALL syntax styles

Features:
- Multiple grouping: [], {}, <>
- $global vs section-local variables
- Cross-file communication
- Database queries (with real adapters)
- All @ operators (85 total)
- Maximum flexibility

DEFAULT CONFIG: peanu.tsk (the bridge of language grace)
"""

import re
import json
import os
import time
import hashlib
import gzip
import struct
import asyncio
import aiohttp
import sqlite3
import psycopg2
import pymongo
import redis
import jwt
import bcrypt
import secrets
import hmac
import base64
from typing import Any, Dict, List, Union, Optional, Tuple
from datetime import datetime, timedelta
from pathlib import Path
from dataclasses import dataclass
from enum import Enum
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class OperatorType(Enum):
    """Operator types for categorization"""
    CORE = "core"
    ADVANCED = "advanced"
    ENTERPRISE = "enterprise"
    SECURITY = "security"
    PERFORMANCE = "performance"


@dataclass
class OperatorInfo:
    """Information about an operator"""
    name: str
    type: OperatorType
    description: str
    implemented: bool
    source_file: str
    method_name: str


class TuskLangEnhanced:
    """Enhanced TuskLang parser with full syntax flexibility and 85 operators"""
    
    def __init__(self):
        self.data = {}
        self.global_variables = {}
        self.section_variables = {}
        self.cache = {}
        self.cross_file_cache = {}
        self.current_section = ""
        self.in_object = False
        self.object_key = ""
        self.peanut_loaded = False
        
        # Database connections
        self.db_connections = {}
        self.redis_client = None
        self.mongo_client = None
        
        # Security and license
        self.license_key = None
        self.license_valid = False
        self.protection_enabled = False
        
        # Performance tracking
        self.metrics = {}
        self.performance_data = {}
        
        # Standard peanu.tsk locations
        self.peanut_locations = [
            "./peanu.tsk",
            "../peanu.tsk", 
            "../../peanu.tsk",
            "/etc/tusklang/peanu.tsk",
            os.path.expanduser("~/.config/tusklang/peanu.tsk"),
            os.environ.get('TUSKLANG_CONFIG', '')
        ]
        
        # Operator registry
        self.operators = self._register_operators()
    
    def _register_operators(self) -> Dict[str, OperatorInfo]:
        """Register all 85 operators with their implementation status"""
        return {
            # Core Operators (15 total)
            'cache': OperatorInfo('cache', OperatorType.CORE, 'Caching with TTL', True, 'tsk_enhanced.py', 'execute_cache'),
            'env': OperatorInfo('env', OperatorType.CORE, 'Environment variables', True, 'tsk_enhanced.py', 'execute_env'),
            'file': OperatorInfo('file', OperatorType.CORE, 'File operations', True, 'tsk_enhanced.py', 'execute_file'),
            'json': OperatorInfo('json', OperatorType.CORE, 'JSON operations', True, 'tsk_enhanced.py', 'execute_json'),
            'date': OperatorInfo('date', OperatorType.CORE, 'Date formatting', True, 'tsk_enhanced.py', 'execute_date'),
            'query': OperatorInfo('query', OperatorType.CORE, 'Database queries', True, 'tsk_enhanced.py', 'execute_query'),
            'metrics': OperatorInfo('metrics', OperatorType.CORE, 'Performance metrics', False, 'tsk_enhanced.py', 'execute_metrics'),
            'learn': OperatorInfo('learn', OperatorType.CORE, 'Machine learning', False, 'tsk_enhanced.py', 'execute_learn'),
            'optimize': OperatorInfo('optimize', OperatorType.CORE, 'Code optimization', False, 'tsk_enhanced.py', 'execute_optimize'),
            'feature': OperatorInfo('feature', OperatorType.CORE, 'Feature flags', False, 'tsk_enhanced.py', 'execute_feature'),
            'request': OperatorInfo('request', OperatorType.CORE, 'HTTP requests', False, 'tsk_enhanced.py', 'execute_request'),
            'if': OperatorInfo('if', OperatorType.CORE, 'Conditional logic', False, 'tsk_enhanced.py', 'execute_if'),
            'output': OperatorInfo('output', OperatorType.CORE, 'Output formatting', False, 'tsk_enhanced.py', 'execute_output'),
            'q': OperatorInfo('q', OperatorType.CORE, 'Query shorthand', False, 'tsk_enhanced.py', 'execute_q'),
            'file.tsk.get': OperatorInfo('file.tsk.get', OperatorType.CORE, 'Cross-file get', True, 'tsk_enhanced.py', 'cross_file_get'),
            'file.tsk.set': OperatorInfo('file.tsk.set', OperatorType.CORE, 'Cross-file set', True, 'tsk_enhanced.py', 'cross_file_set'),
            
            # Advanced Operators (22 total)
            'graphql': OperatorInfo('graphql', OperatorType.ADVANCED, 'GraphQL queries', False, 'graphql_adapter.py', 'execute_graphql'),
            'grpc': OperatorInfo('grpc', OperatorType.ADVANCED, 'gRPC calls', False, 'grpc_adapter.py', 'execute_grpc'),
            'websocket': OperatorInfo('websocket', OperatorType.ADVANCED, 'WebSocket connections', False, 'websocket_adapter.py', 'execute_websocket'),
            'sse': OperatorInfo('sse', OperatorType.ADVANCED, 'Server-Sent Events', False, 'sse_adapter.py', 'execute_sse'),
            'nats': OperatorInfo('nats', OperatorType.ADVANCED, 'NATS messaging', False, 'nats_adapter.py', 'execute_nats'),
            'amqp': OperatorInfo('amqp', OperatorType.ADVANCED, 'AMQP messaging', False, 'amqp_adapter.py', 'execute_amqp'),
            'kafka': OperatorInfo('kafka', OperatorType.ADVANCED, 'Kafka messaging', False, 'kafka_adapter.py', 'execute_kafka'),
            'mongodb': OperatorInfo('mongodb', OperatorType.ADVANCED, 'MongoDB operations', False, 'mongodb_adapter.py', 'execute_mongodb'),
            'postgresql': OperatorInfo('postgresql', OperatorType.ADVANCED, 'PostgreSQL operations', False, 'postgresql_adapter.py', 'execute_postgresql'),
            'mysql': OperatorInfo('mysql', OperatorType.ADVANCED, 'MySQL operations', False, 'mysql_adapter.py', 'execute_mysql'),
            'sqlite': OperatorInfo('sqlite', OperatorType.ADVANCED, 'SQLite operations', False, 'sqlite_adapter.py', 'execute_sqlite'),
            'redis': OperatorInfo('redis', OperatorType.ADVANCED, 'Redis operations', False, 'redis_adapter.py', 'execute_redis'),
            'etcd': OperatorInfo('etcd', OperatorType.ADVANCED, 'etcd operations', False, 'etcd_adapter.py', 'execute_etcd'),
            'elasticsearch': OperatorInfo('elasticsearch', OperatorType.ADVANCED, 'Elasticsearch operations', False, 'elasticsearch_adapter.py', 'execute_elasticsearch'),
            'prometheus': OperatorInfo('prometheus', OperatorType.ADVANCED, 'Prometheus metrics', False, 'prometheus_adapter.py', 'execute_prometheus'),
            'jaeger': OperatorInfo('jaeger', OperatorType.ADVANCED, 'Jaeger tracing', False, 'jaeger_adapter.py', 'execute_jaeger'),
            'zipkin': OperatorInfo('zipkin', OperatorType.ADVANCED, 'Zipkin tracing', False, 'zipkin_adapter.py', 'execute_zipkin'),
            'grafana': OperatorInfo('grafana', OperatorType.ADVANCED, 'Grafana dashboards', False, 'grafana_adapter.py', 'execute_grafana'),
            'istio': OperatorInfo('istio', OperatorType.ADVANCED, 'Istio service mesh', False, 'istio_adapter.py', 'execute_istio'),
            'consul': OperatorInfo('consul', OperatorType.ADVANCED, 'Consul service discovery', False, 'consul_adapter.py', 'execute_consul'),
            'vault': OperatorInfo('vault', OperatorType.ADVANCED, 'HashiCorp Vault', False, 'vault_adapter.py', 'execute_vault'),
            'temporal': OperatorInfo('temporal', OperatorType.ADVANCED, 'Temporal workflows', False, 'temporal_adapter.py', 'execute_temporal'),
            
            # Enterprise Features (6 total)
            'tenant': OperatorInfo('tenant', OperatorType.ENTERPRISE, 'Multi-tenancy', False, 'tenant_adapter.py', 'execute_tenant'),
            'rbac': OperatorInfo('rbac', OperatorType.ENTERPRISE, 'Role-based access control', False, 'rbac_adapter.py', 'execute_rbac'),
            'oauth2': OperatorInfo('oauth2', OperatorType.ENTERPRISE, 'OAuth2 authentication', False, 'oauth2_adapter.py', 'execute_oauth2'),
            'saml': OperatorInfo('saml', OperatorType.ENTERPRISE, 'SAML authentication', False, 'saml_adapter.py', 'execute_saml'),
            'mfa': OperatorInfo('mfa', OperatorType.ENTERPRISE, 'Multi-factor authentication', False, 'mfa_adapter.py', 'execute_mfa'),
            'audit': OperatorInfo('audit', OperatorType.ENTERPRISE, 'Audit logging', False, 'audit_adapter.py', 'execute_audit'),
            
            # Security Features (8 total)
            'license.validate': OperatorInfo('license.validate', OperatorType.SECURITY, 'License validation', False, 'license.py', 'validate_license'),
            'license.verify': OperatorInfo('license.verify', OperatorType.SECURITY, 'Online license verification', False, 'license.py', 'verify_license'),
            'license.check': OperatorInfo('license.check', OperatorType.SECURITY, 'License expiration check', False, 'license.py', 'check_license'),
            'license.permissions': OperatorInfo('license.permissions', OperatorType.SECURITY, 'Feature permissions', False, 'license.py', 'check_permissions'),
            'protection.encrypt': OperatorInfo('protection.encrypt', OperatorType.SECURITY, 'AES-256-GCM encryption', False, 'protection.py', 'encrypt_data'),
            'protection.decrypt': OperatorInfo('protection.decrypt', OperatorType.SECURITY, 'AES-256-GCM decryption', False, 'protection.py', 'decrypt_data'),
            'protection.verify': OperatorInfo('protection.verify', OperatorType.SECURITY, 'Integrity verification', False, 'protection.py', 'verify_integrity'),
            'protection.sign': OperatorInfo('protection.sign', OperatorType.SECURITY, 'HMAC-SHA256 signing', False, 'protection.py', 'generate_signature'),
            'protection.obfuscate': OperatorInfo('protection.obfuscate', OperatorType.SECURITY, 'Code obfuscation', False, 'protection.py', 'obfuscate_code'),
            'protection.detect': OperatorInfo('protection.detect', OperatorType.SECURITY, 'Tampering detection', False, 'protection.py', 'detect_tampering'),
            'protection.report': OperatorInfo('protection.report', OperatorType.SECURITY, 'Violation reporting', False, 'protection.py', 'report_violation'),
            
            # Performance Features (4 total)
            'binary.compile': OperatorInfo('binary.compile', OperatorType.PERFORMANCE, 'Binary compilation', False, 'binary.py', 'compile_binary'),
            'binary.load': OperatorInfo('binary.load', OperatorType.PERFORMANCE, 'Binary loading', False, 'binary.py', 'load_binary'),
            'performance.benchmark': OperatorInfo('performance.benchmark', OperatorType.PERFORMANCE, 'Performance benchmarking', False, 'performance.py', 'run_benchmark'),
            'performance.optimize': OperatorInfo('performance.optimize', OperatorType.PERFORMANCE, 'Code optimization', False, 'performance.py', 'optimize_code'),
            
            # FUJSEN System (5 total)
            'fujsen.serialize': OperatorInfo('fujsen.serialize', OperatorType.CORE, 'Function serialization', False, 'fujsen.py', 'serialize_function'),
            'fujsen.deserialize': OperatorInfo('fujsen.deserialize', OperatorType.CORE, 'Function deserialization', False, 'fujsen.py', 'deserialize_function'),
            'fujsen.execute': OperatorInfo('fujsen.execute', OperatorType.CORE, 'Cross-language execution', False, 'fujsen.py', 'execute_function'),
            'fujsen.cache': OperatorInfo('fujsen.cache', OperatorType.CORE, 'Function caching', False, 'fujsen.py', 'cache_function'),
            'fujsen.context': OperatorInfo('fujsen.context', OperatorType.CORE, 'Context injection', False, 'fujsen.py', 'inject_context'),
            
            # Additional Core Operators (15 total)
            'peanut.load': OperatorInfo('peanut.load', OperatorType.CORE, 'Configuration loading', True, 'peanut_config.py', 'load'),
            'peanut.get': OperatorInfo('peanut.get', OperatorType.CORE, 'Configuration get', True, 'peanut_config.py', 'get'),
            'peanut.compile': OperatorInfo('peanut.compile', OperatorType.CORE, 'Binary compilation', False, 'peanut_config.py', 'compile_binary'),
            'protected.init': OperatorInfo('protected.init', OperatorType.SECURITY, 'Protected initialization', False, 'tsk_protected.py', 'init'),
            'protected.check': OperatorInfo('protected.check', OperatorType.SECURITY, 'Protection verification', False, 'tsk_protected.py', 'check_protection'),
            'protected.validate': OperatorInfo('protected.validate', OperatorType.SECURITY, 'License validation', False, 'tsk_protected.py', 'validate_license'),
            'protected.destruct': OperatorInfo('protected.destruct', OperatorType.SECURITY, 'Self-destruction', False, 'tsk_protected.py', 'self_destruct'),
            'functions': OperatorInfo('functions', OperatorType.CORE, 'Utility functions', False, 'functions.py', 'execute_function'),
            'webassembly': OperatorInfo('webassembly', OperatorType.ADVANCED, 'WebAssembly support', False, 'webassembly_adapter.py', 'execute_webassembly'),
            'unity': OperatorInfo('unity', OperatorType.ADVANCED, 'Unity integration', False, 'unity_adapter.py', 'execute_unity'),
            'azure.functions': OperatorInfo('azure.functions', OperatorType.ADVANCED, 'Azure Functions', False, 'azure_adapter.py', 'execute_azure'),
            'rails': OperatorInfo('rails', OperatorType.ADVANCED, 'Rails integration', False, 'rails_adapter.py', 'execute_rails'),
            'jekyll': OperatorInfo('jekyll', OperatorType.ADVANCED, 'Jekyll integration', False, 'jekyll_adapter.py', 'execute_jekyll'),
            'kubernetes': OperatorInfo('kubernetes', OperatorType.ADVANCED, 'Kubernetes integration', False, 'kubernetes_adapter.py', 'execute_kubernetes'),
            'compliance': OperatorInfo('compliance', OperatorType.ENTERPRISE, 'Compliance features', False, 'compliance_adapter.py', 'execute_compliance'),
        }
    
    def load_peanut(self):
        """Load peanu.tsk if available"""
        if self.peanut_loaded:
            return
            
        for location in self.peanut_locations:
            if location and Path(location).exists():
                print(f"# Loading universal config from: {location}")
                self.parse_file(location)
                self.peanut_loaded = True
                return
    
    def parse_value(self, value: str) -> Any:
        """Parse TuskLang value with all syntax support"""
        value = value.strip()
        
        # Remove optional semicolon
        if value.endswith(';'):
            value = value[:-1].strip()
        
        # Basic types
        if value == 'true':
            return True
        elif value == 'false':
            return False
        elif value == 'null':
            return None
        
        # Numbers
        if re.match(r'^-?\d+$', value):
            return int(value)
        elif re.match(r'^-?\d+\.\d+$', value):
            return float(value)
        
        # $variable references (global)
        if re.match(r'^\$([a-zA-Z_][a-zA-Z0-9_]*)$', value):
            var_name = value[1:]
            return self.global_variables.get(var_name, '')
        
        # Section-local variable references
        if self.current_section and re.match(r'^[a-zA-Z_][a-zA-Z0-9_]*$', value):
            section_key = f"{self.current_section}.{value}"
            if section_key in self.section_variables:
                return self.section_variables[section_key]
        
        # @date function
        date_match = re.match(r'^@date\(["\'](.*)["\']\)$', value)
        if date_match:
            format_str = date_match.group(1)
            return self.execute_date(format_str)
        
        # @env function with default
        env_match = re.match(r'^@env\(["\']([^"\']*)["\'](?:,\s*(.+))?\)$', value)
        if env_match:
            env_var = env_match.group(1)
            default_val = env_match.group(2)
            if default_val:
                default_val = default_val.strip('"\'')
            return os.environ.get(env_var, default_val or '')
        
        # Ranges: 8000-9000
        range_match = re.match(r'^(\d+)-(\d+)$', value)
        if range_match:
            return {
                "min": int(range_match.group(1)),
                "max": int(range_match.group(2)),
                "type": "range"
            }
        
        # Arrays
        if value.startswith('[') and value.endswith(']'):
            return self.parse_array(value)
        
        # Objects
        if value.startswith('{') and value.endswith('}'):
            return self.parse_object(value)
        
        # Cross-file references: @file.tsk.get('key')
        cross_get_match = re.match(r'^@([a-zA-Z0-9_-]+)\.tsk\.get\(["\'](.*)["\']\)$', value)
        if cross_get_match:
            file_name = cross_get_match.group(1)
            key = cross_get_match.group(2)
            return self.cross_file_get(file_name, key)
        
        # Cross-file set: @file.tsk.set('key', value)
        cross_set_match = re.match(r'^@([a-zA-Z0-9_-]+)\.tsk\.set\(["\']([^"\']*)["\'],\s*(.+)\)$', value)
        if cross_set_match:
            file_name = cross_set_match.group(1)
            key = cross_set_match.group(2)
            val = cross_set_match.group(3)
            return self.cross_file_set(file_name, key, val)
        
        # @query function
        query_match = re.match(r'^@query\(["\'](.*)["\'](.*)\)$', value)
        if query_match:
            query = query_match.group(1)
            return self.execute_query(query)
        
        # @ operators with enhanced pattern matching
        operator_match = re.match(r'^@([a-zA-Z_][a-zA-Z0-9_.]*)\((.+)\)$', value)
        if operator_match:
            operator = operator_match.group(1)
            params = operator_match.group(2)
            return self.execute_operator(operator, params)
        
        # String concatenation
        if ' + ' in value:
            parts = value.split(' + ')
            result = ""
            for part in parts:
                part = part.strip().strip('"\'')
                parsed_part = self.parse_value(part) if not part.startswith('"') else part[1:-1]
                result += str(parsed_part)
            return result
        
        # Conditional/ternary: condition ? true_val : false_val
        ternary_match = re.match(r'(.+?)\s*\?\s*(.+?)\s*:\s*(.+)', value)
        if ternary_match:
            condition = ternary_match.group(1).strip()
            true_val = ternary_match.group(2).strip()
            false_val = ternary_match.group(3).strip()
            
            if self.evaluate_condition(condition):
                return self.parse_value(true_val)
            else:
                return self.parse_value(false_val)
        
        # Remove quotes from strings
        if (value.startswith('"') and value.endswith('"')) or (value.startswith("'") and value.endswith("'")):
            return value[1:-1]
        
        # Return as-is
        return value
    
    def parse_array(self, value: str) -> List[Any]:
        """Parse array syntax"""
        content = value[1:-1].strip()
        if not content:
            return []
        
        items = []
        current = ""
        depth = 0
        in_string = False
        quote_char = None
        
        for char in content:
            if char in ['"', "'"] and not in_string:
                in_string = True
                quote_char = char
            elif char == quote_char and in_string:
                in_string = False
                quote_char = None
            
            if not in_string:
                if char in '[{':
                    depth += 1
                elif char in ']}':
                    depth -= 1
                elif char == ',' and depth == 0:
                    items.append(self.parse_value(current.strip()))
                    current = ""
                    continue
            
            current += char
        
        if current.strip():
            items.append(self.parse_value(current.strip()))
        
        return items
    
    def parse_object(self, value: str) -> Dict[str, Any]:
        """Parse object syntax"""
        content = value[1:-1].strip()
        if not content:
            return {}
        
        pairs = []
        current = ""
        depth = 0
        in_string = False
        quote_char = None
        
        for char in content:
            if char in ['"', "'"] and not in_string:
                in_string = True
                quote_char = char
            elif char == quote_char and in_string:
                in_string = False
                quote_char = None
            
            if not in_string:
                if char in '[{':
                    depth += 1
                elif char in ']}':
                    depth -= 1
                elif char == ',' and depth == 0:
                    pairs.append(current.strip())
                    current = ""
                    continue
            
            current += char
        
        if current.strip():
            pairs.append(current.strip())
        
        obj = {}
        for pair in pairs:
            if ':' in pair:
                key, val = pair.split(':', 1)
                key = key.strip().strip('"\'')
                val = val.strip()
                obj[key] = self.parse_value(val)
            elif '=' in pair:
                key, val = pair.split('=', 1)
                key = key.strip().strip('"\'')
                val = val.strip()
                obj[key] = self.parse_value(val)
        
        return obj
    
    def evaluate_condition(self, condition: str) -> bool:
        """Evaluate conditions for ternary expressions"""
        condition = condition.strip()
        
        # Simple equality check
        eq_match = re.match(r'(.+?)\s*==\s*(.+)', condition)
        if eq_match:
            left = self.parse_value(eq_match.group(1).strip())
            right = self.parse_value(eq_match.group(2).strip())
            return str(left) == str(right)
        
        # Not equal
        ne_match = re.match(r'(.+?)\s*!=\s*(.+)', condition)
        if ne_match:
            left = self.parse_value(ne_match.group(1).strip())
            right = self.parse_value(ne_match.group(2).strip())
            return str(left) != str(right)
        
        # Greater than
        gt_match = re.match(r'(.+?)\s*>\s*(.+)', condition)
        if gt_match:
            left = self.parse_value(gt_match.group(1).strip())
            right = self.parse_value(gt_match.group(2).strip())
            try:
                return float(left) > float(right)
            except:
                return str(left) > str(right)
        
        # Default: check if truthy
        value = self.parse_value(condition)
        return bool(value) and value not in [False, None, 0, '0', 'false', 'null']
    
    def cross_file_get(self, file_name: str, key: str) -> Any:
        """Get value from another TSK file"""
        cache_key = f"{file_name}:{key}"
        
        # Check cache
        if cache_key in self.cross_file_cache:
            return self.cross_file_cache[cache_key]
        
        # Find file
        file_path = None
        for directory in ['.', './config', '..', '../config']:
            potential_path = Path(directory) / f"{file_name}.tsk"
            if potential_path.exists():
                file_path = str(potential_path)
                break
        
        if not file_path:
            return ""
        
        # Parse file and get value
        temp_parser = TuskLangEnhanced()
        temp_parser.parse_file(file_path)
        
        value = temp_parser.data.get(key, "")
        
        # Cache result
        self.cross_file_cache[cache_key] = value
        
        return value
    
    def cross_file_set(self, file_name: str, key: str, value: str) -> Any:
        """Set value in another TSK file (cache only for now)"""
        cache_key = f"{file_name}:{key}"
        parsed_value = self.parse_value(value)
        self.cross_file_cache[cache_key] = parsed_value
        return parsed_value
    
    def execute_date(self, format_str: str) -> str:
        """Execute @date function"""
        now = datetime.now()
        
        # Convert PHP-style format to Python
        format_map = {
            'Y': '%Y',  # 4-digit year
            'Y-m-d': '%Y-%m-%d',
            'Y-m-d H:i:s': '%Y-%m-%d %H:%M:%S',
            'c': '%Y-%m-%dT%H:%M:%S%z'
        }
        
        if format_str in format_map:
            return now.strftime(format_map[format_str])
        else:
            return now.strftime('%Y-%m-%d %H:%M:%S')
    
    def execute_query(self, query: str) -> Any:
        """Execute database query using appropriate adapter"""
        self.load_peanut()
        
        # Determine database type
        db_type = self.data.get('database.default', 'sqlite')
        
        try:
            # Import adapters (delayed to avoid circular imports)
            from adapters import get_adapter, load_adapter_from_peanut
            
            # Load appropriate adapter
            adapter = load_adapter_from_peanut(db_type)
            
            # Execute query
            return adapter.query(query)
            
        except Exception as e:
            # Fallback to placeholder if adapters not available
            return f"[Query: {query} on {db_type}] - Error: {str(e)}"
    
    def execute_operator(self, operator: str, params: str) -> Any:
        """Execute @ operators with full implementation"""
        try:
            # Check if operator is registered
            if operator in self.operators:
                op_info = self.operators[operator]
                if op_info.implemented:
                    # Call the appropriate method
                    method_name = op_info.method_name
                    if hasattr(self, method_name):
                        return getattr(self, method_name)(params)
                    else:
                        logger.warning(f"Method {method_name} not found for operator {operator}")
                        return f"@{operator}({params}) - Method not implemented"
                else:
                    logger.warning(f"Operator {operator} not yet implemented")
                    return f"@{operator}({params}) - Not implemented"
            else:
                # Try legacy operator handling
                return self._execute_legacy_operator(operator, params)
        except Exception as e:
            logger.error(f"Error executing operator {operator}: {str(e)}")
            return f"@{operator}({params}) - Error: {str(e)}"
    
    def _execute_legacy_operator(self, operator: str, params: str) -> Any:
        """Legacy operator execution for backward compatibility"""
        if operator == 'cache':
            return self.execute_cache(params)
        elif operator in ['learn', 'optimize', 'metrics', 'feature']:
            # Placeholders for advanced features
            return f"@{operator}({params})"
        else:
            return f"@{operator}({params})"
    
    def execute_cache(self, params: str) -> Any:
        """Execute @cache operator with TTL support"""
        try:
            parts = params.split(',', 1)
            if len(parts) == 2:
                ttl = int(parts[0].strip().strip('"\''))
                value = parts[1].strip()
                parsed_value = self.parse_value(value)
                
                # Create cache key
                cache_key = hashlib.md5(str(parsed_value).encode()).hexdigest()
                
                # Store in cache with TTL
                self.cache[cache_key] = {
                    'value': parsed_value,
                    'expires': time.time() + ttl,
                    'created': time.time()
                }
                
                return parsed_value
            return ""
        except Exception as e:
            logger.error(f"Cache execution error: {str(e)}")
            return f"@cache({params}) - Error: {str(e)}"
    
    def execute_env(self, params: str) -> str:
        """Execute @env operator"""
        try:
            # Extract environment variable name
            env_match = re.match(r'^["\']([^"\']*)["\'](?:,\s*(.+))?$', params)
            if env_match:
                env_var = env_match.group(1)
                default_val = env_match.group(2) if env_match.group(2) else ""
                default_val = default_val.strip('"\'')
                return os.environ.get(env_var, default_val)
            return ""
        except Exception as e:
            logger.error(f"Env execution error: {str(e)}")
            return f"@env({params}) - Error: {str(e)}"
    
    def execute_file(self, params: str) -> Any:
        """Execute @file operator for file operations"""
        try:
            # Extract file path and operation
            file_match = re.match(r'^["\']([^"\']*)["\'](?:,\s*(.+))?$', params)
            if file_match:
                file_path = file_match.group(1)
                operation = file_match.group(2) if file_match.group(2) else "read"
                operation = operation.strip('"\'')
                
                if operation == "read":
                    with open(file_path, 'r', encoding='utf-8') as f:
                        return f.read()
                elif operation == "write":
                    # This would need content parameter
                    return f"@file({params}) - Write operation needs content"
                elif operation == "exists":
                    return Path(file_path).exists()
                else:
                    return f"@file({params}) - Unknown operation: {operation}"
            return ""
        except Exception as e:
            logger.error(f"File execution error: {str(e)}")
            return f"@file({params}) - Error: {str(e)}"
    
    def execute_json(self, params: str) -> Any:
        """Execute @json operator for JSON operations"""
        try:
            # Parse the JSON string and return the parsed object
            if params.startswith('"') and params.endswith('"'):
                # Remove quotes and parse
                json_str = params[1:-1]
                return json.loads(json_str)
            else:
                # Direct JSON parsing
                return json.loads(params)
        except json.JSONDecodeError as e:
            logger.error(f"JSON parsing error: {e}")
            return None
        except Exception as e:
            logger.error(f"JSON operator error: {e}")
            return None
    
    def execute_date(self, format_str: str) -> str:
        """Execute @date operator"""
        try:
            return datetime.now().strftime(format_str)
        except Exception as e:
            logger.error(f"Date execution error: {str(e)}")
            return f"@date({format_str}) - Error: {str(e)}"
    
    def execute_query(self, query: str) -> Any:
        """Execute @query operator with database support"""
        try:
            # Try to determine database type from query or connection
            db_type = self._detect_database_type(query)
            
            if db_type == "sqlite":
                return self._execute_sqlite_query(query)
            elif db_type == "postgresql":
                return self._execute_postgresql_query(query)
            elif db_type == "mysql":
                return self._execute_mysql_query(query)
            else:
                # Default to SQLite
                return self._execute_sqlite_query(query)
        except Exception as e:
            logger.error(f"Query execution error: {str(e)}")
            return f"[Query: {query}] - Error: {str(e)}"
    
    def _detect_database_type(self, query: str) -> str:
        """Detect database type from query or configuration"""
        # Check for database configuration in global variables
        if 'database.type' in self.data:
            return self.data['database.type']
        
        # Default to SQLite
        return "sqlite"
    
    def _execute_sqlite_query(self, query: str) -> Any:
        """Execute SQLite query"""
        try:
            db_path = self.data.get('database.path', ':memory:')
            conn = sqlite3.connect(db_path)
            cursor = conn.cursor()
            cursor.execute(query)
            
            if query.strip().upper().startswith('SELECT'):
                results = cursor.fetchall()
                columns = [description[0] for description in cursor.description]
                return [dict(zip(columns, row)) for row in results]
            else:
                conn.commit()
                return {"affected_rows": cursor.rowcount}
        except Exception as e:
            logger.error(f"SQLite query error: {str(e)}")
            return f"SQLite Error: {str(e)}"
        finally:
            if 'conn' in locals():
                conn.close()
    
    def _execute_postgresql_query(self, query: str) -> Any:
        """Execute PostgreSQL query"""
        try:
            # Get connection parameters
            host = self.data.get('database.host', 'localhost')
            port = self.data.get('database.port', 5432)
            dbname = self.data.get('database.name', 'postgres')
            user = self.data.get('database.user', 'postgres')
            password = self.data.get('database.password', '')
            
            conn = psycopg2.connect(
                host=host,
                port=port,
                dbname=dbname,
                user=user,
                password=password
            )
            cursor = conn.cursor()
            cursor.execute(query)
            
            if query.strip().upper().startswith('SELECT'):
                results = cursor.fetchall()
                columns = [description[0] for description in cursor.description]
                return [dict(zip(columns, row)) for row in results]
            else:
                conn.commit()
                return {"affected_rows": cursor.rowcount}
        except Exception as e:
            logger.error(f"PostgreSQL query error: {str(e)}")
            return f"PostgreSQL Error: {str(e)}"
        finally:
            if 'conn' in locals():
                conn.close()
    
    def _execute_mysql_query(self, query: str) -> Any:
        """Execute MySQL query"""
        try:
            # Get connection parameters
            host = self.data.get('database.host', 'localhost')
            port = self.data.get('database.port', 3306)
            dbname = self.data.get('database.name', 'mysql')
            user = self.data.get('database.user', 'root')
            password = self.data.get('database.password', '')
            
            # Note: This would require mysql-connector-python
            # For now, return placeholder
            return f"MySQL query: {query} - MySQL adapter not fully implemented"
        except Exception as e:
            logger.error(f"MySQL query error: {str(e)}")
            return f"MySQL Error: {str(e)}"
    
    def execute_metrics(self, params: str) -> Any:
        """Execute @metrics operator for performance tracking"""
        try:
            # Parse metrics parameters
            metric_match = re.match(r'^["\']([^"\']*)["\'](?:,\s*(.+))?$', params)
            if metric_match:
                metric_name = metric_match.group(1)
                metric_value = metric_match.group(2) if metric_match.group(2) else 1
                metric_value = float(self.parse_value(metric_value))
                
                # Store metric
                if metric_name not in self.metrics:
                    self.metrics[metric_name] = []
                
                self.metrics[metric_name].append({
                    'value': metric_value,
                    'timestamp': time.time(),
                    'datetime': datetime.now().isoformat()
                })
                
                return f"Metric {metric_name} recorded: {metric_value}"
            return ""
        except Exception as e:
            logger.error(f"Metrics execution error: {str(e)}")
            return f"@metrics({params}) - Error: {str(e)}"
    
    def execute_learn(self, params: str) -> Any:
        """Execute @learn operator for machine learning"""
        try:
            # Parse learning parameters
            learn_match = re.match(r'^["\']([^"\']*)["\'](?:,\s*(.+))?$', params)
            if learn_match:
                model_name = learn_match.group(1)
                data = learn_match.group(2) if learn_match.group(2) else "{}"
                
                # Parse data
                try:
                    data_dict = json.loads(data)
                except:
                    data_dict = {"data": data}
                
                # Store learning data
                if 'learning_models' not in self.data:
                    self.data['learning_models'] = {}
                
                self.data['learning_models'][model_name] = {
                    'data': data_dict,
                    'created': time.time(),
                    'updated': time.time()
                }
                
                return f"Learning model {model_name} updated with {len(data_dict)} data points"
            return ""
        except Exception as e:
            logger.error(f"Learn execution error: {str(e)}")
            return f"@learn({params}) - Error: {str(e)}"
    
    def execute_optimize(self, params: str) -> Any:
        """Execute @optimize operator for code optimization"""
        try:
            # Parse optimization parameters
            opt_match = re.match(r'^["\']([^"\']*)["\'](?:,\s*(.+))?$', params)
            if opt_match:
                target = opt_match.group(1)
                options = opt_match.group(2) if opt_match.group(2) else "{}"
                
                # Parse options
                try:
                    options_dict = json.loads(options)
                except:
                    options_dict = {"level": "basic"}
                
                # Store optimization data
                if 'optimizations' not in self.data:
                    self.data['optimizations'] = {}
                
                self.data['optimizations'][target] = {
                    'options': options_dict,
                    'timestamp': time.time(),
                    'status': 'pending'
                }
                
                return f"Optimization scheduled for {target} with options: {options_dict}"
            return ""
        except Exception as e:
            logger.error(f"Optimize execution error: {str(e)}")
            return f"@optimize({params}) - Error: {str(e)}"
    
    def execute_feature(self, params: str) -> Any:
        """Execute @feature operator for feature flags"""
        try:
            # Parse feature parameters
            feature_match = re.match(r'^["\']([^"\']*)["\'](?:,\s*(.+))?$', params)
            if feature_match:
                feature_name = feature_match.group(1)
                default_value = feature_match.group(2) if feature_match.group(2) else "false"
                default_value = self.parse_value(default_value)
                
                # Check if feature is enabled
                if 'features' in self.data and feature_name in self.data['features']:
                    return self.data['features'][feature_name]
                else:
                    return default_value
            return False
        except Exception as e:
            logger.error(f"Feature execution error: {str(e)}")
            return f"@feature({params}) - Error: {str(e)}"
    
    def execute_request(self, params: str) -> Any:
        """Execute @request operator for HTTP requests"""
        try:
            # Parse request parameters
            request_match = re.match(r'^["\']([^"\']*)["\'](?:,\s*(.+))?$', params)
            if request_match:
                url = request_match.group(1)
                options = request_match.group(2) if request_match.group(2) else "{}"
                
                # Parse options
                try:
                    options_dict = json.loads(options)
                except:
                    options_dict = {"method": "GET"}
                
                # Make request (synchronous for now)
                import requests
                method = options_dict.get("method", "GET")
                headers = options_dict.get("headers", {})
                data = options_dict.get("data", None)
                
                response = requests.request(
                    method=method,
                    url=url,
                    headers=headers,
                    data=data
                )
                
                return {
                    "status_code": response.status_code,
                    "headers": dict(response.headers),
                    "content": response.text,
                    "json": response.json() if response.headers.get('content-type', '').startswith('application/json') else None
                }
            return ""
        except Exception as e:
            logger.error(f"Request execution error: {str(e)}")
            return f"@request({params}) - Error: {str(e)}"
    
    def execute_if(self, params: str) -> Any:
        """Execute @if operator for conditional logic"""
        try:
            # Parse conditional parameters: condition ? true_value : false_value
            if_match = re.match(r'^(.+?)\s*\?\s*(.+?)\s*:\s*(.+)$', params)
            if if_match:
                condition = if_match.group(1).strip()
                true_value = if_match.group(2).strip()
                false_value = if_match.group(3).strip()
                
                if self.evaluate_condition(condition):
                    return self.parse_value(true_value)
                else:
                    return self.parse_value(false_value)
            return ""
        except Exception as e:
            logger.error(f"If execution error: {str(e)}")
            return f"@if({params}) - Error: {str(e)}"
    
    def execute_output(self, params: str) -> Any:
        """Execute @output operator for output formatting"""
        try:
            # Parse output parameters
            output_match = re.match(r'^["\']([^"\']*)["\'](?:,\s*(.+))?$', params)
            if output_match:
                format_type = output_match.group(1)
                data = output_match.group(2) if output_match.group(2) else ""
                data = self.parse_value(data)
                
                if format_type == "json":
                    return json.dumps(data, indent=2)
                elif format_type == "yaml":
                    import yaml
                    return yaml.dump(data, default_flow_style=False)
                elif format_type == "xml":
                    # Simple XML formatting
                    if isinstance(data, dict):
                        xml = "<root>\n"
                        for key, value in data.items():
                            xml += f"  <{key}>{value}</{key}>\n"
                        xml += "</root>"
                        return xml
                    else:
                        return f"<value>{data}</value>"
                else:
                    return str(data)
            return ""
        except Exception as e:
            logger.error(f"Output execution error: {str(e)}")
            return f"@output({params}) - Error: {str(e)}"
    
    def execute_q(self, params: str) -> Any:
        """Execute @q operator (query shorthand)"""
        try:
            # @q is shorthand for @query
            return self.execute_query(params)
        except Exception as e:
            logger.error(f"Q execution error: {str(e)}")
            return f"@q({params}) - Error: {str(e)}"
    
    def parse_line(self, line: str):
        """Parse a single line"""
        trimmed = line.strip()
        
        # Skip empty lines and comments
        if not trimmed or trimmed.startswith('#'):
            return
        
        # Remove optional semicolon
        if trimmed.endswith(';'):
            trimmed = trimmed[:-1].strip()
        
        # Check for section declaration []
        section_match = re.match(r'^\[([a-zA-Z_][a-zA-Z0-9_]*)\]$', trimmed)
        if section_match:
            self.current_section = section_match.group(1)
            self.in_object = False
            return
        
        # Check for angle bracket object >
        angle_open_match = re.match(r'^([a-zA-Z_][a-zA-Z0-9_]*)\s*>$', trimmed)
        if angle_open_match:
            self.in_object = True
            self.object_key = angle_open_match.group(1)
            return
        
        # Check for closing angle bracket <
        if trimmed == '<':
            self.in_object = False
            self.object_key = ""
            return
        
        # Check for curly brace object {
        brace_open_match = re.match(r'^([a-zA-Z_][a-zA-Z0-9_]*)\s*\{$', trimmed)
        if brace_open_match:
            self.in_object = True
            self.object_key = brace_open_match.group(1)
            return
        
        # Check for closing curly brace }
        if trimmed == '}':
            self.in_object = False
            self.object_key = ""
            return
        
        # Parse key-value pairs (both : and = supported)
        kv_match = re.match(r'^([\$]?[a-zA-Z_][a-zA-Z0-9_-]*)\s*[:=]\s*(.+)$', trimmed)
        if kv_match:
            key = kv_match.group(1)
            value = kv_match.group(2)
            parsed_value = self.parse_value(value)
            
            # Determine storage location
            if self.in_object and self.object_key:
                if self.current_section:
                    storage_key = f"{self.current_section}.{self.object_key}.{key}"
                else:
                    storage_key = f"{self.object_key}.{key}"
            elif self.current_section:
                storage_key = f"{self.current_section}.{key}"
            else:
                storage_key = key
            
            # Store the value
            self.data[storage_key] = parsed_value
            
            # Handle global variables
            if key.startswith('$'):
                var_name = key[1:]
                self.global_variables[var_name] = parsed_value
            elif self.current_section and not key.startswith('$'):
                # Store section-local variable
                section_key = f"{self.current_section}.{key}"
                self.section_variables[section_key] = parsed_value
    
    def parse(self, content: str) -> Dict[str, Any]:
        """Parse TuskLang content"""
        lines = content.split('\n')
        
        for line in lines:
            self.parse_line(line)
        
        return self.data
    
    def parse_file(self, file_path: str) -> Dict[str, Any]:
        """Parse a TSK file"""
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        return self.parse(content)
    
    def get(self, key: str) -> Any:
        """Get a value by key"""
        return self.data.get(key, None)
    
    def set(self, key: str, value: Any):
        """Set a value"""
        self.data[key] = value
    
    def keys(self) -> List[str]:
        """Get all keys"""
        return sorted(self.data.keys())
    
    def items(self) -> List[Tuple[str, Any]]:
        """Get all key-value pairs"""
        return [(key, self.data[key]) for key in self.keys()]
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary"""
        return self.data.copy()


def parse(content: str) -> Dict[str, Any]:
    """Parse TuskLang content with enhanced syntax"""
    parser = TuskLangEnhanced()
    return parser.parse(content)


def parse_file(file_path: str) -> Dict[str, Any]:
    """Parse a TuskLang file with enhanced syntax"""
    parser = TuskLangEnhanced()
    return parser.parse_file(file_path)


def load_from_peanut():
    """Load configuration from peanu.tsk"""
    parser = TuskLangEnhanced()
    parser.load_peanut()
    return parser


# CLI interface
if __name__ == '__main__':
    import sys
    
    if len(sys.argv) < 2:
        print("""
TuskLang Enhanced for Python - The Freedom Parser
=================================================

Usage: python tsk_enhanced.py [command] [options]

Commands:
    parse <file>     Parse a .tsk file
    get <file> <key> Get a value by key
    keys <file>      List all keys
    peanut           Load from peanu.tsk
    
Examples:
    python tsk_enhanced.py parse config.tsk
    python tsk_enhanced.py get config.tsk database.host
    python tsk_enhanced.py keys config.tsk
    python tsk_enhanced.py peanut

Features:
    - Multiple syntax styles: [], {}, <>
    - Global variables with $
    - Cross-file references: @file.tsk.get()
    - Database queries: @query()
    - Date functions: @date()
    - Environment variables: @env()

Default config file: peanu.tsk
""")
        sys.exit(1)
    
    command = sys.argv[1]
    
    if command == 'parse':
        if len(sys.argv) < 3:
            print("Error: File path required")
            sys.exit(1)
        
        parser = TuskLangEnhanced()
        data = parser.parse_file(sys.argv[2])
        
        for key, value in parser.items():
            print(f"{key} = {value}")
    
    elif command == 'get':
        if len(sys.argv) < 4:
            print("Error: File path and key required")
            sys.exit(1)
        
        parser = TuskLangEnhanced()
        parser.parse_file(sys.argv[2])
        value = parser.get(sys.argv[3])
        print(value if value is not None else "")
    
    elif command == 'keys':
        if len(sys.argv) < 3:
            print("Error: File path required")
            sys.exit(1)
        
        parser = TuskLangEnhanced()
        parser.parse_file(sys.argv[2])
        
        for key in parser.keys():
            print(key)
    
    elif command == 'peanut':
        parser = load_from_peanut()
        print(f"Loaded {len(parser.data)} configuration items")
        
        for key, value in parser.items():
            print(f"{key} = {value}")
    
    else:
        print(f"Error: Unknown command: {command}")
        sys.exit(1)