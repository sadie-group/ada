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
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler, IDefersPersistence
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
            if (room.Room.Settings != null)
            {
                room.Room.Settings.AccessType = RoomAccessType.Doorbell;
            }
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
            _persist = async () =>
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync();

                await dbContext.Rooms
                    .Where(x => x.Id == room.Room.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Name, room.Room.Name));
            };
        }
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
