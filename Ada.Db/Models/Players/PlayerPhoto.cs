namespace Ada.Db.Models.Players;

public class PlayerPhoto
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public int RoomId { get; init; }
    public required string Url { get; init; }
    public bool IsPublished { get; set; }
    public int ReportCount { get; set; }
    public required DateTime CreatedAt { get; init; }
}
