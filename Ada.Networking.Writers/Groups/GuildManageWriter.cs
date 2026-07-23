using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildManage)]
public class GuildManageWriter : AbstractPacketWriter
{
    private const int BadgeSlots = 5;

    public required int GuildId { get; init; }
    public required int RoomId { get; init; }
    public required string RoomName { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int ColorA { get; init; }
    public required int ColorB { get; init; }
    public required int State { get; init; }
    public required bool AdminOnlyDecoration { get; init; }
    public required string Badge { get; init; }
    public required int MemberCount { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(1);
        writer.WriteInteger(RoomId);
        writer.WriteString(RoomName);
        writer.WriteBool(false);
        writer.WriteBool(true);
        writer.WriteInteger(GuildId);
        writer.WriteString(Name);
        writer.WriteString(Description);
        writer.WriteInteger(RoomId);
        writer.WriteInteger(ColorA);
        writer.WriteInteger(ColorB);
        writer.WriteInteger(State);
        writer.WriteInteger(AdminOnlyDecoration ? 0 : 1);
        writer.WriteBool(false);
        writer.WriteString("");

        writer.WriteInteger(BadgeSlots);

        for (var i = 0; i < BadgeSlots; i++)
        {
            writer.WriteInteger(0);
            writer.WriteInteger(0);
            writer.WriteInteger(0);
        }

        writer.WriteString(Badge);
        writer.WriteInteger(MemberCount);
    }
}
