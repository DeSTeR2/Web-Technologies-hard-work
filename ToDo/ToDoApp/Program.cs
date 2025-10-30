using System.Text.Json.Serialization;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using ToDoAPI.API;
using ToDoAPI.Settings;
using ToDoAPI.Tool;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddRazorPages();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.WriteIndented = true;
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

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
var mongoSettings = builder.Configuration.GetSection("MongoDb").Get<MongoDbSettings>() ?? new MongoDbSettings();

var mongoClient = new MongoClient(mongoSettings.ConnectionString);
var mongoDatabase = mongoClient.GetDatabase(mongoSettings.DatabaseName);

builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton(mongoDatabase);
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddScoped<GoogleCalendarService>();
builder.Services.AddScoped<CalendarController>();

builder.Services.AddDataProtection();
builder.Services.AddSingleton<TokenProtector>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<GoogleTokenStore>();
builder.Services.AddSingleton<UserDataService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<GoogleApiTokenProvider>();
builder.Services.AddAWSService<IAmazonS3>(new AWSOptions
{
    Region = RegionEndpoint.EUCentral1
});
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Login";
    });


builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});


var gcal = builder.Configuration.GetSection("GoogleCalendar");
var googleClientId = gcal["client_id"];
var googleClientSecret = gcal["client_secret"];


builder.Services.AddSingleton<GoogleAuthorizationCodeFlow>(_ =>
    new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
    {
        ClientSecrets = new ClientSecrets
        {
            ClientId = googleClientId,
            ClientSecret = googleClientSecret
        },
        Scopes = new[] { CalendarService.Scope.Calendar },
        DataStore = new FileDataStore("google-calendar-tokens", true) // persists refresh tokens
    })
);


builder.Services.AddScoped<CalendarService>(sp =>
{
    var flow = sp.GetRequiredService<GoogleAuthorizationCodeFlow>();
    var userId = sp.GetRequiredService<UserDataService>().GetCurrentUserId();


    var token = flow.DataStore.GetAsync<TokenResponse>(userId).GetAwaiter().GetResult();
    if (token == null || (string.IsNullOrEmpty(token.RefreshToken) && string.IsNullOrEmpty(token.AccessToken)))
        throw new InvalidOperationException("Google Calendar is not authorized for this user.");

    var credential = new UserCredential(flow, userId, token);
    return new CalendarService(new BaseClientService.Initializer
    {
        HttpClientInitializer = credential,
        ApplicationName = "TodoList"
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoList API v1"); });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapHub<TodoSyncHub>("/ws/todoSync");


app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Login");
    return Task.CompletedTask;
});

app.MapGet("/MainPage", context =>
{
    context.Response.Redirect("/Home");
    return Task.CompletedTask;
});

app.Run();