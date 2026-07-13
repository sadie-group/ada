using Microsoft.Extensions.DependencyInjection;

namespace Ada.API.Interfaces.Plugins;

public interface IPluginServiceCollection
{
    void Register(IServiceCollection serviceCollection);
}