using Ada.API;
using Ada.API.DTOs.Moderation;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Moderation;

[PacketId(ServerPacketId.ModerationTicketChatLog)]
public class ModerationTicketChatLogWriter : AbstractPacketWriter
{
    public required int TicketId { get; init; }
    public required long ReporterPlayerId { get; init; }
    public required long ReportedPlayerId { get; init; }
    public required int RoomId { get; init; }
    public required string RoomName { get; init; }
    public required IReadOnlyList<ModToolChatLineDto> Lines { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(TicketId);
        writer.WriteInteger((int) ReporterPlayerId);
        writer.WriteInteger((int) ReportedPlayerId);
        writer.WriteInteger(1);
        writer.WriteString("roomName");
        writer.WriteInteger(RoomId);
        writer.WriteString(RoomName);
        writer.WriteInteger(Lines.Count);

        foreach (var line in Lines)
        {
            writer.WriteString(line.CreatedAt.ToString("HH:mm"));
            writer.WriteInteger((int) line.PlayerId);
            writer.WriteString(line.Username);
            writer.WriteString(line.Message);
            writer.WriteBool(false);
        }
    }
}
