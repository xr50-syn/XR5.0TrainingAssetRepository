using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Services
{
    // XR50 Tenant Management Service (Repository Pattern) - MySQL
    public interface IXR50TenantManagementService
    {
        Task<IEnumerable<XR50Tenant>> GetAllTenantsAsync();
        Task<XR50Tenant> GetTenantAsync(string tenantName);
        Task<XR50Tenant> CreateTenantAsync(XR50Tenant tenant);
        Task DeleteTenantAsync(string tenantName);
        Task DeleteTenantCompletelyAsync(string tenantName); // New method for complete deletion
    }

    public class XR50TenantManagementService : IXR50TenantManagementService
    {
        private readonly IConfiguration _configuration;
        private readonly IXR50TenantService _tenantService;
        private readonly XR50MigrationService _migrationService;
        private readonly ILogger<XR50TenantManagementService> _logger;

        public XR50TenantManagementService(
            IConfiguration configuration,
            IXR50TenantService tenantService,
            XR50MigrationService migrationService,
            ILogger<XR50TenantManagementService> logger)
        {
            _configuration = configuration;
            _tenantService = tenantService;
            _migrationService = migrationService;
            _logger = logger;
        }

        public async Task<IEnumerable<XR50Tenant>> GetAllTenantsAsync()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // ✅ Auto-create registry table if it doesn't exist
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS `XR50TenantRegistry` (
                `TenantName` varchar(100) NOT NULL PRIMARY KEY,
                `TenantGroup` varchar(100) NULL,
                `Description` varchar(500) NULL,
                `TenantDirectory` varchar(500) NULL,
                `OwnerName` varchar(255) NULL,
                `DatabaseName` varchar(100) NOT NULL,
                `CreatedAt` datetime NOT NULL,
                `IsActive` boolean NOT NULL DEFAULT 1
            )";
    
            using var createCommand = new MySqlCommand(createTableSql, connection);
            await createCommand.ExecuteNonQueryAsync();
            
            var sql = @"
                SELECT TenantName, TenantGroup, Description, TenantDirectory, OwnerName, DatabaseName,
                       CreatedAt, IsActive
                FROM XR50TenantRegistry 
                WHERE IsActive = 1 
                ORDER BY CreatedAt DESC";
            
            using var command = new MySqlCommand(sql, connection);
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
                    TenantSchema = reader["DatabaseName"]?.ToString()
                });
            }
            
            return results;
        }

        public async Task<XR50Tenant> GetTenantAsync(string tenantName)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            
            var sql = @"
                SELECT TenantName, TenantGroup, Description, TenantDirectory, OwnerName, DatabaseName,
                       CreatedAt, IsActive
                FROM XR50TenantRegistry 
                WHERE TenantName = @tenantName AND IsActive = 1";
            
            using var command = new MySqlCommand(sql, connection);
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
                TenantSchema = reader["DatabaseName"]?.ToString()
            };
        }

        public async Task<XR50Tenant> CreateTenantAsync(XR50Tenant tenant)
        {
            // Validate tenant doesn't already exist
            if (await _tenantService.TenantExistsAsync(tenant.TenantName))
            {
                throw new InvalidOperationException($"Tenant '{tenant.TenantName}' already exists");
            }

            // Create the tenant database and infrastructure
            var createdTenant = await _tenantService.CreateTenantAsync(tenant);
            
            // Set the database name for the response
            createdTenant.TenantSchema = _tenantService.GetTenantSchema(tenant.TenantName);
            
            return createdTenant;
        }

        public async Task<XR50Tenant> UpdateTenantAsync(string tenantName, XR50Tenant tenant)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            
            var sql = @"
                UPDATE XR50TenantRegistry 
                SET TenantGroup = @tenantGroup,
                    Description = @description,
                    TenantDirectory = @tenantDirectory,
                    OwnerName = @ownerName
                WHERE TenantName = @tenantName AND IsActive = 1";
            
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@tenantName", tenantName);
            command.Parameters.AddWithValue("@tenantGroup", tenant.TenantGroup ?? "");
            command.Parameters.AddWithValue("@description", tenant.Description ?? "");
            command.Parameters.AddWithValue("@tenantDirectory", tenant.TenantDirectory ?? "");
            command.Parameters.AddWithValue("@ownerName", tenant.OwnerName ?? "");
            
            await command.ExecuteNonQueryAsync();
            
            return await GetTenantAsync(tenantName);
        }

        public async Task DeleteTenantAsync(string tenantName)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            
            var sql = @"
                UPDATE XR50TenantRegistry 
                SET IsActive = 0 
                WHERE TenantName = @tenantName";
            
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@tenantName", tenantName);
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Marked tenant {TenantName} as inactive in registry (database still exists)", tenantName);
        }

        public async Task DeleteTenantCompletelyAsync(string tenantName)
        {
            try
            {
                _logger.LogWarning("Starting complete deletion of tenant: {TenantName}", tenantName);

                // 1. Delete the tenant database
                var databaseDeleted = await _migrationService.DeleteTenantDatabaseAsync(tenantName);
                
                if (!databaseDeleted)
                {
                    _logger.LogWarning("Failed to delete database for tenant {TenantName}, continuing with registry cleanup", tenantName);
                }

                // 2. Remove from registry completely
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                
                var sql = "DELETE FROM XR50TenantRegistry WHERE TenantName = @tenantName";
                using var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@tenantName", tenantName);
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Completely deleted tenant {TenantName} (database and registry entry)", tenantName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during complete deletion of tenant {TenantName}", tenantName);
                throw;
            }
        }
    }
}