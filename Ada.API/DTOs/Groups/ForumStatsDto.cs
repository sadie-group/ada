namespace Ada.API.DTOs.Groups;

public record ForumStatsDto
{
    public int GuildId { get; init; }
    public required string GuildName { get; init; }
    public required string GuildDescription { get; init; }
    public required string Badge { get; init; }
    public int TotalThreads { get; init; }
    public int TotalComments { get; init; }
    public int UnreadComments { get; init; }
    public int LastCommentThreadId { get; init; }
    public long LastCommentUserId { get; init; }
    public required string LastCommentUsername { get; init; }
    public DateTimeOffset? LastCommentAt { get; init; }
}
