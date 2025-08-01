using EcomMMS.Application;
using EcomMMS.Infrastructure;
using EcomMMS.Persistence;
using EcomMMS.API.Middleware;
using EcomMMS.API.Configuration;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://seq:5341")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<Redis>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<Seq>(builder.Configuration.GetSection("Seq"));
builder.Services.Configure<RateLimiting>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.Configure<ApiVersioning>(builder.Configuration.GetSection("ApiVersioning"));
builder.Services.Configure<Cors>(builder.Configuration.GetSection("Cors"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "EcomMMS API",
        Version = "v1",
        Description = "E-commerce Management System API"
    });
});

builder.Services.AddCors(options =>
{
    var corsSettings = builder.Configuration.GetSection("Cors").Get<Cors>();
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(corsSettings?.AllowedOrigins ?? new[] { "http://localhost:3000" })
              .WithMethods(corsSettings?.AllowedMethods ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" })
              .WithHeaders(corsSettings?.AllowedHeaders ?? new[] { "Content-Type", "Authorization" });
        
        if (corsSettings?.AllowCredentials == true)
        {
            policy.AllowCredentials();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    var rateLimitSettings = builder.Configuration.GetSection("RateLimiting").Get<RateLimiting>();
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = rateLimitSettings?.PermitLimit ?? 100;
        limiterOptions.Window = TimeSpan.FromSeconds(rateLimitSettings?.Window ?? 60);
        limiterOptions.QueueLimit = 2;
    });
});

builder.Services.AddApiVersioning(options =>
{
    var apiVersionSettings = builder.Configuration.GetSection("ApiVersioning").Get<ApiVersioning>();
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = apiVersionSettings?.AssumeDefaultVersionWhenUnspecified ?? true;
    options.ReportApiVersions = apiVersionSettings?.ReportApiVersions ?? true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-API-Version"),
        new MediaTypeApiVersionReader("version")
    );
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=ecommms;Username=postgres;Password=postgres")
    .AddRedis(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379");

var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
if (string.IsNullOrEmpty(redisConnectionString))
{
    throw new InvalidOperationException("Redis connection string is not configured. Please add 'Redis:ConnectionString' to your configuration.");
}

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "EcomMMS_";
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

var app = builder.Build();

app.UseRequestResponseLogging();
app.UseCachePerformanceMonitoring();

app.UseCors("CorsPolicy");

app.UseRateLimiter();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        context.Database.EnsureCreated();

        SeedData.SeedCategories(context, logger);
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to seed database");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcomMMS API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

try
{
    Log.Information("Starting EcomMMS API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

