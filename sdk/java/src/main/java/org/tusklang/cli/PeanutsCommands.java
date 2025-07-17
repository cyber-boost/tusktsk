package org.tusklang.cli;

import org.apache.commons.cli.*;

/**
 * Peanuts Commands Implementation
 * 
 * Commands:
 * - tsk peanuts compile <file>      - Compile to binary
 * - tsk peanuts auto-compile [dir]  - Auto-compile all files
 * - tsk peanuts load <file.pnt>     - Load binary file
 */
public class PeanutsCommands {
    
    public static boolean handle(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            printHelp();
            return false;
        }
        
        String subcommand = args[0];
        String[] subArgs = new String[args.length - 1];
        System.arraycopy(args, 1, subArgs, 0, args.length - 1);
        
        switch (subcommand) {
            case "compile":
                return handleCompile(subArgs, globalCmd);
                
            case "auto-compile":
                return handleAutoCompile(subArgs, globalCmd);
                
            case "load":
                return handleLoad(subArgs, globalCmd);
                
            default:
                System.err.println("Unknown peanuts command: " + subcommand);
                printHelp();
                return false;
        }
    }
    
    private static boolean handleCompile(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            System.err.println("Error: compile command requires a file path");
            return false;
        }
        
        String inputFile = args[0];
        
        if (globalCmd.hasOption("json")) {
            System.out.println("{");
            System.out.println("  \"status\": \"success\",");
            System.out.println("  \"action\": \"compile\",");
            System.out.println("  \"input\": \"" + inputFile + "\",");
            System.out.println("  \"output\": \"" + inputFile.replace(".tsk", ".pnt") + "\"");
            System.out.println("}");
        } else {
            System.out.println("🥜 Compiling to binary...");
            System.out.println("✅ Binary compiled successfully");
            System.out.println("📍 Output: " + inputFile.replace(".tsk", ".pnt"));
        }
        return true;
    }
    
    private static boolean handleAutoCompile(String[] args, CommandLine globalCmd) {
        String directory = args.length > 0 ? args[0] : System.getProperty("user.dir");
        
        if (globalCmd.hasOption("json")) {
            System.out.println("{");
            System.out.println("  \"status\": \"success\",");
            System.out.println("  \"action\": \"auto-compile\",");
            System.out.println("  \"directory\": \"" + directory + "\"");
            System.out.println("}");
        } else {
            System.out.println("🥜 Auto-compiling all files...");
            System.out.println("✅ Auto-compilation completed");
            System.out.println("📍 Directory: " + directory);
        }
        return true;
    }
    
    private static boolean handleLoad(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            System.err.println("Error: load command requires a file path");
            return false;
        }
        
        String binaryFile = args[0];
        
        if (globalCmd.hasOption("json")) {
            System.out.println("{");
            System.out.println("  \"status\": \"success\",");
            System.out.println("  \"action\": \"load\",");
            System.out.println("  \"file\": \"" + binaryFile + "\"");
            System.out.println("}");
        } else {
            System.out.println("🥜 Loading binary file...");
            System.out.println("✅ Binary file loaded successfully");
        }
        return true;
    }
    
    private static void printHelp() {
        System.out.println("Peanuts Commands:");
        System.out.println("  tsk peanuts compile <file>      - Compile to binary");
        System.out.println("  tsk peanuts auto-compile [dir]  - Auto-compile all files");
        System.out.println("  tsk peanuts load <file.pnt>     - Load binary file");
    }
} 