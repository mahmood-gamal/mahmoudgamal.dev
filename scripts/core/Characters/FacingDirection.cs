using System.Numerics;

namespace Ragna.Core.Characters;

/// <summary>
/// The eight directions a character sprite can be drawn facing, as seen on screen. The value is the
/// frame row in a directional sprite sheet: row 0 faces the camera, and the rows go clockwise as seen
/// from above (south, south-west, west, ...). West is screen left, north is away from the camera.
/// </summary>
public enum FacingDirection
{
    South = 0,
    SouthWest = 1,
    West = 2,
    NorthWest = 3,
    North = 4,
    NorthEast = 5,
    East = 6,
    SouthEast = 7,
}

/// <summary>
/// Engine-agnostic 8-direction math for billboard sprites. The direction depends on the angle between
/// where the character faces and where the camera looks, both on the ground plane, so it stays correct
/// when the camera turns with Q/E. Vectors are ground-plane (X, Z) pairs in Godot's axes: +X right,
/// -Z forward.
/// </summary>
public static class FacingDirections
{
    /// <summary>The number of directions.</summary>
    public const int Count = 8;

    private const float SectorRadians = MathF.Tau / Count;

    /// <summary>
    /// The direction the sprite shows for a character facing <paramref name="facing"/>, seen by a camera
    /// looking along <paramref name="cameraForward"/>. Neither vector needs to be normalised. A character
    /// facing the camera is <see cref="FacingDirection.South"/>; one walking to the screen's right is
    /// <see cref="FacingDirection.East"/>. Returns <see cref="FacingDirection.South"/> when either vector
    /// is zero.
    /// </summary>
    public static FacingDirection FromVectors(Vector2 facing, Vector2 cameraForward)
    {
        if (facing.LengthSquared() < 1e-12f || cameraForward.LengthSquared() < 1e-12f)
        {
            return FacingDirection.South;
        }

        // Camera right on the ground plane: forward (0, -1) has right (1, 0).
        var cameraRight = new Vector2(-cameraForward.Y, cameraForward.X);
        var away = Vector2.Dot(facing, cameraForward);
        var right = Vector2.Dot(facing, cameraRight);

        // Angle from "away from the camera" (north), clockwise towards screen right (east).
        var angle = MathF.Atan2(right, away);
        var sector = (int)MathF.Round(angle / SectorRadians, MidpointRounding.AwayFromZero);
        var fromNorth = ((sector % Count) + Count) % Count;
        return (FacingDirection)((fromNorth + (int)FacingDirection.North) % Count);
    }

    /// <summary>
    /// Same as <see cref="FromVectors"/>, with both directions given as yaw angles in degrees around +Y
    /// (Godot's <c>rotation.y</c>; yaw 0 looks along -Z).
    /// </summary>
    public static FacingDirection FromYaws(float facingYawDegrees, float cameraYawDegrees) =>
        FromVectors(YawToForward(facingYawDegrees), YawToForward(cameraYawDegrees));

    /// <summary>The ground-plane forward (X, Z) of a node turned <paramref name="yawDegrees"/> around +Y.</summary>
    public static Vector2 YawToForward(float yawDegrees)
    {
        var radians = yawDegrees * (MathF.PI / 180f);
        return new Vector2(-MathF.Sin(radians), -MathF.Cos(radians));
    }

    /// <summary>The frame row of <paramref name="direction"/> in a directional sprite sheet.</summary>
    public static int Row(FacingDirection direction) => (int)direction;

    /// <summary>The left-right mirror image: west becomes east, north and south stay.</summary>
    public static FacingDirection Mirror(FacingDirection direction) =>
        (FacingDirection)((Count - (int)direction) % Count);

    /// <summary>Short suffix for animation names: <c>s</c>, <c>sw</c>, <c>w</c>, <c>nw</c>, <c>n</c>, <c>ne</c>, <c>e</c>, <c>se</c>.</summary>
    public static string Suffix(FacingDirection direction) => direction switch
    {
        FacingDirection.South => "s",
        FacingDirection.SouthWest => "sw",
        FacingDirection.West => "w",
        FacingDirection.NorthWest => "nw",
        FacingDirection.North => "n",
        FacingDirection.NorthEast => "ne",
        FacingDirection.East => "e",
        FacingDirection.SouthEast => "se",
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    /// <summary>
    /// Picks the SpriteFrames animation for <paramref name="baseName"/> (for example <c>walk</c>) facing
    /// <paramref name="direction"/>, using <paramref name="hasAnimation"/> to see which exist:
    /// <list type="number">
    /// <item><c>walk_se</c>, if it exists;</item>
    /// <item>otherwise the mirror, <c>walk_sw</c>, drawn flipped left-right (so 5-direction sheets work);</item>
    /// <item>otherwise plain <c>walk</c>, not flipped.</item>
    /// </list>
    /// </summary>
    public static (string Name, bool FlipH) ResolveAnimation(
        string baseName, FacingDirection direction, Func<string, bool> hasAnimation)
    {
        ArgumentNullException.ThrowIfNull(baseName);
        ArgumentNullException.ThrowIfNull(hasAnimation);

        var exact = $"{baseName}_{Suffix(direction)}";
        if (hasAnimation(exact))
        {
            return (exact, false);
        }

        var mirror = Mirror(direction);
        if (mirror != direction)
        {
            var mirrored = $"{baseName}_{Suffix(mirror)}";
            if (hasAnimation(mirrored))
            {
                return (mirrored, true);
            }
        }

        return (baseName, false);
    }
}
