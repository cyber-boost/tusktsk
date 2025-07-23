package web

import (
	"context"
	"fmt"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/gorilla/websocket"
	"github.com/prometheus/client_golang/prometheus"
	"github.com/prometheus/client_golang/prometheus/promhttp"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/trace"
)

// Framework represents the main web framework
type Framework struct {
	engine     *gin.Engine
	server     *http.Server
	wsUpgrader websocket.Upgrader
	metrics    *Metrics
	tracer     trace.Tracer
	config     *Config
	clients    map[*websocket.Conn]bool
	broadcast  chan []byte
	startTime  time.Time
}

// Config holds web framework configuration
type Config struct {
	Port            int           `json:"port"`
	Host            string        `json:"host"`
	ReadTimeout     time.Duration `json:"read_timeout"`
	WriteTimeout    time.Duration `json:"write_timeout"`
	MaxHeaderBytes  int           `json:"max_header_bytes"`
	EnableCORS      bool          `json:"enable_cors"`
	EnableMetrics   bool          `json:"enable_metrics"`
	EnableTracing   bool          `json:"enable_tracing"`
	EnableWebSocket bool          `json:"enable_websocket"`
	StaticPath      string        `json:"static_path"`
	LogLevel        string        `json:"log_level"`
}

// DefaultConfig returns default configuration
func DefaultConfig() *Config {
	return &Config{
		Port:            8080,
		Host:            "0.0.0.0",
		ReadTimeout:     30 * time.Second,
		WriteTimeout:    30 * time.Second,
		MaxHeaderBytes:  1 << 20,
		EnableCORS:      true,
		EnableMetrics:   true,
		EnableTracing:   true,
		EnableWebSocket: true,
		StaticPath:      "./static",
		LogLevel:        "info",
	}
}

// NewFramework creates a new web framework instance
func NewFramework(config *Config) *Framework {
	if config == nil {
		config = DefaultConfig()
	}

	// Set Gin mode
	if config.LogLevel == "debug" {
		gin.SetMode(gin.DebugMode)
	} else {
		gin.SetMode(gin.ReleaseMode)
	}

	engine := gin.New()
	
	// Add middleware
	engine.Use(gin.Logger())
	engine.Use(gin.Recovery())
	engine.Use(tracingMiddleware())
	engine.Use(errorMiddleware())
	engine.Use(securityMiddleware())
	
	if config.EnableCORS {
		engine.Use(corsMiddleware())
	}

	framework := &Framework{
		engine: engine,
		wsUpgrader: websocket.Upgrader{
			CheckOrigin: func(r *http.Request) bool {
				return true // Allow all origins for development
			},
		},
		metrics:   NewMetrics(),
		tracer:    otel.Tracer("tusktsk-web"),
		config:    config,
		clients:   make(map[*websocket.Conn]bool),
		broadcast: make(chan []byte, 100),
		startTime: time.Now(),
	}

	// Setup routes
	framework.setupRoutes()

	return framework
}

// setupRoutes configures all routes
func (f *Framework) setupRoutes() {
	// Health check
	f.engine.GET("/health", f.healthHandler)
	
	// Metrics endpoint
	if f.config.EnableMetrics {
		f.engine.GET("/metrics", gin.WrapH(promhttp.Handler()))
	}

	// API routes
	api := f.engine.Group("/api/v1")
	{
		api.GET("/status", f.statusHandler)
		api.GET("/info", f.infoHandler)
		api.POST("/echo", f.echoHandler)
	}

	// WebSocket endpoint
	if f.config.EnableWebSocket {
		f.engine.GET("/ws", f.websocketHandler)
	}

	// Static file serving
	if f.config.StaticPath != "" {
		f.engine.Static("/static", f.config.StaticPath)
	}

	// GraphQL endpoint (placeholder for now)
	f.engine.POST("/graphql", f.graphqlHandler)
	f.engine.GET("/graphql", f.graphqlPlaygroundHandler)
}

// Start starts the web server
func (f *Framework) Start() error {
	f.server = &http.Server{
		Addr:           fmt.Sprintf("%s:%d", f.config.Host, f.config.Port),
		Handler:        f.engine,
		ReadTimeout:    f.config.ReadTimeout,
		WriteTimeout:   f.config.WriteTimeout,
		MaxHeaderBytes: f.config.MaxHeaderBytes,
	}

	// Start WebSocket broadcast goroutine
	if f.config.EnableWebSocket {
		go f.broadcastMessages()
	}

	// Start server in goroutine
	go func() {
		fmt.Printf("🚀 Web server starting on %s:%d\n", f.config.Host, f.config.Port)
		if err := f.server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			fmt.Printf("❌ Server error: %v\n", err)
		}
	}()

	// Wait for interrupt signal
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	fmt.Println("🛑 Shutting down server...")
	return f.Shutdown()
}

// Shutdown gracefully shuts down the server
func (f *Framework) Shutdown() error {
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	// Close WebSocket connections
	if f.config.EnableWebSocket {
		for client := range f.clients {
			client.Close()
		}
		close(f.broadcast)
	}

	return f.server.Shutdown(ctx)
}

// GetEngine returns the Gin engine
func (f *Framework) GetEngine() *gin.Engine {
	return f.engine
}

// GetMetrics returns the metrics instance
func (f *Framework) GetMetrics() *Metrics {
	return f.metrics
}

// GetTracer returns the tracer instance
func (f *Framework) GetTracer() trace.Tracer {
	return f.tracer
}

// Broadcast sends a message to all WebSocket clients
func (f *Framework) Broadcast(message []byte) {
	if f.config.EnableWebSocket {
		f.broadcast <- message
	}
}

// broadcastMessages handles WebSocket message broadcasting
func (f *Framework) broadcastMessages() {
	for message := range f.broadcast {
		for client := range f.clients {
			err := client.WriteMessage(websocket.TextMessage, message)
			if err != nil {
				client.Close()
				delete(f.clients, client)
			}
		}
	}
} 