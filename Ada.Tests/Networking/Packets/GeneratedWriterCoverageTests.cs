using System.Reflection;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Networking.Writers.Handshake;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class GeneratedWriterCoverageTests
{
    private static Assembly WritersAssembly => typeof(SecureLoginWriter).Assembly;

    private static Type GeneratedWriters =>
        WritersAssembly.GetType("Ada.Networking.Writers.Generated.GeneratedPacketWriters")
        ?? throw new InvalidOperationException(
            "The packet writer source generator produced no output for Ada.Networking.Writers.");

    private static bool Handles(Type writerType)
        => (bool) GeneratedWriters
            .GetMethod("Handles", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [writerType])!;

    private static bool DeclaresOnSerialize(Type writerType)
        => writerType.GetMethod(
            "OnSerialize",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) != null;

    private static IEnumerable<Type> ConcreteWriters()
        => WritersAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsNested: false } &&
                        t.BaseType == typeof(AbstractPacketWriter));

    [Test]
    public void EveryWriter_IsGeneratedOrHandWritten()
    {
        var uncovered = ConcreteWriters()
            .Where(t => !Handles(t) && !DeclaresOnSerialize(t))
            .Select(t => t.FullName)
            .OrderBy(x => x)
            .ToList();

        Assert.That(uncovered, Is.Empty,
            "These writers fall back to reflection at runtime. Give each one a generated codec, " +
            "or a hand-written OnSerialize if its layout cannot be generated:\n" +
            string.Join("\n", uncovered));
    }

    [Test]
    public void GeneratedWriters_CoverTheMajorityOfWriters()
    {
        var writers = ConcreteWriters().ToList();

        Assert.That(writers, Is.Not.Empty);
        Assert.That(writers.Count(t => Handles(t)), Is.GreaterThan(writers.Count / 2));
    }

    [Test]
    public void FastPath_IsInstalledByModuleInitializer()
    {
        _ = GeneratedWriters;

        Assert.That(PacketWriterFastPath.Handler, Is.Not.Null);
    }
}
