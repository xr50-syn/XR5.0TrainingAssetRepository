using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Services;
using XR50TrainingAssetRepo.Data;
namespace XR50TrainingAssetRepo.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "users")]
    [Route("api/{tenantName}/[controller]")]  // ✅ Tenant-scoped route
    public class UsersController : ControllerBase
    {
        private readonly XR50TrainingContext _context;

        public UsersController(XR50TrainingContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers(string tenantName)
        {
            // ✅ Automatically scoped to current tenant's database
            return await _context.Users.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(string tenantName, [FromBody] User user)
        {
            // ✅ User created in tenant's database only
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { tenantName, id = user.UserName }, user);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string tenantName, int id)
        {
            // ✅ Only finds users in current tenant's database
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(string tenantName, string userName)
        {
            var user = await _context.Users.FindAsync(userName);
            if (user == null) return NotFound();
            
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}