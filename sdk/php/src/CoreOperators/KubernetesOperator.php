<?php

namespace TuskLang\CoreOperators;

use TuskLang\CoreOperators\BaseOperator;

/**
 * Kubernetes Operator for managing Kubernetes resources and operations
 */
class KubernetesOperator extends BaseOperator
{
    public function execute(array $config, array $context = []): mixed
    {
        $action = $config['action'] ?? 'get';
        $data = $config['data'] ?? '';
        $options = $config['options'] ?? [];

        try {
            switch ($action) {
                case 'get':
                    return $this->getResource($data, $options);
                case 'create':
                    return $this->createResource($data, $options);
                case 'update':
                    return $this->updateResource($data, $options);
                case 'delete':
                    return $this->deleteResource($data, $options);
                case 'apply':
                    return $this->applyResource($data, $options);
                case 'scale':
                    return $this->scaleResource($data, $options);
                case 'logs':
                    return $this->getLogs($data, $options);
                case 'exec':
                    return $this->execCommand($data, $options);
                case 'port-forward':
                    return $this->portForward($data, $options);
                case 'describe':
                    return $this->describeResource($data, $options);
                case 'list':
                    return $this->listResources($data, $options);
                case 'watch':
                    return $this->watchResource($data, $options);
                default:
                    throw new \Exception("Unknown Kubernetes action: $action");
            }
        } catch (\Exception $e) {
            error_log("Kubernetes Operator error: " . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Get Kubernetes resource
     */
    private function getResource(string $resourceName, array $options): array
    {
        $resourceType = $options['type'] ?? 'pods';
        $namespace = $options['namespace'] ?? 'default';
        $output = $options['output'] ?? 'json';

        $command = "kubectl get $resourceType $resourceName -n $namespace --output=$output";
        
        if ($output === 'yaml') {
            $command .= ' --export';
        }

        $result = $this->executeKubectlCommand($command);
        
        return [
            'success' => $result['exitCode'] === 0,
            'data' => $result['output'],
            'error' => $result['error'],
            'command' => $command
        ];
    }

    /**
     * Create Kubernetes resource
     */
    private function createResource(array $resourceData, array $options): array
    {
        $namespace = $options['namespace'] ?? 'default';
        $dryRun = $options['dryRun'] ?? false;
        $validate = $options['validate'] ?? true;

        $yaml = $this->arrayToYaml($resourceData);
        $tempFile = $this->createTempFile($yaml);

        $command = "kubectl create -f $tempFile -n $namespace";
        
        if ($dryRun) {
            $command .= ' --dry-run=client';
        }
        
        if (!$validate) {
            $command .= ' --validate=false';
        }

        $result = $this->executeKubectlCommand($command);
        unlink($tempFile);

        return [
            'success' => $result['exitCode'] === 0,
            'data' => $result['output'],
            'error' => $result['error'],
            'command' => $command,
            'resource' => $resourceData
        ];
    }

    /**
     * Update Kubernetes resource
     */
    private function updateResource(array $resourceData, array $options): array
    {
        $namespace = $options['namespace'] ?? 'default';
        $force = $options['force'] ?? false;
        $cascade = $options['cascade'] ?? true;

        $yaml = $this->arrayToYaml($resourceData);
        $tempFile = $this->createTempFile($yaml);

        $command = "kubectl apply -f $tempFile -n $namespace";
        
        if ($force) {
            $command .= ' --force';
        }
        
        if (!$cascade) {
            $command .= ' --cascade=false';
        }

        $result = $this->executeKubectlCommand($command);
        unlink($tempFile);

        return [
            'success' => $result['exitCode'] === 0,
            'data' => $result['output'],
            'error' => $result['error'],
            'command' => $command,
            'resource' => $resourceData
        ];
    }

    /**
     * Delete Kubernetes resource
     */
    private function deleteResource(string $resourceName, array $options): array
    {
        $resourceType = $options['type'] ?? 'pods';
        $namespace = $options['namespace'] ?? 'default';
        $force = $options['force'] ?? false;
        $gracePeriod = $options['gracePeriod'] ?? null;
        $cascade = $options['cascade'] ?? true;

        $command = "kubectl delete $resourceType $resourceName -n $namespace";
        
        if ($force) {
            $command .= ' --force';
        }
        
        if ($gracePeriod !== null) {
            $command .= " --grace-period=$gracePeriod";
        }
        
        if (!$cascade) {
            $command .= ' --cascade=false';
        }

        $result = $this->executeKubectlCommand($command);

        return [
            'success' => $result['exitCode'] === 0,
            'data' => $result['output'],
            'error' => $result['error'],
            'command' => $command
        ];
    }

    /**
     * Apply Kubernetes resource
     */
    private function applyResource(array $resourceData, array $options): array
    {
        $namespace = $options['namespace'] ?? 'default';
        $dryRun = $options['dryRun'] ?? false;
        $force = $options['force'] ?? false;
        $prune = $options['prune'] ?? false;

        $yaml = $this->arrayToYaml($resourceData);
        $tempFile = $this->createTempFile($yaml);

        $command = "kubectl apply -f $tempFile -n $namespace";
        
        if ($dryRun) {
            $command .= ' --dry-run=client';
        }
        
        if ($force) {
            $command .= ' --force';
        }
        
        if ($prune) {
            $command .= ' --prune';
        }

        $result = $this->executeKubectlCommand($command);
        unlink($tempFile);

        return [
            'success' => $result['exitCode'] === 0,
            'data' => $result['output'],
            'error' => $result['error'],
            'command' => $command,
            'resource' => $resourceData
        ];
    }

    /**
     * Scale Kubernetes resource
     */
    private function scaleResource(string $resourceName, array $options): array
    {
        $resourceType = $options['type'] ?? 'deployment';
        $namespace = $options['namespace'] ?? 'default';
        $replicas = $options['replicas'] ?? 1;
        $currentReplicas = $options['currentReplicas'] ?? null;

        $command = "kubectl scale $resourceType $resourceName --replicas=$replicas -n $namespace";
        
        if ($currentReplicas !== null) {
            $command .= " --current-replicas=$currentReplicas";
        }

        $result = $this->executeKubectlCommand($command);

        return [
            'success' => $result['exitCode'] === 0,
            'data' => $result['output'],
            'error' => $result['error'],
            'command' => $command,
            'scaledTo' => $replicas
        ];
    }

    /**
     * Get logs from Kubernetes resource
     */
    private function getLogs(string $resourceName, array $options): array
    {
        $namespace = $options['namespace'] ?? 'default';
        $container = $options['container'] ?? null;
        $tail = $options['tail'] ?? null;
        $follow = $options['follow'] ?? false;
        $previous = $options['previous'] ?? false;
        $timestamps = $options['timestamps'] ?? false;

        $command = "kubectl logs $resourceName -n $namespace";
        
        if ($container) {
            $command .= " -c $container";
        }
        
        if ($tail) {
            $command .= " --tail=$tail";
        }
        
        if ($follow) {
            $command .= ' --follow';
        }
        
        if ($previous) {
            $command .= ' --previous';
        }
        
        if ($timestamps) {
            $command .= ' --timestamps';
        }

        $result = $this->executeKubectlCommand($command);

        return [
            'success' => $result['exitCode'] === 0,
            'logs' => $result['output'],
            'error' => $result['error'],
            'command' => $command
        ];
    }

    /**
     * Execute command in Kubernetes pod
     */
    private function execCommand(string $podName, array $options): array
    {
        $namespace = $options['namespace'] ?? 'default';
        $container = $options['container'] ?? null;
        $command = $options['command'] ?? '/bin/sh';
        $stdin = $options['stdin'] ?? false;
        $tty = $options['tty'] ?? false;

        $kubectlCommand = "kubectl exec $podName -n $namespace";
        
        if ($container) {
            $kubectlCommand .= " -c $container";
        }
        
        if ($stdin) {
            $kubectlCommand .= ' -i';
        }
        
        if ($tty) {
            $kubectlCommand .= ' -t';
        }
        
        $kubectlCommand .= " -- $command";

        $result = $this->executeKubectlCommand($kubectlCommand);

        return [
            'success' => $result['exitCode'] === 0,
            'output' => $result['output'],
            'error' => $result['error'],
            'command' => $kubectlCommand
        ];
    }

    /**
     * Port forward to Kubernetes service
     */
    private function portForward(string $resourceName, array $options): array
    {
        $namespace = $options['namespace'] ?? 'default';
        $localPort = $options['localPort'] ?? 8080;
        $remotePort = $options['remotePort'] ?? 80;
        $resourceType = $options['type'] ?? 'service';

        $command = "kubectl port-forward $resourceType/$resourceName $localPort:$remotePort -n $namespace";

        $result = $this->executeKubectlCommand($command, true); // Run in background

        return [
            'success' => $result['exitCode'] === 0,
            'data' => $result['output'],
            'error' => $result['error'],
            'command' => $command,
            'portMapping' => "$localPort:$remotePort"
        ];
    }

    /**
     * Describe Kubernetes resource
     */
    private function describeResource(string $resourceName, array $options): array
    {
        $resourceType = $options['type'] ?? 'pods';
        $namespace = $options['namespace'] ?? 'default';
        $output = $options['output'] ?? 'yaml';

        $command = "kubectl describe $resourceType $resourceName -n $namespace --output=$output";

        $result = $this->executeKubectlCommand($command);

        return [
            'success' => $result['exitCode'] === 0,
            'description' => $result['output'],
            'error' => $result['error'],
            'command' => $command
        ];
    }

    /**
     * List Kubernetes resources
     */
    private function listResources(array $filters, array $options): array
    {
        $resourceType = $options['type'] ?? 'pods';
        $namespace = $options['namespace'] ?? 'default';
        $output = $options['output'] ?? 'json';
        $labelSelector = $options['labelSelector'] ?? null;
        $fieldSelector = $options['fieldSelector'] ?? null;

        $command = "kubectl get $resourceType -n $namespace --output=$output";
        
        if ($labelSelector) {
            $command .= " -l $labelSelector";
        }
        
        if ($fieldSelector) {
            $command .= " --field-selector=$fieldSelector";
        }

        $result = $this->executeKubectlCommand($command);

        return [
            'success' => $result['exitCode'] === 0,
            'resources' => $result['output'],
            'error' => $result['error'],
            'command' => $command,
            'count' => $this->countResources($result['output'], $output)
        ];
    }

    /**
     * Watch Kubernetes resource
     */
    private function watchResource(string $resourceName, array $options): array
    {
        $resourceType = $options['type'] ?? 'pods';
        $namespace = $options['namespace'] ?? 'default';
        $output = $options['output'] ?? 'json';
        $timeout = $options['timeout'] ?? 300; // 5 minutes

        $command = "kubectl get $resourceType $resourceName -n $namespace --output=$output --watch --timeout=${timeout}s";

        $result = $this->executeKubectlCommand($command);

        return [
            'success' => $result['exitCode'] === 0,
            'events' => $result['output'],
            'error' => $result['error'],
            'command' => $command
        ];
    }

    /**
     * Execute kubectl command
     */
    private function executeKubectlCommand(string $command, bool $background = false): array
    {
        $output = [];
        $error = '';
        $exitCode = 0;

        if ($background) {
            $command .= ' > /dev/null 2>&1 &';
            exec($command, $output, $exitCode);
        } else {
            exec($command . ' 2>&1', $output, $exitCode);
        }

        return [
            'exitCode' => $exitCode,
            'output' => implode("\n", $output),
            'error' => $exitCode !== 0 ? implode("\n", $output) : ''
        ];
    }

    /**
     * Convert array to YAML
     */
    private function arrayToYaml(array $data): string
    {
        $yaml = '';
        
        foreach ($data as $key => $value) {
            if (is_array($value)) {
                $yaml .= "$key:\n";
                $yaml .= $this->arrayToYamlIndented($value, 2);
            } else {
                $yaml .= "$key: " . $this->formatYamlValue($value) . "\n";
            }
        }
        
        return $yaml;
    }

    /**
     * Convert array to YAML with indentation
     */
    private function arrayToYamlIndented(array $data, int $indent): string
    {
        $yaml = '';
        $spaces = str_repeat(' ', $indent);
        
        foreach ($data as $key => $value) {
            if (is_array($value)) {
                $yaml .= "$spaces$key:\n";
                $yaml .= $this->arrayToYamlIndented($value, $indent + 2);
            } else {
                $yaml .= "$spaces$key: " . $this->formatYamlValue($value) . "\n";
            }
        }
        
        return $yaml;
    }

    /**
     * Format value for YAML
     */
    private function formatYamlValue($value): string
    {
        if (is_string($value)) {
            if (strpos($value, "\n") !== false || strpos($value, '"') !== false) {
                return '"' . addcslashes($value, '"\\') . '"';
            }
            return $value;
        } elseif (is_bool($value)) {
            return $value ? 'true' : 'false';
        } elseif (is_null($value)) {
            return 'null';
        }
        
        return (string)$value;
    }

    /**
     * Create temporary file
     */
    private function createTempFile(string $content): string
    {
        $tempFile = tempnam(sys_get_temp_dir(), 'k8s_');
        file_put_contents($tempFile, $content);
        return $tempFile;
    }

    /**
     * Count resources in output
     */
    private function countResources(string $output, string $format): int
    {
        if ($format === 'json') {
            $data = json_decode($output, true);
            if (isset($data['items'])) {
                return count($data['items']);
            }
            return 1;
        }
        
        // For other formats, count lines
        $lines = explode("\n", trim($output));
        return count(array_filter($lines));
    }

    /**
     * Get cluster info
     */
    public function getClusterInfo(): array
    {
        $command = "kubectl cluster-info";
        $result = $this->executeKubectlCommand($command);

        return [
            'success' => $result['exitCode'] === 0,
            'info' => $result['output'],
            'error' => $result['error']
        ];
    }

    /**
     * Get nodes
     */
    public function getNodes(): array
    {
        $command = "kubectl get nodes --output=json";
        $result = $this->executeKubectlCommand($command);

        return [
            'success' => $result['exitCode'] === 0,
            'nodes' => json_decode($result['output'], true),
            'error' => $result['error']
        ];
    }

    /**
     * Get namespaces
     */
    public function getNamespaces(): array
    {
        $command = "kubectl get namespaces --output=json";
        $result = $this->executeKubectlCommand($command);

        return [
            'success' => $result['exitCode'] === 0,
            'namespaces' => json_decode($result['output'], true),
            'error' => $result['error']
        ];
    }
} 