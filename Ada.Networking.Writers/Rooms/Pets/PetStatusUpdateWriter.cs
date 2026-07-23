using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.PetStatusUpdate)]
public class PetStatusUpdateWriter : AbstractPacketWriter
{
    public required int RoomUnitId { get; init; }
    public required int AnyoneCanRide { get; init; }
    public required bool CanBreed { get; init; }
    public required bool NotFullyGrown { get; init; }
    public required bool IsDead { get; init; }
    public required bool PubliclyBreedable { get; init; }
}
