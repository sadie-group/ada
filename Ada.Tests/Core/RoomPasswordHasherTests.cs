using Ada.Core.Shared.Helpers;
using NUnit.Framework;

namespace Ada.Tests.Core;

[TestFixture]
public class RoomPasswordHasherTests
{
    [Test]
    public void Hash_ThenVerify_AcceptsCorrectPassword()
    {
        var hash = RoomPasswordHasher.Hash("secret");

        Assert.Multiple(() =>
        {
            Assert.That(hash, Does.Not.Contain("secret"));
            Assert.That(RoomPasswordHasher.Verify(hash, "secret"), Is.True);
            Assert.That(RoomPasswordHasher.Verify(hash, "wrong"), Is.False);
        });
    }

    [Test]
    public void Hash_UsesRandomSalt_ProducingDistinctHashes()
    {
        var first = RoomPasswordHasher.Hash("secret");
        var second = RoomPasswordHasher.Hash("secret");

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void Verify_LegacyPlaintextStored_StillMatches()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RoomPasswordHasher.Verify("plain", "plain"), Is.True);
            Assert.That(RoomPasswordHasher.Verify("plain", "nope"), Is.False);
        });
    }

    [Test]
    public void Verify_EmptyStored_MatchesEmptyInput()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RoomPasswordHasher.Verify("", ""), Is.True);
            Assert.That(RoomPasswordHasher.Verify(null, ""), Is.True);
            Assert.That(RoomPasswordHasher.Verify("", "x"), Is.False);
        });
    }
}
