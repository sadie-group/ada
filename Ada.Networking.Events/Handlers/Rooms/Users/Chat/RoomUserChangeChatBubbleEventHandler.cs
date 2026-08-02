using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Chat;

[PacketId(EventHandlerId.RoomUserChangeChatBubble)]
public class RoomUserChangeChatBubbleEventHandler : INetworkPacketEventHandler
{
    public int Bubble { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        var avatarData = client.Player.Player.AvatarData;

        if (avatarData == null)
        {
            return;
        }

        avatarData.ChatBubbleId = (ChatBubble) Bubble;
    }
}