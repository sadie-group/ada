using Ada.Db.Models.Rooms;

namespace Ada.Game.Navigator.Filterers;

public interface INavigatorSearchFilterer
{
    public string Name { get; }
    IQueryable<Room> Apply(IQueryable<Room> query, string value);
}