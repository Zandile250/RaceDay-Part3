using System.Reflection;
using Azure.Storage.Blobs;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Part 3: Azure Blob Storage for event banners and profile pictures.
// Locally and in Docker this talks to the Azurite emulator; in Azure set AzureStorage__ConnectionString.
builder.Services.AddSingleton(new BlobServiceClient(
    builder.Configuration["AzureStorage:ConnectionString"] ?? "UseDevelopmentStorage=true"));
builder.Services.AddSingleton<IFileStorage, BlobFileStorage>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Session management: keeps the logged-in user's id and role between requests
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "RaceDay API", Version = "v1" });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.IncludeXmlComments(xml);
});

var app = builder.Build();

// Create or update the database from the migrations when the app starts
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<RaceDay.Api.Data.AppDbContext>().Database.Migrate();
}


app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseSession();
app.MapControllers();
app.Run();

public partial class Program { }
