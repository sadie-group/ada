using Ada.Core.Enums.Game.Groups;

namespace Ada.API.DTOs;

public record GroupDto
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int RoomId { get; init; }
    public int CreatedAt { get; init; }
    public string Badge { get; init; } = string.Empty;
    public int ColorA { get; init; }
    public int ColorB { get; init; }
    public GroupType Type { get; init; }
    public bool AdminOnlyDecoration { get; init; }
    public bool HasForum { get; init; }
    public ForumPermissionLevel ForumReadPermission { get; init; }
    public ForumPermissionLevel ForumPostMessagesPermission { get; init; }
    public ForumPermissionLevel ForumPostThreadsPermission { get; init; }
    public ForumPermissionLevel ForumModPermission { get; init; }
    public IReadOnlyCollection<long>? PlayerIds { get; init; }
}
