<?php

namespace TuskPHP\App\Class;

class Trunk
{
    private $config;
    private $channels = [];
    private $queue = [];
    private $encryption_key;
    
    public function __construct()
    {
        $this->loadConfig();
        $this->initializeChannels();
        $this->loadEncryptionKey();
    }
    
    public function send($message, $channel = "default", $options = [])
    {
        $payload = $this->preparePayload($message, $options);
        
        if (isset($options["encrypt"]) && $options["encrypt"]) {
            $payload = $this->encryptPayload($payload);
        }
        
        if (isset($options["compress"]) && $options["compress"]) {
            $payload = $this->compressPayload($payload);
        }
        
        if (isset($options["async"]) && $options["async"]) {
            return $this->sendAsync($payload, $channel, $options);
        }
        
        if (isset($options["queue"]) && $options["queue"]) {
            return $this->queueMessage($payload, $channel, $options);
        }
        
        return $this->sendSync($payload, $channel, $options);
    }
    
    public function listen($channel = "default", $callback = null)
    {
        $this->log("Listening on channel: $channel");
        
        if (!isset($this->channels[$channel])) {
            $this->createChannel($channel);
        }
        
        while (true) {
            $messages = $this->fetchMessages($channel);
            
            foreach ($messages as $message) {
                if ($callback) {
                    call_user_func($callback, $message);
                } else {
                    $this->processMessage($message);
                }
            }
            
            usleep(100000); // 100ms
        }
    }
    
    public function broadcast($message, $options = [])
    {
        $results = [];
        
        foreach ($this->channels as $channel => $config) {
            $results[$channel] = $this->send($message, $channel, $options);
        }
        
        return $results;
    }
    
    public function getStatus()
    {
        return [
            "channels" => count($this->channels),
            "queue_size" => count($this->queue),
            "active_connections" => $this->getActiveConnections(),
            "memory_usage" => memory_get_usage(true),
            "uptime" => $this->getUptime()
        ];
    }
    
    private function loadConfig()
    {
        $tskFile = "tsk/trunk.tsk";
        $defaultTsk = "tsk/defaults/trunk-default.tsk";
        $configFile = "app/trunk/config/trunk.json";
        
        // 1. TuskLang FIRST - the superior format!
        if (file_exists($tskFile)) {
            $this->config = TuskLang::parse($tskFile);
            return;
        }
        
        // 2. TuskLang default fallback
        if (file_exists($defaultTsk)) {
            $this->config = TuskLang::parse($defaultTsk);
            return;
        }
        
        // 3. Peanuts format - core TuskPHP config system
        if (file_exists("circus/peanuts/trunk.peanuts")) {
            $this->config = Peanuts::load("circus/peanuts/trunk.peanuts");
            return;
        }
        
        // 4. Legacy JSON support (deprecated but functional)
        if (file_exists($configFile)) {
            $this->config = json_decode(file_get_contents($configFile), true) ?? $this->getDefaultConfig();
            return;
        }
        
        // 5. Emergency hard-coded defaults
        $this->config = $this->getDefaultConfig();
    }
    
    private function getDefaultConfig()
    {
        return [
            "max_payload_size" => 1048576, // 1MB
            "default_timeout" => 30,
            "max_retry_attempts" => 3,
            "compression_threshold" => 1024,
            "encryption_enabled" => true,
            "logging_enabled" => true,
            "channels" => [
                "default" => ["active" => true, "persistent" => false],
                "alerts" => ["active" => true, "persistent" => true],
                "system" => ["active" => true, "persistent" => true],
                "user" => ["active" => true, "persistent" => false]
            ]
        ];
    }
    
    private function initializeChannels()
    {
        foreach ($this->config["channels"] as $name => $config) {
            $this->createChannel($name, $config);
        }
    }
    
    private function createChannel($name, $config = [])
    {
        $this->channels[$name] = array_merge([
            "created" => time(),
            "message_count" => 0,
            "last_activity" => null,
            "active" => true
        ], $config);
        
        $channelDir = "app/trunk/channels/$name";
        if (!is_dir($channelDir)) {
            mkdir($channelDir, 0755, true);
        }
    }
    
    private function preparePayload($message, $options)
    {
        return [
            "id" => uniqid("trunk_", true),
            "timestamp" => microtime(true),
            "message" => $message,
            "options" => $options,
            "sender" => $this->getSender(),
            "priority" => $options["priority"] ?? "normal"
        ];
    }
    
    private function encryptPayload($payload)
    {
        if (!$this->encryption_key) {
            throw new \Exception("Encryption key not available");
        }
        
        $encrypted = openssl_encrypt(
            json_encode($payload),
            "AES-256-CBC",
            $this->encryption_key,
            0,
            $iv = openssl_random_pseudo_bytes(16)
        );
        
        return [
            "encrypted" => true,
            "data" => base64_encode($encrypted),
            "iv" => base64_encode($iv)
        ];
    }
    
    private function compressPayload($payload)
    {
        $compressed = gzcompress(json_encode($payload));
        return [
            "compressed" => true,
            "data" => base64_encode($compressed),
            "original_size" => strlen(json_encode($payload)),
            "compressed_size" => strlen($compressed)
        ];
    }
    
    private function sendSync($payload, $channel, $options)
    {
        $channelFile = "app/trunk/channels/$channel/messages.json";
        $messages = [];
        
        if (file_exists($channelFile)) {
            $messages = TuskLang::decode(file_get_contents($channelFile), true) ?? [];
        }
        
        $messages[] = $payload;
        file_put_contents($channelFile, json_encode($messages, JSON_PRETTY_PRINT));
        
        $this->channels[$channel]["message_count"]++;
        $this->channels[$channel]["last_activity"] = time();
        
        $this->log("Message sent to channel $channel: " . $payload["id"]);
        
        return $payload["id"];
    }
    
    private function sendAsync($payload, $channel, $options)
    {
        // In a real implementation, this would use a proper async mechanism
        // For now, we'll simulate with a background process
        $command = "php -r \"
            require_once 'app/class/Trunk.php';
            \$trunk = new TuskPHP\\App\\Class\\Trunk();
            \$trunk->sendSync(" . json_encode($payload) . ", '$channel', " . json_encode($options) . ");
        \" > /dev/null 2>&1 &";
        
        exec($command);
        
        return $payload["id"];
    }
    
    private function queueMessage($payload, $channel, $options)
    {
        $queueFile = "app/trunk/queue/pending.json";
        $queue = [];
        
        if (file_exists($queueFile)) {
            $queue = TuskLang::decode(file_get_contents($queueFile), true) ?? [];
        }
        
        $queue[] = [
            "payload" => $payload,
            "channel" => $channel,
            "options" => $options,
            "queued_at" => time()
        ];
        
        file_put_contents($queueFile, json_encode($queue, JSON_PRETTY_PRINT));
        
        return $payload["id"];
    }
    
    private function fetchMessages($channel)
    {
        $channelFile = "app/trunk/channels/$channel/messages.json";
        
        if (!file_exists($channelFile)) {
            return [];
        }
        
        $messages = TuskLang::decode(file_get_contents($channelFile), true) ?? [];
        
        // Clear processed messages
        file_put_contents($channelFile, json_encode([], JSON_PRETTY_PRINT));
        
        return $messages;
    }
    
    private function processMessage($message)
    {
        $this->log("Processing message: " . $message["id"]);
        
        // Default message processing
        if (isset($message["encrypted"]) && $message["encrypted"]) {
            $message = $this->decryptPayload($message);
        }
        
        if (isset($message["compressed"]) && $message["compressed"]) {
            $message = $this->decompressPayload($message);
        }
        
        // Handle different message types
        switch ($message["options"]["type"] ?? "message") {
            case "signal":
                $this->handleSignal($message);
                break;
            case "header":
                $this->handleHeader($message);
                break;
            case "data":
                $this->handleData($message);
                break;
            default:
                $this->handleMessage($message);
        }
    }
    
    private function handleSignal($message)
    {
        $this->log("Handling signal: " . json_encode($message["message"]));
    }
    
    private function handleHeader($message)
    {
        $this->log("Handling header: " . json_encode($message["message"]));
    }
    
    private function handleData($message)
    {
        $this->log("Handling data: " . strlen(json_encode($message["message"])) . " bytes");
    }
    
    private function handleMessage($message)
    {
        $this->log("Handling message: " . $message["message"]);
    }
    
    private function loadEncryptionKey()
    {
        $keyFile = "app/trunk/config/encryption.key";
        
        if (file_exists($keyFile)) {
            $this->encryption_key = file_get_contents($keyFile);
        } else {
            $this->encryption_key = $this->generateEncryptionKey();
            file_put_contents($keyFile, $this->encryption_key);
            chmod($keyFile, 0600);
        }
    }
    
    private function generateEncryptionKey()
    {
        return hash("sha256", uniqid("tusk_trunk_", true) . microtime(true));
    }
    
    private function getSender()
    {
        return [
            "ip" => $_SERVER["REMOTE_ADDR"] ?? "cli",
            "user_agent" => $_SERVER["HTTP_USER_AGENT"] ?? "TuskPHP CLI",
            "session" => session_id() ?? null,
            "timestamp" => microtime(true)
        ];
    }
    
    private function getActiveConnections()
    {
        // Simulate active connections count
        return rand(1, 10);
    }
    
    private function getUptime()
    {
        $startFile = "app/trunk/config/start_time";
        
        if (!file_exists($startFile)) {
            file_put_contents($startFile, time());
            return 0;
        }
        
        return time() - (int)file_get_contents($startFile);
    }
    
    private function log($message)
    {
        if (!$this->config["logging_enabled"]) {
            return;
        }
        
        $logFile = "app/trunk/logs/trunk.log";
        $timestamp = date("Y-m-d H:i:s");
        $logEntry = "[$timestamp] $message" . PHP_EOL;
        
        file_put_contents($logFile, $logEntry, FILE_APPEND | LOCK_EX);
    }
    
    private function decryptPayload($payload)
    {
        if (!$this->encryption_key) {
            throw new \Exception("Encryption key not available");
        }
        
        $decrypted = openssl_decrypt(
            base64_decode($payload["data"]),
            "AES-256-CBC",
            $this->encryption_key,
            0,
            base64_decode($payload["iv"])
        );
        
        return TuskLang::decode($decrypted, true);
    }
    
    private function decompressPayload($payload)
    {
        $decompressed = gzuncompress(base64_decode($payload["data"]));
        return TuskLang::decode($decompressed, true);
    }
}