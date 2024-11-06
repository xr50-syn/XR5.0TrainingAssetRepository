using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Certificate;
using XR5_0TrainingRepo.Models;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;


var builder = WebApplication.CreateBuilder(args);
var  MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<OwncloudShareContext>(opt => 
    opt.UseInMemoryDatabase("OwncloudShares"));
builder.Services.AddDbContext<TrainingContext>(opt =>
    opt.UseInMemoryDatabase("TrainingCatalogue"));
builder.Services.AddDbContext<AssetContext>(opt =>
    opt.UseInMemoryDatabase("AssetRepository"));
builder.Services.AddDbContext<ResourceContext>(opt =>
   opt.UseInMemoryDatabase("ResourceRepository"));

builder.Services.AddDbContext<XRAIInterfaceContext>(opt =>
    opt.UseInMemoryDatabase("QueryDb"));
builder.Services.AddDbContext<UserContext>(opt =>
    opt.UseInMemoryDatabase("UserList"));
builder.Services.AddDbContext<XR50AppContext>(opt =>
    opt.UseInMemoryDatabase("AppList"));
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy  =>
                      {
                          policy.WithOrigins("https://emmie.frontdesk.lab.synelixis.com");
                      });
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
builder.Services.AddSwaggerGen();
builder.Configuration.AddJsonFile("appsettings.json");
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();    
app.UseAuthorization();

app.MapControllers();

app.Run();
