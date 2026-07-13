namespace Ada.API.DTOs.Rooms;

public record RoomPaintSettingsDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string FloorPaint { get; set; } = string.Empty;
    public string WallPaint { get; set; } = string.Empty;
    public string LandscapePaint { get; set; } = string.Empty;
}