# Micro-Frontends in C# with TuskLang

## Overview

Micro-Frontends is an architectural pattern that extends the microservices concept to frontend development. This guide covers how to implement micro-frontends using C# and TuskLang configuration for building scalable, maintainable web applications.

## Table of Contents

- [Micro-Frontends Concepts](#micro-frontends-concepts)
- [TuskLang Micro-Frontends Configuration](#tusklang-micro-frontends-configuration)
- [Shell Application](#shell-application)
- [C# Micro-Frontend Example](#c-micro-frontend-example)
- [Module Federation](#module-federation)
- [Routing and Navigation](#routing-and-navigation)
- [Best Practices](#best-practices)

## Micro-Frontends Concepts

- **Shell Application**: The main application that hosts micro-frontends
- **Micro-Frontends**: Independent frontend applications
- **Module Federation**: Webpack 5 feature for sharing modules between applications
- **Shared Dependencies**: Common libraries and components
- **Independent Deployment**: Each micro-frontend can be deployed separately

## TuskLang Micro-Frontends Configuration

```ini
# micro-frontends.tsk
[micro_frontends]
enabled = @env("MICRO_FRONTENDS_ENABLED", "true")
shell_app_url = @env("SHELL_APP_URL", "http://localhost:3000")
module_federation_enabled = @env("MODULE_FEDERATION_ENABLED", "true")

[shell_application]
name = @env("SHELL_APP_NAME", "MainApp")
port = @env("SHELL_APP_PORT", "3000")
routing_enabled = @env("SHELL_ROUTING_ENABLED", "true")

[micro_frontends.user_management]
name = @env("USER_MANAGEMENT_MF", "UserManagement")
url = @env("USER_MANAGEMENT_URL", "http://localhost:3001")
scope = @env("USER_MANAGEMENT_SCOPE", "userManagement")
module = @env("USER_MANAGEMENT_MODULE", "UserApp")
enabled = @env("USER_MANAGEMENT_ENABLED", "true")

[micro_frontends.order_management]
name = @env("ORDER_MANAGEMENT_MF", "OrderManagement")
url = @env("ORDER_MANAGEMENT_URL", "http://localhost:3002")
scope = @env("ORDER_MANAGEMENT_SCOPE", "orderManagement")
module = @env("ORDER_MANAGEMENT_MODULE", "OrderApp")
enabled = @env("ORDER_MANAGEMENT_ENABLED", "true")

[micro_frontends.product_catalog]
name = @env("PRODUCT_CATALOG_MF", "ProductCatalog")
url = @env("PRODUCT_CATALOG_URL", "http://localhost:3003")
scope = @env("PRODUCT_CATALOG_SCOPE", "productCatalog")
module = @env("PRODUCT_CATALOG_MODULE", "ProductApp")
enabled = @env("PRODUCT_CATALOG_ENABLED", "true")

[shared_dependencies]
react_version = @env("SHARED_REACT_VERSION", "18.2.0")
react_router_version = @env("SHARED_REACT_ROUTER_VERSION", "6.8.0")
material_ui_version = @env("SHARED_MATERIAL_UI_VERSION", "5.11.0")

[communication]
event_bus_enabled = @env("EVENT_BUS_ENABLED", "true")
shared_state_enabled = @env("SHARED_STATE_ENABLED", "true")
api_gateway_enabled = @env("API_GATEWAY_ENABLED", "true")
```

## Shell Application

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TuskLang;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Load TuskLang configuration
        var tuskConfig = TuskConfig.Load("micro-frontends.tsk");

        services.AddMicroFrontends(tuskConfig);
        services.AddModuleFederation(tuskConfig);
        services.AddSharedState(tuskConfig);
        services.AddEventBus(tuskConfig);

        services.AddControllers();
        services.AddRazorPages();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            
            endpoints.MapRazorPages();
        });
    }
}

public static class MicroFrontendsServiceCollectionExtensions
{
    public static IServiceCollection AddMicroFrontends(
        this IServiceCollection services,
        TuskConfig config)
    {
        if (!bool.Parse(config["micro_frontends:enabled"]))
            return services;

        // Register micro-frontend configurations
        services.Configure<MicroFrontendOptions>(options =>
        {
            options.ShellAppUrl = config["shell_application:url"];
            options.ShellAppName = config["shell_application:name"];
            options.ModuleFederationEnabled = bool.Parse(config["micro_frontends:module_federation_enabled"]);
        });

        // Register micro-frontend services
        services.AddScoped<IMicroFrontendLoader, MicroFrontendLoader>();
        services.AddScoped<IMicroFrontendRouter, MicroFrontendRouter>();
        services.AddScoped<ISharedStateManager, SharedStateManager>();

        return services;
    }
}

public class MicroFrontendOptions
{
    public string ShellAppUrl { get; set; }
    public string ShellAppName { get; set; }
    public bool ModuleFederationEnabled { get; set; }
    public List<MicroFrontendConfig> MicroFrontends { get; set; } = new();
}

public class MicroFrontendConfig
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Scope { get; set; }
    public string Module { get; set; }
    public bool Enabled { get; set; }
}
```

## C# Micro-Frontend Example

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TuskLang;

[ApiController]
[Route("api/[controller]")]
public class MicroFrontendController : ControllerBase
{
    private readonly IMicroFrontendLoader _loader;
    private readonly IMicroFrontendRouter _router;
    private readonly IConfiguration _config;
    private readonly ILogger<MicroFrontendController> _logger;

    public MicroFrontendController(
        IMicroFrontendLoader loader,
        IMicroFrontendRouter router,
        IConfiguration config,
        ILogger<MicroFrontendController> logger)
    {
        _loader = loader;
        _router = router;
        _config = config;
        _logger = logger;
    }

    [HttpGet("load/{microFrontendName}")]
    public async Task<IActionResult> LoadMicroFrontend(string microFrontendName)
    {
        try
        {
            var microFrontend = await _loader.LoadAsync(microFrontendName);
            
            if (microFrontend == null)
                return NotFound($"Micro-frontend {microFrontendName} not found");

            return Ok(new
            {
                Name = microFrontend.Name,
                Url = microFrontend.Url,
                Scope = microFrontend.Scope,
                Module = microFrontend.Module,
                Enabled = microFrontend.Enabled
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading micro-frontend {Name}", microFrontendName);
            return StatusCode(500, "Error loading micro-frontend");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListMicroFrontends()
    {
        try
        {
            var microFrontends = await _loader.GetAllAsync();
            return Ok(microFrontends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing micro-frontends");
            return StatusCode(500, "Error listing micro-frontends");
        }
    }

    [HttpPost("route")]
    public async Task<IActionResult> RouteToMicroFrontend([FromBody] RouteRequest request)
    {
        try
        {
            var route = await _router.RouteAsync(request.Path, request.UserId);
            return Ok(route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing to micro-frontend");
            return StatusCode(500, "Error routing to micro-frontend");
        }
    }
}

public interface IMicroFrontendLoader
{
    Task<MicroFrontendConfig> LoadAsync(string name);
    Task<List<MicroFrontendConfig>> GetAllAsync();
    Task<bool> IsAvailableAsync(string name);
}

public class MicroFrontendLoader : IMicroFrontendLoader
{
    private readonly IConfiguration _config;
    private readonly ILogger<MicroFrontendLoader> _logger;
    private readonly Dictionary<string, MicroFrontendConfig> _microFrontends;

    public MicroFrontendLoader(IConfiguration config, ILogger<MicroFrontendLoader> logger)
    {
        _config = config;
        _logger = logger;
        _microFrontends = LoadMicroFrontendConfigs();
    }

    public async Task<MicroFrontendConfig> LoadAsync(string name)
    {
        if (_microFrontends.TryGetValue(name, out var config))
        {
            if (!config.Enabled)
            {
                _logger.LogWarning("Micro-frontend {Name} is disabled", name);
                return null;
            }

            // Check if micro-frontend is available
            if (await IsAvailableAsync(name))
            {
                return config;
            }
            else
            {
                _logger.LogWarning("Micro-frontend {Name} is not available", name);
                return null;
            }
        }

        return null;
    }

    public async Task<List<MicroFrontendConfig>> GetAllAsync()
    {
        var availableMicroFrontends = new List<MicroFrontendConfig>();

        foreach (var config in _microFrontends.Values)
        {
            if (config.Enabled && await IsAvailableAsync(config.Name))
            {
                availableMicroFrontends.Add(config);
            }
        }

        return availableMicroFrontends;
    }

    public async Task<bool> IsAvailableAsync(string name)
    {
        if (!_microFrontends.TryGetValue(name, out var config))
            return false;

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{config.Url}/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Micro-frontend {Name} health check failed", name);
            return false;
        }
    }

    private Dictionary<string, MicroFrontendConfig> LoadMicroFrontendConfigs()
    {
        var configs = new Dictionary<string, MicroFrontendConfig>();

        // Load user management micro-frontend
        if (bool.Parse(_config["micro_frontends:user_management:enabled"]))
        {
            configs["userManagement"] = new MicroFrontendConfig
            {
                Name = _config["micro_frontends:user_management:name"],
                Url = _config["micro_frontends:user_management:url"],
                Scope = _config["micro_frontends:user_management:scope"],
                Module = _config["micro_frontends:user_management:module"],
                Enabled = true
            };
        }

        // Load order management micro-frontend
        if (bool.Parse(_config["micro_frontends:order_management:enabled"]))
        {
            configs["orderManagement"] = new MicroFrontendConfig
            {
                Name = _config["micro_frontends:order_management:name"],
                Url = _config["micro_frontends:order_management:url"],
                Scope = _config["micro_frontends:order_management:scope"],
                Module = _config["micro_frontends:order_management:module"],
                Enabled = true
            };
        }

        // Load product catalog micro-frontend
        if (bool.Parse(_config["micro_frontends:product_catalog:enabled"]))
        {
            configs["productCatalog"] = new MicroFrontendConfig
            {
                Name = _config["micro_frontends:product_catalog:name"],
                Url = _config["micro_frontends:product_catalog:url"],
                Scope = _config["micro_frontends:product_catalog:scope"],
                Module = _config["micro_frontends:product_catalog:module"],
                Enabled = true
            };
        }

        return configs;
    }
}
```

## Module Federation

```csharp
public interface IModuleFederationService
{
    Task<ModuleInfo> LoadModuleAsync(string scope, string module);
    Task<bool> IsModuleAvailableAsync(string scope, string module);
    Task<List<ModuleInfo>> GetAvailableModulesAsync();
}

public class ModuleFederationService : IModuleFederationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<ModuleFederationService> _logger;
    private readonly HttpClient _httpClient;

    public ModuleFederationService(IConfiguration config, ILogger<ModuleFederationService> logger, HttpClient httpClient)
    {
        _config = config;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ModuleInfo> LoadModuleAsync(string scope, string module)
    {
        try
        {
            var microFrontend = GetMicroFrontendByScope(scope);
            if (microFrontend == null)
                return null;

            var url = $"{microFrontend.Url}/_next/static/chunks/{module}.js";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return new ModuleInfo
                {
                    Scope = scope,
                    Module = module,
                    Url = url,
                    Content = content,
                    LoadedAt = DateTime.UtcNow
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading module {Scope}/{Module}", scope, module);
            return null;
        }
    }

    public async Task<bool> IsModuleAvailableAsync(string scope, string module)
    {
        try
        {
            var microFrontend = GetMicroFrontendByScope(scope);
            if (microFrontend == null)
                return false;

            var url = $"{microFrontend.Url}/_next/static/chunks/{module}.js";
            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Module availability check failed for {Scope}/{Module}", scope, module);
            return false;
        }
    }

    public async Task<List<ModuleInfo>> GetAvailableModulesAsync()
    {
        var modules = new List<ModuleInfo>();

        var scopes = new[] { "userManagement", "orderManagement", "productCatalog" };
        var moduleNames = new[] { "UserApp", "OrderApp", "ProductApp" };

        for (int i = 0; i < scopes.Length; i++)
        {
            if (await IsModuleAvailableAsync(scopes[i], moduleNames[i]))
            {
                modules.Add(new ModuleInfo
                {
                    Scope = scopes[i],
                    Module = moduleNames[i],
                    Available = true
                });
            }
        }

        return modules;
    }

    private MicroFrontendConfig GetMicroFrontendByScope(string scope)
    {
        // Implementation to get micro-frontend config by scope
        return new MicroFrontendConfig
        {
            Name = scope,
            Url = _config[$"micro_frontends:{scope}:url"],
            Scope = scope,
            Module = _config[$"micro_frontends:{scope}:module"]
        };
    }
}

public class ModuleInfo
{
    public string Scope { get; set; }
    public string Module { get; set; }
    public string Url { get; set; }
    public string Content { get; set; }
    public DateTime LoadedAt { get; set; }
    public bool Available { get; set; }
}
```

## Routing and Navigation

```csharp
public interface IMicroFrontendRouter
{
    Task<RouteInfo> RouteAsync(string path, string userId);
    Task<List<RouteInfo>> GetRoutesAsync();
    Task<bool> IsRouteAvailableAsync(string path);
}

public class MicroFrontendRouter : IMicroFrontendRouter
{
    private readonly IConfiguration _config;
    private readonly ILogger<MicroFrontendRouter> _logger;
    private readonly Dictionary<string, RouteInfo> _routes;

    public MicroFrontendRouter(IConfiguration config, ILogger<MicroFrontendRouter> logger)
    {
        _config = config;
        _logger = logger;
        _routes = InitializeRoutes();
    }

    public async Task<RouteInfo> RouteAsync(string path, string userId)
    {
        if (_routes.TryGetValue(path, out var route))
        {
            // Check if user has access to this route
            if (await HasAccessAsync(route, userId))
            {
                return route;
            }
            else
            {
                _logger.LogWarning("User {UserId} does not have access to route {Path}", userId, path);
                return null;
            }
        }

        return null;
    }

    public async Task<List<RouteInfo>> GetRoutesAsync()
    {
        return _routes.Values.ToList();
    }

    public async Task<bool> IsRouteAvailableAsync(string path)
    {
        return _routes.ContainsKey(path);
    }

    private Dictionary<string, RouteInfo> InitializeRoutes()
    {
        var routes = new Dictionary<string, RouteInfo>();

        // User management routes
        routes["/users"] = new RouteInfo
        {
            Path = "/users",
            MicroFrontend = "userManagement",
            Component = "UserList",
            RequiresAuth = true,
            Permissions = new[] { "user:read" }
        };

        routes["/users/create"] = new RouteInfo
        {
            Path = "/users/create",
            MicroFrontend = "userManagement",
            Component = "UserCreate",
            RequiresAuth = true,
            Permissions = new[] { "user:create" }
        };

        // Order management routes
        routes["/orders"] = new RouteInfo
        {
            Path = "/orders",
            MicroFrontend = "orderManagement",
            Component = "OrderList",
            RequiresAuth = true,
            Permissions = new[] { "order:read" }
        };

        routes["/orders/create"] = new RouteInfo
        {
            Path = "/orders/create",
            MicroFrontend = "orderManagement",
            Component = "OrderCreate",
            RequiresAuth = true,
            Permissions = new[] { "order:create" }
        };

        // Product catalog routes
        routes["/products"] = new RouteInfo
        {
            Path = "/products",
            MicroFrontend = "productCatalog",
            Component = "ProductList",
            RequiresAuth = false,
            Permissions = new string[0]
        };

        return routes;
    }

    private async Task<bool> HasAccessAsync(RouteInfo route, string userId)
    {
        if (!route.RequiresAuth)
            return true;

        // Implementation to check user permissions
        // This would typically involve calling an authorization service
        return true; // Simplified for this example
    }
}

public class RouteInfo
{
    public string Path { get; set; }
    public string MicroFrontend { get; set; }
    public string Component { get; set; }
    public bool RequiresAuth { get; set; }
    public string[] Permissions { get; set; }
}

public class RouteRequest
{
    public string Path { get; set; }
    public string UserId { get; set; }
}
```

## Best Practices

1. **Design clear boundaries between micro-frontends**
2. **Use shared dependencies for common functionality**
3. **Implement proper routing and navigation**
4. **Handle loading states and error boundaries**
5. **Use event-driven communication between micro-frontends**
6. **Implement proper authentication and authorization**
7. **Monitor micro-frontend performance and availability**

## Conclusion

Micro-Frontends with C# and TuskLang enables building scalable, maintainable web applications with independent frontend components. By leveraging TuskLang for configuration and micro-frontend patterns, you can create systems that support independent development and deployment of frontend modules. 