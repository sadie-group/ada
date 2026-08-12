using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;

namespace Ada.API.Interfaces.Game.Rooms.Services.Wired;

public interface IWiredEffectStrategy
{
    string InteractionType { get; }

    Task ExecuteAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered);
}
