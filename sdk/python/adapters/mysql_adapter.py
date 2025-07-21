#!/usr/bin/env python3
"""
MySQL Adapter for TuskLang Enhanced Python
=========================================
Enables @query operations with MySQL database

DEFAULT CONFIG: peanu.tsk (the bridge of language grace)
"""

import json
from typing import Any, Dict, List, Union, Optional

try:
    import mysql.connector
    from mysql.connector import Error
    MYSQL_AVAILABLE = True
except ImportError:
    MYSQL_AVAILABLE = False


class MySQLAdapter:
    """MySQL database adapter for TuskLang"""
    
    def __init__(self, options: Dict[str, Any] = None):
        if not MYSQL_AVAILABLE:
            raise Exception('MySQL adapter requires mysql-connector-python. Install it with: pip install mysql-connector-python')
        
        self.config = {
            'host': 'localhost',
            'port': 3306,
            'database': 'tusklang',
            'user': 'root',
            'password': '',
            'charset': 'utf8mb4',
            'autocommit': True,
            'connect_timeout': 10
        }
        
        if options:
            self.config.update(options)
        
        self.connection = None
    
    def connect(self):
        """Connect to MySQL database"""
        if not self.connection:
            try:
                self.connection = mysql.connector.connect(
                    host=self.config['host'],
                    port=self.config['port'],
                    database=self.config['database'],
                    user=self.config['user'],
                    password=self.config['password'],
                    charset=self.config['charset'],
                    autocommit=self.config['autocommit'],
                    connect_timeout=self.config['connect_timeout']
                )
            except Error as e:
                raise Exception(f"MySQL connection error: {str(e)}")
    
    def query(self, sql: str, params: List[Any] = None) -> List[Dict[str, Any]]:
        """Execute SQL query and return results"""
        self.connect()
        
        if params is None:
            params = []
        
        try:
            cursor = self.connection.cursor(dictionary=True)
            cursor.execute(sql, params)
            
            # Handle different query types
            if sql.strip().upper().startswith(('SELECT', 'SHOW', 'DESCRIBE', 'EXPLAIN')):
                rows = cursor.fetchall()
                return rows
            else:
                # INSERT, UPDATE, DELETE, etc.
                self.connection.commit()
                return [{
                    'affected_rows': cursor.rowcount,
                    'last_insert_id': cursor.lastrowid
                }]
                
        except Error as e:
            raise Exception(f"MySQL error: {str(e)}")
        finally:
            cursor.close()
    
    def count(self, table: str, where: str = None, params: List[Any] = None) -> int:
        """Count rows in table with optional WHERE clause"""
        sql = f"SELECT COUNT(*) as count FROM {table}"
        
        if where:
            sql += f" WHERE {where}"
        
        result = self.query(sql, params or [])
        return result[0]['count'] if result else 0
    
    def find_all(self, table: str, where: str = None, params: List[Any] = None) -> List[Dict[str, Any]]:
        """Find all rows in table with optional WHERE clause"""
        sql = f"SELECT * FROM {table}"
        
        if where:
            sql += f" WHERE {where}"
        
        return self.query(sql, params or [])
    
    def find_one(self, table: str, where: str = None, params: List[Any] = None) -> Optional[Dict[str, Any]]:
        """Find one row in table with optional WHERE clause"""
        sql = f"SELECT * FROM {table}"
        
        if where:
            sql += f" WHERE {where}"
        
        sql += " LIMIT 1"
        
        result = self.query(sql, params or [])
        return result[0] if result else None
    
    def sum(self, table: str, column: str, where: str = None, params: List[Any] = None) -> float:
        """Sum values in a column with optional WHERE clause"""
        sql = f"SELECT SUM({column}) as total FROM {table}"
        
        if where:
            sql += f" WHERE {where}"
        
        result = self.query(sql, params or [])
        return float(result[0]['total'] or 0) if result else 0.0
    
    def avg(self, table: str, column: str, where: str = None, params: List[Any] = None) -> float:
        """Average values in a column with optional WHERE clause"""
        sql = f"SELECT AVG({column}) as average FROM {table}"
        
        if where:
            sql += f" WHERE {where}"
        
        result = self.query(sql, params or [])
        return float(result[0]['average'] or 0) if result else 0.0
    
    def max(self, table: str, column: str, where: str = None, params: List[Any] = None) -> Any:
        """Find maximum value in a column with optional WHERE clause"""
        sql = f"SELECT MAX({column}) as maximum FROM {table}"
        
        if where:
            sql += f" WHERE {where}"
        
        result = self.query(sql, params or [])
        return result[0]['maximum'] if result else None
    
    def min(self, table: str, column: str, where: str = None, params: List[Any] = None) -> Any:
        """Find minimum value in a column with optional WHERE clause"""
        sql = f"SELECT MIN({column}) as minimum FROM {table}"
        
        if where:
            sql += f" WHERE {where}"
        
        result = self.query(sql, params or [])
        return result[0]['minimum'] if result else None
    
    def create_test_data(self):
        """Create test data for MySQL"""
        self.connect()
        
        # Drop existing tables
        self.query("DROP TABLE IF EXISTS orders")
        self.query("DROP TABLE IF EXISTS products")
        self.query("DROP TABLE IF EXISTS users")
        
        # Create tables
        self.query("""
            CREATE TABLE users (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                email VARCHAR(255) UNIQUE,
                active BOOLEAN DEFAULT TRUE,
                age INT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
        """)
        
        self.query("""
            CREATE TABLE orders (
                id INT AUTO_INCREMENT PRIMARY KEY,
                user_id INT,
                amount DECIMAL(10,2),
                status VARCHAR(50) DEFAULT 'pending',
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users(id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
        """)
        
        self.query("""
            CREATE TABLE products (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                price DECIMAL(10,2),
                category VARCHAR(100),
                in_stock BOOLEAN DEFAULT TRUE,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
        """)
        
        # Insert test data
        self.query("""
            INSERT INTO users (name, email, active, age) VALUES
            ('John Doe', 'john@example.com', TRUE, 30),
            ('Jane Smith', 'jane@example.com', TRUE, 25),
            ('Bob Johnson', 'bob@example.com', FALSE, 35),
            ('Alice Brown', 'alice@example.com', TRUE, 28)
        """)
        
        self.query("""
            INSERT INTO products (name, price, category, in_stock) VALUES
            ('Laptop', 999.99, 'Electronics', TRUE),
            ('Mouse', 29.99, 'Electronics', TRUE),
            ('Keyboard', 79.99, 'Electronics', FALSE),
            ('Monitor', 299.99, 'Electronics', TRUE),
            ('Desk', 199.99, 'Furniture', TRUE)
        """)
        
        self.query("""
            INSERT INTO orders (user_id, amount, status) VALUES
            (1, 1029.98, 'completed'),
            (2, 79.99, 'pending'),
            (1, 299.99, 'shipped'),
            (4, 199.99, 'completed')
        """)
    
    def is_connected(self) -> bool:
        """Check if connected to database"""
        try:
            if self.connection:
                self.connection.ping(reconnect=False)
                return True
            return False
        except:
            return False
    
    def close(self):
        """Close database connection"""
        if self.connection:
            self.connection.close()
            self.connection = None
    
    @staticmethod
    def load_from_peanut():
        """Load MySQL configuration from peanu.tsk"""
        try:
            from tusk_enhanced import TuskLangEnhanced
            parser = TuskLangEnhanced()
            parser.load_peanut()
            
            config = {
                'host': parser.get('mysql.host') or 'localhost',
                'port': parser.get('mysql.port') or 3306,
                'database': parser.get('mysql.database') or 'tusklang',
                'user': parser.get('mysql.user') or 'root',
                'password': parser.get('mysql.password') or '',
                'charset': parser.get('mysql.charset') or 'utf8mb4'
            }
            
            return MySQLAdapter(config)
        except Exception as e:
            print(f"Error loading MySQL config from peanu.tsk: {e}")
            return MySQLAdapter()


# Global MySQL adapter instance
mysql_adapter = MySQLAdapter()


def get_mysql_adapter() -> MySQLAdapter:
    """Get global MySQL adapter instance"""
    return mysql_adapter 