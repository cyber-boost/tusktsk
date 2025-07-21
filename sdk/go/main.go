package main

import (
	"fmt"
	"os"

	"github.com/cyber-boost/tusktsk/pkg/cli"
	tusktsk "github.com/cyber-boost/tusktsk/pkg/core"
)

func main() {
	// Create SDK instance
	sdk := tusktsk.New()
	
	// Create CLI instance
	cliApp := cli.New(sdk)
	
	// Run CLI
	if err := cliApp.Run(os.Args); err != nil {
		fmt.Fprintf(os.Stderr, "Error: %v\n", err)
		os.Exit(1)
	}
} 