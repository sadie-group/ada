using Ada.Db.Models;
using Ada.Tests.Common;

namespace Ada.Tests.Db;

[TestFixture]
public class AdaDbContextTests
{
    [Test]
    public async Task Badges_SaveAndQuery_RoundTrips()
    {
        var factory = TestDbFactory.CreateDbFactory();

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Badges.Add(new Badge { Id = 1, Code = "ADM" });
            await context.SaveChangesAsync();
        }

        await using (var context = await factory.CreateDbContextAsync())
        {
            var badge = await context.Badges.SingleAsync(b => b.Id == 1);
            Assert.That(badge.Code, Is.EqualTo("ADM"));
        }
    }

    [Test]
    public async Task CreateDbFactory_EachFactory_IsolatesData()
    {
        var first = TestDbFactory.CreateDbFactory();
        var second = TestDbFactory.CreateDbFactory();

        await using (var context = await first.CreateDbContextAsync())
        {
            context.Badges.Add(new Badge { Id = 1, Code = "ADM" });
            await context.SaveChangesAsync();
        }

        await using (var context = await second.CreateDbContextAsync())
        {
            Assert.That(await context.Badges.AnyAsync(), Is.False);
        }
    }
}
