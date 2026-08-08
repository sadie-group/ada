using System.Text.RegularExpressions;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Db;
using Ada.Db.Models.Rooms;
using Ada.Networking.Writers.Generic;
using Ada.Networking.Writers.Rooms.Users;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.FloorPlanEditor;

[PacketId(EventHandlerId.FloorPlanEditorSave)]
public partial class FloorPlanEditorSaveEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public required string HeightMap { get; init; }
    public required int DoorX { get; init; }
    public required int DoorY { get; init; }
    public required int DoorDirection { get; init; }
    public required int WallSize { get; init; }
    public required int FloorSize { get; init; }
    public required int WallHeight { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _) ||
            room.Room.OwnerId != client.Player.Player.Id ||
            room.Room.Layout == null)
        {
            return;
        }

        var errors = GetErrors();

        if (errors.Count != 0)
        {
            await client.WriteToStreamAsync(new BubbleAlertWriter
            {
                Key = EnumHelpers.GetEnumDescription(NotificationType.FloorPlanEditor),
                Messages = new Dictionary<string, string>
                {
                    { "message", string.Join("<br>", errors) }
                }
            });

            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        if (!room.Room.Layout.Name!.Contains("custom_"))
        {
            var layoutEntity = new RoomLayout
            {
                Name = $"custom_{Guid.NewGuid().ToString().Replace("-", "")[..15]}",
                DoorDirection = DoorDirection,
                DoorX = DoorX,
                DoorY = DoorY,
                Heightmap = HeightMap
            };

            dbContext.RoomLayouts.Add(layoutEntity);
            await dbContext.SaveChangesAsync();

            room.Room.Layout = new RoomLayoutDto
            {
                Id = layoutEntity.Id,
                Name = layoutEntity.Name,
                DoorDirection = DoorDirection,
                DoorX = DoorX,
                DoorY = DoorY,
                Heightmap = HeightMap
            };

            room.Room.LayoutId = layoutEntity.Id;

            await dbContext.Rooms
                .Where(x => x.Id == room.Room.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LayoutId, layoutEntity.Id));
        }
        else
        {
            room.Room.Layout.DoorDirection = DoorDirection;
            room.Room.Layout.DoorX = DoorX;
            room.Room.Layout.DoorY = DoorY;
            room.Room.Layout.Heightmap = HeightMap;

            await dbContext.RoomLayouts
                .Where(x => x.Id == room.Room.Layout.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.DoorDirection, DoorDirection)
                    .SetProperty(x => x.DoorX, DoorX)
                    .SetProperty(x => x.DoorY, DoorY)
                    .SetProperty(x => x.Heightmap, HeightMap));
        }

        var playersToForward = new List<IPlayerLogic>();

        foreach (var user in room.UserRepository.GetAll())
        {
            await room.UserRepository.TryRemoveAsync(user.Player.Player.Id, false, true);
            playersToForward.Add(user.Player);
        }

        if (!roomRepository.TryRemove(room.Room.Id, out var roomLogic))
        {
            return;
        }

        var writer = new RoomForwardEntryWriter
        {
            RoomId = roomLogic!.Room.Id
        };

        foreach (var player in playersToForward)
        {
            if (player.NetworkObject == null)
            {
                continue;
            }

            await player.NetworkObject.WriteToStreamAsync(writer);
        }
    }

    private const int _maxDimension = 64;

    public List<string> GetErrors()
    {
        var errors = new List<string>();

        if (!MyRegex().IsMatch(HeightMap))
        {
            errors.Add("${notification.floorplan_editor.error.title}");
        }

        if (HeightMap.Length > 64 * 64)
        {
            errors.Add("${notification.floorplan_editor.error.message.too_large_area}");
        }

        var rows = HeightMap.Split("\r");

        var mapRows = rows.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        if (mapRows.Count == 0 ||
            mapRows.Count > _maxDimension ||
            mapRows[0].Length > _maxDimension ||
            mapRows.Any(x => x.Length != mapRows[0].Length))
        {
            errors.Add("${notification.floorplan_editor.error.message.too_large_area}");
        }

        var doorOutsideMap = mapRows.Count == 0 ||
                             DoorX < 0 || DoorX >= mapRows[0].Length ||
                             DoorY < 0 || DoorY >= mapRows.Count;

        if (doorOutsideMap)
        {
            errors.Add("${notification.floorplan_editor.error.message.entry_tile_outside_map}");
        }
        else if (mapRows[DoorY][DoorX] == 'x')
        {
            errors.Add("${notification.floorplan_editor.error.message.entry_not_on_tile}");
        }

        if (DoorDirection is < 0 or > 7)
        {
            errors.Add("${notification.floorplan_editor.error.message.invalid_entry_tile_direction}");
        }

        if (WallSize is < -2 or > 1)
        {
            errors.Add("${notification.floorplan_editor.error.message.invalid_wall_thickness}");
        }

        if (FloorSize is < -2 or > 1)
        {
            errors.Add("${notification.floorplan_editor.error.message.invalid_floor_thickness}");
        }

        if (WallHeight is < -1 or > 15)
        {
            errors.Add("${notification.floorplan_editor.error.message.invalid_walls_fixed_height}");
        }

        return errors;
    }

    [GeneratedRegex(@"\A[a-zA-Z0-9\r]+\z")]
    private static partial Regex MyRegex();
}
