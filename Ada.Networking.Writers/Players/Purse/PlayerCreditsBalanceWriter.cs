using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Purse;

[PacketId(ServerPacketId.PlayerCreditsBalance)]
public class PlayerCreditsBalanceWriter : AbstractPacketWriter
{
    public required long Credits { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteString(Credits + ".0");
    }
}