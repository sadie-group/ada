using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetRide)]
public class PetRideEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
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
        var playerId = roomUser.Player.Player.Id;

        if (pet.Type != PetHelpers.HorseType || !pet.HasSaddle)
        {
            return;
        }

        if (roomPet.RiderId == playerId)
        {
            roomPet.RiderId = null;
            await room.BroadcastDataAsync(new RoomPetStatusWriter { Pets = [roomPet] });
            return;
        }

        if (roomPet.RiderId != null)
        {
            return;
        }

        if (!pet.AnyoneCanRide && pet.PlayerId != playerId)
        {
            return;
        }

        roomPet.RiderId = playerId;
        roomUser.WalkToPoint(roomPet.Point);
    }
}
