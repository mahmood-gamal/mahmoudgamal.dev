namespace Ragna.Core;

/// <summary>Static metadata about the running game build.</summary>
public sealed record GameInfo(string Name, string Version)
{
    public static GameInfo Current { get; } = new("Ragna", "0.1.0");

    public override string ToString() => $"{Name} v{Version}";
}
