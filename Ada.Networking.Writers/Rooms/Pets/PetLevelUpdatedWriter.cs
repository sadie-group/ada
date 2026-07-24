using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.PetLevelUpdated)]
public class PetLevelUpdatedWriter : AbstractPacketWriter
{
    public required int RoomUnitId { get; init; }
    public required int PetId { get; init; }
    public required int Level { get; init; }
}
