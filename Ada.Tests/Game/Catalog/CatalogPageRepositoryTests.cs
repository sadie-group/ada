using Ada.API.DTOs.Catalog.Pages;
using Ada.Db.Models.Catalog.Pages;
using Ada.Game.Catalog;
using Ada.Tests.Common;
using AutoMapper;
using Moq;

namespace Ada.Tests.Game.Catalog;

[TestFixture]
public class CatalogPageRepositoryTests
{
    [Test]
    public void Pages_BeforeLoad_IsEmpty()
    {
        var repository = new CatalogPagePageRepository(TestDbFactory.CreateDbFactory(), Mock.Of<IMapper>());

        Assert.That(repository.Pages, Is.Empty);
    }

    [Test]
    public async Task LoadAsync_MapsAllPagesAndExposesResult()
    {
        var factory = TestDbFactory.CreateDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.CatalogPages.Add(new CatalogPage { Id = 1, Name = "root", Enabled = true, Visible = true });
            db.CatalogPages.Add(new CatalogPage { Id = 2, Name = "child", CatalogPageId = 1 });
            await db.SaveChangesAsync();
        }

        List<CatalogPage>? captured = null;
        var mapped = new List<CatalogPageDto> { new() { Id = 1, Name = "root" } };
        var mapper = new Mock<IMapper>();
        mapper
            .Setup(m => m.Map<IReadOnlyList<CatalogPageDto>>(It.IsAny<object>()))
            .Callback<object>(source => captured = (List<CatalogPage>)source)
            .Returns(mapped);

        var repository = new CatalogPagePageRepository(factory, mapper.Object);
        await repository.LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(repository.Pages, Is.SameAs(mapped));
            Assert.That(captured, Has.Count.EqualTo(2));
            Assert.That(captured!.Single(p => p.Id == 1).Pages.Single().Id, Is.EqualTo(2));
        });
    }
}
