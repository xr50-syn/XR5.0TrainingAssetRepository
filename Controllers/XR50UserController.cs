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
    [Route("api/{tenantName}/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IXR50TenantDbContextFactory _dbContextFactory;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IXR50TenantDbContextFactory dbContextFactory,
            ILogger<UsersController> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        // GET: api/{tenantName}/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers(string tenantName)
        {
            _logger.LogInformation("Getting users for tenant: {TenantName}", tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            var users = await context.Users.ToListAsync();
            
            _logger.LogInformation("Found {UserCount} users for tenant: {TenantName}", users.Count, tenantName);
            
            return users;
        }

        // GET: api/{tenantName}/users/5
        [HttpGet("{userName}")]
        public async Task<ActionResult<User>> GetUser(string tenantName, string userName)
        {
            _logger.LogInformation("Getting user {UserName} for tenant: {TenantName}", userName, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            var user = await context.Users.FindAsync(userName);

            if (user == null)
            {
                _logger.LogWarning("User {UserName} not found in tenant: {TenantName}", userName, tenantName);
                return NotFound();
            }

            return user;
        }

        // POST: api/{tenantName}/users
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(string tenantName, User user)
        {
            _logger.LogInformation(" Creating user {UserName} for tenant: {TenantName}", user.UserName, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            context.Users.Add(user);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created user {UserName} for tenant: {TenantName}", user.UserName, tenantName);

            return CreatedAtAction(nameof(GetUser), 
                new { tenantName, userName = user.UserName }, 
                user);
        }

        // PUT: api/{tenantName}/users/5
        [HttpPut("{userName}")]
        public async Task<IActionResult> PutUser(string tenantName, string userName, User user)
        {
            if (userName != user.UserName)
            {
                return BadRequest();
            }

            _logger.LogInformation(" Updating user {UserName} for tenant: {TenantName}", userName, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            context.Entry(user).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Updated user {UserName} for tenant: {TenantName}", userName, tenantName);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await UserExistsAsync(userName))
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

        // DELETE: api/{tenantName}/users/5
        [HttpDelete("{userName}")]
        public async Task<IActionResult> DeleteUser(string tenantName, string userName)
        {
            _logger.LogInformation("Deleting user {UserName} for tenant: {TenantName}", userName, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            var user = await context.Users.FindAsync(userName);
            if (user == null)
            {
                return NotFound();
            }

            context.Users.Remove(user);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted user {UserName} for tenant: {TenantName}", userName, tenantName);

            return NoContent();
        }

        private async Task<bool> UserExistsAsync(string userName)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Users.AnyAsync(e => e.UserName == userName);
        }
    }
}