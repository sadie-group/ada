using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets.Breeding;

[PacketId(ServerPacketId.PetBreedingCompleted)]
public class PetBreedingCompletedWriter : AbstractPacketWriter
{
    public required int PetId { get; init; }
    public required int RarityCategory { get; init; }
}
