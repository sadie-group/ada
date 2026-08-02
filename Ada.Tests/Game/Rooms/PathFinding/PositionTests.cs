using Ada.Game.Rooms.PathFinding.ToGo;

namespace Ada.Tests.Game.Rooms.PathFinding;

public class PositionTests
{
    [Test]
    public void Constructor_Defaults_ToZero()
    {
        var position = new Position();

        Assert.Multiple(() =>
        {
            Assert.That(position.Row, Is.EqualTo(0));
            Assert.That(position.Column, Is.EqualTo(0));
        });
    }

    [Test]
    public void Equals_SameCoordinates_True()
    {
        Assert.That(new Position(3, 4).Equals(new Position(3, 4)), Is.True);
    }

    [TestCase(1, 4)]
    [TestCase(3, 1)]
    public void Equals_DifferentCoordinates_False(int row, int column)
    {
        Assert.That(new Position(3, 4).Equals(new Position(row, column)), Is.False);
    }

    [Test]
    public void EqualsObject_BoxedPosition_ComparesValues()
    {
        object boxed = new Position(3, 4);

        Assert.Multiple(() =>
        {
            Assert.That(new Position(3, 4).Equals(boxed), Is.True);
            Assert.That(new Position(3, 4).Equals("other"), Is.False);
            Assert.That(new Position(3, 4).Equals(null), Is.False);
        });
    }

    [Test]
    public void GetHashCode_EqualValues_Match()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new Position(3, 4).GetHashCode(), Is.EqualTo(new Position(3, 4).GetHashCode()));
            Assert.That(new Position(3, 4).GetHashCode(), Is.Not.EqualTo(new Position(4, 3).GetHashCode()));
        });
    }

    [Test]
    public void EqualityOperators_CompareValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new Position(1, 2) == new Position(1, 2), Is.True);
            Assert.That(new Position(1, 2) == new Position(2, 1), Is.False);
            Assert.That(new Position(1, 2) != new Position(2, 1), Is.True);
            Assert.That(new Position(1, 2) != new Position(1, 2), Is.False);
        });
    }

    [Test]
    public void ToString_FormatsRowAndColumn()
    {
        Assert.That(new Position(3, 4).ToString(), Is.EqualTo("[3,4]"));
    }
}
