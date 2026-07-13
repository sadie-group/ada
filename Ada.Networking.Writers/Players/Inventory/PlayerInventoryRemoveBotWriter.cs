using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Inventory;

[PacketId(ServerPacketId.PlayerInventoryRemoveBot)]
public class PlayerInventoryRemoveBotWriter : AbstractPacketWriter
{
    public required int Id { get; init; }
}