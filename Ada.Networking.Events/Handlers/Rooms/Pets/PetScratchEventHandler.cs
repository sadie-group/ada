using Microsoft.EntityFrameworkCore;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Db;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetScratch)]
public class PetScratchEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    private const int ScratchExperience = 10;
    private const int ScratchHappiness = 10;

    public required int Id { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _))
        {
            return;
        }

        if (!room.PetRepository.TryGetById(Id, out var roomPet) || roomPet == null)
        {
            return;
        }

        var pet = roomPet.Pet;

        pet.Respect++;
        pet.Happiness = Math.Min(100, pet.Happiness + ScratchHappiness);
        pet.Experience += ScratchExperience;

        var leveledUp = pet.Level < PetHelpers.MaximumLevel &&
                        pet.Experience >= PetHelpers.ExperienceGoalForLevel(pet.Level, int.MaxValue);

        if (leveledUp)
        {
            pet.Level++;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerPets
            .Where(x => x.Id == pet.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Respect, pet.Respect)
                .SetProperty(x => x.Happiness, pet.Happiness)
                .SetProperty(x => x.Experience, pet.Experience)
                .SetProperty(x => x.Level, pet.Level));

        await room.BroadcastDataAsync(new RoomPetRespectWriter
        {
            RespectType = 1,
            Pet = pet,
        });

        await room.BroadcastDataAsync(new RoomPetExperienceWriter
        {
            PetId = pet.Id,
            RoomUnitId = pet.Id,
            Amount = ScratchExperience,
        });

        if (leveledUp)
        {
            await room.BroadcastDataAsync(new PetLevelUpdatedWriter
            {
                RoomUnitId = pet.Id,
                PetId = pet.Id,
                Level = pet.Level,
            });
        }
    }
}
