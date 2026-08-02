using Ada.Core.Enums.Game.Moderation;

namespace Ada.API.DTOs.Moderation;

public record ModerationTicketDto
{
    public int Id { get; init; }
    public long ReporterPlayerId { get; init; }
    public required string ReporterUsername { get; init; }
    public long? ReportedPlayerId { get; init; }
    public required string ReportedUsername { get; init; }
    public int? RoomId { get; init; }
    public int CategoryId { get; init; }
    public required string Message { get; init; }
    public ModerationTicketState State { get; set; }
    public ModerationTicketResolution Resolution { get; set; }
    public long? PickedByPlayerId { get; set; }
    public string PickedByUsername { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? PickedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}
