using System.Reflection;
using Ada.API.Interfaces.Networking;
using Ada.Networking.Writers;
using Ada.Networking.Writers.Generated;

namespace Ada.Tests.Networking.Packets.Serialization;

[TestFixture]
public class GeneratedWriterCoverageTests
{
    private static IEnumerable<Type> WriterTypes()
        => typeof(ServerPacketId).Assembly
            .GetTypes()
            .Where(t => typeof(AbstractPacketWriter).IsAssignableFrom(t) &&
                        t is { IsAbstract: false, IsGenericTypeDefinition: false });

    private static bool OverridesOnSerialize(Type type)
    {
        var method = type.GetMethod(
            nameof(AbstractPacketWriter.OnSerialize),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return method != null && method.DeclaringType != typeof(AbstractPacketWriter);
    }

    [Test]
    public void EveryWriter_IsGeneratedOrHandWritten()
    {
        var uncovered = WriterTypes()
            .Where(t => !GeneratedPacketWriters.Handles(t) && !OverridesOnSerialize(t))
            .Select(t => t.FullName)
            .OrderBy(x => x)
            .ToList();

        Assert.That(uncovered, Is.Empty,
            "these writers fall through to the reflection path: " + string.Join(", ", uncovered));
    }

    [Test]
    public void WriterTypes_AreDiscovered()
    {
        Assert.That(WriterTypes().Count(), Is.GreaterThan(100));
    }
}
