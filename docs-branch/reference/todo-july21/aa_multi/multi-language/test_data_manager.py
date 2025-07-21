#!/usr/bin/env python3
"""
TuskLang Cross-Language Test Data Manager
Manages test data sharing and fixtures across all language SDKs
"""

import os
import json
import tempfile
import shutil
import hashlib
from pathlib import Path
from typing import Dict, List, Optional, Any, Union
from dataclasses import dataclass, asdict
import logging
import pickle
import sqlite3
from datetime import datetime, timedelta

logger = logging.getLogger(__name__)

@dataclass
class TestFixture:
    """Represents a test fixture that can be shared across languages"""
    name: str
    language: str
    data_type: str  # 'json', 'csv', 'binary', 'text'
    content: Any
    metadata: Dict[str, Any]
    created_at: datetime
    expires_at: Optional[datetime] = None

@dataclass
class TestData:
    """Represents test data that can be shared between languages"""
    key: str
    value: Any
    data_type: str
    language_source: str
    languages_used: List[str]
    created_at: datetime
    last_accessed: datetime
    access_count: int

class CrossLanguageTestDataManager:
    """Manages test data and fixtures across all TuskLang language SDKs"""
    
    def __init__(self, data_dir: Path = None):
        if data_dir is None:
            self.data_dir = Path(tempfile.mkdtemp(prefix='tsk_test_data_'))
        else:
            self.data_dir = data_dir
        
        self.fixtures_dir = self.data_dir / 'fixtures'
        self.shared_data_dir = self.data_dir / 'shared'
        self.db_path = self.data_dir / 'test_data.db'
        
        # Create directories
        self.fixtures_dir.mkdir(exist_ok=True)
        self.shared_data_dir.mkdir(exist_ok=True)
        
        # Initialize database
        self._init_database()
        
        # Supported data types for each language
        self.language_data_types = {
            'python': ['json', 'pickle', 'csv', 'yaml', 'text', 'binary'],
            'rust': ['json', 'csv', 'text', 'binary'],
            'javascript': ['json', 'csv', 'text', 'binary'],
            'ruby': ['json', 'yaml', 'csv', 'text', 'binary'],
            'csharp': ['json', 'xml', 'csv', 'text', 'binary'],
            'go': ['json', 'csv', 'text', 'binary'],
            'php': ['json', 'csv', 'text', 'binary'],
            'java': ['json', 'xml', 'csv', 'text', 'binary'],
            'bash': ['json', 'csv', 'text', 'binary']
        }
    
    def _init_database(self):
        """Initialize SQLite database for tracking test data"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Create tables
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS test_data (
                key TEXT PRIMARY KEY,
                value TEXT,
                data_type TEXT,
                language_source TEXT,
                languages_used TEXT,
                created_at TEXT,
                last_accessed TEXT,
                access_count INTEGER DEFAULT 0
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS fixtures (
                name TEXT,
                language TEXT,
                data_type TEXT,
                file_path TEXT,
                metadata TEXT,
                created_at TEXT,
                expires_at TEXT,
                PRIMARY KEY (name, language)
            )
        ''')
        
        conn.commit()
        conn.close()
    
    def create_fixture(self, name: str, language: str, data: Any, 
                      data_type: str = 'json', metadata: Dict[str, Any] = None,
                      expires_in: Optional[timedelta] = None) -> TestFixture:
        """Create a test fixture for a specific language"""
        if metadata is None:
            metadata = {}
        
        created_at = datetime.now()
        expires_at = None
        if expires_in:
            expires_at = created_at + expires_in
        
        # Save fixture to file
        fixture_file = self.fixtures_dir / f"{language}_{name}.{data_type}"
        
        if data_type == 'json':
            with open(fixture_file, 'w') as f:
                json.dump(data, f, indent=2, default=str)
        elif data_type == 'pickle':
            with open(fixture_file, 'wb') as f:
                pickle.dump(data, f)
        elif data_type == 'csv':
            import csv
            with open(fixture_file, 'w', newline='') as f:
                if isinstance(data, list) and data:
                    writer = csv.DictWriter(f, fieldnames=data[0].keys())
                    writer.writeheader()
                    writer.writerows(data)
                else:
                    writer = csv.writer(f)
                    writer.writerows(data)
        else:
            # Text or binary
            mode = 'w' if data_type == 'text' else 'wb'
            with open(fixture_file, mode) as f:
                f.write(data)
        
        fixture = TestFixture(
            name=name,
            language=language,
            data_type=data_type,
            content=data,
            metadata=metadata,
            created_at=created_at,
            expires_at=expires_at
        )
        
        # Save to database
        self._save_fixture_to_db(fixture, str(fixture_file))
        
        return fixture
    
    def _save_fixture_to_db(self, fixture: TestFixture, file_path: str):
        """Save fixture metadata to database"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            INSERT OR REPLACE INTO fixtures 
            (name, language, data_type, file_path, metadata, created_at, expires_at)
            VALUES (?, ?, ?, ?, ?, ?, ?)
        ''', (
            fixture.name,
            fixture.language,
            fixture.data_type,
            file_path,
            json.dumps(fixture.metadata),
            fixture.created_at.isoformat(),
            fixture.expires_at.isoformat() if fixture.expires_at else None
        ))
        
        conn.commit()
        conn.close()
    
    def get_fixture(self, name: str, language: str) -> Optional[TestFixture]:
        """Get a test fixture for a specific language"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            SELECT name, language, data_type, file_path, metadata, created_at, expires_at
            FROM fixtures WHERE name = ? AND language = ?
        ''', (name, language))
        
        row = cursor.fetchone()
        conn.close()
        
        if not row:
            return None
        
        name, language, data_type, file_path, metadata_str, created_at_str, expires_at_str = row
        
        # Check if expired
        if expires_at_str:
            expires_at = datetime.fromisoformat(expires_at_str)
            if datetime.now() > expires_at:
                self.delete_fixture(name, language)
                return None
        
        # Load content from file
        content = self._load_fixture_content(file_path, data_type)
        
        return TestFixture(
            name=name,
            language=language,
            data_type=data_type,
            content=content,
            metadata=json.loads(metadata_str),
            created_at=datetime.fromisoformat(created_at_str),
            expires_at=datetime.fromisoformat(expires_at_str) if expires_at_str else None
        )
    
    def _load_fixture_content(self, file_path: str, data_type: str) -> Any:
        """Load fixture content from file"""
        if not os.path.exists(file_path):
            return None
        
        try:
            if data_type == 'json':
                with open(file_path, 'r') as f:
                    return json.load(f)
            elif data_type == 'pickle':
                with open(file_path, 'rb') as f:
                    return pickle.load(f)
            elif data_type == 'csv':
                import csv
                with open(file_path, 'r', newline='') as f:
                    reader = csv.DictReader(f)
                    return list(reader)
            else:
                # Text or binary
                mode = 'r' if data_type == 'text' else 'rb'
                with open(file_path, mode) as f:
                    return f.read()
        except Exception as e:
            logger.error(f"Failed to load fixture content from {file_path}: {e}")
            return None
    
    def delete_fixture(self, name: str, language: str) -> bool:
        """Delete a test fixture"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            SELECT file_path FROM fixtures WHERE name = ? AND language = ?
        ''', (name, language))
        
        row = cursor.fetchone()
        if row:
            file_path = row[0]
            # Delete file
            if os.path.exists(file_path):
                os.remove(file_path)
            
            # Delete from database
            cursor.execute('''
                DELETE FROM fixtures WHERE name = ? AND language = ?
            ''', (name, language))
            
            conn.commit()
            conn.close()
            return True
        
        conn.close()
        return False
    
    def list_fixtures(self, language: str = None) -> List[TestFixture]:
        """List all fixtures, optionally filtered by language"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        if language:
            cursor.execute('''
                SELECT name, language, data_type, file_path, metadata, created_at, expires_at
                FROM fixtures WHERE language = ?
            ''', (language,))
        else:
            cursor.execute('''
                SELECT name, language, data_type, file_path, metadata, created_at, expires_at
                FROM fixtures
            ''')
        
        rows = cursor.fetchall()
        conn.close()
        
        fixtures = []
        for row in rows:
            name, language, data_type, file_path, metadata_str, created_at_str, expires_at_str = row
            
            # Check if expired
            if expires_at_str:
                expires_at = datetime.fromisoformat(expires_at_str)
                if datetime.now() > expires_at:
                    self.delete_fixture(name, language)
                    continue
            
            content = self._load_fixture_content(file_path, data_type)
            
            fixtures.append(TestFixture(
                name=name,
                language=language,
                data_type=data_type,
                content=content,
                metadata=json.loads(metadata_str),
                created_at=datetime.fromisoformat(created_at_str),
                expires_at=datetime.fromisoformat(expires_at_str) if expires_at_str else None
            ))
        
        return fixtures
    
    def share_data(self, key: str, value: Any, source_language: str, 
                  target_languages: List[str] = None, data_type: str = 'json') -> TestData:
        """Share test data between languages"""
        if target_languages is None:
            target_languages = list(self.language_data_types.keys())
        
        # Convert data to target language formats
        shared_data = {}
        for lang in target_languages:
            if data_type in self.language_data_types.get(lang, []):
                converted_data = self._convert_data_for_language(value, lang, data_type)
                shared_data[lang] = converted_data
        
        # Save shared data
        test_data = TestData(
            key=key,
            value=value,
            data_type=data_type,
            language_source=source_language,
            languages_used=target_languages,
            created_at=datetime.now(),
            last_accessed=datetime.now(),
            access_count=1
        )
        
        self._save_shared_data_to_db(test_data, shared_data)
        
        return test_data
    
    def _convert_data_for_language(self, data: Any, language: str, data_type: str) -> Any:
        """Convert data to format suitable for target language"""
        if data_type == 'json':
            # JSON is universal
            return data
        elif data_type == 'csv':
            # Convert to CSV format
            if isinstance(data, list) and data:
                import csv
                csv_file = self.shared_data_dir / f"{language}_{hash(str(data))}.csv"
                with open(csv_file, 'w', newline='') as f:
                    if isinstance(data[0], dict):
                        writer = csv.DictWriter(f, fieldnames=data[0].keys())
                        writer.writeheader()
                        writer.writerows(data)
                    else:
                        writer = csv.writer(f)
                        writer.writerows(data)
                return str(csv_file)
        elif data_type == 'text':
            # Text is universal
            return str(data)
        elif data_type == 'binary':
            # Save binary data to file
            binary_file = self.shared_data_dir / f"{language}_{hash(str(data))}.bin"
            with open(binary_file, 'wb') as f:
                f.write(data)
            return str(binary_file)
        
        return data
    
    def _save_shared_data_to_db(self, test_data: TestData, shared_data: Dict[str, Any]):
        """Save shared data to database"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            INSERT OR REPLACE INTO test_data 
            (key, value, data_type, language_source, languages_used, created_at, last_accessed, access_count)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        ''', (
            test_data.key,
            json.dumps(test_data.value, default=str),
            test_data.data_type,
            test_data.language_source,
            json.dumps(test_data.languages_used),
            test_data.created_at.isoformat(),
            test_data.last_accessed.isoformat(),
            test_data.access_count
        ))
        
        conn.commit()
        conn.close()
    
    def get_shared_data(self, key: str, language: str) -> Optional[Any]:
        """Get shared test data for a specific language"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            SELECT value, data_type, languages_used, last_accessed, access_count
            FROM test_data WHERE key = ?
        ''', (key,))
        
        row = cursor.fetchone()
        if not row:
            conn.close()
            return None
        
        value_str, data_type, languages_used_str, last_accessed_str, access_count = row
        languages_used = json.loads(languages_used_str)
        
        if language not in languages_used:
            conn.close()
            return None
        
        # Update access statistics
        cursor.execute('''
            UPDATE test_data 
            SET last_accessed = ?, access_count = ?
            WHERE key = ?
        ''', (datetime.now().isoformat(), access_count + 1, key))
        
        conn.commit()
        conn.close()
        
        # Parse value
        try:
            value = json.loads(value_str)
            return self._convert_data_for_language(value, language, data_type)
        except:
            return value_str
    
    def list_shared_data(self) -> List[TestData]:
        """List all shared test data"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        cursor.execute('''
            SELECT key, value, data_type, language_source, languages_used, created_at, last_accessed, access_count
            FROM test_data
        ''')
        
        rows = cursor.fetchall()
        conn.close()
        
        test_data_list = []
        for row in rows:
            key, value_str, data_type, language_source, languages_used_str, created_at_str, last_accessed_str, access_count = row
            
            try:
                value = json.loads(value_str)
            except:
                value = value_str
            
            test_data_list.append(TestData(
                key=key,
                value=value,
                data_type=data_type,
                language_source=language_source,
                languages_used=json.loads(languages_used_str),
                created_at=datetime.fromisoformat(created_at_str),
                last_accessed=datetime.fromisoformat(last_accessed_str),
                access_count=access_count
            ))
        
        return test_data_list
    
    def cleanup_expired_data(self) -> int:
        """Clean up expired fixtures and old shared data"""
        cleaned_count = 0
        
        # Clean expired fixtures
        fixtures = self.list_fixtures()
        for fixture in fixtures:
            if fixture.expires_at and datetime.now() > fixture.expires_at:
                if self.delete_fixture(fixture.name, fixture.language):
                    cleaned_count += 1
        
        # Clean old shared data (older than 7 days)
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        week_ago = (datetime.now() - timedelta(days=7)).isoformat()
        cursor.execute('''
            DELETE FROM test_data WHERE created_at < ?
        ''', (week_ago,))
        
        cleaned_count += cursor.rowcount
        conn.commit()
        conn.close()
        
        return cleaned_count
    
    def generate_data_report(self) -> Dict[str, Any]:
        """Generate a report of test data usage"""
        fixtures = self.list_fixtures()
        shared_data = self.list_shared_data()
        
        # Group by language
        fixture_stats = {}
        for fixture in fixtures:
            if fixture.language not in fixture_stats:
                fixture_stats[fixture.language] = {'count': 0, 'types': {}}
            fixture_stats[fixture.language]['count'] += 1
            fixture_stats[fixture.language]['types'][fixture.data_type] = \
                fixture_stats[fixture.language]['types'].get(fixture.data_type, 0) + 1
        
        # Shared data stats
        shared_stats = {
            'total_keys': len(shared_data),
            'total_accesses': sum(data.access_count for data in shared_data),
            'languages_used': set()
        }
        
        for data in shared_data:
            shared_stats['languages_used'].update(data.languages_used)
        
        shared_stats['languages_used'] = list(shared_stats['languages_used'])
        
        return {
            'fixtures': fixture_stats,
            'shared_data': shared_stats,
            'total_fixtures': len(fixtures),
            'generated_at': datetime.now().isoformat()
        }

def main():
    """CLI for test data manager"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang Cross-Language Test Data Manager')
    parser.add_argument('--create-fixture', nargs=3, metavar=('NAME', 'LANGUAGE', 'DATA'), help='Create a fixture')
    parser.add_argument('--get-fixture', nargs=2, metavar=('NAME', 'LANGUAGE'), help='Get a fixture')
    parser.add_argument('--list-fixtures', help='List fixtures for language')
    parser.add_argument('--share-data', nargs=3, metavar=('KEY', 'VALUE', 'SOURCE_LANG'), help='Share test data')
    parser.add_argument('--get-data', nargs=2, metavar=('KEY', 'LANGUAGE'), help='Get shared data')
    parser.add_argument('--list-data', action='store_true', help='List all shared data')
    parser.add_argument('--cleanup', action='store_true', help='Clean up expired data')
    parser.add_argument('--report', action='store_true', help='Generate data usage report')
    
    args = parser.parse_args()
    
    manager = CrossLanguageTestDataManager()
    
    if args.create_fixture:
        name, language, data = args.create_fixture
        fixture = manager.create_fixture(name, language, data)
        print(f"Created fixture: {fixture.name} for {fixture.language}")
    
    elif args.get_fixture:
        name, language = args.get_fixture
        fixture = manager.get_fixture(name, language)
        if fixture:
            print(json.dumps(asdict(fixture), indent=2, default=str))
        else:
            print("Fixture not found")
    
    elif args.list_fixtures:
        fixtures = manager.list_fixtures(args.list_fixtures)
        for fixture in fixtures:
            print(f"{fixture.language}: {fixture.name} ({fixture.data_type})")
    
    elif args.share_data:
        key, value, source_lang = args.share_data
        test_data = manager.share_data(key, value, source_lang)
        print(f"Shared data with key: {test_data.key}")
    
    elif args.get_data:
        key, language = args.get_data
        data = manager.get_shared_data(key, language)
        if data:
            print(json.dumps(data, indent=2, default=str))
        else:
            print("Data not found")
    
    elif args.list_data:
        shared_data = manager.list_shared_data()
        for data in shared_data:
            print(f"{data.key}: {data.language_source} -> {data.languages_used}")
    
    elif args.cleanup:
        cleaned = manager.cleanup_expired_data()
        print(f"Cleaned up {cleaned} items")
    
    elif args.report:
        report = manager.generate_data_report()
        print(json.dumps(report, indent=2))
    
    else:
        parser.print_help()

if __name__ == '__main__':
    main() 