using Microsoft.AspNetCore.Mvc;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Controllers
{
    [Route("api/troubleshooting")]
    [ApiController]
    public class TenantTroubleshootingController : ControllerBase
    {
        private readonly IXR50TenantTroubleshootingService _troubleshootingService;
        private readonly XR50MigrationService _migrationService;
        private readonly IXR50TenantManagementService _tenantManagementService;
        private readonly IXR50ManualTableCreator _tableCreator;
        private readonly ILogger<TenantTroubleshootingController> _logger;

        public TenantTroubleshootingController(
            IXR50TenantTroubleshootingService troubleshootingService,
            XR50MigrationService migrationService,
            IXR50TenantManagementService tenantManagementService,
            IXR50ManualTableCreator tableCreator,
            ILogger<TenantTroubleshootingController> logger)
        {
            _troubleshootingService = troubleshootingService;
            _migrationService = migrationService;
            _tenantManagementService = tenantManagementService;
            _tableCreator = tableCreator;
            _logger = logger;
        }

        /// <summary>
        /// Diagnose a specific tenant's database health
        /// </summary>
        [HttpGet("diagnose/{tenantName}")]
        public async Task<ActionResult<TenantDiagnosticResult>> DiagnoseTenant(string tenantName)
        {
            try
            {
                var result = await _troubleshootingService.DiagnoseTenantAsync(tenantName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error diagnosing tenant {TenantName}", tenantName);
                return StatusCode(500, $"Error diagnosing tenant: {ex.Message}");
            }
        }

        /// <summary>
        /// Repair a tenant's database
        /// </summary>
        [HttpPost("repair/{tenantName}")]
        public async Task<ActionResult> RepairTenant(string tenantName)
        {
            try
            {
                var success = await _troubleshootingService.RepairTenantDatabaseAsync(tenantName);
                
                if (success)
                {
                    return Ok(new { Message = $"Tenant {tenantName} repaired successfully" });
                }
                else
                {
                    return BadRequest(new { Message = $"Failed to repair tenant {tenantName}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error repairing tenant {TenantName}", tenantName);
                return StatusCode(500, $"Error repairing tenant: {ex.Message}");
            }
        }

        /// <summary>
        /// Test tenant database connection
        /// </summary>
        [HttpGet("test-connection/{tenantName}")]
        public async Task<ActionResult> TestConnection(string tenantName)
        {
            try
            {
                var canConnect = await _troubleshootingService.TestTenantConnectionAsync(tenantName);
                
                return Ok(new { 
                    TenantName = tenantName, 
                    CanConnect = canConnect,
                    Message = canConnect ? "Connection successful" : "Connection failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection for tenant {TenantName}", tenantName);
                return StatusCode(500, $"Error testing connection: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all tenant databases
        /// </summary>
        [HttpGet("databases")]
        public async Task<ActionResult<List<string>>> GetAllTenantDatabases()
        {
            try
            {
                var databases = await _troubleshootingService.GetAllTenantDatabasesAsync();
                return Ok(databases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant databases");
                return StatusCode(500, $"Error getting databases: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a test tenant for debugging
        /// </summary>
        [HttpPost("create-test-tenant/{tenantName}")]
        public async Task<ActionResult> CreateTestTenant(string tenantName)
        {
            try
            {
                var testTenant = new XR50Tenant
                {
                    TenantName = tenantName,
                    TenantGroup = "test",
                    Description = $"Test tenant created for troubleshooting - {DateTime.UtcNow}",
                    OwnerName = "System",
                    TenantDirectory = $"/test/{tenantName}"
                };

                var createdTenant = await _tenantManagementService.CreateTenantAsync(testTenant);
                
                // Immediately diagnose the created tenant
                var diagnostic = await _troubleshootingService.DiagnoseTenantAsync(tenantName);
                
                return Ok(new { 
                    Message = $"Test tenant {tenantName} created",
                    Tenant = createdTenant,
                    Diagnostic = diagnostic
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test tenant {TenantName}", tenantName);
                return StatusCode(500, $"Error creating test tenant: {ex.Message}");
            }
        }

        /// <summary>
        /// Force recreate a tenant database
        /// </summary>
        [HttpPost("force-recreate/{tenantName}")]
        public async Task<ActionResult> ForceRecreateTenant(string tenantName)
        {
            try
            {
                _logger.LogInformation("Force recreating tenant {TenantName}", tenantName);

                // Create tenant object
                var tenant = new XR50Tenant
                {
                    TenantName = tenantName,
                    TenantGroup = "recreated",
                    Description = $"Force recreated tenant - {DateTime.UtcNow}",
                    OwnerName = "System"
                };

                // Use migration service directly
                await _migrationService.CreateTenantDatabaseAsync(tenant);
                
                // Test the result
                var diagnostic = await _troubleshootingService.DiagnoseTenantAsync(tenantName);
                
                return Ok(new { 
                    Message = $"Tenant {tenantName} force recreated",
                    Diagnostic = diagnostic
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force recreating tenant {TenantName}", tenantName);
                return StatusCode(500, $"Error force recreating tenant: {ex.Message}");
            }
        }

        /// <summary>
        /// Manually create tables in tenant database
        /// </summary>
        [HttpPost("create-tables/{tenantName}")]
        public async Task<ActionResult> CreateTablesManually(string tenantName)
        {
            try
            {
                var success = await _tableCreator.CreateAllTablesAsync(tenantName);
                
                if (success)
                {
                    var tables = await _tableCreator.GetExistingTablesAsync(tenantName);
                    var diagnostic = await _troubleshootingService.DiagnoseTenantAsync(tenantName);
                    
                    return Ok(new { 
                        Message = $"Tables created manually for tenant {tenantName}",
                        TablesCreated = tables,
                        TableCount = tables.Count,
                        Diagnostic = diagnostic
                    });
                }
                else
                {
                    return BadRequest(new { Message = $"Failed to create tables for tenant {tenantName}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error manually creating tables for tenant {TenantName}", tenantName);
                return StatusCode(500, $"Error creating tables: {ex.Message}");
            }
        }

        /// <summary>
        /// Completely rebuild tenant database (drop and recreate all tables)
        /// </summary>
        [HttpPost("rebuild/{tenantName}")]
        public async Task<ActionResult> RebuildTenantDatabase(string tenantName)
        {
            try
            {
                var success = await _migrationService.RepairTenantDatabaseAsync(tenantName);
                
                if (success)
                {
                    var tables = await _tableCreator.GetExistingTablesAsync(tenantName);
                    return Ok(new { 
                        Message = $"Tenant database {tenantName} rebuilt successfully",
                        TablesCreated = tables,
                        TableCount = tables.Count
                    });
                }
                else
                {
                    return BadRequest(new { Message = $"Failed to rebuild tenant database {tenantName}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebuilding tenant database {TenantName}", tenantName);
                return StatusCode(500, $"Error rebuilding database: {ex.Message}");
            }
        }

        /// <summary>
        /// Get existing tables in tenant database
        /// </summary>
        [HttpGet("tables/{tenantName}")]
        public async Task<ActionResult<List<string>>> GetTenantTables(string tenantName)
        {
            try
            {
                var tables = await _tableCreator.GetExistingTablesAsync(tenantName);
                return Ok(new {
                    TenantName = tenantName,
                    Tables = tables,
                    TableCount = tables.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tables for tenant {TenantName}", tenantName);
                return StatusCode(500, $"Error getting tables: {ex.Message}");
            }
        }

        /// <summary>
        /// Completely delete tenant database (WARNING: This will delete all data!)
        /// </summary>
        [HttpDelete("delete-database/{tenantName}")]
        public async Task<ActionResult> DeleteTenantDatabase(string tenantName)
        {
            try
            {
                _logger.LogWarning("Request to delete tenant database: {TenantName}", tenantName);
                
                var success = await _migrationService.DeleteTenantDatabaseAsync(tenantName);
                
                if (success)
                {
                    return Ok(new { 
                        Message = $"Tenant database {tenantName} deleted successfully",
                        Warning = "All data has been permanently deleted"
                    });
                }
                else
                {
                    return BadRequest(new { Message = $"Failed to delete tenant database {tenantName}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tenant database {TenantName}", tenantName);
                return StatusCode(500, $"Error deleting database: {ex.Message}");
            }
        }

        /// <summary>
        /// Completely delete tenant (database AND registry entry) - WARNING: PERMANENT!
        /// </summary>
        [HttpDelete("delete-completely/{tenantName}")]
        public async Task<ActionResult> DeleteTenantCompletely(string tenantName)
        {
            try
            {
                _logger.LogWarning("Request to completely delete tenant: {TenantName}", tenantName);
                
                await _tenantManagementService.DeleteTenantCompletelyAsync(tenantName);
                
                return Ok(new { 
                    Message = $"Tenant {tenantName} completely deleted",
                    Warning = "Database and registry entry permanently deleted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completely deleting tenant {TenantName}", tenantName);
                return StatusCode(500, $"Error deleting tenant: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all tenants with their health status
        /// </summary>
        [HttpGet("health-check")]
        public async Task<ActionResult> GetTenantsHealthCheck()
        {
            try
            {
                var allTenants = await _tenantManagementService.GetAllTenantsAsync();
                var healthResults = new List<object>();

                foreach (var tenant in allTenants)
                {
                    var diagnostic = await _troubleshootingService.DiagnoseTenantAsync(tenant.TenantName);
                    healthResults.Add(new 
                    {
                        TenantName = tenant.TenantName,
                        IsHealthy = diagnostic.IsHealthy,
                        DatabaseExists = diagnostic.DatabaseExists,
                        CanConnect = diagnostic.CanConnect,
                        TableCount = diagnostic.Tables.Count,
                        Error = diagnostic.Error
                    });
                }

                return Ok(healthResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing health check");
                return StatusCode(500, $"Error performing health check: {ex.Message}");
            }
        }
    }
}