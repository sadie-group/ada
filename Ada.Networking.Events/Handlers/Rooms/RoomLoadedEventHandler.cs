using Ada.Db;
using System.Security.Cryptography;
using System.Text;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Generic;
using Ada.Networking.Writers.Rooms;
using Ada.Networking.Writers.Rooms.Doorbell;
using Ada.Networking.Writers.Rooms.Users;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Events.Handlers.Rooms;

[PacketId(EventHandlerId.RoomLoaded)]
public class RoomLoadedEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    ILogger<RoomLoadedEventHandler> logger,
    IRoomRepository roomRepository,
    IRoomUserFactory roomUserFactory,
    IPlayerRepository playerRepository,
    IMapper mapper,
    IRoomTileMapHelperService tileMapHelperService,
    IPlayerHelperService playerHelperService,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService,
    IRoomWiredService wiredService,
    IRoomAccessThrottle roomAccessThrottle)
    : INetworkPacketEventHandler, IManagesOwnRoomLock
{
    public int RoomId { get; init; }
    public required string Password { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var room = await RoomHelpers.TryLoadRoomByIdAsync(
            RoomId,
            roomRepository,
            dbContextFactory,
            mapper);

        var lastRoomId = player.State.CurrentRoomId;

        if (lastRoomId != 0)
        {
            var lastRoom = await RoomHelpers.TryLoadRoomByIdAsync(lastRoomId,
                roomRepository,
                dbContextFactory,
                mapper);

            if (lastRoom != null && lastRoom.UserRepository.TryGetById(player.Player.Id, out var existingUser) && existingUser != null)
            {
                await lastRoom.RunLockedAsync(async () =>
                    await lastRoom.UserRepository.TryRemoveAsync(existingUser.Player.Player.Id));
            }
        }

        if (room == null)
        {
            logger.LogError($"Failed to load room {RoomId} for player '{player.Player.Username}'");
            await client.WriteToStreamAsync(new RoomUserHotelViewWriter());

            return;
        }

        var isOwner = room.Room.OwnerId == player.Player.Id;

        if (!RoomHelpers.CanEnterRoom(room, player, out var enterError))
        {
            await client.WriteToStreamAsync(new RoomEnterErrorWriter
            {
                ErrorCode = (int) enterError
            });

            if (enterError == RoomEnterError.Banned)
            {
                await client.WriteToStreamAsync(new RoomUserHotelViewWriter());
            }

            return;
        }

        if (room.Room.Settings?.AccessType is RoomAccessType.Doorbell or RoomAccessType.Password &&
            !isOwner &&
            !await ValidateRoomAccessForClientAsync(client, room, Password))
        {
            return;
        }

        await RoomEntryEventHelpers.GenericEnterRoomAsync(
            client,
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

    private async Task<bool> ValidateRoomAccessForClientAsync(INetworkClient client, IRoomLogic room, string password)
    {
        var player = client.Player!;
        var settings = room.Room.Settings;

        if (settings == null)
        {
            return false;
        }

        switch (settings.AccessType)
        {
            case RoomAccessType.Password:
                if (!roomAccessThrottle.TryConsume(player.Player.Id, room.Room.Id))
                {
                    await client.WriteToStreamAsync(new RoomUserHotelViewWriter());
                    return false;
                }

                if (CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(settings.Password ?? string.Empty),
                        Encoding.UTF8.GetBytes(password)))
                {
                    return true;
                }

                await client.WriteToStreamAsync(new GenericErrorWriter
                {
                    ErrorCode = (int) GenericErrorCode.NavigatorInvalidPassword
                });

                await client.WriteToStreamAsync(new RoomUserHotelViewWriter());
                return false;

            case RoomAccessType.Doorbell:
            {
                var usersWithRights = room.UserRepository.GetAllWithRights();

                if (usersWithRights.Count < 1)
                {
                    await client.WriteToStreamAsync(new RoomDoorbellNoAnswerWriter
                    {
                        Username = player.Player.Username
                    });

                    return false;
                }

                player.State.PendingDoorbellRoomId = room.Room.Id;

                foreach (var user in usersWithRights)
                {
                    await user.NetworkObject.WriteToStreamAsync(new RoomDoorbellWriter
                    {
                        Username = player.Player.Username
                    });
                }

                await client.WriteToStreamAsync(new RoomDoorbellWriter
                {
                    Username = ""
                });

                return false;
            }
            case RoomAccessType.Open:
            case RoomAccessType.Invisible:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return true;
    }
}
