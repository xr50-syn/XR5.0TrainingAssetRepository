using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Services;
using Amazon.S3;
using Amazon.S3.Model;

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
        private readonly IXR50TenantDbContextFactory _dbContextFactory;
        private readonly IMaterialService _materialService;
        private readonly IXR50TenantManagementService _tenantManagementService;
        private readonly IStorageService _storageService; // Unified storage interface
        private readonly ILogger<AssetService> _logger;

        public AssetService(
            IConfiguration configuration,
            IXR50TenantDbContextFactory dbContextFactory,
            IMaterialService materialService,
            IXR50TenantManagementService tenantManagementService,
            IStorageService storageService,
            ILogger<AssetService> logger)
        {
            _configuration = configuration;
            _dbContextFactory = dbContextFactory;
            _materialService = materialService;
            _tenantManagementService = tenantManagementService;
            _storageService = storageService;
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
                _logger.LogInformation("Creating asset for tenant: {TenantName} using {StorageType} storage",
                    tenantName, _storageService.GetStorageType());

                // Validate tenant exists
                var tenant = await _tenantManagementService.GetTenantAsync(tenantName);
                if (tenant == null)
                {
                    throw new ArgumentException($"Tenant '{tenantName}' not found");
                }

                // Auto-detect filetype if not provided
                if (string.IsNullOrEmpty(asset.Filetype) && !string.IsNullOrEmpty(asset.Filename))
                {
                    asset.Filetype = GetFiletypeFromFilename(asset.Filename);
                }

                // Ensure tenant storage exists
                if (!await _storageService.TenantStorageExistsAsync(tenantName))
                {
                    _logger.LogInformation("Creating storage for tenant: {TenantName}", tenantName);
                    var storageCreated = await _storageService.CreateTenantStorageAsync(tenantName, tenant);
                    if (!storageCreated)
                    {
                        throw new InvalidOperationException($"Failed to create storage for tenant '{tenantName}'");
                    }
                }

                // Upload file using unified storage interface
                _logger.LogInformation("Uploading file: {Filename} to {StorageType}",
                    asset.Filename, _storageService.GetStorageType());

                var contentType = GetContentType(asset.Filename, asset.Filetype);
                using var fileStream = file.OpenReadStream();
                var storageUrl = await _storageService.UploadFileAsync(tenantName, asset.Filename, fileStream, contentType);

                // Update asset with storage URL
                asset.Src = storageUrl;

                // Save to database
                context.Assets.Add(asset);
                await context.SaveChangesAsync();

                _logger.LogInformation("Created asset: {Filename} (Type: {Filetype}) with ID: {Id} in {StorageType}",
                    asset.Filename, asset.Filetype, asset.Id, _storageService.GetStorageType());

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

            _logger.LogInformation("Updated asset: {Id} ({Filename})", asset.Id, asset.Filename);
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

            // Delete from storage
            try
            {
                var deleted = await _storageService.DeleteFileAsync(tenantName, asset.Filename);
                if (!deleted)
                {
                    _logger.LogWarning("Failed to delete file from {StorageType}: {Filename}",
                        _storageService.GetStorageType(), asset.Filename);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from {StorageType} for asset {Id}",
                    _storageService.GetStorageType(), id);
            }

            // Delete from database
            context.Assets.Remove(asset);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted asset: {Id} ({Filename}) from {StorageType}",
                id, asset.Filename, _storageService.GetStorageType());

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

            return await _materialService.GetMaterialsByAssetIdAsync(assetId.ToString());
        }

        public async Task<int> GetAssetUsageCountAsync(int assetId)
        {
            var materials = await GetMaterialsUsingAssetAsync(assetId);
            return materials.Count();
        }

        #endregion

        #region File Management

        public async Task<string> GetAssetDownloadUrlAsync(int assetId)
        {
            var asset = await GetAssetAsync(assetId);
            if (asset == null)
            {
                throw new ArgumentException($"Asset with ID {assetId} not found");
            }

            try
            {
                // Extract tenant name from context or determine from asset
                // For this implementation, we'll use a simplified approach
                // In production, you might store tenant info with the asset or extract it from context
                var tenantName = ExtractTenantNameFromContext(); // Implement this method based on your needs

                var downloadUrl = await _storageService.GetDownloadUrlAsync(tenantName, asset.Filename);

                _logger.LogInformation("Generated download URL for asset {AssetId} ({Filename}) from {StorageType}",
                    assetId, asset.Filename, _storageService.GetStorageType());

                return downloadUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate download URL for asset {AssetId}", assetId);
                throw;
            }
        }

        public async Task<Asset> UploadAssetAsync(IFormFile file, string tenantName, string filename, string? description = null)
        {
            _logger.LogInformation("Uploading file {Filename} to {StorageType} for tenant: {TenantName}",
                filename, _storageService.GetStorageType(), tenantName);

            var asset = new Asset
            {
                Filename = filename,
                Description = description,
                Filetype = GetFiletypeFromFilename(filename)
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

            try
            {
                var tenantName = ExtractTenantNameFromContext(); // Implement based on your needs
                var deleted = await _storageService.DeleteFileAsync(tenantName, asset.Filename);

                _logger.LogInformation("Deleted file from {StorageType} for asset {AssetId} ({Filename}): {Success}",
                    _storageService.GetStorageType(), assetId, asset.Filename, deleted);

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file from {StorageType} for asset {AssetId}",
                    _storageService.GetStorageType(), assetId);
                return false;
            }
        }

        public async Task<long> GetAssetFileSizeAsync(int assetId)
        {
            var asset = await GetAssetAsync(assetId);
            if (asset == null)
            {
                return 0;
            }

            try
            {
                var tenantName = ExtractTenantNameFromContext(); // Implement based on your needs
                var size = await _storageService.GetFileSizeAsync(tenantName, asset.Filename);

                _logger.LogInformation("Retrieved file size from {StorageType} for asset {AssetId} ({Filename}): {Size} bytes",
                    _storageService.GetStorageType(), assetId, asset.Filename, size);

                return size;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file size from {StorageType} for asset {AssetId}",
                    _storageService.GetStorageType(), assetId);
                return 0;
            }
        }

        public async Task<bool> AssetFileExistsAsync(int assetId)
        {
            var asset = await GetAssetAsync(assetId);
            if (asset == null)
            {
                return false;
            }

            try
            {
                var tenantName = ExtractTenantNameFromContext(); // Implement based on your needs
                var exists = await _storageService.FileExistsAsync(tenantName, asset.Filename);

                _logger.LogInformation("Checked file existence in {StorageType} for asset {AssetId} ({Filename}): {Exists}",
                    _storageService.GetStorageType(), assetId, asset.Filename, exists);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check file existence in {StorageType} for asset {AssetId}",
                    _storageService.GetStorageType(), assetId);
                return false;
            }
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

            // Calculate storage statistics using the unified storage service
            long totalStorageUsed = 0;
            try
            {
                var tenantName = ExtractTenantNameFromContext(); // Implement based on your needs
                var storageStats = await _storageService.GetStorageStatisticsAsync(tenantName);
                totalStorageUsed = storageStats.TotalSizeBytes;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get storage statistics from {StorageType}",
                    _storageService.GetStorageType());
            }

            var statistics = new AssetStatistics
            {
                TotalAssets = totalAssets,
                FiletypeBreakdown = filetypeGroups.ToDictionary(g => g.Filetype ?? "unknown", g => g.Count),
                TotalStorageUsed = totalStorageUsed,
                AverageFileSize = totalAssets > 0 ? totalStorageUsed / totalAssets : 0,
                StorageType = _storageService.GetStorageType()
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

        private string GetContentType(string filename, string? filetype = null)
        {
            var extension = Path.GetExtension(filename).ToLowerInvariant();

            return extension switch
            {
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".webm" => "video/webm",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".md" => "text/markdown",
                ".json" => "application/json",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".ogg" => "audio/ogg",
                ".flac" => "audio/flac",
                _ => "application/octet-stream"
            };
        }

        private string ExtractTenantNameFromContext()
        {
            // This is a placeholder - implement based on your tenant resolution logic
            // You might get this from HTTP context, dependency injection, or other means
            // For now, return a default value
            return "default";
        }

        #endregion
    }

    #region Updated Asset DTOs and Models

    public class AssetStatistics
    {
        public int TotalAssets { get; set; }
        public Dictionary<string, int> FiletypeBreakdown { get; set; } = new();
        public long TotalStorageUsed { get; set; } // In bytes
        public long AverageFileSize { get; set; } // In bytes
        public string StorageType { get; set; } = ""; // "S3" or "OwnCloud"
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