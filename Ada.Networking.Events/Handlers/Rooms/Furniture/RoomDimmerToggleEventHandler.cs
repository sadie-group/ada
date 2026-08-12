using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Game.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomDimmerToggle)]
public class RoomDimmerToggleEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService)
    : INetworkPacketEventHandler, IManagesOwnRoomLock, IDefersPersistence
{
    private Func<Task>? _persist;

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _) ||
            room.Room.DimmerSettings == null ||
            client.RoomUser == null ||
            !client.RoomUser.HasRights())
        {
            return;
        }

        var roomId = room.Room.Id;
        var presetId = room.Room.DimmerSettings.PresetId;

        var preset = await ReadPresetAsync(roomId, presetId);

        if (preset == null)
        {
            return;
        }

        var enabledAfterToggle = false;

        await room.RunLockedAsync(async () =>
        {
            var dimmer = room
                .Room.FurnitureItems
                .FirstOrDefault(x =>
                    x.PlayerFurnitureItem.FurnitureItem.InteractionType == FurnitureItemInteractionType.Dimmer);

            if (dimmer == null || room.Room.DimmerSettings == null)
            {
                return;
            }

            room.Room.DimmerSettings.Enabled = !room.Room.DimmerSettings.Enabled;
            enabledAfterToggle = room.Room.DimmerSettings.Enabled;

            var enabled = enabledAfterToggle ? 2 : 1;
            var bgOnly = preset.BackgroundOnly ? 2 : 0;

            await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(
                room,
                dimmer,
                $"{enabled},{preset.PresetId},{bgOnly},{preset.Color},{preset.Intensity}");

            _persist = async () =>
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync();

                await dbContext.RoomDimmerSettings
                    .Where(x => x.RoomId == roomId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Enabled, enabledAfterToggle));
            };
        });
    }

    private async Task<Db.Models.Rooms.RoomDimmerPreset?> ReadPresetAsync(int roomId, int presetId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.RoomDimmerPresets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RoomId == roomId && x.PresetId == presetId);
    }

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
