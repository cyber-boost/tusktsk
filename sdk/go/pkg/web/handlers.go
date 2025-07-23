package web

import (
	"encoding/json"
	"fmt"
	"net/http"
	"runtime"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/gorilla/websocket"
	"go.opentelemetry.io/otel/attribute"
)

// healthHandler provides health check endpoint
func (f *Framework) healthHandler(c *gin.Context) {
	ctx := c.Request.Context()
	_, span := f.tracer.Start(ctx, "health_check")
	defer span.End()

	health := gin.H{
		"status":    "healthy",
		"timestamp": time.Now().UTC().Format(time.RFC3339),
		"uptime":    time.Since(f.startTime).String(),
		"version":   "1.0.0",
		"service":   "tusktsk-web",
	}

	span.SetAttributes(
		attribute.String("health.status", "healthy"),
		attribute.String("health.timestamp", health["timestamp"].(string)),
	)

	c.JSON(http.StatusOK, health)
}

// statusHandler provides detailed status information
func (f *Framework) statusHandler(c *gin.Context) {
	ctx := c.Request.Context()
	_, span := f.tracer.Start(ctx, "status_check")
	defer span.End()

	var m runtime.MemStats
	runtime.ReadMemStats(&m)

	status := gin.H{
		"status": gin.H{
			"healthy": true,
			"uptime":  time.Since(f.startTime).String(),
		},
		"system": gin.H{
			"goroutines": runtime.NumGoroutine(),
			"memory": gin.H{
				"alloc":     m.Alloc,
				"total_alloc": m.TotalAlloc,
				"sys":        m.Sys,
				"num_gc":     m.NumGC,
			},
			"cpu_count": runtime.NumCPU(),
		},
		"web": gin.H{
			"websocket_clients": len(f.clients),
			"port":             f.config.Port,
			"host":             f.config.Host,
		},
		"timestamp": time.Now().UTC().Format(time.RFC3339),
	}

	span.SetAttributes(
		attribute.Bool("status.healthy", true),
		attribute.Int("system.goroutines", runtime.NumGoroutine()),
		attribute.Int("web.websocket_clients", len(f.clients)),
	)

	c.JSON(http.StatusOK, status)
}

// infoHandler provides API information
func (f *Framework) infoHandler(c *gin.Context) {
	ctx := c.Request.Context()
	_, span := f.tracer.Start(ctx, "info_check")
	defer span.End()

	info := gin.H{
		"name":        "TuskTSK Web API",
		"version":     "1.0.0",
		"description": "High-performance web framework for TuskTSK Go SDK",
		"endpoints": gin.H{
			"health":    "/health",
			"status":    "/api/v1/status",
			"info":      "/api/v1/info",
			"echo":      "/api/v1/echo",
			"websocket": "/ws",
			"graphql":   "/graphql",
			"metrics":   "/metrics",
		},
		"features": []string{
			"REST API",
			"WebSocket",
			"GraphQL",
			"Prometheus Metrics",
			"OpenTelemetry Tracing",
			"Rate Limiting",
			"JWT Authentication",
			"CORS Support",
		},
		"timestamp": time.Now().UTC().Format(time.RFC3339),
	}

	span.SetAttributes(
		attribute.String("info.name", info["name"].(string)),
		attribute.String("info.version", info["version"].(string)),
	)

	c.JSON(http.StatusOK, info)
}

// echoHandler provides echo functionality for testing
func (f *Framework) echoHandler(c *gin.Context) {
	ctx := c.Request.Context()
	_, span := f.tracer.Start(ctx, "echo_request")
	defer span.End()

	var requestBody map[string]interface{}
	if err := c.ShouldBindJSON(&requestBody); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid JSON"})
		return
	}

	echo := gin.H{
		"message": "Echo response",
		"data":    requestBody,
		"headers": c.Request.Header,
		"method":  c.Request.Method,
		"url":     c.Request.URL.String(),
		"timestamp": time.Now().UTC().Format(time.RFC3339),
	}

	span.SetAttributes(
		attribute.String("echo.method", c.Request.Method),
		attribute.String("echo.url", c.Request.URL.String()),
	)

	c.JSON(http.StatusOK, echo)
}

// websocketHandler handles WebSocket connections
func (f *Framework) websocketHandler(c *gin.Context) {
	ctx := c.Request.Context()
	_, span := f.tracer.Start(ctx, "websocket_connection")
	defer span.End()

	conn, err := f.wsUpgrader.Upgrade(c.Writer, c.Request, nil)
	if err != nil {
		span.RecordError(err)
		fmt.Printf("WebSocket upgrade error: %v\n", err)
		return
	}

	// Add client to the map
	f.clients[conn] = true

	// Send welcome message
	welcome := map[string]interface{}{
		"type":    "welcome",
		"message": "Connected to TuskTSK WebSocket",
		"clients": len(f.clients),
		"timestamp": time.Now().UTC().Format(time.RFC3339),
	}

	welcomeBytes, _ := json.Marshal(welcome)
	conn.WriteMessage(websocket.TextMessage, welcomeBytes)

	// Handle messages from this client
	go f.handleWebSocketMessages(conn)

	span.SetAttributes(
		attribute.String("websocket.status", "connected"),
		attribute.Int("websocket.total_clients", len(f.clients)),
	)
}

// handleWebSocketMessages handles incoming WebSocket messages
func (f *Framework) handleWebSocketMessages(conn *websocket.Conn) {
	defer func() {
		conn.Close()
		delete(f.clients, conn)
	}()

	for {
		messageType, message, err := conn.ReadMessage()
		if err != nil {
			if websocket.IsUnexpectedCloseError(err, websocket.CloseGoingAway, websocket.CloseAbnormalClosure) {
				fmt.Printf("WebSocket error: %v\n", err)
			}
			break
		}

		// Echo the message back
		response := map[string]interface{}{
			"type":    "echo",
			"message": string(message),
			"timestamp": time.Now().UTC().Format(time.RFC3339),
		}

		responseBytes, _ := json.Marshal(response)
		conn.WriteMessage(messageType, responseBytes)

		// Broadcast to all clients
		f.Broadcast(message)
	}
}

// graphqlHandler handles GraphQL requests
func (f *Framework) graphqlHandler(c *gin.Context) {
	ctx := c.Request.Context()
	_, span := f.tracer.Start(ctx, "graphql_request")
	defer span.End()

	// For now, return a placeholder response
	// This will be implemented with a proper GraphQL server
	response := gin.H{
		"data": gin.H{
			"message": "GraphQL endpoint - coming soon",
		},
		"errors": []string{},
	}

	span.SetAttributes(
		attribute.String("graphql.status", "placeholder"),
	)

	c.JSON(http.StatusOK, response)
}

// graphqlPlaygroundHandler serves GraphQL playground
func (f *Framework) graphqlPlaygroundHandler(c *gin.Context) {
	ctx := c.Request.Context()
	_, span := f.tracer.Start(ctx, "graphql_playground")
	defer span.End()

	// Return GraphQL playground HTML
	playground := `
<!DOCTYPE html>
<html>
<head>
    <title>TuskTSK GraphQL Playground</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .container { max-width: 800px; margin: 0 auto; }
        .header { background: #f5f5f5; padding: 20px; border-radius: 8px; margin-bottom: 20px; }
        .content { background: #fff; padding: 20px; border: 1px solid #ddd; border-radius: 8px; }
        .endpoint { background: #e8f4fd; padding: 10px; border-radius: 4px; font-family: monospace; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🚀 TuskTSK GraphQL Playground</h1>
            <p>High-performance GraphQL API for TuskTSK Go SDK</p>
        </div>
        <div class="content">
            <h2>GraphQL Endpoint</h2>
            <div class="endpoint">POST /graphql</div>
            <p>GraphQL server implementation coming soon...</p>
        </div>
    </div>
</body>
</html>`

	span.SetAttributes(
		attribute.String("graphql.playground", "served"),
	)

	c.Header("Content-Type", "text/html")
	c.String(http.StatusOK, playground)
}

// metricsHandler provides custom metrics endpoint
func (f *Framework) metricsHandler(c *gin.Context) {
	ctx := c.Request.Context()
	_, span := f.tracer.Start(ctx, "metrics_request")
	defer span.End()

	metrics := gin.H{
		"requests_total": f.metrics.RequestsTotal,
		"requests_duration": f.metrics.RequestsDuration,
		"websocket_connections": len(f.clients),
		"uptime_seconds": time.Since(f.startTime).Seconds(),
		"memory_usage": gin.H{
			"alloc":     f.metrics.MemoryAlloc,
			"total_alloc": f.metrics.MemoryTotalAlloc,
			"sys":        f.metrics.MemorySys,
		},
		"timestamp": time.Now().UTC().Format(time.RFC3339),
	}

	span.SetAttributes(
		attribute.Int("metrics.requests_total", f.metrics.RequestsTotal),
		attribute.Int("metrics.websocket_connections", len(f.clients)),
	)

	c.JSON(http.StatusOK, metrics)
} 