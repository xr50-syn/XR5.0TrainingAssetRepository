using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();

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
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Configuration.AddJsonFile("appsettings.json");
var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseAuthentication();    
app.UseAuthorization();

app.MapControllers();

app.Run();
