using Ada.API;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildForumThreads)]
public class GuildForumThreadsWriter : AbstractPacketWriter
{
    public required int GuildId { get; init; }
    public required int StartIndex { get; init; }
    public required IReadOnlyList<ForumThreadDto> Threads { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(GuildId);
        writer.WriteInteger(StartIndex);
        writer.WriteInteger(Threads.Count);

        foreach (var thread in Threads)
        {
            ForumSerialize.Thread(writer, thread);
        }
    }
}
