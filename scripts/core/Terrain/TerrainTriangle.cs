using System.Numerics;

namespace Ragna.Core.Terrain;

/// <summary>
/// One triangle of a tile mesh. Vertices are counter-clockwise when seen from the
/// outside, so <see cref="Normal"/> (right-hand rule) points out of the tile.
/// </summary>
public readonly record struct TerrainTriangle(TerrainSurface Surface, Vector3 A, Vector3 B, Vector3 C)
{
    public Vector3 Normal => Vector3.Normalize(Vector3.Cross(B - A, C - A));

    public IEnumerable<Vector3> Vertices
    {
        get
        {
            yield return A;
            yield return B;
            yield return C;
        }
    }
}
