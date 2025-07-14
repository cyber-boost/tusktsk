# @ Request Query

The @request.query object provides access to URL query parameters in TuskLang web applications. This guide covers parsing, validating, and working with query strings.

## Basic Query Access

### Simple Parameters

```tusk
# URL: /search?q=tusklang&page=2&limit=10

# Access query parameters
search_term = @request.query.q       # "tusklang"
page = @request.query.page          # "2" (string)
limit = @request.query.limit        # "10" (string)

# All parameters as object
all_params = @request.query         # { q: "tusklang", page: "2", limit: "10" }
```

### Type Conversion

```tusk
# Query parameters are always strings, convert as needed
page_number = @int(@request.query.page ?? "1")
items_limit = @int(@request.query.limit ?? "20")
include_draft = @request.query.draft == "true"

# Safe conversion with validation
safe_int = @lambda(value, default_value, {
    num = @int(value)
    return: @isNaN(num) ? default_value : num
})

page = @safe_int(@request.query.page, 1)
```

## Query Parameter Patterns

### Optional Parameters with Defaults

```tusk
# Handle missing parameters gracefully
search_params:
    query: @request.query.q ?? ""
    page: @int(@request.query.page ?? "1")
    limit: @int(@request.query.limit ?? "20")
    sort: @request.query.sort ?? "relevance"
    order: @request.query.order ?? "desc"
    filters: @request.query.filters ?? "all"

# Validate ranges
validated_params:
    page: @max(1, @int(@request.query.page ?? "1"))
    limit: @clamp(@int(@request.query.limit ?? "20"), 1, 100)
```

### Boolean Parameters

```tusk
# Different boolean conventions
parse_boolean = @lambda(value, default_value = false, {
    @if(value == null, return: default_value)
    
    # Common truthy values
    truthy: ["true", "1", "yes", "on", "y", "t"]
    return: @includes(@lower(value), truthy)
})

# Usage
include_archived = @parse_boolean(@request.query.archived)
show_details = @parse_boolean(@request.query.details, true)
debug_mode = @parse_boolean(@request.query.debug)
```

## Array Parameters

### Multiple Values

```tusk
# URL: /filter?tags=javascript&tags=web&tags=frontend
# Or: /filter?tags[]=javascript&tags[]=web&tags[]=frontend

# Access array parameters
tags = @request.query.tags  # ["javascript", "web", "frontend"]

# Handle both single and multiple values
ensure_array = @lambda(value, {
    @if(value == null, return: [])
    @if(@isArray(value), return: value)
    return: [value]
})

selected_tags = @ensure_array(@request.query.tags)
```

### Comma-Separated Values

```tusk
# URL: /filter?ids=1,2,3,4,5

# Parse comma-separated values
ids_string = @request.query.ids ?? ""
ids = @map(@split(ids_string, ","), @lambda(id, @int(@trim(id))))

# More robust parsing
parse_csv = @lambda(value, separator = ",", {
    @if(!value, return: [])
    
    items = @split(value, separator)
    return: @map(items, @lambda(item, @trim(item)))
        .filter(@lambda(item, item != ""))
})

categories = @parse_csv(@request.query.categories)
```

## Complex Query Structures

### Nested Parameters

```tusk
# URL: /search?filter[category]=books&filter[price][min]=10&filter[price][max]=50

# Parse nested structures
parse_nested_query = @lambda(query, {
    result: {}
    
    @each(@keys(query), @lambda(key, {
        # Check for bracket notation
        matches = @regex.match(key, "^([^\\[]+)(\\[.+\\])$")
        
        @if(matches, {
            # Parse nested key
            base_key = matches[1]
            nested_path = matches[2]
            # Implementation for nested parsing...
        }, {
            # Simple key
            result[key] = query[key]
        })
    }))
    
    return: result
})

# Common pattern: object-like parameters
filters:
    category: @request.query["filter[category]"]
    min_price: @int(@request.query["filter[price][min]"] ?? "0")
    max_price: @int(@request.query["filter[price][max]"] ?? "999999")
```

### JSON in Query

```tusk
# URL: /api?data={"name":"John","age":30}

# Parse JSON from query parameter
parse_json_param = @lambda(param_name, default_value = {}, {
    json_string = @request.query[param_name]
    @if(!json_string, return: default_value)
    
    return: @try({
        return: @json.parse(@url.decode(json_string))
    }, {
        @log.warn("Invalid JSON in query parameter: ${param_name}")
        return: default_value
    })
})

# Usage
data = @parse_json_param("data")
config = @parse_json_param("config", { theme: "default" })
```

## Query String Parsing

### Manual Query Parsing

```tusk
# Parse query string manually
parse_query_string = @lambda(query_string, {
    @if(!query_string, return: {})
    
    # Remove leading ?
    clean_query = @regex.replace(query_string, "^\\?", "")
    
    # Split into key-value pairs
    pairs = @split(clean_query, "&")
    result: {}
    
    @each(pairs, @lambda(pair, {
        [key, value] = @split(pair, "=")
        
        # Decode key and value
        decoded_key = @url.decode(key)
        decoded_value = @url.decode(value ?? "")
        
        # Handle array notation
        @if(@regex.test(decoded_key, "\\[\\]$"), {
            array_key = @regex.replace(decoded_key, "\\[\\]$", "")
            result[array_key] = result[array_key] ?? []
            @push(result[array_key], decoded_value)
        }, {
            result[decoded_key] = decoded_value
        })
    }))
    
    return: result
})
```

### Query String Building

```tusk
# Build query string from object
build_query_string = @lambda(params, {
    pairs: []
    
    @each(@keys(params), @lambda(key, {
        value = params[key]
        
        @if(@isArray(value), {
            # Handle arrays
            @each(value, @lambda(item, {
                @push(pairs, "${@url.encode(key)}[]=${@url.encode(item)}")
            }))
        }, {
            # Handle single values
            @if(value != null && value != "", {
                @push(pairs, "${@url.encode(key)}=${@url.encode(@string(value))}")
            })
        })
    }))
    
    return: @join(pairs, "&")
})

# Usage
query = @build_query_string({
    q: "search term"
    page: 2
    tags: ["javascript", "web"]
})
# Result: "q=search%20term&page=2&tags[]=javascript&tags[]=web"
```

## Validation and Sanitization

### Query Parameter Validation

```tusk
# Define validation schema
query_schema: {
    q: { type: "string", required: false, max_length: 100 },
    page: { type: "integer", required: false, min: 1 },
    limit: { type: "integer", required: false, min: 1, max: 100 },
    sort: { type: "enum", values: ["relevance", "date", "title"] },
    order: { type: "enum", values: ["asc", "desc"] }
}

# Validate query parameters
validate_query = @lambda(schema, {
    errors: []
    validated: {}
    
    @each(@keys(schema), @lambda(param, {
        rule = schema[param]
        value = @request.query[param]
        
        # Check required
        @if(rule.required && !value, {
            @push(errors, "${param} is required")
            return
        })
        
        # Skip if not provided and not required
        @if(!value && !rule.required, return)
        
        # Type validation
        @switch(rule.type, {
            "string": {
                @if(rule.max_length && @len(value) > rule.max_length, {
                    @push(errors, "${param} exceeds maximum length")
                })
                validated[param] = value
            },
            "integer": {
                num = @int(value)
                @if(@isNaN(num), {
                    @push(errors, "${param} must be a number")
                    return
                })
                @if(rule.min != null && num < rule.min, {
                    @push(errors, "${param} must be at least ${rule.min}")
                })
                @if(rule.max != null && num > rule.max, {
                    @push(errors, "${param} must be at most ${rule.max}")
                })
                validated[param] = num
            },
            "enum": {
                @if(!@includes(rule.values, value), {
                    @push(errors, "${param} must be one of: ${@join(rule.values, ', ')}")
                })
                validated[param] = value
            }
        })
    }))
    
    return: {
        valid: @len(errors) == 0
        errors: errors
        data: validated
    }
})
```

### Sanitization

```tusk
# Sanitize query parameters
sanitize_query = @lambda({
    sanitized: {}
    
    # Define sanitization rules
    @each(@keys(@request.query), @lambda(key, {
        value = @request.query[key]
        
        # Skip potentially dangerous keys
        @if(@regex.test(key, "^__"), return)
        
        # Sanitize key
        safe_key = @regex.replace(key, "[^a-zA-Z0-9_\\[\\]]", "")
        
        # Sanitize value based on expected type
        @if(@isArray(value), {
            sanitized[safe_key] = @map(value, @lambda(v, {
                # Remove HTML/script tags
                @regex.replace(v, "<[^>]*>", "")
            }))
        }, {
            # Single value sanitization
            sanitized[safe_key] = @regex.replace(value, "<[^>]*>", "")
        })
    }))
    
    return: sanitized
})
```

## Pagination with Query Parameters

### Standard Pagination

```tusk
# Extract pagination parameters
parse_pagination = @lambda(defaults = {}, {
    page = @max(1, @int(@request.query.page ?? defaults.page ?? 1))
    limit = @clamp(
        @int(@request.query.limit ?? defaults.limit ?? 20),
        1,
        100  # Maximum items per page
    )
    
    offset = (page - 1) * limit
    
    return: {
        page: page
        limit: limit
        offset: offset
    }
})

# Usage in query
pagination = @parse_pagination({ limit: 25 })
results = @db.query(
    "SELECT * FROM items LIMIT ? OFFSET ?",
    pagination.limit,
    pagination.offset
)
```

### Cursor-Based Pagination

```tusk
# Parse cursor pagination
parse_cursor_pagination = @lambda({
    cursor = @request.query.cursor
    limit = @clamp(@int(@request.query.limit ?? "20"), 1, 100)
    
    # Decode cursor (base64 encoded timestamp or ID)
    decoded_cursor = @if(cursor, {
        @try({
            return: @base64.decode(cursor)
        }, {
            return: null
        })
    }, null)
    
    return: {
        cursor: decoded_cursor
        limit: limit
        direction: @request.query.direction ?? "next"
    }
})

# Generate next/previous cursors
generate_cursor = @lambda(last_item, {
    @if(!last_item, return: null)
    
    # Encode relevant data for cursor
    cursor_data = {
        id: last_item.id
        timestamp: last_item.created_at
    }
    
    return: @base64.encode(@json.stringify(cursor_data))
})
```

## Search and Filtering

### Advanced Search Parameters

```tusk
# Parse complex search parameters
parse_search_params = @lambda({
    # Basic search
    query = @request.query.q ?? ""
    
    # Parse search operators
    filters: {}
    
    # Price range
    @if(@request.query.price_min || @request.query.price_max, {
        filters.price = {
            min: @float(@request.query.price_min ?? "0")
            max: @float(@request.query.price_max ?? "999999")
        }
    })
    
    # Date range
    @if(@request.query.date_from || @request.query.date_to, {
        filters.date = {
            from: @request.query.date_from
            to: @request.query.date_to
        }
    })
    
    # Categories (multiple)
    @if(@request.query.category, {
        filters.categories = @ensure_array(@request.query.category)
    })
    
    # Boolean filters
    filters.in_stock = @parse_boolean(@request.query.in_stock)
    filters.on_sale = @parse_boolean(@request.query.on_sale)
    
    # Sorting
    sort = {
        field: @request.query.sort ?? "relevance"
        order: @request.query.order ?? "desc"
    }
    
    return: {
        query: query
        filters: filters
        sort: sort
        facets: @parse_csv(@request.query.facets)
    }
})
```

### Building Search Queries

```tusk
# Convert search params to database query
build_search_query = @lambda(params, {
    conditions: []
    values: []
    
    # Text search
    @if(params.query, {
        @push(conditions, "(title LIKE ? OR description LIKE ?)")
        search_term = "%${params.query}%"
        @push(values, search_term)
        @push(values, search_term)
    })
    
    # Price filter
    @if(params.filters.price, {
        @push(conditions, "price BETWEEN ? AND ?")
        @push(values, params.filters.price.min)
        @push(values, params.filters.price.max)
    })
    
    # Category filter
    @if(params.filters.categories && @len(params.filters.categories) > 0, {
        placeholders = @join(@repeat("?", @len(params.filters.categories)), ",")
        @push(conditions, "category IN (${placeholders})")
        values = @concat(values, params.filters.categories)
    })
    
    # Build final query
    where_clause = @if(@len(conditions) > 0,
        "WHERE ${@join(conditions, ' AND ')}",
        ""
    )
    
    order_clause = "ORDER BY ${params.sort.field} ${params.sort.order}"
    
    return: {
        sql: "SELECT * FROM products ${where_clause} ${order_clause}"
        params: values
    }
})
```

## URL State Management

### Preserving Query Parameters

```tusk
# Merge new parameters with existing
update_query_params = @lambda(updates, {
    # Start with current parameters
    current = { ...@request.query }
    
    # Apply updates
    @each(@keys(updates), @lambda(key, {
        value = updates[key]
        
        @if(value == null || value == "", {
            # Remove parameter
            delete current[key]
        }, {
            # Update parameter
            current[key] = value
        })
    }))
    
    return: current
})

# Generate URL with updated params
generate_url = @lambda(path, params, {
    query_string = @build_query_string(params)
    return: @if(query_string, "${path}?${query_string}", path)
})

# Usage: pagination links
next_page_url = @generate_url(@request.path, @update_query_params({
    page: @string(current_page + 1)
}))
```

## Security Considerations

### Query Parameter Injection

```tusk
# Prevent SQL injection from query params
safe_search = @lambda({
    # Never directly interpolate query params into SQL
    # BAD:
    # query = "SELECT * FROM users WHERE name = '${@request.query.name}'"
    
    # GOOD: Use parameterized queries
    name = @request.query.name
    results = @db.query(
        "SELECT * FROM users WHERE name = ?",
        name
    )
    
    return: results
})
```

### XSS Prevention

```tusk
# Escape query parameters for display
escape_for_html = @lambda(value, {
    @if(@isArray(value), {
        return: @map(value, @escape_for_html)
    })
    
    return: @html.escape(@string(value))
})

# Safe display of search term
safe_display = @lambda({
    search_term = @escape_for_html(@request.query.q ?? "")
    return: "<h1>Search results for: ${search_term}</h1>"
})
```

## Best Practices

1. **Always validate and sanitize** query parameters
2. **Convert types explicitly** - Query params are strings
3. **Use defaults** for missing parameters
4. **Limit array sizes** to prevent abuse
5. **Escape for display** to prevent XSS
6. **Use parameterized queries** to prevent SQL injection
7. **Document expected parameters** in API documentation
8. **Consider URL length limits** (around 2000 characters)

## Common Patterns

### Filter Builder

```tusk
# Generic filter builder from query params
build_filters = @lambda(allowed_fields, {
    filters: {}
    
    @each(allowed_fields, @lambda(field, {
        value = @request.query[field]
        @if(value != null && value != "", {
            filters[field] = value
        })
    }))
    
    return: filters
})

# Usage
product_filters = @build_filters([
    "category", 
    "brand", 
    "color", 
    "size"
])
```

### API Response with Query Echo

```tusk
# Include query parameters in response for transparency
api_response = @lambda(data, {
    return: {
        data: data
        query: @request.query
        pagination: {
            page: @int(@request.query.page ?? "1")
            limit: @int(@request.query.limit ?? "20")
            total: data.total_count
        }
        links: {
            self: @request.url
            next: @generate_next_page_url()
            prev: @generate_prev_page_url()
        }
    }
})
```

## Next Steps

- Learn about [Request Body](037-at-request-body.md)
- Explore [Request Headers](038-at-request-headers.md)
- Master [Request IP Address](039-at-request-ip.md)