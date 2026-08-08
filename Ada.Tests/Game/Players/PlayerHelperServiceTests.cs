using Ada.API;
using Ada.API.DTOs;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.DTOs.Server;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Packets;
using Ada.API.Interfaces.Game.Players.Friendships;
using Ada.Core.Enums.Game.Players;
using Ada.Game.Players;
using Ada.Networking.Packets;
using Ada.Game.Players.Packets.Writers;
using Ada.Networking.Writers.Players;
using Ada.Networking.Writers.Players.Friendships;
using Ada.Networking.Writers.Players.Inventory;
using Moq;

namespace Ada.Tests.Game.Players;

[TestFixture]
public class PlayerHelperServiceTests
{
    private PlayerHelperService _service;
    private Mock<IPlayerLogic> _player;
    private Mock<IPlayerRepository> _repo;
    private Mock<INetworkObject> _net;

    [SetUp]
    public void Setup()
    {
        _service = new PlayerHelperService();
        _player = new Mock<IPlayerLogic>();
        _repo = new Mock<IPlayerRepository>();
        _net = new Mock<INetworkObject>();
        _net.SetupGet(x => x.Codec).Returns(TestCodec.Instance);

        _player.Setup(x => x.NetworkObject).Returns(_net.Object);

        var state = new Mock<IPlayerState>();
        state.Setup(x => x.UnseenItems).Returns(new PlayerUnseenItems());
        _player.Setup(x => x.State).Returns(state.Object);
    }

    private sealed class TestCodec : IPacketCodec
    {
        public static readonly TestCodec Instance = new();

        private sealed class AlwaysMappedIdMap : IPacketIdMap
        {
            public bool TryGetHandlerType(short packetId, out Type? handlerType)
            {
                handlerType = null;
                return false;
            }

            public bool TryGetOutgoingId(Type writerType, out short packetId)
            {
                packetId = 1;
                return true;
            }
        }

        public string Revision => "PRODUCTION";
        public IPacketIdMap IdMap { get; } = new AlwaysMappedIdMap();
        public INetworkPacketDecoder Decoder { get; } = new NetworkPacketDecoder();
        public INetworkPacketWriter CreateWriter() => new NetworkPacketWriter();
        public INetworkPacketReader CreateReader(ReadOnlyMemory<byte> body) => new NetworkPacketReader(body);
    }

    private static PlayerDto MakePlayerDto(long id)
    {
        return new PlayerDto(
            id,
            "u",
            "",
            DateTimeOffset.Now,
            [],
            null,
            new PlayerAvatarDataDto { FigureCode = "f", Motto = "m", Gender = PlayerAvatarGender.Male },
            [],
            new List<PlayerRoomLikeDto>(),
            new List<PlayerRelationshipDto>(),
            new List<PlayerRelationshipDto>(),
            null,
            null,
            new List<PlayerBadgeDto>(),
            new List<PlayerFurnitureItemDto>(),
            new List<PlayerWardrobeItemDto>(),
            new List<PlayerSubscriptionDto>(),
            new List<PlayerRespectDto>(),
            new List<PlayerSavedSearchDto>(),
            new List<PlayerFriendshipDto>(),
            new List<PlayerFriendshipDto>(),
            new List<PlayerIgnoreDto>(),
            new List<PlayerIgnoreDto>(),
            new List<ServerPeriodicCurrencyRewardLogDto>(),
            new List<RoomDto>(),
            new List<GroupDto>(),
            new List<PlayerBotDto>(),
            new List<PlayerRoomVisitDto>(),
            new List<PlayerBanDto>(),
            new List<PlayerSsoTokenDto>()
        );
    }

    [Test]
    public async Task SendFriendUpdatesToPlayerAsync_Writes()
    {
        var updates = new List<IPlayerFriendshipUpdate>();
        await _service.SendFriendUpdatesToPlayerAsync(_player.Object, updates);
        _net.Verify(x => x.WriteToStreamAsync(It.IsAny<PlayerUpdateFriendWriter>()), Times.Once);
    }

    [Test]
    public async Task SendPlayerFriendListUpdate_WritesPages()
    {
        var list = new List<PlayerFriendshipDto>();
        for (var i = 0; i < 600; i++)
            list.Add(new PlayerFriendshipDto { Status = PlayerFriendshipStatus.Accepted });

        _player.Setup(x => x.GetMergedFriendships()).Returns(list);
        _player.Setup(x => x.Player).Returns(MakePlayerDto(1));

        await _service.SendPlayerFriendListUpdate(_player.Object, _repo.Object);

        _net.Verify(x => x.WriteToStreamAsync(It.Is<PlayerFriendsListWriter>(w => w.Index == 0)), Times.Once);
        _net.Verify(x => x.WriteToStreamAsync(It.Is<PlayerFriendsListWriter>(w => w.Index == 1)), Times.Once);
    }

    [Test]
    public void GetSubscriptionWriterAsync_ReturnsWriter()
    {
        var now = DateTimeOffset.Now;
        var subs = new List<PlayerSubscriptionDto>
        {
            new PlayerSubscriptionDto
            {
                Subscription = new SubscriptionDto { Name = "VIP" },
                CreatedAt = now.AddDays(-1),
                ExpiresAt = now.AddDays(1)
            }
        };

        var dto = MakePlayerDto(1) with { Subscriptions = subs };
        _player.Setup(x => x.Player).Returns(dto);
        _player.Setup(x => x.State).Returns(new Mock<IPlayerState>().Object);

        var result = _service.GetSubscriptionWriterAsync(_player.Object, "VIP");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("vip"));
    }

    [Test]
    public void GetSubscriptionWriterAsync_ReturnsNull_WhenNotFound()
    {
        var dto = MakePlayerDto(1);
        _player.Setup(x => x.Player).Returns(dto);
        var result = _service.GetSubscriptionWriterAsync(_player.Object, "no");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdatePlayerStatusForFriendsAsync_SendsToEachFriend()
    {
        var dto = MakePlayerDto(1);
        _player.Setup(x => x.Player).Returns(dto);

        var friends = new List<PlayerFriendshipDto>
        {
            new PlayerFriendshipDto { OriginPlayerId = 1, TargetPlayerId = 2 }
        };

        var friendLogic = new Mock<IPlayerLogic>();
        friendLogic.Setup(x => x.NetworkObject).Returns(_net.Object);
        _repo.Setup(x => x.GetPlayerLogicById(2)).Returns(friendLogic.Object);

        await _service.UpdatePlayerStatusForFriendsAsync(_player.Object, friends, true, false, _repo.Object);

        _net.Verify(x => x.QueueOutbound(It.IsAny<INetworkPacketWriter>()), Times.Once);
        _net.Verify(x => x.FlushAsync(), Times.Once);
    }

    [Test]
    public async Task SendUnseenInventoryItemsAsync_Writes()
    {
        var dto = MakePlayerDto(1);
        _player.Setup(x => x.Player).Returns(dto);

        var items = new List<PlayerFurnitureItemDto>();
        await _service.SendUnseenInventoryItemsAsync(_player.Object, items);

        _net.Verify(x => x.WriteToStreamAsync(It.IsAny<PlayerInventoryUnseenItemsWriter>()), Times.Once);
    }

    [Test]
    public async Task RefreshInventoryAsync_Writes()
    {
        var dto = MakePlayerDto(1);
        _player.Setup(x => x.Player).Returns(dto);

        await _service.RefreshInventoryAsync(_player.Object);

        _net.Verify(x => x.WriteToStreamAsync(It.IsAny<PlayerInventoryRefreshWriter>()), Times.Once);
    }
}
