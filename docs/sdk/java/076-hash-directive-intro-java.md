# #hash Directive Introduction (Java)

The `#hash` directive system in TuskLang provides enterprise-grade declarative programming capabilities for Java applications, enabling powerful configuration-driven development with Spring Boot integration and comprehensive directive processing.

## Basic Syntax

```tusk
# Basic hash directive
#directive_name {
    #api /endpoint {
        return @process_request()
    }
}

# Hash directive with parameters
#directive_name param1: "value1", param2: "value2" {
    #web /page {
        @render_page()
    }
}

# Hash directive with configuration
#directive_name {
    config: {
        option1: true
        option2: "value"
        option3: 42
    }
} {
    #cli command: "example" {
        @execute_command()
    }
}
```

## Java Implementation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.HashDirective;
import org.springframework.stereotype.Component;
import org.springframework.context.annotation.Configuration;
import java.util.Map;
import java.util.HashMap;

@Component
public class HashDirectiveProcessor {
    
    private final TuskLang tuskLang;
    private final Map<String, HashDirective> directives;
    
    public HashDirectiveProcessor(TuskLang tuskLang) {
        this.tuskLang = tuskLang;
        this.directives = new HashMap<>();
        initializeDirectives();
    }
    
    private void initializeDirectives() {
        directives.put("api", new ApiDirective());
        directives.put("web", new WebDirective());
        directives.put("cli", new CliDirective());
        directives.put("cron", new CronDirective());
        directives.put("auth", new AuthDirective());
        directives.put("cache", new CacheDirective());
        directives.put("middleware", new MiddlewareDirective());
        directives.put("rate_limit", new RateLimitDirective());
    }
    
    public void processDirective(String directiveName, Map<String, Object> params, 
                               Map<String, Object> config, Object content) {
        HashDirective directive = directives.get(directiveName);
        if (directive != null) {
            directive.process(params, config, content);
        } else {
            throw new IllegalArgumentException("Unknown directive: " + directiveName);
        }
    }
}

@Configuration
public class HashDirectiveConfiguration {
    
    @Bean
    public HashDirectiveProcessor hashDirectiveProcessor(TuskLang tuskLang) {
        return new HashDirectiveProcessor(tuskLang);
    }
}
```

## Directive Types

```tusk
# API directives
#api /users {
    return @get_all_users()
}

# Web directives
#web /home {
    @render("home.html", {title: "Welcome"})
}

# CLI directives
#cli command: "hello" {
    @print("Hello, World!")
}

# Cron directives
#cron "0 0 * * *" {
    @backup_database()
}

# Auth directives
#auth {
    #api /protected {
        return @get_protected_data()
    }
}

# Cache directives
#cache 300 {
    #api /cached {
        return @get_cached_data()
    }
}

# Middleware directives
#middleware {
    before: @log_request()
    after: @log_response()
} {
    #api /logged {
        return @process_request()
    }
}

# Rate limit directives
#rate_limit 100 {
    #api /limited {
        return @process_limited_request()
    }
}
```

## Java Directive Types

```java
import org.springframework.stereotype.Component;
import org.springframework.stereotype.Service;
import java.util.Map;
import java.util.List;

// Base directive interface
public interface HashDirective {
    void process(Map<String, Object> params, Map<String, Object> config, Object content);
    String getName();
    List<String> getSupportedTypes();
}

// API Directive
@Component
public class ApiDirective implements HashDirective {
    
    private final ApiService apiService;
    
    public ApiDirective(ApiService apiService) {
        this.apiService = apiService;
    }
    
    @Override
    public void process(Map<String, Object> params, Map<String, Object> config, Object content) {
        String route = (String) params.get("route");
        String method = (String) params.getOrDefault("method", "GET");
        
        apiService.registerEndpoint(route, method, config, content);
    }
    
    @Override
    public String getName() {
        return "api";
    }
    
    @Override
    public List<String> getSupportedTypes() {
        return List.of("GET", "POST", "PUT", "DELETE", "PATCH");
    }
}

// Web Directive
@Component
public class WebDirective implements HashDirective {
    
    private final WebService webService;
    
    public WebDirective(WebService webService) {
        this.webService = webService;
    }
    
    @Override
    public void process(Map<String, Object> params, Map<String, Object> config, Object content) {
        String route = (String) params.get("route");
        String template = (String) config.get("template");
        
        webService.registerPage(route, template, config, content);
    }
    
    @Override
    public String getName() {
        return "web";
    }
    
    @Override
    public List<String> getSupportedTypes() {
        return List.of("page", "template", "layout");
    }
}

// CLI Directive
@Component
public class CliDirective implements HashDirective {
    
    private final CliService cliService;
    
    public CliDirective(CliService cliService) {
        this.cliService = cliService;
    }
    
    @Override
    public void process(Map<String, Object> params, Map<String, Object> config, Object content) {
        String command = (String) params.get("command");
        String description = (String) config.get("description");
        
        cliService.registerCommand(command, description, config, content);
    }
    
    @Override
    public String getName() {
        return "cli";
    }
    
    @Override
    public List<String> getSupportedTypes() {
        return List.of("command", "subcommand", "option");
    }
}

// Cron Directive
@Component
public class CronDirective implements HashDirective {
    
    private final CronService cronService;
    
    public CronDirective(CronService cronService) {
        this.cronService = cronService;
    }
    
    @Override
    public void process(Map<String, Object> params, Map<String, Object> config, Object content) {
        String schedule = (String) params.get("schedule");
        String timezone = (String) config.get("timezone");
        
        cronService.scheduleJob(schedule, timezone, config, content);
    }
    
    @Override
    public String getName() {
        return "cron";
    }
    
    @Override
    public List<String> getSupportedTypes() {
        return List.of("job", "task", "schedule");
    }
}
```

## Directive Configuration

```tusk
# Directive with configuration
#api {
    route: "/api/data"
    method: "GET"
    auth: true
    cache: true
    cache_ttl: 300
    rate_limit: 100
    middleware: ["logging", "validation"]
} {
    data: @get_data()
    return {data: data}
}

# Nested directives
#auth {
    #api /protected {
        return @get_protected_data()
    }
    
    #web /secure {
        @render("secure.html")
    }
}

# Conditional directives
#cache if: @is_production() {
    #api /cached {
        return @get_cached_data()
    }
}
```

## Java Directive Configuration

```java
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;
import java.util.Map;
import java.util.List;

@Component
@ConfigurationProperties(prefix = "tusk.directives")
public class DirectiveConfig {
    
    private Map<String, DirectiveDefinition> definitions;
    private List<String> enabledDirectives;
    private Map<String, Object> globalConfig;
    
    // Getters and setters
    public Map<String, DirectiveDefinition> getDefinitions() { return definitions; }
    public void setDefinitions(Map<String, DirectiveDefinition> definitions) { this.definitions = definitions; }
    
    public List<String> getEnabledDirectives() { return enabledDirectives; }
    public void setEnabledDirectives(List<String> enabledDirectives) { this.enabledDirectives = enabledDirectives; }
    
    public Map<String, Object> getGlobalConfig() { return globalConfig; }
    public void setGlobalConfig(Map<String, Object> globalConfig) { this.globalConfig = globalConfig; }
    
    public static class DirectiveDefinition {
        private String name;
        private String description;
        private List<String> supportedTypes;
        private Map<String, Object> defaultConfig;
        private List<String> requiredParams;
        private List<String> optionalParams;
        
        // Getters and setters
        public String getName() { return name; }
        public void setName(String name) { this.name = name; }
        
        public String getDescription() { return description; }
        public void setDescription(String description) { this.description = description; }
        
        public List<String> getSupportedTypes() { return supportedTypes; }
        public void setSupportedTypes(List<String> supportedTypes) { this.supportedTypes = supportedTypes; }
        
        public Map<String, Object> getDefaultConfig() { return defaultConfig; }
        public void setDefaultConfig(Map<String, Object> defaultConfig) { this.defaultConfig = defaultConfig; }
        
        public List<String> getRequiredParams() { return requiredParams; }
        public void setRequiredParams(List<String> requiredParams) { this.requiredParams = requiredParams; }
        
        public List<String> getOptionalParams() { return optionalParams; }
        public void setOptionalParams(List<String> optionalParams) { this.optionalParams = optionalParams; }
    }
}

@Service
public class DirectiveConfigurationService {
    
    private final DirectiveConfig config;
    private final Map<String, HashDirective> directives;
    
    public DirectiveConfigurationService(DirectiveConfig config, Map<String, HashDirective> directives) {
        this.config = config;
        this.directives = directives;
    }
    
    public void configureDirective(String directiveName, Map<String, Object> params, 
                                 Map<String, Object> config, Object content) {
        
        // Check if directive is enabled
        if (!config.getEnabledDirectives().contains(directiveName)) {
            throw new DirectiveDisabledException("Directive " + directiveName + " is disabled");
        }
        
        // Get directive definition
        DirectiveConfig.DirectiveDefinition definition = config.getDefinitions().get(directiveName);
        if (definition == null) {
            throw new DirectiveNotFoundException("Directive " + directiveName + " not found");
        }
        
        // Validate required parameters
        validateRequiredParams(params, definition.getRequiredParams());
        
        // Merge with default configuration
        Map<String, Object> mergedConfig = mergeConfig(config, definition.getDefaultConfig());
        
        // Process directive
        HashDirective directive = directives.get(directiveName);
        if (directive != null) {
            directive.process(params, mergedConfig, content);
        }
    }
    
    private void validateRequiredParams(Map<String, Object> params, List<String> requiredParams) {
        if (requiredParams != null) {
            for (String requiredParam : requiredParams) {
                if (!params.containsKey(requiredParam)) {
                    throw new MissingParameterException("Required parameter missing: " + requiredParam);
                }
            }
        }
    }
    
    private Map<String, Object> mergeConfig(Map<String, Object> userConfig, Map<String, Object> defaultConfig) {
        Map<String, Object> merged = new HashMap<>();
        
        // Add default config
        if (defaultConfig != null) {
            merged.putAll(defaultConfig);
        }
        
        // Override with user config
        if (userConfig != null) {
            merged.putAll(userConfig);
        }
        
        return merged;
    }
}
```

## Custom Directives

```tusk
# Custom directive definition
#custom_directive {
    name: "my_directive"
    description: "My custom directive"
    params: ["param1", "param2"]
    config: {
        option1: true
        option2: "default"
    }
} {
    #api /custom {
        param1: @params.param1
        param2: @params.param2
        @process_custom_logic(param1, param2)
    }
}

# Custom directive with validation
#custom_directive {
    name: "validated_directive"
    validation: {
        param1: "required|string"
        param2: "optional|number"
        param3: "required|email"
    }
} {
    @validate_params(@params)
    @process_validated_logic(@params)
}
```

## Java Custom Directives

```java
import org.springframework.stereotype.Component;
import org.springframework.validation.Validator;
import org.springframework.validation.BeanPropertyBindingResult;
import org.springframework.validation.Errors;

@Component
public class CustomDirective implements HashDirective {
    
    private final ValidationService validationService;
    private final CustomLogicService customLogicService;
    
    public CustomDirective(ValidationService validationService, CustomLogicService customLogicService) {
        this.validationService = validationService;
        this.customLogicService = customLogicService;
    }
    
    @Override
    public void process(Map<String, Object> params, Map<String, Object> config, Object content) {
        String directiveName = (String) config.get("name");
        String description = (String) config.get("description");
        
        // Validate parameters if validation rules are specified
        if (config.containsKey("validation")) {
            validateParams(params, (Map<String, Object>) config.get("validation"));
        }
        
        // Process custom logic
        customLogicService.processCustomLogic(directiveName, params, content);
    }
    
    private void validateParams(Map<String, Object> params, Map<String, Object> validationRules) {
        for (Map.Entry<String, Object> rule : validationRules.entrySet()) {
            String paramName = rule.getKey();
            String validationRule = (String) rule.getValue();
            
            Object paramValue = params.get(paramName);
            validationService.validateParameter(paramName, paramValue, validationRule);
        }
    }
    
    @Override
    public String getName() {
        return "custom_directive";
    }
    
    @Override
    public List<String> getSupportedTypes() {
        return List.of("custom", "validated", "logic");
    }
}

@Service
public class CustomLogicService {
    
    public void processCustomLogic(String directiveName, Map<String, Object> params, Object content) {
        // Process custom directive logic
        switch (directiveName) {
            case "my_directive":
                processMyDirective(params, content);
                break;
            case "validated_directive":
                processValidatedDirective(params, content);
                break;
            default:
                throw new IllegalArgumentException("Unknown custom directive: " + directiveName);
        }
    }
    
    private void processMyDirective(Map<String, Object> params, Object content) {
        String param1 = (String) params.get("param1");
        String param2 = (String) params.get("param2");
        
        // Process custom logic
        System.out.println("Processing my_directive with params: " + param1 + ", " + param2);
    }
    
    private void processValidatedDirective(Map<String, Object> params, Object content) {
        // Process validated directive logic
        System.out.println("Processing validated_directive with validated params");
    }
}

@Service
public class ValidationService {
    
    public void validateParameter(String paramName, Object paramValue, String validationRule) {
        String[] rules = validationRule.split("\\|");
        
        for (String rule : rules) {
            switch (rule.trim()) {
                case "required":
                    if (paramValue == null || (paramValue instanceof String && ((String) paramValue).isEmpty())) {
                        throw new ValidationException("Parameter " + paramName + " is required");
                    }
                    break;
                case "string":
                    if (paramValue != null && !(paramValue instanceof String)) {
                        throw new ValidationException("Parameter " + paramName + " must be a string");
                    }
                    break;
                case "number":
                    if (paramValue != null && !(paramValue instanceof Number)) {
                        throw new ValidationException("Parameter " + paramName + " must be a number");
                    }
                    break;
                case "email":
                    if (paramValue != null && !isValidEmail((String) paramValue)) {
                        throw new ValidationException("Parameter " + paramName + " must be a valid email");
                    }
                    break;
            }
        }
    }
    
    private boolean isValidEmail(String email) {
        return email != null && email.matches("^[A-Za-z0-9+_.-]+@(.+)$");
    }
}
```

## Directive Chaining

```tusk
# Chained directives
#auth {
    #cache 300 {
        #api /chained {
            return @get_chained_data()
        }
    }
}

# Multiple directive types
#rate_limit 100 {
    #auth {
        #web /limited-page {
            @render("limited.html")
        }
    }
}

# Conditional chaining
#cache if: @is_production() {
    #auth if: @has_user() {
        #api /conditional-chained {
            return @get_conditional_data()
        }
    }
}
```

## Java Directive Chaining

```java
import org.springframework.stereotype.Service;
import java.util.Stack;
import java.util.Map;

@Service
public class DirectiveChainingService {
    
    private final Map<String, HashDirective> directives;
    private final Stack<DirectiveContext> directiveStack;
    
    public DirectiveChainingService(Map<String, HashDirective> directives) {
        this.directives = directives;
        this.directiveStack = new Stack<>();
    }
    
    public void processChainedDirectives(List<DirectiveChain> chains) {
        for (DirectiveChain chain : chains) {
            processDirectiveChain(chain);
        }
    }
    
    private void processDirectiveChain(DirectiveChain chain) {
        // Push directive context
        DirectiveContext context = new DirectiveContext(chain.getDirectiveName(), 
                                                       chain.getParams(), 
                                                       chain.getConfig());
        directiveStack.push(context);
        
        try {
            // Process directive
            HashDirective directive = directives.get(chain.getDirectiveName());
            if (directive != null) {
                directive.process(chain.getParams(), chain.getConfig(), chain.getContent());
            }
            
            // Process nested directives
            if (chain.getNestedChains() != null) {
                for (DirectiveChain nestedChain : chain.getNestedChains()) {
                    processDirectiveChain(nestedChain);
                }
            }
        } finally {
            // Pop directive context
            directiveStack.pop();
        }
    }
    
    public DirectiveContext getCurrentContext() {
        return directiveStack.isEmpty() ? null : directiveStack.peek();
    }
    
    public List<DirectiveContext> getContextStack() {
        return new ArrayList<>(directiveStack);
    }
}

@Data
@AllArgsConstructor
public class DirectiveChain {
    private String directiveName;
    private Map<String, Object> params;
    private Map<String, Object> config;
    private Object content;
    private List<DirectiveChain> nestedChains;
    private String condition;
}

@Data
@AllArgsConstructor
public class DirectiveContext {
    private String directiveName;
    private Map<String, Object> params;
    private Map<String, Object> config;
    private LocalDateTime timestamp;
    
    public DirectiveContext(String directiveName, Map<String, Object> params, Map<String, Object> config) {
        this.directiveName = directiveName;
        this.params = params;
        this.config = config;
        this.timestamp = LocalDateTime.now();
    }
}
```

## Directive Testing

```java
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.TestPropertySource;
import org.springframework.beans.factory.annotation.Autowired;

@SpringBootTest
@TestPropertySource(properties = {
    "tusk.directives.enabled-directives=api,web,cli,cron",
    "tusk.directives.global-config.debug=true"
})
public class HashDirectiveTest {
    
    @Autowired
    private HashDirectiveProcessor directiveProcessor;
    
    @MockBean
    private ApiService apiService;
    
    @MockBean
    private WebService webService;
    
    @Test
    public void testApiDirective() {
        Map<String, Object> params = new HashMap<>();
        params.put("route", "/api/test");
        params.put("method", "GET");
        
        Map<String, Object> config = new HashMap<>();
        config.put("auth", true);
        config.put("cache", true);
        
        directiveProcessor.processDirective("api", params, config, "test content");
        
        verify(apiService).registerEndpoint("/api/test", "GET", config, "test content");
    }
    
    @Test
    public void testWebDirective() {
        Map<String, Object> params = new HashMap<>();
        params.put("route", "/test-page");
        
        Map<String, Object> config = new HashMap<>();
        config.put("template", "test.html");
        
        directiveProcessor.processDirective("web", params, config, "test content");
        
        verify(webService).registerPage("/test-page", "test.html", config, "test content");
    }
    
    @Test
    public void testCustomDirective() {
        Map<String, Object> params = new HashMap<>();
        params.put("param1", "value1");
        params.put("param2", "value2");
        
        Map<String, Object> config = new HashMap<>();
        config.put("name", "my_directive");
        config.put("description", "Test directive");
        
        directiveProcessor.processDirective("custom_directive", params, config, "test content");
        
        // Verify custom directive processing
    }
    
    @Test
    public void testDirectiveValidation() {
        Map<String, Object> params = new HashMap<>();
        // Missing required parameter
        
        Map<String, Object> config = new HashMap<>();
        config.put("validation", Map.of("param1", "required|string"));
        
        assertThrows(ValidationException.class, () -> {
            directiveProcessor.processDirective("custom_directive", params, config, "test content");
        });
    }
}
```

## Configuration Properties

```yaml
# application.yml
tusk:
  directives:
    enabled-directives:
      - "api"
      - "web"
      - "cli"
      - "cron"
      - "auth"
      - "cache"
      - "middleware"
      - "rate_limit"
      - "custom_directive"
    
    global-config:
      debug: true
      log-level: "INFO"
      timeout: 30000
    
    definitions:
      api:
        name: "api"
        description: "REST API endpoint directive"
        supported-types: ["GET", "POST", "PUT", "DELETE", "PATCH"]
        required-params: ["route"]
        optional-params: ["method", "auth", "cache", "rate_limit"]
        default-config:
          method: "GET"
          auth: false
          cache: false
      
      web:
        name: "web"
        description: "Web page directive"
        supported-types: ["page", "template", "layout"]
        required-params: ["route"]
        optional-params: ["template", "layout", "cache"]
        default-config:
          template: "default.html"
          layout: "main.html"
      
      cli:
        name: "cli"
        description: "Command line interface directive"
        supported-types: ["command", "subcommand", "option"]
        required-params: ["command"]
        optional-params: ["description", "help"]
        default-config:
          help: true
      
      cron:
        name: "cron"
        description: "Scheduled task directive"
        supported-types: ["job", "task", "schedule"]
        required-params: ["schedule"]
        optional-params: ["timezone", "enabled"]
        default-config:
          timezone: "UTC"
          enabled: true
      
      custom_directive:
        name: "custom_directive"
        description: "Custom directive for specific logic"
        supported-types: ["custom", "validated", "logic"]
        required-params: ["name"]
        optional-params: ["description", "validation"]
        default-config:
          debug: false

spring:
  application:
    name: "tusk-directives"
```

## Summary

The `#hash` directive system in TuskLang provides comprehensive declarative programming capabilities for Java applications. With Spring Boot integration, flexible configuration, custom directives, and comprehensive testing support, you can implement sophisticated configuration-driven development that enhances your application's functionality.

Key features include:
- **Multiple directive types**: API, web, CLI, cron, auth, cache, middleware, rate limiting
- **Spring Boot integration**: Seamless integration with Spring Boot framework
- **Flexible configuration**: Configurable directives with parameters and validation
- **Custom directives**: Extensible directive system for custom logic
- **Directive chaining**: Support for nested and chained directives
- **Validation support**: Built-in parameter validation and error handling
- **Testing support**: Comprehensive testing utilities

The Java implementation provides enterprise-grade directive capabilities that integrate seamlessly with Spring Boot applications while maintaining the simplicity and power of TuskLang's declarative syntax. 