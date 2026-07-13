using Ada.API.DTOs.Furniture;

namespace Ada.API.DTOs.Players.Furniture;

public class PlayerFurnitureItemDto
{
    public int Id { get; init; }
    public long PlayerId { get; set; }
    public required FurnitureItemDto FurnitureItem { get; init; }
    public required int FurnitureItemId { get; init; }
    public PlayerFurnitureItemPlacementDataDto? PlacementData { get; set; }
    public required string LimitedData { get; init; }
    public required string MetaData { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}