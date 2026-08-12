using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.Networking.Events.Handlers.Players;
using Ada.Tests.Common;
using Moq;

namespace Ada.Tests.Networking.Events.Handlers.Players;

[TestFixture]
public class PlayerIgnoreEventHandlerTests
{
    private const long _playerId = 1;
    private const long _targetId = 2;
    private const string _targetName = "target";

    private SqliteTestDbFactory _dbFactory = null!;
    private Mock<IPlayerRepository> _players = null!;
    private ICollection<PlayerIgnoreDto> _outgoingIgnores = null!;
    private INetworkClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _dbFactory = new SqliteTestDbFactory();
        _outgoingIgnores = new List<PlayerIgnoreDto>();

        using (var db = _dbFactory.CreateDbContext())
        {
            db.Players.Add(new global::Ada.Db.Models.Players.Player
            {
                Id = _playerId, Username = "player", Email = "", Password = ""
            });

            db.Players.Add(new global::Ada.Db.Models.Players.Player
            {
                Id = _targetId, Username = _targetName, Email = "", Password = ""
            });

            db.SaveChanges();
        }

        var player = new Mock<IPlayerLogic>();
        player.SetupGet(x => x.Player).Returns(MakePlayerDto(_playerId, "player", _outgoingIgnores));
        player.SetupGet(x => x.NetworkObject).Returns(Mock.Of<INetworkObject>());

        var client = new Mock<INetworkClient>();
        client.SetupGet(x => x.Player).Returns(player.Object);
        _client = client.Object;

        _players = new Mock<IPlayerRepository>();

        _players.Setup(x => x.GetPlayerLogicByUsername(_targetName)).Returns((IPlayerLogic?)null);
        _players.Setup(x => x.GetPlayerByUsernameAsync(_targetName))
            .ReturnsAsync(MakePlayerDto(_targetId, _targetName, new List<PlayerIgnoreDto>()));
    }

    [TearDown]
    public void TearDown() => _dbFactory.Dispose();

    private async Task<int> StoredIgnoreCountAsync()
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.Set<global::Ada.Db.Models.Players.PlayerIgnore>().CountAsync();
    }

    [Test]
    public async Task Ignore_TargetOffline_StillRecordsTheIgnore()
    {
        var handler = new PlayerIgnoreUserEventHandler(_players.Object, _dbFactory) { Username = _targetName };

        await handler.HandleAsync(_client);

        Assert.Multiple(async () =>
        {
            Assert.That(_outgoingIgnores, Has.Exactly(1).Items);
            Assert.That(await StoredIgnoreCountAsync(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Unignore_TargetOffline_StillLiftsTheIgnore()
    {
        _outgoingIgnores.Add(new PlayerIgnoreDto { PlayerId = _playerId, TargetPlayerId = _targetId });

        var handler = new PlayerRemoveUserIgnoreEventHandler(_players.Object, _dbFactory)
        {
            Username = _targetName
        };

        await handler.HandleAsync(_client);

        Assert.That(_outgoingIgnores, Is.Empty,
            "an ignore must be liftable whether or not the other player is currently connected");
    }

    [Test]
    public async Task Ignore_Self_IsRefused()
    {
        _players.Setup(x => x.GetPlayerByUsernameAsync("player"))
            .ReturnsAsync(MakePlayerDto(_playerId, "player", new List<PlayerIgnoreDto>()));

        var handler = new PlayerIgnoreUserEventHandler(_players.Object, _dbFactory) { Username = "player" };

        await handler.HandleAsync(_client);

        Assert.That(_outgoingIgnores, Is.Empty);
    }

    [Test]
    public async Task Ignore_AlreadyIgnored_DoesNotDuplicate()
    {
        _outgoingIgnores.Add(new PlayerIgnoreDto { PlayerId = _playerId, TargetPlayerId = _targetId });

        var handler = new PlayerIgnoreUserEventHandler(_players.Object, _dbFactory) { Username = _targetName };

        await handler.HandleAsync(_client);

        Assert.Multiple(async () =>
        {
            Assert.That(_outgoingIgnores, Has.Exactly(1).Items);
            Assert.That(await StoredIgnoreCountAsync(), Is.Zero);
        });
    }

    private static PlayerDto MakePlayerDto(long id, string username, ICollection<PlayerIgnoreDto> outgoingIgnores) =>
        new(id, username, "", DateTimeOffset.UtcNow, [], new PlayerDataDto(), new PlayerAvatarDataDto(),
            [], [], [], [], new PlayerNavigatorSettingsDto(), new PlayerGameSettingsDto(), [], [], [], [], [], [],
            [], [], outgoingIgnores, [], [], [], [], [], [], [], []);
}
