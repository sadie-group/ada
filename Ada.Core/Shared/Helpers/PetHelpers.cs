namespace Ada.Core.Shared.Helpers;

public static class PetHelpers
{
    private static readonly int[] _levelExperienceGoals =
    [
        100, 200, 400, 600, 900, 1300, 1800, 2400, 3200, 4300,
        5700, 7600, 10100, 13300, 17500, 23000, 30200, 39600, 51900
    ];

    public const int MaximumLevel = 20;
    public const int MaximumNameLength = 15;
    public const int HorseType = 15;

    public static int ExperienceGoalForLevel(int level, int experience) => level >= 1 && level <= _levelExperienceGoals.Length ? _levelExperienceGoals[level - 1] : experience;

    public static int MaxEnergyForLevel(int level) => level * 100;

    public const int MonsterPlantType = 16;

    public static string BuildLookString(int type, int race, string color, bool hasSaddle, int hairStyle = -1, int hairColor = -1)
    {
        if (type == HorseType)
        {
            var baseLook = $"{type} {race} {color} {(hasSaddle ? "3" : "2")} 2 {hairStyle} {hairColor} 3 {hairStyle} {hairColor}";
            return hasSaddle ? baseLook + " 4 9 0" : baseLook;
        }

        return $"{type} {race} {color} 2 2 -1 0 3 -1 0";
    }
}
