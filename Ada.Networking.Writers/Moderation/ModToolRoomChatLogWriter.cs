using Ada.API.DTOs.Rooms.Chat;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Moderation;

[PacketId(ServerPacketId.ModToolsRoomChatLog)]
public class ModToolRoomChatLogWriter : AbstractPacketWriter
{
    public required byte Unknown1 { get; init; }
    public required short Unknown2 { get; set; }
    public required string Unknown3 { get; set; }
    public required byte Unknown4 { get; init; }
    public required string Unknown5 { get; set; }
    public required string Unknown6 { get; set; }
    public required byte Unknown7 { get; init; }
    public required int Unknown8 { get; set; }
    public required List<RoomChatMessageDto> Messages { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteByte(Unknown1);
        writer.WriteShort(Unknown2);
        writer.WriteString(Unknown3 ?? "");
        writer.WriteByte(Unknown4);
        writer.WriteString(Unknown5 ?? "");
        writer.WriteString(Unknown6 ?? "");
        writer.WriteByte(Unknown7);
        writer.WriteInteger(Unknown8);
        writer.WriteInteger(Messages.Count);

        foreach (var message in Messages)
        {
            writer.WriteString(message.CreatedAt.ToString("HH:mm"));
            writer.WriteLong(message.PlayerId);
            writer.WriteString(message.Player?.Username ?? string.Empty);
            writer.WriteString(message.Message ?? "Unable to display message");
            writer.WriteBool(false);
        }
    }
}
