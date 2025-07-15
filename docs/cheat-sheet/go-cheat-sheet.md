# Go Cheat Sheet

## Basic Syntax

```go
// Package declaration
package main

// Import statements
import "fmt"
import (
    "os"
    "strings"
)

// Main function
func main() {
    fmt.Println("Hello, World!")
}
```

## Data Types

```go
// Basic types
var str string = "hello"           // String
var num int = 42                   // Integer
var flt float64 = 3.14            // Float
var bl bool = true                 // Boolean
var byt byte = 65                  // Byte (uint8)
var run rune = 'A'                 // Rune (int32, Unicode)

// Complex types
var arr [5]int                     // Array (fixed size)
var slc []int                      // Slice (dynamic array)
var mp map[string]int              // Map
var ch chan int                    // Channel
var ptr *int                       // Pointer
var ifc interface{}                // Interface
var st struct{ x, y int }          // Struct
```

## Variables and Declarations

```go
// Variable declarations
var name string = "value"          // Explicit type
var name = "value"                 // Type inference
name := "value"                    // Short declaration

// Multiple declarations
var a, b, c int
var d, e = 1, "hello"
f, g := 2, "world"

// Constants
const PI = 3.14159
const (
    StatusOK = 200
    StatusNotFound = 404
)

// Iota (enumerated constants)
const (
    Sunday = iota    // 0
    Monday           // 1
    Tuesday          // 2
)
```

## Operators

```go
// Arithmetic
a + b              // Addition
a - b              // Subtraction
a * b              // Multiplication
a / b              // Division
a % b              // Modulo
a++                // Increment
a--                // Decrement

// Comparison
a == b             // Equal
a != b             // Not equal
a < b              // Less than
a <= b             // Less than or equal
a > b              // Greater than
a >= b             // Greater than or equal

// Logical
a && b             // AND
a || b             // OR
!a                 // NOT

// Bitwise
a & b              // AND
a | b              // OR
a ^ b              // XOR
a << b             // Left shift
a >> b             // Right shift
a &^ b             // AND NOT

// Assignment
a = b              // Assignment
a += b             // Add and assign
a -= b             // Subtract and assign
a *= b             // Multiply and assign
a /= b             // Divide and assign
a %= b             // Modulo and assign
```

## Control Structures

### If/Else
```go
// Basic if
if condition {
    // code
}

// If with initialization
if value := getValue(); value > 0 {
    fmt.Println(value)
}

// If-else
if condition {
    // code
} else {
    // code
}

// If-else if-else
if condition1 {
    // code
} else if condition2 {
    // code
} else {
    // code
}
```

### Switch
```go
// Basic switch
switch value {
case 1:
    fmt.Println("One")
case 2:
    fmt.Println("Two")
default:
    fmt.Println("Other")
}

// Switch with initialization
switch value := getValue(); value {
case 1, 2, 3:
    fmt.Println("Low")
case 4, 5, 6:
    fmt.Println("Medium")
}

// Switch without expression (if-else alternative)
switch {
case x < 0:
    fmt.Println("Negative")
case x > 0:
    fmt.Println("Positive")
default:
    fmt.Println("Zero")
}
```

### Loops
```go
// For loop (only loop type in Go)
for i := 0; i < 10; i++ {
    fmt.Println(i)
}

// While-style loop
for condition {
    // code
}

// Infinite loop
for {
    // code
    if condition {
        break
    }
}

// Range loop
for index, value := range slice {
    fmt.Printf("%d: %v\n", index, value)
}

// Range with map
for key, value := range m {
    fmt.Printf("%s: %v\n", key, value)
}

// Range with channel
for value := range ch {
    fmt.Println(value)
}
```

## Functions

```go
// Basic function
func add(a, b int) int {
    return a + b
}

// Multiple return values
func divide(a, b int) (int, error) {
    if b == 0 {
        return 0, errors.New("division by zero")
    }
    return a / b, nil
}

// Named return values
func getCoordinates() (x, y int) {
    x = 10
    y = 20
    return // naked return
}

// Variadic functions
func sum(nums ...int) int {
    total := 0
    for _, num := range nums {
        total += num
    }
    return total
}

// Function as value
var fn func(int) int = func(x int) int {
    return x * 2
}

// Anonymous function
func() {
    fmt.Println("Anonymous function")
}()
```

## Methods

```go
// Method with value receiver
type Circle struct {
    radius float64
}

func (c Circle) area() float64 {
    return math.Pi * c.radius * c.radius
}

// Method with pointer receiver
func (c *Circle) setRadius(r float64) {
    c.radius = r
}

// Method on non-struct type
type MyInt int

func (m MyInt) double() MyInt {
    return m * 2
}
```

## Data Structures

### Arrays
```go
// Array declaration
var arr [5]int                     // Zero-valued array
arr := [5]int{1, 2, 3, 4, 5}      // Array literal
arr := [...]int{1, 2, 3, 4, 5}    // Size inference

// Array operations
len(arr)                           // Length
arr[0] = 10                       // Assignment
value := arr[2]                   // Access
```

### Slices
```go
// Slice declaration
var slice []int                    // Nil slice
slice := []int{1, 2, 3, 4, 5}     // Slice literal
slice := make([]int, 5)           // Make slice
slice := make([]int, 5, 10)       // Make with capacity

// Slice operations
len(slice)                         // Length
cap(slice)                         // Capacity
slice[1:3]                         // Slicing
slice = append(slice, 6)           // Append
copy(dest, src)                    // Copy slices

// Common slice patterns
slice = slice[:0]                  // Clear slice
slice = slice[:len(slice)-1]       // Remove last element
```

### Maps
```go
// Map declaration
var m map[string]int               // Nil map
m := map[string]int{"a": 1, "b": 2} // Map literal
m := make(map[string]int)          // Make map
m := make(map[string]int, 100)     // Make with initial capacity

// Map operations
m["key"] = value                   // Set value
value := m["key"]                  // Get value
value, exists := m["key"]          // Get with existence check
delete(m, "key")                   // Delete key
len(m)                             // Length

// Iterate map
for key, value := range m {
    fmt.Printf("%s: %v\n", key, value)
}
```

### Structs
```go
// Struct definition
type Person struct {
    Name string
    Age  int
}

// Struct instantiation
person := Person{"John", 30}
person := Person{Name: "John", Age: 30}
person := &Person{"John", 30}      // Pointer

// Struct methods
func (p Person) String() string {
    return fmt.Sprintf("%s (%d)", p.Name, p.Age)
}

// Embedded structs
type Employee struct {
    Person
    Salary int
}
```

### Interfaces
```go
// Interface definition
type Shape interface {
    area() float64
    perimeter() float64
}

// Interface implementation (implicit)
type Circle struct {
    radius float64
}

func (c Circle) area() float64 {
    return math.Pi * c.radius * c.radius
}

func (c Circle) perimeter() float64 {
    return 2 * math.Pi * c.radius
}

// Interface usage
var s Shape = Circle{5}
fmt.Println(s.area())
```

## Channels

```go
// Channel declaration
ch := make(chan int)               // Unbuffered channel
ch := make(chan int, 10)           // Buffered channel

// Channel operations
ch <- value                        // Send
value := <-ch                      // Receive
value, ok := <-ch                  // Receive with check

// Channel patterns
close(ch)                          // Close channel
for value := range ch {            // Range over channel
    fmt.Println(value)
}

// Select statement
select {
case value := <-ch1:
    fmt.Println("Received from ch1:", value)
case ch2 <- value:
    fmt.Println("Sent to ch2")
case <-time.After(time.Second):
    fmt.Println("Timeout")
default:
    fmt.Println("No communication")
}
```

## Error Handling

```go
// Error checking
if err != nil {
    return err
}

// Custom errors
type MyError struct {
    message string
}

func (e MyError) Error() string {
    return e.message
}

// Error wrapping (Go 1.13+)
if err != nil {
    return fmt.Errorf("failed to process: %w", err)
}

// Error unwrapping
var targetErr *MyError
if errors.As(err, &targetErr) {
    // Handle specific error type
}
```

## File I/O Operations

```go
// Read file
data, err := os.ReadFile("file.txt")
if err != nil {
    log.Fatal(err)
}

// Write file
err := os.WriteFile("file.txt", []byte("content"), 0644)
if err != nil {
    log.Fatal(err)
}

// Open file
file, err := os.Open("file.txt")
if err != nil {
    log.Fatal(err)
}
defer file.Close()

// Read line by line
scanner := bufio.NewScanner(file)
for scanner.Scan() {
    fmt.Println(scanner.Text())
}

// Write to file
writer := bufio.NewWriter(file)
writer.WriteString("Hello, World!\n")
writer.Flush()
```

## Common Built-in Functions

```go
// Type conversion
int(3.14)                          // Float to int
string(65)                         // Int to string
[]byte("hello")                    // String to bytes

// Length and capacity
len(slice)                         // Slice/array length
cap(slice)                         // Slice capacity
len(map)                           // Map length
len(string)                        // String length

// Memory allocation
new(Type)                          // Allocate and return pointer
make(Type, size)                   // Allocate slice/map/channel

// Panic and recover
panic("error message")             // Panic
defer func() {                     // Recover from panic
    if r := recover(); r != nil {
        fmt.Println("Recovered:", r)
    }
}()

// Complex numbers
complex(real, imag)                // Create complex number
real(complex)                      // Get real part
imag(complex)                      // Get imaginary part
```

## Key Libraries/Modules

### fmt
```go
// Print functions
fmt.Print("text")                  // Print without newline
fmt.Println("text")                // Print with newline
fmt.Printf("format", args)         // Print with formatting

// Scan functions
fmt.Scan(&variable)                // Scan input
fmt.Scanf("format", &variable)     // Scan with format
fmt.Scanln(&variable)              // Scan line

// String formatting
fmt.Sprintf("format", args)        // Return formatted string
```

### strings
```go
// String operations
strings.Contains(s, substr)        // Check if contains substring
strings.HasPrefix(s, prefix)       // Check prefix
strings.HasSuffix(s, suffix)       // Check suffix
strings.Index(s, substr)           // Find substring index
strings.Replace(s, old, new, n)    // Replace substring
strings.Split(s, sep)              // Split string
strings.Join(slice, sep)           // Join slice
strings.ToUpper(s)                 // Convert to uppercase
strings.ToLower(s)                 // Convert to lowercase
strings.TrimSpace(s)               // Trim whitespace
```

### strconv
```go
// String conversion
strconv.Atoi(s)                    // String to int
strconv.ParseInt(s, base, bits)    // String to int64
strconv.ParseFloat(s, bits)        // String to float64
strconv.ParseBool(s)               // String to bool
strconv.Itoa(i)                    // Int to string
strconv.FormatInt(i, base)         // Int64 to string
strconv.FormatFloat(f, fmt, prec, bits) // Float to string
```

### time
```go
// Time operations
time.Now()                         // Current time
time.Sleep(duration)               // Sleep
time.After(duration)               // Timer channel
time.Tick(duration)                // Ticker channel

// Duration
time.Second                        // Duration constants
time.Minute
time.Hour

// Time formatting
time.Parse(layout, value)          // Parse time string
time.Format(layout)                // Format time
```

### os
```go
// Environment
os.Getenv(key)                     // Get environment variable
os.Setenv(key, value)              // Set environment variable
os.Environ()                       // Get all environment variables

// File operations
os.Create(name)                    // Create file
os.Open(name)                      // Open file
os.Remove(name)                    // Remove file
os.Rename(old, new)                // Rename file
os.Mkdir(name, perm)               // Create directory
os.MkdirAll(path, perm)            // Create directory tree

// Process
os.Exit(code)                      // Exit program
os.Getwd()                         // Get working directory
os.Chdir(dir)                      // Change directory
```

### encoding/json
```go
// JSON marshaling
json.Marshal(data)                 // Marshal to JSON
json.MarshalIndent(data, prefix, indent) // Pretty JSON

// JSON unmarshaling
json.Unmarshal(data, &result)      // Unmarshal from JSON

// JSON streaming
json.NewEncoder(writer).Encode(data) // Encode to writer
json.NewDecoder(reader).Decode(&result) // Decode from reader
```

### net/http
```go
// HTTP server
http.HandleFunc("/", handler)      // Register handler
http.ListenAndServe(":8080", nil)  // Start server

// HTTP client
resp, err := http.Get(url)         // GET request
resp, err := http.Post(url, contentType, body) // POST request

// Request/Response
req, err := http.NewRequest(method, url, body) // Create request
client.Do(req)                     // Execute request
```

### sync
```go
// Mutex
var mu sync.Mutex
mu.Lock()                          // Lock
mu.Unlock()                        // Unlock

// WaitGroup
var wg sync.WaitGroup
wg.Add(1)                          // Add to wait group
wg.Done()                          // Mark as done
wg.Wait()                          // Wait for all

// Once
var once sync.Once
once.Do(func() {                   // Execute once
    // initialization code
})
```

### context
```go
// Context creation
ctx := context.Background()        // Root context
ctx := context.TODO()              // TODO context
ctx, cancel := context.WithCancel(parent) // Cancelable context
ctx, cancel := context.WithTimeout(parent, duration) // Timeout context

// Context usage
select {
case <-ctx.Done():
    return ctx.Err()
case result := <-resultCh:
    return result
}
```

## Goroutines and Concurrency

```go
// Goroutine
go function()                      // Start goroutine
go func() {                        // Anonymous goroutine
    // code
}()

// Channel communication
ch := make(chan int)
go func() {
    ch <- 42                       // Send value
}()
value := <-ch                      // Receive value

// Worker pool pattern
func worker(id int, jobs <-chan int, results chan<- int) {
    for j := range jobs {
        results <- j * 2
    }
}

// Fan-out, fan-in pattern
func fanOut(input <-chan int, workers int) []<-chan int {
    channels := make([]<-chan int, workers)
    for i := 0; i < workers; i++ {
        channels[i] = worker(input)
    }
    return channels
}
```

## Testing

```go
// Basic test
func TestFunction(t *testing.T) {
    result := function()
    if result != expected {
        t.Errorf("Expected %v, got %v", expected, result)
    }
}

// Table-driven tests
func TestFunction(t *testing.T) {
    tests := []struct {
        input    int
        expected int
    }{
        {1, 2},
        {2, 4},
        {3, 6},
    }
    
    for _, test := range tests {
        result := function(test.input)
        if result != test.expected {
            t.Errorf("function(%d) = %d, expected %d", 
                test.input, result, test.expected)
        }
    }
}

// Benchmark
func BenchmarkFunction(b *testing.B) {
    for i := 0; i < b.N; i++ {
        function()
    }
}
```

## Best Practices

```go
// Error handling
if err != nil {
    return fmt.Errorf("context: %w", err)
}

// Defer for cleanup
file, err := os.Open("file.txt")
if err != nil {
    return err
}
defer file.Close()

// Interface satisfaction
var _ Interface = (*Type)(nil)     // Compile-time check

// Context usage
func function(ctx context.Context) error {
    select {
    case <-ctx.Done():
        return ctx.Err()
    case result := <-resultCh:
        return nil
    }
}

// Channel closing
close(ch)                          // Only close from sender
for range ch {                     // Range until closed
    // process
}
``` 