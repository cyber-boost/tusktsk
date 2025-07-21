using System;
using System.Collections.Generic;

namespace TuskTsk.Framework
{
    /// <summary>
    /// Framework configuration for TuskTsk
    /// </summary>
    public class Framework
    {
        /// <summary>
        /// Framework name
        /// </summary>
        public string Name { get; set; } = "TuskTsk";

        /// <summary>
        /// Framework version
        /// </summary>
        public string Version { get; set; } = "2.0.2";

        /// <summary>
        /// Framework description
        /// </summary>
        public string Description { get; set; } = "The Freedom Configuration Language for C#";

        /// <summary>
        /// Framework features
        /// </summary>
        public List<string> Features { get; set; } = new List<string>();

        /// <summary>
        /// Framework configuration
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Whether the framework is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
} 