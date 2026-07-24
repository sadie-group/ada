using Ada.API.Interfaces.Game.Jukebox;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Game.Jukebox;

public static class JukeboxServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ISoundTrackRepository, SoundTrackRepository>();
    }
}
