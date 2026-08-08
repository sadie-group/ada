using System.Drawing;
using Ada.API;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db.Models.Constants;
using Ada.Game.Rooms.Unit;
using Ada.Networking.Writers.Rooms.Users;
using Ada.Networking.Writers.Rooms.Users.HandItems;

namespace Ada.Game.Rooms.Users;

public class RoomUser(
    IRoomLogic room,
    INetworkObject networkObject,
    Point point,
    double pointZ,
    HDirection directionHead,
    HDirection direction,
    IPlayerLogic player,
    ServerRoomConstants roomConstants,
    RoomControllerLevel controllerLevel,
    IRoomTileMapHelperService tileMapHelperService,
    IRoomHelperService roomHelperService,
    IRoomWiredService wiredService,
    IRoomPathFinderHelperService pathFinderHelperService,
    IRoomFurnitureItemInteractorRepository interactorRepository)
    : RoomUnitData(
            room,
            point,
            pointZ,
            directionHead,
            direction,
            tileMapHelperService,
            pathFinderHelperService),
        IRoomUser
{
    public IPlayerLogic Player { get; } = player;
    public DateTime LastAction { get; set; } = DateTime.UtcNow;
    public TimeSpan IdleTime { get; } = TimeSpan.FromSeconds(roomConstants.SecondsTillUserIdle);
    public bool IsIdle { get; set; }
    public bool MoonWalking { get; set; }
    public IRoomUserTrade? Trade { get; set; }
    public int TradeStatus { get; set; }
    public int ActiveEffectId { get; set; }
    public IRoomLogic Room { get; } = room;
    public RoomControllerLevel ControllerLevel { get; set; } = controllerLevel;
    public INetworkObject NetworkObject { get; } = networkObject;
    public DateTime SignSet { get; set; } = DateTime.MinValue;

    public void LookAtPoint(Point point)
    {
        var direction = pathFinderHelperService.GetDirectionForNextStep(Point, point);

        if (!StatusMap.ContainsKey(RoomUserStatus.Sit))
        {
            Direction = direction;
        }

        if (Math.Abs(direction - Direction) < 2)
        {
            DirectionHead = direction;
        }
    }

    public void ApplyFlatCtrlStatus() => AddStatus(RoomUserStatus.FlatCtrl, ((int)controllerLevel).ToString());

    public async Task RunPeriodicCheckAsync()
    {
        if (HandItemId != 0 && (DateTime.UtcNow - HandItemSet).TotalSeconds >= 30)
        {
            HandItemId = 0;

            await room.BroadcastDataAsync(new RoomUserHandItemWriter
            {
                UserId = Player.Player.Id,
                ItemId = 0
            });
        }

        if (StatusMap.ContainsKey(RoomUserStatus.Sign) &&
            (DateTime.UtcNow - SignSet).TotalSeconds >= 5)
        {
            RemoveStatuses(RoomUserStatus.Sign);
        }

        var position = Point;

        await ProcessGenericChecksAsync();
        await UpdateIdleStatusAsync();
        await UpdateEffectAsync();

        if (Point != position)
        {
            await CheckForStepTriggersAsync(position, FurnitureItemInteractionType.WiredTriggerUserWalksOffFurniture);
            await CheckForStepTriggersAsync(Point, FurnitureItemInteractionType.WiredTriggerUserWalksOnFurniture);
            await RunStepInteractorsAsync(position, walkedOn: false);
            await RunStepInteractorsAsync(Point, walkedOn: true);
        }
    }

    private async Task RunStepInteractorsAsync(Point point, bool walkedOn)
    {
        foreach (var item in tileMapHelperService.GetItemsOnTilePosition(point.X, point.Y, room.Room.FurnitureItems))
        {
            var interactionType = item.PlayerFurnitureItem.FurnitureItem.InteractionType;

            if (string.IsNullOrEmpty(interactionType))
            {
                continue;
            }

            foreach (var interactor in interactorRepository.GetInteractorsForType(interactionType))
            {
                try
                {
                    if (walkedOn)
                    {
                        await interactor.OnWalkedOnAsync(room, item, this);
                    }
                    else
                    {
                        await interactor.OnWalkedOffAsync(room, item, this);
                    }
                }
                catch (Exception e)
                {
                    Serilog.Log.Error(e, "Step interactor {Interactor} failed", interactor.GetType().Name);
                }
            }
        }
    }

    private async Task CheckForStepTriggersAsync(Point point, string interactionType)
    {
        if (!wiredService.HasTriggers(interactionType, room.Room.FurnitureItems))
        {
            return;
        }

        var itemIdsOnPoint = tileMapHelperService
            .GetItemsOnTilePosition(point.X, point.Y, room.Room.FurnitureItems)
            .Select(x => x.Id)
            .ToList();

        if (itemIdsOnPoint.Count < 1)
        {
            return;
        }

        var triggers = wiredService.GetTriggers(
            interactionType,
            room.Room.FurnitureItems,
            "",
            itemIdsOnPoint);

        foreach (var trigger in triggers)
        {
            await wiredService.RunTriggerForRoomAsync(room, trigger, this);
        }
    }

    private async Task UpdateEffectAsync()
    {
        var effectPointToCheck = IsWalking && NextPoint != null ?
            NextPoint.Value :
            Point;

        var effectId = room.TileMap.EffectMap[effectPointToCheck.Y, effectPointToCheck.X] != 0
            ? room.TileMap.EffectMap[Point.Y, Point.X]
            : 0;

        if (effectId == ActiveEffectId)
        {
            return;
        }

        await SetEffectAsync((RoomUserEffect) effectId);
    }

    private async Task UpdateIdleStatusAsync()
    {
        var shouldBeIdle = DateTime.UtcNow - LastAction > IdleTime;

        if (shouldBeIdle && !IsIdle || !shouldBeIdle && IsIdle)
        {
            IsIdle = shouldBeIdle;

            var writer = new RoomUserIdleWriter
            {
                UserId = Player.Player.Id,
                IsIdle = IsIdle
            };

            await room.BroadcastDataAsync(writer);
        }
    }

    public bool HasRights()
    {
        return ControllerLevel is
            RoomControllerLevel.Owner or
            RoomControllerLevel.Rights;
    }

    public async Task SendWhisperAsync(string message)
    {
        await NetworkObject.WriteToStreamAsync(new RoomUserWhisperWriter
        {
            SenderId = Player.Player.Id,
            Message = message,
            EmotionId = (int) roomHelperService.GetEmotionFromMessage(message),
            ChatBubbleId = 0,
            MessageLength = message.Length,
            Urls = []
        });
    }

    public async Task SetEffectAsync(RoomUserEffect effect)
    {
        ActiveEffectId = (int) effect;

        var writer = new RoomUserEffectWriter
        {
            UserId = (int) Player.Player.Id,
            EffectId = (int) effect,
            DelayMs = 0
        };

        await Room.BroadcastDataAsync(writer);
    }

    public async ValueTask DisposeAsync()
    {
        if (room.TileMap.UnitMap.TryGetValue(Point, out var value))
        {
            value.Remove(this);
        }
    }
}
