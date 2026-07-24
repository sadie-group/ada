using Ada.Core.Enums.Game.WordFilter;
using Ada.Db;
using Ada.Db.Models.Server;
using Ada.Game.WordFilter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ada.Tests.Game.WordFilter;

public class WordFilterServiceTests
{
    private class TestDbContextFactory(DbContextOptions<AdaDbContext> options)
        : IDbContextFactory<AdaDbContext>
    {
        public AdaDbContext CreateDbContext() => new(options);
    }

    private static WordFilterService CreateService(params WordFilterEntry[] entries)
    {
        var options = new DbContextOptionsBuilder<AdaDbContext>()
            .UseInMemoryDatabase(databaseName: $"word-filter-{Guid.NewGuid()}")
            .Options;

        using (var dbContext = new AdaDbContext(options))
        {
            dbContext.WordFilterEntries.AddRange(entries);
            dbContext.SaveChanges();
        }

        return new WordFilterService(
            new TestDbContextFactory(options),
            NullLogger<WordFilterService>.Instance);
    }

    [Test]
    public void Filter_ContainsMatch_ReplacesWithDefault()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "badword"
        });

        var result = service.Filter("this is a badword here", WordFilterContext.Chat);

        Assert.Multiple(() =>
        {
            Assert.That(result.FilteredText, Is.EqualTo("this is a bobba here"));
            Assert.That(result.WasModified, Is.True);
            Assert.That(result.IsBlocked, Is.False);
        });
    }

    [Test]
    public void Filter_CustomReplacement_IsUsed()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "badword",
            Replacement = "flower"
        });

        var result = service.Filter("badword!", WordFilterContext.Chat);

        Assert.That(result.FilteredText, Is.EqualTo("flower!"));
    }

    [Test]
    public void Filter_LeetspeakAndSeparators_StillMatches()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "badword"
        });

        var result = service.Filter("b4d.w0-rd", WordFilterContext.Chat);

        Assert.That(result.FilteredText, Is.EqualTo("bobba"));
    }

    [Test]
    public void Filter_NormalizeDisabled_DoesNotMatchLeetspeak()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "badword",
            NormalizeText = false
        });

        var result = service.Filter("b4dw0rd", WordFilterContext.Chat);

        Assert.That(result.WasModified, Is.False);
    }

    [Test]
    public void Filter_BlockAction_Blocks()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "badword",
            ActionId = WordFilterAction.Block
        });

        var result = service.Filter("a badword", WordFilterContext.Chat);

        Assert.That(result.IsBlocked, Is.True);
    }

    [Test]
    public void Filter_ShadowBlockAction_ShadowBlocks()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "badword",
            ActionId = WordFilterAction.ShadowBlock
        });

        var result = service.Filter("a badword", WordFilterContext.Chat);

        Assert.That(result.IsShadowBlocked, Is.True);
    }

    [Test]
    public void Filter_ContextMismatch_IsIgnored()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "badword",
            Contexts = WordFilterContext.RoomName
        });

        var result = service.Filter("a badword", WordFilterContext.Chat);

        Assert.That(result.HasMatch, Is.False);
    }

    [Test]
    public void Filter_WholeWordMatch_DoesNotMatchSubstring()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "ass",
            MatchTypeId = WordFilterMatchType.WholeWord
        });

        Assert.Multiple(() =>
        {
            Assert.That(service.Filter("what an ass", WordFilterContext.Chat).WasModified, Is.True);
            Assert.That(service.Filter("assassin classic", WordFilterContext.Chat).WasModified, Is.False);
        });
    }

    [Test]
    public void Filter_RegexMatch_Works()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "https?://\\S+",
            MatchTypeId = WordFilterMatchType.Regex,
            NormalizeText = false,
            Replacement = "[link removed]"
        });

        var result = service.Filter("go to https://example.com now", WordFilterContext.Chat);

        Assert.That(result.FilteredText, Is.EqualTo("go to [link removed] now"));
    }

    [Test]
    public void Filter_InvalidRegex_IsSkipped()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "[invalid",
            MatchTypeId = WordFilterMatchType.Regex
        });

        var result = service.Filter("anything [invalid goes", WordFilterContext.Chat);

        Assert.That(result.WasModified, Is.False);
    }

    [Test]
    public void Filter_DisabledEntry_IsIgnored()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "badword",
            Enabled = false
        });

        var result = service.Filter("a badword", WordFilterContext.Chat);

        Assert.That(result.WasModified, Is.False);
    }

    [Test]
    public void Filter_MultipleMatches_AllReplaced()
    {
        var service = CreateService(new WordFilterEntry
        {
            Pattern = "badword"
        });

        var result = service.Filter("badword and badword", WordFilterContext.Chat);

        Assert.That(result.FilteredText, Is.EqualTo("bobba and bobba"));
    }

    [Test]
    public async Task ReloadAsync_PicksUpNewEntries()
    {
        var options = new DbContextOptionsBuilder<AdaDbContext>()
            .UseInMemoryDatabase(databaseName: $"word-filter-{Guid.NewGuid()}")
            .Options;

        var service = new WordFilterService(
            new TestDbContextFactory(options),
            NullLogger<WordFilterService>.Instance);

        Assert.That(service.Filter("badword", WordFilterContext.Chat).WasModified, Is.False);

        await using (var dbContext = new AdaDbContext(options))
        {
            dbContext.WordFilterEntries.Add(new WordFilterEntry { Pattern = "badword" });
            await dbContext.SaveChangesAsync();
        }

        await service.ReloadAsync();

        Assert.That(service.Filter("badword", WordFilterContext.Chat).WasModified, Is.True);
    }
}
