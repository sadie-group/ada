using Ada.API;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildForumMessagePosted)]
public class GuildForumMessagePostedWriter : AbstractPacketWriter
{
    public required int GuildId { get; init; }
    public required int ThreadId { get; init; }
    public required ForumCommentDto Comment { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(GuildId);
        writer.WriteInteger(ThreadId);
        ForumSerialize.Comment(writer, Comment);
    }
}
