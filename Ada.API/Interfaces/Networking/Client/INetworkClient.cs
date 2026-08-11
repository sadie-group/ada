using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Users;

namespace Ada.API.Interfaces.Networking.Client;

public interface INetworkClient : IAsyncDisposable, INetworkObject
{
    IPlayerLogic? Player { get; set; }
    IRoomUser? RoomUser { get; set; }
    string? MachineId { get; set; }
    bool EncryptionEnabled { get; }

    bool TryApplyNegotiatedKey(byte[] sharedKey);
    DateTime LastPing { get; set; }
    DateTime LastPong { get; set; }
}
