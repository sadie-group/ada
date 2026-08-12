namespace Ada.API.DTOs.Moderation;

public record ModToolUserInfoDto
{
    public long UserId { get; init; }
    public required string Username { get; init; }
    public required string Look { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public required string Email { get; init; }
    public int RankId { get; init; }
    public required string RankName { get; init; }
    public int BanCount { get; init; }
}
