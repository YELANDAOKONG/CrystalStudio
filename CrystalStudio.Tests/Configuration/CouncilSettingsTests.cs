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

        Assert.Equal("crystal-council", settings.AdvertisedModel);
        Assert.Equal(4, settings.Members.Count);
        Assert.Equal("chair", settings.Chair.Id);
        Assert.Equal("analyst", settings.Members[0].Id);
        Assert.All(
            settings.Members,
            static member =>
            {
                Assert.Equal("deepseek", member.Provider);
                Assert.Equal("deepseek-v4-flash", member.Model);
            });
    }

    [Fact]
    public void CreateWritingDefault_UsesBookSeats()
    {
        var settings = CouncilSettings.CreateWritingDefault();

        Assert.Equal("crystal-writing", settings.AdvertisedModel);
        Assert.Equal(4, settings.Members.Count);
        Assert.Equal("architect", settings.Members[0].Id);
        Assert.Equal("stylist", settings.Members[1].Id);
        Assert.Equal("critic", settings.Members[2].Id);
        Assert.Equal("chair", settings.Chair.Id);
    }
}
