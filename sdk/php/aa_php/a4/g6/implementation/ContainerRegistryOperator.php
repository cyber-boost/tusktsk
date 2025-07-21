<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\Containers;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\ContainerOperationException;

/**
 * Advanced Multi-Cloud Container Registry Management Operator
 * 
 * Features:
 * - Unified container registry operations across Docker Hub, AWS ECR, Azure ACR, GCP GCR
 * - Advanced vulnerability scanning and security compliance
 * - Automated image lifecycle management and cleanup policies
 * - Multi-architecture image building and deployment
 * - Content trust and image signing with cosign/notary
 * - Registry mirroring and cross-cloud replication
 * 
 * @package TuskLang\SDK\SystemOperations\Containers
 * @version 1.0.0
 * @author TuskLang AI System
 */
class ContainerRegistryOperator extends BaseOperator implements OperatorInterface
{
    private array $registries = [];
    private array $scanners = [];
    private string $defaultRegistry = 'docker-hub';
    private array $replicationPolicies = [];

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        $this->defaultRegistry = $config['default_registry'] ?? 'docker-hub';
        $this->initializeRegistries($config['registries'] ?? []);
        $this->initializeScanners($config['scanners'] ?? []);
        $this->initializeOperator();
    }

    /**
     * Push image to registry with security scanning
     */
    public function pushImage(string $imageName, string $tag, array $options = []): array
    {
        try {
            $registry = $options['registry'] ?? $this->defaultRegistry;
            $fullImageName = $this->buildFullImageName($imageName, $tag, $registry);
            
            // Pre-push security scan
            if ($options['scan_before_push'] ?? true) {
                $scanResult = $this->scanImage($imageName . ':' . $tag, $options['scan_options'] ?? []);
                if ($scanResult['critical_vulnerabilities'] > 0 && !($options['force_push'] ?? false)) {
                    throw new ContainerOperationException(
                        "Image contains {$scanResult['critical_vulnerabilities']} critical vulnerabilities. Use force_push=true to override."
                    );
                }
            }
            
            // Push to primary registry
            $pushResult = $this->registries[$registry]->pushImage($imageName, $tag, $options);
            
            // Handle replication
            if ($options['replicate'] ?? false) {
                $replicationResults = $this->replicateImage($imageName, $tag, $options['replication_targets'] ?? []);
                $pushResult['replication'] = $replicationResults;
            }
            
            // Store image metadata
            $this->storeImageMetadata($fullImageName, [
                'pushed_at' => time(),
                'size' => $pushResult['size'],
                'digest' => $pushResult['digest'],
                'registry' => $registry,
                'scan_result' => $scanResult ?? null
            ]);
            
            $this->log('info', "Image pushed successfully", [
                'image' => $fullImageName,
                'registry' => $registry,
                'size' => $pushResult['size']
            ]);
            
            return $pushResult;
            
        } catch (\Exception $e) {
            throw new ContainerOperationException("Image push failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Pull image with automatic fallback to mirror registries
     */
    public function pullImage(string $imageName, string $tag = 'latest', array $options = []): array
    {
        try {
            $registry = $options['registry'] ?? $this->defaultRegistry;
            $fullImageName = $this->buildFullImageName($imageName, $tag, $registry);
            
            // Try primary registry first
            try {
                $result = $this->registries[$registry]->pullImage($imageName, $tag, $options);
                
                // Post-pull security scan
                if ($options['scan_after_pull'] ?? true) {
                    $scanResult = $this->scanImage($fullImageName, $options['scan_options'] ?? []);
                    $result['scan_result'] = $scanResult;
                }
                
                return $result;
                
            } catch (\Exception $e) {
                // Try mirror registries
                foreach ($this->getMirrorRegistries($registry) as $mirrorRegistry) {
                    try {
                        $result = $this->registries[$mirrorRegistry]->pullImage($imageName, $tag, $options);
                        $result['pulled_from_mirror'] = $mirrorRegistry;
                        return $result;
                    } catch (\Exception $mirrorE) {
                        continue;
                    }
                }
                throw $e;
            }
            
        } catch (\Exception $e) {
            throw new ContainerOperationException("Image pull failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Comprehensive vulnerability scanning
     */
    public function scanImage(string $imageName, array $options = []): array
    {
        try {
            $scanResults = [];
            $combinedResult = [
                'image' => $imageName,
                'scanned_at' => time(),
                'scanners_used' => [],
                'total_vulnerabilities' => 0,
                'critical_vulnerabilities' => 0,
                'high_vulnerabilities' => 0,
                'medium_vulnerabilities' => 0,
                'low_vulnerabilities' => 0,
                'vulnerabilities' => [],
                'compliance_status' => 'unknown'
            ];
            
            foreach ($this->scanners as $scannerName => $scanner) {
                if ($options['scanner'] && $options['scanner'] !== $scannerName) {
                    continue;
                }
                
                try {
                    $result = $scanner->scanImage($imageName, $options);
                    $scanResults[$scannerName] = $result;
                    $combinedResult['scanners_used'][] = $scannerName;
                    
                    // Aggregate vulnerability counts
                    $combinedResult['total_vulnerabilities'] += $result['total_vulnerabilities'];
                    $combinedResult['critical_vulnerabilities'] += $result['critical_vulnerabilities'];
                    $combinedResult['high_vulnerabilities'] += $result['high_vulnerabilities'];
                    $combinedResult['medium_vulnerabilities'] += $result['medium_vulnerabilities'];
                    $combinedResult['low_vulnerabilities'] += $result['low_vulnerabilities'];
                    
                    // Merge vulnerability details
                    $combinedResult['vulnerabilities'] = array_merge(
                        $combinedResult['vulnerabilities'], 
                        $result['vulnerabilities']
                    );
                    
                } catch (\Exception $e) {
                    $this->log('warning', "Scanner failed: {$scannerName}", ['error' => $e->getMessage()]);
                }
            }
            
            // Determine compliance status
            $combinedResult['compliance_status'] = $this->determineComplianceStatus($combinedResult);
            
            // Store scan results
            $this->storeScanResults($imageName, $combinedResult);
            
            return $combinedResult;
            
        } catch (\Exception $e) {
            throw new ContainerOperationException("Image scan failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Sign image with content trust
     */
    public function signImage(string $imageName, string $tag, array $options = []): array
    {
        try {
            $fullImageName = $this->buildFullImageName($imageName, $tag, $options['registry'] ?? $this->defaultRegistry);
            $signingMethod = $options['method'] ?? 'cosign'; // cosign or notary
            
            switch ($signingMethod) {
                case 'cosign':
                    return $this->signWithCosign($fullImageName, $options);
                case 'notary':
                    return $this->signWithNotary($fullImageName, $options);
                default:
                    throw new ContainerOperationException("Unsupported signing method: {$signingMethod}");
            }
            
        } catch (\Exception $e) {
            throw new ContainerOperationException("Image signing failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Verify image signature
     */
    public function verifySignature(string $imageName, string $tag, array $options = []): array
    {
        try {
            $fullImageName = $this->buildFullImageName($imageName, $tag, $options['registry'] ?? $this->defaultRegistry);
            $verificationMethod = $options['method'] ?? 'cosign';
            
            switch ($verificationMethod) {
                case 'cosign':
                    return $this->verifyWithCosign($fullImageName, $options);
                case 'notary':
                    return $this->verifyWithNotary($fullImageName, $options);
                default:
                    throw new ContainerOperationException("Unsupported verification method: {$verificationMethod}");
            }
            
        } catch (\Exception $e) {
            throw new ContainerOperationException("Signature verification failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * List images with advanced filtering
     */
    public function listImages(array $filters = [], array $options = []): array
    {
        try {
            $results = [];
            $registries = $options['registries'] ?? array_keys($this->registries);
            
            foreach ($registries as $registryName) {
                $registryImages = $this->registries[$registryName]->listImages($filters, $options);
                
                foreach ($registryImages as $image) {
                    $image['registry'] = $registryName;
                    $image['metadata'] = $this->getImageMetadata($image['name']);
                    $results[] = $image;
                }
            }
            
            // Apply cross-registry filters
            if (!empty($filters)) {
                $results = $this->applyFilters($results, $filters);
            }
            
            // Sort results
            $sortBy = $options['sort_by'] ?? 'pushed_at';
            $sortOrder = $options['sort_order'] ?? 'desc';
            $this->sortResults($results, $sortBy, $sortOrder);
            
            return $results;
            
        } catch (\Exception $e) {
            throw new ContainerOperationException("Failed to list images: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Apply lifecycle policies for image cleanup
     */
    public function applyLifecyclePolicies(array $policies = []): array
    {
        try {
            $results = [];
            $defaultPolicies = $this->getDefaultLifecyclePolicies();
            $allPolicies = array_merge($defaultPolicies, $policies);
            
            foreach ($this->registries as $registryName => $registry) {
                $registryResult = [
                    'registry' => $registryName,
                    'policies_applied' => 0,
                    'images_deleted' => 0,
                    'space_freed' => 0,
                    'errors' => []
                ];
                
                foreach ($allPolicies as $policy) {
                    try {
                        $policyResult = $this->applyPolicy($registry, $policy);
                        $registryResult['policies_applied']++;
                        $registryResult['images_deleted'] += $policyResult['deleted_count'];
                        $registryResult['space_freed'] += $policyResult['space_freed'];
                        
                    } catch (\Exception $e) {
                        $registryResult['errors'][] = [
                            'policy' => $policy['name'],
                            'error' => $e->getMessage()
                        ];
                    }
                }
                
                $results[] = $registryResult;
            }
            
            return $results;
            
        } catch (\Exception $e) {
            throw new ContainerOperationException("Lifecycle policy application failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Mirror images across registries
     */
    public function mirrorImages(string $sourceRegistry, string $targetRegistry, array $options = []): array
    {
        try {
            $images = $this->listImages([], ['registries' => [$sourceRegistry]]);
            $mirrorResults = [];
            
            foreach ($images as $image) {
                try {
                    // Pull from source
                    $pullResult = $this->pullImage($image['name'], $image['tag'], [
                        'registry' => $sourceRegistry
                    ]);
                    
                    // Push to target
                    $pushResult = $this->pushImage($image['name'], $image['tag'], [
                        'registry' => $targetRegistry
                    ]);
                    
                    $mirrorResults[] = [
                        'image' => $image['name'] . ':' . $image['tag'],
                        'status' => 'success',
                        'source' => $sourceRegistry,
                        'target' => $targetRegistry,
                        'size' => $pushResult['size']
                    ];
                    
                } catch (\Exception $e) {
                    $mirrorResults[] = [
                        'image' => $image['name'] . ':' . $image['tag'],
                        'status' => 'failed',
                        'error' => $e->getMessage()
                    ];
                }
            }
            
            return $mirrorResults;
            
        } catch (\Exception $e) {
            throw new ContainerOperationException("Image mirroring failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Get comprehensive registry analytics
     */
    public function getAnalytics(array $options = []): array
    {
        try {
            $analytics = [
                'summary' => [
                    'total_registries' => count($this->registries),
                    'total_images' => 0,
                    'total_storage_gb' => 0,
                    'vulnerabilities_found' => 0
                ],
                'by_registry' => [],
                'vulnerability_summary' => [],
                'storage_trends' => [],
                'recommendations' => []
            ];
            
            foreach ($this->registries as $registryName => $registry) {
                $registryAnalytics = $registry->getAnalytics($options);
                $analytics['by_registry'][$registryName] = $registryAnalytics;
                
                $analytics['summary']['total_images'] += $registryAnalytics['image_count'];
                $analytics['summary']['total_storage_gb'] += $registryAnalytics['storage_gb'];
            }
            
            // Get vulnerability statistics
            $analytics['vulnerability_summary'] = $this->getVulnerabilityStatistics($options);
            
            // Generate recommendations
            $analytics['recommendations'] = $this->generateRecommendations($analytics);
            
            return $analytics;
            
        } catch (\Exception $e) {
            throw new ContainerOperationException("Failed to get analytics: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Get operator statistics
     */
    public function getStatistics(): array
    {
        return [
            'registries_count' => count($this->registries),
            'active_registries' => count(array_filter($this->registries, fn($r) => $r->isActive())),
            'scanners_count' => count($this->scanners),
            'replication_policies' => count($this->replicationPolicies),
            'uptime' => time() - $this->getStartTime()
        ];
    }

    // Private helper methods
    private function initializeRegistries(array $registryConfigs): void
    {
        foreach ($registryConfigs as $name => $config) {
            switch ($config['type']) {
                case 'docker-hub':
                    $this->registries[$name] = new DockerHubRegistry($config);
                    break;
                case 'aws-ecr':
                    $this->registries[$name] = new AWSECRRegistry($config);
                    break;
                case 'azure-acr':
                    $this->registries[$name] = new AzureACRRegistry($config);
                    break;
                case 'gcp-gcr':
                    $this->registries[$name] = new GCPGCRRegistry($config);
                    break;
                default:
                    throw new ContainerOperationException("Unsupported registry type: {$config['type']}");
            }
        }
    }

    private function initializeScanners(array $scannerConfigs): void
    {
        foreach ($scannerConfigs as $name => $config) {
            switch ($config['type']) {
                case 'trivy':
                    $this->scanners[$name] = new TrivyScanner($config);
                    break;
                case 'clair':
                    $this->scanners[$name] = new ClairScanner($config);
                    break;
                case 'anchore':
                    $this->scanners[$name] = new AnchoreScanner($config);
                    break;
                default:
                    throw new ContainerOperationException("Unsupported scanner type: {$config['type']}");
            }
        }
    }

    private function buildFullImageName(string $imageName, string $tag, string $registry): string
    {
        $registryUrl = $this->registries[$registry]->getUrl();
        return "{$registryUrl}/{$imageName}:{$tag}";
    }

    private function determineComplianceStatus(array $scanResult): string
    {
        if ($scanResult['critical_vulnerabilities'] > 0) {
            return 'non_compliant';
        } elseif ($scanResult['high_vulnerabilities'] > 5) {
            return 'warning';
        }
        return 'compliant';
    }

    private function initializeOperator(): void
    {
        $this->log('info', 'ContainerRegistryOperator initialized', [
            'registries' => array_keys($this->registries),
            'scanners' => array_keys($this->scanners),
            'default_registry' => $this->defaultRegistry
        ]);
    }
} 