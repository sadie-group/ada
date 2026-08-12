namespace Ada.Networking.Events.Handlers.Moderation;

public static class SanctionEscalation
{
    private static readonly int[] _muteHoursByPriorSanctions = [1, 2, 6, 18];
    private static readonly int[] _tradeLockDaysByPriorSanctions = [1, 3, 7, 30];

    public static DateTimeOffset MuteExpiryFor(int priorSanctions, int fallbackHours)
        => DateTimeOffset.UtcNow.AddHours(Step(_muteHoursByPriorSanctions, priorSanctions, fallbackHours));

    public static DateTimeOffset TradeLockExpiryFor(int priorSanctions, int fallbackDays)
        => DateTimeOffset.UtcNow.AddDays(Step(_tradeLockDaysByPriorSanctions, priorSanctions, fallbackDays));

    private static int Step(int[] ladder, int priorSanctions, int fallback)
    {
        if (ladder.Length == 0)
        {
            return fallback;
        }

        var index = Math.Clamp(priorSanctions, 0, ladder.Length - 1);

        return ladder[index];
    }
}
