namespace Ada.API.DTOs;

public record GroupDto
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int RoomId { get; init; }
    public int CreatedAt { get; init; }
    public IReadOnlyCollection<long>? PlayerIds { get; init; }
}