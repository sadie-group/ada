using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Rooms.Furniture;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomDimmerSave)]
public class RoomDimmerSaveEventHandler(
    IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService,
    IMapper mapper) : INetworkPacketEventHandler
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

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var presets = dbContext
            .RoomDimmerPresets
            .Where(x => x.RoomId == room.Room.Id)
            .ToList();

        var preset = presets.FirstOrDefault(x => x.PresetId == PresetId);

        if (preset == null)
        {
            return;
        }

        preset.BackgroundOnly = BackgroundOnly == 2;
        preset.Color = Color;
        preset.Intensity = Intensity;

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

        await dbContext.RoomDimmerSettings
            .Where(x => x.RoomId == room.Room.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Enabled, dimmerSettings.Enabled));

        await dbContext.SaveChangesAsync();

        await room.BroadcastDataAsync(new RoomDimmerSettingsWriter
        {
            DimmerSettings = dimmerSettings,
            DimmerPresets = mapper.Map<List<RoomDimmerPresetDto>>(presets)
        });
    }
}
