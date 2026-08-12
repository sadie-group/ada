namespace Ada.API.Interfaces.Networking.Events.Handlers;

public interface IDefersPersistence
{
    Task PersistAsync();
}
