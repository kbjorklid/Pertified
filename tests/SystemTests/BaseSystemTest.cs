using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Users.Infrastructure;

namespace SystemTests;

/// <summary>
/// Shared test fixture for database container that persists across all tests in a class.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    public PostgreSqlContainer DbContainer { get; private set; } = null!;
    public WebApplicationFactory<Program> WebAppFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Configure PostgreSQL container matching docker-compose.yml settings
        DbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("pertified")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        // Start the database container first
        await DbContainer.StartAsync();

        // Get the dynamic connection string from the container
        string containerConnectionString = DbContainer.GetConnectionString();

        // Create WebApplicationFactory after container is started
        WebAppFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Use UseSetting to override connection string during host building
                // This ensures the connection string is available when Program.cs calls GetConnectionString
                builder.UseSetting("ConnectionStrings:DefaultConnection", containerConnectionString);

                builder.ConfigureLogging(logging =>
                {
                    // Reduce logging noise in tests
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            });

        // Apply migrations to create database schema
        using IServiceScope scope = WebAppFactory.Services.CreateScope();
        UsersDbContext dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await WebAppFactory.DisposeAsync();
        await DbContainer.DisposeAsync();
    }
}

/// <summary>
/// Base class for system tests providing common test infrastructure including database setup.
/// </summary>
public abstract class BaseSystemTest : IClassFixture<DatabaseFixture>
{
    protected readonly DatabaseFixture DatabaseFixture;
    protected WebApplicationFactory<Program> WebAppFactory => DatabaseFixture.WebAppFactory;
    protected HttpClient HttpClient { get; private set; }

    protected BaseSystemTest(DatabaseFixture databaseFixture)
    {
        DatabaseFixture = databaseFixture;
        HttpClient = WebAppFactory.CreateClient();
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
