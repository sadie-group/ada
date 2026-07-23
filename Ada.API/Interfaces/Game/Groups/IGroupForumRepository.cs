using Ada.API.DTOs.Groups;
using Ada.Core.Enums.Game.Groups;

namespace Ada.API.Interfaces.Game.Groups;

public interface IGroupForumRepository
{
    Task<ForumStatsDto?> GetStatsAsync(int guildId);
    Task<(IReadOnlyList<ForumStatsDto> Forums, int Total)> GetForumsListAsync(int mode, int offset, int amount);
    Task<IReadOnlyList<ForumThreadDto>> GetThreadsAsync(int guildId, int startIndex, int amount);
    Task<ForumThreadDto?> GetThreadAsync(int guildId, int threadId);
    Task<ForumCommentDto?> GetCommentAsync(int guildId, int commentId);
    Task<(IReadOnlyList<ForumCommentDto> Comments, int Total)> GetCommentsAsync(
        int guildId, int threadId, int startIndex, int amount);

    Task<ForumThreadDto?> PostThreadAsync(int guildId, long playerId, string subject, string message);
    Task<ForumCommentDto?> PostCommentAsync(int guildId, int threadId, long playerId, string message);

    Task SetThreadPinnedLockedAsync(int threadId, bool isPinned, bool isLocked);
    Task ModerateThreadAsync(int threadId, ForumThreadState state, long adminId);
    Task ModerateCommentAsync(int commentId, ForumMessageState state, long adminId);
    Task UpdateForumSettingsAsync(
        int guildId, ForumPermissionLevel canRead, ForumPermissionLevel postMessages,
        ForumPermissionLevel postThreads, ForumPermissionLevel modForum);

    Task<(int ThreadId, int GroupId, bool IsLocked)?> GetThreadRefAsync(int threadId);
}
