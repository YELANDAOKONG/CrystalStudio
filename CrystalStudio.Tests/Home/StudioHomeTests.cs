using CrystalStudio.Home;

using Xunit;

namespace CrystalStudio.Tests.Home;

public sealed class StudioHomeTests
{
    private static readonly object EnvironmentGate = new();

    [Fact]
    public void CombineDefault_UsesLowercaseCrystalStudioSegments()
    {
        var profile = Path.Combine(
            Path.GetTempPath(),
            "crystal-studio-profile-" + Guid.NewGuid().ToString("N"));
        var root = StudioHome.CombineDefault(profile);

        Assert.Equal(StudioHome.DirectoryName, Path.GetFileName(root));
        Assert.Equal(
            StudioHome.ParentDirectoryName,
            Path.GetFileName(Path.GetDirectoryName(root)));
        Assert.Equal(".crystal", StudioHome.ParentDirectoryName);
        Assert.Equal("studio", StudioHome.DirectoryName);
    }

    [Fact]
    public void Resolve_UsesAnExplicitRoot()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "crystal-studio-root-" + Guid.NewGuid().ToString("N"));
        var home = StudioHome.Resolve(root);

        Assert.Equal(Path.GetFullPath(root), home.Root);
        Assert.Equal(StudioHome.CouncilsDirectoryName, Path.GetFileName(home.CouncilsDirectory));
    }

    [Fact]
    public void Resolve_HonorsTheEnvironmentVariable()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "crystal-studio-env-" + Guid.NewGuid().ToString("N"));
        lock (EnvironmentGate)
        {
            var previous = Environment.GetEnvironmentVariable(StudioHome.EnvironmentVariableName);
            try
            {
                Environment.SetEnvironmentVariable(StudioHome.EnvironmentVariableName, root);
                var home = StudioHome.Resolve();
                Assert.Equal(Path.GetFullPath(root), home.Root);
            }
            finally
            {
                Environment.SetEnvironmentVariable(StudioHome.EnvironmentVariableName, previous);
            }
        }
    }
}
