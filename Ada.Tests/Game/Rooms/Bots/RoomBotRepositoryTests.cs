using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.Game.Rooms.Bots;
using Moq;

namespace Ada.Tests.Game.Rooms.Bots;

[TestFixture]
public class RoomBotRepositoryTests
{
    private static Mock<IRoomBot> CreateBot(int id)
    {
        var bot = new Mock<IRoomBot>();
        bot.Setup(x => x.Bot).Returns(new PlayerBotDto { Id = id });
        bot.Setup(x => x.RunPeriodicCheckAsync()).Returns(Task.CompletedTask);
        return bot;
    }

    [Test]
    public void TryAdd_NewBot_Adds()
    {
        var repository = new RoomBotRepository();
        var bot = CreateBot(1);
        Assert.Multiple(() =>
        {
            Assert.That(repository.TryAdd(bot.Object), Is.True);
            Assert.That(repository.Count, Is.EqualTo(1));
        });
        Assert.That(repository.GetAll().Single(), Is.SameAs(bot.Object));
    }

    [Test]
    public void TryAdd_DuplicateId_Rejects()
    {
        var repository = new RoomBotRepository();
        repository.TryAdd(CreateBot(1).Object);
        Assert.Multiple(() =>
        {
            Assert.That(repository.TryAdd(CreateBot(1).Object), Is.False);
            Assert.That(repository.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void TryGetById_Present_ReturnsBot()
    {
        var repository = new RoomBotRepository();
        var bot = CreateBot(7);
        repository.TryAdd(bot.Object);
        Assert.Multiple(() =>
        {
            Assert.That(repository.TryGetById(7, out var found), Is.True);
            Assert.That(found, Is.SameAs(bot.Object));
        });
    }

    [Test]
    public void TryGetById_Absent_ReturnsFalse()
    {
        var repository = new RoomBotRepository();
        Assert.Multiple(() =>
        {
            Assert.That(repository.TryGetById(404, out var found), Is.False);
            Assert.That(found, Is.Null);
        });
    }

    [Test]
    public async Task RunPeriodicCheckAsync_ChecksEveryBot()
    {
        var repository = new RoomBotRepository();
        var first = CreateBot(1);
        var second = CreateBot(2);
        repository.TryAdd(first.Object);
        repository.TryAdd(second.Object);

        await repository.RunPeriodicCheckAsync();

        first.Verify(x => x.RunPeriodicCheckAsync(), Times.Once);
        second.Verify(x => x.RunPeriodicCheckAsync(), Times.Once);
    }

    [Test]
    public void RunPeriodicCheckAsync_BotThrows_Swallows()
    {
        var repository = new RoomBotRepository();
        var bot = CreateBot(1);
        bot.Setup(x => x.RunPeriodicCheckAsync()).ThrowsAsync(new InvalidOperationException("boom"));
        repository.TryAdd(bot.Object);

        Assert.DoesNotThrowAsync(() => repository.RunPeriodicCheckAsync());
    }

    [Test]
    public void DisposeAsync_Completes()
    {
        var repository = new RoomBotRepository();

        Assert.DoesNotThrowAsync(async () => await repository.DisposeAsync());
    }
}
