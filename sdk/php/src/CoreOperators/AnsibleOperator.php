<?php

namespace TuskLang\CoreOperators;

use Exception;

/**
 * Ansible Operator - Configuration Management and Automation
 * 
 * Provides comprehensive Ansible automation capabilities including:
 * - Playbook execution and management
 * - Inventory management and host groups
 * - Ad-hoc command execution
 * - Role management and dependencies
 * - Variable and fact gathering
 * - Vault encryption and decryption
 * - Galaxy role management
 * - Dynamic inventory support
 * 
 * @package TuskLang\CoreOperators
 */
class AnsibleOperator implements OperatorInterface
{
    private $ansiblePath;
    private $ansiblePlaybookPath;
    private $ansibleGalaxyPath;
    private $defaultInventory;

    public function __construct()
    {
        $this->ansiblePath = $this->findAnsiblePath();
        $this->ansiblePlaybookPath = $this->findAnsiblePlaybookPath();
        $this->ansibleGalaxyPath = $this->findAnsibleGalaxyPath();
        $this->defaultInventory = getenv('ANSIBLE_INVENTORY') ?: '/etc/ansible/hosts';
    }

    /**
     * Execute Ansible operations
     */
    public function execute(array $params, array $context = []): array
    {
        try {
            $operation = $params['operation'] ?? 'playbook';
            $data = $params['data'] ?? [];
            $inventory = $params['inventory'] ?? $this->defaultInventory;
            
            // Substitute context variables
            $data = $this->substituteContext($data, $context);
            
            switch ($operation) {
                case 'playbook':
                    return $this->executePlaybook($data, $inventory);
                case 'adhoc':
                    return $this->executeAdhoc($data, $inventory);
                case 'inventory':
                    return $this->handleInventory($data, $inventory);
                case 'galaxy':
                    return $this->handleGalaxy($data);
                case 'vault':
                    return $this->handleVault($data);
                case 'facts':
                    return $this->gatherFacts($data, $inventory);
                case 'ping':
                    return $this->pingHosts($data, $inventory);
                case 'info':
                default:
                    return $this->getAnsibleInfo();
            }
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => 'Ansible operation failed: ' . $e->getMessage(),
                'data' => null
            ];
        }
    }

    /**
     * Execute Ansible playbook
     */
    private function executePlaybook(array $data, string $inventory): array
    {
        $playbook = $data['playbook'] ?? '';
        $limit = $data['limit'] ?? '';
        $tags = $data['tags'] ?? '';
        $skipTags = $data['skip_tags'] ?? '';
        $extraVars = $data['extra_vars'] ?? [];
        $verbose = $data['verbose'] ?? false;
        $check = $data['check'] ?? false;
        $diff = $data['diff'] ?? false;

        if (empty($playbook)) {
            return [
                'success' => false,
                'error' => 'Playbook file is required',
                'data' => null
            ];
        }

        $cmd = [$this->ansiblePlaybookPath, $playbook];

        // Add inventory
        if (!empty($inventory)) {
            $cmd[] = '-i';
            $cmd[] = $inventory;
        }

        // Add limit
        if (!empty($limit)) {
            $cmd[] = '--limit';
            $cmd[] = $limit;
        }

        // Add tags
        if (!empty($tags)) {
            $cmd[] = '--tags';
            $cmd[] = $tags;
        }

        // Add skip tags
        if (!empty($skipTags)) {
            $cmd[] = '--skip-tags';
            $cmd[] = $skipTags;
        }

        // Add extra vars
        if (!empty($extraVars)) {
            $cmd[] = '--extra-vars';
            $cmd[] = json_encode($extraVars);
        }

        // Add verbose
        if ($verbose) {
            $cmd[] = '-v';
        }

        // Add check mode
        if ($check) {
            $cmd[] = '--check';
        }

        // Add diff
        if ($diff) {
            $cmd[] = '--diff';
        }

        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'playbook' => $playbook,
                'inventory' => $inventory,
                'output' => $result['output'],
                'status' => $result['exit_code'] === 0 ? 'completed' : 'failed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Execute ad-hoc command
     */
    private function executeAdhoc(array $data, string $inventory): array
    {
        $hosts = $data['hosts'] ?? 'all';
        $module = $data['module'] ?? 'ping';
        $args = $data['args'] ?? '';
        $verbose = $data['verbose'] ?? false;

        $cmd = [$this->ansiblePath, $hosts];

        // Add inventory
        if (!empty($inventory)) {
            $cmd[] = '-i';
            $cmd[] = $inventory;
        }

        // Add module
        $cmd[] = '-m';
        $cmd[] = $module;

        // Add arguments
        if (!empty($args)) {
            $cmd[] = '-a';
            $cmd[] = $args;
        }

        // Add verbose
        if ($verbose) {
            $cmd[] = '-v';
        }

        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'hosts' => $hosts,
                'module' => $module,
                'args' => $args,
                'inventory' => $inventory,
                'output' => $result['output'],
                'status' => $result['exit_code'] === 0 ? 'completed' : 'failed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Handle inventory operations
     */
    private function handleInventory(array $data, string $inventory): array
    {
        $action = $data['action'] ?? 'list';

        switch ($action) {
            case 'list':
                return $this->listInventory($inventory);
            case 'list-hosts':
                return $this->listHosts($inventory);
            case 'list-groups':
                return $this->listGroups($inventory);
            case 'get-host':
                return $this->getHost($data['host'], $inventory);
            case 'get-group':
                return $this->getGroup($data['group'], $inventory);
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown inventory action: ' . $action,
                    'data' => null
                ];
        }
    }

    /**
     * Handle Ansible Galaxy operations
     */
    private function handleGalaxy(array $data): array
    {
        $action = $data['action'] ?? 'list';

        switch ($action) {
            case 'install':
                return $this->installGalaxyRole($data);
            case 'remove':
                return $this->removeGalaxyRole($data['role']);
            case 'list':
                return $this->listGalaxyRoles();
            case 'init':
                return $this->initGalaxyRole($data);
            case 'search':
                return $this->searchGalaxyRoles($data['query']);
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown galaxy action: ' . $action,
                    'data' => null
                ];
        }
    }

    /**
     * Handle Ansible Vault operations
     */
    private function handleVault(array $data): array
    {
        $action = $data['action'] ?? 'encrypt';

        switch ($action) {
            case 'encrypt':
                return $this->encryptVault($data);
            case 'decrypt':
                return $this->decryptVault($data);
            case 'edit':
                return $this->editVault($data);
            case 'rekey':
                return $this->rekeyVault($data);
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown vault action: ' . $action,
                    'data' => null
                ];
        }
    }

    /**
     * Gather facts from hosts
     */
    private function gatherFacts(array $data, string $inventory): array
    {
        $hosts = $data['hosts'] ?? 'all';
        $verbose = $data['verbose'] ?? false;

        $cmd = [$this->ansiblePath, $hosts];

        // Add inventory
        if (!empty($inventory)) {
            $cmd[] = '-i';
            $cmd[] = $inventory;
        }

        $cmd[] = '-m';
        $cmd[] = 'setup';

        if ($verbose) {
            $cmd[] = '-v';
        }

        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'hosts' => $hosts,
                'inventory' => $inventory,
                'facts' => $result['output'],
                'status' => $result['exit_code'] === 0 ? 'gathered' : 'failed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Ping hosts
     */
    private function pingHosts(array $data, string $inventory): array
    {
        $hosts = $data['hosts'] ?? 'all';
        $verbose = $data['verbose'] ?? false;

        $cmd = [$this->ansiblePath, $hosts];

        // Add inventory
        if (!empty($inventory)) {
            $cmd[] = '-i';
            $cmd[] = $inventory;
        }

        $cmd[] = '-m';
        $cmd[] = 'ping';

        if ($verbose) {
            $cmd[] = '-v';
        }

        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'hosts' => $hosts,
                'inventory' => $inventory,
                'ping_results' => $result['output'],
                'status' => $result['exit_code'] === 0 ? 'successful' : 'failed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    // Inventory Management Methods
    private function listInventory(string $inventory): array
    {
        $cmd = [$this->ansiblePath, 'all', '-i', $inventory, '--list-hosts'];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $hosts = array_filter(explode("\n", $result['output']));
            
            return [
                'success' => true,
                'data' => [
                    'inventory' => $inventory,
                    'hosts' => $hosts
                ],
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    private function listHosts(string $inventory): array
    {
        $cmd = [$this->ansiblePath, 'all', '-i', $inventory, '--list-hosts'];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $hosts = array_filter(explode("\n", $result['output']));
            
            return [
                'success' => true,
                'data' => [
                    'inventory' => $inventory,
                    'hosts' => $hosts
                ],
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    private function listGroups(string $inventory): array
    {
        $cmd = [$this->ansiblePath, 'all', '-i', $inventory, '--list-groups'];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $groups = array_filter(explode("\n", $result['output']));
            
            return [
                'success' => true,
                'data' => [
                    'inventory' => $inventory,
                    'groups' => $groups
                ],
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    private function getHost(string $host, string $inventory): array
    {
        $cmd = [$this->ansiblePath, $host, '-i', $inventory, '--list-hosts'];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'inventory' => $inventory,
                'host' => $host,
                'info' => $result['output']
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function getGroup(string $group, string $inventory): array
    {
        $cmd = [$this->ansiblePath, $group, '-i', $inventory, '--list-hosts'];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'inventory' => $inventory,
                'group' => $group,
                'hosts' => array_filter(explode("\n", $result['output']))
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    // Galaxy Management Methods
    private function installGalaxyRole(array $data): array
    {
        $role = $data['role'] ?? '';
        $version = $data['version'] ?? '';
        $force = $data['force'] ?? false;

        if (empty($role)) {
            return [
                'success' => false,
                'error' => 'Role name is required',
                'data' => null
            ];
        }

        $cmd = [$this->ansibleGalaxyPath, 'install', $role];

        if (!empty($version)) {
            $cmd[] = '--version';
            $cmd[] = $version;
        }

        if ($force) {
            $cmd[] = '--force';
        }

        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'role' => $role,
                'version' => $version,
                'output' => $result['output'],
                'status' => 'installed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function removeGalaxyRole(string $role): array
    {
        $cmd = [$this->ansibleGalaxyPath, 'remove', $role];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'role' => $role,
                'output' => $result['output'],
                'status' => 'removed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function listGalaxyRoles(): array
    {
        $cmd = [$this->ansibleGalaxyPath, 'list'];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $roles = array_filter(explode("\n", $result['output']));
            
            return [
                'success' => true,
                'data' => [
                    'roles' => $roles
                ],
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    private function initGalaxyRole(array $data): array
    {
        $role = $data['role'] ?? '';
        $initPath = $data['init_path'] ?? '';

        if (empty($role)) {
            return [
                'success' => false,
                'error' => 'Role name is required',
                'data' => null
            ];
        }

        $cmd = [$this->ansibleGalaxyPath, 'init', $role];

        if (!empty($initPath)) {
            $cmd[] = '--init-path';
            $cmd[] = $initPath;
        }

        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'role' => $role,
                'init_path' => $initPath,
                'output' => $result['output'],
                'status' => 'initialized'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function searchGalaxyRoles(string $query): array
    {
        $cmd = [$this->ansibleGalaxyPath, 'search', $query];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $results = array_filter(explode("\n", $result['output']));
            
            return [
                'success' => true,
                'data' => [
                    'query' => $query,
                    'results' => $results
                ],
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    // Vault Management Methods
    private function encryptVault(array $data): array
    {
        $file = $data['file'] ?? '';
        $password = $data['password'] ?? '';

        if (empty($file)) {
            return [
                'success' => false,
                'error' => 'File path is required',
                'data' => null
            ];
        }

        $cmd = [$this->ansiblePlaybookPath, 'encrypt', $file];

        if (!empty($password)) {
            $cmd[] = '--vault-password-file';
            $cmd[] = $password;
        }

        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'file' => $file,
                'output' => $result['output'],
                'status' => 'encrypted'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function decryptVault(array $data): array
    {
        $file = $data['file'] ?? '';
        $password = $data['password'] ?? '';

        if (empty($file)) {
            return [
                'success' => false,
                'error' => 'File path is required',
                'data' => null
            ];
        }

        $cmd = [$this->ansiblePlaybookPath, 'decrypt', $file];

        if (!empty($password)) {
            $cmd[] = '--vault-password-file';
            $cmd[] = $password;
        }

        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'file' => $file,
                'output' => $result['output'],
                'status' => 'decrypted'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function editVault(array $data): array
    {
        $file = $data['file'] ?? '';
        $password = $data['password'] ?? '';

        if (empty($file)) {
            return [
                'success' => false,
                'error' => 'File path is required',
                'data' => null
            ];
        }

        $cmd = [$this->ansiblePlaybookPath, 'edit', $file];

        if (!empty($password)) {
            $cmd[] = '--vault-password-file';
            $cmd[] = $password;
        }

        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'file' => $file,
                'output' => $result['output'],
                'status' => 'edited'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function rekeyVault(array $data): array
    {
        $file = $data['file'] ?? '';
        $oldPassword = $data['old_password'] ?? '';
        $newPassword = $data['new_password'] ?? '';

        if (empty($file)) {
            return [
                'success' => false,
                'error' => 'File path is required',
                'data' => null
            ];
        }

        $cmd = [$this->ansiblePlaybookPath, 'rekey', $file];

        if (!empty($oldPassword)) {
            $cmd[] = '--vault-password-file';
            $cmd[] = $oldPassword;
        }

        if (!empty($newPassword)) {
            $cmd[] = '--new-vault-password-file';
            $cmd[] = $newPassword;
        }

        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'file' => $file,
                'output' => $result['output'],
                'status' => 'rekeyed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    // Helper Methods
    private function getAnsibleInfo(): array
    {
        $cmd = [$this->ansiblePath, '--version'];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $version = trim($result['output']);
            
            return [
                'success' => true,
                'data' => [
                    'ansible_info' => ['version' => $version],
                    'ansible_path' => $this->ansiblePath,
                    'ansible_playbook_path' => $this->ansiblePlaybookPath,
                    'ansible_galaxy_path' => $this->ansibleGalaxyPath,
                    'default_inventory' => $this->defaultInventory
                ],
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    private function findAnsiblePath(): string
    {
        $paths = ['ansible', '/usr/bin/ansible', '/usr/local/bin/ansible'];
        
        foreach ($paths as $path) {
            $result = $this->executeCommand(['which', $path]);
            if ($result['exit_code'] === 0) {
                return trim($result['output']);
            }
        }
        
        return 'ansible'; // Fallback
    }

    private function findAnsiblePlaybookPath(): string
    {
        $paths = ['ansible-playbook', '/usr/bin/ansible-playbook', '/usr/local/bin/ansible-playbook'];
        
        foreach ($paths as $path) {
            $result = $this->executeCommand(['which', $path]);
            if ($result['exit_code'] === 0) {
                return trim($result['output']);
            }
        }
        
        return 'ansible-playbook'; // Fallback
    }

    private function findAnsibleGalaxyPath(): string
    {
        $paths = ['ansible-galaxy', '/usr/bin/ansible-galaxy', '/usr/local/bin/ansible-galaxy'];
        
        foreach ($paths as $path) {
            $result = $this->executeCommand(['which', $path]);
            if ($result['exit_code'] === 0) {
                return trim($result['output']);
            }
        }
        
        return 'ansible-galaxy'; // Fallback
    }

    private function executeCommand(array $cmd): array
    {
        $command = implode(' ', array_map('escapeshellarg', $cmd));
        $output = [];
        $returnVar = 0;
        
        exec($command . ' 2>&1', $output, $returnVar);
        
        return [
            'exit_code' => $returnVar,
            'output' => implode("\n", $output),
            'error' => $returnVar !== 0 ? implode("\n", $output) : null
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