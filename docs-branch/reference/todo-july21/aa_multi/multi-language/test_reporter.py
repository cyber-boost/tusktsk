#!/usr/bin/env python3
"""
TuskLang Multi-Language Test Reporter
Aggregates and reports test results across all language SDKs
"""

import os
import json
import time
import sqlite3
from pathlib import Path
from typing import Dict, List, Optional, Any, Tuple
from dataclasses import dataclass, asdict
from datetime import datetime, timedelta
import logging
try:
    import matplotlib.pyplot as plt
    import pandas as pd
    PANDAS_AVAILABLE = True
except ImportError:
    PANDAS_AVAILABLE = False
    plt = None
    pd = None

from collections import defaultdict

logger = logging.getLogger(__name__)

@dataclass
class TestMetrics:
    """Test performance and quality metrics"""
    total_tests: int
    passed: int
    failed: int
    skipped: int
    errors: int
    duration: float
    coverage_percentage: float
    success_rate: float
    avg_test_duration: float
    slowest_test_duration: float
    fastest_test_duration: float

@dataclass
class CrossLanguageAnalysis:
    """Cross-language test analysis"""
    language_comparison: Dict[str, TestMetrics]
    common_failures: List[Dict[str, Any]]
    performance_correlation: Dict[str, float]
    coverage_gaps: List[str]
    recommendations: List[str]

class MultiLanguageTestReporter:
    """Aggregates and analyzes test results across all language SDKs"""
    
    def __init__(self, results_dir: Path = None):
        if results_dir is None:
            self.results_dir = Path(tempfile.mkdtemp(prefix='tsk_test_results_'))
        else:
            self.results_dir = results_dir
        
        self.db_path = self.results_dir / 'test_results.db'
        self.reports_dir = self.results_dir / 'reports'
        
        # Create directories
        self.reports_dir.mkdir(exist_ok=True)
        
        # Initialize database
        self._init_database()
    
    def _init_database(self):
        """Initialize SQLite database for storing test results"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Create tables
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS test_runs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                run_id TEXT UNIQUE,
                timestamp TEXT,
                total_languages INTEGER,
                total_tests INTEGER,
                total_passed INTEGER,
                total_failed INTEGER,
                total_skipped INTEGER,
                total_errors INTEGER,
                total_duration REAL,
                overall_success_rate REAL
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS language_results (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                run_id TEXT,
                language TEXT,
                total_tests INTEGER,
                passed INTEGER,
                failed INTEGER,
                skipped INTEGER,
                errors INTEGER,
                duration REAL,
                coverage_percentage REAL,
                success_rate REAL,
                FOREIGN KEY (run_id) REFERENCES test_runs (run_id)
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS test_details (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                run_id TEXT,
                language TEXT,
                test_name TEXT,
                test_file TEXT,
                status TEXT,
                duration REAL,
                error_message TEXT,
                stdout TEXT,
                stderr TEXT,
                FOREIGN KEY (run_id) REFERENCES test_runs (run_id)
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS failure_patterns (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                pattern TEXT,
                language TEXT,
                occurrence_count INTEGER,
                first_seen TEXT,
                last_seen TEXT,
                severity TEXT
            )
        ''')
        
        conn.commit()
        conn.close()
    
    def store_test_results(self, run_id: str, test_suites: Dict[str, Any]) -> bool:
        """Store test results in the database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            # Calculate overall metrics
            total_tests = sum(suite['total_tests'] for suite in test_suites.values())
            total_passed = sum(suite['passed'] for suite in test_suites.values())
            total_failed = sum(suite['failed'] for suite in test_suites.values())
            total_skipped = sum(suite['skipped'] for suite in test_suites.values())
            total_errors = sum(suite['errors'] for suite in test_suites.values())
            total_duration = sum(suite['duration'] for suite in test_suites.values())
            overall_success_rate = (total_passed / total_tests * 100) if total_tests > 0 else 0
            
            # Store run summary
            cursor.execute('''
                INSERT INTO test_runs 
                (run_id, timestamp, total_languages, total_tests, total_passed, 
                 total_failed, total_skipped, total_errors, total_duration, overall_success_rate)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                run_id,
                datetime.now().isoformat(),
                len(test_suites),
                total_tests,
                total_passed,
                total_failed,
                total_skipped,
                total_errors,
                total_duration,
                overall_success_rate
            ))
            
            # Store language results
            for language, suite in test_suites.items():
                cursor.execute('''
                    INSERT INTO language_results 
                    (run_id, language, total_tests, passed, failed, skipped, errors, 
                     duration, coverage_percentage, success_rate)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ''', (
                    run_id,
                    language,
                    suite['total_tests'],
                    suite['passed'],
                    suite['failed'],
                    suite['skipped'],
                    suite['errors'],
                    suite['duration'],
                    suite.get('coverage_percentage', 0.0),
                    (suite['passed'] / suite['total_tests'] * 100) if suite['total_tests'] > 0 else 0
                ))
                
                # Store individual test details
                for result in suite.get('results', []):
                    cursor.execute('''
                        INSERT INTO test_details 
                        (run_id, language, test_name, test_file, status, duration, 
                         error_message, stdout, stderr)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                    ''', (
                        run_id,
                        language,
                        result['test_name'],
                        result['test_file'],
                        result['status'],
                        result['duration'],
                        result.get('error_message'),
                        result.get('stdout', ''),
                        result.get('stderr', '')
                    ))
            
            conn.commit()
            conn.close()
            return True
            
        except Exception as e:
            logger.error(f"Failed to store test results: {e}")
            return False
    
    def get_test_metrics(self, run_id: str = None, language: str = None, 
                        time_range: timedelta = None) -> TestMetrics:
        """Get aggregated test metrics"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Build query
        query = '''
            SELECT 
                SUM(total_tests) as total_tests,
                SUM(passed) as passed,
                SUM(failed) as failed,
                SUM(skipped) as skipped,
                SUM(errors) as errors,
                SUM(duration) as duration,
                AVG(coverage_percentage) as coverage_percentage
            FROM language_results
        '''
        
        params = []
        conditions = []
        
        if run_id:
            conditions.append("run_id = ?")
            params.append(run_id)
        
        if language:
            conditions.append("language = ?")
            params.append(language)
        
        if time_range:
            cutoff_time = (datetime.now() - time_range).isoformat()
            conditions.append("run_id IN (SELECT run_id FROM test_runs WHERE timestamp > ?)")
            params.append(cutoff_time)
        
        if conditions:
            query += " WHERE " + " AND ".join(conditions)
        
        cursor.execute(query, params)
        row = cursor.fetchone()
        conn.close()
        
        if not row or row[0] is None:
            return TestMetrics(0, 0, 0, 0, 0, 0.0, 0.0, 0.0, 0.0, 0.0)
        
        total_tests, passed, failed, skipped, errors, duration, coverage = row
        
        success_rate = (passed / total_tests * 100) if total_tests > 0 else 0
        avg_test_duration = duration / total_tests if total_tests > 0 else 0
        
        # Get min/max test durations
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            SELECT MIN(duration), MAX(duration) FROM test_details 
            WHERE duration > 0
        ''')
        
        min_duration, max_duration = cursor.fetchone()
        conn.close()
        
        return TestMetrics(
            total_tests=total_tests,
            passed=passed,
            failed=failed,
            skipped=skipped,
            errors=errors,
            duration=duration,
            coverage_percentage=coverage or 0.0,
            success_rate=success_rate,
            avg_test_duration=avg_test_duration,
            slowest_test_duration=max_duration or 0.0,
            fastest_test_duration=min_duration or 0.0
        )
    
    def analyze_cross_language_patterns(self, time_range: timedelta = None) -> CrossLanguageAnalysis:
        """Analyze patterns across languages"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Get language comparison
        cutoff_time = (datetime.now() - time_range).isoformat() if time_range else None
        
        query = '''
            SELECT language, 
                   SUM(total_tests) as total_tests,
                   SUM(passed) as passed,
                   SUM(failed) as failed,
                   SUM(skipped) as skipped,
                   SUM(errors) as errors,
                   SUM(duration) as duration,
                   AVG(coverage_percentage) as coverage_percentage
            FROM language_results
        '''
        
        params = []
        if cutoff_time:
            query += " WHERE run_id IN (SELECT run_id FROM test_runs WHERE timestamp > ?)"
            params.append(cutoff_time)
        
        query += " GROUP BY language"
        
        cursor.execute(query, params)
        rows = cursor.fetchall()
        
        language_comparison = {}
        for row in rows:
            language, total_tests, passed, failed, skipped, errors, duration, coverage = row
            
            success_rate = (passed / total_tests * 100) if total_tests > 0 else 0
            avg_test_duration = duration / total_tests if total_tests > 0 else 0
            
            language_comparison[language] = TestMetrics(
                total_tests=total_tests,
                passed=passed,
                failed=failed,
                skipped=skipped,
                errors=errors,
                duration=duration,
                coverage_percentage=coverage or 0.0,
                success_rate=success_rate,
                avg_test_duration=avg_test_duration,
                slowest_test_duration=0.0,  # Would need additional query
                fastest_test_duration=0.0   # Would need additional query
            )
        
        # Find common failures
        cursor.execute('''
            SELECT error_message, COUNT(*) as count, language
            FROM test_details 
            WHERE status = 'failed' AND error_message IS NOT NULL
            GROUP BY error_message, language
            ORDER BY count DESC
            LIMIT 10
        ''')
        
        common_failures = []
        for row in cursor.fetchall():
            error_message, count, language = row
            common_failures.append({
                'error_message': error_message,
                'count': count,
                'language': language
            })
        
        # Calculate performance correlation
        performance_correlation = {}
        if len(language_comparison) > 1:
            languages = list(language_comparison.keys())
            for i, lang1 in enumerate(languages):
                for lang2 in languages[i+1:]:
                    metrics1 = language_comparison[lang1]
                    metrics2 = language_comparison[lang2]
                    
                    # Simple correlation based on success rates
                    correlation = 1.0 - abs(metrics1.success_rate - metrics2.success_rate) / 100.0
                    performance_correlation[f"{lang1}-{lang2}"] = correlation
        
        # Identify coverage gaps
        coverage_gaps = []
        for language, metrics in language_comparison.items():
            if metrics.coverage_percentage < 80.0:
                coverage_gaps.append(f"{language}: {metrics.coverage_percentage:.1f}%")
        
        # Generate recommendations
        recommendations = []
        
        # Success rate recommendations
        low_success_languages = [lang for lang, metrics in language_comparison.items() 
                               if metrics.success_rate < 90.0]
        if low_success_languages:
            recommendations.append(f"Focus on improving test reliability in: {', '.join(low_success_languages)}")
        
        # Coverage recommendations
        if coverage_gaps:
            recommendations.append(f"Improve test coverage in: {', '.join(coverage_gaps)}")
        
        # Performance recommendations
        slow_languages = [lang for lang, metrics in language_comparison.items() 
                         if metrics.avg_test_duration > 1.0]
        if slow_languages:
            recommendations.append(f"Optimize test performance in: {', '.join(slow_languages)}")
        
        conn.close()
        
        return CrossLanguageAnalysis(
            language_comparison=language_comparison,
            common_failures=common_failures,
            performance_correlation=performance_correlation,
            coverage_gaps=coverage_gaps,
            recommendations=recommendations
        )
    
    def generate_comprehensive_report(self, run_id: str = None, 
                                    output_format: str = 'html') -> str:
        """Generate a comprehensive test report"""
        # Get metrics
        metrics = self.get_test_metrics(run_id)
        analysis = self.analyze_cross_language_patterns()
        
        # Get recent runs for trend analysis
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            SELECT run_id, timestamp, overall_success_rate, total_tests, total_duration
            FROM test_runs 
            ORDER BY timestamp DESC 
            LIMIT 10
        ''')
        
        recent_runs = cursor.fetchall()
        conn.close()
        
        report_data = {
            'metrics': asdict(metrics),
            'analysis': {
                'language_comparison': {lang: asdict(metrics) for lang, metrics in analysis.language_comparison.items()},
                'common_failures': analysis.common_failures,
                'performance_correlation': analysis.performance_correlation,
                'coverage_gaps': analysis.coverage_gaps,
                'recommendations': analysis.recommendations
            },
            'recent_runs': [
                {
                    'run_id': run_id,
                    'timestamp': timestamp,
                    'success_rate': success_rate,
                    'total_tests': total_tests,
                    'duration': duration
                }
                for run_id, timestamp, success_rate, total_tests, duration in recent_runs
            ],
            'generated_at': datetime.now().isoformat()
        }
        
        if output_format == 'json':
            return json.dumps(report_data, indent=2)
        elif output_format == 'html':
            return self._generate_html_report(report_data)
        else:
            return self._generate_text_report(report_data)
    
    def _generate_html_report(self, report_data: Dict[str, Any]) -> str:
        """Generate HTML report"""
        html = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <title>TuskLang Multi-Language Test Report</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }}
                .container {{ max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; }}
                .header {{ background: #2c3e50; color: white; padding: 20px; border-radius: 5px; margin-bottom: 20px; }}
                .metrics {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin-bottom: 20px; }}
                .metric-card {{ background: #ecf0f1; padding: 15px; border-radius: 5px; text-align: center; }}
                .metric-value {{ font-size: 2em; font-weight: bold; color: #2c3e50; }}
                .metric-label {{ color: #7f8c8d; margin-top: 5px; }}
                .section {{ margin: 20px 0; }}
                .language-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 15px; }}
                .language-card {{ border: 1px solid #ddd; padding: 15px; border-radius: 5px; }}
                .success-high {{ color: #27ae60; }}
                .success-medium {{ color: #f39c12; }}
                .success-low {{ color: #e74c3c; }}
                .recommendations {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; }}
            </style>
        </head>
        <body>
            <div class="container">
                <div class="header">
                    <h1>TuskLang Multi-Language Test Report</h1>
                    <p>Generated: {report_data['generated_at']}</p>
                </div>
                
                <div class="metrics">
                    <div class="metric-card">
                        <div class="metric-value">{report_data['metrics']['total_tests']}</div>
                        <div class="metric-label">Total Tests</div>
                    </div>
                    <div class="metric-card">
                        <div class="metric-value success-high">{report_data['metrics']['success_rate']:.1f}%</div>
                        <div class="metric-label">Success Rate</div>
                    </div>
                    <div class="metric-card">
                        <div class="metric-value">{report_data['metrics']['duration']:.1f}s</div>
                        <div class="metric-label">Total Duration</div>
                    </div>
                    <div class="metric-card">
                        <div class="metric-value">{report_data['metrics']['coverage_percentage']:.1f}%</div>
                        <div class="metric-label">Coverage</div>
                    </div>
                </div>
                
                <div class="section">
                    <h2>Language Comparison</h2>
                    <div class="language-grid">
        """
        
        for language, metrics in report_data['analysis']['language_comparison'].items():
            success_class = 'success-high' if metrics['success_rate'] >= 90 else 'success-medium' if metrics['success_rate'] >= 70 else 'success-low'
            
            html += f"""
                        <div class="language-card">
                            <h3>{language.upper()}</h3>
                            <p><strong>Tests:</strong> {metrics['total_tests']}</p>
                            <p><strong>Success Rate:</strong> <span class="{success_class}">{metrics['success_rate']:.1f}%</span></p>
                            <p><strong>Duration:</strong> {metrics['duration']:.1f}s</p>
                            <p><strong>Coverage:</strong> {metrics['coverage_percentage']:.1f}%</p>
                        </div>
            """
        
        html += """
                    </div>
                </div>
                
                <div class="section">
                    <h2>Common Failures</h2>
                    <ul>
        """
        
        for failure in report_data['analysis']['common_failures'][:5]:
            html += f"""
                        <li><strong>{failure['language']}:</strong> {failure['error_message'][:100]}... ({failure['count']} occurrences)</li>
            """
        
        html += """
                    </ul>
                </div>
                
                <div class="section">
                    <h2>Recommendations</h2>
                    <div class="recommendations">
                        <ul>
        """
        
        for rec in report_data['analysis']['recommendations']:
            html += f"<li>{rec}</li>"
        
        html += """
                        </ul>
                    </div>
                </div>
            </div>
        </body>
        </html>
        """
        
        return html
    
    def _generate_text_report(self, report_data: Dict[str, Any]) -> str:
        """Generate text report"""
        text = f"""
TuskLang Multi-Language Test Report
===================================

Generated: {report_data['generated_at']}

Summary Metrics:
  Total Tests: {report_data['metrics']['total_tests']}
  Success Rate: {report_data['metrics']['success_rate']:.1f}%
  Total Duration: {report_data['metrics']['duration']:.1f}s
  Coverage: {report_data['metrics']['coverage_percentage']:.1f}%

Language Comparison:
"""
        
        for language, metrics in report_data['analysis']['language_comparison'].items():
            text += f"""
{language.upper()}:
  Tests: {metrics['total_tests']}
  Success Rate: {metrics['success_rate']:.1f}%
  Duration: {metrics['duration']:.1f}s
  Coverage: {metrics['coverage_percentage']:.1f}%
"""
        
        text += "\nCommon Failures:\n"
        for failure in report_data['analysis']['common_failures'][:5]:
            text += f"  {failure['language']}: {failure['error_message'][:80]}... ({failure['count']} occurrences)\n"
        
        text += "\nRecommendations:\n"
        for rec in report_data['analysis']['recommendations']:
            text += f"  - {rec}\n"
        
        return text
    
    def export_results(self, run_id: str, export_format: str = 'json') -> str:
        """Export test results in various formats"""
        conn = sqlite3.connect(self.db_path)
        
        if export_format == 'json':
            # Export as JSON
            cursor = conn.cursor()
            
            cursor.execute('''
                SELECT * FROM test_runs WHERE run_id = ?
            ''', (run_id,))
            
            run_data = cursor.fetchone()
            if not run_data:
                conn.close()
                return "{}"
            
            cursor.execute('''
                SELECT * FROM language_results WHERE run_id = ?
            ''', (run_id,))
            
            language_results = cursor.fetchall()
            
            cursor.execute('''
                SELECT * FROM test_details WHERE run_id = ?
            ''', (run_id,))
            
            test_details = cursor.fetchall()
            
            export_data = {
                'run_id': run_id,
                'run_data': run_data,
                'language_results': language_results,
                'test_details': test_details
            }
            
            conn.close()
            return json.dumps(export_data, indent=2)
        
        elif export_format == 'csv':
            if not PANDAS_AVAILABLE:
                return "CSV export requires pandas to be installed"
            
            # Export as CSV
            df_runs = pd.read_sql_query('''
                SELECT * FROM test_runs WHERE run_id = ?
            ''', conn, params=(run_id,))
            
            df_languages = pd.read_sql_query('''
                SELECT * FROM language_results WHERE run_id = ?
            ''', conn, params=(run_id,))
            
            df_details = pd.read_sql_query('''
                SELECT * FROM test_details WHERE run_id = ?
            ''', conn, params=(run_id,))
            
            conn.close()
            
            # Save to CSV files
            csv_dir = self.reports_dir / 'csv_exports'
            csv_dir.mkdir(exist_ok=True)
            
            df_runs.to_csv(csv_dir / f'{run_id}_runs.csv', index=False)
            df_languages.to_csv(csv_dir / f'{run_id}_languages.csv', index=False)
            df_details.to_csv(csv_dir / f'{run_id}_details.csv', index=False)
            
            return f"CSV files exported to {csv_dir}"
        
        conn.close()
        return "Unsupported export format"

def main():
    """CLI for test reporter"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang Multi-Language Test Reporter')
    parser.add_argument('--store-results', help='Store test results from JSON file')
    parser.add_argument('--get-metrics', help='Get metrics for specific run ID')
    parser.add_argument('--analyze', action='store_true', help='Analyze cross-language patterns')
    parser.add_argument('--generate-report', help='Generate comprehensive report')
    parser.add_argument('--export', nargs=2, metavar=('RUN_ID', 'FORMAT'), help='Export results')
    parser.add_argument('--format', choices=['json', 'html', 'text'], default='json', help='Output format')
    
    args = parser.parse_args()
    
    reporter = MultiLanguageTestReporter()
    
    if args.store_results:
        with open(args.store_results, 'r') as f:
            test_suites = json.load(f)
        
        run_id = f"run_{int(time.time())}"
        success = reporter.store_test_results(run_id, test_suites)
        if success:
            print(f"Results stored with run ID: {run_id}")
        else:
            print("Failed to store results")
    
    elif args.get_metrics:
        metrics = reporter.get_test_metrics(args.get_metrics)
        print(json.dumps(asdict(metrics), indent=2))
    
    elif args.analyze:
        analysis = reporter.analyze_cross_language_patterns()
        print(json.dumps({
            'language_comparison': {lang: asdict(metrics) for lang, metrics in analysis.language_comparison.items()},
            'common_failures': analysis.common_failures,
            'recommendations': analysis.recommendations
        }, indent=2))
    
    elif args.generate_report:
        report = reporter.generate_comprehensive_report(args.generate_report, args.format)
        print(report)
    
    elif args.export:
        run_id, export_format = args.export
        result = reporter.export_results(run_id, export_format)
        print(result)
    
    else:
        parser.print_help()

if __name__ == '__main__':
    main() 