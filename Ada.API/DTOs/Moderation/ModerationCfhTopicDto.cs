namespace Ada.API.DTOs.Moderation;

public record ModerationCfhTopicDto
{
    public int Id { get; init; }
    public required string CategoryName { get; init; }
    public required string Name { get; init; }
    public int Order { get; init; }
}
