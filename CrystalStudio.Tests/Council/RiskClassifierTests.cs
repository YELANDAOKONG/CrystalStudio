using Crystal.Tools;

using CrystalStudio.Council;

using Xunit;

namespace CrystalStudio.Tests.Council;

public sealed class RiskClassifierTests
{
    [Fact]
    public void IsHighRisk_ForBashTool()
    {
        var proposal = new Proposal(
            "engineer",
            1,
            string.Empty,
            new ToolCall("call_1", "bash", "{\"command\":\"echo hi\"}"));

        Assert.True(RiskClassifier.IsHighRisk(proposal));
    }

    [Fact]
    public void IsHighRisk_ForForceRemoveFragment()
    {
        var call = new ToolCall("call_1", "run", "{\"command\":\"rm -rf tmp\"}");
        Assert.True(RiskClassifier.IsHighRisk(call));
    }

    [Fact]
    public void IsHighRisk_IsFalseForPlainText()
    {
        Assert.False(RiskClassifier.IsHighRisk(new Proposal("analyst", 1, "just text")));
    }
}
