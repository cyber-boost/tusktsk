#!/usr/bin/env python3
"""
TuskLang Multi-Language Package Manager
Manages dependencies and packages across all language SDKs
"""

import os
import json
import subprocess
import shutil
from pathlib import Path
from typing import Dict, List, Optional, Any, Tuple
import logging
import tempfile
import hashlib

logger = logging.getLogger(__name__)

class MultiLanguagePackageManager:
    """Manages packages and dependencies across all TuskLang language SDKs"""
    
    def __init__(self, sdk_root: Path = None):
        if sdk_root is None:
            self.sdk_root = Path(__file__).parent.parent  # Go up one level to sdk/
        else:
            self.sdk_root = sdk_root
        self.languages = {
            'python': {
                'package_files': ['requirements.txt', 'pyproject.toml', 'setup.py'],
                'install_cmd': ['pip', 'install'],
                'uninstall_cmd': ['pip', 'uninstall'],
                'list_cmd': ['pip', 'list'],
                'update_cmd': ['pip', 'install', '--upgrade']
            },
            'rust': {
                'package_files': ['Cargo.toml'],
                'install_cmd': ['cargo', 'build'],
                'uninstall_cmd': ['cargo', 'remove'],
                'list_cmd': ['cargo', 'tree'],
                'update_cmd': ['cargo', 'update']
            },
            'javascript': {
                'package_files': ['package.json'],
                'install_cmd': ['npm', 'install'],
                'uninstall_cmd': ['npm', 'uninstall'],
                'list_cmd': ['npm', 'list'],
                'update_cmd': ['npm', 'update']
            },
            'ruby': {
                'package_files': ['Gemfile', 'gemspec'],
                'install_cmd': ['bundle', 'install'],
                'uninstall_cmd': ['gem', 'uninstall'],
                'list_cmd': ['bundle', 'list'],
                'update_cmd': ['bundle', 'update']
            },
            'csharp': {
                'package_files': ['*.csproj', 'packages.config'],
                'install_cmd': ['dotnet', 'restore'],
                'uninstall_cmd': ['dotnet', 'remove', 'package'],
                'list_cmd': ['dotnet', 'list', 'package'],
                'update_cmd': ['dotnet', 'add', 'package']
            },
            'go': {
                'package_files': ['go.mod'],
                'install_cmd': ['go', 'mod', 'tidy'],
                'uninstall_cmd': ['go', 'mod', 'edit', '-droprequire'],
                'list_cmd': ['go', 'list', '-m', 'all'],
                'update_cmd': ['go', 'get', '-u']
            },
            'php': {
                'package_files': ['composer.json'],
                'install_cmd': ['composer', 'install'],
                'uninstall_cmd': ['composer', 'remove'],
                'list_cmd': ['composer', 'show'],
                'update_cmd': ['composer', 'update']
            },
            'java': {
                'package_files': ['pom.xml', 'build.gradle'],
                'install_cmd': ['mvn', 'install'],
                'uninstall_cmd': ['mvn', 'dependency:remove'],
                'list_cmd': ['mvn', 'dependency:tree'],
                'update_cmd': ['mvn', 'versions:use-latest-versions']
            },
            'bash': {
                'package_files': ['requirements.sh'],
                'install_cmd': ['bash', 'install.sh'],
                'uninstall_cmd': ['bash', 'uninstall.sh'],
                'list_cmd': ['bash', 'list.sh'],
                'update_cmd': ['bash', 'update.sh']
            }
        }
    
    def get_language_path(self, language: str) -> Optional[Path]:
        """Get the path to a specific language SDK"""
        if language not in self.languages:
            return None
        return self.sdk_root / language
    
    def find_package_files(self, language: str) -> List[Path]:
        """Find package files for a specific language"""
        lang_path = self.get_language_path(language)
        if not lang_path or not lang_path.exists():
            return []
        
        package_files = []
        lang_info = self.languages[language]
        
        for pattern in lang_info['package_files']:
            if '*' in pattern:
                # Handle glob patterns
                for file_path in lang_path.glob(pattern):
                    package_files.append(file_path)
            else:
                # Handle specific files
                file_path = lang_path / pattern
                if file_path.exists():
                    package_files.append(file_path)
        
        return package_files
    
    def install_dependencies(self, language: str, packages: List[str] = None) -> Dict[str, Any]:
        """Install dependencies for a specific language"""
        lang_path = self.get_language_path(language)
        if not lang_path or not lang_path.exists():
            return {'success': False, 'error': f'Language SDK not found: {language}'}
        
        lang_info = self.languages[language]
        
        try:
            if packages:
                # Install specific packages
                cmd = lang_info['install_cmd'] + packages
            else:
                # Install all dependencies from package files
                cmd = lang_info['install_cmd']
            
            result = subprocess.run(
                cmd,
                cwd=lang_path,
                capture_output=True,
                text=True,
                timeout=300  # 5 minutes timeout
            )
            
            return {
                'success': result.returncode == 0,
                'stdout': result.stdout,
                'stderr': result.stderr,
                'returncode': result.returncode,
                'language': language,
                'command': ' '.join(cmd)
            }
            
        except subprocess.TimeoutExpired:
            return {'success': False, 'error': f'Installation timed out for {language}'}
        except Exception as e:
            return {'success': False, 'error': f'Installation error for {language}: {str(e)}'}
    
    def uninstall_package(self, language: str, package: str) -> Dict[str, Any]:
        """Uninstall a package from a specific language"""
        lang_path = self.get_language_path(language)
        if not lang_path or not lang_path.exists():
            return {'success': False, 'error': f'Language SDK not found: {language}'}
        
        lang_info = self.languages[language]
        
        try:
            cmd = lang_info['uninstall_cmd'] + [package]
            
            result = subprocess.run(
                cmd,
                cwd=lang_path,
                capture_output=True,
                text=True,
                timeout=60
            )
            
            return {
                'success': result.returncode == 0,
                'stdout': result.stdout,
                'stderr': result.stderr,
                'returncode': result.returncode,
                'language': language,
                'package': package
            }
            
        except subprocess.TimeoutExpired:
            return {'success': False, 'error': f'Uninstallation timed out for {language}'}
        except Exception as e:
            return {'success': False, 'error': f'Uninstallation error for {language}: {str(e)}'}
    
    def list_packages(self, language: str) -> Dict[str, Any]:
        """List installed packages for a specific language"""
        lang_path = self.get_language_path(language)
        if not lang_path or not lang_path.exists():
            return {'success': False, 'error': f'Language SDK not found: {language}'}
        
        lang_info = self.languages[language]
        
        try:
            cmd = lang_info['list_cmd']
            
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
                'language': language
            }
            
        except subprocess.TimeoutExpired:
            return {'success': False, 'error': f'List command timed out for {language}'}
        except Exception as e:
            return {'success': False, 'error': f'List command error for {language}: {str(e)}'}
    
    def update_packages(self, language: str, packages: List[str] = None) -> Dict[str, Any]:
        """Update packages for a specific language"""
        lang_path = self.get_language_path(language)
        if not lang_path or not lang_path.exists():
            return {'success': False, 'error': f'Language SDK not found: {language}'}
        
        lang_info = self.languages[language]
        
        try:
            if packages:
                # Update specific packages
                cmd = lang_info['update_cmd'] + packages
            else:
                # Update all packages
                cmd = lang_info['update_cmd']
            
            result = subprocess.run(
                cmd,
                cwd=lang_path,
                capture_output=True,
                text=True,
                timeout=300  # 5 minutes timeout
            )
            
            return {
                'success': result.returncode == 0,
                'stdout': result.stdout,
                'stderr': result.stderr,
                'returncode': result.returncode,
                'language': language,
                'command': ' '.join(cmd)
            }
            
        except subprocess.TimeoutExpired:
            return {'success': False, 'error': f'Update timed out for {language}'}
        except Exception as e:
            return {'success': False, 'error': f'Update error for {language}: {str(e)}'}
    
    def install_all_languages(self, packages: Dict[str, List[str]] = None) -> Dict[str, Dict[str, Any]]:
        """Install dependencies for all available languages"""
        results = {}
        
        for language in self.languages:
            lang_packages = packages.get(language, []) if packages else None
            results[language] = self.install_dependencies(language, lang_packages)
        
        return results
    
    def get_dependency_graph(self, language: str) -> Dict[str, Any]:
        """Get dependency graph for a specific language"""
        lang_path = self.get_language_path(language)
        if not lang_path or not lang_path.exists():
            return {'success': False, 'error': f'Language SDK not found: {language}'}
        
        try:
            # Read package files to build dependency graph
            package_files = self.find_package_files(language)
            dependencies = {}
            
            for package_file in package_files:
                if package_file.suffix == '.json':
                    # JSON-based package files
                    with open(package_file, 'r') as f:
                        data = json.load(f)
                        if 'dependencies' in data:
                            dependencies.update(data['dependencies'])
                        if 'devDependencies' in data:
                            dependencies.update(data['devDependencies'])
                
                elif package_file.name == 'requirements.txt':
                    # Python requirements.txt
                    with open(package_file, 'r') as f:
                        for line in f:
                            line = line.strip()
                            if line and not line.startswith('#'):
                                if '==' in line:
                                    package, version = line.split('==', 1)
                                    dependencies[package] = version
                                elif '>=' in line:
                                    package, version = line.split('>=', 1)
                                    dependencies[package] = f">={version}"
                                else:
                                    dependencies[line] = "latest"
                
                elif package_file.name == 'Cargo.toml':
                    # Rust Cargo.toml
                    with open(package_file, 'r') as f:
                        content = f.read()
                        # Simple parsing for dependencies section
                        if '[dependencies]' in content:
                            deps_section = content.split('[dependencies]')[1].split('[')[0]
                            for line in deps_section.split('\n'):
                                line = line.strip()
                                if '=' in line and not line.startswith('#'):
                                    package, version = line.split('=', 1)
                                    dependencies[package.strip()] = version.strip().strip('"\'')
            
            return {
                'success': True,
                'language': language,
                'dependencies': dependencies,
                'package_files': [str(f) for f in package_files]
            }
            
        except Exception as e:
            return {'success': False, 'error': f'Failed to get dependency graph for {language}: {str(e)}'}
    
    def check_dependency_conflicts(self) -> Dict[str, Any]:
        """Check for dependency conflicts across all languages"""
        conflicts = {}
        
        for language in self.languages:
            graph = self.get_dependency_graph(language)
            if graph['success']:
                # Check for common dependency conflicts
                dependencies = graph.get('dependencies', {})
                for package, version in dependencies.items():
                    if package in conflicts:
                        if conflicts[package] != version:
                            conflicts[package] = f"CONFLICT: {conflicts[package]} vs {version} ({language})"
                    else:
                        conflicts[package] = version
        
        return {
            'success': True,
            'conflicts': {k: v for k, v in conflicts.items() if 'CONFLICT' in str(v)},
            'all_dependencies': conflicts
        }
    
    def create_unified_package_file(self, output_path: Path) -> bool:
        """Create a unified package file for all languages"""
        try:
            unified_packages = {}
            
            for language in self.languages:
                graph = self.get_dependency_graph(language)
                if graph['success']:
                    unified_packages[language] = graph.get('dependencies', {})
            
            with open(output_path, 'w') as f:
                json.dump(unified_packages, f, indent=2)
            
            return True
        except Exception as e:
            logger.error(f"Failed to create unified package file: {e}")
            return False
    
    def validate_dependencies(self, language: str) -> Dict[str, Any]:
        """Validate dependencies for a specific language"""
        lang_path = self.get_language_path(language)
        if not lang_path or not lang_path.exists():
            return {'success': False, 'error': f'Language SDK not found: {language}'}
        
        try:
            # Try to install dependencies to validate them
            result = self.install_dependencies(language)
            
            if result['success']:
                return {
                    'success': True,
                    'language': language,
                    'status': 'All dependencies are valid'
                }
            else:
                return {
                    'success': False,
                    'language': language,
                    'error': result.get('error', 'Unknown validation error'),
                    'details': result
                }
                
        except Exception as e:
            return {'success': False, 'error': f'Validation error for {language}: {str(e)}'}

def main():
    """CLI for package management"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang Multi-Language Package Manager')
    parser.add_argument('--install', help='Install dependencies for specific language')
    parser.add_argument('--install-all', action='store_true', help='Install dependencies for all languages')
    parser.add_argument('--uninstall', nargs=2, metavar=('LANGUAGE', 'PACKAGE'), help='Uninstall package from language')
    parser.add_argument('--list', help='List packages for specific language')
    parser.add_argument('--update', help='Update packages for specific language')
    parser.add_argument('--graph', help='Get dependency graph for specific language')
    parser.add_argument('--conflicts', action='store_true', help='Check for dependency conflicts')
    parser.add_argument('--validate', help='Validate dependencies for specific language')
    parser.add_argument('--unified', help='Create unified package file')
    
    args = parser.parse_args()
    
    sdk_root = Path(__file__).parent
    package_manager = MultiLanguagePackageManager(sdk_root)
    
    if args.install:
        result = package_manager.install_dependencies(args.install)
        print(json.dumps(result, indent=2))
    
    elif args.install_all:
        results = package_manager.install_all_languages()
        print(json.dumps(results, indent=2))
    
    elif args.uninstall:
        language, package = args.uninstall
        result = package_manager.uninstall_package(language, package)
        print(json.dumps(result, indent=2))
    
    elif args.list:
        result = package_manager.list_packages(args.list)
        print(json.dumps(result, indent=2))
    
    elif args.update:
        result = package_manager.update_packages(args.update)
        print(json.dumps(result, indent=2))
    
    elif args.graph:
        result = package_manager.get_dependency_graph(args.graph)
        print(json.dumps(result, indent=2))
    
    elif args.conflicts:
        result = package_manager.check_dependency_conflicts()
        print(json.dumps(result, indent=2))
    
    elif args.validate:
        result = package_manager.validate_dependencies(args.validate)
        print(json.dumps(result, indent=2))
    
    elif args.unified:
        output_path = Path(args.unified)
        success = package_manager.create_unified_package_file(output_path)
        if success:
            print(f"Unified package file created: {output_path}")
        else:
            print("Failed to create unified package file")
    
    else:
        parser.print_help()

if __name__ == '__main__':
    main() 