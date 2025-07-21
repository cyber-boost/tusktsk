package main

import (
	"bytes"
	"context"
	"crypto/tls"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"sync"
	"time"
)

// RequestMethod represents HTTP request methods
type RequestMethod string

const (
	GET    RequestMethod = "GET"
	POST   RequestMethod = "POST"
	PUT    RequestMethod = "PUT"
	DELETE RequestMethod = "DELETE"
	PATCH  RequestMethod = "PATCH"
)

// NetworkClient provides HTTP/HTTPS client capabilities
type NetworkClient struct {
	client       *http.Client
	baseURL      string
	headers      map[string]string
	timeout      time.Duration
	maxRetries   int
	retryDelay   time.Duration
	connectionPool *ConnectionPool
	mu           sync.RWMutex
}

// ConnectionPool manages HTTP connections
type ConnectionPool struct {
	maxIdleConns        int
	maxIdleConnsPerHost int
	idleConnTimeout     time.Duration
	mu                  sync.RWMutex
	stats               *PoolStats
}

// PoolStats tracks connection pool statistics
type PoolStats struct {
	ActiveConnections   int           `json:"active_connections"`
	IdleConnections     int           `json:"idle_connections"`
	TotalRequests       int64         `json:"total_requests"`
	AverageResponseTime time.Duration `json:"average_response_time"`
	LastRequestTime     time.Time     `json:"last_request_time"`
}

// Request represents an HTTP request
type Request struct {
	Method  RequestMethod              `json:"method"`
	URL     string                     `json:"url"`
	Headers map[string]string          `json:"headers"`
	Body    interface{}                `json:"body"`
	Timeout time.Duration              `json:"timeout"`
	Context context.Context            `json:"-"`
}

// Response represents an HTTP response
type Response struct {
	StatusCode int                    `json:"status_code"`
	Headers    map[string]string      `json:"headers"`
	Body       []byte                 `json:"body"`
	Duration   time.Duration          `json:"duration"`
	Error      string                 `json:"error,omitempty"`
	Metadata   map[string]interface{} `json:"metadata"`
}

// NewNetworkClient creates a new network client
func NewNetworkClient(baseURL string, timeout time.Duration) *NetworkClient {
	transport := &http.Transport{
		TLSClientConfig: &tls.Config{
			InsecureSkipVerify: false,
		},
		MaxIdleConns:        100,
		MaxIdleConnsPerHost: 10,
		IdleConnTimeout:     90 * time.Second,
	}

	client := &http.Client{
		Transport: transport,
		Timeout:   timeout,
	}

	return &NetworkClient{
		client:     client,
		baseURL:    baseURL,
		headers:    make(map[string]string),
		timeout:    timeout,
		maxRetries: 3,
		retryDelay: 1 * time.Second,
		connectionPool: &ConnectionPool{
			maxIdleConns:        100,
			maxIdleConnsPerHost: 10,
			idleConnTimeout:     90 * time.Second,
			stats: &PoolStats{
				LastRequestTime: time.Now(),
			},
		},
	}
}

// SetHeader sets a default header for all requests
func (nc *NetworkClient) SetHeader(key, value string) {
	nc.mu.Lock()
	defer nc.mu.Unlock()
	nc.headers[key] = value
}

// SetHeaders sets multiple headers at once
func (nc *NetworkClient) SetHeaders(headers map[string]string) {
	nc.mu.Lock()
	defer nc.mu.Unlock()
	for key, value := range headers {
		nc.headers[key] = value
	}
}

// SetTimeout sets the default timeout for requests
func (nc *NetworkClient) SetTimeout(timeout time.Duration) {
	nc.mu.Lock()
	defer nc.mu.Unlock()
	nc.timeout = timeout
	nc.client.Timeout = timeout
}

// SetRetryConfig sets retry configuration
func (nc *NetworkClient) SetRetryConfig(maxRetries int, retryDelay time.Duration) {
	nc.mu.Lock()
	defer nc.mu.Unlock()
	nc.maxRetries = maxRetries
	nc.retryDelay = retryDelay
}

// Get performs a GET request
func (nc *NetworkClient) Get(path string, headers map[string]string) (*Response, error) {
	return nc.Request(&Request{
		Method:  GET,
		URL:     path,
		Headers: headers,
	})
}

// Post performs a POST request
func (nc *NetworkClient) Post(path string, body interface{}, headers map[string]string) (*Response, error) {
	return nc.Request(&Request{
		Method:  POST,
		URL:     path,
		Body:    body,
		Headers: headers,
	})
}

// Put performs a PUT request
func (nc *NetworkClient) Put(path string, body interface{}, headers map[string]string) (*Response, error) {
	return nc.Request(&Request{
		Method:  PUT,
		URL:     path,
		Body:    body,
		Headers: headers,
	})
}

// Delete performs a DELETE request
func (nc *NetworkClient) Delete(path string, headers map[string]string) (*Response, error) {
	return nc.Request(&Request{
		Method:  DELETE,
		URL:     path,
		Headers: headers,
	})
}

// Request performs an HTTP request with retry logic
func (nc *NetworkClient) Request(req *Request) (*Response, error) {
	if req.Context == nil {
		req.Context = context.Background()
	}

	if req.Timeout == 0 {
		req.Timeout = nc.timeout
	}

	var lastErr error
	for attempt := 0; attempt <= nc.maxRetries; attempt++ {
		response, err := nc.doRequest(req)
		if err == nil {
			return response, nil
		}

		lastErr = err
		if attempt < nc.maxRetries {
			time.Sleep(nc.retryDelay * time.Duration(attempt+1))
		}
	}

	return nil, fmt.Errorf("request failed after %d attempts: %v", nc.maxRetries+1, lastErr)
}

// doRequest performs a single HTTP request
func (nc *NetworkClient) doRequest(req *Request) (*Response, error) {
	start := time.Now()

	// Build full URL
	fullURL := req.URL
	if !isAbsoluteURL(req.URL) {
		fullURL = nc.baseURL + req.URL
	}

	// Prepare request body
	var body io.Reader
	if req.Body != nil {
		switch v := req.Body.(type) {
		case []byte:
			body = bytes.NewReader(v)
		case string:
			body = bytes.NewReader([]byte(v))
		default:
			jsonData, err := json.Marshal(v)
			if err != nil {
				return nil, fmt.Errorf("failed to marshal request body: %v", err)
			}
			body = bytes.NewReader(jsonData)
		}
	}

	// Create HTTP request
	httpReq, err := http.NewRequestWithContext(req.Context, string(req.Method), fullURL, body)
	if err != nil {
		return nil, fmt.Errorf("failed to create request: %v", err)
	}

	// Set headers
	nc.mu.RLock()
	for key, value := range nc.headers {
		httpReq.Header.Set(key, value)
	}
	nc.mu.RUnlock()

	for key, value := range req.Headers {
		httpReq.Header.Set(key, value)
	}

	// Set content type for JSON body
	if req.Body != nil && httpReq.Header.Get("Content-Type") == "" {
		httpReq.Header.Set("Content-Type", "application/json")
	}

	// Create client with timeout
	client := &http.Client{
		Transport: nc.client.Transport,
		Timeout:   req.Timeout,
	}

	// Execute request
	resp, err := client.Do(httpReq)
	if err != nil {
		return nil, fmt.Errorf("request failed: %v", err)
	}
	defer resp.Body.Close()

	// Read response body
	bodyBytes, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, fmt.Errorf("failed to read response body: %v", err)
	}

	duration := time.Since(start)

	// Update pool stats
	nc.connectionPool.mu.Lock()
	nc.connectionPool.stats.TotalRequests++
	nc.connectionPool.stats.LastRequestTime = time.Now()
	nc.connectionPool.mu.Unlock()

	// Build response headers map
	responseHeaders := make(map[string]string)
	for key, values := range resp.Header {
		if len(values) > 0 {
			responseHeaders[key] = values[0]
		}
	}

	response := &Response{
		StatusCode: resp.StatusCode,
		Headers:    responseHeaders,
		Body:       bodyBytes,
		Duration:   duration,
		Metadata: map[string]interface{}{
			"url":      fullURL,
			"method":   req.Method,
			"attempts": 1,
		},
	}

	// Check for HTTP error status
	if resp.StatusCode >= 400 {
		response.Error = fmt.Sprintf("HTTP %d: %s", resp.StatusCode, resp.Status)
	}

	return response, nil
}

// isAbsoluteURL checks if a URL is absolute
func isAbsoluteURL(urlStr string) bool {
	parsed, err := url.Parse(urlStr)
	return err == nil && parsed.Scheme != "" && parsed.Host != ""
}

// GetPoolStats returns connection pool statistics
func (nc *NetworkClient) GetPoolStats() *PoolStats {
	nc.connectionPool.mu.RLock()
	defer nc.connectionPool.mu.RUnlock()
	
	stats := &PoolStats{
		ActiveConnections:   nc.connectionPool.stats.ActiveConnections,
		IdleConnections:     nc.connectionPool.stats.IdleConnections,
		TotalRequests:       nc.connectionPool.stats.TotalRequests,
		AverageResponseTime: nc.connectionPool.stats.AverageResponseTime,
		LastRequestTime:     nc.connectionPool.stats.LastRequestTime,
	}
	
	return stats
}

// Close closes the network client and cleans up resources
func (nc *NetworkClient) Close() error {
	// Close idle connections
	if transport, ok := nc.client.Transport.(*http.Transport); ok {
		transport.CloseIdleConnections()
	}
	return nil
}

// Example usage and testing
func main() {
	// Create network client
	client := NewNetworkClient("https://httpbin.org", 30*time.Second)
	defer client.Close()

	// Set default headers
	client.SetHeaders(map[string]string{
		"User-Agent": "TuskLang-Go-SDK/1.0",
		"Accept":     "application/json",
	})

	// Test GET request
	fmt.Println("Testing GET request...")
	getResp, err := client.Get("/get", map[string]string{
		"X-Test-Header": "test-value",
	})
	if err != nil {
		fmt.Printf("GET request failed: %v\n", err)
	} else {
		fmt.Printf("GET Status: %d\n", getResp.StatusCode)
		fmt.Printf("GET Duration: %v\n", getResp.Duration)
		if len(getResp.Body) > 0 {
			fmt.Printf("GET Response: %s\n", string(getResp.Body[:100])+"...")
		}
	}

	// Test POST request
	fmt.Println("\nTesting POST request...")
	postData := map[string]interface{}{
		"name":    "TuskLang",
		"version": "1.0.0",
		"features": []string{
			"security",
			"networking",
			"data_processing",
		},
	}

	postResp, err := client.Post("/post", postData, map[string]string{
		"Content-Type": "application/json",
	})
	if err != nil {
		fmt.Printf("POST request failed: %v\n", err)
	} else {
		fmt.Printf("POST Status: %d\n", postResp.StatusCode)
		fmt.Printf("POST Duration: %v\n", postResp.Duration)
		if len(postResp.Body) > 0 {
			fmt.Printf("POST Response: %s\n", string(postResp.Body[:100])+"...")
		}
	}

	// Test PUT request
	fmt.Println("\nTesting PUT request...")
	putData := map[string]string{
		"status": "updated",
	}

	putResp, err := client.Put("/put", putData, nil)
	if err != nil {
		fmt.Printf("PUT request failed: %v\n", err)
	} else {
		fmt.Printf("PUT Status: %d\n", putResp.StatusCode)
		fmt.Printf("PUT Duration: %v\n", putResp.Duration)
	}

	// Test DELETE request
	fmt.Println("\nTesting DELETE request...")
	deleteResp, err := client.Delete("/delete", nil)
	if err != nil {
		fmt.Printf("DELETE request failed: %v\n", err)
	} else {
		fmt.Printf("DELETE Status: %d\n", deleteResp.StatusCode)
		fmt.Printf("DELETE Duration: %v\n", deleteResp.Duration)
	}

	// Display pool statistics
	stats := client.GetPoolStats()
	fmt.Printf("\nConnection Pool Stats:\n")
	fmt.Printf("  Total Requests: %d\n", stats.TotalRequests)
	fmt.Printf("  Last Request Time: %v\n", stats.LastRequestTime)
	fmt.Printf("  Average Response Time: %v\n", stats.AverageResponseTime)
} 