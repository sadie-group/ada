using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Networking.Writers.Rooms.Users;

namespace Ada.Game.Rooms.Wired.Effects;

public class WiredEffectShowMessageStrategy : IWiredEffectStrategy
{
    public string InteractionType => FurnitureItemInteractionType.WiredEffectShowMessage;

    public async Task ExecuteAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered)
    {
        var message = effect.WiredData?.Message;

        if (userWhoTriggered == null || string.IsNullOrEmpty(message))
        {
            return;
        }

        await userWhoTriggered.NetworkObject.WriteToStreamAsync(new RoomUserWhisperWriter
        {
            SenderId = userWhoTriggered.Player.Player.Id,
            Message = message,
            EmotionId = 0,
            ChatBubbleId = (int)ChatBubble.Alert,
            MessageLength = message.Length,
            Urls = []
        });
    }
}
