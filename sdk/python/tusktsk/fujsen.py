#!/usr/bin/env python3
"""
FUJSEN (Function Serialization) Module for TuskLang Python SDK
==============================================================
Implements cross-language function serialization and execution
"""

import json
import base64
import gzip
import hashlib
import time
import logging
import subprocess
import tempfile
import os
from typing import Any, Dict, List, Optional, Union, Callable
from dataclasses import dataclass, asdict
from datetime import datetime
import pickle
import marshal
import inspect
import ast

logger = logging.getLogger(__name__)


@dataclass
class FujsenFunction:
    """Serialized function representation"""
    name: str
    language: str
    source_code: str
    compiled_code: Optional[bytes] = None
    dependencies: List[str] = None
    context: Dict[str, Any] = None
    metadata: Dict[str, Any] = None
    created_at: float = None
    expires_at: Optional[float] = None
    
    def __post_init__(self):
        if self.dependencies is None:
            self.dependencies = []
        if self.context is None:
            self.context = {}
        if self.metadata is None:
            self.metadata = {}
        if self.created_at is None:
            self.created_at = time.time()


class Fujsen:
    """FUJSEN (Function Serialization) system for TuskLang"""
    
    def __init__(self):
        self.function_cache = {}
        self.context_cache = {}
        self.supported_languages = {
            'python': {
                'extensions': ['.py'],
                'executor': self._execute_python,
                'serializer': self._serialize_python,
                'deserializer': self._deserialize_python
            },
            'javascript': {
                'extensions': ['.js', '.ts'],
                'executor': self._execute_javascript,
                'serializer': self._serialize_javascript,
                'deserializer': self._deserialize_javascript
            },
            'bash': {
                'extensions': ['.sh', '.bash'],
                'executor': self._execute_bash,
                'serializer': self._serialize_bash,
                'deserializer': self._deserialize_bash
            },
            'php': {
                'extensions': ['.php'],
                'executor': self._execute_php,
                'serializer': self._serialize_php,
                'deserializer': self._deserialize_php
            }
        }
        
        # Cache settings
        self.cache_enabled = True
        self.cache_ttl = 3600  # 1 hour
        self.max_cache_size = 1000
    
    def serialize_function(self, func: Union[Callable, str], language: str = None, 
                          context: Dict[str, Any] = None) -> FujsenFunction:
        """Serialize a function for cross-language execution"""
        try:
            if isinstance(func, str):
                # Function is already source code
                source_code = func
                name = f"anonymous_{int(time.time())}"
                detected_language = self._detect_language_from_code(source_code)
            else:
                # Function is a callable
                source_code = self._get_function_source(func)
                name = func.__name__
                detected_language = 'python'
            
            # Use provided language or detected language
            language = language or detected_language
            
            if language not in self.supported_languages:
                raise ValueError(f"Unsupported language: {language}")
            
            # Create FUJSEN function
            fujsen_func = FujsenFunction(
                name=name,
                language=language,
                source_code=source_code,
                context=context or {},
                metadata={
                    'serialized_at': time.time(),
                    'serializer_version': '1.0.0'
                }
            )
            
            # Compile if possible
            try:
                fujsen_func.compiled_code = self.supported_languages[language]['serializer'](source_code)
            except Exception as e:
                logger.warning(f"Compilation failed for {name}: {str(e)}")
            
            # Cache the function
            if self.cache_enabled:
                cache_key = self._generate_cache_key(fujsen_func)
                self.function_cache[cache_key] = fujsen_func
                self._cleanup_cache()
            
            return fujsen_func
            
        except Exception as e:
            logger.error(f"Function serialization error: {str(e)}")
            raise Exception(f"Serialization failed: {str(e)}")
    
    def deserialize_function(self, fujsen_data: Union[str, Dict, FujsenFunction]) -> FujsenFunction:
        """Deserialize a FUJSEN function"""
        try:
            if isinstance(fujsen_data, str):
                # JSON string
                data = json.loads(fujsen_data)
            elif isinstance(fujsen_data, dict):
                # Dictionary
                data = fujsen_data
            elif isinstance(fujsen_data, FujsenFunction):
                # Already a FUJSEN function
                return fujsen_data
            else:
                raise ValueError("Invalid FUJSEN data format")
            
            # Create FUJSEN function from data
            fujsen_func = FujsenFunction(
                name=data.get('name', 'unknown'),
                language=data.get('language', 'python'),
                source_code=data.get('source_code', ''),
                compiled_code=data.get('compiled_code'),
                dependencies=data.get('dependencies', []),
                context=data.get('context', {}),
                metadata=data.get('metadata', {}),
                created_at=data.get('created_at', time.time()),
                expires_at=data.get('expires_at')
            )
            
            # Check expiration
            if fujsen_func.expires_at and time.time() > fujsen_func.expires_at:
                raise Exception("FUJSEN function has expired")
            
            return fujsen_func
            
        except Exception as e:
            logger.error(f"Function deserialization error: {str(e)}")
            raise Exception(f"Deserialization failed: {str(e)}")
    
    def execute_function(self, fujsen_func: FujsenFunction, args: List[Any] = None, 
                        kwargs: Dict[str, Any] = None) -> Any:
        """Execute a FUJSEN function"""
        try:
            args = args or []
            kwargs = kwargs or {}
            
            # Check if function is cached
            cache_key = self._generate_cache_key(fujsen_func)
            if cache_key in self.function_cache:
                cached_func = self.function_cache[cache_key]
                if time.time() - cached_func.created_at < self.cache_ttl:
                    fujsen_func = cached_func
            
            # Get executor for language
            language_config = self.supported_languages.get(fujsen_func.language)
            if not language_config:
                raise ValueError(f"Unsupported language: {fujsen_func.language}")
            
            executor = language_config['executor']
            
            # Execute function
            result = executor(fujsen_func, args, kwargs)
            
            # Cache result if enabled
            if self.cache_enabled:
                result_cache_key = f"{cache_key}_result_{hash(str(args) + str(kwargs))}"
                self.context_cache[result_cache_key] = {
                    'result': result,
                    'timestamp': time.time(),
                    'ttl': self.cache_ttl
                }
            
            return result
            
        except Exception as e:
            logger.error(f"Function execution error: {str(e)}")
            raise Exception(f"Execution failed: {str(e)}")
    
    def cache_function(self, fujsen_func: FujsenFunction, ttl: int = None) -> str:
        """Cache a FUJSEN function"""
        try:
            if not self.cache_enabled:
                return "Caching disabled"
            
            cache_key = self._generate_cache_key(fujsen_func)
            fujsen_func.expires_at = time.time() + (ttl or self.cache_ttl)
            
            self.function_cache[cache_key] = fujsen_func
            self._cleanup_cache()
            
            return cache_key
            
        except Exception as e:
            logger.error(f"Function caching error: {str(e)}")
            raise Exception(f"Caching failed: {str(e)}")
    
    def inject_context(self, fujsen_func: FujsenFunction, context: Dict[str, Any]) -> FujsenFunction:
        """Inject context into a FUJSEN function"""
        try:
            # Create a copy of the function with injected context
            injected_func = FujsenFunction(
                name=fujsen_func.name,
                language=fujsen_func.language,
                source_code=fujsen_func.source_code,
                compiled_code=fujsen_func.compiled_code,
                dependencies=fujsen_func.dependencies,
                context={**fujsen_func.context, **context},
                metadata=fujsen_func.metadata,
                created_at=fujsen_func.created_at,
                expires_at=fujsen_func.expires_at
            )
            
            return injected_func
            
        except Exception as e:
            logger.error(f"Context injection error: {str(e)}")
            raise Exception(f"Context injection failed: {str(e)}")
    
    def _detect_language_from_code(self, source_code: str) -> str:
        """Detect programming language from source code"""
        try:
            # Simple heuristics for language detection
            if 'function' in source_code and ('{' in source_code or '=>' in source_code):
                return 'javascript'
            elif 'def ' in source_code or 'import ' in source_code:
                return 'python'
            elif '<?php' in source_code or '$' in source_code:
                return 'php'
            elif '#!/bin/bash' in source_code or 'echo ' in source_code:
                return 'bash'
            else:
                return 'python'  # Default
                
        except Exception as e:
            logger.warning(f"Language detection failed: {str(e)}")
            return 'python'
    
    def _get_function_source(self, func: Callable) -> str:
        """Get source code of a Python function"""
        try:
            source = inspect.getsource(func)
            # Clean up indentation issues
            lines = source.split('\n')
            if lines:
                # Find the minimum indentation
                min_indent = float('inf')
                for line in lines:
                    if line.strip():  # Skip empty lines
                        indent = len(line) - len(line.lstrip())
                        min_indent = min(min_indent, indent)
                
                # Remove common indentation
                if min_indent < float('inf'):
                    cleaned_lines = []
                    for line in lines:
                        if line.strip():
                            cleaned_lines.append(line[min_indent:])
                        else:
                            cleaned_lines.append('')
                    return '\n'.join(cleaned_lines)
            
            return source
        except Exception as e:
            logger.warning(f"Could not get function source: {str(e)}")
            # Create a simple function definition
            return f"def {func.__name__}(*args, **kwargs):\n    return func(*args, **kwargs)"
    
    def _generate_cache_key(self, fujsen_func: FujsenFunction) -> str:
        """Generate cache key for FUJSEN function"""
        try:
            key_data = f"{fujsen_func.name}:{fujsen_func.language}:{fujsen_func.source_code}"
            return hashlib.md5(key_data.encode()).hexdigest()
        except Exception as e:
            logger.error(f"Cache key generation error: {str(e)}")
            return f"key_{int(time.time())}"
    
    def _cleanup_cache(self):
        """Clean up expired cache entries"""
        try:
            current_time = time.time()
            
            # Clean function cache
            expired_keys = [
                key for key, func in self.function_cache.items()
                if func.expires_at and current_time > func.expires_at
            ]
            for key in expired_keys:
                del self.function_cache[key]
            
            # Clean context cache
            expired_context_keys = [
                key for key, data in self.context_cache.items()
                if current_time - data['timestamp'] > data['ttl']
            ]
            for key in expired_context_keys:
                del self.context_cache[key]
            
            # Limit cache size
            if len(self.function_cache) > self.max_cache_size:
                # Remove oldest entries
                sorted_keys = sorted(
                    self.function_cache.keys(),
                    key=lambda k: self.function_cache[k].created_at
                )
                for key in sorted_keys[:-self.max_cache_size//2]:
                    del self.function_cache[key]
                    
        except Exception as e:
            logger.error(f"Cache cleanup error: {str(e)}")
    
    def _serialize_python(self, source_code: str) -> bytes:
        """Serialize Python code"""
        try:
            # Compile to bytecode
            code_obj = compile(source_code, '<string>', 'exec')
            return marshal.dumps(code_obj)
        except Exception as e:
            logger.error(f"Python serialization error: {str(e)}")
            raise
    
    def _deserialize_python(self, compiled_code: bytes):
        """Deserialize Python code"""
        try:
            return marshal.loads(compiled_code)
        except Exception as e:
            logger.error(f"Python deserialization error: {str(e)}")
            raise
    
    def _serialize_javascript(self, source_code: str) -> bytes:
        """Serialize JavaScript code"""
        try:
            # For JavaScript, we store the source code as bytes
            return source_code.encode('utf-8')
        except Exception as e:
            logger.error(f"JavaScript serialization error: {str(e)}")
            raise
    
    def _deserialize_javascript(self, compiled_code: bytes) -> str:
        """Deserialize JavaScript code"""
        try:
            return compiled_code.decode('utf-8')
        except Exception as e:
            logger.error(f"JavaScript deserialization error: {str(e)}")
            raise
    
    def _serialize_bash(self, source_code: str) -> bytes:
        """Serialize Bash code"""
        try:
            return source_code.encode('utf-8')
        except Exception as e:
            logger.error(f"Bash serialization error: {str(e)}")
            raise
    
    def _deserialize_bash(self, compiled_code: bytes) -> str:
        """Deserialize Bash code"""
        try:
            return compiled_code.decode('utf-8')
        except Exception as e:
            logger.error(f"Bash deserialization error: {str(e)}")
            raise
    
    def _serialize_php(self, source_code: str) -> bytes:
        """Serialize PHP code"""
        try:
            return source_code.encode('utf-8')
        except Exception as e:
            logger.error(f"PHP serialization error: {str(e)}")
            raise
    
    def _deserialize_php(self, compiled_code: bytes) -> str:
        """Deserialize PHP code"""
        try:
            return compiled_code.decode('utf-8')
        except Exception as e:
            logger.error(f"PHP deserialization error: {str(e)}")
            raise
    
    def _execute_python(self, fujsen_func: FujsenFunction, args: List[Any], kwargs: Dict[str, Any]) -> Any:
        """Execute Python function"""
        try:
            # Create execution context
            context = fujsen_func.context.copy()
            
            # Execute the code
            if fujsen_func.compiled_code:
                # Use compiled code
                code_obj = self._deserialize_python(fujsen_func.compiled_code)
                exec(code_obj, context)
            else:
                # Execute source code
                exec(fujsen_func.source_code, context)
            
            # Look for the function
            if fujsen_func.name in context:
                func = context[fujsen_func.name]
                return func(*args, **kwargs)
            else:
                # Try to find any function
                for name, obj in context.items():
                    if callable(obj) and not name.startswith('_'):
                        return obj(*args, **kwargs)
                
                raise Exception(f"Function {fujsen_func.name} not found")
                
        except Exception as e:
            logger.error(f"Python execution error: {str(e)}")
            raise
    
    def _execute_javascript(self, fujsen_func: FujsenFunction, args: List[Any], kwargs: Dict[str, Any]) -> Any:
        """Execute JavaScript function"""
        try:
            # Create temporary file
            with tempfile.NamedTemporaryFile(mode='w', suffix='.js', delete=False) as f:
                # Prepare JavaScript code
                js_code = fujsen_func.source_code
                
                # Add context as global variables
                for key, value in fujsen_func.context.items():
                    if isinstance(value, str):
                        js_code = f"const {key} = '{value}';\n" + js_code
                    else:
                        js_code = f"const {key} = {json.dumps(value)};\n" + js_code
                
                # Add execution wrapper
                js_code += f"\n\n// Execute function\nconsole.log(JSON.stringify({fujsen_func.name}({json.dumps(args)})));"
                
                f.write(js_code)
                temp_file = f.name
            
            try:
                # Execute with Node.js
                result = subprocess.run(
                    ['node', temp_file],
                    capture_output=True,
                    text=True,
                    timeout=30
                )
                
                if result.returncode == 0:
                    return json.loads(result.stdout.strip())
                else:
                    raise Exception(f"JavaScript execution failed: {result.stderr}")
                    
            finally:
                os.unlink(temp_file)
                
        except Exception as e:
            logger.error(f"JavaScript execution error: {str(e)}")
            raise
    
    def _execute_bash(self, fujsen_func: FujsenFunction, args: List[Any], kwargs: Dict[str, Any]) -> Any:
        """Execute Bash function"""
        try:
            # Create temporary file
            with tempfile.NamedTemporaryFile(mode='w', suffix='.sh', delete=False) as f:
                # Prepare bash code
                bash_code = "#!/bin/bash\n\n"
                
                # Add context as environment variables
                for key, value in fujsen_func.context.items():
                    bash_code += f"export {key}='{value}'\n"
                
                bash_code += fujsen_func.source_code
                
                # Add execution
                bash_code += f"\n\n# Execute function\n{fujsen_func.name} {' '.join(map(str, args))}"
                
                f.write(bash_code)
                temp_file = f.name
            
            try:
                # Make executable and run
                os.chmod(temp_file, 0o755)
                result = subprocess.run(
                    [temp_file],
                    capture_output=True,
                    text=True,
                    timeout=30
                )
                
                if result.returncode == 0:
                    return result.stdout.strip()
                else:
                    raise Exception(f"Bash execution failed: {result.stderr}")
                    
            finally:
                os.unlink(temp_file)
                
        except Exception as e:
            logger.error(f"Bash execution error: {str(e)}")
            raise
    
    def _execute_php(self, fujsen_func: FujsenFunction, args: List[Any], kwargs: Dict[str, Any]) -> Any:
        """Execute PHP function"""
        try:
            # Create temporary file
            with tempfile.NamedTemporaryFile(mode='w', suffix='.php', delete=False) as f:
                # Prepare PHP code
                php_code = "<?php\n\n"
                
                # Add context as variables
                for key, value in fujsen_func.context.items():
                    if isinstance(value, str):
                        php_code += f"${key} = '{value}';\n"
                    else:
                        php_code += f"${key} = {json.dumps(value)};\n"
                
                php_code += fujsen_func.source_code
                
                # Add execution
                php_code += f"\n\n// Execute function\necho json_encode({fujsen_func.name}({json.dumps(args)}));"
                
                f.write(php_code)
                temp_file = f.name
            
            try:
                # Execute with PHP
                result = subprocess.run(
                    ['php', temp_file],
                    capture_output=True,
                    text=True,
                    timeout=30
                )
                
                if result.returncode == 0:
                    return json.loads(result.stdout.strip())
                else:
                    raise Exception(f"PHP execution failed: {result.stderr}")
                    
            finally:
                os.unlink(temp_file)
                
        except Exception as e:
            logger.error(f"PHP execution error: {str(e)}")
            raise


# Global FUJSEN instance
_fujsen = Fujsen()


# Operator functions for TuskLang
def serialize_function(func: Union[Callable, str], language: str = None, 
                      context: Dict[str, Any] = None) -> str:
    """Execute @fujsen.serialize operator"""
    try:
        fujsen_func = _fujsen.serialize_function(func, language, context)
        # Convert bytes to base64 for JSON serialization
        func_dict = asdict(fujsen_func)
        if func_dict.get('compiled_code'):
            func_dict['compiled_code'] = base64.b64encode(func_dict['compiled_code']).decode('utf-8')
        return json.dumps(func_dict)
    except Exception as e:
        logger.error(f"FUJSEN serialize error: {str(e)}")
        return f"@fujsen.serialize({func}) - Error: {str(e)}"


def deserialize_function(fujsen_data: str) -> str:
    """Execute @fujsen.deserialize operator"""
    try:
        # Handle base64 encoded compiled_code
        data = json.loads(fujsen_data)
        if data.get('compiled_code'):
            data['compiled_code'] = base64.b64decode(data['compiled_code'].encode('utf-8'))
        
        fujsen_func = _fujsen.deserialize_function(data)
        # Convert back to base64 for return
        func_dict = asdict(fujsen_func)
        if func_dict.get('compiled_code'):
            func_dict['compiled_code'] = base64.b64encode(func_dict['compiled_code']).decode('utf-8')
        return json.dumps(func_dict)
    except Exception as e:
        logger.error(f"FUJSEN deserialize error: {str(e)}")
        return f"@fujsen.deserialize({fujsen_data}) - Error: {str(e)}"


def execute_function(fujsen_data: str, args: List[Any] = None, kwargs: Dict[str, Any] = None) -> Any:
    """Execute @fujsen.execute operator"""
    try:
        # Handle base64 encoded compiled_code
        data = json.loads(fujsen_data)
        if data.get('compiled_code'):
            data['compiled_code'] = base64.b64decode(data['compiled_code'].encode('utf-8'))
        
        fujsen_func = _fujsen.deserialize_function(data)
        result = _fujsen.execute_function(fujsen_func, args or [], kwargs or {})
        return result
    except Exception as e:
        logger.error(f"FUJSEN execute error: {str(e)}")
        return f"@fujsen.execute({fujsen_data}) - Error: {str(e)}"


def cache_function(fujsen_data: str, ttl: int = None) -> str:
    """Execute @fujsen.cache operator"""
    try:
        fujsen_func = _fujsen.deserialize_function(fujsen_data)
        return _fujsen.cache_function(fujsen_func, ttl)
    except Exception as e:
        logger.error(f"FUJSEN cache error: {str(e)}")
        return f"@fujsen.cache({fujsen_data}) - Error: {str(e)}"


def inject_context(fujsen_data: str, context: Dict[str, Any]) -> str:
    """Execute @fujsen.context operator"""
    try:
        fujsen_func = _fujsen.deserialize_function(fujsen_data)
        injected_func = _fujsen.inject_context(fujsen_func, context)
        return json.dumps(asdict(injected_func))
    except Exception as e:
        logger.error(f"FUJSEN context error: {str(e)}")
        return f"@fujsen.context({fujsen_data}) - Error: {str(e)}"


# Convenience functions for direct use
def fujsen_serialize(func: Union[Callable, str], language: str = None) -> FujsenFunction:
    """Serialize a function for cross-language execution"""
    return _fujsen.serialize_function(func, language)


def fujsen_execute(fujsen_func: FujsenFunction, *args, **kwargs) -> Any:
    """Execute a FUJSEN function"""
    return _fujsen.execute_function(fujsen_func, list(args), kwargs)


def fujsen_to_json(fujsen_func: FujsenFunction) -> str:
    """Convert FUJSEN function to JSON"""
    return json.dumps(asdict(fujsen_func))


def fujsen_from_json(json_data: str) -> FujsenFunction:
    """Create FUJSEN function from JSON"""
    return _fujsen.deserialize_function(json_data)


# Test functions
def test_fujsen():
    """Test FUJSEN functionality"""
    print("Testing TuskLang FUJSEN Module...")
    
    # Test Python function serialization
    print("\n1. Testing Python function serialization...")
    
    def test_python_func(x, y):
        return x + y
    
    fujsen_func = fujsen_serialize(test_python_func)
    print(f"Serialized function: {fujsen_func.name}")
    
    # Test execution
    result = fujsen_execute(fujsen_func, 5, 3)
    print(f"Execution result: {result}")
    
    # Test JavaScript function
    print("\n2. Testing JavaScript function...")
    js_code = """
    function multiply(a, b) {
        return a * b;
    }
    """
    
    js_func = fujsen_serialize(js_code, 'javascript')
    js_result = fujsen_execute(js_func, 4, 6)
    print(f"JavaScript result: {js_result}")
    
    # Test context injection
    print("\n3. Testing context injection...")
    context = {"multiplier": 10}
    injected_func = _fujsen.inject_context(fujsen_func, context)
    print(f"Injected context: {injected_func.context}")
    
    # Test caching
    print("\n4. Testing function caching...")
    cache_key = _fujsen.cache_function(fujsen_func, 60)
    print(f"Cache key: {cache_key}")
    
    print("\nFUJSEN module tests completed!")


if __name__ == '__main__':
    test_fujsen() 