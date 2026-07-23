namespace Ada.API.DTOs;

public record SoundTrackDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Author { get; init; }
    public required string Code { get; init; }
    public required string Data { get; init; }
    public int Length { get; init; }
}
