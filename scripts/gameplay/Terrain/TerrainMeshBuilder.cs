using Godot;
using Ragna.Core.Terrain;
using NVector3 = System.Numerics.Vector3;

namespace Ragna.Gameplay.Terrain;

/// <summary>Turns the engine-agnostic tile geometry from Ragna.Core into Godot meshes.</summary>
public static class TerrainMeshBuilder
{
    /// <summary>Builds one mesh per tile, with one surface per material it uses.</summary>
    public static ArrayMesh Build(TerrainTile tile, IReadOnlyDictionary<TerrainSurface, Material> materials)
    {
        var mesh = new ArrayMesh();
        foreach (var group in TerrainTileGeometry.Build(tile).GroupBy(t => t.Surface))
        {
            var st = new SurfaceTool();
            st.Begin(Mesh.PrimitiveType.Triangles);
            foreach (var tri in group)
            {
                var n = tri.Normal;
                // Core winds counter-clockwise; Godot treats clockwise as front-facing.
                foreach (var v in new[] { tri.A, tri.C, tri.B })
                {
                    st.SetNormal(ToGodot(n));
                    st.SetUV(ToGodot(TerrainTileGeometry.Uv(v, n)));
                    st.AddVertex(ToGodot(v));
                }
            }

            st.Index();
            st.GenerateTangents();
            st.SetMaterial(materials[group.Key]);
            st.Commit(mesh);
            mesh.SurfaceSetName(mesh.GetSurfaceCount() - 1, group.Key.ToString());
        }

        return mesh;
    }

    /// <summary>
    /// Collision for one tile, from <see cref="TerrainTileGeometry.Collision"/>. It is used for mouse
    /// picking and navmesh baking, so the water surface is left out.
    /// </summary>
    public static ConcavePolygonShape3D BuildCollision(TerrainTile tile)
    {
        var faces = new List<Vector3>();
        foreach (var tri in TerrainTileGeometry.Collision(tile))
        {
            // Same winding as the visible mesh in Build.
            faces.Add(ToGodot(tri.A));
            faces.Add(ToGodot(tri.C));
            faces.Add(ToGodot(tri.B));
        }

        return new ConcavePolygonShape3D { Data = [.. faces] };
    }

    /// <summary>GridMap orientation index for a yaw (degrees about +Y).</summary>
    public static int Orientation(GridMap gridMap, int yawDegrees) =>
        gridMap.GetOrthogonalIndexFromBasis(new Basis(Vector3.Up, Mathf.DegToRad(yawDegrees)));

    private static Vector3 ToGodot(NVector3 v) => new(v.X, v.Y, v.Z);

    private static Vector2 ToGodot(System.Numerics.Vector2 v) => new(v.X, v.Y);
}
