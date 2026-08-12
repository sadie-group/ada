using Ada.Db;
using Ada.Db.Configuration;
using Ada.Db.Models.Catalog.FrontPage;
using Ada.Db.Models.Catalog.Pages;
using Ada.Db.Models.Constants;
using Ada.Tests.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ada.Tests.Db;

[TestFixture]
[NonParallelizable]
public class DatabaseServiceCollectionTests
{
    private const string ConnectionString = "server=127.0.0.1;port=3306;database=ada;user=root;password=secret";

    private static IConfiguration CreateConfiguration(string? connectionString = ConnectionString)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString
            })
            .Build();

    private sealed class NoSnakeCaseConfiguration : IModelConfiguration
    {
        public bool UseSnakeCaseNamingConvention => false;

        public void Apply(ModelBuilder modelBuilder) => new DefaultModelConfiguration().Apply(modelBuilder);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void AddServices_MissingConnectionString_Throws(string? connectionString)
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(
            () => DatabaseServiceCollection.AddServices(services, CreateConfiguration(connectionString)));
    }

    [Test]
    public void AddServices_ResolvesPooledFactoryAndTransientContext()
    {
        var services = new ServiceCollection();
        DatabaseServiceCollection.AddServices(services, CreateConfiguration());

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<AdaDbContext>>();
        using var pooled = factory.CreateDbContext();
        using var transient = provider.GetRequiredService<AdaDbContext>();

        Assert.Multiple(() =>
        {
            Assert.That(pooled, Is.Not.Null);
            Assert.That(transient, Is.Not.Null);
            Assert.That(provider.GetRequiredService<IDbContextFactory<AdaMigrationsDbContext>>(), Is.Not.Null);
        });
    }

    [Test]
    public void AddServices_WithoutSnakeCaseConvention_StillResolvesContext()
    {
        var previous = ModelConfigurationProvider.Active;
        ModelConfigurationProvider.Active = new NoSnakeCaseConfiguration();

        try
        {
            var services = new ServiceCollection();
            DatabaseServiceCollection.AddServices(services, CreateConfiguration());

            using var provider = services.BuildServiceProvider();
            using var context = provider.GetRequiredService<IDbContextFactory<AdaDbContext>>().CreateDbContext();

            Assert.That(context, Is.Not.Null);
        }
        finally
        {
            ModelConfigurationProvider.Active = previous;
        }
    }

    [Test]
    public void AddServices_ConstantsSingletons_LoadFromDatabase()
    {
        using var sqlite = new SqliteTestDbFactory();
        var converter = new DateTimeOffsetToBinaryConverter();
        var older = (long)converter.ConvertToProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))!;
        var newer = (long)converter.ConvertToProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero))!;

        using (var db = sqlite.CreateDbContext())
        {
            db.Database.ExecuteSqlRaw(
                "INSERT INTO server_player_constants (\"MaxMottoLength\",\"MinSsoLength\",\"MaxFriendships\",\"CreatedAt\") " +
                $"VALUES (38, 12, 300, {older})");
            db.Database.ExecuteSqlRaw(
                "INSERT INTO server_room_constants (\"MaxChatMessageLength\",\"SecondsTillUserIdle\",\"MaxNameLength\",\"MaxDescriptionLength\",\"MaxTagLength\",\"WiredMaxFurnitureSelection\",\"CreatedAt\") " +
                $"VALUES (100, 600, 25, 128, 15, 5, {older})");
            db.Database.ExecuteSqlRaw(
                "INSERT INTO server_room_constants (\"MaxChatMessageLength\",\"SecondsTillUserIdle\",\"MaxNameLength\",\"MaxDescriptionLength\",\"MaxTagLength\",\"WiredMaxFurnitureSelection\",\"CreatedAt\") " +
                $"VALUES (200, 1200, 30, 255, 20, 10, {newer})");
            db.CatalogPages.Add(new CatalogPage { Id = 1, Name = "front" });
            db.Set<CatalogFrontPageItem>().Add(new CatalogFrontPageItem { Id = 1, Title = "promo", CatalogPageId = 1 });
            db.SaveChanges();
        }

        var services = new ServiceCollection();
        DatabaseServiceCollection.AddServices(services, CreateConfiguration());
        services.Replace(ServiceDescriptor.Singleton<IDbContextFactory<AdaDbContext>>(sqlite));

        using var provider = services.BuildServiceProvider();
        var playerConstants = provider.GetRequiredService<ServerPlayerConstants>();
        var roomConstants = provider.GetRequiredService<ServerRoomConstants>();
        var frontPageItems = provider.GetRequiredService<List<CatalogFrontPageItem>>();

        Assert.Multiple(() =>
        {
            Assert.That(playerConstants.MaxMottoLength, Is.EqualTo(38));
            Assert.That(playerConstants.MaxFriendships, Is.EqualTo(300));
            Assert.That(roomConstants.SecondsTillUserIdle, Is.EqualTo(1200));
            Assert.That(roomConstants.MaxChatMessageLength, Is.EqualTo(200));
            Assert.That(frontPageItems, Has.Count.EqualTo(1));
            Assert.That(frontPageItems[0].Title, Is.EqualTo("promo"));
            Assert.That(frontPageItems[0].CatalogPage, Is.Not.Null);
            Assert.That(frontPageItems[0].CatalogPage!.Name, Is.EqualTo("front"));
        });
    }
}
