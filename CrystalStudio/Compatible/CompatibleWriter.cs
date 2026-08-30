using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Crystal.Tools;

using CrystalStudio.Council;

namespace CrystalStudio.Compatible;

/// <summary>
/// Writes OpenAI-compatible Chat Completions JSON and SSE chunks.
/// </summary>
public sealed class CompatibleWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly Stream _stream;
    private readonly string _id;
    private readonly string _model;
    private readonly long _created;
    private readonly bool _streamResponse;
    private bool _roleSent;

    public CompatibleWriter(Stream stream, string id, string model, bool streamResponse)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        _stream = stream;
        _id = id;
        _model = model;
        _created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _streamResponse = streamResponse;
    }

    public async Task WriteThinkingAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!_streamResponse || text.Length == 0)
        {
            return;
        }

        var delta = new JsonObject
        {
            ["reasoning_content"] = text
        };
        if (!_roleSent)
        {
            delta["role"] = "assistant";
            _roleSent = true;
        }

        await WriteChunkAsync(delta, finishReason: null, cancellationToken);
    }

    public async Task WriteActionAsync(CouncilAction action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_streamResponse)
        {
            await WriteStreamedActionAsync(action, cancellationToken);
            return;
        }

        await WriteObjectAsync(BuildCompletion(action), cancellationToken);
    }

    public async Task WriteErrorAsync(string message, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        var payload = new JsonObject
        {
            ["error"] = new JsonObject
            {
                ["message"] = message,
                ["type"] = "invalid_request_error",
                ["code"] = null
            }
        };
        await WriteObjectAsync(payload, cancellationToken);
    }

    public async Task WriteModelsAsync(string advertisedModel, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(advertisedModel);
        var payload = new JsonObject
        {
            ["object"] = "list",
            ["data"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = advertisedModel,
                    ["object"] = "model",
                    ["created"] = _created,
                    ["owned_by"] = "crystal-studio"
                }
            }
        };
        await WriteObjectAsync(payload, cancellationToken);
    }

    private async Task WriteStreamedActionAsync(
        CouncilAction action,
        CancellationToken cancellationToken)
    {
        if (!_roleSent)
        {
            var open = new JsonObject { ["role"] = "assistant" };
            await WriteChunkAsync(open, finishReason: null, cancellationToken);
            _roleSent = true;
        }

        if (action.Outcome == CouncilOutcome.ToolCall && action.ToolCall is { } call)
        {
            var delta = new JsonObject
            {
                ["tool_calls"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["index"] = 0,
                        ["id"] = call.CallId,
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = call.Name,
                            ["arguments"] = call.Arguments
                        }
                    }
                }
            };
            await WriteChunkAsync(delta, finishReason: null, cancellationToken);
            await WriteChunkAsync(new JsonObject(), "tool_calls", cancellationToken);
        }
        else
        {
            if (!string.IsNullOrEmpty(action.Text))
            {
                await WriteChunkAsync(
                    new JsonObject { ["content"] = action.Text },
                    finishReason: null,
                    cancellationToken);
            }

            await WriteChunkAsync(new JsonObject(), "stop", cancellationToken);
        }

        await WriteRawAsync("data: [DONE]\n\n", cancellationToken);
    }

    private JsonObject BuildCompletion(CouncilAction action)
    {
        var message = new JsonObject
        {
            ["role"] = "assistant",
            ["reasoning_content"] = action.Reasoning
        };

        if (action.Outcome == CouncilOutcome.ToolCall && action.ToolCall is { } call)
        {
            message["content"] = string.IsNullOrEmpty(action.Text) ? null : action.Text;
            message["tool_calls"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = call.CallId,
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = call.Name,
                        ["arguments"] = call.Arguments
                    }
                }
            };
        }
        else
        {
            message["content"] = action.Text;
        }

        var finish = action.Outcome == CouncilOutcome.ToolCall ? "tool_calls" : "stop";
        return new JsonObject
        {
            ["id"] = _id,
            ["object"] = "chat.completion",
            ["created"] = _created,
            ["model"] = _model,
            ["choices"] = new JsonArray
            {
                new JsonObject
                {
                    ["index"] = 0,
                    ["message"] = message,
                    ["finish_reason"] = finish
                }
            },
            ["usage"] = new JsonObject
            {
                ["prompt_tokens"] = 0,
                ["completion_tokens"] = 0,
                ["total_tokens"] = 0
            }
        };
    }

    private async Task WriteChunkAsync(
        JsonObject delta,
        string? finishReason,
        CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["id"] = _id,
            ["object"] = "chat.completion.chunk",
            ["created"] = _created,
            ["model"] = _model,
            ["choices"] = new JsonArray
            {
                new JsonObject
                {
                    ["index"] = 0,
                    ["delta"] = delta,
                    ["finish_reason"] = finishReason
                }
            }
        };
        await WriteRawAsync("data: " + payload.ToJsonString(JsonOptions) + "\n\n", cancellationToken);
    }

    private async Task WriteObjectAsync(JsonObject payload, CancellationToken cancellationToken)
    {
        await WriteRawAsync(payload.ToJsonString(JsonOptions), cancellationToken);
    }

    private async Task WriteRawAsync(string text, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await _stream.WriteAsync(bytes, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }
}
