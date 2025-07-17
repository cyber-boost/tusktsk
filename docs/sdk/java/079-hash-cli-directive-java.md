# #cli - CLI Directive (Java)

The `#cli` directive provides enterprise-grade command-line interface capabilities for Java applications, enabling powerful CLI tools with Spring Boot integration and comprehensive command processing.

## Basic Syntax

```tusk
# Basic CLI command
#cli command: "hello" {
    @print("Hello, World!")
}

# CLI with arguments
#cli command: "greet" {
    name: @args.name || "World"
    @print("Hello, {name}!")
}

# CLI with options
#cli command: "process" {
    input: @args.input
    output: @args.output || "output.txt"
    verbose: @args.verbose || false
    
    @process_file(input, output, verbose)
}
```

## Java Implementation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.CliDirective;
import org.springframework.boot.CommandLineRunner;
import org.springframework.stereotype.Component;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import picocli.CommandLine;
import picocli.CommandLine.Command;
import picocli.CommandLine.Option;
import picocli.CommandLine.Parameters;

@Component
public class CliCommands implements CommandLineRunner {
    
    private final TuskLang tuskLang;
    private final CliDirective cliDirective;
    private final FileProcessor fileProcessor;
    
    public CliCommands(TuskLang tuskLang, FileProcessor fileProcessor) {
        this.tuskLang = tuskLang;
        this.cliDirective = new CliDirective();
        this.fileProcessor = fileProcessor;
    }
    
    @Override
    public void run(String... args) throws Exception {
        CommandLine cli = new CommandLine(new CliApplication());
        cli.execute(args);
    }
}

@Command(name = "tusk-cli", mixinStandardHelpOptions = true, version = "1.0.0")
public class CliApplication {
    
    @Command(name = "hello", description = "Print hello message")
    public void hello() {
        System.out.println("Hello, World!");
    }
    
    @Command(name = "greet", description = "Greet a person")
    public void greet(@Option(names = {"-n", "--name"}, description = "Name to greet") String name) {
        String greeting = "Hello, " + (name != null ? name : "World") + "!";
        System.out.println(greeting);
    }
    
    @Command(name = "process", description = "Process a file")
    public void process(@Option(names = {"-i", "--input"}, required = true, description = "Input file") String input,
                       @Option(names = {"-o", "--output"}, description = "Output file") String output,
                       @Option(names = {"-v", "--verbose"}, description = "Verbose output") boolean verbose) {
        
        String outputFile = output != null ? output : "output.txt";
        fileProcessor.processFile(input, outputFile, verbose);
    }
}
```

## CLI Configuration

```tusk
# Detailed CLI configuration
#cli {
    command: "complex"
    description: "Complex command with multiple options"
    version: "1.0.0"
    help: true
    options: {
        input: {required: true, type: "string", description: "Input file"}
        output: {type: "string", default: "output.txt", description: "Output file"}
        verbose: {type: "boolean", default: false, description: "Verbose output"}
        format: {type: "string", choices: ["json", "xml", "csv"], description: "Output format"}
    }
} {
    @process_complex_command(@args.input, @args.output, @args.verbose, @args.format)
}

# CLI with subcommands
#cli {
    command: "database"
    subcommands: {
        backup: {
            description: "Backup database"
            options: {
                database: {required: true, description: "Database name"}
                output: {description: "Backup file path"}
            }
        }
        restore: {
            description: "Restore database"
            options: {
                database: {required: true, description: "Database name"}
                input: {required: true, description: "Backup file path"}
            }
        }
    }
} {
    @handle_database_command(@args.subcommand, @args)
}
```

## Java CLI Configuration

```java
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;
import java.util.Map;
import java.util.List;

@Component
@ConfigurationProperties(prefix = "tusk.cli")
public class CliConfig {
    
    private String defaultCommand = "help";
    private String version = "1.0.0";
    private boolean helpEnabled = true;
    private Map<String, CliCommandDefinition> commands;
    private Map<String, String> aliases;
    
    // Getters and setters
    public String getDefaultCommand() { return defaultCommand; }
    public void setDefaultCommand(String defaultCommand) { this.defaultCommand = defaultCommand; }
    
    public String getVersion() { return version; }
    public void setVersion(String version) { this.version = version; }
    
    public boolean isHelpEnabled() { return helpEnabled; }
    public void setHelpEnabled(boolean helpEnabled) { this.helpEnabled = helpEnabled; }
    
    public Map<String, CliCommandDefinition> getCommands() { return commands; }
    public void setCommands(Map<String, CliCommandDefinition> commands) { this.commands = commands; }
    
    public Map<String, String> getAliases() { return aliases; }
    public void setAliases(Map<String, String> aliases) { this.aliases = aliases; }
    
    public static class CliCommandDefinition {
        private String description;
        private String version;
        private boolean help;
        private Map<String, CliOptionDefinition> options;
        private Map<String, CliSubcommandDefinition> subcommands;
        
        // Getters and setters
        public String getDescription() { return description; }
        public void setDescription(String description) { this.description = description; }
        
        public String getVersion() { return version; }
        public void setVersion(String version) { this.version = version; }
        
        public boolean isHelp() { return help; }
        public void setHelp(boolean help) { this.help = help; }
        
        public Map<String, CliOptionDefinition> getOptions() { return options; }
        public void setOptions(Map<String, CliOptionDefinition> options) { this.options = options; }
        
        public Map<String, CliSubcommandDefinition> getSubcommands() { return subcommands; }
        public void setSubcommands(Map<String, CliSubcommandDefinition> subcommands) { this.subcommands = subcommands; }
    }
    
    public static class CliOptionDefinition {
        private boolean required;
        private String type;
        private String defaultValue;
        private String description;
        private List<String> choices;
        
        // Getters and setters
        public boolean isRequired() { return required; }
        public void setRequired(boolean required) { this.required = required; }
        
        public String getType() { return type; }
        public void setType(String type) { this.type = type; }
        
        public String getDefaultValue() { return defaultValue; }
        public void setDefaultValue(String defaultValue) { this.defaultValue = defaultValue; }
        
        public String getDescription() { return description; }
        public void setDescription(String description) { this.description = description; }
        
        public List<String> getChoices() { return choices; }
        public void setChoices(List<String> choices) { this.choices = choices; }
    }
    
    public static class CliSubcommandDefinition {
        private String description;
        private Map<String, CliOptionDefinition> options;
        
        // Getters and setters
        public String getDescription() { return description; }
        public void setDescription(String description) { this.description = description; }
        
        public Map<String, CliOptionDefinition> options() { return options; }
        public void setOptions(Map<String, CliOptionDefinition> options) { this.options = options; }
    }
}

@Command(name = "tusk-cli", mixinStandardHelpOptions = true, version = "1.0.0")
public class ComplexCliApplication {
    
    @Command(name = "complex", description = "Complex command with multiple options")
    public void complex(@Option(names = {"-i", "--input"}, required = true, description = "Input file") String input,
                       @Option(names = {"-o", "--output"}, description = "Output file") String output,
                       @Option(names = {"-v", "--verbose"}, description = "Verbose output") boolean verbose,
                       @Option(names = {"-f", "--format"}, description = "Output format") String format) {
        
        String outputFile = output != null ? output : "output.txt";
        String outputFormat = format != null ? format : "json";
        
        processComplexCommand(input, outputFile, verbose, outputFormat);
    }
    
    private void processComplexCommand(String input, String output, boolean verbose, String format) {
        if (verbose) {
            System.out.println("Processing input: " + input);
            System.out.println("Output file: " + output);
            System.out.println("Format: " + format);
        }
        
        // Process the command
        fileProcessor.processFile(input, output, format);
        
        if (verbose) {
            System.out.println("Processing completed successfully");
        }
    }
}

@Command(name = "database", description = "Database operations")
public class DatabaseCli {
    
    @Command(name = "backup", description = "Backup database")
    public void backup(@Option(names = {"-d", "--database"}, required = true, description = "Database name") String database,
                      @Option(names = {"-o", "--output"}, description = "Backup file path") String output) {
        
        String backupPath = output != null ? output : database + "_backup.sql";
        databaseService.backupDatabase(database, backupPath);
        System.out.println("Database backup completed: " + backupPath);
    }
    
    @Command(name = "restore", description = "Restore database")
    public void restore(@Option(names = {"-d", "--database"}, required = true, description = "Database name") String database,
                       @Option(names = {"-i", "--input"}, required = true, description = "Backup file path") String input) {
        
        databaseService.restoreDatabase(database, input);
        System.out.println("Database restore completed: " + database);
    }
}
```

## File Processing Commands

```tusk
# File processing CLI
#cli command: "process-file" {
    input: @args.input
    output: @args.output
    format: @args.format || "json"
    
    @process_file_with_format(input, output, format)
}

# Batch file processing
#cli command: "batch-process" {
    directory: @args.directory
    pattern: @args.pattern || "*.txt"
    output_dir: @args.output_dir || "./output"
    
    @batch_process_files(directory, pattern, output_dir)
}

# File conversion
#cli command: "convert" {
    input: @args.input
    from_format: @args.from_format
    to_format: @args.to_format
    output: @args.output
    
    @convert_file_format(input, from_format, to_format, output)
}
```

## Java File Processing Commands

```java
import org.springframework.stereotype.Component;
import java.nio.file.*;
import java.util.stream.Stream;

@Component
public class FileProcessingCommands {
    
    private final FileProcessor fileProcessor;
    private final FileConverter fileConverter;
    
    public FileProcessingCommands(FileProcessor fileProcessor, FileConverter fileConverter) {
        this.fileProcessor = fileProcessor;
        this.fileConverter = fileConverter;
    }
    
    @Command(name = "process-file", description = "Process a single file")
    public void processFile(@Option(names = {"-i", "--input"}, required = true, description = "Input file") String input,
                           @Option(names = {"-o", "--output"}, description = "Output file") String output,
                           @Option(names = {"-f", "--format"}, description = "Output format") String format) {
        
        String outputFile = output != null ? output : input + ".processed";
        String outputFormat = format != null ? format : "json";
        
        try {
            fileProcessor.processFileWithFormat(input, outputFile, outputFormat);
            System.out.println("File processed successfully: " + outputFile);
        } catch (Exception e) {
            System.err.println("Error processing file: " + e.getMessage());
            System.exit(1);
        }
    }
    
    @Command(name = "batch-process", description = "Process multiple files")
    public void batchProcess(@Option(names = {"-d", "--directory"}, required = true, description = "Input directory") String directory,
                            @Option(names = {"-p", "--pattern"}, description = "File pattern") String pattern,
                            @Option(names = {"-o", "--output-dir"}, description = "Output directory") String outputDir) {
        
        String filePattern = pattern != null ? pattern : "*.txt";
        String outputDirectory = outputDir != null ? outputDir : "./output";
        
        try {
            Path inputPath = Paths.get(directory);
            Path outputPath = Paths.get(outputDirectory);
            
            if (!Files.exists(outputPath)) {
                Files.createDirectories(outputPath);
            }
            
            try (Stream<Path> paths = Files.walk(inputPath)) {
                paths.filter(Files::isRegularFile)
                     .filter(path -> path.toString().matches(filePattern.replace("*", ".*")))
                     .forEach(path -> {
                         try {
                             String relativePath = inputPath.relativize(path).toString();
                             Path outputFile = outputPath.resolve(relativePath + ".processed");
                             
                             fileProcessor.processFile(path.toString(), outputFile.toString());
                             System.out.println("Processed: " + relativePath);
                         } catch (Exception e) {
                             System.err.println("Error processing " + path + ": " + e.getMessage());
                         }
                     });
            }
            
            System.out.println("Batch processing completed");
        } catch (Exception e) {
            System.err.println("Error during batch processing: " + e.getMessage());
            System.exit(1);
        }
    }
    
    @Command(name = "convert", description = "Convert file format")
    public void convert(@Option(names = {"-i", "--input"}, required = true, description = "Input file") String input,
                       @Option(names = {"-f", "--from-format"}, required = true, description = "Source format") String fromFormat,
                       @Option(names = {"-t", "--to-format"}, required = true, description = "Target format") String toFormat,
                       @Option(names = {"-o", "--output"}, description = "Output file") String output) {
        
        String outputFile = output != null ? output : input + "." + toFormat;
        
        try {
            fileConverter.convertFileFormat(input, fromFormat, toFormat, outputFile);
            System.out.println("File converted successfully: " + outputFile);
        } catch (Exception e) {
            System.err.println("Error converting file: " + e.getMessage());
            System.exit(1);
        }
    }
}

@Service
public class FileProcessor {
    
    private final ObjectMapper objectMapper;
    private final XmlMapper xmlMapper;
    
    public FileProcessor(ObjectMapper objectMapper, XmlMapper xmlMapper) {
        this.objectMapper = objectMapper;
        this.xmlMapper = xmlMapper;
    }
    
    public void processFileWithFormat(String input, String output, String format) throws IOException {
        // Read input file
        String content = Files.readString(Paths.get(input));
        
        // Process content based on format
        String processedContent = processContent(content, format);
        
        // Write output file
        Files.writeString(Paths.get(output), processedContent);
    }
    
    public void processFile(String input, String output) throws IOException {
        processFileWithFormat(input, output, "json");
    }
    
    private String processContent(String content, String format) throws IOException {
        switch (format.toLowerCase()) {
            case "json":
                return processJsonContent(content);
            case "xml":
                return processXmlContent(content);
            case "csv":
                return processCsvContent(content);
            default:
                throw new IllegalArgumentException("Unsupported format: " + format);
        }
    }
    
    private String processJsonContent(String content) throws IOException {
        JsonNode jsonNode = objectMapper.readTree(content);
        // Add processing timestamp
        ((ObjectNode) jsonNode).put("processed_at", LocalDateTime.now().toString());
        return objectMapper.writerWithDefaultPrettyPrinter().writeValueAsString(jsonNode);
    }
    
    private String processXmlContent(String content) throws IOException {
        // Process XML content
        return content; // Simplified
    }
    
    private String processCsvContent(String content) {
        // Process CSV content
        return content; // Simplified
    }
}

@Service
public class FileConverter {
    
    private final ObjectMapper objectMapper;
    private final XmlMapper xmlMapper;
    
    public FileConverter(ObjectMapper objectMapper, XmlMapper xmlMapper) {
        this.objectMapper = objectMapper;
        this.xmlMapper = xmlMapper;
    }
    
    public void convertFileFormat(String input, String fromFormat, String toFormat, String output) throws IOException {
        // Read input file
        String content = Files.readString(Paths.get(input));
        
        // Convert content
        String convertedContent = convertContent(content, fromFormat, toFormat);
        
        // Write output file
        Files.writeString(Paths.get(output), convertedContent);
    }
    
    private String convertContent(String content, String fromFormat, String toFormat) throws IOException {
        // Convert between formats
        if ("json".equals(fromFormat) && "xml".equals(toFormat)) {
            JsonNode jsonNode = objectMapper.readTree(content);
            return xmlMapper.writeValueAsString(jsonNode);
        } else if ("xml".equals(fromFormat) && "json".equals(toFormat)) {
            JsonNode jsonNode = xmlMapper.readTree(content);
            return objectMapper.writerWithDefaultPrettyPrinter().writeValueAsString(jsonNode);
        } else {
            throw new IllegalArgumentException("Unsupported conversion: " + fromFormat + " to " + toFormat);
        }
    }
}
```

## Database Commands

```tusk
# Database CLI commands
#cli command: "db-backup" {
    database: @args.database
    output: @args.output || "{database}_backup.sql"
    
    @backup_database(database, output)
}

# Database migration
#cli command: "db-migrate" {
    direction: @args.direction || "up"
    version: @args.version
    
    @migrate_database(direction, version)
}

# Database query
#cli command: "db-query" {
    query: @args.query
    output: @args.output || "query_result.json"
    format: @args.format || "json"
    
    @execute_query(query, output, format)
}
```

## Java Database Commands

```java
import org.springframework.stereotype.Component;
import org.springframework.jdbc.core.JdbcTemplate;
import org.flywaydb.core.Flyway;

@Component
public class DatabaseCommands {
    
    private final DatabaseService databaseService;
    private final JdbcTemplate jdbcTemplate;
    private final Flyway flyway;
    
    public DatabaseCommands(DatabaseService databaseService, JdbcTemplate jdbcTemplate, Flyway flyway) {
        this.databaseService = databaseService;
        this.jdbcTemplate = jdbcTemplate;
        this.flyway = flyway;
    }
    
    @Command(name = "db-backup", description = "Backup database")
    public void backupDatabase(@Option(names = {"-d", "--database"}, required = true, description = "Database name") String database,
                              @Option(names = {"-o", "--output"}, description = "Backup file path") String output) {
        
        String backupPath = output != null ? output : database + "_backup.sql";
        
        try {
            databaseService.backupDatabase(database, backupPath);
            System.out.println("Database backup completed: " + backupPath);
        } catch (Exception e) {
            System.err.println("Error backing up database: " + e.getMessage());
            System.exit(1);
        }
    }
    
    @Command(name = "db-migrate", description = "Run database migrations")
    public void migrateDatabase(@Option(names = {"-d", "--direction"}, description = "Migration direction") String direction,
                               @Option(names = {"-v", "--version"}, description = "Target version") String version) {
        
        String migrationDirection = direction != null ? direction : "up";
        
        try {
            if ("up".equals(migrationDirection)) {
                if (version != null) {
                    flyway.migrateTo(version);
                } else {
                    flyway.migrate();
                }
                System.out.println("Database migration completed");
            } else if ("down".equals(migrationDirection)) {
                if (version != null) {
                    flyway.undo(version);
                } else {
                    flyway.undo();
                }
                System.out.println("Database migration rolled back");
            } else {
                throw new IllegalArgumentException("Invalid direction: " + migrationDirection);
            }
        } catch (Exception e) {
            System.err.println("Error during migration: " + e.getMessage());
            System.exit(1);
        }
    }
    
    @Command(name = "db-query", description = "Execute database query")
    public void executeQuery(@Option(names = {"-q", "--query"}, required = true, description = "SQL query") String query,
                            @Option(names = {"-o", "--output"}, description = "Output file") String output,
                            @Option(names = {"-f", "--format"}, description = "Output format") String format) {
        
        String outputFile = output != null ? output : "query_result.json";
        String outputFormat = format != null ? format : "json";
        
        try {
            List<Map<String, Object>> results = jdbcTemplate.queryForList(query);
            
            String resultContent = formatResults(results, outputFormat);
            Files.writeString(Paths.get(outputFile), resultContent);
            
            System.out.println("Query executed successfully. Results saved to: " + outputFile);
        } catch (Exception e) {
            System.err.println("Error executing query: " + e.getMessage());
            System.exit(1);
        }
    }
    
    private String formatResults(List<Map<String, Object>> results, String format) throws IOException {
        switch (format.toLowerCase()) {
            case "json":
                return objectMapper.writerWithDefaultPrettyPrinter().writeValueAsString(results);
            case "csv":
                return convertToCsv(results);
            case "xml":
                return xmlMapper.writeValueAsString(results);
            default:
                throw new IllegalArgumentException("Unsupported format: " + format);
        }
    }
    
    private String convertToCsv(List<Map<String, Object>> results) {
        if (results.isEmpty()) {
            return "";
        }
        
        StringBuilder csv = new StringBuilder();
        
        // Write headers
        String[] headers = results.get(0).keySet().toArray(new String[0]);
        csv.append(String.join(",", headers)).append("\n");
        
        // Write data
        for (Map<String, Object> row : results) {
            String[] values = new String[headers.length];
            for (int i = 0; i < headers.length; i++) {
                Object value = row.get(headers[i]);
                values[i] = value != null ? value.toString() : "";
            }
            csv.append(String.join(",", values)).append("\n");
        }
        
        return csv.toString();
    }
}

@Service
public class DatabaseService {
    
    private final DataSource dataSource;
    
    public DatabaseService(DataSource dataSource) {
        this.dataSource = dataSource;
    }
    
    public void backupDatabase(String database, String outputPath) throws IOException {
        try {
            // Create backup directory if it doesn't exist
            Path outputDir = Paths.get(outputPath).getParent();
            if (outputDir != null && !Files.exists(outputDir)) {
                Files.createDirectories(outputDir);
            }
            
            // Perform database backup
            ProcessBuilder pb = new ProcessBuilder(
                "mysqldump",
                "--host=" + getDatabaseHost(),
                "--port=" + getDatabasePort(),
                "--user=" + getDatabaseUser(),
                "--password=" + getDatabasePassword(),
                "--single-transaction",
                "--routines",
                "--triggers",
                database
            );
            
            Process process = pb.start();
            
            // Write backup to file
            try (FileOutputStream fos = new FileOutputStream(outputPath)) {
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = process.getInputStream().read(buffer)) != -1) {
                    fos.write(buffer, 0, bytesRead);
                }
            }
            
            int exitCode = process.waitFor();
            if (exitCode != 0) {
                throw new RuntimeException("Database backup failed with exit code: " + exitCode);
            }
        } catch (Exception e) {
            throw new IOException("Failed to backup database", e);
        }
    }
    
    private String getDatabaseHost() {
        // Extract host from datasource
        return "localhost"; // Simplified
    }
    
    private String getDatabasePort() {
        return "3306"; // Simplified
    }
    
    private String getDatabaseUser() {
        return "root"; // Simplified
    }
    
    private String getDatabasePassword() {
        return "password"; // Simplified
    }
}
```

## System Commands

```tusk
# System monitoring CLI
#cli command: "system-status" {
    @print_system_status()
}

# Process management
#cli command: "process-list" {
    @list_processes()
}

# System cleanup
#cli command: "cleanup" {
    temp_files: @args.temp_files || true
    logs: @args.logs || false
    cache: @args.cache || false
    
    @cleanup_system(temp_files, logs, cache)
}
```

## Java System Commands

```java
import org.springframework.stereotype.Component;
import java.lang.management.ManagementFactory;
import java.lang.management.MemoryMXBean;
import java.lang.management.OperatingSystemMXBean;

@Component
public class SystemCommands {
    
    private final SystemService systemService;
    private final CleanupService cleanupService;
    
    public SystemCommands(SystemService systemService, CleanupService cleanupService) {
        this.systemService = systemService;
        this.cleanupService = cleanupService;
    }
    
    @Command(name = "system-status", description = "Show system status")
    public void systemStatus() {
        try {
            SystemStatus status = systemService.getSystemStatus();
            
            System.out.println("=== System Status ===");
            System.out.println("CPU Usage: " + String.format("%.1f%%", status.getCpuUsage()));
            System.out.println("Memory Usage: " + String.format("%.1f%%", status.getMemoryUsage()));
            System.out.println("Disk Usage: " + String.format("%.1f%%", status.getDiskUsage()));
            System.out.println("Uptime: " + status.getUptime());
            System.out.println("Active Connections: " + status.getActiveConnections());
            
        } catch (Exception e) {
            System.err.println("Error getting system status: " + e.getMessage());
            System.exit(1);
        }
    }
    
    @Command(name = "process-list", description = "List running processes")
    public void processList() {
        try {
            List<ProcessInfo> processes = systemService.getRunningProcesses();
            
            System.out.println("=== Running Processes ===");
            System.out.printf("%-10s %-20s %-10s %-10s%n", "PID", "Name", "CPU%", "Memory%");
            System.out.println("----------------------------------------");
            
            for (ProcessInfo process : processes) {
                System.out.printf("%-10s %-20s %-10.1f %-10.1f%n",
                    process.getPid(),
                    process.getName(),
                    process.getCpuUsage(),
                    process.getMemoryUsage());
            }
            
        } catch (Exception e) {
            System.err.println("Error listing processes: " + e.getMessage());
            System.exit(1);
        }
    }
    
    @Command(name = "cleanup", description = "Clean up system resources")
    public void cleanup(@Option(names = {"-t", "--temp-files"}, description = "Clean temp files") boolean tempFiles,
                       @Option(names = {"-l", "--logs"}, description = "Clean old logs") boolean logs,
                       @Option(names = {"-c", "--cache"}, description = "Clean cache") boolean cache) {
        
        try {
            CleanupResult result = cleanupService.cleanup(tempFiles, logs, cache);
            
            System.out.println("=== Cleanup Results ===");
            if (tempFiles) {
                System.out.println("Temp files cleaned: " + result.getTempFilesCleaned() + " files");
            }
            if (logs) {
                System.out.println("Log files cleaned: " + result.getLogFilesCleaned() + " files");
            }
            if (cache) {
                System.out.println("Cache cleaned: " + result.getCacheCleaned() + " entries");
            }
            
            System.out.println("Total space freed: " + result.getSpaceFreed() + " bytes");
            
        } catch (Exception e) {
            System.err.println("Error during cleanup: " + e.getMessage());
            System.exit(1);
        }
    }
}

@Service
public class SystemService {
    
    private final OperatingSystemMXBean osBean;
    private final MemoryMXBean memoryBean;
    
    public SystemService() {
        this.osBean = ManagementFactory.getOperatingSystemMXBean();
        this.memoryBean = ManagementFactory.getMemoryMXBean();
    }
    
    public SystemStatus getSystemStatus() {
        SystemStatus status = new SystemStatus();
        
        // Get CPU usage
        if (osBean instanceof com.sun.management.OperatingSystemMXBean) {
            com.sun.management.OperatingSystemMXBean sunOsBean = (com.sun.management.OperatingSystemMXBean) osBean;
            status.setCpuUsage(sunOsBean.getCpuLoad() * 100);
        }
        
        // Get memory usage
        long totalMemory = memoryBean.getHeapMemoryUsage().getMax();
        long usedMemory = memoryBean.getHeapMemoryUsage().getUsed();
        status.setMemoryUsage((double) usedMemory / totalMemory * 100);
        
        // Get disk usage
        File root = new File("/");
        long totalSpace = root.getTotalSpace();
        long freeSpace = root.getFreeSpace();
        status.setDiskUsage((double) (totalSpace - freeSpace) / totalSpace * 100);
        
        // Get uptime
        long uptime = ManagementFactory.getRuntimeMXBean().getUptime();
        status.setUptime(formatUptime(uptime));
        
        // Get active connections (simplified)
        status.setActiveConnections(100); // Placeholder
        
        return status;
    }
    
    public List<ProcessInfo> getRunningProcesses() {
        List<ProcessInfo> processes = new ArrayList<>();
        
        // Get current process info
        ProcessInfo currentProcess = new ProcessInfo();
        currentProcess.setPid(ProcessHandle.current().pid());
        currentProcess.setName("java");
        currentProcess.setCpuUsage(0.0); // Simplified
        currentProcess.setMemoryUsage(0.0); // Simplified
        processes.add(currentProcess);
        
        return processes;
    }
    
    private String formatUptime(long uptime) {
        long days = uptime / (24 * 60 * 60 * 1000);
        long hours = (uptime % (24 * 60 * 60 * 1000)) / (60 * 60 * 1000);
        long minutes = (uptime % (60 * 60 * 1000)) / (60 * 1000);
        
        return String.format("%dd %dh %dm", days, hours, minutes);
    }
}

@Service
public class CleanupService {
    
    private final CacheManager cacheManager;
    
    public CleanupService(CacheManager cacheManager) {
        this.cacheManager = cacheManager;
    }
    
    public CleanupResult cleanup(boolean tempFiles, boolean logs, boolean cache) {
        CleanupResult result = new CleanupResult();
        
        if (tempFiles) {
            result.setTempFilesCleaned(cleanupTempFiles());
        }
        
        if (logs) {
            result.setLogFilesCleaned(cleanupLogFiles());
        }
        
        if (cache) {
            result.setCacheCleaned(cleanupCache());
        }
        
        return result;
    }
    
    private int cleanupTempFiles() {
        try {
            Path tempDir = Paths.get(System.getProperty("java.io.tmpdir"));
            int count = 0;
            
            try (Stream<Path> paths = Files.walk(tempDir)) {
                count = (int) paths.filter(Files::isRegularFile)
                                  .filter(path -> path.toString().endsWith(".tmp"))
                                  .peek(path -> {
                                      try {
                                          Files.delete(path);
                                      } catch (IOException e) {
                                          // Ignore deletion errors
                                      }
                                  })
                                  .count();
            }
            
            return count;
        } catch (Exception e) {
            logger.warn("Error cleaning temp files", e);
            return 0;
        }
    }
    
    private int cleanupLogFiles() {
        try {
            Path logDir = Paths.get("/var/log/application");
            int count = 0;
            
            if (Files.exists(logDir)) {
                try (Stream<Path> paths = Files.walk(logDir)) {
                    count = (int) paths.filter(Files::isRegularFile)
                                      .filter(path -> path.toString().endsWith(".log"))
                                      .filter(path -> {
                                          try {
                                              return Files.getLastModifiedTime(path)
                                                         .toInstant()
                                                         .isBefore(Instant.now().minus(Duration.ofDays(30)));
                                          } catch (IOException e) {
                                              return false;
                                          }
                                      })
                                      .peek(path -> {
                                          try {
                                              Files.delete(path);
                                          } catch (IOException e) {
                                              // Ignore deletion errors
                                          }
                                      })
                                      .count();
                }
            }
            
            return count;
        } catch (Exception e) {
            logger.warn("Error cleaning log files", e);
            return 0;
        }
    }
    
    private int cleanupCache() {
        try {
            int count = 0;
            
            for (String cacheName : cacheManager.getCacheNames()) {
                Cache cache = cacheManager.getCache(cacheName);
                if (cache != null) {
                    cache.clear();
                    count++;
                }
            }
            
            return count;
        } catch (Exception e) {
            logger.warn("Error cleaning cache", e);
            return 0;
        }
    }
}
```

## CLI Testing

```java
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.TestPropertySource;
import org.springframework.beans.factory.annotation.Autowired;

@SpringBootTest
@TestPropertySource(properties = {
    "tusk.cli.default-command=help",
    "tusk.cli.help-enabled=true"
})
public class CliTest {
    
    @Autowired
    private CliCommands cliCommands;
    
    @MockBean
    private FileProcessor fileProcessor;
    
    @MockBean
    private DatabaseService databaseService;
    
    @Test
    public void testHelloCommand() {
        // Test hello command
        String[] args = {"hello"};
        cliCommands.run(args);
        
        // Verify output (would need to capture System.out in real test)
    }
    
    @Test
    public void testGreetCommand() {
        // Test greet command with name
        String[] args = {"greet", "--name", "John"};
        cliCommands.run(args);
        
        // Verify output
    }
    
    @Test
    public void testProcessFileCommand() {
        // Test file processing command
        String[] args = {"process", "--input", "test.txt", "--output", "output.txt"};
        cliCommands.run(args);
        
        verify(fileProcessor).processFile("test.txt", "output.txt");
    }
    
    @Test
    public void testDatabaseBackupCommand() {
        // Test database backup command
        String[] args = {"db-backup", "--database", "testdb", "--output", "backup.sql"};
        cliCommands.run(args);
        
        verify(databaseService).backupDatabase("testdb", "backup.sql");
    }
}
```

## Configuration Properties

```yaml
# application.yml
tusk:
  cli:
    default-command: "help"
    version: "1.0.0"
    help-enabled: true
    
    commands:
      hello:
        description: "Print hello message"
        version: "1.0.0"
        help: true
      
      process-file:
        description: "Process a file"
        options:
          input:
            required: true
            type: "string"
            description: "Input file path"
          output:
            type: "string"
            default: "output.txt"
            description: "Output file path"
          format:
            type: "string"
            choices: ["json", "xml", "csv"]
            description: "Output format"
      
      db-backup:
        description: "Backup database"
        options:
          database:
            required: true
            type: "string"
            description: "Database name"
          output:
            type: "string"
            description: "Backup file path"
      
      system-status:
        description: "Show system status"
        help: true
    
    aliases:
      h: "hello"
      p: "process-file"
      b: "db-backup"
      s: "system-status"

spring:
  application:
    name: "tusk-cli"
```

## Summary

The `#cli` directive in TuskLang provides comprehensive command-line interface capabilities for Java applications. With Spring Boot integration, flexible command processing, and support for various CLI patterns, you can implement sophisticated command-line tools that enhance your application's functionality.

Key features include:
- **Multiple command types**: File processing, database operations, system management
- **Spring Boot integration**: Seamless integration with Spring Boot CLI
- **Flexible configuration**: Configurable commands with options and subcommands
- **Argument processing**: Support for required and optional arguments
- **Help system**: Built-in help and version information
- **Error handling**: Comprehensive error handling and exit codes
- **Testing support**: Comprehensive testing utilities

The Java implementation provides enterprise-grade CLI capabilities that integrate seamlessly with Spring Boot applications while maintaining the simplicity and power of TuskLang's declarative syntax. 