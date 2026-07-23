using Ada.API.DTOs;

namespace Ada.API.Interfaces.Game.Jukebox;

public interface ISoundTrackRepository
{
    Task<IReadOnlyList<SoundTrackDto>> GetByIdsAsync(IReadOnlyList<int> ids);
    Task<SoundTrackDto?> GetByNameAsync(string name);
}
