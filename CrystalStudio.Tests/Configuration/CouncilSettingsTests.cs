using CrystalStudio.Configuration;

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
                Assert.Equal("default", member.Thinking);
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
        Assert.All(
            settings.Members,
            static member =>
            {
                Assert.Contains("writing council", member.Persona, StringComparison.Ordinal);
                Assert.Contains("read the workspace", member.Persona, StringComparison.Ordinal);
            });
        Assert.Contains("Rank a shell listing", settings.Members[2].Persona, StringComparison.Ordinal);
        Assert.Contains("Accept a read of workspace files", settings.Chair.Persona, StringComparison.Ordinal);
    }

    [Fact]
    public void CouncilMember_NormalizesThinkingAliases()
    {
        var member = new CouncilMember(
            "analyst",
            "persona",
            "deepseek",
            "deepseek-v4-flash",
            thinking: "max");

        Assert.Equal("maximum", member.Thinking);
        Assert.Equal("off", new CouncilMember(
            "chair",
            "persona",
            "deepseek",
            "deepseek-v4-flash",
            thinking: "none").Thinking);
    }
}
