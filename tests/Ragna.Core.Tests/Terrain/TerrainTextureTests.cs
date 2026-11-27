using Ragna.Core.Terrain;

namespace Ragna.Core.Tests.Terrain;

public sealed class TerrainTextureTests
{
    [Theory]
    [InlineData(TerrainSurface.Grass)]
    [InlineData(TerrainSurface.Dirt)]
    [InlineData(TerrainSurface.Rock)]
    [InlineData(TerrainSurface.Sand)]
    [InlineData(TerrainSurface.Water)]
    public void Generates_deterministic_rgba_pixels(TerrainSurface surface)
    {
        var first = TerrainTexture.Generate(surface);
        var second = TerrainTexture.Generate(surface);

        Assert.Equal(TerrainTexture.DefaultSize * TerrainTexture.DefaultSize * 4, first.Length);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Water_is_translucent_and_land_is_opaque()
    {
        static IEnumerable<byte> Alpha(byte[] px) => px.Where((_, i) => i % 4 == 3);

        Assert.All(Alpha(TerrainTexture.Generate(TerrainSurface.Water)), a => Assert.True(a < 255));
        Assert.All(Alpha(TerrainTexture.Generate(TerrainSurface.Grass)), a => Assert.Equal(255, a));
    }

    [Fact]
    public void Textures_have_pixel_variation()
    {
        var px = TerrainTexture.Generate(TerrainSurface.Grass);
        var colours = Enumerable.Range(0, px.Length / 4).Select(i => (px[i * 4], px[(i * 4) + 1], px[(i * 4) + 2])).Distinct();

        Assert.True(colours.Count() > 1);
    }

    [Fact]
    public void File_names_are_lowercase_png()
    {
        Assert.Equal("grass.png", TerrainTexture.FileName(TerrainSurface.Grass));
    }

    [Fact]
    public void Rejects_empty_size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TerrainTexture.Generate(TerrainSurface.Grass, 0));
    }
}
