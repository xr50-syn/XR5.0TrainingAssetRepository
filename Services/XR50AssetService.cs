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
        Task<bool> DeleteAssetAsync(string tenantName, int id);
        Task<bool> AssetExistsAsync(int id);
        
        // Asset Search and Filtering
        Task<IEnumerable<Asset>> GetAssetsByFiletypeAsync(string filetype);
        Task<IEnumerable<Asset>> SearchAssetsByFilenameAsync(string searchTerm);
        Task<IEnumerable<Asset>> GetAssetsByDescriptionAsync(string searchTerm);
        
        // Asset Relationships
        Task<IEnumerable<Material>> GetMaterialsUsingAssetAsync(int assetId);
        Task<int> GetAssetUsageCountAsync(int assetId);
        
        // File Management with Storage Service
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
                // Upload file to storage first
                using var stream = file.OpenReadStream();
                var uploadResult = await _storageService.UploadFileAsync(tenantName, asset.Filename, stream, file.ContentType);
                
                // Update asset with storage URL
                asset.Src = uploadResult;
                
                // Save to database
                context.Assets.Add(asset);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Created asset {AssetId} ({Filename}) in {StorageType} storage", 
                    asset.Id, asset.Filename, _storageService.GetStorageType());
                
                return asset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create asset {Filename} for tenant {TenantName}", 
                    asset.Filename, tenantName);
                throw;
            }
        }

        public async Task<Asset> UpdateAssetAsync(Asset asset)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            context.Assets.Update(asset);
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Updated asset {AssetId} ({Filename})", asset.Id, asset.Filename);
            
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

            try
            {
                // Delete file from storage
                var storageDeleted = await _storageService.DeleteFileAsync(tenantName, asset.Filename);
                if (!storageDeleted)
                {
                    _logger.LogWarning("Failed to delete file {Filename} from storage, but continuing with database deletion", 
                        asset.Filename);
                }

                // Delete from database
                context.Assets.Remove(asset);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted asset {AssetId} ({Filename}) from {StorageType} storage", 
                    id, asset.Filename, _storageService.GetStorageType());
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete asset {AssetId} ({Filename})", id, asset.Filename);
                throw;
            }
        }

        public async Task<bool> AssetExistsAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Assets.AnyAsync(a => a.Id == id);
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
                var tenantName = ExtractTenantNameFromContext();

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
            try
            {
                _logger.LogInformation("Uploading asset {Filename} to {StorageType} storage", filename, _storageService.GetStorageType());
                
                // Upload file to storage
                using var stream = file.OpenReadStream();
                var uploadResult = await _storageService.UploadFileAsync(tenantName, filename, stream, file.ContentType);
                
                // Create asset record
                var asset = new Asset
                {
                    Filename = filename,
                    Description = description,
                    Filetype = GetFiletypeFromFilename(filename),
                    Src = uploadResult
                };

                return await CreateAssetAsync(asset, tenantName, file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload asset {Filename} to tenant {TenantName}", filename, tenantName);
                throw;
            }
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
                var tenantName = ExtractTenantNameFromContext();
                var deleted = await _storageService.DeleteFileAsync(tenantName, asset.Filename);
                
                _logger.LogInformation("Deleted file for asset {AssetId} ({Filename}) from {StorageType} storage", 
                    assetId, asset.Filename, _storageService.GetStorageType());
                
                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file for asset {AssetId} ({Filename})", assetId, asset.Filename);
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
                var tenantName = ExtractTenantNameFromContext();
                var size = await _storageService.GetFileSizeAsync(tenantName, asset.Filename);
                
                _logger.LogInformation("Retrieved file size for asset {AssetId} ({Filename}): {Size} bytes", 
                    assetId, asset.Filename, size);
                
                return size;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file size for asset {AssetId} ({Filename})", assetId, asset.Filename);
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
                var tenantName = ExtractTenantNameFromContext();
                var exists = await _storageService.FileExistsAsync(tenantName, asset.Filename);
                
                _logger.LogInformation("File existence check for asset {AssetId} ({Filename}): {Exists}", 
                    assetId, asset.Filename, exists);
                
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check file existence for asset {AssetId} ({Filename})", assetId, asset.Filename);
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

            // Calculate total storage used by querying storage service
            long totalStorageUsed = 0;
            try
            {
                var tenantName = ExtractTenantNameFromContext();
                var storageStats = await _storageService.GetStorageStatisticsAsync(tenantName);
                totalStorageUsed = storageStats.TotalSizeBytes;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get storage statistics for asset statistics calculation");
            }

            var statistics = new AssetStatistics
            {
                TotalAssets = totalAssets,
                FiletypeBreakdown = filetypeGroups.ToDictionary(g => g.Filetype ?? "unknown", g => g.Count),
                TotalStorageUsed = totalStorageUsed,
                AverageFileSize = totalAssets > 0 ? totalStorageUsed / totalAssets : 0
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

        private string ExtractTenantNameFromContext()
        {
            // This is a simplified implementation
            // In a real scenario, you might extract tenant from:
            // - HTTP context (URL path, headers, claims)
            // - Database lookup
            // - Configuration
            
            // For now, return a default tenant name
            // TODO: Implement proper tenant resolution
            return "default-tenant";
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