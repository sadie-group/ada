using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.FloorPlanEditor;

namespace Ada.Networking.Events.Handlers.Rooms.FloorPlanEditor;

[PacketId(EventHandlerId.FloorPlanEditorGetOccupiedTiles)]
public class FloorPlanEditorGetOccupiedTilesEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _))
        {
            return;
        }

        var blockedUserPoints = room
            .TileMap
            .UnitMap
            .Where(x => x.Value.Count > 0)
            .ToList()
            .Select(x => x.Key);

        await client.WriteToStreamAsync(new FloorPlanEditorOccupiedTilesWriter
        {
            Points = blockedUserPoints.ToList()
        });
    }
}