package org.tusklang.cli;

import org.apache.commons.cli.*;
import org.tusklang.TuskLangEnhanced;
import org.tusklang.TuskLangParser;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.io.*;
import java.nio.file.*;
import java.util.*;

/**
 * Utility Commands Implementation
 * 
 * Commands:
 * - tsk parse <file>                # Parse and display TSK file contents
 * - tsk validate <file>             # Validate TSK file syntax
 * - tsk convert -i <input> -o <output>  # Convert between formats
 * - tsk get <file> <key.path>       # Get specific value by key path
 * - tsk set <file> <key.path> <value>   # Set value by key path
 */
public class UtilityCommands {
    
    /**
     * tsk parse <file> - Parse and display TSK file contents
     */
    public static boolean handleParse(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            System.err.println("Error: parse command requires a file path");
            return false;
        }
        
        String filename = args[0];
        
        try {
            if (!Files.exists(Paths.get(filename))) {
                System.err.println("Error: File not found: " + filename);
                return false;
            }
            
            TuskLangEnhanced parser = new TuskLangEnhanced();
            Map<String, Object> config = parser.parseFile(filename);
            
            if (globalCmd.hasOption("json")) {
                ObjectMapper mapper = new ObjectMapper();
                String json = globalCmd.hasOption("pretty") ? 
                    mapper.writerWithDefaultPrettyPrinter().writeValueAsString(config) :
                    mapper.writeValueAsString(config);
                System.out.println(json);
            } else {
                System.out.println("📄 Parsed TSK file: " + filename);
                System.out.println("📍 Keys found: " + config.size());
                System.out.println();
                
                if (globalCmd.hasOption("verbose")) {
                    printConfigStructure(config, 0);
                } else {
                    // Show top-level keys
                    for (String key : config.keySet()) {
                        System.out.println("  - " + key);
                    }
                }
            }
            
            return true;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Parse error: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk validate <file> - Validate TSK file syntax
     */
    public static boolean handleValidate(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            System.err.println("Error: validate command requires a file path");
            return false;
        }
        
        String filename = args[0];
        
        try {
            if (!Files.exists(Paths.get(filename))) {
                System.err.println("Error: File not found: " + filename);
                return false;
            }
            
            String content = new String(Files.readAllBytes(Paths.get(filename)));
            TuskLangParser parser = new TuskLangParser();
            boolean isValid = parser.validate(content);
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"" + (isValid ? "valid" : "invalid") + "\",");
                System.out.println("  \"file\": \"" + filename + "\"");
                System.out.println("}");
            } else {
                if (isValid) {
                    System.out.println("✅ File '" + filename + "' is valid TuskLang syntax");
                } else {
                    System.err.println("❌ File '" + filename + "' has invalid TuskLang syntax");
                }
            }
            
            return isValid;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Validation error: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk convert -i <input> -o <output> - Convert between formats
     */
    public static boolean handleConvert(String[] args, CommandLine globalCmd) {
        String inputFile = null;
        String outputFile = null;
        
        // Parse arguments
        for (int i = 0; i < args.length; i++) {
            if ("-i".equals(args[i]) && i + 1 < args.length) {
                inputFile = args[i + 1];
                i++;
            } else if ("-o".equals(args[i]) && i + 1 < args.length) {
                outputFile = args[i + 1];
                i++;
            }
        }
        
        if (inputFile == null || outputFile == null) {
            System.err.println("Error: convert command requires -i <input> and -o <output>");
            return false;
        }
        
        try {
            if (!Files.exists(Paths.get(inputFile))) {
                System.err.println("Error: Input file not found: " + inputFile);
                return false;
            }
            
            String inputExt = getFileExtension(inputFile).toLowerCase();
            String outputExt = getFileExtension(outputFile).toLowerCase();
            
            Map<String, Object> config;
            
            // Parse input
            if ("tsk".equals(inputExt) || "peanuts".equals(inputExt)) {
                TuskLangEnhanced parser = new TuskLangEnhanced();
                config = parser.parseFile(inputFile);
            } else if ("json".equals(inputExt)) {
                ObjectMapper mapper = new ObjectMapper();
                config = mapper.readValue(new File(inputFile), Map.class);
            } else {
                System.err.println("Unsupported input format: " + inputExt);
                return false;
            }
            
            // Write output
            if ("json".equals(outputExt)) {
                ObjectMapper mapper = new ObjectMapper();
                String json = globalCmd.hasOption("pretty") ? 
                    mapper.writerWithDefaultPrettyPrinter().writeValueAsString(config) :
                    mapper.writeValueAsString(config);
                Files.write(Paths.get(outputFile), json.getBytes());
                
                if (globalCmd.hasOption("json")) {
                    System.out.println("{");
                    System.out.println("  \"status\": \"success\",");
                    System.out.println("  \"input\": \"" + inputFile + "\",");
                    System.out.println("  \"output\": \"" + outputFile + "\",");
                    System.out.println("  \"format\": \"json\"");
                    System.out.println("}");
                } else {
                    System.out.println("✅ Converted to JSON: " + outputFile);
                }
                
            } else if ("tsk".equals(outputExt)) {
                String tskContent = convertToTskFormat(config);
                Files.write(Paths.get(outputFile), tskContent.getBytes());
                
                if (globalCmd.hasOption("json")) {
                    System.out.println("{");
                    System.out.println("  \"status\": \"success\",");
                    System.out.println("  \"input\": \"" + inputFile + "\",");
                    System.out.println("  \"output\": \"" + outputFile + "\",");
                    System.out.println("  \"format\": \"tsk\"");
                    System.out.println("}");
                } else {
                    System.out.println("✅ Converted to TSK: " + outputFile);
                }
                
            } else {
                System.err.println("Unsupported output format: " + outputExt);
                return false;
            }
            
            return true;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Conversion error: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk get <file> <key.path> - Get specific value by key path
     */
    public static boolean handleGet(String[] args, CommandLine globalCmd) {
        if (args.length < 2) {
            System.err.println("Error: get command requires file and key path");
            return false;
        }
        
        String filename = args[0];
        String keyPath = args[1];
        
        try {
            if (!Files.exists(Paths.get(filename))) {
                System.err.println("Error: File not found: " + filename);
                return false;
            }
            
            TuskLangEnhanced parser = new TuskLangEnhanced();
            parser.parseFile(filename);
            Object value = parser.get(keyPath);
            
            if (value == null) {
                if (globalCmd.hasOption("json")) {
                    System.out.println("{");
                    System.out.println("  \"status\": \"not_found\",");
                    System.out.println("  \"key\": \"" + keyPath + "\"");
                    System.out.println("}");
                } else {
                    System.out.println("❌ Key not found: " + keyPath);
                }
                return false;
            }
            
            if (globalCmd.hasOption("json")) {
                ObjectMapper mapper = new ObjectMapper();
                System.out.println("{");
                System.out.println("  \"status\": \"success\",");
                System.out.println("  \"key\": \"" + keyPath + "\",");
                System.out.println("  \"value\": " + mapper.writeValueAsString(value));
                System.out.println("}");
            } else {
                if (value instanceof String || value instanceof Number || value instanceof Boolean) {
                    System.out.println(value);
                } else {
                    ObjectMapper mapper = new ObjectMapper();
                    System.out.println(mapper.writeValueAsString(value));
                }
            }
            
            return true;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Error getting value: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk set <file> <key.path> <value> - Set value by key path
     */
    public static boolean handleSet(String[] args, CommandLine globalCmd) {
        if (args.length < 3) {
            System.err.println("Error: set command requires file, key path, and value");
            return false;
        }
        
        String filename = args[0];
        String keyPath = args[1];
        String valueStr = args[2];
        
        try {
            if (!Files.exists(Paths.get(filename))) {
                System.err.println("Error: File not found: " + filename);
                return false;
            }
            
            TuskLangEnhanced parser = new TuskLangEnhanced();
            parser.parseFile(filename);
            
            // Parse the value
            Object value = parseValue(valueStr);
            parser.set(keyPath, value);
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"success\",");
                System.out.println("  \"key\": \"" + keyPath + "\",");
                System.out.println("  \"value\": " + formatJsonValue(value));
                System.out.println("}");
            } else {
                System.out.println("✅ Value set successfully");
                System.out.println("📍 Key: " + keyPath);
                System.out.println("📍 Value: " + value);
            }
            
            return true;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Error setting value: " + e.getMessage());
            }
            return false;
        }
    }
    
    // Helper methods
    
    private static void printConfigStructure(Map<String, Object> config, int depth) {
        String indent = "  ".repeat(depth);
        
        for (Map.Entry<String, Object> entry : config.entrySet()) {
            String key = entry.getKey();
            Object value = entry.getValue();
            
            if (value instanceof Map) {
                System.out.println(indent + key + ":");
                @SuppressWarnings("unchecked")
                Map<String, Object> nested = (Map<String, Object>) value;
                printConfigStructure(nested, depth + 1);
            } else if (value instanceof List) {
                System.out.println(indent + key + ": [" + ((List<?>) value).size() + " items]");
            } else {
                System.out.println(indent + key + ": " + value);
            }
        }
    }
    
    private static String getFileExtension(String filename) {
        int lastDot = filename.lastIndexOf('.');
        return lastDot > 0 ? filename.substring(lastDot + 1) : "";
    }
    
    private static String convertToTskFormat(Map<String, Object> config) {
        StringBuilder sb = new StringBuilder();
        sb.append("# Converted Configuration\n");
        sb.append("# Generated at: ").append(new java.util.Date()).append("\n\n");
        
        convertMapToTsk(config, sb, 0);
        
        return sb.toString();
    }
    
    private static void convertMapToTsk(Object value, StringBuilder sb, int depth) {
        String indent = "  ".repeat(depth);
        
        if (value instanceof Map) {
            @SuppressWarnings("unchecked")
            Map<String, Object> map = (Map<String, Object>) value;
            
            for (Map.Entry<String, Object> entry : map.entrySet()) {
                String key = entry.getKey();
                Object val = entry.getValue();
                
                if (val instanceof Map || val instanceof List) {
                    sb.append(indent).append(key).append(":\n");
                    convertMapToTsk(val, sb, depth + 1);
                } else {
                    sb.append(indent).append(key).append(": ");
                    if (val instanceof String) {
                        sb.append("\"").append(val).append("\"");
                    } else {
                        sb.append(val);
                    }
                    sb.append("\n");
                }
            }
        } else if (value instanceof List) {
            @SuppressWarnings("unchecked")
            List<Object> list = (List<Object>) value;
            
            for (Object item : list) {
                sb.append(indent).append("- ");
                if (item instanceof String) {
                    sb.append("\"").append(item).append("\"");
                } else {
                    sb.append(item);
                }
                sb.append("\n");
            }
        }
    }
    
    private static Object parseValue(String valueStr) {
        // Try to parse as number
        try {
            if (valueStr.contains(".")) {
                return Double.parseDouble(valueStr);
            } else {
                return Long.parseLong(valueStr);
            }
        } catch (NumberFormatException e) {
            // Not a number
        }
        
        // Try to parse as boolean
        if ("true".equalsIgnoreCase(valueStr)) {
            return true;
        }
        if ("false".equalsIgnoreCase(valueStr)) {
            return false;
        }
        
        // Check for null
        if ("null".equalsIgnoreCase(valueStr)) {
            return null;
        }
        
        // Return as string
        return valueStr;
    }
    
    private static String formatJsonValue(Object value) {
        if (value == null) {
            return "null";
        } else if (value instanceof String) {
            return "\"" + value + "\"";
        } else if (value instanceof Number || value instanceof Boolean) {
            return value.toString();
        } else {
            return "\"" + value.toString() + "\"";
        }
    }
} 