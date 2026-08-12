namespace Ada.API.DTOs.Players;

public record PlayerGameSettingsDto
{
    public int SystemVolume { get; set; }
    public int FurnitureVolume { get; set; }
    public int TraxVolume { get; set; }
    public bool PreferOldChat { get; set; }
    public bool BlockRoomInvites { get; set; }
    public bool BlockCameraFollow { get; set; }
    public int UiFlags { get; set; }
    public bool ShowNotifications { get; set; }
}