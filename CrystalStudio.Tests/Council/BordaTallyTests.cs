using CrystalStudio.Council;

using Xunit;

namespace CrystalStudio.Tests.Council;

public sealed class BordaTallyTests
{
    [Fact]
    public void Score_AwardsHighestPointsToFirstPlace()
    {
        var scores = BordaTally.Score(
            ["1", "2", "3"],
            [
                ["1", "2", "3"],
                ["1", "3", "2"]
            ]);

        Assert.Equal(4, scores["1"]);
        Assert.Equal(1, scores["2"]);
        Assert.Equal(1, scores["3"]);
        Assert.Equal("1", BordaTally.Winner(scores));
        Assert.False(BordaTally.IsDisputed(scores));
    }

    [Fact]
    public void IsDisputed_WhenTopScoresTie()
    {
        var scores = BordaTally.Score(
            ["1", "2"],
            [
                ["1", "2"],
                ["2", "1"]
            ]);

        Assert.True(BordaTally.IsDisputed(scores));
    }

    [Fact]
    public void Score_AcceptsProposalPrefixLabels()
    {
        var scores = BordaTally.Score(
            ["1", "2"],
            [["proposal 2", "Proposal 1"]]);

        Assert.Equal(1, scores["2"]);
        Assert.Equal(0, scores["1"]);
    }
}
