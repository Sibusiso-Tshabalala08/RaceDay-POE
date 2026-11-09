using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

/// <summary>
/// Spins up the real RaceDay API (full middleware pipeline, including
/// session) against an isolated in-memory database, so tests exercise
/// actual HTTP requests through actual controllers rather than mocks.
/// Each instance gets its own uniquely named database so test classes
/// never interfere with one another.
/// </summary>
public class RaceDayWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registration from Program.cs...
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // ...and replace it with an isolated in-memory database.
            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Never talk to real Azure from tests: swap in an in-memory fake
            // that just returns a predictable URL.
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();
        });
    }
}
