namespace Ada.API.DTOs.Server;

public record ServerPeriodicCurrencyRewardLogDto
{
    public int Id { get; set; }
    public long PlayerId { get; set; }
    public string? Type { get; set; }
    public int Amount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}