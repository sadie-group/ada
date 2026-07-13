using Microsoft.EntityFrameworkCore;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players.Rooms;

namespace Ada.Networking.Events.Handlers.Players.Club;

[PacketId(EventHandlerId.PlayerSetHomeRoom)]
public class PlayerSetHomeRoomEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
{
    public int RoomId { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player?.NetworkObject == null ||
            client.Player.Player.Data.HomeRoomId == RoomId)
        {
            return;
        }

        await client.Player.NetworkObject.WriteToStreamAsync(new PlayerHomeRoomWriter
        {
            HomeRoom = RoomId,
            RoomIdToEnter = 0
        });
        
        client.Player.Player.Data.HomeRoomId = RoomId;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        dbContext
            .Entry(client.Player.Player.Data)
            .Property(x => x.HomeRoomId)
            .IsModified = true;
        
        await dbContext.SaveChangesAsync();
    }
}