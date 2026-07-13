using AutoMapper;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms;

namespace Ada.Networking.Events.Handlers.Rooms;

[PacketId(EventHandlerId.RoomSettings)]
public class RequestRoomSettingsEventHandler(
    IRoomRepository roomRepository,
    IMapper mapper) : INetworkPacketEventHandler
{
    public int RoomId { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var room = roomRepository.TryGetRoomById(RoomId);
        
        if (room != null)
        {
            await client.WriteToStreamAsync(new RoomSettingsWriter
            {
                Room = mapper.Map<RoomDto>(room)
            });
        }
    }
}