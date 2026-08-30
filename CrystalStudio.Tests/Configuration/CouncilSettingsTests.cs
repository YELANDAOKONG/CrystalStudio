using CrystalStudio.Configuration;
using CrystalStudio.Home;

using Xunit;

namespace CrystalStudio.Tests.Configuration;

public sealed class CouncilSettingsTests
{
    [Fact]
    public void CreateDefault_UsesDeepSeekFlashForEverySeat()
    {
        var settings = CouncilSettings.CreateDefault();

        Assert.Equal(18790, settings.Port);
        Assert.Equal(4, settings.Members.Count);
        Assert.Equal("chair", settings.Chair.Id);
        Assert.All(
            settings.Members,
            static member =>
            {
                Assert.Equal("deepseek", member.Provider);
                Assert.Equal("deepseek-v4-flash", member.Model);
            });
    }

    [Fact]
    public void WithPort_RebuildsTheListenPrefix()
    {
        var settings = CouncilSettings.CreateDefault().WithPort(19001);
        Assert.Equal(19001, settings.ListenPrefix.Port);
    }

    [Fact]
    public void CouncilStore_RoundTripsAFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "crystal-studio-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new CouncilStore(new StudioHome(root));
            var created = store.LoadOrCreate();
            Assert.True(File.Exists(Path.Combine(root, "council.json")));

            var loaded = store.Load();
            Assert.Equal(created.AdvertisedModel, loaded.AdvertisedModel);
            Assert.Equal(created.Members.Count, loaded.Members.Count);
            Assert.Equal(created.Chair.Id, loaded.Chair.Id);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
