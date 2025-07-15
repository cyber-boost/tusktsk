using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections.Generic; // Added missing import for Dictionary

namespace TuskLang
{
    /// <summary>
    /// Shell format storage for binary data
    /// Handles compression and binary serialization of TSK data
    /// </summary>
    public class ShellStorage
    {
        /// <summary>
        /// Magic bytes for Shell format identification
        /// </summary>
        public static readonly byte[] MAGIC = Encoding.UTF8.GetBytes("FLEX");
        
        /// <summary>
        /// Current version of Shell format
        /// </summary>
        public static readonly byte VERSION = 1;

        /// <summary>
        /// Pack data into Shell binary format
        /// </summary>
        public static byte[] Pack(ShellData data)
        {
            // Compress data if it's not already compressed
            byte[] compressedData;
            if (data.Data is string strData)
            {
                compressedData = CompressString(strData);
            }
            else if (data.Data is byte[] byteData)
            {
                compressedData = CompressBytes(byteData);
            }
            else
            {
                compressedData = CompressString(data.Data.ToString());
            }

            var idBytes = Encoding.UTF8.GetBytes(data.Id);

            // Build binary format
            // Magic (4) + Version (1) + ID Length (4) + ID + Data Length (4) + Data
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(MAGIC);
                writer.Write(VERSION);
                writer.Write(idBytes.Length);
                writer.Write(idBytes);
                writer.Write(compressedData.Length);
                writer.Write(compressedData);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Unpack Shell binary format
        /// </summary>
        public static ShellData Unpack(byte[] shellData)
        {
            using (var ms = new MemoryStream(shellData))
            using (var reader = new BinaryReader(ms))
            {
                // Check magic bytes
                var magic = reader.ReadBytes(4);
                if (!magic.SequenceEqual(MAGIC))
                    throw new ArgumentException("Invalid shell format");

                // Read version
                var version = reader.ReadByte();
                if (version != VERSION)
                    throw new ArgumentException($"Unsupported shell version: {version}");

                // Read ID
                var idLength = reader.ReadInt32();
                var storageId = Encoding.UTF8.GetString(reader.ReadBytes(idLength));

                // Read data
                var dataLength = reader.ReadInt32();
                var compressedData = reader.ReadBytes(dataLength);

                // Decompress
                var data = DecompressBytes(compressedData);

                return new ShellData
                {
                    Version = version,
                    Id = storageId,
                    Data = data
                };
            }
        }

        /// <summary>
        /// Compress string data
        /// </summary>
        private static byte[] CompressString(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return CompressBytes(bytes);
        }

        /// <summary>
        /// Compress byte array data
        /// </summary>
        private static byte[] CompressBytes(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Decompress byte array data
        /// </summary>
        private static string DecompressBytes(byte[] compressedData)
        {
            using (var ms = new MemoryStream(compressedData))
            using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
            using (var resultMs = new MemoryStream())
            {
                gzip.CopyTo(resultMs);
                return Encoding.UTF8.GetString(resultMs.ToArray());
            }
        }

        /// <summary>
        /// Detect the type of data stored in Shell format
        /// </summary>
        public static string DetectType(byte[] shellData)
        {
            try
            {
                var data = Unpack(shellData);
                var content = data.Data.ToString();

                // Try to detect JSON
                if (content.StartsWith("{") || content.StartsWith("["))
                    return "json";

                // Try to detect TSK format
                if (content.Contains("[") && content.Contains("]") && content.Contains("="))
                    return "tsk";

                // Try to detect XML
                if (content.StartsWith("<") && content.Contains(">"))
                    return "xml";

                // Default to text
                return "text";
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// Create Shell data with metadata
        /// </summary>
        public static ShellData CreateShellData(object data, string id, Dictionary<string, object> metadata = null)
        {
            return new ShellData
            {
                Version = VERSION,
                Id = id,
                Data = data,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// Data structure for Shell storage
    /// </summary>
    public class ShellData
    {
        public byte Version { get; set; }
        public string Id { get; set; }
        public object Data { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
} 