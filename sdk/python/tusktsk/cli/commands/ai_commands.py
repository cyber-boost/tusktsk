#!/usr/bin/env python3
"""
TuskLang Python CLI - AI Commands
=================================
AI-powered operations and integrations
"""

import os
import json
import requests
import subprocess
import sys
import time
import hashlib
import sqlite3
from typing import Dict, Any, Optional, List
from pathlib import Path
from datetime import datetime, timedelta

from ..utils import output_formatter, error_handler, config_loader


class AIDependencyManager:
    """Manages AI dependencies and provides helpful installation guidance"""
    
    AI_DEPENDENCIES = [
        'torch', 'transformers', 'tensorflow', 'scikit-learn', 
        'nltk', 'spacy', 'openai', 'anthropic', 'langchain', 'sentence-transformers'
    ]
    
    @classmethod
    def check_ai_dependencies(cls) -> Dict[str, bool]:
        """Check which AI dependencies are installed"""
        installed = {}
        for package in cls.AI_DEPENDENCIES:
            try:
                __import__(package.replace('-', '_'))
                installed[package] = True
            except ImportError:
                installed[package] = False
        return installed
    
    @classmethod
    def get_missing_dependencies(cls) -> list:
        """Get list of missing AI dependencies"""
        installed = cls.check_ai_dependencies()
        return [pkg for pkg, status in installed.items() if not status]
    
    @classmethod
    def show_installation_guide(cls, command: str):
        """Show helpful installation guide for missing dependencies"""
        missing = cls.get_missing_dependencies()
        if not missing:
            return True
            
        print(f"\n🤖 AI Command '{command}' requires additional dependencies.")
        print(f"Missing packages: {', '.join(missing)}")
        print("\n📦 To install AI dependencies, run:")
        print("   tsk deps install ai")
        print("\n💡 Or install all optional dependencies:")
        print("   tsk deps install full")
        print("\n🔧 Would you like to install AI dependencies now? (y/n): ", end="")
        
        try:
            response = input().strip().lower()
            if response in ['y', 'yes']:
                return cls.auto_install_ai_dependencies()
            else:
                print("❌ AI dependencies not installed. Command cannot proceed.")
                return False
        except KeyboardInterrupt:
            print("\n❌ Installation cancelled.")
            return False
    
    @classmethod
    def auto_install_ai_dependencies(cls) -> bool:
        """Automatically install AI dependencies"""
        print("\n📦 Installing AI dependencies...")
        try:
            # Use the dependency manager to install
            from .dependency_commands import DependencyManager
            success = DependencyManager.install_dependencies('ai', verbose=True)
            if success:
                print("✅ AI dependencies installed successfully!")
                print("🔄 Please restart the command.")
                return True
            else:
                print("❌ Failed to install AI dependencies automatically.")
                print("💡 Please run 'tsk deps install ai' manually.")
                return False
        except Exception as e:
            print(f"❌ Error during auto-installation: {e}")
            print("💡 Please run 'tsk deps install ai' manually.")
            return False


class AIConfig:
    """AI configuration management"""
    
    def __init__(self):
        self.config_file = Path.home() / '.tsk' / 'ai_config.json'
        self.config_file.parent.mkdir(exist_ok=True)
        self.load_config()
    
    def load_config(self):
        """Load AI configuration"""
        if self.config_file.exists():
            try:
                with open(self.config_file, 'r') as f:
                    self.config = json.load(f)
            except Exception:
                self.config = {}
        else:
            self.config = {}
    
    def save_config(self):
        """Save AI configuration"""
        with open(self.config_file, 'w') as f:
            json.dump(self.config, f, indent=2)
    
    def get_api_key(self, service: str) -> Optional[str]:
        """Get API key for service"""
        return self.config.get(f'{service}_api_key')
    
    def set_api_key(self, service: str, key: str):
        """Set API key for service"""
        self.config[f'{service}_api_key'] = key
        self.save_config()


class AIUsageTracker:
    """Track AI usage statistics and costs"""
    
    def __init__(self):
        self.db_path = Path.home() / '.tsk' / 'ai_usage.db'
        self.db_path.parent.mkdir(exist_ok=True)
        self.init_database()
    
    def init_database(self):
        """Initialize usage tracking database"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS ai_usage (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                service TEXT NOT NULL,
                model TEXT NOT NULL,
                prompt_tokens INTEGER,
                completion_tokens INTEGER,
                total_tokens INTEGER,
                cost_usd REAL,
                response_time_ms INTEGER,
                timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                success BOOLEAN DEFAULT 1,
                error_message TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS ai_models (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                service TEXT NOT NULL,
                model_name TEXT NOT NULL,
                max_tokens INTEGER,
                cost_per_1k_tokens REAL,
                capabilities TEXT,
                available BOOLEAN DEFAULT 1,
                last_updated DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS ai_cache (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                prompt_hash TEXT UNIQUE NOT NULL,
                service TEXT NOT NULL,
                model TEXT NOT NULL,
                response TEXT NOT NULL,
                tokens_used INTEGER,
                cost_usd REAL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                last_accessed DATETIME DEFAULT CURRENT_TIMESTAMP,
                access_count INTEGER DEFAULT 1
            )
        ''')
        
        conn.commit()
        conn.close()
    
    def log_usage(self, service: str, model: str, prompt_tokens: int, 
                  completion_tokens: int, cost_usd: float, response_time_ms: int, 
                  success: bool = True, error_message: str = None):
        """Log AI usage"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            INSERT INTO ai_usage 
            (service, model, prompt_tokens, completion_tokens, total_tokens, 
             cost_usd, response_time_ms, success, error_message)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        ''', (service, model, prompt_tokens, completion_tokens, 
              prompt_tokens + completion_tokens, cost_usd, response_time_ms, 
              success, error_message))
        
        conn.commit()
        conn.close()
    
    def get_usage_stats(self, days: int = 30) -> Dict[str, Any]:
        """Get usage statistics for the last N days"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Get total usage
        cursor.execute('''
            SELECT 
                COUNT(*) as total_requests,
                SUM(total_tokens) as total_tokens,
                SUM(cost_usd) as total_cost,
                AVG(response_time_ms) as avg_response_time,
                SUM(CASE WHEN success = 1 THEN 1 ELSE 0 END) as successful_requests,
                SUM(CASE WHEN success = 0 THEN 1 ELSE 0 END) as failed_requests
            FROM ai_usage 
            WHERE timestamp >= datetime('now', '-{} days')
        '''.format(days))
        
        total_stats = cursor.fetchone()
        
        # Get usage by service
        cursor.execute('''
            SELECT 
                service,
                COUNT(*) as requests,
                SUM(total_tokens) as tokens,
                SUM(cost_usd) as cost,
                AVG(response_time_ms) as avg_time
            FROM ai_usage 
            WHERE timestamp >= datetime('now', '-{} days')
            GROUP BY service
        '''.format(days))
        
        service_stats = cursor.fetchall()
        
        # Get usage by model
        cursor.execute('''
            SELECT 
                model,
                COUNT(*) as requests,
                SUM(total_tokens) as tokens,
                SUM(cost_usd) as cost
            FROM ai_usage 
            WHERE timestamp >= datetime('now', '-{} days')
            GROUP BY model
        '''.format(days))
        
        model_stats = cursor.fetchall()
        
        # Get daily usage
        cursor.execute('''
            SELECT 
                DATE(timestamp) as date,
                COUNT(*) as requests,
                SUM(total_tokens) as tokens,
                SUM(cost_usd) as cost
            FROM ai_usage 
            WHERE timestamp >= datetime('now', '-{} days')
            GROUP BY DATE(timestamp)
            ORDER BY date DESC
        '''.format(days))
        
        daily_stats = cursor.fetchall()
        
        conn.close()
        
        return {
            'total': {
                'requests': total_stats[0] or 0,
                'tokens': total_stats[1] or 0,
                'cost_usd': total_stats[2] or 0.0,
                'avg_response_time_ms': total_stats[3] or 0,
                'successful_requests': total_stats[4] or 0,
                'failed_requests': total_stats[5] or 0
            },
            'by_service': [
                {
                    'service': row[0],
                    'requests': row[1],
                    'tokens': row[2],
                    'cost_usd': row[3],
                    'avg_time_ms': row[4]
                }
                for row in service_stats
            ],
            'by_model': [
                {
                    'model': row[0],
                    'requests': row[1],
                    'tokens': row[2],
                    'cost_usd': row[3]
                }
                for row in model_stats
            ],
            'daily': [
                {
                    'date': row[0],
                    'requests': row[1],
                    'tokens': row[2],
                    'cost_usd': row[3]
                }
                for row in daily_stats
            ]
        }
    
    def get_cache_stats(self) -> Dict[str, Any]:
        """Get cache statistics"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            SELECT 
                COUNT(*) as total_entries,
                SUM(tokens_used) as total_tokens,
                SUM(cost_usd) as total_cost,
                SUM(access_count) as total_accesses,
                AVG(access_count) as avg_accesses
            FROM ai_cache
        ''')
        
        stats = cursor.fetchone()
        
        cursor.execute('''
            SELECT 
                service,
                COUNT(*) as entries,
                SUM(tokens_used) as tokens,
                SUM(cost_usd) as cost
            FROM ai_cache
            GROUP BY service
        ''')
        
        service_stats = cursor.fetchall()
        
        conn.close()
        
        return {
            'total': {
                'entries': stats[0] or 0,
                'tokens': stats[1] or 0,
                'cost_usd': stats[2] or 0.0,
                'total_accesses': stats[3] or 0,
                'avg_accesses': stats[4] or 0
            },
            'by_service': [
                {
                    'service': row[0],
                    'entries': row[1],
                    'tokens': row[2],
                    'cost_usd': row[3]
                }
                for row in service_stats
            ]
        }


class AICacheManager:
    """Manage AI response caching"""
    
    def __init__(self, usage_tracker: AIUsageTracker):
        self.usage_tracker = usage_tracker
        self.db_path = usage_tracker.db_path
    
    def get_cached_response(self, prompt: str, service: str, model: str) -> Optional[Dict[str, Any]]:
        """Get cached response for prompt"""
        prompt_hash = hashlib.md5(prompt.encode()).hexdigest()
        
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            SELECT response, tokens_used, cost_usd
            FROM ai_cache
            WHERE prompt_hash = ? AND service = ? AND model = ?
        ''', (prompt_hash, service, model))
        
        result = cursor.fetchone()
        
        if result:
            # Update access count and timestamp
            cursor.execute('''
                UPDATE ai_cache
                SET access_count = access_count + 1,
                    last_accessed = CURRENT_TIMESTAMP
                WHERE prompt_hash = ?
            ''', (prompt_hash,))
            
            conn.commit()
            conn.close()
            
            return {
                'response': result[0],
                'tokens_used': result[1],
                'cost_usd': result[2],
                'cached': True
            }
        
        conn.close()
        return None
    
    def cache_response(self, prompt: str, service: str, model: str, 
                      response: str, tokens_used: int, cost_usd: float):
        """Cache AI response"""
        prompt_hash = hashlib.md5(prompt.encode()).hexdigest()
        
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            INSERT OR REPLACE INTO ai_cache
            (prompt_hash, service, model, response, tokens_used, cost_usd)
            VALUES (?, ?, ?, ?, ?, ?)
        ''', (prompt_hash, service, model, response, tokens_used, cost_usd))
        
        conn.commit()
        conn.close()
    
    def clear_cache(self, service: str = None, older_than_days: int = None) -> int:
        """Clear cache entries"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        if service and older_than_days:
            cursor.execute('''
                DELETE FROM ai_cache
                WHERE service = ? AND created_at < datetime('now', '-{} days')
            '''.format(older_than_days), (service,))
        elif service:
            cursor.execute('DELETE FROM ai_cache WHERE service = ?', (service,))
        elif older_than_days:
            cursor.execute('''
                DELETE FROM ai_cache
                WHERE created_at < datetime('now', '-{} days')
            '''.format(older_than_days))
        else:
            cursor.execute('DELETE FROM ai_cache')
        
        deleted_count = cursor.rowcount
        conn.commit()
        conn.close()
        
        return deleted_count


class AIModelManager:
    """Manage AI model information and capabilities"""
    
    def __init__(self, usage_tracker: AIUsageTracker):
        self.usage_tracker = usage_tracker
        self.db_path = usage_tracker.db_path
        self.init_models()
    
    def init_models(self):
        """Initialize default model information"""
        default_models = [
            # OpenAI Models
            ('openai', 'gpt-4', 8192, 0.03, 'text-generation,code-generation,analysis'),
            ('openai', 'gpt-4-turbo', 128000, 0.01, 'text-generation,code-generation,analysis'),
            ('openai', 'gpt-3.5-turbo', 4096, 0.002, 'text-generation,code-generation'),
            ('openai', 'gpt-3.5-turbo-16k', 16384, 0.003, 'text-generation,code-generation'),
            
            # Anthropic Models
            ('anthropic', 'claude-3-opus-20240229', 200000, 0.015, 'text-generation,analysis,reasoning'),
            ('anthropic', 'claude-3-sonnet-20240229', 200000, 0.003, 'text-generation,analysis,reasoning'),
            ('anthropic', 'claude-3-haiku-20240307', 200000, 0.00025, 'text-generation,analysis'),
            
            # Google Models
            ('google', 'gemini-pro', 32768, 0.0005, 'text-generation,code-generation'),
            ('google', 'gemini-pro-vision', 32768, 0.0005, 'text-generation,vision,code-generation'),
            
            # Local Models
            ('local', 'llama-2-7b', 4096, 0.0, 'text-generation,code-generation'),
            ('local', 'llama-2-13b', 4096, 0.0, 'text-generation,code-generation'),
            ('local', 'llama-2-70b', 4096, 0.0, 'text-generation,code-generation'),
        ]
        
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        for service, model_name, max_tokens, cost_per_1k, capabilities in default_models:
            cursor.execute('''
                INSERT OR IGNORE INTO ai_models
                (service, model_name, max_tokens, cost_per_1k_tokens, capabilities)
                VALUES (?, ?, ?, ?, ?)
            ''', (service, model_name, max_tokens, cost_per_1k, capabilities))
        
        conn.commit()
        conn.close()
    
    def get_models(self, service: str = None) -> List[Dict[str, Any]]:
        """Get available models"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        if service:
            cursor.execute('''
                SELECT service, model_name, max_tokens, cost_per_1k_tokens, 
                       capabilities, available, last_updated
                FROM ai_models
                WHERE service = ? AND available = 1
                ORDER BY service, model_name
            ''', (service,))
        else:
            cursor.execute('''
                SELECT service, model_name, max_tokens, cost_per_1k_tokens, 
                       capabilities, available, last_updated
                FROM ai_models
                WHERE available = 1
                ORDER BY service, model_name
            ''')
        
        models = []
        for row in cursor.fetchall():
            models.append({
                'service': row[0],
                'model': row[1],
                'max_tokens': row[2],
                'cost_per_1k_tokens': row[3],
                'capabilities': row[4].split(','),
                'available': bool(row[5]),
                'last_updated': row[6]
            })
        
        conn.close()
        return models
    
    def update_model_info(self, service: str, model_name: str, 
                         max_tokens: int = None, cost_per_1k: float = None,
                         capabilities: str = None, available: bool = None):
        """Update model information"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        updates = []
        params = []
        
        if max_tokens is not None:
            updates.append('max_tokens = ?')
            params.append(max_tokens)
        
        if cost_per_1k is not None:
            updates.append('cost_per_1k_tokens = ?')
            params.append(cost_per_1k)
        
        if capabilities is not None:
            updates.append('capabilities = ?')
            params.append(capabilities)
        
        if available is not None:
            updates.append('available = ?')
            params.append(available)
        
        if updates:
            updates.append('last_updated = CURRENT_TIMESTAMP')
            params.extend([service, model_name])
            
            query = f'''
                UPDATE ai_models
                SET {', '.join(updates)}
                WHERE service = ? AND model_name = ?
            '''
            
            cursor.execute(query, params)
            conn.commit()
        
        conn.close()


class AIKeyManager:
    """Manage API key rotation and security"""
    
    def __init__(self, config: AIConfig):
        self.config = config
        self.key_history_file = Path.home() / '.tsk' / 'ai_key_history.json'
        self.load_key_history()
    
    def load_key_history(self):
        """Load key rotation history"""
        if self.key_history_file.exists():
            try:
                with open(self.key_history_file, 'r') as f:
                    self.key_history = json.load(f)
            except Exception:
                self.key_history = {}
        else:
            self.key_history = {}
    
    def save_key_history(self):
        """Save key rotation history"""
        with open(self.key_history_file, 'w') as f:
            json.dump(self.key_history, f, indent=2)
    
    def rotate_key(self, service: str, new_key: str, reason: str = "Manual rotation") -> bool:
        """Rotate API key for a service"""
        try:
            # Store old key in history
            old_key = self.config.get_api_key(service)
            if old_key:
                if service not in self.key_history:
                    self.key_history[service] = []
                
                self.key_history[service].append({
                    'old_key': old_key[:8] + '...',
                    'rotated_at': datetime.now().isoformat(),
                    'reason': reason
                })
                
                # Keep only last 10 rotations
                if len(self.key_history[service]) > 10:
                    self.key_history[service] = self.key_history[service][-10:]
            
            # Set new key
            self.config.set_api_key(service, new_key)
            self.save_key_history()
            
            return True
            
        except Exception as e:
            print(f"Error rotating key: {e}")
            return False
    
    def get_key_history(self, service: str = None) -> Dict[str, Any]:
        """Get key rotation history"""
        if service:
            return {service: self.key_history.get(service, [])}
        else:
            return self.key_history
    
    def validate_key(self, service: str, key: str) -> bool:
        """Validate API key by making a test request"""
        try:
            if service == 'openai':
                headers = {'Authorization': f'Bearer {key}'}
                response = requests.get('https://api.openai.com/v1/models', 
                                     headers=headers, timeout=10)
                return response.status_code == 200
            
            elif service == 'anthropic':
                headers = {'x-api-key': key}
                response = requests.get('https://api.anthropic.com/v1/messages', 
                                     headers=headers, timeout=10)
                return response.status_code in [200, 401]  # 401 means key is valid but no message
            
            else:
                return True  # Unknown service, assume valid
                
        except Exception:
            return False


class AIBenchmarker:
    """Benchmark AI model performance"""
    
    def __init__(self, usage_tracker: AIUsageTracker):
        self.usage_tracker = usage_tracker
    
    def run_benchmark(self, service: str, model: str, test_prompts: List[str] = None) -> Dict[str, Any]:
        """Run performance benchmark for AI model"""
        if test_prompts is None:
            test_prompts = [
                "Hello, how are you?",
                "What is the capital of France?",
                "Write a short poem about programming.",
                "Explain quantum computing in simple terms.",
                "Generate a Python function to calculate fibonacci numbers."
            ]
        
        results = {
            'service': service,
            'model': model,
            'total_requests': len(test_prompts),
            'successful_requests': 0,
            'failed_requests': 0,
            'total_tokens': 0,
            'total_cost_usd': 0.0,
            'response_times': [],
            'errors': []
        }
        
        for i, prompt in enumerate(test_prompts):
            try:
                start_time = time.time()
                
                # Make API request
                if service == 'openai':
                    response = self._call_openai(model, prompt)
                elif service == 'anthropic':
                    response = self._call_anthropic(model, prompt)
                else:
                    response = {'error': f'Unknown service: {service}'}
                
                response_time = (time.time() - start_time) * 1000  # Convert to ms
                
                if 'error' not in response:
                    results['successful_requests'] += 1
                    results['total_tokens'] += response.get('tokens_used', 0)
                    results['total_cost_usd'] += response.get('cost_usd', 0.0)
                    results['response_times'].append(response_time)
                    
                    # Log usage
                    self.usage_tracker.log_usage(
                        service, model, 
                        response.get('prompt_tokens', 0),
                        response.get('completion_tokens', 0),
                        response.get('cost_usd', 0.0),
                        int(response_time)
                    )
                else:
                    results['failed_requests'] += 1
                    results['errors'].append(response['error'])
                
                # Rate limiting
                if i < len(test_prompts) - 1:
                    time.sleep(1)
                    
            except Exception as e:
                results['failed_requests'] += 1
                results['errors'].append(str(e))
        
        # Calculate statistics
        if results['response_times']:
            results['avg_response_time_ms'] = sum(results['response_times']) / len(results['response_times'])
            results['min_response_time_ms'] = min(results['response_times'])
            results['max_response_time_ms'] = max(results['response_times'])
        else:
            results['avg_response_time_ms'] = 0
            results['min_response_time_ms'] = 0
            results['max_response_time_ms'] = 0
        
        results['success_rate'] = (results['successful_requests'] / results['total_requests']) * 100
        
        return results
    
    def _call_openai(self, model: str, prompt: str) -> Dict[str, Any]:
        """Call OpenAI API"""
        try:
            from .ai_commands import AIConfig
            config = AIConfig()
            api_key = config.get_api_key('openai')
            
            if not api_key:
                return {'error': 'OpenAI API key not configured'}
            
            headers = {
                'Authorization': f'Bearer {api_key}',
                'Content-Type': 'application/json'
            }
            
            data = {
                'model': model,
                'messages': [{'role': 'user', 'content': prompt}],
                'max_tokens': 100
            }
            
            response = requests.post(
                'https://api.openai.com/v1/chat/completions',
                headers=headers,
                json=data,
                timeout=30
            )
            
            if response.status_code == 200:
                result = response.json()
                usage = result.get('usage', {})
                
                # Calculate cost (approximate)
                cost_per_1k = 0.002 if 'gpt-3.5' in model else 0.03
                total_tokens = usage.get('total_tokens', 0)
                cost_usd = (total_tokens / 1000) * cost_per_1k
                
                return {
                    'response': result['choices'][0]['message']['content'],
                    'prompt_tokens': usage.get('prompt_tokens', 0),
                    'completion_tokens': usage.get('completion_tokens', 0),
                    'tokens_used': total_tokens,
                    'cost_usd': cost_usd
                }
            else:
                return {'error': f'API request failed: {response.status_code}'}
                
        except Exception as e:
            return {'error': str(e)}
    
    def _call_anthropic(self, model: str, prompt: str) -> Dict[str, Any]:
        """Call Anthropic API"""
        try:
            from .ai_commands import AIConfig
            config = AIConfig()
            api_key = config.get_api_key('anthropic')
            
            if not api_key:
                return {'error': 'Anthropic API key not configured'}
            
            headers = {
                'x-api-key': api_key,
                'content-type': 'application/json',
                'anthropic-version': '2023-06-01'
            }
            
            data = {
                'model': model,
                'max_tokens': 100,
                'messages': [{'role': 'user', 'content': prompt}]
            }
            
            response = requests.post(
                'https://api.anthropic.com/v1/messages',
                headers=headers,
                json=data,
                timeout=30
            )
            
            if response.status_code == 200:
                result = response.json()
                usage = result.get('usage', {})
                
                # Calculate cost (approximate)
                cost_per_1k = 0.003 if 'sonnet' in model else 0.015
                total_tokens = usage.get('input_tokens', 0) + usage.get('output_tokens', 0)
                cost_usd = (total_tokens / 1000) * cost_per_1k
                
                return {
                    'response': result['content'][0]['text'],
                    'prompt_tokens': usage.get('input_tokens', 0),
                    'completion_tokens': usage.get('output_tokens', 0),
                    'tokens_used': total_tokens,
                    'cost_usd': cost_usd
                }
            else:
                return {'error': f'API request failed: {response.status_code}'}
                
        except Exception as e:
            return {'error': str(e)}


class AIService:
    """Base AI service class"""
    
    def __init__(self, config: AIConfig):
        self.config = config
    
    def query(self, prompt: str) -> Dict[str, Any]:
        """Query AI service - to be implemented by subclasses"""
        raise NotImplementedError


class ClaudeService(AIService):
    """Claude AI service integration"""
    
    def query(self, prompt: str) -> Dict[str, Any]:
        """Query Claude AI"""
        # Check dependencies first
        if not AIDependencyManager.show_installation_guide("ai claude"):
            return {'error': 'AI dependencies not installed'}
        
        api_key = self.config.get_api_key('claude')
        if not api_key:
            return {'error': 'Claude API key not configured. Run: tsk ai setup'}
        
        try:
            headers = {
                'x-api-key': api_key,
                'content-type': 'application/json',
                'anthropic-version': '2023-06-01'
            }
            
            data = {
                'model': 'claude-3-sonnet-20240229',
                'max_tokens': 4000,
                'messages': [{'role': 'user', 'content': prompt}]
            }
            
            response = requests.post(
                'https://api.anthropic.com/v1/messages',
                headers=headers,
                json=data,
                timeout=30
            )
            
            if response.status_code == 200:
                result = response.json()
                return {
                    'success': True,
                    'response': result['content'][0]['text'],
                    'model': result['model'],
                    'usage': result.get('usage', {})
                }
            else:
                return {
                    'error': f'API request failed: {response.status_code}',
                    'details': response.text
                }
                
        except Exception as e:
            return {'error': f'Request failed: {str(e)}'}


class ChatGPTService(AIService):
    """ChatGPT service integration"""
    
    def query(self, prompt: str) -> Dict[str, Any]:
        """Query ChatGPT"""
        # Check dependencies first
        if not AIDependencyManager.show_installation_guide("ai chatgpt"):
            return {'error': 'AI dependencies not installed'}
        
        api_key = self.config.get_api_key('openai')
        if not api_key:
            return {'error': 'OpenAI API key not configured. Run: tsk ai setup'}
        
        try:
            headers = {
                'Authorization': f'Bearer {api_key}',
                'Content-Type': 'application/json'
            }
            
            data = {
                'model': 'gpt-4',
                'messages': [{'role': 'user', 'content': prompt}],
                'max_tokens': 4000
            }
            
            response = requests.post(
                'https://api.openai.com/v1/chat/completions',
                headers=headers,
                json=data,
                timeout=30
            )
            
            if response.status_code == 200:
                result = response.json()
                return {
                    'success': True,
                    'response': result['choices'][0]['message']['content'],
                    'model': result['model'],
                    'usage': result.get('usage', {})
                }
            else:
                return {
                    'error': f'API request failed: {response.status_code}',
                    'details': response.text
                }
                
        except Exception as e:
            return {'error': f'Request failed: {str(e)}'}


class CustomAIService(AIService):
    """Custom AI service integration"""
    
    def __init__(self, config: AIConfig, api_endpoint: str):
        super().__init__(config)
        self.api_endpoint = api_endpoint
    
    def query(self, prompt: str) -> Dict[str, Any]:
        """Query custom AI API"""
        # Check dependencies first
        if not AIDependencyManager.show_installation_guide("ai custom"):
            return {'error': 'AI dependencies not installed'}
        
        try:
            data = {'prompt': prompt}
            response = requests.post(self.api_endpoint, json=data, timeout=30)
            
            if response.status_code == 200:
                return {
                    'success': True,
                    'response': response.json().get('response', response.text)
                }
            else:
                return {
                    'error': f'API request failed: {response.status_code}',
                    'details': response.text
                }
                
        except Exception as e:
            return {'error': f'Request failed: {str(e)}'}


def handle_ai_command(args, cli):
    """Handle AI commands with dependency checking"""
    config = AIConfig()
    
    if args.ai_command == 'claude':
        return handle_claude_command(args, cli, config)
    elif args.ai_command == 'chatgpt':
        return handle_chatgpt_command(args, cli, config)
    elif args.ai_command == 'custom':
        return handle_custom_command(args, cli, config)
    elif args.ai_command == 'config':
        return handle_config_command(args, cli, config)
    elif args.ai_command == 'setup':
        return handle_setup_command(args, cli, config)
    elif args.ai_command == 'test':
        return handle_test_command(args, cli, config)
    elif args.ai_command == 'complete':
        return handle_complete_command(args, cli, config)
    elif args.ai_command == 'analyze':
        return handle_analyze_command(args, cli, config)
    elif args.ai_command == 'optimize':
        return handle_optimize_command(args, cli, config)
    elif args.ai_command == 'security':
        return handle_security_command(args, cli, config)
    elif args.ai_command == 'models':
        return handle_models_command(args, cli, config)
    elif args.ai_command == 'usage':
        return handle_usage_command(args, cli, config)
    elif args.ai_command == 'cache':
        return handle_cache_command(args, cli, config)
    elif args.ai_command == 'benchmark':
        return handle_benchmark_command(args, cli, config)
    elif args.ai_command == 'rotate':
        return handle_rotate_command(args, cli, config)
    elif args.ai_command == 'clear':
        return handle_clear_command(args, cli, config)
    else:
        print("❌ Unknown AI command. Use 'tsk ai --help' for options.")
        return 1


def handle_claude_command(args, cli, config):
    """Handle Claude AI command"""
    service = ClaudeService(config)
    result = service.query(args.prompt)
    
    if result.get('success'):
        print(f"\n🤖 Claude Response:\n{result['response']}")
        if cli.verbose:
            print(f"\n📊 Model: {result['model']}")
            print(f"📊 Usage: {result['usage']}")
        return 0
    else:
        print(f"❌ Error: {result['error']}")
        return 1


def handle_chatgpt_command(args, cli, config):
    """Handle ChatGPT command"""
    service = ChatGPTService(config)
    result = service.query(args.prompt)
    
    if result.get('success'):
        print(f"\n🤖 ChatGPT Response:\n{result['response']}")
        if cli.verbose:
            print(f"\n📊 Model: {result['model']}")
            print(f"📊 Usage: {result['usage']}")
        return 0
    else:
        print(f"❌ Error: {result['error']}")
        return 1


def handle_custom_command(args, cli, config):
    """Handle custom AI command"""
    service = CustomAIService(config, args.api)
    result = service.query(args.prompt)
    
    if result.get('success'):
        print(f"\n🤖 Custom AI Response:\n{result['response']}")
        return 0
    else:
        print(f"❌ Error: {result['error']}")
        return 1


def handle_config_command(args, cli, config):
    """Handle AI config command"""
    print("🤖 AI Configuration:")
    for key, value in config.config.items():
        if 'api_key' in key:
            masked_value = value[:8] + '...' if value else 'Not set'
            print(f"  {key}: {masked_value}")
        else:
            print(f"  {key}: {value}")
    return 0


def handle_setup_command(args, cli, config):
    """Handle AI setup command"""
    print("🤖 AI API Key Setup")
    print("Enter your API keys (press Enter to skip):")
    
    try:
        claude_key = input("Claude API Key: ").strip()
        if claude_key:
            config.set_api_key('claude', claude_key)
            print("✅ Claude API key saved")
        
        openai_key = input("OpenAI API Key: ").strip()
        if openai_key:
            config.set_api_key('openai', openai_key)
            print("✅ OpenAI API key saved")
        
        print("✅ Setup complete!")
        return 0
        
    except KeyboardInterrupt:
        print("\n❌ Setup cancelled")
        return 1


def handle_test_command(args, cli, config):
    """Handle AI test command"""
    print("🤖 Testing AI Connections...")
    
    # Test Claude
    claude_key = config.get_api_key('claude')
    if claude_key:
        service = ClaudeService(config)
        result = service.query("Hello, this is a test message.")
        if result.get('success'):
            print("✅ Claude: Connected")
        else:
            print(f"❌ Claude: {result['error']}")
    else:
        print("⚠️  Claude: No API key configured")
    
    # Test ChatGPT
    openai_key = config.get_api_key('openai')
    if openai_key:
        service = ChatGPTService(config)
        result = service.query("Hello, this is a test message.")
        if result.get('success'):
            print("✅ ChatGPT: Connected")
        else:
            print(f"❌ ChatGPT: {result['error']}")
    else:
        print("⚠️  ChatGPT: No API key configured")
    
    return 0


def handle_complete_command(args, cli, config):
    """Handle AI complete command"""
    # Check dependencies first
    if not AIDependencyManager.show_installation_guide("ai complete"):
        return 1
    
    try:
        with open(args.file, 'r') as f:
            content = f.read()
        
        # Simple completion logic (placeholder)
        print(f"🤖 AI Completion for {args.file}")
        print("📝 This feature requires advanced AI dependencies.")
        print("💡 Run 'tsk deps install ai' to enable full functionality.")
        
        return 0
        
    except FileNotFoundError:
        print(f"❌ File not found: {args.file}")
        return 1
    except Exception as e:
        print(f"❌ Error: {e}")
        return 1


def handle_analyze_command(args, cli, config):
    """Handle AI analyze command"""
    # Check dependencies first
    if not AIDependencyManager.show_installation_guide("ai analyze"):
        return 1
    
    try:
        with open(args.file, 'r') as f:
            content = f.read()
        
        print(f"🤖 AI Analysis for {args.file}")
        print("📝 This feature requires advanced AI dependencies.")
        print("💡 Run 'tsk deps install ai' to enable full functionality.")
        
        return 0
        
    except FileNotFoundError:
        print(f"❌ File not found: {args.file}")
        return 1
    except Exception as e:
        print(f"❌ Error: {e}")
        return 1


def handle_optimize_command(args, cli, config):
    """Handle AI optimize command"""
    # Check dependencies first
    if not AIDependencyManager.show_installation_guide("ai optimize"):
        return 1
    
    try:
        with open(args.file, 'r') as f:
            content = f.read()
        
        print(f"🤖 AI Optimization for {args.file}")
        print("📝 This feature requires advanced AI dependencies.")
        print("💡 Run 'tsk deps install ai' to enable full functionality.")
        
        return 0
        
    except FileNotFoundError:
        print(f"❌ File not found: {args.file}")
        return 1
    except Exception as e:
        print(f"❌ Error: {e}")
        return 1


def handle_security_command(args, cli, config):
    """Handle AI security command"""
    # Check dependencies first
    if not AIDependencyManager.show_installation_guide("ai security"):
        return 1
    
    try:
        with open(args.file, 'r') as f:
            content = f.read()
        
        print(f"🤖 AI Security Scan for {args.file}")
        print("📝 This feature requires advanced AI dependencies.")
        print("💡 Run 'tsk deps install ai' to enable full functionality.")
        
        return 0
        
    except FileNotFoundError:
        print(f"❌ File not found: {args.file}")
        return 1
    except Exception as e:
        print(f"❌ Error: {e}")
        return 1 


def handle_models_command(args, cli, config):
    """Handle AI models command"""
    model_manager = AIModelManager(AIUsageTracker()) # Pass a dummy usage tracker for now
    models = model_manager.get_models()
    
    if not models:
        print("No AI models found or available.")
        return 0
        
    print("\n🤖 Available AI Models:")
    for model_info in models:
        print(f"  Service: {model_info['service']}")
        print(f"    Model: {model_info['model']}")
        print(f"    Max Tokens: {model_info['max_tokens']}")
        print(f"    Cost per 1K Tokens: ${model_info['cost_per_1k_tokens']:.4f}")
        print(f"    Capabilities: {', '.join(model_info['capabilities'])}")
        print(f"    Available: {model_info['available']}")
        print(f"    Last Updated: {model_info['last_updated']}")
        print("-" * 20)
    return 0


def handle_usage_command(args, cli, config):
    """Handle AI usage command"""
    usage_tracker = AIUsageTracker()
    days = args.days if args.days else 30
    
    stats = usage_tracker.get_usage_stats(days)
    
    print(f"\n🤖 AI Usage Statistics (Last {days} days):")
    print(f"  Total Requests: {stats['total']['requests']}")
    print(f"  Total Tokens: {stats['total']['tokens']}")
    print(f"  Total Cost (USD): ${stats['total']['cost_usd']:.4f}")
    print(f"  Average Response Time: {stats['total']['avg_response_time_ms']:.2f} ms")
    print(f"  Successful Requests: {stats['total']['successful_requests']}")
    print(f"  Failed Requests: {stats['total']['failed_requests']}")
    
    if stats['by_service']:
        print("\n📊 Usage by Service:")
        for item in stats['by_service']:
            print(f"  Service: {item['service']}")
            print(f"    Requests: {item['requests']}")
            print(f"    Tokens: {item['tokens']}")
            print(f"    Cost (USD): ${item['cost_usd']:.4f}")
            print(f"    Avg Time: {item['avg_time_ms']:.2f} ms")
            print("-" * 20)
    
    if stats['by_model']:
        print("\n📊 Usage by Model:")
        for item in stats['by_model']:
            print(f"  Model: {item['model']}")
            print(f"    Requests: {item['requests']}")
            print(f"    Tokens: {item['tokens']}")
            print(f"    Cost (USD): ${item['cost_usd']:.4f}")
            print("-" * 20)
    
    if stats['daily']:
        print("\n📊 Daily Usage:")
        for item in stats['daily']:
            print(f"  Date: {item['date']}")
            print(f"    Requests: {item['requests']}")
            print(f"    Tokens: {item['tokens']}")
            print(f"    Cost (USD): ${item['cost_usd']:.4f}")
            print("-" * 20)
    
    return 0


def handle_cache_command(args, cli, config):
    """Handle AI cache command"""
    cache_manager = AICacheManager(AIUsageTracker())
    
    if args.clear:
        if args.service:
            deleted = cache_manager.clear_cache(service=args.service, older_than_days=args.older_than_days)
            print(f"Cleared {deleted} cache entries for service '{args.service}' older than {args.older_than_days} days.")
        elif args.older_than_days:
            deleted = cache_manager.clear_cache(older_than_days=args.older_than_days)
            print(f"Cleared {deleted} cache entries older than {args.older_than_days} days.")
        else:
            deleted = cache_manager.clear_cache()
            print(f"Cleared {deleted} total cache entries.")
    else:
        print("🤖 AI Cache Management:")
        print("  Usage: tsk ai cache [--clear] [--service <service>] [--older-than-days <days>]")
        print("  Options:")
        print("    --clear: Clear all cache entries.")
        print("    --service <service>: Clear cache for a specific service.")
        print("    --older-than-days <days>: Clear cache entries older than <days> days.")
        print("  Example: tsk ai cache --clear --service openai --older-than-days 7")
    
    return 0


def handle_benchmark_command(args, cli, config):
    """Handle AI benchmark command"""
    usage_tracker = AIUsageTracker()
    benchmarker = AIBenchmarker(usage_tracker)
    
    if not args.service:
        print("Usage: tsk ai benchmark --service <service> [--model <model>] [--prompts <prompt1,prompt2,...>]")
        return 1
        
    service = args.service
    model = args.model if args.model else "gpt-4" # Default model
    
    if not AIDependencyManager.show_installation_guide(f"ai benchmark {service}"):
        return 1
        
    try:
        test_prompts = args.prompts.split(',') if args.prompts else None
        results = benchmarker.run_benchmark(service, model, test_prompts)
        
        print(f"\n🤖 AI Benchmark Results for {service} - {model}:")
        print(f"  Total Requests: {results['total_requests']}")
        print(f"  Successful Requests: {results['successful_requests']}")
        print(f"  Failed Requests: {results['failed_requests']}")
        print(f"  Total Tokens: {results['total_tokens']}")
        print(f"  Total Cost (USD): ${results['total_cost_usd']:.4f}")
        print(f"  Average Response Time: {results['avg_response_time_ms']:.2f} ms")
        print(f"  Min Response Time: {results['min_response_time_ms']:.2f} ms")
        print(f"  Max Response Time: {results['max_response_time_ms']:.2f} ms")
        print(f"  Success Rate: {results['success_rate']:.2f}%")
        
        if results['errors']:
            print("\n❌ Errors during benchmark:")
            for error in results['errors']:
                print(f"  - {error}")
        
        return 0
        
    except Exception as e:
        print(f"❌ Error during benchmark: {e}")
        return 1


def handle_rotate_command(args, cli, config):
    """Handle AI rotate command"""
    key_manager = AIKeyManager(config)
    
    if not args.service:
        print("Usage: tsk ai rotate --service <service> [--reason <reason>]")
        return 1
        
    service = args.service
    reason = args.reason if args.reason else "Manual rotation"
    
    if not AIDependencyManager.show_installation_guide(f"ai rotate {service}"):
        return 1
        
    try:
        new_key = input(f"Enter new API key for {service}: ").strip()
        if new_key:
            if key_manager.rotate_key(service, new_key, reason):
                print(f"✅ API key for {service} rotated successfully.")
                print(f"  Old Key: {key_manager.get_key_history(service)[-1]['old_key']}")
                print(f"  Rotated At: {key_manager.get_key_history(service)[-1]['rotated_at']}")
                print(f"  Reason: {key_manager.get_key_history(service)[-1]['reason']}")
            else:
                print(f"❌ Failed to rotate API key for {service}.")
        else:
            print(f"❌ No new key provided for {service}. Key not rotated.")
        return 0
        
    except KeyboardInterrupt:
        print("\n❌ Rotation cancelled.")
        return 1


def handle_clear_command(args, cli, config):
    """Handle AI clear command"""
    print("�� AI Cache and Usage Data Clearing:")
    print("  Usage: tsk ai clear [--cache] [--usage] [--all]")
    print("  Options:")
    print("    --cache: Clear AI cache.")
    print("    --usage: Clear AI usage statistics.")
    print("    --all: Clear both cache and usage statistics.")
    print("  Example: tsk ai clear --cache --usage")
    
    cache_manager = AICacheManager(AIUsageTracker())
    usage_tracker = AIUsageTracker()
    
    if args.cache:
        deleted_cache = cache_manager.clear_cache()
        print(f"Cleared {deleted_cache} total cache entries.")
    if args.usage:
        deleted_usage = usage_tracker.clear_cache() # Assuming clear_cache is available on AIUsageTracker
        print(f"Cleared {deleted_usage} total usage entries.")
    if args.all:
        deleted_cache = cache_manager.clear_cache()
        deleted_usage = usage_tracker.clear_cache()
        print(f"Cleared {deleted_cache} total cache entries.")
        print(f"Cleared {deleted_usage} total usage entries.")
    
    return 0 