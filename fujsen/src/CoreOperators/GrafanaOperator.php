<?php

namespace Fujsen\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;

/**
 * Grafana Operator for TuskLang
 * 
 * Provides Grafana functionality with support for:
 * - Dashboard creation and management
 * - Alerting rules and notifications
 * - Data source configuration
 * - User and organization management
 * - Plugin installation and configuration
 * - Annotations and templating
 * 
 * Usage:
 * ```php
 * // Create dashboard
 * $dashboard = @grafana({
 *   action: "create_dashboard",
 *   title: "System Metrics",
 *   panels: @variable("dashboard_panels"),
 *   folder: "Monitoring"
 * })
 * 
 * // Create alert
 * $alert = @grafana({
 *   action: "create_alert",
 *   name: "High CPU Usage",
 *   query: "avg(rate(node_cpu_seconds_total[5m])) > 0.8",
 *   duration: "5m"
 * })
 * ```
 */
class GrafanaOperator extends BaseOperator
{
    private array $clients = [];
    private array $dashboards = [];
    private array $alerts = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('grafana');
        $this->config = array_merge([
            'default_url' => 'http://localhost:3000',
            'timeout' => 30,
            'retry_attempts' => 3,
            'retry_delay' => 1000,
            'enable_tls' => false,
            'tls_verify' => true,
            'tls_cert' => '',
            'tls_key' => '',
            'tls_ca' => '',
            'api_key' => '',
            'username' => '',
            'password' => ''
        ], $config);
    }

    /**
     * Execute Grafana operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $title = $params['title'] ?? '';
        $panels = $params['panels'] ?? [];
        $folder = $params['folder'] ?? '';
        $url = $params['url'] ?? $this->config['default_url'];
        $name = $params['name'] ?? '';
        $query = $params['query'] ?? '';
        $duration = $params['duration'] ?? '';
        $dataSource = $params['data_source'] ?? '';
        $type = $params['type'] ?? '';
        $config = $params['config'] ?? [];
        
        try {
            $client = $this->getClient($url);
            
            switch ($action) {
                case 'create_dashboard':
                    return $this->createDashboard($client, $title, $panels, $folder);
                case 'get_dashboard':
                    return $this->getDashboard($client, $params);
                case 'update_dashboard':
                    return $this->updateDashboard($client, $params);
                case 'delete_dashboard':
                    return $this->deleteDashboard($client, $params);
                case 'create_alert':
                    return $this->createAlert($client, $name, $query, $duration, $params);
                case 'get_alerts':
                    return $this->getAlerts($client);
                case 'update_alert':
                    return $this->updateAlert($client, $params);
                case 'delete_alert':
                    return $this->deleteAlert($client, $params);
                case 'create_datasource':
                    return $this->createDataSource($client, $name, $type, $config);
                case 'get_datasources':
                    return $this->getDataSources($client);
                case 'update_datasource':
                    return $this->updateDataSource($client, $params);
                case 'delete_datasource':
                    return $this->deleteDataSource($client, $params);
                case 'get_folders':
                    return $this->getFolders($client);
                case 'create_folder':
                    return $this->createFolder($client, $name);
                case 'get_users':
                    return $this->getUsers($client);
                case 'create_user':
                    return $this->createUser($client, $params);
                case 'get_organizations':
                    return $this->getOrganizations($client);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Grafana operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Create dashboard
     */
    private function createDashboard($client, string $title, array $panels, string $folder): array
    {
        $dashboard = [
            'dashboard' => [
                'title' => $title,
                'panels' => $panels,
                'time' => [
                    'from' => 'now-6h',
                    'to' => 'now'
                ],
                'timepicker' => [
                    'refresh_intervals' => ['5s', '10s', '30s', '1m', '5m', '15m', '30m', '1h', '2h', '1d']
                ],
                'templating' => [
                    'list' => []
                ],
                'annotations' => [
                    'list' => []
                ],
                'refresh' => '5s',
                'schemaVersion' => 16,
                'version' => 0,
                'links' => [],
                'gnetId' => null
            ],
            'folderId' => $folder ? $this->getFolderId($client, $folder) : 0,
            'overwrite' => false
        ];
        
        $response = $client->post('/api/dashboards/db', ['json' => $dashboard]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to create dashboard: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        $this->dashboards[$data['id']] = [
            'id' => $data['id'],
            'uid' => $data['uid'],
            'title' => $title,
            'url' => $data['url']
        ];
        
        return [
            'status' => 'created',
            'id' => $data['id'],
            'uid' => $data['uid'],
            'title' => $title,
            'url' => $data['url']
        ];
    }

    /**
     * Get dashboard
     */
    private function getDashboard($client, array $params): array
    {
        $uid = $params['uid'] ?? '';
        $id = $params['id'] ?? '';
        
        $path = $uid ? "/api/dashboards/uid/$uid" : "/api/dashboards/id/$id";
        
        $response = $client->get($path);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get dashboard: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'dashboard' => $data['dashboard'],
            'meta' => $data['meta']
        ];
    }

    /**
     * Update dashboard
     */
    private function updateDashboard($client, array $params): array
    {
        $uid = $params['uid'] ?? '';
        $dashboard = $params['dashboard'] ?? [];
        
        $path = "/api/dashboards/uid/$uid";
        
        $response = $client->put($path, ['json' => ['dashboard' => $dashboard]]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to update dashboard: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'updated',
            'id' => $data['id'],
            'uid' => $data['uid'],
            'title' => $data['title'],
            'url' => $data['url']
        ];
    }

    /**
     * Delete dashboard
     */
    private function deleteDashboard($client, array $params): array
    {
        $uid = $params['uid'] ?? '';
        
        $path = "/api/dashboards/uid/$uid";
        
        $response = $client->delete($path);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to delete dashboard: " . $response->getBody());
        }
        
        return [
            'status' => 'deleted',
            'uid' => $uid
        ];
    }

    /**
     * Create alert
     */
    private function createAlert($client, string $name, string $query, string $duration, array $params): array
    {
        $alert = [
            'name' => $name,
            'type' => 'graph',
            'query' => $query,
            'frequency' => '60s',
            'handler' => 1,
            'message' => $params['message'] ?? "Alert: $name",
            'notifications' => $params['notifications'] ?? [],
            'for' => $duration,
            'conditions' => [
                [
                    'type' => 'query',
                    'query' => [
                        'params' => ['A', '5m', 'now']
                    ],
                    'reducer' => [
                        'type' => 'avg',
                        'params' => []
                    ],
                    'evaluator' => [
                        'type' => 'gt',
                        'params' => [0.8]
                    ]
                ]
            ]
        ];
        
        $response = $client->post('/api/alerts', ['json' => $alert]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to create alert: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        $this->alerts[$data['id']] = [
            'id' => $data['id'],
            'name' => $name,
            'state' => $data['state']
        ];
        
        return [
            'status' => 'created',
            'id' => $data['id'],
            'name' => $name,
            'state' => $data['state']
        ];
    }

    /**
     * Get alerts
     */
    private function getAlerts($client): array
    {
        $response = $client->get('/api/alerts');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get alerts: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'alerts' => $data
        ];
    }

    /**
     * Update alert
     */
    private function updateAlert($client, array $params): array
    {
        $id = $params['id'] ?? '';
        $alert = $params['alert'] ?? [];
        
        $response = $client->put("/api/alerts/$id", ['json' => $alert]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to update alert: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'updated',
            'id' => $id,
            'alert' => $data
        ];
    }

    /**
     * Delete alert
     */
    private function deleteAlert($client, array $params): array
    {
        $id = $params['id'] ?? '';
        
        $response = $client->delete("/api/alerts/$id");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to delete alert: " . $response->getBody());
        }
        
        return [
            'status' => 'deleted',
            'id' => $id
        ];
    }

    /**
     * Create data source
     */
    private function createDataSource($client, string $name, string $type, array $config): array
    {
        $dataSource = [
            'name' => $name,
            'type' => $type,
            'url' => $config['url'] ?? '',
            'access' => $config['access'] ?? 'proxy',
            'isDefault' => $config['is_default'] ?? false,
            'jsonData' => $config['json_data'] ?? [],
            'secureJsonData' => $config['secure_json_data'] ?? []
        ];
        
        $response = $client->post('/api/datasources', ['json' => $dataSource]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to create data source: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'created',
            'id' => $data['id'],
            'name' => $name,
            'type' => $type
        ];
    }

    /**
     * Get data sources
     */
    private function getDataSources($client): array
    {
        $response = $client->get('/api/datasources');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get data sources: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'datasources' => $data
        ];
    }

    /**
     * Update data source
     */
    private function updateDataSource($client, array $params): array
    {
        $id = $params['id'] ?? '';
        $dataSource = $params['datasource'] ?? [];
        
        $response = $client->put("/api/datasources/$id", ['json' => $dataSource]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to update data source: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'updated',
            'id' => $id,
            'datasource' => $data
        ];
    }

    /**
     * Delete data source
     */
    private function deleteDataSource($client, array $params): array
    {
        $id = $params['id'] ?? '';
        
        $response = $client->delete("/api/datasources/$id");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to delete data source: " . $response->getBody());
        }
        
        return [
            'status' => 'deleted',
            'id' => $id
        ];
    }

    /**
     * Get folders
     */
    private function getFolders($client): array
    {
        $response = $client->get('/api/folders');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get folders: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'folders' => $data
        ];
    }

    /**
     * Create folder
     */
    private function createFolder($client, string $name): array
    {
        $folder = ['title' => $name];
        
        $response = $client->post('/api/folders', ['json' => $folder]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to create folder: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'created',
            'id' => $data['id'],
            'title' => $name
        ];
    }

    /**
     * Get users
     */
    private function getUsers($client): array
    {
        $response = $client->get('/api/admin/users');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get users: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'users' => $data
        ];
    }

    /**
     * Create user
     */
    private function createUser($client, array $params): array
    {
        $user = [
            'login' => $params['login'] ?? '',
            'email' => $params['email'] ?? '',
            'password' => $params['password'] ?? '',
            'name' => $params['name'] ?? ''
        ];
        
        $response = $client->post('/api/admin/users', ['json' => $user]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to create user: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'created',
            'id' => $data['id'],
            'login' => $user['login']
        ];
    }

    /**
     * Get organizations
     */
    private function getOrganizations($client): array
    {
        $response = $client->get('/api/orgs');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get organizations: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'organizations' => $data
        ];
    }

    /**
     * Get folder ID by name
     */
    private function getFolderId($client, string $folderName): int
    {
        $folders = $this->getFolders($client);
        
        foreach ($folders['folders'] as $folder) {
            if ($folder['title'] === $folderName) {
                return $folder['id'];
            }
        }
        
        // Create folder if it doesn't exist
        $newFolder = $this->createFolder($client, $folderName);
        return $newFolder['id'];
    }

    /**
     * Get or create Grafana client
     */
    private function getClient(string $url): object
    {
        if (!isset($this->clients[$url])) {
            $this->clients[$url] = $this->createClient($url);
        }
        
        return $this->clients[$url];
    }

    /**
     * Create HTTP client for Grafana
     */
    private function createClient(string $url): object
    {
        $config = [
            'base_uri' => $url,
            'timeout' => $this->config['timeout'],
            'headers' => [
                'Content-Type' => 'application/json'
            ]
        ];
        
        if ($this->config['api_key']) {
            $config['headers']['Authorization'] = 'Bearer ' . $this->config['api_key'];
        } elseif ($this->config['username'] && $this->config['password']) {
            $config['auth'] = [$this->config['username'], $this->config['password']];
        }
        
        if ($this->config['enable_tls']) {
            $config['verify'] = $this->config['tls_verify'];
            if ($this->config['tls_cert'] && $this->config['tls_key']) {
                $config['cert'] = [$this->config['tls_cert'], $this->config['tls_key']];
            }
            if ($this->config['tls_ca']) {
                $config['verify'] = $this->config['tls_ca'];
            }
        }
        
        return new \GuzzleHttp\Client($config);
    }

    /**
     * Validate operator parameters
     */
    private function validateParams(array $params): void
    {
        if (!isset($params['action'])) {
            throw new OperatorException("Action is required");
        }
        
        $validActions = ['create_dashboard', 'get_dashboard', 'update_dashboard', 'delete_dashboard', 'create_alert', 'get_alerts', 'update_alert', 'delete_alert', 'create_datasource', 'get_datasources', 'update_datasource', 'delete_datasource', 'get_folders', 'create_folder', 'get_users', 'create_user', 'get_organizations'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['create_dashboard']) && (!isset($params['title']) || !isset($params['panels']))) {
            throw new OperatorException("Title and panels are required for create_dashboard action");
        }
        
        if (in_array($params['action'], ['get_dashboard', 'update_dashboard', 'delete_dashboard']) && (!isset($params['uid']) && !isset($params['id']))) {
            throw new OperatorException("UID or ID is required for " . $params['action'] . " action");
        }
        
        if (in_array($params['action'], ['create_alert']) && (!isset($params['name']) || !isset($params['query']) || !isset($params['duration']))) {
            throw new OperatorException("Name, query, and duration are required for create_alert action");
        }
        
        if (in_array($params['action'], ['update_alert', 'delete_alert']) && !isset($params['id'])) {
            throw new OperatorException("ID is required for " . $params['action'] . " action");
        }
        
        if (in_array($params['action'], ['create_datasource']) && (!isset($params['name']) || !isset($params['type']) || !isset($params['config']))) {
            throw new OperatorException("Name, type, and config are required for create_datasource action");
        }
        
        if (in_array($params['action'], ['update_datasource', 'delete_datasource']) && !isset($params['id'])) {
            throw new OperatorException("ID is required for " . $params['action'] . " action");
        }
        
        if (in_array($params['action'], ['create_folder']) && !isset($params['name'])) {
            throw new OperatorException("Name is required for create_folder action");
        }
        
        if (in_array($params['action'], ['create_user']) && (!isset($params['login']) || !isset($params['email']) || !isset($params['password']))) {
            throw new OperatorException("Login, email, and password are required for create_user action");
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        $this->clients = [];
        $this->dashboards = [];
        $this->alerts = [];
    }

    /**
     * Get operator schema
     */
    public function getSchema(): array
    {
        return [
            'type' => 'object',
            'properties' => [
                'action' => [
                    'type' => 'string',
                    'enum' => ['create_dashboard', 'get_dashboard', 'update_dashboard', 'delete_dashboard', 'create_alert', 'get_alerts', 'update_alert', 'delete_alert', 'create_datasource', 'get_datasources', 'update_datasource', 'delete_datasource', 'get_folders', 'create_folder', 'get_users', 'create_user', 'get_organizations'],
                    'description' => 'Grafana action'
                ],
                'title' => ['type' => 'string', 'description' => 'Dashboard title'],
                'panels' => ['type' => 'array', 'description' => 'Dashboard panels'],
                'folder' => ['type' => 'string', 'description' => 'Folder name'],
                'url' => ['type' => 'string', 'description' => 'Grafana URL'],
                'name' => ['type' => 'string', 'description' => 'Alert/DataSource name'],
                'query' => ['type' => 'string', 'description' => 'Alert query'],
                'duration' => ['type' => 'string', 'description' => 'Alert duration'],
                'data_source' => ['type' => 'string', 'description' => 'Data source name'],
                'type' => ['type' => 'string', 'description' => 'Data source type'],
                'config' => ['type' => 'object', 'description' => 'Configuration']
            ],
            'required' => ['action']
        ];
    }
} 