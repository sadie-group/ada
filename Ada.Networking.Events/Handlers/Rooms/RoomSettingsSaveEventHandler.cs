using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Extensions;
using Ada.Core.Shared.Helpers;
using Ada.Db;
using Ada.Db.Models.Constants;
using Ada.Db.Models.Rooms;
using Ada.Networking.Writers.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms;

[PacketId(EventHandlerId.RoomSettingsSave)]
public class RoomSettingsSaveEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    ServerRoomConstants roomConstants,
    IWordFilterService wordFilterService) : INetworkPacketEventHandler, IDefersPersistence
{
    public long RoomId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int AccessType { get; init; }
    public required string Password { get; init; }
    public int MaxUsers { get; init; }
    public int CategoryId { get; init; }
    public List<string> Tags { get; init; } = [];
    public int TradeOption { get; init; }
    public bool AllowPets { get; init; }
    public bool CanPetsEat { get; init; }
    public bool CanUsersOverlap { get; init; }
    public bool HideWall { get; init; }
    public int WallSize { get; init; }
    public int FloorSize { get; init; }
    public int WhoCanMute { get; init; }
    public int WhoCanKick { get; init; }
    public int WhoCanBan { get; init; }
    public int ChatType { get; init; }
    public int ChatWeight { get; init; }
    public int ChatSpeed { get; init; }
    public int ChatDistance { get; init; }
    public int ChatProtection { get; init; }

    private const int _maxUsersCeiling = 250;
    private const int _maxTagCount = 20;
    private const int _maxPasswordLength = 64;
    private const int _maxThickness = 2;
    private const int _minThickness = -2;

    public async Task HandleAsync(INetworkClient client)
    {
        var room = roomRepository.TryGetRoomById(RoomId);

        if (room == null)
        {
            return;
        }

        if (room.Room.OwnerId != client.Player!.Player.Id)
        {
            return;
        }

        if (!Enum.IsDefined(typeof(RoomAccessType), AccessType) ||
            !Enum.IsDefined(typeof(RoomTradeOption), TradeOption) ||
            MaxUsers < 1 || MaxUsers > _maxUsersCeiling ||
            Tags.Count > _maxTagCount ||
            Password.Length > _maxPasswordLength ||
            WallSize < _minThickness || WallSize > _maxThickness ||
            FloorSize < _minThickness || FloorSize > _maxThickness)
        {
            await client.WriteToStreamAsync(new RoomSettingsErrorWriter
            {
                RoomId = room.Room.Id,
                ErrorCode = (int) RoomSettingsError.NameRequired,
                Message = ""
            });

            return;
        }

        if (Tags.Any(x => x.Length > roomConstants.MaxTagLength))
        {
            await client.WriteToStreamAsync(new RoomSettingsErrorWriter
            {
                RoomId = room.Room.Id,
                ErrorCode = (int)RoomSettingsError.TagTooLong,
                Message = ""
            });
            return;
        }

        if (string.IsNullOrEmpty(Name))
        {
            await client.WriteToStreamAsync(new RoomSettingsErrorWriter
            {
                RoomId = room.Room.Id,
                ErrorCode = (int)RoomSettingsError.NameRequired,
                Message = ""
            });
            return;
        }

        var nameResult = wordFilterService.Filter(Name, WordFilterContext.RoomName);

        if (nameResult.IsBlocked || nameResult.IsShadowBlocked)
        {
            await client.WriteToStreamAsync(new RoomSettingsErrorWriter
            {
                RoomId = room.Room.Id,
                ErrorCode = (int)RoomSettingsError.NameBadWords,
                Message = ""
            });
            return;
        }

        var descriptionResult = wordFilterService.Filter(Description, WordFilterContext.RoomDescription);

        if (descriptionResult.IsBlocked || descriptionResult.IsShadowBlocked)
        {
            await client.WriteToStreamAsync(new RoomSettingsErrorWriter
            {
                RoomId = room.Room.Id,
                ErrorCode = (int)RoomSettingsError.DescriptionBadWords,
                Message = ""
            });
            return;
        }

        var tagResults = Tags
            .Select(x => wordFilterService.Filter(x, WordFilterContext.RoomTag))
            .ToList();

        if (tagResults.Any(x => x.IsBlocked || x.IsShadowBlocked))
        {
            await client.WriteToStreamAsync(new RoomSettingsErrorWriter
            {
                RoomId = room.Room.Id,
                ErrorCode = (int)RoomSettingsError.TagsBadWords,
                Message = ""
            });
            return;
        }

        if (AccessType == (int) RoomAccessType.Password && string.IsNullOrEmpty(Password))
        {
            await client.WriteToStreamAsync(new RoomSettingsErrorWriter
            {
                RoomId = room.Room.Id,
                ErrorCode = (int) RoomSettingsError.PasswordRequired,
                Message = ""
            });

            return;
        }

        room.Room.Name = nameResult.FilteredText.Truncate(roomConstants.MaxNameLength);
        room.Room.Description = descriptionResult.FilteredText.Truncate(roomConstants.MaxDescriptionLength);
        room.Room.MaxUsersAllowed = MaxUsers;

        room.Room.Tags.Clear();

        foreach (var tagResult in tagResults)
        {
            room.Room.Tags.Add(new RoomTagDto
            {
                Name = tagResult.FilteredText
            });
        }

        var settings = room.Room.Settings;
        var chatSettings = room.Room.ChatSettings;

        if (settings == null || chatSettings == null)
        {
            return;
        }

        UpdateSettings(settings);
        UpdateChatSettings(chatSettings);

        var roomId = room.Room.Id;
        var tagNames = room.Room.Tags.Select(x => x.Name).ToList();

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.Rooms
                .Where(x => x.Id == room.Room.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Name, room.Room.Name)
                    .SetProperty(x => x.Description, room.Room.Description)
                    .SetProperty(x => x.MaxUsersAllowed, room.Room.MaxUsersAllowed));

            await dbContext.RoomSettings
                .Where(x => x.Id == settings.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.AccessType, settings.AccessType)
                    .SetProperty(x => x.Password, settings.Password)
                    .SetProperty(x => x.TradeOption, settings.TradeOption)
                    .SetProperty(x => x.AllowPets, settings.AllowPets)
                    .SetProperty(x => x.CanPetsEat, settings.CanPetsEat)
                    .SetProperty(x => x.CanUsersOverlap, settings.CanUsersOverlap)
                    .SetProperty(x => x.HideWalls, settings.HideWalls)
                    .SetProperty(x => x.WallThickness, settings.WallThickness)
                    .SetProperty(x => x.FloorThickness, settings.FloorThickness)
                    .SetProperty(x => x.WhoCanMute, settings.WhoCanMute)
                    .SetProperty(x => x.WhoCanKick, settings.WhoCanKick)
                    .SetProperty(x => x.WhoCanBan, settings.WhoCanBan));

            await dbContext.RoomChatSettings
                .Where(x => x.Id == chatSettings.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.ChatType, chatSettings.ChatType)
                    .SetProperty(x => x.ChatWeight, chatSettings.ChatWeight)
                    .SetProperty(x => x.ChatSpeed, chatSettings.ChatSpeed)
                    .SetProperty(x => x.ChatDistance, chatSettings.ChatDistance)
                    .SetProperty(x => x.ChatProtection, chatSettings.ChatProtection));

            await SaveTagsAsync(dbContext, roomId, tagNames);
        };

        await BroadcastUpdatesAsync(room);

        await client.WriteToStreamAsync(new RoomSettingsSavedWriter
        {
            RoomId = RoomId
        });
    }

    private static async Task SaveTagsAsync(AdaDbContext dbContext, int roomId, IEnumerable<string> tagNames)
    {
        var entity = await dbContext.Rooms
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.Id == roomId);

        if (entity == null)
        {
            return;
        }

        entity.Tags.Clear();

        foreach (var name in tagNames)
        {
            entity.Tags.Add(new RoomTag { Name = name });
        }

        await dbContext.SaveChangesAsync();
    }

    private void UpdateSettings(RoomSettingsDto settings)
    {
        settings.AccessType = (RoomAccessType) AccessType;
        settings.Password = string.IsNullOrEmpty(Password) ? "" : RoomPasswordHasher.Hash(Password);
        settings.TradeOption = (RoomTradeOption) TradeOption;
        settings.AllowPets = AllowPets;
        settings.CanPetsEat = CanPetsEat;
        settings.CanUsersOverlap = CanUsersOverlap;
        settings.HideWalls = HideWall;
        settings.WallThickness = WallSize;
        settings.FloorThickness = FloorSize;
        settings.WhoCanMute = WhoCanMute;
        settings.WhoCanKick = WhoCanKick;
        settings.WhoCanBan = WhoCanBan;
    }

    private void UpdateChatSettings(RoomChatSettingsDto chatSettings)
    {
        chatSettings.ChatType = ChatType;
        chatSettings.ChatWeight = ChatWeight;
        chatSettings.ChatSpeed = ChatSpeed;
        chatSettings.ChatDistance = ChatDistance;
        chatSettings.ChatProtection = ChatProtection;
    }
    private async Task BroadcastUpdatesAsync(IRoomLogic room)
    {
        var settings = room.Room.Settings;
        var chatSettings = room.Room.ChatSettings;

        if (settings == null || chatSettings == null)
        {
            return;
        }

        var floorSettingsWriter = new RoomWallFloorSettingsWriter
        {
            HideWalls = settings.HideWalls,
            WallThickness = settings.WallThickness,
            FloorThickness = settings.FloorThickness
        };

        var settingsWriter = new RoomChatSettingsWriter
        {
            ChatType = chatSettings.ChatType,
            ChatWeight = chatSettings.ChatWeight,
            ChatSpeed = chatSettings.ChatSpeed,
            ChatDistance = chatSettings.ChatDistance,
            ChatProtection = chatSettings.ChatProtection
        };

        var settingsUpdatedWriter = new RoomSettingsUpdatedWriter
        {
            RoomId = RoomId
        };

        await room.BroadcastDataAsync(floorSettingsWriter);
        await room.BroadcastDataAsync(settingsWriter);
        await room.BroadcastDataAsync(settingsUpdatedWriter);
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
