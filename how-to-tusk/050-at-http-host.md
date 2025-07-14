# @http.host - HTTP Host Information

The `@http.host` operator provides access to host and domain information from HTTP requests, useful for multi-tenant applications and domain-based routing.

## Basic Syntax

```tusk
# Get the host header
host: @http.host

# Get specific parts
domain: @http.host.domain
subdomain: @http.host.subdomain
port: @http.host.port
protocol: @http.host.protocol
```

## Host Components

```tusk
# Full host information
host_info: {
    # Full host header (e.g., "app.example.com:8080")
    full: @http.host
    
    # Protocol (http or https)
    protocol: @http.host.protocol
    
    # Domain parts
    subdomain: @http.host.subdomain    # "app"
    domain: @http.host.domain          # "example"
    tld: @http.host.tld                # "com"
    
    # Full domain
    full_domain: @http.host.full_domain  # "example.com"
    
    # Port (if specified)
    port: @http.host.port|80
    
    # Is secure
    is_secure: @http.host.is_secure
}
```

## Multi-Tenant Applications

```tusk
# Subdomain-based tenancy
#web /* {
    tenant_subdomain: @http.host.subdomain
    
    @if(@tenant_subdomain && @tenant_subdomain != "www") {
        # Load tenant configuration
        tenant: @query("SELECT * FROM tenants WHERE subdomain = ?", 
                      [@tenant_subdomain])
        
        @if(@tenant) {
            # Set tenant context
            @context.tenant: @tenant
            @context.database: @tenant.database_name
            
            # Route to tenant app
            @render("tenant/app.tusk", {tenant: @tenant})
        } else {
            @response.status: 404
            @render("errors/tenant_not_found.tusk")
        }
    } else {
        # Main website
        @render("main/home.tusk")
    }
}
```

## Domain-Based Routing

```tusk
# Route based on domain
domain_router: {
    routes: {
        "api.example.com": "/api/handler"
        "admin.example.com": "/admin/dashboard"
        "blog.example.com": "/blog/home"
        "shop.example.com": "/shop/catalog"
    }
    
    current_host: @http.host.subdomain + "." + @http.host.full_domain
    route: @routes[@current_host]|"/main/home"
    
    @include(@route)
}

# Country-specific domains
#web /* {
    tld: @http.host.tld
    
    country_config: {
        "com": {locale: "en_US", currency: "USD", timezone: "America/New_York"}
        "co.uk": {locale: "en_GB", currency: "GBP", timezone: "Europe/London"}
        "de": {locale: "de_DE", currency: "EUR", timezone: "Europe/Berlin"}
        "jp": {locale: "ja_JP", currency: "JPY", timezone: "Asia/Tokyo"}
    }
    
    config: @country_config[@tld]|@country_config["com"]
    
    # Apply configuration
    @locale.set(@config.locale)
    @currency.set(@config.currency)
    @timezone.set(@config.timezone)
}
```

## SSL/HTTPS Detection

```tusk
# Force HTTPS
#middleware force_https {
    @if(@http.host.protocol != "https" && @env.APP_ENV == "production") {
        secure_url: "https://" + @http.host + @request.uri
        @redirect(@secure_url, 301)
    }
}

# Security headers based on protocol
#middleware security_headers {
    @if(@http.host.is_secure) {
        @response.headers.strict-transport-security: 
            "max-age=31536000; includeSubDomains"
        @response.headers.content-security-policy: 
            "default-src 'self' https:"
    }
}
```

## Development vs Production

```tusk
# Environment detection based on host
detect_environment: {
    host: @http.host
    
    @if(@contains(@host, "localhost") || @contains(@host, "127.0.0.1")) {
        environment: "development"
    } elseif(@contains(@host, "staging.")) {
        environment: "staging"
    } elseif(@contains(@host, ".test") || @contains(@host, ".local")) {
        environment: "testing"
    } else {
        environment: "production"
    }
    
    @env.APP_ENV: @environment
}

# Development tools
@if(@http.host.is_local) {
    @enable_debug_toolbar()
    @disable_cache()
    @enable_error_display()
}
```

## API Versioning

```tusk
# Version based on subdomain
#api /* {
    # Extract version from subdomain (e.g., v1.api.example.com)
    subdomain_parts: @explode(".", @http.host.subdomain)
    api_version: @subdomain_parts[0]
    
    @if(@starts_with(@api_version, "v")) {
        version: @substr(@api_version, 1)
        
        # Route to versioned API
        @include("api/v" + @version + "/router.tusk")
    } else {
        # Default to latest version
        @include("api/v2/router.tusk")
    }
}
```

## White-Label Support

```tusk
# Custom domains for clients
#web /* {
    custom_domain: @http.host.full_domain
    
    # Check if custom domain
    client: @query("SELECT * FROM clients WHERE custom_domain = ?", 
                  [@custom_domain])
    
    @if(@client) {
        # Load client branding
        branding: {
            logo: @client.logo_url
            colors: @json_decode(@client.brand_colors)
            company_name: @client.company_name
            favicon: @client.favicon_url
        }
        
        @context.branding: @branding
        @context.client: @client
        
        # Render with custom branding
        @render("white_label/app.tusk", {
            branding: @branding
            client: @client
        })
    }
}
```

## Cross-Origin Handling

```tusk
# CORS configuration based on origin
#middleware cors {
    origin: @request.headers.origin
    
    # Parse origin host
    origin_host: @parse_url(@origin).host
    
    # Allowed origins
    allowed_origins: [
        @http.host,
        "app." + @http.host.full_domain,
        "api." + @http.host.full_domain
    ]
    
    @if(@in_array(@origin_host, @allowed_origins)) {
        @response.headers.access-control-allow-origin: @origin
        @response.headers.access-control-allow-credentials: "true"
    } elseif(@env.APP_ENV == "development") {
        # Allow localhost in development
        @if(@contains(@origin, "localhost")) {
            @response.headers.access-control-allow-origin: @origin
        }
    }
}
```

## Port-Based Services

```tusk
# Route based on port
service_router: {
    port: @http.host.port|80
    
    services: {
        80: "web"
        443: "web_secure"
        8080: "api"
        8081: "admin"
        3000: "websocket"
    }
    
    service: @services[@port]|"web"
    
    @include("services/" + @service + "/handler.tusk")
}

# Development port detection
@if(@http.host.port >= 3000 && @http.host.port <= 9999) {
    @env.APP_DEBUG: true
    @env.APP_ENV: "development"
}
```

## Mobile App Detection

```tusk
# Detect mobile app subdomains
#web /* {
    subdomain: @http.host.subdomain
    
    @if(@subdomain == "m" || @subdomain == "mobile") {
        # Mobile web version
        @context.is_mobile: true
        @render("mobile/app.tusk")
    } elseif(@subdomain == "app") {
        # Deep link to mobile app
        @redirect("myapp://home")
    }
}
```

## Wildcard Subdomain Handling

```tusk
# User profile subdomains (e.g., john.example.com)
#web /* {
    subdomain: @http.host.subdomain
    reserved_subdomains: ["www", "api", "admin", "app", "mail"]
    
    @if(@subdomain && !@in_array(@subdomain, @reserved_subdomains)) {
        # Treat as username
        user: @query("SELECT * FROM users WHERE username = ?", 
                    [@subdomain])
        
        @if(@user) {
            @render("profiles/public.tusk", {user: @user})
        } else {
            @response.status: 404
            @render("errors/profile_not_found.tusk")
        }
    }
}
```

## Geo-Based Routing

```tusk
# Route based on country domain
geo_router: {
    # Map TLD to region
    regions: {
        "com": "us-east-1"
        "eu": "eu-west-1"
        "co.uk": "eu-west-2"
        "com.au": "ap-southeast-2"
        "co.jp": "ap-northeast-1"
    }
    
    tld: @http.host.tld
    region: @regions[@tld]|"us-east-1"
    
    # Connect to regional database
    @database.connect(@region)
    
    # Set regional configuration
    @config.region: @region
    @config.currency: @get_regional_currency(@region)
    @config.tax_rate: @get_regional_tax_rate(@region)
}
```

## Debugging Host Information

```tusk
# Debug endpoint
#api /debug/host {
    @if(@env.APP_ENV != "development") {
        @response.status: 403
        error: "Forbidden"
        return
    }
    
    @json({
        raw_host: @request.headers.host
        parsed: {
            full: @http.host
            protocol: @http.host.protocol
            subdomain: @http.host.subdomain
            domain: @http.host.domain
            tld: @http.host.tld
            port: @http.host.port
            full_domain: @http.host.full_domain
        }
        flags: {
            is_secure: @http.host.is_secure
            is_local: @http.host.is_local
            is_ip: @http.host.is_ip
        }
        environment: @env.APP_ENV
    })
}
```

## Best Practices

1. **Validate host headers** - Don't trust user-supplied host headers
2. **Use HTTPS in production** - Always check protocol
3. **Handle missing subdomains** - Not all requests have subdomains
4. **Set default ports** - Handle when port is not specified
5. **Whitelist domains** - For security in multi-tenant apps
6. **Cache host parsing** - It doesn't change during request

## Related Operators

- `@request.headers.host` - Raw host header
- `@request.uri` - Request URI
- `@env` - Environment variables
- `@redirect()` - URL redirection
- `@parse_url()` - URL parsing function