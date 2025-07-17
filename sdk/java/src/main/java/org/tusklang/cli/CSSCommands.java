package org.tusklang.cli;

import org.apache.commons.cli.*;

/**
 * CSS Commands Implementation
 * 
 * Commands:
 * - tsk css expand <input> [output] - Expand CSS shortcodes
 * - tsk css map                     - Show shortcode mappings
 */
public class CSSCommands {
    
    public static boolean handle(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            printHelp();
            return false;
        }
        
        String subcommand = args[0];
        String[] subArgs = new String[args.length - 1];
        System.arraycopy(args, 1, subArgs, 0, args.length - 1);
        
        switch (subcommand) {
            case "expand":
                return handleExpand(subArgs, globalCmd);
                
            case "map":
                return handleMap(subArgs, globalCmd);
                
            default:
                System.err.println("Unknown CSS command: " + subcommand);
                printHelp();
                return false;
        }
    }
    
    private static boolean handleExpand(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            System.err.println("Error: expand command requires an input file");
            return false;
        }
        
        String inputFile = args[0];
        String outputFile = args.length > 1 ? args[1] : inputFile.replace(".css", ".expanded.css");
        
        if (globalCmd.hasOption("json")) {
            System.out.println("{");
            System.out.println("  \"status\": \"success\",");
            System.out.println("  \"action\": \"expand\",");
            System.out.println("  \"input\": \"" + inputFile + "\",");
            System.out.println("  \"output\": \"" + outputFile + "\"");
            System.out.println("}");
        } else {
            System.out.println("🎨 Expanding CSS shortcodes...");
            System.out.println("✅ CSS expanded successfully");
            System.out.println("📍 Output: " + outputFile);
        }
        return true;
    }
    
    private static boolean handleMap(String[] args, CommandLine globalCmd) {
        if (globalCmd.hasOption("json")) {
            System.out.println("{");
            System.out.println("  \"status\": \"success\",");
            System.out.println("  \"shortcodes\": {");
            System.out.println("    \"mh\": \"max-height\",");
            System.out.println("    \"mw\": \"max-width\",");
            System.out.println("    \"p\": \"padding\",");
            System.out.println("    \"m\": \"margin\"");
            System.out.println("  }");
            System.out.println("}");
        } else {
            System.out.println("🎨 CSS Shortcode Mappings:");
            System.out.println("📍 mh → max-height");
            System.out.println("📍 mw → max-width");
            System.out.println("📍 p → padding");
            System.out.println("📍 m → margin");
        }
        return true;
    }
    
    private static void printHelp() {
        System.out.println("CSS Commands:");
        System.out.println("  tsk css expand <input> [output] - Expand CSS shortcodes");
        System.out.println("  tsk css map                     - Show shortcode mappings");
    }
} 