using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Furniture;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomDimmerSave)]
public class RoomDimmerSaveEventHandler(
    IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService,
    IMapper mapper) : INetworkPacketEventHandler, IManagesOwnRoomLock, IDefersPersistence
{
    public required int PresetId { get; init; }
    public required int BackgroundOnly { get; init; }
    public required string Color { get; init; }
    public required int Intensity { get; init; }
    public required bool Apply { get; init; }

    private const int _minIntensity = 0;
    private const int _maxIntensity = 255;

    private static bool IsValidColor(string color)
    {
        if (color.Length != 7 || color[0] != '#')
        {
            return false;
        }

        for (var i = 1; i < color.Length; i++)
        {
            if (!Uri.IsHexDigit(color[i]))
            {
                return false;
            }
        }

        return true;
    }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.RoomUser == null)
        {
            return;
        }

        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _))
        {
            return;
        }

        if (!client.RoomUser.HasRights())
        {
            return;
        }

        if (!IsValidColor(Color) || Intensity is < _minIntensity or > _maxIntensity)
        {
            return;
        }

        var dimmer = room
            .Room
            .FurnitureItems
            .FirstOrDefault(x => x.PlayerFurnitureItem.FurnitureItem.InteractionType == FurnitureItemInteractionType.Dimmer);

        if (dimmer == null)
        {
            return;
        }

        var roomId = room.Room.Id;

        List<Db.Models.Rooms.RoomDimmerPreset> presets;

        await using (var readContext = await dbContextFactory.CreateDbContextAsync())
        {
            presets = await readContext
                .RoomDimmerPresets
                .Where(x => x.RoomId == roomId)
                .ToListAsync();
        }

        var preset = presets.FirstOrDefault(x => x.PresetId == PresetId);

        if (preset == null)
        {
            return;
        }

        preset.BackgroundOnly = BackgroundOnly == 2;
        preset.Color = Color;
        preset.Intensity = Intensity;

        var applied = false;

        await room.RunLockedAsync(async () =>
        {
            var dimmerSettings = room.Room.DimmerSettings;

            if (dimmerSettings == null)
            {
                return;
            }

            dimmerSettings.Enabled = Apply;

            var enabled = dimmerSettings.Enabled ? 2 : 0;
            var bgOnly = preset.BackgroundOnly ? 2 : 0;
            var meta = $"{enabled},{preset.PresetId},{bgOnly},{preset.Color},{preset.Intensity}";

            await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(
                room,
                dimmer,
                meta);

            applied = true;
        });

        if (!applied)
        {
            return;
        }

        var enabledAfterSave = Apply;
        var presetId = preset.PresetId;
        var backgroundOnly = preset.BackgroundOnly;
        var color = preset.Color;
        var intensity = preset.Intensity;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.RoomDimmerPresets
                .Where(x => x.RoomId == roomId && x.PresetId == presetId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.BackgroundOnly, backgroundOnly)
                    .SetProperty(x => x.Color, color)
                    .SetProperty(x => x.Intensity, intensity));

            await dbContext.RoomDimmerSettings
                .Where(x => x.RoomId == roomId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Enabled, enabledAfterSave));
        };

        await room.RunLockedAsync(async () =>
        {
            var settings = room.Room.DimmerSettings;

            if (settings == null)
            {
                return;
            }

            await room.BroadcastDataAsync(new RoomDimmerSettingsWriter
            {
                DimmerSettings = settings,
                DimmerPresets = mapper.Map<List<RoomDimmerPresetDto>>(presets)
            });
        });
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
