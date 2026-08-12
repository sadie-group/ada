using Ada.API;
using Ada.API.DTOs.Moderation;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Moderation;

[PacketId(ServerPacketId.ModToolsUserRoomVisits)]
public class ModToolUserRoomVisitsWriter : AbstractPacketWriter
{
    public required long UserId { get; init; }
    public required string Username { get; init; }
    public required IReadOnlyList<ModToolRoomVisitDto> Visits { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger((int) UserId);
        writer.WriteString(Username);
        writer.WriteInteger(Visits.Count);

        foreach (var visit in Visits)
        {
            writer.WriteInteger(visit.RoomId);
            writer.WriteString(visit.RoomName);
            writer.WriteInteger(visit.EnteredAt.Hour);
            writer.WriteInteger(visit.EnteredAt.Minute);
        }
    }
}
