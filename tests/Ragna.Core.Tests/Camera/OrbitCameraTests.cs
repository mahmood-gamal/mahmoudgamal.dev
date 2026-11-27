using System.Numerics;
using Ragna.Core.Camera;

namespace Ragna.Core.Tests.Camera;

public sealed class OrbitCameraTests
{
    private const float Epsilon = 1e-4f;

    private static OrbitCamera Instant(OrbitCameraSettings? settings = null) =>
        new((settings ?? new OrbitCameraSettings()) with { Smoothing = 0f });

    [Fact]
    public void Starts_at_default_distance_and_yaw_zero()
    {
        var camera = new OrbitCamera();

        Assert.Equal(0f, camera.YawDegrees);
        Assert.Equal(18f, camera.Distance);
        Assert.Equal(45f, camera.Settings.PitchDegrees);
    }

    [Fact]
    public void Pitch_is_45_degrees_above_the_focus()
    {
        var camera = Instant();
        var target = new Vector3(10f, 2f, -4f);

        var toCamera = camera.Position(target) - camera.Focus(target);
        var horizontal = MathF.Sqrt((toCamera.X * toCamera.X) + (toCamera.Z * toCamera.Z));

        Assert.Equal(45f, MathF.Atan2(toCamera.Y, horizontal) * 180f / MathF.PI, 3);
        Assert.Equal(camera.Distance, toCamera.Length(), 3);
    }

    [Fact]
    public void Follows_the_target()
    {
        var camera = Instant();
        var a = camera.Position(Vector3.Zero);
        var b = camera.Position(new Vector3(5f, 1f, -3f));

        Assert.True(Vector3.Distance(b - a, new Vector3(5f, 1f, -3f)) < Epsilon);
    }

    [Fact]
    public void Yaw_zero_sits_on_positive_z_looking_towards_negative_z()
    {
        var offset = OrbitCamera.Offset(0f, 45f, 10f);

        Assert.Equal(0f, offset.X, 4);
        Assert.True(offset.Z > 0f);
        Assert.True(offset.Y > 0f);
    }

    [Fact]
    public void Q_and_E_turn_in_45_degree_steps()
    {
        var camera = Instant();

        camera.RotateLeft();
        camera.Update(0.016f);
        Assert.Equal(45f, camera.YawDegrees, 3);

        camera.RotateRight();
        camera.RotateRight();
        camera.Update(0.016f);
        Assert.Equal(315f, camera.YawDegrees, 3);
    }

    [Fact]
    public void Eight_steps_come_full_circle()
    {
        var camera = Instant();

        for (var i = 0; i < 8; i++)
        {
            camera.RotateLeft();
        }

        camera.Update(0.016f);
        Assert.Equal(0f, camera.YawDegrees, 3);
        Assert.True(Vector3.Distance(camera.Position(Vector3.Zero), new OrbitCamera().Position(Vector3.Zero)) < 1e-3f);
    }

    [Fact]
    public void Positive_yaw_is_counter_clockwise_seen_from_above()
    {
        // Seen from above with +X to the right, +Z points down the screen, so moving from
        // +Z towards +X is counter-clockwise.
        var offset = OrbitCamera.Offset(90f, 45f, 10f);

        Assert.True(offset.X > 0f);
        Assert.Equal(0f, offset.Z, 4);
    }

    [Fact]
    public void Wheel_zoom_is_clamped_to_limits()
    {
        var camera = Instant();

        camera.Zoom(100f);
        camera.Update(0.016f);
        Assert.Equal(camera.Settings.MinDistance, camera.Distance);

        camera.Zoom(-100f);
        camera.Update(0.016f);
        Assert.Equal(camera.Settings.MaxDistance, camera.Distance);
    }

    [Fact]
    public void One_notch_moves_one_zoom_step()
    {
        var camera = Instant();

        camera.Zoom(1f);

        Assert.Equal(16f, camera.TargetDistance);
    }

    [Fact]
    public void Default_distance_outside_limits_is_clamped()
    {
        var camera = new OrbitCamera(new OrbitCameraSettings { MinDistance = 5f, MaxDistance = 10f, DefaultDistance = 50f });

        Assert.Equal(10f, camera.Distance);
    }

    [Fact]
    public void Invalid_limits_throw()
    {
        Assert.Throws<ArgumentException>(() => new OrbitCamera(new OrbitCameraSettings { MinDistance = 10f, MaxDistance = 5f }));
        Assert.Throws<ArgumentException>(() => new OrbitCamera(new OrbitCameraSettings { MinDistance = 0f }));
    }

    [Fact]
    public void Smoothing_eases_towards_the_target_and_settles()
    {
        var camera = new OrbitCamera();
        camera.RotateLeft();
        camera.Zoom(-2f);

        camera.Update(1f / 60f);
        Assert.InRange(camera.YawDegrees, 0.1f, 44.9f);
        Assert.InRange(camera.Distance, 18.1f, 21.9f);

        for (var i = 0; i < 120; i++)
        {
            camera.Update(1f / 60f);
        }

        Assert.Equal(45f, camera.YawDegrees);
        Assert.Equal(22f, camera.Distance);
    }

    [Fact]
    public void Turning_past_360_eases_the_short_way()
    {
        var smooth = new OrbitCamera();
        smooth.RotateRight(); // 315°
        smooth.Snap();
        smooth.RotateLeft(); // back to 0°, should pass through ~337°, not ~157°.
        smooth.Update(1f / 60f);

        Assert.InRange(smooth.YawDegrees, 315f, 360f);
    }

    [Fact]
    public void Snap_jumps_to_targets()
    {
        var camera = new OrbitCamera();
        camera.RotateLeft();
        camera.Zoom(1f);

        camera.Snap();

        Assert.Equal(45f, camera.YawDegrees);
        Assert.Equal(16f, camera.Distance);
    }
}
