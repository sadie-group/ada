namespace Ada.Db.Models.Players;

public class PlayerAchievement
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public required string AchievementCode { get; set; }
    public int Level { get; set; }
    public int Progress { get; set; }
}
