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
using Ada.Db.Models.Rooms;
using Ada.Networking.Writers.Navigator;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerCreateRoom)]
public class PlayerCreateRoomEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    ServerRoomConstants roomConstants,
    IWordFilterService wordFilterService,
    IMapper mapper) : INetworkPacketEventHandler
{
    private const int _maxUsersCeiling = 250;
    private const int _maxRoomsPerPlayer = 100;

    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string LayoutName { get; set; }
    public int CategoryId { get; set; }
    public int MaxUsersAllowed { get; set; }
    public int TradingPermission { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Name) ||
            MaxUsersAllowed < 1 ||
            MaxUsersAllowed > _maxUsersCeiling ||
            !Enum.IsDefined(typeof(RoomTradeOption), TradingPermission))
        {
            return;
        }

        var nameResult = wordFilterService.Filter(Name, WordFilterContext.RoomName);
        var descriptionResult = wordFilterService.Filter(Description, WordFilterContext.RoomDescription);

        if (nameResult.IsBlocked || nameResult.IsShadowBlocked ||
            descriptionResult.IsBlocked || descriptionResult.IsShadowBlocked)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var layout = await dbContext.RoomLayouts
            .FirstOrDefaultAsync(x => x.Name == LayoutName);

        if (layout == null)
        {
            return;
        }

        var ownerId = client.Player.Player.Id;

        if (await dbContext.Rooms.CountAsync(x => x.OwnerId == ownerId) >= _maxRoomsPerPlayer)
        {
            return;
        }

        var roomEntity = new Room
        {
            Name = nameResult.FilteredText.Truncate(roomConstants.MaxNameLength),
            OwnerId = ownerId,
            Description = descriptionResult.FilteredText.Truncate(roomConstants.MaxDescriptionLength),
            LayoutId = layout.Id,
            CreatedAt = DateTime.UtcNow,
            MaxUsersAllowed = MaxUsersAllowed,

            Settings = new RoomSettings
            {
                WalkDiagonal = true,
                TradeOption = (RoomTradeOption) TradingPermission
            },

            ChatSettings = new RoomChatSettings(),
            PaintSettings = new RoomPaintSettings()
        };

        dbContext.Rooms.Add(roomEntity);
        await dbContext.SaveChangesAsync();

        var roomDto = mapper.Map<RoomDto>(roomEntity);
        roomDto.Layout = mapper.Map<RoomLayoutDto>(layout);

        var roomLogic = mapper.Map<IRoomLogic>(roomDto);
        roomLogic.UserRepository.SetRoom(roomLogic);
        roomRepository.AddRoom(roomLogic);

        await client.WriteToStreamAsync(new RoomCreatedWriter
        {
            Id = roomDto.Id,
            Name = roomDto.Name
        });
    }
}
