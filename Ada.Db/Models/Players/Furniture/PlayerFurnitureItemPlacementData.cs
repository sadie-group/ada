using System.ComponentModel.DataAnnotations;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Rooms;

namespace Ada.Db.Models.Players.Furniture;

public class PlayerFurnitureItemPlacementData
{
    [Key] public int Id { get; init; }
    public int PlayerFurnitureItemId { get; init; }
    public required PlayerFurnitureItem PlayerFurnitureItem { get; init; }
    public int RoomId { get; init; }
    public Room? Room { get; init; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public double PositionZ { get; set; }
    public string? WallPosition { get; set; }
    public HDirection Direction { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public PlayerFurnitureItemWiredData? WiredData { get; set; }
    
    public ICollection<PlayerFurnitureItemWiredData> SelectedBy { get; init; } = [];
}