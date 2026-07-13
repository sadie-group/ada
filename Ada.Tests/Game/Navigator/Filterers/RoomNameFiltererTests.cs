using Microsoft.EntityFrameworkCore;
using Ada.Db;
using Ada.Game.Navigator.Filterers;

namespace Ada.Tests.Game.Navigator.Filterers;

public class RoomNameFiltererTests : MockHelpers
{
    private RoomNameFilterer? _filterer;
    
    [SetUp]
    public void SetUp()
    {
        _filterer = new RoomNameFilterer();
    }
    
    [Test]
    public void ApplyFilter_OneInMany_AppliedCorrectly()
    {
        var options = new DbContextOptionsBuilder<AdaDbContext>()
            .UseInMemoryDatabase(databaseName: "ada")
            .Options;

        using var dbContext = new AdaDbContext(options);
        
        dbContext.Rooms.Add(MockRoomWithName("someName1"));
        dbContext.Rooms.Add(MockRoomWithName("someName2"));
        dbContext.Rooms.Add(MockRoomWithName("someName3"));

        dbContext.SaveChanges();
        
        var query = dbContext.Rooms.AsQueryable();
        var newQuery = _filterer!.Apply(query, "someName2");

        Assert.That(newQuery.ToList(), Has.Count.EqualTo(1));
    }
}