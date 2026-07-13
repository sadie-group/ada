namespace Ada.API.DTOs.Players;

public class PlayerRoomBanDto
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public PlayerDto? Player { get; init; }
    public int RoomId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}