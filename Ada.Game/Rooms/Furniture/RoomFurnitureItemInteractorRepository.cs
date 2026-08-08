using Ada.API.Interfaces.Game.Rooms.Furniture;

namespace Ada.Game.Rooms.Furniture;

public class RoomFurnitureItemInteractorRepository : IRoomFurnitureItemInteractorRepository
{
    private static readonly IRoomFurnitureItemInteractor[] None = [];

    private readonly Dictionary<string, IRoomFurnitureItemInteractor[]> _interactorsByType;

    public RoomFurnitureItemInteractorRepository(IEnumerable<IRoomFurnitureItemInteractor> interactors)
    {
        var byType = new Dictionary<string, List<IRoomFurnitureItemInteractor>>(StringComparer.Ordinal);

        foreach (var interactor in interactors)
        {
            foreach (var interactionType in interactor.InteractionTypes)
            {
                if (!byType.TryGetValue(interactionType, out var forType))
                {
                    byType[interactionType] = forType = [];
                }

                forType.Add(interactor);
            }
        }

        _interactorsByType = byType.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.Ordinal);
    }

    public ICollection<IRoomFurnitureItemInteractor> GetInteractorsForType(string interactionType) =>
        _interactorsByType.GetValueOrDefault(interactionType, None);
}
