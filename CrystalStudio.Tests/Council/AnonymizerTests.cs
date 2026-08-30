using CrystalStudio.Council;

using Xunit;

namespace CrystalStudio.Tests.Council;

public sealed class AnonymizerTests
{
    [Fact]
    public void Shuffle_UsesNumericLabelsAndHidesMemberOrder()
    {
        var proposals = new List<Proposal>
        {
            new("analyst", 1, "alpha"),
            new("skeptic", 1, "beta"),
            new("engineer", 1, "gamma")
        };

        var labeled = Anonymizer.Shuffle(proposals, seed: 7);
        Assert.Equal(["1", "2", "3"], labeled.Select(static item => item.Label));
        Assert.Equal(3, labeled.Select(static item => item.Proposal.MemberId).Distinct().Count());
        Assert.DoesNotContain(
            labeled,
            static item => item.Label.Contains("analyst", StringComparison.Ordinal));
    }
}
