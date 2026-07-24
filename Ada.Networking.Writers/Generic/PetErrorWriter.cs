using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Generic;

[PacketId(ServerPacketId.PetError)]
public class PetErrorWriter : AbstractPacketWriter
{
    public const int PetsForbiddenInHotel = 0;
    public const int PetsForbiddenInFlat = 1;
    public const int MaxPets = 2;
    public const int SelectedTileNotFree = 3;
    public const int NoFreeTiles = 4;
    public const int MaxOwnPets = 5;

    public required int ErrorCode { get; init; }
}
