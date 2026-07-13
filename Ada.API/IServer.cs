namespace Ada.API;

public interface IServer : IAsyncDisposable
{
    Task RunAsync(CancellationToken token);
}