namespace Ada.API.DTOs.Players;

public record PlayerRoomVisitDto
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public int RoomId { get; init; }
    public DateTime CreatedAt { get; init; }
}