using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetInformation)]
public class PetInformationEventHandler(
    IRoomRepository roomRepository,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler
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
        var requesterId = roomUser.Player.Player.Id;

        await client.WriteToStreamAsync(new PetInformationWriter
        {
            Pet = pet,
            OwnerName = playerRepository.GetPlayerLogicById(pet.PlayerId)?.Player.Username ?? "",
            CanRide = pet.HasSaddle && (pet.AnyoneCanRide || pet.PlayerId == requesterId),
            IsRiding = false,
        });
    }
}
