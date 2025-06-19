using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
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
        private readonly HttpClient _httpClient;
        private readonly IXR50TenantService _tenantService;
        private readonly XR50MigrationService _migrationService;
        private readonly ILogger<XR50TenantManagementService> _logger;

        public XR50TenantManagementService(
            IConfiguration configuration,
            HttpClient httpClient,
            IXR50TenantService tenantService,
            XR50MigrationService migrationService,
            ILogger<XR50TenantManagementService> logger)
            {
            _configuration = configuration;
            _httpClient = httpClient;
            _tenantService = tenantService;
            _migrationService = migrationService;
            _logger = logger;
            }

        public async Task<IEnumerable<XR50Tenant>> GetAllTenantsAsync()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            //  Auto-create registry table if it doesn't exist
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
            
            var tenant = new XR50Tenant
            {
                TenantName = reader["TenantName"]?.ToString(),
                TenantGroup = reader["TenantGroup"]?.ToString(),
                Description = reader["Description"]?.ToString(),
                TenantDirectory = reader["TenantDirectory"]?.ToString(),
                OwnerName = reader["OwnerName"]?.ToString(),
                TenantSchema = reader["DatabaseName"]?.ToString()
            };

            // Close the reader before making another database call
            reader.Close();

            // Now fetch the owner user data from the tenant database
            if (!string.IsNullOrEmpty(tenant.OwnerName) && !string.IsNullOrEmpty(tenant.TenantSchema))
            {
                tenant.Owner = await GetOwnerUserAsync(tenant.OwnerName, tenant.TenantSchema);
            }

            return tenant;
        }

        private async Task<User> GetOwnerUserAsync(string ownerName, string tenantDatabaseName)
        {
            try
            {
                var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
                var baseDatabaseName = _configuration["BaseDatabaseName"] ?? "magical_library";
                var tenantConnectionString = baseConnectionString.Replace($"database={baseDatabaseName}", $"database={tenantDatabaseName}", StringComparison.OrdinalIgnoreCase);

                _logger.LogDebug("Fetching owner user {OwnerName} from tenant database: {TenantDatabase}", ownerName, tenantDatabaseName);

                using var connection = new MySqlConnection(tenantConnectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT UserName, FullName, UserEmail, Password, admin, CreatedDate, UpdatedDate
                    FROM Users 
                    WHERE UserName = @userName";

                using var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@userName", ownerName);

                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync()) 
                {
                    _logger.LogWarning("Owner user {OwnerName} not found in tenant database: {TenantDatabase}", ownerName, tenantDatabaseName);
                    return null;
                }

                var owner = new User
                {
                    UserName = reader["UserName"]?.ToString(),
                    FullName = reader["FullName"]?.ToString(),
                    UserEmail = reader["UserEmail"]?.ToString(),
                    Password = reader["Password"]?.ToString(),
                    admin = Convert.ToBoolean(reader["admin"]),
                    // CreatedDate and UpdatedDate are shadow properties, so we don't set them here
                };

                _logger.LogDebug("Found owner user {OwnerName} in tenant database: {TenantDatabase}", ownerName, tenantDatabaseName);
                return owner;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch owner user {OwnerName} from tenant database: {TenantDatabase}", ownerName, tenantDatabaseName);
                return null; // Return null instead of throwing - tenant info is still valid without owner details
            }
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
            await CreateTenantStorageAsync(tenant);
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
            //Storage First or otherwise we will nuke db
            await DeleteTenantStorageAsync(tenantName);
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
                await DeleteTenantStorageAsync(tenantName);
                _logger.LogInformation("Completely deleted tenant {TenantName} (database and registry entry)", tenantName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during complete deletion of tenant {TenantName}", tenantName);
                throw;
            }
        }
        public async Task CreateTenantStorageAsync(XR50Tenant tenant) {
            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("groupid", tenant.TenantGroup));
            FormUrlEncodedContent messageContent = new FormUrlEncodedContent(values);
            string username = _configuration.GetValue<string>("TenantSettings:Admin");
            string password = _configuration.GetValue<string>("TenantSettings:Password");
            string uri_base = _configuration.GetValue<string>("TenantSettings:BaseAPI");
            string uri_path = _configuration.GetValue<string>("TenantSettings:GroupsPath");
            string webdav_base = _configuration.GetValue<string>("TenantSettings:BaseWebDAV");
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
            User adminUser = tenant.Owner;

            //Console.WriteLine($"Response content: {resultContent}");
            //Create the admin User
            var valuesAdmin = new List<KeyValuePair<string, string>>();
            valuesAdmin.Add(new KeyValuePair<string, string>("userid", adminUser.UserName));
            valuesAdmin.Add(new KeyValuePair<string, string>("password", adminUser.Password));
            valuesAdmin.Add(new KeyValuePair<string, string>("email", adminUser.UserEmail));
            valuesAdmin.Add(new KeyValuePair<string, string>("display", adminUser.FullName));
            valuesAdmin.Add(new KeyValuePair<string, string>("groups[]", tenant.TenantGroup));
            //Target The User Interface
            uri_path = _configuration.GetValue<string>("TenantSettings:UsersPath");
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

            // Create root dir for the Tenant, owned by Admin
            string cmd="curl";
            string dirl=System.Web.HttpUtility.UrlEncode(tenant.TenantDirectory);
            string Arg= $"-X MKCOL -u {adminUser.UserName}:{adminUser.Password} \"{webdav_base}/{dirl}/\"";
            // Create root dir for the Tenant
            Console.WriteLine("Executing command:" + cmd + " " + Arg);
            var startInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = Arg,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                Console.WriteLine("Output: " + output);
                Console.WriteLine("Error: " + error);
            }
            
        }

        public async Task DeleteTenantStorageAsync(string tenantName)
        {
            var tenant = await GetTenantAsync(tenantName);
            if (tenant == null){
                Console.WriteLine($"Did not find Tenant with name: {tenantName}");
                return;
            }
            User adminUser = tenant.Owner;
            var values = new List<KeyValuePair<string, string>>();
            FormUrlEncodedContent messageContent = new FormUrlEncodedContent(values);
            string username = _configuration.GetValue<string>("TenantSettings:Admin");
            string password = _configuration.GetValue<string>("TenantSettings:Password");
            string uri_base = _configuration.GetValue<string>("TenantSettings:BaseAPI");
            string uri_group = _configuration.GetValue<string>("TenantSettings:GroupsPath");
            string uri_user = _configuration.GetValue<string>("TenantSettings:UsersPath");
            string webdav_base = _configuration.GetValue<string>("TenantSettings:BaseWebDAV");

            string authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{uri_group}/{tenant.TenantGroup}")
            {
                Content = messageContent
            };
            Console.WriteLine(tenant.TenantGroup);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri(uri_base);
            //_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var result = _httpClient.SendAsync(request).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            // Delete root dir for the Tenant
            string cmd= "curl";
            string dirl=System.Web.HttpUtility.UrlEncode(tenant.TenantDirectory);
            string Arg=  $"-X DELETE -u {adminUser.UserName}:{adminUser.Password} \"{webdav_base}/{dirl}/\"";
            Console.WriteLine("Executing command: " + cmd + " " + Arg);
            var startInfo = new ProcessStartInfo
            {                                                                                                                           FileName = cmd,
                Arguments = Arg,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                Console.WriteLine("Output: " + output);
                Console.WriteLine("Error: " + error);
            }
            request = new HttpRequestMessage(HttpMethod.Delete, $"{uri_user}/{tenant.OwnerName}")
            {
                Content = messageContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            result = _httpClient.SendAsync(request).Result;
            resultContent = result.Content.ReadAsStringAsync().Result;    
                    }


    }
}