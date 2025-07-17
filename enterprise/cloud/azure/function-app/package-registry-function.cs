using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Data.Tables;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace TuskLang.PackageRegistry
{
    public class PackageRegistryFunction
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly TableClient _tableClient;
        private readonly ILogger<PackageRegistryFunction> _logger;

        public PackageRegistryFunction(
            BlobServiceClient blobServiceClient,
            TableClient tableClient,
            ILogger<PackageRegistryFunction> logger)
        {
            _blobServiceClient = blobServiceClient;
            _tableClient = tableClient;
            _logger = logger;
        }

        [FunctionName("UploadPackage")]
        public async Task<IActionResult> UploadPackage(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "packages")] HttpRequest req,
            ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var uploadRequest = JsonConvert.DeserializeObject<PackageUploadRequest>(requestBody);

                // Validate request
                if (string.IsNullOrEmpty(uploadRequest.Name) || 
                    string.IsNullOrEmpty(uploadRequest.Version) || 
                    string.IsNullOrEmpty(uploadRequest.Data))
                {
                    return new BadRequestObjectResult(new { error = "Missing required fields" });
                }

                // Validate package
                var validationResult = ValidatePackage(uploadRequest.Name, uploadRequest.Version, uploadRequest.Data);
                if (!validationResult.IsValid)
                {
                    return new BadRequestObjectResult(new { error = validationResult.Message });
                }

                string packageId = $"{uploadRequest.Name}-{uploadRequest.Version}";
                string blobName = $"packages/{uploadRequest.Name}/{uploadRequest.Version}/package.tsk";

                // Check if package already exists
                if (await PackageExistsAsync(uploadRequest.Name, uploadRequest.Version))
                {
                    return new ConflictObjectResult(new { error = "Package version already exists" });
                }

                // Upload to Blob Storage
                var containerClient = _blobServiceClient.GetBlobContainerClient("packages");
                var blobClient = containerClient.GetBlobClient(blobName);

                var packageBytes = Convert.FromBase64String(uploadRequest.Data);
                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/octet-stream"
                };

                var metadata = new Dictionary<string, string>
                {
                    { "package-name", uploadRequest.Name },
                    { "package-version", uploadRequest.Version },
                    { "upload-time", DateTime.UtcNow.ToString("O") }
                };

                await blobClient.UploadAsync(new MemoryStream(packageBytes), blobHttpHeaders, metadata);

                // Store metadata in Table Storage
                var packageEntity = new PackageEntity
                {
                    PartitionKey = uploadRequest.Name,
                    RowKey = uploadRequest.Version,
                    PackageId = packageId,
                    Name = uploadRequest.Name,
                    Version = uploadRequest.Version,
                    BlobName = blobName,
                    Size = packageBytes.Length,
                    Metadata = JsonConvert.SerializeObject(uploadRequest.Metadata ?? new Dictionary<string, object>()),
                    UploadTime = DateTime.UtcNow,
                    DownloadCount = 0,
                    LastDownload = null
                };

                await _tableClient.AddEntityAsync(packageEntity);

                return new OkObjectResult(new
                {
                    package_id = packageId,
                    download_url = $"https://tusklangcdn.azureedge.net/{blobName}",
                    message = "Package uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload package error");
                return new ObjectResult(new { error = $"Upload failed: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        [FunctionName("DownloadPackage")]
        public async Task<IActionResult> DownloadPackage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "packages/{name}/{version}/download")] HttpRequest req,
            string name,
            string version,
            ILogger log)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
                {
                    return new BadRequestObjectResult(new { error = "Missing package name or version" });
                }

                string packageId = $"{name}-{version}";

                // Get package metadata
                var packageEntity = await GetPackageEntityAsync(name, version);
                if (packageEntity == null)
                {
                    return new NotFoundObjectResult(new { error = "Package not found" });
                }

                // Generate SAS token for download
                var containerClient = _blobServiceClient.GetBlobContainerClient("packages");
                var blobClient = containerClient.GetBlobClient(packageEntity.BlobName);

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = "packages",
                    BlobName = packageEntity.BlobName,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow,
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasToken = sasBuilder.ToSasQueryParameters(new Azure.Storage.StorageSharedKeyCredential(
                    Environment.GetEnvironmentVariable("StorageAccountName"),
                    Environment.GetEnvironmentVariable("StorageAccountKey"))).ToString();

                var downloadUrl = $"{blobClient.Uri}?{sasToken}";

                // Update download statistics
                await UpdateDownloadStatsAsync(name, version);

                return new OkObjectResult(new
                {
                    package_id = packageId,
                    download_url = downloadUrl,
                    metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(packageEntity.Metadata),
                    size = packageEntity.Size
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download package error");
                return new ObjectResult(new { error = $"Download failed: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        [FunctionName("SearchPackages")]
        public async Task<IActionResult> SearchPackages(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "packages/search")] HttpRequest req,
            ILogger log)
        {
            try
            {
                string searchTerm = req.Query["q"].ToString();
                int limit = int.TryParse(req.Query["limit"].ToString(), out int l) ? l : 20;
                int offset = int.TryParse(req.Query["offset"].ToString(), out int o) ? o : 0;

                // Query Table Storage
                var query = _tableClient.QueryAsync<PackageEntity>();
                var packages = new List<PackageEntity>();

                await foreach (var package in query)
                {
                    if (string.IsNullOrEmpty(searchTerm) || 
                        package.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        package.Version.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        packages.Add(package);
                    }
                }

                // Apply pagination
                var results = packages.Skip(offset).Take(limit).Select(p => new
                {
                    package_id = p.PackageId,
                    name = p.Name,
                    version = p.Version,
                    download_count = p.DownloadCount,
                    upload_time = p.UploadTime.ToString("O"),
                    metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(p.Metadata)
                }).ToList();

                return new OkObjectResult(new
                {
                    packages = results,
                    total = results.Count,
                    next_offset = offset + limit < packages.Count ? offset + limit : (int?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search packages error");
                return new ObjectResult(new { error = $"Search failed: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        [FunctionName("GetPackageInfo")]
        public async Task<IActionResult> GetPackageInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "packages/{name}/{version}")] HttpRequest req,
            string name,
            string version,
            ILogger log)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
                {
                    return new BadRequestObjectResult(new { error = "Missing package name or version" });
                }

                var packageEntity = await GetPackageEntityAsync(name, version);
                if (packageEntity == null)
                {
                    return new NotFoundObjectResult(new { error = "Package not found" });
                }

                var downloadUrl = $"https://tusklangcdn.azureedge.net/{packageEntity.BlobName}";

                return new OkObjectResult(new
                {
                    package_id = packageEntity.PackageId,
                    name = packageEntity.Name,
                    version = packageEntity.Version,
                    download_url = downloadUrl,
                    size = packageEntity.Size,
                    download_count = packageEntity.DownloadCount,
                    upload_time = packageEntity.UploadTime.ToString("O"),
                    last_download = packageEntity.LastDownload?.ToString("O"),
                    metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(packageEntity.Metadata)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get package info error");
                return new ObjectResult(new { error = $"Failed to get package info: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        private ValidationResult ValidatePackage(string name, string version, string data)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || name.Length > 100)
                {
                    return new ValidationResult { IsValid = false, Message = "Invalid package name" };
                }

                if (string.IsNullOrEmpty(version) || version.Length > 50)
                {
                    return new ValidationResult { IsValid = false, Message = "Invalid package version" };
                }

                if (string.IsNullOrEmpty(data))
                {
                    return new ValidationResult { IsValid = false, Message = "Package data is empty" };
                }

                var packageBytes = Convert.FromBase64String(data);
                if (packageBytes.Length > 50 * 1024 * 1024) // 50MB limit
                {
                    return new ValidationResult { IsValid = false, Message = "Package too large" };
                }

                // Basic TuskLang format validation
                var packageText = Encoding.UTF8.GetString(packageBytes);
                if (!packageText.Trim().StartsWith("#") && !packageText.Contains(":"))
                {
                    return new ValidationResult { IsValid = false, Message = "Invalid TuskLang format" };
                }

                return new ValidationResult { IsValid = true, Message = "Package is valid" };
            }
            catch (Exception ex)
            {
                return new ValidationResult { IsValid = false, Message = $"Validation error: {ex.Message}" };
            }
        }

        private async Task<bool> PackageExistsAsync(string name, string version)
        {
            try
            {
                var packageEntity = await GetPackageEntityAsync(name, version);
                return packageEntity != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<PackageEntity> GetPackageEntityAsync(string name, string version)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<PackageEntity>(name, version);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }

        private async Task UpdateDownloadStatsAsync(string name, string version)
        {
            try
            {
                var packageEntity = await GetPackageEntityAsync(name, version);
                if (packageEntity != null)
                {
                    packageEntity.DownloadCount++;
                    packageEntity.LastDownload = DateTime.UtcNow;
                    await _tableClient.UpdateEntityAsync(packageEntity, Azure.ETag.All);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update download stats");
            }
        }
    }

    public class PackageUploadRequest
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Data { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class PackageEntity : Azure.Data.Tables.ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }

        public string PackageId { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string BlobName { get; set; }
        public int Size { get; set; }
        public string Metadata { get; set; }
        public DateTime UploadTime { get; set; }
        public int DownloadCount { get; set; }
        public DateTime? LastDownload { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }
} 