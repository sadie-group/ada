using Microsoft.Extensions.DependencyInjection;
using Ada.API.Interfaces.Game.Jukebox;

namespace Ada.Game.Jukebox;

public static class JukeboxServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ISoundTrackRepository, SoundTrackRepository>();
    }
}
