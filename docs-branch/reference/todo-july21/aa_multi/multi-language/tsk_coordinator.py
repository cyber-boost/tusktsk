#!/usr/bin/env python3
"""
TuskLang Multi-Language CLI Coordinator
Coordinates commands across all language SDKs with unified interface
"""

import os
import sys
import json
import subprocess
import argparse
from pathlib import Path
from typing import Dict, List, Optional, Any
import logging

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class MultiLanguageCoordinator:
    """Coordinates TuskLang operations across multiple language SDKs"""
    
    def __init__(self):
        self.sdk_root = Path(__file__).parent.parent  # Go up one level to sdk/
        self.languages = {
            'python': {'path': 'python', 'executable': 'python3', 'main': 'tsk.py'},
            'rust': {'path': 'rust', 'executable': 'cargo', 'main': 'src/main.rs'},
            'javascript': {'path': 'javascript', 'executable': 'node', 'main': 'tsk.js'},
            'ruby': {'path': 'ruby', 'executable': 'ruby', 'main': 'tsk.rb'},
            'csharp': {'path': 'csharp', 'executable': 'dotnet', 'main': 'tsk.cs'},
            'go': {'path': 'go', 'executable': 'go', 'main': 'tsk.go'},
            'php': {'path': 'php', 'executable': 'php', 'main': 'tsk.php'},
            'java': {'path': 'java', 'executable': 'java', 'main': 'tsk.java'},
            'bash': {'path': 'bash', 'executable': 'bash', 'main': 'tsk.sh'}
        }
        self.config = self.load_config()
    
    def load_config(self) -> Dict[str, Any]:
        """Load configuration from peanu.tsk files with hierarchical override"""
        config = {}
        current_dir = Path.cwd()
        
        # Search for peanu.tsk files from current directory up to root
        while current_dir != current_dir.parent:
            config_file = current_dir / 'peanu.tsk'
            if config_file.exists():
                try:
                    with open(config_file, 'r') as f:
                        file_config = self.parse_tsk_config(f.read())
                        config.update(file_config)  # Child configs override parent
                except Exception as e:
                    logger.warning(f"Failed to load config from {config_file}: {e}")
            current_dir = current_dir.parent
        
        return config
    
    def parse_tsk_config(self, content: str) -> Dict[str, Any]:
        """Parse TuskLang configuration format"""
        config = {}
        lines = content.strip().split('\n')
        
        for line in lines:
            line = line.strip()
            if line and not line.startswith('#'):
                if '=' in line:
                    key, value = line.split('=', 1)
                    config[key.strip()] = value.strip().strip('"\'')
                elif line.startswith('[') and line.endswith(']'):
                    # Section header
                    section = line[1:-1]
                    config[section] = {}
        
        return config
    
    def get_language_path(self, language: str) -> Optional[Path]:
        """Get the path to a specific language SDK"""
        if language not in self.languages:
            return None
        return self.sdk_root / self.languages[language]['path']
    
    def execute_command(self, language: str, command: str, args: List[str] = None) -> Dict[str, Any]:
        """Execute a command in a specific language SDK"""
        if args is None:
            args = []
        
        lang_info = self.languages.get(language)
        if not lang_info:
            return {'success': False, 'error': f'Unsupported language: {language}'}
        
        lang_path = self.get_language_path(language)
        if not lang_path.exists():
            return {'success': False, 'error': f'Language SDK not found: {language}'}
        
        try:
            # Build command based on language
            cmd = self.build_language_command(language, command, args, lang_path)
            
            # Execute command
            result = subprocess.run(
                cmd,
                cwd=lang_path,
                capture_output=True,
                text=True,
                timeout=30
            )
            
            return {
                'success': result.returncode == 0,
                'stdout': result.stdout,
                'stderr': result.stderr,
                'returncode': result.returncode,
                'language': language,
                'command': command
            }
            
        except subprocess.TimeoutExpired:
            return {'success': False, 'error': f'Command timed out for {language}'}
        except Exception as e:
            return {'success': False, 'error': f'Execution error for {language}: {str(e)}'}
    
    def build_language_command(self, language: str, command: str, args: List[str], lang_path: Path) -> List[str]:
        """Build the appropriate command for each language"""
        lang_info = self.languages[language]
        
        if language == 'python':
            return [lang_info['executable'], lang_info['main'], command] + args
        elif language == 'rust':
            return [lang_info['executable'], 'run', '--bin', 'tsk', '--', command] + args
        elif language == 'javascript':
            return [lang_info['executable'], lang_info['main'], command] + args
        elif language == 'ruby':
            return [lang_info['executable'], lang_info['main'], command] + args
        elif language == 'csharp':
            return [lang_info['executable'], 'run', '--project', str(lang_path), '--', command] + args
        elif language == 'go':
            return [lang_info['executable'], 'run', lang_info['main'], command] + args
        elif language == 'php':
            return [lang_info['executable'], lang_info['main'], command] + args
        elif language == 'java':
            return [lang_info['executable'], '-cp', str(lang_path), 'Tsk', command] + args
        elif language == 'bash':
            return [lang_info['executable'], lang_info['main'], command] + args
        
        return []
    
    def execute_all_languages(self, command: str, args: List[str] = None) -> Dict[str, Dict[str, Any]]:
        """Execute a command across all available language SDKs"""
        if args is None:
            args = []
        
        results = {}
        for language in self.languages:
            results[language] = self.execute_command(language, command, args)
        
        return results
    
    def list_available_languages(self) -> List[str]:
        """List all available language SDKs"""
        available = []
        for language, info in self.languages.items():
            lang_path = self.get_language_path(language)
            if lang_path.exists():
                available.append(language)
        return available
    
    def get_sdk_status(self) -> Dict[str, Any]:
        """Get status of all language SDKs"""
        status = {}
        for language in self.languages:
            lang_path = self.get_language_path(language)
            status[language] = {
                'available': lang_path.exists(),
                'path': str(lang_path),
                'executable': self.languages[language]['executable']
            }
        return status

def main():
    """Main CLI entry point"""
    parser = argparse.ArgumentParser(description='TuskLang Multi-Language Coordinator')
    parser.add_argument('command', nargs='?', help='Command to execute')
    parser.add_argument('args', nargs='*', help='Command arguments')
    parser.add_argument('--language', '-l', help='Specific language to use')
    parser.add_argument('--all', '-a', action='store_true', help='Execute in all languages')
    parser.add_argument('--status', '-s', action='store_true', help='Show SDK status')
    parser.add_argument('--list', action='store_true', help='List available languages')
    parser.add_argument('--config', '-c', action='store_true', help='Show current configuration')
    
    args = parser.parse_args()
    
    coordinator = MultiLanguageCoordinator()
    
    if args.status:
        status = coordinator.get_sdk_status()
        print(json.dumps(status, indent=2))
        return
    
    if args.list:
        languages = coordinator.list_available_languages()
        print("Available languages:")
        for lang in languages:
            print(f"  - {lang}")
        return
    
    if args.config:
        print(json.dumps(coordinator.config, indent=2))
        return
    
    if not args.command:
        print("TuskLang Multi-Language Coordinator")
        print("Usage: tsk_coordinator.py <command> [args...]")
        print("Use --help for more information")
        return
    
    if args.all:
        results = coordinator.execute_all_languages(args.command, args.args)
        print(json.dumps(results, indent=2))
    elif args.language:
        result = coordinator.execute_command(args.language, args.command, args.args)
        print(json.dumps(result, indent=2))
    else:
        # Default to Python if no language specified
        result = coordinator.execute_command('python', args.command, args.args)
        print(json.dumps(result, indent=2))

if __name__ == '__main__':
    main() 