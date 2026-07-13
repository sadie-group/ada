using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Inventory;

[PacketId(ServerPacketId.PlayerInventoryBadges)]
public class PlayerInventoryBadgesWriter : AbstractPacketWriter
{
    public required Dictionary<int, string> Badges { get; init; }
    public required Dictionary<int, string> EquippedBadges { get; init; }
}