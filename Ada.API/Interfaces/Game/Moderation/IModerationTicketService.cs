using Ada.API.DTOs.Moderation;
using Ada.Core.Enums.Game.Moderation;

namespace Ada.API.Interfaces.Game.Moderation;

public interface IModerationTicketService
{
    Task LoadAsync();
    IReadOnlyList<ModerationTicketDto> GetActiveTickets();
    ModerationTicketDto? GetById(int ticketId);
    Task<ModerationTicketDto?> CreateAsync(long reporterId, long? reportedId, int? roomId, int categoryId, string message);
    Task<ModerationTicketDto?> TryPickAsync(int ticketId, long moderatorId, string moderatorUsername);
    Task<ModerationTicketDto?> TryReleaseAsync(int ticketId, long moderatorId);
    Task<ModerationTicketDto?> TryCloseAsync(int ticketId, long moderatorId, ModerationTicketResolution resolution);
    Task<ModerationTicketDto?> TryChangeTopicAsync(int ticketId, long moderatorId, int categoryId);
    Task<IReadOnlyList<ModerationCfhTopicDto>> GetTopicsAsync();
}
