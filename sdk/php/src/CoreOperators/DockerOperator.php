<?php

namespace TuskLang\CoreOperators;

use Exception;

/**
 * Docker Operator - Container Management
 * 
 * Provides comprehensive Docker container management capabilities including:
 * - Container CRUD operations (create, start, stop, remove)
 * - Image management (pull, build, tag, remove)
 * - Network management (create, list, remove)
 * - Volume management (create, list, remove)
 * - Docker Compose support
 * - Container inspection and logs
 * - Resource monitoring
 * 
 * @package TuskLang\CoreOperators
 */
class DockerOperator implements OperatorInterface
{
    private $dockerPath;
    private $composePath;
    private $defaultTimeout;

    public function __construct()
    {
        $this->dockerPath = $this->findDockerPath();
        $this->composePath = $this->findComposePath();
        $this->defaultTimeout = 300; // 5 minutes
    }

    /**
     * Execute Docker operations
     */
    public function execute(array $params, array $context = []): array
    {
        try {
            $operation = $params['operation'] ?? 'info';
            $data = $params['data'] ?? [];
            
            // Substitute context variables
            $data = $this->substituteContext($data, $context);
            
            switch ($operation) {
                case 'container':
                    return $this->handleContainer($data);
                case 'image':
                    return $this->handleImage($data);
                case 'network':
                    return $this->handleNetwork($data);
                case 'volume':
                    return $this->handleVolume($data);
                case 'compose':
                    return $this->handleCompose($data);
                case 'system':
                    return $this->handleSystem($data);
                case 'info':
                default:
                    return $this->getDockerInfo();
            }
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => 'Docker operation failed: ' . $e->getMessage(),
                'data' => null
            ];
        }
    }

    /**
     * Handle container operations
     */
    private function handleContainer(array $data): array
    {
        $action = $data['action'] ?? 'list';
        $name = $data['name'] ?? '';
        $image = $data['image'] ?? '';
        $ports = $data['ports'] ?? [];
        $volumes = $data['volumes'] ?? [];
        $environment = $data['environment'] ?? [];
        $command = $data['command'] ?? '';
        $detach = $data['detach'] ?? true;

        switch ($action) {
            case 'create':
                return $this->createContainer($name, $image, $ports, $volumes, $environment, $command);
            case 'start':
                return $this->startContainer($name);
            case 'stop':
                return $this->stopContainer($name);
            case 'remove':
                return $this->removeContainer($name);
            case 'logs':
                return $this->getContainerLogs($name, $data['lines'] ?? 100);
            case 'inspect':
                return $this->inspectContainer($name);
            case 'exec':
                return $this->execContainer($name, $data['cmd'] ?? '');
            case 'stats':
                return $this->getContainerStats($name);
            case 'list':
            default:
                return $this->listContainers($data['all'] ?? false);
        }
    }

    /**
     * Handle image operations
     */
    private function handleImage(array $data): array
    {
        $action = $data['action'] ?? 'list';
        $name = $data['name'] ?? '';
        $tag = $data['tag'] ?? 'latest';
        $path = $data['path'] ?? '';

        switch ($action) {
            case 'pull':
                return $this->pullImage($name, $tag);
            case 'build':
                return $this->buildImage($name, $path);
            case 'tag':
                return $this->tagImage($name, $data['new_name'], $data['new_tag'] ?? 'latest');
            case 'remove':
                return $this->removeImage($name);
            case 'list':
            default:
                return $this->listImages();
        }
    }

    /**
     * Handle network operations
     */
    private function handleNetwork(array $data): array
    {
        $action = $data['action'] ?? 'list';
        $name = $data['name'] ?? '';
        $driver = $data['driver'] ?? 'bridge';

        switch ($action) {
            case 'create':
                return $this->createNetwork($name, $driver);
            case 'remove':
                return $this->removeNetwork($name);
            case 'list':
            default:
                return $this->listNetworks();
        }
    }

    /**
     * Handle volume operations
     */
    private function handleVolume(array $data): array
    {
        $action = $data['action'] ?? 'list';
        $name = $data['name'] ?? '';

        switch ($action) {
            case 'create':
                return $this->createVolume($name);
            case 'remove':
                return $this->removeVolume($name);
            case 'list':
            default:
                return $this->listVolumes();
        }
    }

    /**
     * Handle Docker Compose operations
     */
    private function handleCompose(array $data): array
    {
        $action = $data['action'] ?? 'up';
        $file = $data['file'] ?? 'docker-compose.yml';
        $services = $data['services'] ?? [];

        switch ($action) {
            case 'up':
                return $this->composeUp($file, $services);
            case 'down':
                return $this->composeDown($file);
            case 'build':
                return $this->composeBuild($file, $services);
            case 'logs':
                return $this->composeLogs($file, $services);
            case 'ps':
                return $this->composePs($file);
            case 'restart':
                return $this->composeRestart($file, $services);
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown compose action: ' . $action,
                    'data' => null
                ];
        }
    }

    /**
     * Handle system operations
     */
    private function handleSystem(array $data): array
    {
        $action = $data['action'] ?? 'info';

        switch ($action) {
            case 'prune':
                return $this->systemPrune($data['all'] ?? false);
            case 'df':
                return $this->systemDf();
            case 'info':
            default:
                return $this->getDockerInfo();
        }
    }

    /**
     * Create a new container
     */
    private function createContainer(string $name, string $image, array $ports, array $volumes, array $environment, string $command): array
    {
        $cmd = [$this->dockerPath, 'create'];
        
        if ($name) {
            $cmd[] = '--name';
            $cmd[] = $name;
        }

        foreach ($ports as $hostPort => $containerPort) {
            $cmd[] = '-p';
            $cmd[] = "$hostPort:$containerPort";
        }

        foreach ($volumes as $hostPath => $containerPath) {
            $cmd[] = '-v';
            $cmd[] = "$hostPath:$containerPath";
        }

        foreach ($environment as $key => $value) {
            $cmd[] = '-e';
            $cmd[] = "$key=$value";
        }

        if ($command) {
            $cmd[] = '--entrypoint';
            $cmd[] = $command;
        }

        $cmd[] = $image;

        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'container_id' => trim($result['output']),
                'name' => $name,
                'image' => $image
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Start a container
     */
    private function startContainer(string $name): array
    {
        $cmd = [$this->dockerPath, 'start', $name];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'container' => $name,
                'status' => 'started'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Stop a container
     */
    private function stopContainer(string $name): array
    {
        $cmd = [$this->dockerPath, 'stop', $name];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'container' => $name,
                'status' => 'stopped'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Remove a container
     */
    private function removeContainer(string $name): array
    {
        $cmd = [$this->dockerPath, 'rm', '-f', $name];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'container' => $name,
                'status' => 'removed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * List containers
     */
    private function listContainers(bool $all = false): array
    {
        $cmd = [$this->dockerPath, 'ps'];
        if ($all) {
            $cmd[] = '-a';
        }
        $cmd[] = '--format';
        $cmd[] = 'json';

        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $containers = [];
            $lines = explode("\n", trim($result['output']));
            foreach ($lines as $line) {
                if (trim($line)) {
                    $containers[] = json_decode($line, true);
                }
            }
            
            return [
                'success' => true,
                'data' => $containers,
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    /**
     * Get container logs
     */
    private function getContainerLogs(string $name, int $lines = 100): array
    {
        $cmd = [$this->dockerPath, 'logs', '--tail', $lines, $name];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'container' => $name,
                'logs' => $result['output']
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Inspect container
     */
    private function inspectContainer(string $name): array
    {
        $cmd = [$this->dockerPath, 'inspect', $name];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $inspection = json_decode($result['output'], true);
            
            return [
                'success' => true,
                'data' => $inspection[0] ?? null,
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    /**
     * Execute command in container
     */
    private function execContainer(string $name, string $cmd): array
    {
        $dockerCmd = [$this->dockerPath, 'exec', $name, 'sh', '-c', $cmd];
        $result = $this->executeCommand($dockerCmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'container' => $name,
                'command' => $cmd,
                'output' => $result['output']
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Get container stats
     */
    private function getContainerStats(string $name): array
    {
        $cmd = [$this->dockerPath, 'stats', '--no-stream', '--format', 'json', $name];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $stats = json_decode($result['output'], true);
            
            return [
                'success' => true,
                'data' => $stats,
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    /**
     * Pull Docker image
     */
    private function pullImage(string $name, string $tag = 'latest'): array
    {
        $image = $tag === 'latest' ? $name : "$name:$tag";
        $cmd = [$this->dockerPath, 'pull', $image];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'image' => $image,
                'status' => 'pulled'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Build Docker image
     */
    private function buildImage(string $name, string $path): array
    {
        $cmd = [$this->dockerPath, 'build', '-t', $name, $path];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'image' => $name,
                'path' => $path,
                'status' => 'built'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Tag Docker image
     */
    private function tagImage(string $name, string $newName, string $newTag = 'latest'): array
    {
        $newImage = $newTag === 'latest' ? $newName : "$newName:$newTag";
        $cmd = [$this->dockerPath, 'tag', $name, $newImage];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'original' => $name,
                'new' => $newImage,
                'status' => 'tagged'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Remove Docker image
     */
    private function removeImage(string $name): array
    {
        $cmd = [$this->dockerPath, 'rmi', '-f', $name];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'image' => $name,
                'status' => 'removed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * List Docker images
     */
    private function listImages(): array
    {
        $cmd = [$this->dockerPath, 'images', '--format', 'json'];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $images = [];
            $lines = explode("\n", trim($result['output']));
            foreach ($lines as $line) {
                if (trim($line)) {
                    $images[] = json_decode($line, true);
                }
            }
            
            return [
                'success' => true,
                'data' => $images,
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    /**
     * Create Docker network
     */
    private function createNetwork(string $name, string $driver = 'bridge'): array
    {
        $cmd = [$this->dockerPath, 'network', 'create', '--driver', $driver, $name];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'network' => $name,
                'driver' => $driver,
                'status' => 'created'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Remove Docker network
     */
    private function removeNetwork(string $name): array
    {
        $cmd = [$this->dockerPath, 'network', 'rm', $name];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'network' => $name,
                'status' => 'removed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * List Docker networks
     */
    private function listNetworks(): array
    {
        $cmd = [$this->dockerPath, 'network', 'ls', '--format', 'json'];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $networks = [];
            $lines = explode("\n", trim($result['output']));
            foreach ($lines as $line) {
                if (trim($line)) {
                    $networks[] = json_decode($line, true);
                }
            }
            
            return [
                'success' => true,
                'data' => $networks,
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    /**
     * Create Docker volume
     */
    private function createVolume(string $name): array
    {
        $cmd = [$this->dockerPath, 'volume', 'create', $name];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'volume' => $name,
                'status' => 'created'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Remove Docker volume
     */
    private function removeVolume(string $name): array
    {
        $cmd = [$this->dockerPath, 'volume', 'rm', $name];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'volume' => $name,
                'status' => 'removed'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * List Docker volumes
     */
    private function listVolumes(): array
    {
        $cmd = [$this->dockerPath, 'volume', 'ls', '--format', 'json'];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $volumes = [];
            $lines = explode("\n", trim($result['output']));
            foreach ($lines as $line) {
                if (trim($line)) {
                    $volumes[] = json_decode($line, true);
                }
            }
            
            return [
                'success' => true,
                'data' => $volumes,
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    /**
     * Docker Compose up
     */
    private function composeUp(string $file, array $services = []): array
    {
        $cmd = [$this->composePath, '-f', $file, 'up', '-d'];
        if (!empty($services)) {
            $cmd = array_merge($cmd, $services);
        }
        
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'file' => $file,
                'services' => $services,
                'status' => 'started'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Docker Compose down
     */
    private function composeDown(string $file): array
    {
        $cmd = [$this->composePath, '-f', $file, 'down'];
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'file' => $file,
                'status' => 'stopped'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Docker Compose build
     */
    private function composeBuild(string $file, array $services = []): array
    {
        $cmd = [$this->composePath, '-f', $file, 'build'];
        if (!empty($services)) {
            $cmd = array_merge($cmd, $services);
        }
        
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'file' => $file,
                'services' => $services,
                'status' => 'built'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Docker Compose logs
     */
    private function composeLogs(string $file, array $services = []): array
    {
        $cmd = [$this->composePath, '-f', $file, 'logs'];
        if (!empty($services)) {
            $cmd = array_merge($cmd, $services);
        }
        
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'file' => $file,
                'services' => $services,
                'logs' => $result['output']
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * Docker Compose ps
     */
    private function composePs(string $file): array
    {
        $cmd = [$this->composePath, '-f', $file, 'ps', '--format', 'json'];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $services = [];
            $lines = explode("\n", trim($result['output']));
            foreach ($lines as $line) {
                if (trim($line)) {
                    $services[] = json_decode($line, true);
                }
            }
            
            return [
                'success' => true,
                'data' => $services,
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    /**
     * Docker Compose restart
     */
    private function composeRestart(string $file, array $services = []): array
    {
        $cmd = [$this->composePath, '-f', $file, 'restart'];
        if (!empty($services)) {
            $cmd = array_merge($cmd, $services);
        }
        
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'file' => $file,
                'services' => $services,
                'status' => 'restarted'
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * System prune
     */
    private function systemPrune(bool $all = false): array
    {
        $cmd = [$this->dockerPath, 'system', 'prune'];
        if ($all) {
            $cmd[] = '-a';
        }
        $cmd[] = '-f';
        
        $result = $this->executeCommand($cmd);
        
        return [
            'success' => $result['exit_code'] === 0,
            'data' => [
                'pruned' => true,
                'all' => $all
            ],
            'error' => $result['exit_code'] !== 0 ? $result['error'] : null
        ];
    }

    /**
     * System disk usage
     */
    private function systemDf(): array
    {
        $cmd = [$this->dockerPath, 'system', 'df', '--format', 'json'];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $df = json_decode($result['output'], true);
            
            return [
                'success' => true,
                'data' => $df,
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    /**
     * Get Docker info
     */
    private function getDockerInfo(): array
    {
        $cmd = [$this->dockerPath, 'info', '--format', 'json'];
        $result = $this->executeCommand($cmd);
        
        if ($result['exit_code'] === 0) {
            $info = json_decode($result['output'], true);
            
            return [
                'success' => true,
                'data' => [
                    'docker_info' => $info,
                    'docker_path' => $this->dockerPath,
                    'compose_path' => $this->composePath,
                    'version' => $this->getDockerVersion()
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

    /**
     * Get Docker version
     */
    private function getDockerVersion(): string
    {
        $cmd = [$this->dockerPath, '--version'];
        $result = $this->executeCommand($cmd);
        
        return $result['exit_code'] === 0 ? trim($result['output']) : 'Unknown';
    }

    /**
     * Find Docker executable path
     */
    private function findDockerPath(): string
    {
        $paths = ['docker', '/usr/bin/docker', '/usr/local/bin/docker'];
        
        foreach ($paths as $path) {
            $result = $this->executeCommand(['which', $path]);
            if ($result['exit_code'] === 0) {
                return trim($result['output']);
            }
        }
        
        return 'docker'; // Fallback
    }

    /**
     * Find Docker Compose executable path
     */
    private function findComposePath(): string
    {
        $paths = ['docker-compose', 'docker', '/usr/bin/docker-compose', '/usr/local/bin/docker-compose'];
        
        foreach ($paths as $path) {
            $result = $this->executeCommand(['which', $path]);
            if ($result['exit_code'] === 0) {
                return trim($result['output']);
            }
        }
        
        return 'docker-compose'; // Fallback
    }

    /**
     * Execute shell command
     */
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

    /**
     * Substitute context variables in data
     */
    private function substituteContext(array $data, array $context): array
    {
        $substituted = [];
        
        foreach ($data as $key => $value) {
            if (is_string($value)) {
                // Replace ${var} with context values
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