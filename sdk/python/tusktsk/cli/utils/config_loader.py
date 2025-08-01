#!/usr/bin/env python3
"""
Config Loader for TuskLang Python CLI
=====================================
Handles configuration loading and management
"""

import os
from pathlib import Path
from typing import Dict, Any, Optional, List

from ...tsk import TSK, TSKParser
from ...tsk_enhanced import TuskLangEnhanced
from ...peanut_config import PeanutConfig


class ConfigLoader:
    """Handles configuration loading and management"""
    
    def __init__(self, config_path: Optional[str] = None):
        self.config_path = config_path
        self.peanut_config = PeanutConfig()
        self.enhanced_parser = TuskLangEnhanced()
    
    def load_config(self, directory: Optional[str] = None) -> Dict[str, Any]:
        """Load configuration with hierarchy support"""
        if directory is None:
            directory = os.getcwd()
        
        # Use specified config path if provided
        if self.config_path:
            return self._load_specific_config(self.config_path)
        
        # Try to load from hierarchy
        return self.peanut_config.load(directory)
    
    def _load_specific_config(self, config_path: str) -> Dict[str, Any]:
        """Load configuration from specific path"""
        config_file = Path(config_path)
        
        if not config_file.exists():
            raise FileNotFoundError(f"Configuration file not found: {config_path}")
        
        if config_file.suffix == '.pnt':
            # Load binary config
            return self.peanut_config.load_binary(str(config_file))
        elif config_file.suffix in ['.tsk', '.peanuts']:
            # Load text config
            with open(config_file, 'r') as f:
                content = f.read()
            return self.peanut_config.parse_text_config(content)
        else:
            raise ValueError(f"Unsupported configuration file format: {config_file.suffix}")
    
    def get_value(self, key_path: str, default: Any = None, directory: Optional[str] = None) -> Any:
        """Get configuration value by path"""
        config = self.load_config(directory)
        keys = key_path.split('.')
        value = config
        
        for key in keys:
            if isinstance(value, dict) and key in value:
                value = value[key]
            else:
                return default
        
        return value
    
    def validate_config(self, directory: Optional[str] = None) -> Dict[str, Any]:
        """Validate configuration"""
        try:
            config = self.load_config(directory)
            
            # Basic validation
            validation_result = {
                'valid': True,
                'errors': [],
                'warnings': [],
                'sections': list(config.keys()),
                'total_keys': self._count_keys(config)
            }
            
            # Check for common issues
            if 'database' in config:
                db_config = config['database']
                if 'default' not in db_config:
                    validation_result['warnings'].append("No default database specified")
                
                # Validate database configurations
                for db_type, db_settings in db_config.items():
                    if db_type != 'default':
                        if not isinstance(db_settings, dict):
                            validation_result['errors'].append(f"Invalid {db_type} configuration")
            
            if 'server' in config:
                server_config = config['server']
                if 'port' in server_config:
                    try:
                        port = int(server_config['port'])
                        if port < 1 or port > 65535:
                            validation_result['errors'].append("Invalid server port")
                    except (ValueError, TypeError):
                        validation_result['errors'].append("Server port must be a number")
            
            # Update validation status
            if validation_result['errors']:
                validation_result['valid'] = False
            
            return validation_result
            
        except Exception as e:
            return {
                'valid': False,
                'errors': [str(e)],
                'warnings': [],
                'sections': [],
                'total_keys': 0
            }
    
    def compile_configs(self, directory: Optional[str] = None) -> Dict[str, Any]:
        """Compile all configuration files to binary format"""
        if directory is None:
            directory = os.getcwd()
        
        result = {
            'compiled': [],
            'errors': [],
            'skipped': []
        }
        
        # Find all config files
        config_files = self._find_config_files(directory)
        
        for config_file in config_files:
            try:
                if config_file.suffix in ['.tsk', '.peanuts']:
                    # Compile to binary
                    binary_path = config_file.with_suffix('.pnt')
                    
                    with open(config_file, 'r') as f:
                        content = f.read()
                    
                    config_data = self.peanut_config.parse_text_config(content)
                    self.peanut_config.compile_to_binary(config_data, str(binary_path))
                    
                    result['compiled'].append(str(config_file))
                else:
                    result['skipped'].append(str(config_file))
                    
            except Exception as e:
                result['errors'].append(f"{config_file}: {str(e)}")
        
        return result
    
    def get_config_stats(self, directory: Optional[str] = None) -> Dict[str, Any]:
        """Get configuration statistics"""
        config = self.load_config(directory)
        
        return {
            'total_sections': len(config),
            'total_keys': self._count_keys(config),
            'file_size': self._get_config_file_size(directory),
            'load_time': self._measure_load_time(directory),
            'hierarchy_depth': self._get_hierarchy_depth(directory)
        }
    
    def _find_config_files(self, directory: str) -> List[Path]:
        """Find all configuration files in directory"""
        config_files = []
        dir_path = Path(directory)
        
        for pattern in ['*.tsk', '*.peanuts', '*.pnt']:
            config_files.extend(dir_path.glob(pattern))
        
        return config_files
    
    def _count_keys(self, config: Dict[str, Any]) -> int:
        """Count total number of keys in configuration"""
        count = 0
        for key, value in config.items():
            count += 1
            if isinstance(value, dict):
                count += self._count_keys(value)
        return count
    
    def _get_config_file_size(self, directory: Optional[str] = None) -> int:
        """Get total size of configuration files"""
        if directory is None:
            directory = os.getcwd()
        
        total_size = 0
        config_files = self._find_config_files(directory)
        
        for config_file in config_files:
            if config_file.exists():
                total_size += config_file.stat().st_size
        
        return total_size
    
    def _measure_load_time(self, directory: Optional[str] = None) -> float:
        """Measure configuration load time"""
        import time
        
        start_time = time.time()
        self.load_config(directory)
        return time.time() - start_time
    
    def _get_hierarchy_depth(self, directory: Optional[str] = None) -> int:
        """Get configuration hierarchy depth"""
        if directory is None:
            directory = os.getcwd()
        
        hierarchy = self.peanut_config.find_config_hierarchy(directory)
        return len(hierarchy)
    
    def clear_cache(self, directory: Optional[str] = None) -> bool:
        """Clear configuration cache"""
        try:
            cache_key = os.path.abspath(directory or os.getcwd())
            if cache_key in self.peanut_config.cache:
                del self.peanut_config.cache[cache_key]
            return True
        except Exception:
            return False
    
    def get_config(self, directory: Optional[str] = None) -> Dict[str, Any]:
        """Get configuration data"""
        return self.load_config(directory)
    
    def set_value(self, key_path: str, value: Any, directory: Optional[str] = None) -> bool:
        """Set configuration value by path"""
        try:
            config = self.load_config(directory)
            keys = key_path.split('.')
            current = config
            
            # Navigate to the parent of the target key
            for key in keys[:-1]:
                if key not in current:
                    current[key] = {}
                current = current[key]
            
            # Set the value
            current[keys[-1]] = value
            
            # Save the configuration
            return self._save_config(config, directory)
        except Exception:
            return False
    
    def import_config(self, import_data: Dict[str, Any], directory: Optional[str] = None) -> bool:
        """Import configuration data"""
        try:
            config = self.load_config(directory)
            
            # Merge configurations
            merged_config = self._merge_configs(config, import_data)
            
            # Save the merged configuration
            return self._save_config(merged_config, directory)
        except Exception:
            return False
    
    def _save_config(self, config: Dict[str, Any], directory: Optional[str] = None) -> bool:
        """Save configuration to file"""
        try:
            if directory is None:
                directory = os.getcwd()
            
            # Find the appropriate config file
            config_files = self._find_config_files(directory)
            if config_files:
                config_file = config_files[0]
                
                # Save based on file type
                if config_file.suffix == '.tsk':
                    with open(config_file, 'w') as f:
                        f.write(self._dict_to_tsk(config))
                elif config_file.suffix == '.pnt':
                    # Save as binary
                    self.peanut_config.save_binary(config, str(config_file))
                else:
                    # Save as JSON
                    import json
                    with open(config_file, 'w') as f:
                        json.dump(config, f, indent=2)
                
                return True
            else:
                # Create new config file
                new_config_file = Path(directory) / 'peanu.tsk'
                with open(new_config_file, 'w') as f:
                    f.write(self._dict_to_tsk(config))
                return True
        except Exception:
            return False
    
    def _dict_to_tsk(self, config: Dict[str, Any]) -> str:
        """Convert dictionary to TSK format"""
        lines = []
        for key, value in config.items():
            if isinstance(value, dict):
                lines.append(f"{key} {{")
                lines.extend(self._dict_to_tsk_lines(value, 1))
                lines.append("}")
            else:
                lines.append(f"{key} = {value}")
        return "\n".join(lines)
    
    def _dict_to_tsk_lines(self, config: Dict[str, Any], indent: int) -> List[str]:
        """Convert dictionary to TSK format lines with indentation"""
        lines = []
        for key, value in config.items():
            indent_str = "  " * indent
            if isinstance(value, dict):
                lines.append(f"{indent_str}{key} {{")
                lines.extend(self._dict_to_tsk_lines(value, indent + 1))
                lines.append(f"{indent_str}}}")
            else:
                lines.append(f"{indent_str}{key} = {value}")
        return lines
    
    def _merge_configs(self, base_config: Dict[str, Any], merge_config: Dict[str, Any]) -> Dict[str, Any]:
        """Merge two configuration dictionaries"""
        result = base_config.copy()
        
        for key, value in merge_config.items():
            if key in result and isinstance(result[key], dict) and isinstance(value, dict):
                result[key] = self._merge_configs(result[key], value)
            else:
                result[key] = value
        
        return result


# Global config loader instance
config_loader = ConfigLoader() 