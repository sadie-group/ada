using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.PetScratchFailed)]
public class PetScratchFailedWriter : AbstractPacketWriter
{
    public required int CurrentAge { get; init; }
    public required int RequiredAge { get; init; }
}
