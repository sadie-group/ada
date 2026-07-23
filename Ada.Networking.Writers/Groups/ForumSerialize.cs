using Ada.API;
using Ada.API.DTOs.Groups;

namespace Ada.Networking.Writers.Groups;

internal static class ForumSerialize
{
    private static int SecondsSince(DateTimeOffset when)
        => (int) Math.Max(0, (DateTimeOffset.UtcNow - when).TotalSeconds);

    public static void Thread(INetworkPacketWriter writer, ForumThreadDto thread)
    {
        writer.WriteInteger(thread.ThreadId);
        writer.WriteInteger((int) thread.OpenerId);
        writer.WriteString(thread.OpenerUsername);
        writer.WriteString(thread.Subject);
        writer.WriteBool(thread.IsPinned);
        writer.WriteBool(thread.IsLocked);
        writer.WriteInteger(SecondsSince(thread.CreatedAt));
        writer.WriteInteger(thread.TotalComments);
        writer.WriteInteger(thread.UnreadComments);
        writer.WriteInteger(1);
        writer.WriteInteger((int) thread.LastAuthorId);
        writer.WriteString(thread.LastAuthorUsername);
        writer.WriteInteger(thread.LastCommentAt.HasValue ? SecondsSince(thread.LastCommentAt.Value) : 0);
        writer.WriteByte((byte) thread.State);
        writer.WriteInteger((int) thread.AdminId);
        writer.WriteString(thread.AdminUsername);
        writer.WriteInteger(thread.ThreadId);
    }

    public static void Comment(INetworkPacketWriter writer, ForumCommentDto comment)
    {
        writer.WriteInteger(comment.CommentId);
        writer.WriteInteger(comment.Index);
        writer.WriteInteger((int) comment.UserId);
        writer.WriteString(comment.Username);
        writer.WriteString(comment.FigureCode);
        writer.WriteInteger(SecondsSince(comment.CreatedAt));
        writer.WriteString(comment.Message);
        writer.WriteByte((byte) comment.State);
        writer.WriteInteger((int) comment.AdminId);
        writer.WriteString(comment.AdminUsername);
        writer.WriteInteger(0);
        writer.WriteInteger(comment.AuthorPostCount);
    }

    public static void Stats(INetworkPacketWriter writer, ForumStatsDto stats)
    {
        writer.WriteInteger(stats.GuildId);
        writer.WriteString(stats.GuildName);
        writer.WriteString(stats.GuildDescription);
        writer.WriteString(stats.Badge);
        writer.WriteInteger(stats.TotalThreads);
        writer.WriteInteger(0);
        writer.WriteInteger(stats.TotalComments);
        writer.WriteInteger(stats.UnreadComments);
        writer.WriteInteger(stats.LastCommentThreadId);
        writer.WriteInteger((int) stats.LastCommentUserId);
        writer.WriteString(stats.LastCommentUsername);
        writer.WriteInteger(stats.LastCommentAt.HasValue ? SecondsSince(stats.LastCommentAt.Value) : 0);
    }
}
