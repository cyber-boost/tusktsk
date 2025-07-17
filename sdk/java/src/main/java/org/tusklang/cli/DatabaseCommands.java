package org.tusklang.cli;

import org.apache.commons.cli.*;
import java.io.*;
import java.nio.file.*;
import java.sql.*;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

/**
 * Database Commands Implementation
 * 
 * Commands:
 * - tsk db status                    - Check database connection
 * - tsk db migrate <file>           - Run migration file
 * - tsk db console                  - Interactive database console
 * - tsk db backup [file]            - Backup database
 * - tsk db restore <file>           - Restore from backup
 * - tsk db init                     - Initialize SQLite database
 */
public class DatabaseCommands {
    
    public static boolean handle(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            printHelp();
            return false;
        }
        
        String subcommand = args[0];
        String[] subArgs = new String[args.length - 1];
        System.arraycopy(args, 1, subArgs, 0, args.length - 1);
        
        switch (subcommand) {
            case "status":
                return handleStatus(subArgs, globalCmd);
                
            case "migrate":
                return handleMigrate(subArgs, globalCmd);
                
            case "console":
                return handleConsole(subArgs, globalCmd);
                
            case "backup":
                return handleBackup(subArgs, globalCmd);
                
            case "restore":
                return handleRestore(subArgs, globalCmd);
                
            case "init":
                return handleInit(subArgs, globalCmd);
                
            default:
                System.err.println("Unknown database command: " + subcommand);
                printHelp();
                return false;
        }
    }
    
    /**
     * tsk db status - Check database connection
     */
    private static boolean handleStatus(String[] args, CommandLine globalCmd) {
        try {
            // Try to connect to database
            String dbUrl = getDatabaseUrl();
            if (dbUrl == null) {
                System.out.println("❌ No database configured");
                System.out.println("Use 'tsk db init' to initialize SQLite database");
                return false;
            }
            
            try (Connection conn = DriverManager.getConnection(dbUrl)) {
                DatabaseMetaData meta = conn.getMetaData();
                
                if (globalCmd.hasOption("json")) {
                    // JSON output
                    System.out.println("{");
                    System.out.println("  \"status\": \"connected\",");
                    System.out.println("  \"database\": \"" + meta.getDatabaseProductName() + "\",");
                    System.out.println("  \"version\": \"" + meta.getDatabaseProductVersion() + "\",");
                    System.out.println("  \"url\": \"" + dbUrl + "\",");
                    System.out.println("  \"timestamp\": \"" + LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME) + "\"");
                    System.out.println("}");
                } else {
                    // Human-readable output
                    System.out.println("✅ Database connected");
                    System.out.println("📍 Database: " + meta.getDatabaseProductName());
                    System.out.println("📍 Version: " + meta.getDatabaseProductVersion());
                    System.out.println("📍 URL: " + dbUrl);
                }
                
                return true;
            }
            
        } catch (SQLException e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.out.println("❌ Database connection failed: " + e.getMessage());
            }
            return false;
        }
    }
    
    /**
     * tsk db migrate <file> - Run migration file
     */
    private static boolean handleMigrate(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            System.err.println("Error: migrate command requires a file path");
            return false;
        }
        
        String migrationFile = args[0];
        
        try {
            if (!Files.exists(Paths.get(migrationFile))) {
                System.err.println("Error: Migration file not found: " + migrationFile);
                return false;
            }
            
            String dbUrl = getDatabaseUrl();
            if (dbUrl == null) {
                System.err.println("Error: No database configured");
                return false;
            }
            
            String sql = new String(Files.readAllBytes(Paths.get(migrationFile)));
            
            try (Connection conn = DriverManager.getConnection(dbUrl)) {
                conn.setAutoCommit(false);
                
                // Split SQL by semicolon and execute each statement
                String[] statements = sql.split(";");
                int executed = 0;
                
                for (String statement : statements) {
                    statement = statement.trim();
                    if (!statement.isEmpty()) {
                        try (Statement stmt = conn.createStatement()) {
                            stmt.execute(statement);
                            executed++;
                            
                            if (globalCmd.hasOption("verbose")) {
                                System.out.println("✅ Executed: " + statement.substring(0, Math.min(50, statement.length())) + "...");
                            }
                        }
                    }
                }
                
                conn.commit();
                
                if (globalCmd.hasOption("json")) {
                    System.out.println("{");
                    System.out.println("  \"status\": \"success\",");
                    System.out.println("  \"migration\": \"" + migrationFile + "\",");
                    System.out.println("  \"statements_executed\": " + executed);
                    System.out.println("}");
                } else {
                    System.out.println("✅ Migration completed successfully");
                    System.out.println("📍 File: " + migrationFile);
                    System.out.println("📍 Statements executed: " + executed);
                }
                
                return true;
                
            } catch (SQLException e) {
                if (globalCmd.hasOption("json")) {
                    System.out.println("{");
                    System.out.println("  \"status\": \"error\",");
                    System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                    System.out.println("}");
                } else {
                    System.out.println("❌ Migration failed: " + e.getMessage());
                }
                return false;
            }
            
        } catch (IOException e) {
            System.err.println("Error reading migration file: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * tsk db console - Interactive database console
     */
    private static boolean handleConsole(String[] args, CommandLine globalCmd) {
        String dbUrl = getDatabaseUrl();
        if (dbUrl == null) {
            System.err.println("Error: No database configured");
            return false;
        }
        
        try (Connection conn = DriverManager.getConnection(dbUrl)) {
            System.out.println("🐘 TuskLang Database Console");
            System.out.println("Connected to: " + dbUrl);
            System.out.println("Type 'exit' to quit, 'help' for commands");
            System.out.println();
            
            java.util.Scanner scanner = new java.util.Scanner(System.in);
            
            while (true) {
                System.out.print("db> ");
                String input = scanner.nextLine().trim();
                
                if (input.isEmpty()) {
                    continue;
                }
                
                if ("exit".equalsIgnoreCase(input) || "quit".equalsIgnoreCase(input)) {
                    break;
                }
                
                if ("help".equalsIgnoreCase(input)) {
                    printConsoleHelp();
                    continue;
                }
                
                if (input.toLowerCase().startsWith("select") || input.toLowerCase().startsWith("show")) {
                    // Query command
                    try (Statement stmt = conn.createStatement();
                         ResultSet rs = stmt.executeQuery(input)) {
                        
                        ResultSetMetaData meta = rs.getMetaData();
                        int columns = meta.getColumnCount();
                        
                        // Print headers
                        for (int i = 1; i <= columns; i++) {
                            System.out.print(meta.getColumnName(i) + "\t");
                        }
                        System.out.println();
                        
                        // Print separator
                        for (int i = 1; i <= columns; i++) {
                            System.out.print("--------\t");
                        }
                        System.out.println();
                        
                        // Print data
                        int rows = 0;
                        while (rs.next()) {
                            for (int i = 1; i <= columns; i++) {
                                System.out.print(rs.getString(i) + "\t");
                            }
                            System.out.println();
                            rows++;
                        }
                        
                        System.out.println();
                        System.out.println("📍 " + rows + " row(s) returned");
                        
                    } catch (SQLException e) {
                        System.out.println("❌ Query error: " + e.getMessage());
                    }
                } else {
                    // Update command
                    try (Statement stmt = conn.createStatement()) {
                        int affected = stmt.executeUpdate(input);
                        System.out.println("✅ " + affected + " row(s) affected");
                    } catch (SQLException e) {
                        System.out.println("❌ Command error: " + e.getMessage());
                    }
                }
            }
            
            scanner.close();
            System.out.println("Goodbye!");
            return true;
            
        } catch (SQLException e) {
            System.err.println("Database connection failed: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * tsk db backup [file] - Backup database
     */
    private static boolean handleBackup(String[] args, CommandLine globalCmd) {
        String backupFile = args.length > 0 ? args[0] : generateBackupFilename();
        
        try {
            String dbUrl = getDatabaseUrl();
            if (dbUrl == null) {
                System.err.println("Error: No database configured");
                return false;
            }
            
            // For SQLite, we can just copy the file
            if (dbUrl.startsWith("jdbc:sqlite:")) {
                String dbFile = dbUrl.substring("jdbc:sqlite:".length());
                Files.copy(Paths.get(dbFile), Paths.get(backupFile), StandardCopyOption.REPLACE_EXISTING);
                
                if (globalCmd.hasOption("json")) {
                    System.out.println("{");
                    System.out.println("  \"status\": \"success\",");
                    System.out.println("  \"backup_file\": \"" + backupFile + "\",");
                    System.out.println("  \"size_bytes\": " + Files.size(Paths.get(backupFile)));
                    System.out.println("}");
                } else {
                    System.out.println("✅ Database backed up successfully");
                    System.out.println("📍 File: " + backupFile);
                    System.out.println("📍 Size: " + Files.size(Paths.get(backupFile)) + " bytes");
                }
                
                return true;
            } else {
                // For other databases, we'd need to implement proper backup
                System.err.println("Backup not implemented for this database type");
                return false;
            }
            
        } catch (IOException e) {
            System.err.println("Backup failed: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * tsk db restore <file> - Restore from backup
     */
    private static boolean handleRestore(String[] args, CommandLine globalCmd) {
        if (args.length == 0) {
            System.err.println("Error: restore command requires a backup file");
            return false;
        }
        
        String backupFile = args[0];
        
        try {
            if (!Files.exists(Paths.get(backupFile))) {
                System.err.println("Error: Backup file not found: " + backupFile);
                return false;
            }
            
            String dbUrl = getDatabaseUrl();
            if (dbUrl == null) {
                System.err.println("Error: No database configured");
                return false;
            }
            
            // For SQLite, we can just copy the file back
            if (dbUrl.startsWith("jdbc:sqlite:")) {
                String dbFile = dbUrl.substring("jdbc:sqlite:".length());
                Files.copy(Paths.get(backupFile), Paths.get(dbFile), StandardCopyOption.REPLACE_EXISTING);
                
                if (globalCmd.hasOption("json")) {
                    System.out.println("{");
                    System.out.println("  \"status\": \"success\",");
                    System.out.println("  \"restored_from\": \"" + backupFile + "\"");
                    System.out.println("}");
                } else {
                    System.out.println("✅ Database restored successfully");
                    System.out.println("📍 From: " + backupFile);
                }
                
                return true;
            } else {
                System.err.println("Restore not implemented for this database type");
                return false;
            }
            
        } catch (IOException e) {
            System.err.println("Restore failed: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * tsk db init - Initialize SQLite database
     */
    private static boolean handleInit(String[] args, CommandLine globalCmd) {
        try {
            // Create SQLite database in current directory
            String dbFile = "tusklang.db";
            String dbUrl = "jdbc:sqlite:" + dbFile;
            
            // Test connection (this creates the file)
            try (Connection conn = DriverManager.getConnection(dbUrl)) {
                // Create basic tables
                try (Statement stmt = conn.createStatement()) {
                    stmt.execute("""
                        CREATE TABLE IF NOT EXISTS migrations (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            filename TEXT NOT NULL,
                            executed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )
                    """);
                    
                    stmt.execute("""
                        CREATE TABLE IF NOT EXISTS config_cache (
                            key TEXT PRIMARY KEY,
                            value TEXT,
                            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )
                    """);
                }
                
                if (globalCmd.hasOption("json")) {
                    System.out.println("{");
                    System.out.println("  \"status\": \"success\",");
                    System.out.println("  \"database_file\": \"" + dbFile + "\",");
                    System.out.println("  \"url\": \"" + dbUrl + "\"");
                    System.out.println("}");
                } else {
                    System.out.println("✅ SQLite database initialized successfully");
                    System.out.println("📍 File: " + dbFile);
                    System.out.println("📍 URL: " + dbUrl);
                }
                
                return true;
            }
            
        } catch (SQLException e) {
            if (globalCmd.hasOption("json")) {
                System.out.println("{");
                System.out.println("  \"status\": \"error\",");
                System.out.println("  \"error\": \"" + e.getMessage() + "\"");
                System.out.println("}");
            } else {
                System.out.println("❌ Database initialization failed: " + e.getMessage());
            }
            return false;
        }
    }
    
    // Helper methods
    
    private static String getDatabaseUrl() {
        // Check environment variable first
        String dbUrl = System.getenv("TUSKLANG_DATABASE_URL");
        if (dbUrl != null) {
            return dbUrl;
        }
        
        // Check for SQLite database in current directory
        String dbFile = "tusklang.db";
        if (Files.exists(Paths.get(dbFile))) {
            return "jdbc:sqlite:" + dbFile;
        }
        
        return null;
    }
    
    private static String generateBackupFilename() {
        String timestamp = LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss"));
        return "tusklang_backup_" + timestamp + ".sql";
    }
    
    private static void printHelp() {
        System.out.println("Database Commands:");
        System.out.println("  tsk db status                    - Check database connection");
        System.out.println("  tsk db migrate <file>           - Run migration file");
        System.out.println("  tsk db console                  - Interactive database console");
        System.out.println("  tsk db backup [file]            - Backup database");
        System.out.println("  tsk db restore <file>           - Restore from backup");
        System.out.println("  tsk db init                     - Initialize SQLite database");
    }
    
    private static void printConsoleHelp() {
        System.out.println("Database Console Commands:");
        System.out.println("  SELECT * FROM table;           - Query data");
        System.out.println("  SHOW TABLES;                   - List tables");
        System.out.println("  INSERT INTO table VALUES (...); - Insert data");
        System.out.println("  UPDATE table SET ...;          - Update data");
        System.out.println("  DELETE FROM table WHERE ...;   - Delete data");
        System.out.println("  help                           - Show this help");
        System.out.println("  exit                           - Exit console");
    }
} 