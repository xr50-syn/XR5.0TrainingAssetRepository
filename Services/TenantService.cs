using XR50TrainingAssetRepo.Models;

// Core .NET namespaces
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// ASP.NET Core namespaces
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Entity Framework namespaces
using Microsoft.EntityFrameworkCore;


// OR if using modern Microsoft.Data.SqlClient:

using Microsoft.Data.SqlClient;


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
// XR50 Tenant Management Service (Repository Pattern)
public interface IXR50TenantManagementService
{
    Task<IEnumerable<XR50Tenant>> GetAllTenantsAsync();
    Task<XR50Tenant> GetTenantAsync(string tenantName);
    Task<XR50Tenant> CreateTenantAsync(XR50Tenant tenant);
    Task DeleteTenantAsync(string tenantName);
}

public class XR50TenantManagementService : IXR50TenantManagementService
{
    private readonly IConfiguration _configuration;
    private readonly IXR50TenantService _tenantService;
    private readonly ILogger<XR50TenantManagementService> _logger;

    public XR50TenantManagementService(
        IConfiguration configuration,
        IXR50TenantService tenantService,
        ILogger<XR50TenantManagementService> logger)
    {
        _configuration = configuration;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<IEnumerable<XR50Tenant>> GetAllTenantsAsync()
    {
        var connectionString = _configuration.GetConnectionString("XR50Database");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            SELECT TenantName, TenantGroup, Description, TenantDirectory, OwnerName, SchemaName,
                   TrainingProgramListJson, AdminListJson, CreatedAt, IsActive
            FROM [dbo].[XR50TenantRegistry] 
            WHERE IsActive = 1 
            ORDER BY CreatedAt DESC";
        
        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var results = new List<XR50Tenant>();
        while (await reader.ReadAsync())
        {
            results.Add(new XR50Tenant
            {
                TenantName = reader["TenantName"]?.ToString(),
                TenantGroup = reader["TenantGroup"]?.ToString(),
                Description = reader["Description"]?.ToString(),
                TenantDirectory = reader["TenantDirectory"]?.ToString(),
                OwnerName = reader["OwnerName"]?.ToString(),
                TenantSchema = reader["SchemaName"]?.ToString(),
                TrainingProgramList = JsonSerializer.Deserialize<List<string>>(reader["TrainingProgramListJson"]?.ToString() ?? "[]"),
                AdminList = JsonSerializer.Deserialize<List<string>>(reader["AdminListJson"]?.ToString() ?? "[]")
            });
        }
        
        return results;
    }

    public async Task<XR50Tenant> GetTenantAsync(string tenantName)
    {
        var connectionString = _configuration.GetConnectionString("XR50Database");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            SELECT TenantName, TenantGroup, Description, TenantDirectory, OwnerName, SchemaName,
                   TrainingProgramListJson, AdminListJson, CreatedAt, IsActive
            FROM [dbo].[XR50TenantRegistry] 
            WHERE TenantName = @tenantName AND IsActive = 1";
        
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tenantName", tenantName);
        
        using var reader = await command.ExecuteReaderAsync();
        
        if (!await reader.ReadAsync()) return null;
        
        return new XR50Tenant
        {
            TenantName = reader["TenantName"]?.ToString(),
            TenantGroup = reader["TenantGroup"]?.ToString(),
            Description = reader["Description"]?.ToString(),
            TenantDirectory = reader["TenantDirectory"]?.ToString(),
            OwnerName = reader["OwnerName"]?.ToString(),
            TenantSchema = reader["SchemaName"]?.ToString(),
            TrainingProgramList = JsonSerializer.Deserialize<List<string>>(reader["TrainingProgramListJson"]?.ToString() ?? "[]"),
            AdminList = JsonSerializer.Deserialize<List<string>>(reader["AdminListJson"]?.ToString() ?? "[]")
        };
    }

    public async Task<XR50Tenant> CreateTenantAsync(XR50Tenant tenant)
    {
        // Validate tenant doesn't already exist
        if (await _tenantService.TenantExistsAsync(tenant.TenantName))
        {
            throw new InvalidOperationException($"Tenant '{tenant.TenantName}' already exists");
        }

        // Create the tenant schema and infrastructure
        var createdTenant = await _tenantService.CreateTenantAsync(tenant);
        
        // Set the schema name for the response
        createdTenant.TenantSchema = _tenantService.GetTenantSchema(tenant.TenantName);
        
        return createdTenant;
    }

    public async Task DeleteTenantAsync(string tenantName)
    {
        var connectionString = _configuration.GetConnectionString("XR50Database");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            UPDATE [dbo].[XR50TenantRegistry] 
            SET IsActive = 0 
            WHERE TenantName = @tenantName";
        
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tenantName", tenantName);
        await command.ExecuteNonQueryAsync();
    }
}