using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets.Breeding;

[PacketId(ServerPacketId.PetBreedingStart)]
public class PetBreedingStartWriter : AbstractPacketWriter
{
    public required int State { get; init; }
    public required int PetOneId { get; init; }
    public required int PetTwoId { get; init; }
}
