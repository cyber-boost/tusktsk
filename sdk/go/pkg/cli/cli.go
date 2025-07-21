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
		Long:  "Execute TuskLang code directly or from a compiled binary",
		Args:  cobra.ExactArgs(1),
		RunE: func(cmd *cobra.Command, args []string) error {
			return c.handleExecute(args[0])
		},
	}

	// Add flags
	executeCmd.Flags().BoolP("compile", "c", false, "Compile before executing")
	executeCmd.Flags().StringP("input", "i", "", "Input data file")

	c.rootCmd.AddCommand(executeCmd)
}

// addValidateCommand adds the validate command
func (c *CLI) addValidateCommand() {
	validateCmd := &cobra.Command{
		Use:   "validate [file]",
		Short: "Validate TuskLang code",
		Long:  "Validate TuskLang code for syntax and security",
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
		Run: func(cmd *cobra.Command, args []string) {
			fmt.Println("TuskLang Go SDK v1.0.0")
			fmt.Println("Build: 2025-01-21")
			fmt.Println("Go Version: 1.23.0")
		},
	}

	c.rootCmd.AddCommand(versionCmd)
}

// handleParse handles the parse command
func (c *CLI) handleParse(filename string) error {
	// Read file
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %v", err)
	}

	// Parse code
	result, err := c.sdk.Parse(string(content))
	if err != nil {
		return fmt.Errorf("parse error: %v", err)
	}

	// Display result
	fmt.Printf("Parse successful!\n")
	fmt.Printf("Tokens: %d\n", len(result.Tokens))
	fmt.Printf("AST Nodes: %d\n", len(result.AST))
	
	return nil
}

// handleCompile handles the compile command
func (c *CLI) handleCompile(filename string) error {
	// Read file
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %v", err)
	}

	// Compile code
	result, err := c.sdk.Compile(string(content))
	if err != nil {
		return fmt.Errorf("compile error: %v", err)
	}

	// Display result
	fmt.Printf("Compilation successful!\n")
	fmt.Printf("Binary size: %d bytes\n", len(result.Binary))
	
	return nil
}

// handleExecute handles the execute command
func (c *CLI) handleExecute(filename string) error {
	// Read file
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %v", err)
	}

	// Execute code
	result, err := c.sdk.Execute(string(content))
	if err != nil {
		return fmt.Errorf("execution error: %v", err)
	}

	// Display result
	fmt.Printf("Execution successful!\n")
	fmt.Printf("Output: %s\n", result.Output)
	
	return nil
}

// handleValidate handles the validate command
func (c *CLI) handleValidate(filename string) error {
	// Read file
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %v", err)
	}

	// Parse for validation
	_, err = c.sdk.Parse(string(content))
	if err != nil {
		return fmt.Errorf("validation failed: %v", err)
	}

	fmt.Printf("Validation successful!\n")
	fmt.Printf("File: %s\n", filename)
	fmt.Printf("Size: %d bytes\n", len(content))
	
	return nil
} 
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
		Long:  "Execute TuskLang code directly or from a compiled binary",
		Args:  cobra.ExactArgs(1),
		RunE: func(cmd *cobra.Command, args []string) error {
			return c.handleExecute(args[0])
		},
	}

	// Add flags
	executeCmd.Flags().BoolP("compile", "c", false, "Compile before executing")
	executeCmd.Flags().StringP("input", "i", "", "Input data file")

	c.rootCmd.AddCommand(executeCmd)
}

// addValidateCommand adds the validate command
func (c *CLI) addValidateCommand() {
	validateCmd := &cobra.Command{
		Use:   "validate [file]",
		Short: "Validate TuskLang code",
		Long:  "Validate TuskLang code for syntax and security",
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
		Run: func(cmd *cobra.Command, args []string) {
			fmt.Println("TuskLang Go SDK v1.0.0")
			fmt.Println("Build: 2025-01-21")
			fmt.Println("Go Version: 1.23.0")
		},
	}

	c.rootCmd.AddCommand(versionCmd)
}

// handleParse handles the parse command
func (c *CLI) handleParse(filename string) error {
	// Read file
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %v", err)
	}

	// Parse code
	result, err := c.sdk.Parse(string(content))
	if err != nil {
		return fmt.Errorf("parse error: %v", err)
	}

	// Display result
	fmt.Printf("Parse successful!\n")
	fmt.Printf("Tokens: %d\n", len(result.Tokens))
	fmt.Printf("AST Nodes: %d\n", len(result.AST))
	
	return nil
}

// handleCompile handles the compile command
func (c *CLI) handleCompile(filename string) error {
	// Read file
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %v", err)
	}

	// Compile code
	result, err := c.sdk.Compile(string(content))
	if err != nil {
		return fmt.Errorf("compile error: %v", err)
	}

	// Display result
	fmt.Printf("Compilation successful!\n")
	fmt.Printf("Binary size: %d bytes\n", len(result.Binary))
	
	return nil
}

// handleExecute handles the execute command
func (c *CLI) handleExecute(filename string) error {
	// Read file
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %v", err)
	}

	// Execute code
	result, err := c.sdk.Execute(string(content))
	if err != nil {
		return fmt.Errorf("execution error: %v", err)
	}

	// Display result
	fmt.Printf("Execution successful!\n")
	fmt.Printf("Output: %s\n", result.Output)
	
	return nil
}

// handleValidate handles the validate command
func (c *CLI) handleValidate(filename string) error {
	// Read file
	content, err := os.ReadFile(filename)
	if err != nil {
		return fmt.Errorf("failed to read file: %v", err)
	}

	// Parse for validation
	_, err = c.sdk.Parse(string(content))
	if err != nil {
		return fmt.Errorf("validation failed: %v", err)
	}

	fmt.Printf("Validation successful!\n")
	fmt.Printf("File: %s\n", filename)
	fmt.Printf("Size: %d bytes\n", len(content))
	
	return nil
} 