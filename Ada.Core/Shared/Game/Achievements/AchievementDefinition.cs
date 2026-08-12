namespace Ada.Core.Shared.Game.Achievements;

public sealed record AchievementLevel(int RequiredProgress, int RewardPoints, string BadgeCode);

public sealed record AchievementState(
    int CurrentLevel,
    int MaxLevel,
    int Progress,
    int ProgressForCurrentLevel,
    int ProgressForNextLevel,
    int RewardPoints,
    string BadgeCode,
    bool Completed);

public sealed record AchievementDefinition(string Code, int CategoryId, IReadOnlyList<AchievementLevel> Levels)
{
    public int MaxLevel => Levels.Count;

    public AchievementState Resolve(int progress)
    {
        if (progress < 0)
        {
            progress = 0;
        }

        var level = 0;

        for (var i = 0; i < Levels.Count; i++)
        {
            if (progress >= Levels[i].RequiredProgress)
            {
                level = i + 1;
            }
            else
            {
                break;
            }
        }

        var completed = level >= Levels.Count;
        var currentIndex = level == 0 ? 0 : level - 1;
        var nextIndex = completed ? Levels.Count - 1 : level;

        var progressForCurrentLevel = level == 0 ? 0 : Levels[currentIndex].RequiredProgress;
        var next = Levels[nextIndex];

        return new AchievementState(
            level,
            Levels.Count,
            progress,
            progressForCurrentLevel,
            next.RequiredProgress,
            next.RewardPoints,
            Levels[currentIndex].BadgeCode,
            completed);
    }
}
