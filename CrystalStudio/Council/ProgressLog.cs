using System.Text;

using CrystalStudio.Interfaces;

namespace CrystalStudio.Council;

/// <summary>
/// Thread-safe council progress buffer used as the compatible thinking stream.
/// </summary>
public sealed class ProgressLog : ICouncilObserver
{
    private readonly StringBuilder _text = new();
    private readonly object _gate = new();
    private readonly Func<string, CancellationToken, Task>? _onDelta;

    public ProgressLog(Func<string, CancellationToken, Task>? onDelta = null)
    {
        _onDelta = onDelta;
    }

    public string Text
    {
        get
        {
            lock (_gate)
            {
                return _text.ToString();
            }
        }
    }

    public async Task ReportAsync(string message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        var line = message.TrimEnd() + Environment.NewLine;
        lock (_gate)
        {
            _text.Append(line);
        }

        if (_onDelta is not null)
        {
            await _onDelta(line, cancellationToken);
        }
    }
}
