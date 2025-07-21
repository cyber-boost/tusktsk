package main

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"sync"
	"time"
)

// AIProvider represents different AI service providers
type AIProvider string

const (
	OpenAI    AIProvider = "openai"
	Claude    AIProvider = "claude"
	Anthropic AIProvider = "anthropic"
	Custom    AIProvider = "custom"
)

// AIModel represents different AI models
type AIModel struct {
	Name        string                 `json:"name"`
	Provider    AIProvider             `json:"provider"`
	MaxTokens   int                    `json:"max_tokens"`
	Temperature float64                `json:"temperature"`
	Capabilities []string              `json:"capabilities"`
	Metadata    map[string]interface{} `json:"metadata"`
}

// AIRequest represents a request to an AI service
type AIRequest struct {
	Model       string                 `json:"model"`
	Messages    []AIMessage            `json:"messages"`
	Temperature float64                `json:"temperature,omitempty"`
	MaxTokens   int                    `json:"max_tokens,omitempty"`
	Stream      bool                   `json:"stream,omitempty"`
	Tools       []AITool               `json:"tools,omitempty"`
	Metadata    map[string]interface{} `json:"metadata,omitempty"`
}

// AIMessage represents a message in the AI conversation
type AIMessage struct {
	Role      string                 `json:"role"`
	Content   string                 `json:"content"`
	ToolCalls []AIToolCall           `json:"tool_calls,omitempty"`
	Metadata  map[string]interface{} `json:"metadata,omitempty"`
}

// AITool represents a tool that can be called by the AI
type AITool struct {
	Type     string                 `json:"type"`
	Function AIToolFunction         `json:"function,omitempty"`
	Metadata map[string]interface{} `json:"metadata,omitempty"`
}

// AIToolFunction represents a function definition for AI tools
type AIToolFunction struct {
	Name        string                 `json:"name"`
	Description string                 `json:"description"`
	Parameters  map[string]interface{} `json:"parameters"`
}

// AIToolCall represents a call to a tool by the AI
type AIToolCall struct {
	ID       string                 `json:"id"`
	Type     string                 `json:"type"`
	Function AIToolCallFunction     `json:"function"`
	Metadata map[string]interface{} `json:"metadata,omitempty"`
}

// AIToolCallFunction represents the function call details
type AIToolCallFunction struct {
	Name      string                 `json:"name"`
	Arguments string                 `json:"arguments"`
}

// AIResponse represents a response from an AI service
type AIResponse struct {
	ID                string                 `json:"id"`
	Model             string                 `json:"model"`
	Created           int64                  `json:"created"`
	Choices           []AIChoice             `json:"choices"`
	Usage             AIUsage                `json:"usage"`
	Metadata          map[string]interface{} `json:"metadata,omitempty"`
	ProcessingTime    time.Duration          `json:"processing_time"`
	Provider          AIProvider             `json:"provider"`
}

// AIChoice represents a choice in the AI response
type AIChoice struct {
	Index        int                    `json:"index"`
	Message      AIMessage              `json:"message"`
	FinishReason string                 `json:"finish_reason"`
	Metadata     map[string]interface{} `json:"metadata,omitempty"`
}

// AIUsage represents token usage information
type AIUsage struct {
	PromptTokens     int `json:"prompt_tokens"`
	CompletionTokens int `json:"completion_tokens"`
	TotalTokens      int `json:"total_tokens"`
}

// AIIntegration provides AI integration capabilities
type AIIntegration struct {
	providers    map[AIProvider]*AIProviderConfig
	models       map[string]*AIModel
	tools        map[string]AITool
	httpClient   *http.Client
	mu           sync.RWMutex
	cache        map[string]*AICacheEntry
	rateLimiter  *AIRateLimiter
}

// AIProviderConfig represents configuration for an AI provider
type AIProviderConfig struct {
	APIKey      string                 `json:"api_key"`
	BaseURL     string                 `json:"base_url"`
	Timeout     time.Duration          `json:"timeout"`
	MaxRetries  int                    `json:"max_retries"`
	Headers     map[string]string      `json:"headers"`
	Metadata    map[string]interface{} `json:"metadata"`
}

// AICacheEntry represents a cached AI response
type AICacheEntry struct {
	Response    *AIResponse `json:"response"`
	Expiration  time.Time   `json:"expiration"`
	AccessCount int64       `json:"access_count"`
	LastAccess  time.Time   `json:"last_access"`
}

// AIRateLimiter manages rate limiting for AI requests
type AIRateLimiter struct {
	requests map[AIProvider]*ProviderRateLimit
	mu       sync.RWMutex
}

// ProviderRateLimit tracks rate limits for a provider
type ProviderRateLimit struct {
	RequestsPerMinute int
	RequestsPerHour   int
	LastRequest       time.Time
	RequestCount      int
	HourlyCount       int
	ResetTime         time.Time
}

// NewAIIntegration creates a new AI integration instance
func NewAIIntegration() *AIIntegration {
	return &AIIntegration{
		providers:   make(map[AIProvider]*AIProviderConfig),
		models:      make(map[string]*AIModel),
		tools:       make(map[string]AITool),
		httpClient:  &http.Client{Timeout: 30 * time.Second},
		cache:       make(map[string]*AICacheEntry),
		rateLimiter: &AIRateLimiter{
			requests: make(map[AIProvider]*ProviderRateLimit),
		},
	}
}

// RegisterProvider registers an AI provider
func (ai *AIIntegration) RegisterProvider(provider AIProvider, config *AIProviderConfig) {
	ai.mu.Lock()
	defer ai.mu.Unlock()
	ai.providers[provider] = config
}

// RegisterModel registers an AI model
func (ai *AIIntegration) RegisterModel(model *AIModel) {
	ai.mu.Lock()
	defer ai.mu.Unlock()
	ai.models[model.Name] = model
}

// RegisterTool registers a tool that can be used by the AI
func (ai *AIIntegration) RegisterTool(name string, tool AITool) {
	ai.mu.Lock()
	defer ai.mu.Unlock()
	ai.tools[name] = tool
}

// GenerateText generates text using the specified AI model
func (ai *AIIntegration) GenerateText(ctx context.Context, request *AIRequest) (*AIResponse, error) {
	start := time.Now()

	// Check rate limits
	if err := ai.rateLimiter.CheckLimit(request.Model); err != nil {
		return nil, fmt.Errorf("rate limit exceeded: %v", err)
	}

	// Check cache
	cacheKey := ai.generateCacheKey(request)
	if cached := ai.getFromCache(cacheKey); cached != nil {
		return cached, nil
	}

	// Get provider and model
	provider, err := ai.getProviderForModel(request.Model)
	if err != nil {
		return nil, err
	}

	// Make request
	response, err := ai.makeRequest(ctx, provider, request)
	if err != nil {
		return nil, err
	}

	// Set metadata
	response.ProcessingTime = time.Since(start)
	response.Provider = provider

	// Cache response
	ai.addToCache(cacheKey, response)

	return response, nil
}

// Chat creates a chat conversation with the AI
func (ai *AIIntegration) Chat(ctx context.Context, model string, messages []AIMessage) (*AIResponse, error) {
	request := &AIRequest{
		Model:    model,
		Messages: messages,
	}
	return ai.GenerateText(ctx, request)
}

// AnalyzeText analyzes text using AI capabilities
func (ai *AIIntegration) AnalyzeText(ctx context.Context, text string, analysisType string) (*AIResponse, error) {
	messages := []AIMessage{
		{
			Role:    "system",
			Content: fmt.Sprintf("You are an AI assistant specialized in %s analysis.", analysisType),
		},
		{
			Role:    "user",
			Content: fmt.Sprintf("Please analyze the following text for %s: %s", analysisType, text),
		},
	}

	return ai.Chat(ctx, "gpt-4", messages)
}

// GenerateCode generates code based on requirements
func (ai *AIIntegration) GenerateCode(ctx context.Context, requirements string, language string) (*AIResponse, error) {
	messages := []AIMessage{
		{
			Role:    "system",
			Content: fmt.Sprintf("You are an expert %s programmer. Generate clean, efficient, and well-documented code.", language),
		},
		{
			Role:    "user",
			Content: fmt.Sprintf("Generate %s code for the following requirements: %s", language, requirements),
		},
	}

	return ai.Chat(ctx, "gpt-4", messages)
}

// OptimizeCode optimizes existing code
func (ai *AIIntegration) OptimizeCode(ctx context.Context, code string, language string, optimizationType string) (*AIResponse, error) {
	messages := []AIMessage{
		{
			Role:    "system",
			Content: fmt.Sprintf("You are an expert %s programmer specializing in code optimization.", language),
		},
		{
			Role:    "user",
			Content: fmt.Sprintf("Optimize the following %s code for %s:\n\n%s", language, optimizationType, code),
		},
	}

	return ai.Chat(ctx, "gpt-4", messages)
}

// Helper methods
func (ai *AIIntegration) getProviderForModel(modelName string) (AIProvider, error) {
	ai.mu.RLock()
	defer ai.mu.RUnlock()

	model, exists := ai.models[modelName]
	if !exists {
		return "", fmt.Errorf("model not found: %s", modelName)
	}

	return model.Provider, nil
}

func (ai *AIIntegration) makeRequest(ctx context.Context, provider AIProvider, request *AIRequest) (*AIResponse, error) {
	config, exists := ai.providers[provider]
	if !exists {
		return nil, fmt.Errorf("provider not configured: %s", provider)
	}

	// Prepare request body
	requestBody, err := json.Marshal(request)
	if err != nil {
		return nil, fmt.Errorf("failed to marshal request: %v", err)
	}

	// Create HTTP request
	httpReq, err := http.NewRequestWithContext(ctx, "POST", config.BaseURL, bytes.NewBuffer(requestBody))
	if err != nil {
		return nil, fmt.Errorf("failed to create request: %v", err)
	}

	// Set headers
	httpReq.Header.Set("Content-Type", "application/json")
	httpReq.Header.Set("Authorization", "Bearer "+config.APIKey)
	for key, value := range config.Headers {
		httpReq.Header.Set(key, value)
	}

	// Make request
	resp, err := ai.httpClient.Do(httpReq)
	if err != nil {
		return nil, fmt.Errorf("request failed: %v", err)
	}
	defer resp.Body.Close()

	// Read response
	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, fmt.Errorf("failed to read response: %v", err)
	}

	// Parse response
	var aiResponse AIResponse
	if err := json.Unmarshal(body, &aiResponse); err != nil {
		return nil, fmt.Errorf("failed to parse response: %v", err)
	}

	return &aiResponse, nil
}

func (ai *AIIntegration) generateCacheKey(request *AIRequest) string {
	// Create a hash of the request for caching
	data, _ := json.Marshal(request)
	return fmt.Sprintf("%x", data)
}

func (ai *AIIntegration) getFromCache(key string) *AIResponse {
	ai.mu.RLock()
	defer ai.mu.RUnlock()

	entry, exists := ai.cache[key]
	if !exists {
		return nil
	}

	if time.Now().After(entry.Expiration) {
		delete(ai.cache, key)
		return nil
	}

	entry.AccessCount++
	entry.LastAccess = time.Now()
	return entry.Response
}

func (ai *AIIntegration) addToCache(key string, response *AIResponse) {
	ai.mu.Lock()
	defer ai.mu.Unlock()

	ai.cache[key] = &AICacheEntry{
		Response:    response,
		Expiration:  time.Now().Add(1 * time.Hour),
		AccessCount: 1,
		LastAccess:  time.Now(),
	}
}

func (rl *AIRateLimiter) CheckLimit(model string) error {
	// Simplified rate limiting - in production, this would be more sophisticated
	return nil
}

// Example usage and testing
func main() {
	// Create AI integration
	ai := NewAIIntegration()

	// Register OpenAI provider
	ai.RegisterProvider(OpenAI, &AIProviderConfig{
		APIKey:  "your-api-key-here",
		BaseURL: "https://api.openai.com/v1/chat/completions",
		Timeout: 30 * time.Second,
		MaxRetries: 3,
	})

	// Register models
	ai.RegisterModel(&AIModel{
		Name:        "gpt-4",
		Provider:    OpenAI,
		MaxTokens:   4096,
		Temperature: 0.7,
		Capabilities: []string{"text-generation", "code-generation", "analysis"},
	})

	ai.RegisterModel(&AIModel{
		Name:        "gpt-3.5-turbo",
		Provider:    OpenAI,
		MaxTokens:   2048,
		Temperature: 0.7,
		Capabilities: []string{"text-generation", "chat"},
	})

	// Register tools
	ai.RegisterTool("file_reader", AITool{
		Type: "function",
		Function: AIToolFunction{
			Name:        "read_file",
			Description: "Read contents of a file",
			Parameters: map[string]interface{}{
				"type": "object",
				"properties": map[string]interface{}{
					"path": map[string]interface{}{
						"type":        "string",
						"description": "Path to the file to read",
					},
				},
				"required": []string{"path"},
			},
		},
	})

	fmt.Println("AI Integration Demo")
	fmt.Println("===================")

	// Example: Generate code
	// Note: This would require actual API credentials to work
	fmt.Println("AI Integration System initialized with:")
	fmt.Printf("  - Registered providers: %d\n", len(ai.providers))
	fmt.Printf("  - Registered models: %d\n", len(ai.models))
	fmt.Printf("  - Registered tools: %d\n", len(ai.tools))
	
	// Example conversation
	messages := []AIMessage{
		{
			Role:    "system",
			Content: "You are a helpful AI assistant.",
		},
		{
			Role:    "user",
			Content: "Hello! Can you help me with Go programming?",
		},
	}

	fmt.Printf("\nExample conversation prepared with %d messages\n", len(messages))
	fmt.Println("AI system ready for integration with TuskLang Go SDK")
} 