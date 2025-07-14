# @request.post - Accessing POST Data

The `@request.post` operator provides access to POST data sent in HTTP requests, enabling form processing and API data handling.

## Basic Syntax

```tusk
# Access specific POST field
username: @request.post.username

# Access nested POST data
address: @request.post.user.address.street

# With fallback value
email: @request.post.email|"no-email@example.com"
```

## Form Processing

```tusk
# Process a contact form
contact_form: {
    name: @request.post.name
    email: @request.post.email
    message: @request.post.message
    timestamp: @timestamp
}

# Validate required fields
is_valid: @request.post.name && @request.post.email && @request.post.message

# Save if valid
@if(@is_valid) {
    @query("INSERT INTO contacts", @contact_form)
    response: "Thank you for your message!"
} else {
    response: "Please fill all required fields"
}
```

## JSON POST Data

```tusk
# Handle JSON API requests
api_data: @request.post

# Process webhook data
webhook: {
    event: @request.post.event
    payload: @request.post.data
    signature: @request.headers.x-webhook-signature
}

# Validate webhook
is_valid_webhook: @validate_signature(@webhook.signature, @webhook.payload)
```

## File Uploads

```tusk
# Access uploaded file information
uploaded_file: @request.files.upload

# File properties
file_info: {
    name: @uploaded_file.name
    size: @uploaded_file.size
    type: @uploaded_file.type
    tmp_name: @uploaded_file.tmp_name
}

# Process upload
@if(@file_info.type == "image/jpeg" || @file_info.type == "image/png") {
    @move_uploaded_file(@file_info.tmp_name, "/uploads/" + @file_info.name)
    result: "File uploaded successfully"
} else {
    result: "Invalid file type"
}
```

## Array Handling

```tusk
# Handle array inputs (e.g., checkboxes)
selected_items: @request.post.items[]

# Process each item
@foreach(@selected_items as @item) {
    # Process individual item
    @process_item(@item)
}

# Count selections
selection_count: @count(@selected_items)
```

## Security Considerations

```tusk
# Sanitize input
clean_input: @sanitize(@request.post.user_input)

# Validate data types
age: @int(@request.post.age|0)
price: @float(@request.post.price|0.00)

# Escape for database
safe_query: @escape(@request.post.search_term)

# Check content length
@if(@strlen(@request.post.content) > 10000) {
    error: "Content too long"
}
```

## Common Patterns

```tusk
# Login form processing
#api /login {
    username: @request.post.username
    password: @request.post.password
    
    # Validate credentials
    user: @query("SELECT * FROM users WHERE username = ?", [@username])
    
    @if(@verify_password(@password, @user.password_hash)) {
        @session.user_id: @user.id
        @redirect("/dashboard")
    } else {
        error: "Invalid credentials"
    }
}

# CRUD operations
#api /users/create {
    # Collect all POST data
    user_data: {
        name: @request.post.name
        email: @request.post.email
        role: @request.post.role|"user"
        created_at: @timestamp
    }
    
    # Insert into database
    new_user_id: @query("INSERT INTO users", @user_data)
    
    # Return response
    @json({
        success: true
        user_id: @new_user_id
        message: "User created successfully"
    })
}
```

## Best Practices

1. **Always validate POST data** - Never trust user input
2. **Use fallback values** - Provide defaults for optional fields
3. **Sanitize inputs** - Clean data before processing
4. **Check data types** - Ensure numeric fields contain numbers
5. **Limit input size** - Prevent memory exhaustion attacks

## Related Operators

- `@request` - Parent request object
- `@request.method` - Check if POST request
- `@request.files` - Handle file uploads
- `@sanitize()` - Clean user input
- `@validate()` - Validate data formats