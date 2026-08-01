using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Packets;
using Moq;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class PacketCodecRegistryTests
{
    private static IPacketCodec CodecFor(string revision)
    {
        var codec = new Mock<IPacketCodec>();
        codec.SetupGet(c => c.Revision).Returns(revision);

        return codec.Object;
    }

    [Test]
    public void Default_ResolvesRequestedRevision()
    {
        var registry = new PacketCodecRegistry([CodecFor("R63B"), CodecFor("PRODUCTION")], "PRODUCTION");

        Assert.That(registry.Default.Revision, Is.EqualTo("PRODUCTION"));
    }

    [Test]
    public void TryGet_IsCaseInsensitive()
    {
        var registry = new PacketCodecRegistry([CodecFor("R63B")], "R63B");

        Assert.That(registry.TryGet("r63b", out var codec), Is.True);
        Assert.That(codec!.Revision, Is.EqualTo("R63B"));
    }

    [Test]
    public void TryGet_UnknownRevision_ReturnsFalse()
    {
        var registry = new PacketCodecRegistry([CodecFor("R63B")], "R63B");

        Assert.That(registry.TryGet("SHOCKWAVE", out _), Is.False);
    }

    [Test]
    public void Constructor_NoCodecs_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new PacketCodecRegistry([], "PRODUCTION"));
    }

    [Test]
    public void Constructor_DefaultRevisionMissing_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new PacketCodecRegistry([CodecFor("R63B")], "PRODUCTION"));
    }
}
