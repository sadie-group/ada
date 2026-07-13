namespace Ada.API.DTOs.Players;

public record PlayerGameSettingsDto
{
    public int SystemVolume { get; init; }
    public int FurnitureVolume { get; init; }
    public int TraxVolume { get; init; }
    public bool PreferOldChat { get; init; }
    public bool BlockRoomInvites { get; init; }
    public bool BlockCameraFollow { get; init; }
    public int UiFlags { get; init; }
    public bool ShowNotifications { get; init; }
}