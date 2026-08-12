using Ada.API.Collections;
using Ada.API.DTOs.Players;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms.Chat;
using Ada.API.DTOs.Rooms.Rights;

namespace Ada.API.DTOs.Rooms;

public record RoomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LayoutId { get; set; }
    public RoomLayoutDto? Layout { get; set; }
    public long OwnerId { get; set; }
    public int MaxUsersAllowed { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsMuted { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public RoomSettingsDto? Settings { get; set; }
    public RoomPaintSettingsDto? PaintSettings { get; set; }
    public RoomChatSettingsDto? ChatSettings { get; set; }
    public ICollection<RoomPlayerRightDto> PlayerRights { get; init; } = [];
    public ICollection<RoomChatMessageDto> ChatMessages { get; init; } = [];
    public ICollection<RoomTagDto> Tags { get; init; } = [];
    public ICollection<PlayerRoomLikeDto> PlayerLikes { get; init; } = [];
    public RevisionedCollection<PlayerFurnitureItemPlacementDataDto> FurnitureItems { get; init; } = [];
    public GroupDto? Group { get; init; }
    public RoomDimmerSettingsDto? DimmerSettings { get; set; }
    public ICollection<PlayerRoomBanDto> PlayerBans { get; set; } = [];
}
