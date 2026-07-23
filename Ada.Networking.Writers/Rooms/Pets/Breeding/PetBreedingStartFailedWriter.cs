using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets.Breeding;

[PacketId(ServerPacketId.PetBreedingStartFailed)]
public class PetBreedingStartFailedWriter : AbstractPacketWriter
{
    public required int Reason { get; init; }
}
