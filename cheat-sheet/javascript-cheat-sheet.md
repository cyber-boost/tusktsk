# JavaScript Cheat Sheet

## Basic Syntax and Data Types

### Primitive Types
```javascript
// Numbers
let integer = 42;                    // Integer
let float = 3.14159;                 // Floating point
let hex = 0xFF;                      // Hexadecimal
let binary = 0b1010;                 // Binary
let octal = 0o755;                   // Octal

// Strings
let single = 'Hello';                // Single quotes
let double = "World";                // Double quotes
let template = `Hello ${name}`;      // Template literal
let multiline = `Line 1
Line 2`;                             // Multiline string

// Booleans
let truthy = true;                   // Boolean true
let falsy = false;                   // Boolean false

// Null and Undefined
let empty = null;                    // Intentional absence
let notDefined = undefined;          // Not assigned

// Symbols
let unique = Symbol('description');  // Unique identifier
```

### Type Checking
```javascript
typeof 42;                           // "number"
typeof "hello";                      // "string"
typeof true;                         // "boolean"
typeof null;                         // "object" (bug)
typeof undefined;                    // "undefined"
typeof Symbol();                     // "symbol"
typeof {};                           // "object"
typeof [];                           // "object"
typeof function(){};                 // "function"

// Better type checking
Array.isArray([]);                   // true
Object.prototype.toString.call([]);  // "[object Array]"
```

## Variables and Operators

### Variable Declaration
```javascript
var oldWay = "function-scoped";      // Function scope (avoid)
let modern = "block-scoped";         // Block scope (preferred)
const constant = "immutable";        // Block scope, immutable

// Destructuring
let [a, b, c] = [1, 2, 3];          // Array destructuring
let {x, y, z} = {x: 1, y: 2, z: 3}; // Object destructuring
let {name: alias} = {name: "John"};  // Rename property
```

### Operators
```javascript
// Arithmetic
let sum = a + b;                     // Addition
let diff = a - b;                    // Subtraction
let product = a * b;                 // Multiplication
let quotient = a / b;                // Division
let remainder = a % b;               // Modulo
let power = a ** b;                  // Exponentiation
let increment = ++a;                 // Pre-increment
let decrement = --a;                 // Pre-decrement

// Assignment
let x = 5;                          // Assignment
x += 3;                             // x = x + 3
x -= 2;                             // x = x - 2
x *= 4;                             // x = x * 4
x /= 2;                             // x = x / 2
x %= 3;                             // x = x % 3

// Comparison
let equal = a == b;                  // Loose equality
let strict = a === b;                // Strict equality
let notEqual = a != b;               // Loose inequality
let strictNot = a !== b;             // Strict inequality
let greater = a > b;                 // Greater than
let less = a < b;                    // Less than
let gte = a >= b;                    // Greater or equal
let lte = a <= b;                    // Less or equal

// Logical
let and = a && b;                    // Logical AND
let or = a || b;                     // Logical OR
let not = !a;                        // Logical NOT
let nullish = a ?? b;                // Nullish coalescing
let optional = obj?.prop;            // Optional chaining
```

## Control Structures

### Conditionals
```javascript
// If/else
if (condition) {
    // code
} else if (otherCondition) {
    // code
} else {
    // code
}

// Ternary operator
let result = condition ? value1 : value2;

// Switch
switch (value) {
    case 1:
        // code
        break;
    case 2:
        // code
        break;
    default:
        // code
}
```

### Loops
```javascript
// For loop
for (let i = 0; i < 10; i++) {
    // code
}

// For...of (iterables)
for (let item of array) {
    // code
}

// For...in (object properties)
for (let key in object) {
    // code
}

// While loop
while (condition) {
    // code
}

// Do...while
do {
    // code
} while (condition);

// Array methods
array.forEach(item => {});           // Iterate
array.map(item => transform);        // Transform
array.filter(item => condition);     // Filter
array.reduce((acc, item) => {}, 0); // Accumulate
array.find(item => condition);       // Find first
array.some(item => condition);       // Any match
array.every(item => condition);      // All match
```

## Functions

### Function Declaration
```javascript
// Function declaration
function name(param1, param2) {
    return result;
}

// Function expression
const func = function(param) {
    return result;
};

// Arrow function
const arrow = (param) => result;
const multiLine = (param) => {
    // multiple lines
    return result;
};

// Default parameters
function greet(name = "World") {
    return `Hello ${name}`;
}

// Rest parameters
function sum(...numbers) {
    return numbers.reduce((a, b) => a + b, 0);
}

// Destructuring parameters
function process({name, age}) {
    return `${name} is ${age}`;
}
```

### Function Context
```javascript
// Call, apply, bind
func.call(context, arg1, arg2);      // Call with context
func.apply(context, [arg1, arg2]);   // Call with array args
const bound = func.bind(context);     // Create bound function

// Arrow functions preserve 'this'
const obj = {
    name: "Object",
    regular: function() {
        return this.name;             // 'this' is obj
    },
    arrow: () => this.name            // 'this' is outer scope
};
```

## Data Structures

### Arrays
```javascript
// Creation
let arr = [1, 2, 3];                 // Array literal
let empty = [];                      // Empty array
let fromConstructor = new Array(3);  // Constructor

// Methods
arr.push(item);                      // Add to end
arr.pop();                           // Remove from end
arr.unshift(item);                   // Add to beginning
arr.shift();                         // Remove from beginning
arr.splice(index, count, ...items);  // Insert/remove
arr.slice(start, end);               // Extract portion
arr.concat(other);                   // Combine arrays
arr.join(separator);                 // Join to string
arr.reverse();                       // Reverse order
arr.sort((a, b) => a - b);           // Sort

// Search
arr.indexOf(item);                   // Find index
arr.includes(item);                  // Check existence
arr.find(item => condition);         // Find first match
arr.findIndex(item => condition);    // Find index of match
```

### Objects
```javascript
// Creation
let obj = {key: "value"};            // Object literal
let empty = {};                      // Empty object
let fromConstructor = new Object();  // Constructor

// Properties
obj.property = "value";              // Dot notation
obj["property"] = "value";           // Bracket notation
delete obj.property;                 // Delete property

// Methods
Object.keys(obj);                    // Get keys array
Object.values(obj);                  // Get values array
Object.entries(obj);                 // Get [key, value] pairs
Object.assign(target, source);       // Copy properties
Object.freeze(obj);                  // Make immutable
Object.seal(obj);                    // Prevent adding/deleting
Object.create(proto);                // Create with prototype

// Spread operator
let copy = {...obj};                 // Shallow copy
let merged = {...obj1, ...obj2};     // Merge objects
```

### Maps and Sets
```javascript
// Map
let map = new Map();                 // Create map
map.set(key, value);                 // Set key-value
map.get(key);                        // Get value
map.has(key);                        // Check existence
map.delete(key);                     // Delete key
map.clear();                         // Clear all
map.size;                            // Get size

// Set
let set = new Set();                 // Create set
set.add(value);                      // Add value
set.has(value);                      // Check existence
set.delete(value);                   // Remove value
set.clear();                         // Clear all
set.size;                            // Get size
```

## Common Built-in Functions

### String Methods
```javascript
let str = "Hello World";

str.length;                          // Get length
str.charAt(index);                   // Get character
str.indexOf(substring);              // Find position
str.includes(substring);             // Check contains
str.startsWith(prefix);              // Check prefix
str.endsWith(suffix);                // Check suffix
str.substring(start, end);           // Extract substring
str.slice(start, end);               // Extract substring
str.split(separator);                // Split to array
str.replace(search, replace);        // Replace text
str.toUpperCase();                   // Convert to upper
str.toLowerCase();                   // Convert to lower
str.trim();                          // Remove whitespace
str.padStart(length, char);          // Pad start
str.padEnd(length, char);            // Pad end
```

### Number Methods
```javascript
let num = 3.14159;

num.toFixed(digits);                 // Format decimal
num.toPrecision(digits);             // Format precision
num.toString(radix);                 // Convert to string
num.valueOf();                       // Get primitive value

// Static methods
Number.parseInt(string, radix);      // Parse integer
Number.parseFloat(string);           // Parse float
Number.isInteger(value);             // Check if integer
Number.isNaN(value);                 // Check if NaN
Number.isFinite(value);              // Check if finite
Number.MAX_SAFE_INTEGER;             // Max safe integer
Number.MIN_SAFE_INTEGER;             // Min safe integer
```

### Math Object
```javascript
Math.abs(x);                         // Absolute value
Math.ceil(x);                        // Round up
Math.floor(x);                       // Round down
Math.round(x);                       // Round nearest
Math.max(...values);                 // Maximum value
Math.min(...values);                 // Minimum value
Math.pow(base, exponent);            // Power
Math.sqrt(x);                        // Square root
Math.random();                       // Random 0-1
Math.sin(x);                         // Sine
Math.cos(x);                         // Cosine
Math.tan(x);                         // Tangent
Math.PI;                             // Pi constant
Math.E;                              // Euler's number
```

### Date Object
```javascript
let date = new Date();               // Current date
let specific = new Date(2024, 0, 1); // Specific date

date.getFullYear();                  // Get year
date.getMonth();                     // Get month (0-11)
date.getDate();                      // Get day
date.getDay();                       // Get weekday (0-6)
date.getHours();                     // Get hours
date.getMinutes();                   // Get minutes
date.getSeconds();                   // Get seconds
date.getMilliseconds();              // Get milliseconds
date.getTime();                      // Get timestamp
date.toISOString();                  // ISO string
date.toLocaleDateString();           // Local date string
date.toLocaleTimeString();           // Local time string
```

## File I/O Operations

### Node.js File System
```javascript
const fs = require('fs');

// Synchronous
let content = fs.readFileSync(path, 'utf8');     // Read file
fs.writeFileSync(path, content);                 // Write file
fs.appendFileSync(path, content);                // Append file
fs.unlinkSync(path);                             // Delete file
fs.mkdirSync(path);                              // Create directory
fs.rmdirSync(path);                              // Remove directory
let stats = fs.statSync(path);                   // Get file stats

// Asynchronous (Promise-based)
fs.promises.readFile(path, 'utf8')               // Read file
    .then(content => {})
    .catch(error => {});

// Asynchronous (Callback-based)
fs.readFile(path, 'utf8', (err, content) => {}); // Read file
fs.writeFile(path, content, err => {});          // Write file
fs.appendFile(path, content, err => {});         // Append file
fs.unlink(path, err => {});                      // Delete file
fs.mkdir(path, err => {});                       // Create directory
fs.rmdir(path, err => {});                       // Remove directory
fs.stat(path, (err, stats) => {});               // Get file stats
```

### Browser File API
```javascript
// File input
let input = document.getElementById('fileInput');
let file = input.files[0];

// FileReader
let reader = new FileReader();
reader.onload = (e) => {
    let content = e.target.result;
};
reader.readAsText(file);                         // Read as text
reader.readAsDataURL(file);                      // Read as data URL
reader.readAsArrayBuffer(file);                  // Read as buffer

// Blob
let blob = new Blob([content], {type: 'text/plain'});
let url = URL.createObjectURL(blob);             // Create object URL
URL.revokeObjectURL(url);                        // Clean up URL
```

## Error Handling

### Try-Catch
```javascript
try {
    // Risky code
    throw new Error("Something went wrong");
} catch (error) {
    // Handle error
    console.error(error.message);
} finally {
    // Always execute
    cleanup();
}

// Async error handling
async function risky() {
    try {
        await asyncOperation();
    } catch (error) {
        console.error(error);
    }
}

// Promise error handling
promise
    .then(result => {})
    .catch(error => {})
    .finally(() => {});
```

### Error Types
```javascript
new Error("message");                // Generic error
new TypeError("type error");         // Type error
new ReferenceError("ref error");     // Reference error
new SyntaxError("syntax error");     // Syntax error
new RangeError("range error");       // Range error
new URIError("URI error");           // URI error
```

## Key Libraries/Modules

### CommonJS (Node.js)
```javascript
// Export
module.exports = value;              // Export single value
module.exports = {key: value};       // Export object
exports.key = value;                 // Export property

// Import
const module = require('./module');  // Import module
const {key} = require('./module');   // Destructure import
```

### ES6 Modules
```javascript
// Export
export default value;                // Default export
export {key1, key2};                 // Named exports
export const key = value;            // Inline export

// Import
import defaultExport from './module'; // Default import
import {key1, key2} from './module';  // Named imports
import * as module from './module';   // Namespace import
import defaultExport, {key} from './module'; // Mixed import
```

### Async/Await
```javascript
// Async function
async function fetchData() {
    try {
        const response = await fetch(url);
        const data = await response.json();
        return data;
    } catch (error) {
        console.error(error);
    }
}

// Promise.all
const results = await Promise.all([
    fetch(url1),
    fetch(url2),
    fetch(url3)
]);

// Promise.race
const fastest = await Promise.race([
    fetch(url1),
    fetch(url2)
]);

// Promise.allSettled
const results = await Promise.allSettled([
    fetch(url1),
    fetch(url2)
]);
```

### Fetch API
```javascript
// GET request
fetch(url)
    .then(response => response.json())
    .then(data => {});

// POST request
fetch(url, {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
})
.then(response => response.json())
.then(data => {});

// With async/await
async function postData(url, data) {
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
    });
    return await response.json();
}
```

### Local Storage
```javascript
// Set items
localStorage.setItem('key', 'value');            // Store string
localStorage.setItem('obj', JSON.stringify(obj)); // Store object

// Get items
let value = localStorage.getItem('key');         // Get string
let obj = JSON.parse(localStorage.getItem('obj')); // Get object

// Remove items
localStorage.removeItem('key');                  // Remove item
localStorage.clear();                            // Clear all

// Check existence
if (localStorage.getItem('key')) {               // Check if exists
    // item exists
}
```

### Session Storage
```javascript
// Same API as localStorage but session-scoped
sessionStorage.setItem('key', 'value');         // Store for session
sessionStorage.getItem('key');                  // Get from session
sessionStorage.removeItem('key');               // Remove from session
sessionStorage.clear();                         // Clear session
```

### JSON
```javascript
JSON.stringify(obj);                             // Object to string
JSON.stringify(obj, null, 2);                    // Pretty print
JSON.parse(string);                              // String to object
JSON.stringify(obj, replacer);                   // Custom replacer
JSON.parse(string, reviver);                     // Custom reviver
```

### Regular Expressions
```javascript
let regex = /pattern/flags;                      // Literal syntax
let regex = new RegExp('pattern', 'flags');      // Constructor

// Flags
/pattern/g;                                       // Global
/pattern/i;                                       // Case insensitive
/pattern/m;                                       // Multiline
/pattern/s;                                       // Dot all
/pattern/u;                                       // Unicode
/pattern/y;                                       // Sticky

// Methods
regex.test(string);                              // Test match
regex.exec(string);                              // Execute match
string.match(regex);                             // Find matches
string.replace(regex, replacement);              // Replace matches
string.search(regex);                            // Find position
string.split(regex);                             // Split by regex
```

### Classes
```javascript
class MyClass {
    constructor(param) {                         // Constructor
        this.property = param;
    }
    
    method() {                                   // Instance method
        return this.property;
    }
    
    static staticMethod() {                      // Static method
        return 'static';
    }
    
    get getter() {                               // Getter
        return this.property;
    }
    
    set setter(value) {                          // Setter
        this.property = value;
    }
}

// Inheritance
class Child extends Parent {
    constructor(param) {
        super(param);                            // Call parent constructor
    }
}
```

### Generators
```javascript
function* generator() {                          // Generator function
    yield 1;                                     // Yield value
    yield 2;
    return 3;
}

let gen = generator();                           // Create generator
gen.next();                                      // {value: 1, done: false}
gen.next();                                      // {value: 2, done: false}
gen.next();                                      // {value: 3, done: true}

// Async generators
async function* asyncGenerator() {
    yield await fetch(url1);
    yield await fetch(url2);
}
```

### Proxy
```javascript
let handler = {
    get(target, prop) {                          // Intercept get
        return target[prop];
    },
    set(target, prop, value) {                   // Intercept set
        target[prop] = value;
        return true;
    }
};

let proxy = new Proxy(target, handler);          // Create proxy
```

### WeakMap/WeakSet
```javascript
let weakMap = new WeakMap();                     // Weak references
weakMap.set(obj, value);                         // Set with object key
weakMap.get(obj);                                // Get by object key
weakMap.has(obj);                                // Check existence
weakMap.delete(obj);                             // Delete by object key

let weakSet = new WeakSet();                     // Weak references
weakSet.add(obj);                                // Add object
weakSet.has(obj);                                // Check existence
weakSet.delete(obj);                             // Remove object
```

### Symbol
```javascript
let sym = Symbol('description');                 // Create symbol
let sym2 = Symbol.for('key');                    // Global symbol
Symbol.keyFor(sym2);                             // Get key for symbol

// Well-known symbols
Symbol.iterator;                                 // Iterator symbol
Symbol.asyncIterator;                            // Async iterator
Symbol.toStringTag;                              // String tag
Symbol.toPrimitive;                              // Primitive conversion
```

### BigInt
```javascript
let big = 123n;                                  // BigInt literal
let big2 = BigInt(123);                          // BigInt constructor
let result = big + big2;                         // BigInt arithmetic
let converted = Number(big);                     // Convert to number
```

### Optional Chaining
```javascript
obj?.prop;                                       // Safe property access
obj?.method();                                   // Safe method call
obj?.['prop'];                                   // Safe bracket access
obj?.prop?.subprop;                              // Chain safely
```

### Nullish Coalescing
```javascript
let value = a ?? b;                              // Use b if a is null/undefined
let result = obj.prop ?? defaultValue;           // Default value
```

### Template Literals
```javascript
let name = "World";
let greeting = `Hello ${name}!`;                 // Variable interpolation
let multiline = `Line 1
Line 2`;                                         // Multiline strings
let tagged = tag`Hello ${name}!`;                // Tagged templates
```

### Destructuring
```javascript
// Array destructuring
let [a, b, ...rest] = [1, 2, 3, 4, 5];          // Rest operator
let [x, , z] = [1, 2, 3];                        // Skip elements

// Object destructuring
let {name, age, ...other} = obj;                 // Rest properties
let {name: alias} = obj;                          // Rename property
let {name = "Default"} = obj;                     // Default value
```

### Spread Operator
```javascript
let arr = [...array1, ...array2];                // Spread arrays
let obj = {...obj1, ...obj2};                    // Spread objects
let args = [...arguments];                        // Spread arguments
```

### Rest Parameters
```javascript
function sum(...numbers) {                        // Collect arguments
    return numbers.reduce((a, b) => a + b, 0);
}
```

### Default Parameters
```javascript
function greet(name = "World") {                 // Default value
    return `Hello ${name}`;
}
```

### Arrow Functions
```javascript
let add = (a, b) => a + b;                       // Expression body
let process = (data) => {                         // Block body
    // multiple statements
    return result;
};
```

### Modules (Dynamic Import)
```javascript
// Dynamic import
const module = await import('./module.js');      // Load module dynamically
const {default: defaultExport} = await import('./module.js');
```

### Promise Methods
```javascript
Promise.resolve(value);                          // Resolve promise
Promise.reject(error);                           // Reject promise
Promise.all(promises);                           // Wait for all
Promise.race(promises);                          // Wait for first
Promise.allSettled(promises);                    // Wait for all (settled)
Promise.any(promises);                           // Wait for first (fulfilled)
```

### Array Methods (Modern)
```javascript
array.flat(depth);                               // Flatten nested arrays
array.flatMap(fn);                               // Map then flatten
array.at(index);                                 // Get element (negative index)
array.toReversed();                              // Create reversed copy
array.toSorted(compare);                         // Create sorted copy
array.toSpliced(start, deleteCount, ...items);   // Create spliced copy
array.with(index, value);                        // Create copy with change
```

### Object Methods (Modern)
```javascript
Object.hasOwn(obj, prop);                        // Check own property
Object.groupBy(items, keySelector);              // Group items
Object.fromEntries(entries);                     // Create from entries
```

### String Methods (Modern)
```javascript
str.at(index);                                   // Get character (negative index)
str.replaceAll(search, replace);                 // Replace all occurrences
```

### Number Methods (Modern)
```javascript
Number.isNaN(value);                             // Check if NaN
Number.isFinite(value);                          // Check if finite
Number.isInteger(value);                         // Check if integer
Number.isSafeInteger(value);                     // Check if safe integer
Number.parseFloat(string);                       // Parse float
Number.parseInt(string, radix);                  // Parse integer
```

### Math Methods (Modern)
```javascript
Math.cbrt(x);                                    // Cube root
Math.clz32(x);                                   // Count leading zeros
Math.imul(x, y);                                 // Integer multiplication
Math.sign(x);                                    // Sign function
Math.trunc(x);                                   // Truncate to integer
Math.fround(x);                                  // Nearest float32
Math.hypot(...values);                           // Hypotenuse
Math.log2(x);                                    // Log base 2
Math.log10(x);                                   // Log base 10
Math.log1p(x);                                   // Log(1 + x)
Math.expm1(x);                                   // exp(x) - 1
Math.cosh(x);                                    // Hyperbolic cosine
Math.sinh(x);                                    // Hyperbolic sine
Math.tanh(x);                                    // Hyperbolic tangent
Math.acosh(x);                                   // Inverse hyperbolic cosine
Math.asinh(x);                                   // Inverse hyperbolic sine
Math.atanh(x);                                   // Inverse hyperbolic tangent
```

### Date Methods (Modern)
```javascript
date.toLocaleDateString(locale, options);       // Localized date
date.toLocaleTimeString(locale, options);       // Localized time
date.toLocaleString(locale, options);            // Localized date/time
```

### Console Methods
```javascript
console.log(message);                            // Log message
console.error(message);                          // Log error
console.warn(message);                           // Log warning
console.info(message);                           // Log info
console.debug(message);                          // Log debug
console.table(data);                             // Log as table
console.group(label);                            // Start group
console.groupEnd();                              // End group
console.time(label);                             // Start timer
console.timeEnd(label);                          // End timer
console.trace();                                 // Stack trace
console.clear();                                 // Clear console
```

### Performance API
```javascript
performance.now();                               // High-res timestamp
performance.mark(name);                          // Create mark
performance.measure(name, start, end);           // Measure duration
performance.getEntriesByType('mark');            // Get marks
performance.getEntriesByType('measure');         // Get measures
```

### Web APIs (Browser)
```javascript
// Intersection Observer
let observer = new IntersectionObserver(callback, options);
observer.observe(element);
observer.unobserve(element);

// Resize Observer
let resizeObserver = new ResizeObserver(entries => {});
resizeObserver.observe(element);

// Mutation Observer
let mutationObserver = new MutationObserver(callback);
mutationObserver.observe(target, config);

// Abort Controller
let controller = new AbortController();
fetch(url, {signal: controller.signal});
controller.abort();
```

### Node.js Specific
```javascript
// Process
process.env.VARIABLE;                            // Environment variable
process.argv;                                    // Command line arguments
process.cwd();                                   // Current working directory
process.exit(code);                              // Exit process
process.on('event', handler);                    // Event listener

// Buffer
Buffer.from(string);                             // Create from string
Buffer.alloc(size);                              // Allocate buffer
Buffer.allocUnsafe(size);                        // Unsafe allocation
buffer.toString(encoding);                       // Convert to string
buffer.write(string, offset, length, encoding);  // Write to buffer

// Stream
const {Readable, Writable, Transform} = require('stream');
const fs = require('fs');

fs.createReadStream(path)                        // Read stream
    .pipe(transform)                             // Transform
    .pipe(fs.createWriteStream(output));         // Write stream
```

### Testing (Jest)
```javascript
// Test structure
describe('Test suite', () => {
    test('should do something', () => {
        expect(value).toBe(expected);
    });
    
    it('should do something else', () => {
        expect(value).toEqual(expected);
    });
});

// Matchers
expect(value).toBe(expected);                    // Strict equality
expect(value).toEqual(expected);                 // Deep equality
expect(value).toMatch(regex);                    // String match
expect(value).toContain(item);                   // Array/string contains
expect(value).toBeInstanceOf(Class);             // Instance check
expect(value).toBeDefined();                     // Defined check
expect(value).toBeUndefined();                   // Undefined check
expect(value).toBeNull();                        // Null check
expect(value).toBeTruthy();                      // Truthy check
expect(value).toBeFalsy();                       // Falsy check
expect(fn).toThrow();                            // Throws error
expect(fn).toHaveBeenCalled();                   // Function called
expect(fn).toHaveBeenCalledWith(args);           // Called with args
```

### Debugging
```javascript
debugger;                                        // Breakpoint
console.trace();                                 // Stack trace
console.dir(obj, {depth: null});                 // Object inspection
console.table(data);                             // Tabular data
console.time('label');                           // Performance timing
console.timeEnd('label');
console.count('label');                          // Count calls
console.countReset('label');
console.group('label');                          // Grouped output
console.groupEnd();
console.assert(condition, message);              // Assertion
```

### Memory Management
```javascript
// Weak references
let weakMap = new WeakMap();                     // Garbage collectible
let weakSet = new WeakSet();                     // Garbage collectible

// Manual cleanup
element.removeEventListener(event, handler);     // Remove listeners
clearInterval(intervalId);                       // Clear intervals
clearTimeout(timeoutId);                         // Clear timeouts
clearImmediate(immediateId);                     // Clear immediates
URL.revokeObjectURL(url);                        // Revoke object URLs
```

### Security
```javascript
// Content Security Policy
// Add to HTML: <meta http-equiv="Content-Security-Policy" content="...">

// XSS Prevention
element.textContent = userInput;                 // Safe text content
element.innerHTML = DOMPurify.sanitize(html);    // Sanitize HTML

// CSRF Protection
fetch(url, {
    headers: {
        'X-CSRF-Token': token                    // CSRF token
    }
});

// Input Validation
if (typeof input !== 'string') {                 // Type checking
    throw new Error('Invalid input');
}

// Output Encoding
const encoded = encodeURIComponent(value);       // URL encoding
const escaped = value.replace(/[&<>"']/g, char => {
    const entities = {'&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'};
    return entities[char];
});
```

### Performance Optimization
```javascript
// Debouncing
function debounce(func, delay) {
    let timeoutId;
    return function(...args) {
        clearTimeout(timeoutId);
        timeoutId = setTimeout(() => func.apply(this, args), delay);
    };
}

// Throttling
function throttle(func, limit) {
    let inThrottle;
    return function(...args) {
        if (!inThrottle) {
            func.apply(this, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

// Memoization
function memoize(func) {
    const cache = new Map();
    return function(...args) {
        const key = JSON.stringify(args);
        if (cache.has(key)) {
            return cache.get(key);
        }
        const result = func.apply(this, args);
        cache.set(key, result);
        return result;
    };
}

// Lazy loading
const lazyModule = () => import('./heavy-module.js');
const loadWhenNeeded = async () => {
    const module = await lazyModule();
    module.doSomething();
};
```

### Best Practices
```javascript
// Use const by default, let when needed
const PI = 3.14159;                              // Constants
let counter = 0;                                 // Variables that change

// Prefer arrow functions for callbacks
array.map(item => item * 2);                     // Arrow function
array.filter(item => item > 0);                  // Arrow function

// Use template literals for strings
const message = `Hello ${name}, you are ${age} years old`; // Template literal

// Use destructuring for cleaner code
const {name, age} = user;                        // Object destructuring
const [first, second] = array;                   // Array destructuring

// Use default parameters
function greet(name = 'World') {                 // Default parameter
    return `Hello ${name}`;
}

// Use rest parameters
function sum(...numbers) {                        // Rest parameters
    return numbers.reduce((a, b) => a + b, 0);
}

// Use spread operator
const combined = [...array1, ...array2];         // Spread arrays
const merged = {...obj1, ...obj2};               // Spread objects

// Use optional chaining
const value = obj?.prop?.subprop;                // Safe property access

// Use nullish coalescing
const result = value ?? defaultValue;            // Null/undefined check

// Use async/await for promises
async function fetchData() {                      // Async function
    try {
        const response = await fetch(url);
        return await response.json();
    } catch (error) {
        console.error(error);
    }
}

// Use modules
import {function} from './module.js';             // ES6 modules
export default class;                            // Default export

// Use classes for OOP
class MyClass {                                  // Class syntax
    constructor(param) {
        this.param = param;
    }
    
    method() {
        return this.param;
    }
}

// Use Map/Set for collections
const map = new Map();                           // Key-value pairs
const set = new Set();                           // Unique values

// Use WeakMap/WeakSet for memory management
const weakMap = new WeakMap();                   // Garbage collectible
const weakSet = new WeakSet();                   // Garbage collectible

// Use Symbol for private properties
const _private = Symbol('private');              // Private property
class MyClass {
    constructor() {
        this[_private] = 'private value';
    }
}

// Use Proxy for metaprogramming
const proxy = new Proxy(target, handler);        // Intercept operations

// Use Reflect for reflection
Reflect.get(obj, prop);                          // Get property
Reflect.set(obj, prop, value);                   // Set property
Reflect.has(obj, prop);                          // Check property
Reflect.deleteProperty(obj, prop);               // Delete property

// Use BigInt for large integers
const big = 123456789012345678901234567890n;     // BigInt literal

// Use Intl for internationalization
const formatter = new Intl.NumberFormat('en-US'); // Number formatting
const dateFormatter = new Intl.DateTimeFormat('en-US'); // Date formatting

// Use AbortController for cancellation
const controller = new AbortController();         // Request cancellation
fetch(url, {signal: controller.signal});
controller.abort();                               // Cancel request
``` 