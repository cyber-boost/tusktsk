package org.tusklang;

import org.apache.commons.cli.*;

import java.io.File;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.Map;

/**
 * Simple CLI tool for TuskLang
 * 
 * Usage:
 * java -jar tusklang-java.jar parse config.tsk
 * java -jar tusklang-java.jar validate config.tsk
 */
public class TuskLangCLI {
    
    public static void main(String[] args) {
        Options options = new Options();
        
        options.addOption("h", "help", false, "Show help");
        options.addOption("p", "pretty", false, "Pretty print JSON output");
        options.addOption("v", "verbose", false, "Verbose output");
        
        CommandLineParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();
        
        try {
            CommandLine cmd = parser.parse(options, args);
            
            if (cmd.hasOption("help") || args.length == 0) {
                printHelp(formatter, options);
                return;
            }
            
            String command = args[0];
            
            switch (command) {
                case "parse":
                    if (args.length < 2) {
                        System.err.println("Error: parse command requires a file path");
                        printHelp(formatter, options);
                        return;
                    }
                    parseCommand(args[1], cmd.hasOption("pretty"));
                    break;
                    
                case "validate":
                    if (args.length < 2) {
                        System.err.println("Error: validate command requires a file path");
                        printHelp(formatter, options);
                        return;
                    }
                    validateCommand(args[1], cmd.hasOption("verbose"));
                    break;
                    
                default:
                    System.err.println("Unknown command: " + command);
                    printHelp(formatter, options);
                    break;
            }
            
        } catch (ParseException e) {
            System.err.println("Error parsing arguments: " + e.getMessage());
            printHelp(formatter, options);
        }
    }
    
    private static void printHelp(HelpFormatter formatter, Options options) {
        System.out.println("TuskLang Java CLI - Simple TuskLang parser");
        System.out.println();
        System.out.println("Usage:");
        System.out.println("  java -jar tusklang-java.jar parse <file>     Parse and output as JSON");
        System.out.println("  java -jar tusklang-java.jar validate <file>  Validate TuskLang syntax");
        System.out.println();
        System.out.println("Options:");
        formatter.printHelp("tusklang-java", options);
        System.out.println();
        System.out.println("Examples:");
        System.out.println("  java -jar tusklang-java.jar parse config.tsk --pretty");
        System.out.println("  java -jar tusklang-java.jar validate config.tsk --verbose");
    }
    
    private static void parseCommand(String filename, boolean pretty) {
        try {
            TuskLangParser parser = new TuskLangParser();
            
            // Check if file exists
            if (!new File(filename).exists()) {
                System.err.println("Error: File not found: " + filename);
                return;
            }
            
            // Parse the file
            Map<String, Object> config = parser.parseFile(filename);
            
            // Output as JSON
            String json = pretty ? parser.toPrettyJson(config) : parser.toJson(config);
            System.out.println(json);
            
        } catch (TuskLangException e) {
            System.err.println("Parse error: " + e.getMessage());
            if (e.getCause() != null) {
                System.err.println("Cause: " + e.getCause().getMessage());
                e.getCause().printStackTrace();
            }
            System.exit(1);
        } catch (Exception e) {
            System.err.println("Unexpected error: " + e.getMessage());
            e.printStackTrace();
            System.exit(1);
        }
    }
    
    private static void validateCommand(String filename, boolean verbose) {
        try {
            TuskLangParser parser = new TuskLangParser();
            
            // Check if file exists
            if (!new File(filename).exists()) {
                System.err.println("Error: File not found: " + filename);
                return;
            }
            
            // Read file content
            String content = new String(Files.readAllBytes(Paths.get(filename)));
            
            // Validate
            if (parser.validate(content)) {
                if (verbose) {
                    System.out.println("✅ File '" + filename + "' is valid TuskLang syntax");
                } else {
                    System.out.println("✅ Valid");
                }
            } else {
                if (verbose) {
                    System.err.println("❌ File '" + filename + "' has invalid TuskLang syntax");
                } else {
                    System.err.println("❌ Invalid");
                }
                System.exit(1);
            }
            
        } catch (Exception e) {
            System.err.println("Error: " + e.getMessage());
            System.exit(1);
        }
    }
} 