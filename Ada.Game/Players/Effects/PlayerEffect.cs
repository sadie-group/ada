using Ada.API.Interfaces.Game.Players.Effects;

namespace Ada.Game.Players.Effects;

public class PlayerEffect(int duration, int id) : IPlayerEffect
{
    public int Id { get; } = id;
    public int Duration { get; } = duration;
}