# @json Function - JavaScript

## Overview

The `@json` function in TuskLang allows you to parse JSON strings and access their properties directly in your configuration. This is particularly useful for JavaScript applications that need to work with API responses, configuration data, or complex nested structures.

## Basic Syntax

```tsk
# Parse JSON string and access properties
api_response: @json('{"status": "success", "data": {"user": "john", "id": 123}}')
user_name: @json('{"status": "success", "data": {"user": "john", "id": 123}}').data.user
user_id: @json('{"status": "success", "data": {"user": "john", "id": 123}}').data.id
```

## JavaScript Integration

### Node.js Example

```javascript
const tusk = require('tusklang');

// Load configuration with JSON parsing
const config = tusk.load('config.tsk');

// Access parsed JSON data
console.log(config.api_response); // Full parsed object
console.log(config.user_name);    // "john"
console.log(config.user_id);      // 123

// Use in API responses
const apiConfig = {
  endpoint: config.api_response.data.user,
  userId: config.user_id
};
```

### Browser Example

```javascript
// Load TuskLang configuration
const config = await tusk.load('config.tsk');

// Access JSON data
const userData = config.api_response;
const userName = config.user_name;

// Use in frontend components
const UserProfile = {
  name: userName,
  id: config.user_id,
  status: config.api_response.status
};
```

## Advanced Usage

### Nested JSON Access

```tsk
# Complex nested JSON structure
complex_data: @json('{"users": [{"name": "Alice", "settings": {"theme": "dark", "notifications": true}}, {"name": "Bob", "settings": {"theme": "light", "notifications": false}}]}')

# Access nested properties
first_user: @json('{"users": [{"name": "Alice", "settings": {"theme": "dark", "notifications": true}}]}').users[0].name
first_user_theme: @json('{"users": [{"name": "Alice", "settings": {"theme": "dark", "notifications": true}}]}').users[0].settings.theme
```

### Dynamic JSON from Variables

```tsk
# Store JSON string in variable
json_string: '{"api_key": "abc123", "endpoint": "https://api.example.com"}'

# Parse and access
api_key: @json($json_string).api_key
api_endpoint: @json($json_string).endpoint
```

### Error Handling

```tsk
# Invalid JSON with fallback
invalid_json: @json('{"incomplete": json}', '{"error": "fallback"}')
safe_value: @json('invalid json', '{"default": "value"}').default
```

## JavaScript Implementation

### Error Handling in JavaScript

```javascript
const tusk = require('tusklang');

try {
  const config = tusk.load('config.tsk');
  
  // Access JSON data with error handling
  const userData = config.complex_data?.users?.[0] || {};
  const theme = userData.settings?.theme || 'default';
  
} catch (error) {
  console.error('JSON parsing error:', error);
  // Use fallback values
}
```

### TypeScript Support

```typescript
interface UserConfig {
  name: string;
  settings: {
    theme: 'dark' | 'light';
    notifications: boolean;
  };
}

interface TuskConfig {
  complex_data: {
    users: UserConfig[];
  };
  first_user: string;
  first_user_theme: string;
}

const config: TuskConfig = tusk.load('config.tsk');
```

## Real-World Examples

### API Configuration

```tsk
# API response configuration
api_config: @json('{"base_url": "https://api.example.com", "version": "v2", "timeout": 5000, "retries": 3}')

# Extract configuration values
api_base_url: @json('{"base_url": "https://api.example.com", "version": "v2", "timeout": 5000, "retries": 3}').base_url
api_version: @json('{"base_url": "https://api.example.com", "version": "v2", "timeout": 5000, "retries": 3}').version
api_timeout: @json('{"base_url": "https://api.example.com", "version": "v2", "timeout": 5000, "retries": 3}').timeout
```

### User Preferences

```tsk
# User preferences from database or API
user_prefs: @json('{"theme": "dark", "language": "en", "notifications": {"email": true, "push": false, "sms": true}, "privacy": {"profile_public": true, "show_email": false}}')

# Extract specific preferences
user_theme: @json('{"theme": "dark", "language": "en", "notifications": {"email": true, "push": false, "sms": true}, "privacy": {"profile_public": true, "show_email": false}}').theme
email_notifications: @json('{"theme": "dark", "language": "en", "notifications": {"email": true, "push": false, "sms": true}, "privacy": {"profile_public": true, "show_email": false}}').notifications.email
```

## Performance Considerations

### Caching JSON Parsing

```tsk
# Cache expensive JSON parsing operations
cached_json: @cache("5m", @json('{"large": "dataset", "with": "many", "nested": "properties"}'))
```

### Lazy Loading

```javascript
// Load JSON data only when needed
class ConfigManager {
  constructor() {
    this.cache = new Map();
  }
  
  async getJsonData(key) {
    if (!this.cache.has(key)) {
      const config = await tusk.load('config.tsk');
      this.cache.set(key, config[key]);
    }
    return this.cache.get(key);
  }
}
```

## Security Notes

- **Input Validation**: Always validate JSON input before parsing
- **Size Limits**: Be aware of JSON size limits to prevent memory issues
- **Malicious Input**: Sanitize JSON strings to prevent injection attacks

```javascript
// Secure JSON handling
function safeJsonParse(jsonString, fallback = {}) {
  try {
    // Validate JSON string
    if (typeof jsonString !== 'string' || jsonString.length > 10000) {
      return fallback;
    }
    
    const parsed = JSON.parse(jsonString);
    return typeof parsed === 'object' ? parsed : fallback;
  } catch (error) {
    console.warn('JSON parsing failed:', error);
    return fallback;
  }
}
```

## Best Practices

1. **Use Fallbacks**: Always provide fallback values for JSON parsing
2. **Validate Structure**: Check for expected properties before accessing
3. **Handle Errors**: Implement proper error handling for malformed JSON
4. **Cache Results**: Cache parsed JSON for better performance
5. **Type Safety**: Use TypeScript interfaces for better type safety

## Next Steps

- Learn about [@render function](./042-at-render-function-javascript.md) for template rendering
- Explore [@redirect function](./043-at-redirect-function-javascript.md) for URL redirection
- Master [@query database](./044-at-query-database-javascript.md) for database operations 