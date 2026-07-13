namespace Ada.API.DTOs.Players;

public record PlayerTagDto
{
    public int Id { get; init; }
    public string? Name { get; init; }
}