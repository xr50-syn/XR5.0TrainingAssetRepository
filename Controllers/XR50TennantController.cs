using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Controllers
{
    // XR50 Tenants Controller - Matching your OpenAPI exactly
    [ApiController]
    [Route("xr50/trainingAssetRepository/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly IXR50TenantManagementService _tenantManagementService;

        public TenantsController(IXR50TenantManagementService tenantManagementService)
        {
            _tenantManagementService = tenantManagementService;
        }

        [HttpGet]
        public async Task<ActionResult<XR50Tenant[]>> GetTenants()
        {
            var tenants = await _tenantManagementService.GetAllTenantsAsync();
            return Ok(tenants.ToArray());
        }

        [HttpPost]
        public async Task<ActionResult<XR50Tenant>> CreateTenant([FromBody] XR50Tenant tenant)
        {
            try
            {
                var createdTenant = await _tenantManagementService.CreateTenantAsync(tenant);
                return Ok(createdTenant);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{tenantName}")]
        public async Task<ActionResult<XR50Tenant>> GetTenant(string tenantName)
        {
            var tenant = await _tenantManagementService.GetTenantAsync(tenantName);
            if (tenant == null)
                return NotFound();
            
            return Ok(tenant);
        }

        [HttpDelete("{tenantName}")]
        public async Task<ActionResult> DeleteTenant(string tenantName)
        {
            await _tenantManagementService.DeleteTenantAsync(tenantName);
            return Ok();
        }
    }
}