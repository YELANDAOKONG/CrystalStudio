using CrystalStudio.Council;

using Xunit;

namespace CrystalStudio.Tests.Council;

public sealed class ReviewParserTests
{
    [Fact]
    public void TryParse_ReadsRankingAndRisksFromProse()
    {
        var ballot = ReviewParser.TryParse(
            "analyst",
            """
            Here is my review.
            {"ranking":["2","1"],"risks":[{"id":"1","level":"high","note":"deletes files"}]}
            """);

        Assert.NotNull(ballot);
        Assert.Equal(["2", "1"], ballot.Ranking);
        Assert.True(ballot.Risks[0].IsHigh);
    }

    [Fact]
    public void TryParse_ReturnsNullWhenJsonIsMissing()
    {
        Assert.Null(ReviewParser.TryParse("analyst", "I like the second one more."));
    }

    [Fact]
    public void TryParseChair_ReadsAcceptFlag()
    {
        Assert.True(ReviewParser.TryParseChair(
            "{\"accept\":false,\"explanation\":\"too risky\"}",
            out var accept,
            out var explanation));
        Assert.False(accept);
        Assert.Equal("too risky", explanation);
    }
}
