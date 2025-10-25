using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Data;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using RestaurantApi.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "RestaurantApi_";
});

builder.Services.AddScoped<OrderService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Logea la excepción con Serilog y relanza para que el pipeline la maneje (o puedes retornar un 500 aquí)
        Log.Error(ex, "Unhandled exception processing request {Method} {Path}", context.Request.Method, context.Request.Path);
        throw;
    }
    finally
    {
        sw.Stop();
        Log.Information("Request {Method} {Path} responded {StatusCode} in {Elapsed:0.0000} ms",
            context.Request.Method,
            context.Request.Path,
            context.Response?.StatusCode,
            sw.Elapsed.TotalMilliseconds);
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
