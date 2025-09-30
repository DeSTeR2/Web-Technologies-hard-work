using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using ProjectInfrastructure.Context;
using ProjectInfrastructure.Models;
using ProjectMVC.Utils.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add MVC and OData controllers
builder.Services.AddControllersWithViews();
builder.Services.AddControllers().AddOData(options =>
{
    var modelBuilder = new ODataConventionModelBuilder();
    modelBuilder.EntityType<LeaderboardModel>();
    modelBuilder.EntitySet<LeaderboardRecordModel>("Records");

    options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(null)
           .AddRouteComponents("odata", modelBuilder.GetEdmModel());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });


// Add DB contexts
builder.Services.AddDbContext<LeaderboardDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MainConnection")));

builder.Services.AddDbContext<UserApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MainConnection")));

// Configure Identity with your custom User model
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<UserApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllers().AddControllersAsServices();
builder.Services.AddControllers()
    .AddJsonOptions(opt => {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LeaderboardSwagger",
        Version = "v1"
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });


var app = builder.Build();

// Apply EF migrations
app.ApplyMigration<UserApplicationDbContext>();
app.ApplyMigration<LeaderboardDbContext>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "LeaderboardAPI v1");
    });
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // Ensure authentication is active
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
