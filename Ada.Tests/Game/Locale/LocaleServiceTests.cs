using Ada.Db.Models;
using Ada.Game.Locale;
using Ada.Tests.Common;

namespace Ada.Tests.Game.Locale;

[TestFixture]
public class LocaleServiceTests
{
    [Test]
    public async Task Indexer_KnownKey_ReturnsText()
    {
        var factory = TestDbFactory.CreateDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.ServerLocaleTexts.Add(new ServerLocaleText { Id = 1, Key = "greeting", Text = "hello" });
            db.ServerLocaleTexts.Add(new ServerLocaleText { Id = 2, Key = "farewell", Text = "bye" });
            await db.SaveChangesAsync();
        }

        var service = new LocaleService(factory);

        Assert.Multiple(() =>
        {
            Assert.That(service["greeting"], Is.EqualTo("hello"));
            Assert.That(service["farewell"], Is.EqualTo("bye"));
        });
    }

    [Test]
    public void Indexer_UnknownKey_ReturnsKey()
    {
        var service = new LocaleService(TestDbFactory.CreateDbFactory());

        Assert.That(service["missing.key"], Is.EqualTo("missing.key"));
    }

    [Test]
    public async Task Indexer_Set_OverridesLoadedText()
    {
        var factory = TestDbFactory.CreateDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.ServerLocaleTexts.Add(new ServerLocaleText { Id = 1, Key = "greeting", Text = "hello" });
            await db.SaveChangesAsync();
        }

        var service = new LocaleService(factory);
        service["greeting"] = "howdy";
        service["fresh"] = "new";

        Assert.Multiple(() =>
        {
            Assert.That(service["greeting"], Is.EqualTo("howdy"));
            Assert.That(service["fresh"], Is.EqualTo("new"));
        });
    }
}
