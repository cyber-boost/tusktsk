package main

import (
	"fmt"
	"os"

	"github.com/google/licensecheck"
)

func main() {
	// Read our LICENSE file
	data, err := os.ReadFile("LICENSE")
	if err != nil {
		fmt.Printf("Error reading LICENSE file: %v\n", err)
		os.Exit(1)
	}

	// Use Google's licensecheck to scan our license
	cov := licensecheck.Scan(data)
	
	fmt.Printf("License Detection Results:\n")
	fmt.Printf("========================\n")
	fmt.Printf("Coverage: %.1f%%\n", cov.Percent)
	fmt.Printf("Matches: %d\n", len(cov.Match))
	
	for i, match := range cov.Match {
		fmt.Printf("\nMatch %d:\n", i+1)
		fmt.Printf("  ID: %s\n", match.ID)
		fmt.Printf("  Type: %s\n", match.Type)
		fmt.Printf("  Start: %d\n", match.Start)
		fmt.Printf("  End: %d\n", match.End)
		fmt.Printf("  IsURL: %v\n", match.IsURL)
	}
	
	// Check if we have good coverage
	if cov.Percent >= 80.0 {
		fmt.Printf("\n✅ LICENSE DETECTED: %.1f%% coverage\n", cov.Percent)
	} else {
		fmt.Printf("\n❌ LICENSE NOT DETECTED: Only %.1f%% coverage\n", cov.Percent)
	}
} 