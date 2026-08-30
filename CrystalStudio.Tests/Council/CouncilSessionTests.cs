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
    public async Task RunAsync_ReturnsLeadingToolCallWhenMembersDisagree()
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
                        "{\"ranking\":[\"2\",\"1\"],\"risks\":[{\"id\":\"2\",\"level\":\"high\",\"note\":\"destructive\"}]}"),
                    ChatReplies.Text("{\"accept\":true,\"explanation\":\"borda winner\"}")
                ])
            });
        var session = new CouncilSession(settings, factory);

        var action = await session.RunAsync(
            new ChatRequest([new ChatMessage(ChatRole.User, "clean the disk")]),
            new SilentObserver(),
            CancellationToken.None);

        Assert.Equal(CouncilOutcome.ToolCall, action.Outcome);
        Assert.NotNull(action.ToolCall);
        Assert.Equal("bash", action.ToolCall.Name);
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

    [Fact]
    public async Task RunAsync_ReturnsAgreedReadOnlyBash()
    {
        const string listing = "{\"command\":\"ls -la zh-CN\"}";
        var settings = TwoMembers(maxRounds: 1);
        var factory = new ScriptedClientFactory(
            new Dictionary<string, IChatClient>(StringComparer.Ordinal)
            {
                ["analyst"] = new ScriptedClient(
                [
                    ChatReplies.Tool("c1", "bash", listing),
                    ChatReplies.Text("{\"ranking\":[\"1\",\"2\"],\"risks\":[]}")
                ]),
                ["chair"] = new ScriptedClient(
                [
                    ChatReplies.Tool("c2", "bash", listing),
                    ChatReplies.Text("{\"ranking\":[\"1\",\"2\"],\"risks\":[]}"),
                    ChatReplies.Text("{\"accept\":true,\"explanation\":\"needed listing\"}")
                ])
            });
        var session = new CouncilSession(settings, factory);

        var action = await session.RunAsync(
            new ChatRequest([new ChatMessage(ChatRole.User, "list the files")]),
            new SilentObserver(),
            CancellationToken.None);

        Assert.Equal(CouncilOutcome.ToolCall, action.Outcome);
        Assert.NotNull(action.ToolCall);
        Assert.Equal("bash", action.ToolCall.Name);
        Assert.Equal(listing, action.ToolCall.Arguments);
    }

    private static CouncilSettings TwoMembers(int maxRounds)
    {
        return new CouncilSettings(
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
