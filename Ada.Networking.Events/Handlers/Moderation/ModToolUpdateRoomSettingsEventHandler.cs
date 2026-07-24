using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsUpdateRoomSettings)]
public class ModToolUpdateRoomSettingsEventHandler(
    IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
{
    public int RoomId { get; set; }
    public int LockDoor { get; set; }
    public int ChangeTitle { get; set; }
    public int KickUsers { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null ||
            !client.Player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var room = roomRepository.TryGetRoomById(RoomId);

        if (room == null)
        {
            return;
        }
        
        var needsSaving = false;
        
        if (LockDoor == 1)
        {
            room.Room.Settings.AccessType = RoomAccessType.Doorbell;
            needsSaving = true;
        }

        if (ChangeTitle == 1)
        {
            room.Room.Name = "Inappropriate to hotel management.";
            needsSaving = true;
        }

        if (KickUsers == 1)
        {
            foreach (var user in room.UserRepository.GetAll())
            {
                await room.UserRepository.TryRemoveAsync(user.Player.Player.Id, true, true);
            }
        }

        if (needsSaving)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            dbContext.Entry(room).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
        }
    }
}