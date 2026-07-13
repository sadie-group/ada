using System.Drawing;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Unit;
using Ada.Core.Enums.Game.Rooms;

namespace Ada.API.Interfaces.Game.Rooms.Users;

public interface IRoomUser : IRoomUnit, IAsyncDisposable
{
    IPlayerLogic Player { get; }
    DateTime LastAction { get; set; }
    TimeSpan IdleTime { get; }
    bool IsIdle { get; set; }
    bool MoonWalking { get; set; }
    IRoomUserTrade? Trade { get; set; }
    int TradeStatus { get; set; }
    int ActiveEffectId { get; set; }
    IRoomLogic Room { get; }
    RoomControllerLevel ControllerLevel { get; set; }
    INetworkObject NetworkObject { get; }
    void LookAtPoint(Point point);
    void ApplyFlatCtrlStatus();
    void CheckStatusForCurrentTile();
    bool HasRights();
    Task SendWhisperAsync(string message);
    DateTime SignSet { get; set; }
}