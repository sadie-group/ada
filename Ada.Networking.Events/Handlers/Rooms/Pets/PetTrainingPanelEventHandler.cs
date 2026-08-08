using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetTrainingPanel)]
public class PetTrainingPanelEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    private static readonly (int CommandId, int RequiredLevel)[] _commands =
    [
        (0, 1), (1, 1), (2, 1), (3, 2), (4, 3), (5, 4), (6, 5), (7, 6), (8, 6),
        (9, 7), (10, 8), (11, 9), (12, 10), (13, 11), (14, 12), (15, 13), (16, 14), (17, 15),
    ];

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

        await client.WriteToStreamAsync(new PetTrainingPanelWriter
        {
            PetId = roomPet.Pet.Id,
            CommandIds = _commands.Select(x => x.CommandId).ToList(),
            EnabledCommandIds = _commands
                .Where(x => roomPet.Pet.Level >= x.RequiredLevel)
                .Select(x => x.CommandId)
                .ToList(),
        });
    }
}
