using Ada.Core.Enums.Game.Groups;
using Ada.Db.Models.Players;

namespace Ada.Db.Models.Groups;

public class GroupMembership
{
    public int Id { get; init; }
    public int GroupId { get; init; }
    public Group? Group { get; init; }
    public long PlayerId { get; init; }
    public Player? Player { get; init; }
    public GroupMemberRank Rank { get; set; }
    public bool IsPending { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
