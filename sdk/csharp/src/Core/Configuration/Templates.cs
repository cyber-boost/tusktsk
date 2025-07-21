using System;
using System.Collections.Generic;

namespace TuskLang.Configuration
{
    /// <summary>
    /// Templates configuration for TuskTsk
    /// </summary>
    public class Templates
    {
        /// <summary>
        /// Template directory
        /// </summary>
        public string TemplateDirectory { get; set; } = "templates";

        /// <summary>
        /// Available templates
        /// </summary>
        public List<TemplateInfo> AvailableTemplates { get; set; } = new List<TemplateInfo>();

        /// <summary>
        /// Default template
        /// </summary>
        public string DefaultTemplate { get; set; } = "basic";

        /// <summary>
        /// Template variables
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Whether to overwrite existing files
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;

        /// <summary>
        /// Template engine type
        /// </summary>
        public string Engine { get; set; } = "default";
    }

    /// <summary>
    /// Template information
    /// </summary>
    public class TemplateInfo
    {
        /// <summary>
        /// Template name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Template description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Template file path
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Template type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Template version
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Template author
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Template tags
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
} 