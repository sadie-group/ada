using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets.Breeding;

[PacketId(ServerPacketId.PetBreedingFailed)]
public class PetBreedingFailedWriter : AbstractPacketWriter
{
    public required int NestId { get; init; }
    public required int Reason { get; init; }
}
