using System;

namespace TuskLang.Configuration
{
    public class ConfigurationEngineOptions
    {
        public bool EnableValidation { get; set; } = true;
        public bool EnableCaching { get; set; } = true;
        public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public bool EnableLogging { get; set; } = true;
        public string LogLevel { get; set; } = "Info";
        public bool EnableEncryption { get; set; } = false;
        public string EncryptionKey { get; set; } = "";
        public bool EnableCompression { get; set; } = false;
        public int MaxConfigurationSize { get; set; } = 1024 * 1024; // 1MB
        public bool EnableHotReload { get; set; } = true;
        public TimeSpan HotReloadInterval { get; set; } = TimeSpan.FromSeconds(30);
    }
} 