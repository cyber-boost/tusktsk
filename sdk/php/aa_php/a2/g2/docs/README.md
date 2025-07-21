# GraphQL Implementation (A2-G2)

## Overview

The GraphQL Implementation module provides a complete GraphQL server with advanced features for building modern API services that offer flexibility, efficiency, and powerful developer tools.

## Features

### GraphQL Server
- Complete GraphQL query/mutation/subscription support
- Schema validation and introspection
- Query complexity analysis and depth limiting
- Data loader integration for N+1 problem prevention
- Batch query processing
- Custom error handling and formatting
- Performance monitoring and metrics

### Schema Management
- Schema Definition Language (SDL) parsing
- Dynamic schema building from arrays
- Custom scalar types (DateTime, JSON, Upload)
- Custom directive support (@auth, @rateLimit, @cache)
- Schema stitching and federation capabilities
- Automatic schema validation

### Security & Performance
- Query complexity analysis
- Depth limiting
- Introspection control
- Rate limiting integration
- Input validation and sanitization
- CSRF protection
- Data loader batching

## Quick Start

### Basic GraphQL Server

```php
use TuskLang\Communication\GraphQL\GraphQLServer;
use TuskLang\Communication\GraphQL\SchemaBuilder;
use GraphQL\Type\Schema;
use GraphQL\Type\Definition\ObjectType;
use GraphQL\Type\Definition\Type;

// Create schema
$schema = new Schema([
    'query' => new ObjectType([
        'name' => 'Query',
        'fields' => [
            'hello' => [
                'type' => Type::string(),
                'resolve' => function() {
                    return 'Hello World!';
                }
            ],
            'user' => [
                'type' => $userType,
                'args' => ['id' => ['type' => Type::nonNull(Type::id())]],
                'resolve' => function($root, $args, $context) {
                    return $context->getDataLoaders()->load('users', $args['id']);
                }
            ]
        ]
    ])
]);

// Create server
$server = new GraphQLServer($schema, [
    'debug' => false,
    'max_query_complexity' => 1000,
    'max_query_depth' => 15,
    'introspection' => true
]);

// Handle request
$response = $server->handle($request);
```

### Schema from SDL

```php
use TuskLang\Communication\GraphQL\SchemaBuilder;

$sdl = '
    type User {
        id: ID!
        name: String!
        email: String!
        posts: [Post!]! @auth
    }
    
    type Post {
        id: ID!
        title: String!
        content: String!
        author: User! @cache(ttl: 300)
    }
    
    type Query {
        user(id: ID!): User
        posts(limit: Int = 10): [Post!]!
    }
    
    type Mutation {
        createPost(input: CreatePostInput!): Post! @auth @rateLimit(limit: 5)
    }
    
    input CreatePostInput {
        title: String!
        content: String!
    }
';

$resolvers = [
    'Query' => [
        'user' => function($root, $args, $context) {
            return $context->getDataLoaders()->load('users', $args['id']);
        },
        'posts' => function($root, $args) {
            return getPosts($args['limit']);
        }
    ],
    'Mutation' => [
        'createPost' => function($root, $args, $context) {
            if (!$context->isAuthenticated()) {
                throw new GraphQLException('Authentication required');
            }
            return createPost($args['input']);
        }
    ],
    'User' => [
        'posts' => function($user, $args, $context) {
            return $context->getDataLoaders()->load('userPosts', $user['id']);
        }
    ]
];

$builder = new SchemaBuilder();
$schema = $builder->buildFromSDL($sdl, $resolvers);
```

## Advanced Features

### Data Loaders (N+1 Problem Prevention)

```php
// Register data loaders
$server->registerDataLoader('users', function($userIds) {
    return User::whereIn('id', $userIds)->get()->keyBy('id')->toArray();
});

$server->registerDataLoader('userPosts', function($userIds) {
    $posts = Post::whereIn('user_id', $userIds)->get();
    return $posts->groupBy('user_id')->toArray();
});

// Use in resolvers
$userType = new ObjectType([
    'name' => 'User',
    'fields' => [
        'id' => ['type' => Type::id()],
        'name' => ['type' => Type::string()],
        'posts' => [
            'type' => Type::listOf($postType),
            'resolve' => function($user, $args, $context) {
                // This will be batched automatically
                return $context->getDataLoaders()->load('userPosts', $user['id']);
            }
        ]
    ]
]);
```

### Custom Scalars

```php
$builder = new SchemaBuilder();

// DateTime scalar
$dateTimeScalar = new ScalarType([
    'name' => 'DateTime',
    'serialize' => function($value) {
        return $value instanceof DateTime ? $value->format('c') : $value;
    },
    'parseValue' => function($value) {
        return new DateTime($value);
    },
    'parseLiteral' => function($valueNode) {
        return new DateTime($valueNode->value);
    }
]);

$builder->addScalar('DateTime', $dateTimeScalar);

// Use in schema
$sdl = '
    scalar DateTime
    
    type Post {
        id: ID!
        title: String!
        createdAt: DateTime!
    }
';
```

### Custom Directives

```php
// @auth directive
$server = new GraphQLServer($schema);

$authMiddleware = function($resolve, $source, $args, $context, $info) {
    // Check if field has @auth directive
    $directives = $info->fieldDefinition->astNode->directives ?? [];
    
    foreach ($directives as $directive) {
        if ($directive->name->value === 'auth') {
            if (!$context->isAuthenticated()) {
                throw new GraphQLException('Authentication required');
            }
            break;
        }
    }
    
    return $resolve($source, $args, $context, $info);
};

$server->setFieldResolver($authMiddleware);
```

### Query Complexity Analysis

```php
$server = new GraphQLServer($schema, [
    'max_query_complexity' => 1000,
    'max_query_depth' => 15,
    'field_complexity' => [
        'users' => 10,      // Expensive field
        'search' => 20,     // Very expensive
        'reports' => 50     // Extremely expensive
    ]
]);

// Custom complexity analysis
$server->setComplexityLimits(500, 10);
```

### Batch Queries

```php
// Client sends multiple queries
$batchRequest = [
    ['query' => '{ user(id: "1") { name } }'],
    ['query' => '{ user(id: "2") { name } }'],
    ['query' => '{ posts(limit: 5) { title } }']
];

// Server processes all at once
$response = $server->handle($request); // Returns array of results
```

### Error Handling

```php
$server = new GraphQLServer($schema, [
    'debug' => false,
    'error_formatter' => function($error) {
        return [
            'message' => $error->getMessage(),
            'code' => $error->getCode(),
            'path' => $error->getPath(),
            'extensions' => [
                'category' => 'business_logic'
            ]
        ];
    },
    'error_handler' => function($errors, $formatter) {
        // Log errors
        foreach ($errors as $error) {
            error_log('GraphQL Error: ' . $error->getMessage());
        }
        
        return array_map($formatter, $errors);
    }
]);
```

### Subscription Support

```php
$schema = new Schema([
    'query' => $queryType,
    'mutation' => $mutationType,
    'subscription' => new ObjectType([
        'name' => 'Subscription',
        'fields' => [
            'postAdded' => [
                'type' => $postType,
                'resolve' => function($root, $args, $context) {
                    // Return new posts as they're created
                    return $context->getEventStream('postAdded');
                }
            ],
            'userOnline' => [
                'type' => Type::boolean(),
                'args' => ['userId' => ['type' => Type::nonNull(Type::id())]],
                'resolve' => function($root, $args, $context) {
                    return $context->getPresenceStream($args['userId']);
                }
            ]
        ]
    ])
]);
```

## Security Best Practices

### Query Validation

```php
$server = new GraphQLServer($schema, [
    'max_query_complexity' => 1000,
    'max_query_depth' => 10,
    'max_query_length' => 10000,
    'introspection' => false, // Disable in production
    'debug' => false          // Never enable in production
]);

// Add custom validation rules
$server->addValidationRules([
    new \GraphQL\Validator\Rules\QueryComplexity(1000),
    new \GraphQL\Validator\Rules\QueryDepth(10),
    new \GraphQL\Validator\Rules\DisableIntrospection()
]);
```

### Rate Limiting

```php
// Per-field rate limiting
$server->middleware(function($request, $next) {
    $context = $request->getContext();
    $user = $context->getUser();
    
    if (!HttpSecurity::checkRateLimit($user['id'] ?? 'anonymous', 100)) {
        throw new GraphQLException('Rate limit exceeded');
    }
    
    return $next($request);
});
```

### Input Sanitization

```php
$resolvers = [
    'Mutation' => [
        'createPost' => function($root, $args, $context) {
            // Sanitize input
            $input = HttpSecurity::sanitizeInput($args['input']);
            
            // Validate
            $validator = new ApiValidator();
            $errors = $validator->validate($input, [
                'title' => 'required|string|min:3|max:100',
                'content' => 'required|string|min:10|max:5000'
            ]);
            
            if (!empty($errors)) {
                throw new GraphQLException('Validation failed', null, null, [], $errors);
            }
            
            return createPost($input);
        }
    ]
];
```

## Performance Optimization

### Caching

```php
// Query result caching
$server->enableQueryCache($cacheInstance);

// Field-level caching with directive
$sdl = '
    type User {
        profile: UserProfile @cache(ttl: 3600)
        stats: UserStats @cache(ttl: 300)
    }
';
```

### Persisted Queries

```php
// Enable persisted queries to reduce bandwidth
$queryStorage = new RedisQueryStorage($redis);
$server->enablePersistedQueries($queryStorage);

// Client sends query hash instead of full query
// Server retrieves full query from storage
```

### Connection Pooling

```php
// Use with database connections
$server->registerDataLoader('users', function($ids) use ($connectionPool) {
    $connection = $connectionPool->getConnection();
    try {
        return $connection->select("SELECT * FROM users WHERE id IN (?)", [$ids]);
    } finally {
        $connectionPool->releaseConnection($connection);
    }
});
```

## Integration Examples

### With HTTP API Server

```php
use TuskLang\Communication\Http\RestApiServer;

$apiServer = new RestApiServer();

// GraphQL endpoint
$apiServer->post('/graphql', function($request) use ($graphqlServer) {
    return $graphqlServer->handle($request);
});

// GraphiQL interface
$apiServer->get('/graphiql', function() use ($graphqlServer) {
    return new ApiResponse(200, ['Content-Type' => 'text/html'], 
        $graphqlServer->renderGraphiQL('/graphql')
    );
});
```

### With Authentication

```php
$server->middleware(function($request, $next) {
    $context = $request->getContext();
    $authHeader = $request->getHeader('Authorization');
    
    if ($authHeader && str_starts_with($authHeader, 'Bearer ')) {
        $token = substr($authHeader, 7);
        $user = validateJWT($token); // Your JWT validation
        $context->set('user', $user);
    }
    
    return $next($request);
});
```

## Testing

### Unit Testing

```php
class GraphQLTest extends TestCase
{
    public function testUserQuery()
    {
        $server = new GraphQLServer($this->createTestSchema());
        
        $query = '{ user(id: "1") { name, email } }';
        $result = $server->executeQuery($query);
        
        $this->assertNull($result->errors);
        $this->assertEquals('John Doe', $result->data['user']['name']);
    }
    
    public function testComplexityLimiting()
    {
        $server = new GraphQLServer($this->schema, ['max_query_complexity' => 10]);
        
        $complexQuery = '{ users { posts { comments { replies } } } }';
        $result = $server->executeQuery($complexQuery);
        
        $this->assertNotNull($result->errors);
        $this->assertStringContains('complexity', $result->errors[0]->getMessage());
    }
}
```

### Integration Testing

```php
class GraphQLIntegrationTest extends TestCase
{
    public function testFullWorkflow()
    {
        // Test complete GraphQL workflow
        $request = $this->createGraphQLRequest([
            'query' => 'mutation($input: CreatePostInput!) { createPost(input: $input) { id, title } }',
            'variables' => ['input' => ['title' => 'Test Post', 'content' => 'Test content']]
        ]);
        
        $response = $this->server->handle($request);
        $data = json_decode($response->getBody(), true);
        
        $this->assertEquals(200, $response->getStatusCode());
        $this->assertArrayHasKey('data', $data);
        $this->assertArrayHasKey('createPost', $data['data']);
    }
}
```

## Best Practices

1. **Use Data Loaders** - Always implement data loaders for related data to prevent N+1 queries
2. **Limit Query Complexity** - Set reasonable complexity and depth limits
3. **Disable Introspection in Production** - Prevent schema discovery
4. **Implement Proper Authentication** - Use context to pass user information
5. **Validate All Inputs** - Sanitize and validate all mutation inputs
6. **Cache Expensive Operations** - Use field-level caching for expensive computations
7. **Monitor Performance** - Track query complexity and execution time
8. **Handle Errors Gracefully** - Provide meaningful error messages without exposing internals
9. **Use Persisted Queries** - Reduce bandwidth and improve security
10. **Test Thoroughly** - Test all queries, mutations, and edge cases

## Related Modules

- **A2-G1**: REST API integration for hybrid endpoints
- **A3-G2**: JWT/OAuth authentication integration
- **A2-G3**: WebSocket subscriptions for real-time features
- **A1-G1**: Database integration with connection pooling
- **A5-G1**: Data processing integration for analytics queries 