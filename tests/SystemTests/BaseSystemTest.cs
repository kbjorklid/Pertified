using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Users.Infrastructure;

namespace SystemTests;

/// <summary>
/// Base class for system tests providing common test infrastructure including database setup.
/// </summary>
public abstract class BaseSystemTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    protected WebApplicationFactory<Program> WebAppFactory = null!;
    protected HttpClient HttpClient = null!;

    protected BaseSystemTest()
    {
        // Configure PostgreSQL container matching docker-compose.yml settings
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("pertified")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start the database container first
        await _dbContainer.StartAsync();

        // Create WebApplicationFactory after container is started
        WebAppFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Override connection string to use test database
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString()
                    }!);
                });

                builder.ConfigureLogging(logging =>
                {
                    // Reduce logging noise in tests
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            });

        HttpClient = WebAppFactory.CreateClient();

        // Apply migrations to create database schema
        using IServiceScope scope = WebAppFactory.Services.CreateScope();
        UsersDbContext dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearDatabaseAsync();
        HttpClient.Dispose();
        await WebAppFactory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Clears all data from the database to ensure clean state for each test.
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        using IServiceScope scope = WebAppFactory.Services.CreateScope();
        UsersDbContext dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        // Clear Users table
        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Users\".\"Users\" CASCADE");
    }

    /// <summary>
    /// Serializes an object to JSON with camelCase naming policy to match API conventions.
    /// </summary>
    protected static StringContent ToJsonContent(object obj)
    {
        string json = JsonSerializer.Serialize(obj,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Deserializes JSON response to the specified type with camelCase naming policy.
    /// </summary>
    protected static async Task<T> FromJsonAsync<T>(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;
    }

}
