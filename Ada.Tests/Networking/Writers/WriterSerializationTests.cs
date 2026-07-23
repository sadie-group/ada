using Ada.Networking.Packets;
using Ada.Networking.Packets.Serialization;
using Ada.Networking.Writers.Players.Friendships;
using Ada.Networking.Writers.Players.Purse;
using Ada.Networking.Writers.Rooms;
using Ada.Networking.Writers.Rooms.Users.Chat;

namespace Ada.Tests.Networking.Writers;

[TestFixture]
public class WriterSerializationTests
{
    private static byte[] Payload(object packet)
    {
        var writer = (NetworkPacketWriter)NetworkPacketWriterSerializer.Serialize(packet);
        return writer.GetAllBytes().Skip(4).ToArray();
    }

    [Test]
    public void RoomUserTypingWriter_ConvertsIsTypingToInt()
    {
        var payload = Payload(new RoomUserTypingWriter { UserId = 5, IsTyping = true });

        var reader = new NetworkPacketReader(payload.AsSpan(2));
        var userId = reader.ReadInt();
        var isTyping = reader.ReadInt();

        Assert.Multiple(() =>
        {
            Assert.That(userId, Is.EqualTo(5));
            Assert.That(isTyping, Is.EqualTo(1));
        });
    }

    [Test]
    public void PlayerCreditsBalanceWriter_ConvertsCreditsToDecimalString()
    {
        var payload = Payload(new PlayerCreditsBalanceWriter { Credits = 250 });

        var reader = new NetworkPacketReader(payload.AsSpan(2));
        Assert.That(reader.ReadString(), Is.EqualTo("250.0"));
    }

    [Test]
    public void RoomEnterErrorWriter_AppendsEmptyStringAfterErrorCode()
    {
        var payload = Payload(new RoomEnterErrorWriter { ErrorCode = 4 });

        var reader = new NetworkPacketReader(payload.AsSpan(2));
        var errorCode = reader.ReadInt();
        var suffix = reader.ReadString();

        Assert.Multiple(() =>
        {
            Assert.That(errorCode, Is.EqualTo(4));
            Assert.That(suffix, Is.Empty);
            Assert.That(payload, Has.Length.EqualTo(2 + 4 + 2));
        });
    }

    [Test]
    public void PlayerRemoveFriendsWriter_WritesMinusOnePairPerPlayerId()
    {
        var payload = Payload(new PlayerRemoveFriendsWriter { Unknown1 = 0, PlayerIds = [10, 20] });

        var reader = new NetworkPacketReader(payload.AsSpan(2));
        var unknown = reader.ReadInt();
        var count = reader.ReadInt();
        var values = new[] { reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt() };

        Assert.Multiple(() =>
        {
            Assert.That(unknown, Is.EqualTo(0));
            Assert.That(count, Is.EqualTo(2));
            Assert.That(values, Is.EqualTo(new[] { -1, 10, -1, 20 }));
        });
    }

    [Test]
    public void RoomChatSettingsWriter_WritesAllFiveSettings()
    {
        var payload = Payload(new RoomChatSettingsWriter
        {
            ChatType = 1,
            ChatWeight = 2,
            ChatSpeed = 3,
            ChatDistance = 4,
            ChatProtection = 5,
        });

        var reader = new NetworkPacketReader(payload.AsSpan(2));
        var values = new[] { reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt() };

        Assert.That(values, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }
}
