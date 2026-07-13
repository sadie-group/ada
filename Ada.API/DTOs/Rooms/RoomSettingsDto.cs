using Ada.Core.Enums.Game.Rooms;

namespace Ada.API.DTOs.Rooms;

public record RoomSettingsDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public bool WalkDiagonal { get; set; }
    public RoomAccessType AccessType { get; set; }
    public string? Password { get; set; }
    public int WhoCanMute { get; set; }
    public int WhoCanKick { get; set; }
    public int WhoCanBan { get; set; }
    public bool AllowPets { get; set; }
    public bool CanPetsEat { get; set; }
    public bool HideWalls { get; set; }
    public int WallThickness { get; set; }
    public int FloorThickness { get; set; }
    public bool CanUsersOverlap { get; set; }
    public RoomTradeOption TradeOption { get; set; }
}