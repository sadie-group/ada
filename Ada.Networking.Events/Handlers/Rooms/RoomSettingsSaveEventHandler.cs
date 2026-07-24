using Ada.Core.Shared.Security;
using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Extensions;
using Ada.Db;
using Ada.Db.Models.Constants;
using Ada.Networking.Writers.Rooms;

namespace Ada.Networking.Events.Handlers.Rooms;

[PacketId(EventHandlerId.RoomSettingsSave)]
public class RoomSettingsSaveEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    ServerRoomConstants roomConstants,
    IWordFilterService wordFilterService) : INetworkPacketEventHandler
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

        foreach (var tagResult in tagResults)
        {
            room.Room.Tags.Add(new RoomTagDto
            {
                Name = tagResult.FilteredText
            });
        }
        
        UpdateSettings(room.Room.Settings);
        UpdateChatSettings(room.Room.ChatSettings);
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.Entry(room).State = EntityState.Modified;
        dbContext.Entry(room.Room.Settings).State = EntityState.Modified;
        dbContext.Entry(room.Room.ChatSettings).State = EntityState.Modified;
        
        await dbContext.SaveChangesAsync();
        await BroadcastUpdatesAsync(room);
        
        await client.WriteToStreamAsync(new RoomSettingsSavedWriter
        {
            RoomId = RoomId
        });
    }

    private void UpdateSettings(RoomSettingsDto settings)
    {
        settings.AccessType = (RoomAccessType) AccessType;
        // Store room passwords hashed, never plaintext. Empty password (non-password rooms)
        // is left as-is so it can be cleared.
        settings.Password = string.IsNullOrEmpty(Password)
            ? Password
            : RoomPasswordHasher.Hash(Password);
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
}