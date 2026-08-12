using Ada.API;
using Ada.API.DTOs.Moderation;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Moderation;

[PacketId(ServerPacketId.ModerationTicketIssue)]
public class ModerationTicketIssueWriter : AbstractPacketWriter
{
    public required ModerationTicketDto Ticket { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        var ageInMs = (int) Math.Clamp(
            (DateTimeOffset.UtcNow - Ticket.CreatedAt).TotalMilliseconds, 0, int.MaxValue);

        writer.WriteInteger(Ticket.Id);
        writer.WriteInteger((int) Ticket.State);
        writer.WriteInteger(Ticket.CategoryId);
        writer.WriteInteger(0);
        writer.WriteInteger(ageInMs);
        writer.WriteInteger(1);
        writer.WriteInteger(0);
        writer.WriteInteger((int) Ticket.ReporterPlayerId);
        writer.WriteString(Ticket.ReporterUsername);
        writer.WriteInteger((int) (Ticket.ReportedPlayerId ?? 0));
        writer.WriteString(Ticket.ReportedUsername);
        writer.WriteInteger((int) (Ticket.PickedByPlayerId ?? 0));
        writer.WriteString(Ticket.PickedByUsername);
        writer.WriteString(Ticket.Message);
        writer.WriteInteger(-1);
        writer.WriteInteger(0);
    }
}
