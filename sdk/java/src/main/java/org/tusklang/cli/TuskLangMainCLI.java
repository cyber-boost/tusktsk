package org.tusklang.cli;

import org.apache.commons.cli.*;
import java.util.Arrays;
import org.tusklang.cli.*;

/**
 * Main TuskLang CLI - Universal Command Implementation
 * 
 * Implements ALL commands from the Universal CLI Command Specification
 * 
 * Usage:
 * tsk [global-options] <command> [command-options] [arguments]
 */
public class TuskLangMainCLI {
    private static final Options globalOptions = new Options();
    
    public static void main(String[] args) {
        // Handle global options first
        globalOptions.addOption("h", "help", false, "Show help for any command");
        globalOptions.addOption("v", "version", false, "Show version information");
        globalOptions.addOption("V", "verbose", false, "Enable verbose output");
        globalOptions.addOption("q", "quiet", false, "Suppress non-error output");
        globalOptions.addOption("c", "config", true, "Use alternate config file");
        globalOptions.addOption("j", "json", false, "Output in JSON format");
        
        CommandLineParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();
        CommandLine globalCmd = null;
        
        try {
            // Parse global options
            globalCmd = parser.parse(globalOptions, args, true);
            
            // Show version
            if (globalCmd.hasOption("version")) {
                System.out.println("TuskLang Java SDK v2.0.0");
                System.out.println("Strong. Secure. Scalable.");
                return;
            }
            
            // Get remaining arguments for command
            String[] remainingArgs = globalCmd.getArgs();
            
            if (remainingArgs.length == 0) {
                // Enter interactive mode
                interactiveMode();
                return;
            }
            
            String command = remainingArgs[0];
            String[] commandArgs = Arrays.copyOfRange(remainingArgs, 1, remainingArgs.length);
            
            // Route to appropriate command handler
            boolean success = routeCommand(command, commandArgs, globalCmd);
            
            if (!success) {
                System.err.println("Unknown command: " + command);
                printGlobalHelp(formatter, globalOptions);
                System.exit(1);
            }
            
        } catch (ParseException e) {
            System.err.println("Error parsing arguments: " + e.getMessage());
            printGlobalHelp(formatter, globalOptions);
            System.exit(1);
        } catch (Exception e) {
            System.err.println("Unexpected error: " + e.getMessage());
            if (globalCmd != null && globalCmd.hasOption("verbose")) {
                e.printStackTrace();
            }
            System.exit(1);
        }
    }
    
    /**
     * Route command to appropriate handler
     */
    private static boolean routeCommand(String command, String[] args, CommandLine globalCmd) {
        switch (command) {
            case "db":
                return DatabaseCommands.handle(args, globalCmd);
                
            case "serve":
                return DevelopmentCommands.handleServe(args, globalCmd);
                
            case "compile":
                return DevelopmentCommands.handleCompile(args, globalCmd);
                
            case "optimize":
                return DevelopmentCommands.handleOptimize(args, globalCmd);
                
            case "test":
                return TestingCommands.handle(args, globalCmd);
                
            case "services":
                return ServiceCommands.handle(args, globalCmd);
                
            case "init":
                return ProjectCommands.handleInit(args, globalCmd);
                
            case "migrate":
                return ProjectCommands.handleMigrate(args, globalCmd);
                
            case "cache":
                return CacheCommands.handle(args, globalCmd);
                
            case "license":
                return LicenseCommands.handle(args, globalCmd);
                
            case "config":
                return ConfigCommands.handle(args, globalCmd);
                
            case "binary":
                return BinaryCommands.handle(args, globalCmd);
                
            case "peanuts":
                return PeanutsCommands.handle(args, globalCmd);
                
            case "css":
                return CSSCommands.handle(args, globalCmd);
                
            case "ai":
                return AICommands.handle(args, globalCmd);
                
            case "parse":
                return UtilityCommands.handleParse(args, globalCmd);
                
            case "validate":
                return UtilityCommands.handleValidate(args, globalCmd);
                
            case "convert":
                return UtilityCommands.handleConvert(args, globalCmd);
                
            case "get":
                return UtilityCommands.handleGet(args, globalCmd);
                
            case "set":
                return UtilityCommands.handleSet(args, globalCmd);
                
            case "help":
                printGlobalHelp(new HelpFormatter(), globalOptions);
                return true;
                
            default:
                return false;
        }
    }
    
    /**
     * Interactive REPL mode
     */
    private static void interactiveMode() {
        System.out.println("TuskLang v2.0.0 - Interactive Mode");
        System.out.println("Type 'help' for commands, 'exit' to quit");
        System.out.println();
        
        java.util.Scanner scanner = new java.util.Scanner(System.in);
        
        while (true) {
            System.out.print("tsk> ");
            String input = scanner.nextLine().trim();
            
            if (input.isEmpty()) {
                continue;
            }
            
            if ("exit".equalsIgnoreCase(input) || "quit".equalsIgnoreCase(input)) {
                break;
            }
            
            if ("help".equalsIgnoreCase(input)) {
                printInteractiveHelp();
                continue;
            }
            
            // Parse and execute command
            try {
                String[] args = input.split("\\s+");
                String command = args[0];
                String[] commandArgs = Arrays.copyOfRange(args, 1, args.length);
                
                Options globalOptions = new Options();
                CommandLine globalCmd = new DefaultParser().parse(globalOptions, new String[0]);
                
                boolean success = routeCommand(command, commandArgs, globalCmd);
                
                if (!success) {
                    System.out.println("Unknown command: " + command);
                    System.out.println("Type 'help' for available commands");
                }
                
            } catch (Exception e) {
                System.out.println("Error: " + e.getMessage());
            }
        }
        
        scanner.close();
        System.out.println("Goodbye!");
    }
    
    /**
     * Print interactive help
     */
    private static void printInteractiveHelp() {
        System.out.println("Available commands:");
        System.out.println("  db status                    - Check database connection");
        System.out.println("  db migrate <file>           - Run migration file");
        System.out.println("  serve [port]                - Start development server");
        System.out.println("  compile <file>              - Compile .tsk file");
        System.out.println("  test [suite]                - Run test suites");
        System.out.println("  config get <key.path>       - Get configuration value");
        System.out.println("  parse <file>                - Parse TSK file");
        System.out.println("  validate <file>             - Validate syntax");
        System.out.println("  help                        - Show this help");
        System.out.println("  exit                        - Exit interactive mode");
    }
    
    /**
     * Print global help
     */
    private static void printGlobalHelp(HelpFormatter formatter, Options options) {
        System.out.println("🐘 TuskLang Java SDK - Universal CLI");
        System.out.println("Strong. Secure. Scalable.");
        System.out.println();
        System.out.println("Usage: tsk [global-options] <command> [command-options] [arguments]");
        System.out.println();
        System.out.println("Commands:");
        System.out.println("  🗄️  Database:");
        System.out.println("    db status                    - Check database connection");
        System.out.println("    db migrate <file>           - Run migration file");
        System.out.println("    db console                  - Interactive database console");
        System.out.println("    db backup [file]            - Backup database");
        System.out.println("    db restore <file>           - Restore from backup");
        System.out.println("    db init                     - Initialize SQLite database");
        System.out.println();
        System.out.println("  🔧 Development:");
        System.out.println("    serve [port]                - Start development server");
        System.out.println("    compile <file>              - Compile .tsk file");
        System.out.println("    optimize <file>             - Optimize .tsk file");
        System.out.println();
        System.out.println("  🧪 Testing:");
        System.out.println("    test [suite]                - Run test suites");
        System.out.println("    test all                    - Run all tests");
        System.out.println("    test parser                 - Test parser only");
        System.out.println("    test fujsen                 - Test FUJSEN only");
        System.out.println("    test sdk                    - Test SDK only");
        System.out.println("    test performance            - Performance tests");
        System.out.println();
        System.out.println("  ⚙️  Services:");
        System.out.println("    services start              - Start all services");
        System.out.println("    services stop               - Stop all services");
        System.out.println("    services restart            - Restart services");
        System.out.println("    services status             - Show service status");
        System.out.println();
        System.out.println("  📦 Project:");
        System.out.println("    init [project-name]         - Initialize new project");
        System.out.println("    migrate --from=<format>     - Migrate from other formats");
        System.out.println();
        System.out.println("  🏃 Cache:");
        System.out.println("    cache clear                 - Clear all caches");
        System.out.println("    cache status                - Show cache status");
        System.out.println("    cache warm                  - Pre-warm caches");
        System.out.println("    cache memcached [subcommand] - Memcached operations");
        System.out.println();
        System.out.println("  🔐 License:");
        System.out.println("    license check               - Check license status");
        System.out.println("    license activate <key>      - Activate license");
        System.out.println();
        System.out.println("  🥜 Configuration:");
        System.out.println("    config get <key.path>       - Get configuration value");
        System.out.println("    config check [path]         - Check hierarchy");
        System.out.println("    config validate [path]      - Validate configuration");
        System.out.println("    config compile [path]       - Auto-compile configs");
        System.out.println("    config docs [path]          - Generate documentation");
        System.out.println("    config clear-cache [path]   - Clear cache");
        System.out.println("    config stats                - Show statistics");
        System.out.println();
        System.out.println("  🚀 Binary Performance:");
        System.out.println("    binary compile <file.tsk>   - Compile to binary");
        System.out.println("    binary execute <file.tskb>  - Execute binary");
        System.out.println("    binary benchmark <file>     - Benchmark performance");
        System.out.println("    binary optimize <file>      - Optimize binary");
        System.out.println();
        System.out.println("  🥜 Peanuts:");
        System.out.println("    peanuts compile <file>      - Compile to binary");
        System.out.println("    peanuts auto-compile [dir]  - Auto-compile all files");
        System.out.println("    peanuts load <file.pnt>     - Load binary file");
        System.out.println();
        System.out.println("  🎨 CSS:");
        System.out.println("    css expand <input> [output] - Expand CSS shortcodes");
        System.out.println("    css map                     - Show shortcode mappings");
        System.out.println();
        System.out.println("  🤖 AI:");
        System.out.println("    ai claude <prompt>          - Query Claude AI");
        System.out.println("    ai chatgpt <prompt>         - Query ChatGPT");
        System.out.println("    ai custom <api> <prompt>    - Query custom AI");
        System.out.println("    ai config                   - Show AI configuration");
        System.out.println("    ai setup                    - Interactive AI setup");
        System.out.println("    ai test                     - Test AI connections");
        System.out.println("    ai complete <file> [line] [column] - Auto-completion");
        System.out.println("    ai analyze <file>           - Analyze code");
        System.out.println("    ai optimize <file>          - Optimization suggestions");
        System.out.println("    ai security <file>          - Security scan");
        System.out.println();
        System.out.println("  🛠️  Utilities:");
        System.out.println("    parse <file>                - Parse TSK file");
        System.out.println("    validate <file>             - Validate syntax");
        System.out.println("    convert -i <input> -o <output> - Convert formats");
        System.out.println("    get <file> <key.path>       - Get value");
        System.out.println("    set <file> <key.path> <val> - Set value");
        System.out.println();
        System.out.println("Global Options:");
        formatter.printHelp("tsk", TuskLangMainCLI.globalOptions);
        System.out.println();
        System.out.println("Examples:");
        System.out.println("  tsk db status");
        System.out.println("  tsk serve 3000");
        System.out.println("  tsk test all");
        System.out.println("  tsk config get server.port");
        System.out.println("  tsk binary compile app.tsk");
        System.out.println("  tsk ai claude \"Analyze this code\"");
    }
} 