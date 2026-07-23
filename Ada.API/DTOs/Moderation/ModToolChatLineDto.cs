namespace Ada.API.DTOs.Moderation;

public record ModToolChatLineDto
{
    public DateTimeOffset CreatedAt { get; init; }
    public long PlayerId { get; init; }
    public required string Username { get; init; }
    public required string Message { get; init; }
}
