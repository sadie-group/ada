namespace Ada.API.Interfaces.Game.Rooms.Users;

public interface IRoomUserRepository : IAsyncDisposable
{
    ICollection<IRoomUser> GetAll();
    bool TryAdd(IRoomUser user);
    bool TryGetById(long id, out IRoomUser? user);
    bool TryGetByUsername(string username, out IRoomUser? user);
    Task TryRemoveAsync(long id, bool notifyLeft = true, bool hotelView = false);
    int Count { get; }
    ICollection<IRoomUser> GetAllWithRights();
    Task RunPeriodicCheckAsync();
    Task ProcessNewWalkRequestsAsync();
    void SetRoom(IRoomLogic room);
    DateTime? NoUsersSince { get; set; }
}