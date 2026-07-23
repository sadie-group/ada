using Ada.Core.Enums.Game.Groups;

namespace Ada.API.DTOs.Groups;

public record GroupMembershipDto
{
    public int GroupId { get; init; }
    public long PlayerId { get; init; }
    public GroupMemberRank Rank { get; init; }
    public bool IsPending { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
