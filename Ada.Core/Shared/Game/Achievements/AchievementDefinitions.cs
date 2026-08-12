namespace Ada.Core.Shared.Game.Achievements;

public static class AchievementDefinitions
{
    private static readonly IReadOnlyDictionary<string, AchievementDefinition> ByCode = BuildAll();

    public static IReadOnlyCollection<AchievementDefinition> All => (IReadOnlyCollection<AchievementDefinition>) ByCode.Values;

    public static AchievementDefinition? Find(string code)
        => ByCode.GetValueOrDefault(code);

    private static IReadOnlyDictionary<string, AchievementDefinition> BuildAll()
    {
        var definitions = new[]
        {
            new AchievementDefinition("ACH_Login", 3, Levels(
                (1, 10, "ACH_Login1"),
                (5, 15, "ACH_Login2"),
                (10, 20, "ACH_Login3"),
                (25, 30, "ACH_Login4"),
                (50, 50, "ACH_Login5"))),

            new AchievementDefinition("ACH_RoomEntry", 5, Levels(
                (1, 10, "ACH_RoomEntry1"),
                (10, 15, "ACH_RoomEntry2"),
                (50, 20, "ACH_RoomEntry3"),
                (100, 30, "ACH_RoomEntry4"))),

            new AchievementDefinition("ACH_Motto", 2, Levels(
                (1, 10, "ACH_Motto1"))),

            new AchievementDefinition("ACH_AvatarLooks", 2, Levels(
                (1, 10, "ACH_AvatarLooks1"),
                (5, 15, "ACH_AvatarLooks2"))),

            new AchievementDefinition("ACH_RespectEarned", 4, Levels(
                (1, 10, "ACH_RespectEarned1"),
                (10, 15, "ACH_RespectEarned2"),
                (50, 25, "ACH_RespectEarned3"),
                (100, 40, "ACH_RespectEarned4"))),

            new AchievementDefinition("ACH_RespectGiven", 4, Levels(
                (1, 10, "ACH_RespectGiven1"),
                (10, 15, "ACH_RespectGiven2"),
                (50, 25, "ACH_RespectGiven3"))),

            new AchievementDefinition("ACH_FriendCount", 6, Levels(
                (1, 10, "ACH_FriendCount1"),
                (5, 15, "ACH_FriendCount2"),
                (25, 25, "ACH_FriendCount3"),
                (50, 40, "ACH_FriendCount4"))),

            new AchievementDefinition("ACH_RoomDecoFurniCount", 7, Levels(
                (1, 10, "ACH_RoomDecoFurniCount1"),
                (25, 15, "ACH_RoomDecoFurniCount2"),
                (100, 25, "ACH_RoomDecoFurniCount3")))
        };

        return definitions.ToDictionary(x => x.Code);
    }

    private static IReadOnlyList<AchievementLevel> Levels(params (int Required, int Reward, string Badge)[] levels)
        => levels.Select(x => new AchievementLevel(x.Required, x.Reward, x.Badge)).ToArray();
}
