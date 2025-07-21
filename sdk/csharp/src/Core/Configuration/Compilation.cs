using System;
using System.Collections.Generic;

namespace TuskLang.Configuration
{
    /// <summary>
    /// Compilation configuration for TuskTsk
    /// </summary>
    public class Compilation
    {
        /// <summary>
        /// Target framework
        /// </summary>
        public string TargetFramework { get; set; } = "net8.0";

        /// <summary>
        /// Output directory
        /// </summary>
        public string OutputDirectory { get; set; } = "bin";

        /// <summary>
        /// Whether to optimize the compilation
        /// </summary>
        public bool Optimize { get; set; } = true;

        /// <summary>
        /// Whether to include debug information
        /// </summary>
        public bool IncludeDebugInfo { get; set; } = false;

        /// <summary>
        /// Compilation warnings to suppress
        /// </summary>
        public List<string> SuppressedWarnings { get; set; } = new List<string>();

        /// <summary>
        /// Additional compiler options
        /// </summary>
        public Dictionary<string, string> CompilerOptions { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Preprocessor definitions
        /// </summary>
        public List<string> PreprocessorDefinitions { get; set; } = new List<string>();

        /// <summary>
        /// Whether to treat warnings as errors
        /// </summary>
        public bool TreatWarningsAsErrors { get; set; } = false;

        /// <summary>
        /// Warning level
        /// </summary>
        public int WarningLevel { get; set; } = 4;
    }
} 