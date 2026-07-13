using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms;

[PacketId(ServerPacketId.RoomPromotion)]
public class RoomPromotionWriter : AbstractPacketWriter
{
    public required int AdId { get; set; }
    public required long OwnerId { get; set; }
    public required string OwnerUsername { get; set; }
    public required int FlatId { get; set; }
    public required int Type { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required int Unknown8 { get; set; }
    public required int Unknown9 { get; set; }
    public required int CategoryId { get; set; }
}