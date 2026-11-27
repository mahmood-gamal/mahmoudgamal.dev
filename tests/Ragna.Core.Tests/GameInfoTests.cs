namespace Ragna.Core.Tests;

public sealed class GameInfoTests
{
    [Fact]
    public void Current_describes_the_game()
    {
        var info = GameInfo.Current;

        Assert.Equal("Ragna", info.Name);
        Assert.False(string.IsNullOrWhiteSpace(info.Version));
        Assert.Equal($"Ragna v{info.Version}", info.ToString());
    }
}
