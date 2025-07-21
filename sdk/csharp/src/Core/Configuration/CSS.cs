using System;
using System.Collections.Generic;

namespace TuskLang.Configuration
{
    /// <summary>
    /// CSS configuration for TuskTsk
    /// </summary>
    public class CSS
    {
        /// <summary>
        /// CSS output directory
        /// </summary>
        public string OutputDirectory { get; set; } = "wwwroot/css";

        /// <summary>
        /// Whether to minify CSS output
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
        /// CSS imports
        /// </summary>
        public List<string> Imports { get; set; } = new List<string>();

        /// <summary>
        /// CSS plugins
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
    }
} 