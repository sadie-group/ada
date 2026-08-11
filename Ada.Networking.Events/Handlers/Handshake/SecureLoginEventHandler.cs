using System.Diagnostics;
using Ada.API.DTOs.Players;
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
using Ada.Networking.Options;
using Ada.Networking.Writers.Handshake;
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
    IOptions<NetworkOptions> networkOptions,
    ILoginAttemptThrottle loginThrottle,
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

        if (!client.EncryptionEnabled && !networkOptions.Value.AllowInsecureTransport)
        {
            logger.LogWarning(
                "Rejected login from {Ip}: the listener is plaintext, so the SSO token would cross the " +
                "wire in the clear. Set NetworkOptions:UseWss, or NetworkOptions:AllowInsecureTransport " +
                "to accept the risk on a trusted network.",
                client.IpAddress);

            await client.DisposeAsync();
            return;
        }

        if (string.IsNullOrEmpty(Token) || !ValidateSso(Token))
        {
            logger.LogWarning("Rejected an insecure sso token");
            await client.DisposeAsync();
            return;
        }

        if (!loginThrottle.TryConsume(client.IpAddress))
        {
            logger.LogWarning("Rejected login from {Ip}: too many recent attempts", client.IpAddress);
            await client.DisposeAsync();
            return;
        }

        var tokenRecord = await playerLoaderService.GetTokenAsync(Token);

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

        if (!await PassesBanChecksAsync(client, player, ipAddress))
        {
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
            logger.LogError("Player {Username} could not be registered", playerLogic.Player.Username);
            await client.DisposeAsync();
            return;
        }

        await client.WriteToStreamAsync(new SecureLoginWriter());

        if (playerLogic.Player.Data != null)
        {
            playerLogic.Player.Data.IsOnline = true;
            playerLogic.Player.Data.LastOnline = DateTime.Now;
        }

        playerLogic.Authenticated = true;

        client.Player = playerLogic;

        await playerLoginPacketService.SendAsync(client, playerLogic);
        await PlayerSubscriptionPacketHelper.SendAsync(playerLogic);

        await SendPostLoginNotificationsAsync(playerLogic, player);

        await NotifySessionListenersAsync(client, playerLogic, resumed);

        logger.LogInformation("Player {Username} logged in from {IpAddress} ({ElapsedMs}ms)", playerLogic.Player.Username, ipAddress, Math.Round(sw.Elapsed.TotalMilliseconds));
    }

    private async Task<bool> PassesBanChecksAsync(INetworkClient client, PlayerDto player, string ipAddress)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        if (await dbContext.BannedIpAddresses.AnyAsync(x =>
                x.IpAddress == ipAddress && (x.ExpiresAt == null || x.ExpiresAt >= DateTime.Now)))
        {
            logger.LogWarning("Disconnected banned IP {@Ip}", ipAddress);
            return false;
        }

        var machineId = client.MachineId;

        if (string.IsNullOrEmpty(machineId))
        {
            if (config.GetValue("PlayerOptions:RequireMachineId", true))
            {
                logger.LogWarning(
                    "Rejected login for {Username} from {Ip}: no machine fingerprint was sent, so the " +
                    "machine ban list cannot be enforced. Set PlayerOptions:RequireMachineId to false " +
                    "if this client does not send the UniqueID packet.",
                    player.Username, ipAddress);

                return false;
            }

            logger.LogWarning(
                "Login for {Username} from {Ip} has no machine fingerprint; machine bans are not " +
                "being enforced for this session.",
                player.Username, ipAddress);

            return true;
        }

        if (await dbContext.BannedMachines.AnyAsync(x =>
                x.MachineId == machineId && (x.ExpiresAt == null || x.ExpiresAt >= DateTime.Now)))
        {
            logger.LogWarning("Disconnected banned machine {@MachineId}", machineId);
            return false;
        }

        return true;
    }

    private async Task SendPostLoginNotificationsAsync(IPlayerLogic playerLogic, PlayerDto player)
    {
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
            logger.LogError(e, "Post-login notifications failed for {Username}; login stands.",
                playerLogic.Player.Username);
        }
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
            .Replace("[version]", GlobalState.Version?.ToString() ?? string.Empty);

        await player.SendAlertAsync(formattedMessage);
    }

    private bool ValidateSso(string sso) => sso.Length >= constants.MinSsoLength;
}
