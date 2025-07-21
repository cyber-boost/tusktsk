<?php

namespace TuskLang\CoreOperators;

use Exception;

/**
 * Terraform Operator - Infrastructure as Code Management
 * 
 * Provides comprehensive Terraform infrastructure management including:
 * - Terraform initialization and workspace management
 * - Plan, apply, and destroy operations
 * - State management and locking
 * - Module management and validation
 * - Variable and output handling
 * - Remote state configuration
 * - Workspace operations
 * 
 * @package TuskLang\CoreOperators
 */
class TerraformOperator implements OperatorInterface
{
    private $terraformPath;
    private $defaultWorkingDir;

    public function __construct()
    {
        $this->terraformPath = $this->findTerraformPath();
        $this->defaultWorkingDir = getcwd();
    }

    /**
     * Execute Terraform operations
     */
    public function execute(array $params, array $context = []): array
    {
        try {
            $operation = $params['operation'] ?? 'init';
            $data = $params['data'] ?? [];
            $workingDir = $params['working_dir'] ?? $this->defaultWorkingDir;
            
            // Substitute context variables
            $data = $this->substituteContext($data, $context);
            
            switch ($operation) {
                case 'init':
                    return $this->initTerraform($data, $workingDir);
                case 'plan':
                    return $this->planTerraform($data, $workingDir);
                case 'apply':
                    return $this->applyTerraform($data, $workingDir);
                case 'destroy':
                    return $this->destroyTerraform($data, $workingDir);
                case 'validate':
                    return $this->validateTerraform($workingDir);
                case 'fmt':
                    return $this->formatTerraform($workingDir);
                case 'state':
                    return $this->handleState($data, $workingDir);
                case 'workspace':
                    return $this->handleWorkspace($data, $workingDir);
                case 'output':
                    return $this->getTerraformOutput($data, $workingDir);
                case 'info':
                default:
                    return $this->getTerraformInfo($workingDir);
            }
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => 'Terraform operation failed: ' . $e->getMessage(),
                'data' => null
            ];
        }
    }

    /**
     * Initialize Terraform
     */
    private function initTerraform(array $data, string $workingDir): array
    {
        $cmd = [
            $this->terraformPath, 'init',
            '-input=false'
        ];

        if (isset($data['backend_config'])) {
            foreach ($data['backend_config'] as $key => $value) {
                $cmd[] = "-backend-config=$key=$value";
            }
        }

        if (isset($data['upgrade'])) {
            $cmd[] = '-upgrade';
        }

        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'output' => $result['output'],
                'status' => 'initialized'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Plan Terraform changes
     */
    private function planTerraform(array $data, string $workingDir): array
    {
        $cmd = [
            $this->terraformPath, 'plan',
            '-input=false',
            '-out=' . ($data['plan_file'] ?? 'terraform.tfplan')
        ];

        // Add variables
        if (isset($data['variables'])) {
            foreach ($data['variables'] as $key => $value) {
                $cmd[] = "-var=$key=$value";
            }
        }

        // Add variable files
        if (isset($data['var_files'])) {
            foreach ($data['var_files'] as $file) {
                $cmd[] = "-var-file=$file";
            }
        }

        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'plan_file' => $data['plan_file'] ?? 'terraform.tfplan',
                'output' => $result['output'],
                'status' => 'planned'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Apply Terraform changes
     */
    private function applyTerraform(array $data, string $workingDir): array
    {
        $cmd = [
            $this->terraformPath, 'apply',
            '-input=false',
            '-auto-approve'
        ];

        if (isset($data['plan_file'])) {
            $cmd[] = $data['plan_file'];
        }

        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'output' => $result['output'],
                'status' => 'applied'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Destroy Terraform infrastructure
     */
    private function destroyTerraform(array $data, string $workingDir): array
    {
        $cmd = [
            $this->terraformPath, 'destroy',
            '-input=false',
            '-auto-approve'
        ];

        // Add variables
        if (isset($data['variables'])) {
            foreach ($data['variables'] as $key => $value) {
                $cmd[] = "-var=$key=$value";
            }
        }

        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'output' => $result['output'],
                'status' => 'destroyed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Validate Terraform configuration
     */
    private function validateTerraform(string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'validate'];
        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'output' => $result['output'],
                'status' => 'validated'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Format Terraform files
     */
    private function formatTerraform(string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'fmt', '-recursive'];
        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'output' => $result['output'],
                'status' => 'formatted'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Handle Terraform state operations
     */
    private function handleState(array $data, string $workingDir): array
    {
        $action = $data['action'] ?? 'list';
        
        switch ($action) {
            case 'list':
                return $this->listState($workingDir);
            case 'show':
                return $this->showState($data['resource'], $workingDir);
            case 'pull':
                return $this->pullState($workingDir);
            case 'push':
                return $this->pushState($data['state_file'], $workingDir);
            case 'mv':
                return $this->moveState($data['from'], $data['to'], $workingDir);
            case 'rm':
                return $this->removeState($data['resource'], $workingDir);
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown state action: ' . $action,
                    'data' => null
                ];
        }
    }

    /**
     * Handle Terraform workspace operations
     */
    private function handleWorkspace(array $data, string $workingDir): array
    {
        $action = $data['action'] ?? 'list';
        
        switch ($action) {
            case 'list':
                return $this->listWorkspaces($workingDir);
            case 'show':
                return $this->showWorkspace($workingDir);
            case 'new':
                return $this->newWorkspace($data['name'], $workingDir);
            case 'select':
                return $this->selectWorkspace($data['name'], $workingDir);
            case 'delete':
                return $this->deleteWorkspace($data['name'], $workingDir);
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown workspace action: ' . $action,
                    'data' => null
                ];
        }
    }

    /**
     * Get Terraform outputs
     */
    private function getTerraformOutput(array $data, string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'output', '-json'];
        
        if (isset($data['name'])) {
            $cmd = [$this->terraformPath, 'output', '-json', $data['name']];
        }

        $result = $this->executeCommand($cmd, $workingDir);
        
        if ($result['exit_code'] === 0) {
            $outputs = json_decode($result['output'], true);
            
            return [
                'success' => true,
                'data' => [
                    'working_dir' => $workingDir,
                    'outputs' => $outputs
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

    // State Management Methods
    private function listState(string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'state', 'list'];
        $result = $this->executeCommand($cmd, $workingDir);
        
        if ($result['exit_code'] === 0) {
            $resources = array_filter(explode("\n", $result['output']));
            
            return [
                'success' => true,
                'data' => [
                    'working_dir' => $workingDir,
                    'resources' => $resources
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

    private function showState(string $resource, string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'state', 'show', $resource];
        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'resource' => $resource,
                'state' => $result['output']
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function pullState(string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'state', 'pull'];
        $result = $this->executeCommand($cmd, $workingDir);
        
        if ($result['exit_code'] === 0) {
            $state = json_decode($result['output'], true);
            
            return [
                'success' => true,
                'data' => [
                    'working_dir' => $workingDir,
                    'state' => $state
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

    private function pushState(string $stateFile, string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'state', 'push', $stateFile];
        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'state_file' => $stateFile,
                'status' => 'pushed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function moveState(string $from, string $to, string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'state', 'mv', $from, $to];
        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'from' => $from,
                'to' => $to,
                'status' => 'moved'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function removeState(string $resource, string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'state', 'rm', $resource];
        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'resource' => $resource,
                'status' => 'removed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    // Workspace Management Methods
    private function listWorkspaces(string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'workspace', 'list'];
        $result = $this->executeCommand($cmd, $workingDir);
        
        if ($result['exit_code'] === 0) {
            $workspaces = array_filter(explode("\n", $result['output']));
            
            return [
                'success' => true,
                'data' => [
                    'working_dir' => $workingDir,
                    'workspaces' => $workspaces
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

    private function showWorkspace(string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'workspace', 'show'];
        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'current_workspace' => trim($result['output'])
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function newWorkspace(string $name, string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'workspace', 'new', $name];
        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'workspace' => $name,
                'status' => 'created'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function selectWorkspace(string $name, string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'workspace', 'select', $name];
        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'workspace' => $name,
                'status' => 'selected'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    private function deleteWorkspace(string $name, string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'workspace', 'delete', $name];
        $result = $this->executeCommand($cmd, $workingDir);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'working_dir' => $workingDir,
                'workspace' => $name,
                'status' => 'deleted'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    // Helper Methods
    private function getTerraformInfo(string $workingDir): array
    {
        $cmd = [$this->terraformPath, 'version'];
        $result = $this->executeCommand($cmd, $workingDir);
        
        if ($result['exit_code'] === 0) {
            $version = trim($result['output']);
            
            return [
                'success' => true,
                'data' => [
                    'terraform_info' => ['version' => $version],
                    'terraform_path' => $this->terraformPath,
                    'working_dir' => $workingDir
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

    private function findTerraformPath(): string
    {
        $paths = ['terraform', '/usr/bin/terraform', '/usr/local/bin/terraform'];
        
        foreach ($paths as $path) {
            $result = $this->executeCommand(['which', $path]);
            if ($result['exit_code'] === 0) {
                return trim($result['output']);
            }
        }
        
        return 'terraform'; // Fallback
    }

    private function executeCommand(array $cmd, string $workingDir = null): array
    {
        $command = implode(' ', array_map('escapeshellarg', $cmd));
        
        if ($workingDir) {
            $command = "cd " . escapeshellarg($workingDir) . " && $command";
        }
        
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