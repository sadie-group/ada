using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.Core.Enums.Game.Players;
using Ada.Game.Players.Packets.Writers;
using Ada.Networking.Packets;
using Ada.Networking.Packets.Serialization;
using Ada.Tests.Networking;
using Moq;

namespace Ada.Tests.Game.Players.Packets;

[TestFixture]
public class PlayerFriendsListWriterTests
{
    private static byte[] Payload(object packet)
    {
        var writer = (NetworkPacketWriter)NetworkPacketWriterSerializer.Serialize(packet, TestPacketCodec.Instance);
        return writer.GetAllBytes().Skip(4).ToArray();
    }

    private static PlayerDto MakePlayer(long id, string username, PlayerAvatarDataDto? avatarData) =>
        new(
            id,
            username,
            "test@example.com",
            DateTimeOffset.UtcNow,
            [],
            new PlayerDataDto(),
            avatarData,
            [],
            [],
            [],
            [],
            new PlayerNavigatorSettingsDto(),
            new PlayerGameSettingsDto(),
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);

    [Test]
    public void OnSerialize_OnlineTargetFriendWithRelationship_WritesAllFields()
    {
        var friendPlayer = MakePlayer(20, "amy",
            new PlayerAvatarDataDto { Gender = PlayerAvatarGender.Male, FigureCode = "fig20", Motto = "m20" });

        var state = new Mock<IPlayerState>();
        state.SetupGet(x => x.CurrentRoomId).Returns(5);
        var onlineFriend = new Mock<IPlayerLogic>();
        onlineFriend.SetupGet(x => x.State).Returns(state.Object);

        var repository = new Mock<IPlayerRepository>();
        repository.Setup(x => x.GetPlayerLogicById(20)).Returns(onlineFriend.Object);

        var payload = Payload(new PlayerFriendsListWriter
        {
            Pages = 2,
            Index = 1,
            PlayerId = 10,
            Friends = [new PlayerFriendshipDto { OriginPlayerId = 10, TargetPlayerId = 20, TargetPlayer = friendPlayer }],
            PlayerRepository = repository.Object,
            Relationships = [new PlayerRelationshipDto { TargetPlayerId = 20, TypeId = (int)PlayerRelationshipType.Friend }]
        });

        var reader = new NetworkPacketReader(payload.AsMemory(2));

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt(), Is.EqualTo(2));
            Assert.That(reader.ReadInt(), Is.EqualTo(1));
            Assert.That(reader.ReadInt(), Is.EqualTo(1));
            Assert.That(reader.ReadInt(), Is.EqualTo(20));
            Assert.That(reader.ReadString(), Is.EqualTo("amy"));
            Assert.That(reader.ReadInt(), Is.EqualTo(0));
            Assert.That(reader.ReadBool(), Is.True);
            Assert.That(reader.ReadBool(), Is.True);
            Assert.That(reader.ReadString(), Is.EqualTo("fig20"));
            Assert.That(reader.ReadInt(), Is.EqualTo(0));
            Assert.That(reader.ReadString(), Is.EqualTo("m20"));
            Assert.That(reader.ReadString(), Is.EqualTo("amy"));
            Assert.That(reader.ReadString(), Is.Empty);
            Assert.That(reader.ReadBool(), Is.False);
            Assert.That(reader.ReadBool(), Is.False);
            Assert.That(reader.ReadBool(), Is.False);
            Assert.That(reader.ReadShort(), Is.EqualTo((short)PlayerRelationshipType.Friend));
        });
    }

    [Test]
    public void OnSerialize_OfflineOriginFriendWithoutAvatar_WritesDefaults()
    {
        var friendPlayer = MakePlayer(30, "bea", null);

        var payload = Payload(new PlayerFriendsListWriter
        {
            Pages = 1,
            Index = 0,
            PlayerId = 10,
            Friends = [new PlayerFriendshipDto { OriginPlayerId = 30, TargetPlayerId = 10, OriginPlayer = friendPlayer }],
            PlayerRepository = Mock.Of<IPlayerRepository>(),
            Relationships = []
        });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        reader.ReadInt();
        reader.ReadInt();

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt(), Is.EqualTo(1));
            Assert.That(reader.ReadInt(), Is.EqualTo(30));
            Assert.That(reader.ReadString(), Is.EqualTo("bea"));
            Assert.That(reader.ReadInt(), Is.EqualTo(1));
            Assert.That(reader.ReadBool(), Is.False);
            Assert.That(reader.ReadBool(), Is.False);
            Assert.That(reader.ReadString(), Is.Empty);
            Assert.That(reader.ReadInt(), Is.EqualTo(0));
            Assert.That(reader.ReadString(), Is.Empty);
            Assert.That(reader.ReadString(), Is.EqualTo("bea"));
            Assert.That(reader.ReadString(), Is.Empty);
            Assert.That(reader.ReadBool(), Is.False);
            Assert.That(reader.ReadBool(), Is.False);
            Assert.That(reader.ReadBool(), Is.False);
            Assert.That(reader.ReadShort(), Is.EqualTo((short)PlayerRelationshipType.None));
        });
    }

    [Test]
    public void OnSerialize_FriendWithoutPlayerData_SkipsEntry()
    {
        var payload = Payload(new PlayerFriendsListWriter
        {
            Pages = 1,
            Index = 0,
            PlayerId = 10,
            Friends = [new PlayerFriendshipDto { OriginPlayerId = 10, TargetPlayerId = 20 }],
            PlayerRepository = Mock.Of<IPlayerRepository>(),
            Relationships = []
        });

        var reader = new NetworkPacketReader(payload.AsMemory(2));
        reader.ReadInt();
        reader.ReadInt();

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt(), Is.EqualTo(1));
            Assert.That(payload, Has.Length.EqualTo(2 + 12));
        });
    }
}
