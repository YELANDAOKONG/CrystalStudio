namespace CrystalStudio.Interfaces;

/// <summary>
/// Receives live council progress for the compatible thinking stream.
/// </summary>
public interface ICouncilObserver
{
    Task ReportAsync(string message, CancellationToken cancellationToken = default);
}
