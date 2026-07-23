using Ada.Core.Enums.Game.Groups;
using Ada.Db.Models.Players;

namespace Ada.Db.Models.Groups;

public class GroupForumMessage
{
    public int Id { get; init; }
    public int ThreadId { get; init; }
    public GroupForumThread? Thread { get; init; }
    public long PlayerId { get; init; }
    public Player? Player { get; init; }
    public required string Message { get; set; }
    public ForumMessageState State { get; set; }
    public long AdminId { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
