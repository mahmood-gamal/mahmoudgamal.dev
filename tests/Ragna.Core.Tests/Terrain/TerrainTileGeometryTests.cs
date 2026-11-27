using System.Numerics;
using Ragna.Core.Terrain;

namespace Ragna.Core.Tests.Terrain;

public sealed class TerrainTileGeometryTests
{
    private const float H = TerrainTileGeometry.CellSize / 2f;
    private const float Epsilon = 1e-5f;

    public static TheoryData<TerrainTile> Tiles()
    {
        var data = new TheoryData<TerrainTile>();
        foreach (var tile in TerrainTileGeometry.All)
        {
            data.Add(tile);
        }

        return data;
    }

    [Fact]
    public void Item_ids_are_stable()
    {
        // Painted GridMaps store these numbers; changing them breaks saved maps.
        Assert.Equal(0, (int)TerrainTile.GrassFlat);
        Assert.Equal(1, (int)TerrainTile.DirtFlat);
        Assert.Equal(2, (int)TerrainTile.GrassSlope);
        Assert.Equal(3, (int)TerrainTile.Cliff);
        Assert.Equal(4, (int)TerrainTile.Water);
    }

    [Theory]
    [MemberData(nameof(Tiles))]
    public void Every_tile_has_a_name_and_geometry(TerrainTile tile)
    {
        Assert.False(string.IsNullOrWhiteSpace(TerrainTileGeometry.DisplayName(tile)));
        Assert.NotEmpty(TerrainTileGeometry.Build(tile));
    }

    [Theory]
    [MemberData(nameof(Tiles))]
    public void Geometry_stays_inside_its_cell_footprint(TerrainTile tile)
    {
        foreach (var v in TerrainTileGeometry.Build(tile).SelectMany(t => t.Vertices))
        {
            Assert.InRange(v.X, -H - Epsilon, H + Epsilon);
            Assert.InRange(v.Z, -H - Epsilon, H + Epsilon);
            Assert.InRange(v.Y, TerrainTileGeometry.WaterBedY - Epsilon, TerrainTileGeometry.CellHeight + Epsilon);
        }
    }

    [Theory]
    [MemberData(nameof(Tiles))]
    public void Triangles_face_outward(TerrainTile tile)
    {
        foreach (var tri in TerrainTileGeometry.Build(tile))
        {
            var n = tri.Normal;
            var centroid = (tri.A + tri.B + tri.C) / 3f;
            if (MathF.Abs(n.Y) > 0.1f)
            {
                // Ground-like faces must face the sky.
                Assert.True(n.Y > 0, $"{tile} {tri.Surface} face points down: {n}");
            }
            else
            {
                // Walls must face away from the cell centre.
                var outward = (n.X * centroid.X) + (n.Z * centroid.Z);
                Assert.True(outward > 0, $"{tile} {tri.Surface} wall faces inward: {n}");
            }
        }
    }

    [Fact]
    public void Flat_tiles_sit_at_ground_level()
    {
        foreach (var tile in new[] { TerrainTile.GrassFlat, TerrainTile.DirtFlat })
        {
            Assert.All(TerrainTileGeometry.Build(tile).SelectMany(t => t.Vertices), v => Assert.Equal(0f, v.Y));
        }
    }

    [Fact]
    public void Cliff_top_is_one_step_up_and_has_four_rock_sides()
    {
        var tris = TerrainTileGeometry.Build(TerrainTile.Cliff);

        var top = tris.Where(t => t.Surface == TerrainSurface.Grass).ToList();
        Assert.Equal(2, top.Count);
        Assert.All(top.SelectMany(t => t.Vertices), v => Assert.Equal(TerrainTileGeometry.CellHeight, v.Y));

        var sideNormals = tris.Where(t => t.Surface == TerrainSurface.Rock)
            .Select(t => Round(t.Normal))
            .Distinct()
            .ToHashSet();
        Assert.Equal([Vector3.UnitX, -Vector3.UnitX, Vector3.UnitZ, -Vector3.UnitZ], sideNormals);
    }

    [Fact]
    public void Slope_rises_from_ground_at_plus_z_to_one_step_at_minus_z()
    {
        var ramp = TerrainTileGeometry.Build(TerrainTile.GrassSlope)
            .Where(t => t.Surface == TerrainSurface.Grass)
            .ToList();

        Assert.Equal(2, ramp.Count);
        foreach (var v in ramp.SelectMany(t => t.Vertices))
        {
            var expected = v.Z > 0 ? 0f : TerrainTileGeometry.CellHeight;
            Assert.Equal(expected, v.Y);
        }

        // Normal leans downhill (towards +Z).
        Assert.All(ramp, t => Assert.True(t.Normal.Z > 0));
    }

    [Fact]
    public void Water_surface_sits_between_bed_and_ground()
    {
        var tris = TerrainTileGeometry.Build(TerrainTile.Water);

        Assert.All(
            tris.Where(t => t.Surface == TerrainSurface.Water).SelectMany(t => t.Vertices),
            v => Assert.Equal(TerrainTileGeometry.WaterSurfaceY, v.Y));
        Assert.All(
            tris.Where(t => t.Surface == TerrainSurface.Sand).SelectMany(t => t.Vertices),
            v => Assert.Equal(TerrainTileGeometry.WaterBedY, v.Y));
        Assert.True(TerrainTileGeometry.WaterBedY < TerrainTileGeometry.WaterSurfaceY);
        Assert.True(TerrainTileGeometry.WaterSurfaceY < 0);
    }

    [Fact]
    public void Uvs_repeat_once_per_cell_so_neighbours_tile_seamlessly()
    {
        var up = Vector3.UnitY;
        var west = TerrainTileGeometry.Uv(new Vector3(-H, 0, 0), up);
        var east = TerrainTileGeometry.Uv(new Vector3(H, 0, 0), up);

        Assert.Equal(1f, east.X - west.X, 5);

        var wall = TerrainTileGeometry.Uv(new Vector3(0, TerrainTileGeometry.CellHeight, H), Vector3.UnitZ);
        var wallBase = TerrainTileGeometry.Uv(new Vector3(0, 0, H), Vector3.UnitZ);
        Assert.True(wall.Y < wallBase.Y, "Wall texture should be upright (v grows downward).");
    }

    private static Vector3 Round(Vector3 v) => new(MathF.Round(v.X), MathF.Round(v.Y), MathF.Round(v.Z));
}
