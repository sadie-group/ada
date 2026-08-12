namespace Ada.API.DTOs.Groups;

public record ForumThreadDto
{
    public int ThreadId { get; init; }
    public long OpenerId { get; init; }
    public required string OpenerUsername { get; init; }
    public required string Subject { get; init; }
    public bool IsPinned { get; init; }
    public bool IsLocked { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int TotalComments { get; init; }
    public int UnreadComments { get; init; }
    public long LastAuthorId { get; init; }
    public required string LastAuthorUsername { get; init; }
    public DateTimeOffset? LastCommentAt { get; init; }
    public int State { get; init; }
    public long AdminId { get; init; }
    public required string AdminUsername { get; init; }
}
