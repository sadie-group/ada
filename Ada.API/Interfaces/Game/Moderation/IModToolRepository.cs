using Ada.API.DTOs.Moderation;

namespace Ada.API.Interfaces.Game.Moderation;

public interface IModToolRepository
{
    Task<ModToolUserInfoDto?> GetUserInfoAsync(long userId);
    Task<(string Username, IReadOnlyList<ModToolRoomVisitDto> Visits)> GetRoomVisitsAsync(long userId, int limit);
    Task<(string Username, IReadOnlyList<ModToolChatRoomDto> Rooms)> GetUserChatlogAsync(long userId, int limit);
    Task<bool> CreateBanAsync(long moderatorId, long targetId, string reason, DateTimeOffset? expiresAt);
    Task<bool> ApplyMuteAsync(long targetId, DateTimeOffset expiresAt);
    Task<bool> ApplyTradeLockAsync(long targetId, DateTimeOffset expiresAt);
    Task<int> GetPriorSanctionCountAsync(long targetId);
}
