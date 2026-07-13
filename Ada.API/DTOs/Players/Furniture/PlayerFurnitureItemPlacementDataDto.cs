using Ada.Core.Enums.Miscellaneous;

namespace Ada.API.DTOs.Players.Furniture;

public class PlayerFurnitureItemPlacementDataDto
{
    public int Id { get; init; }
    public int PlayerFurnitureItemId { get; init; }
    public required PlayerFurnitureItemDto PlayerFurnitureItem { get; init; }
    public int RoomId { get; init; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public double PositionZ { get; set; }
    public string? WallPosition { get; set; }
    public HDirection Direction { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public PlayerFurnitureItemWiredDataDto? WiredData { get; set; }
}