using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking.Packets;

namespace Ada.API.Interfaces.Networking.Client;

public interface INetworkClient : IAsyncDisposable, INetworkObject
{
    IPacketCodec Codec { get; set; }
    IPlayerLogic? Player { get; set; }
    IRoomUser? RoomUser { get; set; }
    string? MachineId { get; set; }
    bool EncryptionEnabled { get; }
    void EnableEncryption(byte[] sharedKey);
    DateTime LastPing { get; set; }
    DateTime LastPong { get; set; }
}