using System.Collections.Concurrent;
using System.Reflection;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Filters;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Core.Shared.Extensions;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Events.Handlers.Rooms.Users;
using Ada.Networking.Events.Handlers.Rooms.Users.Chat;
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
    public async Task HandleAsync(INetworkClient client, INetworkPacket packet)
    {
        try
        {
            if (!client.Codec.IdMap.TryGetHandlerType(packet.PacketId, out var packetEventType))
            {
                if (packetOptions.Value.NotifyMissingPacket)
                {
                    NotifyMissingPacketAsync(packet.PacketId, client)
                        .FireAndForget(logger, "missing-packet notification");
                }

                logger.LogWarning($"Couldn't resolve packet event handler for header '{packet.PacketId}'");
                return;
            }

            foreach (var filter in preDispatchFilters)
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
                client.RoomUser.LastAction = DateTime.Now;
            }

            foreach (var filter in packetFilters)
            {
                if (await filter.AllowAsync(client, eventHandler))
                {
                    continue;
                }

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug($"Packet '{eventHandler.GetType().Name}' blocked by filter '{filter.GetType().Name}'");
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

    private static readonly ConcurrentDictionary<Type, HandlerAttributeFlags> AttributeFlagsCache = new();

    private readonly record struct HandlerAttributeFlags(bool AllowsUnauthenticated, bool RequiresRoomRights);

    private static bool ValidateAttributes(INetworkPacketEventHandler eventHandler,
        INetworkClient client)
    {
        var flags = AttributeFlagsCache.GetOrAdd(eventHandler.GetType(), static type =>
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
        logger.LogWarning($"Rejected packet '{eventHandler.GetType().Name}' (authenticated: {client.Player != null})");

        try
        {
            await client.WriteToStreamAsync(new GenericErrorWriter { ErrorCode = 1 });
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
        }
    }

    private async Task ExecuteAsync(INetworkClient client, INetworkPacketEventHandler eventHandler)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug($"Executing packet '{eventHandler.GetType().Name}'");
        }

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
            logger.LogError(e.ToString());
        }
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
