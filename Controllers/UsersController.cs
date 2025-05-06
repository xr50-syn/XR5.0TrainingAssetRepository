using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Controllers
{
    [Route("/xr50/trainingAssetRepository/[controller]")]
    [ApiController]
    public class userManagementController : ControllerBase
    {
        private readonly XR50TrainingAssetRepoContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public userManagementController(XR50TrainingAssetRepoContext context, HttpClient httpClient, IConfiguration configuration)
        { 
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;     
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {

            return await _context.Users.ToListAsync();
        }
        
        // GET: api/Users/5
        [HttpGet("{userName}")]
        public async Task<ActionResult<User>> GetUser(string userName)
        {
            var user = await _context.Users.FindAsync(userName);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        /*
        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{userName}")]
        public async Task<IActionResult> PutUser(string userName, User user)
        {
            if  (!user.UserName.Equals(userName))
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(userName))
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
        */

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            
            var XR50Tenant = await _context.Tenants.FindAsync(user.TenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {user.TenantName}");
            }
            
            if (user.admin)
            {
                XR50Tenant.AdminList.Add(user.UserName);
            }
            _context.Users.Add(user);
            _context.SaveChanges();

            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("userid", user.UserName));
            values.Add(new KeyValuePair<string, string>("password", user.Password));
            values.Add(new KeyValuePair<string, string>("email", user.UserEmail));
            values.Add(new KeyValuePair<string, string>("display", user.FullName));
            values.Add(new KeyValuePair<string, string>("groups[]", XR50Tenant.OwncloudGroup));
            FormUrlEncodedContent messageContent = new FormUrlEncodedContent(values);
            string username = _configuration.GetValue<string>("OwncloudSettings:Admin");
            string password = _configuration.GetValue<string>("OwncloudSettings:Password");
            string uri_base = _configuration.GetValue<string>("OwncloudSettings:BaseAPI");
            string uri_path = _configuration.GetValue<string>("OwncloudSettings:UserManagementPath");
            string authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));

            var request = new HttpRequestMessage(HttpMethod.Post, uri_path)
            {
                Content = messageContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri(uri_base);
            // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var result = _httpClient.SendAsync(request).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"Response content: {resultContent}");

            return CreatedAtAction("PostUser", new { id = user.UserName }, user);
        }
        [HttpPost("/xr50/trainingAssetRepository/[controller]/group-management")]
        public async Task<ActionResult<Group>> PostGroup(Group group)
        {
            var XR50Tenant = await _context.Tenants.FindAsync(group.TenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {group.TenantName}");
            } 
            var adminUser = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (adminUser ==null) 
            {
                return NotFound($"Couldnt Find Admin user for {group.TenantName}");
            }
            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("groupid", group.GroupName));
            FormUrlEncodedContent messageContent = new FormUrlEncodedContent(values);
            string username = _configuration.GetValue<string>("OwncloudSettings:Admin");
            string password = _configuration.GetValue<string>("OwncloudSettings:Password");
            string uri_base = _configuration.GetValue<string>("OwncloudSettings:BaseAPI");
            string uri_path = _configuration.GetValue<string>("OwncloudSettings:GroupManagementPath");
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            string authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
            var request = new HttpRequestMessage(HttpMethod.Post, uri_path)
            {
                Content = messageContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri(uri_base);
           // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var result = _httpClient.SendAsync(request).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            _context.SaveChanges();
            //Console.WriteLine($"Response content: {resultContent}");
            //Create the admin User
            var valuesAdmin = new List<KeyValuePair<string, string>>();
            valuesAdmin.Add(new KeyValuePair<string, string>("userid", adminUser.UserName));
            valuesAdmin.Add(new KeyValuePair<string, string>("password", adminUser.Password));
            valuesAdmin.Add(new KeyValuePair<string, string>("email", adminUser.UserEmail));
            valuesAdmin.Add(new KeyValuePair<string, string>("display", adminUser.FullName));
            valuesAdmin.Add(new KeyValuePair<string, string>("groups[]", XR50Tenant.OwncloudGroup));
            //Target The User Interface
            uri_path = _configuration.GetValue<string>("OwncloudSettings:UserManagementPath");
            FormUrlEncodedContent messageContentAdmin = new FormUrlEncodedContent(valuesAdmin);
           
            var requestAdmin = new HttpRequestMessage(HttpMethod.Post, uri_path)
            {
                Content = messageContentAdmin
            };
            requestAdmin.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
           
            // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var resultAdmin = _httpClient.SendAsync(requestAdmin).Result;
            string resultAdminContent = resultAdmin.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"Response content: {resultAdminContent}");
            _context.SaveChanges();
            return CreatedAtAction("PostGroup", group);
        }
        // DELETE: api/Users/5
        [HttpDelete("{userName}")]
        public async Task<IActionResult> DeleteUser(string userName)
        {
            var user = await _context.Users.FindAsync(userName);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("userid", user.UserName));
            values.Add(new KeyValuePair<string, string>("password", user.Password));
            values.Add(new KeyValuePair<string, string>("email", user.UserEmail));
            values.Add(new KeyValuePair<string, string>("display", user.FullName));
            FormUrlEncodedContent messageContent = new FormUrlEncodedContent(values);
            string username = _configuration.GetValue<string>("OwncloudSettings:Admin");
            string password = _configuration.GetValue<string>("OwncloudSettings:Password");
            string uri_base = _configuration.GetValue<string>("OwncloudSettings:BaseAPI");
            string uri_path = _configuration.GetValue<string>("OwncloudSettings:UserManagementPath");

            string authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{uri_path}/{user.UserName}")
            {
                Content = messageContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri(uri_base);
            // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var result = _httpClient.SendAsync(request).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            //Console.WriteLine($"Response content: {resultContent}");
            return NoContent();
        }

        private bool UserExists(string userName)
        {
            return _context.Users.Any(e => e.UserName.Equals(userName));
        }
    }
}
