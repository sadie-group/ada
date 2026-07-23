using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.PetExperience)]
public class RoomPetExperienceWriter : AbstractPacketWriter
{
    public required int PetId { get; init; }
    public required int RoomUnitId { get; init; }
    public required int Amount { get; init; }
}
