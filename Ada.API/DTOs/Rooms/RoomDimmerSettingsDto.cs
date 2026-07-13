namespace Ada.API.DTOs.Rooms;

public record RoomDimmerSettingsDto
{
    public int Id { get; init; }
    public int RoomId { get; init; }
    public bool Enabled { get; set; }
    public int PresetId { get; init; }
}