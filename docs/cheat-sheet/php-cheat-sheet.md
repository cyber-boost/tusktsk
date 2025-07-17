# PHP Cheat Sheet

## Basic Syntax and Data Types

### Primitive Types
```php
// Numbers
$integer = 42;                      // Integer
$float = 3.14159;                   // Floating point
$hex = 0xFF;                        // Hexadecimal
$binary = 0b1010;                   // Binary
$octal = 0o755;                     // Octal

// Strings
$single = 'Hello';                  // Single quotes (literal)
$double = "World";                  // Double quotes (parsed)
$heredoc = <<<EOT                   // Heredoc syntax
Multi-line string
with variables: $variable
EOT;
$nowdoc = <<<'EOT'                  // Nowdoc syntax (literal)
Multi-line string
no variable parsing
EOT;

// Booleans
$true = true;                       // Boolean true
$false = false;                     // Boolean false

// Null
$empty = null;                      // Null value

// Arrays
$array = [1, 2, 3];                 // Array literal (PHP 5.4+)
$array = array(1, 2, 3);            // Array constructor
```

### Type Checking
```php
is_int($value);                     // Check if integer
is_float($value);                   // Check if float
is_string($value);                  // Check if string
is_bool($value);                    // Check if boolean
is_array($value);                   // Check if array
is_object($value);                  // Check if object
is_null($value);                    // Check if null
is_numeric($value);                 // Check if numeric
is_callable($value);                // Check if callable
gettype($value);                    // Get type as string
var_dump($value);                   // Dump variable info
```

## Variables and Operators

### Variable Declaration
```php
$variable = "value";                // Variable assignment
$$variable = "dynamic";             // Variable variable
const CONSTANT = "value";           // Class constant
define('CONSTANT', 'value');        // Global constant

// Variable scope
global $variable;                   // Access global variable
static $variable = 0;               // Static variable
```

### Operators
```php
// Arithmetic
$sum = $a + $b;                     // Addition
$diff = $a - $b;                    // Subtraction
$product = $a * $b;                 // Multiplication
$quotient = $a / $b;                // Division
$remainder = $a % $b;               // Modulo
$power = $a ** $b;                  // Exponentiation (PHP 5.6+)
$increment = ++$a;                  // Pre-increment
$decrement = --$a;                  // Pre-decrement

// Assignment
$x = 5;                            // Assignment
$x += 3;                           // x = x + 3
$x -= 2;                           // x = x - 2
$x *= 4;                           // x = x * 4
$x /= 2;                           // x = x / 2
$x %= 3;                           // x = x % 3

// Comparison
$equal = $a == $b;                  // Loose equality
$strict = $a === $b;                // Strict equality
$notEqual = $a != $b;               // Loose inequality
$strictNot = $a !== $b;             // Strict inequality
$greater = $a > $b;                 // Greater than
$less = $a < $b;                    // Less than
$gte = $a >= $b;                    // Greater or equal
$lte = $a <= $b;                    // Less or equal

// Logical
$and = $a && $b;                    // Logical AND
$or = $a || $b;                     // Logical OR
$not = !$a;                         // Logical NOT
$xor = $a xor $b;                   // Logical XOR
$nullish = $a ?? $b;                // Null coalescing (PHP 7+)
$ternary = $a ? $b : $c;            // Ternary operator
```

## Control Structures

### Conditionals
```php
// If/else
if ($condition) {
    // code
} elseif ($otherCondition) {
    // code
} else {
    // code
}

// Switch
switch ($value) {
    case 1:
        // code
        break;
    case 2:
        // code
        break;
    default:
        // code
}

// Match (PHP 8+)
$result = match($value) {
    1 => 'one',
    2 => 'two',
    default => 'unknown'
};
```

### Loops
```php
// For loop
for ($i = 0; $i < 10; $i++) {
    // code
}

// Foreach loop
foreach ($array as $value) {
    // code
}
foreach ($array as $key => $value) {
    // code
}

// While loop
while ($condition) {
    // code
}

// Do...while
do {
    // code
} while ($condition);

// Break and continue
break;                              // Exit loop
break 2;                            // Exit 2 levels
continue;                           // Skip iteration
continue 2;                         // Skip 2 levels
```

## Functions

### Function Declaration
```php
// Basic function
function name($param1, $param2) {
    return $result;
}

// Function with default parameters
function greet($name = "World") {
    return "Hello $name";
}

// Function with type hints
function add(int $a, int $b): int {
    return $a + $b;
}

// Function with nullable types
function process(?string $input): ?string {
    return $input ? strtoupper($input) : null;
}

// Function with union types (PHP 8+)
function process(string|int $input): string|int {
    return is_string($input) ? strtoupper($input) : $input * 2;
}

// Anonymous function
$func = function($param) {
    return $param * 2;
};

// Arrow function (PHP 7.4+)
$arrow = fn($param) => $param * 2;
```

### Function Scope
```php
// Global variable access
function test() {
    global $variable;               // Access global
    $GLOBALS['variable'] = 'value'; // Alternative
}

// Static variables
function counter() {
    static $count = 0;              // Persists between calls
    return ++$count;
}

// Variable scope
function scope() {
    $local = 'local';               // Local variable
    return $local;
}
```

## Data Structures

### Arrays
```php
// Indexed arrays
$array = [1, 2, 3];                 // Array literal
$array = array(1, 2, 3);            // Array constructor
$array[] = 4;                       // Add to end
$array[0] = 10;                     // Set specific index

// Associative arrays
$assoc = ['name' => 'John', 'age' => 30];
$assoc['city'] = 'New York';        // Add key-value

// Multi-dimensional arrays
$multi = [
    'users' => [
        ['name' => 'John', 'age' => 30],
        ['name' => 'Jane', 'age' => 25]
    ]
];

// Array functions
count($array);                      // Get array length
array_push($array, $value);         // Add to end
array_pop($array);                  // Remove from end
array_unshift($array, $value);      // Add to beginning
array_shift($array);                // Remove from beginning
array_merge($array1, $array2);      // Merge arrays
array_slice($array, 0, 3);          // Extract portion
array_keys($array);                 // Get keys
array_values($array);               // Get values
in_array($value, $array);           // Check if exists
array_search($value, $array);       // Find key
array_unique($array);               // Remove duplicates
sort($array);                       // Sort array
rsort($array);                      // Reverse sort
asort($array);                      // Sort associative
arsort($array);                     // Reverse sort associative
ksort($array);                      // Sort by keys
krsort($array);                     // Reverse sort by keys
```

### Objects
```php
// Object creation
$object = new stdClass();
$object->property = 'value';        // Set property
$value = $object->property;         // Get property

// Object functions
get_object_vars($object);           // Get properties
property_exists($object, 'prop');   // Check property
method_exists($object, 'method');   // Check method
is_object($object);                 // Check if object
```

## Common Built-in Functions

### String Functions
```php
// String manipulation
strlen($string);                    // Get length
strpos($string, $needle);           // Find position
str_replace($search, $replace, $string); // Replace
substr($string, $start, $length);   // Extract substring
strtolower($string);                // Convert to lowercase
strtoupper($string);                // Convert to uppercase
ucfirst($string);                   // Capitalize first
ucwords($string);                   // Capitalize words
trim($string);                      // Remove whitespace
ltrim($string);                     // Remove left whitespace
rtrim($string);                     // Remove right whitespace
explode($delimiter, $string);       // Split to array
implode($glue, $array);             // Join array to string
str_split($string, $length);        // Split to characters
preg_split($pattern, $string);      // Split by regex
htmlspecialchars($string);          // Escape HTML
strip_tags($string);                // Remove HTML tags
md5($string);                       // MD5 hash
sha1($string);                      // SHA1 hash
password_hash($string, PASSWORD_DEFAULT); // Hash password
password_verify($string, $hash);    // Verify password
```

### Number Functions
```php
// Number manipulation
abs($number);                       // Absolute value
ceil($number);                      // Round up
floor($number);                     // Round down
round($number, $precision);         // Round to precision
min($array);                        // Minimum value
max($array);                        // Maximum value
rand($min, $max);                   // Random integer
mt_rand($min, $max);                // Better random
is_numeric($value);                 // Check if numeric
number_format($number, $decimals);  // Format number
```

### Array Functions
```php
// Array manipulation
array_map($callback, $array);       // Apply function to all
array_filter($array, $callback);    // Filter array
array_reduce($array, $callback);    // Reduce array
array_walk($array, $callback);      // Walk through array
array_column($array, $column);      // Extract column
array_chunk($array, $size);         // Split into chunks
array_fill($start, $count, $value); // Fill array
array_flip($array);                 // Swap keys and values
array_intersect($array1, $array2);  // Common elements
array_diff($array1, $array2);       // Different elements
array_sum($array);                  // Sum all values
array_product($array);              // Product of values
array_rand($array);                 // Random key
shuffle($array);                    // Shuffle array
```

### Date and Time Functions
```php
// Date functions
date('Y-m-d H:i:s');                // Current date/time
time();                             // Current timestamp
strtotime($string);                 // Parse date string
mktime($hour, $min, $sec, $mon, $day, $year); // Create timestamp
date_create($string);               // Create DateTime object
date_format($date, $format);        // Format date
date_add($date, $interval);         // Add interval
date_diff($date1, $date2);          // Date difference
date_default_timezone_set('UTC');   // Set timezone
```

## File I/O Operations

### File Operations
```php
// File reading
file_get_contents($filename);       // Read entire file
file_put_contents($filename, $data); // Write to file
fopen($filename, $mode);            // Open file handle
fread($handle, $length);            // Read from handle
fwrite($handle, $data);             // Write to handle
fclose($handle);                    // Close handle
fgets($handle);                     // Read line
fgetcsv($handle);                   // Read CSV line
file($filename);                    // Read file to array

// File information
file_exists($filename);             // Check if exists
is_file($filename);                 // Check if file
is_dir($filename);                  // Check if directory
is_readable($filename);             // Check if readable
is_writable($filename);             // Check if writable
filesize($filename);                // Get file size
filemtime($filename);               // Get modification time
fileperms($filename);               // Get permissions

// Directory operations
mkdir($dirname);                    // Create directory
rmdir($dirname);                    // Remove directory
opendir($dirname);                  // Open directory
readdir($handle);                   // Read directory entry
closedir($handle);                  // Close directory
scandir($dirname);                  // List directory contents
glob($pattern);                     // Find files by pattern
```

### File Modes
```php
'r'                                 // Read only
'w'                                 // Write only (truncate)
'a'                                 // Write only (append)
'x'                                 // Write only (create)
'r+'                                // Read/write
'w+'                                // Read/write (truncate)
'a+'                                // Read/write (append)
'x+'                                // Read/write (create)
```

## Error Handling

### Error Control
```php
// Error suppression
@function_call();                   // Suppress errors

// Error reporting
error_reporting(E_ALL);             // Report all errors
error_reporting(0);                 // Disable error reporting
ini_set('display_errors', 1);       // Display errors
ini_set('log_errors', 1);           // Log errors
ini_set('error_log', '/path/to/log'); // Set log file

// Custom error handler
set_error_handler('my_error_handler');
function my_error_handler($errno, $errstr, $errfile, $errline) {
    // Handle error
}
```

### Exception Handling
```php
// Try-catch
try {
    // Risky code
    throw new Exception("Error message");
} catch (Exception $e) {
    // Handle exception
    echo $e->getMessage();
} catch (CustomException $e) {
    // Handle custom exception
} finally {
    // Always execute
}

// Custom exceptions
class CustomException extends Exception {
    public function __construct($message, $code = 0) {
        parent::__construct($message, $code);
    }
}

// Exception methods
$e->getMessage();                   // Get error message
$e->getCode();                      // Get error code
$e->getFile();                      // Get file name
$e->getLine();                      // Get line number
$e->getTrace();                     // Get stack trace
$e->getTraceAsString();             // Get trace as string
```

## Key Libraries/Modules

### PDO Database
```php
// Database connection
$pdo = new PDO($dsn, $username, $password);
$pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

// Prepared statements
$stmt = $pdo->prepare("SELECT * FROM users WHERE id = ?");
$stmt->execute([$id]);
$user = $stmt->fetch(PDO::FETCH_ASSOC);

// Transactions
$pdo->beginTransaction();
try {
    // Database operations
    $pdo->commit();
} catch (Exception $e) {
    $pdo->rollBack();
    throw $e;
}

// Fetch modes
PDO::FETCH_ASSOC                    // Associative array
PDO::FETCH_NUM                      // Numeric array
PDO::FETCH_BOTH                     // Both arrays
PDO::FETCH_OBJ                      // Object
PDO::FETCH_CLASS                    // Class instance
```

### cURL
```php
// Basic cURL
$ch = curl_init();
curl_setopt($ch, CURLOPT_URL, $url);
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
$response = curl_exec($ch);
curl_close($ch);

// POST request
curl_setopt($ch, CURLOPT_POST, true);
curl_setopt($ch, CURLOPT_POSTFIELDS, $data);

// With headers
curl_setopt($ch, CURLOPT_HTTPHEADER, [
    'Content-Type: application/json',
    'Authorization: Bearer ' . $token
]);

// Error handling
if (curl_errno($ch)) {
    $error = curl_error($ch);
}
```

### JSON
```php
// JSON encoding/decoding
json_encode($data);                 // Encode to JSON
json_decode($json, true);           // Decode from JSON (assoc)
json_decode($json);                 // Decode from JSON (object)

// JSON options
JSON_PRETTY_PRINT                   // Pretty print
JSON_UNESCAPED_UNICODE              // Don't escape Unicode
JSON_UNESCAPED_SLASHES              // Don't escape slashes
JSON_PRESERVE_ZERO_FRACTION         // Preserve zero fraction

// JSON error handling
json_last_error();                  // Get last error
json_last_error_msg();              // Get error message
```

### Regular Expressions
```php
// PCRE functions
preg_match($pattern, $subject);     // Match pattern
preg_match_all($pattern, $subject); // Match all occurrences
preg_replace($pattern, $replacement, $subject); // Replace
preg_split($pattern, $subject);     // Split by pattern
preg_quote($string);                // Quote special characters

// Pattern modifiers
i                                   // Case insensitive
m                                   // Multiline
s                                   // Dot matches newlines
u                                   // Unicode
x                                   // Extended whitespace
```

### Session Management
```php
// Session functions
session_start();                    // Start session
session_id();                       // Get session ID
session_name();                     // Get session name
session_save_path();                // Get save path
session_destroy();                  // Destroy session
session_unset();                    // Unset all variables

// Session variables
$_SESSION['key'] = 'value';         // Set session variable
$value = $_SESSION['key'];          // Get session variable
unset($_SESSION['key']);            // Unset variable
isset($_SESSION['key']);            // Check if set
```

### Cookie Management
```php
// Set cookie
setcookie($name, $value, $expire, $path, $domain, $secure, $httponly);

// Get cookie
$value = $_COOKIE['name'];

// Delete cookie
setcookie($name, '', time() - 3600);

// Cookie options
time() + 3600                       // 1 hour from now
'/'                                 // Available on entire site
'.example.com'                      // Available on subdomains
true                                // HTTPS only
true                                // HTTP only (no JavaScript)
```

### File Upload
```php
// Upload handling
if (isset($_FILES['file'])) {
    $file = $_FILES['file'];
    $name = $file['name'];
    $type = $file['type'];
    $size = $file['size'];
    $tmp_name = $file['tmp_name'];
    $error = $file['error'];
    
    if ($error === UPLOAD_ERR_OK) {
        move_uploaded_file($tmp_name, $destination);
    }
}

// Upload error codes
UPLOAD_ERR_OK                       // No error
UPLOAD_ERR_INI_SIZE                 // File too large (ini)
UPLOAD_ERR_FORM_SIZE                // File too large (form)
UPLOAD_ERR_PARTIAL                  // Partial upload
UPLOAD_ERR_NO_FILE                  // No file uploaded
UPLOAD_ERR_NO_TMP_DIR               // No temp directory
UPLOAD_ERR_CANT_WRITE               // Can't write to disk
UPLOAD_ERR_EXTENSION                // Extension stopped upload
```

### Composer (Package Management)
```php
// Autoloading
require 'vendor/autoload.php';      // Include Composer autoloader

// Use statements
use Namespace\ClassName;            // Import class
use Namespace\ClassName as Alias;   // Import with alias
use function Namespace\functionName; // Import function
use const Namespace\CONSTANT;       // Import constant

// Composer.json example
{
    "require": {
        "monolog/monolog": "^2.0",
        "guzzlehttp/guzzle": "^7.0"
    },
    "autoload": {
        "psr-4": {
            "App\\": "src/"
        }
    }
}
```

### Namespaces
```php
// Namespace declaration
namespace MyApp\Controllers;

// Use statements
use MyApp\Models\User;
use MyApp\Services\EmailService as Mailer;

// Global namespace
$global = new \GlobalClass();

// Namespace constants
namespace MyApp;
const VERSION = '1.0.0';

// Namespace functions
namespace MyApp;
function helper() {
    return 'helper function';
}
```

### Classes and Objects
```php
// Class definition
class User {
    // Properties
    public $name;
    private $email;
    protected $password;
    
    // Constructor
    public function __construct($name, $email) {
        $this->name = $name;
        $this->email = $email;
    }
    
    // Methods
    public function getName() {
        return $this->name;
    }
    
    // Static method
    public static function create($name, $email) {
        return new self($name, $email);
    }
    
    // Magic methods
    public function __toString() {
        return $this->name;
    }
    
    public function __get($property) {
        return $this->$property;
    }
    
    public function __set($property, $value) {
        $this->$property = $value;
    }
}

// Object creation
$user = new User('John', 'john@example.com');
$user = User::create('John', 'john@example.com');

// Inheritance
class Admin extends User {
    public function isAdmin() {
        return true;
    }
}
```

### Traits
```php
// Trait definition
trait Loggable {
    public function log($message) {
        echo "Log: $message\n";
    }
}

// Using traits
class User {
    use Loggable;
    
    public function create() {
        $this->log('User created');
    }
}

// Multiple traits
trait Timestampable {
    public function getCreatedAt() {
        return date('Y-m-d H:i:s');
    }
}

class Post {
    use Loggable, Timestampable;
}
```

### Interfaces
```php
// Interface definition
interface LoggerInterface {
    public function log($message);
    public function error($message);
}

// Implementing interface
class FileLogger implements LoggerInterface {
    public function log($message) {
        file_put_contents('log.txt', $message . "\n", FILE_APPEND);
    }
    
    public function error($message) {
        $this->log("ERROR: $message");
    }
}
```

### Generators
```php
// Generator function
function numberGenerator($start, $end) {
    for ($i = $start; $i <= $end; $i++) {
        yield $i;
    }
}

// Using generator
foreach (numberGenerator(1, 10) as $number) {
    echo $number . "\n";
}

// Generator with keys
function keyValueGenerator() {
    yield 'key1' => 'value1';
    yield 'key2' => 'value2';
}
```

### Closures
```php
// Closure definition
$closure = function($name) {
    return "Hello, $name!";
};

// Using closure
echo $closure('John');

// Closure with use
$prefix = 'Hello';
$closure = function($name) use ($prefix) {
    return "$prefix, $name!";
};

// Arrow function (PHP 7.4+)
$arrow = fn($name) => "Hello, $name!";
```

### Type Declarations
```php
// Parameter types
function process(string $name, int $age): string {
    return "$name is $age years old";
}

// Return types
function getAge(): int {
    return 25;
}

// Nullable types
function process(?string $input): ?string {
    return $input ? strtoupper($input) : null;
}

// Union types (PHP 8+)
function process(string|int $input): string|int {
    return is_string($input) ? strtoupper($input) : $input * 2;
}

// Mixed type (PHP 8+)
function process(mixed $input): mixed {
    return $input;
}
```

### Attributes (PHP 8+)
```php
// Attribute definition
#[Attribute]
class Route {
    public function __construct(public string $path) {}
}

// Using attributes
#[Route('/users')]
class UserController {
    #[Route('/list')]
    public function list() {
        // Method implementation
    }
}
```

### Match Expression (PHP 8+)
```php
// Match expression
$result = match($value) {
    1 => 'one',
    2 => 'two',
    3, 4, 5 => 'three to five',
    default => 'unknown'
};

// Match with conditions
$result = match(true) {
    $age < 18 => 'minor',
    $age < 65 => 'adult',
    default => 'senior'
};
```

### Named Arguments (PHP 8+)
```php
// Function with named arguments
function createUser(string $name, string $email, int $age = 0) {
    return compact('name', 'email', 'age');
}

// Using named arguments
$user = createUser(
    name: 'John',
    email: 'john@example.com',
    age: 30
);
```

### Constructor Property Promotion (PHP 8+)
```php
// Constructor property promotion
class User {
    public function __construct(
        public string $name,
        public string $email,
        private int $age = 0
    ) {}
}

// Equivalent to:
class User {
    public string $name;
    public string $email;
    private int $age;
    
    public function __construct(string $name, string $email, int $age = 0) {
        $this->name = $name;
        $this->email = $email;
        $this->age = $age;
    }
}
```

### Nullsafe Operator (PHP 8+)
```php
// Nullsafe operator
$result = $object?->method()?->property;

// Equivalent to:
$result = null;
if ($object !== null) {
    $temp = $object->method();
    if ($temp !== null) {
        $result = $temp->property;
    }
}
```

### String Functions (PHP 8+)
```php
// New string functions
str_contains($haystack, $needle);   // Check if contains
str_starts_with($haystack, $needle); // Check if starts with
str_ends_with($haystack, $needle);  // Check if ends with
str_contains($haystack, $needle);   // Check if contains
```

### Array Functions (PHP 8+)
```php
// New array functions
array_is_list($array);              // Check if indexed array
array_key_first($array);            // Get first key
array_key_last($array);             // Get last key
```

### Performance Tips
```php
// Use isset() instead of array_key_exists() for performance
isset($array['key']);               // Faster
array_key_exists('key', $array);    // Slower

// Use === instead of == for strict comparison
$value === 'string';                // Faster and safer
$value == 'string';                 // Slower, type juggling

// Use single quotes for strings without variables
$string = 'Hello World';            // Faster
$string = "Hello World";            // Slower

// Use foreach instead of for for arrays
foreach ($array as $value) {        // Faster
    // code
}
for ($i = 0; $i < count($array); $i++) { // Slower
    // code
}

// Cache function results
$count = count($array);             // Cache once
for ($i = 0; $i < $count; $i++) {   // Use cached value
    // code
}
```

### Security Best Practices
```php
// SQL injection prevention
$stmt = $pdo->prepare("SELECT * FROM users WHERE id = ?");
$stmt->execute([$id]);

// XSS prevention
echo htmlspecialchars($userInput);

// CSRF protection
if (!hash_equals($_SESSION['csrf_token'], $_POST['csrf_token'])) {
    die('CSRF token mismatch');
}

// Password hashing
$hash = password_hash($password, PASSWORD_DEFAULT);
if (password_verify($password, $hash)) {
    // Password is correct
}

// File upload security
$allowedTypes = ['image/jpeg', 'image/png'];
if (!in_array($_FILES['file']['type'], $allowedTypes)) {
    die('Invalid file type');
}

// Input validation
$email = filter_var($input, FILTER_VALIDATE_EMAIL);
if ($email === false) {
    die('Invalid email');
}
``` 