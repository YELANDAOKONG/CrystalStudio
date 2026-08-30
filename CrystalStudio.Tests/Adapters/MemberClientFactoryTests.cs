using Crystal.Reasoning;

using CrystalHarness.Configuration;
using CrystalHarness.Home;

using CrystalStudio.Adapters;
using CrystalStudio.Configuration;

using Xunit;

namespace CrystalStudio.Tests.Adapters;

public sealed class MemberClientFactoryTests
{
    [Fact]
    public void ResolveReasoning_EnablesAnAllowedEffort()
    {
        using var factory = CreateFactory();
        var member = new CouncilMember(
            "analyst",
            "persona",
            "deepseek",
            "deepseek-v4-flash",
            thinking: "high");

        var options = factory.ResolveReasoning(member);

        Assert.NotNull(options);
        Assert.Equal(ReasoningMode.Enabled, options.Mode);
        Assert.Equal(ReasoningEffort.High, options.Effort);
    }

    [Fact]
    public void ResolveReasoning_DisablesWhenOff()
    {
        using var factory = CreateFactory();
        var member = new CouncilMember(
            "analyst",
            "persona",
            "deepseek",
            "deepseek-v4-flash",
            thinking: "off");

        var options = factory.ResolveReasoning(member);

        Assert.NotNull(options);
        Assert.Equal(ReasoningMode.Disabled, options.Mode);
        Assert.Null(options.Effort);
    }

    [Fact]
    public void ResolveReasoning_UsesAutomaticForDefaultOnAThinkingModel()
    {
        using var factory = CreateFactory();
        var member = new CouncilMember(
            "analyst",
            "persona",
            "deepseek",
            "deepseek-v4-flash");

        var options = factory.ResolveReasoning(member);

        Assert.NotNull(options);
        Assert.Equal(ReasoningMode.Automatic, options.Mode);
    }

    [Fact]
    public void ResolveReasoning_OmitsHintsWhenTheModelDoesNotThink()
    {
        using var factory = CreateFactory();
        var member = new CouncilMember(
            "analyst",
            "persona",
            "openai",
            "gpt-5.6-sol",
            thinking: "high");

        Assert.Null(factory.ResolveReasoning(member));
    }

    private static MemberClientFactory CreateFactory()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "crystal-studio-tests",
            Guid.NewGuid().ToString("N"));
        return new MemberClientFactory(
            HarnessSettings.CreateDefault(),
            new CredentialStore(new CrystalHome(root)));
    }
}
