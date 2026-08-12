using Ada.API.Interfaces.Game.Catalog;
using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Server;
using Ada.API.Interfaces.Server.Tasks;
using Moq;

namespace Ada.Tests.Boot;

[TestFixture]
public class ServerBootTests
{
    private sealed class Recorder
    {
        private readonly Lock _gate = new();

        public List<string> Order { get; } = [];

        public void Record(string step)
        {
            lock (_gate)
            {
                Order.Add(step);
            }
        }
    }

    [Test]
    public async Task Boot_MigratesBeforeAnythingElseAndStartsTasksLast()
    {
        var recorder = new Recorder();

        var migrator = new Mock<IServerMigrator>();
        migrator.Setup(x => x.MigrateAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                recorder.Record("migrate");
                return Task.CompletedTask;
            });

        var cleaner = new Mock<IServerDataCleaner>();
        cleaner.Setup(x => x.CleanAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                recorder.Record("clean");
                return Task.CompletedTask;
            });

        var catalog = new Mock<ICatalogPageRepository>();
        catalog.Setup(x => x.LoadAsync())
            .Returns(() =>
            {
                recorder.Record("catalog");
                return Task.CompletedTask;
            });

        var tickets = new Mock<IModerationTicketService>();
        tickets.Setup(x => x.LoadAsync())
            .Returns(() =>
            {
                recorder.Record("tickets");
                return Task.CompletedTask;
            });

        var tasks = new Mock<IServerTaskWorker>();
        tasks.Setup(x => x.WorkAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                recorder.Record("tasks");
                return Task.CompletedTask;
            });

        var server = new global::Ada.Server.Server(
            NullLogger<global::Ada.Server.Server>.Instance,
            migrator.Object,
            cleaner.Object,
            tasks.Object,
            Mock.Of<INetworkClientRepository>(),
            catalog.Object,
            tickets.Object);

        await server.RunAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(recorder.Order.First(), Is.EqualTo("migrate"),
                "nothing may touch the schema before it is migrated");
            Assert.That(recorder.Order.Last(), Is.EqualTo("tasks"),
                "periodic tasks must not start before the data they read is loaded");
            Assert.That(recorder.Order, Is.EquivalentTo(new[] { "migrate", "clean", "catalog", "tickets", "tasks" }));
        });
    }

    [Test]
    public async Task Boot_LoadsTheIndependentStepsConcurrently()
    {
        var started = 0;
        var allStarted = new TaskCompletionSource();

        Task Step()
        {
            if (Interlocked.Increment(ref started) == 3)
            {
                allStarted.SetResult();
            }

            return allStarted.Task;
        }

        var migrator = new Mock<IServerMigrator>();
        migrator.Setup(x => x.MigrateAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var cleaner = new Mock<IServerDataCleaner>();
        cleaner.Setup(x => x.CleanAsync(It.IsAny<CancellationToken>())).Returns(Step);

        var catalog = new Mock<ICatalogPageRepository>();
        catalog.Setup(x => x.LoadAsync()).Returns(Step);

        var tickets = new Mock<IModerationTicketService>();
        tickets.Setup(x => x.LoadAsync()).Returns(Step);

        var tasks = new Mock<IServerTaskWorker>();
        tasks.Setup(x => x.WorkAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var server = new global::Ada.Server.Server(
            NullLogger<global::Ada.Server.Server>.Instance,
            migrator.Object,
            cleaner.Object,
            tasks.Object,
            Mock.Of<INetworkClientRepository>(),
            catalog.Object,
            tickets.Object);

        await server.RunAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(started, Is.EqualTo(3),
            "the three database-bound boot steps must overlap rather than run one after another");
    }
}
