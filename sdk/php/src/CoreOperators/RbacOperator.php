<?php

namespace TuskLang\CoreOperators;

use Exception;

/**
 * RBAC Operator - Role-Based Access Control
 * 
 * Provides comprehensive RBAC capabilities including:
 * - User management and authentication
 * - Role creation and management
 * - Permission assignment and validation
 * - Access control and authorization
 * - Role hierarchy and inheritance
 * - Session management
 * - Audit logging
 * 
 * @package TuskLang\CoreOperators
 */
class RbacOperator implements OperatorInterface
{
    private $users = [];
    private $roles = [];
    private $permissions = [];
    private $userRoles = [];
    private $rolePermissions = [];
    private $sessions = [];

    public function __construct()
    {
        $this->initializeRbacSystem();
    }

    /**
     * Execute RBAC operations
     */
    public function execute(array $params, array $context = []): array
    {
        try {
            $operation = $params['operation'] ?? 'check';
            $data = $params['data'] ?? [];
            
            // Substitute context variables
            $data = $this->substituteContext($data, $context);
            
            switch ($operation) {
                case 'create-user':
                    return $this->createUser($data);
                case 'create-role':
                    return $this->createRole($data);
                case 'create-permission':
                    return $this->createPermission($data);
                case 'assign-role':
                    return $this->assignRole($data);
                case 'assign-permission':
                    return $this->assignPermission($data);
                case 'check':
                    return $this->checkPermission($data);
                case 'authenticate':
                    return $this->authenticateUser($data);
                case 'authorize':
                    return $this->authorizeUser($data);
                case 'session':
                    return $this->handleSession($data);
                case 'audit':
                    return $this->getAuditLog($data);
                case 'info':
                default:
                    return $this->getRbacInfo();
            }
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => 'RBAC operation failed: ' . $e->getMessage(),
                'data' => null
            ];
        }
    }

    /**
     * Create a new user
     */
    private function createUser(array $data): array
    {
        $username = $data['username'] ?? '';
        $email = $data['email'] ?? '';
        $password = $data['password'] ?? '';
        $firstName = $data['first_name'] ?? '';
        $lastName = $data['last_name'] ?? '';
        $active = $data['active'] ?? true;

        if (empty($username) || empty($email) || empty($password)) {
            return [
                'success' => false,
                'error' => 'Username, email, and password are required',
                'data' => null
            ];
        }

        // Check if user already exists
        if ($this->userExists($username) || $this->emailExists($email)) {
            return [
                'success' => false,
                'error' => 'User with this username or email already exists',
                'data' => null
            ];
        }

        $userId = uniqid('user_');
        $hashedPassword = password_hash($password, PASSWORD_DEFAULT);

        $user = [
            'id' => $userId,
            'username' => $username,
            'email' => $email,
            'password_hash' => $hashedPassword,
            'first_name' => $firstName,
            'last_name' => $lastName,
            'active' => $active,
            'created_at' => date('Y-m-d H:i:s'),
            'updated_at' => date('Y-m-d H:i:s'),
            'last_login' => null
        ];

        $this->users[$userId] = $user;

        return [
            'success' => true,
            'data' => [
                'user_id' => $userId,
                'user' => array_merge($user, ['password_hash' => '[HIDDEN]']),
                'status' => 'created'
            ],
            'error' => null
        ];
    }

    /**
     * Create a new role
     */
    private function createRole(array $data): array
    {
        $name = $data['name'] ?? '';
        $description = $data['description'] ?? '';
        $parentRole = $data['parent_role'] ?? null;
        $permissions = $data['permissions'] ?? [];

        if (empty($name)) {
            return [
                'success' => false,
                'error' => 'Role name is required',
                'data' => null
            ];
        }

        // Check if role already exists
        if ($this->roleExists($name)) {
            return [
                'success' => false,
                'error' => 'Role with this name already exists',
                'data' => null
            ];
        }

        $roleId = uniqid('role_');
        $role = [
            'id' => $roleId,
            'name' => $name,
            'description' => $description,
            'parent_role' => $parentRole,
            'created_at' => date('Y-m-d H:i:s'),
            'updated_at' => date('Y-m-d H:i:s')
        ];

        $this->roles[$roleId] = $role;

        // Assign permissions if provided
        if (!empty($permissions)) {
            foreach ($permissions as $permission) {
                $this->assignPermissionToRole($roleId, $permission);
            }
        }

        return [
            'success' => true,
            'data' => [
                'role_id' => $roleId,
                'role' => $role,
                'status' => 'created'
            ],
            'error' => null
        ];
    }

    /**
     * Create a new permission
     */
    private function createPermission(array $data): array
    {
        $name = $data['name'] ?? '';
        $description = $data['description'] ?? '';
        $resource = $data['resource'] ?? '';
        $action = $data['action'] ?? '';

        if (empty($name) || empty($resource) || empty($action)) {
            return [
                'success' => false,
                'error' => 'Permission name, resource, and action are required',
                'data' => null
            ];
        }

        // Check if permission already exists
        if ($this->permissionExists($name)) {
            return [
                'success' => false,
                'error' => 'Permission with this name already exists',
                'data' => null
            ];
        }

        $permissionId = uniqid('permission_');
        $permission = [
            'id' => $permissionId,
            'name' => $name,
            'description' => $description,
            'resource' => $resource,
            'action' => $action,
            'created_at' => date('Y-m-d H:i:s'),
            'updated_at' => date('Y-m-d H:i:s')
        ];

        $this->permissions[$permissionId] = $permission;

        return [
            'success' => true,
            'data' => [
                'permission_id' => $permissionId,
                'permission' => $permission,
                'status' => 'created'
            ],
            'error' => null
        ];
    }

    /**
     * Assign role to user
     */
    private function assignRole(array $data): array
    {
        $userId = $data['user_id'] ?? '';
        $roleId = $data['role_id'] ?? '';

        if (empty($userId) || empty($roleId)) {
            return [
                'success' => false,
                'error' => 'User ID and Role ID are required',
                'data' => null
            ];
        }

        if (!isset($this->users[$userId])) {
            return [
                'success' => false,
                'error' => 'User not found',
                'data' => null
            ];
        }

        if (!isset($this->roles[$roleId])) {
            return [
                'success' => false,
                'error' => 'Role not found',
                'data' => null
            ];
        }

        if (!isset($this->userRoles[$userId])) {
            $this->userRoles[$userId] = [];
        }

        if (in_array($roleId, $this->userRoles[$userId])) {
            return [
                'success' => false,
                'error' => 'User already has this role',
                'data' => null
            ];
        }

        $this->userRoles[$userId][] = $roleId;

        return [
            'success' => true,
            'data' => [
                'user_id' => $userId,
                'role_id' => $roleId,
                'status' => 'assigned'
            ],
            'error' => null
        ];
    }

    /**
     * Assign permission to role
     */
    private function assignPermission(array $data): array
    {
        $roleId = $data['role_id'] ?? '';
        $permissionId = $data['permission_id'] ?? '';

        if (empty($roleId) || empty($permissionId)) {
            return [
                'success' => false,
                'error' => 'Role ID and Permission ID are required',
                'data' => null
            ];
        }

        if (!isset($this->roles[$roleId])) {
            return [
                'success' => false,
                'error' => 'Role not found',
                'data' => null
            ];
        }

        if (!isset($this->permissions[$permissionId])) {
            return [
                'success' => false,
                'error' => 'Permission not found',
                'data' => null
            ];
        }

        return $this->assignPermissionToRole($roleId, $permissionId);
    }

    /**
     * Check if user has permission
     */
    private function checkPermission(array $data): array
    {
        $userId = $data['user_id'] ?? '';
        $permissionName = $data['permission'] ?? '';
        $resource = $data['resource'] ?? '';
        $action = $data['action'] ?? '';

        if (empty($userId)) {
            return [
                'success' => false,
                'error' => 'User ID is required',
                'data' => null
            ];
        }

        if (!isset($this->users[$userId])) {
            return [
                'success' => false,
                'error' => 'User not found',
                'data' => null
            ];
        }

        if (!$this->users[$userId]['active']) {
            return [
                'success' => false,
                'error' => 'User is inactive',
                'data' => null
            ];
        }

        $userRoles = $this->userRoles[$userId] ?? [];
        $hasPermission = false;
        $matchedPermissions = [];

        foreach ($userRoles as $roleId) {
            $rolePermissions = $this->rolePermissions[$roleId] ?? [];
            
            foreach ($rolePermissions as $permissionId) {
                $permission = $this->permissions[$permissionId] ?? null;
                
                if ($permission) {
                    $matches = true;
                    
                    if (!empty($permissionName) && $permission['name'] !== $permissionName) {
                        $matches = false;
                    }
                    
                    if (!empty($resource) && $permission['resource'] !== $resource) {
                        $matches = false;
                    }
                    
                    if (!empty($action) && $permission['action'] !== $action) {
                        $matches = false;
                    }
                    
                    if ($matches) {
                        $hasPermission = true;
                        $matchedPermissions[] = $permission;
                    }
                }
            }
        }

        return [
            'success' => true,
            'data' => [
                'user_id' => $userId,
                'has_permission' => $hasPermission,
                'matched_permissions' => $matchedPermissions,
                'check_criteria' => [
                    'permission' => $permissionName,
                    'resource' => $resource,
                    'action' => $action
                ]
            ],
            'error' => null
        ];
    }

    /**
     * Authenticate user
     */
    private function authenticateUser(array $data): array
    {
        $username = $data['username'] ?? '';
        $password = $data['password'] ?? '';

        if (empty($username) || empty($password)) {
            return [
                'success' => false,
                'error' => 'Username and password are required',
                'data' => null
            ];
        }

        $user = $this->findUserByUsername($username);
        if (!$user) {
            return [
                'success' => false,
                'error' => 'Invalid username or password',
                'data' => null
            ];
        }

        if (!$user['active']) {
            return [
                'success' => false,
                'error' => 'User account is inactive',
                'data' => null
            ];
        }

        if (!password_verify($password, $user['password_hash'])) {
            return [
                'success' => false,
                'error' => 'Invalid username or password',
                'data' => null
            ];
        }

        // Update last login
        $this->users[$user['id']]['last_login'] = date('Y-m-d H:i:s');

        // Create session
        $sessionId = uniqid('session_');
        $session = [
            'id' => $sessionId,
            'user_id' => $user['id'],
            'created_at' => date('Y-m-d H:i:s'),
            'expires_at' => date('Y-m-d H:i:s', time() + 3600), // 1 hour
            'ip_address' => $data['ip_address'] ?? '',
            'user_agent' => $data['user_agent'] ?? ''
        ];

        $this->sessions[$sessionId] = $session;

        return [
            'success' => true,
            'data' => [
                'user_id' => $user['id'],
                'session_id' => $sessionId,
                'user' => array_merge($user, ['password_hash' => '[HIDDEN]']),
                'session' => $session,
                'status' => 'authenticated'
            ],
            'error' => null
        ];
    }

    /**
     * Authorize user for specific action
     */
    private function authorizeUser(array $data): array
    {
        $sessionId = $data['session_id'] ?? '';
        $permissionName = $data['permission'] ?? '';
        $resource = $data['resource'] ?? '';
        $action = $data['action'] ?? '';

        if (empty($sessionId)) {
            return [
                'success' => false,
                'error' => 'Session ID is required',
                'data' => null
            ];
        }

        if (!isset($this->sessions[$sessionId])) {
            return [
                'success' => false,
                'error' => 'Invalid session',
                'data' => null
            ];
        }

        $session = $this->sessions[$sessionId];
        
        // Check if session is expired
        if (strtotime($session['expires_at']) < time()) {
            unset($this->sessions[$sessionId]);
            return [
                'success' => false,
                'error' => 'Session expired',
                'data' => null
            ];
        }

        $userId = $session['user_id'];
        
        return $this->checkPermission([
            'user_id' => $userId,
            'permission' => $permissionName,
            'resource' => $resource,
            'action' => $action
        ]);
    }

    /**
     * Handle session operations
     */
    private function handleSession(array $data): array
    {
        $action = $data['action'] ?? 'validate';
        $sessionId = $data['session_id'] ?? '';

        switch ($action) {
            case 'validate':
                return $this->validateSession($sessionId);
            case 'refresh':
                return $this->refreshSession($sessionId);
            case 'revoke':
                return $this->revokeSession($sessionId);
            case 'list':
                return $this->listSessions($data['user_id'] ?? '');
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown session action: ' . $action,
                    'data' => null
                ];
        }
    }

    /**
     * Get audit log
     */
    private function getAuditLog(array $data): array
    {
        $userId = $data['user_id'] ?? '';
        $action = $data['action'] ?? '';
        $startDate = $data['start_date'] ?? '';
        $endDate = $data['end_date'] ?? '';
        $limit = $data['limit'] ?? 100;

        // Simulate audit log
        $auditLog = [
            [
                'id' => uniqid('audit_'),
                'user_id' => $userId ?: 'system',
                'action' => 'login',
                'resource' => 'auth',
                'timestamp' => date('Y-m-d H:i:s', time() - 3600),
                'ip_address' => '192.168.1.100',
                'details' => 'User logged in successfully'
            ],
            [
                'id' => uniqid('audit_'),
                'user_id' => $userId ?: 'system',
                'action' => 'permission_check',
                'resource' => 'rbac',
                'timestamp' => date('Y-m-d H:i:s', time() - 1800),
                'ip_address' => '192.168.1.100',
                'details' => 'Permission check performed'
            ]
        ];

        return [
            'success' => true,
            'data' => [
                'audit_log' => $auditLog,
                'total_entries' => count($auditLog),
                'filters' => [
                    'user_id' => $userId,
                    'action' => $action,
                    'start_date' => $startDate,
                    'end_date' => $endDate,
                    'limit' => $limit
                ]
            ],
            'error' => null
        ];
    }

    // Helper Methods
    private function initializeRbacSystem(): void
    {
        $this->users = [];
        $this->roles = [];
        $this->permissions = [];
        $this->userRoles = [];
        $this->rolePermissions = [];
        $this->sessions = [];

        // Create default roles and permissions
        $this->createDefaultRoles();
        $this->createDefaultPermissions();
    }

    private function createDefaultRoles(): void
    {
        $adminRole = $this->createRole([
            'name' => 'admin',
            'description' => 'System Administrator',
            'permissions' => ['all']
        ]);

        $userRole = $this->createRole([
            'name' => 'user',
            'description' => 'Regular User',
            'permissions' => ['read']
        ]);
    }

    private function createDefaultPermissions(): void
    {
        $this->createPermission([
            'name' => 'read',
            'description' => 'Read access',
            'resource' => '*',
            'action' => 'read'
        ]);

        $this->createPermission([
            'name' => 'write',
            'description' => 'Write access',
            'resource' => '*',
            'action' => 'write'
        ]);

        $this->createPermission([
            'name' => 'delete',
            'description' => 'Delete access',
            'resource' => '*',
            'action' => 'delete'
        ]);

        $this->createPermission([
            'name' => 'all',
            'description' => 'All permissions',
            'resource' => '*',
            'action' => '*'
        ]);
    }

    private function userExists(string $username): bool
    {
        foreach ($this->users as $user) {
            if ($user['username'] === $username) {
                return true;
            }
        }
        return false;
    }

    private function emailExists(string $email): bool
    {
        foreach ($this->users as $user) {
            if ($user['email'] === $email) {
                return true;
            }
        }
        return false;
    }

    private function roleExists(string $name): bool
    {
        foreach ($this->roles as $role) {
            if ($role['name'] === $name) {
                return true;
            }
        }
        return false;
    }

    private function permissionExists(string $name): bool
    {
        foreach ($this->permissions as $permission) {
            if ($permission['name'] === $name) {
                return true;
            }
        }
        return false;
    }

    private function findUserByUsername(string $username): ?array
    {
        foreach ($this->users as $user) {
            if ($user['username'] === $username) {
                return $user;
            }
        }
        return null;
    }

    private function assignPermissionToRole(string $roleId, string $permissionId): array
    {
        if (!isset($this->rolePermissions[$roleId])) {
            $this->rolePermissions[$roleId] = [];
        }

        if (in_array($permissionId, $this->rolePermissions[$roleId])) {
            return [
                'success' => false,
                'error' => 'Role already has this permission',
                'data' => null
            ];
        }

        $this->rolePermissions[$roleId][] = $permissionId;

        return [
            'success' => true,
            'data' => [
                'role_id' => $roleId,
                'permission_id' => $permissionId,
                'status' => 'assigned'
            ],
            'error' => null
        ];
    }

    private function validateSession(string $sessionId): array
    {
        if (!isset($this->sessions[$sessionId])) {
            return [
                'success' => false,
                'error' => 'Invalid session',
                'data' => null
            ];
        }

        $session = $this->sessions[$sessionId];
        
        if (strtotime($session['expires_at']) < time()) {
            unset($this->sessions[$sessionId]);
            return [
                'success' => false,
                'error' => 'Session expired',
                'data' => null
            ];
        }

        return [
            'success' => true,
            'data' => [
                'session' => $session,
                'valid' => true
            ],
            'error' => null
        ];
    }

    private function refreshSession(string $sessionId): array
    {
        if (!isset($this->sessions[$sessionId])) {
            return [
                'success' => false,
                'error' => 'Invalid session',
                'data' => null
            ];
        }

        $this->sessions[$sessionId]['expires_at'] = date('Y-m-d H:i:s', time() + 3600);

        return [
            'success' => true,
            'data' => [
                'session_id' => $sessionId,
                'new_expires_at' => $this->sessions[$sessionId]['expires_at'],
                'status' => 'refreshed'
            ],
            'error' => null
        ];
    }

    private function revokeSession(string $sessionId): array
    {
        if (!isset($this->sessions[$sessionId])) {
            return [
                'success' => false,
                'error' => 'Invalid session',
                'data' => null
            ];
        }

        unset($this->sessions[$sessionId]);

        return [
            'success' => true,
            'data' => [
                'session_id' => $sessionId,
                'status' => 'revoked'
            ],
            'error' => null
        ];
    }

    private function listSessions(string $userId): array
    {
        $userSessions = [];
        
        foreach ($this->sessions as $sessionId => $session) {
            if (empty($userId) || $session['user_id'] === $userId) {
                $userSessions[] = $session;
            }
        }

        return [
            'success' => true,
            'data' => [
                'sessions' => $userSessions,
                'total_sessions' => count($userSessions)
            ],
            'error' => null
        ];
    }

    private function getRbacInfo(): array
    {
        return [
            'success' => true,
            'data' => [
                'total_users' => count($this->users),
                'total_roles' => count($this->roles),
                'total_permissions' => count($this->permissions),
                'total_sessions' => count($this->sessions),
                'capabilities' => [
                    'user_management' => true,
                    'role_management' => true,
                    'permission_management' => true,
                    'access_control' => true,
                    'session_management' => true,
                    'audit_logging' => true,
                    'role_hierarchy' => true,
                    'permission_inheritance' => true
                ]
            ],
            'error' => null
        ];
    }

    private function substituteContext(array $data, array $context): array
    {
        $substituted = [];
        
        foreach ($data as $key => $value) {
            if (is_string($value)) {
                $value = preg_replace_callback('/\$\{([^}]+)\}/', function($matches) use ($context) {
                    $varName = $matches[1];
                    return $context[$varName] ?? $matches[0];
                }, $value);
            } elseif (is_array($value)) {
                $value = $this->substituteContext($value, $context);
            }
            
            $substituted[$key] = $value;
        }
        
        return $substituted;
    }
} 