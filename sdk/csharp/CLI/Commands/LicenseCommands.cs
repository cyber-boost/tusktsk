using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using TuskLang.CLI;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// License management commands for TuskLang CLI
    /// Implements: tsk license check, tsk license activate
    /// </summary>
    public static class LicenseCommands
    {
        private static readonly string LicenseFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tusklang", "license.json");

        public static void AddCommands(RootCommand rootCommand)
        {
            // tsk license check
            var checkCommand = new Command("check", "Check current license status")
            {
                new Option<bool>("--verbose", "Show detailed license information")
            };
            checkCommand.Handler = CommandHandler.Create<bool>(CheckLicense);
            rootCommand.AddCommand(checkCommand);

            // tsk license activate <key>
            var activateCommand = new Command("activate", "Activate license with key")
            {
                new Argument<string>("key", "License key to activate")
            };
            activateCommand.Handler = CommandHandler.Create<string>(ActivateLicense);
            rootCommand.AddCommand(activateCommand);

            // Add license subcommand
            var licenseCommand = new Command("license", "License management commands")
            {
                checkCommand,
                activateCommand
            };
            rootCommand.AddCommand(licenseCommand);
        }

        private static async Task<int> CheckLicense(bool verbose)
        {
            try
            {
                GlobalOptions.WriteProcessing("Checking license status...");

                if (!File.Exists(LicenseFile))
                {
                    GlobalOptions.WriteWarning("No license file found");
                    GlobalOptions.WriteLine("Run 'tsk license activate <key>' to activate your license");
                    return 7; // License error
                }

                var licenseJson = await File.ReadAllTextAsync(LicenseFile);
                var license = JsonSerializer.Deserialize<LicenseInfo>(licenseJson);

                if (license == null)
                {
                    GlobalOptions.WriteError("Invalid license file");
                    return 7;
                }

                // Check if license is expired
                var isExpired = license.ExpiresAt.HasValue && license.ExpiresAt.Value < DateTime.UtcNow;
                var isExpiringSoon = license.ExpiresAt.HasValue && 
                                   license.ExpiresAt.Value > DateTime.UtcNow && 
                                   license.ExpiresAt.Value < DateTime.UtcNow.AddDays(30);

                if (isExpired)
                {
                    GlobalOptions.WriteError("License has expired");
                    GlobalOptions.WriteLine($"Expired on: {license.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                    return 7;
                }

                if (isExpiringSoon)
                {
                    GlobalOptions.WriteWarning("License expires soon");
                    GlobalOptions.WriteLine($"Expires on: {license.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                }
                else
                {
                    GlobalOptions.WriteSuccess("License is valid");
                }

                if (verbose)
                {
                    GlobalOptions.WriteLine("");
                    GlobalOptions.WriteLine("License Details:");
                    GlobalOptions.WriteLine($"  Type: {license.Type}");
                    GlobalOptions.WriteLine($"  Customer: {license.Customer}");
                    GlobalOptions.WriteLine($"  Issued: {license.IssuedAt:yyyy-MM-dd HH:mm:ss} UTC");
                    
                    if (license.ExpiresAt.HasValue)
                    {
                        GlobalOptions.WriteLine($"  Expires: {license.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                        var daysLeft = (license.ExpiresAt.Value - DateTime.UtcNow).Days;
                        GlobalOptions.WriteLine($"  Days Remaining: {daysLeft}");
                    }
                    else
                    {
                        GlobalOptions.WriteLine("  Expires: Never (perpetual license)");
                    }

                    GlobalOptions.WriteLine($"  Features: {string.Join(", ", license.Features)}");
                    GlobalOptions.WriteLine($"  Max Users: {license.MaxUsers}");
                    GlobalOptions.WriteLine($"  Max Projects: {license.MaxProjects}");
                }
                else
                {
                    GlobalOptions.WriteLine($"License Type: {license.Type}");
                    if (license.ExpiresAt.HasValue)
                    {
                        var daysLeft = (license.ExpiresAt.Value - DateTime.UtcNow).Days;
                        GlobalOptions.WriteLine($"Days Remaining: {daysLeft}");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to check license: {ex.Message}");
                if (GlobalOptions.Verbose)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                return 1;
            }
        }

        private static async Task<int> ActivateLicense(string key)
        {
            try
            {
                GlobalOptions.WriteProcessing("Activating license...");

                if (string.IsNullOrWhiteSpace(key))
                {
                    GlobalOptions.WriteError("License key is required");
                    return 2;
                }

                // Validate key format (basic validation)
                if (key.Length < 20)
                {
                    GlobalOptions.WriteError("Invalid license key format");
                    return 2;
                }

                // Simulate license validation (in real implementation, this would call a license server)
                var license = await ValidateLicenseKey(key);
                
                if (license == null)
                {
                    GlobalOptions.WriteError("Invalid or expired license key");
                    GlobalOptions.WriteLine("Please check your license key and try again");
                    return 7;
                }

                // Ensure license directory exists
                var licenseDir = Path.GetDirectoryName(LicenseFile);
                if (!string.IsNullOrEmpty(licenseDir) && !Directory.Exists(licenseDir))
                {
                    Directory.CreateDirectory(licenseDir);
                }

                // Save license information
                var licenseJson = JsonSerializer.Serialize(license, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(LicenseFile, licenseJson);

                GlobalOptions.WriteSuccess("License activated successfully!");
                GlobalOptions.WriteLine($"License Type: {license.Type}");
                GlobalOptions.WriteLine($"Customer: {license.Customer}");
                
                if (license.ExpiresAt.HasValue)
                {
                    GlobalOptions.WriteLine($"Expires: {license.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                }
                else
                {
                    GlobalOptions.WriteLine("Expires: Never (perpetual license)");
                }

                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"Failed to activate license: {ex.Message}");
                if (GlobalOptions.Verbose)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                return 1;
            }
        }

        private static async Task<LicenseInfo?> ValidateLicenseKey(string key)
        {
            // Simulate network delay
            await Task.Delay(1000);

            // In a real implementation, this would validate the key with a license server
            // For now, we'll simulate different license types based on key patterns
            
            if (key.StartsWith("TUSK-"))
            {
                // Trial license
                return new LicenseInfo
                {
                    Key = key,
                    Type = "Trial",
                    Customer = "Trial User",
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    Features = new[] { "basic", "parser", "config" },
                    MaxUsers = 1,
                    MaxProjects = 3
                };
            }
            else if (key.StartsWith("PRO-"))
            {
                // Professional license
                return new LicenseInfo
                {
                    Key = key,
                    Type = "Professional",
                    Customer = "Professional User",
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    Features = new[] { "basic", "parser", "config", "ai", "binary", "cache" },
                    MaxUsers = 10,
                    MaxProjects = 50
                };
            }
            else if (key.StartsWith("ENT-"))
            {
                // Enterprise license
                return new LicenseInfo
                {
                    Key = key,
                    Type = "Enterprise",
                    Customer = "Enterprise Customer",
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = null, // Perpetual
                    Features = new[] { "basic", "parser", "config", "ai", "binary", "cache", "distributed", "security" },
                    MaxUsers = -1, // Unlimited
                    MaxProjects = -1 // Unlimited
                };
            }
            else if (key.StartsWith("DEV-"))
            {
                // Developer license
                return new LicenseInfo
                {
                    Key = key,
                    Type = "Developer",
                    Customer = "Developer",
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    Features = new[] { "basic", "parser", "config", "ai", "binary", "cache", "distributed" },
                    MaxUsers = 5,
                    MaxProjects = 20
                };
            }

            // Invalid key
            return null;
        }
    }

    /// <summary>
    /// License information model
    /// </summary>
    public class LicenseInfo
    {
        public string Key { get; set; } = "";
        public string Type { get; set; } = "";
        public string Customer { get; set; } = "";
        public DateTime IssuedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string[] Features { get; set; } = Array.Empty<string>();
        public int MaxUsers { get; set; }
        public int MaxProjects { get; set; }
    }
} 