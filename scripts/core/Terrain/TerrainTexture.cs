namespace Ragna.Core.Terrain;

/// <summary>
/// Generates small pixel-art placeholder textures. Output is deterministic, so
/// re-running the builder doesn't churn the committed PNGs.
/// </summary>
public static class TerrainTexture
{
    public const int DefaultSize = 16;

    /// <summary>RGBA8 pixels, row-major, <paramref name="size"/> x <paramref name="size"/>.</summary>
    public static byte[] Generate(TerrainSurface surface, int size = DefaultSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        var palette = Palette(surface);
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var color = palette[Pick(surface, x, y, size, palette.Length)];
                var i = ((y * size) + x) * 4;
                pixels[i] = color.R;
                pixels[i + 1] = color.G;
                pixels[i + 2] = color.B;
                pixels[i + 3] = color.A;
            }
        }

        return pixels;
    }

    public static string FileName(TerrainSurface surface) => $"{surface.ToString().ToLowerInvariant()}.png";

    private readonly record struct Rgba(byte R, byte G, byte B, byte A = 255);

    private static Rgba[] Palette(TerrainSurface surface) => surface switch
    {
        TerrainSurface.Grass => [new(86, 140, 60), new(98, 156, 68), new(74, 124, 52), new(120, 170, 80)],
        TerrainSurface.Dirt => [new(140, 108, 72), new(152, 120, 82), new(122, 92, 62), new(166, 134, 94)],
        TerrainSurface.Rock => [new(118, 112, 104), new(132, 126, 116), new(98, 94, 88), new(150, 144, 134)],
        TerrainSurface.Sand => [new(206, 186, 136), new(218, 198, 148), new(190, 170, 122), new(228, 210, 162)],
        TerrainSurface.Water => [new(60, 120, 190, 170), new(70, 134, 204, 170), new(52, 108, 176, 170), new(140, 190, 230, 170)],
        _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown terrain surface."),
    };

    // Mostly the first two shades, with sparse darker and lighter flecks.
    private static int Pick(TerrainSurface surface, int x, int y, int size, int count)
    {
        // Rock gets horizontal strata so cliff faces read as stone.
        var hash = Hash(x, y, (int)surface);
        if (surface == TerrainSurface.Rock && y % 5 == 4 && hash % 4 != 0)
        {
            return 2;
        }

        // Water gets a couple of light ripple lines.
        if (surface == TerrainSurface.Water && (y == size / 4 || y == (3 * size) / 4) && (x + y) % 4 < 2)
        {
            return 3;
        }

        var roll = hash % 16;
        return roll switch
        {
            < 7 => 0,
            < 13 => 1 % count,
            < 15 => 2 % count,
            _ => 3 % count,
        };
    }

    private static uint Hash(int x, int y, int seed)
    {
        unchecked
        {
            var h = (uint)((x * 73856093) ^ (y * 19349663) ^ (seed * 83492791));
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return h;
        }
    }
}
