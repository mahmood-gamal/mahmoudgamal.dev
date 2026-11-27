using Godot;
using Ragna.Core.Camera;
using NVector3 = System.Numerics.Vector3;

namespace Ragna.Gameplay.Camera;

/// <summary>
/// Ragnarok-style follow camera: fixed 45° pitch, Q/E turn around the target in 45° steps,
/// and the mouse wheel zooms between <see cref="MinDistance"/> and <see cref="MaxDistance"/>.
/// The maths lives in <see cref="OrbitCamera"/>; this node feeds it input and applies the result.
/// </summary>
public partial class GameCamera : Camera3D
{
    public const string RotateLeftAction = "camera_rotate_left";
    public const string RotateRightAction = "camera_rotate_right";
    public const string ZoomInAction = "camera_zoom_in";
    public const string ZoomOutAction = "camera_zoom_out";

    private OrbitCamera _orbit = new();

    /// <summary>The node to follow, normally the player.</summary>
    [Export]
    public Node3D? Target { get; set; }

    [Export(PropertyHint.Range, "10,80,1")]
    public float PitchDegrees { get; set; } = 45f;

    [Export(PropertyHint.Range, "1,90,1")]
    public float YawStepDegrees { get; set; } = 45f;

    [Export(PropertyHint.Range, "1,100,0.5,suffix:m")]
    public float MinDistance { get; set; } = 8f;

    [Export(PropertyHint.Range, "1,100,0.5,suffix:m")]
    public float MaxDistance { get; set; } = 30f;

    [Export(PropertyHint.Range, "1,100,0.5,suffix:m")]
    public float DefaultDistance { get; set; } = 18f;

    [Export(PropertyHint.Range, "0.1,10,0.1,suffix:m")]
    public float ZoomStep { get; set; } = 2f;

    /// <summary>How quickly turning and zooming ease in. 0 snaps instantly.</summary>
    [Export(PropertyHint.Range, "0,50,0.5")]
    public float Smoothing { get; set; } = 12f;

    [Export(PropertyHint.Range, "0,5,0.1,suffix:m")]
    public float LookAtHeight { get; set; } = 1f;

    /// <summary>The camera model, for code that needs the current yaw (for example 8-direction sprite facing).</summary>
    public OrbitCamera Orbit => _orbit;

    public override void _Ready()
    {
        // Run after gameplay nodes so the camera uses this frame's target position.
        ProcessPriority = 100;
        _orbit = new OrbitCamera(new OrbitCameraSettings
        {
            PitchDegrees = PitchDegrees,
            YawStepDegrees = YawStepDegrees,
            MinDistance = MinDistance,
            MaxDistance = Mathf.Max(MinDistance, MaxDistance),
            DefaultDistance = DefaultDistance,
            ZoomStep = ZoomStep,
            Smoothing = Smoothing,
            LookAtHeight = LookAtHeight,
        });
        MakeCurrent();
        Follow();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(RotateLeftAction))
        {
            _orbit.RotateLeft();
        }
        else if (@event.IsActionPressed(RotateRightAction))
        {
            _orbit.RotateRight();
        }
        else if (@event.IsActionPressed(ZoomInAction))
        {
            _orbit.Zoom(WheelNotches(@event));
        }
        else if (@event.IsActionPressed(ZoomOutAction))
        {
            _orbit.Zoom(-WheelNotches(@event));
        }
        else
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        _orbit.Update((float)delta);
        Follow();
    }

    private void Follow()
    {
        if (Target is null)
        {
            return;
        }

        var target = ToCore(Target.GlobalPosition);
        GlobalPosition = ToGodot(_orbit.Position(target));
        LookAt(ToGodot(_orbit.Focus(target)), Vector3.Up);
    }

    // Precise scrolling (touchpads) reports fractional notches in Factor; plain wheels report 1 or 0.
    private static float WheelNotches(InputEvent @event) =>
        @event is InputEventMouseButton { Factor: > 0f } wheel ? wheel.Factor : 1f;

    private static NVector3 ToCore(Vector3 v) => new(v.X, v.Y, v.Z);

    private static Vector3 ToGodot(NVector3 v) => new(v.X, v.Y, v.Z);
}
