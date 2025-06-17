using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XR50TrainingAssetRepo.Models;
using Microsoft.Data.SqlClient; 

namespace XR50TrainingAssetRepo.Services
{
    // Enhanced Tenant Service for XR50
    public interface IXR50TenantService
    {
        string GetCurrentTenant();
        Task<bool> ValidateTenantAsync(string tenantName);
        Task<bool> TenantExistsAsync(string tenantName);
        Task<XR50Tenant> CreateTenantAsync(XR50Tenant tenant);
        string GetTenantSchema(string tenantName);
    }

    public class XR50TenantService : IXR50TenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<XR50TenantService> _logger;

        public XR50TenantService(
            IHttpContextAccessor httpContextAccessor, 
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<XR50TenantService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public string GetCurrentTenant()
        {
            var context = _httpContextAccessor.HttpContext;
            
            // XR50 tenant from URL patterns:
            // /api/{tenant}/programs
            // /xr50/{tenant}/materials  
            // /{tenant}/trainingAssetRepository/...
            var path = context?.Request.Path.Value;
            if (!string.IsNullOrEmpty(path))
            {
                var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                
                if (pathSegments.Length >= 2)
                {
                    // Pattern: /api/{tenant}/...
                    if (pathSegments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
                    {
                        return pathSegments[1];
                    }
                    
                    // Pattern: /xr50/{tenant}/... or /xr50/trainingAssetRepository/tenants (admin endpoint)
                    if (pathSegments[0].Equals("xr50", StringComparison.OrdinalIgnoreCase))
                    {
                        if (pathSegments.Length >= 3 && 
                            !pathSegments[1].Equals("trainingAssetRepository", StringComparison.OrdinalIgnoreCase))
                        {
                            return pathSegments[1]; // /xr50/{tenant}/...
                        }
                    }
                    
                    // Pattern: /{tenant}/api/... (tenant-first routing)
                    if (pathSegments.Length >= 3 && 
                        pathSegments[1].Equals("api", StringComparison.OrdinalIgnoreCase))
                    {
                        return pathSegments[0];
                    }
                }
            }
            
            // Fallback: From header (useful for testing/admin operations)
            if (context?.Request.Headers.TryGetValue("X-Tenant-Name", out var tenantHeader) == true)
            {
                return tenantHeader.FirstOrDefault();
            }
            
            // From JWT claims
            var tenantClaim = context?.User?.FindFirst("tenantName")?.Value;
            return tenantClaim ?? "default";
        }

        public async Task<bool> ValidateTenantAsync(string tenantName)
        {
            return await TenantExistsAsync(tenantName);
        }

        public async Task<bool> TenantExistsAsync(string tenantName)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("XR50Database");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if tenant schema exists
                var schemaName = GetTenantSchema(tenantName);
                var sql = "SELECT COUNT(*) FROM sys.schemas WHERE name = @schemaName";
                
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@schemaName", schemaName);
                
                var count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if tenant {TenantName} exists", tenantName);
                return false;
            }
        }

        public async Task<XR50Tenant> CreateTenantAsync(XR50Tenant tenant)
        {
            try
            {
                // Create the schema and run migrations
                using var scope = _serviceProvider.CreateScope();
                var migrationService = scope.ServiceProvider.GetRequiredService<XR50MigrationService>();
                
                await migrationService.CreateTenantSchemaAsync(tenant);
                
                _logger.LogInformation("Successfully created tenant schema: {TenantName}", tenant.TenantName);
                return tenant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tenant {TenantName}", tenant.TenantName);
                throw;
            }
        }

        public string GetTenantSchema(string tenantName)
        {
            // Sanitize tenant name for schema
            var sanitized = Regex.Replace(tenantName, @"[^a-zA-Z0-9_]", "_");
            return $"tenant_{sanitized}";
        }
    }
}