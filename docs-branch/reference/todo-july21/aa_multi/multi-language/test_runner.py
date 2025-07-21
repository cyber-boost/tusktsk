#!/usr/bin/env python3
"""
TuskLang Multi-Language Test Runner
Executes tests across all language SDKs with unified reporting
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
from datetime import datetime
import logging
import tempfile
import shutil

logger = logging.getLogger(__name__)

@dataclass
class TestResult:
    """Represents a single test result"""
    language: str
    test_name: str
    test_file: str
    status: str  # 'passed', 'failed', 'skipped', 'error'
    duration: float
    stdout: str
    stderr: str
    error_message: Optional[str] = None
    coverage: Optional[Dict[str, Any]] = None
    metadata: Optional[Dict[str, Any]] = None

@dataclass
class TestSuite:
    """Represents a test suite for a language"""
    language: str
    total_tests: int
    passed: int
    failed: int
    skipped: int
    errors: int
    duration: float
    coverage_percentage: float
    results: List[TestResult]

class MultiLanguageTestRunner:
    """Executes tests across all TuskLang language SDKs"""
    
    def __init__(self, sdk_root: Path = None):
        if sdk_root is None:
            self.sdk_root = Path(__file__).parent.parent
        else:
            self.sdk_root = sdk_root
        
        self.languages = {
            'python': {
                'test_frameworks': ['pytest', 'unittest'],
                'test_patterns': ['test_*.py', '*_test.py', 'tests/'],
                'run_cmd': ['python', '-m', 'pytest'],
                'coverage_cmd': ['python', '-m', 'coverage', 'run', '-m', 'pytest'],
                'coverage_report_cmd': ['python', '-m', 'coverage', 'report', '--json']
            },
            'rust': {
                'test_frameworks': ['cargo test'],
                'test_patterns': ['tests/', 'src/'],
                'run_cmd': ['cargo', 'test'],
                'coverage_cmd': ['cargo', 'tarpaulin', '--out', 'Json'],
                'coverage_report_cmd': ['cargo', 'tarpaulin', '--out', 'Json']
            },
            'javascript': {
                'test_frameworks': ['jest', 'mocha', 'tape'],
                'test_patterns': ['test/', 'tests/', '*.test.js', '*.spec.js'],
                'run_cmd': ['npm', 'test'],
                'coverage_cmd': ['npm', 'run', 'test:coverage'],
                'coverage_report_cmd': ['npm', 'run', 'coverage:report']
            },
            'ruby': {
                'test_frameworks': ['rspec', 'minitest'],
                'test_patterns': ['spec/', 'test/'],
                'run_cmd': ['bundle', 'exec', 'rspec'],
                'coverage_cmd': ['bundle', 'exec', 'rspec', '--format', 'json'],
                'coverage_report_cmd': ['bundle', 'exec', 'simplecov']
            },
            'csharp': {
                'test_frameworks': ['xunit', 'nunit', 'mstest'],
                'test_patterns': ['Tests/', 'tests/'],
                'run_cmd': ['dotnet', 'test'],
                'coverage_cmd': ['dotnet', 'test', '--collect', 'Coverage'],
                'coverage_report_cmd': ['dotnet', 'test', '--collect', 'Coverage', '--logger', 'trx']
            },
            'go': {
                'test_frameworks': ['testing'],
                'test_patterns': ['*_test.go'],
                'run_cmd': ['go', 'test', './...'],
                'coverage_cmd': ['go', 'test', '-coverprofile=coverage.out', './...'],
                'coverage_report_cmd': ['go', 'tool', 'cover', '-func=coverage.out']
            },
            'php': {
                'test_frameworks': ['phpunit', 'pest'],
                'test_patterns': ['tests/', 'test/'],
                'run_cmd': ['vendor/bin/phpunit'],
                'coverage_cmd': ['vendor/bin/phpunit', '--coverage-html', 'coverage'],
                'coverage_report_cmd': ['vendor/bin/phpunit', '--coverage-text']
            },
            'java': {
                'test_frameworks': ['junit', 'testng'],
                'test_patterns': ['src/test/', 'test/'],
                'run_cmd': ['mvn', 'test'],
                'coverage_cmd': ['mvn', 'test', 'jacoco:report'],
                'coverage_report_cmd': ['mvn', 'jacoco:report']
            },
            'bash': {
                'test_frameworks': ['bats', 'shunit2'],
                'test_patterns': ['test/', 'tests/', '*.bats'],
                'run_cmd': ['bats', 'test/'],
                'coverage_cmd': ['bats', 'test/', '--tap'],
                'coverage_report_cmd': ['bats', 'test/', '--tap']
            }
        }
        
        self.test_data_dir = Path(tempfile.mkdtemp(prefix='tsk_test_data_'))
        self.results_dir = Path(tempfile.mkdtemp(prefix='tsk_test_results_'))
        
    def find_test_files(self, language: str) -> List[Path]:
        """Find test files for a specific language"""
        lang_path = self.sdk_root / language
        if not lang_path.exists():
            return []
        
        test_files = []
        lang_info = self.languages[language]
        
        for pattern in lang_info['test_patterns']:
            if '*' in pattern:
                # Handle glob patterns
                for file_path in lang_path.rglob(pattern):
                    if file_path.is_file():
                        test_files.append(file_path)
            else:
                # Handle directory patterns
                dir_path = lang_path / pattern
                if dir_path.exists() and dir_path.is_dir():
                    for file_path in dir_path.rglob('*'):
                        if file_path.is_file() and self._is_test_file(file_path, language):
                            test_files.append(file_path)
        
        return test_files
    
    def _is_test_file(self, file_path: Path, language: str) -> bool:
        """Check if a file is a test file for the given language"""
        filename = file_path.name.lower()
        
        if language == 'python':
            return filename.startswith('test_') or filename.endswith('_test.py')
        elif language == 'rust':
            return filename.endswith('.rs') and ('test' in filename or 'mod' in filename)
        elif language == 'javascript':
            return filename.endswith('.js') and ('test' in filename or 'spec' in filename)
        elif language == 'ruby':
            return filename.endswith('.rb') and ('test' in filename or 'spec' in filename)
        elif language == 'csharp':
            return filename.endswith('.cs') and ('test' in filename or 'spec' in filename)
        elif language == 'go':
            return filename.endswith('_test.go')
        elif language == 'php':
            return filename.endswith('.php') and ('test' in filename or 'spec' in filename)
        elif language == 'java':
            return filename.endswith('.java') and ('test' in filename or 'spec' in filename)
        elif language == 'bash':
            return filename.endswith('.bats') or filename.endswith('.sh')
        
        return False
    
    def run_tests_for_language(self, language: str, test_files: List[Path] = None, 
                              coverage: bool = False, parallel: bool = False) -> TestSuite:
        """Run tests for a specific language"""
        lang_path = self.sdk_root / language
        if not lang_path.exists():
            return TestSuite(
                language=language,
                total_tests=0,
                passed=0,
                failed=0,
                skipped=0,
                errors=0,
                duration=0.0,
                coverage_percentage=0.0,
                results=[]
            )
        
        if test_files is None:
            test_files = self.find_test_files(language)
        
        lang_info = self.languages[language]
        start_time = time.time()
        results = []
        
        try:
            # Prepare test command
            if coverage:
                cmd = lang_info['coverage_cmd']
            else:
                cmd = lang_info['run_cmd']
            
            # Add test files if specified
            if test_files:
                cmd.extend([str(f) for f in test_files])
            
            # Add parallel flag if supported
            if parallel and '--parallel' in ' '.join(cmd):
                cmd.append('--parallel')
            
            # Execute tests
            result = subprocess.run(
                cmd,
                cwd=lang_path,
                capture_output=True,
                text=True,
                timeout=300  # 5 minutes timeout
            )
            
            duration = time.time() - start_time
            
            # Parse test results
            results = self._parse_test_results(language, result, test_files)
            
            # Get coverage if requested
            coverage_percentage = 0.0
            if coverage:
                coverage_percentage = self._get_coverage_percentage(language, lang_path)
            
            # Calculate summary
            total_tests = len(results)
            passed = len([r for r in results if r.status == 'passed'])
            failed = len([r for r in results if r.status == 'failed'])
            skipped = len([r for r in results if r.status == 'skipped'])
            errors = len([r for r in results if r.status == 'error'])
            
            return TestSuite(
                language=language,
                total_tests=total_tests,
                passed=passed,
                failed=failed,
                skipped=skipped,
                errors=errors,
                duration=duration,
                coverage_percentage=coverage_percentage,
                results=results
            )
            
        except subprocess.TimeoutExpired:
            logger.error(f"Test execution timed out for {language}")
            return TestSuite(
                language=language,
                total_tests=0,
                passed=0,
                failed=0,
                skipped=0,
                errors=1,
                duration=300.0,
                coverage_percentage=0.0,
                results=[TestResult(
                    language=language,
                    test_name="timeout",
                    test_file="",
                    status="error",
                    duration=300.0,
                    stdout="",
                    stderr="Test execution timed out",
                    error_message="Test execution timed out after 5 minutes"
                )]
            )
        except Exception as e:
            logger.error(f"Error running tests for {language}: {e}")
            return TestSuite(
                language=language,
                total_tests=0,
                passed=0,
                failed=0,
                skipped=0,
                errors=1,
                duration=0.0,
                coverage_percentage=0.0,
                results=[TestResult(
                    language=language,
                    test_name="error",
                    test_file="",
                    status="error",
                    duration=0.0,
                    stdout="",
                    stderr=str(e),
                    error_message=str(e)
                )]
            )
    
    def _parse_test_results(self, language: str, result: subprocess.CompletedProcess, 
                           test_files: List[Path]) -> List[TestResult]:
        """Parse test results based on language and framework"""
        results = []
        
        if result.returncode == 0:
            # Tests passed
            for test_file in test_files:
                results.append(TestResult(
                    language=language,
                    test_name=test_file.stem,
                    test_file=str(test_file),
                    status='passed',
                    duration=0.0,
                    stdout=result.stdout,
                    stderr=result.stderr
                ))
        else:
            # Tests failed - try to parse detailed results
            results = self._parse_failed_results(language, result, test_files)
        
        return results
    
    def _parse_failed_results(self, language: str, result: subprocess.CompletedProcess, 
                             test_files: List[Path]) -> List[TestResult]:
        """Parse failed test results with detailed information"""
        results = []
        
        # This is a simplified parser - in a real implementation, you'd have
        # language-specific parsers for each test framework
        lines = result.stdout.split('\n') + result.stderr.split('\n')
        
        for line in lines:
            if any(keyword in line.lower() for keyword in ['fail', 'error', 'exception']):
                # Extract test name from line
                test_name = self._extract_test_name(line, language)
                results.append(TestResult(
                    language=language,
                    test_name=test_name or "unknown",
                    test_file="",
                    status='failed',
                    duration=0.0,
                    stdout=result.stdout,
                    stderr=result.stderr,
                    error_message=line.strip()
                ))
        
        # If no specific failures found, create a general failure result
        if not results:
            results.append(TestResult(
                language=language,
                test_name="general_failure",
                test_file="",
                status='failed',
                duration=0.0,
                stdout=result.stdout,
                stderr=result.stderr,
                error_message="Test execution failed"
            ))
        
        return results
    
    def _extract_test_name(self, line: str, language: str) -> str:
        """Extract test name from a test output line"""
        # Simple extraction - could be enhanced with regex patterns
        if '::' in line:
            return line.split('::')[-1].strip()
        elif 'test' in line.lower():
            words = line.split()
            for i, word in enumerate(words):
                if 'test' in word.lower():
                    if i + 1 < len(words):
                        return words[i + 1]
                    return word
        return "unknown"
    
    def _get_coverage_percentage(self, language: str, lang_path: Path) -> float:
        """Get coverage percentage for a language"""
        try:
            lang_info = self.languages[language]
            cmd = lang_info['coverage_report_cmd']
            
            result = subprocess.run(
                cmd,
                cwd=lang_path,
                capture_output=True,
                text=True,
                timeout=30
            )
            
            if result.returncode == 0:
                # Parse coverage output - this is framework-specific
                return self._parse_coverage_output(language, result.stdout)
            
        except Exception as e:
            logger.warning(f"Failed to get coverage for {language}: {e}")
        
        return 0.0
    
    def _parse_coverage_output(self, language: str, output: str) -> float:
        """Parse coverage output to extract percentage"""
        # This is a simplified parser - real implementation would be framework-specific
        lines = output.split('\n')
        for line in lines:
            if '%' in line and any(keyword in line.lower() for keyword in ['coverage', 'total']):
                try:
                    # Extract percentage from line
                    import re
                    match = re.search(r'(\d+(?:\.\d+)?)%', line)
                    if match:
                        return float(match.group(1))
                except:
                    pass
        return 0.0
    
    def run_all_tests(self, languages: List[str] = None, coverage: bool = False, 
                     parallel: bool = False) -> Dict[str, TestSuite]:
        """Run tests for all languages or specified languages"""
        if languages is None:
            languages = list(self.languages.keys())
        
        results = {}
        
        if parallel:
            # Run tests in parallel
            threads = []
            for language in languages:
                thread = threading.Thread(
                    target=lambda lang: results.update({lang: self.run_tests_for_language(lang, coverage=coverage)}),
                    args=(language,)
                )
                threads.append(thread)
                thread.start()
            
            for thread in threads:
                thread.join()
        else:
            # Run tests sequentially
            for language in languages:
                results[language] = self.run_tests_for_language(language, coverage=coverage)
        
        return results
    
    def generate_test_report(self, test_suites: Dict[str, TestSuite], 
                           output_format: str = 'json') -> str:
        """Generate a comprehensive test report"""
        total_tests = sum(suite.total_tests for suite in test_suites.values())
        total_passed = sum(suite.passed for suite in test_suites.values())
        total_failed = sum(suite.failed for suite in test_suites.values())
        total_skipped = sum(suite.skipped for suite in test_suites.values())
        total_errors = sum(suite.errors for suite in test_suites.values())
        total_duration = sum(suite.duration for suite in test_suites.values())
        
        report = {
            'summary': {
                'total_tests': total_tests,
                'passed': total_passed,
                'failed': total_failed,
                'skipped': total_skipped,
                'errors': total_errors,
                'success_rate': (total_passed / total_tests * 100) if total_tests > 0 else 0,
                'total_duration': total_duration,
                'timestamp': datetime.now().isoformat()
            },
            'languages': {
                lang: {
                    'summary': {
                        'total_tests': suite.total_tests,
                        'passed': suite.passed,
                        'failed': suite.failed,
                        'skipped': suite.skipped,
                        'errors': suite.errors,
                        'duration': suite.duration,
                        'coverage_percentage': suite.coverage_percentage
                    },
                    'results': [asdict(result) for result in suite.results]
                }
                for lang, suite in test_suites.items()
            }
        }
        
        if output_format == 'json':
            return json.dumps(report, indent=2)
        elif output_format == 'html':
            return self._generate_html_report(report)
        else:
            return self._generate_text_report(report)
    
    def _generate_html_report(self, report: Dict[str, Any]) -> str:
        """Generate HTML test report"""
        html = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <title>TuskLang Multi-Language Test Report</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 20px; }}
                .summary {{ background: #f5f5f5; padding: 15px; border-radius: 5px; }}
                .language {{ margin: 20px 0; border: 1px solid #ddd; padding: 15px; }}
                .passed {{ color: green; }}
                .failed {{ color: red; }}
                .skipped {{ color: orange; }}
                .error {{ color: red; font-weight: bold; }}
            </style>
        </head>
        <body>
            <h1>TuskLang Multi-Language Test Report</h1>
            <div class="summary">
                <h2>Summary</h2>
                <p>Total Tests: {report['summary']['total_tests']}</p>
                <p class="passed">Passed: {report['summary']['passed']}</p>
                <p class="failed">Failed: {report['summary']['failed']}</p>
                <p class="skipped">Skipped: {report['summary']['skipped']}</p>
                <p class="error">Errors: {report['summary']['errors']}</p>
                <p>Success Rate: {report['summary']['success_rate']:.1f}%</p>
                <p>Total Duration: {report['summary']['total_duration']:.2f}s</p>
            </div>
        """
        
        for lang, data in report['languages'].items():
            html += f"""
            <div class="language">
                <h3>{lang.upper()}</h3>
                <p>Tests: {data['summary']['total_tests']} | 
                   Passed: {data['summary']['passed']} | 
                   Failed: {data['summary']['failed']} | 
                   Duration: {data['summary']['duration']:.2f}s | 
                   Coverage: {data['summary']['coverage_percentage']:.1f}%</p>
            </div>
            """
        
        html += "</body></html>"
        return html
    
    def _generate_text_report(self, report: Dict[str, Any]) -> str:
        """Generate text test report"""
        text = f"""
TuskLang Multi-Language Test Report
===================================

Summary:
  Total Tests: {report['summary']['total_tests']}
  Passed: {report['summary']['passed']}
  Failed: {report['summary']['failed']}
  Skipped: {report['summary']['skipped']}
  Errors: {report['summary']['errors']}
  Success Rate: {report['summary']['success_rate']:.1f}%
  Total Duration: {report['summary']['total_duration']:.2f}s

Language Results:
"""
        
        for lang, data in report['languages'].items():
            text += f"""
{lang.upper()}:
  Tests: {data['summary']['total_tests']}
  Passed: {data['summary']['passed']}
  Failed: {data['summary']['failed']}
  Duration: {data['summary']['duration']:.2f}s
  Coverage: {data['summary']['coverage_percentage']:.1f}%
"""
        
        return text

def main():
    """CLI for test runner"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang Multi-Language Test Runner')
    parser.add_argument('--languages', nargs='+', help='Specific languages to test')
    parser.add_argument('--coverage', action='store_true', help='Run with coverage')
    parser.add_argument('--parallel', action='store_true', help='Run tests in parallel')
    parser.add_argument('--format', choices=['json', 'html', 'text'], default='json', help='Output format')
    parser.add_argument('--output', help='Output file path')
    
    args = parser.parse_args()
    
    runner = MultiLanguageTestRunner()
    
    # Run tests
    test_suites = runner.run_all_tests(
        languages=args.languages,
        coverage=args.coverage,
        parallel=args.parallel
    )
    
    # Generate report
    report = runner.generate_test_report(test_suites, args.format)
    
    # Output report
    if args.output:
        with open(args.output, 'w') as f:
            f.write(report)
        print(f"Test report saved to {args.output}")
    else:
        print(report)

if __name__ == '__main__':
    main() 