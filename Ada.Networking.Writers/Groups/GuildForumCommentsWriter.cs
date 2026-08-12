using Ada.API;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildForumComments)]
public class GuildForumCommentsWriter : AbstractPacketWriter
{
    public required int GuildId { get; init; }
    public required int ThreadId { get; init; }
    public required int StartIndex { get; init; }
    public required IReadOnlyList<ForumCommentDto> Comments { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(GuildId);
        writer.WriteInteger(ThreadId);
        writer.WriteInteger(StartIndex);
        writer.WriteInteger(Comments.Count);

        foreach (var comment in Comments)
        {
            ForumSerialize.Comment(writer, comment);
        }
    }
}
