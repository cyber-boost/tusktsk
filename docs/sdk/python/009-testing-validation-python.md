# 🧪 Testing & Validation - Python

**"We don't bow to any king" - Testing Edition**

TuskLang provides comprehensive testing and validation capabilities, ensuring your configurations, FUJSEN functions, and database integrations work correctly and securely.

## 🚀 Unit Testing

### Basic TSK Testing

```python
import unittest
from tsk import TSK
import tempfile
import os

class TestTSKConfig(unittest.TestCase):
    def setUp(self):
        """Set up test configuration"""
        self.config = TSK.from_string("""
        [test]
        value: 42
        string: "hello"
        boolean: true
        array: [1, 2, 3]
        object: {
            key: "value",
            nested: {
                deep: "value"
            }
        }
        
        add_fujsen = '''
        def add(a, b):
            return a + b
        '''
        
        multiply_fujsen = '''
        def multiply(a, b):
            return a * b
        '''
        
        validate_user_fujsen = '''
        def validate_user(user_data):
            if not user_data.get('username'):
                return {'valid': False, 'error': 'Username required'}
            
            if len(user_data.get('password', '')) < 8:
                return {'valid': False, 'error': 'Password too short'}
            
            return {'valid': True, 'user': user_data}
        '''
        """)
    
    def test_basic_parsing(self):
        """Test basic configuration parsing"""
        result = self.config.parse()
        
        self.assertEqual(result['test']['value'], 42)
        self.assertEqual(result['test']['string'], "hello")
        self.assertTrue(result['test']['boolean'])
        self.assertEqual(result['test']['array'], [1, 2, 3])
        self.assertEqual(result['test']['object']['key'], "value")
        self.assertEqual(result['test']['object']['nested']['deep'], "value")
    
    def test_fujsen_execution(self):
        """Test FUJSEN function execution"""
        # Test add function
        result = self.config.execute_fujsen('test', 'add', 2, 3)
        self.assertEqual(result, 5)
        
        # Test multiply function
        result = self.config.execute_fujsen('test', 'multiply', 4, 5)
        self.assertEqual(result, 20)
    
    def test_validation_fujsen(self):
        """Test validation FUJSEN functions"""
        # Valid user
        valid_user = {
            'username': 'testuser',
            'password': 'password123'
        }
        result = self.config.execute_fujsen('test', 'validate_user', valid_user)
        self.assertTrue(result['valid'])
        self.assertEqual(result['user'], valid_user)
        
        # Invalid user - missing username
        invalid_user = {'password': 'password123'}
        result = self.config.execute_fujsen('test', 'validate_user', invalid_user)
        self.assertFalse(result['valid'])
        self.assertEqual(result['error'], 'Username required')
        
        # Invalid user - short password
        invalid_user = {
            'username': 'testuser',
            'password': '123'
        }
        result = self.config.execute_fujsen('test', 'validate_user', invalid_user)
        self.assertFalse(result['valid'])
        self.assertEqual(result['error'], 'Password too short')
    
    def test_file_operations(self):
        """Test file-based operations"""
        with tempfile.NamedTemporaryFile(mode='w', delete=False) as f:
            f.write("test content")
            temp_file = f.name
        
        try:
            # Test file reading
            config = TSK.from_string(f"""
            [file_test]
            content: @file.read("{temp_file}")
            """)
            
            result = config.parse()
            self.assertEqual(result['file_test']['content'], "test content")
        
        finally:
            os.unlink(temp_file)
    
    def test_environment_variables(self):
        """Test environment variable integration"""
        import os
        os.environ['TEST_VAR'] = 'test_value'
        
        config = TSK.from_string("""
        [env_test]
        value: @env("TEST_VAR", "default")
        missing: @env("MISSING_VAR", "default")
        """)
        
        result = config.parse()
        self.assertEqual(result['env_test']['value'], 'test_value')
        self.assertEqual(result['env_test']['missing'], 'default')
        
        del os.environ['TEST_VAR']

if __name__ == '__main__':
    unittest.main()
```

### Advanced Testing with pytest

```python
import pytest
from tsk import TSK
from tsk.adapters import SQLiteAdapter
import tempfile
import os

@pytest.fixture
def test_db():
    """Create test database"""
    return SQLiteAdapter(':memory:')

@pytest.fixture
def tsk_instance(test_db):
    """Create TSK instance with test database"""
    tsk = TSK()
    tsk.set_database_adapter(test_db)
    return tsk

@pytest.fixture
def sample_config():
    """Sample configuration for testing"""
    return """
    [users]
    create_user_fujsen = '''
    def create_user(username, email, password):
        # Check if user exists
        existing = query("SELECT id FROM users WHERE username = ?", username)
        if existing:
            raise ValueError("Username already exists")
        
        # Create user
        user_id = execute("INSERT INTO users (username, email, password) VALUES (?, ?, ?)",
                         username, email, password)
        
        return {
            'id': user_id,
            'username': username,
            'email': email
        }
    '''
    
    get_user_fujsen = '''
    def get_user(user_id):
        user = query("SELECT id, username, email FROM users WHERE id = ?", user_id)
        if not user:
            return None
        
        return {
            'id': user[0][0],
            'username': user[0][1],
            'email': user[0][2]
        }
    '''
    
    update_user_fujsen = '''
    def update_user(user_id, **updates):
        # Check if user exists
        user = query("SELECT id FROM users WHERE id = ?", user_id)
        if not user:
            raise ValueError("User not found")
        
        # Build update query
        set_clauses = []
        values = []
        for key, value in updates.items():
            if key in ['username', 'email']:
                set_clauses.append(f"{key} = ?")
                values.append(value)
        
        if not set_clauses:
            return {'message': 'No valid fields to update'}
        
        values.append(user_id)
        query_str = f"UPDATE users SET {', '.join(set_clauses)} WHERE id = ?"
        execute(query_str, *values)
        
        return {'message': 'User updated successfully'}
    '''
    """

def test_user_creation(tsk_instance, sample_config):
    """Test user creation functionality"""
    # Setup database
    tsk_instance.get_database_adapter().execute("""
        CREATE TABLE users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT UNIQUE NOT NULL,
            email TEXT NOT NULL,
            password TEXT NOT NULL
        )
    """)
    
    # Load configuration
    config = tsk_instance.from_string(sample_config)
    
    # Test user creation
    user = config.execute_fujsen('users', 'create_user', 'testuser', 'test@example.com', 'password123')
    
    assert user['username'] == 'testuser'
    assert user['email'] == 'test@example.com'
    assert 'id' in user
    
    # Test duplicate username
    with pytest.raises(ValueError, match="Username already exists"):
        config.execute_fujsen('users', 'create_user', 'testuser', 'test2@example.com', 'password123')

def test_user_retrieval(tsk_instance, sample_config):
    """Test user retrieval functionality"""
    # Setup database
    db = tsk_instance.get_database_adapter()
    db.execute("""
        CREATE TABLE users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT UNIQUE NOT NULL,
            email TEXT NOT NULL,
            password TEXT NOT NULL
        )
    """)
    
    # Insert test user
    db.execute("INSERT INTO users (username, email, password) VALUES (?, ?, ?)",
               'testuser', 'test@example.com', 'password123')
    
    # Load configuration
    config = tsk_instance.from_string(sample_config)
    
    # Test user retrieval
    user = config.execute_fujsen('users', 'get_user', 1)
    
    assert user['username'] == 'testuser'
    assert user['email'] == 'test@example.com'
    assert user['id'] == 1
    
    # Test non-existent user
    user = config.execute_fujsen('users', 'get_user', 999)
    assert user is None

def test_user_update(tsk_instance, sample_config):
    """Test user update functionality"""
    # Setup database
    db = tsk_instance.get_database_adapter()
    db.execute("""
        CREATE TABLE users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT UNIQUE NOT NULL,
            email TEXT NOT NULL,
            password TEXT NOT NULL
        )
    """)
    
    # Insert test user
    db.execute("INSERT INTO users (username, email, password) VALUES (?, ?, ?)",
               'testuser', 'test@example.com', 'password123')
    
    # Load configuration
    config = tsk_instance.from_string(sample_config)
    
    # Test user update
    result = config.execute_fujsen('users', 'update_user', 1, email='newemail@example.com')
    assert result['message'] == 'User updated successfully'
    
    # Verify update
    user = config.execute_fujsen('users', 'get_user', 1)
    assert user['email'] == 'newemail@example.com'
    
    # Test update non-existent user
    with pytest.raises(ValueError, match="User not found"):
        config.execute_fujsen('users', 'update_user', 999, email='test@example.com')

def test_invalid_field_update(tsk_instance, sample_config):
    """Test update with invalid fields"""
    # Setup database
    db = tsk_instance.get_database_adapter()
    db.execute("""
        CREATE TABLE users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT UNIQUE NOT NULL,
            email TEXT NOT NULL,
            password TEXT NOT NULL
        )
    """)
    
    # Insert test user
    db.execute("INSERT INTO users (username, email, password) VALUES (?, ?, ?)",
               'testuser', 'test@example.com', 'password123')
    
    # Load configuration
    config = tsk_instance.from_string(sample_config)
    
    # Test update with invalid field
    result = config.execute_fujsen('users', 'update_user', 1, invalid_field='value')
    assert result['message'] == 'No valid fields to update'
```

## 🔍 Validation Testing

### Input Validation Testing

```python
import pytest
from tsk import TSK

class TestInputValidation:
    def setup_method(self):
        """Set up validation configuration"""
        self.config = TSK.from_string("""
        [validation]
        validate_email_fujsen = '''
        def validate_email(email):
            import re
            pattern = r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
            if not re.match(pattern, email):
                return {'valid': False, 'error': 'Invalid email format'}
            return {'valid': True, 'email': email}
        '''
        
        validate_password_fujsen = '''
        def validate_password(password):
            if len(password) < 8:
                return {'valid': False, 'error': 'Password too short'}
            
            if not any(c.isupper() for c in password):
                return {'valid': False, 'error': 'Password must contain uppercase letter'}
            
            if not any(c.islower() for c in password):
                return {'valid': False, 'error': 'Password must contain lowercase letter'}
            
            if not any(c.isdigit() for c in password):
                return {'valid': False, 'error': 'Password must contain digit'}
            
            return {'valid': True, 'password': password}
        '''
        
        validate_age_fujsen = '''
        def validate_age(age):
            try:
                age_int = int(age)
                if age_int < 0 or age_int > 150:
                    return {'valid': False, 'error': 'Age must be between 0 and 150'}
                return {'valid': True, 'age': age_int}
            except ValueError:
                return {'valid': False, 'error': 'Age must be a number'}
        '''
        
        validate_url_fujsen = '''
        def validate_url(url):
            import re
            pattern = r'^https?://[^\s/$.?#].[^\s]*$'
            if not re.match(pattern, url):
                return {'valid': False, 'error': 'Invalid URL format'}
            return {'valid': True, 'url': url}
        '''
        """)
    
    def test_email_validation(self):
        """Test email validation"""
        # Valid emails
        valid_emails = [
            'test@example.com',
            'user.name@domain.co.uk',
            'user+tag@example.org'
        ]
        
        for email in valid_emails:
            result = self.config.execute_fujsen('validation', 'validate_email', email)
            assert result['valid'] == True
            assert result['email'] == email
        
        # Invalid emails
        invalid_emails = [
            'invalid-email',
            '@example.com',
            'user@',
            'user@.com'
        ]
        
        for email in invalid_emails:
            result = self.config.execute_fujsen('validation', 'validate_email', email)
            assert result['valid'] == False
            assert 'error' in result
    
    def test_password_validation(self):
        """Test password validation"""
        # Valid passwords
        valid_passwords = [
            'Password123',
            'MySecurePass1',
            'ComplexP@ss1'
        ]
        
        for password in valid_passwords:
            result = self.config.execute_fujsen('validation', 'validate_password', password)
            assert result['valid'] == True
            assert result['password'] == password
        
        # Invalid passwords
        invalid_passwords = [
            ('short', 'Password too short'),
            ('nouppercase123', 'Password must contain uppercase letter'),
            ('NOLOWERCASE123', 'Password must contain lowercase letter'),
            ('NoDigits', 'Password must contain digit')
        ]
        
        for password, expected_error in invalid_passwords:
            result = self.config.execute_fujsen('validation', 'validate_password', password)
            assert result['valid'] == False
            assert result['error'] == expected_error
    
    def test_age_validation(self):
        """Test age validation"""
        # Valid ages
        valid_ages = ['0', '25', '100', '150']
        
        for age in valid_ages:
            result = self.config.execute_fujsen('validation', 'validate_age', age)
            assert result['valid'] == True
            assert result['age'] == int(age)
        
        # Invalid ages
        invalid_ages = [
            ('-1', 'Age must be between 0 and 150'),
            ('151', 'Age must be between 0 and 150'),
            ('abc', 'Age must be a number'),
            ('25.5', 'Age must be a number')
        ]
        
        for age, expected_error in invalid_ages:
            result = self.config.execute_fujsen('validation', 'validate_age', age)
            assert result['valid'] == False
            assert result['error'] == expected_error
    
    def test_url_validation(self):
        """Test URL validation"""
        # Valid URLs
        valid_urls = [
            'http://example.com',
            'https://www.example.org',
            'http://subdomain.example.co.uk/path'
        ]
        
        for url in valid_urls:
            result = self.config.execute_fujsen('validation', 'validate_url', url)
            assert result['valid'] == True
            assert result['url'] == url
        
        # Invalid URLs
        invalid_urls = [
            'not-a-url',
            'ftp://example.com',
            'http://',
            'https://'
        ]
        
        for url in invalid_urls:
            result = self.config.execute_fujsen('validation', 'validate_url', url)
            assert result['valid'] == False
            assert 'error' in result
```

### Business Logic Validation

```python
import pytest
from tsk import TSK
from tsk.adapters import SQLiteAdapter

class TestBusinessLogicValidation:
    def setup_method(self):
        """Set up business logic validation"""
        self.tsk = TSK()
        self.db = SQLiteAdapter(':memory:')
        self.tsk.set_database_adapter(self.db)
        
        # Setup test database
        self.db.execute("""
            CREATE TABLE accounts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                balance DECIMAL(10,2) DEFAULT 0.00,
                account_type TEXT NOT NULL,
                status TEXT DEFAULT 'active'
            )
        """)
        
        self.db.execute("""
            CREATE TABLE transactions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                account_id INTEGER NOT NULL,
                amount DECIMAL(10,2) NOT NULL,
                transaction_type TEXT NOT NULL,
                description TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        """)
        
        # Insert test data
        self.db.execute("""
            INSERT INTO accounts (user_id, balance, account_type) VALUES 
            (1, 1000.00, 'checking'),
            (2, 500.00, 'savings'),
            (3, 0.00, 'checking')
        """)
        
        self.config = self.tsk.from_string("""
        [business]
        validate_transfer_fujsen = '''
        def validate_transfer(from_account_id, to_account_id, amount):
            # Check if accounts exist
            from_account = query("SELECT id, balance, status FROM accounts WHERE id = ?", from_account_id)
            to_account = query("SELECT id, status FROM accounts WHERE id = ?", to_account_id)
            
            if not from_account:
                return {'valid': False, 'error': 'Source account not found'}
            
            if not to_account:
                return {'valid': False, 'error': 'Destination account not found'}
            
            # Check account status
            if from_account[0][2] != 'active':
                return {'valid': False, 'error': 'Source account is not active'}
            
            if to_account[0][1] != 'active':
                return {'valid': False, 'error': 'Destination account is not active'}
            
            # Check sufficient balance
            if from_account[0][1] < amount:
                return {'valid': False, 'error': 'Insufficient balance'}
            
            # Check amount validity
            if amount <= 0:
                return {'valid': False, 'error': 'Transfer amount must be positive'}
            
            # Check daily transfer limit
            today_transfers = query("""
                SELECT COALESCE(SUM(amount), 0) FROM transactions 
                WHERE account_id = ? AND transaction_type = 'transfer' 
                AND DATE(created_at) = DATE('now')
            """, from_account_id)[0][0]
            
            if today_transfers + amount > 10000:
                return {'valid': False, 'error': 'Daily transfer limit exceeded'}
            
            return {
                'valid': True,
                'from_balance': from_account[0][1],
                'to_account': to_account[0][0]
            }
        '''
        
        execute_transfer_fujsen = '''
        def execute_transfer(from_account_id, to_account_id, amount, description=""):
            # Validate transfer
            validation = validate_transfer(from_account_id, to_account_id, amount)
            if not validation['valid']:
                raise ValueError(validation['error'])
            
            # Execute transfer
            execute("UPDATE accounts SET balance = balance - ? WHERE id = ?", 
                   amount, from_account_id)
            execute("UPDATE accounts SET balance = balance + ? WHERE id = ?", 
                   amount, to_account_id)
            
            # Record transactions
            from_transaction_id = execute("""
                INSERT INTO transactions (account_id, amount, transaction_type, description)
                VALUES (?, ?, 'transfer', ?)
            """, from_account_id, -amount, f"Transfer to account {to_account_id}: {description}")
            
            to_transaction_id = execute("""
                INSERT INTO transactions (account_id, amount, transaction_type, description)
                VALUES (?, ?, 'transfer', ?)
            """, to_account_id, amount, f"Transfer from account {from_account_id}: {description}")
            
            return {
                'success': True,
                'from_transaction_id': from_transaction_id,
                'to_transaction_id': to_transaction_id,
                'amount': amount
            }
        '''
        """)
    
    def test_valid_transfer(self):
        """Test valid transfer validation"""
        result = self.config.execute_fujsen('business', 'validate_transfer', 1, 2, 100.00)
        
        assert result['valid'] == True
        assert result['from_balance'] == 1000.00
        assert result['to_account'] == 2
    
    def test_insufficient_balance(self):
        """Test insufficient balance validation"""
        result = self.config.execute_fujsen('business', 'validate_transfer', 1, 2, 1500.00)
        
        assert result['valid'] == False
        assert result['error'] == 'Insufficient balance'
    
    def test_invalid_amount(self):
        """Test invalid amount validation"""
        result = self.config.execute_fujsen('business', 'validate_transfer', 1, 2, -50.00)
        
        assert result['valid'] == False
        assert result['error'] == 'Transfer amount must be positive'
    
    def test_nonexistent_account(self):
        """Test nonexistent account validation"""
        result = self.config.execute_fujsen('business', 'validate_transfer', 999, 2, 100.00)
        
        assert result['valid'] == False
        assert result['error'] == 'Source account not found'
    
    def test_execute_transfer(self):
        """Test transfer execution"""
        result = self.config.execute_fujsen('business', 'execute_transfer', 1, 2, 100.00, 'Test transfer')
        
        assert result['success'] == True
        assert 'from_transaction_id' in result
        assert 'to_transaction_id' in result
        assert result['amount'] == 100.00
        
        # Verify account balances
        from_balance = self.db.query("SELECT balance FROM accounts WHERE id = 1")[0][0]
        to_balance = self.db.query("SELECT balance FROM accounts WHERE id = 2")[0][0]
        
        assert from_balance == 900.00
        assert to_balance == 600.00
    
    def test_execute_transfer_validation_error(self):
        """Test transfer execution with validation error"""
        with pytest.raises(ValueError, match="Insufficient balance"):
            self.config.execute_fujsen('business', 'execute_transfer', 1, 2, 1500.00)
```

## 🔧 Integration Testing

### Database Integration Testing

```python
import pytest
from tsk import TSK
from tsk.adapters import SQLiteAdapter, PostgreSQLAdapter
import tempfile
import os

class TestDatabaseIntegration:
    def setup_method(self):
        """Set up database integration tests"""
        self.tsk = TSK()
        
        # Use SQLite for testing
        self.db = SQLiteAdapter(':memory:')
        self.tsk.set_database_adapter(self.db)
        
        # Setup test schema
        self.db.execute("""
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT UNIQUE NOT NULL,
                email TEXT NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        """)
        
        self.db.execute("""
            CREATE TABLE posts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                title TEXT NOT NULL,
                content TEXT,
                published BOOLEAN DEFAULT FALSE,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users (id)
            )
        """)
        
        # Insert test data
        self.db.execute("""
            INSERT INTO users (username, email) VALUES 
            ('alice', 'alice@example.com'),
            ('bob', 'bob@example.com'),
            ('charlie', 'charlie@example.com')
        """)
        
        self.db.execute("""
            INSERT INTO posts (user_id, title, content, published) VALUES 
            (1, 'First Post', 'Hello World!', TRUE),
            (1, 'Draft Post', 'This is a draft', FALSE),
            (2, 'Bob Post', 'Bob says hello', TRUE)
        """)
        
        self.config = self.tsk.from_string("""
        [database]
        user_count: @query("SELECT COUNT(*) FROM users")
        published_posts: @query("SELECT COUNT(*) FROM posts WHERE published = 1")
        recent_users: @query("SELECT username FROM users ORDER BY created_at DESC LIMIT 5")
        
        get_user_posts_fujsen = '''
        def get_user_posts(user_id, published_only=False):
            if published_only:
                posts = query("""
                    SELECT id, title, content, created_at 
                    FROM posts 
                    WHERE user_id = ? AND published = 1 
                    ORDER BY created_at DESC
                """, user_id)
            else:
                posts = query("""
                    SELECT id, title, content, published, created_at 
                    FROM posts 
                    WHERE user_id = ? 
                    ORDER BY created_at DESC
                """, user_id)
            
            return [{
                'id': post[0],
                'title': post[1],
                'content': post[2],
                'published': post[3] if not published_only else True,
                'created_at': post[-1]
            } for post in posts]
        '''
        
        create_user_fujsen = '''
        def create_user(username, email):
            # Check if username exists
            existing = query("SELECT id FROM users WHERE username = ?", username)
            if existing:
                raise ValueError("Username already exists")
            
            # Create user
            user_id = execute("INSERT INTO users (username, email) VALUES (?, ?)", username, email)
            
            return {
                'id': user_id,
                'username': username,
                'email': email
            }
        '''
        
        publish_post_fujsen = '''
        def publish_post(post_id):
            # Check if post exists
            post = query("SELECT id, user_id, title FROM posts WHERE id = ?", post_id)
            if not post:
                raise ValueError("Post not found")
            
            # Publish post
            execute("UPDATE posts SET published = 1 WHERE id = ?", post_id)
            
            return {
                'id': post_id,
                'title': post[0][2],
                'published': True
            }
        '''
        """)
    
    def test_basic_queries(self):
        """Test basic database queries"""
        result = self.config.parse()
        
        assert result['database']['user_count'] == 3
        assert result['database']['published_posts'] == 2
        assert len(result['database']['recent_users']) == 3
    
    def test_get_user_posts(self):
        """Test getting user posts"""
        # Get all posts for user 1
        posts = self.config.execute_fujsen('database', 'get_user_posts', 1, False)
        
        assert len(posts) == 2
        assert posts[0]['title'] == 'Draft Post'  # Most recent first
        assert posts[1]['title'] == 'First Post'
        
        # Get only published posts
        published_posts = self.config.execute_fujsen('database', 'get_user_posts', 1, True)
        
        assert len(published_posts) == 1
        assert published_posts[0]['title'] == 'First Post'
        assert published_posts[0]['published'] == True
    
    def test_create_user(self):
        """Test user creation"""
        user = self.config.execute_fujsen('database', 'create_user', 'david', 'david@example.com')
        
        assert user['username'] == 'david'
        assert user['email'] == 'david@example.com'
        assert 'id' in user
        
        # Verify in database
        new_count = self.config.get('database.user_count')
        assert new_count == 4
        
        # Test duplicate username
        with pytest.raises(ValueError, match="Username already exists"):
            self.config.execute_fujsen('database', 'create_user', 'david', 'david2@example.com')
    
    def test_publish_post(self):
        """Test post publishing"""
        result = self.config.execute_fujsen('database', 'publish_post', 2)
        
        assert result['id'] == 2
        assert result['title'] == 'Draft Post'
        assert result['published'] == True
        
        # Verify in database
        published_count = self.config.get('database.published_posts')
        assert published_count == 3
        
        # Test non-existent post
        with pytest.raises(ValueError, match="Post not found"):
            self.config.execute_fujsen('database', 'publish_post', 999)

@pytest.fixture
def postgres_test_db():
    """PostgreSQL test database fixture"""
    # This would require a test PostgreSQL instance
    # For now, we'll skip these tests if PostgreSQL is not available
    try:
        db = PostgreSQLAdapter(
            host='localhost',
            port=5432,
            database='test_db',
            user='test_user',
            password='test_password'
        )
        # Test connection
        db.query("SELECT 1")
        return db
    except Exception:
        pytest.skip("PostgreSQL not available")

def test_postgresql_integration(postgres_test_db):
    """Test PostgreSQL integration"""
    tsk = TSK()
    tsk.set_database_adapter(postgres_test_db)
    
    config = tsk.from_string("""
    [postgres_test]
    version: @query("SELECT version()")
    current_database: @query("SELECT current_database()")
    """)
    
    result = config.parse()
    
    assert 'version' in result['postgres_test']
    assert 'current_database' in result['postgres_test']
```

## 🚀 Performance Testing

### Benchmark Testing

```python
import time
import pytest
from tsk import TSK
from tsk.adapters import SQLiteAdapter

class TestPerformance:
    def setup_method(self):
        """Set up performance tests"""
        self.tsk = TSK()
        self.db = SQLiteAdapter(':memory:')
        self.tsk.set_database_adapter(self.db)
        
        # Setup large dataset
        self.db.execute("""
            CREATE TABLE large_dataset (
                id INTEGER PRIMARY KEY,
                name TEXT,
                value INTEGER,
                category TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        """)
        
        # Insert 1000 test records
        for i in range(1000):
            self.db.execute("""
                INSERT INTO large_dataset (name, value, category) VALUES (?, ?, ?)
            """, f"item_{i}", i, f"category_{i % 10}")
        
        self.config = self.tsk.from_string("""
        [performance]
        count_all_fujsen = '''
        def count_all():
            return query("SELECT COUNT(*) FROM large_dataset")[0][0]
        '''
        
        search_by_category_fujsen = '''
        def search_by_category(category):
            return query("SELECT * FROM large_dataset WHERE category = ?", category)
        '''
        
        complex_query_fujsen = '''
        def complex_query(min_value, max_value, categories):
            placeholders = ','.join(['?' for _ in categories])
            query_str = f"""
                SELECT category, COUNT(*), AVG(value) 
                FROM large_dataset 
                WHERE value BETWEEN ? AND ? AND category IN ({placeholders})
                GROUP BY category
                ORDER BY COUNT(*) DESC
            """
            return query(query_str, min_value, max_value, *categories)
        '''
        """)
    
    def test_parsing_performance(self):
        """Test configuration parsing performance"""
        large_config = """
        [test]
        """ + "\n".join([f"key_{i}: value_{i}" for i in range(1000)])
        
        config = TSK.from_string(large_config)
        
        start_time = time.time()
        result = config.parse()
        end_time = time.time()
        
        parsing_time = end_time - start_time
        print(f"Parsing time: {parsing_time:.4f} seconds")
        
        # Should complete in reasonable time
        assert parsing_time < 1.0
    
    def test_fujsen_execution_performance(self):
        """Test FUJSEN execution performance"""
        # Test simple query
        start_time = time.time()
        count = self.config.execute_fujsen('performance', 'count_all')
        end_time = time.time()
        
        execution_time = end_time - start_time
        print(f"Simple query time: {execution_time:.4f} seconds")
        
        assert count == 1000
        assert execution_time < 0.1
        
        # Test category search
        start_time = time.time()
        results = self.config.execute_fujsen('performance', 'search_by_category', 'category_1')
        end_time = time.time()
        
        execution_time = end_time - start_time
        print(f"Category search time: {execution_time:.4f} seconds")
        
        assert len(results) == 100  # 1000 items / 10 categories
        assert execution_time < 0.1
        
        # Test complex query
        start_time = time.time()
        results = self.config.execute_fujsen('performance', 'complex_query', 100, 500, ['category_1', 'category_2'])
        end_time = time.time()
        
        execution_time = end_time - start_time
        print(f"Complex query time: {execution_time:.4f} seconds")
        
        assert len(results) <= 2
        assert execution_time < 0.1
    
    def test_memory_usage(self):
        """Test memory usage"""
        import psutil
        import os
        
        process = psutil.Process(os.getpid())
        initial_memory = process.memory_info().rss
        
        # Create large configuration
        large_config = TSK.from_string("""
        [large]
        """ + "\n".join([f"key_{i}: value_{i}" for i in range(10000)]))
        
        # Parse configuration
        result = large_config.parse()
        
        final_memory = process.memory_info().rss
        memory_increase = final_memory - initial_memory
        
        print(f"Memory increase: {memory_increase / 1024 / 1024:.2f} MB")
        
        # Memory increase should be reasonable
        assert memory_increase < 100 * 1024 * 1024  # Less than 100MB
```

## 🔧 Test Configuration Files

### Test Configuration (test.tsk)

```ini
$test_mode: true
$test_database: ":memory:"

[test_config]
app_name: "TestApp"
version: "1.0.0-test"
debug: true

[test_database]
url: @test_database
pool_size: 1
timeout: 5

[test_validation]
validate_test_fujsen = '''
def validate_test(data):
    if not data.get('test_field'):
        return {'valid': False, 'error': 'Test field required'}
    
    if len(data.get('test_field', '')) < 3:
        return {'valid': False, 'error': 'Test field too short'}
    
    return {'valid': True, 'data': data}
'''

test_operation_fujsen = '''
def test_operation(input_value):
    # Simple test operation
    result = input_value * 2
    
    # Log for testing
    print(f"Test operation: {input_value} -> {result}")
    
    return {
        'input': input_value,
        'output': result,
        'success': True
    }
'''

test_database_fujsen = '''
def test_database():
    # Test database connection
    result = query("SELECT 1 as test")
    return {'database_working': True, 'result': result[0][0]}
'''
```

### Integration Test Configuration (integration.tsk)

```ini
$integration_mode: true
$test_environment: "integration"

[integration]
base_url: @env("TEST_BASE_URL", "http://localhost:8000")
api_key: @env("TEST_API_KEY", "test_key")
timeout: @env("TEST_TIMEOUT", 30)

[test_data]
users: [
    {
        username: "testuser1",
        email: "test1@example.com",
        password: "password123"
    },
    {
        username: "testuser2", 
        email: "test2@example.com",
        password: "password456"
    }
]

test_posts: [
    {
        title: "Test Post 1",
        content: "This is test content 1",
        published: true
    },
    {
        title: "Test Post 2",
        content: "This is test content 2", 
        published: false
    }
]

[test_operations]
create_test_user_fujsen = '''
def create_test_user(user_data):
    # Create test user
    user_id = execute("""
        INSERT INTO users (username, email, password) 
        VALUES (?, ?, ?)
    """, user_data['username'], user_data['email'], user_data['password'])
    
    return {
        'id': user_id,
        'username': user_data['username'],
        'email': user_data['email']
    }
'''

create_test_post_fujsen = '''
def create_test_post(user_id, post_data):
    # Create test post
    post_id = execute("""
        INSERT INTO posts (user_id, title, content, published)
        VALUES (?, ?, ?, ?)
    """, user_id, post_data['title'], post_data['content'], post_data['published'])
    
    return {
        'id': post_id,
        'user_id': user_id,
        'title': post_data['title'],
        'content': post_data['content'],
        'published': post_data['published']
    }
'''

cleanup_test_data_fujsen = '''
def cleanup_test_data():
    # Clean up test data
    execute("DELETE FROM posts WHERE title LIKE 'Test Post%'")
    execute("DELETE FROM users WHERE username LIKE 'testuser%'")
    
    return {'cleaned': True, 'message': 'Test data cleaned up'}
'''
```

## 🎯 Best Practices

### 1. Test Organization
- Use descriptive test names
- Group related tests in classes
- Use fixtures for common setup
- Separate unit, integration, and performance tests

### 2. Test Data Management
- Use in-memory databases for unit tests
- Create isolated test data
- Clean up after tests
- Use realistic test data

### 3. Validation Testing
- Test both valid and invalid inputs
- Test edge cases and boundary conditions
- Test error messages and handling
- Use parameterized tests for multiple scenarios

### 4. Performance Testing
- Set reasonable performance expectations
- Test with realistic data sizes
- Monitor memory usage
- Use profiling tools when needed

### 5. Integration Testing
- Test database interactions
- Test external service integrations
- Test configuration loading
- Test error scenarios

## 🚀 Next Steps

1. **Set up testing environment** with pytest and TuskLang
2. **Write unit tests** for your FUJSEN functions
3. **Create integration tests** for database operations
4. **Implement validation tests** for input data
5. **Add performance tests** for critical operations

---

**"We don't bow to any king"** - TuskLang provides comprehensive testing and validation capabilities, ensuring your configurations, FUJSEN functions, and database integrations work correctly and securely. Test thoroughly, validate everything, and build robust applications! 