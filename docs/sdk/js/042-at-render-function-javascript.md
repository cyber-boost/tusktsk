# @render Function - JavaScript

## Overview

The `@render` function in TuskLang enables dynamic template rendering with variable substitution. This is essential for JavaScript applications that need to generate dynamic content, HTML templates, or configuration files with embedded variables.

## Basic Syntax

```tsk
# Simple template rendering
template: "Hello, {{name}}! Welcome to {{app_name}}."
greeting: @render($template, {"name": "John", "app_name": "TuskLang"})

# Multi-line template
email_template: """
Dear {{user_name}},

Thank you for joining {{platform}}. Your account ID is {{user_id}}.

Best regards,
{{company_name}} Team
"""

rendered_email: @render($email_template, {"user_name": "Alice", "platform": "TuskLang", "user_id": "12345", "company_name": "TuskCorp"})
```

## JavaScript Integration

### Node.js Template Rendering

```javascript
const tusk = require('tusklang');

// Load configuration with templates
const config = tusk.load('templates.tsk');

// Use rendered templates
console.log(config.greeting); // "Hello, John! Welcome to TuskLang."

// Render templates dynamically
const userData = {
  name: 'Sarah',
  app_name: 'MyApp'
};

const dynamicGreeting = tusk.render(config.template, userData);
console.log(dynamicGreeting); // "Hello, Sarah! Welcome to MyApp."
```

### Browser Template Rendering

```javascript
// Load TuskLang configuration
const config = await tusk.load('templates.tsk');

// Use in frontend components
const EmailComponent = {
  render(userData) {
    const emailContent = tusk.render(config.email_template, userData);
    return `<div class="email">${emailContent}</div>`;
  }
};

// Usage
const email = EmailComponent.render({
  user_name: 'Bob',
  platform: 'TuskLang',
  user_id: '67890',
  company_name: 'TuskCorp'
});
```

## Advanced Usage

### Conditional Templates

```tsk
# Template with conditional logic
conditional_template: """
{{#if user_type == "admin"}}
Welcome, Administrator {{name}}!
You have access to all features.
{{else}}
Welcome, {{name}}!
Your access level: {{user_type}}
{{/if}}
"""

admin_greeting: @render($conditional_template, {"name": "Admin", "user_type": "admin"})
user_greeting: @render($conditional_template, {"name": "User", "user_type": "basic"})
```

### Loop Templates

```tsk
# Template with loops
list_template: """
Available features:
{{#each features}}
- {{name}}: {{description}}
{{/each}}
"""

feature_list: @render($list_template, {"features": [{"name": "Templates", "description": "Dynamic rendering"}, {"name": "Database", "description": "Direct queries"}]})
```

### Nested Templates

```tsk
# Nested template structure
header_template: "{{title}} - {{subtitle}}"
content_template: """
{{header}}
{{body}}
{{footer}}
"""

page_content: @render($content_template, {
  "header": @render($header_template, {"title": "TuskLang", "subtitle": "Documentation"}),
  "body": "Welcome to TuskLang documentation!",
  "footer": "© 2024 TuskCorp"
})
```

## JavaScript Implementation

### Custom Template Engine

```javascript
class TuskTemplateEngine {
  constructor() {
    this.cache = new Map();
  }
  
  render(template, data) {
    // Check cache first
    const cacheKey = `${template}-${JSON.stringify(data)}`;
    if (this.cache.has(cacheKey)) {
      return this.cache.get(cacheKey);
    }
    
    // Simple template rendering
    let result = template;
    
    // Replace variables
    Object.keys(data).forEach(key => {
      const regex = new RegExp(`{{${key}}}`, 'g');
      result = result.replace(regex, data[key]);
    });
    
    // Handle conditionals
    result = this.processConditionals(result, data);
    
    // Handle loops
    result = this.processLoops(result, data);
    
    // Cache result
    this.cache.set(cacheKey, result);
    return result;
  }
  
  processConditionals(template, data) {
    // Process {{#if condition}}...{{else}}...{{/if}}
    const ifRegex = /{{#if\s+([^}]+)}}([\s\S]*?)(?:{{else}}([\s\S]*?))?{{\/if}}/g;
    
    return template.replace(ifRegex, (match, condition, ifContent, elseContent) => {
      const isTrue = this.evaluateCondition(condition, data);
      return isTrue ? ifContent : (elseContent || '');
    });
  }
  
  processLoops(template, data) {
    // Process {{#each array}}...{{/each}}
    const eachRegex = /{{#each\s+([^}]+)}}([\s\S]*?){{\/each}}/g;
    
    return template.replace(eachRegex, (match, arrayName, loopContent) => {
      const array = data[arrayName] || [];
      return array.map(item => {
        let content = loopContent;
        Object.keys(item).forEach(key => {
          const regex = new RegExp(`{{${key}}}`, 'g');
          content = content.replace(regex, item[key]);
        });
        return content;
      }).join('');
    });
  }
  
  evaluateCondition(condition, data) {
    // Simple condition evaluation
    const parts = condition.split('==');
    if (parts.length === 2) {
      const key = parts[0].trim();
      const value = parts[1].trim().replace(/['"]/g, '');
      return data[key] === value;
    }
    return false;
  }
}
```

### TypeScript Support

```typescript
interface TemplateData {
  [key: string]: any;
}

interface TemplateConfig {
  template: string;
  greeting: string;
  email_template: string;
  rendered_email: string;
}

class TemplateManager {
  private engine: TuskTemplateEngine;
  
  constructor() {
    this.engine = new TuskTemplateEngine();
  }
  
  renderTemplate(template: string, data: TemplateData): string {
    return this.engine.render(template, data);
  }
  
  async loadTemplates(): Promise<TemplateConfig> {
    return await tusk.load('templates.tsk');
  }
}
```

## Real-World Examples

### Email Templates

```tsk
# Email notification template
notification_template: """
Subject: {{subject}}

Dear {{recipient_name}},

{{message}}

{{#if action_required}}
Action Required: {{action_description}}
{{/if}}

{{#if include_link}}
Click here: {{link}}
{{/if}}

Best regards,
{{sender_name}}
{{company_name}}
"""

welcome_email: @render($notification_template, {
  "subject": "Welcome to TuskLang!",
  "recipient_name": "New User",
  "message": "Thank you for joining our platform. We're excited to have you on board!",
  "action_required": true,
  "action_description": "Please verify your email address",
  "include_link": true,
  "link": "https://tusklang.org/verify",
  "sender_name": "TuskLang Team",
  "company_name": "TuskCorp"
})
```

### API Response Templates

```tsk
# API response template
api_response_template: """
{
  "status": "{{status}}",
  "message": "{{message}}",
  "data": {
    {{#each data_fields}}
    "{{key}}": "{{value}}"{{#unless @last}},{{/unless}}
    {{/each}}
  },
  "timestamp": "{{timestamp}}"
}
"""

success_response: @render($api_response_template, {
  "status": "success",
  "message": "User created successfully",
  "data_fields": [
    {"key": "user_id", "value": "12345"},
    {"key": "username", "value": "john_doe"},
    {"key": "email", "value": "john@example.com"}
  ],
  "timestamp": "2024-01-15T10:30:00Z"
})
```

### Configuration Templates

```tsk
# Dynamic configuration template
config_template: """
# {{app_name}} Configuration
# Generated on {{timestamp}}

APP_NAME={{app_name}}
APP_VERSION={{version}}
DEBUG={{debug_mode}}
DATABASE_URL={{db_url}}
API_KEY={{api_key}}

# Features
{{#each features}}
{{name}}=true
{{/each}}
"""

app_config: @render($config_template, {
  "app_name": "TuskLang App",
  "timestamp": "2024-01-15",
  "version": "1.0.0",
  "debug_mode": "true",
  "db_url": "postgresql://localhost/tuskdb",
  "api_key": "abc123",
  "features": [
    {"name": "TEMPLATES"},
    {"name": "DATABASE"},
    {"name": "CACHING"}
  ]
})
```

## Performance Considerations

### Template Caching

```tsk
# Cache rendered templates
cached_template: @cache("10m", @render($complex_template, $template_data))
```

### Pre-compiled Templates

```javascript
// Pre-compile templates for better performance
class TemplateCompiler {
  constructor() {
    this.compiledTemplates = new Map();
  }
  
  compile(template) {
    if (this.compiledTemplates.has(template)) {
      return this.compiledTemplates.get(template);
    }
    
    // Compile template to function
    const compiled = this.createTemplateFunction(template);
    this.compiledTemplates.set(template, compiled);
    return compiled;
  }
  
  createTemplateFunction(template) {
    // Convert template to JavaScript function
    const functionBody = this.convertTemplateToFunction(template);
    return new Function('data', functionBody);
  }
}
```

## Security Notes

- **Input Sanitization**: Always sanitize template variables to prevent XSS
- **Template Validation**: Validate template syntax before rendering
- **Access Control**: Limit template access to prevent unauthorized rendering

```javascript
// Secure template rendering
function secureRender(template, data) {
  // Sanitize data
  const sanitizedData = {};
  Object.keys(data).forEach(key => {
    if (typeof data[key] === 'string') {
      sanitizedData[key] = escapeHtml(data[key]);
    } else {
      sanitizedData[key] = data[key];
    }
  });
  
  return tusk.render(template, sanitizedData);
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}
```

## Best Practices

1. **Template Validation**: Validate templates before rendering
2. **Error Handling**: Implement proper error handling for template failures
3. **Caching**: Cache compiled templates for better performance
4. **Security**: Always sanitize template variables
5. **Testing**: Test templates with various data scenarios

## Next Steps

- Explore [@redirect function](./043-at-redirect-function-javascript.md) for URL redirection
- Master [@query database](./044-at-query-database-javascript.md) for database operations
- Learn about [@tusk object](./045-at-tusk-object-javascript.md) for advanced operations 