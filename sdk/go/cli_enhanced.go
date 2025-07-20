package main

import (
	"encoding/json"
	"fmt"
	"os"
	
)

func main() {
	if len(os.Args) < 2 {
		showHelp()
		return
	}
	
	command := os.Args[1]
	
	switch command {
	case "parse":
		if len(os.Args) < 3 {
			fmt.Println("Error: File path required")
			os.Exit(1)
		}
		
		parser := NewEnhancedParser()
		err := parser.ParseFile(os.Args[2])
		if err != nil {
			fmt.Printf("Error parsing file: %v\n", err)
			os.Exit(1)
		}
		
		for _, key := range parser.Keys() {
			value := parser.Get(key)
			fmt.Printf("%s = %v\n", key, value)
		}
		
	case "get":
		if len(os.Args) < 4 {
			fmt.Println("Error: File path and key required")
			os.Exit(1)
		}
		
		parser := NewEnhancedParser()
		err := parser.ParseFile(os.Args[2])
		if err != nil {
			fmt.Printf("Error parsing file: %v\n", err)
			os.Exit(1)
		}
		
		value := parser.Get(os.Args[3])
		if value != nil {
			fmt.Printf("%v\n", value)
		}
		
	case "keys":
		if len(os.Args) < 3 {
			fmt.Println("Error: File path required")
			os.Exit(1)
		}
		
		parser := NewEnhancedParser()
		err := parser.ParseFile(os.Args[2])
		if err != nil {
			fmt.Printf("Error parsing file: %v\n", err)
			os.Exit(1)
		}
		
		for _, key := range parser.Keys() {
			fmt.Println(key)
		}
		
	case "peanut":
		parser := NewEnhancedParser()LoadFromPeanut()
		fmt.Printf("Loaded %d configuration items\n", len(parser.Items()))
		
		for _, key := range parser.Keys() {
			value := parser.Get(key)
			fmt.Printf("%s = %v\n", key, value)
		}
		
	case "json":
		if len(os.Args) < 3 {
			fmt.Println("Error: File path required")
			os.Exit(1)
		}
		
		parser := NewEnhancedParser()
		err := parser.ParseFile(os.Args[2])
		if err != nil {
			fmt.Printf("Error parsing file: %v\n", err)
			os.Exit(1)
		}
		
		jsonData, err := json.MarshalIndent(parser.Items(), "", "  ")
		if err != nil {
			fmt.Printf("Error converting to JSON: %v\n", err)
			os.Exit(1)
		}
		
		fmt.Println(string(jsonData))
		
	case "validate":
		if len(os.Args) < 3 {
			fmt.Println("Error: File path required")
			os.Exit(1)
		}
		
		parser := NewEnhancedParser()
		err := parser.ParseFile(os.Args[2])
		if err != nil {
			fmt.Printf("❌ Validation failed: %v\n", err)
			os.Exit(1)
		}
		
		fmt.Println("✅ File is valid TuskLang syntax")
		
	default:
		fmt.Printf("Error: Unknown command: %s\n", command)
		showHelp()
		os.Exit(1)
	}
}

func showHelp() {
	fmt.Println(`
TuskLang Enhanced for Go - The Freedom Parser
============================================

Usage: tsk-sdk [command] [options]

Commands:
    parse <file>     Parse a .tsk file and show all key-value pairs
    get <file> <key> Get a specific value by key
    keys <file>      List all keys in the file
    json <file>      Convert .tsk file to JSON format
    validate <file>  Validate .tsk file syntax
    peanut           Load configuration from peanut.tsk
    
Examples:
    tsk-sdk parse config.tsk
    tsk-sdk get config.tsk database.host
    tsk-sdk keys config.tsk
    tsk-sdk json config.tsk
    tsk-sdk validate config.tsk
    tsk-sdk peanut

Features:
    - Multiple syntax styles: [], {}, <>
    - Global variables with $
    - Cross-file references: @file.tsk.get()
    - Database queries: @query()
    - Date functions: @date()
    - Environment variables: @env()
    - Conditional expressions (ternary operator)
    - Range syntax: 8000-9000
    - String concatenation with +
    - Optional semicolons

Default config file: peanut.tsk
"We don't bow to any king" - Maximum syntax flexibility
`)
}