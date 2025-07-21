#!/usr/bin/env python3
"""
TuskLang Advanced Cross-Language Interoperability Bridge
Seamless data exchange and communication between all language SDKs
"""

import os
import json
import time
import sqlite3
import threading
import asyncio
import socket
import struct
import pickle
import base64
import hashlib
from pathlib import Path
from typing import Dict, List, Optional, Any, Tuple, Union
from dataclasses import dataclass, asdict
from datetime import datetime, timedelta
import logging
import tempfile
import uuid
from collections import defaultdict, deque
import queue
import select
import ssl
import zlib

logger = logging.getLogger(__name__)

@dataclass
class DataType:
    """Cross-language data type definition"""
    name: str
    language: str
    native_type: str
    size: int
    is_primitive: bool
    is_complex: bool
    serialization_format: str  # 'json', 'pickle', 'protobuf', 'msgpack'
    validation_schema: Optional[Dict] = None

@dataclass
class TypeMapping:
    """Type mapping between languages"""
    source_language: str
    target_language: str
    source_type: str
    target_type: str
    conversion_function: str
    bidirectional: bool = True
    lossless: bool = True

@dataclass
class Message:
    """Cross-language message"""
    message_id: str
    source_language: str
    target_language: str
    message_type: str  # 'data', 'command', 'response', 'error'
    payload: Any
    metadata: Dict[str, Any]
    timestamp: datetime
    ttl: int = 300  # Time to live in seconds
    priority: int = 0  # 0=normal, 1=high, 2=urgent

@dataclass
class ProtocolDefinition:
    """Communication protocol definition"""
    protocol_name: str
    version: str
    supported_languages: List[str]
    message_formats: Dict[str, Dict]
    encoding: str  # 'utf-8', 'ascii', 'binary'
    compression: bool = False
    encryption: bool = False

@dataclass
class ServiceEndpoint:
    """Service endpoint definition"""
    service_id: str
    language: str
    endpoint_type: str  # 'http', 'tcp', 'unix', 'memory'
    address: str
    port: Optional[int] = None
    protocol: str = 'json'
    authentication: Optional[Dict] = None
    health_check_url: Optional[str] = None

class CrossLanguageBridge:
    """Advanced cross-language communication and data exchange system"""
    
    def __init__(self, bridge_dir: Path = None):
        if bridge_dir is None:
            self.bridge_dir = Path(tempfile.mkdtemp(prefix='tsk_interop_bridge_'))
        else:
            self.bridge_dir = bridge_dir
        
        self.db_path = self.bridge_dir / 'interoperability_bridge.db'
        self.cache_dir = self.bridge_dir / 'cache'
        self.logs_dir = self.bridge_dir / 'logs'
        
        # Create directories
        self.cache_dir.mkdir(exist_ok=True)
        self.logs_dir.mkdir(exist_ok=True)
        
        # Initialize database
        self._init_database()
        
        # Bridge state
        self.bridge_active = False
        self.bridge_thread = None
        self.stop_bridge = threading.Event()
        
        # Data type registry
        self.data_types = self._load_default_data_types()
        self.type_mappings = self._load_default_type_mappings()
        
        # Protocol definitions
        self.protocols = self._load_default_protocols()
        
        # Service endpoints
        self.service_endpoints = {}
        
        # Message queues
        self.message_queues = defaultdict(queue.Queue)
        self.message_history = deque(maxlen=10000)
        
        # Connection pools
        self.connection_pools = {}
        
        # Serialization formats
        self.serializers = {
            'json': self._serialize_json,
            'pickle': self._serialize_pickle,
            'msgpack': self._serialize_msgpack,
            'protobuf': self._serialize_protobuf
        }
        
        self.deserializers = {
            'json': self._deserialize_json,
            'pickle': self._deserialize_pickle,
            'msgpack': self._deserialize_msgpack,
            'protobuf': self._deserialize_protobuf
        }
    
    def _init_database(self):
        """Initialize SQLite database for interoperability bridge"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Create tables
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS data_types (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT,
                language TEXT,
                native_type TEXT,
                size INTEGER,
                is_primitive BOOLEAN,
                is_complex BOOLEAN,
                serialization_format TEXT,
                validation_schema TEXT,
                created_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS type_mappings (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                source_language TEXT,
                target_language TEXT,
                source_type TEXT,
                target_type TEXT,
                conversion_function TEXT,
                bidirectional BOOLEAN,
                lossless BOOLEAN,
                created_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS messages (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                message_id TEXT UNIQUE,
                source_language TEXT,
                target_language TEXT,
                message_type TEXT,
                payload TEXT,
                metadata TEXT,
                timestamp TEXT,
                ttl INTEGER,
                priority INTEGER,
                status TEXT,
                created_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS service_endpoints (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                service_id TEXT UNIQUE,
                language TEXT,
                endpoint_type TEXT,
                address TEXT,
                port INTEGER,
                protocol TEXT,
                authentication TEXT,
                health_check_url TEXT,
                created_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS protocol_definitions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                protocol_name TEXT,
                version TEXT,
                supported_languages TEXT,
                message_formats TEXT,
                encoding TEXT,
                compression BOOLEAN,
                encryption BOOLEAN,
                created_at TEXT
            )
        ''')
        
        conn.commit()
        conn.close()
    
    def _load_default_data_types(self) -> Dict[str, DataType]:
        """Load default data type definitions"""
        default_types = {}
        
        # Python types
        python_types = [
            DataType("int", "python", "int", 8, True, False, "json"),
            DataType("float", "python", "float", 8, True, False, "json"),
            DataType("str", "python", "str", 0, True, False, "json"),
            DataType("bool", "python", "bool", 1, True, False, "json"),
            DataType("list", "python", "list", 0, False, True, "json"),
            DataType("dict", "python", "dict", 0, False, True, "json"),
            DataType("tuple", "python", "tuple", 0, False, True, "pickle"),
            DataType("set", "python", "set", 0, False, True, "pickle"),
            DataType("bytes", "python", "bytes", 0, True, False, "base64"),
            DataType("datetime", "python", "datetime", 8, False, True, "iso8601")
        ]
        
        for dt in python_types:
            default_types[f"python_{dt.name}"] = dt
        
        # Rust types
        rust_types = [
            DataType("i32", "rust", "i32", 4, True, False, "json"),
            DataType("i64", "rust", "i64", 8, True, False, "json"),
            DataType("f64", "rust", "f64", 8, True, False, "json"),
            DataType("String", "rust", "String", 0, True, False, "json"),
            DataType("bool", "rust", "bool", 1, True, False, "json"),
            DataType("Vec", "rust", "Vec<T>", 0, False, True, "json"),
            DataType("HashMap", "rust", "HashMap<K,V>", 0, False, True, "json"),
            DataType("Option", "rust", "Option<T>", 0, False, True, "json"),
            DataType("Result", "rust", "Result<T,E>", 0, False, True, "json")
        ]
        
        for dt in rust_types:
            default_types[f"rust_{dt.name}"] = dt
        
        # JavaScript types
        js_types = [
            DataType("number", "javascript", "number", 8, True, False, "json"),
            DataType("string", "javascript", "string", 0, True, False, "json"),
            DataType("boolean", "javascript", "boolean", 1, True, False, "json"),
            DataType("array", "javascript", "Array", 0, False, True, "json"),
            DataType("object", "javascript", "Object", 0, False, True, "json"),
            DataType("null", "javascript", "null", 0, True, False, "json"),
            DataType("undefined", "javascript", "undefined", 0, True, False, "json"),
            DataType("Date", "javascript", "Date", 8, False, True, "iso8601")
        ]
        
        for dt in js_types:
            default_types[f"javascript_{dt.name}"] = dt
        
        # Save default types to database
        for dt in default_types.values():
            self.save_data_type(dt)
        
        return default_types
    
    def _load_default_type_mappings(self) -> List[TypeMapping]:
        """Load default type mappings between languages"""
        mappings = [
            # Python <-> Rust
            TypeMapping("python", "rust", "int", "i64", "int_to_i64"),
            TypeMapping("python", "rust", "float", "f64", "float_to_f64"),
            TypeMapping("python", "rust", "str", "String", "str_to_string"),
            TypeMapping("python", "rust", "bool", "bool", "bool_to_bool"),
            TypeMapping("python", "rust", "list", "Vec", "list_to_vec"),
            TypeMapping("python", "rust", "dict", "HashMap", "dict_to_hashmap"),
            
            # Python <-> JavaScript
            TypeMapping("python", "javascript", "int", "number", "int_to_number"),
            TypeMapping("python", "javascript", "float", "number", "float_to_number"),
            TypeMapping("python", "javascript", "str", "string", "str_to_string"),
            TypeMapping("python", "javascript", "bool", "boolean", "bool_to_boolean"),
            TypeMapping("python", "javascript", "list", "array", "list_to_array"),
            TypeMapping("python", "javascript", "dict", "object", "dict_to_object"),
            
            # Rust <-> JavaScript
            TypeMapping("rust", "javascript", "i64", "number", "i64_to_number"),
            TypeMapping("rust", "javascript", "f64", "number", "f64_to_number"),
            TypeMapping("rust", "javascript", "String", "string", "string_to_string"),
            TypeMapping("rust", "javascript", "bool", "boolean", "bool_to_boolean"),
            TypeMapping("rust", "javascript", "Vec", "array", "vec_to_array"),
            TypeMapping("rust", "javascript", "HashMap", "object", "hashmap_to_object"),
        ]
        
        # Save mappings to database
        for mapping in mappings:
            self.save_type_mapping(mapping)
        
        return mappings
    
    def _load_default_protocols(self) -> Dict[str, ProtocolDefinition]:
        """Load default protocol definitions"""
        protocols = {
            'json-rpc': ProtocolDefinition(
                protocol_name='json-rpc',
                version='2.0',
                supported_languages=['python', 'rust', 'javascript', 'ruby', 'csharp', 'go', 'php', 'java'],
                message_formats={
                    'request': {
                        'jsonrpc': '2.0',
                        'method': 'string',
                        'params': 'object',
                        'id': 'string|number|null'
                    },
                    'response': {
                        'jsonrpc': '2.0',
                        'result': 'object',
                        'error': 'object',
                        'id': 'string|number|null'
                    }
                },
                encoding='utf-8',
                compression=False,
                encryption=False
            ),
            'grpc': ProtocolDefinition(
                protocol_name='grpc',
                version='1.0',
                supported_languages=['python', 'rust', 'javascript', 'csharp', 'go', 'java'],
                message_formats={
                    'request': {
                        'method': 'string',
                        'headers': 'object',
                        'body': 'bytes'
                    },
                    'response': {
                        'status': 'number',
                        'headers': 'object',
                        'body': 'bytes'
                    }
                },
                encoding='binary',
                compression=True,
                encryption=True
            ),
            'messagepack': ProtocolDefinition(
                protocol_name='messagepack',
                version='1.0',
                supported_languages=['python', 'rust', 'javascript', 'ruby', 'go', 'php'],
                message_formats={
                    'message': {
                        'type': 'string',
                        'payload': 'binary',
                        'metadata': 'object'
                    }
                },
                encoding='binary',
                compression=True,
                encryption=False
            )
        }
        
        # Save protocols to database
        for protocol in protocols.values():
            self.save_protocol(protocol)
        
        return protocols
    
    def start_bridge(self) -> bool:
        """Start the interoperability bridge"""
        if self.bridge_active:
            logger.warning("Interoperability bridge is already active")
            return False
        
        try:
            self.bridge_active = True
            self.stop_bridge.clear()
            
            # Start bridge thread
            self.bridge_thread = threading.Thread(
                target=self._bridge_loop
            )
            self.bridge_thread.daemon = True
            self.bridge_thread.start()
            
            logger.info("Started interoperability bridge")
            return True
            
        except Exception as e:
            logger.error(f"Failed to start bridge: {e}")
            self.bridge_active = False
            return False
    
    def stop_bridge(self):
        """Stop the interoperability bridge"""
        if not self.bridge_active:
            return
        
        self.stop_bridge.set()
        self.bridge_active = False
        
        if self.bridge_thread:
            self.bridge_thread.join(timeout=5)
        
        logger.info("Stopped interoperability bridge")
    
    def _bridge_loop(self):
        """Main bridge loop for message processing"""
        while not self.stop_bridge.is_set():
            try:
                # Process message queues
                self._process_message_queues()
                
                # Clean up expired messages
                self._cleanup_expired_messages()
                
                # Health check endpoints
                self._health_check_endpoints()
                
                # Wait for next cycle
                time.sleep(1)  # 1-second processing interval
                
            except Exception as e:
                logger.error(f"Error in bridge loop: {e}")
                time.sleep(5)  # Wait before retrying
    
    def _process_message_queues(self):
        """Process messages in all queues"""
        for language, queue in self.message_queues.items():
            try:
                while not queue.empty():
                    message = queue.get_nowait()
                    self._process_message(message)
            except Exception as e:
                logger.error(f"Error processing messages for {language}: {e}")
    
    def _process_message(self, message: Message):
        """Process a single message"""
        try:
            # Validate message
            if not self._validate_message(message):
                logger.warning(f"Invalid message: {message.message_id}")
                return
            
            # Convert data types if needed
            if message.source_language != message.target_language:
                converted_payload = self._convert_data_types(
                    message.payload, 
                    message.source_language, 
                    message.target_language
                )
                message.payload = converted_payload
            
            # Route message to target
            self._route_message(message)
            
            # Store in history
            self.message_history.append(message)
            
            # Save to database
            self._save_message(message)
            
        except Exception as e:
            logger.error(f"Error processing message {message.message_id}: {e}")
    
    def _validate_message(self, message: Message) -> bool:
        """Validate message format and content"""
        try:
            # Check required fields
            if not all([message.message_id, message.source_language, 
                       message.target_language, message.message_type]):
                return False
            
            # Check TTL
            if message.timestamp + timedelta(seconds=message.ttl) < datetime.now():
                return False
            
            # Check supported languages
            if message.source_language not in ['python', 'rust', 'javascript', 'ruby', 'csharp', 'go', 'php', 'java', 'bash']:
                return False
            
            if message.target_language not in ['python', 'rust', 'javascript', 'ruby', 'csharp', 'go', 'php', 'java', 'bash']:
                return False
            
            return True
            
        except Exception:
            return False
    
    def _convert_data_types(self, data: Any, source_lang: str, target_lang: str) -> Any:
        """Convert data types between languages"""
        try:
            # Find appropriate type mapping
            mapping = self._find_type_mapping(source_lang, target_lang, type(data).__name__)
            if not mapping:
                # Use default conversion
                return self._default_type_conversion(data, source_lang, target_lang)
            
            # Apply conversion function
            conversion_func = getattr(self, mapping.conversion_function, None)
            if conversion_func:
                return conversion_func(data)
            else:
                return self._default_type_conversion(data, source_lang, target_lang)
                
        except Exception as e:
            logger.error(f"Error converting data types: {e}")
            return data
    
    def _find_type_mapping(self, source_lang: str, target_lang: str, source_type: str) -> Optional[TypeMapping]:
        """Find type mapping between languages"""
        for mapping in self.type_mappings:
            if (mapping.source_language == source_lang and 
                mapping.target_language == target_lang and 
                mapping.source_type == source_type):
                return mapping
        return None
    
    def _default_type_conversion(self, data: Any, source_lang: str, target_lang: str) -> Any:
        """Default type conversion using JSON serialization"""
        try:
            # Serialize to JSON
            json_data = json.dumps(data, default=str)
            
            # Deserialize back to Python object
            converted_data = json.loads(json_data)
            
            return converted_data
            
        except Exception as e:
            logger.error(f"Error in default type conversion: {e}")
            return data
    
    def _route_message(self, message: Message):
        """Route message to target language"""
        try:
            # Get target endpoint
            endpoint = self.service_endpoints.get(message.target_language)
            if not endpoint:
                logger.warning(f"No endpoint found for {message.target_language}")
                return
            
            # Serialize message
            serialized_data = self._serialize_message(message, endpoint.protocol)
            
            # Send to endpoint
            self._send_to_endpoint(endpoint, serialized_data)
            
        except Exception as e:
            logger.error(f"Error routing message: {e}")
    
    def _serialize_message(self, message: Message, protocol: str) -> bytes:
        """Serialize message using specified protocol"""
        try:
            if protocol == 'json':
                return json.dumps(asdict(message), default=str).encode('utf-8')
            elif protocol == 'pickle':
                return pickle.dumps(message)
            elif protocol == 'msgpack':
                return self._serialize_msgpack(asdict(message))
            else:
                return json.dumps(asdict(message), default=str).encode('utf-8')
                
        except Exception as e:
            logger.error(f"Error serializing message: {e}")
            return json.dumps(asdict(message), default=str).encode('utf-8')
    
    def _serialize_json(self, data: Any) -> bytes:
        """Serialize data to JSON"""
        return json.dumps(data, default=str).encode('utf-8')
    
    def _serialize_pickle(self, data: Any) -> bytes:
        """Serialize data using pickle"""
        return pickle.dumps(data)
    
    def _serialize_msgpack(self, data: Any) -> bytes:
        """Serialize data using MessagePack"""
        try:
            import msgpack
            return msgpack.packb(data, default=str)
        except ImportError:
            # Fallback to JSON
            return json.dumps(data, default=str).encode('utf-8')
    
    def _serialize_protobuf(self, data: Any) -> bytes:
        """Serialize data using Protocol Buffers"""
        try:
            # This would require protobuf definitions
            # For now, fallback to JSON
            return json.dumps(data, default=str).encode('utf-8')
        except Exception:
            return json.dumps(data, default=str).encode('utf-8')
    
    def _deserialize_json(self, data: bytes) -> Any:
        """Deserialize data from JSON"""
        return json.loads(data.decode('utf-8'))
    
    def _deserialize_pickle(self, data: bytes) -> Any:
        """Deserialize data using pickle"""
        return pickle.loads(data)
    
    def _deserialize_msgpack(self, data: bytes) -> Any:
        """Deserialize data using MessagePack"""
        try:
            import msgpack
            return msgpack.unpackb(data, raw=False)
        except ImportError:
            # Fallback to JSON
            return json.loads(data.decode('utf-8'))
    
    def _deserialize_protobuf(self, data: bytes) -> Any:
        """Deserialize data using Protocol Buffers"""
        try:
            # This would require protobuf definitions
            # For now, fallback to JSON
            return json.loads(data.decode('utf-8'))
        except Exception:
            return json.loads(data.decode('utf-8'))
    
    def _send_to_endpoint(self, endpoint: ServiceEndpoint, data: bytes):
        """Send data to service endpoint"""
        try:
            if endpoint.endpoint_type == 'http':
                self._send_http(endpoint, data)
            elif endpoint.endpoint_type == 'tcp':
                self._send_tcp(endpoint, data)
            elif endpoint.endpoint_type == 'unix':
                self._send_unix(endpoint, data)
            elif endpoint.endpoint_type == 'memory':
                self._send_memory(endpoint, data)
                
        except Exception as e:
            logger.error(f"Error sending to endpoint {endpoint.service_id}: {e}")
    
    def _send_http(self, endpoint: ServiceEndpoint, data: bytes):
        """Send data via HTTP"""
        try:
            import requests
            
            url = f"http://{endpoint.address}"
            if endpoint.port:
                url = f"http://{endpoint.address}:{endpoint.port}"
            
            headers = {'Content-Type': 'application/json'}
            if endpoint.authentication:
                headers.update(endpoint.authentication)
            
            response = requests.post(url, data=data, headers=headers, timeout=10)
            response.raise_for_status()
            
        except Exception as e:
            logger.error(f"HTTP send error: {e}")
    
    def _send_tcp(self, endpoint: ServiceEndpoint, data: bytes):
        """Send data via TCP"""
        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.connect((endpoint.address, endpoint.port or 8080))
            sock.send(data)
            sock.close()
            
        except Exception as e:
            logger.error(f"TCP send error: {e}")
    
    def _send_unix(self, endpoint: ServiceEndpoint, data: bytes):
        """Send data via Unix socket"""
        try:
            sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
            sock.connect(endpoint.address)
            sock.send(data)
            sock.close()
            
        except Exception as e:
            logger.error(f"Unix socket send error: {e}")
    
    def _send_memory(self, endpoint: ServiceEndpoint, data: bytes):
        """Send data via memory queue"""
        try:
            if endpoint.address in self.message_queues:
                # Deserialize and add to queue
                message_data = self._deserialize_json(data)
                message = Message(**message_data)
                self.message_queues[endpoint.address].put(message)
            
        except Exception as e:
            logger.error(f"Memory send error: {e}")
    
    def send_message(self, source_lang: str, target_lang: str, 
                    message_type: str, payload: Any, 
                    metadata: Dict[str, Any] = None) -> str:
        """Send a message between languages"""
        try:
            message_id = str(uuid.uuid4())
            
            message = Message(
                message_id=message_id,
                source_language=source_lang,
                target_language=target_lang,
                message_type=message_type,
                payload=payload,
                metadata=metadata or {},
                timestamp=datetime.now(),
                ttl=300,
                priority=0
            )
            
            # Add to source language queue
            self.message_queues[source_lang].put(message)
            
            return message_id
            
        except Exception as e:
            logger.error(f"Error sending message: {e}")
            return None
    
    def receive_message(self, language: str, timeout: float = 1.0) -> Optional[Message]:
        """Receive a message for a language"""
        try:
            if language in self.message_queues:
                return self.message_queues[language].get(timeout=timeout)
            return None
            
        except queue.Empty:
            return None
        except Exception as e:
            logger.error(f"Error receiving message: {e}")
            return None
    
    def register_service_endpoint(self, service_id: str, language: str,
                                endpoint_type: str, address: str,
                                port: int = None, protocol: str = 'json',
                                authentication: Dict = None) -> bool:
        """Register a service endpoint"""
        try:
            endpoint = ServiceEndpoint(
                service_id=service_id,
                language=language,
                endpoint_type=endpoint_type,
                address=address,
                port=port,
                protocol=protocol,
                authentication=authentication
            )
            
            self.service_endpoints[service_id] = endpoint
            
            # Save to database
            self._save_service_endpoint(endpoint)
            
            return True
            
        except Exception as e:
            logger.error(f"Error registering endpoint: {e}")
            return False
    
    def save_data_type(self, data_type: DataType) -> bool:
        """Save data type definition"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO data_types 
                (name, language, native_type, size, is_primitive, is_complex, serialization_format, validation_schema, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                data_type.name,
                data_type.language,
                data_type.native_type,
                data_type.size,
                data_type.is_primitive,
                data_type.is_complex,
                data_type.serialization_format,
                json.dumps(data_type.validation_schema) if data_type.validation_schema else None,
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            logger.error(f"Failed to save data type: {e}")
            return False
    
    def save_type_mapping(self, mapping: TypeMapping) -> bool:
        """Save type mapping"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO type_mappings 
                (source_language, target_language, source_type, target_type, conversion_function, bidirectional, lossless, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                mapping.source_language,
                mapping.target_language,
                mapping.source_type,
                mapping.target_type,
                mapping.conversion_function,
                mapping.bidirectional,
                mapping.lossless,
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            logger.error(f"Failed to save type mapping: {e}")
            return False
    
    def save_protocol(self, protocol: ProtocolDefinition) -> bool:
        """Save protocol definition"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO protocol_definitions 
                (protocol_name, version, supported_languages, message_formats, encoding, compression, encryption, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                protocol.protocol_name,
                protocol.version,
                json.dumps(protocol.supported_languages),
                json.dumps(protocol.message_formats),
                protocol.encoding,
                protocol.compression,
                protocol.encryption,
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            logger.error(f"Failed to save protocol: {e}")
            return False
    
    def _save_service_endpoint(self, endpoint: ServiceEndpoint):
        """Save service endpoint to database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO service_endpoints 
                (service_id, language, endpoint_type, address, port, protocol, authentication, health_check_url, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                endpoint.service_id,
                endpoint.language,
                endpoint.endpoint_type,
                endpoint.address,
                endpoint.port,
                endpoint.protocol,
                json.dumps(endpoint.authentication) if endpoint.authentication else None,
                endpoint.health_check_url,
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"Failed to save service endpoint: {e}")
    
    def _save_message(self, message: Message):
        """Save message to database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO messages 
                (message_id, source_language, target_language, message_type, payload, metadata, timestamp, ttl, priority, status, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                message.message_id,
                message.source_language,
                message.target_language,
                message.message_type,
                json.dumps(message.payload, default=str),
                json.dumps(message.metadata),
                message.timestamp.isoformat(),
                message.ttl,
                message.priority,
                'sent',
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"Failed to save message: {e}")
    
    def _cleanup_expired_messages(self):
        """Clean up expired messages"""
        try:
            current_time = datetime.now()
            expired_messages = []
            
            for message in self.message_history:
                if message.timestamp + timedelta(seconds=message.ttl) < current_time:
                    expired_messages.append(message)
            
            for message in expired_messages:
                self.message_history.remove(message)
                
        except Exception as e:
            logger.error(f"Error cleaning up expired messages: {e}")
    
    def _health_check_endpoints(self):
        """Health check service endpoints"""
        for service_id, endpoint in self.service_endpoints.items():
            try:
                if endpoint.health_check_url:
                    import requests
                    response = requests.get(endpoint.health_check_url, timeout=5)
                    if response.status_code != 200:
                        logger.warning(f"Health check failed for {service_id}")
            except Exception as e:
                logger.error(f"Health check error for {service_id}: {e}")

def main():
    """CLI for interoperability bridge"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang Interoperability Bridge')
    parser.add_argument('--start', action='store_true', help='Start bridge')
    parser.add_argument('--stop', action='store_true', help='Stop bridge')
    parser.add_argument('--send', nargs=4, metavar=('SOURCE', 'TARGET', 'TYPE', 'PAYLOAD'), help='Send message')
    parser.add_argument('--receive', help='Receive message for language')
    parser.add_argument('--register-endpoint', nargs=6, metavar=('ID', 'LANG', 'TYPE', 'ADDR', 'PORT', 'PROTO'), help='Register endpoint')
    parser.add_argument('--status', action='store_true', help='Show bridge status')
    
    args = parser.parse_args()
    
    bridge = CrossLanguageBridge()
    
    if args.start:
        success = bridge.start_bridge()
        print(f"Bridge started: {success}")
    
    elif args.stop:
        bridge.stop_bridge()
        print("Bridge stopped")
    
    elif args.send:
        source, target, msg_type, payload = args.send
        message_id = bridge.send_message(source, target, msg_type, payload)
        print(f"Message sent: {message_id}")
    
    elif args.receive:
        message = bridge.receive_message(args.receive)
        if message:
            print(json.dumps(asdict(message), indent=2, default=str))
        else:
            print("No message received")
    
    elif args.register_endpoint:
        service_id, language, endpoint_type, address, port, protocol = args.register_endpoint
        success = bridge.register_service_endpoint(service_id, language, endpoint_type, address, int(port), protocol)
        print(f"Endpoint registered: {success}")
    
    elif args.status:
        print(f"Bridge active: {bridge.bridge_active}")
        print(f"Service endpoints: {len(bridge.service_endpoints)}")
        print(f"Data types: {len(bridge.data_types)}")
        print(f"Type mappings: {len(bridge.type_mappings)}")
    
    else:
        parser.print_help()

if __name__ == '__main__':
    main() 