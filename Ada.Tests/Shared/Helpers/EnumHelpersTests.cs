using Ada.Core.Shared.Helpers;

namespace Ada.Tests.Shared.Helpers;

[TestFixture]
public class EnumHelpersTests
{
    private enum Sample
    {
        [System.ComponentModel.Description("first value")]
        First,
        Second,
    }

    [Test]
    public void GetEnumValueFromDescription_MatchingDescription_ReturnsValue()
    {
        var result = EnumHelpers.GetEnumValueFromDescription<Sample>("first value");
        Assert.That(result, Is.EqualTo(Sample.First));
    }

    [Test]
    public void GetEnumValueFromDescription_UnknownDescription_Throws()
    {
        Assert.Throws<Exception>(() => EnumHelpers.GetEnumValueFromDescription<Sample>("nope"));
    }

    [Test]
    public void GetEnumDescription_WithDescriptionAttribute_ReturnsDescription()
    {
        Assert.That(EnumHelpers.GetEnumDescription(Sample.First), Is.EqualTo("first value"));
    }

    [Test]
    public void GetEnumDescription_WithoutAttribute_ReturnsName()
    {
        Assert.That(EnumHelpers.GetEnumDescription(Sample.Second), Is.EqualTo("Second"));
    }
}
