using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Inventory;

[PacketId(ServerPacketId.PlayerInventoryRefresh)]
public class PlayerInventoryRefreshWriter : AbstractPacketWriter;