namespace Ada.API.DTOs.Rooms;

public record RoomTagDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
}