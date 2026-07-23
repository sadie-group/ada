using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildBought)]
public class GuildBoughtWriter : AbstractPacketWriter
{
    public required int RoomId { get; init; }
    public required int GuildId { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(RoomId);
        writer.WriteInteger(GuildId);
    }
}
