using Ada.API.Interfaces.Game.Pets;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Networking.Writers.Rooms.Users;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetCompostMonsterPlant)]
public class PetCompostMonsterPlantEventHandler(
    IPlayerPetPersistence petPersistence,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public required int Id { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        if (!room.PetRepository.TryGetById(Id, out var roomPet) || roomPet == null)
        {
            return;
        }

        var pet = roomPet.Pet;

        if (pet.PlayerId != roomUser.Player.Player.Id ||
            pet.Type != PetHelpers.MonsterPlantType ||
            !pet.IsDead)
        {
            return;
        }

        if (!room.PetRepository.TryRemove(Id, out _))
        {
            return;
        }

        room.TileMap.UnitMap[roomPet.Point].Remove(roomPet);

        _persist = () => petPersistence.DeleteAsync(pet.Id);

        await room.BroadcastDataAsync(new RoomUserLeftWriter
        {
            UserId = pet.Id.ToString(),
        });
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
