package org.tusklang;

import org.apache.commons.cli.*;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.io.File;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.Map;

/**
 * Enhanced CLI tool for TuskLang with all features
 * 
 * Usage:
 * java -jar tusklang-java.jar parse config.tsk
 * java -jar tusklang-java.jar validate config.tsk
 * java -jar tusklang-java.jar get config.tsk database.host
 * java -jar tusklang-java.jar set config.tsk database.port 5432
 * java -jar tusklang-java.jar convert config.json output.tsk
 */
public class TuskLangEnhancedCLI {
    
    public static void main(String[] args) {
        Options options = new Options();
        
        options.addOption("h", "help", false, "Show help");
        options.addOption("p", "pretty", false, "Pretty print JSON output");
        options.addOption("v", "verbose", false, "Verbose output");
        options.addOption("e", "enhanced", false, "Use enhanced parser with @ operators");
        options.addOption(Option.builder("D")
            .longOpt("define")
            .hasArg()
            .argName("key=value")
            .desc("Define global variable")
            .build());
        
        CommandLineParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();
        
        try {
            CommandLine cmd = parser.parse(options, args);
            
            if (cmd.hasOption("help") || args.length == 0) {
                printHelp(formatter, options);
                return;
            }
            
            // Get command after options
            String[] remainingArgs = cmd.getArgs();
            if (remainingArgs.length == 0) {
                System.err.println("Error: No command specified");
                printHelp(formatter, options);
                return;
            }
            
            String command = remainingArgs[0];
            boolean useEnhanced = cmd.hasOption("enhanced");
            
            // Process global variables
            if (cmd.hasOption("D")) {
                String[] defines = cmd.getOptionValues("D");
                for (String define : defines) {
                    String[] kv = define.split("=", 2);
                    if (kv.length == 2 && useEnhanced) {
                        // Will be set in enhanced parser
                    }
                }
            }
            
            switch (command) {
                case "parse":
                    if (remainingArgs.length < 2) {
                        System.err.println("Error: parse command requires a file path");
                        return;
                    }
                    parseCommand(remainingArgs[1], cmd.hasOption("pretty"), useEnhanced, cmd);
                    break;
                    
                case "validate":
                    if (remainingArgs.length < 2) {
                        System.err.println("Error: validate command requires a file path");
                        return;
                    }
                    validateCommand(remainingArgs[1], cmd.hasOption("verbose"), useEnhanced);
                    break;
                    
                case "get":
                    if (remainingArgs.length < 3) {
                        System.err.println("Error: get command requires file and key");
                        return;
                    }
                    getCommand(remainingArgs[1], remainingArgs[2], useEnhanced);
                    break;
                    
                case "set":
                    if (remainingArgs.length < 4) {
                        System.err.println("Error: set command requires file, key, and value");
                        return;
                    }
                    setCommand(remainingArgs[1], remainingArgs[2], remainingArgs[3], useEnhanced);
                    break;
                    
                case "convert":
                    if (remainingArgs.length < 3) {
                        System.err.println("Error: convert command requires input and output files");
                        return;
                    }
                    convertCommand(remainingArgs[1], remainingArgs[2], cmd.hasOption("pretty"));
                    break;
                    
                case "version":
                    System.out.println("TuskLang Java SDK v2.0.0");
                    System.out.println("Enhanced parser with @ operators, peanut.tsk support");
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
        System.out.println("TuskLang Java CLI - Enhanced TuskLang parser with @ operators");
        System.out.println();
        System.out.println("Usage:");
        System.out.println("  tusklang parse <file>           Parse and output as JSON");
        System.out.println("  tusklang validate <file>        Validate TuskLang syntax");
        System.out.println("  tusklang get <file> <key>       Get value by key path");
        System.out.println("  tusklang set <file> <key> <val> Set value and save");
        System.out.println("  tusklang convert <in> <out>     Convert between formats");
        System.out.println("  tusklang version                Show version info");
        System.out.println();
        System.out.println("Options:");
        formatter.printHelp("tusklang", options);
        System.out.println();
        System.out.println("Examples:");
        System.out.println("  tusklang parse config.tsk --pretty --enhanced");
        System.out.println("  tusklang get config.tsk database.host");
        System.out.println("  tusklang set config.tsk server.port 8080 --enhanced");
        System.out.println("  tusklang convert config.json config.tsk");
        System.out.println();
        System.out.println("Enhanced Features (--enhanced flag):");
        System.out.println("  - @ operators: @env, @date, @cache, @metrics, @learn, @optimize, @query");
        System.out.println("  - Global variables with $ prefix");
        System.out.println("  - Multiple syntax styles: [], {}, <>");
        System.out.println("  - Cross-file references: @file.tsk.get(\"key\")");
        System.out.println("  - Automatic peanut.tsk loading");
        System.out.println("  - Database queries with JPA");
    }
    
    private static void parseCommand(String filename, boolean pretty, boolean enhanced, CommandLine cmd) {
        try {
            // Check if file exists
            if (!new File(filename).exists()) {
                System.err.println("Error: File not found: " + filename);
                return;
            }
            
            Map<String, Object> config;
            
            if (enhanced) {
                TuskLangEnhanced parser = new TuskLangEnhanced();
                
                // Set global variables
                if (cmd.hasOption("D")) {
                    String[] defines = cmd.getOptionValues("D");
                    for (String define : defines) {
                        String[] kv = define.split("=", 2);
                        if (kv.length == 2) {
                            parser.setGlobalVariable(kv[0], kv[1]);
                        }
                    }
                }
                
                config = parser.parseFile(filename);
            } else {
                TuskLangParser parser = new TuskLangParser();
                config = parser.parseFile(filename);
            }
            
            // Output as JSON
            ObjectMapper mapper = new ObjectMapper();
            String json = pretty ? 
                mapper.writerWithDefaultPrettyPrinter().writeValueAsString(config) :
                mapper.writeValueAsString(config);
            System.out.println(json);
            
        } catch (Exception e) {
            System.err.println("Parse error: " + e.getMessage());
            if (e.getCause() != null) {
                e.getCause().printStackTrace();
            }
            System.exit(1);
        }
    }
    
    private static void validateCommand(String filename, boolean verbose, boolean enhanced) {
        try {
            // Check if file exists
            if (!new File(filename).exists()) {
                System.err.println("Error: File not found: " + filename);
                return;
            }
            
            // Read file content
            String content = new String(Files.readAllBytes(Paths.get(filename)));
            
            // Validate
            boolean valid;
            try {
                if (enhanced) {
                    TuskLangEnhanced parser = new TuskLangEnhanced();
                    parser.parse(content);
                } else {
                    TuskLangParser parser = new TuskLangParser();
                    parser.parse(content);
                }
                valid = true;
            } catch (Exception e) {
                valid = false;
                if (verbose) {
                    System.err.println("Validation error: " + e.getMessage());
                }
            }
            
            if (valid) {
                if (verbose) {
                    System.out.println("✅ File '" + filename + "' is valid TuskLang syntax");
                    if (enhanced) {
                        System.out.println("   Enhanced features validated successfully");
                    }
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
    
    private static void getCommand(String filename, String key, boolean enhanced) {
        try {
            if (!new File(filename).exists()) {
                System.err.println("Error: File not found: " + filename);
                return;
            }
            
            Object value;
            
            if (enhanced) {
                TuskLangEnhanced parser = new TuskLangEnhanced();
                parser.parseFile(filename);
                value = parser.get(key);
            } else {
                TuskLangParser parser = new TuskLangParser();
                Map<String, Object> config = parser.parseFile(filename);
                value = getNestedValue(config, key);
            }
            
            if (value != null) {
                if (value instanceof String || value instanceof Number || value instanceof Boolean) {
                    System.out.println(value);
                } else {
                    ObjectMapper mapper = new ObjectMapper();
                    System.out.println(mapper.writeValueAsString(value));
                }
            } else {
                System.err.println("Key not found: " + key);
                System.exit(1);
            }
            
        } catch (Exception e) {
            System.err.println("Error: " + e.getMessage());
            System.exit(1);
        }
    }
    
    private static void setCommand(String filename, String key, String value, boolean enhanced) {
        try {
            if (!new File(filename).exists()) {
                System.err.println("Error: File not found: " + filename);
                return;
            }
            
            if (enhanced) {
                TuskLangEnhanced parser = new TuskLangEnhanced();
                parser.parseFile(filename);
                
                // Parse the value
                Object parsedValue;
                try {
                    parsedValue = Long.parseLong(value);
                } catch (NumberFormatException e1) {
                    try {
                        parsedValue = Double.parseDouble(value);
                    } catch (NumberFormatException e2) {
                        if ("true".equalsIgnoreCase(value) || "false".equalsIgnoreCase(value)) {
                            parsedValue = Boolean.parseBoolean(value);
                        } else {
                            parsedValue = value;
                        }
                    }
                }
                
                parser.set(key, parsedValue);
                
                // TODO: Serialize back to .tsk format
                System.out.println("✅ Value set successfully");
                System.out.println("Note: Serialization back to .tsk format not yet implemented");
                
            } else {
                System.err.println("Set command requires --enhanced flag");
                System.exit(1);
            }
            
        } catch (Exception e) {
            System.err.println("Error: " + e.getMessage());
            System.exit(1);
        }
    }
    
    private static void convertCommand(String inputFile, String outputFile, boolean pretty) {
        try {
            if (!new File(inputFile).exists()) {
                System.err.println("Error: Input file not found: " + inputFile);
                return;
            }
            
            String inputExt = inputFile.substring(inputFile.lastIndexOf('.') + 1).toLowerCase();
            String outputExt = outputFile.substring(outputFile.lastIndexOf('.') + 1).toLowerCase();
            
            Map<String, Object> config;
            
            // Parse input
            if ("json".equals(inputExt)) {
                // JSON to TSK
                ObjectMapper mapper = new ObjectMapper();
                config = mapper.readValue(new File(inputFile), Map.class);
            } else if ("tsk".equals(inputExt)) {
                // TSK to JSON
                TuskLangEnhanced parser = new TuskLangEnhanced();
                config = parser.parseFile(inputFile);
            } else {
                System.err.println("Unsupported input format: " + inputExt);
                return;
            }
            
            // Write output
            if ("json".equals(outputExt)) {
                ObjectMapper mapper = new ObjectMapper();
                String json = pretty ? 
                    mapper.writerWithDefaultPrettyPrinter().writeValueAsString(config) :
                    mapper.writeValueAsString(config);
                Files.write(Paths.get(outputFile), json.getBytes());
                System.out.println("✅ Converted to JSON: " + outputFile);
            } else if ("tsk".equals(outputExt)) {
                // TODO: Implement TSK serialization
                System.err.println("TSK output format not yet implemented");
            } else {
                System.err.println("Unsupported output format: " + outputExt);
            }
            
        } catch (Exception e) {
            System.err.println("Error: " + e.getMessage());
            System.exit(1);
        }
    }
    
    private static Object getNestedValue(Map<String, Object> map, String key) {
        String[] parts = key.split("\\.");
        Object current = map;
        
        for (String part : parts) {
            if (current instanceof Map) {
                current = ((Map<String, Object>) current).get(part);
            } else {
                return null;
            }
        }
        
        return current;
    }
}