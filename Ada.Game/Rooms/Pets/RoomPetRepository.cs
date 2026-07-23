using System.Collections.Concurrent;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Serilog;

namespace Ada.Game.Rooms.Pets;

public class RoomPetRepository : IRoomPetRepository
{
    private readonly ConcurrentDictionary<int, IRoomPet> _pets = new();
    private readonly ConcurrentDictionary<int, (int PetOneId, int PetTwoId)> _breedingNests = new();

    public ICollection<IRoomPet> GetAll() => _pets.Values;
    public bool TryAdd(IRoomPet pet) => _pets.TryAdd(pet.Pet.Id, pet);
    public bool TryGetById(int id, out IRoomPet? pet) => _pets.TryGetValue(id, out pet);
    public bool TryRemove(int id, out IRoomPet? pet) => _pets.TryRemove(id, out pet);
    public int Count => _pets.Count;
    public bool Loaded { get; set; }

    public bool TryStartBreeding(int nestId, int petOneId, int petTwoId)
        => _breedingNests.TryAdd(nestId, (petOneId, petTwoId));

    public bool TryGetBreeding(int nestId, out (int PetOneId, int PetTwoId) pets)
        => _breedingNests.TryGetValue(nestId, out pets);

    public void StopBreeding(int nestId)
        => _breedingNests.TryRemove(nestId, out _);

    public async Task RunPeriodicCheckAsync()
    {
        try
        {
            var pets = _pets.Values;

            foreach (var pet in pets)
            {
                await pet.RunPeriodicCheckAsync();
            }
        }
        catch (Exception e)
        {
            Log.Logger.Error(e.ToString());
        }
    }

    public async ValueTask DisposeAsync()
    {
    }
}
