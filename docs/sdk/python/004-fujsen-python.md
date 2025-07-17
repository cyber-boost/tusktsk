# ⚡ FUJSEN Functions for Python

**"We don't bow to any king" - Executable Configuration**

FUJSEN (Function Serialization) is TuskLang's revolutionary feature that allows you to write executable Python functions directly in your configuration files. This brings the power of dynamic logic to static configuration.

## 🎯 What is FUJSEN?

FUJSEN stands for **Function Serialization** - the ability to define, serialize, and execute Python functions within TuskLang configuration files. This transforms static configuration into dynamic, executable logic.

### Why FUJSEN is Revolutionary

1. **Executable Configuration** - Write Python functions in your config files
2. **Dynamic Logic** - Configuration that adapts and responds to data
3. **Business Rules** - Embed complex business logic in configuration
4. **Smart Contracts** - Self-executing contracts with embedded logic
5. **Real-time Processing** - Process data as configuration is loaded

## 🚀 Basic FUJSEN Functions

### Simple Function Definition

```python
from tsk import TSK

config = TSK.from_string("""
[math]
add_fujsen = '''
def add(a, b):
    return a + b
'''

multiply_fujsen = '''
def multiply(a, b):
    return a * b
'''

power_fujsen = '''
def power(base, exponent):
    return base ** exponent
'''
""")

# Execute FUJSEN functions
result1 = config.execute_fujsen('math', 'add', 5, 3)
result2 = config.execute_fujsen('math', 'multiply', 4, 7)
result3 = config.execute_fujsen('math', 'power', 2, 8)

print(f"5 + 3 = {result1}")      # Output: 5 + 3 = 8
print(f"4 * 7 = {result2}")      # Output: 4 * 7 = 28
print(f"2^8 = {result3}")        # Output: 2^8 = 256
```

### String Processing Functions

```python
config = TSK.from_string("""
[strings]
uppercase_fujsen = '''
def uppercase(text):
    return text.upper()
'''

lowercase_fujsen = '''
def lowercase(text):
    return text.lower()
'''

reverse_fujsen = '''
def reverse(text):
    return text[::-1]
'''

capitalize_fujsen = '''
def capitalize(text):
    return text.capitalize()
'''
""")

# Execute string functions
text = "Hello, TuskLang!"
upper = config.execute_fujsen('strings', 'uppercase', text)
lower = config.execute_fujsen('strings', 'lowercase', text)
reversed_text = config.execute_fujsen('strings', 'reverse', text)
capitalized = config.execute_fujsen('strings', 'capitalize', text)

print(f"Original: {text}")
print(f"Uppercase: {upper}")
print(f"Lowercase: {lower}")
print(f"Reversed: {reversed_text}")
print(f"Capitalized: {capitalized}")
```

## 🔧 Advanced FUJSEN Features

### Functions with Multiple Parameters

```python
config = TSK.from_string("""
[user]
create_user_fujsen = '''
def create_user(name, email, age, active=True):
    user = {
        'id': int(time.time()),  # Simple ID generation
        'name': name,
        'email': email,
        'age': age,
        'active': active,
        'created_at': time.strftime('%Y-%m-%d %H:%M:%S')
    }
    return user
'''

validate_user_fujsen = '''
def validate_user(user_data):
    errors = []
    
    if not user_data.get('name'):
        errors.append("Name is required")
    
    if not user_data.get('email'):
        errors.append("Email is required")
    elif '@' not in user_data.get('email', ''):
        errors.append("Invalid email format")
    
    if user_data.get('age', 0) < 0:
        errors.append("Age must be positive")
    
    return {
        'valid': len(errors) == 0,
        'errors': errors
    }
'''
""")

# Create and validate a user
user = config.execute_fujsen('user', 'create_user', 'Alice', 'alice@example.com', 25)
validation = config.execute_fujsen('user', 'validate_user', user)

print(f"User: {user}")
print(f"Validation: {validation}")
```

### Functions with Complex Logic

```python
config = TSK.from_string("""
[payment]
process_payment_fujsen = '''
def process_payment(amount, currency, payment_method, user_data):
    import time
    import hashlib
    
    # Validate amount
    if amount <= 0:
        raise ValueError("Amount must be positive")
    
    # Calculate fees based on payment method
    fees = {
        'credit_card': 0.025,
        'debit_card': 0.015,
        'bank_transfer': 0.005,
        'crypto': 0.01
    }
    
    fee_rate = fees.get(payment_method, 0.02)
    fee_amount = amount * fee_rate
    total_amount = amount + fee_amount
    
    # Generate transaction ID
    transaction_id = hashlib.md5(
        f"{amount}{currency}{payment_method}{user_data['id']}{time.time()}".encode()
    ).hexdigest()[:16]
    
    # Process payment (simulated)
    transaction = {
        'transaction_id': transaction_id,
        'amount': amount,
        'currency': currency,
        'fee_amount': fee_amount,
        'total_amount': total_amount,
        'payment_method': payment_method,
        'user_id': user_data['id'],
        'status': 'completed',
        'timestamp': time.strftime('%Y-%m-%d %H:%M:%S')
    }
    
    return transaction
'''

calculate_discount_fujsen = '''
def calculate_discount(amount, user_type, loyalty_years):
    # Base discount rates
    base_discounts = {
        'premium': 0.10,
        'gold': 0.05,
        'silver': 0.02,
        'bronze': 0.01
    }
    
    # Loyalty bonus (0.5% per year, max 5%)
    loyalty_bonus = min(loyalty_years * 0.005, 0.05)
    
    # Total discount rate
    total_discount_rate = base_discounts.get(user_type, 0) + loyalty_bonus
    
    # Calculate discount
    discount_amount = amount * total_discount_rate
    final_amount = amount - discount_amount
    
    return {
        'original_amount': amount,
        'discount_rate': total_discount_rate,
        'discount_amount': discount_amount,
        'final_amount': final_amount
    }
'''
""")

# Process a payment with discount
user_data = {'id': 123, 'name': 'Alice'}
payment = config.execute_fujsen('payment', 'process_payment', 100, 'USD', 'credit_card', user_data)
discount = config.execute_fujsen('payment', 'calculate_discount', 100, 'premium', 3)

print(f"Payment: {payment}")
print(f"Discount: {discount}")
```

## 🗄️ Database Integration with FUJSEN

### Database Query Functions

```python
from tsk import TSK
from tsk.adapters import SQLiteAdapter

# Set up database
db = SQLiteAdapter('app.db')
tsk = TSK()
tsk.set_database_adapter(db)

# Create test data
db.execute("""
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY,
    name TEXT,
    email TEXT,
    age INTEGER,
    active BOOLEAN,
    created_at TIMESTAMP
)
""")

db.execute("""
INSERT OR REPLACE INTO users VALUES 
(1, 'Alice', 'alice@example.com', 25, 1, '2024-01-01 10:00:00'),
(2, 'Bob', 'bob@example.com', 30, 1, '2024-01-02 11:00:00'),
(3, 'Charlie', 'charlie@example.com', 35, 0, '2024-01-03 12:00:00')
""")

# FUJSEN with database operations
config = TSK.from_string("""
[users]
get_user_by_id_fujsen = '''
def get_user_by_id(user_id):
    result = db.query("SELECT * FROM users WHERE id = ?", user_id)
    return result[0] if result else None
'''

get_active_users_fujsen = '''
def get_active_users():
    return db.query("SELECT * FROM users WHERE active = 1")
'''

create_user_fujsen = '''
def create_user(name, email, age):
    db.execute(
        "INSERT INTO users (name, email, age, active, created_at) VALUES (?, ?, ?, ?, ?)",
        name, email, age, True, time.strftime('%Y-%m-%d %H:%M:%S')
    )
    return db.query("SELECT * FROM users WHERE email = ?", email)[0]
'''

update_user_fujsen = '''
def update_user(user_id, **kwargs):
    set_clauses = []
    values = []
    
    for key, value in kwargs.items():
        set_clauses.append(f"{key} = ?")
        values.append(value)
    
    values.append(user_id)
    query = f"UPDATE users SET {', '.join(set_clauses)} WHERE id = ?"
    db.execute(query, *values)
    
    return get_user_by_id(user_id)
'''
""")

# Execute database functions
user = config.execute_fujsen('users', 'get_user_by_id', 1)
active_users = config.execute_fujsen('users', 'get_active_users')
new_user = config.execute_fujsen('users', 'create_user', 'David', 'david@example.com', 28)
updated_user = config.execute_fujsen('users', 'update_user', 1, age=26, active=False)

print(f"User 1: {user}")
print(f"Active users: {len(active_users)}")
print(f"New user: {new_user}")
print(f"Updated user: {updated_user}")
```

## 🔐 Security and Validation

### Input Validation Functions

```python
config = TSK.from_string("""
[validation]
validate_email_fujsen = '''
import re

def validate_email(email):
    pattern = r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
    return bool(re.match(pattern, email))
'''

validate_password_fujsen = '''
def validate_password(password):
    errors = []
    
    if len(password) < 8:
        errors.append("Password must be at least 8 characters")
    
    if not any(c.isupper() for c in password):
        errors.append("Password must contain at least one uppercase letter")
    
    if not any(c.islower() for c in password):
        errors.append("Password must contain at least one lowercase letter")
    
    if not any(c.isdigit() for c in password):
        errors.append("Password must contain at least one digit")
    
    if not any(c in '!@#$%^&*()_+-=[]{}|;:,.<>?' for c in password):
        errors.append("Password must contain at least one special character")
    
    return {
        'valid': len(errors) == 0,
        'errors': errors
    }
'''

sanitize_input_fujsen = '''
import html

def sanitize_input(text):
    # Remove HTML tags
    import re
    text = re.sub(r'<[^>]+>', '', text)
    
    # Escape HTML entities
    text = html.escape(text)
    
    # Remove dangerous characters
    text = text.replace('javascript:', '')
    text = text.replace('onclick', '')
    text = text.replace('onload', '')
    
    return text.strip()
'''
""")

# Test validation functions
email_valid = config.execute_fujsen('validation', 'validate_email', 'alice@example.com')
email_invalid = config.execute_fujsen('validation', 'validate_email', 'invalid-email')

password_check = config.execute_fujsen('validation', 'validate_password', 'weak')
strong_password_check = config.execute_fujsen('validation', 'validate_password', 'StrongP@ss123')

sanitized = config.execute_fujsen('validation', 'sanitize_input', '<script>alert("xss")</script>Hello World!')

print(f"Email valid: {email_valid}")
print(f"Email invalid: {email_invalid}")
print(f"Weak password: {password_check}")
print(f"Strong password: {strong_password_check}")
print(f"Sanitized: {sanitized}")
```

## 🧮 Mathematical and Statistical Functions

### Advanced Math Functions

```python
config = TSK.from_string("""
[math]
calculate_statistics_fujsen = '''
import statistics

def calculate_statistics(numbers):
    if not numbers:
        return None
    
    return {
        'count': len(numbers),
        'sum': sum(numbers),
        'mean': statistics.mean(numbers),
        'median': statistics.median(numbers),
        'mode': statistics.mode(numbers) if len(set(numbers)) < len(numbers) else None,
        'min': min(numbers),
        'max': max(numbers),
        'variance': statistics.variance(numbers),
        'std_dev': statistics.stdev(numbers)
    }
'''

calculate_percentile_fujsen = '''
def calculate_percentile(numbers, percentile):
    if not numbers:
        return None
    
    sorted_numbers = sorted(numbers)
    index = (percentile / 100) * (len(sorted_numbers) - 1)
    
    if index.is_integer():
        return sorted_numbers[int(index)]
    else:
        lower = sorted_numbers[int(index)]
        upper = sorted_numbers[int(index) + 1]
        return lower + (upper - lower) * (index - int(index))
'''

calculate_compound_interest_fujsen = '''
def calculate_compound_interest(principal, rate, time, compounds_per_year=12):
    rate_decimal = rate / 100
    amount = principal * (1 + rate_decimal / compounds_per_year) ** (compounds_per_year * time)
    interest = amount - principal
    
    return {
        'principal': principal,
        'rate': rate,
        'time': time,
        'compounds_per_year': compounds_per_year,
        'final_amount': round(amount, 2),
        'interest_earned': round(interest, 2)
    }
'''
""")

# Test mathematical functions
numbers = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
stats = config.execute_fujsen('math', 'calculate_statistics', numbers)
percentile_75 = config.execute_fujsen('math', 'calculate_percentile', numbers, 75)
interest = config.execute_fujsen('math', 'calculate_compound_interest', 1000, 5, 10)

print(f"Statistics: {stats}")
print(f"75th percentile: {percentile_75}")
print(f"Compound interest: {interest}")
```

## 🔄 Data Processing Functions

### Data Transformation

```python
config = TSK.from_string("""
[data]
transform_data_fujsen = '''
def transform_data(data, transformations):
    result = []
    
    for item in data:
        transformed_item = {}
        
        for field, transform in transformations.items():
            if field in item:
                if transform == 'uppercase':
                    transformed_item[field] = str(item[field]).upper()
                elif transform == 'lowercase':
                    transformed_item[field] = str(item[field]).lower()
                elif transform == 'capitalize':
                    transformed_item[field] = str(item[field]).capitalize()
                elif transform == 'int':
                    transformed_item[field] = int(item[field])
                elif transform == 'float':
                    transformed_item[field] = float(item[field])
                elif transform == 'bool':
                    transformed_item[field] = bool(item[field])
                else:
                    transformed_item[field] = item[field]
            else:
                transformed_item[field] = None
        
        result.append(transformed_item)
    
    return result
'''

filter_data_fujsen = '''
def filter_data(data, filters):
    result = []
    
    for item in data:
        include_item = True
        
        for field, condition in filters.items():
            if field in item:
                if isinstance(condition, dict):
                    # Complex condition
                    if 'min' in condition and item[field] < condition['min']:
                        include_item = False
                    if 'max' in condition and item[field] > condition['max']:
                        include_item = False
                    if 'equals' in condition and item[field] != condition['equals']:
                        include_item = False
                    if 'contains' in condition and condition['contains'] not in str(item[field]):
                        include_item = False
                else:
                    # Simple equality
                    if item[field] != condition:
                        include_item = False
            else:
                include_item = False
        
        if include_item:
            result.append(item)
    
    return result
'''

aggregate_data_fujsen = '''
def aggregate_data(data, group_by, aggregate_field, operation='sum'):
    groups = {}
    
    for item in data:
        group_key = item.get(group_by)
        if group_key not in groups:
            groups[group_key] = []
        groups[group_key].append(item.get(aggregate_field, 0))
    
    result = {}
    for group, values in groups.items():
        if operation == 'sum':
            result[group] = sum(values)
        elif operation == 'avg':
            result[group] = sum(values) / len(values)
        elif operation == 'min':
            result[group] = min(values)
        elif operation == 'max':
            result[group] = max(values)
        elif operation == 'count':
            result[group] = len(values)
    
    return result
'''
""")

# Test data processing functions
data = [
    {'name': 'Alice', 'age': 25, 'salary': 50000, 'department': 'Engineering'},
    {'name': 'Bob', 'age': 30, 'salary': 60000, 'department': 'Engineering'},
    {'name': 'Charlie', 'age': 35, 'salary': 70000, 'department': 'Sales'},
    {'name': 'David', 'age': 28, 'salary': 55000, 'department': 'Engineering'},
    {'name': 'Eve', 'age': 32, 'salary': 65000, 'department': 'Sales'}
]

transformations = {'name': 'uppercase', 'department': 'capitalize'}
transformed = config.execute_fujsen('data', 'transform_data', data, transformations)

filters = {'department': 'Engineering', 'age': {'min': 25, 'max': 35}}
filtered = config.execute_fujsen('data', 'filter_data', data, filters)

aggregated = config.execute_fujsen('data', 'aggregate_data', data, 'department', 'salary', 'avg')

print(f"Transformed: {transformed}")
print(f"Filtered: {filtered}")
print(f"Aggregated: {aggregated}")
```

## 🚀 Smart Contracts with FUJSEN

### Payment Processing Contract

```python
config = TSK.from_string("""
[contract]
name: "PaymentProcessor"
version: "1.0.0"

# Contract state
balance: 10000
transactions: []
users: {}

process_payment_fujsen = '''
def process_payment(amount, recipient_id, sender_id):
    global balance, transactions, users
    
    # Validate transaction
    if amount <= 0:
        raise ValueError("Amount must be positive")
    
    if sender_id not in users:
        raise ValueError("Sender not found")
    
    if users[sender_id]['balance'] < amount:
        raise ValueError("Insufficient balance")
    
    # Execute transaction
    users[sender_id]['balance'] -= amount
    if recipient_id in users:
        users[recipient_id]['balance'] += amount
    else:
        # External recipient
        balance += amount
    
    # Record transaction
    transaction = {
        'id': len(transactions) + 1,
        'amount': amount,
        'sender_id': sender_id,
        'recipient_id': recipient_id,
        'timestamp': time.time(),
        'status': 'completed'
    }
    transactions.append(transaction)
    
    return transaction
'''

add_user_fujsen = '''
def add_user(user_id, name, initial_balance=0):
    global users
    
    if user_id in users:
        raise ValueError("User already exists")
    
    users[user_id] = {
        'id': user_id,
        'name': name,
        'balance': initial_balance,
        'created_at': time.time()
    }
    
    return users[user_id]
'''

get_balance_fujsen = '''
def get_balance(user_id=None):
    global balance, users
    
    if user_id:
        return users.get(user_id, {}).get('balance', 0)
    else:
        return balance
'''

get_transactions_fujsen = '''
def get_transactions(user_id=None):
    global transactions
    
    if user_id:
        return [t for t in transactions if t['sender_id'] == user_id or t['recipient_id'] == user_id]
    else:
        return transactions
'''
""")

# Use the smart contract
try:
    # Add users
    alice = config.execute_fujsen('contract', 'add_user', 'alice_001', 'Alice', 1000)
    bob = config.execute_fujsen('contract', 'add_user', 'bob_002', 'Bob', 500)
    
    # Process payments
    payment1 = config.execute_fujsen('contract', 'process_payment', 100, 'bob_002', 'alice_001')
    payment2 = config.execute_fujsen('contract', 'process_payment', 50, 'external_user', 'bob_002')
    
    # Check balances
    alice_balance = config.execute_fujsen('contract', 'get_balance', 'alice_001')
    bob_balance = config.execute_fujsen('contract', 'get_balance', 'bob_002')
    contract_balance = config.execute_fujsen('contract', 'get_balance')
    
    # Get transactions
    alice_transactions = config.execute_fujsen('contract', 'get_transactions', 'alice_001')
    
    print(f"Alice balance: {alice_balance}")
    print(f"Bob balance: {bob_balance}")
    print(f"Contract balance: {contract_balance}")
    print(f"Alice transactions: {len(alice_transactions)}")
    
except ValueError as e:
    print(f"Contract error: {e}")
```

## 🛠️ Error Handling and Debugging

### Robust Error Handling

```python
config = TSK.from_string("""
[utils]
safe_divide_fujsen = '''
def safe_divide(numerator, denominator):
    try:
        if denominator == 0:
            raise ValueError("Division by zero")
        return numerator / denominator
    except Exception as e:
        return {
            'error': str(e),
            'result': None
        }
'''

validate_and_process_fujsen = '''
def validate_and_process(data, schema):
    errors = []
    processed_data = {}
    
    try:
        for field, rules in schema.items():
            if field not in data:
                if rules.get('required', False):
                    errors.append(f"Missing required field: {field}")
                continue
            
            value = data[field]
            
            # Type validation
            expected_type = rules.get('type')
            if expected_type and not isinstance(value, expected_type):
                errors.append(f"Field {field} must be {expected_type.__name__}")
                continue
            
            # Range validation
            if 'min' in rules and value < rules['min']:
                errors.append(f"Field {field} must be >= {rules['min']}")
                continue
            
            if 'max' in rules and value > rules['max']:
                errors.append(f"Field {field} must be <= {rules['max']}")
                continue
            
            # Custom validation
            if 'validator' in rules:
                if not rules['validator'](value):
                    errors.append(f"Field {field} failed custom validation")
                    continue
            
            processed_data[field] = value
        
        return {
            'valid': len(errors) == 0,
            'errors': errors,
            'data': processed_data
        }
        
    except Exception as e:
        return {
            'valid': False,
            'errors': [f"Validation error: {str(e)}"],
            'data': {}
        }
'''
""")

# Test error handling
division_result = config.execute_fujsen('utils', 'safe_divide', 10, 0)
print(f"Safe division: {division_result}")

# Test validation
schema = {
    'name': {'type': str, 'required': True},
    'age': {'type': int, 'required': True, 'min': 0, 'max': 150},
    'email': {'type': str, 'required': True, 'validator': lambda x: '@' in x}
}

test_data = {'name': 'Alice', 'age': 25, 'email': 'alice@example.com'}
validation_result = config.execute_fujsen('utils', 'validate_and_process', test_data, schema)
print(f"Validation result: {validation_result}")
```

## 🚀 Next Steps

Now that you understand FUJSEN functions:

1. **Master @ Operators** - [006-at-operators-python.md](006-at-operators-python.md)
2. **Integrate Databases** - [005-database-integration-python.md](005-database-integration-python.md)
3. **Explore Advanced Features** - [007-advanced-features-python.md](007-advanced-features-python.md)

## 💡 Best Practices

1. **Keep functions focused** - Each function should do one thing well
2. **Handle errors gracefully** - Always include proper error handling
3. **Validate inputs** - Check parameters before processing
4. **Document complex logic** - Add comments for complex functions
5. **Test thoroughly** - Test functions with various inputs
6. **Use meaningful names** - Choose descriptive function names
7. **Limit complexity** - Keep functions under 50 lines when possible

---

**"We don't bow to any king"** - FUJSEN gives you the power to write executable configuration with a heartbeat. Transform static configs into dynamic, intelligent systems! 