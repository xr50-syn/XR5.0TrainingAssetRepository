using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Services
{
    public class XR50MigrationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<XR50MigrationService> _logger;
        private readonly IXR50ManualTableCreator _tableCreator;

        public XR50MigrationService(
            IServiceProvider serviceProvider, 
            IConfiguration configuration,
            ILogger<XR50MigrationService> logger,
            IXR50ManualTableCreator tableCreator)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
            _tableCreator = tableCreator;
        }

        public async Task CreateTenantDatabaseAsync(XR50Tenant tenant)
        {
            var tenantDbName = GetTenantDatabase(tenant.TenantName);
            var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
            
            _logger.LogInformation("=== Creating tenant database for: {TenantName} ===", tenant.TenantName);
            _logger.LogInformation("Tenant database name: {TenantDatabase}", tenantDbName);
            
            // Connection to MySQL server (not specific database)
            var adminConnectionString = baseConnectionString.Replace($"Database={GetBaseDatabaseName()}", "Database=mysql");
            _logger.LogInformation("Admin connection: {AdminConnection}", adminConnectionString.Replace("Password=", "Password=***"));

            using var connection = new MySqlConnection(adminConnectionString);
            await connection.OpenAsync();

            try
            {
                // 1. Create tenant database
                var createDbCommand = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS `{tenantDbName}`", connection);
                await createDbCommand.ExecuteNonQueryAsync();
                _logger.LogInformation("✅ Created tenant database: {TenantDatabase}", tenantDbName);

                // 2. Create tables using manual table creator
                _logger.LogInformation("Creating tables in tenant database...");
                var tablesCreated = await _tableCreator.CreateTablesInDatabaseAsync(tenantDbName);
                
                if (!tablesCreated)
                {
                    throw new InvalidOperationException($"Failed to create tables in tenant database {tenantDbName}");
                }

                // 3. Verify tables were created
                var tables = await _tableCreator.GetExistingTablesAsync(tenant.TenantName);
                _logger.LogInformation("✅ Tenant database {TenantDatabase} now has {TableCount} tables: {Tables}", 
                    tenantDbName, tables.Count, string.Join(", ", tables));

                // 4. Store tenant metadata in central registry
                await StoreTenantMetadataInCentralRegistry(tenant, tenantDbName);
                _logger.LogInformation("✅ Stored tenant metadata in central registry");

                _logger.LogInformation("=== Successfully completed tenant creation: {TenantName} ===", tenant.TenantName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create tenant database {TenantDatabase}", tenantDbName);
                
                // Cleanup on failure
                try
                {
                    var dropDbCommand = new MySqlCommand($"DROP DATABASE IF EXISTS `{tenantDbName}`", connection);
                    await dropDbCommand.ExecuteNonQueryAsync();
                    _logger.LogInformation("🧹 Cleaned up failed database: {TenantDatabase}", tenantDbName);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "❌ Failed to cleanup database {TenantDatabase} after creation failure", tenantDbName);
                }

                throw;
            }
        }

        public async Task<bool> RepairTenantDatabaseAsync(string tenantName)
        {
            try
            {
                _logger.LogInformation("Repairing tenant database for: {TenantName}", tenantName);

                // 1. Ensure database exists
                var tenantDbName = GetTenantDatabase(tenantName);
                var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
                var adminConnectionString = baseConnectionString.Replace($"Database={GetBaseDatabaseName()}", "Database=mysql");

                using (var connection = new MySqlConnection(adminConnectionString))
                {
                    await connection.OpenAsync();
                    var createDbCommand = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS `{tenantDbName}`", connection);
                    await createDbCommand.ExecuteNonQueryAsync();
                }

                // 2. Drop existing tables and recreate them
                await _tableCreator.DropAllTablesAsync(tenantName);
                
                // 3. Create all tables fresh
                var success = await _tableCreator.CreateAllTablesAsync(tenantName);
                
                if (success)
                {
                    var tables = await _tableCreator.GetExistingTablesAsync(tenantName);
                    _logger.LogInformation("Successfully repaired tenant {TenantName} with {TableCount} tables", tenantName, tables.Count);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to repair tenant database for: {TenantName}", tenantName);
                return false;
            }
        }

        public async Task<bool> ManuallyCreateTablesAsync(string tenantName)
        {
            return await _tableCreator.CreateAllTablesAsync(tenantName);
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

        public async Task<bool> DeleteTenantDatabaseAsync(string tenantName)
        {
            try
            {
                var tenantDbName = GetTenantDatabase(tenantName);
                var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
                var adminConnectionString = baseConnectionString.Replace($"Database={GetBaseDatabaseName()}", "Database=mysql");

                _logger.LogInformation("Deleting tenant database: {TenantDatabase} for tenant: {TenantName}", tenantDbName, tenantName);

                using var connection = new MySqlConnection(adminConnectionString);
                await connection.OpenAsync();

                // Drop the tenant database
                var dropDbCommand = new MySqlCommand($"DROP DATABASE IF EXISTS `{tenantDbName}`", connection);
                await dropDbCommand.ExecuteNonQueryAsync();

                _logger.LogInformation("Successfully deleted tenant database: {TenantDatabase}", tenantDbName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete tenant database for: {TenantName}", tenantName);
                return false;
            }
        }

        private string GetTenantDatabase(string tenantName)
        {
            var sanitized = Regex.Replace(tenantName, @"[^a-zA-Z0-9_]", "_");
            return $"xr50_tenant_{sanitized}";
        }

        private string GetBaseDatabaseName()
        {
            return _configuration["BaseDatabaseName"] ?? "magical_library";
        }
    }
}