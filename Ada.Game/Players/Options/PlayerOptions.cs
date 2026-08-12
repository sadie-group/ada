namespace Ada.Game.Players.Options;

public class PlayerOptions
{
    public bool CanReuseSsoTokens { get; init; }
    public int SsoGraceSeconds { get; init; } = 5;
    public bool RequireHashedSsoTokens { get; init; } = true;
}
