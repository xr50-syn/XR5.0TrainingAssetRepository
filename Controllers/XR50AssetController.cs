using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Data;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Controllers
{
         public class FileUploadFormData
        {
            public string? Description { get; set; }
            public string? Src { get; set; }
            public string? Filetype { get; set; }
            public string Filename  { get; set; }
            public IFormFile File { get; set; }
        }

    [Route("api/{tenantName}/[controller]")]
    [ApiController]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly ILogger<AssetsController> _logger;

        public AssetsController(
            IAssetService assetService,
            ILogger<AssetsController> logger)
        {
            _assetService = assetService;
            _logger = logger;
        }

        #region Basic Asset Operations

        // GET: api/{tenantName}/assets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAssets(string tenantName)
        {
            _logger.LogInformation("Getting assets for tenant: {TenantName}", tenantName);
            
            var assets = await _assetService.GetAllAssetsAsync();
            
            _logger.LogInformation("Found {AssetCount} assets for tenant: {TenantName}", 
                assets.Count(), tenantName);
            
            return Ok(assets);
        }

        // GET: api/{tenantName}/assets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Asset>> GetAsset(string tenantName, int id)
        {
            _logger.LogInformation("Getting asset {Id} for tenant: {TenantName}", id, tenantName);
            
            var asset = await _assetService.GetAssetAsync(id);

            if (asset == null)
            {
                _logger.LogWarning("Asset {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            return asset;
        }

        // POST: api/{tenantName}/assets
        [HttpPost]
        public async Task<ActionResult<Asset>> PostAsset(string tenantName, [FromForm] FileUploadFormData fileUpload)
        {
            Asset asset = new Asset();
            asset.Src = fileUpload.Src;
            asset.Description = fileUpload.Description;
            asset.Filetype = fileUpload.Filetype;
            asset.Filename = Guid.NewGuid().ToString();
            if (fileUpload.Filetype != null)
            {
                asset.Filename += $".{fileUpload.Filetype}";
            }

            _logger.LogInformation("Creating asset {Filename} for tenant: {TenantName}", 
                asset.Filename, tenantName);
            
            var createdAsset = await _assetService.CreateAssetAsync(asset, tenantName, fileUpload.File);

            _logger.LogInformation("Created asset {Filename} with ID {Id} for tenant: {TenantName}", 
                createdAsset.Filename, createdAsset.Id, tenantName);

            return CreatedAtAction(nameof(GetAsset), 
                new { tenantName, id = createdAsset.Id }, 
                createdAsset);
        }

        // PUT: api/{tenantName}/assets/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsset(string tenantName, int id, Asset asset)
        {
            if (id != asset.Id)
            {
                return BadRequest("ID mismatch");
            }

            _logger.LogInformation("Updating asset {Id} for tenant: {TenantName}", id, tenantName);
            
            try
            {
                await _assetService.UpdateAssetAsync(asset);
                _logger.LogInformation("Updated asset {Id} for tenant: {TenantName}", id, tenantName);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _assetService.AssetExistsAsync(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/{tenantName}/assets/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsset(string tenantName, int id)
        {
            _logger.LogInformation("Deleting asset {Id} for tenant: {TenantName}", id, tenantName);
            
            var deleted = await _assetService.DeleteAssetAsync(tenantName, id);
            
            if (!deleted)
            {
                return BadRequest("Asset not found or is being used by materials");
            }

            _logger.LogInformation("Deleted asset {Id} for tenant: {TenantName}", id, tenantName);

            return NoContent();
        }

        #endregion

        #region Asset Search and Filtering

        // GET: api/{tenantName}/assets/search?searchTerm=video&filetype=mp4
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Asset>>> SearchAssets(
            string tenantName, 
            [FromQuery] string? searchTerm = null, 
            [FromQuery] string? filetype = null)
        {
            _logger.LogInformation("Searching assets for tenant: {TenantName} (term: {SearchTerm}, type: {Filetype})", 
                tenantName, searchTerm, filetype);
            
            IEnumerable<Asset> assets;

            if (!string.IsNullOrEmpty(filetype))
            {
                assets = await _assetService.GetAssetsByFiletypeAsync(filetype);
            }
            else if (!string.IsNullOrEmpty(searchTerm))
            {
                assets = await _assetService.SearchAssetsByFilenameAsync(searchTerm);
            }
            else
            {
                assets = await _assetService.GetAllAssetsAsync();
            }
            
            _logger.LogInformation("Found {Count} assets matching search criteria for tenant: {TenantName}", 
                assets.Count(), tenantName);
            
            return Ok(assets);
        }

        // GET: api/{tenantName}/assets/by-filetype/video
        [HttpGet("by-filetype/{filetype}")]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAssetsByFiletype(string tenantName, string filetype)
        {
            _logger.LogInformation("Getting {Filetype} assets for tenant: {TenantName}", filetype, tenantName);
            
            var assets = await _assetService.GetAssetsByFiletypeAsync(filetype);
            
            _logger.LogInformation("Found {Count} {Filetype} assets for tenant: {TenantName}", 
                assets.Count(), filetype, tenantName);
            
            return Ok(assets);
        }

        #endregion

        #region Asset Relationships

        // GET: api/{tenantName}/assets/5/materials
        [HttpGet("{id}/materials")]
        public async Task<ActionResult<IEnumerable<Material>>> GetAssetMaterials(string tenantName, int id)
        {
            _logger.LogInformation("Getting materials using asset {Id} for tenant: {TenantName}", id, tenantName);
            
            var materials = await _assetService.GetMaterialsUsingAssetAsync(id);
            
            _logger.LogInformation("Found {Count} materials using asset {Id} for tenant: {TenantName}", 
                materials.Count(), id, tenantName);
            
            return Ok(materials);
        }

        // GET: api/{tenantName}/assets/5/usage-count
        [HttpGet("{id}/usage-count")]
        public async Task<ActionResult<object>> GetAssetUsageCount(string tenantName, int id)
        {
            _logger.LogInformation("📊 Getting usage count for asset {Id} for tenant: {TenantName}", id, tenantName);
            
            var count = await _assetService.GetAssetUsageCountAsync(id);
            
            _logger.LogInformation("Asset {Id} is used by {Count} materials for tenant: {TenantName}", 
                id, count, tenantName);
            
            return Ok(new { AssetId = id, UsageCount = count });
        }

        #endregion

        #region File Management (Placeholder Endpoints)

        // GET: api/{tenantName}/assets/5/download
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadAsset(string tenantName, int id)
        {
            _logger.LogInformation("⬇️ Download request for asset {Id} for tenant: {TenantName}", id, tenantName);
            
            try
            {
                var downloadUrl = await _assetService.GetAssetDownloadUrlAsync(id);
                
                // TODO: Implement actual file download from OwnCloud/S3
                _logger.LogInformation("TODO: Redirect to download URL: {DownloadUrl}", downloadUrl);
                
                return Ok(new { 
                    Message = "TODO: Implement file download", 
                    DownloadUrl = downloadUrl,
                    Note = "This is a placeholder - implement with OwnCloud/S3"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // POST: api/{tenantName}/assets/upload
        [HttpPost("upload")]
        public async Task<ActionResult<Asset>> UploadAsset(
            string tenantName, 
            IFormFile file, 
            [FromForm] string? description = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided");
            }

            _logger.LogInformation("Upload request for file {Filename} ({Size} bytes) for tenant: {TenantName}", 
                file.FileName, file.Length, tenantName);

            try
            {
                using var stream = file.OpenReadStream();
                var asset = await _assetService.UploadAssetAsync(file, file.FileName, description);

                _logger.LogInformation("TODO: File uploaded as asset {Id} for tenant: {TenantName}", 
                    asset.Id, tenantName);

                return CreatedAtAction(nameof(GetAsset), 
                    new { tenantName, id = asset.Id }, 
                    asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {Filename} for tenant: {TenantName}", 
                    file.FileName, tenantName);
                return StatusCode(500, "Upload failed: " + ex.Message);
            }
        }

        // GET: api/{tenantName}/assets/5/file-info
        [HttpGet("{id}/file-info")]
        public async Task<ActionResult<object>> GetAssetFileInfo(string tenantName, int id)
        {
            _logger.LogInformation("Getting file info for asset {Id} for tenant: {TenantName}", id, tenantName);
            
            var asset = await _assetService.GetAssetAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            var fileSize = await _assetService.GetAssetFileSizeAsync(id);
            var fileExists = await _assetService.AssetFileExistsAsync(id);
            
            return Ok(new
            {
                AssetId = id,
                Filename = asset.Filename,
                Filetype = asset.Filetype,
                FileSize = fileSize,
                FileExists = fileExists,
                Src = asset.Src,
                Note = "File operations are placeholder - implement with OwnCloud/S3"
            });
        }

        #endregion

        #region Asset Statistics

        // GET: api/{tenantName}/assets/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<AssetStatistics>> GetAssetStatistics(string tenantName)
        {
            _logger.LogInformation("📊 Getting asset statistics for tenant: {TenantName}", tenantName);
            
            var statistics = await _assetService.GetAssetStatisticsAsync();
            
            _logger.LogInformation("Retrieved asset statistics for tenant: {TenantName} ({TotalAssets} total assets)", 
                tenantName, statistics.TotalAssets);
            
            return Ok(statistics);
        }

        #endregion

        #region Utility Endpoints

        // GET: api/{tenantName}/assets/filetypes
        [HttpGet("filetypes")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableFiletypes(string tenantName)
        {
            _logger.LogInformation("📋 Getting available filetypes for tenant: {TenantName}", tenantName);
            
            var assets = await _assetService.GetAllAssetsAsync();
            var filetypes = assets.Select(a => a.Filetype).Distinct().Where(ft => !string.IsNullOrEmpty(ft)).ToList();
            
            _logger.LogInformation("Found {Count} filetypes for tenant: {TenantName}", 
                filetypes.Count, tenantName);
            
            return Ok(filetypes);
        }

        // POST: api/{tenantName}/assets/validate-filename
        [HttpPost("validate-filename")]
        public async Task<ActionResult<object>> ValidateFilename(string tenantName, [FromBody] string filename)
        {
            _logger.LogInformation("Validating filename '{Filename}' for tenant: {TenantName}", filename, tenantName);
            
            if (string.IsNullOrWhiteSpace(filename))
            {
                return BadRequest(new { IsValid = false, Message = "Filename cannot be empty" });
            }

            // Check for existing assets with same filename
            var existingAssets = await _assetService.SearchAssetsByFilenameAsync(filename);
            var exactMatch = existingAssets.Any(a => a.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase));
            
            return Ok(new
            {
                IsValid = !exactMatch,
                Message = exactMatch ? "Filename already exists" : "Filename is available",
                ExistingMatches = existingAssets.Count()
            });
        }

        #endregion
    }
}