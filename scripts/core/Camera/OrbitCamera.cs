using System.Numerics;

namespace Ragna.Core.Camera;

/// <summary>Tuning for <see cref="OrbitCamera"/>. Distances are in metres, angles in degrees.</summary>
public sealed record OrbitCameraSettings
{
    /// <summary>Angle between the ground plane and the line of sight. Fixed during play.</summary>
    public float PitchDegrees { get; init; } = 45f;

    /// <summary>How far one Q/E press turns the camera around the target.</summary>
    public float YawStepDegrees { get; init; } = 45f;

    public float MinDistance { get; init; } = 8f;

    public float MaxDistance { get; init; } = 30f;

    public float DefaultDistance { get; init; } = 18f;

    /// <summary>How much one mouse wheel notch moves the camera in or out.</summary>
    public float ZoomStep { get; init; } = 2f;

    /// <summary>
    /// How quickly the shown yaw and distance catch up with their targets, per second.
    /// Higher is snappier. 0 or less snaps instantly.
    /// </summary>
    public float Smoothing { get; init; } = 12f;

    /// <summary>Height above the target's origin that the camera looks at, roughly chest height.</summary>
    public float LookAtHeight { get; init; } = 1f;
}

/// <summary>
/// Engine-agnostic state for the Ragnarok-style follow camera: a fixed pitch, yaw that
/// turns in fixed steps, and a zoom distance clamped to limits. The engine node feeds it
/// input and the target position, then places the camera at <see cref="Position"/>.
/// </summary>
/// <remarks>
/// Yaw is measured around +Y. At yaw 0 the camera sits on the +Z side of the target and
/// looks towards -Z. Positive yaw turns the camera counter-clockwise seen from above.
/// </remarks>
public sealed class OrbitCamera
{
    private const float SnapEpsilon = 1e-3f;

    // Unwrapped, so turning past 360° keeps easing the short way instead of spinning back.
    private float _targetYaw;
    private float _yaw;

    public OrbitCamera(OrbitCameraSettings? settings = null)
    {
        Settings = settings ?? new OrbitCameraSettings();
        if (Settings.MinDistance <= 0f || Settings.MaxDistance < Settings.MinDistance)
        {
            throw new ArgumentException("Need 0 < MinDistance <= MaxDistance.", nameof(settings));
        }

        TargetDistance = Distance = Math.Clamp(Settings.DefaultDistance, Settings.MinDistance, Settings.MaxDistance);
    }

    public OrbitCameraSettings Settings { get; }

    /// <summary>The yaw step the camera is turning to, from 0 to 359.</summary>
    public float TargetYawDegrees => Wrap(_targetYaw);

    /// <summary>The yaw currently shown, from 0 to 359. Lags the target while easing.</summary>
    public float YawDegrees => Wrap(_yaw);

    /// <summary>The zoom distance the camera is easing to, within the min/max limits.</summary>
    public float TargetDistance { get; private set; }

    /// <summary>The zoom distance currently shown.</summary>
    public float Distance { get; private set; }

    /// <summary>Turns one step counter-clockwise, seen from above (Q).</summary>
    public void RotateLeft() => _targetYaw += Settings.YawStepDegrees;

    /// <summary>Turns one step clockwise, seen from above (E).</summary>
    public void RotateRight() => _targetYaw -= Settings.YawStepDegrees;

    /// <summary>Moves the camera closer by <paramref name="notches"/> wheel notches. Negative zooms out.</summary>
    public void Zoom(float notches) =>
        TargetDistance = Math.Clamp(TargetDistance - (notches * Settings.ZoomStep), Settings.MinDistance, Settings.MaxDistance);

    /// <summary>Eases the shown yaw and distance towards their targets.</summary>
    public void Update(float deltaSeconds)
    {
        var t = Settings.Smoothing <= 0f ? 1f : 1f - MathF.Exp(-Settings.Smoothing * Math.Max(deltaSeconds, 0f));
        _yaw = Approach(_yaw, _targetYaw, t);
        Distance = Approach(Distance, TargetDistance, t);
    }

    /// <summary>Jumps straight to the target yaw and distance, for example after a teleport.</summary>
    public void Snap()
    {
        _yaw = _targetYaw;
        Distance = TargetDistance;
    }

    /// <summary>The point the camera looks at for a target standing at <paramref name="target"/>.</summary>
    public Vector3 Focus(Vector3 target) => target + new Vector3(0f, Settings.LookAtHeight, 0f);

    /// <summary>Where to put the camera so it views <paramref name="target"/> at the current yaw, pitch and distance.</summary>
    public Vector3 Position(Vector3 target) => Focus(target) + Offset(_yaw, Settings.PitchDegrees, Distance);

    /// <summary>The camera's offset from the point it looks at.</summary>
    public static Vector3 Offset(float yawDegrees, float pitchDegrees, float distance)
    {
        var yaw = yawDegrees * MathF.PI / 180f;
        var pitch = pitchDegrees * MathF.PI / 180f;
        var horizontal = distance * MathF.Cos(pitch);
        return new Vector3(horizontal * MathF.Sin(yaw), distance * MathF.Sin(pitch), horizontal * MathF.Cos(yaw));
    }

    private static float Approach(float current, float target, float t)
    {
        var next = current + ((target - current) * t);
        return MathF.Abs(target - next) < SnapEpsilon ? target : next;
    }

    private static float Wrap(float degrees)
    {
        var wrapped = degrees % 360f;
        return wrapped < 0f ? wrapped + 360f : wrapped;
    }
}
