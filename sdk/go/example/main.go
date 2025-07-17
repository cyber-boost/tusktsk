package main

import (
	"fmt"
	"log"
	"os"

	"tusklang-go"
)

// AppConfig represents the application configuration structure
type AppConfig struct {
	AppName    string                 `tsk:"app_name"`
	Version    string                 `tsk:"version"`
	Debug      bool                   `tsk:"debug"`
	Port       int                    `tsk:"port"`
	Environment string                `tsk:"environment"`
	BaseURL    string                 `tsk:"base_url"`
	Database   DatabaseConfig         `tsk:"database"`
	Features   []string               `tsk:"features"`
	API        APIConfig              `tsk:"api"`
	Logging    LoggingConfig          `tsk:"logging"`
	Cache      CacheConfig            `tsk:"cache"`
}

type DatabaseConfig struct {
	Host            string `tsk:"host"`
	Port            int    `tsk:"port"`
	Name            string `tsk:"name"`
	User            string `tsk:"user"`
	Password        string `tsk:"password"`
	SSLMode         string `tsk:"ssl_mode"`
	MaxConnections  int    `tsk:"max_connections"`
	Timeout         int    `tsk:"timeout"`
}

type APIConfig struct {
	Version   string                 `tsk:"version"`
	RateLimit int                    `tsk:"rate_limit"`
	Timeout   int                    `tsk:"timeout"`
	CORS      map[string]interface{} `tsk:"cors"`
}

type LoggingConfig struct {
	Level  string                 `tsk:"level"`
	Format string                 `tsk:"format"`
	Output string                 `tsk:"output"`
	File   map[string]interface{} `tsk:"file"`
}

type CacheConfig struct {
	Type   string `tsk:"type"`
	Host   string `tsk:"host"`
	Port   int    `tsk:"port"`
	TTL    int    `tsk:"ttl"`
	Prefix string `tsk:"prefix"`
}

func main() {
	if len(os.Args) < 2 {
		fmt.Println("Usage: go run main.go <config.tsk>")
		os.Exit(1)
	}

	configFile := os.Args[1]

	// Open the configuration file
	file, err := os.Open(configFile)
	if err != nil {
		log.Fatalf("Error opening config file: %v", err)
	}
	defer file.Close()

	// Parse the TuskLang configuration
	parser := tusklanggo.NewParser(file)
	data, err := parser.Parse()
	if err != nil {
		log.Fatalf("Error parsing config: %v", err)
	}

	// Unmarshal into our struct
	var config AppConfig
	err = tusklanggo.UnmarshalTSK(data, &config)
	if err != nil {
		log.Fatalf("Error unmarshaling config: %v", err)
	}

	// Use the configuration
	fmt.Printf("🚀 Starting %s v%s\n", config.AppName, config.Version)
	fmt.Printf("📍 Environment: %s\n", config.Environment)
	fmt.Printf("🌐 Base URL: %s\n", config.BaseURL)
	fmt.Printf("🔌 Port: %d\n", config.Port)
	fmt.Printf("🐛 Debug: %t\n", config.Debug)
	
	fmt.Printf("\n📊 Database: %s:%d/%s\n", config.Database.Host, config.Database.Port, config.Database.Name)
	fmt.Printf("🔐 Cache: %s:%d (TTL: %ds)\n", config.Cache.Host, config.Cache.Port, config.Cache.TTL)
	
	fmt.Printf("\n✨ Features enabled:\n")
	for _, feature := range config.Features {
		fmt.Printf("  - %s\n", feature)
	}
	
	fmt.Printf("\n📝 Logging: %s level, %s format\n", config.Logging.Level, config.Logging.Format)
	fmt.Printf("🔗 API: v%s, Rate limit: %d req/min\n", config.API.Version, config.API.RateLimit)
} 