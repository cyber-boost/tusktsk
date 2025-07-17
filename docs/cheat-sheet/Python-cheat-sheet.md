# Python Cheat Sheet

## Basic Syntax

### Comments
```python
# Single line comment
"""Multi-line comment"""
'''Another multi-line comment'''
```

### Indentation
```python
if condition:
    # 4 spaces or 1 tab
    print("Indented block")
```

## Data Types

### Primitive Types
```python
# Numbers
integer = 42                    # int: whole number
float_num = 3.14               # float: decimal number
complex_num = 1 + 2j           # complex: complex number

# Strings
string = "Hello World"         # str: text
raw_string = r"C:\path"        # raw string (no escaping)
f_string = f"Value: {x}"       # f-string (formatted)

# Booleans
boolean = True                 # bool: True or False
false_val = False             # bool: False value

# None
null_value = None             # NoneType: null value
```

### Collections
```python
# Lists (mutable)
list_example = [1, 2, 3]      # list: ordered, mutable sequence
mixed_list = [1, "hello", 3.14, True]

# Tuples (immutable)
tuple_example = (1, 2, 3)     # tuple: ordered, immutable sequence
single_tuple = (42,)          # Note the comma

# Dictionaries
dict_example = {"key": "value"} # dict: key-value pairs
nested_dict = {"a": {"b": 1}}

# Sets
set_example = {1, 2, 3}       # set: unordered, unique elements
frozen_set = frozenset([1, 2, 3]) # immutable set
```

## Variables

### Declaration and Assignment
```python
# Simple assignment
x = 10                        # Variable declaration
y = "hello"                   # String assignment
z = [1, 2, 3]                # List assignment

# Multiple assignment
a, b, c = 1, 2, 3            # Unpacking
x = y = z = 0                 # Multiple variables same value

# Augmented assignment
x += 1                        # x = x + 1
y *= 2                        # y = y * 2
z //= 3                       # z = z // 3
```

### Variable Scope
```python
global_var = "global"         # Global variable

def function():
    local_var = "local"       # Local variable
    global global_var         # Access global variable
    nonlocal outer_var        # Access outer function variable
```

## Operators

### Arithmetic Operators
```python
a, b = 10, 3
addition = a + b              # 13
subtraction = a - b           # 7
multiplication = a * b        # 30
division = a / b              # 3.333...
floor_division = a // b       # 3
modulus = a % b               # 1
exponentiation = a ** b       # 1000
```

### Comparison Operators
```python
x, y = 5, 10
equal = x == y                # False
not_equal = x != y            # True
less_than = x < y             # True
greater_than = x > y          # False
less_equal = x <= y           # True
greater_equal = x >= y        # False
```

### Logical Operators
```python
a, b = True, False
and_result = a and b          # False
or_result = a or b            # True
not_result = not a            # False
```

### Bitwise Operators
```python
x, y = 5, 3
bitwise_and = x & y           # 1
bitwise_or = x | y            # 7
bitwise_xor = x ^ y           # 6
bitwise_not = ~x              # -6
left_shift = x << 1           # 10
right_shift = x >> 1          # 2
```

### Membership Operators
```python
list_example = [1, 2, 3]
in_result = 2 in list_example # True
not_in = 4 not in list_example # True
```

### Identity Operators
```python
x = [1, 2, 3]
y = [1, 2, 3]
is_same = x is y              # False (different objects)
is_not_same = x is not y      # True
```

## Control Structures

### If-Else Statements
```python
if condition:
    # code block
elif another_condition:
    # code block
else:
    # code block

# Ternary operator
result = "yes" if condition else "no"
```

### Loops

#### For Loop
```python
# Iterate over sequence
for item in [1, 2, 3]:
    print(item)

# With range
for i in range(5):            # 0, 1, 2, 3, 4
    print(i)

# With enumerate
for index, value in enumerate(['a', 'b', 'c']):
    print(f"{index}: {value}")

# Dictionary iteration
for key, value in dict_example.items():
    print(f"{key}: {value}")
```

#### While Loop
```python
while condition:
    # code block
    if break_condition:
        break
    if continue_condition:
        continue
```

#### Loop Control
```python
for item in items:
    if item == "skip":
        continue               # Skip to next iteration
    if item == "stop":
        break                  # Exit loop
    if item == "found":
        return item           # Exit function
else:
    # Executed if loop completes normally
    print("Loop finished")
```

## Functions/Methods

### Function Definition
```python
def function_name(param1, param2, default_param="default"):
    """Docstring describing function"""
    # function body
    return result

# Lambda function
lambda_func = lambda x: x * 2

# Function with type hints
def typed_function(x: int, y: str) -> bool:
    return len(y) > x
```

### Function Arguments
```python
# Positional arguments
function(1, 2, 3)

# Keyword arguments
function(param1=1, param2=2)

# Variable arguments
def var_args(*args):          # Tuple of positional args
    return sum(args)

# Keyword variable arguments
def var_kwargs(**kwargs):     # Dict of keyword args
    return kwargs

# Combined
def combined(*args, **kwargs):
    pass
```

### Decorators
```python
def decorator(func):
    def wrapper(*args, **kwargs):
        # Before function
        result = func(*args, **kwargs)
        # After function
        return result
    return wrapper

@decorator
def decorated_function():
    pass
```

## Data Structures

### Lists
```python
# Creation
my_list = [1, 2, 3]
list_from_range = list(range(5))

# Accessing
first = my_list[0]            # Index access
last = my_list[-1]            # Negative indexing
slice_example = my_list[1:3]  # Slicing

# Methods
my_list.append(4)             # Add to end
my_list.insert(1, 1.5)       # Insert at index
my_list.extend([5, 6])       # Extend with iterable
my_list.remove(2)             # Remove first occurrence
popped = my_list.pop()        # Remove and return last
popped = my_list.pop(1)       # Remove and return at index
my_list.sort()                # Sort in place
my_list.reverse()             # Reverse in place
count = my_list.count(1)      # Count occurrences
index = my_list.index(3)      # Find index
length = len(my_list)         # Get length
```

### Dictionaries
```python
# Creation
my_dict = {"key": "value"}
dict_from_items = dict([("a", 1), ("b", 2)])

# Accessing
value = my_dict["key"]        # Key access
value = my_dict.get("key")    # Safe access
value = my_dict.get("key", "default") # With default

# Methods
my_dict["new_key"] = "new_value" # Add/update
my_dict.update({"c": 3})      # Update with dict
popped = my_dict.pop("key")   # Remove and return
my_dict.clear()               # Clear all items
keys = my_dict.keys()         # Get keys view
values = my_dict.values()     # Get values view
items = my_dict.items()       # Get items view
```

### Sets
```python
# Creation
my_set = {1, 2, 3}
set_from_list = set([1, 2, 2, 3])

# Methods
my_set.add(4)                 # Add element
my_set.remove(2)              # Remove element (raises error)
my_set.discard(2)             # Remove element (no error)
popped = my_set.pop()         # Remove and return arbitrary
my_set.clear()                # Clear all elements

# Set operations
union = set1 | set2           # Union
intersection = set1 & set2    # Intersection
difference = set1 - set2      # Difference
symmetric_diff = set1 ^ set2  # Symmetric difference
```

### Tuples
```python
# Creation
my_tuple = (1, 2, 3)
single_tuple = (42,)

# Accessing (same as lists)
first = my_tuple[0]
slice_example = my_tuple[1:3]

# Methods
count = my_tuple.count(1)     # Count occurrences
index = my_tuple.index(2)     # Find index
```

## Common Built-in Functions

### Type Conversion
```python
int("123")                    # Convert to int
float("3.14")                 # Convert to float
str(42)                       # Convert to string
list("hello")                 # Convert to list
tuple([1, 2, 3])             # Convert to tuple
dict([("a", 1), ("b", 2)])   # Convert to dict
set([1, 2, 2, 3])            # Convert to set
bool(1)                       # Convert to bool
```

### Mathematical Functions
```python
abs(-5)                       # Absolute value: 5
round(3.14159, 2)            # Round to 2 decimals: 3.14
min([1, 2, 3])               # Minimum value: 1
max([1, 2, 3])               # Maximum value: 3
sum([1, 2, 3])               # Sum: 6
pow(2, 3)                    # Power: 8
divmod(10, 3)                # Division and remainder: (3, 1)
```

### Sequence Functions
```python
len([1, 2, 3])               # Length: 3
sorted([3, 1, 2])            # Sorted list: [1, 2, 3]
reversed([1, 2, 3])          # Reversed iterator
enumerate(['a', 'b'])        # Enumerate: [(0, 'a'), (1, 'b')]
zip([1, 2], ['a', 'b'])      # Zip: [(1, 'a'), (2, 'b')]
all([True, True, False])     # All true: False
any([False, True, False])    # Any true: True
```

### Object Functions
```python
type(42)                     # Get type: <class 'int'>
isinstance(42, int)          # Check type: True
hasattr(obj, 'attr')         # Check attribute: True
getattr(obj, 'attr', 'default') # Get attribute
setattr(obj, 'attr', 'value') # Set attribute
delattr(obj, 'attr')         # Delete attribute
dir(obj)                     # List attributes
vars(obj)                    # Object's dict
```

### Input/Output Functions
```python
print("Hello")                # Print to stdout
input("Prompt: ")            # Get user input
open("file.txt", "r")        # Open file
format(3.14159, ".2f")       # Format string: "3.14"
repr("hello")                # String representation
ascii("hello")               # ASCII representation
```

## File I/O Operations

### Reading Files
```python
# Read entire file
with open("file.txt", "r") as f:
    content = f.read()

# Read line by line
with open("file.txt", "r") as f:
    for line in f:
        print(line.strip())

# Read all lines
with open("file.txt", "r") as f:
    lines = f.readlines()

# Read specific encoding
with open("file.txt", "r", encoding="utf-8") as f:
    content = f.read()
```

### Writing Files
```python
# Write text
with open("file.txt", "w") as f:
    f.write("Hello World")

# Write lines
with open("file.txt", "w") as f:
    f.writelines(["line1\n", "line2\n"])

# Append to file
with open("file.txt", "a") as f:
    f.write("New line\n")

# Binary mode
with open("file.bin", "wb") as f:
    f.write(b"binary data")
```

### File Operations
```python
import os

# File existence
os.path.exists("file.txt")    # Check if file exists

# File info
os.path.getsize("file.txt")   # Get file size
os.path.getmtime("file.txt")  # Get modification time

# File operations
os.rename("old.txt", "new.txt") # Rename file
os.remove("file.txt")         # Delete file
os.makedirs("dir/subdir")     # Create directories
```

## Error Handling

### Try-Except Blocks
```python
try:
    # Code that might raise exception
    result = 10 / 0
except ZeroDivisionError:
    # Handle specific exception
    print("Division by zero")
except (ValueError, TypeError) as e:
    # Handle multiple exceptions
    print(f"Error: {e}")
except Exception as e:
    # Handle any exception
    print(f"Unexpected error: {e}")
else:
    # Executed if no exception
    print("No errors")
finally:
    # Always executed
    print("Cleanup")
```

### Raising Exceptions
```python
# Raise built-in exception
raise ValueError("Invalid value")

# Raise custom exception
class CustomError(Exception):
    pass

raise CustomError("Custom error message")

# Re-raise exception
try:
    risky_operation()
except Exception as e:
    print(f"Caught: {e}")
    raise  # Re-raise the exception
```

### Context Managers
```python
# Custom context manager
class MyContextManager:
    def __enter__(self):
        # Setup
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        # Cleanup
        pass

# Using context manager
with MyContextManager() as cm:
    # Use cm
    pass
```

## Key Libraries/Modules

### Standard Library

#### os - Operating System Interface
```python
import os

os.getcwd()                   # Get current directory
os.chdir("/path")             # Change directory
os.listdir(".")               # List directory contents
os.environ["VAR"]             # Get environment variable
os.system("ls")               # Execute shell command
os.path.join("a", "b")        # Join path components
```

#### sys - System-Specific Parameters
```python
import sys

sys.argv                      # Command line arguments
sys.exit(0)                   # Exit program
sys.path                      # Python path
sys.version                   # Python version
sys.platform                  # Platform identifier
```

#### datetime - Date and Time
```python
from datetime import datetime, timedelta

now = datetime.now()          # Current datetime
today = datetime.today()      # Current date
dt = datetime(2023, 1, 1)     # Create datetime
formatted = dt.strftime("%Y-%m-%d") # Format datetime
parsed = datetime.strptime("2023-01-01", "%Y-%m-%d") # Parse string
delta = timedelta(days=7)     # Time delta
future = now + delta          # Add time delta
```

#### json - JSON Processing
```python
import json

# Serialization
json_str = json.dumps(data)   # Dict to JSON string
json_str = json.dumps(data, indent=2) # Pretty print

# Deserialization
data = json.loads(json_str)   # JSON string to dict

# File operations
with open("data.json", "w") as f:
    json.dump(data, f)        # Write to file

with open("data.json", "r") as f:
    data = json.load(f)       # Read from file
```

#### re - Regular Expressions
```python
import re

# Pattern matching
match = re.search(r"\d+", "abc123def") # Find first match
matches = re.findall(r"\d+", "abc123def456") # Find all matches
split = re.split(r"\s+", "a  b   c") # Split by pattern

# Pattern compilation
pattern = re.compile(r"\d+")
match = pattern.search("abc123def")

# Substitution
result = re.sub(r"\d+", "X", "abc123def") # Replace matches
```

#### collections - Specialized Container Types
```python
from collections import defaultdict, Counter, deque

# DefaultDict
dd = defaultdict(list)        # Default to empty list
dd["key"].append(1)           # No KeyError

# Counter
counter = Counter("hello")    # Count occurrences
counter.most_common(2)        # Most common elements

# Deque
dq = deque([1, 2, 3])        # Double-ended queue
dq.append(4)                  # Add to right
dq.appendleft(0)              # Add to left
dq.popleft()                  # Remove from left
```

#### itertools - Iterator Tools
```python
from itertools import chain, combinations, permutations

# Chain iterables
combined = chain([1, 2], [3, 4]) # [1, 2, 3, 4]

# Combinations
combs = combinations([1, 2, 3], 2) # All pairs

# Permutations
perms = permutations([1, 2, 3]) # All orderings
```

### Common Third-Party Libraries

#### requests - HTTP Library
```python
import requests

# GET request
response = requests.get("https://api.example.com")
data = response.json()

# POST request
response = requests.post("https://api.example.com", 
                        json={"key": "value"})

# With parameters
response = requests.get("https://api.example.com", 
                       params={"q": "search"})
```

#### numpy - Numerical Computing
```python
import numpy as np

# Arrays
arr = np.array([1, 2, 3])     # Create array
zeros = np.zeros((3, 3))      # Zero array
ones = np.ones((2, 2))        # Ones array
range_arr = np.arange(10)     # Range array

# Operations
result = arr + 1              # Element-wise addition
result = arr * 2              # Element-wise multiplication
result = np.dot(arr1, arr2)   # Matrix multiplication
```

#### pandas - Data Analysis
```python
import pandas as pd

# DataFrame creation
df = pd.DataFrame({"A": [1, 2, 3], "B": [4, 5, 6]})

# Reading data
df = pd.read_csv("data.csv")
df = pd.read_excel("data.xlsx")

# Operations
df.head()                     # First 5 rows
df.describe()                 # Statistical summary
df.groupby("column").mean()   # Group and aggregate
```

## Object-Oriented Programming

### Class Definition
```python
class MyClass:
    """Class docstring"""
    
    # Class variable
    class_var = "shared"
    
    def __init__(self, param):
        # Constructor
        self.instance_var = param
    
    def instance_method(self):
        # Instance method
        return self.instance_var
    
    @classmethod
    def class_method(cls):
        # Class method
        return cls.class_var
    
    @staticmethod
    def static_method():
        # Static method
        return "static"
```

### Inheritance
```python
class Parent:
    def method(self):
        return "parent"

class Child(Parent):
    def method(self):
        # Call parent method
        parent_result = super().method()
        return f"child {parent_result}"
```

### Properties
```python
class MyClass:
    def __init__(self):
        self._value = 0
    
    @property
    def value(self):
        return self._value
    
    @value.setter
    def value(self, new_value):
        if new_value < 0:
            raise ValueError("Must be positive")
        self._value = new_value
```

## Decorators and Generators

### Decorators
```python
def timer(func):
    def wrapper(*args, **kwargs):
        import time
        start = time.time()
        result = func(*args, **kwargs)
        end = time.time()
        print(f"{func.__name__} took {end - start} seconds")
        return result
    return wrapper

@timer
def slow_function():
    import time
    time.sleep(1)
```

### Generators
```python
# Generator function
def number_generator(n):
    for i in range(n):
        yield i

# Generator expression
squares = (x**2 for x in range(10))

# Using generators
for num in number_generator(5):
    print(num)
```

## Context Managers

### With Statement
```python
# File handling
with open("file.txt", "r") as f:
    content = f.read()

# Database connection
with database.connect() as conn:
    conn.execute("SELECT * FROM table")

# Custom context manager
class MyContext:
    def __enter__(self):
        print("Entering")
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        print("Exiting")
```

## Performance Tips

### List Comprehensions
```python
# Instead of loop
squares = []
for x in range(10):
    squares.append(x**2)

# Use list comprehension
squares = [x**2 for x in range(10)]

# With condition
even_squares = [x**2 for x in range(10) if x % 2 == 0]
```

### Generator Expressions
```python
# Memory efficient for large datasets
sum_squares = sum(x**2 for x in range(1000000))
```

### Efficient String Concatenation
```python
# Instead of + in loop
result = ""
for i in range(1000):
    result += str(i)

# Use join
result = "".join(str(i) for i in range(1000))
```

## Best Practices

### Naming Conventions
```python
# Variables and functions: snake_case
my_variable = 42
def my_function():
    pass

# Classes: PascalCase
class MyClass:
    pass

# Constants: UPPER_SNAKE_CASE
MAX_VALUE = 100

# Private attributes: _leading_underscore
class MyClass:
    def __init__(self):
        self._private_attr = 42
```

### Code Organization
```python
# Imports at top
import os
import sys
from datetime import datetime

# Constants
MAX_RETRIES = 3

# Classes
class MyClass:
    pass

# Functions
def my_function():
    pass

# Main execution
if __name__ == "__main__":
    main()
```

### Documentation
```python
def complex_function(param1: int, param2: str) -> bool:
    """
    Brief description of function.
    
    Args:
        param1: Description of param1
        param2: Description of param2
    
    Returns:
        Description of return value
    
    Raises:
        ValueError: When param1 is negative
    """
    if param1 < 0:
        raise ValueError("param1 must be positive")
    return len(param2) > param1
```

This cheat sheet covers the essential Python concepts and syntax. Remember to refer to the official Python documentation for more detailed information and best practices. 