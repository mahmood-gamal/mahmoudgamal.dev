namespace Ragna.Core.Terrain;

/// <summary>A tile placed in a GridMap cell. <see cref="YawDegrees"/> is the rotation about +Y, counter-clockwise seen from above.</summary>
public readonly record struct TerrainCell(int X, int Y, int Z, TerrainTile Tile, int YawDegrees);

/// <summary>
/// Turns ASCII art into GridMap cells. Each layer is a block of rows; layer index
/// is the cell's Y (height step). Row index is +Z (down the page), column is +X.
/// <code>
/// .  grass        d  dirt path      ~  water      C  cliff block
/// ^  slope, uphill to -Z (up the page)    v  uphill to +Z
/// &gt;  slope, uphill to +X                  &lt;  uphill to -X
/// space or -  empty cell
/// </code>
/// </summary>
public static class TerrainMapLayout
{
    public static IReadOnlyList<TerrainCell> Parse(IReadOnlyList<IReadOnlyList<string>> layers)
    {
        var cells = new List<TerrainCell>();
        for (var y = 0; y < layers.Count; y++)
        {
            var rows = layers[y];
            for (var z = 0; z < rows.Count; z++)
            {
                var row = rows[z];
                for (var x = 0; x < row.Length; x++)
                {
                    if (TryParse(row[x], out var tile, out var yaw))
                    {
                        cells.Add(new TerrainCell(x, y, z, tile, yaw));
                    }
                    else if (row[x] is not (' ' or '-'))
                    {
                        throw new FormatException($"Unknown terrain symbol '{row[x]}' at layer {y}, row {z}, column {x}.");
                    }
                }
            }
        }

        return cells;
    }

    private static bool TryParse(char symbol, out TerrainTile tile, out int yaw)
    {
        yaw = 0;
        switch (symbol)
        {
            case '.': tile = TerrainTile.GrassFlat; return true;
            case 'd': tile = TerrainTile.DirtFlat; return true;
            case '~': tile = TerrainTile.Water; return true;
            case 'C': tile = TerrainTile.Cliff; return true;
            case '^': tile = TerrainTile.GrassSlope; return true;
            case 'v': tile = TerrainTile.GrassSlope; yaw = 180; return true;
            case '<': tile = TerrainTile.GrassSlope; yaw = 90; return true;
            case '>': tile = TerrainTile.GrassSlope; yaw = 270; return true;
            default: tile = default; return false;
        }
    }
}
