using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text.Json;

namespace TuskLang.Operators
{
    /// <summary>
    /// Comprehensive file system operator for TuskTsk
    /// Provides high-performance file operations with async support
    /// </summary>
    public class FileSystemOperator : BaseOperator
    {
        private readonly ILogger<FileSystemOperator> _logger;

        public FileSystemOperator(ILogger<FileSystemOperator> logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSystemOperator>.Instance;
        }

        #region File Operations

        /// <summary>
        /// Read file content as string
        /// </summary>
        public async Task<string> ReadFileAsync(string filePath, Encoding encoding = null)
        {
            try
            {
                encoding ??= Encoding.UTF8;
                var content = await File.ReadAllTextAsync(filePath, encoding);
                _logger.LogDebug($"Read file: {filePath}, size: {content.Length} characters");
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading file: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Read file content as bytes
        /// </summary>
        public async Task<byte[]> ReadFileBytesAsync(string filePath)
        {
            try
            {
                var content = await File.ReadAllBytesAsync(filePath);
                _logger.LogDebug($"Read file bytes: {filePath}, size: {content.Length} bytes");
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading file bytes: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Read file lines
        /// </summary>
        public async Task<string[]> ReadFileLinesAsync(string filePath, Encoding encoding = null)
        {
            try
            {
                encoding ??= Encoding.UTF8;
                var lines = await File.ReadAllLinesAsync(filePath, encoding);
                _logger.LogDebug($"Read file lines: {filePath}, lines: {lines.Length}");
                return lines;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading file lines: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Write content to file
        /// </summary>
        public async Task WriteFileAsync(string filePath, string content, Encoding encoding = null, bool append = false)
        {
            try
            {
                encoding ??= Encoding.UTF8;
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (append)
                {
                    await File.AppendAllTextAsync(filePath, content, encoding);
                }
                else
                {
                    await File.WriteAllTextAsync(filePath, content, encoding);
                }

                _logger.LogDebug($"Wrote file: {filePath}, size: {content.Length} characters, append: {append}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error writing file: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Write bytes to file
        /// </summary>
        public async Task WriteFileBytesAsync(string filePath, byte[] content, bool append = false)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (append)
                {
                    await File.AppendAllBytesAsync(filePath, content);
                }
                else
                {
                    await File.WriteAllBytesAsync(filePath, content);
                }

                _logger.LogDebug($"Wrote file bytes: {filePath}, size: {content.Length} bytes, append: {append}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error writing file bytes: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Write lines to file
        /// </summary>
        public async Task WriteFileLinesAsync(string filePath, IEnumerable<string> lines, Encoding encoding = null, bool append = false)
        {
            try
            {
                encoding ??= Encoding.UTF8;
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (append)
                {
                    await File.AppendAllLinesAsync(filePath, lines, encoding);
                }
                else
                {
                    await File.WriteAllLinesAsync(filePath, lines, encoding);
                }

                _logger.LogDebug($"Wrote file lines: {filePath}, lines: {lines.Count()}, append: {append}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error writing file lines: {filePath}");
                throw;
            }
        }

        #endregion

        #region File Information

        /// <summary>
        /// Get file information
        /// </summary>
        public FileInfo GetFileInfo(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                _logger.LogDebug($"Got file info: {filePath}, size: {fileInfo.Length}, exists: {fileInfo.Exists}");
                return fileInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file info: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Check if file exists
        /// </summary>
        public bool FileExists(string filePath)
        {
            var exists = File.Exists(filePath);
            _logger.LogDebug($"File exists check: {filePath}, exists: {exists}");
            return exists;
        }

        /// <summary>
        /// Get file size
        /// </summary>
        public long GetFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                _logger.LogDebug($"Got file size: {filePath}, size: {fileInfo.Length}");
                return fileInfo.Length;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file size: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Get file hash (MD5, SHA1, SHA256)
        /// </summary>
        public async Task<string> GetFileHashAsync(string filePath, string algorithm = "SHA256")
        {
            try
            {
                using var hashAlgorithm = algorithm.ToUpper() switch
                {
                    "MD5" => MD5.Create(),
                    "SHA1" => SHA1.Create(),
                    "SHA256" => SHA256.Create(),
                    "SHA512" => SHA512.Create(),
                    _ => SHA256.Create()
                };

                using var stream = File.OpenRead(filePath);
                var hash = await hashAlgorithm.ComputeHashAsync(stream);
                var hashString = Convert.ToHexString(hash).ToLower();
                
                _logger.LogDebug($"Got file hash: {filePath}, algorithm: {algorithm}, hash: {hashString}");
                return hashString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file hash: {filePath}");
                throw;
            }
        }

        #endregion

        #region File Management

        /// <summary>
        /// Copy file
        /// </summary>
        public async Task CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false)
        {
            try
            {
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await Task.Run(() => File.Copy(sourcePath, destinationPath, overwrite));
                _logger.LogDebug($"Copied file: {sourcePath} -> {destinationPath}, overwrite: {overwrite}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error copying file: {sourcePath} -> {destinationPath}");
                throw;
            }
        }

        /// <summary>
        /// Move file
        /// </summary>
        public async Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = false)
        {
            try
            {
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (overwrite && File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

                await Task.Run(() => File.Move(sourcePath, destinationPath));
                _logger.LogDebug($"Moved file: {sourcePath} -> {destinationPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error moving file: {sourcePath} -> {destinationPath}");
                throw;
            }
        }

        /// <summary>
        /// Delete file
        /// </summary>
        public async Task DeleteFileAsync(string filePath)
        {
            try
            {
                await Task.Run(() => File.Delete(filePath));
                _logger.LogDebug($"Deleted file: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {filePath}");
                throw;
            }
        }

        #endregion

        #region Directory Operations

        /// <summary>
        /// Create directory
        /// </summary>
        public DirectoryInfo CreateDirectory(string directoryPath)
        {
            try
            {
                var directory = Directory.CreateDirectory(directoryPath);
                _logger.LogDebug($"Created directory: {directoryPath}");
                return directory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating directory: {directoryPath}");
                throw;
            }
        }

        /// <summary>
        /// Check if directory exists
        /// </summary>
        public bool DirectoryExists(string directoryPath)
        {
            var exists = Directory.Exists(directoryPath);
            _logger.LogDebug($"Directory exists check: {directoryPath}, exists: {exists}");
            return exists;
        }

        /// <summary>
        /// Get directory files
        /// </summary>
        public string[] GetDirectoryFiles(string directoryPath, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath, searchPattern, searchOption);
                _logger.LogDebug($"Got directory files: {directoryPath}, pattern: {searchPattern}, count: {files.Length}");
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting directory files: {directoryPath}");
                throw;
            }
        }

        /// <summary>
        /// Get directory subdirectories
        /// </summary>
        public string[] GetDirectoryDirectories(string directoryPath, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            try
            {
                var directories = Directory.GetDirectories(directoryPath, searchPattern, searchOption);
                _logger.LogDebug($"Got directory subdirectories: {directoryPath}, pattern: {searchPattern}, count: {directories.Length}");
                return directories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting directory subdirectories: {directoryPath}");
                throw;
            }
        }

        /// <summary>
        /// Delete directory
        /// </summary>
        public async Task DeleteDirectoryAsync(string directoryPath, bool recursive = false)
        {
            try
            {
                await Task.Run(() => Directory.Delete(directoryPath, recursive));
                _logger.LogDebug($"Deleted directory: {directoryPath}, recursive: {recursive}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting directory: {directoryPath}");
                throw;
            }
        }

        #endregion

        #region File Monitoring

        /// <summary>
        /// Watch file for changes
        /// </summary>
        public FileSystemWatcher WatchFile(string filePath, Action<FileSystemEventArgs> onChange = null, Action<ErrorEventArgs> onError = null)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileName(filePath);
                
                var watcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    EnableRaisingEvents = true
                };

                if (onChange != null)
                {
                    watcher.Changed += (sender, e) => onChange(e);
                    watcher.Created += (sender, e) => onChange(e);
                    watcher.Deleted += (sender, e) => onChange(e);
                    watcher.Renamed += (sender, e) => onChange(e);
                }

                if (onError != null)
                {
                    watcher.Error += (sender, e) => onError(e);
                }

                _logger.LogDebug($"Started watching file: {filePath}");
                return watcher;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error watching file: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Watch directory for changes
        /// </summary>
        public FileSystemWatcher WatchDirectory(string directoryPath, string filter = "*.*", Action<FileSystemEventArgs> onChange = null, Action<ErrorEventArgs> onError = null)
        {
            try
            {
                var watcher = new FileSystemWatcher(directoryPath, filter)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = true
                };

                if (onChange != null)
                {
                    watcher.Changed += (sender, e) => onChange(e);
                    watcher.Created += (sender, e) => onChange(e);
                    watcher.Deleted += (sender, e) => onChange(e);
                    watcher.Renamed += (sender, e) => onChange(e);
                }

                if (onError != null)
                {
                    watcher.Error += (sender, e) => onError(e);
                }

                _logger.LogDebug($"Started watching directory: {directoryPath}, filter: {filter}");
                return watcher;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error watching directory: {directoryPath}");
                throw;
            }
        }

        #endregion

        #region Compression

        /// <summary>
        /// Compress file to ZIP
        /// </summary>
        public async Task CompressFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await Task.Run(() =>
                {
                    using var zip = ZipFile.Open(destinationPath, ZipArchiveMode.Create);
                    zip.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath));
                });

                _logger.LogDebug($"Compressed file: {sourcePath} -> {destinationPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error compressing file: {sourcePath} -> {destinationPath}");
                throw;
            }
        }

        /// <summary>
        /// Extract ZIP file
        /// </summary>
        public async Task ExtractZipAsync(string zipPath, string destinationPath)
        {
            try
            {
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, destinationPath));
                _logger.LogDebug($"Extracted ZIP: {zipPath} -> {destinationPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting ZIP: {zipPath} -> {destinationPath}");
                throw;
            }
        }

        #endregion

        #region JSON Operations

        /// <summary>
        /// Read JSON file
        /// </summary>
        public async Task<T> ReadJsonAsync<T>(string filePath, JsonSerializerOptions options = null)
        {
            try
            {
                var json = await ReadFileAsync(filePath);
                var result = JsonSerializer.Deserialize<T>(json, options);
                _logger.LogDebug($"Read JSON file: {filePath}, type: {typeof(T).Name}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading JSON file: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Write JSON file
        /// </summary>
        public async Task WriteJsonAsync<T>(string filePath, T data, JsonSerializerOptions options = null)
        {
            try
            {
                options ??= new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(data, options);
                await WriteFileAsync(filePath, json);
                _logger.LogDebug($"Wrote JSON file: {filePath}, type: {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error writing JSON file: {filePath}");
                throw;
            }
        }

        #endregion

        #region Stream Operations

        /// <summary>
        /// Read file as stream
        /// </summary>
        public FileStream OpenFileStream(string filePath, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read)
        {
            try
            {
                var stream = new FileStream(filePath, mode, access, share);
                _logger.LogDebug($"Opened file stream: {filePath}, mode: {mode}, access: {access}");
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error opening file stream: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Copy stream to file
        /// </summary>
        public async Task CopyStreamToFileAsync(Stream sourceStream, string destinationPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
                await sourceStream.CopyToAsync(destinationStream);
                
                _logger.LogDebug($"Copied stream to file: {destinationPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error copying stream to file: {destinationPath}");
                throw;
            }
        }

        #endregion
    }
} 