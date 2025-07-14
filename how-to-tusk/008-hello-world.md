# Hello World in TuskLang

Let's create your first TuskLang program! This guide will walk you through several "Hello World" examples, from simple to advanced.

## The Simplest Hello World

Create a file named `hello.tsk`:

```tusk
# My first TuskLang program
message: "Hello, World!"
@print(message)
```

Run it:
```bash
tusk run hello.tsk
```

Output:
```
Hello, World!
```

## Understanding the Code

Let's break down what happened:

1. `# My first TuskLang program` - A comment for documentation
2. `message: "Hello, World!"` - Static assignment using `:`
3. `@print(message)` - The @ operator calls the print function

## Alternative Approaches

### Direct Print

```tusk
# Direct printing without variable
@print("Hello, World!")
```

### Using String Interpolation

```tusk
# String interpolation
name: "World"
@print("Hello, ${name}!")
```

### Dynamic Assignment

```tusk
# Using = for dynamic assignment
greeting = "Hello"
target = "World"
message = "${greeting}, ${target}!"
@print(message)
```

## Interactive Hello World

### Getting User Input

```tusk
# Interactive greeting
@print("What's your name?")
name: @input()
@print("Hello, ${name}!")
```

### With Default Value

```tusk
# Greeting with fallback
name: @input("Enter your name: ") || "World"
greeting = "Hello, ${name}!"
@print(greeting)
```

## Web-Based Hello World

### Simple HTTP Server

Create `hello-server.tsk`:

```tusk
# Hello World web server
server:
    port: 8080
    host: "localhost"

routes:
    "/":
        response: "Hello, World!"
    
    "/greet":
        response = "Hello, ${@request.query.name || 'World'}!"
    
    "/json":
        response: @json({
            message: "Hello, World!"
            timestamp: @request.timestamp
            platform: @tusk.platform
        })
```

Run the server:
```bash
tusk serve hello-server.tsk
```

Test it:
```bash
# Plain text response
curl http://localhost:8080/

# With query parameter
curl http://localhost:8080/greet?name=TuskLang

# JSON response
curl http://localhost:8080/json
```

## Conditional Hello World

### Time-Based Greeting

```tusk
# Time-aware greeting
hour = @time.hour

greeting = @if(hour < 12, "Good morning", 
           @if(hour < 18, "Good afternoon", "Good evening"))

@print("${greeting}, World!")
```

### Language-Based Greeting

```tusk
# Multilingual greeting
greetings:
    en: "Hello"
    es: "Hola"
    fr: "Bonjour"
    de: "Hallo"
    jp: "こんにちは"

# Get language from environment or default to English
lang: @env.LANG || "en"
greeting: @greetings[lang] || @greetings.en

@print("${greeting}, World!")
```

## Hello World with Functions

### Custom Greeting Function

```tusk
# Define a greeting function
greet = @lambda(name, {
    prefix: @if(@time.hour < 12, "Good morning", "Hello")
    return: "${prefix}, ${name}!"
})

# Use the function
@print(@greet("World"))
@print(@greet("TuskLang"))
```

## Styled Hello World

### With Colors (Terminal)

```tusk
# Colored output
colors:
    red: "\033[31m"
    green: "\033[32m"
    blue: "\033[34m"
    reset: "\033[0m"

@print("${colors.red}Hello${colors.reset}, ${colors.blue}World${colors.reset}!")
```

### With ASCII Art

```tusk
# ASCII art greeting
banner: """
 _   _      _ _        __        __         _     _ _ 
| | | | ___| | | ___   \ \      / /__  _ __| | __| | |
| |_| |/ _ \ | |/ _ \   \ \ /\ / / _ \| '__| |/ _` | |
|  _  |  __/ | | (_) |   \ V  V / (_) | |  | | (_| |_|
|_| |_|\___|_|_|\___/     \_/\_/ \___/|_|  |_|\__,_(_)
"""

@print(banner)
```

## Hello World with Data

### From Configuration

```tusk
# Load from config
config:
    app_name: "TuskLang Demo"
    version: "1.0.0"
    author: "Your Name"

@print("Hello from ${config.app_name} v${config.version}!")
@print("Created by ${config.author}")
```

### From External File

Create `names.tsk`:
```tusk
names: ["Alice", "Bob", "Charlie", "Diana"]
```

Main file:
```tusk
# Import names
@import("names.tsk")

# Greet everyone
@each(names, @lambda(name, {
    @print("Hello, ${name}!")
}))
```

## Hello World API Client

### Fetching External Data

```tusk
# Fetch greeting from API
api_response: @http.get("https://api.example.com/greeting")
greeting: @api_response.data.message || "Hello"

@print("${greeting}, World!")
```

## Hello World with Error Handling

### Graceful Failures

```tusk
# Robust greeting
try_greeting = @try({
    # Attempt to read name from file
    name: @file.read("name.txt")
    return: "Hello, ${name}!"
}, {
    # Fallback if file doesn't exist
    return: "Hello, World!"
})

@print(try_greeting)
```

## Hello World Generator

### Dynamic Content

```tusk
# Generate multiple greetings
languages: ["English", "Spanish", "French", "German", "Japanese"]
greetings: ["Hello", "Hola", "Bonjour", "Hallo", "こんにちは"]

# Create greeting pairs
greeting_pairs = @zip(languages, greetings)

# Generate all greetings
@each(greeting_pairs, @lambda(pair, {
    @print("${pair[1]}, World! (${pair[0]})")
}))
```

## Hello World with Persistence

### Saving State

```tusk
# Track greeting count
count: @cache.get("greeting_count") || 0
count = count + 1
@cache.set("greeting_count", count)

@print("Hello, World! (Greeting #${count})")
```

## Advanced Hello World

### Full Application

Create `hello-app.tsk`:

```tusk
# Complete Hello World application
app:
    name: "Hello World Pro"
    version: "2.0.0"
    debug: @env.DEBUG || false

# Configuration
config:
    default_name: "World"
    greeting_prefix: "Hello"
    
# Custom greeting function
create_greeting = @lambda(name, {
    prefix: @config.greeting_prefix
    suffix: @if(@app.debug, " [DEBUG]", "")
    return: "${prefix}, ${name}!${suffix}"
})

# Main logic
main = {
    # Get name from various sources
    name: @request.query.name || 
          @env.USER_NAME || 
          @cache.get("last_name") || 
          @config.default_name
    
    # Save for next time
    @cache.set("last_name", name)
    
    # Create and return greeting
    greeting: @create_greeting(name)
    
    # Log if in debug mode
    @if(@app.debug, @log("Greeted: ${name}"))
    
    return: greeting
}

# Execute based on context
result = @if(@tusk.is_server, {
    # Server mode - return as HTTP response
    routes:
        "/": @main
        "/health": "OK"
}, {
    # CLI mode - print to console
    @print(@main)
})
```

## Testing Your Hello World

Create `hello-test.tsk`:

```tusk
# Test file for hello world
tests:
    "should greet world by default":
        expected: "Hello, World!"
        actual: @import("hello.tsk").message
    
    "should greet custom name":
        name: "TuskLang"
        expected: "Hello, TuskLang!"
        actual = "Hello, ${name}!"

# Run tests
@test.run(tests)
```

## Debugging Tips

### Add Debug Output

```tusk
# Debug mode
debug: @env.DEBUG || false

@if(debug, @print("[DEBUG] Starting Hello World"))

message: "Hello, World!"

@if(debug, @print("[DEBUG] Message created: ${message}"))

@print(message)

@if(debug, @print("[DEBUG] Program complete"))
```

## Best Practices

1. **Start Simple**: Begin with basic examples and add complexity gradually
2. **Use Comments**: Document your code for future reference
3. **Handle Errors**: Add fallbacks for missing data
4. **Test Thoroughly**: Verify your code works in different scenarios
5. **Keep It Readable**: Use meaningful variable names

## Common Mistakes to Avoid

```tusk
# DON'T: Forget quotes in direct strings
message: Hello, World!  # This might parse incorrectly

# DO: Use quotes for string literals
message: "Hello, World!"

# DON'T: Mix assignment operators incorrectly
static = @dynamic.value  # Wrong operator

# DO: Use = for dynamic assignments
dynamic = @dynamic.value
```

## Next Steps

Now that you've created your first TuskLang programs:

1. Explore [File Structure](009-file-structure.md) to organize larger projects
2. Learn the [CLI Overview](010-cli-overview.md) for more commands
3. Dive into [Basic Syntax](011-comments.md) for language details
4. Master [@ Operators](031-at-operator-intro.md) for dynamic features

## Exercises

Try these modifications to practice:

1. Create a greeting that includes the current date
2. Make a hello world that greets in a random language
3. Build a web service that tracks visitor count
4. Write a hello world that reads names from a database
5. Create an animated hello world in the terminal

Remember: Every expert was once a beginner. Happy coding with TuskLang!