using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data.Common;
using System.Collections.Generic;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// Database commands for TuskLang CLI
    /// </summary>
    public static class DatabaseCommands
    {
        public static void AddCommands(RootCommand rootCommand)
        {
            var dbCommand = new Command("db", "Database operations")
            {
                new Command("status", "Check database connection status")
                {
                    Handler = CommandHandler.Create(CheckDatabaseStatus)
                },
                new Command("migrate", "Run migration file")
                {
                    new Argument<string>("file", "Migration file path"),
                    Handler = CommandHandler.Create<string>(RunMigration)
                },
                new Command("console", "Open interactive database console")
                {
                    Handler = CommandHandler.Create(OpenDatabaseConsole)
                },
                new Command("backup", "Backup database")
                {
                    new Argument<string>("file", "Backup file path (optional)"),
                    Handler = CommandHandler.Create<string>(BackupDatabase)
                },
                new Command("restore", "Restore from backup file")
                {
                    new Argument<string>("file", "Backup file path"),
                    Handler = CommandHandler.Create<string>(RestoreDatabase)
                },
                new Command("init", "Initialize SQLite database")
                {
                    Handler = CommandHandler.Create(InitializeDatabase)
                }
            };

            rootCommand.AddCommand(dbCommand);
        }

        private static async Task<int> CheckDatabaseStatus()
        {
            try
            {
                GlobalOptions.WriteProcessing("Checking database connection...");

                var config = await LoadConfiguration();
                var connectionString = GetConnectionString(config);

                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var version = connection.ServerVersion;
                    
                    GlobalOptions.WriteSuccess($"Database connected successfully");
                    GlobalOptions.WriteLine($"Server Version: {version}");
                    GlobalOptions.WriteLine($"Database: {connection.Database}");
                    GlobalOptions.WriteLine($"State: {connection.State}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Database connection failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunMigration(string file)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"Migration file not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Running migration: {file}");

                var config = await LoadConfiguration();
                var connectionString = GetConnectionString(config);
                var sql = await File.ReadAllTextAsync(file);

                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        var result = await command.ExecuteNonQueryAsync();
                        GlobalOptions.WriteSuccess($"Migration completed. {result} rows affected.");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Migration failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> OpenDatabaseConsole()
        {
            try
            {
                GlobalOptions.WriteProcessing("Opening interactive database console...");

                var config = await LoadConfiguration();
                var connectionString = GetConnectionString(config);

                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    GlobalOptions.WriteLine("Interactive SQLite Console (type 'exit' to quit)");
                    GlobalOptions.WriteLine("=============================================");

                    while (true)
                    {
                        Console.Write("sqlite> ");
                        var input = Console.ReadLine();

                        if (string.IsNullOrEmpty(input) || input.ToLower() == "exit")
                            break;

                        try
                        {
                            using (var command = new SQLiteCommand(input, connection))
                            {
                                if (input.Trim().ToLower().StartsWith("select"))
                                {
                                    using (var reader = await command.ExecuteReaderAsync())
                                    {
                                        var columns = new string[reader.FieldCount];
                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            columns[i] = reader.GetName(i);
                                        }

                                        GlobalOptions.WriteLine(string.Join(" | ", columns));
                                        GlobalOptions.WriteLine(new string('-', columns.Length * 10));

                                        while (await reader.ReadAsync())
                                        {
                                            var values = new string[reader.FieldCount];
                                            for (int i = 0; i < reader.FieldCount; i++)
                                            {
                                                values[i] = reader[i]?.ToString() ?? "NULL";
                                            }
                                            GlobalOptions.WriteLine(string.Join(" | ", values));
                                        }
                                    }
                                }
                                else
                                {
                                    var result = await command.ExecuteNonQueryAsync();
                                    GlobalOptions.WriteLine($"Query executed. {result} rows affected.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            GlobalOptions.WriteError($"SQL Error: {ex.Message}");
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to open database console: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> BackupDatabase(string file)
        {
            try
            {
                var config = await LoadConfiguration();
                var connectionString = GetConnectionString(config);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFile = file ?? $"tusklang_backup_{timestamp}.sql";

                GlobalOptions.WriteProcessing($"Creating database backup: {backupFile}");

                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // SQLite backup
                    using (var backup = new SQLiteConnection($"Data Source={backupFile};Version=3;"))
                    {
                        await backup.OpenAsync();
                        connection.BackupDatabase(backup, "main", "main", -1, null, 0);
                    }
                }

                GlobalOptions.WriteSuccess($"Database backed up to: {backupFile}");
                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Backup failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RestoreDatabase(string file)
        {
            try
            {
                if (!File.Exists(file))
                {
                    GlobalOptions.WriteError($"Backup file not found: {file}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Restoring database from: {file}");

                var config = await LoadConfiguration();
                var connectionString = GetConnectionString(config);

                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // SQLite restore
                    using (var backup = new SQLiteConnection($"Data Source={file};Version=3;"))
                    {
                        await backup.OpenAsync();
                        backup.BackupDatabase(connection, "main", "main", -1, null, 0);
                    }
                }

                GlobalOptions.WriteSuccess("Database restored successfully");
                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Restore failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> InitializeDatabase()
        {
            try
            {
                GlobalOptions.WriteProcessing("Initializing SQLite database...");

                var config = await LoadConfiguration();
                var connectionString = GetConnectionString(config);
                var dbPath = ExtractDatabasePath(connectionString);

                if (File.Exists(dbPath))
                {
                    GlobalOptions.WriteWarning($"Database already exists: {dbPath}");
                    return 0;
                }

                // Create database directory if needed
                var dbDir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                }

                // Initialize database with basic schema
                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    var initSql = @"
                        CREATE TABLE IF NOT EXISTS migrations (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            filename TEXT NOT NULL,
                            applied_at DATETIME DEFAULT CURRENT_TIMESTAMP
                        );
                        
                        CREATE TABLE IF NOT EXISTS config_cache (
                            key TEXT PRIMARY KEY,
                            value TEXT,
                            expires_at DATETIME
                        );
                        
                        CREATE TABLE IF NOT EXISTS user_features (
                            user_id TEXT PRIMARY KEY,
                            features TEXT,
                            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                        );
                        
                        CREATE TABLE IF NOT EXISTS content_features (
                            content_id TEXT PRIMARY KEY,
                            features TEXT,
                            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                        );
                        
                        CREATE TABLE IF NOT EXISTS activity_events (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            user_id TEXT,
                            event_type TEXT,
                            data TEXT,
                            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                        );
                        
                        CREATE TABLE IF NOT EXISTS trending_content (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            content_id TEXT,
                            score REAL,
                            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                        );
                        
                        CREATE TABLE IF NOT EXISTS feed_algorithm_configs (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            name TEXT UNIQUE,
                            config TEXT,
                            is_active BOOLEAN DEFAULT 1
                        );
                    ";

                    using (var command = new SQLiteCommand(initSql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                GlobalOptions.WriteSuccess($"SQLite database initialized: {dbPath}");
                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Database initialization failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<Dictionary<string, object>> LoadConfiguration()
        {
            var config = new PeanutConfig();
            return await config.LoadAsync();
        }

        private static string GetConnectionString(Dictionary<string, object> config)
        {
            // Try to get database configuration
            var dbType = config.GetValueOrDefault("database.type", "sqlite")?.ToString()?.ToLower();
            
            switch (dbType)
            {
                case "sqlite":
                    var dbPath = config.GetValueOrDefault("database.path", "tusklang.db")?.ToString();
                    return $"Data Source={dbPath};Version=3;";
                
                case "sqlserver":
                    var server = config.GetValueOrDefault("database.server", "localhost")?.ToString();
                    var database = config.GetValueOrDefault("database.database", "tusklang")?.ToString();
                    var trusted = config.GetValueOrDefault("database.trusted_connection", true);
                    return $"Server={server};Database={database};Trusted_Connection={trusted};";
                
                case "postgresql":
                    var host = config.GetValueOrDefault("database.host", "localhost")?.ToString();
                    var port = config.GetValueOrDefault("database.port", 5432)?.ToString();
                    var pgDatabase = config.GetValueOrDefault("database.database", "tusklang")?.ToString();
                    var user = config.GetValueOrDefault("database.user", "postgres")?.ToString();
                    var password = config.GetValueOrDefault("database.password", "")?.ToString();
                    return $"Host={host};Port={port};Database={pgDatabase};Username={user};Password={password};";
                
                default:
                    return "Data Source=tusklang.db;Version=3;";
            }
        }

        private static string ExtractDatabasePath(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("Data Source="))
                {
                    return part.Split('=')[1];
                }
            }
            return "tusklang.db";
        }
    }
} 