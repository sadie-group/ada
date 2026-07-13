using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Purse;

[PacketId(ServerPacketId.PlayerCreditsBalance)]
public class PlayerCreditsBalanceWriter : AbstractPacketWriter
{
    public required long Credits { get; init; }

    public override void OnConfigureRules()
    {
        Convert<string>(
            GetType().GetProperty(nameof(Credits))!,
            i => (long) i + ".0");
    }
}