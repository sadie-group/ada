namespace Ada.Db.Models.Players;

public class PlayerFavouriteRoom
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public int RoomId { get; init; }
    public required DateTime CreatedAt { get; init; }
}
