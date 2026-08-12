using Ada.API.Interfaces.Game.Pets;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetScratch)]
public class PetScratchEventHandler(
    IPlayerPetPersistence petPersistence,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    private const int _scratchExperience = 10;
    private const int _scratchHappiness = 10;

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
        pet.Happiness = Math.Min(100, pet.Happiness + _scratchHappiness);
        pet.Experience += _scratchExperience;

        var leveledUp = pet.Level < PetHelpers.MaximumLevel &&
                        pet.Experience >= PetHelpers.ExperienceGoalForLevel(pet.Level, int.MaxValue);

        if (leveledUp)
        {
            pet.Level++;
        }

        _persist = () => petPersistence.SaveScratchAsync(pet);

        await room.BroadcastDataAsync(new RoomPetRespectWriter
        {
            RespectType = 1,
            Pet = pet,
        });

        await room.BroadcastDataAsync(new RoomPetExperienceWriter
        {
            PetId = pet.Id,
            RoomUnitId = pet.Id,
            Amount = _scratchExperience,
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

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
