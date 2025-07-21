#!/usr/bin/env python3
"""
TuskLang Cross-Language Deployment Configuration Manager
Manages deployment configurations and environments across all language SDKs
"""

import os
import json
import yaml
import sqlite3
from pathlib import Path
from typing import Dict, List, Optional, Any, Union
from dataclasses import dataclass, asdict
from datetime import datetime
import logging
import tempfile
import shutil
from jinja2 import Template, Environment, FileSystemLoader

logger = logging.getLogger(__name__)

@dataclass
class EnvironmentConfig:
    """Configuration for a deployment environment"""
    name: str
    description: str
    variables: Dict[str, str]
    secrets: Dict[str, str]
    deployment_strategy: str  # 'rolling', 'blue-green', 'canary'
    health_check_url: Optional[str] = None
    rollback_enabled: bool = True
    auto_scaling: bool = False
    max_instances: int = 3
    min_instances: int = 1

@dataclass
class LanguageDeploymentConfig:
    """Language-specific deployment configuration"""
    language: str
    environments: Dict[str, EnvironmentConfig]
    build_settings: Dict[str, Any]
    deploy_settings: Dict[str, Any]
    dependencies: List[str]
    health_checks: List[Dict[str, Any]]
    resource_limits: Dict[str, Any]

@dataclass
class DeploymentValidation:
    """Deployment validation result"""
    language: str
    environment: str
    is_valid: bool
    errors: List[str]
    warnings: List[str]
    dependencies_ok: bool
    config_ok: bool
    permissions_ok: bool

class DeploymentConfigManager:
    """Manages deployment configurations across all language SDKs"""
    
    def __init__(self, config_dir: Path = None):
        if config_dir is None:
            self.config_dir = Path(tempfile.mkdtemp(prefix='tsk_deploy_config_'))
        else:
            self.config_dir = config_dir
        
        self.db_path = self.config_dir / 'deployment_config.db'
        self.templates_dir = self.config_dir / 'templates'
        self.environments_dir = self.config_dir / 'environments'
        
        # Create directories
        self.templates_dir.mkdir(exist_ok=True)
        self.environments_dir.mkdir(exist_ok=True)
        
        # Initialize database
        self._init_database()
        
        # Load default configurations
        self._load_default_configs()
    
    def _init_database(self):
        """Initialize SQLite database for configuration management"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Create tables
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS environments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT UNIQUE,
                description TEXT,
                variables TEXT,
                secrets TEXT,
                deployment_strategy TEXT,
                health_check_url TEXT,
                rollback_enabled BOOLEAN,
                auto_scaling BOOLEAN,
                max_instances INTEGER,
                min_instances INTEGER,
                created_at TEXT,
                updated_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS language_configs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                language TEXT,
                environment_name TEXT,
                build_settings TEXT,
                deploy_settings TEXT,
                dependencies TEXT,
                health_checks TEXT,
                resource_limits TEXT,
                created_at TEXT,
                updated_at TEXT,
                FOREIGN KEY (environment_name) REFERENCES environments (name)
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS deployment_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                deployment_id TEXT,
                language TEXT,
                environment TEXT,
                status TEXT,
                config_snapshot TEXT,
                started_at TEXT,
                completed_at TEXT,
                duration REAL
            )
        ''')
        
        conn.commit()
        conn.close()
    
    def _load_default_configs(self):
        """Load default deployment configurations"""
        default_environments = {
            'development': EnvironmentConfig(
                name='development',
                description='Development environment for testing',
                variables={
                    'DEBUG': 'true',
                    'LOG_LEVEL': 'debug',
                    'DATABASE_URL': 'sqlite:///dev.db'
                },
                secrets={},
                deployment_strategy='rolling',
                health_check_url='http://localhost:8080/health',
                rollback_enabled=True,
                auto_scaling=False,
                max_instances=1,
                min_instances=1
            ),
            'staging': EnvironmentConfig(
                name='staging',
                description='Staging environment for pre-production testing',
                variables={
                    'DEBUG': 'false',
                    'LOG_LEVEL': 'info',
                    'DATABASE_URL': 'postgresql://staging:5432/app'
                },
                secrets={
                    'API_KEY': 'staging_key_placeholder',
                    'DATABASE_PASSWORD': 'staging_password_placeholder'
                },
                deployment_strategy='blue-green',
                health_check_url='https://staging.example.com/health',
                rollback_enabled=True,
                auto_scaling=True,
                max_instances=3,
                min_instances=1
            ),
            'production': EnvironmentConfig(
                name='production',
                description='Production environment',
                variables={
                    'DEBUG': 'false',
                    'LOG_LEVEL': 'warn',
                    'DATABASE_URL': 'postgresql://prod:5432/app'
                },
                secrets={
                    'API_KEY': 'prod_key_placeholder',
                    'DATABASE_PASSWORD': 'prod_password_placeholder'
                },
                deployment_strategy='canary',
                health_check_url='https://api.example.com/health',
                rollback_enabled=True,
                auto_scaling=True,
                max_instances=10,
                min_instances=2
            )
        }
        
        # Save default environments
        for env_name, env_config in default_environments.items():
            self.save_environment(env_config)
    
    def save_environment(self, environment: EnvironmentConfig) -> bool:
        """Save an environment configuration"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO environments 
                (name, description, variables, secrets, deployment_strategy, 
                 health_check_url, rollback_enabled, auto_scaling, max_instances, 
                 min_instances, created_at, updated_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                environment.name,
                environment.description,
                json.dumps(environment.variables),
                json.dumps(environment.secrets),
                environment.deployment_strategy,
                environment.health_check_url,
                environment.rollback_enabled,
                environment.auto_scaling,
                environment.max_instances,
                environment.min_instances,
                datetime.now().isoformat(),
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            logger.error(f"Failed to save environment {environment.name}: {e}")
            return False
    
    def get_environment(self, name: str) -> Optional[EnvironmentConfig]:
        """Get an environment configuration"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                SELECT name, description, variables, secrets, deployment_strategy,
                       health_check_url, rollback_enabled, auto_scaling, max_instances, min_instances
                FROM environments WHERE name = ?
            ''', (name,))
            
            row = cursor.fetchone()
            conn.close()
            
            if row:
                name, description, variables_str, secrets_str, deployment_strategy, health_check_url, rollback_enabled, auto_scaling, max_instances, min_instances = row
                
                return EnvironmentConfig(
                    name=name,
                    description=description,
                    variables=json.loads(variables_str),
                    secrets=json.loads(secrets_str),
                    deployment_strategy=deployment_strategy,
                    health_check_url=health_check_url,
                    rollback_enabled=bool(rollback_enabled),
                    auto_scaling=bool(auto_scaling),
                    max_instances=max_instances,
                    min_instances=min_instances
                )
            
            return None
            
        except Exception as e:
            logger.error(f"Failed to get environment {name}: {e}")
            return None
    
    def list_environments(self) -> List[EnvironmentConfig]:
        """List all environment configurations"""
        environments = []
        
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                SELECT name, description, variables, secrets, deployment_strategy,
                       health_check_url, rollback_enabled, auto_scaling, max_instances, min_instances
                FROM environments
            ''')
            
            for row in cursor.fetchall():
                name, description, variables_str, secrets_str, deployment_strategy, health_check_url, rollback_enabled, auto_scaling, max_instances, min_instances = row
                
                environments.append(EnvironmentConfig(
                    name=name,
                    description=description,
                    variables=json.loads(variables_str),
                    secrets=json.loads(secrets_str),
                    deployment_strategy=deployment_strategy,
                    health_check_url=health_check_url,
                    rollback_enabled=bool(rollback_enabled),
                    auto_scaling=bool(auto_scaling),
                    max_instances=max_instances,
                    min_instances=min_instances
                ))
            
            conn.close()
            
        except Exception as e:
            logger.error(f"Failed to list environments: {e}")
        
        return environments
    
    def save_language_config(self, language: str, environment: str, 
                           config: LanguageDeploymentConfig) -> bool:
        """Save language-specific deployment configuration"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO language_configs 
                (language, environment_name, build_settings, deploy_settings, 
                 dependencies, health_checks, resource_limits, created_at, updated_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                language,
                environment,
                json.dumps(config.build_settings),
                json.dumps(config.deploy_settings),
                json.dumps(config.dependencies),
                json.dumps(config.health_checks),
                json.dumps(config.resource_limits),
                datetime.now().isoformat(),
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            logger.error(f"Failed to save language config for {language}/{environment}: {e}")
            return False
    
    def get_language_config(self, language: str, environment: str) -> Optional[LanguageDeploymentConfig]:
        """Get language-specific deployment configuration"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                SELECT build_settings, deploy_settings, dependencies, health_checks, resource_limits
                FROM language_configs WHERE language = ? AND environment_name = ?
            ''', (language, environment))
            
            row = cursor.fetchone()
            conn.close()
            
            if row:
                build_settings_str, deploy_settings_str, dependencies_str, health_checks_str, resource_limits_str = row
                
                return LanguageDeploymentConfig(
                    language=language,
                    environments={},  # Will be populated separately
                    build_settings=json.loads(build_settings_str),
                    deploy_settings=json.loads(deploy_settings_str),
                    dependencies=json.loads(dependencies_str),
                    health_checks=json.loads(health_checks_str),
                    resource_limits=json.loads(resource_limits_str)
                )
            
            return None
            
        except Exception as e:
            logger.error(f"Failed to get language config for {language}/{environment}: {e}")
            return None
    
    def validate_deployment_config(self, language: str, environment: str) -> DeploymentValidation:
        """Validate deployment configuration"""
        errors = []
        warnings = []
        
        # Check if environment exists
        env_config = self.get_environment(environment)
        if not env_config:
            errors.append(f"Environment '{environment}' does not exist")
            return DeploymentValidation(
                language=language,
                environment=environment,
                is_valid=False,
                errors=errors,
                warnings=warnings,
                dependencies_ok=False,
                config_ok=False,
                permissions_ok=False
            )
        
        # Check if language config exists
        lang_config = self.get_language_config(language, environment)
        if not lang_config:
            warnings.append(f"No specific configuration found for {language} in {environment}")
        
        # Validate dependencies
        dependencies_ok = True
        if lang_config:
            for dep in lang_config.dependencies:
                # Check if dependency is available
                if not self._check_dependency_availability(language, dep):
                    errors.append(f"Dependency '{dep}' not available for {language}")
                    dependencies_ok = False
        
        # Validate configuration
        config_ok = True
        if lang_config:
            # Validate build settings
            if 'build_command' not in lang_config.build_settings:
                errors.append(f"Missing build_command in build settings for {language}")
                config_ok = False
            
            # Validate deploy settings
            if 'deploy_command' not in lang_config.deploy_settings:
                errors.append(f"Missing deploy_command in deploy settings for {language}")
                config_ok = False
        
        # Validate permissions (simplified)
        permissions_ok = True
        # In a real implementation, this would check file permissions, network access, etc.
        
        is_valid = len(errors) == 0
        
        return DeploymentValidation(
            language=language,
            environment=environment,
            is_valid=is_valid,
            errors=errors,
            warnings=warnings,
            dependencies_ok=dependencies_ok,
            config_ok=config_ok,
            permissions_ok=permissions_ok
        )
    
    def _check_dependency_availability(self, language: str, dependency: str) -> bool:
        """Check if a dependency is available for a language"""
        # Simplified check - in reality, this would check actual dependency availability
        return True
    
    def generate_deployment_script(self, language: str, environment: str, 
                                 template_name: str = None) -> str:
        """Generate deployment script from template"""
        if template_name is None:
            template_name = f"{language}_deploy.sh"
        
        template_path = self.templates_dir / template_name
        if not template_path.exists():
            # Create default template
            self._create_default_template(language, template_path)
        
        # Get configurations
        env_config = self.get_environment(environment)
        lang_config = self.get_language_config(language, environment)
        
        if not env_config:
            raise ValueError(f"Environment '{environment}' not found")
        
        # Prepare template variables
        template_vars = {
            'language': language,
            'environment': environment,
            'env_vars': env_config.variables,
            'secrets': env_config.secrets,
            'deployment_strategy': env_config.deployment_strategy,
            'health_check_url': env_config.health_check_url,
            'rollback_enabled': env_config.rollback_enabled,
            'auto_scaling': env_config.auto_scaling,
            'max_instances': env_config.max_instances,
            'min_instances': env_config.min_instances
        }
        
        if lang_config:
            template_vars.update({
                'build_settings': lang_config.build_settings,
                'deploy_settings': lang_config.deploy_settings,
                'dependencies': lang_config.dependencies,
                'health_checks': lang_config.health_checks,
                'resource_limits': lang_config.resource_limits
            })
        
        # Render template
        with open(template_path, 'r') as f:
            template_content = f.read()
        
        template = Template(template_content)
        return template.render(**template_vars)
    
    def _create_default_template(self, language: str, template_path: Path):
        """Create default deployment template for a language"""
        if language == 'python':
            template_content = '''#!/bin/bash
# {{ language }} deployment script for {{ environment }}

set -e

echo "Deploying {{ language }} application to {{ environment }}"

# Set environment variables
{% for key, value in env_vars.items() %}
export {{ key }}="{{ value }}"
{% endfor %}

# Install dependencies
{% if dependencies %}
echo "Installing dependencies..."
{% for dep in dependencies %}
pip install {{ dep }}
{% endfor %}
{% endif %}

# Build application
{% if build_settings.build_command %}
echo "Building application..."
{{ build_settings.build_command | join(' ') }}
{% endif %}

# Deploy application
{% if deploy_settings.deploy_command %}
echo "Deploying application..."
{{ deploy_settings.deploy_command | join(' ') }}
{% endif %}

# Health check
{% if health_check_url %}
echo "Performing health check..."
sleep 10
curl -f {{ health_check_url }} || exit 1
{% endif %}

echo "Deployment completed successfully"
'''
        elif language == 'rust':
            template_content = '''#!/bin/bash
# {{ language }} deployment script for {{ environment }}

set -e

echo "Deploying {{ language }} application to {{ environment }}"

# Set environment variables
{% for key, value in env_vars.items() %}
export {{ key }}="{{ value }}"
{% endfor %}

# Build application
echo "Building application..."
cargo build --release

# Deploy application
echo "Deploying application..."
sudo cp target/release/app /usr/local/bin/

# Health check
{% if health_check_url %}
echo "Performing health check..."
sleep 5
curl -f {{ health_check_url }} || exit 1
{% endif %}

echo "Deployment completed successfully"
'''
        else:
            template_content = '''#!/bin/bash
# {{ language }} deployment script for {{ environment }}

set -e

echo "Deploying {{ language }} application to {{ environment }}"

# Set environment variables
{% for key, value in env_vars.items() %}
export {{ key }}="{{ value }}"
{% endfor %}

# Build and deploy
echo "Building and deploying application..."
# Add language-specific build and deploy commands here

# Health check
{% if health_check_url %}
echo "Performing health check..."
sleep 10
curl -f {{ health_check_url }} || exit 1
{% endif %}

echo "Deployment completed successfully"
'''
        
        with open(template_path, 'w') as f:
            f.write(template_content)
        
        # Make executable
        os.chmod(template_path, 0o755)
    
    def export_configuration(self, format: str = 'json') -> str:
        """Export all configurations"""
        config_data = {
            'environments': [asdict(env) for env in self.list_environments()],
            'language_configs': {},
            'exported_at': datetime.now().isoformat()
        }
        
        # Get all language configs
        for env in config_data['environments']:
            env_name = env['name']
            config_data['language_configs'][env_name] = {}
            
            for language in ['python', 'rust', 'javascript', 'ruby', 'csharp', 'go', 'php', 'java', 'bash']:
                lang_config = self.get_language_config(language, env_name)
                if lang_config:
                    config_data['language_configs'][env_name][language] = asdict(lang_config)
        
        if format == 'json':
            return json.dumps(config_data, indent=2)
        elif format == 'yaml':
            return yaml.dump(config_data, default_flow_style=False)
        else:
            raise ValueError(f"Unsupported export format: {format}")
    
    def import_configuration(self, config_data: str, format: str = 'json') -> bool:
        """Import configurations"""
        try:
            if format == 'json':
                data = json.loads(config_data)
            elif format == 'yaml':
                data = yaml.safe_load(config_data)
            else:
                raise ValueError(f"Unsupported import format: {format}")
            
            # Import environments
            for env_data in data.get('environments', []):
                env_config = EnvironmentConfig(**env_data)
                self.save_environment(env_config)
            
            # Import language configs
            for env_name, lang_configs in data.get('language_configs', {}).items():
                for language, config_data in lang_configs.items():
                    lang_config = LanguageDeploymentConfig(**config_data)
                    self.save_language_config(language, env_name, lang_config)
            
            return True
            
        except Exception as e:
            logger.error(f"Failed to import configuration: {e}")
            return False

def main():
    """CLI for deployment config manager"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang Deployment Configuration Manager')
    parser.add_argument('--save-env', help='Save environment configuration from JSON file')
    parser.add_argument('--get-env', help='Get environment configuration')
    parser.add_argument('--list-envs', action='store_true', help='List all environments')
    parser.add_argument('--save-lang-config', nargs=3, metavar=('LANGUAGE', 'ENVIRONMENT', 'FILE'), help='Save language configuration')
    parser.add_argument('--get-lang-config', nargs=2, metavar=('LANGUAGE', 'ENVIRONMENT'), help='Get language configuration')
    parser.add_argument('--validate', nargs=2, metavar=('LANGUAGE', 'ENVIRONMENT'), help='Validate deployment configuration')
    parser.add_argument('--generate-script', nargs=3, metavar=('LANGUAGE', 'ENVIRONMENT', 'OUTPUT'), help='Generate deployment script')
    parser.add_argument('--export', help='Export all configurations')
    parser.add_argument('--import-config', help='Import configurations from file')
    
    args = parser.parse_args()
    
    manager = DeploymentConfigManager()
    
    if args.save_env:
        with open(args.save_env, 'r') as f:
            env_data = json.load(f)
        env_config = EnvironmentConfig(**env_data)
        success = manager.save_environment(env_config)
        print(f"Environment saved: {success}")
    
    elif args.get_env:
        env_config = manager.get_environment(args.get_env)
        if env_config:
            print(json.dumps(asdict(env_config), indent=2))
        else:
            print("Environment not found")
    
    elif args.list_envs:
        environments = manager.list_environments()
        for env in environments:
            print(f"{env.name}: {env.description}")
    
    elif args.save_lang_config:
        language, environment, config_file = args.save_lang_config
        with open(config_file, 'r') as f:
            config_data = json.load(f)
        lang_config = LanguageDeploymentConfig(**config_data)
        success = manager.save_language_config(language, environment, lang_config)
        print(f"Language config saved: {success}")
    
    elif args.get_lang_config:
        language, environment = args.get_lang_config
        lang_config = manager.get_language_config(language, environment)
        if lang_config:
            print(json.dumps(asdict(lang_config), indent=2))
        else:
            print("Language config not found")
    
    elif args.validate:
        language, environment = args.validate
        validation = manager.validate_deployment_config(language, environment)
        print(json.dumps(asdict(validation), indent=2))
    
    elif args.generate_script:
        language, environment, output_file = args.generate_script
        script = manager.generate_deployment_script(language, environment)
        with open(output_file, 'w') as f:
            f.write(script)
        print(f"Deployment script generated: {output_file}")
    
    elif args.export:
        config_data = manager.export_configuration(args.export)
        print(config_data)
    
    elif args.import_config:
        with open(args.import_config, 'r') as f:
            config_data = f.read()
        success = manager.import_configuration(config_data)
        print(f"Configuration imported: {success}")
    
    else:
        parser.print_help()

if __name__ == '__main__':
    main() 