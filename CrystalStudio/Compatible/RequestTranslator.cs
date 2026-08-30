using System.Text.Json;

using Crystal.Chat;
using Crystal.Reasoning;
using Crystal.Tools;

namespace CrystalStudio.Compatible;

/// <summary>
/// Maps an OpenAI Chat Completions body onto a Crystal <see cref="ChatRequest"/>.
/// </summary>
public static class RequestTranslator
{
    private static readonly JsonElement EmptySchema = JsonDocument.Parse("{}").RootElement.Clone();

    public static bool TryRead(
        string body,
        out ChatRequest request,
        out bool stream,
        out string model,
        out string error)
    {
        request = new ChatRequest([new ChatMessage(ChatRole.User, string.Empty)]);
        stream = false;
        model = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(body))
        {
            error = "Request body is empty.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            stream = root.TryGetProperty("stream", out var streamNode)
                && streamNode.ValueKind == JsonValueKind.True;
            model = root.TryGetProperty("model", out var modelNode)
                && modelNode.ValueKind == JsonValueKind.String
                    ? modelNode.GetString() ?? string.Empty
                    : string.Empty;

            if (!root.TryGetProperty("messages", out var messagesNode)
                || messagesNode.ValueKind != JsonValueKind.Array
                || messagesNode.GetArrayLength() == 0)
            {
                error = "messages must be a non-empty array.";
                return false;
            }

            var items = new List<ChatItem>();
            foreach (var message in messagesNode.EnumerateArray())
            {
                if (!TryAddMessage(message, items, out error))
                {
                    return false;
                }
            }

            if (items.Count == 0)
            {
                error = "messages did not contain any usable content.";
                return false;
            }

            var tools = ReadTools(root);
            request = new ChatRequest(items, tools);
            return true;
        }
        catch (JsonException exception)
        {
            error = $"Request body was not valid JSON: {exception.Message}";
            return false;
        }
    }

    private static bool TryAddMessage(
        JsonElement message,
        List<ChatItem> items,
        out string error)
    {
        error = string.Empty;
        var role = message.TryGetProperty("role", out var roleNode)
            && roleNode.ValueKind == JsonValueKind.String
                ? roleNode.GetString() ?? string.Empty
                : string.Empty;
        var content = ReadContent(message);

        if (string.Equals(role, "tool", StringComparison.OrdinalIgnoreCase))
        {
            var callId = message.TryGetProperty("tool_call_id", out var idNode)
                && idNode.ValueKind == JsonValueKind.String
                    ? idNode.GetString()
                    : null;
            if (string.IsNullOrWhiteSpace(callId))
            {
                error = "A tool message needs tool_call_id.";
                return false;
            }

            items.Add(new ToolResult(callId, content));
            return true;
        }

        if (string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase))
        {
            AddReasoning(message, items);
            if (!string.IsNullOrEmpty(content))
            {
                items.Add(new ChatMessage(ChatRole.Assistant, content));
            }

            if (message.TryGetProperty("tool_calls", out var calls)
                && calls.ValueKind == JsonValueKind.Array)
            {
                foreach (var call in calls.EnumerateArray())
                {
                    if (!TryReadToolCall(call, out var toolCall, out error))
                    {
                        return false;
                    }

                    items.Add(toolCall);
                }
            }

            return true;
        }

        var chatRole = role.ToLowerInvariant() switch
        {
            "system" => ChatRole.System,
            "user" => ChatRole.User,
            _ => ChatRole.User
        };
        items.Add(new ChatMessage(chatRole, content));
        return true;
    }

    private static bool TryReadToolCall(
        JsonElement call,
        out ToolCall toolCall,
        out string error)
    {
        error = string.Empty;
        toolCall = new ToolCall("unused", "unused", "{}");
        var id = call.TryGetProperty("id", out var idNode)
            && idNode.ValueKind == JsonValueKind.String
                ? idNode.GetString()
                : null;
        JsonElement function = default;
        if (call.TryGetProperty("function", out var functionNode)
            && functionNode.ValueKind == JsonValueKind.Object)
        {
            function = functionNode;
        }

        var name = function.ValueKind == JsonValueKind.Object
            && function.TryGetProperty("name", out var nameNode)
            && nameNode.ValueKind == JsonValueKind.String
                ? nameNode.GetString()
                : null;
        var arguments = function.ValueKind == JsonValueKind.Object
            && function.TryGetProperty("arguments", out var argumentsNode)
                ? argumentsNode.ValueKind == JsonValueKind.String
                    ? argumentsNode.GetString()
                    : argumentsNode.GetRawText()
                : "{}";

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            error = "Each tool_call needs id and function.name.";
            return false;
        }

        toolCall = new ToolCall(id, name, arguments ?? "{}");
        return true;
    }

    private static IReadOnlyList<ToolDefinition> ReadTools(JsonElement root)
    {
        if (!root.TryGetProperty("tools", out var toolsNode)
            || toolsNode.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var tools = new List<ToolDefinition>();
        foreach (var tool in toolsNode.EnumerateArray())
        {
            if (!tool.TryGetProperty("function", out var function)
                || function.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!function.TryGetProperty("name", out var nameNode)
                || nameNode.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(nameNode.GetString()))
            {
                continue;
            }

            var name = nameNode.GetString()!;
            var description = function.TryGetProperty("description", out var descriptionNode)
                && descriptionNode.ValueKind == JsonValueKind.String
                    ? descriptionNode.GetString()
                    : null;
            var schema = function.TryGetProperty("parameters", out var parameters)
                && parameters.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null
                    ? parameters.Clone()
                    : EmptySchema.Clone();
            tools.Add(new ToolDefinition(name, schema, description));
        }

        return tools;
    }

    private static void AddReasoning(JsonElement message, List<ChatItem> items)
    {
        if (!message.TryGetProperty("reasoning_content", out var node)
            || node.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var text = node.GetString();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        items.Add(
            new ChatReasoningItem(
                new ReasoningContent([new ReasoningText(text, ReasoningTextKind.Trace)])));
    }

    private static string ReadContent(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content)
            || content.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return string.Empty;
        }

        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        if (content.ValueKind != JsonValueKind.Array)
        {
            return content.GetRawText();
        }

        var pieces = new List<string>();
        foreach (var part in content.EnumerateArray())
        {
            if (part.ValueKind == JsonValueKind.String)
            {
                pieces.Add(part.GetString() ?? string.Empty);
                continue;
            }

            if (part.ValueKind == JsonValueKind.Object
                && part.TryGetProperty("text", out var text)
                && text.ValueKind == JsonValueKind.String)
            {
                pieces.Add(text.GetString() ?? string.Empty);
            }
        }

        return string.Join(string.Empty, pieces);
    }
}
