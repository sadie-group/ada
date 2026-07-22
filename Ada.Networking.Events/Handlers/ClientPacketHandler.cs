using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Events.Handlers.Rooms.Users;
using Ada.Networking.Events.Handlers.Rooms.Users.Chat;
using Ada.Networking.Options;
using Ada.Networking.Packets;
using Ada.Networking.Writers.Generic;

namespace Ada.Networking.Events.Handlers;

public class ClientPacketHandler(
    ILogger<ClientPacketHandler> logger,
    Dictionary<short, Type> packetHandlerTypeMap,
    PacketHandlerFactory handlerFactory,
    IOptions<NetworkPacketOptions> packetOptions)
    : INetworkPacketHandler
{
    public async Task HandleAsync(INetworkClient client, INetworkPacket packet)
    {
        try
        {
            if (!packetHandlerTypeMap.TryGetValue(packet.PacketId, out var packetEventType))
            {
                if (packetOptions.Value.NotifyMissingPacket)
                {
                    _ = NotifyMissingPacketAsync(packet.PacketId, client);
                }
            
                logger.LogWarning($"Couldn't resolve packet event handler for header '{packet.PacketId}'");
                return;
            }

            var eventHandler = handlerFactory.Create(packet.PacketId);

            if (!ValidateAttributes(eventHandler, client))
            {
                _ = RejectAsync(eventHandler, client);
                return;
            }
            
            var packetReader = new NetworkPacketReader(packet.Data.Span);
            EventSerializer.SetPropertiesForEventHandler(eventHandler, packetReader);

            if (client.RoomUser != null &&
                (packetEventType == typeof(RoomUserWalkEventHandler) ||
                 packetEventType == typeof(RoomUserChatEventHandler) ||
                 packetEventType == typeof(RoomUserShoutEventHandler) ||
                 packetEventType == typeof(RoomUserActionEventHandler) ||
                 packetEventType == typeof(RoomUserDanceEventHandler) ||
                 packetEventType == typeof(RoomUserSignEventHandler) ||
                 packetEventType == typeof(RoomUserSitEventHandler) ||
                 packetEventType == typeof(RoomUserLookAtEventHandler)))
            {
                client.RoomUser.LastAction = DateTime.Now;
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

    private static bool ValidateAttributes(INetworkPacketEventHandler eventHandler,
        INetworkClient client)
    {
        var type = eventHandler.GetType();

        var method = type.GetMethods()
            .SingleOrDefault(x => x.Name == "HandleAsync");

        var allowsUnauthenticated = HasAttribute<AllowUnauthenticatedAttribute>(type, method);

        if (!allowsUnauthenticated && client.Player == null)
        {
            return false;
        }

        if (HasAttribute<RequiresRoomRightsAttribute>(type, method))
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
        logger.LogDebug($"Executing packet '{eventHandler.GetType().Name}'");
        
        try
        {
            await eventHandler.HandleAsync(client);
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
        }
    }
}