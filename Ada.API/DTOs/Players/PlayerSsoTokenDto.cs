namespace Ada.API.DTOs.Players;

public record PlayerSsoTokenDto
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public string? Token { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? UsedAt { get; init; }
}