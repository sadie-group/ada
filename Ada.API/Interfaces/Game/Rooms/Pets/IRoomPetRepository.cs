namespace Ada.API.Interfaces.Game.Rooms.Pets;

public interface IRoomPetRepository : IAsyncDisposable
{
    ICollection<IRoomPet> GetAll();
    bool TryAdd(IRoomPet pet);
    bool TryGetById(int id, out IRoomPet? pet);
    bool TryRemove(int id, out IRoomPet? pet);
    int Count { get; }
    bool Loaded { get; set; }
    Task RunPeriodicCheckAsync();
    bool TryStartBreeding(int nestId, int petOneId, int petTwoId);
    bool TryGetBreeding(int nestId, out (int PetOneId, int PetTwoId) pets);
    void StopBreeding(int nestId);
}
