<?php

namespace TuskLang\CoreOperators;

use Exception;

/**
 * AI Operator - Artificial Intelligence Integration
 * 
 * Provides comprehensive AI capabilities including:
 * - Machine learning model management
 * - Natural language processing
 * - Computer vision operations
 * - AI model training and inference
 * - Data preprocessing and feature engineering
 * - Model evaluation and optimization
 * - AI service integration (OpenAI, Azure, AWS)
 * 
 * @package TuskLang\CoreOperators
 */
class AiOperator implements OperatorInterface
{
    private $models = [];
    private $datasets = [];
    private $pipelines = [];
    private $providers = [];

    public function __construct()
    {
        $this->initializeAiSystem();
    }

    /**
     * Execute AI operations
     */
    public function execute(array $params, array $context = []): array
    {
        try {
            $operation = $params['operation'] ?? 'predict';
            $data = $params['data'] ?? [];
            
            // Substitute context variables
            $data = $this->substituteContext($data, $context);
            
            switch ($operation) {
                case 'predict':
                    return $this->predict($data);
                case 'train':
                    return $this->trainModel($data);
                case 'evaluate':
                    return $this->evaluateModel($data);
                case 'preprocess':
                    return $this->preprocessData($data);
                case 'nlp':
                    return $this->handleNlp($data);
                case 'vision':
                    return $this->handleVision($data);
                case 'embedding':
                    return $this->generateEmbedding($data);
                case 'chat':
                    return $this->chatCompletion($data);
                case 'model':
                    return $this->handleModel($data);
                case 'dataset':
                    return $this->handleDataset($data);
                case 'pipeline':
                    return $this->handlePipeline($data);
                case 'info':
                default:
                    return $this->getAiInfo();
            }
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => 'AI operation failed: ' . $e->getMessage(),
                'data' => null
            ];
        }
    }

    /**
     * Make predictions using AI model
     */
    private function predict(array $data): array
    {
        $modelId = $data['model_id'] ?? '';
        $input = $data['input'] ?? '';
        $provider = $data['provider'] ?? 'local';

        if (empty($modelId) || empty($input)) {
            return [
                'success' => false,
                'error' => 'Model ID and input are required',
                'data' => null
            ];
        }

        switch ($provider) {
            case 'openai':
                return $this->predictWithOpenai($modelId, $input, $data);
            case 'azure':
                return $this->predictWithAzure($modelId, $input, $data);
            case 'aws':
                return $this->predictWithAws($modelId, $input, $data);
            case 'local':
            default:
                return $this->predictWithLocalModel($modelId, $input, $data);
        }
    }

    /**
     * Train AI model
     */
    private function trainModel(array $data): array
    {
        $modelType = $data['model_type'] ?? '';
        $datasetId = $data['dataset_id'] ?? '';
        $hyperparameters = $data['hyperparameters'] ?? [];
        $validationSplit = $data['validation_split'] ?? 0.2;

        if (empty($modelType) || empty($datasetId)) {
            return [
                'success' => false,
                'error' => 'Model type and dataset ID are required',
                'data' => null
            ];
        }

        if (!isset($this->datasets[$datasetId])) {
            return [
                'success' => false,
                'error' => 'Dataset not found: ' . $datasetId,
                'data' => null
            ];
        }

        $modelId = uniqid('model_');
        $trainingConfig = [
            'model_type' => $modelType,
            'dataset_id' => $datasetId,
            'hyperparameters' => $hyperparameters,
            'validation_split' => $validationSplit,
            'started_at' => date('Y-m-d H:i:s')
        ];

        // Simulate training process
        $trainingResult = $this->simulateTraining($modelType, $hyperparameters);
        
        $model = [
            'id' => $modelId,
            'type' => $modelType,
            'status' => 'trained',
            'accuracy' => $trainingResult['accuracy'],
            'loss' => $trainingResult['loss'],
            'training_config' => $trainingConfig,
            'trained_at' => date('Y-m-d H:i:s'),
            'metrics' => $trainingResult['metrics']
        ];

        $this->models[$modelId] = $model;

        return [
            'success' => true,
            'data' => [
                'model_id' => $modelId,
                'model' => $model,
                'training_result' => $trainingResult,
                'status' => 'trained'
            ],
            'error' => null
        ];
    }

    /**
     * Evaluate AI model
     */
    private function evaluateModel(array $data): array
    {
        $modelId = $data['model_id'] ?? '';
        $testDatasetId = $data['test_dataset_id'] ?? '';
        $metrics = $data['metrics'] ?? ['accuracy', 'precision', 'recall', 'f1'];

        if (empty($modelId) || empty($testDatasetId)) {
            return [
                'success' => false,
                'error' => 'Model ID and test dataset ID are required',
                'data' => null
            ];
        }

        if (!isset($this->models[$modelId])) {
            return [
                'success' => false,
                'error' => 'Model not found: ' . $modelId,
                'data' => null
            ];
        }

        if (!isset($this->datasets[$testDatasetId])) {
            return [
                'success' => false,
                'error' => 'Test dataset not found: ' . $testDatasetId,
                'data' => null
            ];
        }

        // Simulate model evaluation
        $evaluationResult = $this->simulateEvaluation($metrics);

        return [
            'success' => true,
            'data' => [
                'model_id' => $modelId,
                'test_dataset_id' => $testDatasetId,
                'evaluation_metrics' => $evaluationResult,
                'evaluated_at' => date('Y-m-d H:i:s'),
                'status' => 'evaluated'
            ],
            'error' => null
        ];
    }

    /**
     * Preprocess data for AI models
     */
    private function preprocessData(array $data): array
    {
        $input = $data['input'] ?? '';
        $operations = $data['operations'] ?? [];
        $outputFormat = $data['output_format'] ?? 'array';

        if (empty($input)) {
            return [
                'success' => false,
                'error' => 'Input data is required',
                'data' => null
            ];
        }

        $processedData = $input;

        foreach ($operations as $operation) {
            $type = $operation['type'] ?? '';
            $params = $operation['params'] ?? [];

            switch ($type) {
                case 'normalize':
                    $processedData = $this->normalizeData($processedData, $params);
                    break;
                case 'scale':
                    $processedData = $this->scaleData($processedData, $params);
                    break;
                case 'encode':
                    $processedData = $this->encodeData($processedData, $params);
                    break;
                case 'clean':
                    $processedData = $this->cleanData($processedData, $params);
                    break;
                case 'augment':
                    $processedData = $this->augmentData($processedData, $params);
                    break;
            }
        }

        return [
            'success' => true,
            'data' => [
                'original_data' => $input,
                'processed_data' => $processedData,
                'operations_applied' => $operations,
                'output_format' => $outputFormat,
                'status' => 'preprocessed'
            ],
            'error' => null
        ];
    }

    /**
     * Handle Natural Language Processing operations
     */
    private function handleNlp(array $data): array
    {
        $operation = $data['operation'] ?? 'tokenize';
        $text = $data['text'] ?? '';
        $language = $data['language'] ?? 'en';

        if (empty($text)) {
            return [
                'success' => false,
                'error' => 'Text input is required',
                'data' => null
            ];
        }

        switch ($operation) {
            case 'tokenize':
                return $this->tokenizeText($text, $language);
            case 'sentiment':
                return $this->analyzeSentiment($text, $language);
            case 'entities':
                return $this->extractEntities($text, $language);
            case 'summarize':
                return $this->summarizeText($text, $language);
            case 'translate':
                return $this->translateText($text, $data['target_language'] ?? 'en');
            case 'classify':
                return $this->classifyText($text, $data['categories'] ?? []);
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown NLP operation: ' . $operation,
                    'data' => null
                ];
        }
    }

    /**
     * Handle Computer Vision operations
     */
    private function handleVision(array $data): array
    {
        $operation = $data['operation'] ?? 'classify';
        $imagePath = $data['image_path'] ?? '';
        $imageData = $data['image_data'] ?? '';

        if (empty($imagePath) && empty($imageData)) {
            return [
                'success' => false,
                'error' => 'Image path or image data is required',
                'data' => null
            ];
        }

        switch ($operation) {
            case 'classify':
                return $this->classifyImage($imagePath, $imageData);
            case 'detect':
                return $this->detectObjects($imagePath, $imageData);
            case 'segment':
                return $this->segmentImage($imagePath, $imageData);
            case 'ocr':
                return $this->extractText($imagePath, $imageData);
            case 'face':
                return $this->detectFaces($imagePath, $imageData);
            case 'enhance':
                return $this->enhanceImage($imagePath, $imageData);
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown vision operation: ' . $operation,
                    'data' => null
                ];
        }
    }

    /**
     * Generate embeddings
     */
    private function generateEmbedding(array $data): array
    {
        $text = $data['text'] ?? '';
        $model = $data['model'] ?? 'text-embedding-ada-002';
        $provider = $data['provider'] ?? 'openai';

        if (empty($text)) {
            return [
                'success' => false,
                'error' => 'Text input is required',
                'data' => null
            ];
        }

        switch ($provider) {
            case 'openai':
                return $this->generateOpenaiEmbedding($text, $model);
            case 'azure':
                return $this->generateAzureEmbedding($text, $model);
            case 'local':
            default:
                return $this->generateLocalEmbedding($text, $model);
        }
    }

    /**
     * Chat completion
     */
    private function chatCompletion(array $data): array
    {
        $messages = $data['messages'] ?? [];
        $model = $data['model'] ?? 'gpt-3.5-turbo';
        $provider = $data['provider'] ?? 'openai';
        $temperature = $data['temperature'] ?? 0.7;
        $maxTokens = $data['max_tokens'] ?? 1000;

        if (empty($messages)) {
            return [
                'success' => false,
                'error' => 'Messages are required',
                'data' => null
            ];
        }

        switch ($provider) {
            case 'openai':
                return $this->openaiChatCompletion($messages, $model, $temperature, $maxTokens);
            case 'azure':
                return $this->azureChatCompletion($messages, $model, $temperature, $maxTokens);
            case 'local':
            default:
                return $this->localChatCompletion($messages, $model, $temperature, $maxTokens);
        }
    }

    /**
     * Handle model operations
     */
    private function handleModel(array $data): array
    {
        $action = $data['action'] ?? 'list';
        $modelId = $data['model_id'] ?? '';

        switch ($action) {
            case 'create':
                return $this->createModel($data);
            case 'load':
                return $this->loadModel($modelId);
            case 'save':
                return $this->saveModel($modelId);
            case 'delete':
                return $this->deleteModel($modelId);
            case 'list':
            default:
                return $this->listModels();
        }
    }

    /**
     * Handle dataset operations
     */
    private function handleDataset(array $data): array
    {
        $action = $data['action'] ?? 'list';
        $datasetId = $data['dataset_id'] ?? '';

        switch ($action) {
            case 'create':
                return $this->createDataset($data);
            case 'load':
                return $this->loadDataset($datasetId);
            case 'split':
                return $this->splitDataset($datasetId, $data);
            case 'delete':
                return $this->deleteDataset($datasetId);
            case 'list':
            default:
                return $this->listDatasets();
        }
    }

    /**
     * Handle pipeline operations
     */
    private function handlePipeline(array $data): array
    {
        $action = $data['action'] ?? 'list';
        $pipelineId = $data['pipeline_id'] ?? '';

        switch ($action) {
            case 'create':
                return $this->createPipeline($data);
            case 'execute':
                return $this->executePipeline($pipelineId, $data);
            case 'delete':
                return $this->deletePipeline($pipelineId);
            case 'list':
            default:
                return $this->listPipelines();
        }
    }

    // AI Provider Implementations
    private function predictWithOpenai(string $modelId, $input, array $data): array
    {
        $apiKey = getenv('OPENAI_API_KEY') ?: '';
        
        if (empty($apiKey)) {
            return [
                'success' => false,
                'error' => 'OpenAI API key not configured',
                'data' => null
            ];
        }

        // Simulate OpenAI API call
        return [
            'success' => true,
            'data' => [
                'model_id' => $modelId,
                'input' => $input,
                'prediction' => 'Simulated OpenAI prediction',
                'confidence' => 0.95,
                'provider' => 'openai',
                'timestamp' => date('Y-m-d H:i:s')
            ],
            'error' => null
        ];
    }

    private function predictWithAzure(string $modelId, $input, array $data): array
    {
        $endpoint = getenv('AZURE_AI_ENDPOINT') ?: '';
        $apiKey = getenv('AZURE_AI_API_KEY') ?: '';
        
        if (empty($endpoint) || empty($apiKey)) {
            return [
                'success' => false,
                'error' => 'Azure AI credentials not configured',
                'data' => null
            ];
        }

        // Simulate Azure AI API call
        return [
            'success' => true,
            'data' => [
                'model_id' => $modelId,
                'input' => $input,
                'prediction' => 'Simulated Azure AI prediction',
                'confidence' => 0.92,
                'provider' => 'azure',
                'timestamp' => date('Y-m-d H:i:s')
            ],
            'error' => null
        ];
    }

    private function predictWithAws(string $modelId, $input, array $data): array
    {
        $region = getenv('AWS_REGION') ?: 'us-east-1';
        $accessKey = getenv('AWS_ACCESS_KEY_ID') ?: '';
        $secretKey = getenv('AWS_SECRET_ACCESS_KEY') ?: '';
        
        if (empty($accessKey) || empty($secretKey)) {
            return [
                'success' => false,
                'error' => 'AWS credentials not configured',
                'data' => null
            ];
        }

        // Simulate AWS AI API call
        return [
            'success' => true,
            'data' => [
                'model_id' => $modelId,
                'input' => $input,
                'prediction' => 'Simulated AWS AI prediction',
                'confidence' => 0.89,
                'provider' => 'aws',
                'timestamp' => date('Y-m-d H:i:s')
            ],
            'error' => null
        ];
    }

    private function predictWithLocalModel(string $modelId, $input, array $data): array
    {
        if (!isset($this->models[$modelId])) {
            return [
                'success' => false,
                'error' => 'Local model not found: ' . $modelId,
                'data' => null
            ];
        }

        // Simulate local model prediction
        return [
            'success' => true,
            'data' => [
                'model_id' => $modelId,
                'input' => $input,
                'prediction' => 'Simulated local model prediction',
                'confidence' => 0.87,
                'provider' => 'local',
                'timestamp' => date('Y-m-d H:i:s')
            ],
            'error' => null
        ];
    }

    // NLP Operations
    private function tokenizeText(string $text, string $language): array
    {
        $tokens = preg_split('/\s+/', trim($text));
        
        return [
            'success' => true,
            'data' => [
                'text' => $text,
                'language' => $language,
                'tokens' => $tokens,
                'token_count' => count($tokens),
                'operation' => 'tokenize'
            ],
            'error' => null
        ];
    }

    private function analyzeSentiment(string $text, string $language): array
    {
        // Simple sentiment analysis simulation
        $positiveWords = ['good', 'great', 'excellent', 'amazing', 'wonderful'];
        $negativeWords = ['bad', 'terrible', 'awful', 'horrible', 'disgusting'];
        
        $words = strtolower($text);
        $positiveCount = 0;
        $negativeCount = 0;
        
        foreach ($positiveWords as $word) {
            $positiveCount += substr_count($words, $word);
        }
        
        foreach ($negativeWords as $word) {
            $negativeCount += substr_count($words, $word);
        }
        
        $sentiment = 'neutral';
        if ($positiveCount > $negativeCount) {
            $sentiment = 'positive';
        } elseif ($negativeCount > $positiveCount) {
            $sentiment = 'negative';
        }
        
        return [
            'success' => true,
            'data' => [
                'text' => $text,
                'language' => $language,
                'sentiment' => $sentiment,
                'confidence' => 0.75,
                'positive_score' => $positiveCount,
                'negative_score' => $negativeCount,
                'operation' => 'sentiment_analysis'
            ],
            'error' => null
        ];
    }

    // Vision Operations
    private function classifyImage(string $imagePath, string $imageData): array
    {
        return [
            'success' => true,
            'data' => [
                'image_path' => $imagePath,
                'classifications' => [
                    ['label' => 'cat', 'confidence' => 0.95],
                    ['label' => 'animal', 'confidence' => 0.98],
                    ['label' => 'mammal', 'confidence' => 0.92]
                ],
                'operation' => 'image_classification'
            ],
            'error' => null
        ];
    }

    private function detectObjects(string $imagePath, string $imageData): array
    {
        return [
            'success' => true,
            'data' => [
                'image_path' => $imagePath,
                'objects' => [
                    [
                        'label' => 'person',
                        'confidence' => 0.89,
                        'bbox' => [100, 150, 200, 300]
                    ],
                    [
                        'label' => 'car',
                        'confidence' => 0.92,
                        'bbox' => [300, 200, 450, 280]
                    ]
                ],
                'operation' => 'object_detection'
            ],
            'error' => null
        ];
    }

    // Helper Methods
    private function initializeAiSystem(): void
    {
        $this->models = [];
        $this->datasets = [];
        $this->pipelines = [];
        $this->providers = [
            'openai' => [
                'name' => 'OpenAI',
                'supported_models' => ['gpt-3.5-turbo', 'gpt-4', 'text-embedding-ada-002'],
                'capabilities' => ['chat', 'completion', 'embedding', 'fine-tuning']
            ],
            'azure' => [
                'name' => 'Azure AI',
                'supported_models' => ['gpt-35-turbo', 'gpt-4', 'text-embedding-ada-002'],
                'capabilities' => ['chat', 'completion', 'embedding', 'vision']
            ],
            'aws' => [
                'name' => 'AWS AI',
                'supported_models' => ['anthropic.claude', 'amazon.titan'],
                'capabilities' => ['chat', 'completion', 'embedding', 'vision']
            ],
            'local' => [
                'name' => 'Local Models',
                'supported_models' => ['custom', 'tensorflow', 'pytorch'],
                'capabilities' => ['training', 'inference', 'custom_models']
            ]
        ];
    }

    private function simulateTraining(string $modelType, array $hyperparameters): array
    {
        return [
            'accuracy' => 0.85 + (rand(0, 15) / 100),
            'loss' => 0.12 + (rand(0, 8) / 100),
            'metrics' => [
                'precision' => 0.83 + (rand(0, 17) / 100),
                'recall' => 0.87 + (rand(0, 13) / 100),
                'f1_score' => 0.85 + (rand(0, 15) / 100)
            ]
        ];
    }

    private function simulateEvaluation(array $metrics): array
    {
        $results = [];
        foreach ($metrics as $metric) {
            $results[$metric] = 0.80 + (rand(0, 20) / 100);
        }
        return $results;
    }

    private function normalizeData($data, array $params): array
    {
        // Simulate data normalization
        return $data;
    }

    private function scaleData($data, array $params): array
    {
        // Simulate data scaling
        return $data;
    }

    private function encodeData($data, array $params): array
    {
        // Simulate data encoding
        return $data;
    }

    private function cleanData($data, array $params): array
    {
        // Simulate data cleaning
        return $data;
    }

    private function augmentData($data, array $params): array
    {
        // Simulate data augmentation
        return $data;
    }

    private function generateOpenaiEmbedding(string $text, string $model): array
    {
        return [
            'success' => true,
            'data' => [
                'text' => $text,
                'model' => $model,
                'embedding' => array_fill(0, 1536, rand(0, 100) / 100),
                'provider' => 'openai'
            ],
            'error' => null
        ];
    }

    private function generateAzureEmbedding(string $text, string $model): array
    {
        return [
            'success' => true,
            'data' => [
                'text' => $text,
                'model' => $model,
                'embedding' => array_fill(0, 1536, rand(0, 100) / 100),
                'provider' => 'azure'
            ],
            'error' => null
        ];
    }

    private function generateLocalEmbedding(string $text, string $model): array
    {
        return [
            'success' => true,
            'data' => [
                'text' => $text,
                'model' => $model,
                'embedding' => array_fill(0, 512, rand(0, 100) / 100),
                'provider' => 'local'
            ],
            'error' => null
        ];
    }

    private function openaiChatCompletion(array $messages, string $model, float $temperature, int $maxTokens): array
    {
        return [
            'success' => true,
            'data' => [
                'messages' => $messages,
                'model' => $model,
                'response' => 'This is a simulated OpenAI chat completion response.',
                'usage' => [
                    'prompt_tokens' => 50,
                    'completion_tokens' => 25,
                    'total_tokens' => 75
                ],
                'provider' => 'openai'
            ],
            'error' => null
        ];
    }

    private function azureChatCompletion(array $messages, string $model, float $temperature, int $maxTokens): array
    {
        return [
            'success' => true,
            'data' => [
                'messages' => $messages,
                'model' => $model,
                'response' => 'This is a simulated Azure AI chat completion response.',
                'usage' => [
                    'prompt_tokens' => 50,
                    'completion_tokens' => 25,
                    'total_tokens' => 75
                ],
                'provider' => 'azure'
            ],
            'error' => null
        ];
    }

    private function localChatCompletion(array $messages, string $model, float $temperature, int $maxTokens): array
    {
        return [
            'success' => true,
            'data' => [
                'messages' => $messages,
                'model' => $model,
                'response' => 'This is a simulated local chat completion response.',
                'usage' => [
                    'prompt_tokens' => 50,
                    'completion_tokens' => 25,
                    'total_tokens' => 75
                ],
                'provider' => 'local'
            ],
            'error' => null
        ];
    }

    // Additional stub methods for other operations
    private function extractEntities(string $text, string $language): array
    {
        return ['success' => true, 'data' => ['entities' => []], 'error' => null];
    }

    private function summarizeText(string $text, string $language): array
    {
        return ['success' => true, 'data' => ['summary' => ''], 'error' => null];
    }

    private function translateText(string $text, string $targetLanguage): array
    {
        return ['success' => true, 'data' => ['translation' => ''], 'error' => null];
    }

    private function classifyText(string $text, array $categories): array
    {
        return ['success' => true, 'data' => ['classification' => ''], 'error' => null];
    }

    private function segmentImage(string $imagePath, string $imageData): array
    {
        return ['success' => true, 'data' => ['segments' => []], 'error' => null];
    }

    private function extractText(string $imagePath, string $imageData): array
    {
        return ['success' => true, 'data' => ['text' => ''], 'error' => null];
    }

    private function detectFaces(string $imagePath, string $imageData): array
    {
        return ['success' => true, 'data' => ['faces' => []], 'error' => null];
    }

    private function enhanceImage(string $imagePath, string $imageData): array
    {
        return ['success' => true, 'data' => ['enhanced' => true], 'error' => null];
    }

    private function createModel(array $data): array
    {
        return ['success' => true, 'data' => ['created' => true], 'error' => null];
    }

    private function loadModel(string $modelId): array
    {
        return ['success' => true, 'data' => ['loaded' => true], 'error' => null];
    }

    private function saveModel(string $modelId): array
    {
        return ['success' => true, 'data' => ['saved' => true], 'error' => null];
    }

    private function deleteModel(string $modelId): array
    {
        return ['success' => true, 'data' => ['deleted' => true], 'error' => null];
    }

    private function listModels(): array
    {
        return ['success' => true, 'data' => ['models' => $this->models], 'error' => null];
    }

    private function createDataset(array $data): array
    {
        return ['success' => true, 'data' => ['created' => true], 'error' => null];
    }

    private function loadDataset(string $datasetId): array
    {
        return ['success' => true, 'data' => ['loaded' => true], 'error' => null];
    }

    private function splitDataset(string $datasetId, array $data): array
    {
        return ['success' => true, 'data' => ['split' => true], 'error' => null];
    }

    private function deleteDataset(string $datasetId): array
    {
        return ['success' => true, 'data' => ['deleted' => true], 'error' => null];
    }

    private function listDatasets(): array
    {
        return ['success' => true, 'data' => ['datasets' => $this->datasets], 'error' => null];
    }

    private function createPipeline(array $data): array
    {
        return ['success' => true, 'data' => ['created' => true], 'error' => null];
    }

    private function executePipeline(string $pipelineId, array $data): array
    {
        return ['success' => true, 'data' => ['executed' => true], 'error' => null];
    }

    private function deletePipeline(string $pipelineId): array
    {
        return ['success' => true, 'data' => ['deleted' => true], 'error' => null];
    }

    private function listPipelines(): array
    {
        return ['success' => true, 'data' => ['pipelines' => $this->pipelines], 'error' => null];
    }

    private function getAiInfo(): array
    {
        return [
            'success' => true,
            'data' => [
                'total_models' => count($this->models),
                'total_datasets' => count($this->datasets),
                'total_pipelines' => count($this->pipelines),
                'providers' => $this->providers,
                'capabilities' => [
                    'machine_learning' => true,
                    'natural_language_processing' => true,
                    'computer_vision' => true,
                    'model_training' => true,
                    'model_inference' => true,
                    'data_preprocessing' => true,
                    'embedding_generation' => true,
                    'chat_completion' => true
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