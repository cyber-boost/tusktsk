# Plugin System in TuskLang

TuskLang's plugin system allows you to extend the language and framework with reusable packages, creating a modular and extensible architecture for your applications.

## Creating Plugins

```tusk
# Basic plugin structure
class MyPlugin extends Plugin {
    # Plugin metadata
    name: "my-plugin"
    version: "1.0.0"
    description: "A sample TuskLang plugin"
    author: "Your Name"
    
    # Plugin dependencies
    requires: [
        "tusk-core >= 2.0",
        "another-plugin >= 1.5"
    ]
    
    # Plugin configuration schema
    config_schema: {
        api_key: {
            type: "string",
            required: true,
            description: "API key for external service"
        },
        cache_ttl: {
            type: "integer",
            default: 3600,
            description: "Cache time-to-live in seconds"
        },
        features: {
            type: "object",
            properties: {
                advanced_mode: {type: "boolean", default: false},
                max_retries: {type: "integer", default: 3}
            }
        }
    }
    
    # Initialize plugin
    install(app, config) {
        this.app: app
        this.config: @validate_config(config)
        
        # Register services
        @register_services()
        
        # Register middleware
        @register_middleware()
        
        # Register commands
        @register_commands()
        
        # Hook into lifecycle
        @register_hooks()
    }
    
    # Clean up on uninstall
    uninstall() {
        @cleanup_resources()
        @remove_hooks()
    }
}

# Register plugin
@app.use(MyPlugin, {
    api_key: @env("MY_PLUGIN_API_KEY"),
    cache_ttl: 7200,
    features: {
        advanced_mode: true
    }
})
```

## Service Provider Pattern

```tusk
# Advanced plugin with service provider
class AuthenticationPlugin extends Plugin {
    name: "auth-plugin"
    
    # Register services in the container
    register() {
        # Bind interface to implementation
        @app.bind("Auth", () => {
            return new AuthManager({
                driver: this.config.driver || "jwt",
                secret: this.config.secret
            })
        })
        
        # Singleton binding
        @app.singleton("AuthGuard", () => {
            return new AuthGuard(@app.make("Auth"))
        })
        
        # Tagged bindings
        @app.tag([JwtDriver, SessionDriver], "auth.drivers")
    }
    
    # Boot services after all plugins registered
    boot() {
        auth: @app.make("Auth")
        
        # Add custom guards
        auth.extend("api", () => {
            return new ApiGuard(this.config.api)
        })
        
        # Add macros
        @Request.macro("user", function() {
            return auth.user(this)
        })
        
        # Register middleware
        @app.middleware.register("auth", AuthMiddleware)
        @app.middleware.register("auth.api", ApiAuthMiddleware)
    }
    
    # Provide plugin API
    provides() {
        return {
            authenticate: (credentials) => {
                return @app.make("Auth").attempt(credentials)
            },
            
            check: () => {
                return @app.make("Auth").check()
            },
            
            guard: (name) => {
                return @app.make("Auth").guard(name)
            }
        }
    }
}
```

## Plugin Hooks and Events

```tusk
# Plugin with extensive hook system
class CachePlugin extends Plugin {
    name: "cache-plugin"
    
    install(app, config) {
        this.cache_manager: new CacheManager(config)
        
        # Hook into request lifecycle
        app.hook("request.before", async (request) => {
            if (this.should_cache(request)) {
                cached: await this.get_cached_response(request)
                if (cached) {
                    return cached  # Short-circuit request
                }
            }
        })
        
        app.hook("response.after", async (request, response) => {
            if (this.should_cache(request) && response.status === 200) {
                await this.cache_response(request, response)
            }
        })
        
        # Database query hooks
        app.hook("db.query.before", (query) => {
            if (query.cache_key) {
                cached: this.cache_manager.get(query.cache_key)
                if (cached) {
                    query.skip: true
                    query.result: cached
                }
            }
        })
        
        app.hook("db.query.after", (query, result) => {
            if (query.cache_key && !query.skip) {
                this.cache_manager.put(query.cache_key, result, query.cache_ttl)
            }
        })
        
        # Custom plugin events
        app.on("cache.clear", (tags) => {
            this.cache_manager.tags(tags).flush()
        })
        
        app.on("cache.warm", async (keys) => {
            for (key of keys) {
                await this.warm_cache(key)
            }
        })
    }
    
    # Define available hooks for other plugins
    hooks() {
        return {
            "cache.before.get": "Before retrieving from cache",
            "cache.after.get": "After retrieving from cache",
            "cache.before.put": "Before storing in cache",
            "cache.after.put": "After storing in cache",
            "cache.before.flush": "Before flushing cache",
            "cache.after.flush": "After flushing cache"
        }
    }
}
```

## Plugin CLI Commands

```tusk
# Plugin that adds CLI commands
class MigrationPlugin extends Plugin {
    name: "migration-plugin"
    
    register_commands() {
        # Register main command
        @app.command("migrate", {
            description: "Run database migrations",
            options: {
                "--force": "Force run in production",
                "--step": "Number of migrations to run",
                "--rollback": "Rollback migrations"
            },
            handler: this.handle_migrate.bind(this)
        })
        
        # Register sub-commands
        @app.command("make:migration", {
            description: "Create a new migration",
            arguments: {
                name: "Migration name"
            },
            handler: this.handle_make_migration.bind(this)
        })
        
        # Command with interactive prompts
        @app.command("migrate:fresh", {
            description: "Drop all tables and re-run migrations",
            handler: async (args, options) => {
                if (@app.env === "production" && !options.force) {
                    confirm: await @prompt.confirm(
                        "This will delete all data. Are you sure?"
                    )
                    
                    if (!confirm) {
                        @console.error("Migration cancelled")
                        return 1
                    }
                }
                
                await this.fresh_migrate()
                return 0
            }
        })
    }
    
    async handle_migrate(args, options) {
        @console.info("Running migrations...")
        
        if (options.rollback) {
            await this.rollback(options.step || 1)
        } else {
            await this.migrate(options.step)
        }
        
        @console.success("Migrations complete")
    }
}
```

## Plugin Middleware

```tusk
# Plugin providing middleware
class SecurityPlugin extends Plugin {
    name: "security-plugin"
    
    middleware() {
        return {
            # CORS middleware
            cors: (options = {}) => {
                return async (request, next) => {
                    origin: request.headers.origin
                    allowed: options.origins || ["*"]
                    
                    if (allowed[0] === "*" || allowed.includes(origin)) {
                        @response.headers["Access-Control-Allow-Origin"]: origin
                        @response.headers["Access-Control-Allow-Methods"]: 
                            options.methods || "GET,POST,PUT,DELETE,OPTIONS"
                        @response.headers["Access-Control-Allow-Headers"]: 
                            options.headers || "Content-Type,Authorization"
                    }
                    
                    if (request.method === "OPTIONS") {
                        return @response.status(204)
                    }
                    
                    return await next()
                }
            },
            
            # Rate limiting middleware
            throttle: (requests = 60, minutes = 1) => {
                limits: new Map()
                
                return async (request, next) => {
                    key: options.by || request.ip
                    limit_key: `throttle:${key}`
                    
                    current: limits.get(limit_key) || {count: 0, reset: Date.now() + minutes * 60000}
                    
                    if (Date.now() > current.reset) {
                        current: {count: 0, reset: Date.now() + minutes * 60000}
                    }
                    
                    if (current.count >= requests) {
                        return @response.status(429).json({
                            error: "Too many requests"
                        })
                    }
                    
                    current.count++
                    limits.set(limit_key, current)
                    
                    return await next()
                }
            },
            
            # Security headers
            secure: () => {
                return async (request, next) => {
                    response: await next()
                    
                    response.headers["X-Content-Type-Options"]: "nosniff"
                    response.headers["X-Frame-Options"]: "DENY"
                    response.headers["X-XSS-Protection"]: "1; mode=block"
                    response.headers["Referrer-Policy"]: "strict-origin-when-cross-origin"
                    
                    if (request.secure) {
                        response.headers["Strict-Transport-Security"]: 
                            "max-age=31536000; includeSubDomains"
                    }
                    
                    return response
                }
            }
        }
    }
}
```

## Plugin Assets and Resources

```tusk
# Plugin with assets and views
class UIPlugin extends Plugin {
    name: "ui-plugin"
    
    install(app, config) {
        # Register view paths
        @app.view.addPath(@plugin_path("views"))
        
        # Register asset publishing
        @app.assets.publish({
            from: @plugin_path("assets"),
            to: "plugins/ui",
            files: ["css/**", "js/**", "images/**"]
        })
        
        # Register components
        @app.view.component("ui-button", @plugin_path("components/button.tusk"))
        @app.view.component("ui-modal", @plugin_path("components/modal.tusk"))
        
        # Add global view data
        @app.view.share({
            ui_version: this.version,
            ui_config: config.ui || {}
        })
        
        # Register routes for plugin UI
        @app.router.group({
            prefix: "/admin/ui",
            middleware: ["auth", "admin"]
        }, (router) => {
            router.get("/", this.show_dashboard)
            router.get("/components", this.show_components)
            router.post("/settings", this.update_settings)
        })
    }
    
    # Helper to get plugin paths
    plugin_path(relative = "") {
        return @path.join(@plugins_path(), this.name, relative)
    }
}
```

## Plugin Testing

```tusk
# Testable plugin structure
class TestablePlugin extends Plugin {
    name: "testable-plugin"
    
    # Provide test helpers
    testing() {
        return {
            # Mock services
            mock: (service, implementation) => {
                @app.instance(service, implementation)
            },
            
            # Test data factories
            factory: (model) => {
                return this.factories[model] || null
            },
            
            # Assertions
            assertions: {
                assertPluginLoaded: (name) => {
                    loaded: @app.plugins.has(name)
                    @assert.true(loaded, `Plugin ${name} not loaded`)
                },
                
                assertServiceBound: (service) => {
                    bound: @app.bound(service)
                    @assert.true(bound, `Service ${service} not bound`)
                }
            }
        }
    }
}

# Plugin test
#test "Plugin loads correctly" {
    # Create test app
    app: @create_test_app()
    
    # Load plugin with test config
    app.use(TestablePlugin, {
        test_mode: true
    })
    
    # Get testing helpers
    plugin_test: app.plugin("testable-plugin").testing()
    
    # Test plugin loaded
    plugin_test.assertions.assertPluginLoaded("testable-plugin")
    
    # Test services registered
    plugin_test.assertions.assertServiceBound("TestService")
    
    # Mock external service
    plugin_test.mock("ExternalApi", {
        fetch: async () => ({data: "test"})
    })
    
    # Test functionality
    result: await app.make("TestService").process()
    @assert.equals(result.data, "test")
}
```

## Plugin Distribution

```tusk
# Plugin package.tsk file
{
    name: "awesome-plugin",
    version: "2.1.0",
    description: "An awesome TuskLang plugin",
    keywords: ["tusk", "plugin", "awesome"],
    
    # Plugin metadata
    plugin: {
        class: "AwesomePlugin",
        category: "utilities",
        compatibility: "^3.0"
    },
    
    # Dependencies
    dependencies: {
        "tusk-framework": "^3.0",
        "another-package": "^1.2"
    },
    
    # Dev dependencies
    devDependencies: {
        "tusk-test": "^3.0"
    },
    
    # Auto-discovery
    extra: {
        tusk: {
            providers: [
                "AwesomePlugin"
            ],
            aliases: {
                "Awesome": "AwesomeFacade"
            }
        }
    },
    
    # Scripts
    scripts: {
        test: "tusk test",
        "post-install": "tusk plugin:publish --tag=assets"
    }
}

# Publishing plugin
@console.command("plugin:publish", async () => {
    # Validate plugin
    validation: await @validate_plugin()
    if (!validation.valid) {
        @console.error("Plugin validation failed:", validation.errors)
        return 1
    }
    
    # Build plugin
    await @build_plugin({
        minify: true,
        sourcemaps: false
    })
    
    # Publish to registry
    await @publish_to_registry({
        registry: @config.plugin_registry,
        token: @env("PLUGIN_REGISTRY_TOKEN")
    })
    
    @console.success("Plugin published successfully!")
})
```

## Plugin Best Practices

```tusk
# Well-structured plugin
class BestPracticePlugin extends Plugin {
    name: "best-practice"
    
    # Version compatibility
    compatible_versions: ["3.0", "3.1", "3.2"]
    
    # Feature detection
    install(app, config) {
        # Check for required features
        if (!app.has_feature("async_hooks")) {
            throw "This plugin requires async_hooks feature"
        }
        
        # Graceful degradation
        if (app.has_feature("advanced_cache")) {
            this.cache_strategy: "advanced"
        } else {
            this.cache_strategy: "basic"
        }
        
        # Namespace isolation
        this.namespace: `plugin:${this.name}:`
        
        # Error boundaries
        this.wrap_errors(() => {
            this.initialize_services()
        })
    }
    
    # Provide upgrade path
    upgrade(from_version, to_version) {
        if (from_version < "2.0" && to_version >= "2.0") {
            # Migrate configuration
            @migrate_v1_config_to_v2()
        }
    }
    
    # Clean uninstall
    uninstall() {
        # Remove all plugin data
        @cache.forget_pattern(this.namespace + "*")
        @db.table("plugin_data").where("plugin", this.name).delete()
        
        # Unregister all services
        for (service of this.registered_services) {
            @app.forget(service)
        }
        
        # Remove event listeners
        @app.off(this.event_listeners)
    }
    
    # Debugging support
    debug_info() {
        return {
            version: this.version,
            config: @sanitize_config(this.config),
            services: this.registered_services,
            stats: this.get_usage_stats()
        }
    }
}
```

## Best Practices

1. **Follow naming conventions** - Use clear, descriptive names
2. **Version your plugins** - Use semantic versioning
3. **Document configuration** - Provide schema and examples
4. **Handle errors gracefully** - Don't crash the host app
5. **Clean up on uninstall** - Remove all traces
6. **Test thoroughly** - Include unit and integration tests
7. **Minimize dependencies** - Keep plugins lightweight
8. **Provide migration paths** - Support upgrades

## Related Topics

- `dependency-injection` - Service container
- `event-system` - Event handling
- `middleware` - Middleware system
- `package-management` - Package distribution
- `testing-plugins` - Plugin testing