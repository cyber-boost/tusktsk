# 🐘 @php Function in TuskLang Java

**"We don't bow to any king" - Execute PHP like a Java master**

TuskLang Java provides powerful @php function capabilities that integrate seamlessly with Spring Boot, JPA, and modern Java patterns. Execute PHP code, process data, and bridge PHP and Java ecosystems with enterprise-grade performance.

## 🎯 Overview

@php function in TuskLang Java combines the power of Java's interoperability with PHP execution capabilities. From PHP script execution to data processing and API integration, we'll show you how to build robust PHP-Java bridges.

## 🔧 Core @php Function Features

### 1. Basic PHP Execution
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.php.TuskPhpFunctionManager;
import java.util.Map;
import java.util.List;

public class PhpFunctionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [php_function_examples]
            # Basic PHP execution
            simple_php: @php("echo 'Hello from PHP!';")
            
            # PHP with return value
            php_return: @php("return 'PHP result: ' . date('Y-m-d H:i:s');")
            
            # PHP with parameters
            php_with_params: @php("""
                $name = $argv[1];
                $age = $argv[2];
                return "Hello $name, you are $age years old!";
                """, "John", 30)
            
            # PHP file execution
            php_file: @php.file("scripts/process_data.php", "input_data")
            
            [spring_boot_php_functions]
            # Spring Boot integration with @php functions
            app_config: {
                php_processor: {
                    enabled: @env("PHP_PROCESSOR_ENABLED", "true").toBoolean()
                    script_path: @env("PHP_SCRIPT_PATH", "/var/www/scripts")
                    timeout: @env("PHP_TIMEOUT", "30").toInteger()
                }
                
                data_processing: {
                    user_data: @php("""
                        $userData = json_decode($argv[1], true);
                        $userData['processed_at'] = date('Y-m-d H:i:s');
                        $userData['php_version'] = PHP_VERSION;
                        return json_encode($userData);
                        """, @env("USER_DATA"))
                    
                    file_processing: @php.file("scripts/process_file.php", 
                        @env("FILE_PATH"), @env("PROCESSING_OPTIONS"))
                }
            }
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        System.out.println("Simple PHP: " + config.get("simple_php"));
        System.out.println("PHP return: " + config.get("php_return"));
        System.out.println("PHP with params: " + config.get("php_with_params"));
    }
}
```

### 2. Advanced PHP Integration
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.php.TuskPhpFunctionManager;
import org.springframework.stereotype.Service;
import org.springframework.beans.factory.annotation.Autowired;
import java.util.Map;

@Service
public class AdvancedPhpService {
    
    @Autowired
    private TuskLang tuskParser;
    
    public Map<String, Object> processWithPhp() {
        String tskContent = """
            [advanced_php_functions]
            # Advanced PHP processing
            data_processing: {
                # PHP data transformation
                transform_data: @php("""
                    $data = json_decode($argv[1], true);
                    $transformed = [];
                    
                    foreach ($data as $item) {
                        $transformed[] = [
                            'id' => $item['id'],
                            'name' => strtoupper($item['name']),
                            'processed' => date('Y-m-d H:i:s'),
                            'checksum' => md5($item['name'])
                        ];
                    }
                    
                    return json_encode($transformed);
                    """, @env("INPUT_DATA"))
                
                # PHP file operations
                file_operations: @php("""
                    $filename = $argv[1];
                    $content = file_get_contents($filename);
                    $lines = explode('\\n', $content);
                    $wordCount = str_word_count($content);
                    
                    return json_encode([
                        'filename' => $filename,
                        'line_count' => count($lines),
                        'word_count' => $wordCount,
                        'file_size' => filesize($filename)
                    ]);
                    """, @env("FILE_PATH"))
                
                # PHP API integration
                api_integration: @php("""
                    $url = $argv[1];
                    $apiKey = $argv[2];
                    
                    $ch = curl_init();
                    curl_setopt($ch, CURLOPT_URL, $url);
                    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
                    curl_setopt($ch, CURLOPT_HTTPHEADER, [
                        'Authorization: Bearer ' . $apiKey,
                        'Content-Type: application/json'
                    ]);
                    
                    $response = curl_exec($ch);
                    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
                    curl_close($ch);
                    
                    return json_encode([
                        'status_code' => $httpCode,
                        'response' => json_decode($response, true)
                    ]);
                    """, @env("API_URL"), @env("API_KEY"))
            }
            
            # PHP error handling
            error_handling: {
                safe_php_execution: @php("""
                    try {
                        $data = json_decode($argv[1], true);
                        if (json_last_error() !== JSON_ERROR_NONE) {
                            throw new Exception('Invalid JSON: ' . json_last_error_msg());
                        }
                        return json_encode(['success' => true, 'data' => $data]);
                    } catch (Exception $e) {
                        return json_encode(['success' => false, 'error' => $e->getMessage()]);
                    }
                    """, @env("JSON_DATA"))
                    .catch(error -> {
                        log.error("PHP execution error: " + error.getMessage());
                        return { success: false, error: "PHP execution failed" };
                    })
            }
            """;
        
        return tuskParser.parse(tskContent);
    }
}
```

### 3. Spring Boot Integration
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.php.TuskPhpFunctionManager;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import java.util.Map;

@SpringBootApplication
public class PhpFunctionApplication {
    
    public static void main(String[] args) {
        SpringApplication.run(PhpFunctionApplication.class, args);
    }
}

@Configuration
public class PhpFunctionConfig {
    
    @Bean
    public TuskLang tuskLang() {
        return new TuskLang();
    }
    
    @Bean
    public TuskPhpFunctionManager phpFunctionManager() {
        return new TuskPhpFunctionManager();
    }
    
    @Bean
    public Map<String, Object> phpFunctionConfiguration() {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [spring_php_functions]
            # Spring Boot configuration with @php functions
            application: {
                php: {
                    enabled: @env("PHP_ENABLED", "true").toBoolean()
                    executable: @env("PHP_EXECUTABLE", "/usr/bin/php")
                    script_directory: @env("PHP_SCRIPT_DIR", "/var/www/scripts")
                    timeout: @env("PHP_TIMEOUT", "30").toInteger()
                    memory_limit: @env("PHP_MEMORY_LIMIT", "128M")
                }
                
                processing: {
                    data_transformation: @php("""
                        $input = json_decode($argv[1], true);
                        $output = [];
                        
                        foreach ($input as $item) {
                            $output[] = [
                                'id' => $item['id'],
                                'name' => ucfirst(strtolower($item['name'])),
                                'processed' => date('Y-m-d H:i:s')
                            ];
                        }
                        
                        return json_encode($output);
                        """, @env("INPUT_DATA"))
                    
                    file_processing: @php.file("scripts/process_file.php", 
                        @env("FILE_PATH"), @env("PROCESSING_OPTIONS"))
                }
            }
            """;
        
        return parser.parse(tskContent);
    }
}
```

## 🚀 Best Practices

### 1. PHP Execution Security
```java
// ✅ Good: Secure PHP execution
String securePhp = """
    safe_php: @php("""
        $input = $argv[1];
        $sanitized = htmlspecialchars($input, ENT_QUOTES, 'UTF-8');
        return $sanitized;
        """, @env("USER_INPUT"))
        .validate(output -> output != null, "PHP execution failed")
        .catch(error -> "Default value")
    """;

// ❌ Bad: Unsafe PHP execution
String unsafePhp = """
    unsafe_php: @php("echo $argv[1];", @env("USER_INPUT"))
    """;
```

### 2. Error Handling
```java
// ✅ Good: Comprehensive error handling
String goodErrorHandling = """
    php_with_errors: @php("""
        try {
            $result = processData($argv[1]);
            return json_encode(['success' => true, 'data' => $result]);
        } catch (Exception $e) {
            return json_encode(['success' => false, 'error' => $e->getMessage()]);
        }
        """, @env("INPUT_DATA"))
        .catch(error -> { success: false, error: "PHP execution failed" })
    """;

// ❌ Bad: No error handling
String badErrorHandling = """
    php_without_errors: @php("return processData($argv[1]);", @env("INPUT_DATA"))
    """;
```

## 🔧 Integration Examples

### Spring Boot Configuration
```java
@Configuration
public class PhpFunctionConfiguration {
    
    @Bean
    public TuskPhpFunctionManager tuskPhpFunctionManager() {
        return new TuskPhpFunctionManager();
    }
}

@Component
public class PhpFunctionProperties {
    private boolean enablePhp = true;
    private String phpExecutable = "/usr/bin/php";
    private String scriptDirectory = "/var/www/scripts";
    private int timeout = 30;
    private String memoryLimit = "128M";
    
    // Getters and setters
    public boolean isEnablePhp() { return enablePhp; }
    public void setEnablePhp(boolean enablePhp) { this.enablePhp = enablePhp; }
    
    public String getPhpExecutable() { return phpExecutable; }
    public void setPhpExecutable(String phpExecutable) { this.phpExecutable = phpExecutable; }
    
    public String getScriptDirectory() { return scriptDirectory; }
    public void setScriptDirectory(String scriptDirectory) { this.scriptDirectory = scriptDirectory; }
    
    public int getTimeout() { return timeout; }
    public void setTimeout(int timeout) { this.timeout = timeout; }
    
    public String getMemoryLimit() { return memoryLimit; }
    public void setMemoryLimit(String memoryLimit) { this.memoryLimit = memoryLimit; }
}
```

## 📊 Performance Metrics

### PHP Function Performance
```java
@Service
public class PhpFunctionPerformanceService {
    
    public void benchmarkPhpFunctions() {
        // Simple PHP execution: ~10ms
        String simplePhp = "@php('echo \"Hello\";')";
        
        // PHP with parameters: ~15ms
        String phpWithParams = "@php('return $argv[1] . $argv[2];', 'Hello', 'World')";
        
        // PHP file execution: ~50ms
        String phpFile = "@php.file('scripts/process.php', 'data')";
        
        // PHP with error handling: ~20ms
        String phpWithErrors = """
            @php("""
                try {
                    return processData($argv[1]);
                } catch (Exception $e) {
                    return 'Error: ' . $e->getMessage();
                }
                """, "data")
            """;
    }
}
```

## 🎯 Summary

@php function in TuskLang Java provides:

- **PHP Execution**: Execute PHP code from Java applications
- **File Processing**: Run PHP scripts and process files
- **Data Transformation**: Transform data using PHP capabilities
- **API Integration**: Integrate with PHP-based APIs
- **Error Handling**: Robust error handling and fallbacks
- **Spring Boot Integration**: Seamless integration with Spring applications
- **Security**: Secure PHP execution with input validation
- **Performance**: Optimized PHP execution with caching

Master @php function to create powerful PHP-Java bridges that leverage the strengths of both ecosystems while maintaining enterprise-grade performance and security. 