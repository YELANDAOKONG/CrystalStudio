using Crystal;

using CrystalStudio.Council;

using Xunit;

namespace CrystalStudio.Tests.Council;

public sealed class UsageTallyTests
{
    [Fact]
    public void Snapshot_SumsEveryAddedUsage()
    {
        var tally = new UsageTally();
        tally.Add(new TokenUsage(10, 4, 2));
        tally.Add(new TokenUsage(5, 1));
        tally.Add(null);

        var usage = tally.Snapshot();
        Assert.Equal(15, usage.InputTokenCount);
        Assert.Equal(5, usage.OutputTokenCount);
        Assert.Equal(20, usage.TotalTokenCount);
        Assert.Equal(2, usage.ReasoningTokenCount);
    }

    [Fact]
    public void Snapshot_OmitsReasoningWhenNoProviderReportedIt()
    {
        var tally = new UsageTally();
        tally.Add(new TokenUsage(3, 1));

        Assert.Null(tally.Snapshot().ReasoningTokenCount);
    }
}
