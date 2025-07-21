#!/usr/bin/env python3
"""
TuskLang Cross-Language Configuration Manager
Manages configuration across all language SDKs using peanu.tsk files
"""

import os
import json
import yaml
from pathlib import Path
from typing import Dict, Any, Optional, List
import logging

logger = logging.getLogger(__name__)

class ConfigManager:
    """Manages configuration across all TuskLang language SDKs"""
    
    def __init__(self, sdk_root: Path = None):
        if sdk_root is None:
            self.sdk_root = Path(__file__).parent.parent  # Go up one level to sdk/
        else:
            self.sdk_root = sdk_root
        self.config_files = ['peanu.tsk', 'peanu.pnt', '.peanuts']
        self.languages = ['python', 'rust', 'javascript', 'ruby', 'csharp', 'go', 'php', 'java', 'bash']
    
    def find_config_files(self, start_path: Path = None) -> List[Path]:
        """Find all configuration files from start_path up to root"""
        if start_path is None:
            start_path = Path.cwd()
        
        config_files = []
        current = start_path
        
        while current != current.parent:
            for config_name in self.config_files:
                config_file = current / config_name
                if config_file.exists():
                    config_files.append(config_file)
            current = current.parent
        
        return config_files
    
    def load_hierarchical_config(self, start_path: Path = None) -> Dict[str, Any]:
        """Load configuration with hierarchical override support"""
        config_files = self.find_config_files(start_path)
        config = {}
        
        # Load from root to child (child configs override parent)
        for config_file in reversed(config_files):
            try:
                file_config = self.parse_config_file(config_file)
                config.update(file_config)
                logger.info(f"Loaded config from {config_file}")
            except Exception as e:
                logger.warning(f"Failed to load config from {config_file}: {e}")
        
        return config
    
    def parse_config_file(self, config_file: Path) -> Dict[str, Any]:
        """Parse configuration file based on its type"""
        content = config_file.read_text(encoding='utf-8')
        
        if config_file.suffix == '.tsk':
            return self.parse_tsk_config(content)
        elif config_file.suffix == '.pnt':
            return self.parse_pnt_config(content)
        elif config_file.name == '.peanuts':
            return self.parse_peanuts_config(content)
        else:
            # Try to parse as JSON or YAML
            try:
                return json.loads(content)
            except json.JSONDecodeError:
                try:
                    return yaml.safe_load(content)
                except yaml.YAMLError:
                    return self.parse_tsk_config(content)  # Default to TSK format
    
    def parse_tsk_config(self, content: str) -> Dict[str, Any]:
        """Parse TuskLang configuration format"""
        config = {}
        current_section = None
        
        for line_num, line in enumerate(content.split('\n'), 1):
            line = line.strip()
            
            # Skip empty lines and comments
            if not line or line.startswith('#'):
                continue
            
            # Section header
            if line.startswith('[') and line.endswith(']'):
                current_section = line[1:-1]
                config[current_section] = {}
                continue
            
            # Key-value pair
            if '=' in line:
                key, value = line.split('=', 1)
                key = key.strip()
                value = value.strip().strip('"\'')
                
                if current_section:
                    config[current_section][key] = value
                else:
                    config[key] = value
        
        return config
    
    def parse_pnt_config(self, content: str) -> Dict[str, Any]:
        """Parse Peanut configuration format"""
        config = {}
        
        for line in content.split('\n'):
            line = line.strip()
            if line and not line.startswith('#'):
                if ':' in line:
                    key, value = line.split(':', 1)
                    config[key.strip()] = value.strip()
        
        return config
    
    def parse_peanuts_config(self, content: str) -> Dict[str, Any]:
        """Parse .peanuts configuration format"""
        try:
            return json.loads(content)
        except json.JSONDecodeError:
            return self.parse_tsk_config(content)
    
    def get_language_config(self, language: str, config: Dict[str, Any]) -> Dict[str, Any]:
        """Get configuration specific to a language"""
        lang_config = {}
        
        # Language-specific section
        if language in config:
            lang_config.update(config[language])
        
        # Global configuration
        for key, value in config.items():
            if not isinstance(value, dict) and not key.startswith('_'):
                lang_config[key] = value
        
        return lang_config
    
    def validate_config(self, config: Dict[str, Any]) -> List[str]:
        """Validate configuration and return any errors"""
        errors = []
        
        # Check for required fields
        required_fields = ['version', 'environment']
        for field in required_fields:
            if field not in config:
                errors.append(f"Missing required field: {field}")
        
        # Validate language configurations
        for language in self.languages:
            if language in config:
                lang_config = config[language]
                if isinstance(lang_config, dict):
                    # Check for language-specific required fields
                    if 'executable' in lang_config and not self.check_executable(lang_config['executable']):
                        errors.append(f"Invalid executable for {language}: {lang_config['executable']}")
        
        return errors
    
    def check_executable(self, executable: str) -> bool:
        """Check if an executable exists in PATH"""
        import shutil
        return shutil.which(executable) is not None
    
    def generate_config_template(self, language: str = None) -> str:
        """Generate a configuration template"""
        if language:
            template = f"""[{language}]
executable = {self.get_default_executable(language)}
version = 1.0.0
environment = development

[global]
version = 1.0.0
environment = development
debug = false
log_level = info
"""
        else:
            template = """[global]
version = 1.0.0
environment = development
debug = false
log_level = info

[python]
executable = python3
version = 1.0.0

[rust]
executable = cargo
version = 1.0.0

[javascript]
executable = node
version = 1.0.0

[ruby]
executable = ruby
version = 1.0.0

[csharp]
executable = dotnet
version = 1.0.0

[go]
executable = go
version = 1.0.0

[php]
executable = php
version = 1.0.0

[java]
executable = java
version = 1.0.0

[bash]
executable = bash
version = 1.0.0
"""
        
        return template
    
    def get_default_executable(self, language: str) -> str:
        """Get default executable for a language"""
        defaults = {
            'python': 'python3',
            'rust': 'cargo',
            'javascript': 'node',
            'ruby': 'ruby',
            'csharp': 'dotnet',
            'go': 'go',
            'php': 'php',
            'java': 'java',
            'bash': 'bash'
        }
        return defaults.get(language, 'unknown')
    
    def save_config(self, config: Dict[str, Any], file_path: Path, format: str = 'tsk') -> bool:
        """Save configuration to file"""
        try:
            if format == 'tsk':
                content = self.config_to_tsk(config)
            elif format == 'json':
                content = json.dumps(config, indent=2)
            elif format == 'yaml':
                content = yaml.dump(config, default_flow_style=False)
            else:
                raise ValueError(f"Unsupported format: {format}")
            
            file_path.write_text(content, encoding='utf-8')
            return True
        except Exception as e:
            logger.error(f"Failed to save config to {file_path}: {e}")
            return False
    
    def config_to_tsk(self, config: Dict[str, Any]) -> str:
        """Convert configuration dictionary to TSK format"""
        lines = []
        
        for key, value in config.items():
            if isinstance(value, dict):
                lines.append(f"[{key}]")
                for sub_key, sub_value in value.items():
                    lines.append(f"{sub_key} = {sub_value}")
                lines.append("")
            else:
                lines.append(f"{key} = {value}")
        
        return "\n".join(lines)
    
    def merge_configs(self, base_config: Dict[str, Any], override_config: Dict[str, Any]) -> Dict[str, Any]:
        """Merge two configurations with override support"""
        merged = base_config.copy()
        
        for key, value in override_config.items():
            if key in merged and isinstance(merged[key], dict) and isinstance(value, dict):
                merged[key] = self.merge_configs(merged[key], value)
            else:
                merged[key] = value
        
        return merged

def main():
    """CLI for configuration management"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang Configuration Manager')
    parser.add_argument('--load', action='store_true', help='Load and display current configuration')
    parser.add_argument('--validate', action='store_true', help='Validate current configuration')
    parser.add_argument('--template', help='Generate template for specific language')
    parser.add_argument('--save', help='Save configuration to file')
    parser.add_argument('--format', choices=['tsk', 'json', 'yaml'], default='tsk', help='Output format')
    
    args = parser.parse_args()
    
    sdk_root = Path(__file__).parent
    config_manager = ConfigManager(sdk_root)
    
    if args.load:
        config = config_manager.load_hierarchical_config()
        print(json.dumps(config, indent=2))
    
    elif args.validate:
        config = config_manager.load_hierarchical_config()
        errors = config_manager.validate_config(config)
        if errors:
            print("Configuration errors:")
            for error in errors:
                print(f"  - {error}")
        else:
            print("Configuration is valid")
    
    elif args.template:
        template = config_manager.generate_config_template(args.template)
        print(template)
    
    elif args.save:
        config = config_manager.load_hierarchical_config()
        file_path = Path(args.save)
        if config_manager.save_config(config, file_path, args.format):
            print(f"Configuration saved to {file_path}")
        else:
            print("Failed to save configuration")
    
    else:
        # Show help
        parser.print_help()

if __name__ == '__main__':
    main() 