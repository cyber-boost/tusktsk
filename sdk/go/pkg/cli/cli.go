// Package cli provides command-line interface functionality for the TuskLang SDK
package cli

import (
	"fmt"
	"os"

	tusktsk "github.com/cyber-boost/tusktsk/sdk/go/pkg/core"
	"github.com/spf13/cobra"
)

// CLI represents the command-line interface
type CLI struct {
	rootCmd *cobra.Command
	sdk     *tusktsk.SDK
}

// New creates a new CLI instance
func New(sdk *tusktsk.SDK) *CLI {
	cli := &CLI{
		sdk: sdk,
	}
	cli.setupCommands()
	return cli
}

// Run runs the CLI with the given arguments
func (c *CLI) Run(args []string) error {
	c.rootCmd.SetArgs(args[1:]) // Skip the program name
	return c.rootCmd.Execute()
}

// setupCommands sets up all CLI commands
func (c *CLI) setupCommands() {
	c.rootCmd = &cobra.Command{
		Use:   "tusktsk",
		Short: "TuskLang Go SDK - A powerful language processing toolkit",
		Long: `TuskLang Go SDK provides comprehensive tools for parsing, compiling, and executing TuskLang code.

Features:
- Advanced parsing and syntax analysis
- Binary compilation and execution
- Security validation and protection
- Performance optimization
- Comprehensive error handling`,
		Version: "1.0.0",
	}

	// Add subcommands
	c.addParseCommand()
	c.addCompileCommand()
	c.addExecuteCommand()
	c.addValidateCommand()
	c.addVersionCommand()
}

// addParseCommand adds the parse command
func (c *CLI) addParseCommand() {
	parseCmd := &cobra.Command{
		Use:   "parse [file]",
		Short: "Parse TuskLang code",
		Long:  "Parse TuskLang code and display the AST (Abstract Syntax Tree)",
		Args:  cobra.ExactArgs(1),
		RunE: func(cmd *cobra.Command, args []string) error {
			return c.handleParse(args[0])
		},
	}

	// Add flags
	parseCmd.Flags().BoolP("verbose", "v", false, "Verbose output")
	parseCmd.Flags().StringP("output", "o", "", "Output file for AST")

	c.rootCmd.AddCommand(parseCmd)
}

// addCompileCommand adds the compile command
func (c *CLI) addCompileCommand() {
	compileCmd := &cobra.Command{
		Use:   "compile [file]",
		Short: "Compile TuskLang code to binary",
		Long:  "Compile TuskLang code to binary format for execution",
		Args:  cobra.ExactArgs(1),
		RunE: func(cmd *cobra.Command, args []string) error {
			return c.handleCompile(args[0])
		},
	}

	// Add flags
	compileCmd.Flags().StringP("output", "o", "", "Output binary file")
	compileCmd.Flags().BoolP("optimize", "O", false, "Enable optimization")

	c.rootCmd.AddCommand(compileCmd)
}

// addExecuteCommand adds the execute command
func (c *CLI) addExecuteCommand() {
	executeCmd := &cobra.Command{
		Use:   "execute [file]",
		Short: "Execute TuskLang code",
		Long:  "Execute compiled TuskLang code and display the results",
		Args:  cobra.ExactArgs(1),
		RunE: func(cmd *cobra.Command, args []string) error {
			return c.handleExecute(args[0])
		},
	}

	// Add flags
	executeCmd.Flags().BoolP("debug", "d", false, "Enable debug mode")

	c.rootCmd.AddCommand(executeCmd)
}

// addValidateCommand adds the validate command
func (c *CLI) addValidateCommand() {
	validateCmd := &cobra.Command{
		Use:   "validate [file]",
		Short: "Validate TuskLang code",
		Long:  "Validate TuskLang code for security and syntax issues",
		Args:  cobra.ExactArgs(1),
		RunE: func(cmd *cobra.Command, args []string) error {
			return c.handleValidate(args[0])
		},
	}

	// Add flags
	validateCmd.Flags().BoolP("security", "s", true, "Enable security validation")
	validateCmd.Flags().BoolP("syntax", "y", true, "Enable syntax validation")

	c.rootCmd.AddCommand(validateCmd)
}

// addVersionCommand adds the version command
func (c *CLI) addVersionCommand() {
	versionCmd := &cobra.Command{
		Use:   "version",
		Short: "Show version information",
		Long:  "Display the version of TuskLang Go SDK",
		Run: func(cmd *cobra.Command, args []string) {
			fmt.Printf("TuskLang Go SDK v%s\n", "1.0.0")
		},
	}

	c.rootCmd.AddCommand(versionCmd)
}

// handleParse handles the parse command
func (c *CLI) handleParse(filename string) error {
	// Read file content
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %w", err)
	}

	// Parse the code
	result, err := c.sdk.Parse(string(content))
	if err != nil {
		return fmt.Errorf("parsing failed: %w", err)
	}

	// Display results
	fmt.Printf("Parsed %d tokens\n", len(result.Tokens))
	fmt.Printf("Generated %d AST nodes\n", len(result.AST))

	if len(result.Errors) > 0 {
		fmt.Printf("Found %d errors:\n", len(result.Errors))
		for _, err := range result.Errors {
			fmt.Printf("  - %s\n", err.Message)
		}
	}

	return nil
}

// handleCompile handles the compile command
func (c *CLI) handleCompile(filename string) error {
	// Read file content
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %w", err)
	}

	// Compile the code
	result, err := c.sdk.Compile(string(content))
	if err != nil {
		return fmt.Errorf("compilation failed: %w", err)
	}

	// Display results
	fmt.Printf("Compiled successfully\n")
	fmt.Printf("Binary size: %d bytes\n", result.Size)
	fmt.Printf("Format: %s\n", result.Format)

	return nil
}

// handleExecute handles the execute command
func (c *CLI) handleExecute(filename string) error {
	// Read file content
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %w", err)
	}

	// Execute the code
	result, err := c.sdk.Execute(string(content))
	if err != nil {
		return fmt.Errorf("execution failed: %w", err)
	}

	// Display results
	if result.Output != "" {
		fmt.Printf("Output: %s\n", result.Output)
	}
	if result.Error != "" {
		fmt.Printf("Error: %s\n", result.Error)
	}
	fmt.Printf("Exit code: %d\n", result.Code)

	return nil
}

// handleValidate handles the validate command
func (c *CLI) handleValidate(filename string) error {
	// Read file content
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %w", err)
	}

	// Validate the code
	result := c.sdk.Security.ValidateCode(string(content))

	// Display results
	fmt.Printf("Validation completed\n")
	fmt.Printf("Security score: %d/100\n", result.Score)
	fmt.Printf("Valid: %t\n", result.Valid)

	if len(result.Issues) > 0 {
		fmt.Printf("Found %d issues:\n", len(result.Issues))
		for _, issue := range result.Issues {
			fmt.Printf("  - [%s] %s: %s\n", issue.Severity.String(), issue.Type, issue.Message)
		}
	}

	return nil
} 