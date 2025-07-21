using System;
using System.Collections.Generic;

namespace TuskLang.Configuration
{
    /// <summary>
    /// CSS processing options for TuskTsk
    /// </summary>
    public class CssProcessingOptions
    {
        /// <summary>
        /// Input CSS file path
        /// </summary>
        public string InputFile { get; set; } = string.Empty;

        /// <summary>
        /// Output CSS file path
        /// </summary>
        public string OutputFile { get; set; } = string.Empty;

        /// <summary>
        /// Whether to minify the CSS
        /// </summary>
        public bool Minify { get; set; } = true;

        /// <summary>
        /// Whether to generate source maps
        /// </summary>
        public bool GenerateSourceMaps { get; set; } = false;

        /// <summary>
        /// CSS preprocessor type
        /// </summary>
        public string Preprocessor { get; set; } = "none";

        /// <summary>
        /// CSS framework
        /// </summary>
        public string Framework { get; set; } = "none";

        /// <summary>
        /// Custom CSS variables
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// CSS imports to include
        /// </summary>
        public List<string> Imports { get; set; } = new List<string>();

        /// <summary>
        /// CSS plugins to use
        /// </summary>
        public List<string> Plugins { get; set; } = new List<string>();

        /// <summary>
        /// Whether to autoprefix CSS
        /// </summary>
        public bool Autoprefix { get; set; } = true;

        /// <summary>
        /// Target browsers for autoprefixing
        /// </summary>
        public List<string> TargetBrowsers { get; set; } = new List<string> { "> 1%", "last 2 versions" };

        /// <summary>
        /// Whether to optimize the CSS
        /// </summary>
        public bool Optimize { get; set; } = true;

        /// <summary>
        /// Whether to remove comments
        /// </summary>
        public bool RemoveComments { get; set; } = true;

        /// <summary>
        /// Whether to remove unused CSS
        /// </summary>
        public bool RemoveUnused { get; set; } = false;

        /// <summary>
        /// Whether to inline critical CSS
        /// </summary>
        public bool InlineCritical { get; set; } = false;

        /// <summary>
        /// Whether to preserve line breaks
        /// </summary>
        public bool PreserveLineBreaks { get; set; } = false;

        /// <summary>
        /// Output format
        /// </summary>
        public string OutputFormat { get; set; } = "css";

        /// <summary>
        /// Whether to watch for changes
        /// </summary>
        public bool Watch { get; set; } = false;

        /// <summary>
        /// Watch directory
        /// </summary>
        public string WatchDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Whether to create backup files
        /// </summary>
        public bool CreateBackup { get; set; } = false;

        /// <summary>
        /// Backup file extension
        /// </summary>
        public string BackupExtension { get; set; } = ".backup";

        /// <summary>
        /// Whether to validate CSS
        /// </summary>
        public bool Validate { get; set; } = true;

        /// <summary>
        /// Whether to report statistics
        /// </summary>
        public bool ReportStatistics { get; set; } = false;

        /// <summary>
        /// Whether to be verbose
        /// </summary>
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Whether to debug
        /// </summary>
        public bool Debug { get; set; } = false;
    }
} 