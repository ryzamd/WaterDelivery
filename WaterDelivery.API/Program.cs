using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using WaterDelivery.API.Middleware;
using WaterDelivery.Application.Interfaces;
using WaterDelivery.Infrastructure.Data;
using WaterDelivery.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// JWT Configuration
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new ArgumentNullException("Jwt:SecretKey");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Google OAuth if configured
var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

builder.Services.AddAuthorization();

// Rate Limiting Configuration (Configurable)
var rateLimitingEnabled = builder.Configuration.GetValue<bool>("RateLimiting:Enabled", true);

if (rateLimitingEnabled)
{
    Console.WriteLine("Rate Limiting ENABLED");

    var globalLimit = builder.Configuration.GetValue<int>("RateLimiting:GlobalLimit", 100);
    var authLimit = builder.Configuration.GetValue<int>("RateLimiting:AuthLimit", 5);
    var userLimit = builder.Configuration.GetValue<int>("RateLimiting:UserLimit", 30);
    var strictLimit = builder.Configuration.GetValue<int>("RateLimiting:StrictLimit", 10);
    var adminLimit = builder.Configuration.GetValue<int>("RateLimiting:AdminLimit", 50);

    builder.Services.AddRateLimiter(options =>
    {
        // Global rate limiting
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = globalLimit,
                    Window = TimeSpan.FromMinutes(1)
                }));

        // Auth policy - authentication endpoints
        options.AddFixedWindowLimiter("AuthPolicy", options =>
        {
            options.PermitLimit = authLimit;
            options.Window = TimeSpan.FromMinutes(1);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 3;
        });

        // User policy - normal operations
        options.AddFixedWindowLimiter("UserPolicy", options =>
        {
            options.PermitLimit = userLimit;
            options.Window = TimeSpan.FromMinutes(1);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 5;
        });

        // Strict policy - sensitive operations
        options.AddFixedWindowLimiter("StrictPolicy", options =>
        {
            options.PermitLimit = strictLimit;
            options.Window = TimeSpan.FromMinutes(1);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 2;
        });

        // Admin policy - admin operations  
        options.AddFixedWindowLimiter("AdminPolicy", options =>
        {
            options.PermitLimit = adminLimit;
            options.Window = TimeSpan.FromMinutes(1);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 10;
        });

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            context.HttpContext.Response.ContentType = "application/json";

            var response = new
            {
                error = "Too many requests",
                message = "Rate limit exceeded. Please try again later.",
                retryAfter = "60 seconds"
            };

            await context.HttpContext.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(response), token);
        };
    });
}
else
{
    Console.WriteLine("Rate Limiting DISABLED (Performance Testing Mode)");
}

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ISmsService, TwilioSmsService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global Exception Handling (FIRST - to catch all errors)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Rate Limiting (SECOND - after error handling, before auth)
if (rateLimitingEnabled)
{
    app.UseRateLimiter();
}

//app.UseHttpsRedirection();

// Auto redirect to Swagger in development
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

// Authentication & Authorization (AFTER rate limiting)
app.UseAuthentication();
app.UseAuthorization();

// Controllers (LAST)
app.MapControllers();

// Test database connection
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await context.Database.CanConnectAsync();
        Console.WriteLine("Database connection successful!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database connection failed: {ex.Message}");
    }
}

Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Rate Limiting: {(rateLimitingEnabled ? "ENABLED" : "DISABLED")}");

app.Run();