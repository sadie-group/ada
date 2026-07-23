using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Inventory;

[PacketId(ServerPacketId.PlayerInventoryRemovePet)]
public class PlayerInventoryRemovePetWriter : AbstractPacketWriter
{
    public required int Id { get; init; }
}
