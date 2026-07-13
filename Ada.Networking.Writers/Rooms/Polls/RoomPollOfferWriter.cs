using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Polls;

[PacketId(ServerPacketId.RoomPollOffer)]
public class RoomPollOfferWriter : AbstractPacketWriter
{
    public required int Id { get; init; }
    public required string Type { get; init; }
    public required string Headline { get; init; }
    public required string Summary { get; init; }
}