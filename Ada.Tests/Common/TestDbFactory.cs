using Ada.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Ada.Tests.Common;

public class TestDbFactory
{
    public static IDbContextFactory<AdaDbContext> CreateDbFactory()
    {
        var options = new DbContextOptionsBuilder<AdaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var factory = new PooledDbContextFactory<AdaDbContext>(options);
        return factory;
    }
}