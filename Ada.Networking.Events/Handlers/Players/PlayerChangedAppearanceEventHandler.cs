using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Db;
using Ada.Networking.Writers.Players;
using Ada.Networking.Writers.Rooms.Users;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerChangedAppearance)]
public class PlayerChangedAppearanceEventHandler(
    IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
{
    public required string Gender { get; set; }
    public required string FigureCode { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;
        
        if (player?.Player.AvatarData == null)
        {
            return;
        }

        if (!AvatarHelpers.IsValidFigureCode(FigureCode))
        {
            return;
        }

        var gender = Gender == "M" ?
            PlayerAvatarGender.Male :
            PlayerAvatarGender.Female;

        var figureCode = FigureCode;

        player.Player.AvatarData.Gender = gender;
        player.Player.AvatarData.FigureCode = figureCode;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerAvatarData
            .Where(x => x.PlayerId == player.Player.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Gender, gender)
                .SetProperty(x => x.FigureCode, figureCode));
        
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }
        
        await client.WriteToStreamAsync(new PlayerChangedAppearanceWriter
        {
            FigureCode = figureCode,
            Gender = gender.ToString()
        });
        
        await room.BroadcastDataAsync(new RoomUserDataWriter
        {
            Users = [roomUser]
        });
    }
}