using System.Numerics;
using Ragna.Core.Characters;

namespace Ragna.Core.Tests.Characters;

public sealed class FacingDirectionTests
{
    // Default camera: yaw 0, looking along -Z. Screen right is +X.
    private static readonly Vector2 CameraForward = new(0f, -1f);

    [Theory]
    [InlineData(0f, 1f, FacingDirection.South)] // towards the camera
    [InlineData(-1f, 1f, FacingDirection.SouthWest)]
    [InlineData(-1f, 0f, FacingDirection.West)] // screen left
    [InlineData(-1f, -1f, FacingDirection.NorthWest)]
    [InlineData(0f, -1f, FacingDirection.North)] // away from the camera
    [InlineData(1f, -1f, FacingDirection.NorthEast)]
    [InlineData(1f, 0f, FacingDirection.East)] // screen right
    [InlineData(1f, 1f, FacingDirection.SouthEast)]
    public void Picks_each_of_the_eight_directions(float x, float z, FacingDirection expected)
    {
        Assert.Equal(expected, FacingDirections.FromVectors(new Vector2(x, z), CameraForward));
    }

    [Theory]
    [InlineData(20f, FacingDirection.South)] // within 22.5° of facing the camera
    [InlineData(-20f, FacingDirection.South)]
    [InlineData(25f, FacingDirection.SouthEast)]
    [InlineData(-25f, FacingDirection.SouthWest)]
    [InlineData(60f, FacingDirection.SouthEast)]
    [InlineData(80f, FacingDirection.East)]
    public void Snaps_to_the_nearest_of_eight_sectors(float degreesClockwiseFromSouth, FacingDirection expected)
    {
        // South is +Z (towards the camera); turning towards screen right (+X). Sector edges sit at 22.5° + n·45°.
        var radians = degreesClockwiseFromSouth * MathF.PI / 180f;
        var facing = new Vector2(MathF.Sin(radians), MathF.Cos(radians));

        Assert.Equal(expected, FacingDirections.FromVectors(facing, CameraForward));
    }

    [Fact]
    public void Does_not_need_normalised_vectors()
    {
        Assert.Equal(
            FacingDirection.East,
            FacingDirections.FromVectors(new Vector2(7f, 0f), new Vector2(0f, -0.25f)));
    }

    [Fact]
    public void Zero_vectors_face_south()
    {
        Assert.Equal(FacingDirection.South, FacingDirections.FromVectors(Vector2.Zero, CameraForward));
        Assert.Equal(FacingDirection.South, FacingDirections.FromVectors(new Vector2(1f, 0f), Vector2.Zero));
    }

    [Fact]
    public void Turning_the_camera_changes_the_drawn_direction_but_not_the_facing()
    {
        // Character walks towards -Z (north in world terms) and keeps facing that way.
        var facing = new Vector2(0f, -1f);

        // Yaw 0: camera looks along -Z, so we see its back.
        Assert.Equal(FacingDirection.North, FacingDirections.FromVectors(facing, FacingDirections.YawToForward(0f)));

        // Q turns the camera +45° (RotateLeft). Camera now looks along (-sin45, -cos45):
        // the character faces up and to the right on screen.
        Assert.Equal(FacingDirection.NorthEast, FacingDirections.FromVectors(facing, FacingDirections.YawToForward(45f)));
        Assert.Equal(FacingDirection.East, FacingDirections.FromVectors(facing, FacingDirections.YawToForward(90f)));
        Assert.Equal(FacingDirection.South, FacingDirections.FromVectors(facing, FacingDirections.YawToForward(180f)));

        // E turns it the other way.
        Assert.Equal(FacingDirection.NorthWest, FacingDirections.FromVectors(facing, FacingDirections.YawToForward(-45f)));
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(45f, 0f)]
    [InlineData(135f, -45f)]
    [InlineData(-90f, 270f)]
    [InlineData(10f, 370f)]
    public void Only_the_relative_angle_matters(float facingYaw, float cameraYaw)
    {
        for (var turn = 0f; turn < 360f; turn += 15f)
        {
            Assert.Equal(
                FacingDirections.FromYaws(facingYaw, cameraYaw),
                FacingDirections.FromYaws(facingYaw + turn, cameraYaw + turn));
        }
    }

    [Theory]
    [InlineData(0f, 0f, FacingDirection.North)] // same way as the camera: back view
    [InlineData(180f, 0f, FacingDirection.South)] // facing the camera
    [InlineData(-90f, 0f, FacingDirection.East)] // yaw -90 looks along +X, screen right
    [InlineData(90f, 0f, FacingDirection.West)]
    [InlineData(-45f, 0f, FacingDirection.NorthEast)]
    public void From_yaws(float facingYaw, float cameraYaw, FacingDirection expected)
    {
        Assert.Equal(expected, FacingDirections.FromYaws(facingYaw, cameraYaw));
    }

    [Fact]
    public void Yaw_zero_looks_along_negative_z()
    {
        var forward = FacingDirections.YawToForward(0f);

        Assert.Equal(0f, forward.X, 5);
        Assert.Equal(-1f, forward.Y, 5);
    }

    [Theory]
    [InlineData(FacingDirection.South, 0)]
    [InlineData(FacingDirection.SouthWest, 1)]
    [InlineData(FacingDirection.West, 2)]
    [InlineData(FacingDirection.NorthWest, 3)]
    [InlineData(FacingDirection.North, 4)]
    [InlineData(FacingDirection.NorthEast, 5)]
    [InlineData(FacingDirection.East, 6)]
    [InlineData(FacingDirection.SouthEast, 7)]
    public void Row_is_clockwise_from_south(FacingDirection direction, int row)
    {
        Assert.Equal(row, FacingDirections.Row(direction));
    }

    [Theory]
    [InlineData(FacingDirection.South, FacingDirection.South)]
    [InlineData(FacingDirection.SouthWest, FacingDirection.SouthEast)]
    [InlineData(FacingDirection.West, FacingDirection.East)]
    [InlineData(FacingDirection.NorthWest, FacingDirection.NorthEast)]
    [InlineData(FacingDirection.North, FacingDirection.North)]
    [InlineData(FacingDirection.NorthEast, FacingDirection.NorthWest)]
    [InlineData(FacingDirection.East, FacingDirection.West)]
    [InlineData(FacingDirection.SouthEast, FacingDirection.SouthWest)]
    public void Mirror_swaps_left_and_right(FacingDirection direction, FacingDirection mirrored)
    {
        Assert.Equal(mirrored, FacingDirections.Mirror(direction));
    }

    [Fact]
    public void Suffixes_are_unique()
    {
        var suffixes = Enum.GetValues<FacingDirection>().Select(FacingDirections.Suffix).ToList();

        Assert.Equal(FacingDirections.Count, suffixes.Distinct().Count());
        Assert.Equal("se", FacingDirections.Suffix(FacingDirection.SouthEast));
    }

    [Fact]
    public void Resolve_prefers_the_exact_direction()
    {
        var names = new HashSet<string> { "walk", "walk_se", "walk_sw" };

        Assert.Equal(("walk_se", false), FacingDirections.ResolveAnimation("walk", FacingDirection.SouthEast, names.Contains));
    }

    [Fact]
    public void Resolve_falls_back_to_the_flipped_mirror()
    {
        // A 5-direction sheet: S, SW, W, NW, N.
        var names = new HashSet<string> { "walk_s", "walk_sw", "walk_w", "walk_nw", "walk_n" };

        Assert.Equal(("walk_sw", true), FacingDirections.ResolveAnimation("walk", FacingDirection.SouthEast, names.Contains));
        Assert.Equal(("walk_w", true), FacingDirections.ResolveAnimation("walk", FacingDirection.East, names.Contains));
        Assert.Equal(("walk_nw", true), FacingDirections.ResolveAnimation("walk", FacingDirection.NorthEast, names.Contains));
        Assert.Equal(("walk_n", false), FacingDirections.ResolveAnimation("walk", FacingDirection.North, names.Contains));
    }

    [Fact]
    public void Resolve_falls_back_to_the_plain_animation()
    {
        var names = new HashSet<string> { "idle", "walk", "attack" };

        Assert.Equal(("walk", false), FacingDirections.ResolveAnimation("walk", FacingDirection.East, names.Contains));
        Assert.Equal(("idle", false), FacingDirections.ResolveAnimation("idle", FacingDirection.South, names.Contains));
    }
}
