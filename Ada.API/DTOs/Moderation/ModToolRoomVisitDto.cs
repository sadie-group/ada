namespace Ada.API.DTOs.Moderation;

public record ModToolRoomVisitDto
{
    public int RoomId { get; init; }
    public required string RoomName { get; init; }
    public DateTimeOffset EnteredAt { get; init; }
}
