using Crystal.Chat;

namespace CrystalStudio.Tests.Council;

internal sealed class FailingChatClient : IChatClient
{
    private readonly Exception _exception;

    public FailingChatClient(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _exception = exception;
    }

    public Task<ChatResponse> CompleteAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromException<ChatResponse>(_exception);
    }
}
