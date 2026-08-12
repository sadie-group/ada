using Ada.API.DTOs.Jukebox;

namespace Ada.API.Interfaces.Game.Jukebox;

public interface IRoomJukeboxService
{
    int MaxTracksPerRoom { get; }

    Task<IReadOnlyList<RoomJukeboxTrackDto>> GetPlaylistAsync(int roomId);

    Task<bool> TryAddAsync(int roomId, long playerId, int playerFurnitureItemId);

    Task<int?> TryRemoveAsync(int roomId, int orderIndex);
}
