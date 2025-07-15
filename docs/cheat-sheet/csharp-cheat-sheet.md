# C# Cheat Sheet

## Basic Syntax and Data Types

### Primitive Types
```csharp
// Numeric types
int number = 42;                    // 32-bit integer
long bigNumber = 1234567890L;       // 64-bit integer
double decimal = 3.14;              // 64-bit floating point
float single = 3.14f;               // 32-bit floating point
decimal precise = 3.14m;            // 128-bit decimal

// Text types
string text = "Hello World";        // Unicode string
char character = 'A';               // 16-bit Unicode character

// Boolean
bool flag = true;                   // true/false value

// Other
object obj = new object();          // Base type for all objects
dynamic dynamic = "anything";       // Runtime type checking
```

### Reference Types
```csharp
// Arrays
int[] numbers = { 1, 2, 3, 4, 5 };           // Fixed-size array
int[,] matrix = new int[2, 3];               // 2D array
int[][] jagged = new int[3][];               // Jagged array

// Collections
List<int> list = new List<int>();            // Dynamic list
Dictionary<string, int> dict = new Dictionary<string, int>(); // Key-value pairs
HashSet<int> set = new HashSet<int>();       // Unique values
Queue<string> queue = new Queue<string>();   // FIFO collection
Stack<int> stack = new Stack<int>();         // LIFO collection

// Nullable types
int? nullableInt = null;                     // Nullable integer
Nullable<int> nullable = null;               // Alternative syntax
```

## Variables and Operators

### Variable Declaration
```csharp
// Explicit typing
string name = "John";
var age = 25;                               // Type inference
const double PI = 3.14159;                  // Compile-time constant
readonly int MAX_SIZE = 100;                // Runtime constant

// Multiple declaration
int x = 1, y = 2, z = 3;
```

### Operators
```csharp
// Arithmetic
int sum = a + b;                            // Addition
int diff = a - b;                           // Subtraction
int product = a * b;                        // Multiplication
int quotient = a / b;                       // Division
int remainder = a % b;                      // Modulo
int power = (int)Math.Pow(a, b);            // Exponentiation

// Assignment
int value = 10;                             // Simple assignment
value += 5;                                 // Compound assignment
value -= 3;                                 // value = value - 3
value *= 2;                                 // value = value * 2
value /= 4;                                 // value = value / 4

// Comparison
bool equal = a == b;                        // Equality
bool notEqual = a != b;                     // Inequality
bool greater = a > b;                       // Greater than
bool less = a < b;                          // Less than
bool greaterEqual = a >= b;                 // Greater or equal
bool lessEqual = a <= b;                    // Less or equal

// Logical
bool and = a && b;                          // Logical AND
bool or = a || b;                           // Logical OR
bool not = !a;                              // Logical NOT
bool xor = a ^ b;                           // Logical XOR

// Bitwise
int bitAnd = a & b;                         // Bitwise AND
int bitOr = a | b;                          // Bitwise OR
int bitXor = a ^ b;                         // Bitwise XOR
int bitNot = ~a;                            // Bitwise NOT
int leftShift = a << 2;                     // Left shift
int rightShift = a >> 2;                    // Right shift

// Null operators
string result = name ?? "Default";          // Null coalescing
string length = name?.Length.ToString();    // Null conditional
string safe = name ??= "Default";           // Null assignment
```

## Control Structures

### Conditional Statements
```csharp
// If-else
if (condition)
{
    // code
}
else if (otherCondition)
{
    // code
}
else
{
    // code
}

// Ternary operator
string result = condition ? "Yes" : "No";

// Switch expression (C# 8.0+)
string message = value switch
{
    1 => "One",
    2 => "Two",
    _ => "Unknown"
};

// Switch statement
switch (value)
{
    case 1:
        Console.WriteLine("One");
        break;
    case 2:
        Console.WriteLine("Two");
        break;
    default:
        Console.WriteLine("Unknown");
        break;
}
```

### Loops
```csharp
// For loop
for (int i = 0; i < 10; i++)
{
    Console.WriteLine(i);
}

// While loop
while (condition)
{
    // code
}

// Do-while loop
do
{
    // code
} while (condition);

// Foreach loop
foreach (var item in collection)
{
    Console.WriteLine(item);
}

// Loop control
for (int i = 0; i < 10; i++)
{
    if (i == 5) continue;                   // Skip iteration
    if (i == 8) break;                      // Exit loop
}
```

## Functions/Methods

### Method Declaration
```csharp
// Basic method
public void DoSomething()
{
    // code
}

// Method with parameters
public int Add(int a, int b)
{
    return a + b;
}

// Method with optional parameters
public void Greet(string name, string greeting = "Hello")
{
    Console.WriteLine($"{greeting}, {name}!");
}

// Method with params
public int Sum(params int[] numbers)
{
    return numbers.Sum();
}

// Expression-bodied member
public int Multiply(int a, int b) => a * b;

// Async method
public async Task<string> GetDataAsync()
{
    await Task.Delay(1000);
    return "Data";
}
```

### Lambda Expressions
```csharp
// Lambda expression
Func<int, int> square = x => x * x;

// Lambda with multiple parameters
Func<int, int, int> add = (x, y) => x + y;

// Lambda with statement body
Action<string> print = name =>
{
    Console.WriteLine($"Hello, {name}!");
};

// LINQ with lambda
var filtered = numbers.Where(n => n > 5);
var doubled = numbers.Select(n => n * 2);
```

## Data Structures

### Arrays and Collections
```csharp
// Array initialization
int[] numbers = { 1, 2, 3, 4, 5 };
int[] empty = new int[5];

// List operations
List<int> list = new List<int> { 1, 2, 3 };
list.Add(4);                               // Add item
list.Remove(2);                            // Remove item
list.RemoveAt(0);                          // Remove at index
list.Insert(1, 10);                        // Insert at index
bool contains = list.Contains(3);          // Check if contains
int count = list.Count;                    // Get count

// Dictionary operations
Dictionary<string, int> dict = new Dictionary<string, int>();
dict.Add("key", 42);                       // Add key-value
dict["key"] = 100;                         // Set value
int value = dict["key"];                   // Get value
bool hasKey = dict.ContainsKey("key");     // Check key exists
dict.Remove("key");                        // Remove key-value

// LINQ operations
var filtered = list.Where(x => x > 5);
var sorted = list.OrderBy(x => x);
var grouped = list.GroupBy(x => x % 2);
var first = list.First();
var last = list.Last();
var any = list.Any(x => x > 10);
var all = list.All(x => x > 0);
```

### Custom Data Structures
```csharp
// Struct (value type)
public struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
    
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}

// Class (reference type)
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    
    public Person(string name, int age)
    {
        Name = name;
        Age = age;
    }
    
    public override string ToString()
    {
        return $"{Name} ({Age})";
    }
}
```

## Common Built-in Functions

### String Operations
```csharp
string text = "Hello World";

// String methods
int length = text.Length;                   // Get length
string upper = text.ToUpper();              // Convert to uppercase
string lower = text.ToLower();              // Convert to lowercase
string trimmed = text.Trim();               // Remove whitespace
string[] parts = text.Split(' ');           // Split string
string joined = string.Join("-", parts);    // Join strings
bool starts = text.StartsWith("Hello");     // Check prefix
bool ends = text.EndsWith("World");         // Check suffix
bool contains = text.Contains("lo");        // Check substring
string replaced = text.Replace("World", "C#"); // Replace substring
int index = text.IndexOf("o");              // Find index
string substring = text.Substring(0, 5);    // Extract substring

// String interpolation
string message = $"Hello {name}, you are {age} years old";
string formatted = $"{value:C}";            // Currency format
string padded = $"{number:D4}";             // Zero-padded
```

### Math Operations
```csharp
// Math class
double abs = Math.Abs(-5.5);                // Absolute value
double round = Math.Round(3.7);             // Round to nearest
double ceiling = Math.Ceiling(3.2);         // Round up
double floor = Math.Floor(3.8);             // Round down
double power = Math.Pow(2, 3);              // Exponentiation
double sqrt = Math.Sqrt(16);                // Square root
double max = Math.Max(5, 10);               // Maximum value
double min = Math.Min(5, 10);               // Minimum value
double random = new Random().NextDouble();  // Random 0-1
int randomInt = new Random().Next(1, 101);  // Random range
```

### DateTime Operations
```csharp
// DateTime creation
DateTime now = DateTime.Now;                // Current date/time
DateTime today = DateTime.Today;            // Current date
DateTime utc = DateTime.UtcNow;             // UTC date/time
DateTime custom = new DateTime(2023, 12, 25); // Custom date

// DateTime operations
DateTime tomorrow = today.AddDays(1);       // Add days
DateTime nextWeek = today.AddDays(7);       // Add days
DateTime nextMonth = today.AddMonths(1);    // Add months
DateTime nextYear = today.AddYears(1);      // Add years
TimeSpan diff = tomorrow - today;           // Time difference
int dayOfWeek = (int)today.DayOfWeek;       // Day of week (0=Sunday)
string formatted = today.ToString("yyyy-MM-dd"); // Format date
```

## File I/O Operations

### File Operations
```csharp
// Read file
string content = File.ReadAllText("file.txt");
string[] lines = File.ReadAllLines("file.txt");
byte[] bytes = File.ReadAllBytes("file.bin");

// Write file
File.WriteAllText("file.txt", "content");
File.WriteAllLines("file.txt", lines);
File.WriteAllBytes("file.bin", bytes);

// Append to file
File.AppendAllText("file.txt", "new content");
File.AppendAllLines("file.txt", newLines);

// File info
bool exists = File.Exists("file.txt");
DateTime created = File.GetCreationTime("file.txt");
DateTime modified = File.GetLastWriteTime("file.txt");
long size = new FileInfo("file.txt").Length;

// Directory operations
Directory.CreateDirectory("newfolder");
string[] files = Directory.GetFiles("folder");
string[] dirs = Directory.GetDirectories("folder");
Directory.Delete("folder", true);           // Recursive delete
```

### Stream Operations
```csharp
// Read with StreamReader
using (var reader = new StreamReader("file.txt"))
{
    string line;
    while ((line = reader.ReadLine()) != null)
    {
        Console.WriteLine(line);
    }
}

// Write with StreamWriter
using (var writer = new StreamWriter("file.txt"))
{
    writer.WriteLine("Line 1");
    writer.WriteLine("Line 2");
}

// Binary operations
using (var stream = new FileStream("file.bin", FileMode.Open))
{
    byte[] buffer = new byte[1024];
    int bytesRead = stream.Read(buffer, 0, buffer.Length);
}
```

## Error Handling

### Exception Handling
```csharp
// Try-catch
try
{
    // Risky code
    int result = int.Parse("invalid");
}
catch (FormatException ex)
{
    Console.WriteLine($"Format error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"General error: {ex.Message}");
}
finally
{
    // Always executed
    Console.WriteLine("Cleanup code");
}

// Exception filters (C# 6.0+)
try
{
    // code
}
catch (Exception ex) when (ex.Message.Contains("specific"))
{
    // Handle specific error
}

// Using statement (automatic disposal)
using (var resource = new SomeResource())
{
    // Use resource
} // Automatically disposed

// Using declaration (C# 8.0+)
using var resource = new SomeResource();
// Use resource
// Automatically disposed at end of scope
```

### Custom Exceptions
```csharp
public class CustomException : Exception
{
    public CustomException() : base() { }
    public CustomException(string message) : base(message) { }
    public CustomException(string message, Exception inner) 
        : base(message, inner) { }
}

// Throwing exceptions
throw new ArgumentException("Invalid argument");
throw new InvalidOperationException("Invalid operation");
throw new CustomException("Custom error message");
```

## Key Libraries/Modules

### LINQ (Language Integrated Query)
```csharp
// Query syntax
var query = from item in collection
            where item > 5
            orderby item
            select item * 2;

// Method syntax
var result = collection
    .Where(item => item > 5)
    .OrderBy(item => item)
    .Select(item => item * 2);

// Common LINQ methods
var filtered = list.Where(x => x > 5);
var projected = list.Select(x => x.ToString());
var ordered = list.OrderBy(x => x);
var grouped = list.GroupBy(x => x % 2);
var aggregated = list.Aggregate((a, b) => a + b);
var distinct = list.Distinct();
var skipped = list.Skip(5);
var taken = list.Take(10);
```

### Collections
```csharp
// Generic collections
List<T> list = new List<T>();               // Dynamic array
Dictionary<K, V> dict = new Dictionary<K, V>(); // Key-value pairs
HashSet<T> set = new HashSet<T>();           // Unique values
Queue<T> queue = new Queue<T>();             // FIFO collection
Stack<T> stack = new Stack<T>();             // LIFO collection
LinkedList<T> linked = new LinkedList<T>();  // Doubly-linked list

// Concurrent collections
ConcurrentDictionary<K, V> concurrentDict = new ConcurrentDictionary<K, V>();
ConcurrentQueue<T> concurrentQueue = new ConcurrentQueue<T>();
ConcurrentStack<T> concurrentStack = new ConcurrentStack<T>();
```

### Reflection
```csharp
// Get type information
Type type = typeof(MyClass);
Type runtimeType = obj.GetType();

// Get properties
PropertyInfo[] properties = type.GetProperties();
PropertyInfo prop = type.GetProperty("Name");

// Get methods
MethodInfo[] methods = type.GetMethods();
MethodInfo method = type.GetMethod("DoSomething");

// Invoke methods
object result = method.Invoke(obj, new object[] { "param" });

// Create instance
object instance = Activator.CreateInstance(type);
```

### Async/Await
```csharp
// Async method
public async Task<string> GetDataAsync()
{
    await Task.Delay(1000);
    return "Data";
}

// Async with cancellation
public async Task<string> GetDataAsync(CancellationToken token)
{
    await Task.Delay(1000, token);
    return "Data";
}

// Parallel operations
var tasks = new List<Task<string>>();
for (int i = 0; i < 10; i++)
{
    tasks.Add(GetDataAsync());
}
string[] results = await Task.WhenAll(tasks);

// Task completion source
var tcs = new TaskCompletionSource<string>();
tcs.SetResult("Result");
string result = await tcs.Task;
```

### Memory Management
```csharp
// Garbage collection
GC.Collect();                               // Force collection
GC.WaitForPendingFinalizers();              // Wait for finalizers
int generation = GC.GetGeneration(obj);     // Get object generation

// Memory allocation
using var handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
IntPtr ptr = handle.AddrOfPinnedObject();

// Weak references
WeakReference weakRef = new WeakReference(obj);
object target = weakRef.Target;
bool isAlive = weakRef.IsAlive;
```

### Serialization
```csharp
// JSON serialization
using System.Text.Json;

var obj = new { Name = "John", Age = 30 };
string json = JsonSerializer.Serialize(obj);
var deserialized = JsonSerializer.Deserialize<MyClass>(json);

// XML serialization
using System.Xml.Serialization;

var serializer = new XmlSerializer(typeof(MyClass));
using var writer = new StringWriter();
serializer.Serialize(writer, obj);
string xml = writer.ToString();
```

This cheat sheet covers the essential C# syntax and features. For more advanced topics, refer to the official Microsoft C# documentation. 