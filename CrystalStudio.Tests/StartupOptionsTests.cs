using Xunit;

namespace CrystalStudio.Tests;

public sealed class StartupOptionsTests
{
    [Fact]
    public void Parse_ReadsPortAndHomes()
    {
        var options = StartupOptions.Parse(
            ["--port", "19001", "--studio-home", "/tmp/studio", "--harness-home", "/tmp/crystal"]);

        Assert.Equal(19001, options.Port);
        Assert.Equal("/tmp/studio", options.StudioHome);
        Assert.Equal("/tmp/crystal", options.HarnessHome);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    public void Parse_HelpDoesNotThrow()
    {
        var options = StartupOptions.Parse(["--help"]);
        Assert.True(options.ShowHelp);
    }
}
