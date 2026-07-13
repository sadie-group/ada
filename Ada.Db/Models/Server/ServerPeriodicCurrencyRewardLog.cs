using Ada.Db.Models.Players;

namespace Ada.Db.Models.Server;

public class ServerPeriodicCurrencyRewardLog
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public Player? Player { get; init; }
    public string? Type { get; init; }
    public int Amount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}