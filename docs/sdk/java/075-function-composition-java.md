# Function Composition (Java)

Function composition is a powerful paradigm in TuskLang that allows you to build complex operations by combining simple functions, promoting code reuse and clarity in Java applications with Spring Boot integration.

## Basic Composition

```tusk
# Simple function composition
add_one: (x) => x + 1
double: (x) => x * 2
square: (x) => x ** 2

# Manual composition
result: square(double(add_one(5)))  # ((5 + 1) * 2)² = 144

# Compose function
compose: (...fns) => {
    return (x) => fns.reduceRight((acc, fn) => fn(acc), x)
}

# Using compose (right to left)
calculate: compose(square, double, add_one)
result: calculate(5)  # 144

# Pipe function (left to right)
pipe: (...fns) => {
    return (x) => fns.reduce((acc, fn) => fn(acc), x)
}

# Using pipe
process: pipe(add_one, double, square)
result: process(5)  # 144
```

## Java Implementation

```java
import java.util.function.Function;
import java.util.List;
import java.util.Arrays;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class FunctionComposition {
    
    // Basic functions
    public static Function<Integer, Integer> addOne = x -> x + 1;
    public static Function<Integer, Integer> doubleValue = x -> x * 2;
    public static Function<Integer, Integer> square = x -> x * x;
    
    // Manual composition
    public static int manualComposition(int x) {
        return square.apply(doubleValue.apply(addOne.apply(x)));
    }
    
    // Compose function (right to left)
    public static <T> Function<T, T> compose(List<Function<T, T>> functions) {
        return functions.stream()
            .reduce(Function.identity(), Function::compose);
    }
    
    // Pipe function (left to right)
    public static <T> Function<T, T> pipe(List<Function<T, T>> functions) {
        return functions.stream()
            .reduce(Function.identity(), (f, g) -> f.andThen(g));
    }
    
    // Using compose
    public static Function<Integer, Integer> calculate = compose(Arrays.asList(
        square, doubleValue, addOne
    ));
    
    // Using pipe
    public static Function<Integer, Integer> process = pipe(Arrays.asList(
        addOne, doubleValue, square
    ));
    
    public static void main(String[] args) {
        int result1 = manualComposition(5);  // 144
        int result2 = calculate.apply(5);    // 144
        int result3 = process.apply(5);      // 144
        
        System.out.println("Manual composition: " + result1);
        System.out.println("Compose: " + result2);
        System.out.println("Pipe: " + result3);
    }
}
```

## Practical Examples

```tusk
# String processing pipeline
trim: (s) => s.trim()
lowercase: (s) => s.toLowerCase()
replace_spaces: (s) => s.replace(/\s+/g, '-')
remove_special: (s) => s.replace(/[^a-z0-9-]/g, '')

slugify: pipe(
    trim,
    lowercase,
    replace_spaces,
    remove_special
)

slug: slugify("  Hello World!  ")  # "hello-world"

# Data transformation
parse_int: (x) => parseInt(x)
validate_positive: (x) => x > 0 ? x : 0
add_tax: (x) => x * 1.08
format_currency: (x) => "$" + x.toFixed(2)

process_price: pipe(
    parse_int,
    validate_positive,
    add_tax,
    format_currency
)

price: process_price("100")  # "$108.00"
```

## Java Practical Examples

```java
import java.util.function.Function;
import java.util.List;
import java.util.Arrays;
import java.util.regex.Pattern;

public class PracticalComposition {
    
    // String processing functions
    public static Function<String, String> trim = String::trim;
    public static Function<String, String> lowercase = String::toLowerCase;
    public static Function<String, String> replaceSpaces = s -> s.replaceAll("\\s+", "-");
    public static Function<String, String> removeSpecial = s -> s.replaceAll("[^a-z0-9-]", "");
    
    // Slugify pipeline
    public static Function<String, String> slugify = pipe(Arrays.asList(
        trim,
        lowercase,
        replaceSpaces,
        removeSpecial
    ));
    
    // Data transformation functions
    public static Function<String, Integer> parseInteger = Integer::parseInt;
    public static Function<Integer, Integer> validatePositive = x -> x > 0 ? x : 0;
    public static Function<Integer, Double> addTax = x -> x * 1.08;
    public static Function<Double, String> formatCurrency = x -> String.format("$%.2f", x);
    
    // Price processing pipeline
    public static Function<String, String> processPrice = pipe(Arrays.asList(
        parseInteger,
        validatePositive,
        addTax,
        formatCurrency
    ));
    
    public static void main(String[] args) {
        // String processing
        String slug = slugify.apply("  Hello World!  ");
        System.out.println("Slug: " + slug);  // "hello-world"
        
        // Price processing
        String price = processPrice.apply("100");
        System.out.println("Price: " + price);  // "$108.00"
    }
    
    private static <T> Function<T, T> pipe(List<Function<T, T>> functions) {
        return functions.stream()
            .reduce(Function.identity(), (f, g) -> f.andThen(g));
    }
}
```

## Partial Application

```tusk
# Partial application for composition
multiply: (a) => (b) => a * b
add: (a) => (b) => a + b
divide: (a) => (b) => b / a

# Create specialized functions
double: multiply(2)
triple: multiply(3)
add_ten: add(10)
half: divide(2)

# Compose with partial functions
calculate: pipe(
    add_ten,      # x + 10
    double,       # (x + 10) * 2
    half          # ((x + 10) * 2) / 2
)

result: calculate(5)  # 15

# More complex example
filter_by: (predicate) => (array) => array.filter(predicate)
map_by: (fn) => (array) => array.map(fn)
reduce_by: (fn, initial) => (array) => array.reduce(fn, initial)

# Create pipeline
process_numbers: pipe(
    filter_by(x => x > 0),              # Keep positive
    map_by(x => x * 2),                 # Double them
    reduce_by((sum, x) => sum + x, 0)   # Sum them
)

total: process_numbers([1, -2, 3, -4, 5])  # 18
```

## Java Partial Application

```java
import java.util.function.BiFunction;
import java.util.function.Function;
import java.util.List;
import java.util.stream.Collectors;

public class PartialApplication {
    
    // Curried functions
    public static <A, B, C> Function<A, Function<B, C>> curry(BiFunction<A, B, C> f) {
        return a -> b -> f.apply(a, b);
    }
    
    // Partial application functions
    public static Function<Integer, Integer> multiply(int a) {
        return b -> a * b;
    }
    
    public static Function<Integer, Integer> add(int a) {
        return b -> a + b;
    }
    
    public static Function<Integer, Integer> divide(int a) {
        return b -> b / a;
    }
    
    // Specialized functions
    public static Function<Integer, Integer> doubleValue = multiply(2);
    public static Function<Integer, Integer> tripleValue = multiply(3);
    public static Function<Integer, Integer> addTen = add(10);
    public static Function<Integer, Integer> half = divide(2);
    
    // Compose with partial functions
    public static Function<Integer, Integer> calculate = pipe(Arrays.asList(
        addTen,      // x + 10
        doubleValue, // (x + 10) * 2
        half         // ((x + 10) * 2) / 2
    ));
    
    // Stream processing functions
    public static <T> Function<List<T>, List<T>> filterBy(Function<T, Boolean> predicate) {
        return list -> list.stream()
            .filter(predicate::apply)
            .collect(Collectors.toList());
    }
    
    public static <T, R> Function<List<T>, List<R>> mapBy(Function<T, R> fn) {
        return list -> list.stream()
            .map(fn)
            .collect(Collectors.toList());
    }
    
    public static <T> Function<List<T>, T> reduceBy(BiFunction<T, T, T> fn, T initial) {
        return list -> list.stream()
            .reduce(initial, fn);
    }
    
    // Process numbers pipeline
    public static Function<List<Integer>, Integer> processNumbers = pipe(Arrays.asList(
        filterBy(x -> x > 0),                    // Keep positive
        mapBy(x -> x * 2),                       // Double them
        reduceBy((sum, x) -> sum + x, 0)         // Sum them
    ));
    
    public static void main(String[] args) {
        // Test calculate function
        int result = calculate.apply(5);
        System.out.println("Calculate result: " + result);  // 15
        
        // Test number processing
        List<Integer> numbers = Arrays.asList(1, -2, 3, -4, 5);
        int total = processNumbers.apply(numbers);
        System.out.println("Processed total: " + total);  // 18
    }
    
    private static <T> Function<T, T> pipe(List<Function<T, T>> functions) {
        return functions.stream()
            .reduce(Function.identity(), (f, g) -> f.andThen(g));
    }
}
```

## Async Composition

```tusk
# Async pipe
pipe_async: (...fns) => {
    return async (x) => {
        result: x
        for (fn of fns) {
            result: await fn(result)
        }
        return result
    }
}

# Async functions
fetch_user: async (id) => {
    response: await fetch(`/api/users/${id}`)
    return response.json()
}

enrich_user: async (user) => {
    posts: await fetch(`/api/users/${user.id}/posts`).then(r => r.json())
    return {...user, posts}
}

format_user: async (user) => {
    return {
        name: user.name,
        email: user.email,
        post_count: user.posts.length
    }
}

# Compose async operations
get_user_summary: pipe_async(
    fetch_user,
    enrich_user,
    format_user
)

summary: await get_user_summary(123)
```

## Java Async Composition

```java
import java.util.concurrent.CompletableFuture;
import java.util.function.Function;
import java.util.List;
import java.util.Arrays;

public class AsyncComposition {
    
    // Async pipe function
    public static <T> Function<T, CompletableFuture<T>> pipeAsync(List<Function<T, CompletableFuture<T>>> functions) {
        return functions.stream()
            .reduce(
                x -> CompletableFuture.completedFuture(x),
                (f, g) -> x -> f.apply(x).thenCompose(g)
            );
    }
    
    // Async functions
    public static CompletableFuture<User> fetchUser(int id) {
        return CompletableFuture.supplyAsync(() -> {
            // Simulate API call
            try {
                Thread.sleep(100);
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
            }
            return new User(id, "User " + id, "user" + id + "@example.com");
        });
    }
    
    public static CompletableFuture<User> enrichUser(User user) {
        return CompletableFuture.supplyAsync(() -> {
            // Simulate API call to get posts
            try {
                Thread.sleep(100);
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
            }
            List<Post> posts = Arrays.asList(
                new Post(1, "Post 1"),
                new Post(2, "Post 2")
            );
            user.setPosts(posts);
            return user;
        });
    }
    
    public static CompletableFuture<UserSummary> formatUser(User user) {
        return CompletableFuture.supplyAsync(() -> {
            return new UserSummary(
                user.getName(),
                user.getEmail(),
                user.getPosts().size()
            );
        });
    }
    
    // Compose async operations
    public static Function<Integer, CompletableFuture<UserSummary>> getUserSummary = pipeAsync(Arrays.asList(
        AsyncComposition::fetchUser,
        AsyncComposition::enrichUser,
        AsyncComposition::formatUser
    ));
    
    public static void main(String[] args) {
        // Test async composition
        CompletableFuture<UserSummary> future = getUserSummary.apply(123);
        
        future.thenAccept(summary -> {
            System.out.println("User Summary: " + summary);
        }).join();
    }
}

@Data
@AllArgsConstructor
class User {
    private int id;
    private String name;
    private String email;
    private List<Post> posts = new ArrayList<>();
    
    public User(int id, String name, String email) {
        this.id = id;
        this.name = name;
        this.email = email;
    }
}

@Data
@AllArgsConstructor
class Post {
    private int id;
    private String title;
}

@Data
@AllArgsConstructor
class UserSummary {
    private String name;
    private String email;
    private int postCount;
}
```

## Function Decorators

```tusk
# Logging decorator
with_logging: (fn) => {
    return (...args) => {
        console.log(`Calling ${fn.name} with`, args)
        result: fn(...args)
        console.log(`Result:`, result)
        return result
    }
}

# Timing decorator
with_timing: (fn) => {
    return (...args) => {
        start: performance.now()
        result: fn(...args)
        end: performance.now()
        console.log(`${fn.name} took ${end - start}ms`)
        return result
    }
}

# Memoization decorator
memoize: (fn) => {
    cache: new Map()
    
    return (...args) => {
        key: JSON.stringify(args)
        
        if (cache.has(key)) {
            return cache.get(key)
        }
        
        result: fn(...args)
        cache.set(key, result)
        return result
    }
}

# Compose decorators
enhance: compose(
    with_logging,
    with_timing,
    memoize
)

# Enhanced function
fibonacci: enhance((n) => {
    if (n <= 1) return n
    return fibonacci(n - 1) + fibonacci(n - 2)
})
```

## Java Function Decorators

```java
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.Function;
import java.util.function.BiFunction;
import java.util.Arrays;
import java.util.List;

public class FunctionDecorators {
    
    // Logging decorator
    public static <T, R> Function<T, R> withLogging(Function<T, R> fn, String name) {
        return input -> {
            System.out.println("Calling " + name + " with: " + input);
            R result = fn.apply(input);
            System.out.println("Result: " + result);
            return result;
        };
    }
    
    // Timing decorator
    public static <T, R> Function<T, R> withTiming(Function<T, R> fn, String name) {
        return input -> {
            long start = System.currentTimeMillis();
            R result = fn.apply(input);
            long end = System.currentTimeMillis();
            System.out.println(name + " took " + (end - start) + "ms");
            return result;
        };
    }
    
    // Memoization decorator
    public static <T, R> Function<T, R> memoize(Function<T, R> fn) {
        Map<T, R> cache = new ConcurrentHashMap<>();
        
        return input -> {
            return cache.computeIfAbsent(input, fn);
        };
    }
    
    // Compose decorators
    public static <T, R> Function<T, R> enhance(Function<T, R> fn, String name) {
        return pipe(Arrays.asList(
            withLogging(fn, name),
            withTiming(fn, name),
            memoize(fn)
        ));
    }
    
    // Enhanced fibonacci function
    public static Function<Integer, Integer> fibonacci = enhance(
        n -> {
            if (n <= 1) return n;
            return fibonacci.apply(n - 1) + fibonacci.apply(n - 2);
        },
        "fibonacci"
    );
    
    public static void main(String[] args) {
        // Test enhanced fibonacci
        int result = fibonacci.apply(10);
        System.out.println("Fibonacci(10): " + result);
    }
    
    private static <T> Function<T, T> pipe(List<Function<T, T>> functions) {
        return functions.stream()
            .reduce(Function.identity(), (f, g) -> f.andThen(g));
    }
}
```

## Monadic Composition

```tusk
# Maybe monad for null safety
Maybe: {
    of: (value) => ({
        value,
        map: (fn) => value != null ? Maybe.of(fn(value)) : Maybe.of(null),
        chain: (fn) => value != null ? fn(value) : Maybe.of(null),
        or_else: (default) => value != null ? value : default
    })
}

# Safe property access
safe_prop: (prop) => (obj) => Maybe.of(obj?.[prop])

# Compose with Maybe
get_city: pipe(
    Maybe.of,
    (m) => m.chain(safe_prop('address')),
    (m) => m.chain(safe_prop('city')),
    (m) => m.or_else('Unknown')
)

city: get_city(user)  # Safe even if address is null

# Result monad for error handling
Result: {
    ok: (value) => ({
        is_ok: true,
        value,
        map: (fn) => Result.ok(fn(value)),
        chain: (fn) => fn(value),
        or_else: () => value
    }),
    
    err: (error) => ({
        is_ok: false,
        error,
        map: () => Result.err(error),
        chain: () => Result.err(error),
        or_else: (fn) => fn(error)
    })
}

# Safe division
safe_divide: (a, b) => {
    if (b === 0) return Result.err("Division by zero")
    return Result.ok(a / b)
}

# Compose with Result
calculate: pipe(
    safe_divide(10, 2),
    (r) => r.map(x => x * 3),
    (r) => r.or_else(0)
)
```

## Java Monadic Composition

```java
import java.util.Optional;
import java.util.function.Function;
import java.util.function.Supplier;

public class MonadicComposition {
    
    // Maybe monad using Optional
    public static <T> Optional<T> maybeOf(T value) {
        return Optional.ofNullable(value);
    }
    
    public static <T, R> Function<Optional<T>, Optional<R>> map(Function<T, R> fn) {
        return maybe -> maybe.map(fn);
    }
    
    public static <T, R> Function<Optional<T>, Optional<R>> chain(Function<T, Optional<R>> fn) {
        return maybe -> maybe.flatMap(fn);
    }
    
    public static <T> Function<Optional<T>, T> orElse(T defaultValue) {
        return maybe -> maybe.orElse(defaultValue);
    }
    
    // Safe property access
    public static <T, R> Function<T, Optional<R>> safeProp(Function<T, R> prop) {
        return obj -> maybeOf(obj).map(prop);
    }
    
    // Compose with Maybe
    public static Function<User, String> getCity = pipe(Arrays.asList(
        MonadicComposition::maybeOf,
        chain(safeProp(User::getAddress)),
        chain(safeProp(Address::getCity)),
        orElse("Unknown")
    ));
    
    // Result monad
    public static class Result<T> {
        private final boolean isOk;
        private final T value;
        private final String error;
        
        private Result(boolean isOk, T value, String error) {
            this.isOk = isOk;
            this.value = value;
            this.error = error;
        }
        
        public static <T> Result<T> ok(T value) {
            return new Result<>(true, value, null);
        }
        
        public static <T> Result<T> err(String error) {
            return new Result<>(false, null, error);
        }
        
        public <R> Result<R> map(Function<T, R> fn) {
            return isOk ? ok(fn.apply(value)) : err(error);
        }
        
        public <R> Result<R> chain(Function<T, Result<R>> fn) {
            return isOk ? fn.apply(value) : err(error);
        }
        
        public T orElse(T defaultValue) {
            return isOk ? value : defaultValue;
        }
        
        public T orElseGet(Supplier<T> supplier) {
            return isOk ? value : supplier.get();
        }
    }
    
    // Safe division
    public static Result<Double> safeDivide(double a, double b) {
        if (b == 0) {
            return Result.err("Division by zero");
        }
        return Result.ok(a / b);
    }
    
    // Compose with Result
    public static Function<Double, Double> calculate = pipe(Arrays.asList(
        b -> safeDivide(10.0, b),
        r -> r.map(x -> x * 3),
        r -> r.orElse(0.0)
    ));
    
    public static void main(String[] args) {
        // Test Maybe composition
        User user = new User(1, "John", "john@example.com");
        user.setAddress(new Address("123 Main St", "New York"));
        
        String city = getCity.apply(user);
        System.out.println("City: " + city);  // "New York"
        
        // Test with null address
        User userWithoutAddress = new User(2, "Jane", "jane@example.com");
        String city2 = getCity.apply(userWithoutAddress);
        System.out.println("City: " + city2);  // "Unknown"
        
        // Test Result composition
        double result1 = calculate.apply(2.0);
        System.out.println("Calculate(2): " + result1);  // 15.0
        
        double result2 = calculate.apply(0.0);
        System.out.println("Calculate(0): " + result2);  // 0.0
    }
    
    private static <T> Function<T, T> pipe(List<Function<T, T>> functions) {
        return functions.stream()
            .reduce(Function.identity(), (f, g) -> f.andThen(g));
    }
}

@Data
@AllArgsConstructor
class Address {
    private String street;
    private String city;
}

@Data
@AllArgsConstructor
class User {
    private int id;
    private String name;
    private String email;
    private Address address;
    
    public User(int id, String name, String email) {
        this.id = id;
        this.name = name;
        this.email = email;
    }
}
```

## Spring Boot Integration

```tusk
# Service composition
#api /composed-service {
    user_data: @get_user_data(@request.user.id)
    processed_data: @compose_processing_pipeline(user_data)
    enriched_data: @enrich_with_external_data(processed_data)
    
    return {
        success: true,
        data: enriched_data
    }
}

# Pipeline configuration
processing_pipeline: pipe(
    @validate_data,
    @transform_data,
    @filter_data,
    @sort_data
)
```

## Java Spring Boot Integration

```java
import org.springframework.stereotype.Service;
import org.springframework.web.bind.annotation.*;
import org.springframework.http.ResponseEntity;
import java.util.function.Function;
import java.util.List;
import java.util.Map;
import java.util.HashMap;

@Service
public class ComposedService {
    
    private final UserService userService;
    private final DataProcessingService dataProcessingService;
    private final ExternalDataService externalDataService;
    
    public ComposedService(UserService userService,
                          DataProcessingService dataProcessingService,
                          ExternalDataService externalDataService) {
        this.userService = userService;
        this.dataProcessingService = dataProcessingService;
        this.externalDataService = externalDataService;
    }
    
    // Processing pipeline
    public static Function<UserData, UserData> processingPipeline = pipe(Arrays.asList(
        ComposedService::validateData,
        ComposedService::transformData,
        ComposedService::filterData,
        ComposedService::sortData
    ));
    
    @GetMapping("/api/composed-service")
    public ResponseEntity<Map<String, Object>> getComposedService(@AuthenticationPrincipal User user) {
        try {
            // Get user data
            UserData userData = userService.getUserData(user.getId());
            
            // Process data through pipeline
            UserData processedData = processingPipeline.apply(userData);
            
            // Enrich with external data
            EnrichedData enrichedData = externalDataService.enrichData(processedData);
            
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("data", enrichedData);
            
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> errorResponse = new HashMap<>();
            errorResponse.put("success", false);
            errorResponse.put("error", e.getMessage());
            
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(errorResponse);
        }
    }
    
    // Pipeline functions
    private static UserData validateData(UserData data) {
        if (data == null || data.getItems().isEmpty()) {
            throw new ValidationException("Invalid user data");
        }
        return data;
    }
    
    private static UserData transformData(UserData data) {
        // Transform data
        data.getItems().forEach(item -> item.setProcessed(true));
        return data;
    }
    
    private static UserData filterData(UserData data) {
        // Filter data
        data.setItems(data.getItems().stream()
            .filter(item -> item.isActive())
            .collect(Collectors.toList()));
        return data;
    }
    
    private static UserData sortData(UserData data) {
        // Sort data
        data.setItems(data.getItems().stream()
            .sorted((a, b) -> a.getName().compareTo(b.getName()))
            .collect(Collectors.toList()));
        return data;
    }
    
    private static <T> Function<T, T> pipe(List<Function<T, T>> functions) {
        return functions.stream()
            .reduce(Function.identity(), (f, g) -> f.andThen(g));
    }
}

@Service
public class DataProcessingService {
    
    public UserData processData(UserData data) {
        // Process data using composition
        return ComposedService.processingPipeline.apply(data);
    }
}

@Data
class UserData {
    private List<DataItem> items = new ArrayList<>();
}

@Data
class DataItem {
    private String name;
    private boolean active;
    private boolean processed;
}

@Data
class EnrichedData {
    private UserData userData;
    private Map<String, Object> externalData = new HashMap<>();
}
```

## Function Composition Testing

```java
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.TestPropertySource;
import org.springframework.beans.factory.annotation.Autowired;
import java.util.Arrays;
import java.util.List;

@SpringBootTest
@TestPropertySource(properties = {
    "spring.application.name=function-composition-test"
})
public class FunctionCompositionTest {
    
    @Autowired
    private ComposedService composedService;
    
    @MockBean
    private UserService userService;
    
    @MockBean
    private ExternalDataService externalDataService;
    
    @Test
    public void testBasicComposition() {
        // Test basic function composition
        Function<Integer, Integer> addOne = x -> x + 1;
        Function<Integer, Integer> doubleValue = x -> x * 2;
        Function<Integer, Integer> square = x -> x * x;
        
        Function<Integer, Integer> composed = addOne.andThen(doubleValue).andThen(square);
        
        int result = composed.apply(5);
        assertEquals(144, result);  // ((5 + 1) * 2)² = 144
    }
    
    @Test
    public void testStringProcessingPipeline() {
        // Test string processing pipeline
        Function<String, String> trim = String::trim;
        Function<String, String> lowercase = String::toLowerCase;
        Function<String, String> replaceSpaces = s -> s.replaceAll("\\s+", "-");
        
        Function<String, String> slugify = trim.andThen(lowercase).andThen(replaceSpaces);
        
        String result = slugify.apply("  Hello World!  ");
        assertEquals("hello-world!", result);
    }
    
    @Test
    public void testDataProcessingPipeline() {
        // Test data processing pipeline
        UserData userData = new UserData();
        userData.setItems(Arrays.asList(
            new DataItem("Charlie", true, false),
            new DataItem("Alice", true, false),
            new DataItem("Bob", false, false)
        ));
        
        UserData result = ComposedService.processingPipeline.apply(userData);
        
        // Should be filtered (only active items), sorted, and processed
        assertEquals(2, result.getItems().size());
        assertEquals("Alice", result.getItems().get(0).getName());
        assertEquals("Charlie", result.getItems().get(1).getName());
        assertTrue(result.getItems().get(0).isProcessed());
        assertTrue(result.getItems().get(1).isProcessed());
    }
    
    @Test
    public void testMonadicComposition() {
        // Test Maybe monad composition
        User user = new User(1, "John", "john@example.com");
        user.setAddress(new Address("123 Main St", "New York"));
        
        String city = MonadicComposition.getCity.apply(user);
        assertEquals("New York", city);
        
        // Test with null address
        User userWithoutAddress = new User(2, "Jane", "jane@example.com");
        String city2 = MonadicComposition.getCity.apply(userWithoutAddress);
        assertEquals("Unknown", city2);
    }
    
    @Test
    public void testAsyncComposition() {
        // Test async composition
        CompletableFuture<UserSummary> future = AsyncComposition.getUserSummary.apply(123);
        
        UserSummary summary = future.join();
        assertNotNull(summary);
        assertEquals("User 123", summary.getName());
        assertEquals(2, summary.getPostCount());
    }
}
```

## Configuration Properties

```yaml
# application.yml
tusk:
  function-composition:
    enabled: true
    debug: false
    cache-results: true
    max-pipeline-length: 10
    
    pipelines:
      data-processing:
        functions:
          - "validate"
          - "transform"
          - "filter"
          - "sort"
        cache-ttl: 300
        
      string-processing:
        functions:
          - "trim"
          - "lowercase"
          - "replace-spaces"
        cache-ttl: 600
    
    decorators:
      logging:
        enabled: true
        level: "INFO"
      
      timing:
        enabled: true
        threshold-ms: 100
      
      memoization:
        enabled: true
        max-cache-size: 1000

spring:
  application:
    name: "function-composition"
```

## Summary

Function composition in TuskLang provides powerful functional programming capabilities for Java applications. With Spring Boot integration, flexible composition patterns, monadic operations, and comprehensive testing support, you can implement sophisticated data processing pipelines that enhance your application's functionality.

Key features include:
- **Multiple composition patterns**: Basic composition, partial application, async composition
- **Spring Boot integration**: Seamless integration with Spring Boot framework
- **Flexible configuration**: Configurable pipelines with caching and decorators
- **Monadic operations**: Maybe and Result monads for safe operations
- **Function decorators**: Logging, timing, and memoization decorators
- **Async support**: Async composition with CompletableFuture
- **Testing support**: Comprehensive testing utilities

The Java implementation provides enterprise-grade functional programming capabilities that integrate seamlessly with Spring Boot applications while maintaining the simplicity and power of TuskLang's declarative syntax. 