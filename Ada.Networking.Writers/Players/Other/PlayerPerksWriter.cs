using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Other;

[PacketId(ServerPacketId.PlayerPerks)]
public class PlayerPerksWriter : AbstractPacketWriter
{
    public required List<IPerkData> Perks { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Perks.Count);

        foreach (var perk in Perks)
        {
            writer.WriteString(perk.Code ?? "");
            writer.WriteString(perk.ErrorMessage ?? "");
            writer.WriteBool(perk.Allowed);
        }
    }
}
