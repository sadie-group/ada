using System.Diagnostics;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.API.Interfaces.Plugins;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Constants;
using Ada.Db.Models.Server;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Writers.Handshake;
using Ada.Options.Options;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ada.Networking.Events.Handlers.Handshake;

[PacketId(EventHandlerId.SecureLogin)]
[AllowUnauthenticated]
public class SecureLoginEventHandler(
    ILogger<SecureLoginEventHandler> logger,
    IOptions<EncryptionOptions> encryptionOptions,
    IPlayerRepository playerRepository,
    ServerPlayerConstants constants,
    INetworkClientRepository networkClientRepository,
    ServerSettings serverSettings,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper,
    IPlayerLoaderService playerLoaderService,
    IPlayerHelperService playerHelperService,
    IConfiguration config,
    PlayerLoginPacketService playerLoginPacketService,
    IPlayerSessionResumeService sessionResumeService,
    IEnumerable<IPlayerSessionListener> sessionListeners)
    : INetworkPacketEventHandler
{
    public string? Token { get; set; }
    public int DelayMs { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var sw = Stopwatch.StartNew();

        if (DelayMs >= config.GetValue("PlayerOptions:MaxSsoDelayMs", 300_000))
        {
            await client.DisposeAsync();
            return;
        }

        if (string.IsNullOrEmpty(Token) || !ValidateSso(Token))
        {
            logger.LogWarning("Rejected an insecure sso token");
            await client.DisposeAsync();
            return;
        }
        
        if (encryptionOptions.Value.Enabled && !client.EncryptionEnabled)
        {
            logger.LogWarning("Encryption is enabled and TLS Handshake isn't finished.");
            await client.DisposeAsync();
            return;
        }

        var tokenRecord = await playerLoaderService.GetTokenAsync(Token, DelayMs);
        
        if (tokenRecord == null)
        {
            logger.LogWarning("Failed to find token record for provided sso.");
            await client.DisposeAsync();
            return;
        }
        
        var player = await playerRepository.GetPlayerByIdAsync(tokenRecord.PlayerId);

        if (player?.Data == null ||
            player.AvatarData == null ||
            player.NavigatorSettings == null ||
            player.GameSettings == null)
        {
            logger.LogError("Player record is missing required associated data.");
            await client.DisposeAsync();
            return;
        }
        
        if (player.Bans.Any(x => x.ExpiresAt == null || x.ExpiresAt >= DateTime.Now))
        {
            logger.LogWarning("Disconnected banned player {@PlayerUsername}", player.Username);
            await client.DisposeAsync();
            return;
        }

        var ipAddress = client.IpAddress.ToString();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        if (dbContext.BannedIpAddresses.Any(x => x.IpAddress == ipAddress && (x.ExpiresAt == null || x.ExpiresAt >= DateTime.Now)))
        {
            logger.LogWarning("Disconnected banned IP {@Ip}", ipAddress);
            await client.DisposeAsync();
            return;
        }
        
        var playerLogic = mapper.Map<IPlayerLogic>(player);

        playerLogic.NetworkObject = client;

        var playerId = player.Id;
        var existingPlayer = playerRepository.GetPlayerLogicById(playerId);
        var resumed = false;

        if (existingPlayer != null && await sessionResumeService.TryResumeAsync(existingPlayer, client))
        {
            playerLogic = existingPlayer;
            playerLogic.NetworkObject = client;
            resumed = true;
        }
        else if (existingPlayer?.NetworkObject != null)
        {
            await networkClientRepository.TryRemoveAsync(existingPlayer.NetworkObject.Guid);
        }

        if (!resumed && !playerRepository.TryAddPlayer(playerLogic))
        {
            logger.LogError($"Player {playerLogic.Player.Username} could not be registered");
            await client.DisposeAsync();
            return;
        }
        
        await client.WriteToStreamAsync(new SecureLoginWriter());
        
        playerLogic.Player.Data.IsOnline = true;
        playerLogic.Player.Data.LastOnline = DateTime.Now;
        
        playerLogic.Authenticated = true;

        client.Player = playerLogic;

        await playerLoginPacketService.SendAsync(client, playerLogic);
        await PlayerSubscriptionPacketHelper.SendAsync(playerLogic);

        try
        {
            await playerHelperService.SendPlayerFriendListUpdate(playerLogic, playerRepository);

            var playersFriends = player.OutgoingFriendships
                .Concat(player.IncomingFriendships)
                .Where(x => x.Status == PlayerFriendshipStatus.Accepted);

            await playerHelperService.UpdatePlayerStatusForFriendsAsync(
                playerLogic,
                playersFriends,
                true,
                false,
                playerRepository);

            await SendWelcomeMessageAsync(playerLogic);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Post-login notifications failed for {Username}; login stands.", playerLogic.Player.Username);
        }

        client.Player = playerLogic;

        await NotifySessionListenersAsync(client, playerLogic, resumed);

        logger.LogInformation($"Player '{playerLogic.Player.Username}' has logged in from {ipAddress} ({Math.Round(sw.Elapsed.TotalMilliseconds)}ms)");
    }

    private async Task NotifySessionListenersAsync(INetworkClient client, IPlayerLogic player, bool resumed)
    {
        foreach (var listener in sessionListeners)
        {
            try
            {
                await listener.OnLoginAsync(client, player, resumed);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Session listener {Listener} failed on login", listener.GetType().Name);
            }
        }
    }

    private async Task SendWelcomeMessageAsync(IPlayerLogic player)
    {
        if (string.IsNullOrEmpty(serverSettings.PlayerWelcomeMessage))
        {
            return;
        }

        var formattedMessage = serverSettings.PlayerWelcomeMessage
            .Replace("[username]", player.Player.Username)
            .Replace("[version]", GlobalState.Version.ToString());

        await player.SendAlertAsync(formattedMessage);
    }

    private bool ValidateSso(string sso) => sso.Length >= constants.MinSsoLength;
}