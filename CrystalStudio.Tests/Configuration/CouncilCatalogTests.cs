using CrystalStudio.Configuration;

using Xunit;

namespace CrystalStudio.Tests.Configuration;

public sealed class CouncilCatalogTests
{
    [Fact]
    public void Constructor_PrefersCrystalCouncilAsDefault()
    {
        var writing = CouncilSettings.CreateWritingDefault();
        var coding = CouncilSettings.CreateDefault();
        var catalog = new CouncilCatalog(
            CouncilCatalog.CreateDefaultListenPrefix(),
            [writing, coding]);

        Assert.Equal("crystal-council", catalog.Default.AdvertisedModel);
        Assert.Equal(18790, catalog.Port);
        Assert.Equal(["crystal-writing", "crystal-council"], catalog.AdvertisedModels);
    }

    [Fact]
    public void TryGet_SelectsByAdvertisedModel()
    {
        var catalog = new CouncilCatalog(
            CouncilCatalog.CreateDefaultListenPrefix(),
            [CouncilSettings.CreateDefault(), CouncilSettings.CreateWritingDefault()]);

        Assert.True(catalog.TryGet("crystal-writing", out var writing));
        Assert.Equal("crystal-writing", writing.AdvertisedModel);
        Assert.True(catalog.TryGet(null, out var fallback));
        Assert.Equal("crystal-council", fallback.AdvertisedModel);
        Assert.False(catalog.TryGet("missing", out _));
    }

    [Fact]
    public void WithPort_RebuildsTheListenPrefix()
    {
        var catalog = new CouncilCatalog(
            CouncilCatalog.CreateDefaultListenPrefix(),
            [CouncilSettings.CreateDefault()]).WithPort(19001);
        Assert.Equal(19001, catalog.ListenPrefix.Port);
    }

    [Fact]
    public void Constructor_RejectsDuplicateModels()
    {
        var first = CouncilSettings.CreateDefault();
        Assert.Throws<ArgumentException>(
            () => new CouncilCatalog(
                CouncilCatalog.CreateDefaultListenPrefix(),
                [first, first]));
    }
}
