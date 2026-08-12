namespace Ada.Db.Models.Rooms;

public class RoomWordFilter
{
    public int Id { get; init; }
    public int RoomId { get; init; }
    public required string Word { get; init; }
    public required DateTime CreatedAt { get; init; }
}
