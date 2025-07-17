# Cross-File Communication in TuskLang for Java Applications

This guide covers comprehensive cross-file communication strategies for TuskLang in Java applications, including file imports, references, modular configuration, and advanced communication patterns.

## Table of Contents

- [Overview](#overview)
- [File Import System](#file-import-system)
- [Cross-File References](#cross-file-references)
- [Modular Configuration](#modular-configuration)
- [Environment-Specific Files](#environment-specific-files)
- [Dynamic File Loading](#dynamic-file-loading)
- [File Validation](#file-validation)
- [Performance Optimization](#performance-optimization)
- [Security Considerations](#security-considerations)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

Cross-file communication in TuskLang enables modular, maintainable, and scalable configuration management. This guide covers how to effectively use multiple configuration files in Java applications.

### Cross-File Communication Benefits

```java
// Benefits of cross-file communication
public enum CrossFileBenefit {
    MODULARITY,         // Separate concerns into different files
    REUSABILITY,        // Share configuration across applications
    MAINTAINABILITY,    // Easier to maintain and update
    SCALABILITY,        // Scale configuration as application grows
    ENVIRONMENT_ISOLATION, // Separate environment-specific configs
    TEAM_COLLABORATION  // Multiple teams can work on different configs
}
```

## File Import System

Use TuskLang's powerful import system to include and reference other configuration files.

### Basic File Import

```java
import com.tusklang.config.TuskLangConfig;
import com.tusklang.imports.FileImporter;

public class BasicFileImportExample {
    
    public void importConfiguration() {
        // Main configuration file
        String mainConfig = """
            # Import other configuration files
            @import("database.tsk")
            @import("security.tsk")
            @import("logging.tsk")
            
            [app]
            name = MyApplication
            version = 1.0.0
            """;
        
        // Create file importer
        FileImporter importer = new FileImporter();
        importer.setBasePath("/config");
        
        // Parse with imports
        TuskLangConfig config = importer.parseWithImports(mainConfig);
        
        // Access imported configuration
        String dbHost = config.getString("database.host");
        String jwtSecret = config.getString("security.jwt.secret");
        String logLevel = config.getString("logging.level");
        
        System.out.println("Database host: " + dbHost);
        System.out.println("JWT secret: " + jwtSecret);
        System.out.println("Log level: " + logLevel);
    }
}
```

### Advanced Import Options

```java
public class AdvancedImportExample {
    
    public void advancedImportConfiguration() {
        // Advanced import configuration
        String config = """
            # Import with options
            @import("database.tsk", {
                "priority": "high",
                "required": true,
                "cache": true
            })
            
            # Conditional import
            @import.if("monitoring.tsk", "ENABLE_MONITORING")
            
            # Environment-specific import
            @import.env("production.tsk", "production")
            
            # Import with namespace
            @import.as("external-api.tsk", "api")
            
            [app]
            name = MyApplication
            """;
        
        // Configure importer with advanced options
        FileImporter importer = new FileImporter();
        importer.setBasePath("/config");
        importer.setCacheEnabled(true);
        importer.setValidationEnabled(true);
        
        // Parse with advanced imports
        TuskLangConfig result = importer.parseWithImports(config);
        
        // Access namespaced configuration
        String apiUrl = result.getString("api.base_url");
        String apiKey = result.getString("api.key");
        
        System.out.println("API URL: " + apiUrl);
        System.out.println("API Key: " + apiKey);
    }
}
```

### Import Resolution

```java
public class ImportResolutionExample {
    
    public void resolveImports() {
        // Import resolution configuration
        ImportResolver resolver = new ImportResolver();
        
        // Set search paths
        resolver.addSearchPath("/config");
        resolver.addSearchPath("/config/environments");
        resolver.addSearchPath("/config/modules");
        
        // Set file extensions
        resolver.addFileExtension(".tsk");
        resolver.addFileExtension(".conf");
        
        // Configure resolution strategy
        resolver.setResolutionStrategy(ResolutionStrategy.FIRST_MATCH);
        resolver.setCaseSensitive(false);
        
        // Resolve imports
        String config = """
            @import("database")
            @import("security")
            """;
        
        List<ResolvedImport> resolvedImports = resolver.resolveImports(config);
        
        for (ResolvedImport import_ : resolvedImports) {
            System.out.println("Resolved: " + import_.getOriginalPath() + " -> " + import_.getResolvedPath());
        }
    }
}
```

## Cross-File References

Reference values and sections from other configuration files.

### Basic Cross-File References

```java
public class CrossFileReferenceExample {
    
    public void basicCrossFileReferences() {
        // Database configuration file (database.tsk)
        String databaseConfig = """
            [database]
            host = localhost
            port = 5432
            name = myapp
            username = user
            password = password
            """;
        
        // Application configuration file (app.tsk)
        String appConfig = """
            @import("database.tsk")
            
            [app]
            name = MyApplication
            database_url = postgresql://${database.host}:${database.port}/${database.name}
            """;
        
        // Parse with cross-file references
        FileImporter importer = new FileImporter();
        TuskLangConfig config = importer.parseWithImports(appConfig);
        
        // Access cross-file reference
        String databaseUrl = config.getString("app.database_url");
        System.out.println("Database URL: " + databaseUrl);
        // Output: postgresql://localhost:5432/myapp
    }
}
```

### Advanced Cross-File References

```java
public class AdvancedCrossFileReferenceExample {
    
    public void advancedCrossFileReferences() {
        // Security configuration (security.tsk)
        String securityConfig = """
            [security]
            jwt_secret = @env.secure(JWT_SECRET)
            encryption_key = @env.secure(ENCRYPTION_KEY)
            session_timeout = 3600
            """;
        
        // API configuration (api.tsk)
        String apiConfig = """
            @import("security.tsk")
            
            [api]
            base_url = https://api.example.com
            timeout = 5000
            retries = 3
            auth_header = Bearer ${security.jwt_secret}
            """;
        
        // Main application configuration (app.tsk)
        String mainConfig = """
            @import("api.tsk")
            @import("database.tsk")
            
            [app]
            name = MyApplication
            api_config = ${api}
            security_config = ${security}
            """;
        
        // Parse with advanced references
        FileImporter importer = new FileImporter();
        TuskLangConfig config = importer.parseWithImports(mainConfig);
        
        // Access nested cross-file references
        String authHeader = config.getString("app.api_config.auth_header");
        String jwtSecret = config.getString("app.security_config.jwt_secret");
        
        System.out.println("Auth header: " + authHeader);
        System.out.println("JWT secret: " + jwtSecret);
    }
}
```

### Reference Resolution

```java
public class ReferenceResolutionExample {
    
    public void resolveReferences() {
        // Reference resolver
        ReferenceResolver resolver = new ReferenceResolver();
        
        // Configure resolution options
        resolver.setAllowCircularReferences(false);
        resolver.setMaxReferenceDepth(10);
        resolver.setDefaultValueProvider(new DefaultValueProvider());
        
        // Resolve references
        String config = """
            @import("base.tsk")
            @import("override.tsk")
            
            [app]
            name = ${base.app_name}
            version = ${override.app_version}
            """;
        
        ResolutionResult result = resolver.resolveReferences(config);
        
        if (result.isSuccessful()) {
            System.out.println("References resolved successfully");
            System.out.println("Resolved config: " + result.getResolvedConfig());
        } else {
            System.out.println("Reference resolution failed: " + result.getError());
        }
    }
}
```

## Modular Configuration

Organize configuration into modular, reusable components.

### Configuration Modules

```java
public class ConfigurationModulesExample {
    
    public void useConfigurationModules() {
        // Database module (modules/database.tsk)
        String databaseModule = """
            [database]
            host = localhost
            port = 5432
            name = myapp
            username = user
            password = password
            
            [database.pool]
            min_size = 5
            max_size = 20
            timeout = 30000
            """;
        
        // Security module (modules/security.tsk)
        String securityModule = """
            [security]
            jwt_secret = @env.secure(JWT_SECRET)
            encryption_key = @env.secure(ENCRYPTION_KEY)
            
            [security.auth]
            session_timeout = 3600
            refresh_token_expiry = 604800
            """;
        
        // Monitoring module (modules/monitoring.tsk)
        String monitoringModule = """
            [monitoring]
            enabled = true
            metrics_endpoint = /metrics
            health_check_interval = 30
            
            [monitoring.logging]
            level = INFO
            format = JSON
            """;
        
        // Main application (app.tsk)
        String mainApp = """
            @import("modules/database.tsk")
            @import("modules/security.tsk")
            @import("modules/monitoring.tsk")
            
            [app]
            name = MyApplication
            version = 1.0.0
            """;
        
        // Load modular configuration
        ModularConfigLoader loader = new ModularConfigLoader();
        loader.setModulesPath("/config/modules");
        
        TuskLangConfig config = loader.loadModularConfig(mainApp);
        
        // Access module configurations
        String dbHost = config.getString("database.host");
        String jwtSecret = config.getString("security.jwt_secret");
        boolean monitoringEnabled = config.getBoolean("monitoring.enabled");
        
        System.out.println("Database host: " + dbHost);
        System.out.println("JWT secret: " + jwtSecret);
        System.out.println("Monitoring enabled: " + monitoringEnabled);
    }
}
```

### Module Management

```java
public class ModuleManagementExample {
    
    public void manageModules() {
        ModuleManager manager = new ModuleManager();
        
        // Register modules
        manager.registerModule("database", "/config/modules/database.tsk");
        manager.registerModule("security", "/config/modules/security.tsk");
        manager.registerModule("monitoring", "/config/modules/monitoring.tsk");
        
        // Set module dependencies
        manager.addDependency("security", "database");
        manager.addDependency("monitoring", "security");
        
        // Load modules with dependency resolution
        List<Module> modules = manager.loadModules(Arrays.asList("database", "security", "monitoring"));
        
        for (Module module : modules) {
            System.out.println("Loaded module: " + module.getName());
            System.out.println("Dependencies: " + module.getDependencies());
        }
    }
}
```

## Environment-Specific Files

Manage different configurations for different environments.

### Environment-Specific Configuration

```java
public class EnvironmentSpecificExample {
    
    public void loadEnvironmentSpecificConfig() {
        // Base configuration (base.tsk)
        String baseConfig = """
            [app]
            name = MyApplication
            version = 1.0.0
            
            [database]
            host = localhost
            port = 5432
            name = myapp
            """;
        
        // Development configuration (dev.tsk)
        String devConfig = """
            @import("base.tsk")
            
            [app]
            environment = development
            debug = true
            
            [database]
            host = localhost
            name = myapp_dev
            """;
        
        // Production configuration (prod.tsk)
        String prodConfig = """
            @import("base.tsk")
            
            [app]
            environment = production
            debug = false
            
            [database]
            host = prod-db.example.com
            name = myapp_prod
            username = ${DB_USERNAME}
            password = ${DB_PASSWORD}
            """;
        
        // Environment-aware loader
        EnvironmentAwareLoader loader = new EnvironmentAwareLoader();
        loader.setEnvironment("production");
        loader.setBasePath("/config/environments");
        
        TuskLangConfig config = loader.loadEnvironmentConfig();
        
        System.out.println("Environment: " + config.getString("app.environment"));
        System.out.println("Database host: " + config.getString("database.host"));
        System.out.println("Debug mode: " + config.getBoolean("app.debug"));
    }
}
```

### Environment Detection

```java
public class EnvironmentDetectionExample {
    
    public void detectEnvironment() {
        EnvironmentDetector detector = new EnvironmentDetector();
        
        // Configure detection sources
        detector.addDetectionSource(DetectionSource.ENVIRONMENT_VARIABLE, "NODE_ENV");
        detector.addDetectionSource(DetectionSource.SYSTEM_PROPERTY, "spring.profiles.active");
        detector.addDetectionSource(DetectionSource.FILE, "/etc/environment");
        
        // Detect environment
        String environment = detector.detectEnvironment();
        System.out.println("Detected environment: " + environment);
        
        // Load appropriate configuration
        EnvironmentAwareLoader loader = new EnvironmentAwareLoader();
        loader.setEnvironment(environment);
        
        TuskLangConfig config = loader.loadEnvironmentConfig();
        System.out.println("Loaded configuration for: " + environment);
    }
}
```

## Dynamic File Loading

Load configuration files dynamically based on runtime conditions.

### Dynamic Loading

```java
public class DynamicLoadingExample {
    
    public void loadConfigurationDynamically() {
        DynamicConfigLoader loader = new DynamicConfigLoader();
        
        // Configure dynamic loading
        loader.setReloadEnabled(true);
        loader.setReloadInterval(30); // 30 seconds
        loader.setFileWatcherEnabled(true);
        
        // Add dynamic import conditions
        loader.addConditionalImport("monitoring.tsk", () -> {
            return System.getProperty("ENABLE_MONITORING", "false").equals("true");
        });
        
        loader.addConditionalImport("debug.tsk", () -> {
            return System.getProperty("DEBUG_MODE", "false").equals("true");
        });
        
        // Load configuration
        TuskLangConfig config = loader.loadDynamicConfig();
        
        // Register change listener
        loader.addChangeListener((changedFiles) -> {
            System.out.println("Configuration files changed: " + changedFiles);
            reloadConfiguration();
        });
        
        // Start dynamic loading
        loader.start();
    }
    
    private void reloadConfiguration() {
        System.out.println("Reloading configuration...");
        // Reload configuration logic
    }
}
```

### Conditional Loading

```java
public class ConditionalLoadingExample {
    
    public void conditionalLoading() {
        ConditionalLoader loader = new ConditionalLoader();
        
        // Add loading conditions
        loader.addCondition("feature-flags.tsk", new FeatureFlagCondition("ENABLE_FEATURE_FLAGS"));
        loader.addCondition("experimental.tsk", new ExperimentalFeatureCondition());
        loader.addCondition("beta.tsk", new BetaFeatureCondition());
        
        // Load configuration with conditions
        TuskLangConfig config = loader.loadConditionalConfig();
        
        // Check which files were loaded
        List<String> loadedFiles = loader.getLoadedFiles();
        System.out.println("Loaded files: " + loadedFiles);
    }
}

class FeatureFlagCondition implements LoadingCondition {
    private final String flagName;
    
    public FeatureFlagCondition(String flagName) {
        this.flagName = flagName;
    }
    
    @Override
    public boolean shouldLoad() {
        return System.getProperty(flagName, "false").equals("true");
    }
}
```

## File Validation

Validate cross-file configuration for consistency and correctness.

### Cross-File Validation

```java
public class CrossFileValidationExample {
    
    public void validateCrossFileConfiguration() {
        CrossFileValidator validator = new CrossFileValidator();
        
        // Add validation rules
        validator.addRule(new ReferenceValidationRule());
        validator.addRule(new CircularReferenceRule());
        validator.addRule(new RequiredFileRule(Arrays.asList("database.tsk", "security.tsk")));
        validator.addRule(new EnvironmentConsistencyRule());
        
        // Validate configuration
        ValidationResult result = validator.validate("/config");
        
        if (result.isValid()) {
            System.out.println("Cross-file validation passed");
        } else {
            System.out.println("Cross-file validation failed:");
            for (ValidationError error : result.getErrors()) {
                System.out.println("- " + error.getMessage());
                System.out.println("  File: " + error.getFile());
                System.out.println("  Line: " + error.getLine());
            }
        }
    }
}
```

### Dependency Validation

```java
public class DependencyValidationExample {
    
    public void validateDependencies() {
        DependencyValidator validator = new DependencyValidator();
        
        // Configure validation
        validator.setCheckCircularDependencies(true);
        validator.setCheckMissingDependencies(true);
        validator.setCheckVersionCompatibility(true);
        
        // Validate dependencies
        DependencyValidationResult result = validator.validate("/config");
        
        if (result.hasCircularDependencies()) {
            System.out.println("Circular dependencies detected:");
            for (CircularDependency dep : result.getCircularDependencies()) {
                System.out.println("- " + dep.getFiles());
            }
        }
        
        if (result.hasMissingDependencies()) {
            System.out.println("Missing dependencies:");
            for (MissingDependency dep : result.getMissingDependencies()) {
                System.out.println("- " + dep.getFile() + " requires " + dep.getDependency());
            }
        }
    }
}
```

## Performance Optimization

Optimize cross-file communication for better performance.

### Caching Strategies

```java
public class CachingStrategyExample {
    
    public void implementCaching() {
        ConfigCache cache = new ConfigCache();
        
        // Configure cache
        cache.setMaxSize(100);
        cache.setExpirationTime(300); // 5 minutes
        cache.setEvictionPolicy(EvictionPolicy.LRU);
        
        // Cache-aware loader
        CachedConfigLoader loader = new CachedConfigLoader();
        loader.setCache(cache);
        loader.setCacheEnabled(true);
        
        // Load configuration with caching
        TuskLangConfig config = loader.loadCachedConfig("/config/app.tsk");
        
        // Check cache statistics
        CacheStatistics stats = cache.getStatistics();
        System.out.println("Cache hits: " + stats.getHits());
        System.out.println("Cache misses: " + stats.getMisses());
        System.out.println("Cache size: " + stats.getSize());
    }
}
```

### Lazy Loading

```java
public class LazyLoadingExample {
    
    public void implementLazyLoading() {
        LazyConfigLoader loader = new LazyConfigLoader();
        
        // Configure lazy loading
        loader.setLazyLoadingEnabled(true);
        loader.setPreloadCriticalFiles(true);
        loader.setBackgroundLoadingEnabled(true);
        
        // Load configuration lazily
        TuskLangConfig config = loader.loadLazyConfig("/config");
        
        // Access configuration (loaded on demand)
        String dbHost = config.getString("database.host"); // Triggers lazy loading
        
        // Preload specific files
        loader.preloadFiles(Arrays.asList("security.tsk", "monitoring.tsk"));
    }
}
```

## Security Considerations

Implement security measures for cross-file communication.

### File Access Control

```java
public class FileAccessControlExample {
    
    public void implementAccessControl() {
        FileAccessController controller = new FileAccessController();
        
        // Configure access control
        controller.setRestrictedPaths(Arrays.asList("/config/secrets", "/config/production"));
        controller.setAllowedExtensions(Arrays.asList(".tsk", ".conf"));
        controller.setRequireAuthentication(true);
        
        // Add security rules
        controller.addRule(new FilePermissionRule());
        controller.addRule(new ContentValidationRule());
        controller.addRule(new EncryptionRule());
        
        // Secure loader
        SecureConfigLoader loader = new SecureConfigLoader();
        loader.setAccessController(controller);
        
        // Load configuration securely
        try {
            TuskLangConfig config = loader.loadSecureConfig("/config");
            System.out.println("Configuration loaded securely");
        } catch (SecurityException e) {
            System.err.println("Access denied: " + e.getMessage());
        }
    }
}
```

### Content Validation

```java
public class ContentValidationExample {
    
    public void validateContent() {
        ContentValidator validator = new ContentValidator();
        
        // Add validation rules
        validator.addRule(new SensitiveDataRule());
        validator.addRule(new MaliciousContentRule());
        validator.addRule(new FormatValidationRule());
        
        // Validate file content
        ValidationResult result = validator.validateFile("/config/app.tsk");
        
        if (result.isValid()) {
            System.out.println("Content validation passed");
        } else {
            System.out.println("Content validation failed:");
            for (ValidationError error : result.getErrors()) {
                System.out.println("- " + error.getMessage());
            }
        }
    }
}
```

## Best Practices

Follow these best practices for effective cross-file communication.

### File Organization

```java
public class FileOrganizationExample {
    
    public void organizeFiles() {
        ConfigFileOrganizer organizer = new ConfigFileOrganizer();
        
        // Define organization structure
        organizer.setStructure(new FileStructure()
            .addDirectory("base", "Base configuration files")
            .addDirectory("environments", "Environment-specific files")
            .addDirectory("modules", "Reusable modules")
            .addDirectory("secrets", "Secret configuration files")
            .addDirectory("templates", "Configuration templates"));
        
        // Organize files
        organizer.organize("/config");
        
        // Validate organization
        OrganizationValidationResult result = organizer.validateOrganization();
        
        if (result.isValid()) {
            System.out.println("File organization is valid");
        } else {
            System.out.println("Organization issues found:");
            for (OrganizationIssue issue : result.getIssues()) {
                System.out.println("- " + issue.getDescription());
            }
        }
    }
}
```

### Naming Conventions

```java
public class NamingConventionsExample {
    
    public void applyNamingConventions() {
        NamingConventionEnforcer enforcer = new NamingConventionEnforcer();
        
        // Define naming conventions
        enforcer.setFileNamingPattern("^[a-z][a-z0-9_-]*\\.tsk$");
        enforcer.setSectionNamingPattern("^[a-z][a-z0-9_-]*$");
        enforcer.setKeyNamingPattern("^[a-z][a-z0-9_-]*$");
        
        // Enforce conventions
        List<NamingViolation> violations = enforcer.enforceConventions("/config");
        
        if (violations.isEmpty()) {
            System.out.println("All files follow naming conventions");
        } else {
            System.out.println("Naming convention violations:");
            for (NamingViolation violation : violations) {
                System.out.println("- " + violation.getFile() + ": " + violation.getDescription());
            }
        }
    }
}
```

## Troubleshooting

Common issues and solutions for cross-file communication.

### Import Issues

```java
public class ImportTroubleshootingExample {
    
    public void troubleshootImports() {
        ImportTroubleshooter troubleshooter = new ImportTroubleshooter();
        
        // Check import issues
        List<ImportIssue> issues = troubleshooter.checkImports("/config");
        
        for (ImportIssue issue : issues) {
            System.out.println("Import issue: " + issue.getType());
            System.out.println("File: " + issue.getFile());
            System.out.println("Description: " + issue.getDescription());
            System.out.println("Solution: " + issue.getSolution());
        }
        
        // Fix common issues
        troubleshooter.fixCommonIssues("/config");
    }
}
```

### Reference Issues

```java
public class ReferenceTroubleshootingExample {
    
    public void troubleshootReferences() {
        ReferenceTroubleshooter troubleshooter = new ReferenceTroubleshooter();
        
        // Check reference issues
        List<ReferenceIssue> issues = troubleshooter.checkReferences("/config");
        
        for (ReferenceIssue issue : issues) {
            System.out.println("Reference issue: " + issue.getType());
            System.out.println("File: " + issue.getFile());
            System.out.println("Reference: " + issue.getReference());
            System.out.println("Description: " + issue.getDescription());
        }
        
        // Resolve issues
        troubleshooter.resolveIssues("/config");
    }
}
```

This comprehensive cross-file communication guide provides everything needed to effectively manage multiple configuration files in TuskLang Java applications, ensuring modular, maintainable, and scalable configuration management. 