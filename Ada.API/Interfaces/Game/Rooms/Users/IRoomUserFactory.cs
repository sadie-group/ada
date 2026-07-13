using System.Drawing;
using Ada.API.Interfaces.Game.Players;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Miscellaneous;

namespace Ada.API.Interfaces.Game.Rooms.Users;

public interface IRoomUserFactory
{
    IRoomUser Create(
        IRoomLogic room,
        INetworkObject networkObject, 
        Point point, 
        double pointZ,
        HDirection directionHead,
        HDirection direction, 
        IPlayerLogic player,
        RoomControllerLevel controllerLevel);
}