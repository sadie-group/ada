namespace Ada.Db.Models.Players;

public class PlayerEffectItem
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public int EffectId { get; init; }
    public int Duration { get; init; }
    public bool IsActivated { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public required DateTime CreatedAt { get; init; }
}
