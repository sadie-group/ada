using System.Drawing;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Events.Handlers.Rooms.Pets;

[PacketId(EventHandlerId.PetMove)]
public class PetMoveEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public required int Id { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Direction { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        if (room.Room.OwnerId != roomUser.Player.Player.Id)
        {
            return;
        }

        if (!room.PetRepository.TryGetById(Id, out var roomPet) || roomPet == null)
        {
            return;
        }

        if (X >= room.TileMap.SizeX || Y >= room.TileMap.SizeY || room.TileMap.TileExistenceMap[Y, X] == 0)
        {
            return;
        }

        var point = new Point(X, Y);

        room.TileMap.UnitMap[roomPet.Point].Remove(roomPet);
        room.TileMap.AddUnitToMap(point, roomPet);

        await roomPet.SetPositionAsync(point);
        roomPet.PointZ = room.TileMap.ZMap[Y, X];
        roomPet.Direction = (HDirection) Direction;
        roomPet.DirectionHead = (HDirection) Direction;

        await room.BroadcastDataAsync(new RoomPetStatusWriter
        {
            Pets = [roomPet],
        });
    }
}
