using Ada.API;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildForumList)]
public class GuildForumListWriter : AbstractPacketWriter
{
    public required int Mode { get; init; }
    public required int Total { get; init; }
    public required int StartIndex { get; init; }
    public required IReadOnlyList<ForumStatsDto> Forums { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Mode);
        writer.WriteInteger(Total);
        writer.WriteInteger(StartIndex);
        writer.WriteInteger(Forums.Count);

        foreach (var forum in Forums)
        {
            ForumSerialize.Stats(writer, forum);
        }
    }
}
