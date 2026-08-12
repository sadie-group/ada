using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Filters;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Core.Shared.Extensions;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Options;
using Ada.Networking.Packets;
using Ada.Networking.Writers.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ada.Networking.Events.Handlers;

public class ClientPacketHandler(
    ILogger<ClientPacketHandler> logger,
    PacketHandlerFactory handlerFactory,
    IOptions<NetworkPacketOptions> packetOptions,
    IEnumerable<INetworkPacketEventFilter> packetFilters,
    IEnumerable<IPreDispatchPacketFilter> preDispatchFilters)
    : INetworkPacketHandler
{
    private readonly INetworkPacketEventFilter[] _packetFilters = packetFilters.ToArray();
    private readonly IPreDispatchPacketFilter[] _preDispatchFilters = preDispatchFilters.ToArray();
    private static readonly ConcurrentDictionary<short, byte> _reportedUnknownHeaders = new();

    public async Task HandleAsync(INetworkClient client, INetworkPacket packet)
    {
        try
        {
            if (!client.Codec.IdMap.TryGetHandlerType(packet.PacketId, out var packetEventType))
            {
                HandleUnknownHeader(packet.PacketId, client);
                return;
            }

            foreach (var filter in _preDispatchFilters)
            {
                if (!filter.Allow(client, packet.PacketId, packetEventType))
                {
                    return;
                }
            }

            var eventHandler = handlerFactory.Create(packetEventType);

            if (!ValidateAttributes(eventHandler, client))
            {
                RejectAsync(eventHandler, client)
                    .FireAndForget(logger, "packet rejection notice");
                return;
            }

            var packetReader = client.Codec.CreateReader(packet.Data);

            try
            {
                EventSerializer.SetPropertiesForEventHandler(eventHandler, packetReader);
            }
            catch (MalformedPacketException e)
            {
                logger.LogWarning(
                    $"Discarded malformed packet '{packet.PacketId}' for handler " +
                    $"'{eventHandler.GetType().Name}' from {client.IpAddress}: {e.Message}");
                return;
            }

            if (client.RoomUser != null && eventHandler is ICountsAsRoomActivity)
            {
                client.RoomUser.LastAction = DateTime.UtcNow;
            }

            foreach (var filter in _packetFilters)
            {
                if (await filter.AllowAsync(client, eventHandler))
                {
                    continue;
                }

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Packet {Packet} blocked by filter {Filter}", eventHandler.GetType().Name, filter.GetType().Name);
                }
                return;
            }

            await ExecuteAsync(client, eventHandler);
        }
        catch (Exception e)
        {
            logger.LogCritical(e.ToString());
        }
    }

    private void HandleUnknownHeader(short header, INetworkClient client)
    {
        if (packetOptions.Value.NotifyMissingPacket && client.Player != null)
        {
            NotifyMissingPacketAsync(header, client)
                .FireAndForget(logger, "missing-packet notification");
        }

        if (_reportedUnknownHeaders.TryAdd(header, 0))
        {
            logger.LogWarning("Couldn't resolve packet event handler for header {Header}", header);
            return;
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Couldn't resolve packet event handler for header {Header}", header);
        }
    }

    private async Task NotifyMissingPacketAsync(int messageId, INetworkClient client)
    {
        try
        {
            var writer = new ServerErrorWriter
            {
                MessageId = messageId,
                ErrorCode = 1,
                DateTime = DateTime.Now.ToString("M/d/yy, h:mm tt")
            };

            await client.WriteToStreamAsync(writer);
        }
        catch (Exception e)
        {
            logger.LogCritical(e.ToString());
        }
    }

    private static readonly ConcurrentDictionary<Type, HandlerAttributeFlags> _attributeFlagsCache = new();

    private readonly record struct HandlerAttributeFlags(bool AllowsUnauthenticated, bool RequiresRoomRights);

    private static bool ValidateAttributes(INetworkPacketEventHandler eventHandler,
        INetworkClient client)
    {
        var flags = _attributeFlagsCache.GetOrAdd(eventHandler.GetType(), static type =>
        {
            var method = type.GetMethods()
                .SingleOrDefault(x => x.Name == "HandleAsync");

            return new HandlerAttributeFlags(
                HasAttribute<AllowUnauthenticatedAttribute>(type, method),
                HasAttribute<RequiresRoomRightsAttribute>(type, method));
        });

        if (!flags.AllowsUnauthenticated && client.Player == null)
        {
            return false;
        }

        if (flags.RequiresRoomRights)
        {
            return client.RoomUser != null && client.RoomUser.HasRights();
        }

        return true;
    }

    private static bool HasAttribute<T>(Type type, MemberInfo? method) where T : Attribute
    {
        return method?.GetCustomAttributes(typeof(T), true).FirstOrDefault() != null ||
               type.GetCustomAttributes(typeof(T), true).FirstOrDefault() != null;
    }

    private async Task RejectAsync(INetworkPacketEventHandler eventHandler, INetworkClient client)
    {
        logger.LogWarning("Rejected packet {Packet} (authenticated: {Authenticated})", eventHandler.GetType().Name, client.Player != null);

        try
        {
            await client.WriteToStreamAsync(new GenericErrorWriter { ErrorCode = 1 });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send rejection for packet {Packet}", eventHandler.GetType().Name);
        }
    }

    private async Task ExecuteAsync(INetworkClient client, INetworkPacketEventHandler eventHandler)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Executing packet {Packet}", eventHandler.GetType().Name);
        }

        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var room = eventHandler is IManagesOwnRoomLock or IRunsOutsideRoomLock
                ? null
                : client.RoomUser?.Room;

            if (room != null)
            {
                await room.RunLockedAsync(() => eventHandler.HandleAsync(client));
            }
            else
            {
                await eventHandler.HandleAsync(client);
            }

            if (eventHandler is IDefersPersistence deferred)
            {
                await PersistAsync(deferred);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception executing packet {Packet}", eventHandler.GetType().Name);
        }
        finally
        {
            WarnIfSlow(eventHandler, startedAt);
        }
    }

    private const int _slowHandlerThresholdMilliseconds = 1_000;

    private void WarnIfSlow(INetworkPacketEventHandler eventHandler, long startedAt)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        if (elapsed.TotalMilliseconds < _slowHandlerThresholdMilliseconds)
        {
            return;
        }

        logger.LogWarning(
            "Handler '{Handler}' took {ElapsedMs}ms",
            eventHandler.GetType().Name,
            (long) elapsed.TotalMilliseconds);
    }

    private async Task PersistAsync(IDefersPersistence deferred)
    {
        try
        {
            await deferred.PersistAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e,
                "Deferred persistence failed for packet '{Handler}'; the change is live in memory but was not stored",
                deferred.GetType().Name);
        }
    }
}
