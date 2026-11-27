namespace Ragna.Core.Terrain;

/// <summary>
/// The M1 test map: open meadow, a dirt path, a pond, and a two-step plateau
/// reached by slopes. 24 x 18 cells (48 m x 36 m). See TerrainMapLayout for symbols.
/// </summary>
public static class TestMapLayout
{
    public static IReadOnlyList<IReadOnlyList<string>> Layers { get; } =
    [
        // Layer 0: ground.
        [
            "........................",
            "........................",
            "..CCCCCCCC..............",
            "..CCCCCCCC......~~~.....",
            "..CCCCCCCC.....~~~~~....",
            "..CCCCCCCC.....~~~~~~...",
            "..CCCCCCCC......~~~~....",
            "..CCCCCCCC..............",
            "..CCCCCCCC..............",
            "....^.dddddddddddddddd..",
            "......d...............>C",
            "......d................C",
            "......d....CCC.........C",
            "......d....CCC..........",
            "dddddddddddCCC..........",
            "..........>CCC..........",
            "........................",
            "........................",
        ],
        // Layer 1: on top of the layer-0 cliffs.
        [
            "",
            "",
            "--------",
            "--CCCC--",
            "--CCCC--",
            "--CCCC--",
            "----^---",
            "--------",
        ],
    ];

    public static IReadOnlyList<TerrainCell> Cells() => TerrainMapLayout.Parse(Layers);
}
