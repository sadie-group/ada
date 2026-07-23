using Ada.API.DTOs.Moderation;

namespace Ada.API.Interfaces.Game.Moderation;

public interface IModToolRepository
{
    Task<ModToolUserInfoDto?> GetUserInfoAsync(long userId);
    Task<(string Username, IReadOnlyList<ModToolRoomVisitDto> Visits)> GetRoomVisitsAsync(long userId, int limit);
}
