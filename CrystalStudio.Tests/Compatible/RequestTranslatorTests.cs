using Crystal.Chat;
using Crystal.Reasoning;
using Crystal.Tools;

using CrystalStudio.Compatible;

using Xunit;

namespace CrystalStudio.Tests.Compatible;

public sealed class RequestTranslatorTests
{
    [Fact]
    public void TryRead_MapsMessagesToolsAndStream()
    {
        var body =
            """
            {
              "model": "crystal-council",
              "stream": true,
              "messages": [
                {"role": "system", "content": "be brief"},
                {"role": "user", "content": [{"type": "text", "text": "hello"}]}
              ],
              "tools": [
                {
                  "type": "function",
                  "function": {
                    "name": "read",
                    "description": "Read a file",
                    "parameters": {"type": "object"}
                  }
                }
              ]
            }
            """;

        Assert.True(RequestTranslator.TryRead(body, out var request, out var stream, out var model, out var error));
        Assert.True(stream);
        Assert.Equal("crystal-council", model);
        Assert.Equal(string.Empty, error);
        Assert.Equal(2, request.Items.Count);
        Assert.Equal("hello", ((ChatMessage)request.Items[1]).Text);
        Assert.Equal("read", request.Tools[0].Name);
    }

    [Fact]
    public void TryRead_MapsToolResults()
    {
        var body =
            """
            {
              "messages": [
                {"role": "assistant", "tool_calls": [{"id": "c1", "function": {"name": "read", "arguments": "{}"}}]},
                {"role": "tool", "tool_call_id": "c1", "content": "ok"}
              ]
            }
            """;

        Assert.True(RequestTranslator.TryRead(body, out var request, out _, out _, out _));
        Assert.IsType<ToolCall>(request.Items[0]);
        var result = Assert.IsType<ToolResult>(request.Items[1]);
        Assert.Equal("ok", result.Text);
    }

    [Fact]
    public void TryRead_MapsAssistantReasoningContent()
    {
        var body =
            """
            {
              "messages": [
                {"role": "user", "content": "again"},
                {
                  "role": "assistant",
                  "reasoning_content": "Council opened.",
                  "content": "the previous answer"
                }
              ]
            }
            """;

        Assert.True(RequestTranslator.TryRead(body, out var request, out _, out _, out _));
        var reasoning = Assert.IsType<ChatReasoningItem>(request.Items[1]);
        Assert.Equal("Council opened.", reasoning.Content.TextSegments[0].Text);
        Assert.Equal("the previous answer", ((ChatMessage)request.Items[2]).Text);
    }

    [Fact]
    public void TryRead_RejectsEmptyMessages()
    {
        Assert.False(RequestTranslator.TryRead("{\"messages\":[]}", out _, out _, out _, out var error));
        Assert.Contains("messages", error, StringComparison.Ordinal);
    }
}
