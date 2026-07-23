namespace Ada.API.DTOs.Groups;

public record ForumCommentDto
{
    public int CommentId { get; init; }
    public int Index { get; init; }
    public long UserId { get; init; }
    public required string Username { get; init; }
    public required string FigureCode { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public required string Message { get; init; }
    public int State { get; init; }
    public long AdminId { get; init; }
    public required string AdminUsername { get; init; }
    public int AuthorPostCount { get; init; }
}
