using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetStopBreeding)]
public class PetStopBreedingEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public required int NestId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _))
        {
            return;
        }

        if (!room.PetRepository.TryGetBreeding(NestId, out _))
        {
            return;
        }

        room.PetRepository.StopBreeding(NestId);

        await client.WriteToStreamAsync(new PetPackageNameValidationWriter
        {
            ItemId = NestId,
            ErrorCode = PetPackageNameValidationWriter.CloseWidget,
            ErrorText = "",
        });
    }
}
