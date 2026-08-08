using Ada.Db;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.Users;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms;

[PacketId(EventHandlerId.RoomForwardData)]
public class RoomForwardDataEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,IRoomRepository roomRepository,
    IMapper mapper,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    public int RoomId { get; init; }
    public int EnterRoom { get; init; }
    public int ForwardRoom { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var room = await RoomHelpers.TryLoadRoomByIdAsync(
            RoomId, 
            roomRepository, 
            dbContextFactory, 
            mapper);
        
        if (room == null)
        {
            return;
        }

        if (client.Player?.Player.Data == null)
        {
            return;
        }

        var isOwner = room.Room.OwnerId == client.Player.Player.Id;
        var ownerUsername = await playerRepository.GetPlayerUsernameByIdAsync(room.Room.OwnerId);

        await client.WriteToStreamAsync(new RoomForwardDataWriter
        {
            Room = room.Room,
            RoomForward = true,
            EnterRoom = EnterRoom != 0 || ForwardRoom != 1,
            IsOwner = isOwner,
            UsersNow = room.UserRepository.Count,
            OwnerUsername = ownerUsername ?? string.Empty
        });
    }
}