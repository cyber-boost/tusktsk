package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
)

type CommandDoc struct {
	Path        string
	Title       string
	Description string
	Synopsis    string
	Options     []Option
	Examples    []string
}

type Option struct {
	Long     string
	Short    string
	Desc     string
	Default  string
}

func main() {
	commands := []CommandDoc{
		// Development Commands
		{
			Path:        "commands/dev/serve.md",
			Title:       "tsk serve",
			Description: "Start development server for local development and testing",
			Synopsis:    "tsk serve [OPTIONS] [port]",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--config", "-c", "Configuration file path", "peanu.peanuts"},
				{"--hot-reload", "-r", "Enable hot reloading", "false"},
				{"--host", "", "Server host address", "localhost"},
				{"--timeout", "-t", "Request timeout in seconds", "30"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk serve 8080",
				"tsk serve --hot-reload 3000",
				"tsk serve --config dev.peanuts 9000",
			},
		},
		{
			Path:        "commands/dev/compile.md",
			Title:       "tsk compile",
			Description: "Compile TuskLang files to binary format for production",
			Synopsis:    "tsk compile [OPTIONS] <file>",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--output", "-o", "Output file path", "-"},
				{"--optimize", "", "Enable optimization", "false"},
				{"--format", "-f", "Output format (binary, json)", "binary"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk compile app.tsk",
				"tsk compile -o app.bin app.tsk",
				"tsk compile --optimize config.tsk",
			},
		},
		{
			Path:        "commands/dev/optimize.md",
			Title:       "tsk optimize",
			Description: "Optimize TuskLang files for better performance",
			Synopsis:    "tsk optimize [OPTIONS] <file>",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--output", "-o", "Output file path", "-"},
				{"--level", "-l", "Optimization level (1-3)", "2"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk optimize config.tsk",
				"tsk optimize -o optimized.tsk config.tsk",
				"tsk optimize --level 3 app.tsk",
			},
		},
		// Testing Commands
		{
			Path:        "commands/test/all.md",
			Title:       "tsk test all",
			Description: "Run all test suites including parser, FUJSEN, SDK, and performance tests",
			Synopsis:    "tsk test all [OPTIONS]",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--parallel", "-p", "Run tests in parallel", "false"},
				{"--timeout", "-t", "Test timeout in seconds", "300"},
				{"--coverage", "-c", "Generate coverage report", "false"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk test all",
				"tsk test all --parallel",
				"tsk test all --coverage",
			},
		},
		{
			Path:        "commands/test/parser.md",
			Title:       "tsk test parser",
			Description: "Run parser-specific tests to validate TuskLang syntax parsing",
			Synopsis:    "tsk test parser [OPTIONS]",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--file", "-f", "Test specific file", "-"},
				{"--timeout", "-t", "Test timeout in seconds", "60"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk test parser",
				"tsk test parser --file test.tsk",
				"tsk test parser --verbose",
			},
		},
		{
			Path:        "commands/test/fujsen.md",
			Title:       "tsk test fujsen",
			Description: "Run FUJSEN intelligence system tests",
			Synopsis:    "tsk test fujsen [OPTIONS]",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--suite", "-s", "Test suite name", "-"},
				{"--timeout", "-t", "Test timeout in seconds", "120"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk test fujsen",
				"tsk test fujsen --suite core",
				"tsk test fujsen --verbose",
			},
		},
		{
			Path:        "commands/test/sdk.md",
			Title:       "tsk test sdk",
			Description: "Run SDK integration and functionality tests",
			Synopsis:    "tsk test sdk [OPTIONS]",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--module", "-m", "Test specific module", "-"},
				{"--timeout", "-t", "Test timeout in seconds", "180"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk test sdk",
				"tsk test sdk --module peanut",
				"tsk test sdk --verbose",
			},
		},
		{
			Path:        "commands/test/performance.md",
			Title:       "tsk test performance",
			Description: "Run performance benchmarks and load tests",
			Synopsis:    "tsk test performance [OPTIONS]",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--duration", "-d", "Test duration in seconds", "60"},
				{"--concurrent", "-c", "Number of concurrent requests", "10"},
				{"--output", "-o", "Output file for results", "-"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk test performance",
				"tsk test performance --duration 120",
				"tsk test performance --concurrent 50",
			},
		},
		// Configuration Commands
		{
			Path:        "commands/config/get.md",
			Title:       "tsk config get",
			Description: "Get configuration value using dot notation path",
			Synopsis:    "tsk config get [OPTIONS] <key.path> [dir]",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--default", "-d", "Default value if key not found", "-"},
				{"--type", "-t", "Expected value type", "auto"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk config get server.port",
				"tsk config get database.host .",
				"tsk config get app.name --default 'My App'",
			},
		},
		{
			Path:        "commands/config/check.md",
			Title:       "tsk config check",
			Description: "Check configuration hierarchy and file loading order",
			Synopsis:    "tsk config check [OPTIONS] [path]",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--detailed", "-d", "Show detailed information", "false"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk config check",
				"tsk config check .",
				"tsk config check --detailed",
			},
		},
		{
			Path:        "commands/config/validate.md",
			Title:       "tsk config validate",
			Description: "Validate configuration syntax and structure",
			Synopsis:    "tsk config validate [OPTIONS] [path]",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--schema", "-s", "Schema file for validation", "-"},
				{"--strict", "", "Enable strict validation", "false"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk config validate",
				"tsk config validate .",
				"tsk config validate --schema schema.json",
			},
		},
		{
			Path:        "commands/config/compile.md",
			Title:       "tsk config compile",
			Description: "Compile configuration files to binary format",
			Synopsis:    "tsk config compile [OPTIONS] [path]",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--output", "-o", "Output directory", "-"},
				{"--format", "-f", "Output format (pnt, json)", "pnt"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk config compile",
				"tsk config compile .",
				"tsk config compile --output dist/",
			},
		},
		// Peanuts Commands
		{
			Path:        "commands/peanuts/compile.md",
			Title:       "tsk peanuts compile",
			Description: "Compile .peanuts or .tsk files to binary .pnt format",
			Synopsis:    "tsk peanuts compile [OPTIONS] <file>",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--output", "-o", "Output file path", "-"},
				{"--optimize", "", "Enable optimization", "false"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk peanuts compile config.peanuts",
				"tsk peanuts compile -o config.pnt config.tsk",
				"tsk peanuts compile --optimize app.peanuts",
			},
		},
		{
			Path:        "commands/peanuts/load.md",
			Title:       "tsk peanuts load",
			Description: "Load and display binary peanuts file contents",
			Synopsis:    "tsk peanuts load [OPTIONS] <file.pnt>",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--format", "-f", "Output format (table, json, yaml)", "table"},
				{"--key", "-k", "Show specific key only", "-"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk peanuts load config.pnt",
				"tsk peanuts load --format json config.pnt",
				"tsk peanuts load --key server.port config.pnt",
			},
		},
		// Utility Commands
		{
			Path:        "commands/utility/parse.md",
			Title:       "tsk parse",
			Description: "Parse TuskLang file and display syntax tree",
			Synopsis:    "tsk parse [OPTIONS] <file>",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--format", "-f", "Output format (tree, json, yaml)", "tree"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk parse config.tsk",
				"tsk parse --format json app.tsk",
				"tsk parse --verbose schema.tsk",
			},
		},
		{
			Path:        "commands/utility/validate.md",
			Title:       "tsk validate",
			Description: "Validate TuskLang file syntax and structure",
			Synopsis:    "tsk validate [OPTIONS] <file>",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--schema", "-s", "Schema file for validation", "-"},
				{"--strict", "", "Enable strict validation", "false"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk validate config.tsk",
				"tsk validate --schema schema.json app.tsk",
				"tsk validate --strict schema.tsk",
			},
		},
		{
			Path:        "commands/utility/version.md",
			Title:       "tsk version",
			Description: "Display TuskLang CLI version and build information",
			Synopsis:    "tsk version [OPTIONS]",
			Options: []Option{
				{"--help", "-h", "Show help for this command", "-"},
				{"--json", "-j", "Output in JSON format", "false"},
				{"--quiet", "-q", "Suppress output", "false"},
				{"--verbose", "-v", "Show verbose output", "false"},
			},
			Examples: []string{
				"tsk version",
				"tsk version --json",
				"tsk version --verbose",
			},
		},
	}

	baseDir := "cli-docs/go"

	for _, cmd := range commands {
		filePath := filepath.Join(baseDir, cmd.Path)
		dir := filepath.Dir(filePath)
		
		// Create directory if it doesn't exist
		if err := os.MkdirAll(dir, 0755); err != nil {
			fmt.Printf("Error creating directory %s: %v\n", dir, err)
			continue
		}

		content := generateCommandDoc(cmd)
		if err := os.WriteFile(filePath, []byte(content), 0644); err != nil {
			fmt.Printf("Error writing file %s: %v\n", filePath, err)
		} else {
			fmt.Printf("Created: %s\n", filePath)
		}
	}

	// Create README files for command categories
	categories := []string{
		"commands/test", "commands/services", "commands/cache", "commands/config",
		"commands/binary", "commands/peanuts", "commands/ai", "commands/css",
		"commands/license", "commands/utility", "commands/cache/memcached", "examples",
	}

	for _, category := range categories {
		readmePath := filepath.Join(baseDir, category, "README.md")
		dir := filepath.Dir(readmePath)
		
		if err := os.MkdirAll(dir, 0755); err != nil {
			fmt.Printf("Error creating directory %s: %v\n", dir, err)
			continue
		}

		content := generateCategoryREADME(category)
		if err := os.WriteFile(readmePath, []byte(content), 0644); err != nil {
			fmt.Printf("Error writing file %s: %v\n", readmePath, err)
		} else {
			fmt.Printf("Created: %s\n", readmePath)
		}
	}

	fmt.Println("\nCLI documentation generation completed!")
}

func generateCommandDoc(cmd CommandDoc) string {
	var optionsTable strings.Builder
	optionsTable.WriteString("| Option | Short | Description | Default |\n")
	optionsTable.WriteString("|--------|-------|-------------|---------|\n")
	
	for _, opt := range cmd.Options {
		optionsTable.WriteString(fmt.Sprintf("| %s | %s | %s | %s |\n", opt.Long, opt.Short, opt.Desc, opt.Default))
	}

	var examples strings.Builder
	for _, ex := range cmd.Examples {
		examples.WriteString(fmt.Sprintf("```bash\n%s\n```\n\n", ex))
	}

	return fmt.Sprintf("# %s\n\n%s\n\n## Synopsis\n\n```bash\n%s\n```\n\n## Description\n\n%s\n\n## Options\n\n%s\n\n## Examples\n\n%s\n\n## Go API Usage\n\n```go\npackage main\n\nimport (\n    \"fmt\"\n    \"log\"\n    \"github.com/tusklang/go-sdk/cli\"\n)\n\nfunc main() {\n    // Create CLI client\n    client := cli.NewClient()\n    \n    // Execute command\n    result, err := client.Execute(\"%s\")\n    if err != nil {\n        log.Fatal(err)\n    }\n    \n    fmt.Printf(\"Command executed successfully: %%s\\n\", result.Output)\n}\n```\n\n## Output\n\n### Success Output\n\nWhen the command completes successfully:\n\n- Success status with ✅ symbol\n- Command results and summary\n- Execution duration\n- Relevant metrics\n\n### Error Output\n\nWhen the command fails:\n\n- Error status with ❌ symbol\n- Detailed error message\n- Troubleshooting suggestions\n\n## Exit Codes\n\n| Code | Description |\n|------|-------------|\n| 0 | Success - Command completed |\n| 1 | Error - Command failed |\n| 2 | Error - Invalid arguments |\n| 3 | Error - Configuration error |\n\n## Related Commands\n\n- Related command links will be added here\n\n## Notes\n\n- **Configuration**: Uses settings from peanu.peanuts file\n- **Performance**: Optimized for Go applications\n- **Compatibility**: Works across all supported platforms\n\n## See Also\n\n- [Commands Overview](../README.md)\n- [Configuration Guide](../../../go/docs/PNT_GUIDE.md)\n- [Examples](../../examples/)\n", cmd.Title, cmd.Description, cmd.Synopsis, cmd.Description, optionsTable.String(), examples.String(), strings.Fields(cmd.Title)[1], cmd.Title)
}

func generateCategoryREADME(category string) string {
	title := strings.Title(strings.ReplaceAll(strings.TrimPrefix(category, "commands/"), "/", " "))
	
	return fmt.Sprintf(`# %s Commands

%s operations for TuskLang Go CLI.

## Available Commands

| Command | Description |
|---------|-------------|
| [Command documentation files will be listed here] | [Brief descriptions] |

## Common Use Cases

- **Use Case 1**: Description of common scenario
- **Use Case 2**: Description of common scenario
- **Use Case 3**: Description of common scenario

## Go-Specific Notes

The Go CLI %s commands provide:

- **Feature 1**: Description
- **Feature 2**: Description
- **Feature 3**: Description

## Examples

### Basic Usage

```bash
# Example command usage
tsk command example
```

### Advanced Usage

```bash
# Advanced command usage
tsk command --option value
```

## Related Categories

- [Other command categories](../README.md)
`, title, title, strings.ToLower(title))
} 