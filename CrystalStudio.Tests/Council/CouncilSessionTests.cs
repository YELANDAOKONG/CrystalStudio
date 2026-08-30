using Crystal;
using Crystal.Chat;

using CrystalStudio.Configuration;
using CrystalStudio.Council;

using Xunit;

namespace CrystalStudio.Tests.Council;

public sealed class CouncilSessionTests
{
    [Fact]
    public async Task RunAsync_SelectsTheSharedOriginalProposal()
    {
        var settings = TwoMembers(maxRounds: 1);
        var factory = new ScriptedClientFactory(
            new Dictionary<string, IChatClient>(StringComparer.Ordinal)
            {
                ["analyst"] = new ScriptedClient(
                [
                    ChatReplies.Text("the shared complete answer", new TokenUsage(10, 4, 2)),
                    ChatReplies.Text("{\"ranking\":[\"1\",\"2\"],\"risks\":[]}", new TokenUsage(8, 3))
                ]),
                ["chair"] = new ScriptedClient(
                [
                    ChatReplies.Text("the shared complete answer", new TokenUsage(10, 4, 2)),
                    ChatReplies.Text("{\"ranking\":[\"1\",\"2\"],\"risks\":[]}", new TokenUsage(8, 3)),
                    ChatReplies.Text(
                        "{\"accept\":true,\"explanation\":\"clear winner\"}",
                        new TokenUsage(6, 2))
                ])
            });
        var session = new CouncilSession(settings, factory);

        var action = await session.RunAsync(
            new ChatRequest([new ChatMessage(ChatRole.User, "What is 2+2?")]),
            new SilentObserver(),
            CancellationToken.None);

        Assert.Equal(CouncilOutcome.Text, action.Outcome);
        Assert.Equal("the shared complete answer", action.Text);
        Assert.Equal(42, action.Usage.InputTokenCount);
        Assert.Equal(16, action.Usage.OutputTokenCount);
        Assert.Equal(58, action.Usage.TotalTokenCount);
        Assert.Equal(4, action.Usage.ReasoningTokenCount);
    }

    [Fact]
    public async Task RunAsync_DegradesHighRiskDisputedToolCall()
    {
        var settings = TwoMembers(maxRounds: 1);
        var factory = new ScriptedClientFactory(
            new Dictionary<string, IChatClient>(StringComparer.Ordinal)
            {
                ["analyst"] = new ScriptedClient(
                [
                    ChatReplies.Tool("c1", "bash", "{\"command\":\"rm -rf tmp\"}"),
                    ChatReplies.Text(
                        "{\"ranking\":[\"1\",\"2\"],\"risks\":[{\"id\":\"1\",\"level\":\"high\",\"note\":\"destructive\"}]}")
                ]),
                ["chair"] = new ScriptedClient(
                [
                    ChatReplies.Tool("c2", "bash", "{\"command\":\"rm -rf var\"}"),
                    ChatReplies.Text(
                        "{\"ranking\":[\"2\",\"1\"],\"risks\":[{\"id\":\"2\",\"level\":\"high\",\"note\":\"destructive\"}]}")
                ])
            });
        var session = new CouncilSession(settings, factory);

        var action = await session.RunAsync(
            new ChatRequest([new ChatMessage(ChatRole.User, "clean the disk")]),
            new SilentObserver(),
            CancellationToken.None);

        Assert.Equal(CouncilOutcome.Degraded, action.Outcome);
        Assert.Contains("will not execute", action.Text, StringComparison.Ordinal);
        Assert.Null(action.ToolCall);
    }

    [Fact]
    public async Task RunAsync_DegradesWhenChairRejects()
    {
        var settings = TwoMembers(maxRounds: 1);
        var factory = new ScriptedClientFactory(
            new Dictionary<string, IChatClient>(StringComparer.Ordinal)
            {
                ["analyst"] = new ScriptedClient(
                [
                    ChatReplies.Text("install the package"),
                    ChatReplies.Text("{\"ranking\":[\"1\",\"2\"],\"risks\":[]}")
                ]),
                ["chair"] = new ScriptedClient(
                [
                    ChatReplies.Text("install the package"),
                    ChatReplies.Text("{\"ranking\":[\"1\",\"2\"],\"risks\":[]}"),
                    ChatReplies.Text("{\"accept\":false,\"explanation\":\"not verified\"}")
                ])
            });
        var session = new CouncilSession(settings, factory);

        var action = await session.RunAsync(
            new ChatRequest([new ChatMessage(ChatRole.User, "install it")]),
            new SilentObserver(),
            CancellationToken.None);

        Assert.Equal(CouncilOutcome.Degraded, action.Outcome);
        Assert.Contains("chair rejected", action.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static CouncilSettings TwoMembers(int maxRounds)
    {
        return new CouncilSettings(
            new Uri("http://127.0.0.1:18790/"),
            maxRounds,
            0.85,
            TimeSpan.FromSeconds(5),
            "crystal-council",
            [
                new CouncilMember("analyst", "analyst persona", "deepseek", "deepseek-v4-flash"),
                new CouncilMember(
                    "chair",
                    "chair persona",
                    "deepseek",
                    "deepseek-v4-flash",
                    chair: true)
            ]);
    }
}
