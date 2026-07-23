using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildRefreshMembers)]
public class GuildRefreshMembersWriter : AbstractPacketWriter
{
    public required int GuildId { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(GuildId);
        writer.WriteInteger(0);
    }
}
