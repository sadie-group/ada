using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms.Unit;

namespace Ada.API.Interfaces.Game.Rooms.Bots;

public interface IRoomBot : IRoomUnit
{ 
    PlayerBotDto Bot { get; }
}