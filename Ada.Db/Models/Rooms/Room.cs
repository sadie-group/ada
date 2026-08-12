using Ada.Db.Models.Players;
using Ada.Db.Models.Players.Furniture;
using Ada.Db.Models.Rooms.Chat;
using Ada.Db.Models.Rooms.Rights;

namespace Ada.Db.Models.Rooms;

public class Room
{
    public int Id { get; init; }
    public string? Thumbnail { get; set; }
    public required string Name { get; set; }
    public int LayoutId { get; set; }
    public RoomLayout? Layout { get; set; }
    public long OwnerId { get; init; }
    public Player? Owner { get; set; }
    public int MaxUsersAllowed { get; set; }
    public required string Description { get; set; }
    public bool IsMuted { get; set; }
    public RoomSettings? Settings { get; set; }
    public RoomPaintSettings? PaintSettings { get; set; }
    public RoomChatSettings? ChatSettings{ get; set; }
    public ICollection<RoomPlayerRight> PlayerRights { get; init; } = [];
    public ICollection<RoomChatMessage> ChatMessages { get; init; } = [];
    public ICollection<RoomTag> Tags { get; init; } = [];
    public ICollection<PlayerRoomLike> PlayerLikes { get; init; } = [];
    public ICollection<PlayerFurnitureItemPlacementData> FurnitureItems { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public Group? Group { get; init; }
    public RoomDimmerSettings? DimmerSettings { get; set; }
    public ICollection<PlayerRoomBan> PlayerBans { get; init; } = [];
}