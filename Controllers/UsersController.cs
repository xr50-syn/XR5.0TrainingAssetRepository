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
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Controllers
{
    [Route("/xr50/training-repo/user-management/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserContext _context;
        private readonly XR50AppContext _xr50AppContext;
        private readonly HttpClient _httpClient;

        public UsersController(UserContext context, XR50AppContext xr50AppContext, HttpClient httpClient)
        { 
            _context = context;
            _xr50AppContext = xr50AppContext;
            _httpClient = httpClient;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var values = new List<KeyValuePair<string, string>>();
            FormUrlEncodedContent messageContent = new FormUrlEncodedContent(values);
            string username = "emmie";
            string password = "!@m!nL0v3W!th@my";

            string authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));

            var request = new HttpRequestMessage(HttpMethod.Get, "/ocs/v1.php/cloud/users")
            {
                Content = messageContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri("http://192.168.169.6:8080");
            // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var result = _httpClient.SendAsync(request).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            //Console.WriteLine($"Response content: {resultContent}");

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
            
            var xR50App = await _xr50AppContext.Apps.FindAsync(user.AppName);
            if (xR50App == null)
            {
                return NotFound();
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("userid", user.UserName));
            values.Add(new KeyValuePair<string, string>("password", user.Password));
            values.Add(new KeyValuePair<string, string>("email", user.UserEmail));
            values.Add(new KeyValuePair<string, string>("display", user.FullName));
            values.Add(new KeyValuePair<string, string>("groups[]", user.AppName));
            FormUrlEncodedContent messageContent = new FormUrlEncodedContent(values);
            string username = "emmie";
            string password = "!@m!nL0v3W!th@my";

            string authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));

            var request = new HttpRequestMessage(HttpMethod.Post, "/ocs/v1.php/cloud/users")
            {
                Content = messageContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri("http://192.168.169.6:8080");
            // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var result = _httpClient.SendAsync(request).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            //Console.WriteLine($"Response content: {resultContent}");
           /* var valuesMod = new List<KeyValuePair<string, string>>();
            valuesMod.Add(new KeyValuePair<string, string>("groupid", user.AppName));
            FormUrlEncodedContent messageContentMod = new FormUrlEncodedContent(valuesMod);

            var requestMod = new HttpRequestMessage(HttpMethod.Post, $"/ocs/v1.php/cloud/users/{user.UserName}/groups")
            {
                 Content = messageContentMod
            };
            requestMod.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            
            // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var resultMod = _httpClient.SendAsync(requestMod).Result;
            string resultContentMod = resultMod.Content.ReadAsStringAsync().Result;*/
            //Console.WriteLine($"Response content: {resultContent}");

            return CreatedAtAction("GetUser", new { id = user.UserName }, user);
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
            string username = "emmie";
            string password = "!@m!nL0v3W!th@my";

            string authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));

            var request = new HttpRequestMessage(HttpMethod.Delete, $"/ocs/v1.php/cloud/users/{user.UserName}")
            {
                Content = messageContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri("http://192.168.169.6:8080");
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
