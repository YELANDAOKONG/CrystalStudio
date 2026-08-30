using Crystal.Chat;

namespace CrystalStudio.Tests.Council;

internal sealed class ScriptedClient : IChatClient
{
    private readonly Queue<ChatResponse> _responses;

    public ScriptedClient(IEnumerable<ChatResponse> responses)
    {
        _responses = new Queue<ChatResponse>(responses);
    }

    public int Remaining => _responses.Count;

    public Task<ChatResponse> CompleteAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No scripted chat response remains.");
        }

        return Task.FromResult(_responses.Dequeue());
    }
}
