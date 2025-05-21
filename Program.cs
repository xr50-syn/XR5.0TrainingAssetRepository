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


var builder = WebApplication.CreateBuilder(args);
var  MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
// Add services to the container.

builder.Services.AddControllers();
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<XR50TrainingAssetRepoContext>(opt =>
    opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));    
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
builder.Services.AddSwaggerGen(c =>
{
    // Define multiple Swagger documents, one for each logical grouping
    c.SwaggerDoc("tenants", new OpenApiInfo { Title = "1. Tenant Management", Version = "v1" });
    c.SwaggerDoc("programs", new OpenApiInfo { Title = "2. Training Program Management", Version = "v1" });
    c.SwaggerDoc("paths", new OpenApiInfo { Title = "3. Learning Path Management", Version = "v1" });
    c.SwaggerDoc("materials", new OpenApiInfo { Title = "4. Material Management", Version = "v1" });
    c.SwaggerDoc("assets", new OpenApiInfo { Title = "5. Asset Management", Version = "v1" });
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
            "tenants" => controllerName.Contains("tenants"),
            "programs" => controllerName.Contains("trainingPrograms"),
            "paths" => controllerName.Contains("learningPaths"),
            "materials" => controllerName.Contains("materials"),
            "assets" => controllerName.Contains("assets"),
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

    // Configure Swagger UI 
    app.UseSwaggerUI(c =>
    {
        // Add endpoints in desired order
        c.SwaggerEndpoint("/swagger/tenants/swagger.json", "1. Tenant Management");
        c.SwaggerEndpoint("/swagger/programs/swagger.json", "2. Training Program Management");
        c.SwaggerEndpoint("/swagger/paths/swagger.json", "3. Learning Path Management");
        c.SwaggerEndpoint("/swagger/materials/swagger.json", "4. Material Management");
        c.SwaggerEndpoint("/swagger/assets/swagger.json", "5. Asset Management");
    });




    try
    {

        app.MapControllers();
    }
    catch (System.Exception exp)
    {

        throw;
    }

    app.Run();
}

public class HierarchicalOrderDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Only apply to the "all" document
        if (context.DocumentName != "all")
            return;
            
        // Create a new ordered dictionary for paths
        var orderedPaths = new OrderedDictionary();
        
        // Define the order we want sections to appear in
        var sectionPrefixes = new[]
        {
            "/xr50/trainingAssetRepository/tenantManagement",
            "/xr50/trainingAssetRepository/trainingProgramManagement",
            "/xr50/trainingAssetRepository/learningPathManagement",
            "/xr50/trainingAssetRepository/materialManagement",
            "/xr50/trainingAssetRepository/assetManagement"
        };
        
        // Add paths in the desired order
        foreach (var prefix in sectionPrefixes)
        {
            // Find all paths that start with this prefix
            var matchingPaths = swaggerDoc.Paths
                .Where(p => p.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Key) // Order alphabetically within each section
                .ToList();
                
            // Add them to our ordered dictionary
            foreach (var path in matchingPaths)
            {
                orderedPaths.Add(path.Key, path.Value);
                // Remove from original to track which ones we've processed
                swaggerDoc.Paths.Remove(path.Key);
            }
        }
        
        // Add any remaining paths that didn't match our prefixes
        foreach (var path in swaggerDoc.Paths.OrderBy(p => p.Key))
        {
            orderedPaths.Add(path.Key, path.Value);
        }
        
        // Clear and rebuild the paths collection
        swaggerDoc.Paths.Clear();
        foreach (DictionaryEntry path in orderedPaths)
        {
            swaggerDoc.Paths.Add(path.Key.ToString(), (OpenApiPathItem)path.Value);
        }
    }
}