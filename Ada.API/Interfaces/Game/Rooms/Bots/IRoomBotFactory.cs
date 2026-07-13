using System.Drawing;

namespace Ada.API.Interfaces.Game.Rooms.Bots;

public interface IRoomBotFactory
{
    IRoomBot Create(
        IRoomLogic room,
        int id, 
        Point point,
        double pointZ);
}