using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.Networking.Client;
using AutoMapper;
using Moq;

namespace Ada.Tests.Networking.Client;

[TestFixture]
public class NetworkClientRepositoryTests
{
    private static NetworkClientRepository CreateRepository()
        => new(
            NullLogger<NetworkClientRepository>.Instance,
            Mock.Of<IPlayerRepository>(),
            Mock.Of<IPlayerPresenceStore>(),
            Mock.Of<IPlayerHelperService>(),
            Mock.Of<IMapper>(),
            []);

    private static Mock<INetworkClient> CreateClient(Guid guid)
    {
        var client = new Mock<INetworkClient>();
        client.SetupGet(c => c.Guid).Returns(guid);
        client.SetupGet(c => c.Player).Returns((IPlayerLogic?)null);
        client.SetupGet(c => c.RoomUser).Returns((Ada.API.Interfaces.Game.Rooms.Users.IRoomUser?)null);
        client.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return client;
    }

    [Test]
    public void AddClient_ThenGetByGuid_ReturnsClient()
    {
        var repository = CreateRepository();
        var guid = Guid.NewGuid();
        var client = CreateClient(guid);

        repository.AddClient(guid, client.Object);

        Assert.Multiple(() =>
        {
            Assert.That(repository.TryGetClientByGuid(guid), Is.SameAs(client.Object));
            Assert.That(repository.Clients, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void TryGetClientByGuid_UnknownGuid_ReturnsNull() => Assert.That(CreateRepository().TryGetClientByGuid(Guid.NewGuid()), Is.Null);

    [Test]
    public async Task TryRemoveAsync_KnownClientWithoutPlayer_RemovesAndDisposes()
    {
        var repository = CreateRepository();
        var guid = Guid.NewGuid();
        var client = CreateClient(guid);
        repository.AddClient(guid, client.Object);

        var removed = await repository.TryRemoveAsync(guid);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(repository.Clients, Is.Empty);
        });
        client.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task TryRemoveAsync_UnknownGuid_ReturnsFalse() => Assert.That(await CreateRepository().TryRemoveAsync(Guid.NewGuid()), Is.False);

    [Test]
    public async Task TryRemoveAsync_SameClientTwice_SecondCallReturnsFalse()
    {
        var repository = CreateRepository();
        var guid = Guid.NewGuid();
        repository.AddClient(guid, CreateClient(guid).Object);

        var first = await repository.TryRemoveAsync(guid);
        var second = await repository.TryRemoveAsync(guid);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
        });
    }

    [Test]
    public async Task DisconnectIdleClientsAsync_RecentPong_ClientStaysConnected()
    {
        var repository = CreateRepository();
        var guid = Guid.NewGuid();
        var client = CreateClient(guid);
        client.SetupGet(c => c.LastPong).Returns(DateTime.UtcNow);
        repository.AddClient(guid, client.Object);

        await repository.DisconnectIdleClientsAsync();

        Assert.That(repository.Clients, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DisconnectIdleClientsAsync_StalePong_ClientIsRemoved()
    {
        var repository = CreateRepository();
        var guid = Guid.NewGuid();
        var client = CreateClient(guid);
        client.SetupGet(c => c.LastPong).Returns(DateTime.UtcNow.AddMinutes(-5));
        repository.AddClient(guid, client.Object);

        await repository.DisconnectIdleClientsAsync();

        Assert.That(repository.Clients, Is.Empty);
        client.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task DisposeAsync_RemovesAllClients()
    {
        var repository = CreateRepository();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        repository.AddClient(first, CreateClient(first).Object);
        repository.AddClient(second, CreateClient(second).Object);

        await repository.DisposeAsync();

        Assert.That(repository.Clients, Is.Empty);
    }
}
