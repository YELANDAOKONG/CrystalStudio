using Crystal;
using Crystal.Chat;
using Crystal.Tools;

namespace CrystalStudio.Tests.Council;

internal static class ChatReplies
{
    public static ChatResponse Text(string text)
    {
        return new ChatResponse(
            [new ChatCandidate([new ChatMessage(ChatRole.Assistant, text)], FinishReason.Stop)]);
    }

    public static ChatResponse Tool(string callId, string name, string arguments)
    {
        return new ChatResponse(
            [
                new ChatCandidate(
                    [new ToolCall(callId, name, arguments)],
                    FinishReason.ToolCalls)
            ]);
    }
}
