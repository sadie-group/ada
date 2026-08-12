using Ada.API;
using Ada.API.DTOs.Moderation;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Moderation;

[PacketId(ServerPacketId.ModToolsUserChatLog)]
public class ModToolUserChatLogWriter : AbstractPacketWriter
{
    public required long UserId { get; init; }
    public required string Username { get; init; }
    public required IReadOnlyList<ModToolChatRoomDto> Rooms { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger((int) UserId);
        writer.WriteString(Username);
        writer.WriteInteger(Rooms.Count);

        foreach (var room in Rooms)
        {
            writer.WriteByte(1);
            writer.WriteShort(2);
            writer.WriteString("roomName");
            writer.WriteByte(2);
            writer.WriteString(room.RoomName);
            writer.WriteString("roomId");
            writer.WriteByte(1);
            writer.WriteInteger(room.RoomId);

            writer.WriteInteger(room.Lines.Count);

            foreach (var line in room.Lines)
            {
                writer.WriteString(line.CreatedAt.ToString("HH:mm"));
                writer.WriteLong(line.PlayerId);
                writer.WriteString(line.Username);
                writer.WriteString(line.Message);
                writer.WriteBool(false);
            }
        }
    }
}
