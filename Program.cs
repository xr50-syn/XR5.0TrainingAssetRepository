using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.OpenApi.Models;
using XR50TrainingAssetRepo.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq; 
using Microsoft.Extensions.DependencyInjection;
using XR50TrainingAssetRepo.Data;
using XR50TrainingAssetRepo.Services;

var builder = WebApplication.CreateBuilder(args);
var  MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();
builder.Services.AddXR50MultitenancyWithDynamicDb(builder.Configuration);
/*builder.Services.AddScoped<IXR50TenantService, XR50TenantService>();
builder.Services.AddScoped<IXR50TenantManagementService, XR50TenantManagementService>();
builder.Services.AddScoped<XR50MigrationService>();
*/
/*string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<XR50TrainingContext>(opt =>
    opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
*/
builder.Services.AddDbContext<XR50TrainingContext>((serviceProvider, options) =>
{
    var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
    var tenantService = serviceProvider.GetService<IXR50TenantService>();
    
    var baseDatabaseName = builder.Configuration["BaseDatabaseName"] ?? "magical_library";
    var baseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    string connectionString;
    
    if (httpContextAccessor?.HttpContext != null)
    {
        // Get current tenant and switch database
        var currentTenant = tenantService.GetCurrentTenant();
        var tenantDatabase = tenantService.GetTenantSchema(currentTenant);
        connectionString = baseConnectionString.Replace($"Database={baseDatabaseName}", $"Database={tenantDatabase}");
    }
    else
    {
        // Fallback to base database
        connectionString = baseConnectionString;
    }
    
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
}); 
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
/*builder.Services.AddAuthentication(
        CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = context =>
            {
                if (validationService.ValidateCertificate(context.ClientCertificate))
                {
                    context.Success();
                }
                else
                {
                    context.Fail("invalid cert");
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                context.Fail("invalid cert");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.ConfigureHttpsDefaults(options =>
        options.ClientCertificateMode = ClientCertificateMode.RequireCertificate);
});*/
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    // Define multiple Swagger documents, one for each logical grouping
    c.SwaggerDoc("tenants", new OpenApiInfo { Title = "1. Tenant Management", Version = "v1" });
    c.SwaggerDoc("test", new OpenApiInfo { Title = "7. test", Version = "v1" });
    c.SwaggerDoc("programs", new OpenApiInfo { Title = "2. Training Program Management", Version = "v1" });
    c.SwaggerDoc("paths", new OpenApiInfo { Title = "3. Learning Path Management", Version = "v1" });
    c.SwaggerDoc("materials", new OpenApiInfo { Title = "4. Material Management", Version = "v1" });
    c.SwaggerDoc("assets", new OpenApiInfo { Title = "5. Asset Management", Version = "v1" });
    c.SwaggerDoc("users", new OpenApiInfo   { Title = "6. User Management", Version = "v1" });

    c.SwaggerDoc("all", new OpenApiInfo { 
        Title = "Complete XR50 Training Asset Repository API", 
        Version = "v1",
        Description = "Complete API documentation covering all controllers and endpoints"
    });

    c.DocumentFilter<HierarchicalOrderDocumentFilter>();

    // Define which controllers go into which document
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
         if (docName == "all") return true;
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
        
        return docName switch
        {
            "tenants" => controllerName.Contains("Tenants"),
            "programs" => controllerName.Contains("TrainingPrograms"),
            "paths" => controllerName.Contains("LearningPaths"),
            "materials" => controllerName.Contains("Materials"),
            "assets" => controllerName.Contains("Assets"),
            "users" => controllerName.Contains("Users"),
            "test" => controllerName.Contains("test"),
            _ => false
        };
    });
});


builder.Configuration.AddJsonFile("appsettings.json");
builder.Services.AddCors(options =>{
            options.AddDefaultPolicy(
                builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowAnyOrigin();
                });
        });
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
      {
          // Add endpoints in desired order
          c.SwaggerEndpoint("/swagger/v1/swagger.json", "Default");
          c.SwaggerEndpoint("/swagger/tenants/swagger.json", "1. Tenant Management");
          c.SwaggerEndpoint("/swagger/programs/swagger.json", "2. Training Program Management");
          c.SwaggerEndpoint("/swagger/paths/swagger.json", "3. Learning Path Management");
          c.SwaggerEndpoint("/swagger/materials/swagger.json", "4. Material Management");
          c.SwaggerEndpoint("/swagger/assets/swagger.json", "5. Asset Management");
          c.SwaggerEndpoint("/swagger/users/swagger.json", "6. User Management");
      });
}

app.UseRouting(); 
app.MapControllers();
 

app.Run();

public class HierarchicalOrderDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Create ordered tags
        var orderedTags = new List<OpenApiTag>();

        // Define tag order
        var tagOrder = new Dictionary<string, int>
        {
            { "tenants", 1 },
            { "trainingPrograms", 2 },
            { "learningPaths", 3 },
            { "materials", 4 },
            { "assets", 5 },
            { "users", 6 }
        };

        // Add existing tags in order
        if (swaggerDoc.Tags != null)
        {
            // Create a new ordered collection, preserving all existing tags
            var existingTags = swaggerDoc.Tags.ToList();

            // Order the existing tags
            swaggerDoc.Tags = existingTags
                .OrderBy(t => tagOrder.ContainsKey(t.Name) ? tagOrder[t.Name] : 999)
                .ToList();
        }

        // Don't modify paths unless you need to - they're already ordered by URL
        // For ordering paths, create a similar approach but be careful to preserve all paths
    }
}
// Updated Program.cs Registration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddXR50MultitenancyWithDynamicDb(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IXR50TenantService, XR50TenantService>();
        services.AddScoped<IXR50TenantManagementService, XR50TenantManagementService>();
        services.AddScoped<XR50MigrationService>();
        services.AddScoped<IXR50DatabaseInitializer, XR50DatabaseInitializer>();
        services.AddScoped<IXR50TenantTroubleshootingService, XR50TenantTroubleshootingService>();
        services.AddScoped<IXR50ManualTableCreator, XR50ManualTableCreator>();

        // Register DbContext with proper configuration
        services.AddDbContext<XR50TrainingContext>((serviceProvider, options) =>
        {
            var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
            var tenantService = serviceProvider.GetService<IXR50TenantService>();
            var config = serviceProvider.GetService<IConfiguration>();
            
            var baseDatabaseName = configuration["BaseDatabaseName"] ?? "magical_library";
            var baseConnectionString = configuration.GetConnectionString("DefaultConnection");
            
            string connectionString;
            
            if (httpContextAccessor?.HttpContext != null && tenantService != null)
            {
                // Get current tenant and switch database
                var currentTenant = tenantService.GetCurrentTenant();
                var tenantDatabase = tenantService.GetTenantSchema(currentTenant);
                connectionString = baseConnectionString.Replace($"Database={baseDatabaseName}", $"Database={tenantDatabase}");
            }
            else
            {
                // Fallback to base database
                connectionString = baseConnectionString;
            }
            
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }); 

        return services;
    }
    
    // Extension method to initialize databases
    public static async Task<IApplicationBuilder> InitializeXR50DatabasesAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IXR50DatabaseInitializer>();
        
        try
        {
            await initializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Failed to initialize databases during startup");
            throw;
        }
        
        return app;
    }
}