using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users.Chat;

[PacketId(ServerPacketId.RoomUserTyping)]
public class RoomUserTypingWriter : AbstractPacketWriter
{
    public required long UserId { get; init; }
    public required bool IsTyping { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteLong(UserId);
        writer.WriteInteger(IsTyping ? 1 : 0);
    }
}