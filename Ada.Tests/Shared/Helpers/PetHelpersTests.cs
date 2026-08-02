using Ada.Core.Shared.Helpers;

namespace Ada.Tests.Shared.Helpers;

public class PetHelpersTests
{
    [TestCase(1, 999, ExpectedResult = 100)]
    [TestCase(5, 999, ExpectedResult = 900)]
    [TestCase(19, 999, ExpectedResult = 51900)]
    public int ExperienceGoalForLevel_ValidLevel_ReturnsGoal(int level, int experience) =>
        PetHelpers.ExperienceGoalForLevel(level, experience);

    [TestCase(0, 42, ExpectedResult = 42)]
    [TestCase(20, 42, ExpectedResult = 42)]
    [TestCase(-1, 7, ExpectedResult = 7)]
    public int ExperienceGoalForLevel_OutOfRange_ReturnsExperience(int level, int experience) =>
        PetHelpers.ExperienceGoalForLevel(level, experience);

    [TestCase(1, ExpectedResult = 100)]
    [TestCase(7, ExpectedResult = 700)]
    public int MaxEnergyForLevel_MultipliesByHundred(int level) =>
        PetHelpers.MaxEnergyForLevel(level);

    [Test]
    public void BuildLookString_HorseWithSaddle_AppendsSaddleParts()
    {
        var look = PetHelpers.BuildLookString(PetHelpers.HorseType, 2, "c", true, 7, 8);

        Assert.That(look, Is.EqualTo("15 2 c 3 2 7 8 3 7 8 4 9 0"));
    }

    [Test]
    public void BuildLookString_HorseWithoutSaddle_NoSaddleParts()
    {
        var look = PetHelpers.BuildLookString(PetHelpers.HorseType, 2, "c", false, 7, 8);

        Assert.That(look, Is.EqualTo("15 2 c 2 2 7 8 3 7 8"));
    }

    [Test]
    public void BuildLookString_NonHorse_UsesGenericLook()
    {
        var look = PetHelpers.BuildLookString(1, 3, "blue", true);

        Assert.That(look, Is.EqualTo("1 3 blue 2 2 -1 0 3 -1 0"));
    }
}
