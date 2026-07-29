namespace Ada.API.Interfaces.Networking.Events.Handlers;

// Handlers that touch more than one room (e.g. switching rooms) opt out of the
// automatic current-room lock and take per-room locks sequentially themselves,
// so two room locks are never held at once.
public interface IManagesOwnRoomLock;
