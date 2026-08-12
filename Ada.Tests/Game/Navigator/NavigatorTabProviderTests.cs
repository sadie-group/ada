using Ada.API.DTOs.Navigator;
using Ada.Db.Models.Navigator;
using Ada.Game.Navigator;
using Ada.Tests.Common;
using AutoMapper;
using Moq;

namespace Ada.Tests.Game.Navigator;

[TestFixture]
public class NavigatorTabProviderTests
{
    private static Mock<IMapper> CreateMapperMock()
    {
        var mapper = new Mock<IMapper>();

        mapper.Setup(x => x.Map<List<NavigatorCategoryDto>>(It.IsAny<object>()))
            .Returns((object source) => ((IEnumerable<NavigatorCategory>) source)
                .Select(x => new NavigatorCategoryDto
                {
                    Name = x.Name,
                    CodeName = x.CodeName,
                    OrderId = x.OrderId,
                    TabId = x.TabId
                })
                .ToList());

        return mapper;
    }

    private static async Task SeedAsync(SqliteTestDbFactory factory)
    {
        await using var context = factory.CreateDbContext();

        context.Set<NavigatorTab>().Add(new NavigatorTab { Id = 1, Name = "official_view" });
        context.Set<NavigatorCategory>().AddRange(
            new NavigatorCategory { Id = 1, Name = "Popular", CodeName = "popular", OrderId = 2, TabId = 1 },
            new NavigatorCategory { Id = 2, Name = "Official", CodeName = "official", OrderId = 1, TabId = 1 });

        await context.SaveChangesAsync();
    }

    [Test]
    public async Task GetCategoriesForTabAsync_ReturnsCategoriesInOrder()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedAsync(factory);

        var provider = new NavigatorTabProvider(factory, CreateMapperMock().Object);

        var categories = await provider.GetCategoriesForTabAsync("official_view");

        Assert.That(categories.Select(x => x.CodeName), Is.EqualTo(new[] { "official", "popular" }));
    }

    [Test]
    public async Task GetCategoriesForTabAsync_SecondCall_ServesFromCache()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedAsync(factory);

        var provider = new NavigatorTabProvider(factory, CreateMapperMock().Object);

        var first = await provider.GetCategoriesForTabAsync("official_view");
        var second = await provider.GetCategoriesForTabAsync("official_view");

        Assert.That(first, Is.SameAs(second));
    }

    [Test]
    public async Task GetCategoriesForTabAsync_UnknownTab_CachesTheMiss()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedAsync(factory);

        var provider = new NavigatorTabProvider(factory, CreateMapperMock().Object);

        var first = await provider.GetCategoriesForTabAsync("does_not_exist");
        var second = await provider.GetCategoriesForTabAsync("does_not_exist");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Empty);
            Assert.That(first, Is.SameAs(second),
                "an unknown tab name must not buy a round trip per message");
        });
    }

    [Test]
    public async Task GetCategoriesForTabAsync_NullTab_DoesNotThrow()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedAsync(factory);

        var provider = new NavigatorTabProvider(factory, CreateMapperMock().Object);

        Assert.That(await provider.GetCategoriesForTabAsync(null), Is.Empty);
    }

    [Test]
    public async Task Invalidate_ForcesAReload()
    {
        using var factory = new SqliteTestDbFactory();
        await SeedAsync(factory);

        var provider = new NavigatorTabProvider(factory, CreateMapperMock().Object);

        var first = await provider.GetCategoriesForTabAsync("official_view");

        provider.Invalidate();

        var second = await provider.GetCategoriesForTabAsync("official_view");

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Select(x => x.CodeName), Is.EqualTo(first.Select(x => x.CodeName)));
        });
    }
}
