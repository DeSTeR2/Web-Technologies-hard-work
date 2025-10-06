using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ToDoApp.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddDbContext<ToDoContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MainConnection"));
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TodoList",
        Version = "v1"
    });
});

var app = builder.Build();
app.UseHttpsRedirection();
app.UseDeveloperExceptionPage();
app.MapRazorPages();
app.UseRouting();
app.MapControllers();

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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoList API v1");
    });
}

app.MapGet("/", () => "Hello");
app.Run();