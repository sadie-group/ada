namespace Ada.Core.Shared;

public static class GlobalState
{
    public static Random Random => Random.Shared;
    public static Version? Version { get; set; }
}
