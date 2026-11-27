using Godot;

namespace Ragna.Core;

/// <summary>Root node of the main scene.</summary>
public partial class Main : Node3D
{
    public override void _Ready() => GD.Print($"{GameInfo.Current} started.");
}
