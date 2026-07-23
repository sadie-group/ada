using Ada.API;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildForumData)]
public class GuildForumDataWriter : AbstractPacketWriter
{
    public required ForumStatsDto Stats { get; init; }
    public required int ReadPermission { get; init; }
    public required int PostMessagesPermission { get; init; }
    public required int PostThreadsPermission { get; init; }
    public required int ModPermission { get; init; }
    public required string ErrorRead { get; init; }
    public required string ErrorPost { get; init; }
    public required string ErrorStartThread { get; init; }
    public required string ErrorModerate { get; init; }
    public required bool CanChangeSettings { get; init; }
    public required bool CanModerate { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        ForumSerialize.Stats(writer, Stats);

        writer.WriteInteger(ReadPermission);
        writer.WriteInteger(PostMessagesPermission);
        writer.WriteInteger(PostThreadsPermission);
        writer.WriteInteger(ModPermission);
        writer.WriteString(ErrorRead);
        writer.WriteString(ErrorPost);
        writer.WriteString(ErrorStartThread);
        writer.WriteString(ErrorModerate);
        writer.WriteString("");
        writer.WriteBool(CanChangeSettings);
        writer.WriteBool(CanModerate);
    }
}
