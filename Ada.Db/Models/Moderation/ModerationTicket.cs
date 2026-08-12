using Ada.Core.Enums.Game.Moderation;
using Ada.Db.Models.Players;
using Ada.Db.Models.Rooms;

namespace Ada.Db.Models.Moderation;

public class ModerationTicket
{
    public int Id { get; init; }
    public required long ReporterPlayerId { get; init; }
    public Player? ReporterPlayer { get; init; }
    public long? ReportedPlayerId { get; init; }
    public Player? ReportedPlayer { get; init; }
    public int? RoomId { get; init; }
    public Room? Room { get; init; }
    public int CategoryId { get; init; }
    public required string Message { get; init; }
    public ModerationTicketState State { get; set; } = ModerationTicketState.Open;
    public ModerationTicketResolution Resolution { get; set; } = ModerationTicketResolution.None;
    public long? PickedByPlayerId { get; set; }
    public Player? PickedByPlayer { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? PickedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}
