using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Data;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Services
{
    public class XR50MigrationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<XR50MigrationService> _logger;

        public XR50MigrationService(
            IServiceProvider serviceProvider, 
            IConfiguration configuration,
            ILogger<XR50MigrationService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task CreateTenantDatabaseAsync(XR50Tenant tenant)
        {
            var tenantDbName = GetTenantDatabase(tenant.TenantName);
            var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
            
            // Connection to MySQL server (not specific database)
            var adminConnectionString = baseConnectionString.Replace($"Database={GetBaseDatabaseName()}", "Database=mysql");

            using var connection = new MySqlConnection(adminConnectionString);
            await connection.OpenAsync();

            try
            {
                // 1. Create tenant database
                var createDbCommand = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS `{tenantDbName}`", connection);
                await createDbCommand.ExecuteNonQueryAsync();

                _logger.LogInformation("Created tenant database: {TenantDatabase}", tenantDbName);

                // 2. Run EF migrations on the new database
                await RunMigrationsOnTenantDatabase(tenantDbName);

                // 3. Store tenant metadata in central registry (magical_library)
                await StoreTenantMetadataInCentralRegistry(tenant, tenantDbName);

                _logger.LogInformation("Successfully created tenant: {TenantName}", tenant.TenantName);
            }
            catch (Exception ex)
            {
                // Cleanup on failure
                try
                {
                    var dropDbCommand = new MySqlCommand($"DROP DATABASE IF EXISTS `{tenantDbName}`", connection);
                    await dropDbCommand.ExecuteNonQueryAsync();
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to cleanup database {TenantDatabase} after creation failure", tenantDbName);
                }

                _logger.LogError(ex, "Failed to create tenant database {TenantDatabase}", tenantDbName);
                throw;
            }
        }

        private async Task RunMigrationsOnTenantDatabase(string tenantDbName)
        {
            var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
            var tenantConnectionString = baseConnectionString.Replace($"Database={GetBaseDatabaseName()}", $"Database={tenantDbName}");

            // Create a temporary DbContext for this specific tenant database
            var optionsBuilder = new DbContextOptionsBuilder<XR50TrainingContext>();
            optionsBuilder.UseMySql(tenantConnectionString, ServerVersion.AutoDetect(tenantConnectionString));

            // Create a mock tenant service for migrations
            var mockTenantService = new MockTenantService();

            using var context = new XR50TrainingContext(optionsBuilder.Options, mockTenantService);
            await context.Database.EnsureCreatedAsync(); // Creates tables without schema references

            _logger.LogInformation("Ran migrations on tenant database: {TenantDatabase}", tenantDbName);
        }

        private async Task StoreTenantMetadataInCentralRegistry(XR50Tenant tenant, string tenantDbName)
        {
            var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
            
            using var connection = new MySqlConnection(baseConnectionString);
            await connection.OpenAsync();

            // Ensure the registry table exists in central database
            var createRegistryTableCommand = new MySqlCommand(@"
                CREATE TABLE IF NOT EXISTS `XR50TenantRegistry` (
                    `TenantName` varchar(100) NOT NULL PRIMARY KEY,
                    `TenantGroup` varchar(100) NULL,
                    `Description` varchar(500) NULL,
                    `TenantDirectory` varchar(500) NULL,
                    `OwnerName` varchar(255) NULL,
                    `DatabaseName` varchar(100) NOT NULL,
                    `CreatedAt` datetime NOT NULL,
                    `IsActive` boolean NOT NULL DEFAULT 1
                )", connection);
            await createRegistryTableCommand.ExecuteNonQueryAsync();

            // Insert tenant metadata
            var insertCommand = new MySqlCommand(@"
                INSERT INTO `XR50TenantRegistry` 
                    (`TenantName`, `TenantGroup`, `Description`, `TenantDirectory`, `OwnerName`, `DatabaseName`, `CreatedAt`, `IsActive`)
                VALUES 
                    (@tenantName, @tenantGroup, @description, @tenantDirectory, @ownerName, @databaseName, @createdAt, 1)
                ON DUPLICATE KEY UPDATE
                    `TenantGroup` = @tenantGroup,
                    `Description` = @description,
                    `TenantDirectory` = @tenantDirectory,
                    `OwnerName` = @ownerName,
                    `DatabaseName` = @databaseName", connection);

            insertCommand.Parameters.AddWithValue("@tenantName", tenant.TenantName ?? "");
            insertCommand.Parameters.AddWithValue("@tenantGroup", tenant.TenantGroup ?? "");
            insertCommand.Parameters.AddWithValue("@description", tenant.Description ?? "");
            insertCommand.Parameters.AddWithValue("@tenantDirectory", tenant.TenantDirectory ?? "");
            insertCommand.Parameters.AddWithValue("@ownerName", tenant.OwnerName ?? "");
            insertCommand.Parameters.AddWithValue("@databaseName", tenantDbName);
            insertCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

            await insertCommand.ExecuteNonQueryAsync();
        }

        private string GetTenantDatabase(string tenantName)
        {
            var sanitized = Regex.Replace(tenantName, @"[^a-zA-Z0-9_]", "_");
            return $"xr50_tenant_{sanitized}";
        }

        private string GetBaseDatabaseName()
        {
            // Extract database name from connection string
            return _configuration["BaseDatabaseName"] ?? "magical_library";
        }

        // Mock tenant service for migrations (no HTTP context during migrations)
        private class MockTenantService : IXR50TenantService
        {
            public string GetCurrentTenant() => "migration";
            public Task<bool> ValidateTenantAsync(string tenantId) => Task.FromResult(true);
            public Task<bool> TenantExistsAsync(string tenantName) => Task.FromResult(true);
            public Task<XR50Tenant> CreateTenantAsync(XR50Tenant tenant) => Task.FromResult(tenant);
            public string GetTenantSchema(string tenantName) => ""; // Not used for MySQL
        }
    }
}