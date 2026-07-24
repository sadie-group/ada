using System.Globalization;
using Ada.API;
using Ada.API.DTOs.Groups;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Groups;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildMembers)]
public class GuildMembersWriter : AbstractPacketWriter
{
    public const int MembersPerPage = 14;

    public required int GuildId { get; init; }
    public required string GuildName { get; init; }
    public required int RoomId { get; init; }
    public required string Badge { get; init; }
    public required long OwnerId { get; init; }
    public required int TotalMemberCount { get; init; }
    public required IReadOnlyList<GroupMemberDto> Members { get; init; }
    public required bool ViewerIsAdmin { get; init; }
    public required int PageId { get; init; }
    public required int LevelId { get; init; }
    public required string SearchValue { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(GuildId);
        writer.WriteString(GuildName);
        writer.WriteInteger(RoomId);
        writer.WriteString(Badge);
        writer.WriteInteger(TotalMemberCount);
        writer.WriteInteger(Members.Count);

        foreach (var member in Members)
        {
            var rankType = RankTypeFor(member);

            writer.WriteInteger(rankType);
            writer.WriteInteger((int) member.PlayerId);
            writer.WriteString(member.Username);
            writer.WriteString(member.FigureCode);
            writer.WriteString(rankType >= 3
                ? ""
                : member.JoinedAt.ToString("d/M/yyyy", CultureInfo.InvariantCulture));
        }

        writer.WriteBool(ViewerIsAdmin);
        writer.WriteInteger(MembersPerPage);
        writer.WriteInteger(PageId);
        writer.WriteInteger(LevelId);
        writer.WriteString(SearchValue);
    }

    private int RankTypeFor(GroupMemberDto member)
    {
        if (member.IsPending)
        {
            return 3;
        }

        if (member.PlayerId == OwnerId)
        {
            return 0;
        }

        return member.Rank == GroupMemberRank.Admin ? 1 : 2;
    }
}
