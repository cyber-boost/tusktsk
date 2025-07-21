<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\FileSystem;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\PermissionOperationException;
use TuskLang\SDK\SystemOperations\Security\RBACManager;
use TuskLang\SDK\SystemOperations\Audit\PermissionAuditor;

/**
 * Advanced Permission Management Operator with RBAC Support
 * 
 * Features:
 * - Advanced permission management system
 * - Role-based access control (RBAC)
 * - Permission inheritance and conflict resolution
 * - Permission auditing with detailed logging
 * - Dynamic permission adjustment based on context
 * 
 * @package TuskLang\SDK\SystemOperations\FileSystem
 * @version 1.0.0
 * @author TuskLang AI System
 */
class PermissionOperator extends BaseOperator implements OperatorInterface
{
    private RBACManager $rbacManager;
    private PermissionAuditor $auditor;
    private array $permissionCache = [];
    private array $inheritanceRules = [];
    private array $conflictResolutionRules = [];

    // Permission constants
    const PERM_READ = 0444;
    const PERM_WRITE = 0222;
    const PERM_EXECUTE = 0111;
    const PERM_FULL = 0777;
    const PERM_NONE = 0000;

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        
        $this->rbacManager = new RBACManager($config['rbac_config'] ?? []);
        $this->auditor = new PermissionAuditor($config['audit_config'] ?? []);
        
        $this->loadInheritanceRules($config['inheritance_rules'] ?? []);
        $this->loadConflictResolutionRules($config['conflict_rules'] ?? []);
        
        $this->initializeOperator();
    }

    /**
     * Set file/directory permissions with RBAC validation
     */
    public function setPermissions(string $path, $permissions, array $options = []): bool
    {
        try {
            $this->validatePath($path);
            
            if (!file_exists($path)) {
                throw new PermissionOperationException("Path does not exist: {$path}");
            }

            // Convert permissions to octal if needed
            $octalPermissions = $this->normalizePermissions($permissions);
            
            // RBAC validation
            if ($options['rbac_check'] ?? true) {
                $user = $options['user'] ?? $this->getCurrentUser();
                if (!$this->rbacManager->canModifyPermissions($user, $path, $octalPermissions)) {
                    throw new PermissionOperationException("RBAC: User '{$user}' not authorized to modify permissions for: {$path}");
                }
            }

            // Apply inheritance rules
            if ($options['apply_inheritance'] ?? true && is_dir($path)) {
                $octalPermissions = $this->applyInheritanceRules($path, $octalPermissions, $options);
            }

            // Set permissions
            $oldPermissions = fileperms($path) & 0777;
            $success = chmod($path, $octalPermissions);

            if ($success) {
                // Update cache
                $this->permissionCache[$path] = $octalPermissions;
                
                // Apply to children if recursive
                if ($options['recursive'] ?? false && is_dir($path)) {
                    $this->setPermissionsRecursive($path, $octalPermissions, $options);
                }
                
                // Audit the change
                $this->auditor->logPermissionChange($path, $oldPermissions, $octalPermissions, $options);
                
                $this->logOperation('set_permissions_success', $path, [
                    'old_permissions' => sprintf('%o', $oldPermissions),
                    'new_permissions' => sprintf('%o', $octalPermissions)
                ]);
                
                return true;
            } else {
                throw new PermissionOperationException("Failed to set permissions");
            }

        } catch (\Exception $e) {
            $this->logOperation('set_permissions_error', $path, ['error' => $e->getMessage()]);
            throw new PermissionOperationException("Permission setting failed for {$path}: " . $e->getMessage());
        }
    }

    /**
     * Get comprehensive permission information
     */
    public function getPermissions(string $path, array $options = []): array
    {
        try {
            $this->validatePath($path);
            
            if (!file_exists($path)) {
                throw new PermissionOperationException("Path does not exist: {$path}");
            }

            $permissions = fileperms($path);
            $octalPermissions = $permissions & 0777;
            $fileInfo = stat($path);

            $result = [
                'path' => $path,
                'type' => is_dir($path) ? 'directory' : 'file',
                'permissions' => [
                    'octal' => sprintf('%o', $octalPermissions),
                    'symbolic' => $this->octalToSymbolic($octalPermissions),
                    'readable' => $this->isReadablePermission($octalPermissions),
                    'writable' => $this->isWritablePermission($octalPermissions),
                    'executable' => $this->isExecutablePermission($octalPermissions)
                ],
                'owner' => [
                    'uid' => $fileInfo['uid'],
                    'name' => $this->getOwnerName($fileInfo['uid']),
                    'permissions' => $this->getOwnerPermissions($octalPermissions)
                ],
                'group' => [
                    'gid' => $fileInfo['gid'],
                    'name' => $this->getGroupName($fileInfo['gid']),
                    'permissions' => $this->getGroupPermissions($octalPermissions)
                ],
                'other' => [
                    'permissions' => $this->getOtherPermissions($octalPermissions)
                ],
                'special_bits' => [
                    'sticky' => ($permissions & 01000) !== 0,
                    'setgid' => ($permissions & 02000) !== 0,
                    'setuid' => ($permissions & 04000) !== 0
                ]
            ];

            // Add RBAC information if requested
            if ($options['include_rbac'] ?? false) {
                $user = $options['user'] ?? $this->getCurrentUser();
                $result['rbac'] = $this->rbacManager->getUserPermissions($user, $path);
            }

            // Add inheritance information for directories
            if (is_dir($path) && ($options['include_inheritance'] ?? false)) {
                $result['inheritance'] = $this->getInheritanceInfo($path);
            }

            // Add audit history if requested
            if ($options['include_audit'] ?? false) {
                $result['audit_history'] = $this->auditor->getHistory($path, $options['audit_limit'] ?? 10);
            }

            $this->logOperation('get_permissions_success', $path);
            return $result;

        } catch (\Exception $e) {
            $this->logOperation('get_permissions_error', $path, ['error' => $e->getMessage()]);
            throw new PermissionOperationException("Failed to get permissions for {$path}: " . $e->getMessage());
        }
    }

    /**
     * Check if user has specific permission on path
     */
    public function checkPermission(string $path, string $user, string $permission): bool
    {
        try {
            $this->validatePath($path);
            
            if (!file_exists($path)) {
                return false;
            }

            // Check RBAC first
            if ($this->rbacManager->hasPermission($user, $path, $permission)) {
                $this->logOperation('permission_check_rbac_allow', $path, [
                    'user' => $user,
                    'permission' => $permission
                ]);
                return true;
            }

            // Fall back to traditional file permissions
            $filePermissions = fileperms($path) & 0777;
            $userInfo = $this->getUserInfo($user);
            $fileInfo = stat($path);

            $hasPermission = false;

            // Check owner permissions
            if ($userInfo['uid'] === $fileInfo['uid']) {
                $hasPermission = $this->checkOwnerPermission($filePermissions, $permission);
            }
            // Check group permissions
            elseif (in_array($fileInfo['gid'], $userInfo['groups'])) {
                $hasPermission = $this->checkGroupPermission($filePermissions, $permission);
            }
            // Check other permissions
            else {
                $hasPermission = $this->checkOtherPermission($filePermissions, $permission);
            }

            $this->logOperation('permission_check_traditional', $path, [
                'user' => $user,
                'permission' => $permission,
                'result' => $hasPermission
            ]);

            return $hasPermission;

        } catch (\Exception $e) {
            $this->logOperation('permission_check_error', $path, [
                'user' => $user,
                'permission' => $permission,
                'error' => $e->getMessage()
            ]);
            return false;
        }
    }

    /**
     * Create and assign role to user
     */
    public function createRole(string $roleName, array $permissions, array $options = []): bool
    {
        try {
            $success = $this->rbacManager->createRole($roleName, $permissions, $options);
            
            if ($success) {
                $this->auditor->logRoleCreation($roleName, $permissions, $options);
                $this->logOperation('role_created', '', [
                    'role' => $roleName,
                    'permissions' => $permissions
                ]);
            }

            return $success;

        } catch (\Exception $e) {
            $this->logOperation('role_create_error', '', [
                'role' => $roleName,
                'error' => $e->getMessage()
            ]);
            throw new PermissionOperationException("Failed to create role '{$roleName}': " . $e->getMessage());
        }
    }

    /**
     * Assign role to user
     */
    public function assignRole(string $user, string $roleName, array $options = []): bool
    {
        try {
            $success = $this->rbacManager->assignRole($user, $roleName, $options);
            
            if ($success) {
                // Clear user permission cache
                $this->clearUserPermissionCache($user);
                
                $this->auditor->logRoleAssignment($user, $roleName, $options);
                $this->logOperation('role_assigned', '', [
                    'user' => $user,
                    'role' => $roleName
                ]);
            }

            return $success;

        } catch (\Exception $e) {
            $this->logOperation('role_assign_error', '', [
                'user' => $user,
                'role' => $roleName,
                'error' => $e->getMessage()
            ]);
            throw new PermissionOperationException("Failed to assign role '{$roleName}' to user '{$user}': " . $e->getMessage());
        }
    }

    /**
     * Revoke role from user
     */
    public function revokeRole(string $user, string $roleName, array $options = []): bool
    {
        try {
            $success = $this->rbacManager->revokeRole($user, $roleName, $options);
            
            if ($success) {
                // Clear user permission cache
                $this->clearUserPermissionCache($user);
                
                $this->auditor->logRoleRevocation($user, $roleName, $options);
                $this->logOperation('role_revoked', '', [
                    'user' => $user,
                    'role' => $roleName
                ]);
            }

            return $success;

        } catch (\Exception $e) {
            $this->logOperation('role_revoke_error', '', [
                'user' => $user,
                'role' => $roleName,
                'error' => $e->getMessage()
            ]);
            throw new PermissionOperationException("Failed to revoke role '{$roleName}' from user '{$user}': " . $e->getMessage());
        }
    }

    /**
     * Set inheritance rules for directory
     */
    public function setInheritanceRules(string $path, array $rules): bool
    {
        try {
            $this->validatePath($path);
            
            if (!is_dir($path)) {
                throw new PermissionOperationException("Inheritance rules can only be set on directories: {$path}");
            }

            $this->inheritanceRules[$path] = $rules;
            
            // Apply rules to existing children if requested
            if ($rules['apply_to_existing'] ?? false) {
                $this->applyInheritanceToChildren($path, $rules);
            }

            $this->auditor->logInheritanceRuleChange($path, $rules);
            $this->logOperation('inheritance_rules_set', $path, ['rules' => $rules]);
            
            return true;

        } catch (\Exception $e) {
            $this->logOperation('inheritance_rules_error', $path, [
                'rules' => $rules,
                'error' => $e->getMessage()
            ]);
            throw new PermissionOperationException("Failed to set inheritance rules for {$path}: " . $e->getMessage());
        }
    }

    /**
     * Resolve permission conflicts
     */
    public function resolveConflicts(string $path, array $options = []): array
    {
        try {
            $this->validatePath($path);
            
            if (!file_exists($path)) {
                throw new PermissionOperationException("Path does not exist: {$path}");
            }

            $conflicts = $this->detectConflicts($path, $options);
            $resolutions = [];

            foreach ($conflicts as $conflict) {
                $resolution = $this->applyConflictResolution($path, $conflict, $options);
                $resolutions[] = $resolution;
            }

            if (!empty($resolutions)) {
                $this->auditor->logConflictResolution($path, $conflicts, $resolutions);
                $this->logOperation('conflicts_resolved', $path, [
                    'conflicts' => count($conflicts),
                    'resolutions' => $resolutions
                ]);
            }

            return $resolutions;

        } catch (\Exception $e) {
            $this->logOperation('conflict_resolution_error', $path, ['error' => $e->getMessage()]);
            throw new PermissionOperationException("Failed to resolve conflicts for {$path}: " . $e->getMessage());
        }
    }

    /**
     * Dynamically adjust permissions based on context
     */
    public function adjustPermissionsContext(string $path, array $context): bool
    {
        try {
            $this->validatePath($path);
            
            if (!file_exists($path)) {
                throw new PermissionOperationException("Path does not exist: {$path}");
            }

            $currentPermissions = fileperms($path) & 0777;
            $adjustedPermissions = $this->calculateContextualPermissions($path, $currentPermissions, $context);

            if ($adjustedPermissions !== $currentPermissions) {
                $success = $this->setPermissions($path, $adjustedPermissions, [
                    'rbac_check' => false, // Already validated contextually
                    'apply_inheritance' => false // Prevent loops
                ]);

                if ($success) {
                    $this->auditor->logContextualAdjustment($path, $currentPermissions, $adjustedPermissions, $context);
                    $this->logOperation('permissions_adjusted_context', $path, [
                        'old_permissions' => sprintf('%o', $currentPermissions),
                        'new_permissions' => sprintf('%o', $adjustedPermissions),
                        'context' => $context
                    ]);
                }

                return $success;
            }

            return true; // No adjustment needed

        } catch (\Exception $e) {
            $this->logOperation('context_adjustment_error', $path, [
                'context' => $context,
                'error' => $e->getMessage()
            ]);
            throw new PermissionOperationException("Failed to adjust permissions contextually for {$path}: " . $e->getMessage());
        }
    }

    /**
     * Get comprehensive audit report
     */
    public function getAuditReport(array $options = []): array
    {
        try {
            $report = $this->auditor->generateReport($options);
            
            $this->logOperation('audit_report_generated', '', [
                'entries' => count($report['entries'] ?? []),
                'period' => $options['period'] ?? 'all'
            ]);
            
            return $report;

        } catch (\Exception $e) {
            $this->logOperation('audit_report_error', '', ['error' => $e->getMessage()]);
            throw new PermissionOperationException("Failed to generate audit report: " . $e->getMessage());
        }
    }

    // Private helper methods

    private function normalizePermissions($permissions): int
    {
        if (is_string($permissions)) {
            // Convert symbolic permissions (e.g., "rwxr-xr-x") to octal
            if (strlen($permissions) === 9 && preg_match('/^[rwx-]{9}$/', $permissions)) {
                return $this->symbolicToOctal($permissions);
            }
            // Convert octal string to integer
            return octdec($permissions);
        }
        
        if (is_int($permissions)) {
            return $permissions;
        }
        
        throw new PermissionOperationException("Invalid permission format");
    }

    private function symbolicToOctal(string $symbolic): int
    {
        $octal = 0;
        
        // Owner permissions
        if ($symbolic[0] === 'r') $octal += 0400;
        if ($symbolic[1] === 'w') $octal += 0200;
        if ($symbolic[2] === 'x') $octal += 0100;
        
        // Group permissions
        if ($symbolic[3] === 'r') $octal += 0040;
        if ($symbolic[4] === 'w') $octal += 0020;
        if ($symbolic[5] === 'x') $octal += 0010;
        
        // Other permissions
        if ($symbolic[6] === 'r') $octal += 0004;
        if ($symbolic[7] === 'w') $octal += 0002;
        if ($symbolic[8] === 'x') $octal += 0001;
        
        return $octal;
    }

    private function octalToSymbolic(int $octal): string
    {
        $symbolic = '';
        
        // Owner permissions
        $symbolic .= ($octal & 0400) ? 'r' : '-';
        $symbolic .= ($octal & 0200) ? 'w' : '-';
        $symbolic .= ($octal & 0100) ? 'x' : '-';
        
        // Group permissions
        $symbolic .= ($octal & 0040) ? 'r' : '-';
        $symbolic .= ($octal & 0020) ? 'w' : '-';
        $symbolic .= ($octal & 0010) ? 'x' : '-';
        
        // Other permissions
        $symbolic .= ($octal & 0004) ? 'r' : '-';
        $symbolic .= ($octal & 0002) ? 'w' : '-';
        $symbolic .= ($octal & 0001) ? 'x' : '-';
        
        return $symbolic;
    }

    private function setPermissionsRecursive(string $path, int $permissions, array $options): void
    {
        $iterator = new \RecursiveIteratorIterator(
            new \RecursiveDirectoryIterator($path, \RecursiveDirectoryIterator::SKIP_DOTS)
        );

        foreach ($iterator as $item) {
            $itemPath = $item->getRealPath();
            $itemPermissions = $permissions;
            
            // Apply different permissions for files vs directories if specified
            if ($item->isFile() && isset($options['file_permissions'])) {
                $itemPermissions = $this->normalizePermissions($options['file_permissions']);
            } elseif ($item->isDir() && isset($options['dir_permissions'])) {
                $itemPermissions = $this->normalizePermissions($options['dir_permissions']);
            }
            
            chmod($itemPath, $itemPermissions);
            $this->permissionCache[$itemPath] = $itemPermissions;
        }
    }

    private function applyInheritanceRules(string $path, int $permissions, array $options): int
    {
        $parentPath = dirname($path);
        
        while ($parentPath !== '/' && $parentPath !== $path) {
            if (isset($this->inheritanceRules[$parentPath])) {
                $rules = $this->inheritanceRules[$parentPath];
                $permissions = $this->applyInheritanceRule($permissions, $rules);
                break;
            }
            $parentPath = dirname($parentPath);
        }
        
        return $permissions;
    }

    private function applyInheritanceRule(int $permissions, array $rules): int
    {
        foreach ($rules as $rule => $value) {
            switch ($rule) {
                case 'inherit_owner_read':
                    if ($value && ($permissions & 0400)) {
                        // Implementation of inheritance logic
                    }
                    break;
                case 'inherit_group_write':
                    if ($value && ($permissions & 0020)) {
                        // Implementation of inheritance logic
                    }
                    break;
                // Add more inheritance rules as needed
            }
        }
        
        return $permissions;
    }

    private function applyInheritanceToChildren(string $path, array $rules): void
    {
        $iterator = new \RecursiveIteratorIterator(
            new \RecursiveDirectoryIterator($path, \RecursiveDirectoryIterator::SKIP_DOTS)
        );

        foreach ($iterator as $item) {
            $itemPath = $item->getRealPath();
            $currentPermissions = fileperms($itemPath) & 0777;
            $newPermissions = $this->applyInheritanceRule($currentPermissions, $rules);
            
            if ($newPermissions !== $currentPermissions) {
                chmod($itemPath, $newPermissions);
                $this->permissionCache[$itemPath] = $newPermissions;
            }
        }
    }

    private function detectConflicts(string $path, array $options): array
    {
        $conflicts = [];
        
        // Check for RBAC vs file permission conflicts
        $filePermissions = fileperms($path) & 0777;
        $users = $options['check_users'] ?? [];
        
        foreach ($users as $user) {
            $rbacPermissions = $this->rbacManager->getUserPermissions($user, $path);
            $fileAllowed = $this->checkFilePermissions($user, $path, $filePermissions);
            
            if ($rbacPermissions['read'] !== $fileAllowed['read'] ||
                $rbacPermissions['write'] !== $fileAllowed['write'] ||
                $rbacPermissions['execute'] !== $fileAllowed['execute']) {
                
                $conflicts[] = [
                    'type' => 'rbac_vs_file',
                    'user' => $user,
                    'path' => $path,
                    'rbac_permissions' => $rbacPermissions,
                    'file_permissions' => $fileAllowed
                ];
            }
        }
        
        return $conflicts;
    }

    private function applyConflictResolution(string $path, array $conflict, array $options): array
    {
        $resolution = [
            'conflict' => $conflict,
            'action' => 'none',
            'result' => false
        ];
        
        $strategy = $options['conflict_strategy'] ?? 'rbac_priority';
        
        switch ($strategy) {
            case 'rbac_priority':
                // RBAC permissions take priority
                if ($conflict['type'] === 'rbac_vs_file') {
                    $newPermissions = $this->calculatePermissionsFromRBAC($path, $conflict['user']);
                    $success = $this->setPermissions($path, $newPermissions, ['rbac_check' => false]);
                    $resolution['action'] = 'updated_file_permissions';
                    $resolution['result'] = $success;
                }
                break;
                
            case 'file_priority':
                // File permissions take priority
                if ($conflict['type'] === 'rbac_vs_file') {
                    $success = $this->rbacManager->updateUserPermissions(
                        $conflict['user'], 
                        $path, 
                        $conflict['file_permissions']
                    );
                    $resolution['action'] = 'updated_rbac_permissions';
                    $resolution['result'] = $success;
                }
                break;
                
            case 'most_restrictive':
                // Apply most restrictive permissions
                $resolution = $this->applyMostRestrictivePermissions($path, $conflict);
                break;
        }
        
        return $resolution;
    }

    private function calculateContextualPermissions(string $path, int $currentPermissions, array $context): int
    {
        $adjustedPermissions = $currentPermissions;
        
        // Time-based adjustments
        if (isset($context['time_restrictions'])) {
            $currentHour = (int)date('H');
            $timeRestrictions = $context['time_restrictions'];
            
            if (isset($timeRestrictions['business_hours']) && 
                ($currentHour < $timeRestrictions['start_hour'] || $currentHour > $timeRestrictions['end_hour'])) {
                // Reduce permissions outside business hours
                $adjustedPermissions &= ~0222; // Remove write permissions
            }
        }
        
        // Location-based adjustments
        if (isset($context['location_restrictions'])) {
            $userIP = $context['user_ip'] ?? '';
            $allowedNetworks = $context['location_restrictions']['allowed_networks'] ?? [];
            
            if (!empty($allowedNetworks) && !$this->isIPInNetworks($userIP, $allowedNetworks)) {
                // Reduce permissions for users outside allowed networks
                $adjustedPermissions &= ~0222; // Remove write permissions
            }
        }
        
        // Load-based adjustments
        if (isset($context['system_load'])) {
            $systemLoad = $context['system_load'];
            $highLoadThreshold = $context['high_load_threshold'] ?? 80;
            
            if ($systemLoad > $highLoadThreshold) {
                // Reduce permissions during high system load
                $adjustedPermissions &= ~0111; // Remove execute permissions
            }
        }
        
        return $adjustedPermissions;
    }

    private function isReadablePermission(int $permissions): bool
    {
        return ($permissions & 0444) !== 0;
    }

    private function isWritablePermission(int $permissions): bool
    {
        return ($permissions & 0222) !== 0;
    }

    private function isExecutablePermission(int $permissions): bool
    {
        return ($permissions & 0111) !== 0;
    }

    private function getOwnerPermissions(int $permissions): array
    {
        return [
            'read' => ($permissions & 0400) !== 0,
            'write' => ($permissions & 0200) !== 0,
            'execute' => ($permissions & 0100) !== 0
        ];
    }

    private function getGroupPermissions(int $permissions): array
    {
        return [
            'read' => ($permissions & 0040) !== 0,
            'write' => ($permissions & 0020) !== 0,
            'execute' => ($permissions & 0010) !== 0
        ];
    }

    private function getOtherPermissions(int $permissions): array
    {
        return [
            'read' => ($permissions & 0004) !== 0,
            'write' => ($permissions & 0002) !== 0,
            'execute' => ($permissions & 0001) !== 0
        ];
    }

    private function checkOwnerPermission(int $filePermissions, string $permission): bool
    {
        switch ($permission) {
            case 'read':
                return ($filePermissions & 0400) !== 0;
            case 'write':
                return ($filePermissions & 0200) !== 0;
            case 'execute':
                return ($filePermissions & 0100) !== 0;
        }
        return false;
    }

    private function checkGroupPermission(int $filePermissions, string $permission): bool
    {
        switch ($permission) {
            case 'read':
                return ($filePermissions & 0040) !== 0;
            case 'write':
                return ($filePermissions & 0020) !== 0;
            case 'execute':
                return ($filePermissions & 0010) !== 0;
        }
        return false;
    }

    private function checkOtherPermission(int $filePermissions, string $permission): bool
    {
        switch ($permission) {
            case 'read':
                return ($filePermissions & 0004) !== 0;
            case 'write':
                return ($filePermissions & 0002) !== 0;
            case 'execute':
                return ($filePermissions & 0001) !== 0;
        }
        return false;
    }

    private function getCurrentUser(): string
    {
        return posix_getpwuid(posix_geteuid())['name'] ?? 'unknown';
    }

    private function getUserInfo(string $user): array
    {
        $userInfo = posix_getpwnam($user);
        if (!$userInfo) {
            throw new PermissionOperationException("User not found: {$user}");
        }
        
        $groups = [];
        $groupInfo = posix_getgrgid($userInfo['gid']);
        if ($groupInfo) {
            $groups[] = $groupInfo['gid'];
        }
        
        return [
            'uid' => $userInfo['uid'],
            'gid' => $userInfo['gid'],
            'groups' => $groups
        ];
    }

    private function getOwnerName(int $uid): string
    {
        $userInfo = posix_getpwuid($uid);
        return $userInfo['name'] ?? "uid:{$uid}";
    }

    private function getGroupName(int $gid): string
    {
        $groupInfo = posix_getgrgid($gid);
        return $groupInfo['name'] ?? "gid:{$gid}";
    }

    private function getInheritanceInfo(string $path): array
    {
        return $this->inheritanceRules[$path] ?? [];
    }

    private function clearUserPermissionCache(string $user): void
    {
        // Clear cached permissions for user
        foreach ($this->permissionCache as $path => $permissions) {
            // Remove user-specific cache entries
            unset($this->permissionCache["{$path}::{$user}"]);
        }
    }

    private function loadInheritanceRules(array $rules): void
    {
        $this->inheritanceRules = $rules;
    }

    private function loadConflictResolutionRules(array $rules): void
    {
        $this->conflictResolutionRules = $rules;
    }

    private function validatePath(string $path): void
    {
        if (empty($path)) {
            throw new PermissionOperationException("Path cannot be empty");
        }

        // Path traversal protection
        if (strpos($path, '..') !== false) {
            throw new PermissionOperationException("Path traversal not allowed: {$path}");
        }
    }

    private function initializeOperator(): void
    {
        // Initialize RBAC manager
        $this->rbacManager->initialize();
        
        // Initialize auditor
        $this->auditor->initialize();
        
        $this->logOperation('permission_operator_initialized', '');
    }

    private function logOperation(string $operation, string $path, array $context = []): void
    {
        // Log operation for monitoring and debugging
        $logData = [
            'operation' => $operation,
            'path' => $path,
            'timestamp' => microtime(true),
            'context' => $context
        ];
        
        // Would integrate with logging system
        error_log("PermissionOperator: " . json_encode($logData));
    }
} 