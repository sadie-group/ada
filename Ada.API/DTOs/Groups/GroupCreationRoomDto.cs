namespace Ada.API.DTOs.Groups;

public record GroupCreationRoomDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
}
