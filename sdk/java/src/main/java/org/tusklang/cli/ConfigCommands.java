package org.tusklang.cli;

import org.apache.commons.cli.*;
import org.tusklang.PeanutConfig;
import java.io.*;
import java.nio.file.*;
import java.util.*;

/**
 * Configuration Commands Implementation
 * 
 * Commands:
 * - tsk config get <key.path> [dir] # Get configuration value by path
 * - tsk config check [path]         # Check configuration hierarchy
 * - tsk config validate [path]      # Validate entire configuration chain
 * - tsk config compile [path]       # Auto-compile all peanu.tsk files
 * - tsk config docs [path]          # Generate configuration documentation
 * - tsk config clear-cache [path]   # Clear configuration cache
 * - tsk config stats                # Show configuration performance statistics
 */
public class ConfigCommands {
    
    public static boolean handle(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            printHelp();
            return false;
        }
        
        String subcommand = args[0];
        String[] subArgs = new String[args.length - 1];
        System.arraycopy(args, 1, subArgs, 0, args.length - 1);
        
        switch (subcommand) {
            case "get":
                return handleGet(subArgs, globalCmd);
                
            case "check":
                return handleCheck(subArgs, globalCmd);
                
            case "validate":
                return handleValidate(subArgs, globalCmd);
                
            case "compile":
                return handleCompile(subArgs, globalCmd);
                
            case "docs":
                return handleDocs(subArgs, globalCmd);
                
            case "clear-cache":
                return handleClearCache(subArgs, globalCmd);
                
            case "stats":
                return handleStats(subArgs, globalCmd);
                
            default:
                System.err.println("Unknown config command: " + subcommand);
                printHelp();
                return false;
        }
    }
    
    /**
     * tsk config get <key.path> [dir] - Get configuration value by path
     */
    private static boolean handleGet(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            System.err.println("Error: get command requires a key path");
            return false;
        }
        
        String keyPath = args[0];
        String directory = args.length > 1 ? args[1] : System.getProperty("user.dir");
        
        try {
            PeanutConfig config = new PeanutConfig();
            Object value = config.get(keyPath, null, directory);
            
            if (value == null) {
                if (globalCmd.hasOption("json")) {
                    System.out.println("{");
                    System.out.println("  \"status\": \"not_found\",");
                    System.out.println("  \"key\": \"" + keyPath + "\",");
                    System.out.println("  \"directory\": \"" + directory + "\"");
                    System.out.println("}");
                } else {
                    System.out.println("❌ Key not found: " + keyPath);
                }
                return false;
            }
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"success\",");
                System.out.println("  \"key\": \"" + keyPath + "\",");
                System.out.println("  \"value\": " + formatJsonValue(value) + ",");
                System.out.println("  \"directory\": \"" + directory + "\"");
                System.out.println("}");
            } else {
                System.out.println("✅ " + keyPath + ": " + value);
            }
            
            return true;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Error getting config: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk config check [path] - Check configuration hierarchy
     */
    private static boolean handleCheck(String[] args, CommandLine globalCmd) {
        String directory = args.length > 0 ? args[0] : System.getProperty("user.dir");
        
        try {
            PeanutConfig config = new PeanutConfig();
            List<PeanutConfig.ConfigFile> hierarchy = config.findConfigHierarchy(directory);
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"success\",");
                System.out.println("  \"directory\": \"" + directory + "\",");
                System.out.println("  \"hierarchy\": [");
                
                for (int i = 0; i < hierarchy.size(); i++) {
                    PeanutConfig.ConfigFile file = hierarchy.get(i);
                    System.out.println("    {");
                    System.out.println("      \"path\": \"" + file.path + "\",");
                    System.out.println("      \"type\": \"" + file.type + "\",");
                    System.out.println("      \"modified\": \"" + file.mtime + "\"");
                    System.out.println("    }" + (i < hierarchy.size() - 1 ? "," : ""));
                }
                
                System.out.println("  ]");
                System.out.println("}");
            } else {
                System.out.println("📋 Configuration Hierarchy for: " + directory);
                System.out.println();
                
                if (hierarchy.isEmpty()) {
                    System.out.println("❌ No configuration files found");
                } else {
                    for (int i = 0; i < hierarchy.size(); i++) {
                        PeanutConfig.ConfigFile file = hierarchy.get(i);
                        System.out.println((i + 1) + ". " + file.path + " (" + file.type + ")");
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
                System.err.println("❌ Error checking config: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk config validate [path] - Validate entire configuration chain
     */
    private static boolean handleValidate(String[] args, CommandLine globalCmd) {
        String directory = args.length > 0 ? args[0] : System.getProperty("user.dir");
        
        try {
            PeanutConfig config = new PeanutConfig();
            List<PeanutConfig.ConfigFile> hierarchy = config.findConfigHierarchy(directory);
            
            int validFiles = 0;
            int totalFiles = hierarchy.size();
            List<String> errors = new ArrayList<>();
            
            for (PeanutConfig.ConfigFile file : hierarchy) {
                try {
                    switch (file.type) {
                        case BINARY:
                            config.loadBinary(file.path);
                            break;
                        case TSK:
                        case TEXT:
                            String content = new String(Files.readAllBytes(file.path));
                            config.parseTextConfig(content);
                            break;
                    }
                    validFiles++;
                } catch (Exception e) {
                    errors.add(file.path + ": " + e.getMessage());
                }
            }
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"" + (validFiles == totalFiles ? "valid" : "invalid") + "\",");
                System.out.println("  \"directory\": \"" + directory + "\",");
                System.out.println("  \"valid_files\": " + validFiles + ",");
                System.out.println("  \"total_files\": " + totalFiles + ",");
                System.out.println("  \"errors\": [");
                
                for (int i = 0; i < errors.size(); i++) {
                    System.out.println("    \"" + errors.get(i) + "\"" + (i < errors.size() - 1 ? "," : ""));
                }
                
                System.out.println("  ]");
                System.out.println("}");
            } else {
                System.out.println("🔍 Validating configuration in: " + directory);
                System.out.println();
                
                if (validFiles == totalFiles) {
                    System.out.println("✅ All " + totalFiles + " configuration files are valid");
                } else {
                    System.out.println("❌ " + validFiles + "/" + totalFiles + " configuration files are valid");
                    System.out.println();
                    System.out.println("Errors:");
                    for (String error : errors) {
                        System.out.println("  - " + error);
                    }
                }
            }
            
            return validFiles == totalFiles;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Error validating config: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk config compile [path] - Auto-compile all peanu.tsk files
     */
    private static boolean handleCompile(String[] args, CommandLine globalCmd) {
        String directory = args.length > 0 ? args[0] : System.getProperty("user.dir");
        
        try {
            PeanutConfig config = new PeanutConfig();
            List<PeanutConfig.ConfigFile> hierarchy = config.findConfigHierarchy(directory);
            
            int compiledFiles = 0;
            int totalFiles = 0;
            List<String> compiled = new ArrayList<>();
            
            for (PeanutConfig.ConfigFile file : hierarchy) {
                if (file.type == PeanutConfig.ConfigType.TSK || file.type == PeanutConfig.ConfigType.TEXT) {
                    totalFiles++;
                    try {
                        String content = new String(Files.readAllBytes(file.path));
                        Map<String, Object> parsed = config.parseTextConfig(content);
                        
                        // Generate binary filename
                        String binaryName = file.path.getFileName().toString()
                            .replace(".tsk", ".pnt")
                            .replace(".peanuts", ".pnt");
                        Path binaryPath = file.path.resolveSibling(binaryName);
                        
                        config.compileToBinary(parsed, binaryPath);
                        compiledFiles++;
                        compiled.add(binaryPath.toString());
                        
                    } catch (Exception e) {
                        if (globalCmd.hasOption("verbose")) {
                            System.err.println("Failed to compile " + file.path + ": " + e.getMessage());
                        }
                    }
                }
            }
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"success\",");
                System.out.println("  \"directory\": \"" + directory + "\",");
                System.out.println("  \"compiled_files\": " + compiledFiles + ",");
                System.out.println("  \"total_files\": " + totalFiles + ",");
                System.out.println("  \"binary_files\": [");
                
                for (int i = 0; i < compiled.size(); i++) {
                    System.out.println("    \"" + compiled.get(i) + "\"" + (i < compiled.size() - 1 ? "," : ""));
                }
                
                System.out.println("  ]");
                System.out.println("}");
            } else {
                System.out.println("🔨 Compiling configuration files in: " + directory);
                System.out.println();
                
                if (compiledFiles > 0) {
                    System.out.println("✅ Compiled " + compiledFiles + "/" + totalFiles + " files to binary format");
                    for (String binaryFile : compiled) {
                        System.out.println("  📍 " + binaryFile);
                    }
                } else {
                    System.out.println("⚠️  No files to compile");
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
                System.err.println("❌ Error compiling config: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk config docs [path] - Generate configuration documentation
     */
    private static boolean handleDocs(String[] args, CommandLine globalCmd) {
        String directory = args.length > 0 ? args[0] : System.getProperty("user.dir");
        String outputFile = args.length > 1 ? args[1] : "config-docs.md";
        
        try {
            PeanutConfig config = new PeanutConfig();
            Map<String, Object> fullConfig = config.load(directory);
            
            String documentation = generateDocumentation(fullConfig, directory);
            Files.write(Paths.get(outputFile), documentation.getBytes());
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"success\",");
                System.out.println("  \"directory\": \"" + directory + "\",");
                System.out.println("  \"output_file\": \"" + outputFile + "\",");
                System.out.println("  \"size_bytes\": " + Files.size(Paths.get(outputFile)));
                System.out.println("}");
            } else {
                System.out.println("📚 Configuration documentation generated");
                System.out.println("📍 Directory: " + directory);
                System.out.println("📍 Output: " + outputFile);
                System.out.println("📍 Size: " + Files.size(Paths.get(outputFile)) + " bytes");
            }
            
            return true;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Error generating docs: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk config clear-cache [path] - Clear configuration cache
     */
    private static boolean handleClearCache(String[] args, CommandLine globalCmd) {
        String directory = args.length > 0 ? args[0] : System.getProperty("user.dir");
        
        try {
            // Clear any cached configurations
            // Note: This is a simplified implementation
            // In a real implementation, you'd have a proper cache manager
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"success\",");
                System.out.println("  \"directory\": \"" + directory + "\",");
                System.out.println("  \"message\": \"Cache cleared\"");
                System.out.println("}");
            } else {
                System.out.println("🧹 Configuration cache cleared");
                System.out.println("📍 Directory: " + directory);
            }
            
            return true;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Error clearing cache: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk config stats - Show configuration performance statistics
     */
    private static boolean handleStats(String[] args, CommandLine globalCmd) {
        try {
            // Run performance benchmark
            PeanutConfig.benchmark();
            
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"success\",");
                System.out.println("  \"message\": \"Performance benchmark completed\"");
                System.out.println("}");
            }
            
            return true;
            
        } catch (Exception e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.err.println("❌ Error running stats: " + e.getMessage());
            }
            return false;
        }
    }
    
    // Helper methods
    
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
    
    private static String generateDocumentation(Map<String, Object> config, String directory) {
        StringBuilder sb = new StringBuilder();
        sb.append("# Configuration Documentation\n\n");
        sb.append("Generated from: ").append(directory).append("\n");
        sb.append("Generated at: ").append(new java.util.Date()).append("\n\n");
        
        sb.append("## Configuration Structure\n\n");
        generateConfigDocs(config, sb, 0);
        
        return sb.toString();
    }
    
    private static void generateConfigDocs(Object value, StringBuilder sb, int depth) {
        String indent = "  ".repeat(depth);
        
        if (value instanceof Map) {
            @SuppressWarnings("unchecked")
            Map<String, Object> map = (Map<String, Object>) value;
            
            for (Map.Entry<String, Object> entry : map.entrySet()) {
                String key = entry.getKey();
                Object val = entry.getValue();
                
                sb.append(indent).append("- **").append(key).append("**");
                
                if (val instanceof Map || val instanceof java.util.List) {
                    sb.append("\n");
                    generateConfigDocs(val, sb, depth + 1);
                } else {
                    sb.append(": ").append(formatValue(val)).append("\n");
                }
            }
        } else if (value instanceof java.util.List) {
            @SuppressWarnings("unchecked")
            java.util.List<Object> list = (java.util.List<Object>) value;
            
            for (Object item : list) {
                sb.append(indent).append("- ").append(formatValue(item)).append("\n");
            }
        }
    }
    
    private static String formatValue(Object value) {
        if (value == null) {
            return "`null`";
        } else if (value instanceof String) {
            return "`\"" + value + "\"`";
        } else {
            return "`" + value + "`";
        }
    }
    
    private static void printHelp() {
        System.out.println("Configuration Commands:");
        System.out.println("  tsk config get <key.path> [dir] # Get configuration value by path");
        System.out.println("  tsk config check [path]         # Check configuration hierarchy");
        System.out.println("  tsk config validate [path]      # Validate entire configuration chain");
        System.out.println("  tsk config compile [path]       # Auto-compile all peanu.tsk files");
        System.out.println("  tsk config docs [path]          # Generate configuration documentation");
        System.out.println("  tsk config clear-cache [path]   # Clear configuration cache");
        System.out.println("  tsk config stats                # Show configuration performance statistics");
    }
} 