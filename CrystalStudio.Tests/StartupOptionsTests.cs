using Xunit;

namespace CrystalStudio.Tests;

public sealed class StartupOptionsTests
{
    [Fact]
    public void Parse_ReadsPortAndHomes()
    {
        var studioHome = Path.Combine("home", "studio");
        var harnessHome = Path.Combine("home", "crystal");
        var options = StartupOptions.Parse(
            ["--port", "19001", "--studio-home", studioHome, "--harness-home", harnessHome]);

        Assert.Equal(19001, options.Port);
        Assert.Equal(studioHome, options.StudioHome);
        Assert.Equal(harnessHome, options.HarnessHome);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    public void Parse_HelpDoesNotThrow()
    {
        var options = StartupOptions.Parse(["--help"]);
        Assert.True(options.ShowHelp);
    }
}
