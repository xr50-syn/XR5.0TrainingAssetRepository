using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Services;
using System.Diagnostics;

namespace XR50TrainingAssetRepo.Services
{
    public interface IAssetService
    {
        // Basic Asset Operations
        Task<IEnumerable<Asset>> GetAllAssetsAsync();
        Task<Asset?> GetAssetAsync(int id);
        Task<Asset> CreateAssetAsync(Asset asset, string tenantName, IFormFile file);
        Task<Asset> UpdateAssetAsync(Asset asset);
        Task<bool> DeleteAssetAsync(string tenantName,int id);
        Task<bool> AssetExistsAsync(int id);
        
        // Asset Search and Filtering
        Task<IEnumerable<Asset>> GetAssetsByFiletypeAsync(string filetype);
        Task<IEnumerable<Asset>> SearchAssetsByFilenameAsync(string searchTerm);
        Task<IEnumerable<Asset>> GetAssetsByDescriptionAsync(string searchTerm);
        
        // Asset Relationships
        Task<IEnumerable<Material>> GetMaterialsUsingAssetAsync(int assetId);
        Task<int> GetAssetUsageCountAsync(int assetId);
        
        // File Management Placeholders (to be implemented with OwnCloud/S3)
        Task<string> GetAssetDownloadUrlAsync(int assetId);
        Task<Asset> UploadAssetAsync(IFormFile file, string tenantName, string filename, string? description = null);
        Task<bool> DeleteAssetFileAsync(int assetId);
        Task<long> GetAssetFileSizeAsync(int assetId);
        Task<bool> AssetFileExistsAsync(int assetId);
        
        // Asset Statistics
        Task<AssetStatistics> GetAssetStatisticsAsync();
    }

    public class AssetService : IAssetService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IXR50TenantDbContextFactory _dbContextFactory;
        private readonly IMaterialService _materialService;
        private readonly IXR50TenantManagementService _tenantManagementService;
        private readonly ILogger<AssetService> _logger;

        public AssetService(
            IConfiguration configuration,
            HttpClient httpClient,
            IXR50TenantDbContextFactory dbContextFactory,
            IMaterialService materialService,
            IXR50TenantManagementService tenantManagementService,
            ILogger<AssetService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _dbContextFactory = dbContextFactory;
            _materialService = materialService;
            _tenantManagementService = tenantManagementService;
            _logger = logger;
        }

        #region Basic Asset Operations

        public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Assets.OrderBy(a => a.Filename).ToListAsync();
        }

        public async Task<Asset?> GetAssetAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Assets.FindAsync(id);
        }

        public async Task<Asset> CreateAssetAsync(Asset asset, string tenantName, IFormFile file)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            try
            {
                // Add null checks and logging
                _logger.LogInformation("Starting asset creation for tenant: {TenantName}", tenantName);
                
                // Check if required services are available
                if (_tenantManagementService == null)
                {
                    _logger.LogError("TenantManagementService is null");
                    throw new InvalidOperationException("TenantManagementService is not available");
                }
                
                if (_configuration == null)
                {
                    _logger.LogError("Configuration is null");
                    throw new InvalidOperationException("Configuration is not available");
                }

                // Auto-detect filetype from filename if not provided
                if (string.IsNullOrEmpty(asset.Filetype) && !string.IsNullOrEmpty(asset.Filename))
                {
                    asset.Filetype = GetFiletypeFromFilename(asset.Filename);
                }

                _logger.LogInformation("Getting tenant information for: {TenantName}", tenantName);
                var tenant = await _tenantManagementService.GetTenantAsync(tenantName);
                
                if (tenant == null)
                {
                    _logger.LogError("Tenant not found: {TenantName}", tenantName);
                    throw new ArgumentException($"Tenant '{tenantName}' not found");
                }

                _logger.LogInformation("Found tenant: {TenantName}, Owner: {OwnerName}, Schema: {TenantSchema}", 
                    tenant.TenantName, tenant.OwnerName, tenant.TenantSchema);

                if (string.IsNullOrEmpty(tenant.OwnerName))
                {
                    _logger.LogError("Tenant {TenantName} has no owner configured", tenantName);
                    throw new InvalidOperationException($"Tenant '{tenantName}' has no owner configured");
                }

                // FIX: Use the tenant's database schema name instead of manually constructing it
                string tenantDatabaseName = tenant.TenantSchema ?? throw new InvalidOperationException($"Tenant '{tenantName}' has no schema configured");
                
                _logger.LogInformation("Getting owner user for: {OwnerName} from database: {TenantDatabase}", 
                    tenant.OwnerName, tenantDatabaseName);
                
                var adminUser = await _tenantManagementService.GetOwnerUserAsync(tenant.OwnerName, tenantDatabaseName);
                
                if (adminUser == null)
                {
                    _logger.LogError("Owner user not found: {OwnerName} for tenant: {TenantName} in database: {TenantDatabase}", 
                        tenant.OwnerName, tenantName, tenantDatabaseName);
                    throw new InvalidOperationException($"Owner user '{tenant.OwnerName}' not found for tenant '{tenantName}' in database '{tenantDatabaseName}'");
                }

                string username = adminUser.UserName ?? throw new InvalidOperationException("Admin user has no username");
                string password = adminUser.Password ?? throw new InvalidOperationException("Admin user has no password");
                
                string? webdav_base = _configuration.GetValue<string>("TenantSettings:BaseWebDAV");
                if (string.IsNullOrEmpty(webdav_base))
                {
                    _logger.LogError("BaseWebDAV configuration is missing");
                    throw new InvalidOperationException("BaseWebDAV configuration is not set");
                }

                if (string.IsNullOrEmpty(tenant.TenantDirectory))
                {
                    _logger.LogError("Tenant {TenantName} has no directory configured", tenantName);
                    throw new InvalidOperationException($"Tenant '{tenantName}' has no directory configured");
                }

                // Rest of the method continues as before...
                // Create temp file for upload
                _logger.LogInformation("Creating temporary file for upload");
                string tempFileName = Path.GetTempFileName();
                
                try
                {
                    using (var stream = System.IO.File.Create(tempFileName))
                    {
                        await file.CopyToAsync(stream);
                    }

                    string cmd = "curl";
                    string dirl = System.Web.HttpUtility.UrlEncode(tenant.TenantDirectory);
                    string Arg = $"-X PUT -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\" --data-binary @\"{tempFileName}\" \"{webdav_base}/{dirl}/{asset.Filename}\"";
                    
                    _logger.LogInformation("Executing WebDAV upload command for file: {Filename}", asset.Filename);
                    _logger.LogDebug("Command: {Command} {Args}", cmd, Arg.Replace(password, "***"));

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = cmd,
                        Arguments = Arg,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process == null)
                        {
                            throw new InvalidOperationException("Failed to start curl process");
                        }

                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitAsync();
                        
                        _logger.LogInformation("Upload process completed. Exit code: {ExitCode}", process.ExitCode);
                        _logger.LogDebug("Output: {Output}", output);
                        
                        if (!string.IsNullOrEmpty(error))
                        {
                            _logger.LogWarning("Upload stderr: {Error}", error);
                        }

                        if (process.ExitCode != 0)
                        {
                            _logger.LogError("Upload failed with exit code: {ExitCode}", process.ExitCode);
                            throw new InvalidOperationException($"File upload failed. Exit code: {process.ExitCode}. Error: {error}");
                        }
                    }
                }
                finally
                {
                    // Clean up temp file
                    try
                    {
                        if (System.IO.File.Exists(tempFileName))
                        {
                            System.IO.File.Delete(tempFileName);
                            _logger.LogDebug("Cleaned up temporary file: {TempFile}", tempFileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to clean up temporary file: {TempFile}", tempFileName);
                    }
                }

                // Save to database
                _logger.LogInformation("Saving asset to database");
                context.Assets.Add(asset);
                await context.SaveChangesAsync();

                _logger.LogInformation("Created asset: {Filename} (Type: {Filetype}) with ID: {Id}", 
                    asset.Filename, asset.Filetype, asset.Id);
                
                return asset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating asset {Filename} for tenant {TenantName}: {Error}", 
                    asset.Filename, tenantName, ex.Message);
                throw;
            }
        }
        public async Task<Asset> UpdateAssetAsync(Asset asset)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            context.Entry(asset).State = EntityState.Modified;
            await context.SaveChangesAsync();

            _logger.LogInformation("Updated asset: {Id} ({Filename})", 
                asset.Id, asset.Filename);
            
            return asset;
        }

        public async Task<bool> DeleteAssetAsync(string tenantName, int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var asset = await context.Assets.FindAsync(id);
            if (asset == null)
            {
                return false;
            }

            // Check if asset is being used by materials
            var materialsUsingAsset = await GetMaterialsUsingAssetAsync(id);
            if (materialsUsingAsset.Any())
            {
                _logger.LogWarning("Cannot delete asset {Id} ({Filename}) - it is being used by {Count} materials", 
                    id, asset.Filename, materialsUsingAsset.Count());
                return false;
            }

            // Get tenant information
            var tenant = await _tenantManagementService.GetTenantAsync(tenantName);
            if (tenant == null)
            {
                _logger.LogError("Tenant not found: {TenantName}", tenantName);
                return false;
            }

            _logger.LogInformation("Got tenant info, owner: {OwnerName}", tenant.OwnerName);

            // FIX: Use the tenant's database schema name instead of manually constructing it
            string tenantDatabaseName = tenant.TenantSchema ?? throw new InvalidOperationException($"Tenant '{tenantName}' has no schema configured");
            
            var adminUser = await _tenantManagementService.GetOwnerUserAsync(tenant.OwnerName, tenantDatabaseName);
            if (adminUser == null)
            {
                _logger.LogError("Owner user not found: {OwnerName} for tenant: {TenantName} in database: {TenantDatabase}", 
                    tenant.OwnerName, tenantName, tenantDatabaseName);
                return false;
            }

            string username = adminUser.UserName;
            string password = adminUser.Password; 
            
            string webdav_base = _configuration.GetValue<string>("TenantSettings:BaseWebDAV");
            
            // Create root dir for the TrainingProgram
            string cmd = "curl";
            string dirl = System.Web.HttpUtility.UrlEncode(tenant.TenantDirectory);
            string Arg = $"-X DELETE -u {username}:{password} \"{webdav_base}/{dirl}/{asset.Filename}\"";
            
            Console.WriteLine("Executing command: " + cmd + " " + Arg);
            var startInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = Arg,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                Console.WriteLine("Output: " + output);
                Console.WriteLine("Error: " + error);
            }

            context.Assets.Remove(asset);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted asset: {Id} ({Filename})", id, asset.Filename);
            
            return true;
        }
        public async Task<bool> AssetExistsAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Assets.AnyAsync(e => e.Id == id);
        }

        #endregion

        #region Asset Search and Filtering

        public async Task<IEnumerable<Asset>> GetAssetsByFiletypeAsync(string filetype)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await context.Assets
                .Where(a => a.Filetype == filetype)
                .OrderBy(a => a.Filename)
                .ToListAsync();
        }

        public async Task<IEnumerable<Asset>> SearchAssetsByFilenameAsync(string searchTerm)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await context.Assets
                .Where(a => a.Filename.Contains(searchTerm))
                .OrderBy(a => a.Filename)
                .ToListAsync();
        }

        public async Task<IEnumerable<Asset>> GetAssetsByDescriptionAsync(string searchTerm)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await context.Assets
                .Where(a => a.Description != null && a.Description.Contains(searchTerm))
                .OrderBy(a => a.Filename)
                .ToListAsync();
        }

        #endregion

        #region Asset Relationships

        public async Task<IEnumerable<Material>> GetMaterialsUsingAssetAsync(int assetId)
        {
            var asset = await GetAssetAsync(assetId);
            if (asset == null)
            {
                return new List<Material>();
            }

            // Use the MaterialService to find materials using this asset
            return await _materialService.GetMaterialsByAssetIdAsync(assetId.ToString());
        }

        public async Task<int> GetAssetUsageCountAsync(int assetId)
        {
            var materials = await GetMaterialsUsingAssetAsync(assetId);
            return materials.Count();
        }

        #endregion

        #region File Management Placeholders

        public async Task<string> GetAssetDownloadUrlAsync(int assetId)
        {
            var asset = await GetAssetAsync(assetId);
            if (asset == null)
            {
                throw new ArgumentException($"Asset with ID {assetId} not found");
            }

            // TODO: Implement with OwnCloud/S3
            // For now, return a placeholder URL
            _logger.LogInformation("TODO: Generate download URL for asset {AssetId} ({Filename})", 
                assetId, asset.Filename);
            
            return $"/api/assets/{assetId}/download"; // Placeholder
        }

        public async Task<Asset> UploadAssetAsync(IFormFile file, string tenantName,string filename, string? description = null)
        {
            // TODO: Implement file upload to OwnCloud/S3
            _logger.LogInformation("TODO: Upload file {Filename} to storage", filename);
            
            // For now, just create the database record
            var asset = new Asset
            {
                Filename = filename,
                Description = description,
                Filetype = GetFiletypeFromFilename(filename),
                Src = $"/storage/{filename}" // Placeholder path
            };

            return await CreateAssetAsync(asset, tenantName, file);
        }

        public async Task<bool> DeleteAssetFileAsync(int assetId)
        {
            var asset = await GetAssetAsync(assetId);
            if (asset == null)
            {
                return false;
            }

            // TODO: Implement file deletion from OwnCloud/S3
            _logger.LogInformation("TODO: Delete file for asset {AssetId} ({Filename}) from storage", 
                assetId, asset.Filename);
            
            return true; // Placeholder
        }

        public async Task<long> GetAssetFileSizeAsync(int assetId)
        {
            var asset = await GetAssetAsync(assetId);
            if (asset == null)
            {
                return 0;
            }

            // TODO: Implement file size retrieval from OwnCloud/S3
            _logger.LogInformation("TODO: Get file size for asset {AssetId} ({Filename})", 
                assetId, asset.Filename);
            
            return 0; // Placeholder
        }

        public async Task<bool> AssetFileExistsAsync(int assetId)
        {
            var asset = await GetAssetAsync(assetId);
            if (asset == null)
            {
                return false;
            }

            // TODO: Implement file existence check in OwnCloud/S3
            _logger.LogInformation("TODO: Check if file exists for asset {AssetId} ({Filename})", 
                assetId, asset.Filename);
            
            return true; // Placeholder - assume file exists
        }

        #endregion

        #region Asset Statistics

        public async Task<AssetStatistics> GetAssetStatisticsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var totalAssets = await context.Assets.CountAsync();
            var filetypeGroups = await context.Assets
                .GroupBy(a => a.Filetype)
                .Select(g => new { Filetype = g.Key, Count = g.Count() })
                .ToListAsync();

            var statistics = new AssetStatistics
            {
                TotalAssets = totalAssets,
                FiletypeBreakdown = filetypeGroups.ToDictionary(g => g.Filetype ?? "unknown", g => g.Count),
                // TODO: Add file size statistics when storage integration is complete
                TotalStorageUsed = 0,
                AverageFileSize = 0
            };

            return statistics;
        }

        #endregion

        #region Helper Methods

        private string GetFiletypeFromFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return "unknown";

            var extension = Path.GetExtension(filename).ToLowerInvariant();
            
            return extension switch
            {
                ".mp4" or ".avi" or ".mov" or ".wmv" or ".webm" => "video",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" or ".webp" => "image",
                ".pdf" => "document",
                ".doc" or ".docx" => "document",
                ".xls" or ".xlsx" => "spreadsheet",
                ".ppt" or ".pptx" => "presentation",
                ".txt" or ".md" => "text",
                ".json" => "data",
                ".zip" or ".rar" or ".7z" => "archive",
                ".unity" or ".unitypackage" => "unity",
                ".fbx" or ".obj" or ".3ds" => "3d_model",
                ".wav" or ".mp3" or ".ogg" or ".flac" => "audio",
                _ => "unknown"
            };
        }

        #endregion
    }

    #region Asset DTOs and Models

    public class AssetStatistics
    {
        public int TotalAssets { get; set; }
        public Dictionary<string, int> FiletypeBreakdown { get; set; } = new();
        public long TotalStorageUsed { get; set; } // In bytes
        public long AverageFileSize { get; set; } // In bytes
    }

    public class AssetUploadRequest
    {
        public string Filename { get; set; } = "";
        public string? Description { get; set; }
        public string? Filetype { get; set; }
    }

    public class AssetSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? Filetype { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }

    #endregion
}
