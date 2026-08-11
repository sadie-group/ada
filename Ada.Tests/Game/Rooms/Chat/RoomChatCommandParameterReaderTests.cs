using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Game.Rooms.Chat.Commands;
using Ada.Networking.Events;
using Moq;

namespace Ada.Tests.Game.Rooms.Chat;

[TestFixture]
public class RoomChatCommandParameterReaderTests
{
    private static RoomChatCommandParameterReader CreateReader(params string[] parameters)
        => new(new Queue<string>(parameters));

    [Test]
    public void GetWord_WithParameters_DequeuesInOrder()
    {
        var reader = CreateReader("one", "two");

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetWord(out var first), Is.True);
            Assert.That(first, Is.EqualTo("one"));
            Assert.That(reader.GetWord(out var second), Is.True);
            Assert.That(second, Is.EqualTo("two"));
        });
    }

    [Test]
    public void GetWord_Empty_ReturnsFalse()
    {
        Assert.That(CreateReader().GetWord(out _), Is.False);
    }

    [Test]
    public void GetSentence_JoinsRemainingParameters_SkippingWhitespace()
    {
        var reader = CreateReader("hello", " ", "big", "world");

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetSentence(out var sentence), Is.True);
            Assert.That(sentence, Is.EqualTo("hello big world"));
        });
    }

    [Test]
    public void GetSentence_Empty_ReturnsFalse()
    {
        var result = CreateReader().GetSentence(out var sentence);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(sentence, Is.Null);
        });
    }

    [Test]
    public void GetInt_NumericParameter_Parses()
    {
        var reader = CreateReader("15");

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetInt(out var value), Is.True);
            Assert.That(value, Is.EqualTo(15));
        });
    }

    [Test]
    public void GetInt_NonNumericParameter_ReturnsFalse()
    {
        Assert.That(CreateReader("abc").GetInt(out _), Is.False);
    }

    [Test]
    public void TryGetUser_KnownUserId_ReturnsUser()
    {
        var user = Mock.Of<IRoomUser>();
        var repository = new Mock<IRoomUserRepository>();
        repository.Setup(r => r.TryGetById(7, out user)).Returns(true);

        var reader = CreateReader("7");

        Assert.Multiple(() =>
        {
            Assert.That(reader.TryGetUser(repository.Object, out var userId, out var found), Is.True);
            Assert.That(userId, Is.EqualTo(7));
            Assert.That(found, Is.SameAs(user));
        });
    }

    [Test]
    public void TryGetUser_UnknownUserId_ReturnsFalse()
    {
        var repository = new Mock<IRoomUserRepository>();

        Assert.That(CreateReader("7").TryGetUser(repository.Object, out _, out _), Is.False);
    }
}
