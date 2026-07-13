namespace Ada.API.DTOs.Players.Furniture;

public record PlayerFurnitureItemWiredDataDto
{
    public int Id { get; init; }
    public required int PlayerFurnitureItemPlacementDataId { get; init; }
    public required PlayerFurnitureItemPlacementDataDto PlacementData { get; init; }
    public ICollection<PlayerFurnitureItemPlacementDataDto> SelectedItems { get; init; } = [];
    public required string Message { get; init; }
    public int Delay { get; init; }
}