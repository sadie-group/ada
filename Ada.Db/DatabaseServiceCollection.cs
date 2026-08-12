using Ada.Db.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.Db.Configuration;
using Ada.Db.Models.Catalog.FrontPage;
using Ada.Db.Models.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        void ConfigureAdaDb(DbContextOptionsBuilder options)
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

            if (ModelConfigurationProvider.Active.UseSnakeCaseNamingConvention)
            {
                options.UseSnakeCaseNamingConvention();
            }
        }

        serviceCollection.AddPooledDbContextFactory<AdaDbContext>(ConfigureAdaDb);
        serviceCollection.AddSingleton<IPlayerPresenceStore, PlayerPresenceStore>();

        serviceCollection.AddTransient<AdaDbContext>(provider =>
            provider.GetRequiredService<IDbContextFactory<AdaDbContext>>().CreateDbContext());

        serviceCollection.AddDbContextFactory<AdaMigrationsDbContext>();

        serviceCollection.AddSingleton<ServerPlayerConstants>(provider =>
        {
            using var dbContext = provider.GetRequiredService<IDbContextFactory<AdaDbContext>>().CreateDbContext();
            return dbContext.ServerPlayerConstants.AsNoTracking().First();
        });

        serviceCollection.AddSingleton<ServerRoomConstants>(provider =>
        {
            using var dbContext = provider.GetRequiredService<IDbContextFactory<AdaDbContext>>().CreateDbContext();
            return dbContext.ServerRoomConstants
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .First();
        });

        serviceCollection.AddSingleton(provider =>
        {
            using var dbContext = provider.GetRequiredService<IDbContextFactory<AdaDbContext>>().CreateDbContext();
            return dbContext
                .Set<CatalogFrontPageItem>()
                .AsNoTracking()
                .Include(x => x.CatalogPage)
                .ToList();
        });
    }
}
