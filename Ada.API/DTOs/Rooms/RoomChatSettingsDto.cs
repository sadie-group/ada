namespace Ada.API.DTOs.Rooms;

public record RoomChatSettingsDto
{
    public int Id { get; init; }
    public int RoomId { get; init; }
    public int ChatType { get; set; }
    public int ChatWeight { get; set; }
    public int ChatSpeed { get; set; }
    public int ChatDistance { get; set; }
    public int ChatProtection { get; set; }
}