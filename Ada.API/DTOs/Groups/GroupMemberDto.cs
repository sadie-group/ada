using Ada.Core.Enums.Game.Groups;

namespace Ada.API.DTOs.Groups;

public record GroupMemberDto
{
    public long PlayerId { get; init; }
    public required string Username { get; init; }
    public required string FigureCode { get; init; }
    public GroupMemberRank Rank { get; init; }
    public bool IsPending { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
}
