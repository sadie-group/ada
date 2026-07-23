namespace Ada.API.DTOs.Groups;

public record GroupListItemDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Badge { get; init; }
    public int ColorA { get; init; }
    public int ColorB { get; init; }
    public bool IsFavourite { get; init; }
    public long OwnerId { get; init; }
    public bool HasForum { get; init; }
}
