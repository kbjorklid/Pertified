using Microsoft.Extensions.Hosting;
using Wolverine;

namespace Users.Application;

/// <summary>
/// Extension methods for registering Users Application services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Users Application services to the dependency injection container.
    /// </summary>
    /// <param name="hostBuilder">The host builder to configure Wolverine on.</param>
    /// <returns>The host builder for method chaining.</returns>
    public static IHostBuilder AddUsersApplicationServices(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseWolverine(opts =>
        {
            // Auto-discover message handlers in this assembly
            opts.Discovery.IncludeAssembly(typeof(AssemblyMarker).Assembly);

            // Configure for mediator usage
            opts.Durability.Mode = DurabilityMode.MediatorOnly;
        });
    }
}
