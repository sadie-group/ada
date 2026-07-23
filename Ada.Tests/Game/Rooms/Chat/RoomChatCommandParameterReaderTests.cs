using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Game.Rooms.Chat.Commands;
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
        });
    }

    [Test]
    public void GetSentence_JoinsRemainingParameters()
    {
        var reader = CreateReader("hello", "world");

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetSentence(out var sentence), Is.True);
            Assert.That(sentence, Is.EqualTo("hello world"));
        });
    }

    [Test]
    public void GetInt_ParsesAndRejects()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CreateReader("15").GetInt(out var value), Is.True);
            Assert.That(value, Is.EqualTo(15));
            Assert.That(CreateReader("abc").GetInt(out _), Is.False);
            Assert.That(CreateReader().GetInt(out _), Is.False);
        });
    }

    [Test]
    public void TryGetUser_ResolvesFromRepository()
    {
        var user = Mock.Of<IRoomUser>();
        var repository = new Mock<IRoomUserRepository>();
        repository.Setup(r => r.TryGetById(7, out user)).Returns(true);

        Assert.Multiple(() =>
        {
            Assert.That(CreateReader("7").TryGetUser(repository.Object, out var userId, out var found), Is.True);
            Assert.That(userId, Is.EqualTo(7));
            Assert.That(found, Is.SameAs(user));
            Assert.That(CreateReader("9").TryGetUser(repository.Object, out _, out _), Is.False);
        });
    }
}
