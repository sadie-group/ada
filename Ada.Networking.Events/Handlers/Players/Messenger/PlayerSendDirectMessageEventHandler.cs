using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Enums.Game.WordFilter;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Constants;
using Ada.Core.Shared.Extensions;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Networking.Writers.Players.Messenger;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Messenger;

[PacketId(EventHandlerId.PlayerSendDirectMessage)]
public class PlayerSendDirectMessageEventHandler(
    IPlayerRepository playerRepository,
    IWordFilterService wordFilterService,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper)
    : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int PlayerId { get; set; }
    public required string Message { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        if ((DateTime.UtcNow - client.Player.State.LastDirectMessage).TotalMilliseconds < CooldownIntervals.PlayerDirectMessage)
        {
            return;
        }
        
        client.Player.State.LastDirectMessage = DateTime.UtcNow;

        var playerId = PlayerId;
        var message = Message;

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        var filtered = wordFilterService.Filter(message, WordFilterContext.Whisper);

        if (filtered.IsBlocked)
        {
            return;
        }

        message = filtered.FilteredText.Truncate(500);

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (!client.Player.IsFriendsWith(PlayerId))
        {
            await client.WriteToStreamAsync(new PlayerMessageErrorWriter
            {
                Error = (int) PlayerMessageError.NotFriends,
                TargetId = playerId
            });
            
            return;
        }

        var targetPlayer = playerRepository.GetPlayerLogicById(playerId);

        if (targetPlayer?.NetworkObject == null)
        {
            return;
        }

        var playerMessage = new PlayerMessageDto
        {
            OriginPlayerId = client.Player.Player.Id,
            TargetPlayerId = targetPlayer.Player.Id,
            Message = message,
            CreatedAt = DateTime.Now
        };

        await targetPlayer.NetworkObject.WriteToStreamAsync(new PlayerDirectMessageWriter
        {
            Message = playerMessage
        });

        var entity = mapper.Map<PlayerMessage>(playerMessage);
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.PlayerMessages.Add(entity);
        await dbContext.SaveChangesAsync();
    }
}