using Ada.API;
using Ada.Core.Enums.Game.Rooms.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Helpers;
using Ada.Networking.Writers.Generic;

namespace Ada.Networking.Events;

public static class FurniturePlacementErrorSender
{
    public static async Task SendAsync(
        INetworkObject client,
        RoomFurniturePlacementError error)
    {
        await client.WriteToStreamAsync(new BubbleAlertWriter
        {
            Key = EnumHelpers.GetEnumDescription(
                NotificationType.FurniturePlacementError),
            Messages = new Dictionary<string, string>
            {
                { "message", error.ToString() }
            }
        });
    }
}