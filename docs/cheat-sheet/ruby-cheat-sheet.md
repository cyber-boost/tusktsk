# Ruby Cheat Sheet

## Basic Syntax

```ruby
# Comments
# Single line comment
=begin
  Multi-line
  comment block
=end

# Print statements
puts "Hello, World!"    # Prints with newline
print "Hello"           # Prints without newline
p [1, 2, 3]            # Pretty print for debugging

# Basic program structure
#!/usr/bin/env ruby     # Shebang line
require 'library'       # Import library
```

## Data Types

```ruby
# Numbers
integer = 42            # Integer
float = 3.14           # Float
big_num = 1_000_000    # Underscores for readability

# Strings
string = "Hello"       # Double quotes (interpolation)
string = 'World'       # Single quotes (literal)
string = %q{Hello}     # Alternative syntax
string = <<~HEREDOC    # Heredoc syntax
  Multi-line
  string
HEREDOC

# Booleans
true                   # True value
false                  # False value
nil                    # Null/nil value

# Symbols
symbol = :name         # Immutable identifier
symbol = :"complex name" # Symbol with spaces

# Arrays
array = [1, 2, 3]     # Array literal
array = Array.new(3)  # Create array with size
array = %w[one two three] # Word array

# Hashes
hash = {key: "value"}  # Symbol keys (Ruby 1.9+)
hash = {"key" => "value"} # String keys
hash = Hash.new(0)     # Hash with default value

# Ranges
range = 1..10          # Inclusive range
range = 1...10         # Exclusive range
```

## Variables and Declarations

```ruby
# Variable types
local_var = "local"    # Local variable
@instance_var = "instance" # Instance variable
@@class_var = "class"  # Class variable
$global_var = "global" # Global variable
CONSTANT = "constant"  # Constant (uppercase)

# Multiple assignment
a, b, c = 1, 2, 3     # Multiple assignment
a, b = b, a           # Swap values
first, *rest = [1, 2, 3, 4] # Splat operator

# Parallel assignment
x, y = 10, 20         # Assign multiple values
```

## Operators

```ruby
# Arithmetic
a + b                 # Addition
a - b                 # Subtraction
a * b                 # Multiplication
a / b                 # Division
a % b                 # Modulo
a ** b                # Exponentiation

# Comparison
a == b                # Equal
a != b                # Not equal
a < b                 # Less than
a <= b                # Less than or equal
a > b                 # Greater than
a >= b                # Greater than or equal
a <=> b               # Spaceship operator

# Logical
a && b                # AND
a || b                # OR
!a                    # NOT
a and b               # Lower precedence AND
a or b                # Lower precedence OR
not a                 # Lower precedence NOT

# Assignment
a = b                 # Assignment
a += b                # Add and assign
a -= b                # Subtract and assign
a *= b                # Multiply and assign
a /= b                # Divide and assign
a %= b                # Modulo and assign
a **= b               # Exponentiate and assign

# Bitwise
a & b                 # Bitwise AND
a | b                 # Bitwise OR
a ^ b                 # Bitwise XOR
a << b                # Left shift
a >> b                # Right shift
~a                    # Bitwise NOT
```

## Control Structures

### If/Else
```ruby
# Basic if
if condition
  # code
end

# If with elsif
if condition1
  # code
elsif condition2
  # code
else
  # code
end

# Inline if
result = "positive" if number > 0

# Unless (opposite of if)
unless condition
  # code
end

# Case statement
case value
when 1
  puts "One"
when 2
  puts "Two"
else
  puts "Other"
end

# Case with ranges
case value
when 1..10
  puts "Low"
when 11..20
  puts "Medium"
end
```

### Loops
```ruby
# While loop
while condition
  # code
end

# Until loop (opposite of while)
until condition
  # code
end

# For loop
for item in collection
  # code
end

# Each iterator
collection.each do |item|
  # code
end

# Times loop
5.times do |i|
  puts i
end

# Upto loop
1.upto(10) do |i|
  puts i
end

# Downto loop
10.downto(1) do |i|
  puts i
end

# Step loop
(0..10).step(2) do |i|
  puts i
end
```

### Loop Control
```ruby
next                  # Skip to next iteration
break                 # Exit loop
redo                  # Restart current iteration
retry                 # Restart entire loop (in rescue)
```

## Functions/Methods

```ruby
# Method definition
def method_name
  # code
end

# Method with parameters
def greet(name)
  puts "Hello, #{name}!"
end

# Method with default parameters
def greet(name = "World")
  puts "Hello, #{name}!"
end

# Method with multiple parameters
def add(a, b)
  a + b
end

# Method with splat operator
def sum(*numbers)
  numbers.sum
end

# Method with keyword arguments
def create_user(name:, email:, age: 18)
  {name: name, email: email, age: age}
end

# Method with block
def with_block
  yield if block_given?
end

# Lambda/Proc
lambda = ->(x) { x * 2 }
proc = Proc.new { |x| x * 2 }

# Method return values
def explicit_return
  return "early return"
  "never reached"
end

def implicit_return
  "last expression is returned"
end
```

## Classes and Objects

```ruby
# Class definition
class MyClass
  # Class variable
  @@count = 0
  
  # Class method
  def self.class_method
    puts "Class method"
  end
  
  # Constructor
  def initialize(name)
    @name = name
    @@count += 1
  end
  
  # Instance method
  def greet
    puts "Hello, #{@name}!"
  end
  
  # Getter method
  def name
    @name
  end
  
  # Setter method
  def name=(value)
    @name = value
  end
  
  # Attribute accessors
  attr_reader :name           # Getter only
  attr_writer :name           # Setter only
  attr_accessor :name         # Both getter and setter
end

# Inheritance
class Child < Parent
  def initialize
    super                    # Call parent constructor
  end
end

# Module inclusion
module MyModule
  def module_method
    puts "Module method"
  end
end

class MyClass
  include MyModule          # Include module
  extend MyModule           # Extend with class methods
end
```

## Data Structures

### Arrays
```ruby
# Array creation
array = [1, 2, 3]
array = Array.new(3, 0)     # [0, 0, 0]
array = Array.new(3) { |i| i * 2 } # [0, 2, 4]

# Array access
array[0]                    # First element
array[-1]                   # Last element
array[1..3]                 # Range (inclusive)
array[1...3]                # Range (exclusive)
array.first                 # First element
array.last                  # Last element

# Array modification
array << 4                  # Append element
array.push(5)               # Append element
array.unshift(0)            # Prepend element
array.pop                   # Remove last element
array.shift                 # Remove first element
array.insert(1, "new")      # Insert at index

# Array methods
array.length                # Array size
array.empty?                # Check if empty
array.include?(3)           # Check if contains element
array.index(3)              # Find index of element
array.sort                  # Sort array
array.reverse               # Reverse array
array.uniq                  # Remove duplicates
array.flatten               # Flatten nested arrays
array.compact               # Remove nil values
```

### Hashes
```ruby
# Hash creation
hash = {name: "John", age: 30}
hash = Hash.new(0)          # Hash with default value
hash = Hash.new { |h, k| h[k] = [] } # Hash with default block

# Hash access
hash[:name]                 # Access by symbol key
hash["name"]                # Access by string key
hash.fetch(:name, "default") # Access with default
hash.key?(:name)            # Check if key exists
hash.value?("John")         # Check if value exists

# Hash modification
hash[:city] = "NYC"         # Add/update key
hash.merge(other_hash)      # Merge hashes
hash.update(other_hash)     # Merge and modify original
hash.delete(:age)           # Remove key
hash.clear                  # Remove all keys

# Hash methods
hash.keys                   # Get all keys
hash.values                 # Get all values
hash.length                 # Hash size
hash.empty?                 # Check if empty
hash.invert                 # Swap keys and values
hash.transform_keys(&:to_s) # Transform keys
hash.transform_values(&:upcase) # Transform values
```

### Strings
```ruby
# String creation
string = "Hello"
string = %q{Hello}          # Single quoted
string = %Q{Hello #{name}}  # Double quoted
string = <<~HEREDOC         # Heredoc
  Multi-line
  string
HEREDOC

# String interpolation
name = "World"
greeting = "Hello, #{name}!" # Interpolation
greeting = 'Hello, #{name}!' # No interpolation

# String methods
string.length               # String length
string.empty?               # Check if empty
string.upcase               # Convert to uppercase
string.downcase             # Convert to lowercase
string.capitalize           # Capitalize first letter
string.strip                # Remove whitespace
string.split(",")           # Split into array
string.gsub("old", "new")   # Global substitution
string.sub("old", "new")    # Single substitution
string.include?("substring") # Check if contains
string.start_with?("Hello") # Check prefix
string.end_with?("!")       # Check suffix
string.reverse               # Reverse string
string.chars                # Array of characters
string.bytes                # Array of byte values
```

## Common Built-in Functions

```ruby
# Type conversion
value.to_s                  # Convert to string
value.to_i                  # Convert to integer
value.to_f                  # Convert to float
value.to_a                  # Convert to array
value.to_h                  # Convert to hash
value.to_sym                # Convert to symbol

# Type checking
value.is_a?(String)         # Check type
value.kind_of?(Array)       # Check type
value.instance_of?(Hash)    # Check exact type
value.nil?                  # Check if nil
value.respond_to?(:method)  # Check if responds to method

# Math functions
Math.sqrt(16)               # Square root
Math.log(100)               # Natural logarithm
Math.sin(angle)             # Sine
Math.cos(angle)             # Cosine
Math.tan(angle)             # Tangent
Math::PI                    # Pi constant
Math::E                     # Euler's number

# Random numbers
rand                       # Random float 0-1
rand(10)                   # Random integer 0-9
rand(1..10)                # Random integer in range
Random.new.seed            # Random seed
```

## File I/O Operations

```ruby
# File reading
File.read("file.txt")       # Read entire file
File.readlines("file.txt")  # Read lines into array
File.exist?("file.txt")     # Check if file exists
File.size("file.txt")       # Get file size
File.mtime("file.txt")      # Get modification time

# File writing
File.write("file.txt", "content") # Write content
File.open("file.txt", "w") do |f| # Open for writing
  f.puts "line 1"
  f.puts "line 2"
end

# File modes
"r"                        # Read only (default)
"w"                        # Write (truncate)
"a"                        # Append
"r+"                       # Read and write
"w+"                       # Read and write (truncate)
"a+"                       # Read and append

# Directory operations
Dir.mkdir("new_dir")        # Create directory
Dir.rmdir("empty_dir")      # Remove empty directory
Dir.entries(".")            # List directory contents
Dir.glob("*.txt")           # Find files by pattern
Dir.pwd                     # Current working directory
Dir.chdir("/path")          # Change directory

# Path operations
File.join("path", "to", "file") # Join path components
File.dirname("/path/file.txt")  # Get directory name
File.basename("/path/file.txt") # Get filename
File.extname("file.txt")        # Get file extension
File.expand_path("~/file.txt")  # Expand path
```

## Error Handling

```ruby
# Basic exception handling
begin
  # Risky code
  result = 10 / 0
rescue ZeroDivisionError => e
  puts "Error: #{e.message}"
rescue => e
  puts "General error: #{e.message}"
else
  puts "No error occurred"
ensure
  puts "Always executed"
end

# Rescue modifier
result = 10 / 0 rescue "Error occurred"

# Custom exceptions
class CustomError < StandardError
  def initialize(message = "Custom error")
    super(message)
  end
end

# Raise exceptions
raise "Error message"       # Raise RuntimeError
raise CustomError, "Custom message" # Raise custom error
fail "Error message"        # Alias for raise

# Retry mechanism
begin
  # Risky operation
rescue => e
  retry if attempts < 3     # Retry up to 3 times
end
```

## Key Libraries/Modules

### Standard Library
```ruby
# JSON
require 'json'
JSON.parse('{"key": "value"}') # Parse JSON
{key: "value"}.to_json         # Convert to JSON

# CSV
require 'csv'
CSV.read("file.csv")           # Read CSV file
CSV.open("file.csv", "w") do |csv| # Write CSV
  csv << ["name", "age"]
  csv << ["John", 30]
end

# Date/Time
require 'date'
Date.today                     # Current date
Date.parse("2024-01-01")       # Parse date string
Time.now                       # Current time
Time.parse("2024-01-01 12:00") # Parse time string

# URI
require 'uri'
URI.parse("https://example.com") # Parse URL
URI.encode_www_form({q: "test"}) # Encode form data

# Net::HTTP
require 'net/http'
response = Net::HTTP.get_response(URI("https://example.com"))
response.body                  # Response body
response.code                  # Status code

# OpenURI
require 'open-uri'
content = URI.open("https://example.com").read # Read URL content

# FileUtils
require 'fileutils'
FileUtils.cp("source", "dest") # Copy file
FileUtils.mv("old", "new")     # Move file
FileUtils.rm("file")           # Remove file
FileUtils.mkdir_p("path")      # Create directory tree
```

### Common Gems
```ruby
# Bundler
require 'bundler/setup'        # Load gems from Gemfile
Bundler.require                # Require all gems

# RSpec (testing)
require 'rspec'
describe "MyClass" do
  it "should work" do
    expect(result).to eq(expected)
  end
end

# Sinatra (web framework)
require 'sinatra'
get '/' do
  "Hello, World!"
end

# Nokogiri (XML/HTML parsing)
require 'nokogiri'
doc = Nokogiri::HTML(html_string)
doc.css('h1').text             # Extract text from CSS selector

# HTTParty (HTTP client)
require 'httparty'
response = HTTParty.get('https://api.example.com')
response.body                  # Response body
response.code                  # Status code
```

## Blocks and Iterators

```ruby
# Block syntax
[1, 2, 3].each { |n| puts n } # Single line block
[1, 2, 3].each do |n|         # Multi-line block
  puts n
  puts n * 2
end

# Common iterators
array.each { |item| }         # Iterate over each element
array.map { |item| }          # Transform each element
array.select { |item| }       # Filter elements
array.reject { |item| }       # Filter out elements
array.find { |item| }         # Find first matching element
array.any? { |item| }         # Check if any element matches
array.all? { |item| }         # Check if all elements match
array.none? { |item| }        # Check if no elements match
array.count { |item| }        # Count matching elements
array.reduce(0) { |sum, item| sum + item } # Reduce to single value

# Block parameters
proc = Proc.new { |x, y| x + y }
lambda = ->(x, y) { x + y }

# Yield keyword
def with_block
  yield("Hello") if block_given?
end

with_block { |message| puts message }
```

## Regular Expressions

```ruby
# Regex creation
regex = /pattern/             # Regex literal
regex = Regexp.new("pattern") # Regex object

# Matching
string.match(/pattern/)       # Match pattern
string =~ /pattern/           # Match operator
string !~ /pattern/           # Not match operator

# Common patterns
/\d+/                        # One or more digits
/\w+/                        # One or more word characters
/\s+/                        # One or more whitespace
/^start/                     # Start of string
/end$/                       # End of string
/[abc]/                      # Character class
/[a-z]/                      # Range in character class
/\d{3}/                      # Exactly 3 digits
/\d{3,5}/                    # 3 to 5 digits
/\d{3,}/                     # 3 or more digits

# Substitution
string.gsub(/old/, "new")    # Global substitution
string.sub(/old/, "new")     # Single substitution
string.gsub(/pattern/) { |match| match.upcase } # Block substitution

# Capture groups
match = string.match(/(\w+)@(\w+)/)
match[1]                      # First capture group
match[2]                      # Second capture group
```

## Metaprogramming

```ruby
# Dynamic method definition
define_method(:greet) do |name|
  puts "Hello, #{name}!"
end

# Method missing
def method_missing(method_name, *args)
  puts "Method #{method_name} not found"
end

# Send method
object.send(:method_name, *args) # Call method dynamically
object.public_send(:method_name, *args) # Call public method

# Eval
eval("2 + 2")                # Evaluate string as code
instance_eval { @variable }   # Evaluate in object context
class_eval { def method; end } # Evaluate in class context

# Define class dynamically
MyClass = Class.new do
  def method
    "dynamic method"
  end
end

# Open classes (monkey patching)
class String
  def custom_method
    "custom"
  end
end
```

## Best Practices

```ruby
# Naming conventions
snake_case = "variables and methods" # Variables and methods
CamelCase = "classes and modules"    # Classes and modules
SCREAMING_SNAKE_CASE = "constants"   # Constants

# Code style
def method_name(param1, param2)      # Method definition
  # Use 2 spaces for indentation
  if condition
    # Do something
  end
end

# Guard clauses
def process_user(user)
  return unless user.valid?           # Early return
  return if user.admin?               # Guard clause
  
  # Main logic here
end

# Use symbols for hash keys
hash = {name: "John", age: 30}       # Preferred
hash = {"name" => "John", "age" => 30} # Less preferred

# Use unless for negative conditions
unless user.logged_in?               # Preferred
  redirect_to login_path
end

# Use unless instead of if !condition
unless condition                      # Preferred
  # code
end
``` 