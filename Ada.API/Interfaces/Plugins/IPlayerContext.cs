namespace Ada.API.Interfaces.Plugins;

public interface IPluginContext : IAsyncDisposable
{
    Task BootstrapAsync();
}