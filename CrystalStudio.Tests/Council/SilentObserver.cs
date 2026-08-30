using CrystalStudio.Interfaces;

namespace CrystalStudio.Tests.Council;

internal sealed class SilentObserver : ICouncilObserver
{
    public Task ReportAsync(string message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
