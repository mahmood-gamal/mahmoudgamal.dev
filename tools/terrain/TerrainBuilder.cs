using Godot;
using Ragna.Core.Terrain;
using Ragna.Gameplay.Terrain;

namespace Ragna.Tools.Terrain;

/// <summary>
/// Generates the terrain assets. Run it with tools/build_terrain.sh, which calls:
/// <c>godot --headless res://tools/terrain/TerrainBuilder.tscn -- &lt;step&gt;</c>
/// Steps:
/// <list type="bullet">
/// <item><c>textures</c>: writes placeholder PNGs (skips files that already exist, so hand-made art is kept).</item>
/// <item><c>library</c>: writes materials (skips existing ones) and always rebuilds the MeshLibrary.</item>
/// <item><c>map</c>: always rebuilds scenes/maps/TestMap.tscn from TestMapLayout.</item>
/// </list>
/// </summary>
public partial class TerrainBuilder : Node
{
    public const string TextureDir = "res://assets/terrain/textures";
    public const string MaterialDir = "res://assets/terrain/materials";
    public const string LibraryPath = "res://assets/terrain/terrain_tiles.tres";
    public const string TestMapPath = "res://scenes/maps/TestMap.tscn";

    public override void _Ready()
    {
        var steps = OS.GetCmdlineUserArgs();
        var exitCode = 0;
        try
        {
            foreach (var step in steps.Length > 0 ? steps : ["textures", "library", "map"])
            {
                switch (step)
                {
                    case "textures": WriteTextures(); break;
                    case "library": WriteLibrary(); break;
                    case "map": WriteTestMap(); break;
                    default: throw new ArgumentException($"Unknown step '{step}'. Use textures, library or map.");
                }
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"TerrainBuilder failed: {ex}");
            exitCode = 1;
        }

        GetTree().Quit(exitCode);
    }

    private static void WriteTextures()
    {
        EnsureDir(TextureDir);
        foreach (var surface in Enum.GetValues<TerrainSurface>())
        {
            var path = $"{TextureDir}/{TerrainTexture.FileName(surface)}";
            if (Godot.FileAccess.FileExists(path))
            {
                GD.Print($"keep   {path}");
                continue;
            }

            var size = TerrainTexture.DefaultSize;
            var image = Image.CreateFromData(size, size, false, Image.Format.Rgba8, TerrainTexture.Generate(surface));
            Check(image.SavePng(ProjectSettings.GlobalizePath(path)), path);
            GD.Print($"wrote  {path}");
        }
    }

    private static void WriteLibrary()
    {
        EnsureDir(MaterialDir);
        var materials = Enum.GetValues<TerrainSurface>().ToDictionary(s => s, LoadOrCreateMaterial);

        var library = new MeshLibrary();
        foreach (var tile in TerrainTileGeometry.All)
        {
            var id = (int)tile;
            var mesh = TerrainMeshBuilder.Build(tile, materials);
            library.CreateItem(id);
            library.SetItemName(id, TerrainTileGeometry.DisplayName(tile));
            library.SetItemMesh(id, mesh);
            // Collision lets mouse picking (and later navmesh baking) hit the terrain.
            library.SetItemShapes(id, new Godot.Collections.Array { mesh.CreateTrimeshShape(), Transform3D.Identity });
        }

        Check(ResourceSaver.Save(library, LibraryPath), LibraryPath);
        GD.Print($"wrote  {LibraryPath} ({TerrainTileGeometry.All.Count} items)");
    }

    private static Material LoadOrCreateMaterial(TerrainSurface surface)
    {
        var path = $"{MaterialDir}/{surface.ToString().ToLowerInvariant()}.tres";
        if (ResourceLoader.Exists(path))
        {
            GD.Print($"keep   {path}");
            return ResourceLoader.Load<Material>(path);
        }

        var texturePath = $"{TextureDir}/{TerrainTexture.FileName(surface)}";
        var texture = ResourceLoader.Load<Texture2D>(texturePath)
            ?? throw new InvalidOperationException($"{texturePath} isn't imported. Run the textures step, then godot --headless --import.");

        var material = new StandardMaterial3D
        {
            AlbedoTexture = texture,
            // Crisp pixel-art look up close.
            TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps,
            Roughness = 1f,
        };

        if (surface == TerrainSurface.Water)
        {
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            material.Roughness = 0.2f;
            material.Metallic = 0f;
            material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        }

        Check(ResourceSaver.Save(material, path), path);
        GD.Print($"wrote  {path}");
        return ResourceLoader.Load<Material>(path);
    }

    private static void WriteTestMap()
    {
        EnsureDir(TestMapPath.GetBaseDir());
        var library = ResourceLoader.Load<MeshLibrary>(LibraryPath)
            ?? throw new InvalidOperationException($"{LibraryPath} is missing. Run the library step first.");

        var root = new Node3D { Name = "TestMap" };

        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() },
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightEnergy = 0.7f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
        };
        Add(root, new WorldEnvironment { Name = "WorldEnvironment", Environment = env });

        // Soft sun: large angular size blurs shadow edges, low-ish energy keeps contrast gentle.
        Add(root, new DirectionalLight3D
        {
            Name = "Sun",
            RotationDegrees = new Vector3(-55f, -35f, 0f),
            LightColor = new Color(1f, 0.96f, 0.88f),
            LightEnergy = 0.9f,
            LightAngularDistance = 3f,
            ShadowEnabled = true,
            ShadowBlur = 1.5f,
            DirectionalShadowMaxDistance = 80f,
        });

        var grid = new GridMap
        {
            Name = "Terrain",
            MeshLibrary = library,
            CellSize = new Vector3(TerrainTileGeometry.CellSize, TerrainTileGeometry.CellHeight, TerrainTileGeometry.CellSize),
            CellCenterY = false,
        };
        Add(root, grid);

        var cells = TestMapLayout.Cells();
        foreach (var cell in cells)
        {
            grid.SetCellItem(new Vector3I(cell.X, cell.Y, cell.Z), (int)cell.Tile, TerrainMeshBuilder.Orientation(grid, cell.YawDegrees));
        }

        // Editor/F6 preview of the whole map at a 45° pitch. In game, GameCamera in Main.tscn takes over.
        var width = TestMapLayout.Layers[0][0].Length * TerrainTileGeometry.CellSize;
        var depth = TestMapLayout.Layers[0].Count * TerrainTileGeometry.CellSize;
        var centre = new Vector3(width / 2f, 0f, depth / 2f);
        var camera = new Camera3D { Name = "PreviewCamera", Fov = 50f, Current = true };
        Add(root, camera);
        camera.LookAtFromPosition(centre + new Vector3(0f, 30f, 30f), centre, Vector3.Up);

        var scene = new PackedScene();
        Check(scene.Pack(root), TestMapPath);
        Check(ResourceSaver.Save(scene, TestMapPath), TestMapPath);
        root.Free();
        GD.Print($"wrote  {TestMapPath} ({cells.Count} cells)");
    }

    private static void Add(Node root, Node child)
    {
        root.AddChild(child);
        child.Owner = root;
    }

    private static void EnsureDir(string resDir) =>
        Check(DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(resDir)), resDir);

    private static void Check(Error error, string what)
    {
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"{what}: {error}");
        }
    }
}
