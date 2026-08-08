using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Writers.Rooms.Doorbell;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Doorbell;

[PacketId(EventHandlerId.RoomDoorbellAnswer)]
public class RoomDoorbellAnswerEventHandler(
    IPlayerRepository playerRepository,
    IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomUserFactory roomUserFactory,
    INetworkClientRepository clientRepository,
    IRoomTileMapHelperService tileMapHelperService,
    IPlayerHelperService playerHelperService,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService,
    IRoomWiredService wiredService,
    IMapper mapper) : INetworkPacketEventHandler
{
    public required string Username { get; init; }
    public bool Accept { get; init; }

    [RequiresRoomRights]
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _))
        {
            return;
        }
        
        var player = playerRepository.GetPlayerLogicByUsername(Username);

        if (player?.NetworkObject == null)
        {
            return;
        }

        if (player.State.PendingDoorbellRoomId != room.Room.Id)
        {
            return;
        }

        player.State.PendingDoorbellRoomId = 0;

        if (Accept)
        {
            if (!RoomHelpers.CanEnterRoom(room, player, out _))
            {
                await player.NetworkObject.WriteToStreamAsync(
                    new RoomDoorbellNoAnswerWriter { Username = Username });

                return;
            }

            await player.NetworkObject.WriteToStreamAsync(new RoomDoorbellAcceptWriter
            {
                Username = Username
            });

            var playerClient = clientRepository.TryGetClientByGuid(player.NetworkObject.Guid);

            if (playerClient != null)
            {
                await RoomEntryEventHelpers.GenericEnterRoomAsync(
                    playerClient, 
                    room, 
                    roomUserFactory, 
                    dbContextFactory, 
                    playerRepository,
                    tileMapHelperService,
                    playerHelperService,
                    roomFurnitureItemHelperService,
                    wiredService,
                    mapper);
            }
            
            return;
        }

        await player.NetworkObject.WriteToStreamAsync(new RoomDoorbellNoAnswerWriter { Username = Username });
    }
}