namespace Ragna.Core.Terrain;

/// <summary>
/// The tiles in the terrain MeshLibrary. The numeric value is the GridMap item ID,
/// so painted maps keep working: never renumber, only append.
/// </summary>
public enum TerrainTile
{
    GrassFlat = 0,
    DirtFlat = 1,
    GrassSlope = 2,
    Cliff = 3,
    Water = 4,
}

/// <summary>The material a terrain triangle is drawn with.</summary>
public enum TerrainSurface
{
    Grass,
    Dirt,
    Rock,
    Sand,
    Water,
}
