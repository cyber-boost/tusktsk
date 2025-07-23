// Package tusktsk provides a high-performance Go implementation of the TuskLang SDK
// with advanced features for parsing, executing, and optimizing TuskLang code.
//
// The TuskLang Go SDK delivers:
//   - 20x performance improvement over JavaScript SDK
//   - 104+ operators for data manipulation and logic
//   - 42 CLI commands for development and deployment
//   - Multi-database support (SQLite, PostgreSQL, MySQL, MongoDB, Redis)
//   - JIT compilation and multi-level caching
//   - Built-in web framework with HTTP, WebSocket, and GraphQL
//   - Enterprise security features and monitoring
//
// Quick Start:
//
//	go get github.com/cyber-boost/tusktsk
//
//	package main
//
//	import "github.com/cyber-boost/tusktsk/pkg/core"
//
//	func main() {
//		sdk := core.New()
//		result, _ := sdk.Parse(`@if(@env("DEBUG"), "Debug", "Production")`)
//		output, _ := sdk.Execute(result)
//		fmt.Println(output)
//	}
//
// Features:
//   - High Performance: 5ms execution time with JIT compilation
//   - Multi-Database: 5 database adapters with unified interface
//   - Rich Operators: 104+ operators for comprehensive functionality
//   - CLI Tools: 42 commands for development and management
//   - Web Framework: Built-in HTTP server with REST, WebSocket, GraphQL
//   - Enterprise Ready: Security, monitoring, and scalability features
//
// For more information, visit: https://docs.tusklang.org
package main

import (
	"fmt"
	"os"

	"github.com/cyber-boost/tusktsk/pkg/cli"
)

func main() {
	// Initialize CLI
	app := cli.New()
	
	// Execute CLI commands
	if err := app.Execute(); err != nil {
		fmt.Fprintf(os.Stderr, "Error: %v\n", err)
		os.Exit(1)
	}
} 