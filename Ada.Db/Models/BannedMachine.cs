using Ada.Db.Models.Players;

namespace Ada.Db.Models;

public class BannedMachine
{
    public int Id { get; set; }
    public required long CreatorId { get; init; }
    public required Player Creator { get; init; }
    public required string Reason { get; init; }
    public required string MachineId { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
