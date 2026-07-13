using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Inventory;

[PacketId(ServerPacketId.PlayerInventoryRemoveItem)]
public class PlayerInventoryRemoveItemWriter : AbstractPacketWriter
{
    public required long ItemId { get; init; }
}