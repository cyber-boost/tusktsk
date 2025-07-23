package main

import (
	"fmt"
	"log"
	"time"

	"github.com/cyber-boost/tusktsk/pkg/web"
)

func main() {
	fmt.Println("🚀 TuskTSK Web Server Demo")
	fmt.Println("==========================")

	// Create custom configuration
	config := &web.Config{
		Port:            9090,
		Host:            "localhost",
		ReadTimeout:     30 * time.Second,
		WriteTimeout:    30 * time.Second,
		MaxHeaderBytes:  1 << 20,
		EnableCORS:      true,
		EnableMetrics:   true,
		EnableTracing:   true,
		EnableWebSocket: true,
		StaticPath:      "./static",
		LogLevel:        "debug",
	}

	// Create web framework
	framework := web.NewFramework(config)

	fmt.Printf("📍 Server will start on http://%s:%d\n", config.Host, config.Port)
	fmt.Printf("🔌 WebSocket endpoint: ws://%s:%d/ws\n", config.Host, config.Port)
	fmt.Printf("📊 Metrics endpoint: http://%s:%d/metrics\n", config.Host, config.Port)
	fmt.Printf("🏥 Health check: http://%s:%d/health\n", config.Host, config.Port)
	fmt.Printf("📋 API status: http://%s:%d/api/v1/status\n", config.Host, config.Port)
	fmt.Printf("ℹ️  API info: http://%s:%d/api/v1/info\n", config.Host, config.Port)
	fmt.Printf("🔄 Echo endpoint: http://%s:%d/api/v1/echo\n", config.Host, config.Port)
	fmt.Printf("🔍 GraphQL playground: http://%s:%d/graphql\n", config.Host, config.Port)
	fmt.Println()

	// Start metrics update goroutine
	go func() {
		ticker := time.NewTicker(30 * time.Second)
		defer ticker.Stop()

		for {
			select {
			case <-ticker.C:
				framework.GetMetrics().UpdateSystemMetrics()
				framework.GetMetrics().UpdateUptime(time.Since(framework.startTime))
			}
		}
	}()

	// Start the server
	fmt.Println("🎯 Starting server...")
	if err := framework.Start(); err != nil {
		log.Fatalf("Failed to start server: %v", err)
	}
} 