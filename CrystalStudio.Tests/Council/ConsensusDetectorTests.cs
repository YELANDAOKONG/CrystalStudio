using Crystal.Tools;

using CrystalStudio.Council;

using Xunit;

namespace CrystalStudio.Tests.Council;

public sealed class ConsensusDetectorTests
{
    [Fact]
    public void ShouldStop_WhenRoundCapIsReached()
    {
        var proposals = new List<Proposal>
        {
            new("a", 1, "one unique answer about cats"),
            new("b", 1, "another unique answer about orbit")
        };

        Assert.True(ConsensusDetector.ShouldStop(proposals, 2, 2, 0.99));
    }

    [Fact]
    public void ShouldStop_WhenTextsMatch()
    {
        var proposals = new List<Proposal>
        {
            new("a", 1, "the same complete answer"),
            new("b", 1, "the same complete answer")
        };

        Assert.True(ConsensusDetector.ShouldStop(proposals, 1, 3, 0.85));
    }

    [Fact]
    public void ShouldStop_WhenToolCallsMatchRegardlessOfOrder()
    {
        var first = new ToolCall("c1", "read", "{\"path\":\"a.md\"}");
        var second = new ToolCall("c2", "read", "{\"path\":\"b.md\"}");
        var proposals = new List<Proposal>
        {
            new("a", 1, string.Empty, [first, second]),
            new("b", 1, string.Empty, [second, first])
        };

        Assert.True(ConsensusDetector.ShouldStop(proposals, 1, 3, 0.85));
    }
}
