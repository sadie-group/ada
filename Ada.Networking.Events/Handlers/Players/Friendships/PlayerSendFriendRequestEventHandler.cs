using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Constants;
using Ada.Db.Models.Players;
using Ada.Networking.Writers.Players.Friendships;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Friendships;

[PacketId(EventHandlerId.PlayerFriendRequests)]
public class PlayerSendFriendRequestEventHandler(
    IPlayerRepository playerRepository,
    ServerPlayerConstants playerConstants,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper)
    : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public string? TargetUsername { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || string.IsNullOrEmpty(TargetUsername))
        {
            return;
        }

        if (player.GetAcceptedFriendshipCount() >= playerConstants.MaxFriendships)
        {
            await client.WriteToStreamAsync(new PlayerFriendshipErrorWriter
            {
                ClientMessageId = 0,
                Error = (int) PlayerFriendshipError.TooManyFriends
            });

            return;
        }

        if (TargetUsername == player.Player.Username)
        {
            return;
        }

        PlayerDto? targetPlayer;
        var targetOnline = false;
        var onlineTarget = playerRepository.GetPlayerLogicByUsername(TargetUsername);

        if (onlineTarget != null)
        {
            targetPlayer = mapper.Map<PlayerDto>(onlineTarget);
            targetOnline = true;
        }
        else
        {
            targetPlayer = await playerRepository.GetPlayerByUsernameAsync(TargetUsername);
        }

        if (targetPlayer == null)
        {
            await client.WriteToStreamAsync(new PlayerFriendshipErrorWriter
            {
                ClientMessageId = 0,
                Error = (int) PlayerFriendshipError.TargetNotFound
            });
            return;
        }

        var acceptedFriends = await playerRepository.GetAcceptedFriendshipCountAsync(targetPlayer.Id);

        if (acceptedFriends >= playerConstants.MaxFriendships)
        {
            await client.WriteToStreamAsync(new PlayerFriendshipErrorWriter
            {
                ClientMessageId = 0,
                Error = (int) PlayerFriendshipError.TargetTooManyFriends
            });
            return;
        }

        if (targetPlayer.Data is not { AllowFriendRequests: true })
        {
            await client.WriteToStreamAsync(new PlayerFriendshipErrorWriter
            {
                ClientMessageId = 0,
                Error = (int) PlayerFriendshipError.TargetNotAccepting
            });
            return;
        }

        var existingRequest = player
            .Player.IncomingFriendships
            .FirstOrDefault(x => x.OriginPlayerId == targetPlayer.Id);

        if (existingRequest is { Status: PlayerFriendshipStatus.Pending })
        {
            await AcceptPendingAsync(
                existingRequest,
                targetOnline,
                onlineTarget,
                player.Player.Id);

            return;
        }

        await SendRequestAsync(
            mapper.Map<PlayerDto>(player),
            targetPlayer,
            targetOnline,
            onlineTarget);
    }

    private async Task AcceptPendingAsync(
        PlayerFriendshipDto incomingRequest,
        bool targetOnline,
        IPlayerLogic? onlineTarget,
        long playerId)
    {
        if (incomingRequest.Status != PlayerFriendshipStatus.Pending)
        {
            return;
        }

        incomingRequest.Status = PlayerFriendshipStatus.Accepted;

        if (targetOnline && onlineTarget != null)
        {
            var targetRequest = onlineTarget
                .Player
                .OutgoingFriendships
                .FirstOrDefault(x => x.TargetPlayerId == playerId);

            if (targetRequest != null)
            {
                targetRequest.Status = PlayerFriendshipStatus.Accepted;
            }
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.SaveChangesAsync();
    }

    private async Task SendRequestAsync(
        PlayerDto player,
        PlayerDto targetPlayer,
        bool targetOnline,
        IPlayerLogic? onlineTarget)
    {
        var playerFriendship = new PlayerFriendshipDto
        {
            OriginPlayerId = player.Id,
            TargetPlayerId = targetPlayer.Id,
            Status = PlayerFriendshipStatus.Pending,
            CreatedAt = DateTime.Now
        };

        player.OutgoingFriendships.Add(playerFriendship);

        if (targetOnline && onlineTarget != null)
        {
            var friendRequestWriter = new PlayerFriendRequestWriter
            {
                Id = player.Id,
                Username = player.Username,
                FigureCode = player.AvatarData?.FigureCode ?? string.Empty
            };

            onlineTarget.Player.IncomingFriendships.Add(playerFriendship);

            if (onlineTarget.NetworkObject != null)
            {
                await onlineTarget.NetworkObject.WriteToStreamAsync(friendRequestWriter);
            }
        }

        var entity = mapper.Map<PlayerFriendship>(playerFriendship);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.Set<PlayerFriendship>().Add(entity);
        await dbContext.SaveChangesAsync();
    }
}
