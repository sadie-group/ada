using Ada.API.Interfaces.Game.Pets;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetRemoveSaddle)]
public class PetRemoveSaddleEventHandler(
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

        if (pet.PlayerId != roomUser.Player.Player.Id || pet.Type != PetHelpers.HorseType || !pet.HasSaddle)
        {
            return;
        }

        pet.HasSaddle = false;
        roomPet.RiderId = null;

        _persist = () => petPersistence.SaveSaddleAsync(pet);

        await room.BroadcastDataAsync(new RoomPetHorseFigureWriter
        {
            Pet = pet,
        });
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
