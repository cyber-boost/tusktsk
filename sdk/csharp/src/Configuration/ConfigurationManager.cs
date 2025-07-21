using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TuskLang.Configuration
{
    public class ConfigurationManager
    {
        private readonly Dictionary<string, object> _settings;
        private readonly string _configPath;

        public ConfigurationManager()
        {
            _settings = new Dictionary<string, object>();
            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tusktsk", "config.json");
        }

        public T GetSetting<T>(string key, T defaultValue = default(T))
        {
            if (_settings.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return defaultValue;
        }

        public void SetSetting<T>(string key, T value)
        {
            _settings[key] = value;
        }

        public async Task LoadAsync()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var content = await File.ReadAllTextAsync(_configPath);
                    // For now, just load basic settings
                    _settings["config_loaded"] = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load configuration: {ex.Message}");
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // For now, just save basic settings
                var content = "{\"version\":\"1.0\"}";
                await File.WriteAllTextAsync(_configPath, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not save configuration: {ex.Message}");
            }
        }
    }
} 