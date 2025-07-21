#!/usr/bin/env python3
"""
TuskLang Multi-Language Deployment Orchestrator
Deploys applications across all language SDKs with unified pipeline
"""

import os
import sys
import json
import time
import subprocess
import threading
from pathlib import Path
from typing import Dict, List, Optional, Any, Tuple
from dataclasses import dataclass, asdict
from datetime import datetime, timedelta
import logging
import tempfile
import shutil
import hashlib
import tarfile
import zipfile

logger = logging.getLogger(__name__)

@dataclass
class DeploymentConfig:
    """Deployment configuration for a language"""
    language: str
    build_command: List[str]
    package_command: List[str]
    deploy_command: List[str]
    dependencies: List[str]
    environment_vars: Dict[str, str]
    build_timeout: int = 300
    deploy_timeout: int = 600

@dataclass
class DeploymentResult:
    """Result of a deployment operation"""
    language: str
    stage: str  # 'build', 'package', 'deploy'
    status: str  # 'success', 'failed', 'in_progress'
    duration: float
    output: str
    error_message: Optional[str] = None
    artifacts: List[str] = None
    deployment_id: Optional[str] = None

@dataclass
class DeploymentPackage:
    """Deployment package information"""
    language: str
    package_path: str
    package_size: int
    checksum: str
    dependencies: List[str]
    metadata: Dict[str, Any]

class MultiLanguageDeploymentOrchestrator:
    """Orchestrates deployment across all TuskLang language SDKs"""
    
    def __init__(self, sdk_root: Path = None):
        if sdk_root is None:
            self.sdk_root = Path(__file__).parent.parent
        else:
            self.sdk_root = sdk_root
        
        self.deployments_dir = Path(tempfile.mkdtemp(prefix='tsk_deployments_'))
        self.packages_dir = self.deployments_dir / 'packages'
        self.logs_dir = self.deployments_dir / 'logs'
        
        # Create directories
        self.packages_dir.mkdir(exist_ok=True)
        self.logs_dir.mkdir(exist_ok=True)
        
        # Language-specific deployment configurations
        self.deployment_configs = {
            'python': DeploymentConfig(
                language='python',
                build_command=['python', 'setup.py', 'build'],
                package_command=['python', 'setup.py', 'sdist', 'bdist_wheel'],
                deploy_command=['pip', 'install', '--upgrade'],
                dependencies=['setuptools', 'wheel', 'twine'],
                environment_vars={'PYTHONPATH': '.'},
                build_timeout=300,
                deploy_timeout=600
            ),
            'rust': DeploymentConfig(
                language='rust',
                build_command=['cargo', 'build', '--release'],
                package_command=['cargo', 'package'],
                deploy_command=['cargo', 'install', '--path', '.'],
                dependencies=[],
                environment_vars={'CARGO_TARGET_DIR': 'target/release'},
                build_timeout=600,
                deploy_timeout=300
            ),
            'javascript': DeploymentConfig(
                language='javascript',
                build_command=['npm', 'run', 'build'],
                package_command=['npm', 'pack'],
                deploy_command=['npm', 'install', '-g'],
                dependencies=['npm'],
                environment_vars={'NODE_ENV': 'production'},
                build_timeout=300,
                deploy_timeout=300
            ),
            'ruby': DeploymentConfig(
                language='ruby',
                build_command=['bundle', 'install'],
                package_command=['gem', 'build'],
                deploy_command=['gem', 'install'],
                dependencies=['bundler'],
                environment_vars={'BUNDLE_PATH': 'vendor/bundle'},
                build_timeout=300,
                deploy_timeout=300
            ),
            'csharp': DeploymentConfig(
                language='csharp',
                build_command=['dotnet', 'build', '--configuration', 'Release'],
                package_command=['dotnet', 'pack', '--configuration', 'Release'],
                deploy_command=['dotnet', 'tool', 'install', '--global'],
                dependencies=['dotnet-sdk'],
                environment_vars={'DOTNET_ENVIRONMENT': 'Production'},
                build_timeout=600,
                deploy_timeout=300
            ),
            'go': DeploymentConfig(
                language='go',
                build_command=['go', 'build', '-o', 'app'],
                package_command=['go', 'mod', 'tidy'],
                deploy_command=['go', 'install'],
                dependencies=[],
                environment_vars={'GOOS': 'linux', 'GOARCH': 'amd64'},
                build_timeout=300,
                deploy_timeout=300
            ),
            'php': DeploymentConfig(
                language='php',
                build_command=['composer', 'install', '--no-dev'],
                package_command=['composer', 'archive'],
                deploy_command=['composer', 'global', 'require'],
                dependencies=['composer'],
                environment_vars={'COMPOSER_HOME': '.composer'},
                build_timeout=300,
                deploy_timeout=300
            ),
            'java': DeploymentConfig(
                language='java',
                build_command=['mvn', 'clean', 'compile'],
                package_command=['mvn', 'package'],
                deploy_command=['mvn', 'install'],
                dependencies=['maven'],
                environment_vars={'JAVA_HOME': '/usr/lib/jvm/default-java'},
                build_timeout=600,
                deploy_timeout=300
            ),
            'bash': DeploymentConfig(
                language='bash',
                build_command=['chmod', '+x', '*.sh'],
                package_command=['tar', '-czf'],
                deploy_command=['cp', '-r'],
                dependencies=[],
                environment_vars={},
                build_timeout=60,
                deploy_timeout=120
            )
        }
    
    def build_application(self, language: str, project_path: Path = None, 
                         config_overrides: Dict[str, Any] = None) -> DeploymentResult:
        """Build an application for a specific language"""
        if project_path is None:
            project_path = self.sdk_root / language
        
        if not project_path.exists():
            return DeploymentResult(
                language=language,
                stage='build',
                status='failed',
                duration=0.0,
                output='',
                error_message=f'Project path does not exist: {project_path}'
            )
        
        config = self.deployment_configs.get(language)
        if not config:
            return DeploymentResult(
                language=language,
                stage='build',
                status='failed',
                duration=0.0,
                output='',
                error_message=f'No deployment configuration for language: {language}'
            )
        
        # Apply config overrides
        if config_overrides:
            for key, value in config_overrides.items():
                if hasattr(config, key):
                    setattr(config, key, value)
        
        start_time = time.time()
        
        try:
            # Set environment variables
            env = os.environ.copy()
            env.update(config.environment_vars)
            
            # Execute build command
            result = subprocess.run(
                config.build_command,
                cwd=project_path,
                env=env,
                capture_output=True,
                text=True,
                timeout=config.build_timeout
            )
            
            duration = time.time() - start_time
            
            if result.returncode == 0:
                return DeploymentResult(
                    language=language,
                    stage='build',
                    status='success',
                    duration=duration,
                    output=result.stdout,
                    artifacts=self._find_build_artifacts(language, project_path)
                )
            else:
                return DeploymentResult(
                    language=language,
                    stage='build',
                    status='failed',
                    duration=duration,
                    output=result.stdout,
                    error_message=result.stderr
                )
                
        except subprocess.TimeoutExpired:
            duration = time.time() - start_time
            return DeploymentResult(
                language=language,
                stage='build',
                status='failed',
                duration=duration,
                output='',
                error_message=f'Build timed out after {config.build_timeout} seconds'
            )
        except Exception as e:
            duration = time.time() - start_time
            return DeploymentResult(
                language=language,
                stage='build',
                status='failed',
                duration=duration,
                output='',
                error_message=str(e)
            )
    
    def _find_build_artifacts(self, language: str, project_path: Path) -> List[str]:
        """Find build artifacts for a language"""
        artifacts = []
        
        if language == 'python':
            # Look for dist/ and build/ directories
            for pattern in ['dist/*', 'build/*', '*.egg-info']:
                artifacts.extend([str(p) for p in project_path.glob(pattern)])
        
        elif language == 'rust':
            # Look for target/release/ directory
            target_dir = project_path / 'target' / 'release'
            if target_dir.exists():
                artifacts.extend([str(p) for p in target_dir.rglob('*') if p.is_file()])
        
        elif language == 'javascript':
            # Look for dist/ and build/ directories
            for pattern in ['dist/*', 'build/*', '*.tgz']:
                artifacts.extend([str(p) for p in project_path.glob(pattern)])
        
        elif language == 'ruby':
            # Look for *.gem files
            artifacts.extend([str(p) for p in project_path.glob('*.gem')])
        
        elif language == 'csharp':
            # Look for bin/Release/ directory
            bin_dir = project_path / 'bin' / 'Release'
            if bin_dir.exists():
                artifacts.extend([str(p) for p in bin_dir.rglob('*') if p.is_file()])
        
        elif language == 'go':
            # Look for executable
            artifacts.extend([str(p) for p in project_path.glob('app*') if p.is_file()])
        
        elif language == 'php':
            # Look for vendor/ directory and *.phar files
            vendor_dir = project_path / 'vendor'
            if vendor_dir.exists():
                artifacts.append(str(vendor_dir))
            artifacts.extend([str(p) for p in project_path.glob('*.phar')])
        
        elif language == 'java':
            # Look for target/ directory
            target_dir = project_path / 'target'
            if target_dir.exists():
                artifacts.extend([str(p) for p in target_dir.rglob('*.jar')])
        
        elif language == 'bash':
            # Look for *.sh files
            artifacts.extend([str(p) for p in project_path.glob('*.sh')])
        
        return artifacts
    
    def package_application(self, language: str, project_path: Path = None,
                           package_name: str = None) -> DeploymentPackage:
        """Package an application for deployment"""
        if project_path is None:
            project_path = self.sdk_root / language
        
        if not project_path.exists():
            raise ValueError(f'Project path does not exist: {project_path}')
        
        config = self.deployment_configs.get(language)
        if not config:
            raise ValueError(f'No deployment configuration for language: {language}')
        
        # Generate package name if not provided
        if not package_name:
            package_name = f"{language}_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
        
        package_path = self.packages_dir / f"{package_name}.tar.gz"
        
        try:
            # Create package
            with tarfile.open(package_path, 'w:gz') as tar:
                tar.add(project_path, arcname=language)
            
            # Calculate checksum
            checksum = self._calculate_checksum(package_path)
            
            # Get package size
            package_size = package_path.stat().st_size
            
            # Get dependencies
            dependencies = self._get_dependencies(language, project_path)
            
            # Create metadata
            metadata = {
                'created_at': datetime.now().isoformat(),
                'language': language,
                'project_path': str(project_path),
                'build_artifacts': self._find_build_artifacts(language, project_path)
            }
            
            return DeploymentPackage(
                language=language,
                package_path=str(package_path),
                package_size=package_size,
                checksum=checksum,
                dependencies=dependencies,
                metadata=metadata
            )
            
        except Exception as e:
            raise RuntimeError(f'Failed to package {language} application: {e}')
    
    def _calculate_checksum(self, file_path: Path) -> str:
        """Calculate SHA256 checksum of a file"""
        sha256_hash = hashlib.sha256()
        with open(file_path, 'rb') as f:
            for chunk in iter(lambda: f.read(4096), b''):
                sha256_hash.update(chunk)
        return sha256_hash.hexdigest()
    
    def _get_dependencies(self, language: str, project_path: Path) -> List[str]:
        """Get dependencies for a language project"""
        dependencies = []
        
        try:
            if language == 'python':
                # Look for requirements.txt or setup.py
                req_file = project_path / 'requirements.txt'
                if req_file.exists():
                    with open(req_file, 'r') as f:
                        dependencies = [line.strip() for line in f if line.strip() and not line.startswith('#')]
            
            elif language == 'rust':
                # Look for Cargo.toml
                cargo_file = project_path / 'Cargo.toml'
                if cargo_file.exists():
                    with open(cargo_file, 'r') as f:
                        content = f.read()
                        # Simple parsing for dependencies
                        import re
                        deps = re.findall(r'(\w+)\s*=\s*["\']([^"\']+)["\']', content)
                        dependencies = [f"{dep[0]}={dep[1]}" for dep in deps]
            
            elif language == 'javascript':
                # Look for package.json
                package_file = project_path / 'package.json'
                if package_file.exists():
                    with open(package_file, 'r') as f:
                        data = json.load(f)
                        deps = data.get('dependencies', {})
                        dev_deps = data.get('devDependencies', {})
                        dependencies = [f"{k}@{v}" for k, v in {**deps, **dev_deps}.items()]
            
            elif language == 'ruby':
                # Look for Gemfile
                gemfile = project_path / 'Gemfile'
                if gemfile.exists():
                    with open(gemfile, 'r') as f:
                        dependencies = [line.strip() for line in f if 'gem ' in line]
            
            elif language == 'csharp':
                # Look for .csproj files
                for csproj in project_path.glob('*.csproj'):
                    with open(csproj, 'r') as f:
                        content = f.read()
                        import re
                        deps = re.findall(r'<PackageReference Include="([^"]+)" Version="([^"]+)"', content)
                        dependencies = [f"{dep[0]}@{dep[1]}" for dep in deps]
            
            elif language == 'go':
                # Look for go.mod
                go_mod = project_path / 'go.mod'
                if go_mod.exists():
                    with open(go_mod, 'r') as f:
                        dependencies = [line.strip() for line in f if 'require ' in line]
            
            elif language == 'php':
                # Look for composer.json
                composer_file = project_path / 'composer.json'
                if composer_file.exists():
                    with open(composer_file, 'r') as f:
                        data = json.load(f)
                        deps = data.get('require', {})
                        dev_deps = data.get('require-dev', {})
                        dependencies = [f"{k}:{v}" for k, v in {**deps, **dev_deps}.items()]
            
            elif language == 'java':
                # Look for pom.xml
                pom_file = project_path / 'pom.xml'
                if pom_file.exists():
                    with open(pom_file, 'r') as f:
                        content = f.read()
                        import re
                        deps = re.findall(r'<dependency>.*?<groupId>([^<]+)</groupId>.*?<artifactId>([^<]+)</artifactId>.*?<version>([^<]+)</version>.*?</dependency>', content, re.DOTALL)
                        dependencies = [f"{dep[0]}:{dep[1]}:{dep[2]}" for dep in deps]
        
        except Exception as e:
            logger.warning(f"Failed to parse dependencies for {language}: {e}")
        
        return dependencies
    
    def deploy_application(self, language: str, package_path: str = None,
                          target_environment: str = 'production',
                          config_overrides: Dict[str, Any] = None) -> DeploymentResult:
        """Deploy an application to a target environment"""
        config = self.deployment_configs.get(language)
        if not config:
            return DeploymentResult(
                language=language,
                stage='deploy',
                status='failed',
                duration=0.0,
                output='',
                error_message=f'No deployment configuration for language: {language}'
            )
        
        # Apply config overrides
        if config_overrides:
            for key, value in config_overrides.items():
                if hasattr(config, key):
                    setattr(config, key, value)
        
        start_time = time.time()
        deployment_id = f"{language}_{target_environment}_{int(time.time())}"
        
        try:
            # Set environment variables
            env = os.environ.copy()
            env.update(config.environment_vars)
            env['DEPLOYMENT_ENV'] = target_environment
            
            # Execute deploy command
            deploy_cmd = config.deploy_command.copy()
            if package_path:
                deploy_cmd.append(package_path)
            
            result = subprocess.run(
                deploy_cmd,
                env=env,
                capture_output=True,
                text=True,
                timeout=config.deploy_timeout
            )
            
            duration = time.time() - start_time
            
            if result.returncode == 0:
                return DeploymentResult(
                    language=language,
                    stage='deploy',
                    status='success',
                    duration=duration,
                    output=result.stdout,
                    deployment_id=deployment_id
                )
            else:
                return DeploymentResult(
                    language=language,
                    stage='deploy',
                    status='failed',
                    duration=duration,
                    output=result.stdout,
                    error_message=result.stderr,
                    deployment_id=deployment_id
                )
                
        except subprocess.TimeoutExpired:
            duration = time.time() - start_time
            return DeploymentResult(
                language=language,
                stage='deploy',
                status='failed',
                duration=duration,
                output='',
                error_message=f'Deployment timed out after {config.deploy_timeout} seconds',
                deployment_id=deployment_id
            )
        except Exception as e:
            duration = time.time() - start_time
            return DeploymentResult(
                language=language,
                stage='deploy',
                status='failed',
                duration=duration,
                output='',
                error_message=str(e),
                deployment_id=deployment_id
            )
    
    def deploy_all_languages(self, target_environment: str = 'production',
                           parallel: bool = True) -> Dict[str, DeploymentResult]:
        """Deploy applications for all languages"""
        results = {}
        
        if parallel:
            # Deploy in parallel
            threads = []
            for language in self.deployment_configs.keys():
                thread = threading.Thread(
                    target=lambda lang: results.update({lang: self.deploy_application(lang, target_environment=target_environment)}),
                    args=(language,)
                )
                threads.append(thread)
                thread.start()
            
            for thread in threads:
                thread.join()
        else:
            # Deploy sequentially
            for language in self.deployment_configs.keys():
                results[language] = self.deploy_application(language, target_environment=target_environment)
        
        return results
    
    def build_and_deploy(self, language: str, project_path: Path = None,
                        target_environment: str = 'production',
                        config_overrides: Dict[str, Any] = None) -> Dict[str, DeploymentResult]:
        """Build and deploy an application in one operation"""
        results = {}
        
        # Build
        build_result = self.build_application(language, project_path, config_overrides)
        results['build'] = build_result
        
        if build_result.status == 'success':
            # Package
            try:
                package = self.package_application(language, project_path)
                results['package'] = DeploymentResult(
                    language=language,
                    stage='package',
                    status='success',
                    duration=0.0,
                    output=f'Package created: {package.package_path}',
                    artifacts=[package.package_path]
                )
                
                # Deploy
                deploy_result = self.deploy_application(language, package.package_path, target_environment, config_overrides)
                results['deploy'] = deploy_result
                
            except Exception as e:
                results['package'] = DeploymentResult(
                    language=language,
                    stage='package',
                    status='failed',
                    duration=0.0,
                    output='',
                    error_message=str(e)
                )
        else:
            results['package'] = DeploymentResult(
                language=language,
                stage='package',
                status='skipped',
                duration=0.0,
                output='Skipped due to build failure'
            )
            results['deploy'] = DeploymentResult(
                language=language,
                stage='deploy',
                status='skipped',
                duration=0.0,
                output='Skipped due to build failure'
            )
        
        return results
    
    def get_deployment_status(self, deployment_id: str) -> Optional[DeploymentResult]:
        """Get status of a specific deployment"""
        # This would typically query a database or deployment service
        # For now, return None as we don't persist deployment results
        return None
    
    def list_deployments(self, language: str = None) -> List[Dict[str, Any]]:
        """List recent deployments"""
        # This would typically query a database
        # For now, return empty list
        return []
    
    def cleanup_old_packages(self, days: int = 7) -> int:
        """Clean up old deployment packages"""
        cutoff_time = datetime.now() - timedelta(days=days)
        cleaned_count = 0
        
        for package_file in self.packages_dir.glob('*.tar.gz'):
            if package_file.stat().st_mtime < cutoff_time.timestamp():
                try:
                    package_file.unlink()
                    cleaned_count += 1
                except Exception as e:
                    logger.warning(f"Failed to delete old package {package_file}: {e}")
        
        return cleaned_count

def main():
    """CLI for deployment orchestrator"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang Multi-Language Deployment Orchestrator')
    parser.add_argument('--build', help='Build application for language')
    parser.add_argument('--package', help='Package application for language')
    parser.add_argument('--deploy', help='Deploy application for language')
    parser.add_argument('--build-deploy', help='Build and deploy application for language')
    parser.add_argument('--deploy-all', action='store_true', help='Deploy all languages')
    parser.add_argument('--environment', default='production', help='Target environment')
    parser.add_argument('--project-path', help='Project path')
    parser.add_argument('--parallel', action='store_true', help='Run operations in parallel')
    parser.add_argument('--cleanup', type=int, help='Clean up packages older than N days')
    
    args = parser.parse_args()
    
    orchestrator = MultiLanguageDeploymentOrchestrator()
    
    if args.build:
        project_path = Path(args.project_path) if args.project_path else None
        result = orchestrator.build_application(args.build, project_path)
        print(json.dumps(asdict(result), indent=2, default=str))
    
    elif args.package:
        project_path = Path(args.project_path) if args.project_path else None
        package = orchestrator.package_application(args.package, project_path)
        print(json.dumps(asdict(package), indent=2, default=str))
    
    elif args.deploy:
        result = orchestrator.deploy_application(args.deploy, target_environment=args.environment)
        print(json.dumps(asdict(result), indent=2, default=str))
    
    elif args.build_deploy:
        project_path = Path(args.project_path) if args.project_path else None
        results = orchestrator.build_and_deploy(args.build_deploy, project_path, args.environment)
        print(json.dumps({stage: asdict(result) for stage, result in results.items()}, indent=2, default=str))
    
    elif args.deploy_all:
        results = orchestrator.deploy_all_languages(args.environment, args.parallel)
        print(json.dumps({lang: asdict(result) for lang, result in results.items()}, indent=2, default=str))
    
    elif args.cleanup:
        cleaned = orchestrator.cleanup_old_packages(args.cleanup)
        print(f"Cleaned up {cleaned} old packages")
    
    else:
        parser.print_help()

if __name__ == '__main__':
    main() 