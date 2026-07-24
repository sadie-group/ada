using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ada.Db.Models.Catalog.FrontPage;
using Ada.Db.Models.Constants;

namespace Ada.Db;

public static class DatabaseServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No database connection string configured. Set ConnectionStrings:Default in " +
                "appsettings.json or the ConnectionStrings__Default environment variable.");
        }

        serviceCollection.AddDbContextFactory<AdaMigrationsDbContext>();
        serviceCollection.AddDbContextFactory<AdaDbContext>();

        serviceCollection.AddDbContext<AdaDbContext>(options =>
        {
            options.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion,
                    mySqlOptions =>
                    {
                        mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);

                        mySqlOptions.MigrationsAssembly("Ada.Db");
                    })
                .LogTo(Console.WriteLine, LogLevel.Error);
        
            options.UseSnakeCaseNamingConvention();
        }, ServiceLifetime.Transient);
        
        serviceCollection.AddDbContextFactory<AdaMigrationsDbContext>();

        serviceCollection.AddSingleton<ServerPlayerConstants>(provider =>
            provider.GetRequiredService<AdaDbContext>()
                .ServerPlayerConstants
                .First()
        );

        serviceCollection.AddSingleton<ServerRoomConstants>(provider =>
            provider.GetRequiredService<AdaDbContext>()
                .ServerRoomConstants
                .OrderByDescending(x => x.CreatedAt)
                .First()
        );

        serviceCollection.AddSingleton(provider =>
            provider.GetRequiredService<AdaDbContext>()
                .Set<CatalogFrontPageItem>()
                .Include(x => x.CatalogPage)
                .ToList());
    }
}
