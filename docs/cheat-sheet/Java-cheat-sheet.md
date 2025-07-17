# Java Cheat Sheet

## Basic Syntax

### Class Structure
```java
public class MyClass {
    // Class body
    public static void main(String[] args) {
        // Main method - entry point
    }
}
```

### Comments
```java
// Single line comment
/* Multi-line comment */
/** Javadoc comment */
```

## Data Types

### Primitive Types
```java
byte b = 127;           // 8-bit signed integer (-128 to 127)
short s = 32767;        // 16-bit signed integer (-32,768 to 32,767)
int i = 2147483647;     // 32-bit signed integer
long l = 9223372036854775807L; // 64-bit signed integer
float f = 3.14f;        // 32-bit floating point
double d = 3.14;        // 64-bit floating point
boolean b = true;       // true or false
char c = 'A';           // 16-bit Unicode character
```

### Reference Types
```java
String str = "Hello";   // String object
Integer num = 42;       // Wrapper class
Object obj = new Object(); // Any object
```

## Variables

### Declaration
```java
int x = 10;             // Variable declaration with initialization
final int CONSTANT = 100; // Constant (cannot be changed)
String name;            // Declaration without initialization
name = "John";          // Assignment
```

### Variable Scope
```java
public class Scope {
    static int classVar = 1;    // Class variable (static)
    int instanceVar = 2;        // Instance variable
    
    void method() {
        int localVar = 3;       // Local variable
        {
            int blockVar = 4;   // Block-scoped variable
        }
    }
}
```

## Operators

### Arithmetic
```java
int a = 10, b = 3;
int sum = a + b;        // Addition: 13
int diff = a - b;       // Subtraction: 7
int product = a * b;    // Multiplication: 30
int quotient = a / b;   // Division: 3
int remainder = a % b;  // Modulus: 1
int increment = ++a;    // Pre-increment: 11
int decrement = --a;    // Pre-decrement: 9
```

### Assignment
```java
int x = 5;              // Simple assignment
x += 3;                 // Addition assignment: x = x + 3
x -= 2;                 // Subtraction assignment
x *= 4;                 // Multiplication assignment
x /= 2;                 // Division assignment
x %= 3;                 // Modulus assignment
```

### Comparison
```java
boolean eq = a == b;    // Equal to
boolean ne = a != b;    // Not equal to
boolean gt = a > b;     // Greater than
boolean lt = a < b;     // Less than
boolean ge = a >= b;    // Greater than or equal
boolean le = a <= b;    // Less than or equal
```

### Logical
```java
boolean and = a > 0 && b > 0;  // Logical AND
boolean or = a > 0 || b > 0;   // Logical OR
boolean not = !(a > 0);        // Logical NOT
```

### Bitwise
```java
int bitAnd = a & b;     // Bitwise AND
int bitOr = a | b;      // Bitwise OR
int bitXor = a ^ b;     // Bitwise XOR
int bitNot = ~a;        // Bitwise NOT
int leftShift = a << 2; // Left shift
int rightShift = a >> 2; // Right shift
int unsignedRightShift = a >>> 2; // Unsigned right shift
```

## Control Structures

### If-Else
```java
if (condition) {
    // Code block
} else if (anotherCondition) {
    // Code block
} else {
    // Code block
}

// Ternary operator
String result = (a > b) ? "a is greater" : "b is greater";
```

### Switch
```java
switch (value) {
    case 1:
        // Code for value == 1
        break;
    case 2:
        // Code for value == 2
        break;
    default:
        // Default code
        break;
}

// Switch expression (Java 14+)
String result = switch (value) {
    case 1 -> "One";
    case 2 -> "Two";
    default -> "Unknown";
};
```

### Loops

#### For Loop
```java
for (int i = 0; i < 10; i++) {
    // Code block
}

// Enhanced for loop (for-each)
for (String item : array) {
    // Code block
}
```

#### While Loop
```java
while (condition) {
    // Code block
}

do {
    // Code block
} while (condition);
```

#### Loop Control
```java
for (int i = 0; i < 10; i++) {
    if (i == 5) {
        continue;        // Skip to next iteration
    }
    if (i == 8) {
        break;           // Exit loop
    }
}
```

## Functions/Methods

### Method Declaration
```java
public static int add(int a, int b) {
    return a + b;
}

// Method overloading
public static int add(int a, int b, int c) {
    return a + b + c;
}

// Varargs (variable arguments)
public static int sum(int... numbers) {
    int total = 0;
    for (int num : numbers) {
        total += num;
    }
    return total;
}
```

### Method Types
```java
public class MyClass {
    // Instance method
    public void instanceMethod() {
        // Can access instance variables
    }
    
    // Static method
    public static void staticMethod() {
        // Cannot access instance variables
    }
    
    // Private method
    private void privateMethod() {
        // Only accessible within class
    }
    
    // Protected method
    protected void protectedMethod() {
        // Accessible in same package and subclasses
    }
}
```

## Data Structures

### Arrays
```java
// Array declaration
int[] numbers = new int[5];
int[] numbers2 = {1, 2, 3, 4, 5};

// Multi-dimensional array
int[][] matrix = new int[3][3];
int[][] matrix2 = {{1,2,3}, {4,5,6}, {7,8,9}};

// Array operations
int length = numbers.length;    // Get array length
numbers[0] = 10;               // Set element
int value = numbers[0];        // Get element
```

### Collections Framework

#### List
```java
import java.util.*;

List<String> list = new ArrayList<>();
list.add("item");              // Add element
list.get(0);                   // Get element
list.set(0, "newItem");        // Set element
list.remove(0);                // Remove element
list.size();                   // Get size
list.isEmpty();                // Check if empty
list.contains("item");         // Check if contains
list.clear();                  // Clear all elements

// LinkedList
List<String> linkedList = new LinkedList<>();

// Vector (thread-safe)
List<String> vector = new Vector<>();
```

#### Set
```java
Set<String> set = new HashSet<>();
set.add("item");               // Add element
set.remove("item");            // Remove element
set.contains("item");          // Check if contains
set.size();                    // Get size

// TreeSet (sorted)
Set<String> treeSet = new TreeSet<>();

// LinkedHashSet (maintains insertion order)
Set<String> linkedHashSet = new LinkedHashSet<>();
```

#### Map
```java
Map<String, Integer> map = new HashMap<>();
map.put("key", 100);           // Add key-value pair
map.get("key");                // Get value
map.remove("key");             // Remove key-value pair
map.containsKey("key");        // Check if contains key
map.containsValue(100);        // Check if contains value
map.size();                    // Get size
map.keySet();                  // Get all keys
map.values();                  // Get all values
map.entrySet();                // Get all entries

// TreeMap (sorted by keys)
Map<String, Integer> treeMap = new TreeMap<>();

// LinkedHashMap (maintains insertion order)
Map<String, Integer> linkedHashMap = new LinkedHashMap<>();
```

#### Queue
```java
Queue<String> queue = new LinkedList<>();
queue.offer("item");           // Add element
queue.poll();                  // Remove and return head
queue.peek();                  // Return head without removing
queue.size();                  // Get size

// PriorityQueue (sorted)
Queue<String> priorityQueue = new PriorityQueue<>();
```

### Stack
```java
Stack<String> stack = new Stack<>();
stack.push("item");            // Push element
stack.pop();                   // Pop element
stack.peek();                  // Peek at top element
stack.isEmpty();               // Check if empty
stack.size();                  // Get size
```

## Common Built-in Functions

### String Operations
```java
String str = "Hello World";
str.length();                  // Get length: 11
str.charAt(0);                 // Get character: 'H'
str.substring(0, 5);           // Get substring: "Hello"
str.indexOf("World");          // Find index: 6
str.contains("Hello");         // Check if contains: true
str.startsWith("Hello");       // Check prefix: true
str.endsWith("World");         // Check suffix: true
str.toUpperCase();             // Convert to uppercase
str.toLowerCase();             // Convert to lowercase
str.trim();                    // Remove whitespace
str.split(" ");                // Split string: ["Hello", "World"]
str.replace("World", "Java");  // Replace substring
str.equals("Hello World");     // Compare strings
str.equalsIgnoreCase("hello world"); // Case-insensitive comparison
```

### Math Operations
```java
import java.lang.Math;

Math.abs(-5);                  // Absolute value: 5
Math.max(10, 20);             // Maximum: 20
Math.min(10, 20);             // Minimum: 10
Math.sqrt(16);                // Square root: 4.0
Math.pow(2, 3);               // Power: 8.0
Math.round(3.6);              // Round: 4
Math.ceil(3.2);               // Ceiling: 4.0
Math.floor(3.8);              // Floor: 3.0
Math.random();                // Random number (0.0 to 1.0)
Math.PI;                      // Pi constant
Math.E;                       // Euler's number
```

### Date and Time (Java 8+)
```java
import java.time.*;

LocalDate date = LocalDate.now();           // Current date
LocalTime time = LocalTime.now();           // Current time
LocalDateTime datetime = LocalDateTime.now(); // Current date and time
Instant instant = Instant.now();            // Current instant

// Date operations
LocalDate tomorrow = date.plusDays(1);
LocalDate yesterday = date.minusDays(1);
int year = date.getYear();
int month = date.getMonthValue();
int day = date.getDayOfMonth();

// Time formatting
DateTimeFormatter formatter = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss");
String formatted = datetime.format(formatter);
```

## File I/O Operations

### File Operations
```java
import java.io.*;

File file = new File("example.txt");
file.exists();                 // Check if file exists
file.createNewFile();          // Create new file
file.delete();                 // Delete file
file.length();                 // Get file size
file.isFile();                 // Check if it's a file
file.isDirectory();            // Check if it's a directory
file.listFiles();              // List files in directory
```

### Reading Files
```java
// Read text file
try (BufferedReader reader = new BufferedReader(new FileReader("file.txt"))) {
    String line;
    while ((line = reader.readLine()) != null) {
        System.out.println(line);
    }
}

// Read all lines (Java 8+)
List<String> lines = Files.readAllLines(Paths.get("file.txt"));

// Read bytes
byte[] bytes = Files.readAllBytes(Paths.get("file.txt"));
```

### Writing Files
```java
// Write text file
try (BufferedWriter writer = new BufferedWriter(new FileWriter("file.txt"))) {
    writer.write("Hello World");
    writer.newLine();
    writer.write("Second line");
}

// Write all lines (Java 8+)
List<String> lines = Arrays.asList("Line 1", "Line 2", "Line 3");
Files.write(Paths.get("file.txt"), lines);

// Write bytes
byte[] bytes = "Hello World".getBytes();
Files.write(Paths.get("file.txt"), bytes);
```

### Serialization
```java
import java.io.*;

// Serialize object
try (ObjectOutputStream oos = new ObjectOutputStream(new FileOutputStream("object.ser"))) {
    oos.writeObject(myObject);
}

// Deserialize object
try (ObjectInputStream ois = new ObjectInputStream(new FileInputStream("object.ser"))) {
    MyClass obj = (MyClass) ois.readObject();
}
```

## Error Handling

### Try-Catch
```java
try {
    // Code that might throw exception
    int result = 10 / 0;
} catch (ArithmeticException e) {
    // Handle specific exception
    System.err.println("Division by zero: " + e.getMessage());
} catch (Exception e) {
    // Handle any other exception
    System.err.println("General error: " + e.getMessage());
} finally {
    // Always executed
    System.out.println("Cleanup code");
}
```

### Try-With-Resources (Java 7+)
```java
try (FileInputStream fis = new FileInputStream("file.txt")) {
    // Use resource
    int data = fis.read();
} catch (IOException e) {
    // Handle exception
    System.err.println("IO Error: " + e.getMessage());
}
```

### Custom Exceptions
```java
public class CustomException extends Exception {
    public CustomException(String message) {
        super(message);
    }
    
    public CustomException(String message, Throwable cause) {
        super(message, cause);
    }
}

// Throwing exceptions
public void method() throws CustomException {
    if (condition) {
        throw new CustomException("Error message");
    }
}
```

## Key Libraries/Modules

### Collections
```java
import java.util.*;
// ArrayList, LinkedList, HashSet, TreeSet, HashMap, TreeMap, etc.
```

### I/O
```java
import java.io.*;
// File, FileReader, FileWriter, BufferedReader, BufferedWriter, etc.
```

### Networking
```java
import java.net.*;
// URL, URLConnection, Socket, ServerSocket, etc.
```

### Concurrency
```java
import java.util.concurrent.*;
// ExecutorService, Future, CompletableFuture, etc.
```

### Reflection
```java
import java.lang.reflect.*;
// Class, Method, Field, Constructor, etc.
```

### Regular Expressions
```java
import java.util.regex.*;
// Pattern, Matcher
Pattern pattern = Pattern.compile("\\d+");
Matcher matcher = pattern.matcher("123");
boolean matches = matcher.matches();
```

### Streams (Java 8+)
```java
import java.util.stream.*;

List<Integer> numbers = Arrays.asList(1, 2, 3, 4, 5);
List<Integer> doubled = numbers.stream()
    .map(n -> n * 2)
    .filter(n -> n > 5)
    .collect(Collectors.toList());

// Parallel streams
List<Integer> parallelResult = numbers.parallelStream()
    .map(n -> n * 2)
    .collect(Collectors.toList());
```

### Optional (Java 8+)
```java
import java.util.Optional;

Optional<String> optional = Optional.of("value");
String value = optional.orElse("default");
String value2 = optional.orElseGet(() -> "computed default");
optional.ifPresent(System.out::println);

// Chaining
Optional<String> result = Optional.of("hello")
    .map(String::toUpperCase)
    .filter(s -> s.length() > 3);
```

## Object-Oriented Programming

### Classes and Objects
```java
public class Person {
    // Instance variables
    private String name;
    private int age;
    
    // Constructor
    public Person(String name, int age) {
        this.name = name;
        this.age = age;
    }
    
    // Getter and setter methods
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    public int getAge() { return age; }
    public void setAge(int age) { this.age = age; }
    
    // Method
    public void introduce() {
        System.out.println("Hi, I'm " + name + " and I'm " + age + " years old.");
    }
}
```

### Inheritance
```java
public class Student extends Person {
    private String major;
    
    public Student(String name, int age, String major) {
        super(name, age);  // Call parent constructor
        this.major = major;
    }
    
    @Override
    public void introduce() {
        super.introduce();  // Call parent method
        System.out.println("I'm studying " + major);
    }
}
```

### Interfaces
```java
public interface Drawable {
    void draw();
    default void erase() {
        System.out.println("Erasing...");
    }
}

public class Circle implements Drawable {
    @Override
    public void draw() {
        System.out.println("Drawing circle");
    }
}
```

### Abstract Classes
```java
public abstract class Shape {
    protected String color;
    
    public abstract double getArea();
    
    public void setColor(String color) {
        this.color = color;
    }
}

public class Rectangle extends Shape {
    private double width, height;
    
    @Override
    public double getArea() {
        return width * height;
    }
}
```

### Enums
```java
public enum DayOfWeek {
    MONDAY, TUESDAY, WEDNESDAY, THURSDAY, FRIDAY, SATURDAY, SUNDAY;
    
    public boolean isWeekend() {
        return this == SATURDAY || this == SUNDAY;
    }
}

// Using enum
DayOfWeek today = DayOfWeek.MONDAY;
if (today.isWeekend()) {
    System.out.println("It's weekend!");
}
```

## Generics

### Generic Classes
```java
public class Box<T> {
    private T content;
    
    public void set(T content) {
        this.content = content;
    }
    
    public T get() {
        return content;
    }
}

// Using generic class
Box<String> stringBox = new Box<>();
stringBox.set("Hello");
String value = stringBox.get();
```

### Generic Methods
```java
public class Utils {
    public static <T> void swap(T[] array, int i, int j) {
        T temp = array[i];
        array[i] = array[j];
        array[j] = temp;
    }
    
    public static <T extends Comparable<T>> T max(T a, T b) {
        return a.compareTo(b) > 0 ? a : b;
    }
}
```

### Wildcards
```java
public void processList(List<?> list) {
    // Can read from list but not write
    for (Object item : list) {
        System.out.println(item);
    }
}

public void addNumbers(List<? super Integer> list) {
    // Can add Integer or its subtypes
    list.add(42);
}

public void getNumbers(List<? extends Number> list) {
    // Can read Number or its subtypes
    for (Number num : list) {
        System.out.println(num);
    }
}
```

## Annotations

### Built-in Annotations
```java
@Override
public String toString() {
    return "Custom toString";
}

@Deprecated
public void oldMethod() {
    // This method is deprecated
}

@SuppressWarnings("unchecked")
public void method() {
    // Suppress unchecked warnings
}

@FunctionalInterface
public interface MyFunctionalInterface {
    void doSomething();
}
```

### Custom Annotations
```java
import java.lang.annotation.*;

@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.METHOD)
public @interface Test {
    String value() default "";
    int timeout() default 1000;
}

// Using custom annotation
@Test(value = "myTest", timeout = 5000)
public void testMethod() {
    // Test implementation
}
```

## Lambda Expressions (Java 8+)

### Basic Lambda
```java
// Before Java 8
Runnable runnable = new Runnable() {
    @Override
    public void run() {
        System.out.println("Hello");
    }
};

// With lambda
Runnable runnable = () -> System.out.println("Hello");

// Lambda with parameters
List<String> names = Arrays.asList("Alice", "Bob", "Charlie");
names.forEach(name -> System.out.println("Hello " + name));

// Lambda with multiple statements
names.forEach(name -> {
    String greeting = "Hello " + name;
    System.out.println(greeting);
});
```

### Method References
```java
List<String> names = Arrays.asList("Alice", "Bob", "Charlie");

// Static method reference
names.forEach(System.out::println);

// Instance method reference
names.forEach(String::toUpperCase);

// Constructor reference
Supplier<List<String>> listSupplier = ArrayList::new;
```

## Common Patterns

### Singleton
```java
public class Singleton {
    private static Singleton instance;
    
    private Singleton() {}
    
    public static Singleton getInstance() {
        if (instance == null) {
            instance = new Singleton();
        }
        return instance;
    }
}
```

### Builder Pattern
```java
public class PersonBuilder {
    private String name;
    private int age;
    private String address;
    
    public PersonBuilder name(String name) {
        this.name = name;
        return this;
    }
    
    public PersonBuilder age(int age) {
        this.age = age;
        return this;
    }
    
    public PersonBuilder address(String address) {
        this.address = address;
        return this;
    }
    
    public Person build() {
        return new Person(name, age, address);
    }
}

// Usage
Person person = new PersonBuilder()
    .name("John")
    .age(30)
    .address("123 Main St")
    .build();
```

### Factory Pattern
```java
public interface Animal {
    void makeSound();
}

public class Dog implements Animal {
    @Override
    public void makeSound() {
        System.out.println("Woof!");
    }
}

public class Cat implements Animal {
    @Override
    public void makeSound() {
        System.out.println("Meow!");
    }
}

public class AnimalFactory {
    public static Animal createAnimal(String type) {
        switch (type.toLowerCase()) {
            case "dog": return new Dog();
            case "cat": return new Cat();
            default: throw new IllegalArgumentException("Unknown animal type");
        }
    }
}
```

## Performance Tips

### String Concatenation
```java
// Inefficient (creates many String objects)
String result = "";
for (int i = 0; i < 1000; i++) {
    result += i;
}

// Efficient (uses StringBuilder)
StringBuilder sb = new StringBuilder();
for (int i = 0; i < 1000; i++) {
    sb.append(i);
}
String result = sb.toString();
```

### Collection Initialization
```java
// Specify initial capacity for better performance
List<String> list = new ArrayList<>(1000);
Map<String, Integer> map = new HashMap<>(1000);
```

### Boxing/Unboxing
```java
// Avoid unnecessary boxing/unboxing
Integer sum = 0;
for (int i = 0; i < 1000; i++) {
    sum += i;  // Creates new Integer objects
}

// Better approach
int sum = 0;
for (int i = 0; i < 1000; i++) {
    sum += i;  // Uses primitive int
}
```

## Memory Management

### Garbage Collection
```java
// Request garbage collection (not guaranteed)
System.gc();

// Get memory information
Runtime runtime = Runtime.getRuntime();
long totalMemory = runtime.totalMemory();
long freeMemory = runtime.freeMemory();
long usedMemory = totalMemory - freeMemory;
```

### Resource Management
```java
// Always close resources
try (FileInputStream fis = new FileInputStream("file.txt");
     BufferedInputStream bis = new BufferedInputStream(fis)) {
    // Use resources
} catch (IOException e) {
    // Handle exception
}
```

## Debugging

### Logging
```java
import java.util.logging.*;

Logger logger = Logger.getLogger(MyClass.class.getName());
logger.info("Info message");
logger.warning("Warning message");
logger.severe("Error message");

// With parameters
logger.info("Processing user: " + userId);
```

### Assertions
```java
// Enable assertions: java -ea MyClass
assert condition : "Error message";

// Example
public int divide(int a, int b) {
    assert b != 0 : "Division by zero";
    return a / b;
}
```

### Stack Trace
```java
try {
    // Code that might throw exception
} catch (Exception e) {
    e.printStackTrace();  // Print stack trace
    System.err.println("Error: " + e.getMessage());
}
```

## Best Practices

### Naming Conventions
```java
// Classes: PascalCase
public class MyClass {}

// Methods and variables: camelCase
public void myMethod() {
    int myVariable = 10;
}

// Constants: UPPER_SNAKE_CASE
public static final int MAX_SIZE = 100;

// Packages: lowercase
package com.example.myapp;
```

### Code Organization
```java
public class MyClass {
    // 1. Static variables
    private static final int CONSTANT = 100;
    
    // 2. Instance variables
    private String name;
    private int age;
    
    // 3. Constructors
    public MyClass() {}
    public MyClass(String name) {
        this.name = name;
    }
    
    // 4. Public methods
    public void publicMethod() {}
    
    // 5. Private methods
    private void privateMethod() {}
}
```

### Immutability
```java
public final class ImmutablePerson {
    private final String name;
    private final int age;
    
    public ImmutablePerson(String name, int age) {
        this.name = name;
        this.age = age;
    }
    
    public String getName() { return name; }
    public int getAge() { return age; }
    
    // No setters - object cannot be modified after creation
}
```

This cheat sheet covers the essential Java concepts and syntax. Remember to refer to the official Java documentation for more detailed information and best practices. 