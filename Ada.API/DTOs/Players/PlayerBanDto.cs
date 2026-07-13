namespace Ada.API.DTOs.Players;

public class PlayerBanDto
{
    public int Id { get; init; }
    public required long CreatorId { get; init; }
    public required PlayerDto Creator { get; init; }
    public required long PlayerId { get; init; }
    public required PlayerDto Player { get; init; }
    public required string Reason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}