using System.IO;
using System.Text;
using System.Threading.Channels;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using ProductsApi.Constants;
using Microsoft.IdentityModel.Tokens;
using ProductsApi.Configuration;
using ProductsApi.Models;
using ProductsApi.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var mongoSettings = new MongoDbSettings
{
    ConnectionString = Environment.GetEnvironmentVariable(ApiConstants.Environment.MongoConnectionString) ?? ApiConstants.Defaults.MongoConnectionString,
    DatabaseName = Environment.GetEnvironmentVariable(ApiConstants.Environment.MongoDatabaseName) ?? ApiConstants.Defaults.MongoDatabaseName
};

var jwtSettings = new JwtSettings
{
    Secret = Environment.GetEnvironmentVariable(ApiConstants.Environment.JwtSecret) ?? throw new InvalidOperationException(ApiConstants.Errors.JwtSecretRequired),
    Issuer = Environment.GetEnvironmentVariable(ApiConstants.Environment.JwtIssuer) ?? ApiConstants.Defaults.JwtIssuer,
    Audience = Environment.GetEnvironmentVariable(ApiConstants.Environment.JwtAudience) ?? ApiConstants.Defaults.JwtAudience
};

builder.Services.Configure<MongoDbSettings>(options =>
{
    options.ConnectionString = mongoSettings.ConnectionString;
    options.DatabaseName = mongoSettings.DatabaseName;
});

builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = jwtSettings.Secret;
    options.Issuer = jwtSettings.Issuer;
    options.Audience = jwtSettings.Audience;
    options.AccessTokenExpirationMinutes = jwtSettings.AccessTokenExpirationMinutes;
    options.RefreshTokenExpirationDays = jwtSettings.RefreshTokenExpirationDays;
});

builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddSingleton(Channel.CreateBounded<AuditEntry>(new BoundedChannelOptions(1024)
{
    SingleReader = true,
    SingleWriter = false,
    FullMode = BoundedChannelFullMode.DropWrite
}));
builder.Services.AddSingleton<IAuditService, AuditService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHostedService<AuditBackgroundService>();
builder.Services.AddHostedService<MongoIndexInitializerHostedService>();

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection()
    .SetApplicationName(ApiConstants.Defaults.ApplicationName)
    .PersistKeysToFileSystem(new DirectoryInfo(ApiConstants.Defaults.DataProtectionDirectory));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies[ApiConstants.Defaults.AccessTokenCookie];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy(ApiConstants.Defaults.CorsPolicy, policy =>
    {
        policy.WithOrigins(Environment.GetEnvironmentVariable(ApiConstants.Environment.FrontendUrl) ?? ApiConstants.Defaults.FrontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(ApiConstants.Defaults.GlobalExceptionLogger);

        if (exceptionFeature?.Error is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request aborted by the client.");
            context.Response.StatusCode = 499;
            return;
        }

        logger.LogError(exceptionFeature?.Error, "Unhandled exception while processing request.");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(ApiConstants.Errors.UnexpectedError));
    });
});

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseCors(ApiConstants.Defaults.CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
