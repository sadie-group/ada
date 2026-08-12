using Ada.API.DTOs.Players.Furniture;

namespace Ada.Networking.Events.Handlers.Rooms.StickyNotes;

public static class StickyNotes
{
    private const int _maxContentsLength = 684;

    private const string _stickyNoteType = "post_it";

    public static bool IsStickyNote(PlayerFurnitureItemPlacementDataDto item)
        => item.PlayerFurnitureItem.FurnitureItem.InteractionType == _stickyNoteType;

    public static string Sanitise(string contents)
    {
        var trimmed = contents.Length > _maxContentsLength
            ? contents[.._maxContentsLength]
            : contents;

        return trimmed.Replace("\r", string.Empty).Replace("\n", " ");
    }
}
