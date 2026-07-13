using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Purse;

[PacketId(ServerPacketId.PlayerActivityPointsBalance)]
public class PlayerActivityPointsBalanceWriter : AbstractPacketWriter
{
    public required Dictionary<int, long> Currencies { get; init; }
}