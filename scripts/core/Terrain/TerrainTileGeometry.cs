using System.Numerics;

namespace Ragna.Core.Terrain;

/// <summary>
/// Low-poly placeholder geometry for each <see cref="TerrainTile"/>.
/// A tile spans x and z in [-HalfSize, HalfSize]. Ground level is y = 0 and one
/// height step is y = <see cref="CellHeight"/>. This matches a GridMap with
/// cell size (CellSize, CellHeight, CellSize) and cell_center_y off.
/// </summary>
public static class TerrainTileGeometry
{
    /// <summary>Width and depth of one cell, in metres.</summary>
    public const float CellSize = 2f;

    /// <summary>Height of one cell (one cliff or slope step), in metres.</summary>
    public const float CellHeight = 1f;

    /// <summary>Height of the water surface, below ground level.</summary>
    public const float WaterSurfaceY = -0.15f;

    /// <summary>Height of the pond bed under the water surface.</summary>
    public const float WaterBedY = -0.5f;

    private const float H = CellSize / 2f;

    public static IReadOnlyList<TerrainTile> All { get; } = Enum.GetValues<TerrainTile>();

    /// <summary>Builds the triangles for one tile.</summary>
    public static IReadOnlyList<TerrainTriangle> Build(TerrainTile tile) => tile switch
    {
        TerrainTile.GrassFlat => FlatTop(TerrainSurface.Grass, 0f),
        TerrainTile.DirtFlat => FlatTop(TerrainSurface.Dirt, 0f),
        TerrainTile.GrassSlope => Slope(),
        TerrainTile.Cliff => Cliff(),
        TerrainTile.Water => Water(),
        _ => throw new ArgumentOutOfRangeException(nameof(tile), tile, "Unknown terrain tile."),
    };

    /// <summary>Human-readable name used for the MeshLibrary item.</summary>
    public static string DisplayName(TerrainTile tile) => tile switch
    {
        TerrainTile.GrassFlat => "Grass (flat)",
        TerrainTile.DirtFlat => "Dirt path (flat)",
        TerrainTile.GrassSlope => "Grass slope (rises to -Z)",
        TerrainTile.Cliff => "Cliff block",
        TerrainTile.Water => "Water",
        _ => throw new ArgumentOutOfRangeException(nameof(tile), tile, "Unknown terrain tile."),
    };

    /// <summary>
    /// Texture coordinates for a vertex. Projected in world units so textures line
    /// up across neighbouring cells: one texture repeat per cell.
    /// </summary>
    public static Vector2 Uv(Vector3 position, Vector3 normal)
    {
        var n = Vector3.Abs(normal);
        Vector2 uv;
        if (n.Y >= n.X && n.Y >= n.Z)
        {
            uv = new Vector2(position.X, position.Z);
        }
        else if (n.X >= n.Z)
        {
            uv = new Vector2(position.Z, -position.Y);
        }
        else
        {
            uv = new Vector2(position.X, -position.Y);
        }

        return (uv / CellSize) + new Vector2(0.5f, 0.5f);
    }

    private static List<TerrainTriangle> FlatTop(TerrainSurface surface, float y)
    {
        var tris = new List<TerrainTriangle>();
        Quad(tris, surface, new(-H, y, H), new(H, y, H), new(H, y, -H), new(-H, y, -H));
        return tris;
    }

    // Ramp from ground (y = 0) at +Z up to one step (y = CellHeight) at -Z.
    private static List<TerrainTriangle> Slope()
    {
        const float top = CellHeight;
        var tris = new List<TerrainTriangle>();
        Quad(tris, TerrainSurface.Grass, new(-H, 0, H), new(H, 0, H), new(H, top, -H), new(-H, top, -H));
        // Back wall under the high edge.
        Quad(tris, TerrainSurface.Rock, new(H, 0, -H), new(-H, 0, -H), new(-H, top, -H), new(H, top, -H));
        // Triangular side walls.
        tris.Add(new(TerrainSurface.Rock, new(H, 0, H), new(H, 0, -H), new(H, top, -H)));
        tris.Add(new(TerrainSurface.Rock, new(-H, 0, -H), new(-H, 0, H), new(-H, top, -H)));
        return tris;
    }

    // Full-height block: grass on top, rock on the four sides.
    private static List<TerrainTriangle> Cliff()
    {
        const float top = CellHeight;
        var tris = FlatTop(TerrainSurface.Grass, top);
        Quad(tris, TerrainSurface.Rock, new(-H, 0, H), new(H, 0, H), new(H, top, H), new(-H, top, H));     // +Z
        Quad(tris, TerrainSurface.Rock, new(H, 0, -H), new(-H, 0, -H), new(-H, top, -H), new(H, top, -H)); // -Z
        Quad(tris, TerrainSurface.Rock, new(H, 0, H), new(H, 0, -H), new(H, top, -H), new(H, top, H));     // +X
        Quad(tris, TerrainSurface.Rock, new(-H, 0, -H), new(-H, 0, H), new(-H, top, H), new(-H, top, -H)); // -X
        return tris;
    }

    // Sand pond bed with a translucent water surface above it.
    private static List<TerrainTriangle> Water()
    {
        var tris = FlatTop(TerrainSurface.Sand, WaterBedY);
        Quad(tris, TerrainSurface.Water, new(-H, WaterSurfaceY, H), new(H, WaterSurfaceY, H), new(H, WaterSurfaceY, -H), new(-H, WaterSurfaceY, -H));
        return tris;
    }

    // a, b, c, d counter-clockwise seen from outside.
    private static void Quad(List<TerrainTriangle> tris, TerrainSurface surface, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        tris.Add(new(surface, a, b, c));
        tris.Add(new(surface, a, c, d));
    }
}
