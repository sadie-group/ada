namespace Ada.API.DTOs.Rooms.Rights;

public record RoomPlayerRightDto
{
    public int Id { get; init; }
    public int RoomId { get; init; }
    public long PlayerId { get; init; }
    public required DateTime CreatedAt { get; init; }
}