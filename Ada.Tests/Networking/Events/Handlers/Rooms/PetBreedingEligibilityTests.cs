using System.Reflection;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.Networking.Events.Handlers.Rooms.Pets;
using Moq;

namespace Ada.Tests.Networking.Events.Handlers.Rooms;

[TestFixture]
public class PetBreedingEligibilityTests
{
    private const long _owner = 1;
    private const long _stranger = 2;

    private static bool IsBreedableBy(IRoomPet pet, long playerId) =>
        (bool)typeof(PetConfirmBreedingEventHandler)
            .GetMethod("IsBreedableBy", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [pet, playerId])!;

    private static IRoomPet Pet(
        long ownerId,
        int growthStage = 7,
        bool isDead = false,
        bool publiclyBreedable = false)
    {
        var pet = new Mock<IRoomPet>();

        pet.SetupGet(x => x.Pet).Returns(new PlayerPetDto
        {
            Id = 10,
            PlayerId = ownerId,
            Name = "plant",
            Color = "ffffff",
            GrowthStage = growthStage,
            IsDead = isDead,
            PubliclyBreedable = publiclyBreedable
        });

        return pet.Object;
    }

    [Test]
    public void OwnPet_IsBreedable() => Assert.That(IsBreedableBy(Pet(_owner), _owner), Is.True);

    [Test]
    public void SomeoneElsesPrivatePet_IsNotBreedable()
    {
        Assert.That(IsBreedableBy(Pet(_owner), _stranger), Is.False,
            "confirming a stranger's nest must not hand over their offspring");
    }

    [Test]
    public void SomeoneElsesPubliclyBreedablePet_IsBreedable() => Assert.That(IsBreedableBy(Pet(_owner, publiclyBreedable: true), _stranger), Is.True);

    [Test]
    public void NotFullyGrownPet_IsNotBreedable() => Assert.That(IsBreedableBy(Pet(_owner, growthStage: 6), _owner), Is.False);

    [Test]
    public void DeadPet_IsNotBreedable() => Assert.That(IsBreedableBy(Pet(_owner, isDead: true), _owner), Is.False);
}
