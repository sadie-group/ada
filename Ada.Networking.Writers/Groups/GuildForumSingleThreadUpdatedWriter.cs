using Ada.API;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildForumSingleThreadUpdated)]
public class GuildForumSingleThreadUpdatedWriter : AbstractPacketWriter
{
    public required int GuildId { get; init; }
    public required ForumThreadDto Thread { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(GuildId);
        ForumSerialize.Thread(writer, Thread);
    }
}
