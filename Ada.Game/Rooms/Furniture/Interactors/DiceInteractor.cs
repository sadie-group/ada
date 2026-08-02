using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;

namespace Ada.Game.Rooms.Furniture.Interactors;

public class DiceInteractor(
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService)
    : AbstractRoomFurnitureItemInteractor
{
    private const int _rollDelayMilliseconds = 1500;

    public override List<string> InteractionTypes => ["dice"];

    public override async Task OnTriggerAsync(IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto item,
        IRoomUser roomUser)
    {
        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room,
            item, "-1");

        using (ExecutionContext.SuppressFlow())
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(_rollDelayMilliseconds);

                await room.RunLockedAsync(() =>
                    roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(
                        room,
                        item,
                        Random.Shared.Next(1, 6).ToString()));
            });
        }
    }
}
