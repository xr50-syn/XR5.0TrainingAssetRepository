using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using XR50TrainingAssetRepo.Models;
using Microsoft.Data.SqlClient; 

namespace XR50TrainingAssetRepo.Services
{
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
}