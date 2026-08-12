using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Core.Shared;
using Ada.Db.Models.Players;
using Ada.Db;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Pets.Breeding;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetBreedMonsterPlants)]
public class PetBreedMonsterPlantsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IManagesOwnRoomLock
{
    public required int State { get; init; }
    public required int PetOneId { get; init; }
    public required int PetTwoId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (State != 0)
        {
            return;
        }

        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        var playerId = roomUser.Player.Player.Id;

        PlayerPet? seed = null;

        await room.RunLockedAsync(() =>
        {
            seed = TryBuildSeed(room, playerId);
            return Task.CompletedTask;
        });

        if (seed == null)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.PlayerPets.Add(seed);
        await dbContext.SaveChangesAsync();

        await client.WriteToStreamAsync(new PetBreedingCompletedWriter
        {
            PetId = seed.Id,
            RarityCategory = seed.Rarity,
        });
    }

    private PlayerPet? TryBuildSeed(IRoomLogic room, long playerId)
    {
        if (!room.PetRepository.TryGetById(PetOneId, out var petOne) || petOne == null ||
            !room.PetRepository.TryGetById(PetTwoId, out var petTwo) || petTwo == null ||
            PetOneId == PetTwoId)
        {
            return null;
        }

        if (petOne.Pet.Type != PetHelpers.MonsterPlantType || petTwo.Pet.Type != PetHelpers.MonsterPlantType)
        {
            return null;
        }

        var canBreedOne = petOne.Pet.GrowthStage >= 7 && !petOne.Pet.IsDead &&
                          (petOne.Pet.PlayerId == playerId || petOne.Pet.PubliclyBreedable);
        var canBreedTwo = petTwo.Pet.GrowthStage >= 7 && !petTwo.Pet.IsDead &&
                          (petTwo.Pet.PlayerId == playerId || petTwo.Pet.PubliclyBreedable);

        if (!canBreedOne || !canBreedTwo)
        {
            return null;
        }

        var rarity = Math.Min(petOne.Pet.Rarity, petTwo.Pet.Rarity) + (GlobalState.Random.Next(0, 4) == 0 ? 1 : 0);

        return new PlayerPet
        {
            PlayerId = playerId,
            RoomId = null,
            Name = "Monster Plant Seed",
            Type = PetHelpers.MonsterPlantType,
            Race = GlobalState.Random.Next(0, 2) == 0 ? petOne.Pet.Race : petTwo.Pet.Race,
            Color = "ffffff",
            Rarity = rarity,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
