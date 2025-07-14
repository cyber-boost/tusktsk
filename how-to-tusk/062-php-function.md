# php() - PHP Integration Function

The `php()` function allows seamless integration with PHP code and libraries, enabling you to leverage the vast PHP ecosystem within TuskLang applications.

## Basic Syntax

```tusk
# Execute PHP code
result: php("return strtoupper('hello');")  # "HELLO"

# Call PHP functions
timestamp: php("time()")
formatted: php("date('Y-m-d H:i:s')")

# Multi-line PHP code
calculation: php("""
    $sum = 0;
    for ($i = 1; $i <= 10; $i++) {
        $sum += $i;
    }
    return $sum;
""")
```

## Passing Variables

```tusk
# Pass TuskLang variables to PHP
name: "John Doe"
age: 30

greeting: php("return 'Hello, ' . $name . '! You are ' . $age . ' years old.';", {
    name: name,
    age: age
})

# Complex data structures
data: {
    users: ["Alice", "Bob", "Charlie"]
    settings: {
        theme: "dark"
        language: "en"
    }
}

processed: php("""
    $result = [];
    foreach ($users as $user) {
        $result[] = strtoupper($user) . ' (' . $settings['language'] . ')';
    }
    return $result;
""", {
    users: data.users,
    settings: data.settings
})
```

## Using PHP Libraries

```tusk
# Load PHP libraries
php("require_once 'vendor/autoload.php';")

# Use Composer packages
markdown_html: php("""
    use League\CommonMark\CommonMarkConverter;
    
    $converter = new CommonMarkConverter();
    return $converter->convertToHtml($markdown);
""", {
    markdown: "# Hello World\n\nThis is **markdown**!"
})

# Using PHP built-in classes
datetime: php("""
    $date = new DateTime($dateString);
    $date->add(new DateInterval('P1D'));
    return $date->format('Y-m-d H:i:s');
""", {
    dateString: "2024-01-01"
})
```

## Class Integration

```tusk
# Define PHP class
php("""
    class Calculator {
        private $precision;
        
        public function __construct($precision = 2) {
            $this->precision = $precision;
        }
        
        public function add($a, $b) {
            return round($a + $b, $this->precision);
        }
        
        public function multiply($a, $b) {
            return round($a * $b, $this->precision);
        }
        
        public function percentage($value, $percent) {
            return round($value * ($percent / 100), $this->precision);
        }
    }
""")

# Use PHP class from TuskLang
calc: php("return new Calculator(2);")

# Call methods
sum: php("return $calc->add($a, $b);", {calc: calc, a: 10.555, b: 20.333})
product: php("return $calc->multiply($x, $y);", {calc: calc, x: 5.5, y: 2.2})
discount: php("return $calc->percentage($price, $percent);", {
    calc: calc,
    price: 99.99,
    percent: 15
})
```

## Database Operations

```tusk
# Use PHP PDO
connection: php("""
    try {
        $pdo = new PDO($dsn, $username, $password);
        $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
        return $pdo;
    } catch (PDOException $e) {
        throw new Exception('Connection failed: ' . $e->getMessage());
    }
""", {
    dsn: "mysql:host=localhost;dbname=myapp",
    username: env("DB_USERNAME"),
    password: env("DB_PASSWORD")
})

# Execute queries
users: php("""
    $stmt = $pdo->prepare("SELECT * FROM users WHERE active = :active");
    $stmt->execute(['active' => 1]);
    return $stmt->fetchAll(PDO::FETCH_ASSOC);
""", {
    pdo: connection
})

# Prepared statements
insert_user: (name, email) => {
    return php("""
        $stmt = $pdo->prepare("INSERT INTO users (name, email) VALUES (:name, :email)");
        $stmt->execute(['name' => $name, 'email' => $email]);
        return $pdo->lastInsertId();
    """, {
        pdo: connection,
        name: name,
        email: email
    })
}
```

## Error Handling

```tusk
# Catch PHP exceptions
safe_php: (code, vars: {}) => {
    try {
        return php(code, vars)
    } catch (e) {
        log.error("PHP execution failed", {
            error: e.message,
            code: code,
            vars: vars
        })
        return null
    }
}

# Error handling in PHP code
result: php("""
    try {
        if (!function_exists($function)) {
            throw new Exception("Function does not exist: " . $function);
        }
        
        return call_user_func($function, $param);
    } catch (Exception $e) {
        return ['error' => $e->getMessage()];
    }
""", {
    function: "some_function",
    param: "test"
})

if (result.error) {
    handle_error(result.error)
}
```

## Working with Files

```tusk
# File operations using PHP
file_info: php("""
    $info = [];
    if (file_exists($path)) {
        $info['exists'] = true;
        $info['size'] = filesize($path);
        $info['modified'] = date('Y-m-d H:i:s', filemtime($path));
        $info['type'] = mime_content_type($path);
        $info['readable'] = is_readable($path);
        $info['writable'] = is_writable($path);
    } else {
        $info['exists'] = false;
    }
    return $info;
""", {
    path: "/path/to/file.txt"
})

# Process CSV with PHP
csv_data: php("""
    $data = [];
    if (($handle = fopen($filename, "r")) !== FALSE) {
        $headers = fgetcsv($handle);
        while (($row = fgetcsv($handle)) !== FALSE) {
            $data[] = array_combine($headers, $row);
        }
        fclose($handle);
    }
    return $data;
""", {
    filename: "data.csv"
})
```

## Image Processing

```tusk
# Use GD library
thumbnail: php("""
    $source = imagecreatefromjpeg($source_path);
    $width = imagesx($source);
    $height = imagesy($source);
    
    $thumb_width = 200;
    $thumb_height = (int)($height * ($thumb_width / $width));
    
    $thumb = imagecreatetruecolor($thumb_width, $thumb_height);
    imagecopyresampled($thumb, $source, 0, 0, 0, 0, 
                      $thumb_width, $thumb_height, $width, $height);
    
    imagejpeg($thumb, $dest_path, 90);
    imagedestroy($source);
    imagedestroy($thumb);
    
    return true;
""", {
    source_path: "original.jpg",
    dest_path: "thumbnail.jpg"
})

# ImageMagick operations
php("exec('convert $input -resize 800x600 -quality 85 $output');", {
    input: escapeshellarg("input.jpg"),
    output: escapeshellarg("output.jpg")
})
```

## Regular Expressions

```tusk
# Complex regex with PHP
matches: php("""
    preg_match_all($pattern, $text, $matches, PREG_SET_ORDER);
    return $matches;
""", {
    pattern: '/(?P<protocol>https?):\/\/(?P<domain>[^\/]+)(?P<path>\/.*)?/',
    text: "Visit https://example.com/page and http://test.com"
})

# Replace with callback
processed: php("""
    return preg_replace_callback($pattern, function($matches) use ($replacements) {
        return $replacements[$matches[1]] ?? $matches[0];
    }, $text);
""", {
    pattern: '/@(\w+)/',
    text: "Hello @user, welcome to @app!",
    replacements: {
        user: "John",
        app: "TuskLang"
    }
})
```

## Cryptography

```tusk
# Encryption with PHP
encrypted: php("""
    $cipher = "AES-256-CBC";
    $ivlen = openssl_cipher_iv_length($cipher);
    $iv = openssl_random_pseudo_bytes($ivlen);
    $ciphertext = openssl_encrypt($plaintext, $cipher, $key, 0, $iv);
    return base64_encode($iv . $ciphertext);
""", {
    plaintext: "Secret message",
    key: env("ENCRYPTION_KEY")
})

# Decryption
decrypted: php("""
    $cipher = "AES-256-CBC";
    $data = base64_decode($encrypted);
    $ivlen = openssl_cipher_iv_length($cipher);
    $iv = substr($data, 0, $ivlen);
    $ciphertext = substr($data, $ivlen);
    return openssl_decrypt($ciphertext, $cipher, $key, 0, $iv);
""", {
    encrypted: encrypted,
    key: env("ENCRYPTION_KEY")
})

# Password hashing
hashed: php("return password_hash($password, PASSWORD_ARGON2ID);", {
    password: "user_password"
})

verified: php("return password_verify($password, $hash);", {
    password: "user_password",
    hash: hashed
})
```

## Performance Optimization

```tusk
# Cache PHP execution results
php_cached: (code, vars: {}, ttl: 3600) => {
    cache_key: "php:" + md5(code + json_encode(vars))
    
    cached: cache.get(cache_key)
    if (cached !== null) {
        return cached
    }
    
    result: php(code, vars)
    cache.set(cache_key, result, ttl)
    
    return result
}

# Precompile PHP code
compiled: php("return create_function('$params', $code);", {
    code: 'return $params["a"] + $params["b"];'
})

# Use compiled function
result: php("return $func(['a' => 10, 'b' => 20]);", {func: compiled})
```

## Sandbox Execution

```tusk
# Safe PHP execution with restrictions
safe_eval: (code, vars: {}) => {
    return php("""
        // Disable dangerous functions
        $disabled = ['exec', 'system', 'shell_exec', 'eval', 'file_get_contents'];
        foreach ($disabled as $func) {
            if (stripos($code, $func) !== false) {
                throw new Exception("Function '$func' is not allowed");
            }
        }
        
        // Execute in limited scope
        $sandbox = function($vars) use ($code) {
            extract($vars);
            return eval('return ' . $code . ';');
        };
        
        return $sandbox($vars);
    """, {
        code: code,
        vars: vars
    })
}
```

## Best Practices

1. **Validate PHP code** - Never execute untrusted PHP code
2. **Use prepared statements** - Prevent SQL injection in PHP
3. **Handle errors gracefully** - Catch PHP exceptions
4. **Sanitize inputs** - Clean data before passing to PHP
5. **Cache results** - Avoid repeated PHP execution
6. **Use type hints** - Ensure correct data types
7. **Minimize context switching** - Batch PHP operations
8. **Document PHP dependencies** - List required extensions

## Related Functions

- `eval()` - Evaluate code (use with caution)
- `include()` - Include PHP files
- `require()` - Require PHP files
- `exec()` - Execute system commands
- `serialize()` - PHP serialization