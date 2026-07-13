using System.ComponentModel.DataAnnotations;

namespace Ada.Db.Models.Players;

public class PlayerRoomBan
{
    [Key] public int Id { get; init; }
    public long PlayerId { get; init; }
    public Player? Player { get; init; }
    public int RoomId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}