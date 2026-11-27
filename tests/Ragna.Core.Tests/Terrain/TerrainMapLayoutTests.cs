using Ragna.Core.Terrain;

namespace Ragna.Core.Tests.Terrain;

public sealed class TerrainMapLayoutTests
{
    [Fact]
    public void Parses_symbols_into_cells()
    {
        var cells = TerrainMapLayout.Parse([[".d~C", "^v<>", " -"]]);

        Assert.Equal(
            [
                new TerrainCell(0, 0, 0, TerrainTile.GrassFlat, 0),
                new TerrainCell(1, 0, 0, TerrainTile.DirtFlat, 0),
                new TerrainCell(2, 0, 0, TerrainTile.Water, 0),
                new TerrainCell(3, 0, 0, TerrainTile.Cliff, 0),
                new TerrainCell(0, 0, 1, TerrainTile.GrassSlope, 0),
                new TerrainCell(1, 0, 1, TerrainTile.GrassSlope, 180),
                new TerrainCell(2, 0, 1, TerrainTile.GrassSlope, 90),
                new TerrainCell(3, 0, 1, TerrainTile.GrassSlope, 270),
            ],
            cells);
    }

    [Fact]
    public void Layer_index_becomes_cell_height()
    {
        var cells = TerrainMapLayout.Parse([["C"], ["."]]);

        Assert.Equal(new TerrainCell(0, 1, 0, TerrainTile.GrassFlat, 0), cells[1]);
    }

    [Fact]
    public void Rejects_unknown_symbols()
    {
        var ex = Assert.Throws<FormatException>(() => TerrainMapLayout.Parse([["..x"]]));

        Assert.Contains("'x'", ex.Message);
        Assert.Contains("column 2", ex.Message);
    }

    [Fact]
    public void Test_map_uses_every_tile()
    {
        var used = TestMapLayout.Cells().Select(c => c.Tile).ToHashSet();

        Assert.Equal(TerrainTileGeometry.All.ToHashSet(), used);
    }

    [Fact]
    public void Test_map_upper_cells_stand_on_cliffs()
    {
        var cells = TestMapLayout.Cells();
        var byPos = cells.ToDictionary(c => (c.X, c.Y, c.Z));

        foreach (var cell in cells.Where(c => c.Y > 0))
        {
            Assert.True(
                byPos.TryGetValue((cell.X, cell.Y - 1, cell.Z), out var below) && below.Tile == TerrainTile.Cliff,
                $"Cell {cell} has no cliff below it.");
        }
    }

    [Fact]
    public void Test_map_ground_layer_is_fully_covered()
    {
        var ground = TestMapLayout.Layers[0];
        var width = ground[0].Length;

        Assert.All(ground, row => Assert.Equal(width, row.Length));
        Assert.All(ground, row => Assert.DoesNotContain(' ', row));
        Assert.All(ground, row => Assert.DoesNotContain('-', row));
    }

    [Fact]
    public void Test_map_slopes_lead_up_onto_a_cliff()
    {
        var cells = TestMapLayout.Cells();
        var byPos = cells.ToDictionary(c => (c.X, c.Y, c.Z));

        foreach (var slope in cells.Where(c => c.Tile == TerrainTile.GrassSlope))
        {
            var (dx, dz) = slope.YawDegrees switch
            {
                0 => (0, -1),
                180 => (0, 1),
                90 => (-1, 0),
                270 => (1, 0),
                _ => throw new InvalidOperationException(),
            };

            // Uphill neighbour is a cliff on the same layer (its top is the slope's high edge)...
            var uphillIsCliff = byPos.TryGetValue((slope.X + dx, slope.Y, slope.Z + dz), out var up) && up.Tile == TerrainTile.Cliff;
            Assert.True(uphillIsCliff, $"Slope {slope} doesn't lead onto a cliff.");
        }
    }
}
