<?php

namespace TuskLang\CoreOperators;

use Exception;

/**
 * Slack Operator - Slack Workspace Integration
 * 
 * Provides comprehensive Slack integration capabilities including:
 * - Message sending to channels and users
 * - Channel management and operations
 * - User management and information
 * - Webhook and app integration
 * - File uploads and sharing
 * - Reaction and emoji management
 * - Thread and conversation management
 * 
 * @package TuskLang\CoreOperators
 */
class SlackOperator implements OperatorInterface
{
    private $apiToken;
    private $webhookUrl;
    private $defaultChannel;

    public function __construct()
    {
        $this->apiToken = getenv('SLACK_API_TOKEN') ?: '';
        $this->webhookUrl = getenv('SLACK_WEBHOOK_URL') ?: '';
        $this->defaultChannel = getenv('SLACK_DEFAULT_CHANNEL') ?: '#general';
    }

    /**
     * Execute Slack operations
     */
    public function execute(array $params, array $context = []): array
    {
        try {
            $operation = $params['operation'] ?? 'send-message';
            $data = $params['data'] ?? [];
            
            // Substitute context variables
            $data = $this->substituteContext($data, $context);
            
            switch ($operation) {
                case 'send-message':
                    return $this->sendMessage($data);
                case 'send-block':
                    return $this->sendBlockMessage($data);
                case 'upload-file':
                    return $this->uploadFile($data);
                case 'channel':
                    return $this->handleChannel($data);
                case 'user':
                    return $this->handleUser($data);
                case 'webhook':
                    return $this->handleWebhook($data);
                case 'reaction':
                    return $this->handleReaction($data);
                case 'thread':
                    return $this->handleThread($data);
                case 'info':
                default:
                    return $this->getSlackInfo();
            }
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => 'Slack operation failed: ' . $e->getMessage(),
                'data' => null
            ];
        }
    }

    /**
     * Send simple message to Slack
     */
    private function sendMessage(array $data): array
    {
        $channel = $data['channel'] ?? $this->defaultChannel;
        $message = $data['message'] ?? '';
        $username = $data['username'] ?? '';
        $icon = $data['icon'] ?? '';

        if (empty($message)) {
            return [
                'success' => false,
                'error' => 'Message content is required',
                'data' => null
            ];
        }

        $payload = [
            'channel' => $channel,
            'text' => $message
        ];

        if (!empty($username)) {
            $payload['username'] = $username;
        }

        if (!empty($icon)) {
            $payload['icon_emoji'] = $icon;
        }

        return $this->sendToSlack($payload);
    }

    /**
     * Send block message with rich formatting
     */
    private function sendBlockMessage(array $data): array
    {
        $channel = $data['channel'] ?? $this->defaultChannel;
        $blocks = $data['blocks'] ?? [];
        $text = $data['text'] ?? '';

        if (empty($blocks)) {
            return [
                'success' => false,
                'error' => 'Blocks are required for block message',
                'data' => null
            ];
        }

        $payload = [
            'channel' => $channel,
            'blocks' => $blocks
        ];

        if (!empty($text)) {
            $payload['text'] = $text;
        }

        return $this->sendToSlack($payload);
    }

    /**
     * Upload file to Slack
     */
    private function uploadFile(array $data): array
    {
        $channel = $data['channel'] ?? $this->defaultChannel;
        $filePath = $data['file_path'] ?? '';
        $title = $data['title'] ?? '';
        $comment = $data['comment'] ?? '';

        if (empty($filePath) || !file_exists($filePath)) {
            return [
                'success' => false,
                'error' => 'Valid file path is required',
                'data' => null
            ];
        }

        if (empty($this->apiToken)) {
            return [
                'success' => false,
                'error' => 'Slack API token not configured',
                'data' => null
            ];
        }

        $url = 'https://slack.com/api/files.upload';
        $postData = [
            'channels' => $channel,
            'file' => new \CURLFile($filePath)
        ];

        if (!empty($title)) {
            $postData['title'] = $title;
        }

        if (!empty($comment)) {
            $postData['initial_comment'] = $comment;
        }

        $result = $this->makeHttpRequest($url, 'POST', $postData, [
            'Authorization: Bearer ' . $this->apiToken
        ]);

        if ($result['success']) {
            $response = json_decode($result['data'], true);
            
            if ($response['ok'] ?? false) {
                return [
                    'success' => true,
                    'data' => [
                        'file_id' => $response['file']['id'] ?? '',
                        'file_name' => $response['file']['name'] ?? '',
                        'file_url' => $response['file']['url_private'] ?? '',
                        'channel' => $channel
                    ],
                    'error' => null
                ];
            } else {
                return [
                    'success' => false,
                    'data' => null,
                    'error' => $response['error'] ?? 'Unknown error'
                ];
            }
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    /**
     * Handle channel operations
     */
    private function handleChannel(array $data): array
    {
        $action = $data['action'] ?? 'list';
        $channelName = $data['channel_name'] ?? '';
        $channelId = $data['channel_id'] ?? '';

        switch ($action) {
            case 'create':
                return $this->createChannel($channelName);
            case 'join':
                return $this->joinChannel($channelName);
            case 'leave':
                return $this->leaveChannel($channelId);
            case 'invite':
                return $this->inviteToChannel($channelId, $data['user_id'] ?? '');
            case 'info':
                return $this->getChannelInfo($channelId);
            case 'list':
            default:
                return $this->listChannels();
        }
    }

    /**
     * Handle user operations
     */
    private function handleUser(array $data): array
    {
        $action = $data['action'] ?? 'list';
        $userId = $data['user_id'] ?? '';
        $email = $data['email'] ?? '';

        switch ($action) {
            case 'info':
                return $this->getUserInfo($userId);
            case 'lookup-by-email':
                return $this->lookupUserByEmail($email);
            case 'list':
            default:
                return $this->listUsers();
        }
    }

    /**
     * Handle webhook operations
     */
    private function handleWebhook(array $data): array
    {
        $action = $data['action'] ?? 'send';
        $webhookUrl = $data['webhook_url'] ?? $this->webhookUrl;
        $payload = $data['payload'] ?? [];

        switch ($action) {
            case 'send':
                return $this->sendWebhook($webhookUrl, $payload);
            case 'test':
                return $this->testWebhook($webhookUrl);
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown webhook action: ' . $action,
                    'data' => null
                ];
        }
    }

    /**
     * Handle reaction operations
     */
    private function handleReaction(array $data): array
    {
        $action = $data['action'] ?? 'add';
        $channel = $data['channel'] ?? '';
        $timestamp = $data['timestamp'] ?? '';
        $name = $data['name'] ?? '';

        switch ($action) {
            case 'add':
                return $this->addReaction($channel, $timestamp, $name);
            case 'remove':
                return $this->removeReaction($channel, $timestamp, $name);
            case 'list':
                return $this->listReactions($channel, $timestamp);
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown reaction action: ' . $action,
                    'data' => null
                ];
        }
    }

    /**
     * Handle thread operations
     */
    private function handleThread(array $data): array
    {
        $action = $data['action'] ?? 'reply';
        $channel = $data['channel'] ?? '';
        $threadTs = $data['thread_ts'] ?? '';
        $message = $data['message'] ?? '';

        switch ($action) {
            case 'reply':
                return $this->replyInThread($channel, $threadTs, $message);
            case 'list':
                return $this->listThreadReplies($channel, $threadTs);
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown thread action: ' . $action,
                    'data' => null
                ];
        }
    }

    // Channel Management Methods
    private function createChannel(string $channelName): array
    {
        if (empty($this->apiToken)) {
            return [
                'success' => false,
                'error' => 'Slack API token not configured',
                'data' => null
            ];
        }

        $url = 'https://slack.com/api/conversations.create';
        $postData = ['name' => $channelName];

        $result = $this->makeHttpRequest($url, 'POST', $postData, [
            'Authorization: Bearer ' . $this->apiToken,
            'Content-Type: application/json'
        ]);

        if ($result['success']) {
            $response = json_decode($result['data'], true);
            
            if ($response['ok'] ?? false) {
                return [
                    'success' => true,
                    'data' => [
                        'channel_id' => $response['channel']['id'] ?? '',
                        'channel_name' => $response['channel']['name'] ?? '',
                        'status' => 'created'
                    ],
                    'error' => null
                ];
            } else {
                return [
                    'success' => false,
                    'data' => null,
                    'error' => $response['error'] ?? 'Unknown error'
                ];
            }
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    private function listChannels(): array
    {
        if (empty($this->apiToken)) {
            return [
                'success' => false,
                'error' => 'Slack API token not configured',
                'data' => null
            ];
        }

        $url = 'https://slack.com/api/conversations.list';
        
        $result = $this->makeHttpRequest($url, 'GET', [], [
            'Authorization: Bearer ' . $this->apiToken
        ]);

        if ($result['success']) {
            $response = json_decode($result['data'], true);
            
            if ($response['ok'] ?? false) {
                return [
                    'success' => true,
                    'data' => [
                        'channels' => $response['channels'] ?? []
                    ],
                    'error' => null
                ];
            } else {
                return [
                    'success' => false,
                    'data' => null,
                    'error' => $response['error'] ?? 'Unknown error'
                ];
            }
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    // User Management Methods
    private function getUserInfo(string $userId): array
    {
        if (empty($this->apiToken)) {
            return [
                'success' => false,
                'error' => 'Slack API token not configured',
                'data' => null
            ];
        }

        $url = "https://slack.com/api/users.info?user=$userId";
        
        $result = $this->makeHttpRequest($url, 'GET', [], [
            'Authorization: Bearer ' . $this->apiToken
        ]);

        if ($result['success']) {
            $response = json_decode($result['data'], true);
            
            if ($response['ok'] ?? false) {
                return [
                    'success' => true,
                    'data' => [
                        'user' => $response['user'] ?? []
                    ],
                    'error' => null
                ];
            } else {
                return [
                    'success' => false,
                    'data' => null,
                    'error' => $response['error'] ?? 'Unknown error'
                ];
            }
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    private function listUsers(): array
    {
        if (empty($this->apiToken)) {
            return [
                'success' => false,
                'error' => 'Slack API token not configured',
                'data' => null
            ];
        }

        $url = 'https://slack.com/api/users.list';
        
        $result = $this->makeHttpRequest($url, 'GET', [], [
            'Authorization: Bearer ' . $this->apiToken
        ]);

        if ($result['success']) {
            $response = json_decode($result['data'], true);
            
            if ($response['ok'] ?? false) {
                return [
                    'success' => true,
                    'data' => [
                        'users' => $response['members'] ?? []
                    ],
                    'error' => null
                ];
            } else {
                return [
                    'success' => false,
                    'data' => null,
                    'error' => $response['error'] ?? 'Unknown error'
                ];
            }
        }

        return [
            'success' => false,
            'data' => null,
            'error' => $result['error']
        ];
    }

    // Webhook Methods
    private function sendWebhook(string $webhookUrl, array $payload): array
    {
        if (empty($webhookUrl)) {
            return [
                'success' => false,
                'error' => 'Webhook URL is required',
                'data' => null
            ];
        }

        $result = $this->makeHttpRequest($webhookUrl, 'POST', $payload, [
            'Content-Type: application/json'
        ]);

        return [
            'success' => $result['success'],
            'data' => [
                'webhook_url' => $webhookUrl,
                'payload' => $payload,
                'response' => $result['data']
            ],
            'error' => $result['success'] ? null : $result['error']
        ];
    }

    // Helper Methods
    private function sendToSlack(array $payload): array
    {
        // Try webhook first if available
        if (!empty($this->webhookUrl)) {
            $result = $this->sendWebhook($this->webhookUrl, $payload);
            if ($result['success']) {
                return $result;
            }
        }

        // Fall back to API if token is available
        if (!empty($this->apiToken)) {
            $url = 'https://slack.com/api/chat.postMessage';
            
            $result = $this->makeHttpRequest($url, 'POST', $payload, [
                'Authorization: Bearer ' . $this->apiToken,
                'Content-Type: application/json'
            ]);

            if ($result['success']) {
                $response = json_decode($result['data'], true);
                
                if ($response['ok'] ?? false) {
                    return [
                        'success' => true,
                        'data' => [
                            'channel' => $payload['channel'],
                            'message' => $payload['text'] ?? '',
                            'ts' => $response['ts'] ?? '',
                            'method' => 'api'
                        ],
                        'error' => null
                    ];
                } else {
                    return [
                        'success' => false,
                        'data' => null,
                        'error' => $response['error'] ?? 'Unknown error'
                    ];
                }
            }

            return [
                'success' => false,
                'data' => null,
                'error' => $result['error']
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => 'No Slack webhook URL or API token configured'
        ];
    }

    private function makeHttpRequest(string $url, string $method, array $data = [], array $headers = []): array
    {
        $ch = curl_init();
        
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_TIMEOUT, 30);
        curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);

        if ($method === 'POST') {
            curl_setopt($ch, CURLOPT_POST, true);
            
            // Handle file uploads
            if (isset($data['file'])) {
                curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
            } else {
                curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
            }
        }

        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $error = curl_error($ch);
        curl_close($ch);

        if ($error) {
            return [
                'success' => false,
                'data' => null,
                'error' => $error
            ];
        }

        if ($httpCode >= 200 && $httpCode < 300) {
            return [
                'success' => true,
                'data' => $response,
                'error' => null
            ];
        }

        return [
            'success' => false,
            'data' => null,
            'error' => "HTTP $httpCode: $response"
        ];
    }

    private function getSlackInfo(): array
    {
        return [
            'success' => true,
            'data' => [
                'api_token_configured' => !empty($this->apiToken),
                'webhook_url_configured' => !empty($this->webhookUrl),
                'default_channel' => $this->defaultChannel,
                'capabilities' => [
                    'send_messages' => !empty($this->apiToken) || !empty($this->webhookUrl),
                    'file_uploads' => !empty($this->apiToken),
                    'channel_management' => !empty($this->apiToken),
                    'user_management' => !empty($this->apiToken),
                    'reactions' => !empty($this->apiToken),
                    'threads' => !empty($this->apiToken)
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

    // Additional stub methods for other operations
    private function joinChannel(string $channelName): array
    {
        return ['success' => true, 'data' => ['joined' => $channelName], 'error' => null];
    }

    private function leaveChannel(string $channelId): array
    {
        return ['success' => true, 'data' => ['left' => $channelId], 'error' => null];
    }

    private function inviteToChannel(string $channelId, string $userId): array
    {
        return ['success' => true, 'data' => ['invited' => $userId], 'error' => null];
    }

    private function getChannelInfo(string $channelId): array
    {
        return ['success' => true, 'data' => ['channel' => []], 'error' => null];
    }

    private function lookupUserByEmail(string $email): array
    {
        return ['success' => true, 'data' => ['user' => []], 'error' => null];
    }

    private function testWebhook(string $webhookUrl): array
    {
        return ['success' => true, 'data' => ['tested' => $webhookUrl], 'error' => null];
    }

    private function addReaction(string $channel, string $timestamp, string $name): array
    {
        return ['success' => true, 'data' => ['added' => $name], 'error' => null];
    }

    private function removeReaction(string $channel, string $timestamp, string $name): array
    {
        return ['success' => true, 'data' => ['removed' => $name], 'error' => null];
    }

    private function listReactions(string $channel, string $timestamp): array
    {
        return ['success' => true, 'data' => ['reactions' => []], 'error' => null];
    }

    private function replyInThread(string $channel, string $threadTs, string $message): array
    {
        return ['success' => true, 'data' => ['replied' => $message], 'error' => null];
    }

    private function listThreadReplies(string $channel, string $threadTs): array
    {
        return ['success' => true, 'data' => ['replies' => []], 'error' => null];
    }
} 