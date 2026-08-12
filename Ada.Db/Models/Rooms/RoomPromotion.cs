namespace Ada.Db.Models.Rooms;

public class RoomPromotion
{
    public int Id { get; init; }
    public int RoomId { get; init; }
    public long OwnerId { get; init; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public int CategoryId { get; set; }
    public required DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; set; }
}
