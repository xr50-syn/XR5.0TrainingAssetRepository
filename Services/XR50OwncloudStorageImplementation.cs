using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Services
{
    public class OwnCloudStorageServiceImplementation : IStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<OwnCloudStorageServiceImplementation> _logger;

        public OwnCloudStorageServiceImplementation(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<OwnCloudStorageServiceImplementation> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public string GetStorageType() => "OwnCloud";
        public bool SupportsSharing() => true;
        public async Task<bool> CreateTenantStorageAsync(string tenantName, XR50Tenant tenant)
        {
            try
            {
                _logger.LogInformation("Creating OwnCloud storage for tenant: {TenantName}", tenantName);

                // Create group
                var groupCreated = await CreateGroupAsync(tenant.TenantGroup);
                if (!groupCreated)
                {
                    _logger.LogError("Failed to create group for tenant: {TenantName}", tenantName);
                    return false;
                }

                // Create admin user
                if (tenant.Owner != null)
                {
                    var userCreated = await CreateUserAsync(tenant.Owner, tenant.TenantGroup);
                    if (!userCreated)
                    {
                        _logger.LogError("Failed to create user for tenant: {TenantName}", tenantName);
                        return false;
                    }

                    // Create directory
                    var dirCreated = await CreateDirectoryAsync(tenant.TenantDirectory, tenant.Owner);
                    if (!dirCreated)
                    {
                        _logger.LogError("Failed to create directory for tenant: {TenantName}", tenantName);
                        return false;
                    }
                }

                _logger.LogInformation("Successfully created OwnCloud storage for tenant: {TenantName}", tenantName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating OwnCloud storage for tenant: {TenantName}", tenantName);
                return false;
            }
        }

        public async Task<bool> DeleteTenantStorageAsync(string tenantName)
        {
            try
            {
                _logger.LogWarning("Deleting OwnCloud storage for tenant: {TenantName}", tenantName);

                // Note: This is a simplified implementation
                // In a real scenario, you'd need to get tenant details first

                // Delete directory (using curl command for simplicity)
                var tenantDirectory = tenantName; // Simplified
                var deleteDirectoryResult = await ExecuteWebDAVCommand("DELETE", tenantDirectory, null);

                _logger.LogInformation("Deleted OwnCloud storage for tenant: {TenantName}", tenantName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting OwnCloud storage for tenant: {TenantName}", tenantName);
                return false;
            }
        }

        public async Task<bool> TenantStorageExistsAsync(string tenantName)
        {
            try
            {
                // Check if tenant directory exists by trying to access it
                var result = await ExecuteWebDAVCommand("HEAD", tenantName, null);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if tenant storage exists: {TenantName}", tenantName);
                return false;
            }
        }

        public async Task<string> UploadFileAsync(string tenantName, string fileName, Stream fileStream, string contentType = "application/octet-stream")
        {
            try
            {
                _logger.LogInformation("Uploading file to OwnCloud: {TenantName}/{FileName}", tenantName, fileName);

                // Create temp file for upload
                string tempFileName = Path.GetTempFileName();

                try
                {
                    using (var fileWriteStream = File.Create(tempFileName))
                    {
                        await fileStream.CopyToAsync(fileWriteStream);
                    }

                    var success = await ExecuteWebDAVCommand("PUT", $"{tenantName}/{fileName}", tempFileName);

                    if (success)
                    {
                        var url = $"/owncloud/{tenantName}/{fileName}";
                        _logger.LogInformation("Successfully uploaded file to OwnCloud: {Url}", url);
                        return url;
                    }

                    throw new InvalidOperationException("File upload failed");
                }
                finally
                {
                    // Clean up temp file
                    if (File.Exists(tempFileName))
                    {
                        File.Delete(tempFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to OwnCloud: {TenantName}/{FileName}", tenantName, fileName);
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string tenantName, string fileName)
        {
            try
            {
                _logger.LogInformation("Downloading file from OwnCloud: {TenantName}/{FileName}", tenantName, fileName);

                // For OwnCloud, we'd need to implement proper WebDAV download
                // This is a simplified placeholder
                var webdavBase = _configuration.GetValue<string>("TenantSettings:BaseWebDAV");
                var url = $"{webdavBase}/{tenantName}/{fileName}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStreamAsync();
                }

                throw new InvalidOperationException($"Download failed with status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file from OwnCloud: {TenantName}/{FileName}", tenantName, fileName);
                throw;
            }
        }

        public async Task<string> GetDownloadUrlAsync(string tenantName, string fileName, TimeSpan? expiration = null)
        {
            try
            {
                // For OwnCloud, generate a direct WebDAV URL
                // Note: This doesn't implement actual expiration - that would require OwnCloud share API
                var webdavBase = _configuration.GetValue<string>("TenantSettings:BaseWebDAV");
                var url = $"{webdavBase}/{tenantName}/{fileName}";

                _logger.LogInformation("Generated OwnCloud download URL: {TenantName}/{FileName}", tenantName, fileName);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate download URL: {TenantName}/{FileName}", tenantName, fileName);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string tenantName, string fileName)
        {
            try
            {
                _logger.LogInformation("Deleting file from OwnCloud: {TenantName}/{FileName}", tenantName, fileName);

                var success = await ExecuteWebDAVCommand("DELETE", $"{tenantName}/{fileName}", null);

                if (success)
                {
                    _logger.LogInformation("Successfully deleted file from OwnCloud: {TenantName}/{FileName}", tenantName, fileName);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file from OwnCloud: {TenantName}/{FileName}", tenantName, fileName);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string tenantName, string fileName)
        {
            try
            {
                // Check if file exists using HEAD request
                return await ExecuteWebDAVCommand("HEAD", $"{tenantName}/{fileName}", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file exists in OwnCloud: {TenantName}/{FileName}", tenantName, fileName);
                return false;
            }
        }

        public async Task<long> GetFileSizeAsync(string tenantName, string fileName)
        {
            try
            {
                // This would require implementing WebDAV PROPFIND
                // For now, return 0 as placeholder
                _logger.LogInformation("Getting file size from OwnCloud: {TenantName}/{FileName}", tenantName, fileName);
                return 0; // Placeholder
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file size from OwnCloud: {TenantName}/{FileName}", tenantName, fileName);
                return 0;
            }
        }

        public async Task<StorageStatistics> GetStorageStatisticsAsync(string tenantName)
        {
            try
            {
                // This would require implementing WebDAV directory listing
                // For now, return placeholder statistics
                return new StorageStatistics
                {
                    TenantName = tenantName,
                    StorageType = "OwnCloud",
                    TotalFiles = 0, // Placeholder
                    TotalSizeBytes = 0 // Placeholder
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get storage statistics for tenant: {TenantName}", tenantName);
                return new StorageStatistics { TenantName = tenantName, StorageType = "OwnCloud" };
            }
        }

        #region OwnCloud Helper Methods

        private async Task<bool> CreateGroupAsync(string groupName)
        {
            try
            {
                var values = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("groupid", groupName)
                };

                var messageContent = new FormUrlEncodedContent(values);
                var uri_path = _configuration.GetValue<string>("TenantSettings:GroupsPath");

                var request = new HttpRequestMessage(HttpMethod.Post, uri_path)
                {
                    Content = messageContent
                };

                AddBasicAuthHeader(request);

                var result = await _httpClient.SendAsync(request);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create group: {GroupName}", groupName);
                return false;
            }
        }

        private async Task<bool> CreateUserAsync(User user, string groupName)
        {
            try
            {
                var values = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("userid", user.UserName),
                    new KeyValuePair<string, string>("password", user.Password),
                    new KeyValuePair<string, string>("email", user.UserEmail),
                    new KeyValuePair<string, string>("display", user.FullName),
                    new KeyValuePair<string, string>("groups[]", groupName)
                };

                var messageContent = new FormUrlEncodedContent(values);
                var uri_path = _configuration.GetValue<string>("TenantSettings:UsersPath");

                var request = new HttpRequestMessage(HttpMethod.Post, uri_path)
                {
                    Content = messageContent
                };

                AddBasicAuthHeader(request);

                var result = await _httpClient.SendAsync(request);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user: {UserName}", user.UserName);
                return false;
            }
        }

        private async Task<bool> CreateDirectoryAsync(string directoryPath, User user)
        {
            try
            {
                return await ExecuteWebDAVCommand("MKCOL", directoryPath, null, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directory: {DirectoryPath}", directoryPath);
                return false;
            }
        }

        private async Task<bool> ExecuteWebDAVCommand(string method, string path, string filePath, User user = null)
        {
            try
            {
                var webdavBase = _configuration.GetValue<string>("TenantSettings:BaseWebDAV");
                var username = user?.UserName ?? _configuration.GetValue<string>("TenantSettings:Admin");
                var password = user?.Password ?? _configuration.GetValue<string>("TenantSettings:Password");

                var encodedPath = System.Web.HttpUtility.UrlEncode(path);
                var args = method switch
                {
                    "PUT" when !string.IsNullOrEmpty(filePath) =>
                        $"-X PUT -u {username}:{password} --data-binary @\"{filePath}\" \"{webdavBase}/{encodedPath}\"",
                    "DELETE" =>
                        $"-X DELETE -u {username}:{password} \"{webdavBase}/{encodedPath}\"",
                    "MKCOL" =>
                        $"-X MKCOL -u {username}:{password} \"{webdavBase}/{encodedPath}/\"",
                    "HEAD" =>
                        $"-X HEAD -u {username}:{password} \"{webdavBase}/{encodedPath}\"",
                    _ => throw new ArgumentException($"Unsupported WebDAV method: {method}")
                };

                _logger.LogDebug("Executing WebDAV command: curl {Args}", args.Replace(password, "***"));

                var startInfo = new ProcessStartInfo
                {
                    FileName = "curl",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start curl process");
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                _logger.LogDebug("WebDAV command completed. Exit code: {ExitCode}", process.ExitCode);

                if (!string.IsNullOrEmpty(error) && process.ExitCode != 0)
                {
                    _logger.LogWarning("WebDAV command stderr: {Error}", error);
                }

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute WebDAV command: {Method} {Path}", method, path);
                return false;
            }
        }

        private void AddBasicAuthHeader(HttpRequestMessage request)
        {
            var username = _configuration.GetValue<string>("TenantSettings:Admin");
            var password = _configuration.GetValue<string>("TenantSettings:Password");
            var authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
        }
        #endregion

        #region Share Management

        /// <summary>
        /// Create a share in OwnCloud and return the share URL
        /// </summary>
        public async Task<string> CreateShareAsync(string tenantName, XR50Tenant tenant, Asset asset)
        {
            try
            {
                _logger.LogInformation("Creating OwnCloud share for asset {AssetId} in tenant {TenantName}",
                    asset.Id, tenantName);

                var values = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("shareType", "1"), // Group share
                    new KeyValuePair<string, string>("shareWith", tenant.TenantGroup ?? ""),
                    new KeyValuePair<string, string>("permissions", "1"), // Read permission
                    new KeyValuePair<string, string>("path", $"{tenant.TenantDirectory}/{asset.Filename}")
                };

                var messageContent = new FormUrlEncodedContent(values);
                var uri_base = _configuration.GetValue<string>("TenantSettings:BaseAPI");
                var uri_share = _configuration.GetValue<string>("TenantSettings:SharesPath");

                var request = new HttpRequestMessage(HttpMethod.Post, uri_share)
                {
                    Content = messageContent
                };

                AddBasicAuthHeader(request, tenant.Owner);

                _httpClient.BaseAddress = new Uri(uri_base);
                var result = await _httpClient.SendAsync(request);

                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var shareUrl = ParseShareUrlFromResponse(content);

                    _logger.LogInformation("OwnCloud share created successfully for asset {AssetId}: {ShareUrl}",
                        asset.Id, shareUrl);

                    return shareUrl;
                }
                else
                {
                    _logger.LogError("Failed to create OwnCloud share for asset {AssetId}. Status: {StatusCode}",
                        asset.Id, result.StatusCode);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating OwnCloud share for asset {AssetId}", asset.Id);
                return string.Empty;
            }
        }

        /// <summary>
        /// Delete a share from OwnCloud
        /// </summary>
        public async Task<bool> DeleteShareAsync(string tenantName, string shareId)
        {
            try
            {
                _logger.LogInformation("Deleting OwnCloud share {ShareId} for tenant {TenantName}",
                    shareId, tenantName);

                var uri_base = _configuration.GetValue<string>("TenantSettings:BaseAPI");
                var uri_share = _configuration.GetValue<string>("TenantSettings:SharesPath");

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{uri_share}/{shareId}");

                // For deletion, we'd need tenant info to get the owner credentials
                // This is a simplification - in practice you'd need to get the tenant
                var username = _configuration.GetValue<string>("TenantSettings:Admin");
                var password = _configuration.GetValue<string>("TenantSettings:Password");
                AddBasicAuthHeader(request, username, password);

                _httpClient.BaseAddress = new Uri(uri_base);
                var result = await _httpClient.SendAsync(request);

                if (result.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully deleted OwnCloud share {ShareId}", shareId);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to delete OwnCloud share {ShareId}. Status: {StatusCode}",
                        shareId, result.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting OwnCloud share {ShareId}", shareId);
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Parse the share URL from OwnCloud XML response
        /// </summary>
        private string ParseShareUrlFromResponse(string xmlResponse)
        {
            try
            {
                // Simple XML parsing for share URL
                var doc = System.Xml.Linq.XDocument.Parse(xmlResponse);
                var url = doc.Root?.Element("data")?.Element("url")?.Value;
                return url ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing share URL from OwnCloud response");
                return string.Empty;
            }
        }

        /// <summary>
        /// Add basic authentication header for OwnCloud requests
        /// </summary>
        private void AddBasicAuthHeader(HttpRequestMessage request, User user)
        {
            if (user != null)
            {
                var authenticationString = $"{user.UserName}:{user.Password}";
                var base64EncodedAuthenticationString = Convert.ToBase64String(
                    System.Text.Encoding.ASCII.GetBytes(authenticationString));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic", base64EncodedAuthenticationString);
            }
        }

        /// <summary>
        /// Add basic authentication header with username/password
        /// </summary>
        private void AddBasicAuthHeader(HttpRequestMessage request, string username, string password)
        {
            var authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes(authenticationString));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic", base64EncodedAuthenticationString);
        }

        #endregion

        // ... rest of existing methods ...
    }
}
        