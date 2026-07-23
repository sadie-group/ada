using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildFurniWidget)]
public class GuildFurniWidgetWriter : AbstractPacketWriter
{
    public required int ItemId { get; init; }
    public required int GuildId { get; init; }
    public required string GuildName { get; init; }
    public required int RoomId { get; init; }
    public required bool UserJoined { get; init; }
    public required bool HasForum { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(ItemId);
        writer.WriteInteger(GuildId);
        writer.WriteString(GuildName);
        writer.WriteInteger(RoomId);
        writer.WriteBool(UserJoined);
        writer.WriteBool(HasForum);
    }
}
