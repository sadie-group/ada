using Ada.API.DTOs.Furniture;

namespace Ada.API.DTOs;

public record HandItemDto
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public ICollection<FurnitureItemDto> FurnitureItems { get; init; } = [];
}