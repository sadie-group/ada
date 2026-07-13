using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Other;

[PacketId(ServerPacketId.PlayerPerks)]
public class PlayerPerksWriter : AbstractPacketWriter
{
    public required List<IPerkData> Perks { get; init; }
}