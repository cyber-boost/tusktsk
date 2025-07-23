package web

import (
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"time"

	"github.com/spf13/cobra"
)

// WebCLI provides CLI commands for web server management
type WebCLI struct {
	framework *Framework
}

// NewWebCLI creates a new web CLI instance
func NewWebCLI() *WebCLI {
	return &WebCLI{}
}

// GetCommands returns all web-related CLI commands
func (w *WebCLI) GetCommands() []*cobra.Command {
	return []*cobra.Command{
		w.startCommand(),
		w.statusCommand(),
		w.stopCommand(),
		w.testCommand(),
		w.configCommand(),
		w.logsCommand(),
	}
}

// startCommand creates the web start command
func (w *WebCLI) startCommand() *cobra.Command {
	var port int
	var host string
	var configFile string

	cmd := &cobra.Command{
		Use:   "start",
		Short: "Start the web server",
		Long:  "Start the TuskTSK web server with the specified configuration",
		RunE: func(cmd *cobra.Command, args []string) error {
			config := DefaultConfig()
			
			// Override with command line flags
			if port != 0 {
				config.Port = port
			}
			if host != "" {
				config.Host = host
			}

			// Load config from file if specified
			if configFile != "" {
				if err := w.loadConfigFromFile(config, configFile); err != nil {
					return fmt.Errorf("failed to load config: %w", err)
				}
			}

			// Create and start framework
			w.framework = NewFramework(config)
			
			fmt.Printf("🚀 Starting TuskTSK Web Server...\n")
			fmt.Printf("📍 Host: %s\n", config.Host)
			fmt.Printf("🔌 Port: %d\n", config.Port)
			fmt.Printf("🌐 CORS: %t\n", config.EnableCORS)
			fmt.Printf("📊 Metrics: %t\n", config.EnableMetrics)
			fmt.Printf("🔍 Tracing: %t\n", config.EnableTracing)
			fmt.Printf("🔌 WebSocket: %t\n", config.EnableWebSocket)
			fmt.Printf("📁 Static Path: %s\n", config.StaticPath)
			fmt.Printf("📝 Log Level: %s\n", config.LogLevel)
			fmt.Printf("⏰ Read Timeout: %v\n", config.ReadTimeout)
			fmt.Printf("⏰ Write Timeout: %v\n", config.WriteTimeout)
			fmt.Printf("📦 Max Header Bytes: %d\n", config.MaxHeaderBytes)
			fmt.Println()

			return w.framework.Start()
		},
	}

	cmd.Flags().IntVarP(&port, "port", "p", 8080, "Port to listen on")
	cmd.Flags().StringVarP(&host, "host", "H", "0.0.0.0", "Host to bind to")
	cmd.Flags().StringVarP(&configFile, "config", "c", "", "Configuration file path")

	return cmd
}

// statusCommand creates the web status command
func (w *WebCLI) statusCommand() *cobra.Command {
	var serverURL string

	cmd := &cobra.Command{
		Use:   "status",
		Short: "Check web server status",
		Long:  "Check the status of the TuskTSK web server",
		RunE: func(cmd *cobra.Command, args []string) error {
			if serverURL == "" {
				serverURL = "http://localhost:8080"
			}

			fmt.Printf("🔍 Checking server status at %s...\n", serverURL)

			// Check health endpoint
			healthURL := serverURL + "/health"
			resp, err := http.Get(healthURL)
			if err != nil {
				return fmt.Errorf("server not responding: %w", err)
			}
			defer resp.Body.Close()

			if resp.StatusCode != http.StatusOK {
				return fmt.Errorf("server returned status: %d", resp.StatusCode)
			}

			// Check status endpoint
			statusURL := serverURL + "/api/v1/status"
			resp, err = http.Get(statusURL)
			if err != nil {
				return fmt.Errorf("failed to get status: %w", err)
			}
			defer resp.Body.Close()

			var status map[string]interface{}
			if err := json.NewDecoder(resp.Body).Decode(&status); err != nil {
				return fmt.Errorf("failed to decode status: %w", err)
			}

			fmt.Printf("✅ Server is running\n")
			fmt.Printf("📊 Status: %v\n", status["status"])
			fmt.Printf("🖥️  System: %v\n", status["system"])
			fmt.Printf("🌐 Web: %v\n", status["web"])
			fmt.Printf("⏰ Timestamp: %v\n", status["timestamp"])

			return nil
		},
	}

	cmd.Flags().StringVarP(&serverURL, "url", "u", "", "Server URL to check")

	return cmd
}

// stopCommand creates the web stop command
func (w *WebCLI) stopCommand() *cobra.Command {
	var serverURL string

	cmd := &cobra.Command{
		Use:   "stop",
		Short: "Stop the web server",
		Long:  "Stop the TuskTSK web server gracefully",
		RunE: func(cmd *cobra.Command, args []string) error {
			if w.framework != nil {
				fmt.Println("🛑 Stopping web server...")
				return w.framework.Shutdown()
			}

			// Try to stop via HTTP if framework is not available
			if serverURL == "" {
				serverURL = "http://localhost:8080"
			}

			fmt.Printf("🛑 Attempting to stop server at %s...\n", serverURL)
			
			// Send shutdown signal (this would need to be implemented in the server)
			shutdownURL := serverURL + "/shutdown"
			resp, err := http.Post(shutdownURL, "application/json", nil)
			if err != nil {
				return fmt.Errorf("failed to send shutdown signal: %w", err)
			}
			defer resp.Body.Close()

			fmt.Println("✅ Shutdown signal sent successfully")
			return nil
		},
	}

	cmd.Flags().StringVarP(&serverURL, "url", "u", "", "Server URL to stop")

	return cmd
}

// testCommand creates the web test command
func (w *WebCLI) testCommand() *cobra.Command {
	var serverURL string
	var endpoint string

	cmd := &cobra.Command{
		Use:   "test",
		Short: "Test web server endpoints",
		Long:  "Test various endpoints of the TuskTSK web server",
		RunE: func(cmd *cobra.Command, args []string) error {
			if serverURL == "" {
				serverURL = "http://localhost:8080"
			}

			fmt.Printf("🧪 Testing web server at %s...\n\n", serverURL)

			// Test endpoints
			endpoints := []string{
				"/health",
				"/api/v1/status",
				"/api/v1/info",
				"/metrics",
			}

			if endpoint != "" {
				endpoints = []string{endpoint}
			}

			for _, ep := range endpoints {
				if err := w.testEndpoint(serverURL + ep); err != nil {
					fmt.Printf("❌ %s: %v\n", ep, err)
				} else {
					fmt.Printf("✅ %s: OK\n", ep)
				}
			}

			// Test WebSocket if enabled
			if endpoint == "" || endpoint == "/ws" {
				if err := w.testWebSocket(serverURL + "/ws"); err != nil {
					fmt.Printf("❌ /ws: %v\n", err)
				} else {
					fmt.Printf("✅ /ws: OK\n")
				}
			}

			return nil
		},
	}

	cmd.Flags().StringVarP(&serverURL, "url", "u", "", "Server URL to test")
	cmd.Flags().StringVarP(&endpoint, "endpoint", "e", "", "Specific endpoint to test")

	return cmd
}

// configCommand creates the web config command
func (w *WebCLI) configCommand() *cobra.Command {
	var outputFile string

	cmd := &cobra.Command{
		Use:   "config",
		Short: "Show web server configuration",
		Long:  "Display the current web server configuration",
		RunE: func(cmd *cobra.Command, args []string) error {
			config := DefaultConfig()
			
			configJSON, err := json.MarshalIndent(config, "", "  ")
			if err != nil {
				return fmt.Errorf("failed to marshal config: %w", err)
			}

			if outputFile != "" {
				if err := os.WriteFile(outputFile, configJSON, 0644); err != nil {
					return fmt.Errorf("failed to write config file: %w", err)
				}
				fmt.Printf("📄 Configuration written to %s\n", outputFile)
			} else {
				fmt.Println("⚙️  Web Server Configuration:")
				fmt.Println(string(configJSON))
			}

			return nil
		},
	}

	cmd.Flags().StringVarP(&outputFile, "output", "o", "", "Output file for configuration")

	return cmd
}

// logsCommand creates the web logs command
func (w *WebCLI) logsCommand() *cobra.Command {
	var follow bool
	var lines int

	cmd := &cobra.Command{
		Use:   "logs",
		Short: "Show web server logs",
		Long:  "Display web server logs (placeholder for log management)",
		RunE: func(cmd *cobra.Command, args []string) error {
			fmt.Println("📋 Web Server Logs")
			fmt.Println("==================")
			fmt.Println("Log management system coming soon...")
			fmt.Printf("Follow: %t\n", follow)
			fmt.Printf("Lines: %d\n", lines)
			fmt.Println()
			fmt.Println("For now, logs are output to stdout/stderr.")
			fmt.Println("Use 'tsk web start' to see real-time logs.")

			return nil
		},
	}

	cmd.Flags().BoolVarP(&follow, "follow", "f", false, "Follow log output")
	cmd.Flags().IntVarP(&lines, "lines", "n", 50, "Number of lines to show")

	return cmd
}

// testEndpoint tests a specific endpoint
func (w *WebCLI) testEndpoint(url string) error {
	client := &http.Client{
		Timeout: 10 * time.Second,
	}

	resp, err := client.Get(url)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	if resp.StatusCode >= 400 {
		return fmt.Errorf("HTTP %d", resp.StatusCode)
	}

	return nil
}

// testWebSocket tests WebSocket connectivity
func (w *WebCLI) testWebSocket(url string) error {
	// This is a placeholder - actual WebSocket testing would require
	// a WebSocket client implementation
	return fmt.Errorf("WebSocket testing not implemented yet")
}

// loadConfigFromFile loads configuration from a JSON file
func (w *WebCLI) loadConfigFromFile(config *Config, filename string) error {
	data, err := os.ReadFile(filename)
	if err != nil {
		return err
	}

	return json.Unmarshal(data, config)
} 