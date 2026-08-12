namespace Ada.Networking.Events.Handlers.Rooms.Bots;

public static class BotChatLines
{
    public const int MaxLines = 10;
    public const int MaxLineLength = 100;
    public const int MinChatDelaySeconds = 5;
    public const int MaxChatDelaySeconds = 120;

    public static IReadOnlyList<string> Split(string stored)
        => string.IsNullOrEmpty(stored)
            ? []
            : stored.Split('\r', StringSplitOptions.RemoveEmptyEntries);

    public static string Join(IEnumerable<string> lines)
        => string.Join('\r', lines
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().Length > MaxLineLength ? x.Trim()[..MaxLineLength] : x.Trim())
            .Take(MaxLines));
}
