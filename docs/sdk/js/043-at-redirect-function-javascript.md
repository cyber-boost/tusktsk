# @redirect Function - JavaScript

## Overview

The `@redirect` function in TuskLang enables dynamic URL redirection and routing within your configuration. This is essential for JavaScript applications that need to handle dynamic routing, API redirects, or conditional navigation based on configuration values.

## Basic Syntax

```tsk
# Simple redirect configuration
login_redirect: @redirect("/dashboard", {"status": 302})
logout_redirect: @redirect("/login", {"status": 301})

# Conditional redirects
user_redirect: @redirect($user_dashboard_url, {"status": 302, "condition": "user_authenticated"})
admin_redirect: @redirect("/admin/panel", {"status": 302, "condition": "user_is_admin"})
```

## JavaScript Integration

### Node.js Redirect Handling

```javascript
const tusk = require('tusklang');
const express = require('express');

// Load configuration with redirects
const config = tusk.load('redirects.tsk');

// Express.js middleware for handling redirects
app.use('/auth', (req, res, next) => {
  if (req.path === '/login') {
    const redirectUrl = tusk.evaluate(config.login_redirect);
    res.redirect(redirectUrl.status || 302, redirectUrl.url);
  } else if (req.path === '/logout') {
    const redirectUrl = tusk.evaluate(config.logout_redirect);
    res.redirect(redirectUrl.status || 301, redirectUrl.url);
  } else {
    next();
  }
});

// Dynamic redirect based on user role
app.get('/dashboard', (req, res) => {
  const user = req.user;
  
  if (user.isAdmin) {
    const adminRedirect = tusk.evaluate(config.admin_redirect);
    res.redirect(adminRedirect.status, adminRedirect.url);
  } else {
    const userRedirect = tusk.evaluate(config.user_redirect);
    res.redirect(userRedirect.status, userRedirect.url);
  }
});
```

### Browser Redirect Handling

```javascript
// Load TuskLang configuration
const config = await tusk.load('redirects.tsk');

// Browser redirect utility
class RedirectManager {
  constructor() {
    this.config = config;
  }
  
  redirect(path, options = {}) {
    const redirectConfig = this.config[path];
    if (redirectConfig) {
      const url = tusk.evaluate(redirectConfig);
      window.location.href = url.url;
    } else {
      window.location.href = path;
    }
  }
  
  conditionalRedirect(condition, path) {
    const redirectConfig = this.config[`${condition}_redirect`];
    if (redirectConfig) {
      const url = tusk.evaluate(redirectConfig);
      if (url.condition) {
        window.location.href = url.url;
        return true;
      }
    }
    return false;
  }
}

// Usage
const redirectManager = new RedirectManager();
redirectManager.redirect('login');
redirectManager.conditionalRedirect('user_authenticated', 'dashboard');
```

## Advanced Usage

### Dynamic URL Generation

```tsk
# Dynamic redirects with variables
user_profile_redirect: @redirect("/users/{{user_id}}/profile", {"status": 302})
product_redirect: @redirect("/products/{{category}}/{{product_id}}", {"status": 301})

# API redirects
api_redirect: @redirect("{{api_base_url}}/{{endpoint}}", {"status": 307, "headers": {"Authorization": "Bearer {{token}}"}})
```

### Conditional Redirects

```tsk
# Redirect based on conditions
mobile_redirect: @redirect("/mobile/{{page}}", {"status": 302, "condition": "is_mobile"})
desktop_redirect: @redirect("/desktop/{{page}}", {"status": 302, "condition": "is_desktop"})

# Role-based redirects
guest_redirect: @redirect("/welcome", {"status": 302, "condition": "user_role == 'guest'"})
premium_redirect: @redirect("/premium/dashboard", {"status": 302, "condition": "user_role == 'premium'"})
```

### Redirect Chains

```tsk
# Redirect chain configuration
redirect_chain: [
  @redirect("/step1", {"status": 302, "next": "step2"}),
  @redirect("/step2", {"status": 302, "next": "step3"}),
  @redirect("/step3", {"status": 302, "next": "complete"})
]
```

## JavaScript Implementation

### Express.js Redirect Middleware

```javascript
class TuskRedirectMiddleware {
  constructor(configPath) {
    this.config = tusk.load(configPath);
  }
  
  middleware() {
    return (req, res, next) => {
      // Check for redirect configuration
      const redirectKey = `${req.method.toLowerCase()}_${req.path.replace(/\//g, '_')}`;
      const redirectConfig = this.config[redirectKey];
      
      if (redirectConfig) {
        const redirect = tusk.evaluate(redirectConfig);
        
        // Apply conditions
        if (redirect.condition && !this.evaluateCondition(redirect.condition, req)) {
          return next();
        }
        
        // Set headers
        if (redirect.headers) {
          Object.keys(redirect.headers).forEach(key => {
            res.set(key, redirect.headers[key]);
          });
        }
        
        // Perform redirect
        res.redirect(redirect.status || 302, redirect.url);
      } else {
        next();
      }
    };
  }
  
  evaluateCondition(condition, req) {
    // Simple condition evaluation
    if (condition === 'is_mobile') {
      return req.headers['user-agent'].includes('Mobile');
    }
    if (condition === 'user_authenticated') {
      return req.user && req.user.id;
    }
    if (condition.includes('user_role ==')) {
      const role = condition.split('==')[1].trim().replace(/['"]/g, '');
      return req.user && req.user.role === role;
    }
    return false;
  }
}

// Usage
const redirectMiddleware = new TuskRedirectMiddleware('redirects.tsk');
app.use(redirectMiddleware.middleware());
```

### React Router Integration

```javascript
import { useNavigate } from 'react-router-dom';

// Custom hook for TuskLang redirects
function useTuskRedirect() {
  const navigate = useNavigate();
  
  const redirect = async (path, options = {}) => {
    try {
      const config = await tusk.load('redirects.tsk');
      const redirectConfig = config[path];
      
      if (redirectConfig) {
        const result = tusk.evaluate(redirectConfig);
        
        // Check conditions
        if (result.condition && !evaluateCondition(result.condition)) {
          return false;
        }
        
        // Navigate to URL
        navigate(result.url, { replace: options.replace || false });
        return true;
      } else {
        navigate(path, { replace: options.replace || false });
        return true;
      }
    } catch (error) {
      console.error('Redirect error:', error);
      return false;
    }
  };
  
  const evaluateCondition = (condition) => {
    // Evaluate conditions based on app state
    if (condition === 'is_mobile') {
      return window.innerWidth < 768;
    }
    if (condition === 'user_authenticated') {
      return localStorage.getItem('auth_token');
    }
    return false;
  };
  
  return { redirect };
}

// Usage in component
function LoginComponent() {
  const { redirect } = useTuskRedirect();
  
  const handleLogin = async () => {
    // ... login logic
    await redirect('login_success');
  };
  
  return (
    <button onClick={handleLogin}>
      Login
    </button>
  );
}
```

## Real-World Examples

### E-commerce Redirects

```tsk
# Product page redirects
product_redirect: @redirect("/products/{{category}}/{{product_id}}", {"status": 301})
category_redirect: @redirect("/categories/{{category_name}}", {"status": 302})

# Checkout flow redirects
cart_redirect: @redirect("/cart", {"status": 302, "condition": "cart_not_empty"})
checkout_redirect: @redirect("/checkout/{{step}}", {"status": 302, "condition": "user_authenticated"})
payment_redirect: @redirect("/payment/{{method}}", {"status": 302, "condition": "payment_method_selected"})
```

### API Gateway Redirects

```tsk
# API version redirects
api_v1_redirect: @redirect("{{api_base_url}}/v1/{{endpoint}}", {"status": 307})
api_v2_redirect: @redirect("{{api_base_url}}/v2/{{endpoint}}", {"status": 307})

# Service redirects
auth_service_redirect: @redirect("{{auth_service_url}}/{{action}}", {"status": 307, "headers": {"X-Service": "auth"}})
payment_service_redirect: @redirect("{{payment_service_url}}/{{action}}", {"status": 307, "headers": {"X-Service": "payment"}})
```

### Multi-language Redirects

```tsk
# Language-based redirects
en_redirect: @redirect("/en/{{page}}", {"status": 302, "condition": "language == 'en'"})
es_redirect: @redirect("/es/{{page}}", {"status": 302, "condition": "language == 'es'"})
fr_redirect: @redirect("/fr/{{page}}", {"status": 302, "condition": "language == 'fr'"})

# Default language redirect
default_language_redirect: @redirect("/{{default_language}}/{{page}}", {"status": 302, "condition": "no_language_detected"})
```

## Performance Considerations

### Redirect Caching

```tsk
# Cache redirect configurations
cached_redirect: @cache("5m", @redirect("/cached/{{path}}", {"status": 302}))
```

### Lazy Loading

```javascript
// Load redirect configurations on demand
class LazyRedirectManager {
  constructor() {
    this.configCache = new Map();
  }
  
  async getRedirect(path) {
    if (!this.configCache.has(path)) {
      const config = await tusk.load('redirects.tsk');
      this.configCache.set(path, config[path]);
    }
    return this.configCache.get(path);
  }
}
```

## Security Notes

- **URL Validation**: Always validate redirect URLs to prevent open redirects
- **Authentication**: Ensure redirects respect authentication requirements
- **HTTPS**: Use HTTPS for sensitive redirects

```javascript
// Secure redirect handling
function secureRedirect(url, allowedDomains = []) {
  try {
    const urlObj = new URL(url);
    
    // Check if domain is allowed
    if (allowedDomains.length > 0 && !allowedDomains.includes(urlObj.hostname)) {
      throw new Error('Redirect to unauthorized domain');
    }
    
    // Ensure HTTPS for sensitive operations
    if (urlObj.protocol !== 'https:' && urlObj.hostname !== 'localhost') {
      urlObj.protocol = 'https:';
    }
    
    return urlObj.toString();
  } catch (error) {
    console.error('Invalid redirect URL:', error);
    return '/error';
  }
}
```

## Best Practices

1. **URL Validation**: Always validate redirect URLs
2. **Status Codes**: Use appropriate HTTP status codes
3. **Conditional Logic**: Implement proper conditional redirects
4. **Error Handling**: Handle redirect failures gracefully
5. **Security**: Prevent open redirect vulnerabilities

## Next Steps

- Master [@query database](./044-at-query-database-javascript.md) for database operations
- Learn about [@tusk object](./045-at-tusk-object-javascript.md) for advanced operations
- Explore [@cache function](./046-at-cache-function-javascript.md) for caching strategies 