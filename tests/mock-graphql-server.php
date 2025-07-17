<?php
/**
 * 🐘 Mock GraphQL Server for Testing
 * ==================================
 * Simple HTTP server that responds to GraphQL queries for testing
 */

// Start server on port 8080
$host = '127.0.0.1';
$port = 8080;

echo "🚀 Starting Mock GraphQL Server on http://$host:$port\n";
echo "Press Ctrl+C to stop\n\n";

$socket = socket_create(AF_INET, SOCK_STREAM, SOL_TCP);
socket_set_option($socket, SOL_SOCKET, SO_REUSEADDR, 1);
socket_bind($socket, $host, $port);
socket_listen($socket);

while (true) {
    $client = socket_accept($socket);
    $request = socket_read($client, 4096);
    
    // Parse HTTP request
    $lines = explode("\n", $request);
    $firstLine = $lines[0];
    
    if (strpos($firstLine, 'POST /graphql') !== false) {
        // Extract JSON body
        $body = '';
        $inBody = false;
        foreach ($lines as $line) {
            if (trim($line) === '') {
                $inBody = true;
                continue;
            }
            if ($inBody) {
                $body .= $line;
            }
        }
        
        $data = json_decode($body, true);
        $query = $data['query'] ?? '';
        $variables = $data['variables'] ?? [];
        
        // Debug: Log the query being received
        error_log("Mock GraphQL Server received query: " . $query);
        error_log("Mock GraphQL Server received variables: " . json_encode($variables));
        
        // Generate mock response based on query
        $response = generateMockResponse($query, $variables);
        
        $httpResponse = "HTTP/1.1 200 OK\r\n";
        $httpResponse .= "Content-Type: application/json\r\n";
        $httpResponse .= "Access-Control-Allow-Origin: *\r\n";
        $httpResponse .= "Content-Length: " . strlen($response) . "\r\n";
        $httpResponse .= "\r\n";
        $httpResponse .= $response;
        
        socket_write($client, $httpResponse);
    } else {
        // Return 404 for non-GraphQL requests
        $httpResponse = "HTTP/1.1 404 Not Found\r\n";
        $httpResponse .= "Content-Type: text/plain\r\n";
        $httpResponse .= "Content-Length: 13\r\n";
        $httpResponse .= "\r\n";
        $httpResponse .= "Not Found";
        
        socket_write($client, $httpResponse);
    }
    
    socket_close($client);
}

function generateMockResponse($query, $variables) {
    // Simple query
    if (strpos($query, '{ users { id name email } }') !== false) {
        return json_encode([
            'data' => [
                'users' => [
                    ['id' => '1', 'name' => 'John Doe', 'email' => 'john@example.com'],
                    ['id' => '2', 'name' => 'Jane Smith', 'email' => 'jane@example.com'],
                    ['id' => '3', 'name' => 'Bob Johnson', 'email' => 'bob@example.com']
                ]
            ]
        ]);
    }
    
    // Simple users query
    if (strpos($query, '{ users { id name } }') !== false) {
        return json_encode([
            'data' => [
                'users' => [
                    ['id' => '1', 'name' => 'John Doe'],
                    ['id' => '2', 'name' => 'Jane Smith'],
                    ['id' => '3', 'name' => 'Bob Johnson']
                ]
            ]
        ]);
    }
    
    // User by ID query
    if (strpos($query, 'user(id: $id)') !== false || strpos($query, 'user(id: $userId)') !== false) {
        $userId = $variables['id'] ?? $variables['userId'] ?? '123';
        return json_encode([
            'data' => [
                'user' => [
                    'id' => $userId,
                    'name' => 'Test User',
                    'email' => 'test@example.com',
                    'profile' => [
                        'avatar' => 'https://example.com/avatar.jpg',
                        'bio' => 'Test user bio'
                    ],
                    'posts' => [
                        [
                            'id' => '1',
                            'title' => 'First Post',
                            'content' => 'Content 1'
                        ],
                        [
                            'id' => '2', 
                            'title' => 'Second Post',
                            'content' => 'Content 2'
                        ]
                    ]
                ]
            ]
        ]);
    }
    
    // Complex query with fragments
    if (strpos($query, 'fragment') !== false) {
        return json_encode([
            'data' => [
                'users' => [
                    [
                        'id' => '1',
                        'name' => 'John Doe',
                        'email' => 'john@example.com',
                        'profile' => [
                            'avatar' => 'https://example.com/avatar1.jpg',
                            'bio' => 'John\'s bio'
                        ]
                    ],
                    [
                        'id' => '2',
                        'name' => 'Jane Smith', 
                        'email' => 'jane@example.com',
                        'profile' => [
                            'avatar' => 'https://example.com/avatar2.jpg',
                            'bio' => 'Jane\'s bio'
                        ]
                    ]
                ]
            ]
        ]);
    }
    
    // Mutation queries
    if (strpos($query, 'mutation') !== false) {
        return json_encode([
            'data' => [
                'createUser' => [
                    'id' => '999',
                    'name' => 'New User',
                    'email' => 'new@example.com'
                ]
            ]
        ]);
    }
    
    // Schema introspection
    if (strpos($query, '__schema') !== false) {
        return json_encode([
            'data' => [
                '__schema' => [
                    'types' => [
                        ['name' => 'User'],
                        ['name' => 'Post'],
                        ['name' => 'Comment'],
                        ['name' => 'Query'],
                        ['name' => 'Mutation']
                    ]
                ]
            ]
        ]);
    }
    
    // Posts query
    if (strpos($query, 'posts') !== false) {
        return json_encode([
            'data' => [
                'posts' => [
                    ['id' => '1', 'title' => 'First Post', 'content' => 'Content 1'],
                    ['id' => '2', 'title' => 'Second Post', 'content' => 'Content 2']
                ]
            ]
        ]);
    }
    
    // Create user mutation
    if (strpos($query, 'createUser') !== false) {
        $name = $variables['name'] ?? 'New User';
        $email = $variables['email'] ?? 'new@example.com';
        return json_encode([
            'data' => [
                'createUser' => [
                    'id' => '999',
                    'name' => $name,
                    'email' => $email,
                    'createdAt' => date('Y-m-d H:i:s')
                ]
            ]
        ]);
    }
    
    // Update user mutation
    if (strpos($query, 'updateUser') !== false) {
        $id = $variables['id'] ?? '123';
        $name = $variables['name'] ?? 'Updated User';
        return json_encode([
            'data' => [
                'updateUser' => [
                    'id' => $id,
                    'name' => $name,
                    'email' => 'updated@example.com',
                    'updatedAt' => date('Y-m-d H:i:s')
                ]
            ]
        ]);
    }
    
    // Delete user mutation
    if (strpos($query, 'deleteUser') !== false) {
        return json_encode([
            'data' => [
                'deleteUser' => [
                    'success' => true,
                    'message' => 'User deleted successfully'
                ]
            ]
        ]);
    }
    
    // User stats query
    if (strpos($query, 'userStats') !== false) {
        return json_encode([
            'data' => [
                'userStats' => [
                    'totalUsers' => 1000,
                    'activeUsers' => 750,
                    'newUsersThisMonth' => 50,
                    'averageSessionDuration' => 1800
                ]
            ]
        ]);
    }
    
    // Default response for unknown queries
    return json_encode([
        'data' => null,
        'errors' => [
            [
                'message' => 'Unknown query',
                'locations' => [['line' => 1, 'column' => 1]]
            ]
        ]
    ]);
} 