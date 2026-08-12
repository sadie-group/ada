using System.Collections.Concurrent;
using Ada.Core.Shared.Extensions;

namespace Ada.Tests.Shared.Extensions;

[TestFixture]
public class DictionaryExtensionsTests
{
    [Test]
    public void GetOrInsert_MissingKey_InsertsAndReturnsFactoryValue()
    {
        var dictionary = new ConcurrentDictionary<string, int>();

        var result = dictionary.GetOrInsert("a", () => 5);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(5));
            Assert.That(dictionary["a"], Is.EqualTo(5));
        });
    }

    [Test]
    public void GetOrInsert_ExistingKey_ReturnsStoredValueWithoutCallingFactory()
    {
        var dictionary = new ConcurrentDictionary<string, int>
        {
            ["a"] = 1
        };

        var result = dictionary.GetOrInsert("a", () => throw new InvalidOperationException("factory should not run"));

        Assert.That(result, Is.EqualTo(1));
    }
}
