using System.Text.Json.Nodes;

using Crystal.Tools;

using CrystalStudio.Compatible;
using CrystalStudio.Council;

using Xunit;

namespace CrystalStudio.Tests.Compatible;

public sealed class CompatibleWriterTests
{
    [Fact]
    public async Task WriteModelsAsync_ListsEveryAdvertisedId()
    {
        using var stream = new MemoryStream();
        var writer = new CompatibleWriter(stream, "models", "crystal-council", streamResponse: false);

        await writer.WriteModelsAsync(["crystal-council", "crystal-writing"], CancellationToken.None);

        stream.Position = 0;
        var document = JsonNode.Parse(stream);
        var ids = document!["data"]!.AsArray().Select(static node => node!["id"]!.GetValue<string>()).ToArray();
        Assert.Equal(["crystal-council", "crystal-writing"], ids);
    }

    [Fact]
    public async Task WriteActionAsync_WritesEveryToolCall()
    {
        using var stream = new MemoryStream();
        var writer = new CompatibleWriter(stream, "cmpl-1", "crystal-council", streamResponse: false);
        var action = new CouncilAction(
            CouncilOutcome.ToolCall,
            string.Empty,
            [
                new ToolCall("c1", "read", "{\"path\":\"a.md\"}"),
                new ToolCall("c2", "read", "{\"path\":\"b.md\"}")
            ]);

        await writer.WriteActionAsync(action, CancellationToken.None);

        stream.Position = 0;
        var document = JsonNode.Parse(stream);
        var calls = document!["choices"]![0]!["message"]!["tool_calls"]!.AsArray();
        Assert.Equal(2, calls.Count);
        Assert.Equal("c1", calls[0]!["id"]!.GetValue<string>());
        Assert.Equal("c2", calls[1]!["id"]!.GetValue<string>());
        Assert.Equal("read", calls[0]!["function"]!["name"]!.GetValue<string>());
        Assert.Equal("{\"path\":\"b.md\"}", calls[1]!["function"]!["arguments"]!.GetValue<string>());
        Assert.Equal("tool_calls", document["choices"]![0]!["finish_reason"]!.GetValue<string>());
    }
}
