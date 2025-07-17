package main

import (
	"fmt"
	"os"
	"path/filepath"

	"github.com/spf13/cobra"
	"tusklang-go"
	"tusklang-go/peanut"
)

var (
	verbose bool
	quiet   bool
	json    bool
	config  string
)

func main() {
	var rootCmd = &cobra.Command{
		Use:   "tsk",
		Short: "TuskLang CLI - Strong. Secure. Scalable.",
		Long: `TuskLang CLI for Go - Universal command-line interface for TuskLang configuration management.

Features:
- Database operations and migrations
- Development server and compilation
- Testing and performance benchmarks
- Service management
- Cache operations
- Configuration management
- Binary compilation and execution
- AI integration
- Utility commands

For more information, visit: https://tusklang.org`,
		Version: "2.0.0",
		PersistentPreRun: func(cmd *cobra.Command, args []string) {
			// Load configuration if specified
			if config != "" {
				// TODO: Load custom config
			}
		},
	}

	// Global flags
	rootCmd.PersistentFlags().BoolVarP(&verbose, "verbose", "v", false, "Enable verbose output")
	rootCmd.PersistentFlags().BoolVarP(&quiet, "quiet", "q", false, "Suppress non-error output")
	rootCmd.PersistentFlags().StringVar(&config, "config", "", "Use alternate config file")
	rootCmd.PersistentFlags().BoolVar(&json, "json", false, "Output in JSON format")

	// Add command groups
	addDatabaseCommands(rootCmd)
	addDevelopmentCommands(rootCmd)
	addTestingCommands(rootCmd)
	addServiceCommands(rootCmd)
	addCacheCommands(rootCmd)
	addConfigCommands(rootCmd)
	addBinaryCommands(rootCmd)
	addAICommands(rootCmd)
	addUtilityCommands(rootCmd)
	addPeanutsCommands(rootCmd)
	addCSSCommands(rootCmd)
	addLicenseCommands(rootCmd)
	addProjectCommands(rootCmd)

	// Interactive mode when no command provided
	rootCmd.RunE = func(cmd *cobra.Command, args []string) error {
		if len(args) == 0 {
			return runInteractiveMode()
		}
		return cmd.Help()
	}

	if err := rootCmd.Execute(); err != nil {
		fmt.Fprintf(os.Stderr, "Error: %v\n", err)
		os.Exit(1)
	}
}

func runInteractiveMode() error {
	fmt.Println("TuskLang v2.0.0 - Interactive Mode")
	fmt.Println("Type 'help' for commands, 'exit' to quit")
	
	// Simple interactive loop
	for {
		fmt.Print("tsk> ")
		var input string
		fmt.Scanln(&input)
		
		if input == "exit" || input == "quit" {
			break
		}
		if input == "help" {
			fmt.Println("Available commands: db, serve, test, config, binary, ai, etc.")
			continue
		}
		
		// Parse and execute command
		// TODO: Implement proper command parsing
		fmt.Printf("Command: %s (not implemented in interactive mode)\n", input)
	}
	
	return nil
}

func addDatabaseCommands(rootCmd *cobra.Command) {
	dbCmd := &cobra.Command{
		Use:   "db",
		Short: "Database operations",
		Long:  "Manage database connections, migrations, and operations",
	}

	dbCmd.AddCommand(
		&cobra.Command{
			Use:   "status",
			Short: "Check database connection status",
			RunE:  runDBStatus,
		},
		&cobra.Command{
			Use:   "migrate [file]",
			Short: "Run migration file",
			Args:  cobra.ExactArgs(1),
			RunE:  runDBMigrate,
		},
		&cobra.Command{
			Use:   "console",
			Short: "Open interactive database console",
			RunE:  runDBConsole,
		},
		&cobra.Command{
			Use:   "backup [file]",
			Short: "Backup database",
			RunE:  runDBBackup,
		},
		&cobra.Command{
			Use:   "restore [file]",
			Short: "Restore from backup file",
			Args:  cobra.ExactArgs(1),
			RunE:  runDBRestore,
		},
		&cobra.Command{
			Use:   "init",
			Short: "Initialize SQLite database",
			RunE:  runDBInit,
		},
	)

	rootCmd.AddCommand(dbCmd)
}

func addDevelopmentCommands(rootCmd *cobra.Command) {
	devCmd := &cobra.Command{
		Use:   "serve [port]",
		Short: "Start development server",
		Long:  "Start a development server for TuskLang applications",
		RunE:  runServe,
	}

	compileCmd := &cobra.Command{
		Use:   "compile [file]",
		Short: "Compile .tsk file to optimized format",
		Args:  cobra.ExactArgs(1),
		RunE:  runCompile,
	}

	optimizeCmd := &cobra.Command{
		Use:   "optimize [file]",
		Short: "Optimize .tsk file for production",
		Args:  cobra.ExactArgs(1),
		RunE:  runOptimize,
	}

	rootCmd.AddCommand(devCmd, compileCmd, optimizeCmd)
}

func addTestingCommands(rootCmd *cobra.Command) {
	testCmd := &cobra.Command{
		Use:   "test [suite]",
		Short: "Run test suites",
		Long:  "Run various test suites for TuskLang components",
		RunE:  runTest,
	}

	rootCmd.AddCommand(testCmd)
}

func addServiceCommands(rootCmd *cobra.Command) {
	servicesCmd := &cobra.Command{
		Use:   "services",
		Short: "Service management",
		Long:  "Manage TuskLang services",
	}

	servicesCmd.AddCommand(
		&cobra.Command{
			Use:   "start",
			Short: "Start all TuskLang services",
			RunE:  runServicesStart,
		},
		&cobra.Command{
			Use:   "stop",
			Short: "Stop all TuskLang services",
			RunE:  runServicesStop,
		},
		&cobra.Command{
			Use:   "restart",
			Short: "Restart all services",
			RunE:  runServicesRestart,
		},
		&cobra.Command{
			Use:   "status",
			Short: "Show status of all services",
			RunE:  runServicesStatus,
		},
	)

	rootCmd.AddCommand(servicesCmd)
}

func addCacheCommands(rootCmd *cobra.Command) {
	cacheCmd := &cobra.Command{
		Use:   "cache",
		Short: "Cache operations",
		Long:  "Manage application caches",
	}

	cacheCmd.AddCommand(
		&cobra.Command{
			Use:   "clear",
			Short: "Clear all caches",
			RunE:  runCacheClear,
		},
		&cobra.Command{
			Use:   "status",
			Short: "Show cache status and statistics",
			RunE:  runCacheStatus,
		},
		&cobra.Command{
			Use:   "warm",
			Short: "Pre-warm caches",
			RunE:  runCacheWarm,
		},
		&cobra.Command{
			Use:   "distributed",
			Short: "Show distributed cache status",
			RunE:  runCacheDistributed,
		},
	)

	// Memcached subcommands
	memcachedCmd := &cobra.Command{
		Use:   "memcached",
		Short: "Memcached operations",
	}

	memcachedCmd.AddCommand(
		&cobra.Command{
			Use:   "status",
			Short: "Check Memcached connection status",
			RunE:  runMemcachedStatus,
		},
		&cobra.Command{
			Use:   "stats",
			Short: "Show detailed Memcached statistics",
			RunE:  runMemcachedStats,
		},
		&cobra.Command{
			Use:   "flush",
			Short: "Flush all Memcached data",
			RunE:  runMemcachedFlush,
		},
		&cobra.Command{
			Use:   "restart",
			Short: "Restart Memcached service",
			RunE:  runMemcachedRestart,
		},
		&cobra.Command{
			Use:   "test",
			Short: "Test Memcached connection",
			RunE:  runMemcachedTest,
		},
	)

	cacheCmd.AddCommand(memcachedCmd)
	rootCmd.AddCommand(cacheCmd)
}

func addConfigCommands(rootCmd *cobra.Command) {
	configCmd := &cobra.Command{
		Use:   "config",
		Short: "Configuration management",
		Long:  "Manage TuskLang configuration files",
	}

	configCmd.AddCommand(
		&cobra.Command{
			Use:   "get [key.path] [dir]",
			Short: "Get configuration value by path",
			Args:  cobra.RangeArgs(1, 2),
			RunE:  runConfigGet,
		},
		&cobra.Command{
			Use:   "check [path]",
			Short: "Check configuration hierarchy",
			RunE:  runConfigCheck,
		},
		&cobra.Command{
			Use:   "validate [path]",
			Short: "Validate entire configuration chain",
			RunE:  runConfigValidate,
		},
		&cobra.Command{
			Use:   "compile [path]",
			Short: "Auto-compile all peanu.tsk files",
			RunE:  runConfigCompile,
		},
		&cobra.Command{
			Use:   "docs [path]",
			Short: "Generate configuration documentation",
			RunE:  runConfigDocs,
		},
		&cobra.Command{
			Use:   "clear-cache [path]",
			Short: "Clear configuration cache",
			RunE:  runConfigClearCache,
		},
		&cobra.Command{
			Use:   "stats",
			Short: "Show configuration performance statistics",
			RunE:  runConfigStats,
		},
	)

	rootCmd.AddCommand(configCmd)
}

func addBinaryCommands(rootCmd *cobra.Command) {
	binaryCmd := &cobra.Command{
		Use:   "binary",
		Short: "Binary performance operations",
		Long:  "Compile and execute binary TuskLang files",
	}

	binaryCmd.AddCommand(
		&cobra.Command{
			Use:   "compile [file.tsk]",
			Short: "Compile to binary format (.tskb)",
			Args:  cobra.ExactArgs(1),
			RunE:  runBinaryCompile,
		},
		&cobra.Command{
			Use:   "execute [file.tskb]",
			Short: "Execute binary file directly",
			Args:  cobra.ExactArgs(1),
			RunE:  runBinaryExecute,
		},
		&cobra.Command{
			Use:   "benchmark [file]",
			Short: "Compare binary vs text performance",
			Args:  cobra.ExactArgs(1),
			RunE:  runBinaryBenchmark,
		},
		&cobra.Command{
			Use:   "optimize [file]",
			Short: "Optimize binary for production",
			Args:  cobra.ExactArgs(1),
			RunE:  runBinaryOptimize,
		},
	)

	rootCmd.AddCommand(binaryCmd)
}

func addAICommands(rootCmd *cobra.Command) {
	aiCmd := &cobra.Command{
		Use:   "ai",
		Short: "AI integration",
		Long:  "Integrate with AI services for code analysis and optimization",
	}

	aiCmd.AddCommand(
		&cobra.Command{
			Use:   "claude [prompt]",
			Short: "Query Claude AI with prompt",
			Args:  cobra.MinimumNArgs(1),
			RunE:  runAIClaude,
		},
		&cobra.Command{
			Use:   "chatgpt [prompt]",
			Short: "Query ChatGPT with prompt",
			Args:  cobra.MinimumNArgs(1),
			RunE:  runAIChatGPT,
		},
		&cobra.Command{
			Use:   "custom [api] [prompt]",
			Short: "Query custom AI API endpoint",
			Args:  cobra.ExactArgs(2),
			RunE:  runAICustom,
		},
		&cobra.Command{
			Use:   "config",
			Short: "Show current AI configuration",
			RunE:  runAIConfig,
		},
		&cobra.Command{
			Use:   "setup",
			Short: "Interactive AI API key setup",
			RunE:  runAISetup,
		},
		&cobra.Command{
			Use:   "test",
			Short: "Test all configured AI connections",
			RunE:  runAITest,
		},
		&cobra.Command{
			Use:   "complete [file] [line] [column]",
			Short: "Get AI-powered auto-completion",
			Args:  cobra.RangeArgs(1, 3),
			RunE:  runAIComplete,
		},
		&cobra.Command{
			Use:   "analyze [file]",
			Short: "Analyze file for errors and improvements",
			Args:  cobra.ExactArgs(1),
			RunE:  runAIAnalyze,
		},
		&cobra.Command{
			Use:   "optimize [file]",
			Short: "Get performance optimization suggestions",
			Args:  cobra.ExactArgs(1),
			RunE:  runAIOptimize,
		},
		&cobra.Command{
			Use:   "security [file]",
			Short: "Scan for security vulnerabilities",
			Args:  cobra.ExactArgs(1),
			RunE:  runAISecurity,
		},
	)

	rootCmd.AddCommand(aiCmd)
}

func addUtilityCommands(rootCmd *cobra.Command) {
	rootCmd.AddCommand(
		&cobra.Command{
			Use:   "parse [file]",
			Short: "Parse and display TSK file contents",
			Args:  cobra.ExactArgs(1),
			RunE:  runParse,
		},
		&cobra.Command{
			Use:   "validate [file]",
			Short: "Validate TSK file syntax",
			Args:  cobra.ExactArgs(1),
			RunE:  runValidate,
		},
		&cobra.Command{
			Use:   "convert",
			Short: "Convert between formats",
			RunE:  runConvert,
		},
		&cobra.Command{
			Use:   "get [file] [key.path]",
			Short: "Get specific value by key path",
			Args:  cobra.ExactArgs(2),
			RunE:  runGet,
		},
		&cobra.Command{
			Use:   "set [file] [key.path] [value]",
			Short: "Set value by key path",
			Args:  cobra.ExactArgs(3),
			RunE:  runSet,
		},
		&cobra.Command{
			Use:   "version",
			Short: "Show version information",
			Run:   runVersion,
		},
	)
}

func addPeanutsCommands(rootCmd *cobra.Command) {
	peanutsCmd := &cobra.Command{
		Use:   "peanuts",
		Short: "Peanuts configuration operations",
		Long:  "Manage .peanuts files and binary compilation",
	}

	peanutsCmd.AddCommand(
		&cobra.Command{
			Use:   "compile [file]",
			Short: "Compile .peanuts to binary .pnt",
			Args:  cobra.ExactArgs(1),
			RunE:  runPeanutsCompile,
		},
		&cobra.Command{
			Use:   "auto-compile [dir]",
			Short: "Auto-compile all peanuts files in directory",
			RunE:  runPeanutsAutoCompile,
		},
		&cobra.Command{
			Use:   "load [file.pnt]",
			Short: "Load and display binary peanuts file",
			Args:  cobra.ExactArgs(1),
			RunE:  runPeanutsLoad,
		},
	)

	rootCmd.AddCommand(peanutsCmd)
}

func addCSSCommands(rootCmd *cobra.Command) {
	cssCmd := &cobra.Command{
		Use:   "css",
		Short: "CSS operations",
		Long:  "Expand CSS shortcodes and manage mappings",
	}

	cssCmd.AddCommand(
		&cobra.Command{
			Use:   "expand [input] [output]",
			Short: "Expand CSS shortcodes in file",
			Args:  cobra.RangeArgs(1, 2),
			RunE:  runCSSExpand,
		},
		&cobra.Command{
			Use:   "map",
			Short: "Show all shortcode → property mappings",
			RunE:  runCSSMap,
		},
	)

	rootCmd.AddCommand(cssCmd)
}

func addLicenseCommands(rootCmd *cobra.Command) {
	licenseCmd := &cobra.Command{
		Use:   "license",
		Short: "License management",
		Long:  "Check and manage TuskLang licenses",
	}

	licenseCmd.AddCommand(
		&cobra.Command{
			Use:   "check",
			Short: "Check current license status",
			RunE:  runLicenseCheck,
		},
		&cobra.Command{
			Use:   "activate [key]",
			Short: "Activate license with key",
			Args:  cobra.ExactArgs(1),
			RunE:  runLicenseActivate,
		},
	)

	rootCmd.AddCommand(licenseCmd)
}

func addProjectCommands(rootCmd *cobra.Command) {
	rootCmd.AddCommand(
		&cobra.Command{
			Use:   "init [project-name]",
			Short: "Initialize new TuskLang project",
			RunE:  runInit,
		},
		&cobra.Command{
			Use:   "migrate",
			Short: "Migrate from other formats",
			RunE:  runMigrate,
		},
	)
} 