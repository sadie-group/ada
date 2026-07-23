using System.Diagnostics;
using Ada.API;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Server.Tasks;

namespace Ada.Tests.Api;

[TestFixture]
public class AbstractBaseClassDefaultsTests
{
    private class NoopInteractor : AbstractRoomFurnitureItemInteractor
    {
        public override List<string> InteractionTypes => [];
    }

    private class NoopCommand : AbstractRoomChatCommand
    {
        public override string Trigger => "noop";
        public override string Description => "";
        public override Task ExecuteAsync(IRoomUser user, IRoomChatCommandParameterReader reader) => Task.CompletedTask;
    }

    private class FastTask : IServerTask
    {
        public TimeSpan PeriodicInterval => TimeSpan.FromMilliseconds(1);
        public long LastExecutedTicks { get; set; }
        public Task ExecuteAsync() => Task.CompletedTask;
    }

    private class SlowTask : IServerTask
    {
        public TimeSpan PeriodicInterval => TimeSpan.FromHours(1);
        public long LastExecutedTicks { get; set; }
        public Task ExecuteAsync() => Task.CompletedTask;
    }

    [Test]
    public async Task Interactor_DefaultHooks_CompleteWithoutSideEffects()
    {
        var interactor = new NoopInteractor();

        await interactor.OnTriggerAsync(null!, null!, null!);
        await interactor.OnPlaceAsync(null!, null!, null!);
        await interactor.OnPickUpAsync(null!, null!, null!);
        await interactor.OnMoveAsync(null!, null!, null!);

        Assert.That(interactor.InteractionTypes, Is.Empty);
    }

    [Test]
    public void ChatCommand_Defaults_AreOpenToEveryone()
    {
        var command = new NoopCommand();

        Assert.Multiple(() =>
        {
            Assert.That(command.PermissionsRequired, Is.Empty);
            Assert.That(command.BypassPermissionCheckIfRoomOwner, Is.False);
            Assert.That(command.Parameters, Is.Empty);
        });
    }

    [Test]
    public void ServerTask_NeverExecuted_IsWaitingToExecute()
    {
        IServerTask task = new SlowTask();

        Assert.That(task.WaitingToExecute(), Is.True);
    }

    [Test]
    public void ServerTask_ExecutedRecently_IsNotWaitingWithinInterval()
    {
        IServerTask task = new SlowTask { LastExecutedTicks = Stopwatch.GetTimestamp() };

        Assert.That(task.WaitingToExecute(), Is.False);
    }

    [Test]
    public async Task ServerTask_IntervalElapsed_IsWaitingAgain()
    {
        IServerTask task = new FastTask { LastExecutedTicks = Stopwatch.GetTimestamp() };

        await Task.Delay(20);

        Assert.That(task.WaitingToExecute(), Is.True);
    }
}
