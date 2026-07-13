using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Clothing;

[PacketId(ServerPacketId.PlayerClothingList)]
public class PlayerClothingListWriter : AbstractPacketWriter
{
    public required List<int> SetIds { get; init; }
    public required List<string> FurnitureNames { get; init; }
}